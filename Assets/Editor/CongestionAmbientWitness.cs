using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    // V1 orchestration reuses V0's real host observer, metrics and lifecycle fixture.
    // Configurations are disposable research settings, never saved into a scene.
    public static class CongestionAmbientWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsCongestionAmbientV1";
        [Serializable] public sealed class Configuration
        {
            public string name;
            public BenignArrivalPolicy policy;
            public float smallMean = 8, advancedMean = 6, loadScale = 1, dbWeight = 1;
            public float smallGoal, advancedGoal;
            public bool targetBusiest;
        }
        [Serializable] public sealed class Trial
        { public Configuration configuration; public IndependentDemandWitness.Measurement measurement; }
        [Serializable] public sealed class Results { public Trial[] runs; }
        public static readonly Configuration Nominal = new() { name = "nominal", policy = BenignArrivalPolicy.NominalOccupancy };
        private static readonly int[] ScreeningSeeds = { 1701, 3141 };
        private static readonly int[] FullSeeds = { 1701, 1702, 3141, 9001 };
        private static readonly List<string> checks = new();
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("CongestionAmbient FAIL: " + message); checks.Add("PASS " + message); }
        public static void Configure(SimulationHost host, Configuration config)
        {
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            if (config.loadScale != 1) IntegrityWitness.Set(traffic, "experimentalBenignLoadMultiplier", config.loadScale);
            float goal = host.gameObject.scene.name == "Connections" ? config.smallGoal : config.advancedGoal;
            if (goal > 0) IntegrityWitness.Set(traffic, "benignPopulationGoal", goal);
            if (config.dbWeight != 1)
            {
                var targets = (PurposeTargetOverride[])IntegrityWitness.Field(traffic, "purposeTargetOverrides") ?? Array.Empty<PurposeTargetOverride>();
                IntegrityWitness.Set(traffic, "experimentalBenignPurposeWeights", traffic.Profiles.Where(p => p.purposeTemplate).Select(p =>
                    new AmbientWeightOverride { profileId = p.id, weight = p.ambientWeight *
                        ((targets.FirstOrDefault(t => t.purposeId == p.id)?.eligibleTargetIds ?? p.eligibleTargetIds).Contains("DB") ? config.dbWeight : 1) }).ToArray());
            }
        }
        public static IndependentDemandWitness.Measurement Observe(string scene, Configuration config, int seed, string scenario,
            bool mixed = false, bool quota = false, string target = "DB")
        {
            var trial = new IndependentDemandWitness.DisruptionTrial { load = 8, targetNodeId = target };
            if (scenario.StartsWith("overlap", StringComparison.Ordinal))
            { trial.overlapOffset = 5; trial.survivalSeconds = 20; }
            else if (scenario.StartsWith("kill", StringComparison.Ordinal))
                trial.survivalSeconds = float.Parse(new string(scenario.Skip(4).TakeWhile(char.IsDigit).ToArray()), System.Globalization.CultureInfo.InvariantCulture);
            trial.overclock = scenario.EndsWith("-overclock", StringComparison.Ordinal);
            trial.killOneLegitimate = scenario.EndsWith("-shed", StringComparison.Ordinal);
            return IndependentDemandWitness.Observe(scene, config.policy,
                scene == CanonicalSceneAuthoring.Scenes[0] ? config.smallMean : config.advancedMean, seed, scenario,
                mixed, quota, 360, host => Configure(host, config), trial);
        }
        [MenuItem("Tools/Connections/Run Congestion Ambient V1 Witness")]
        public static void Run()
        {
            checks.Clear();
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                var healthy = Observe(scene, Nominal, 1701, "healthy");
                var attacked = Observe(scene, Nominal, 1701, "kill10");
                Check(IndependentDemandWitness.SameDemandPrefix(healthy, attacked), scene + " nominal demand and complete recipes ignore real LOADER delay/kill");
                var replay = Observe(scene, Nominal, 1701, "kill10");
                Check(replay.gameplaySignature == attacked.gameplaySignature, scene + " nominal gameplay exactly replays by seed");
                Check(attacked.arrived == attacked.admitted && attacked.unadmitted == 0 && attacked.waiting == 0, scene + " selected nominal requests are admitted and accounted");
                var host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy);
                host.StepSimulation(30);
                var traffic = host.GetComponent<ConnectionTrafficDirector>();
                string prefix = Schedule(host);
                var first = host.RuntimeConnections.First(c => !c.IsHostile && c.IsActive);
                host.KillConnection(first.ConnectionId); host.StepSimulation(80); string killed = Schedule(host);
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy); host.StepSimulation(110);
                Check(killed == Schedule(host), scene + " legitimate kill does not replenish or change nominal requests");
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy);
                host.StepSimulation(30); host.TryInjectInfection("DB", InfectionKind.Blackout, 0, out _); host.StepSimulation(40);
                foreach (var infection in host.Infections.ToArray()) host.TryRemoveInfection(infection.Target.View.NodeId, infection.Payload.Kind, out _);
                host.StepSimulation(40);
                Check(killed == Schedule(host), scene + " Blackout cancellations and cleaning do not change nominal demand");
                host.ResetSimulation(); host.Recorder.BeginRun(); host.StepSimulation(30);
                Check(prefix == Schedule(host), scene + " nominal reset restores ledger, counters and seeded opportunities");
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy);
                traffic = host.GetComponent<ConnectionTrafficDirector>(); traffic.Tick(110, true); string coarse = Schedule(host);
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy);
                traffic = host.GetComponent<ConnectionTrafficDirector>(); for (int i = 1; i <= 110; i++) traffic.Tick(i, true);
                Check(coarse == Schedule(host), scene + " coarse scheduling preserves every nominal opportunity and expiry");
                var opportunities = host.Recorder.CurrentLog.events.Where(e => e.type == "nominal_coverage_opportunity").ToArray();
                Check(opportunities.All(e => e.nominalPopulationBefore >= 0 && (e.connectionId == null || e.nominalLifetimeSeconds > 0)), scene + " recorder distinguishes nominal occupancy/lifetime from actual active states");
                host.Recorder.EndRun();
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy);
                var normal = host.GetComponent<ConnectionTrafficDirector>().GenerateBenignPlan(0);
                var manual = host.GetComponent<ConnectionTrafficDirector>().QueueProfile("product-lookup");
                var hostile = host.GetComponent<ConnectionTrafficDirector>().GenerateHostilePlan(0);
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy, configure: h =>
                    IntegrityWitness.Set(h.GetComponent<ConnectionTrafficDirector>(), "experimentalBenignLoadMultiplier", 3f));
                traffic = host.GetComponent<ConnectionTrafficDirector>();
                var scaled = traffic.GenerateBenignPlan(0);
                Check(normal.Operations.Zip(scaled.Operations, (a,b) => Mathf.Abs(a.TotalWork/a.DesiredLoad - b.TotalWork/b.DesiredLoad) < .0001f &&
                    Mathf.Abs(b.DesiredLoad-3*a.DesiredLoad) < .0001f).All(x => x), scene + " ambient LOAD/WORK scaling preserves normal nominal operation durations");
                Check(JsonUtility.ToJson(manual.GenerationTrace) == JsonUtility.ToJson(traffic.QueueProfile("product-lookup").GenerationTrace) &&
                    JsonUtility.ToJson(hostile.GenerationTrace) == JsonUtility.ToJson(traffic.GenerateHostilePlan(0).GenerationTrace), scene + " ambient benign scaling leaves manual and hostile recipes unchanged");
                Check(host.Recorder.CurrentLog.run.traffic.benignArrivalPolicy == "NominalOccupancy" &&
                    host.Recorder.CurrentLog.run.traffic.experimentalBenignLoadMultiplier == 3, scene + " recorder stores research policy and LOAD multiplier");
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy);
                string ordinaryHostile = JsonUtility.ToJson(host.GetComponent<ConnectionTrafficDirector>().GenerateHostilePlan(0).GenerationTrace);
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy, configure: h => Configure(h,
                    new Configuration { dbWeight = 2.5f }));
                Check(ordinaryHostile == JsonUtility.ToJson(host.GetComponent<ConnectionTrafficDirector>().GenerateHostilePlan(0).GenerationTrace),
                    scene + " experimental benign PURPOSE weights leave hostile selection/recipes unchanged");
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy);
                traffic = host.GetComponent<ConnectionTrafficDirector>(); host.StepSimulation(30);
                traffic.AutoGenerationEnabled = false; int count = traffic.Demand.arrived; host.StepSimulation(20);
                traffic.AutoGenerationEnabled = true; host.StepSimulation(.02f);
                Check(traffic.Demand.arrived == count, scene + " explicit pause creates no demand and resumes with a fresh opportunity wait");
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy);
                traffic = host.GetComponent<ConnectionTrafficDirector>(); host.SetRunMode(ConnectionRunMode.AuthoredWitness); host.StepSimulation(30);
                Check(traffic.Demand.arrived == 0, scene + " Authored Witness mode suppresses nominal ambient demand");
                host = IndependentDemandWitness.Open(scene, BenignArrivalPolicy.NominalOccupancy);
                traffic = host.GetComponent<ConnectionTrafficDirector>(); for (int i = 0; i < 261; i++) traffic.GenerateBenignPlan(0);
                host.StepSimulation(30);
                Check(traffic.Demand.arrived > 0 && traffic.Demand.arrived == traffic.Demand.unadmitted && traffic.Demand.admitted == 0,
                    scene + " nominal selected requests remain explicitly counted after finite ID exhaustion");
                host.Recorder.EndRun();
            }
            Directory.CreateDirectory(Evidence); File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks);
            Debug.Log("CongestionAmbientWitness PASS: " + checks.Count + " assertions.");
        }
        private static string Schedule(SimulationHost host) => string.Join("\n", host.Recorder.CurrentLog.events
            .Where(e => e.type == "nominal_coverage_opportunity").Select(e =>
                e.scheduledSpawnTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + e.connectionId + ":" + e.nominalPopulationBefore + ":" + e.nominalLifetimeSeconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
        public static void Validate()
        {
            var outcomes = new List<string>();
            foreach (var test in new (string name, Action run)[] {
                ("V1Nominal", Run), ("ScreeningReplay", VerifyScreening), ("V0Independent", IndependentDemandWitness.Run),
                ("ParentWork", WorkShiftWitness.Run), ("ParentQuotas", WorkShiftWitness.VerifyTargets),
                ("CanonicalGameplayAndTerminal", DuelConcessionWitness.Regressions) })
            {
                try { test.run(); outcomes.Add("PASS " + test.name); }
                catch (Exception error) { outcomes.Add("FAIL " + test.name + ": " + error.Message); }
            }
            Directory.CreateDirectory(Evidence); File.WriteAllLines(Evidence + "/validation-checks.txt", outcomes);
            foreach (string outcome in outcomes) Debug.Log("V1 VALIDATION " + outcome);
            if (outcomes.Any(s => s.StartsWith("FAIL", StringComparison.Ordinal)))
                throw new InvalidOperationException("V1 aggregate validation has failures; inspect retained reports without relaxing assertions.");
        }
        public static void VerifyScreening()
        {
            Results saved;
            using (var file = File.OpenRead(Evidence + "/concentration.json.gz"))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzip)) saved = JsonUtility.FromJson<Results>(reader.ReadToEnd());
            var outcomes = new List<string>();
            foreach (var old in saved.runs.Where(r => r.configuration.name == "nominal-load2-db" && r.measurement.seed == 1701 &&
                (r.measurement.scenario == "healthy" || r.measurement.scenario == "kill30")))
            {
                string scene = "Assets/Scenes/" + old.measurement.scene + ".unity";
                var replay = Observe(scene, old.configuration, 1701, old.measurement.scenario);
                if (replay.gameplaySignature != old.measurement.gameplaySignature)
                    throw new InvalidOperationException("Screening changed after isolating benign weight controls: " + scene);
                outcomes.Add("PASS " + old.measurement.scene + "/" + old.measurement.scenario + " exact screening gameplay replay with isolated benign weights");
            }
            File.WriteAllLines(Evidence + "/screening-replay-checks.txt", outcomes);
            Debug.Log("V1 screening replay PASS: " + outcomes.Count + " exact real Unity traces.");
        }
        public static void NominalFirst()
        {
            Run();
            Group("nominal-first", new[] { Nominal }, ScreeningSeeds,
                new[] { "healthy", "kill5", "kill10", "kill20", "kill30", "overlap20" });
        }
        public static void Comparisons() => Group("comparisons", new[] {
            new Configuration { name = "population", policy = BenignArrivalPolicy.PopulationResponsive },
            new Configuration { name = "independent-trial", policy = BenignArrivalPolicy.IndependentDemand },
            new Configuration { name = "independent-matched", policy = BenignArrivalPolicy.IndependentDemand, smallMean = 23.225807f, advancedMean = 18.947369f }
        }, ScreeningSeeds, new[] { "healthy", "kill10", "kill30", "overlap20" });
        public static void Variations() => Group("variations", new[] {
            new Configuration { name = "nominal-load3", policy = BenignArrivalPolicy.NominalOccupancy, loadScale = 3 },
            new Configuration { name = "nominal-load3-headroom", policy = BenignArrivalPolicy.NominalOccupancy, loadScale = 3, smallGoal = 2.5f, advancedGoal = 3 }
        }, ScreeningSeeds, new[] { "healthy", "kill10", "kill30", "overlap20" });
        public static void Headroom() => Group("headroom", new[] {
            new Configuration { name = "nominal-load2-goal6", policy = BenignArrivalPolicy.NominalOccupancy, loadScale = 2, smallGoal = 6, advancedGoal = 8 }
        }, ScreeningSeeds, new[] { "healthy", "kill10", "kill30", "overlap20" });
        public static void Concentration() => Group("concentration", new[] {
            new Configuration { name = "nominal-load2-db", policy = BenignArrivalPolicy.NominalOccupancy, loadScale = 2, dbWeight = 2.5f },
            new Configuration { name = "nominal-load1.5-db-goal6", policy = BenignArrivalPolicy.NominalOccupancy, loadScale = 1.5f, dbWeight = 2, smallGoal = 6, advancedGoal = 8 }
        }, ScreeningSeeds, new[] { "healthy", "kill10", "kill30", "overlap20" });
        public static readonly Configuration Candidate = new() { name = "nominal-load2-db-busy", policy = BenignArrivalPolicy.NominalOccupancy, loadScale = 2, dbWeight = 2.5f, targetBusiest = true };
        public static void Full() => Group("full", new[] { Candidate }, FullSeeds,
            new[] { "healthy", "kill5", "kill10", "kill20", "kill30", "overlap20", "kill20-overclock", "kill20-shed", "overlap20-overclock", "overlap20-shed" });
        public static void Quotas() => Group("quotas", new[] { Nominal, Candidate }, ScreeningSeeds,
            new[] { "healthy", "kill10", "kill30" }, quota: true);
        public static void Natural() => Group("natural", new[] { Nominal, Candidate }, FullSeeds,
            new[] { "unattended", "visible-defense" }, mixed: true, quota: true);
        public static readonly Configuration AdvancedCandidate = new() { name = "nominal-load1.5-db-goal8-busy", policy = BenignArrivalPolicy.NominalOccupancy,
            loadScale = 1.5f, dbWeight = 2, advancedGoal = 8, targetBusiest = true };
        public static void AdvancedTarget() => Group("advanced-target", new[] { AdvancedCandidate }, FullSeeds,
            new[] { "healthy", "kill5", "kill10", "kill20", "kill30", "overlap20", "kill20-overclock", "kill20-shed", "overlap20-overclock", "overlap20-shed" }, onlyScene: "ConnectionsAdvanced");
        public static void AdvancedShifts()
        {
            Group("advanced-quotas", new[] { AdvancedCandidate }, ScreeningSeeds, new[] { "healthy", "kill10", "kill30" }, quota: true, onlyScene: "ConnectionsAdvanced");
            Group("advanced-natural", new[] { AdvancedCandidate }, ScreeningSeeds, new[] { "unattended", "visible-defense" }, mixed: true, quota: true, onlyScene: "ConnectionsAdvanced");
        }
        public static void Group(string name, Configuration[] configurations, int[] seeds, string[] scenarios, bool mixed = false, bool quota = false, string onlyScene = null)
        {
            var runs = new List<Trial>();
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                if (onlyScene != null && Path.GetFileNameWithoutExtension(scene) != onlyScene) continue;
                foreach (var config in configurations)
                {
                    foreach (int seed in seeds)
                    {
                        IndependentDemandWitness.Measurement healthy = null;
                        foreach (string scenario in scenarios)
                        {
                            Debug.Log("V1 TRIAL " + name + "/" + scene + "/" + config.name + "/" + seed + "/" + scenario);
                            string target = "DB";
                            if (config.targetBusiest && healthy != null)
                            {
                                var at90 = healthy.samples.FirstOrDefault(s => s.time >= 90) ?? healthy.samples.Last();
                                int node = Enumerable.Range(0, healthy.nodes.Length).OrderByDescending(i => at90.benignRequested[i]).First();
                                target = healthy.nodes[node].id;
                            }
                            var run = Observe(scene, config, seed, scenario, mixed, quota, target);
                            if (healthy == null) healthy = run;
                            else if (!mixed && config.policy != BenignArrivalPolicy.PopulationResponsive && !IndependentDemandWitness.SameDemandPrefix(healthy, run, quota))
                                throw new InvalidOperationException("V1 demand/recipe comparison changed: " + scene + "/" + config.name + "/" + seed + "/" + scenario);
                            if (run.waiting != 0 || run.unadmitted != 0) throw new InvalidOperationException("V1 admission loss: " + config.name);
                            runs.Add(new Trial { configuration = config, measurement = run });
                        }
                    }
                    Save(name, runs);
                }
            }
            Debug.Log("CongestionAmbient " + name + " PASS: " + runs.Count + " real Unity runs; " +
                (mixed ? "ordinary mixed-threat shifts; no paired demand claim." : "independent/nominal pairs preserve scheduled recipes through the common ending-censored prefix."));
        }
        private static void Save(string name, List<Trial> runs)
        {
            Directory.CreateDirectory(Evidence);
            using var file = File.Create(Evidence + "/" + name + ".json.gz");
            using var gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal);
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Results { runs = runs.ToArray() }));
            gzip.Write(bytes, 0, bytes.Length);
        }
    }
}
