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
        [SerializeField, Min(0f)] private float initialGraceSeconds = 3f;
        [SerializeField, Min(0.01f)] private float spawnIntervalSeconds = 13.5f;
        [SerializeField, Min(0f)] private float spawnJitterSeconds = 1.5f;
        [SerializeField] private string ambientOriginId = "EXT1";
        [SerializeField] private ConnectionTrafficProfile[] profiles;
        [Tooltip("Optional ambient weights by profile ID. Built-in profile operations stay intact; zero disables ambient selection but not spawn <profileId>.")]
        [SerializeField] private AmbientWeightOverride[] ambientWeightOverrides;
        [Header("Purpose generation (provisional playtest rates)")]
        [SerializeField, Range(0f, 1f)] private float hostileChance = 0.25f;
        [SerializeField, Range(0f, 1f)] private float deliveryChance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float benignOutlierChance = 0.08f;
        [SerializeField, Range(0f, 1f)] private float secondAnomalyChance = 0.15f;
        [SerializeField, Range(0f, 1f)] private float subtleDeliveryChance = 0.1f;
        [SerializeField, Range(0f, 0.5f)] private float normalWorkSpread = 0.25f;
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
        private const string IdLetters = "abcdefghijklmnopqrstuvwxyz";
        private const string IdDigits = "0123456789";
        private string[] shuffledIds;
        private int nextIdIndex;
        private bool idsExhausted;
        private System.Random random;
        private System.Random spawnerRandom;
        private float nextAmbientAt;
        private long nextOrder;
        private readonly Dictionary<string, float> effectiveWeights =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public bool AutoGenerationEnabled { get => autoGenerationEnabled; set => autoGenerationEnabled = value; }
        public int Seed { get => seed; set => seed = value; }
        public IReadOnlyList<ConnectionTrafficProfile> Profiles => profiles;
        public float InitialGraceSeconds => initialGraceSeconds;
        public float SpawnIntervalSeconds => spawnIntervalSeconds;
        public float SpawnJitterSeconds => spawnJitterSeconds;
        public string AmbientOriginId => ambientOriginId;
        public float HostileChance => hostileChance;
        public float DeliveryChance => deliveryChance;
        public float BenignOutlierChance => benignOutlierChance;
        public float SecondAnomalyChance => secondAnomalyChance;
        public float SubtleDeliveryChance => subtleDeliveryChance;
        public float NormalWorkSpread => normalWorkSpread;
        public float PhantomLoadWeight => phantomLoadWeight;
        public float BlackoutWeight => blackoutWeight;
        public float SpawnerWeight => spawnerWeight;
        public float EffectiveAmbientWeight(string profileId) =>
            profileId != null && effectiveWeights.TryGetValue(profileId, out float weight) ? weight : 0f;

        public void ResetDirector()
        {
            random = new System.Random(seed);
            spawnerRandom = new System.Random(unchecked(seed ^ (int)0x53A7B10));
            queue.Clear();
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
            nextAmbientAt = initialGraceSeconds;
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
            if (allowAmbient && autoGenerationEnabled && !idsExhausted)
            {
                while (nextAmbientAt <= simulationTime + 0.00001f)
                {
                    ConnectionSpawnPlan plan = GeneratePlan(nextAmbientAt);
                    if (plan != null) QueueSpawnPlan(plan, "ambient");
                    if (idsExhausted) break;
                    nextAmbientAt += Mathf.Max(0.01f, spawnIntervalSeconds +
                        Range(-spawnJitterSeconds, spawnJitterSeconds));
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

        public ConnectionSpawnPlan GeneratePlan(float atTime)
        {
            if (random == null) ResetDirector();
            float total = 0f;
            foreach (ConnectionTrafficProfile profile in profiles)
                if (profile != null) total += Mathf.Max(0f, EffectiveAmbientWeight(profile.id));
            if (total <= 0f) return null;
            float choice = (float)random.NextDouble() * total;
            ConnectionTrafficProfile selected = null;
            foreach (ConnectionTrafficProfile profile in profiles)
            {
                if (profile == null || EffectiveAmbientWeight(profile.id) <= 0f) continue;
                selected = profile;
                choice -= EffectiveAmbientWeight(profile.id);
                if (choice < 0f) break;
            }
            return BuildPlan(selected, atTime, true);
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
            NodeView originOverride = null, NodeView targetOverride = null)
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
                    overrides);
            return new ConnectionSpawnPlan(atTime, id, route, selected.id,
                selected.claimedPurpose, stages, selected.isHostile,
                selected.connectionBehaviors);
        }

        private ConnectionSpawnPlan BuildPurposePlan(ConnectionTrafficProfile purpose, float atTime,
            string id, NodeView[] route, OperationStage[] normal, bool ambient,
            ConnectionSpawnOverrides overrides)
        {
            bool hostile = overrides != null || ambient && random.NextDouble() < hostileChance;
            string objective = overrides != null ? overrides.Objective.ToString().ToUpperInvariant() :
                hostile ? (random.NextDouble() < deliveryChance ? "DELIVERY" : "LOADER") : "NONE";
            var stages = new List<OperationStage>(normal);
            var anomalies = new List<string>();
            string generationDecision = ambient ? "BENIGN_NORMAL" : "MANUAL_BENIGN";
            var selectedKeywords = new List<GeneratedKeywordSelection>();
            if (objective == "LOADER")
            {
                generationDecision = "LOADER";
                ApplyAnomaly(stages, "high-load", true);
                anomalies.Add("high-load");
                if (random.NextDouble() < secondAnomalyChance)
                {
                    ApplyAnomaly(stages, "work-deviation", true);
                    anomalies.Add("work-deviation");
                }
                // The keyword engine is shared with benign traffic; this does not label intent.
                if (random.NextDouble() < 0.25) selectedKeywords.Add(AddKeyword(stages));
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
                stages.ToArray(), hostile, purpose.connectionBehaviors, trace);
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
                PurposeStage(OperationKind.Read, "Retrieving product details", 0.5f, 1.1f, 24f)),
            Purpose("customer-login", "Customer Authentication", "AUTH", 1f,
                PurposeStage(OperationKind.Execute, "Authenticating customer", 0.8f, 1.6f, 24f)),
            Purpose("cart-pricing", "Cart Price Calculation", "API", 1f,
                PurposeStage(OperationKind.Execute, "Calculating cart price", 1f, 2f, 35f)),
            Purpose("order-submit", "Checkout Order Submission", "API", 0.9f,
                PurposeStage(OperationKind.Write, "Submitting order", 1.5f, 3f, 60f),
                PurposeStage(OperationKind.Read, "Reading order confirmation", 0.6f, 1.3f, 18f)),
            Purpose("inventory-sync", "Inventory Reconciliation", "DB", 0.6f,
                PurposeStage(OperationKind.Read, "Reading inventory", 1f, 1.8f, 40f),
                PurposeStage(OperationKind.Write, "Updating inventory", 1.7f, 3.2f, 65f)),
            Purpose("sales-export", "Sales Report Export", "DB", 0.25f,
                PurposeStage(OperationKind.Read, "Exporting sales report", 2.5f, 4.5f, 145f)),
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
