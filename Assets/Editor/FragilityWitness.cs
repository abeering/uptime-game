using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class FragilityWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsFragilityV0";
        private static readonly List<string> checks = new();
        public static void Set(object owner, string name, object value) => owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private static void Check(bool value, string label)
        { checks.Add((value ? "PASS " : "FAIL ") + label); if (!value) throw new Exception(label); }
        private static void Near(float value, float expected, string label) => Check(
            Mathf.Abs(value - expected) < .003f, $"{label}: expected {expected:R}, observed {value:R}");
        public static SimulationHost Open(string scene = "Assets/Scenes/Connections.unity", bool coarse = true)
        {
            EditorSceneManager.OpenScene(scene);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            host.enabled = false;
            Set(host, "shiftEnabled", false); // Service mechanics independently of shift outcomes.
            Check(host.ServiceTimeoutsEnabled, scene + " authored service enforcement enabled");
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            traffic.AutoGenerationEnabled = false; traffic.HostileGenerationEnabled = false;
            traffic.RandomizeSeed = false; traffic.Seed = 1701;
            if (coarse) Set(host, "integrityTuning", new IntegrityTuning { enabled = false });
            Set(host, "naturalLoadNoiseEnabled", false);
            host.SetRunMode(ConnectionRunMode.Generated);
            host.Recorder.RecordingEnabled = true; host.Recorder.BeginRun();
            return host;
        }
        public static LogicalConnectionState Add(SimulationHost host, string id, float work = 100,
            float load = 5, bool hostile = false, OperationStage[] stages = null,
            TimedModifierBehaviorDefinition[] behaviors = null, float delay = 0)
        {
            var route = new[] { host.FindNodeResources("EXT1").View, host.FindNodeResources("API").View, host.FindNodeResources("DB").View };
            var plan = new ConnectionSpawnPlan(delay, id, route, "service-witness", "Service witness",
                stages ?? new[] { new OperationStage(OperationKind.Read, work, load, "Read") },
                hostile, behaviors);
            var definition = LogicalConnectionDefinition.FromPlan(plan);
            Set(definition, "startDelaySeconds", delay);
            return host.RegisterConnection(definition);
        }
        public static void Advance(SimulationHost host, float seconds)
        {
            int whole = Mathf.FloorToInt(seconds / .02f);
            for (int i = 0; i < whole && !host.RunEnded; i++) host.StepSimulation(.02f);
            float remainder = seconds - whole * .02f;
            if (remainder > .000001f) host.StepSimulation(remainder);
        }
        [MenuItem("Tools/Connections/Load Benign Fragility Fixture (Play Mode only)")]
        public static void LoadFixture() => LoadFixture(4, 2);
        [MenuItem("Tools/Connections/Load Healthy Fragility Fixture (Play Mode only)")]
        public static void LoadHealthyFixture() => LoadFixture(4, 1);
        [MenuItem("Tools/Connections/Load Slowed Safe Fragility Fixture (Play Mode only)")]
        public static void LoadSafeFixture() => LoadFixture(2, 2);
        private static void LoadFixture(float baseline, int count)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter canonical Play Mode first; fixture is never saved.");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            string[] ids = ConfigureFixture(host, baseline, count);
            Debug.Log("Benign fixture IDs: " + string.Join(" ", ids) +
                ". Inspect, overclock DB, or kill an ID. Stop Play Mode or reload scene to discard fixture tuning.");
        }
        // Real PURPOSE builder, existing queue and allocator. Only disposable test tuning changes.
        public static string[] ConfigureFixture(SimulationHost host, float baseline, int count = 2)
        {
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            Set(traffic, "legitimateWorkloadMultiplier", 1f); // Retain this fixture's authored 100 WORK / 5 LOAD examples.
            traffic.AutoGenerationEnabled = false; traffic.HostileGenerationEnabled = false;
            traffic.RandomizeSeed = false; traffic.Seed = 1701;
            Set(host, "naturalLoadNoiseEnabled", false);
            Set(traffic, "normalWorkSpread", 0f); Set(traffic, "benignOutlierChance", 0f);
            var profile = traffic.Profiles.Single(p => p.id == "product-lookup");
            profile.eligibleTargetIds = new[] { "DB" };
            foreach (var op in profile.operations)
            { op.minimumLoad = op.maximumLoad = 5; op.meanWork = 100; }
            Set(host.FindNodeResources("DB"), "loadCapacity", 10f);
            Set(host.FindNodeResources("DB"), "baselineLoad", baseline);
            host.SetRunMode(ConnectionRunMode.Generated);
            host.Recorder.RecordingEnabled = true; host.Recorder.BeginRun();
            return Enumerable.Range(0, count).Select(_ => traffic.QueueProfile("product-lookup").ConnectionId).ToArray();
        }
        public static void Validate() { Run(); Regressions(); }
        public static void Supplemental()
        {
            NaturalLoadNoiseWitness.Run();
            BackdoorWitness.Run();
            File.WriteAllText(Evidence + "/supplemental-checks.txt", "PASS NaturalLoadNoise\nPASS Backdoor\n");
        }

        public static void Regressions()
        {
            Directory.CreateDirectory(Evidence);
            var reports = new List<string>();
            foreach (var test in new (string name, Action run)[] {
                ("CanonicalRegressions", CanonicalSceneWitness.Regressions),
                ("CanonicalParity", CanonicalSceneWitness.Run), ("Lifecycle", LifecycleWitness.Run),
                ("Behavior", BehaviorWitness.Run), ("NaturalLoadNoise", NaturalLoadNoiseWitness.Run), ("Backdoor", BackdoorWitness.Run), ("Loader", LoaderWitness.Run), ("Infector", InfectorWitness.Run),
                ("InspectV2", InspectV2Witness.Run), ("ResolvedInspect", ResolvedInspectWitness.Run),
                ("DuelConcession", DuelConcessionWitness.Run), ("LoadOutput", LoadOutputWitness.Run),
                ("WorkOutput", WorkOutputWitness.Run), ("LogsOutput", LogsOutputWitness.Run) })
            {
                try { test.run(); reports.Add("PASS " + test.name); }
                catch (Exception error) { reports.Add("FAIL " + test.name + ": " + error.Message); Debug.LogException(error); }
            }
            File.WriteAllLines(Evidence + "/regression-checks.txt", reports);
            File.Copy(CanonicalSceneWitness.Evidence + "/regression-checks.txt", Evidence + "/canonical-regression-checks.txt", true);
            if (reports.Any(r => r.StartsWith("FAIL"))) throw new Exception("Regressions contain failures; inspect retained reports.");
        }
        [MenuItem("Tools/Connections/Run Fragility Witness")]
        public static void Run()
        {
            checks.Clear(); Directory.CreateDirectory(Evidence);
            try
            {
                var h = Open();
                ((NetworkHealthTuning)typeof(SimulationHost).GetField("healthTuning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(h)).deadlineTolerance = 9f;
                var a = Add(h, "pending", delay: 5);
                h.StepSimulation(5); Check(a.IsConnecting && !a.HasServiceTarget, "Pending excludes service budget");
                h.StepSimulation(.6f); Check(a.IsConnecting && !a.HasEstablished, "Connecting excludes service budget");
                h.StepSimulation(.6f); Near(a.EstablishedAtSeconds, 6.2f, "destination establishment");
                Near(a.ServiceNominalDurationSeconds, 20, "nominal WORK/LOAD");
                Near(a.ServiceBudgetSeconds, 30, "independent tolerance 1.5");
                Near(a.ServiceDeadlineSeconds, 36.2f, "establishment plus budget");
                Near(a.HealthDeadlineSeconds, 186.2f, "Health deadline independently tuned; does not enforce service");
                h.StepSimulation(20); Check(a.Resolution == ConnectionResolution.Completed && h.LegitimateFailureCount == 0,
                    "uncontended completion has no business failure");

                h = Open(); Set(h, "serviceTimeoutTolerance", 1f); h.ResetSimulation();
                a = Add(h, "exact", work: 50); h.StepSimulation(1.2f); h.StepSimulation(10);
                Check(a.Resolution == ConnectionResolution.Completed, "final completion exactly at deadline wins");
                Near(a.ResolvedAtSeconds, a.ServiceDeadlineSeconds, "exact boundary timestamp");

                h = Open(); a = Add(h, "coarse"); Add(h, "peer"); Add(h, "peer2"); Add(h, "peer3");
                h.StepSimulation(100);
                Check(a.Resolution == ConnectionResolution.TimedOut && a.TerminationCause == ConnectionTerminationCause.ServiceTimeout,
                    "coarse step crossing boundary yields distinct timeout");
                Near(a.ResolvedAtSeconds, 31.2f, "coarse timeout timestamp");
                Near(a.CompletedWork, 75, "partial WORK at timeout");
                Near(h.FindNodeResources("DB").AllocatedLoad, 0, "timeout releases all LOAD");
                Check(h.LegitimateFailureCount == 4 && h.BusinessFailures.Select(f => f.ConnectionId).Distinct().Count() == 4,
                    "one receipt per canonical connection");
                float stopped = a.CompletedWork; h.StepSimulation(100);
                Check(a.CompletedWork == stopped && !h.KillConnection(a.ConnectionId) && h.LegitimateFailureCount == 4,
                    "resolved kill and subsequent stepping cannot double count or progress");
                Check(h.Recorder.CurrentLog.events.Count(e => e.type == "business_failure") == 4 &&
                    h.Recorder.CurrentLog.events.First(e => e.type == "connection_resolved").serviceBudgetSeconds == 30,
                    "recorder stores budgets, causes, receipts and totals");
                Check(h.FindConnection(a.ConnectionId) == a && a.CompletedOperationCount == 0,
                    "historical timeout state retained");
                var prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>("Assets/Prefabs/Connections/InspectOutput.prefab");
                var output = UnityEngine.Object.Instantiate(prefab);
                output.Bind(new[] { a }, presentationMode: InspectPresentationMode.Verbose);
                Check(output.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "TIMED OUT"), "verbose historical timeout label");
                output.GetComponentInChildren<InspectEntry>().ShowResolvedSummary(a);
                Check(output.GetComponentsInChildren<ResolvedInspectRow>(true).Any(row =>
                    row.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "TIMED OUT")), "collapsed timeout label");
                UnityEngine.Object.DestroyImmediate(output.gameObject);

                h = Open();
                a = Add(h, "arc", stages: new[] { new OperationStage(OperationKind.Read, 10, 5, "First"),
                    new OperationStage(OperationKind.Write, 20, 5, "Second", CompletionEffect.InfectionDelivery, behaviors: new[] {
                        TimedModifierBehaviorDefinition.Rate("paused", 0, 0, 0) }) });
                h.StepSimulation(1.2f); float deadline = a.ServiceDeadlineSeconds;
                h.StepSimulation(2);
                Check(a.IsTransitioning && a.CompletedOperationCount == 1, "first operation completes and transition remains held");
                h.StepSimulation(.02f);
                Check(!a.IsTransitioning && float.IsPositiveInfinity(a.ProjectedServiceSeconds) && a.AtRisk,
                    "zero productive rate projects danger safely after transition");
                Near(a.ServiceDeadlineSeconds, deadline, "operation transition never resets deadline");
                h.StepSimulation(100);
                Check(a.Resolution == ConnectionResolution.TimedOut && a.CompletedOperationCount == 1 &&
                    a.CurrentOperationIndex == 1 && a.CompletedWork == 0 && a.Behaviors.ActiveModifiers.Count == 0 &&
                    h.Occurrences.Count == 0, "later timeout preserves earlier completion, cleans behavior, prevents completion effects");
                Near(a.ResolvedAtSeconds, deadline, "multi-operation deadline spans connection");

                foreach (bool recording in new[] { true, false })
                {
                    h = Open(); h.Recorder.RecordingEnabled = recording;
                    a = Add(h, "killed");
                    Check(h.KillConnection("killed") && !h.KillConnection("killed"), "Connecting kill exact once");
                    var active = Add(h, "active-kill"); h.StepSimulation(1.2f); h.KillConnection(active.ConnectionId);
                    var canceled = Add(h, "blackout"); h.TryInjectInfection("API", InfectionKind.Blackout, 0, out _);
                    h.StepSimulation(.6f);
                    Check(canceled.TerminationCause == ConnectionTerminationCause.Blackout, "Blackout interruption receipt");
                    h.TryRemoveInfection("API", InfectionKind.Blackout, out _);
                    var isolated = Add(h, "isolation"); h.StepSimulation(1.2f);
                    h.Worms.TryIsolate("DB", "isolate DB", out _);
                    Check(isolated.TerminationCause == ConnectionTerminationCause.Isolation, "isolation interruption receipt");
                    var hostile = Add(h, "hostile", hostile: true); h.KillConnection(hostile.ConnectionId);
                    Check(h.LegitimateFailureCount == 4, "recording=" + recording + " kill/Blackout/isolation parity; hostiles excluded");
                    h.ResetSimulation();
                    if (recording) h.Recorder.BeginRun();
                    Add(h, "timeout1"); Add(h, "timeout2"); Add(h, "timeout3"); Add(h, "timeout4");
                    h.StepSimulation(100);
                    Check(h.LegitimateFailureCount == 4, "recording=" + recording + " timeout accounting parity");
                    h.ResetSimulation(); Check(h.LegitimateFailureCount == 0 && h.BusinessFailures.Count == 0, "reset clears receipts");
                }
                h = Open(); Set(h.FindNodeResources("DB"), "baselineLoad", 4f);
                a = Add(h, "short", work: 10); var recovering = Add(h, "long");
                h.StepSimulation(1.2f); h.StepSimulation(3);
                Check(a.Resolution == ConnectionResolution.TimedOut && recovering.IsActive, "earlier timeout leaves longer peer unresolved");
                Near(recovering.AllocatedLoad, 5, "released LOAD reallocated to peer at timeout boundary");
                h.StepSimulation(.1f); Check(!recovering.AtRisk, "peer recovery published after release");
                h = Open();
                a = Add(h, "successful-arc", stages: new[] { new OperationStage(OperationKind.Read, 50, 5, "First"),
                    new OperationStage(OperationKind.Write, 50, 5, "Second") });
                h.StepSimulation(1.2f); Near(a.ServiceBudgetSeconds, 30, "budget sums whole authored arc");
                h.StepSimulation(10); h.StepSimulation(10);
                Check(a.Resolution == ConnectionResolution.Completed && a.CompletedOperationCount == 2,
                    "normal operation transition and final completion preserved");
                h = Open(); a = Add(h, "hostile-long", hostile: true,
                    behaviors: new[] { TimedModifierBehaviorDefinition.Rate("pause", 0, 0, 0) });
                var benign = Add(h, "benign-long", behaviors: new[] { TimedModifierBehaviorDefinition.Rate("pause", 0, 0, 0) });
                h.StepSimulation(1.2f); h.StepSimulation(.02f);
                Check(a.ServiceExplanation == benign.ServiceExplanation && a.AtRisk && benign.AtRisk,
                    "same service presentation for identical benign/hostile recipes");
                h.StepSimulation(40);
                Check(a.IsActive && benign.Resolution == ConnectionResolution.TimedOut && h.LegitimateFailureCount == 1,
                    "hostile lifetime preserved past service target");

                var trials = new List<string> { "scene,case,established,deadline,resolution,completedWork,failureUnits" };
                foreach (string scene in CanonicalSceneAuthoring.Scenes)
                {
                    foreach (string intervention in new[] { "none", "overclock", "sacrifice", "safe", "healthy" })
                    {
                        h = Open(scene, coarse: false);
                        string[] ids = ConfigureFixture(h, intervention == "safe" ? 2 : 4, intervention == "healthy" ? 1 : 2);
                        Advance(h, 3f); a = h.FindConnection(ids[0]);
                        Check(a.HasEstablished && !a.IsHostile && a.Definition.ProfileId == "product-lookup", scene + " real legitimate PURPOSE queue");
                        Check(a.AtRisk == (intervention != "safe" && intervention != "healthy"), intervention + " initial projection");
                        if (intervention == "safe") Check(a.Performance == LoadPerformance.Slowed, "slowed can be safe");
                        float fixedDeadline = a.ServiceDeadlineSeconds;
                        Advance(h, 10);
                        if (intervention == "overclock") h.TryActivateOverclock("DB", "overclock DB", out _);
                        if (intervention == "sacrifice") h.KillConnection(ids[1]);
                        Advance(h, .02f);
                        Near(a.ServiceDeadlineSeconds, fixedDeadline, intervention + " never moves fixed service deadline");
                        if (intervention == "overclock" || intervention == "sacrifice")
                            Check(!a.AtRisk, intervention + " danger clears with useful intervention window remaining");
                        Advance(h, 35);
                        Check(a.Resolution == (intervention == "none" ? ConnectionResolution.TimedOut : ConnectionResolution.Completed),
                            scene + " " + intervention + " final outcome " + a.Resolution);
                        int expected = intervention == "none" ? 2 : intervention == "sacrifice" ? 1 : 0;
                        Check(h.LegitimateFailureCount == expected && h.Integrity.Nodes.All(n => n.External || n.Value == 2),
                            scene + " " + intervention + " failures=" + expected + "; no Integrity damage");
                        trials.Add(FormattableString.Invariant($"{h.gameObject.scene.name},{intervention},{a.EstablishedAtSeconds:R},{a.ServiceDeadlineSeconds:R},{a.Resolution},{a.CompletedWork:R},{h.LegitimateFailureCount}"));
                        if (intervention == "none")
                        {
                            using var file = File.Create(Evidence + "/" + h.gameObject.scene.name + "-benign-timeouts.json.gz");
                            using var gzip = new GZipStream(file, CompressionMode.Compress);
                            using var writer = new StreamWriter(gzip);
                            writer.Write(JsonUtility.ToJson(h.Recorder.CurrentLog, true));
                        }
                    }
                }
                File.WriteAllLines(Evidence + "/benign-trials.csv", trials);
                h = Open(coarse: false); a = Add(h, "frozen"); h.StepSimulation(1.2f);
                h.DamageIntegrity(h.FindNodeResources("API"), "fixture", "freeze");
                h.DamageIntegrity(h.FindNodeResources("API"), "fixture", "freeze");
                h.DamageIntegrity(h.FindNodeResources("DB"), "fixture", "freeze");
                h.DamageIntegrity(h.FindNodeResources("DB"), "fixture", "freeze");
                float frozenTime = h.SimulationTimeSeconds; h.StepSimulation(100);
                Check(h.RunEnded && h.SimulationTimeSeconds == frozenTime && a.IsActive &&
                    !a.Resolution.HasValue && h.LegitimateFailureCount == 0, "CORE freezes unresolved work without business failure");
                h = Open(); a = Add(h, "late"); Add(h, "late-peer"); Add(h, "late-peer2"); Add(h, "late-peer3");
                h.StepSimulation(1.2f); h.StepSimulation(28); h.KillConnection("late-peer");
                h.KillConnection("late-peer2"); h.KillConnection("late-peer3"); h.StepSimulation(.02f);
                Check(a.Performance == LoadPerformance.AtSpeed && a.AtRisk, "at-speed connection can remain endangered after earlier delay");
                var replay = new string[2];
                for (int i = 0; i < 2; i++)
                {
                    h = Open(); ConfigureFixture(h, 4); Advance(h, 40);
                    replay[i] = string.Join("|", h.BusinessFailures.Select(f => f.ConnectionId + ":" + f.TimeSeconds.ToString("R") + ":" + f.Resolution));
                }
                Check(replay[0] == replay[1], "reset seeded recipes and failure replay match exactly");
                Debug.Log("FragilityWitness PASS: " + checks.Count + " assertions");

            }
            finally { File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks); }
        }
    }
}
