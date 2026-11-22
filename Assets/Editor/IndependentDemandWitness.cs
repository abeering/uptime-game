using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class IndependentDemandWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsIndependentDemandV0";
        public const string LoaderId = "demand-loader-fixture";
        private static readonly int[] Seeds = { 1701, 1702, 3141, 9001 };
        private static readonly List<string> checks = new();
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] public sealed class NodeMeasure
        {
            public string id;
            public double requestedLoadSeconds, availableLoadSeconds, capacitySeconds, excessLoadSeconds;
            public float peakRequested, contentionSeconds, postRemovalContentionSeconds;
        }
        [Serializable] public sealed class Sample
        {
            public float time;
            public int active, connecting, arrived, admitted, waiting, unadmitted;
            public double credited, outstandingWork;
            public float[] requested, available, capacity, benignRequested, benignAllocated;
        }
        [Serializable] public sealed class OperationMeasure
        {
            public string id, node, decision;
            public int index;
            public float completedAt, actualSeconds, authoredNominalSeconds, delaySeconds;
        }
        [Serializable] public sealed class Measurement
        {
            public string scene, policy, scenario, ending, demandSignature, planSignature, gameplaySignature;
            public string[] demandPrefixSignatures, planPrefixSignatures;
            public int seed, arrived, admitted, waiting, unadmitted, peakActive, peakVisible, completedOperations, commands, integrityStrikes, compromisedNodes;
            public float meanInterval, seconds, averageActive, contentionSeconds, benignShortfallLoadSeconds;
            public float loaderActivatedAt = -1, removedAt = -1, recoveryAt = -1, recoveryDelay = -1;
            public float postRemovalContentionSeconds, peakBenignShortfall, completedMeanDelay, coreBreachAt = -1;
            public float normalCapacityRecoveryDelay = -1;
            public bool recoveryCensored;
            public ShiftSnapshot work;
            public NodeMeasure[] nodes;
            public Sample[] samples;
            public OperationMeasure[] operations;
        }
        [Serializable] public sealed class Results { public Measurement[] runs; }
        private static void Check(bool condition, string text)
        { if (!condition) throw new InvalidOperationException("IndependentDemand FAIL: " + text); checks.Add("PASS " + text); }
        public static SimulationHost Open(string scene, BenignArrivalPolicy policy, float mean = 8, int seed = 1701,
            bool mixed = false, bool quota = false, float horizon = 360)
        {
            return IntegrityWitness.Open(true, seed, host =>
            {
                IntegrityWitness.Set(host, "shiftEnabled", true);
                if (!quota)
                {
                    IntegrityWitness.Set(host, "shiftDurationSeconds", horizon);
                    IntegrityWitness.Set(host, "shiftWorkTarget", 1000000f);
                }
                var traffic = host.GetComponent<ConnectionTrafficDirector>();
                traffic.HostileGenerationEnabled = mixed;
                IntegrityWitness.Set(traffic, "benignArrivalPolicy", policy);
                IntegrityWitness.Set(traffic, "independentBenignMeanSeconds", mean);
            }, scene);
        }
        public static LogicalConnectionState AddLoader(SimulationHost host, string id = LoaderId)
        {
            // A controlled ordinary LOADER: real proportional LOAD sharing and the
            // existing zero-WORK-rate/withdrawal lifecycle. No generator IDs/RNG consumed.
            var stages = new[] { new OperationStage(OperationKind.Read, 1000, 12, "Reading records",
                behaviors: new[] { TimedModifierBehaviorDefinition.Rate("LOADER_ACTIVE", 0, 0, 0) }) };
            var plan = new ConnectionSpawnPlan(host.SimulationTimeSeconds, id,
                (NodeView[])typeof(ConnectionTrafficDirector).GetMethod("FindRoute", Private).Invoke(host.GetComponent<ConnectionTrafficDirector>(),
                    new object[] { host.FindNodeResources("EXT1").View, host.FindNodeResources("DB").View }),
                "controlled-loader", "Product Detail Lookups", stages, true,
                loaderAttack: new LoaderAttackPlan { structure = LoaderStructure.Single, pattern = LoaderPattern.Immediate,
                    attackOperationIndex = 0, targetCapacity = 10, normalLoad = 1, attackLoad = 12, durationSeconds = 30 });
            return host.RegisterConnection(LogicalConnectionDefinition.FromPlan(plan));
        }
        public static string Hash(string value)
        { using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", ""); }
        [MenuItem("Tools/Connections/Run Independent Demand Witness")]
        public static void Run()
        {
            checks.Clear();
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene);
                var authored = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
                Check(authored.BenignPolicy == BenignArrivalPolicy.PopulationResponsive, scene + " canonical default remains population-responsive");
                var healthy = Observe(scene, BenignArrivalPolicy.IndependentDemand, 6, 1701, "healthy", horizon: 150);
                var disrupted = Observe(scene, BenignArrivalPolicy.IndependentDemand, 6, 1701, "kill10", horizon: 150);
                Check(healthy.demandSignature == disrupted.demandSignature && healthy.planSignature == disrupted.planSignature,
                    scene + " controlled LOADER does not reroll demand times, IDs, plans or natural-noise seeds");
                var replay = Observe(scene, BenignArrivalPolicy.IndependentDemand, 6, 1701, "kill10", horizon: 150);
                Check(disrupted.gameplaySignature == replay.gameplaySignature, scene + " exact seeded events/snapshots replay");
                Check(healthy.arrived == healthy.admitted && healthy.waiting == 0 && healthy.unadmitted == 0 && healthy.arrived > 0,
                    scene + " every independent request follows existing queue/registration, no occupancy admission limit");
                Check(disrupted.loaderActivatedAt >= 90 && Math.Abs(disrupted.removedAt - disrupted.loaderActivatedAt - 10) < .03,
                    scene + " real LOADER is killed after ten active seconds, not ten seconds from spawn");
                var host = Open(scene, BenignArrivalPolicy.IndependentDemand, 6);
                host.StepSimulation(30); var traffic = host.GetComponent<ConnectionTrafficDirector>();
                string prefix = DemandPlans(host);
                host.ResetSimulation(); host.Recorder.BeginRun(); host.StepSimulation(30);
                Check(prefix == DemandPlans(host), scene + " reset clears counters and replays schedule/recipe streams");
                Check(traffic.Demand.arrived == traffic.Demand.admitted && traffic.Demand.waiting == 0, "reset admission accounting");
                var saved = JsonUtility.FromJson<ConnectionsRunLog>(JsonUtility.ToJson(host.Recorder.CurrentLog));
                Check(saved.run.traffic.benignArrivalPolicy == "IndependentDemand" && saved.run.traffic.independentBenignMeanSeconds == 6 &&
                    saved.events.Count(e => e.type == "benign_demand_arrived") == traffic.Demand.arrived &&
                    saved.events.Count(e => e.type == "benign_demand_admitted") == traffic.Demand.admitted,
                    "saved recorder configuration and separate demand/admission events");
                var demands = host.Recorder.CurrentLog.events.Where(e => e.type == "benign_demand_arrived").ToArray();
                Check(demands.All(e => e.nextDemandAtSeconds > e.demandScheduledAtSeconds && e.demandMeanSeconds == 6), "seeded positive exponential intervals");
                traffic.AutoGenerationEnabled = false; int count = traffic.Demand.arrived; host.StepSimulation(20);
                Check(count == traffic.Demand.arrived, "disabled generation creates no demand");
                traffic.AutoGenerationEnabled = true; host.StepSimulation(.02f);
                Check(traffic.Demand.arrived == count, "resume has a fresh wait instead of an invented catch-up backlog");
                host = Open(scene, BenignArrivalPolicy.IndependentDemand, 6);
                host.SetRunMode(ConnectionRunMode.AuthoredWitness); host.StepSimulation(10);
                Check(host.GetComponent<ConnectionTrafficDirector>().Demand.arrived == 0, "authored mode suppresses ambient independent demand");
                host = Open(scene, BenignArrivalPolicy.IndependentDemand, 6);
                traffic = host.GetComponent<ConnectionTrafficDirector>();
                traffic.Tick(30, true); int coarse = traffic.Demand.arrived;
                host = Open(scene, BenignArrivalPolicy.IndependentDemand, 6); traffic = host.GetComponent<ConnectionTrafficDirector>();
                for (int i = 1; i <= 30; i++) traffic.Tick(i, true);
                Check(traffic.Demand.arrived == coarse, "coarse scheduler calls retain all scheduled arrivals");
                host = Open(scene, BenignArrivalPolicy.IndependentDemand, 6);
                traffic = host.GetComponent<ConnectionTrafficDirector>();
                host.StepSimulation(15); host.TryInjectInfection("DB", InfectionKind.Blackout, 0, out _); host.StepSimulation(15);
                string blackout = DemandPlans(host);
                host = Open(scene, BenignArrivalPolicy.IndependentDemand, 6); host.StepSimulation(30);
                Check(blackout == DemandPlans(host), "Blackout cancellation pressure does not suppress or reschedule independent demand");
                host = Open(scene, BenignArrivalPolicy.IndependentDemand, 6);
                traffic = host.GetComponent<ConnectionTrafficDirector>();
                for (int i = 0; i < 261; i++) traffic.GenerateBenignPlan(0);
                traffic.Tick(30, true);
                Check(traffic.ConnectionIdsExhausted && traffic.Demand.arrived > 0 && traffic.Demand.admitted == 0 &&
                    traffic.Demand.unadmitted == traffic.Demand.arrived && traffic.Demand.waiting == 0 &&
                    host.Recorder.CurrentLog.events.Where(e => e.type == "benign_demand_arrived").All(e => e.reason == "ids_exhausted"),
                    "existing finite ID pool is an explicit recorded failure, never silently suppressed demand or a fictional waiting queue");
                int frozen = traffic.Demand.arrived; host.StepSimulation(400); int atEnd = traffic.Demand.arrived; host.StepSimulation(10);
                Check(host.RunEnded && traffic.Demand.arrived == atEnd && atEnd >= frozen, "shift ending freezes demand creation/admission");
                host.Recorder.EndRun();
            }
            Directory.CreateDirectory(Evidence); File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks);
            Debug.Log("IndependentDemandWitness PASS: " + checks.Count + " assertions.");
        }
        private static string DemandPlans(SimulationHost host) => string.Join("\n", host.Recorder.CurrentLog.events
            .Where(e => e.type == "plan_queued" && (e.source == "independent_benign" || e.source == "ambient_benign"))
            .Select(e => JsonUtility.ToJson(e.definition)));
        private static string[] PrefixHashes(IEnumerable<string> rows)
        {
            var text = new StringBuilder(); var hashes = new List<string>();
            foreach (string row in rows) { text.Append(row).Append('\n'); hashes.Add(Hash(text.ToString())); }
            return hashes.ToArray();
        }
        private static bool SameDemandPrefix(Measurement healthy, Measurement other)
        {
            if (healthy.demandSignature == other.demandSignature && healthy.planSignature == other.planSignature) return true;
            // A real CORE breach freezes the simulation. Compare all requests in the
            // shorter run, rather than demanding arrivals after that run has ended.
            int common = Math.Min(healthy.arrived, other.arrived);
            return common > 0 && (healthy.coreBreachAt >= 0 || other.coreBreachAt >= 0) &&
                healthy.demandPrefixSignatures[common - 1] == other.demandPrefixSignatures[common - 1] &&
                healthy.planPrefixSignatures[common - 1] == other.planPrefixSignatures[common - 1];
        }
        public static Measurement Observe(string scene, BenignArrivalPolicy policy, float mean, int seed, string scenario,
            bool mixed = false, bool quota = false, float horizon = 360)
        {
            var host = Open(scene, policy, mean, seed, mixed, quota, horizon);
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            var duels = host.GetComponent<ConnectionsDuelDirector>();
            var overlay = UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>(); IntegrityWitness.Set(terminal, "maxEntries", 10000);
            var nodes = host.Nodes.Where(n => traffic.IsInternalNode(n.View)).ToArray();
            var result = new Measurement { scene = Path.GetFileNameWithoutExtension(scene), seed = seed,
                policy = policy.ToString(), meanInterval = mean, scenario = scenario,
                nodes = nodes.Select(n => new NodeMeasure { id = n.View.NodeId }).ToArray() };
            var samples = new List<Sample>(); var operations = new List<OperationMeasure>();
            host.OperationCompleted += (state, index, work) =>
            {
                if (state.IsHostile) return;
                float actual = host.SimulationTimeSeconds - state.OperationStartedAtSeconds;
                float nominal = state.Definition.WorkAt(index) / Mathf.Max(.0001f, state.Definition.LoadAt(index));
                operations.Add(new OperationMeasure { id = state.ConnectionId, node = state.TargetNode.NodeId, index = index,
                    decision = host.Recorder.CurrentLog.events.LastOrDefault(e => e.type == "plan_queued" && e.connectionId == state.ConnectionId)?.generation?.generationDecision, completedAt = host.SimulationTimeSeconds,
                    actualSeconds = actual, authoredNominalSeconds = nominal, delaySeconds = actual - nominal });
            };
            int admittedBenign = 0;
            host.ConnectionRegistered += state => { if (!state.IsHostile) admittedBenign++; };
            LogicalConnectionState loader = null;
            double activeIntegral = 0; float quiet = 0, normalQuiet = 0, nextDecision = 6, splash = 0, blockedUntil = 0;
            int encounter = -1, lastSecond = -1;
            for (int tick = 0; tick < Mathf.CeilToInt(horizon / .02f) + 2 && !host.RunEnded; tick++)
            {
                float before = host.SimulationTimeSeconds;
                if (!mixed && scenario != "healthy" && loader == null && before >= 90) loader = AddLoader(host);
                if (loader?.LoaderActivatedAtSeconds >= 0)
                {
                    result.loaderActivatedAt = loader.LoaderActivatedAtSeconds;
                    if (scenario.StartsWith("kill", StringComparison.Ordinal) && loader.IsActive &&
                        before >= loader.LoaderActivatedAtSeconds + 10)
                    {
                        host.KillConnection(LoaderId); result.commands++;
                        if (scenario == "kill10-overclock")
                        { host.TryActivateOverclock("DB", "overclock db", out _); result.commands++; }
                    }
                }
                bool boostedBeforeStep = host.OverclockedNode != null;
                host.StepSimulation(.02f);
                float now = host.SimulationTimeSeconds, dt = now - before;
                if (loader?.Resolution.HasValue == true && result.removedAt < 0) result.removedAt = loader.ResolvedAtSeconds;
                if (mixed && duels.Current?.Lifecycle == DuelLifecycle.Active && !host.RunEnded)
                {
                    if (encounter != duels.Current.Id)
                    {
                        encounter = duels.Current.Id; string id = duels.Current.Target.View.NodeId;
                        string text = "INTRUSION DETECTED / " + id + "\n\nssh operator@" + id.ToLowerInvariant() +
                            "\nAUTHENTICATING... OK\nINITIALIZING COUNTERMEASURES...";
                        splash = text.Length * Mathf.Max(.02f, (float)IntegrityWitness.Field(overlay, "splashCharacterSeconds")) + .25f;
                    }
                    if (splash > 0) splash -= dt;
                    else
                    {
                        var race = (CommandRaceSession)IntegrityWitness.Field(overlay, "race"); race.Advance(dt);
                        if (race.PlayerWon.HasValue)
                        { duels.Resolve(race); blockedUntil = now + (float)IntegrityWitness.Field(overlay, "resultSeconds"); overlay.Abort(); }
                    }
                }
                if (mixed && scenario == "visible-defense" && !host.RunEnded && now >= nextDecision && now >= blockedUntil &&
                    duels.Current?.Lifecycle != DuelLifecycle.Active)
                {
                    nextDecision = now + 6;
                    var payload = host.Payloads.Payloads.FirstOrDefault(p => p.Phase != PayloadPhase.Resolved && p.NextNode == p.FinalNode);
                    var busy = nodes.OrderByDescending(n => n.RequestedLoad / n.LoadCapacity).First();
                    string command = payload != null ? "intercept @ " + payload.NextNode.NodeId :
                        busy.RequestedLoad > busy.LoadCapacity && host.OverclockCooldownRemaining <= 0 ? "overclock " + busy.View.NodeId : null;
                    if (command != null)
                    { typeof(Terminal).GetMethod("HandleSubmit", Private).Invoke(terminal, new object[] { command }); result.commands++; }
                }
                var benign = host.RuntimeConnections.Where(c => !c.IsHostile).ToArray();
                int active = benign.Count(c => c.IsActive), connecting = benign.Count(c => c.IsConnecting);
                result.peakActive = Math.Max(result.peakActive, active);
                result.peakVisible = Math.Max(result.peakVisible, host.RuntimeConnections.Count(c => c.IsActive || c.IsConnecting));
                activeIntegral += active * dt;
                float shortfall = benign.Where(c => c.IsDemanding).Sum(c => Mathf.Max(0, c.DesiredLoad - c.AllocatedLoad));
                result.benignShortfallLoadSeconds += shortfall * dt;
                result.peakBenignShortfall = Mathf.Max(result.peakBenignShortfall, shortfall);
                bool contention = shortfall > .01f;
                if (contention) result.contentionSeconds += dt;
                if (result.removedAt >= 0)
                {
                    float postDt = Mathf.Max(0, now - Mathf.Max(before, result.removedAt));
                    if (contention) { result.postRemovalContentionSeconds += postDt; quiet = 0; }
                    else quiet += postDt;
                    if (result.recoveryAt < 0 && quiet >= 10)
                    { result.recoveryAt = now - quiet; result.recoveryDelay = Mathf.Max(0, result.recoveryAt - result.removedAt); }
                    if (contention || boostedBeforeStep || host.OverclockedNode != null) normalQuiet = 0;
                    else normalQuiet += postDt;
                    if (result.normalCapacityRecoveryDelay < 0 && normalQuiet >= 10)
                        result.normalCapacityRecoveryDelay = Mathf.Max(0, now - normalQuiet - result.removedAt);
                }
                for (int i = 0; i < nodes.Length; i++)
                {
                    NodeResources node = nodes[i]; NodeMeasure m = result.nodes[i];
                    float available = Mathf.Max(0, node.LoadCapacity - node.BaselineLoad - node.InfectionAllocatedLoad - node.DefensiveAllocatedLoad);
                    float excess = Mathf.Max(0, node.RequestedLoad - node.LoadCapacity);
                    m.requestedLoadSeconds += node.RequestedLoad * dt; m.capacitySeconds += node.LoadCapacity * dt;
                    m.availableLoadSeconds += available * dt; m.excessLoadSeconds += excess * dt;
                    m.peakRequested = Mathf.Max(m.peakRequested, node.RequestedLoad);
                    if (excess > .01f) { m.contentionSeconds += dt; if (result.removedAt >= 0) m.postRemovalContentionSeconds += Mathf.Max(0, now - Mathf.Max(before, result.removedAt)); }
                }
                if (Mathf.FloorToInt(now) != lastSecond || host.RunEnded)
                {
                    lastSecond = Mathf.FloorToInt(now);
                    samples.Add(new Sample { time = now, active = active, connecting = connecting, credited = host.CreditedWork,
                        arrived = policy == BenignArrivalPolicy.IndependentDemand ? traffic.Demand.arrived : admittedBenign,
                        admitted = policy == BenignArrivalPolicy.IndependentDemand ? traffic.Demand.admitted : admittedBenign, waiting = traffic.Demand.waiting,
                        outstandingWork = benign.Where(c => !c.IsComplete).Sum(c => (double)c.RemainingWork +
                            Enumerable.Range(c.CurrentOperationIndex + 1, c.OperationCount - c.CurrentOperationIndex - 1).Sum(i => (double)c.Definition.WorkAt(i))),
                        unadmitted = traffic.Demand.unadmitted, requested = nodes.Select(n => n.RequestedLoad).ToArray(),
                        capacity = nodes.Select(n => n.LoadCapacity).ToArray(),
                        available = nodes.Select(n => Mathf.Max(0, n.LoadCapacity - n.BaselineLoad - n.InfectionAllocatedLoad - n.DefensiveAllocatedLoad)).ToArray(),
                        benignRequested = nodes.Select(n => benign.Where(c => c.IsDemanding && c.TargetNode == n.View).Sum(c => c.DesiredLoad)).ToArray(),
                        benignAllocated = nodes.Select(n => benign.Where(c => c.IsDemanding && c.TargetNode == n.View).Sum(c => c.AllocatedLoad)).ToArray() });
                }
            }
            result.seconds = host.SimulationTimeSeconds; result.ending = host.Ending.ToString(); result.work = host.Shift;
            result.averageActive = (float)(activeIntegral / result.seconds); result.samples = samples.ToArray(); result.operations = operations.ToArray();
            result.completedOperations = operations.Count; result.completedMeanDelay = operations.Count == 0 ? 0 : operations.Average(o => o.delaySeconds);
            result.coreBreachAt = host.Integrity.Breach?.time ?? -1;
            result.integrityStrikes = host.Recorder.CurrentLog.events.Count(e => e.type == "integrity_damage");
            result.compromisedNodes = host.Integrity.Nodes.Count(n => n.Compromised);
            result.arrived = policy == BenignArrivalPolicy.IndependentDemand ? traffic.Demand.arrived :
                host.Recorder.CurrentLog.events.Count(e => e.type == "plan_queued" && e.source == "ambient_benign");
            result.admitted = policy == BenignArrivalPolicy.IndependentDemand ? traffic.Demand.admitted : admittedBenign;
            result.waiting = traffic.Demand.waiting; result.unadmitted = traffic.Demand.unadmitted;
            result.recoveryCensored = result.removedAt >= 0 && result.recoveryAt < 0;
            result.demandSignature = Hash(string.Join("\n", host.Recorder.CurrentLog.events.Where(e => e.type == "benign_demand_arrived")
                .Select(e => e.demandScheduledAtSeconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
            result.planSignature = Hash(DemandPlans(host));
            result.demandPrefixSignatures = PrefixHashes(host.Recorder.CurrentLog.events.Where(e => e.type == "benign_demand_arrived")
                .Select(e => e.demandScheduledAtSeconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
            result.planPrefixSignatures = PrefixHashes(host.Recorder.CurrentLog.events
                .Where(e => e.type == "plan_queued" && e.source == "independent_benign").Select(e => JsonUtility.ToJson(e.definition)));
            result.gameplaySignature = Hash(string.Join("\n", host.Recorder.CurrentLog.events.Select(JsonUtility.ToJson)) +
                string.Join("\n", host.Recorder.CurrentLog.snapshots.Select(JsonUtility.ToJson)));
            host.Recorder.EndRun(); typeof(ConnectionsDuelDirector).GetMethod("OnDisable", Private).Invoke(duels, null);
            Debug.Log($"DEMAND {result.scene}/{policy}/{mean:0.##}/{seed}/{scenario} arrivals{result.arrived} peak{result.peakActive} contention{result.contentionSeconds:0.##} tail{result.recoveryDelay:0.##} post{result.postRemovalContentionSeconds:0.##} WORK{result.work.credited:0.##} ending{result.ending}");
            return result;
        }
        public static void Measure()
        {
            Run();
            var runs = new List<Measurement>();
            string existing = Evidence + "/controlled-runs.json.gz";
            if (File.Exists(existing))
                using (var file = File.OpenRead(existing))
                using (var gzip = new GZipStream(file, CompressionMode.Decompress))
                using (var reader = new StreamReader(gzip))
                    runs.AddRange(JsonUtility.FromJson<Results>(reader.ReadToEnd()).runs);
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                foreach (int seed in Seeds)
                    foreach (string scenario in new[] { "healthy", "withdraw30", "kill10", "kill10-overclock" })
                        if (!runs.Any(r => r.scene == Path.GetFileNameWithoutExtension(scene) && r.policy == "PopulationResponsive" && r.seed == seed && r.scenario == scenario))
                            runs.Add(Observe(scene, BenignArrivalPolicy.PopulationResponsive, 8, seed, scenario));
                float matched = runs.Where(r => r.scene == Path.GetFileNameWithoutExtension(scene) && r.policy == "PopulationResponsive" && r.scenario == "healthy")
                    .Sum(r => r.seconds) / runs.Where(r => r.scene == Path.GetFileNameWithoutExtension(scene) && r.policy == "PopulationResponsive" && r.scenario == "healthy").Sum(r => r.arrived);
                foreach (float mean in new[] { matched, 4f, 6f, 8f, 12f })
                {
                    if (runs.Count(r => r.scene == Path.GetFileNameWithoutExtension(scene) && r.policy == "IndependentDemand" && r.meanInterval == mean) == Seeds.Length * 4) continue;
                    foreach (int seed in Seeds)
                    {
                        Measurement healthy = null;
                        foreach (string scenario in new[] { "healthy", "withdraw30", "kill10", "kill10-overclock" })
                        {
                            var r = Observe(scene, BenignArrivalPolicy.IndependentDemand, mean, seed, scenario);
                            if (healthy == null) healthy = r;
                            else if (!SameDemandPrefix(healthy, r))
                                throw new InvalidOperationException("Paired independent demand/recipe schedule changed.");
                            if (r.unadmitted != 0 || r.waiting != 0) throw new InvalidOperationException("Admission limit invalidates the measurement.");
                            runs.Add(r);
                        }
                    }
                    Save(runs, "controlled");
                }
            }
            Save(runs, "controlled");
            Debug.Log("Independent demand controlled measurement PASS: " + runs.Count + " real Unity runs; paired demand/recipes identical through common CORE-censored prefix; no unadmitted demand.");
        }
        public static void Natural()
        {
            var runs = new List<Measurement>();
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
                foreach (int seed in Seeds.Concat(new[] { 42, 99 }))
                    foreach (BenignArrivalPolicy policy in Enum.GetValues(typeof(BenignArrivalPolicy)))
                        foreach (string scenario in new[] { "unattended", "visible-defense" })
                            runs.Add(Observe(scene, policy, scene == CanonicalSceneAuthoring.Scenes[0] ? 8 : 6, seed, scenario, true, true));
            Save(runs, "natural");
            Debug.Log("Independent demand natural measurement PASS: " + runs.Count + " quota-enabled mixed-threat shifts; quotas and hostile tuning unchanged.");
        }
        private static void Save(List<Measurement> runs, string name)
        {
            Directory.CreateDirectory(Evidence);
            using (var file = File.Create(Evidence + "/" + name + "-runs.json.gz"))
            using (var gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal))
            { byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Results { runs = runs.ToArray() })); gzip.Write(bytes, 0, bytes.Length); }
            var samples = runs.Select(r => r.samples).ToArray(); var operations = runs.Select(r => r.operations).ToArray();
            for (int i = 0; i < runs.Count; i++) { runs[i].samples = null; runs[i].operations = null; }
            File.WriteAllText(Evidence + "/" + name + "-summary.json", JsonUtility.ToJson(new Results { runs = runs.ToArray() }, true));
            for (int i = 0; i < runs.Count; i++) { runs[i].samples = samples[i]; runs[i].operations = operations[i]; }
        }
    }
}
