using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class ConnectionOperationProfile
    {
        public OperationKind operation;
        public string activityDescription;
        [Min(0.01f)] public float minimumLoad = 1f;
        [Min(0.01f)] public float maximumLoad = 3f;
        [Min(0.01f)] public float minimumDuration = 40f;
        [Min(0.01f)] public float maximumDuration = 75f;
        [Min(0f)] public float meanWork;
        public CompletionEffect completionEffect;
        public TimedModifierBehaviorDefinition[] behaviors;
    }

    [Serializable]
    public sealed class ConnectionTrafficProfile
    {
        public string id;
        public string claimedPurpose;
        public string[] eligibleTargetIds;
        [Min(0f)] public float ambientWeight = 1f;
        public bool purposeTemplate;
        public bool isHostile;
        public TimedModifierBehaviorDefinition[] connectionBehaviors;
        public ConnectionOperationProfile[] operations;
    }

    [Serializable]
    public sealed class AmbientWeightOverride
    {
        public string profileId;
        [Min(0f)] public float weight;
    }

    // Owns only generation and scheduling. SimulationHost owns every live connection.
    public sealed class ConnectionTrafficDirector : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private bool autoGenerationEnabled = true;
        [SerializeField] private int seed = 1701;
        [SerializeField] private bool randomizeSeed = true;
        [SerializeField, Min(0f)] private float initialGraceSeconds = 3f;
        [Header("Benign population (soft coverage)")]
        [SerializeField, Min(0f)] private float benignPopulationGoal = 3f;
        [SerializeField, Min(0f)] private float coverageStartupGoal = 1f;
        [SerializeField, Min(0.01f)] private float coverageRampSeconds = 75f;
        [SerializeField, Min(0.01f)] private float coverageMeanIntervalSeconds = 8f;
        [SerializeField, Min(0f)] private float coverageResponse = 0.6f;
        [SerializeField, Min(0f)] private float coverageCancellationMemorySeconds = 60f;
        [SerializeField, Range(0f, 1f)] private float coverageAtGoalChance = 0.12f;
        [SerializeField, Min(0.1f)] private float coverageOutageRetryMaxSeconds = 20f;
        [SerializeField] private string ambientOriginId = "EXT1";
        [SerializeField] private ConnectionTrafficProfile[] profiles;
        [Tooltip("Optional canonical PURPOSE weights by profile ID. Built-in operations stay intact; zero disables ambient selection but not spawn <profileId>.")]
        [SerializeField] private AmbientWeightOverride[] ambientWeightOverrides;
        [Header("Hostile ambient arrivals")]
        [SerializeField] private bool hostileGenerationEnabled = true;
        [SerializeField, Min(0.01f)] private float hostileMeanIntervalSeconds = 54f;
        [Header("Purpose generation (provisional playtest rates)")]
        [SerializeField, Range(0f, 1f)] private float deliveryChance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float benignOutlierChance = 0.08f;
        [SerializeField, Range(0f, 1f)] private float secondAnomalyChance = 0.15f;
        [SerializeField, Range(0f, 1f)] private float subtleDeliveryChance = 0.1f;
        [SerializeField, Range(0f, 0.5f)] private float normalWorkSpread = 0.25f;
        [Header("LOADER (provisional)")]
        [SerializeField, Range(0f, 1f)] private float loaderDelayedChance = 0.35f;
        [SerializeField, Min(0f)] private float loaderMinAttackCapacityFraction = 0.8f;
        [SerializeField, Min(0f)] private float loaderMaxAttackCapacityFraction = 1.2f;
        [SerializeField, Min(0f)] private float loaderMinOnsetSeconds = 5f;
        [SerializeField, Min(0f)] private float loaderMaxOnsetSeconds = 15f;
        [SerializeField, Min(0f)] private float loaderSpikeQuietSeconds = 5f;
        [SerializeField, Min(0f)] private float loaderSpikeElevatedSeconds = 2f;
        [SerializeField, Min(0f)] private float loaderMinDurationSeconds = 30f;
        [SerializeField, Min(0f)] private float loaderMaxDurationSeconds = 60f;
        [Header("Delivery infection weights")]
        [SerializeField, Min(0f)] private float phantomLoadWeight = 1f;
        [SerializeField, Min(0f)] private float blackoutWeight = 1f;
        [SerializeField, Min(0f)] private float spawnerWeight = 1f;

        private sealed class Queued
        {
            public ConnectionSpawnPlan Plan;
            public long Order;
        }
        private readonly List<Queued> queue = new List<Queued>();
        private readonly HashSet<string> reservedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<float> recentBenignCancellations = new List<float>();
        private const string IdLetters = "abcdefghijklmnopqrstuvwxyz";
        private const string IdDigits = "0123456789";
        private const int CanceledAttemptWeight = 2;
        private const float ImminentPlanSeconds = 5f;
        private string[] shuffledIds;
        private int nextIdIndex;
        private bool idsExhausted;
        private System.Random random;
        private System.Random spawnerRandom;
        private System.Random coverageRandom;
        private System.Random benignPlanRandom;
        private System.Random hostileScheduleRandom;
        private float nextHostileAt;
        private float nextCoverageAt;
        private bool hostileClockPaused;
        private int lastBlackoutCount;
        private long nextOrder;
        private readonly Dictionary<string, float> effectiveWeights =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public bool AutoGenerationEnabled { get => autoGenerationEnabled; set => autoGenerationEnabled = value; }
        public int Seed { get => seed; set => seed = value; }
        public bool RandomizeSeed { get => randomizeSeed; set => randomizeSeed = value; }
        public IReadOnlyList<ConnectionTrafficProfile> Profiles => profiles;
        public float InitialGraceSeconds => initialGraceSeconds;
        public bool HostileGenerationEnabled { get => hostileGenerationEnabled; set => hostileGenerationEnabled = value; }
        public float HostileMeanIntervalSeconds => hostileMeanIntervalSeconds;
        public float BenignPopulationGoal => benignPopulationGoal;
        public float CoverageStartupGoal => coverageStartupGoal;
        public float CoverageRampSeconds => coverageRampSeconds;
        public float CoverageMeanIntervalSeconds => coverageMeanIntervalSeconds;
        public float CoverageResponse => coverageResponse;
        public float CoverageCancellationMemorySeconds => coverageCancellationMemorySeconds;
        public float CoverageAtGoalChance => coverageAtGoalChance;
        public float CoverageOutageRetryMaxSeconds => coverageOutageRetryMaxSeconds;
        public float CoverageImminentPlanSeconds => ImminentPlanSeconds;
        public int CoverageCanceledAttemptWeight => CanceledAttemptWeight;
        public string AmbientOriginId => ambientOriginId;
        public float DeliveryChance => deliveryChance;
        public float BenignOutlierChance => benignOutlierChance;
        public float SecondAnomalyChance => secondAnomalyChance;
        public float SubtleDeliveryChance => subtleDeliveryChance;
        public float NormalWorkSpread => normalWorkSpread;
        public float LoaderDelayedChance => loaderDelayedChance;
        public float LoaderMinAttackCapacityFraction => loaderMinAttackCapacityFraction;
        public float LoaderMaxAttackCapacityFraction => loaderMaxAttackCapacityFraction;
        public float LoaderMinOnsetSeconds => loaderMinOnsetSeconds;
        public float LoaderMaxOnsetSeconds => loaderMaxOnsetSeconds;
        public float LoaderSpikeQuietSeconds => loaderSpikeQuietSeconds;
        public float LoaderSpikeElevatedSeconds => loaderSpikeElevatedSeconds;
        public float LoaderMinDurationSeconds => loaderMinDurationSeconds;
        public float LoaderMaxDurationSeconds => loaderMaxDurationSeconds;
        public float PhantomLoadWeight => phantomLoadWeight;
        public float BlackoutWeight => blackoutWeight;
        public float SpawnerWeight => spawnerWeight;
        public float EffectiveAmbientWeight(string profileId) =>
            profileId != null && effectiveWeights.TryGetValue(profileId, out float weight) ? weight : 0f;

        // Called once by the host at Play start, before director reset and recorder startup.
        // A new seed changes the run; later simulation resets replay this same seed.
        public void BeginPlaySession()
        {
            if (!randomizeSeed) return;
            int chosen = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & int.MaxValue;
            seed = chosen == seed ? (chosen == int.MaxValue ? 0 : chosen + 1) : chosen;
        }

        private void OnDisable()
        {
            if (host != null) host.ConnectionResolved -= OnConnectionResolved;
        }

        private void OnConnectionResolved(LogicalConnectionState state, ConnectionResolution reason)
        {
            if (reason == ConnectionResolution.Canceled && !state.IsHostile)
                recentBenignCancellations.Add(state.ResolvedAtSeconds);
        }

        public void ResetDirector()
        {
            // Edit Mode witnesses reset the director without MonoBehaviour enable callbacks.
            if (host != null)
            {
                host.ConnectionResolved -= OnConnectionResolved;
                host.ConnectionResolved += OnConnectionResolved;
            }
            random = new System.Random(seed);
            spawnerRandom = new System.Random(unchecked(seed ^ (int)0x53A7B10));
            coverageRandom = new System.Random(unchecked(seed ^ (int)0xC0B3A63));
            benignPlanRandom = new System.Random(unchecked(seed ^ (int)0xB36E610));
            hostileScheduleRandom = new System.Random(unchecked(seed ^ (int)0x4853541));
            queue.Clear();
            recentBenignCancellations.Clear();
            reservedIds.Clear();
            shuffledIds = new string[IdLetters.Length * IdDigits.Length];
            for (int i = 0; i < shuffledIds.Length; i++)
                shuffledIds[i] = new string(new[] { IdLetters[i / IdDigits.Length],
                    IdDigits[i % IdDigits.Length] });
            // A separate stream means ID changes cannot shift traffic/profile draws.
            var idRandom = new System.Random(unchecked(seed ^ (int)0x5EED1D5));
            for (int i = shuffledIds.Length - 1; i > 0; i--)
            {
                int swap = idRandom.Next(i + 1);
                (shuffledIds[i], shuffledIds[swap]) = (shuffledIds[swap], shuffledIds[i]);
            }
            nextIdIndex = 0;
            idsExhausted = false;
            nextOrder = 0;
            nextHostileAt = initialGraceSeconds + HostileDelay();
            nextCoverageAt = initialGraceSeconds + CoverageDelay(0,
                EffectiveCoverageGoal(initialGraceSeconds));
            hostileClockPaused = false;
            lastBlackoutCount = 0;
            if (profiles == null || profiles.Length == 0)
                profiles = DefaultProfiles();
            RebuildEffectiveWeights();
        }

        private void RebuildEffectiveWeights()
        {
            effectiveWeights.Clear();
            foreach (ConnectionTrafficProfile profile in profiles)
                if (profile != null && !string.IsNullOrWhiteSpace(profile.id))
                {
                    if (effectiveWeights.ContainsKey(profile.id))
                        throw new InvalidOperationException("Duplicate traffic profile ID: " + profile.id);
                    effectiveWeights.Add(profile.id, profile.ambientWeight);
                }
            if (ambientWeightOverrides == null) return;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (AmbientWeightOverride entry in ambientWeightOverrides)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.profileId) ||
                    float.IsNaN(entry.weight) || float.IsInfinity(entry.weight) || entry.weight < 0f)
                    throw new InvalidOperationException("Ambient weight overrides require an ID and finite nonnegative weight.");
                if (!seen.Add(entry.profileId))
                    throw new InvalidOperationException("Duplicate ambient weight override: " + entry.profileId);
                if (!effectiveWeights.ContainsKey(entry.profileId))
                    throw new InvalidOperationException("Unknown ambient weight profile: " + entry.profileId);
                effectiveWeights[entry.profileId] = entry.weight;
            }
        }

        public void QueueSpawnPlan(ConnectionSpawnPlan plan, string source = "explicit")
        {
            if (plan == null || string.IsNullOrWhiteSpace(plan.ConnectionId) ||
                !reservedIds.Add(plan.ConnectionId))
                throw new InvalidOperationException("Spawn plans require a unique, nonempty connection ID.");
            try { host.ValidatePlan(plan); }
            catch { reservedIds.Remove(plan.ConnectionId); throw; }
            queue.Add(new Queued { Plan = plan, Order = nextOrder++ });
            host.Recorder?.RecordPlanQueued(plan, source, host.SimulationTimeSeconds);
        }

        public void Tick(float simulationTime, bool allowAmbient)
        {
            if (random == null) ResetDirector();
            int blackoutCount = 0;
            foreach (NodeInfection infection in host.Infections)
                if (infection.Payload.Kind == InfectionKind.Blackout) blackoutCount++;
            if (blackoutCount < lastBlackoutCount)
            {
                if (blackoutCount == 0) recentBenignCancellations.Clear();
                // Cleaning restores a viable route; don't retain a long delay sampled
                // while it was blocked. This reschedules only the coverage stream.
                nextCoverageAt = Mathf.Min(nextCoverageAt, simulationTime + 1f);
            }
            lastBlackoutCount = blackoutCount;
            bool hostileActive = allowAmbient && autoGenerationEnabled && hostileGenerationEnabled;
            if (!hostileActive) hostileClockPaused = true;
            else if (hostileClockPaused)
            {
                // Re-enabling hostile traffic starts a fresh wait; missed opportunities
                // while the clock was disabled never arrive as a catch-up burst.
                nextHostileAt = simulationTime + HostileDelay();
                hostileClockPaused = false;
            }
            if (allowAmbient && autoGenerationEnabled && !idsExhausted)
            {
                if (hostileGenerationEnabled)
                {
                    while (nextHostileAt <= simulationTime + 0.00001f)
                    {
                        float dueAt = nextHostileAt;
                        ConnectionSpawnPlan plan = GenerateHostilePlan(dueAt);
                        if (plan != null) QueueSpawnPlan(plan, "ambient_hostile");
                        float interval = HostileDelay();
                        nextHostileAt = dueAt + interval;
                        host.Recorder?.RecordHostileOpportunity(simulationTime, dueAt,
                            hostileMeanIntervalSeconds, interval, nextHostileAt,
                            plan?.ConnectionId, plan != null ? null :
                                idsExhausted ? "ids_exhausted" : "no_weighted_purpose");
                        if (idsExhausted) break;
                    }
                }
                // One stochastic opportunity per step prevents catch-up bursts after long steps.
                if (!idsExhausted && nextCoverageAt <= simulationTime + 0.00001f)
                {
                    int established, anticipated;
                    CountBenignPopulation(simulationTime, out established, out anticipated);
                    int cancellations = RecentBenignCancellations(simulationTime);
                    float goal = EffectiveCoverageGoal(simulationTime);
                    int selectionPressure = established + anticipated +
                        (blackoutCount > 0 ? CanceledAttemptWeight * cancellations : 0);
                    float chance = Mathf.Lerp(coverageAtGoalChance, 1f,
                        Mathf.Clamp01((goal - selectionPressure) / 1.5f));
                    float roll = (float)coverageRandom.NextDouble();
                    bool selected = roll < chance;
                    ConnectionSpawnPlan plan = selected ? GenerateBenignPlan(simulationTime) : null;
                    if (plan != null) QueueSpawnPlan(plan, "ambient_benign");
                    int afterEstablished, afterAnticipated;
                    CountBenignPopulation(simulationTime, out afterEstablished, out afterAnticipated);
                    // Failed attempts remain recent evidence that another immediate
                    // attempt is unlikely to establish while an outage persists.
                    int nextPressure = afterEstablished + afterAnticipated +
                        CanceledAttemptWeight * cancellations;
                    float mean = CoverageMean(nextPressure, goal);
                    float interval = CoverageDelay(nextPressure, goal);
                    if (cancellations > 0 && blackoutCount > 0)
                        interval = Mathf.Min(interval, coverageOutageRetryMaxSeconds);
                    nextCoverageAt = simulationTime + interval;
                    host.Recorder?.RecordCoverageOpportunity(simulationTime, established,
                        anticipated, cancellations, goal, chance, roll, nextPressure,
                        mean, interval, nextCoverageAt, plan?.ConnectionId,
                        plan != null ? null : !selected ? "probability_skip" :
                            idsExhausted ? "ids_exhausted" : "no_weighted_purpose");
                }
            }
            queue.Sort((a, b) =>
            {
                int time = a.Plan.ScheduledSpawnTime.CompareTo(b.Plan.ScheduledSpawnTime);
                return time != 0 ? time : a.Order.CompareTo(b.Order);
            });
            while (queue.Count > 0 && queue[0].Plan.ScheduledSpawnTime <= simulationTime + 0.00001f)
            {
                ConnectionSpawnPlan plan = queue[0].Plan;
                queue.RemoveAt(0);
                host.RegisterConnection(LogicalConnectionDefinition.FromPlan(plan));
            }
        }

        // Development command producer. It uses the same profile-to-plan path as ambient
        // generation; queue dispatch remains independent of the ambient toggle.
        public ConnectionSpawnPlan QueueProfile(string profileId,
            ConnectionSpawnOverrides overrides = null)
        {
            if (random == null) ResetDirector();
            ConnectionTrafficProfile selected = null;
            foreach (ConnectionTrafficProfile profile in profiles)
                if (profile != null && string.Equals(profile.id, profileId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    selected = profile;
                    break;
                }
            if (selected == null)
                throw new InvalidOperationException("Unknown traffic profile: " + profileId);
            if (overrides != null)
            {
                if (!selected.purposeTemplate)
                    throw new InvalidOperationException("Objective overrides require a purpose ID: " + profileId);
                if (!Enum.IsDefined(typeof(ManualObjective), overrides.Objective))
                    throw new InvalidOperationException("Unknown objective override.");
                if (overrides.Objective != ManualObjective.Delivery &&
                    (overrides.InfectionKind.HasValue || overrides.InfectionLoad.HasValue ||
                     overrides.InfectionCadence.HasValue))
                    throw new InvalidOperationException("Infection options require obj:delivery.");
                if (overrides.InfectionKind.HasValue &&
                    !Enum.IsDefined(typeof(InfectionKind), overrides.InfectionKind.Value))
                    throw new InvalidOperationException("Unknown infection type.");
                if (overrides.InfectionLoad.HasValue && overrides.InfectionKind != InfectionKind.PhantomLoad)
                    throw new InvalidOperationException("infp:load requires inf:phantom-load.");
                if (overrides.InfectionCadence.HasValue && overrides.InfectionKind != InfectionKind.Spawner)
                    throw new InvalidOperationException("infp:cadence requires inf:spawner.");
                if (overrides.InfectionLoad.HasValue &&
                    (overrides.InfectionLoad.Value <= 0f ||
                     float.IsNaN(overrides.InfectionLoad.Value) ||
                     float.IsInfinity(overrides.InfectionLoad.Value)))
                    throw new InvalidOperationException("infp:load requires a finite positive number.");
                if (overrides.InfectionCadence.HasValue &&
                    (overrides.InfectionCadence.Value <= 0f ||
                     float.IsNaN(overrides.InfectionCadence.Value) ||
                     float.IsInfinity(overrides.InfectionCadence.Value)))
                    throw new InvalidOperationException("infp:cadence requires a finite positive number.");
            }
            ConnectionSpawnPlan plan = BuildPlan(selected, host.SimulationTimeSeconds, false,
                overrides);
            if (plan == null)
                throw new InvalidOperationException("All 260 generated connection IDs are reserved for this run.");
            QueueSpawnPlan(plan, "manual");
            return plan;
        }

        // Opt-in mechanical witness. Normal ambient selection excludes profiles with weight zero.
        public ConnectionSpawnPlan QueueHighDemandWitness(string targetId = "DB", bool obvious = true)
        {
            if (random == null) ResetDirector();
            NodeView origin = FindNode(ambientOriginId);
            NodeView target = FindNode(targetId);
            NodeView[] route = FindRoute(origin, target);
            string id = NewId();
            if (id == null)
                throw new InvalidOperationException("All 260 generated connection IDs are reserved for this run.");
            float load = obvious ? Range(10f, 12f) : Range(7f, 9f);
            var plan = new ConnectionSpawnPlan(host.SimulationTimeSeconds, id, route,
                "high-demand-witness", "High Demand Test",
                new[] { new OperationStage(OperationKind.Read, load * 50f, load, "High demand test") });
            QueueSpawnPlan(plan, "high-demand-witness");
            return plan;
        }

        public ConnectionSpawnPlan GenerateHostilePlan(float atTime)
        {
            if (random == null) ResetDirector();
            string objective = random.NextDouble() < deliveryChance ? "DELIVERY" : "LOADER";
            return GenerateWeightedPurposePlan(atTime, false, objective);
        }

        // Deterministic witness seam; the production clock samples these dimensions itself.
        public ConnectionSpawnPlan GenerateLoaderPlanForWitness(float atTime,
            LoaderStructure structure, LoaderPattern pattern)
        {
            if (random == null) ResetDirector();
            if (!Enum.IsDefined(typeof(LoaderStructure), structure) ||
                !Enum.IsDefined(typeof(LoaderPattern), pattern))
                throw new ArgumentOutOfRangeException(nameof(pattern));
            return GenerateWeightedPurposePlan(atTime, false, "LOADER", structure, pattern);
        }

        private ConnectionSpawnPlan GenerateWeightedPurposePlan(float atTime, bool benign,
            string objective = null, LoaderStructure? structure = null, LoaderPattern? pattern = null)
        {
            float total = 0f;
            foreach (ConnectionTrafficProfile profile in profiles)
                if (profile != null && profile.purposeTemplate && Compatible(profile, objective))
                    total += Mathf.Max(0f, EffectiveAmbientWeight(profile.id));
            if (total <= 0f) return null;
            float choice = (float)random.NextDouble() * total;
            ConnectionTrafficProfile selected = null;
            foreach (ConnectionTrafficProfile profile in profiles)
            {
                if (profile == null || !profile.purposeTemplate || !Compatible(profile, objective) ||
                    EffectiveAmbientWeight(profile.id) <= 0f) continue;
                selected = profile;
                choice -= EffectiveAmbientWeight(profile.id);
                if (choice < 0f) break;
            }
            return BuildPlan(selected, atTime, true, forceBenign: benign,
                selectedObjective: objective, loaderStructure: structure, loaderPattern: pattern);
        }

        private static bool Compatible(ConnectionTrafficProfile purpose, string objective) =>
            purpose.purposeTemplate && (objective == null || objective == "LOADER" ||
                objective == "DELIVERY"); // The six current PURPOSES support both genera.

        private void CountBenignPopulation(float now, out int established, out int anticipated)
        {
            established = 0;
            anticipated = 0;
            if (host.RuntimeConnections != null)
                foreach (LogicalConnectionState state in host.RuntimeConnections)
                {
                    if (state.IsHostile) continue;
                    if (state.IsActive) established++;
                    else if (state.IsConnecting || state.IsPending) anticipated++;
                }
            foreach (Queued queued in queue)
                if (!queued.Plan.IsHostile &&
                    queued.Plan.ScheduledSpawnTime <= now + ImminentPlanSeconds) anticipated++;
        }

        private int RecentBenignCancellations(float now)
        {
            recentBenignCancellations.RemoveAll(at =>
                now - at >= coverageCancellationMemorySeconds);
            return recentBenignCancellations.Count;
        }

        private float EffectiveCoverageGoal(float now)
        {
            return Mathf.Lerp(Mathf.Max(0f, coverageStartupGoal),
                Mathf.Max(0f, benignPopulationGoal),
                Mathf.Clamp01(now / Mathf.Max(0.01f, coverageRampSeconds)));
        }

        private float CoverageMean(int coveragePressure, float goal)
        {
            return Mathf.Max(0.01f, coverageMeanIntervalSeconds) *
                Mathf.Exp(Mathf.Clamp(coverageResponse *
                    (coveragePressure - goal), -1.4f, 1.8f));
        }

        private float CoverageDelay(int coveragePressure, float goal)
        {
            // Exponential waiting times allow both lulls and close arrivals. The occupancy
            // response changes their mean, without a hard floor or population cap.
            return Mathf.Max(0.1f, (float)(-Math.Log(1d - coverageRandom.NextDouble()) *
                CoverageMean(coveragePressure, goal)));
        }

        private float HostileDelay()
        {
            return Mathf.Max(0.1f, (float)(-Math.Log(1d -
                hostileScheduleRandom.NextDouble()) *
                Mathf.Max(0.01f, hostileMeanIntervalSeconds)));
        }

        public ConnectionSpawnPlan GenerateBenignPlan(float atTime)
        {
            if (random == null) ResetDirector();
            System.Random ambientRandom = random;
            random = benignPlanRandom;
            try
            {
                return GenerateWeightedPurposePlan(atTime, true);
            }
            finally { random = ambientRandom; }
        }

        // Uses the purpose recipe with a separate random stream. The child is queued now and
        // dispatched by Tick on the following simulation step.
        public ConnectionSpawnPlan QueueSpawnerChild(NodeInfection parent, float atTime,
            out string reason)
        {
            if (random == null) ResetDirector();
            NodeView origin = parent.Target.View;
            var choices = new List<(ConnectionTrafficProfile profile, NodeView target)>();
            foreach (ConnectionTrafficProfile profile in profiles)
            {
                if (profile == null || !profile.purposeTemplate || profile.eligibleTargetIds == null)
                    continue;
                foreach (string targetId in profile.eligibleTargetIds)
                {
                    try
                    {
                        NodeView target = FindNode(targetId);
                        if (target == origin || string.Equals(target.NodeId,
                            ambientOriginId, StringComparison.OrdinalIgnoreCase)) continue;
                        FindRoute(origin, target);
                        choices.Add((profile, target));
                    }
                    catch (InvalidOperationException) { }
                }
            }
            if (choices.Count == 0)
            { reason = "no_physical_internal_route"; return null; }
            if (nextIdIndex >= shuffledIds.Length)
            { reason = "ids_exhausted"; return null; }
            System.Random ambientRandom = random;
            random = spawnerRandom;
            try
            {
                var chosen = choices[random.Next(choices.Count)];
                InfectionKind kind = ChooseInfection();
                var overrides = new ConnectionSpawnOverrides(ManualObjective.Delivery, kind);
                ConnectionSpawnPlan basePlan = BuildPlan(chosen.profile, atTime, false,
                    overrides, origin, chosen.target);
                if (basePlan == null)
                { reason = "ids_exhausted"; return null; }
                var child = new ConnectionSpawnPlan(basePlan.ScheduledSpawnTime,
                    basePlan.ConnectionId, basePlan.Route, basePlan.ProfileId,
                    basePlan.ClaimedPurpose, basePlan.Operations, basePlan.IsHostile,
                    basePlan.ConnectionBehaviors, basePlan.GenerationTrace,
                    origin.NodeId, parent.SourceConnectionId, parent.AppliedAtSeconds);
                QueueSpawnPlan(child, "spawner");
                reason = null;
                return child;
            }
            finally { random = ambientRandom; }
        }

        private ConnectionSpawnPlan BuildPlan(ConnectionTrafficProfile selected, float atTime,
            bool ambient = false, ConnectionSpawnOverrides overrides = null,
            NodeView originOverride = null, NodeView targetOverride = null,
            bool forceBenign = false, string selectedObjective = null,
            LoaderStructure? loaderStructure = null, LoaderPattern? loaderPattern = null)
        {
            string id = NewId();
            if (id == null) return null;
            NodeView origin = originOverride != null ? originOverride : FindNode(ambientOriginId);
            NodeView target = targetOverride != null ? targetOverride :
                FindNode(selected.eligibleTargetIds[random.Next(selected.eligibleTargetIds.Length)]);
            NodeView[] route = FindRoute(origin, target);
            var stages = new OperationStage[selected.operations.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                ConnectionOperationProfile spec = selected.operations[i];
                float load = Range(spec.minimumLoad, spec.maximumLoad);
                float work = selected.purposeTemplate
                    ? spec.meanWork * Range(1f - normalWorkSpread, 1f + normalWorkSpread)
                    : load * Range(spec.minimumDuration, spec.maximumDuration);
                stages[i] = new OperationStage(spec.operation, work, load,
                    spec.activityDescription, spec.completionEffect, spec.behaviors,
                    spec.completionEffect == CompletionEffect.InfectionDelivery ?
                        new InfectionPayload(InfectionKind.PhantomLoad,
                            host.DefaultPhantomLoad) : null);
            }
            if (selected.purposeTemplate)
                return BuildPurposePlan(selected, atTime, id, route, stages, ambient,
                    overrides, forceBenign, selectedObjective, loaderStructure, loaderPattern);
            return new ConnectionSpawnPlan(atTime, id, route, selected.id,
                selected.claimedPurpose, stages, selected.isHostile,
                selected.connectionBehaviors);
        }

        private ConnectionSpawnPlan BuildPurposePlan(ConnectionTrafficProfile purpose, float atTime,
            string id, NodeView[] route, OperationStage[] normal, bool ambient,
            ConnectionSpawnOverrides overrides, bool forceBenign, string selectedObjective,
            LoaderStructure? loaderStructure, LoaderPattern? loaderPattern)
        {
            bool hostile = overrides != null || ambient && !forceBenign;
            string objective = overrides != null ? overrides.Objective.ToString().ToUpperInvariant() :
                hostile ? selectedObjective ?? "LOADER" : "NONE";
            var stages = new List<OperationStage>(normal);
            var anomalies = new List<string>();
            string generationDecision = ambient ? "BENIGN_NORMAL" : "MANUAL_BENIGN";
            var selectedKeywords = new List<GeneratedKeywordSelection>();
            LoaderAttackPlan loaderAttack = null;
            if (objective == "LOADER")
            {
                generationDecision = "LOADER";
                loaderAttack = ComposeLoader(stages, route[route.Length - 1],
                    loaderStructure, loaderPattern);
                anomalies.Add("loader-capacity-demand");
                if (loaderAttack.pattern != LoaderPattern.Immediate)
                    selectedKeywords.Add(new GeneratedKeywordSelection
                    {
                        operationIndex = loaderAttack.attackOperationIndex,
                        behaviorId = loaderAttack.pattern == LoaderPattern.Overload ?
                            "LOADER_OVERLOAD" : "LOADER_SPIKING",
                        behaviorKind = loaderAttack.pattern.ToString()
                    });
            }
            else if (objective == "DELIVERY")
            {
                // Preserve the same one-roll branch: true means no anomaly was intentionally selected.
                bool subtle = random.NextDouble() < subtleDeliveryChance;
                generationDecision = subtle ? "DELIVERY_SUBTLE" : "DELIVERY_ANOMALOUS";
                if (!subtle)
                {
                    string anomaly = RandomAnomaly();
                    ApplyAnomaly(stages, anomaly, false);
                    anomalies.Add(anomaly);
                    if (random.NextDouble() < secondAnomalyChance)
                    {
                        anomaly = RandomAnomaly();
                        ApplyAnomaly(stages, anomaly, false);
                        anomalies.Add(anomaly);
                    }
                }
                int last = stages.Count - 1;
                OperationStage stage = stages[last];
                InfectionKind kind = overrides != null ?
                    overrides.InfectionKind ?? InfectionKind.PhantomLoad : ChooseInfection();
                stages[last] = Changed(stage, effect: CompletionEffect.InfectionDelivery,
                    payload: Payload(kind, overrides));
            }
            else if (ambient && random.NextDouble() < benignOutlierChance)
            {
                generationDecision = "BENIGN_OUTLIER";
                if (random.NextDouble() < 0.25) selectedKeywords.Add(AddKeyword(stages));
                else
                {
                    string anomaly = RandomAnomaly();
                    ApplyAnomaly(stages, anomaly, false);
                    anomalies.Add(anomaly);
                }
            }
            var trace = new ConnectionGenerationTrace
            {
                purposeId = purpose.id, objective = objective,
                generationDecision = generationDecision,
                selectedInfectionKind = stages[stages.Count - 1].InfectionPayload?.Kind.ToString(),
                selectedInfectionLoad = stages[stages.Count - 1].InfectionPayload?.RequestedLoad ?? 0f,
                selectedInfectionCadence = stages[stages.Count - 1].InfectionPayload?.CadenceSeconds ?? 0f,
                selectedKeywords = selectedKeywords.ToArray(),
                anomalies = anomalies.ToArray(),
                normalOperations = ConnectionGenerationTrace.Capture(normal),
                finalOperations = ConnectionGenerationTrace.Capture(stages.ToArray()),
                loaderAttack = loaderAttack?.Copy(),
                forcedOptions = overrides == null ? null : new ForcedGenerationOptions
                {
                    objective = objective,
                    infectionType = overrides.InfectionKind?.ToString(),
                    hasLoadParameter = overrides.InfectionLoad.HasValue,
                    loadParameter = overrides.InfectionLoad ?? 0f,
                    hasCadenceParameter = overrides.InfectionCadence.HasValue,
                    cadenceParameter = overrides.InfectionCadence ?? 0f
                }
            };
            return new ConnectionSpawnPlan(atTime, id, route, purpose.id, purpose.claimedPurpose,
                stages.ToArray(), hostile, purpose.connectionBehaviors, trace,
                loaderAttack: loaderAttack);
        }

        private LoaderAttackPlan ComposeLoader(List<OperationStage> stages, NodeView target,
            LoaderStructure? forcedStructure, LoaderPattern? forcedPattern)
        {
            LoaderStructure structure = forcedStructure ??
                (random.NextDouble() < loaderDelayedChance ? LoaderStructure.Delayed : LoaderStructure.Single);
            LoaderPattern pattern = forcedPattern ?? (LoaderPattern)random.Next(3);
            int sourceIndex = stages.Count - 1;
            OperationStage source = stages[sourceIndex];
            if (structure == LoaderStructure.Single)
            {
                // The sampled PURPOSE supplies the operation kind and plausible activity.
                sourceIndex = random.Next(stages.Count);
                source = stages[sourceIndex];
                stages.Clear();
                stages.Add(source);
            }
            else stages.Add(source);
            int attackIndex = stages.Count - 1;
            float capacity = 0f;
            foreach (NodeResources node in host.Nodes)
                if (node.View == target) { capacity = node.LoadCapacity; break; }
            if (capacity <= 0f) throw new InvalidOperationException("LOADER target needs positive LOAD capacity.");
            float attackLoad = Mathf.Max(capacity * Range(loaderMinAttackCapacityFraction,
                loaderMaxAttackCapacityFraction), source.DesiredLoad +
                Mathf.Max(0.5f, capacity * 0.2f));
            float onset = pattern == LoaderPattern.Immediate ? 0f :
                Range(loaderMinOnsetSeconds, loaderMaxOnsetSeconds);
            float duration = Range(loaderMinDurationSeconds, loaderMaxDurationSeconds);
            float work = Mathf.Max(source.TotalWork,
                source.DesiredLoad * (onset + 1f) + 0.01f);
            var behaviors = new List<TimedModifierBehaviorDefinition>();
            if (pattern == LoaderPattern.Overload)
                behaviors.Add(TimedModifierBehaviorDefinition.Overload("LOADER_OVERLOAD",
                    onset, attackLoad - source.DesiredLoad));
            if (pattern == LoaderPattern.Spiking)
                behaviors.Add(TimedModifierBehaviorDefinition.Spiking("LOADER_SPIKING",
                    loaderSpikeQuietSeconds, loaderSpikeElevatedSeconds,
                    attackLoad - source.DesiredLoad, onset));
            behaviors.Add(TimedModifierBehaviorDefinition.Rate("LOADER_ACTIVE", onset, 0f, 0f));
            stages[attackIndex] = new OperationStage(source.Operation, work,
                pattern == LoaderPattern.Immediate ? attackLoad : source.DesiredLoad,
                source.ActivityDescription, CompletionEffect.None, behaviors.ToArray());
            return new LoaderAttackPlan
            {
                structure = structure, pattern = pattern,
                attackOperationIndex = attackIndex, targetCapacity = capacity,
                normalLoad = source.DesiredLoad, attackLoad = attackLoad,
                onsetSeconds = onset, spikeQuietSeconds = pattern == LoaderPattern.Spiking ?
                    loaderSpikeQuietSeconds : 0f,
                spikeElevatedSeconds = pattern == LoaderPattern.Spiking ?
                    loaderSpikeElevatedSeconds : 0f,
                durationSeconds = duration
            };
        }

        private InfectionPayload Payload(InfectionKind kind, ConnectionSpawnOverrides overrides) =>
            new InfectionPayload(kind,
                kind == InfectionKind.PhantomLoad ? overrides?.InfectionLoad ?? host.DefaultPhantomLoad : 0f,
                kind == InfectionKind.Spawner ? overrides?.InfectionCadence ?? host.DefaultSpawnerCadence : 0f);

        private InfectionKind ChooseInfection()
        {
            float phantom = Mathf.Max(0f, phantomLoadWeight);
            float blackout = Mathf.Max(0f, blackoutWeight);
            float spawner = Mathf.Max(0f, spawnerWeight);
            float total = phantom + blackout + spawner;
            if (total <= 0f) throw new InvalidOperationException("Delivery infection weights must include a positive kind.");
            float draw = (float)random.NextDouble() * total;
            return draw < phantom ? InfectionKind.PhantomLoad :
                draw < phantom + blackout ? InfectionKind.Blackout : InfectionKind.Spawner;
        }

        private string RandomAnomaly()
        {
            switch (random.Next(4))
            {
                case 0: return "operation-deviation";
                case 1: return "high-load";
                case 2: return "low-load";
                default: return "work-deviation";
            }
        }

        private void ApplyAnomaly(List<OperationStage> stages, string anomaly, bool sustainedWork)
        {
            int index = random.Next(stages.Count);
            OperationStage stage = stages[index];
            switch (anomaly)
            {
                case "operation-deviation":
                    OperationKind other = (OperationKind)(((int)stage.Operation + 1 + random.Next(2)) % 3);
                    if (random.NextDouble() < 0.5)
                        stages.Add(Changed(stage, kind: other, description: Activity(other)));
                    else stages[index] = Changed(stage, kind: other, description: Activity(other));
                    break;
                case "high-load":
                    stages[index] = Changed(stage, load: stage.DesiredLoad * Range(2.5f, 4f));
                    break;
                case "low-load":
                    stages[index] = Changed(stage, load: stage.DesiredLoad * Range(0.2f, 0.5f));
                    break;
                case "work-deviation":
                    stages[index] = Changed(stage, work: stage.TotalWork * (sustainedWork ||
                        random.NextDouble() < 0.5 ? Range(2f, 4f) : Range(0.25f, 0.5f)));
                    break;
            }
        }

        private GeneratedKeywordSelection AddKeyword(List<OperationStage> stages)
        {
            int index = random.Next(stages.Count);
            OperationStage stage = stages[index];
            var behaviors = new List<TimedModifierBehaviorDefinition>(stage.Behaviors);
            var keyword = random.NextDouble() < 0.5
                ? TimedModifierBehaviorDefinition.Overload("OVERLOAD", 5f, 5f)
                : TimedModifierBehaviorDefinition.Spiking("SPIKING", 5f, 2f, 5f);
            behaviors.Add(keyword);
            stages[index] = Changed(stage, behaviors: behaviors.ToArray());
            return new GeneratedKeywordSelection
            {
                operationIndex = index, behaviorId = keyword.Id,
                behaviorKind = keyword.Kind.ToString()
            };
        }

        private static OperationStage Changed(OperationStage stage, OperationKind? kind = null,
            float? load = null, float? work = null, string description = null,
            CompletionEffect? effect = null, TimedModifierBehaviorDefinition[] behaviors = null,
            InfectionPayload payload = null) =>
            new OperationStage(kind ?? stage.Operation, work ?? stage.TotalWork,
                load ?? stage.DesiredLoad, description ?? stage.ActivityDescription,
                effect ?? stage.EffectOnCompletion, behaviors ?? stage.Behaviors,
                payload ?? stage.InfectionPayload);

        private static string Activity(OperationKind kind) => kind == OperationKind.Read
            ? "Reading records" : kind == OperationKind.Write ? "Writing records" : "Processing request";

        private string NewId()
        {
            while (nextIdIndex < shuffledIds.Length)
            {
                string id = shuffledIds[nextIdIndex++];
                if (!reservedIds.Contains(id) && !host.HasEverUsedId(id))
                    return id;
            }
            if (!idsExhausted)
                Debug.LogWarning("All 260 generated connection IDs are reserved; ambient spawning has stopped.", this);
            idsExhausted = true;
            return null;
        }
        private float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
        private NodeView FindNode(string id)
        {
            foreach (NodeResources node in host.Nodes)
                if (node.View != null && string.Equals(node.View.NodeId, id, StringComparison.OrdinalIgnoreCase))
                    return node.View;
            throw new InvalidOperationException("Unknown traffic node: " + id);
        }
        private NodeView[] FindRoute(NodeView origin, NodeView target)
        {
            if (origin == null || target == null || origin == target)
                throw new InvalidOperationException("Traffic requires distinct configured origin and target nodes.");
            var paths = new Queue<List<NodeView>>();
            paths.Enqueue(new List<NodeView> { origin });
            var visited = new HashSet<NodeView> { origin };
            while (paths.Count > 0)
            {
                List<NodeView> path = paths.Dequeue();
                NodeView tail = path[path.Count - 1];
                foreach (PhysicalLinkReference link in host.PhysicalLinks)
                {
                    NodeView adjacent = link.View.NodeA == tail ? link.View.NodeB :
                        link.View.NodeB == tail ? link.View.NodeA : null;
                    if (adjacent == null || !visited.Add(adjacent)) continue;
                    var extended = new List<NodeView>(path) { adjacent };
                    if (adjacent == target) return extended.ToArray();
                    paths.Enqueue(extended);
                }
            }
            throw new InvalidOperationException("No route to traffic target " + target.NodeId);
        }

        private static ConnectionTrafficProfile[] DefaultProfiles() => new[]
        {
            Purpose("product-lookup", "Product Detail Retrieval", "DB", 1.5f,
                PurposeStage(OperationKind.Read, "Retrieving product details", 0.5f, 1.1f, 48f)),
            Purpose("customer-login", "Customer Authentication", "AUTH", 1f,
                PurposeStage(OperationKind.Execute, "Authenticating customer", 0.8f, 1.6f, 48f)),
            Purpose("cart-pricing", "Cart Price Calculation", "API", 1f,
                PurposeStage(OperationKind.Execute, "Calculating cart price", 1f, 2f, 70f)),
            Purpose("order-submit", "Checkout Order Submission", "API", 0.9f,
                PurposeStage(OperationKind.Write, "Submitting order", 1.5f, 3f, 120f),
                PurposeStage(OperationKind.Read, "Reading order confirmation", 0.6f, 1.3f, 36f)),
            Purpose("inventory-sync", "Inventory Reconciliation", "DB", 0.6f,
                PurposeStage(OperationKind.Read, "Reading inventory", 1f, 1.8f, 80f),
                PurposeStage(OperationKind.Write, "Updating inventory", 1.7f, 3.2f, 130f)),
            Purpose("sales-export", "Sales Report Export", "DB", 0.25f,
                PurposeStage(OperationKind.Read, "Exporting sales report", 2.5f, 4.5f, 290f)),
            Profile("retrieval", "Record Retrieval", "DB", 0f,
                Stage(OperationKind.Read, "Reading records", 1f, 2.5f, 40f, 70f)),
            Profile("indexing", "Record Indexing", "DB", 0f,
                Stage(OperationKind.Write, "Updating the DB index", 1.5f, 3f, 20f, 35f),
                Stage(OperationKind.Read, "Reading the updated index", 1f, 2.5f, 20f, 35f)),
            Profile("auth", "Customer Authentications", "AUTH", 0f,
                Stage(OperationKind.Execute, "Processing authentications", 1f, 2.5f, 40f, 70f)),
            Profile("updates", "Application Updates", "API", 0f,
                Stage(OperationKind.Write, "Applying updates", 1.5f, 3f, 20f, 35f),
                Stage(OperationKind.Read, "Verifying updates", 1f, 2.5f, 20f, 35f)),
            Profile("quick-lookup", "Quick Record Lookup", "DB", 0f,
                Stage(OperationKind.Read, "Looking up a record", 0.5f, 1.5f, 4f, 8f)),
            new ConnectionTrafficProfile
            {
                id = "basic-threat", claimedPurpose = "Database Maintenance",
                eligibleTargetIds = new[] { "DB" }, ambientWeight = 0f, isHostile = true,
                operations = new[] { ThreatWriteStage() }
            },
            new ConnectionTrafficProfile
            {
                id = "delayed-threat", claimedPurpose = "Database Maintenance",
                eligibleTargetIds = new[] { "DB" }, ambientWeight = 0f, isHostile = true,
                operations = new[]
                {
                    Stage(OperationKind.Read, "Reading records", 2f, 2f, 8f, 8f),
                    ThreatWriteStage()
                }
            },
            new ConnectionTrafficProfile
            {
                id = "spiking-threat", claimedPurpose = "Database Maintenance",
                eligibleTargetIds = new[] { "DB" }, ambientWeight = 0f, isHostile = true,
                operations = new[] { SpikingWriteStage(CompletionEffect.InfectionDelivery) }
            },
            new ConnectionTrafficProfile
            {
                id = "spiking-benign", claimedPurpose = "Database Maintenance",
                eligibleTargetIds = new[] { "DB" }, ambientWeight = 0f,
                operations = new[] { SpikingWriteStage(CompletionEffect.None) }
            },
            new ConnectionTrafficProfile
            {
                id = "overload-demo", claimedPurpose = "Database Maintenance",
                eligibleTargetIds = new[] { "DB" }, ambientWeight = 0f,
                operations = new[] { new ConnectionOperationProfile
                {
                    operation = OperationKind.Write, activityDescription = "Writing records",
                    minimumLoad = 2f, maximumLoad = 2f,
                    minimumDuration = 120f, maximumDuration = 120f,
                    behaviors = new[] { TimedModifierBehaviorDefinition.Overload(
                        "OVERLOAD", 5f, 10f) }
                } }
            }
        };
        private static ConnectionOperationProfile SpikingWriteStage(CompletionEffect effect) =>
            new ConnectionOperationProfile
            {
                operation = OperationKind.Write, activityDescription = "Writing records",
                minimumLoad = 2f, maximumLoad = 2f,
                minimumDuration = 120f, maximumDuration = 120f,
                completionEffect = effect,
                behaviors = new[] { TimedModifierBehaviorDefinition.Spiking(
                    "SPIKING", 5f, 2f, 10f) }
            };
        private static ConnectionOperationProfile ThreatWriteStage() => new ConnectionOperationProfile
        {
            operation = OperationKind.Write, activityDescription = "Writing records",
            minimumLoad = 12f, maximumLoad = 12f,
            minimumDuration = 20f, maximumDuration = 20f,
            completionEffect = CompletionEffect.InfectionDelivery
        };
        private static ConnectionTrafficProfile Profile(string id, string purpose, string target,
            float weight, params ConnectionOperationProfile[] stages) => new ConnectionTrafficProfile
            { id = id, claimedPurpose = purpose, eligibleTargetIds = new[] { target },
                ambientWeight = weight, operations = stages };
        private static ConnectionTrafficProfile Purpose(string id, string purpose, string target,
            float weight, params ConnectionOperationProfile[] stages) => new ConnectionTrafficProfile
            { id = id, claimedPurpose = purpose, eligibleTargetIds = new[] { target },
                ambientWeight = weight, purposeTemplate = true, operations = stages };
        private static ConnectionOperationProfile PurposeStage(OperationKind kind, string description,
            float lowLoad, float highLoad, float meanWork) => new ConnectionOperationProfile
            { operation = kind, activityDescription = description, minimumLoad = lowLoad,
                maximumLoad = highLoad, meanWork = meanWork };
        private static ConnectionOperationProfile Stage(OperationKind kind, string description,
            float lowLoad, float highLoad, float lowSeconds, float highSeconds) => new ConnectionOperationProfile
            { operation = kind, activityDescription = description, minimumLoad = lowLoad,
                maximumLoad = highLoad, minimumDuration = lowSeconds, maximumDuration = highSeconds };
    }
}
