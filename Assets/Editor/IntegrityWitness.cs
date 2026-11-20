using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class IntegrityWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsIntegrityV0";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly List<string> results = new List<string>();
        public static void RunRegressions()
        {
            var checks = new (string name, Action run)[]
            {
                ("Traffic", TrafficWitness.Run), ("AmbientHostileActivity", AmbientHostileActivityWitness.Run),
                ("NetworkHealth", NetworkHealthWitness.Run), ("Duel", DuelWitness.Run),
                ("Consequence", ConsequenceWitness.Run), ("AdvancedScene", AdvancedSceneWitness.Run),
                ("Recorder", RecorderWitness.Run), ("Payload", PayloadWitness.Run),
                ("Worm", WormWitness.Run), ("AmbientPayloadThreat", AmbientPayloadThreatWitness.Run),
                ("Scan", ScanWitness.Run), ("Infection", InfectionWitness.Run),
                ("Overclock", OverclockWitness.Run), ("NodeStatus", NodeStatusWitness.Run),
                ("Geometry", GeometryWitness.Run), ("Resource", ResourceWitness.Run)
            };
            var outcomes = new List<string>();
            foreach (var check in checks)
            {
                try { check.run(); outcomes.Add("PASS " + check.name); }
                catch (Exception error) { outcomes.Add("FAIL " + check.name + ": " + error.Message); }
            }
            Directory.CreateDirectory(Evidence);
            File.WriteAllLines(Evidence + "/regression-checks.txt", outcomes);
            foreach (string outcome in outcomes) Debug.Log("INTEGRITY REGRESSION " + outcome);
            if (outcomes.Any(o => o.StartsWith("FAIL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Integrity regression failures; see regression-checks.txt and parent-revision reproduction.");
        }
        public static void BuildAndRun() { IntegritySceneAuthoring.Build(); Run(); }
        public static void RunFinalChecks()
        {
            BuildAndRun();
            RunRegressions();
        }
        [MenuItem("Tools/Connections/Run Integrity Witness")]
        public static void Run()
        {
            results.Clear();
            AllocationExamples();
            Graph();
            DamageKnowledge();
            Overload();
            DuelAndFreeze();
            BindingsAndIsolation();
            Directory.CreateDirectory(Evidence);
            File.WriteAllLines(Evidence + "/deterministic-checks.txt", results);
            Debug.Log("IntegrityWitness PASS: " + results.Count + " deterministic assertions.");
        }
        public static SimulationHost Open(bool trafficEnabled = false, int seed = 1701, Action<SimulationHost> configure = null)
        {
            EditorSceneManager.OpenScene(IntegritySceneAuthoring.Scene);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            traffic.RandomizeSeed = false; traffic.Seed = seed;
            traffic.AutoGenerationEnabled = trafficEnabled;
            typeof(ConnectionsDuelDirector).GetMethod("OnEnable", Private).Invoke(host.GetComponent<ConnectionsDuelDirector>(), null);
            configure?.Invoke(host);
            host.ResetSimulation();
            host.Recorder.BeginRun();
            return host;
        }
        private static void AllocationExamples()
        {
            // Numerical coverage independent of ResourceWitness's older inspect-pill assertion.
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var settings = new SerializedObject(host);
            settings.FindProperty("integrityTuning.enabled").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Set(host, "connectionTravelSecondsPerLink", .000001f);
            host.SetRunMode(ConnectionRunMode.AuthoredWitness);
            NodeResources db = host.FindNodeResources("DB");
            LogicalConnectionState C(string id) => host.FindConnection(id);
            bool Near(float actual, float expected) => Mathf.Abs(actual - expected) < .001f;
            host.StepSimulation(.02f);
            Check(Near(C("c1").AllocatedLoad, 5) && Near(db.RequestedLoad, 5),
                "allocator no contention: expected c1=5 and DB request=5; observed within0.001");
            int steps = 0;
            while (!C("c3").IsActive && steps++ < 1000) host.StepSimulation(.02f);
            host.StepSimulation(.02f);
            Check(Near(db.RequestedLoad, 13) && Near(db.AllocatedLoad, 10) &&
                Near(C("c1").AllocatedLoad, 50f / 13) && Near(C("c3").AllocatedLoad, 80f / 13) &&
                Near(C("c2").AllocatedLoad, 5),
                "allocator oversubscription/independent target: expected DB13/10 c1=3.846154 c3=6.153846 AUTH c2=5; observed within0.001");
            steps = 0;
            while (C("c3").CurrentOperationIndex == 0 && steps++ < 10000) host.StepSimulation(.02f);
            Check(C("c3").IsTransitioning && Near(C("c3").AllocatedLoad, 0), "operation transition releases LOAD at boundary");
            host.StepSimulation(.02f);
            Check(Near(C("c3").AllocatedLoad, 4) && Near(db.RequestedLoad, 9),
                "next operation: expected c3=4 DB request=9; observed within0.001");
            steps = 0;
            while (!C("c3").IsComplete && steps++ < 10000) host.StepSimulation(.02f);
            host.StepSimulation(.02f);
            Check(Near(C("c1").AllocatedLoad, 5) && Near(db.RequestedLoad, 5),
                "completion releases resources: expected c1=5 DB request=5; observed within0.001");
            Set(db, "baselineLoad", 2f); host.ResetSimulation();
            steps = 0;
            while (!C("c3").IsActive && steps++ < 1000) host.StepSimulation(.02f);
            host.StepSimulation(.02f);
            Check(Near(db.RequestedLoad, 15) && Near(db.AllocatedLoad, 10) && Near(C("c1").AllocatedLoad, 40f / 13),
                "baseline priority: expected request15 allocated10 c1=3.076923; observed within0.001");
            host.KillConnection("c3"); host.StepSimulation(.02f);
            Check(Near(C("c1").AllocatedLoad, 5) && Near(db.RequestedLoad, 7),
                "kill releases resources: expected c1=5 baseline+request7; observed within0.001");
            Set(db, "baselineLoad", 10f); host.ResetSimulation(); host.StepSimulation(.02f);
            Check(Near(C("c1").AllocatedLoad, 0) && Near(C("c1").CompletedWork, 0),
                "baseline blocking: expected c1 LOAD0 WORK0; observed within0.001");
        }
        private static void Graph()
        {
            var host = Open();
            var tuning = new IntegrityTuning { enabled = true, criticalNodeIds = new[] { "DB", "AUTH" } };
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            IntegrityState Model(PhysicalLinkReference[] links = null) => new IntegrityState(host.Nodes,
                links ?? host.PhysicalLinks, n => !traffic.IsInternalNode(n.View), tuning);
            var model = Model();
            Check(model.Nodes.Where(n => !n.External).All(n => n.Value == 2) &&
                model.Nodes.Count(n => n.External) == 2 && IntegrityState.Maximum == 2,
                "initial: six internal nodes at 2/max2; two external ingress nodes excluded");
            Check(!model.Damage(host.FindNodeResources("EXT1"), "test", "external", 0, true), "external damage rejected");
            void Compromise(IntegrityState state, string id)
            { var node = host.FindNodeResources(id); state.Damage(node, "test", id, 0, true); state.Damage(node, "test", id, 0, true); }
            Compromise(model, "DB");
            Check(!model.For(host.FindNodeResources("DB")).Connected && !model.LatchBreach(0), "isolated critical compromise: no route and no loss");
            Compromise(model, "API");
            Check(model.Snapshot().connectedLinks.Length == 0 && !model.LatchBreach(0), "adjacent isolated compromises: links remain normal");
            Compromise(model, "WEB");
            Check(model.LatchBreach(0) && model.Breach.path.SequenceEqual(new[] { "EXT1", "WEB", "API", "DB" }), "EXT1-WEB-API-DB exact winning path");
            Check(!model.Damage(host.FindNodeResources("AUTH"), "test", "after_end", 1, true), "model rejects post-breach damage");
            model = Model(); Compromise(model, "WEB"); Compromise(model, "API");
            Check(!model.LatchBreach(0) && model.Snapshot().connectedLinks.Length == 2, "healthy critical blocks loss; inward connected region still visible");
            Compromise(model, "AUTH");
            Check(model.LatchBreach(0) && model.Breach.criticalNode == "AUTH", "second critical supported independently");
            model = Model(); Compromise(model, "JOBS"); Compromise(model, "API"); Compromise(model, "DB");
            Check(model.LatchBreach(0) && model.Breach.path[0] == "EXT2", "second external ingress establishes independent breach");
            var extra = new GameObject("witness cycle", typeof(RectTransform), typeof(PhysicalLinkView)).GetComponent<PhysicalLinkView>();
            Set(extra, "linkId", "cycle"); Set(extra, "nodeA", host.FindNodeResources("WEB").View); Set(extra, "nodeB", host.FindNodeResources("DB").View);
            model = Model(host.PhysicalLinks.Concat(new[] { new PhysicalLinkReference(extra) }).ToArray());
            Compromise(model, "WEB"); Compromise(model, "API"); Compromise(model, "DB");
            Check(model.LatchBreach(0) && model.Breach.path.Length == 3 && model.IsConnectedLink("cycle"), "cycle terminates; deterministic shortest physical path");
            UnityEngine.Object.DestroyImmediate(extra.gameObject);
            host.Recorder.EndRun();
        }
        private static void DamageKnowledge()
        {
            var host = Open(); var db = host.FindNodeResources("DB");
            Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 3, out _), "accepted infection");
            Check(host.Integrity.For(db).Value == 1 && !host.Integrity.For(db).Revealed &&
                !host.Recorder.CurrentLog.events.Last(e => e.type == "integrity_damage").integrity.visible,
                "first silent infection: actual1, unrevealed, no damage announcement");
            Check(!host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 3, out _) && host.Integrity.For(db).Value == 1,
                "duplicate rejection: unchanged Integrity");
            Check(host.StartScan("DB", "scan DB", out _) == ScanStartResult.Started, "existing scan lifecycle accepted");
            host.StepSimulation(host.ScanDurationSeconds);
            Check(host.Integrity.For(db).Revealed && host.IntegrityReceipt(db).Contains("1/2"), "scan completion reveals true1");
            host.StartClean("DB", "clean DB", out var clean);
            Check(host.SelectCleanPid(clean.Processes.First(p => p.CpuPercent > 80).Pid, out _) == CleanSelectionResult.Success &&
                host.Integrity.For(db).Value == 1 && host.Infections.Count == 0, "CLEAN removes infection; damage persists");
            Check(host.TryInjectInfection("DB", InfectionKind.Backdoor, 0, out _) && host.Integrity.For(db).Compromised &&
                host.Integrity.For(db).Revealed && !host.RunEnded, "different kind: zero revealed immediately; disconnected critical not loss");
            Check(!host.DamageIntegrity(db, "test", "clamp") && host.Integrity.For(db).Value == 0, "zero clamped; no further damage");
            host.StartClean("DB", "clean DB", out clean);
            host.SelectCleanPid(clean.Processes.First(p => p.CpuPercent > 80).Pid, out _);
            Check(host.IntegrityReceipt(db).Contains("0/2  COMPROMISED"), "CLEAN at zero reports compromise without repair");
            host.ResetSimulation();
            host.TryInjectInfection("DB", InfectionKind.Backdoor, 0, out _);
            host.StartClean("DB", "clean DB", out clean);
            host.SelectCleanPid(clean.Processes.First(p => p.CpuPercent > 80).Pid, out _);
            Check(host.Integrity.For(db).Revealed && host.Integrity.For(db).Value == 1, "successful CLEAN independently reveals hidden total");
            host.ResetSimulation();
            host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 1, out _);
            host.TryInjectInfection("DB", InfectionKind.Backdoor, 0, out _);
            Check(host.Integrity.For(db).Compromised && host.Integrity.For(db).Revealed, "second hidden strike reaches zero and forces reveal");
            host.ResetSimulation();
            Check(host.Integrity.Nodes.Where(n => !n.External).All(n => n.Value == 2 && !n.Revealed && !n.Overloaded), "reset clears all damage/knowledge/timers");
            host.Recorder.EndRun();
        }
        private static void Overload()
        {
            var host = Open(); var db = host.FindNodeResources("DB");
            // Non-Duel consequence suppression isolates overload damage from infection damage.
            host.TryApplyConsequenceInfection("DB", InfectionKind.PhantomLoad, 16, "witness:load", out _, true);
            host.StepSimulation(9f);
            Check(host.Integrity.For(db).Value == 2 && host.Integrity.For(db).Overloaded, "16/10 LOAD at9s: no damage before full10s warning");
            host.TryActivateOverclock("DB", "overclock DB", out _);
            host.StepSimulation(.02f);
            Check(!host.Integrity.For(db).Overloaded && host.Integrity.For(db).OverloadElapsed == 0 && host.Integrity.For(db).Value == 2,
                "overclock: effective capacity15;16/15 below1.5; timer fully resets");
            host.StepSimulation(15f);
            Check(host.Integrity.For(db).Value == 2, "no strike during overclock window");
            host.StepSimulation(9.99f);
            Check(host.Integrity.For(db).Value == 1 && host.Integrity.For(db).Revealed,
                "fresh sustained overload window: one overt strike");
            host.StepSimulation(10f);
            Check(host.Integrity.For(db).Value == 0, "continued severe load: second full warning window strikes again");
            host.ResetSimulation();
            host.TryApplyConsequenceInfection("DB", InfectionKind.PhantomLoad, 15, "witness:threshold", out _, true);
            host.StepSimulation(30f);
            Check(host.Integrity.For(db).Value == 2 && !host.Integrity.For(db).Overloaded, "exact1.5 threshold does not trigger; allocated LOAD never exceeds10");
            host.Recorder.EndRun();
        }
        private static void DuelAndFreeze()
        {
            var host = Open(); var director = host.GetComponent<ConnectionsDuelDirector>();
            var config = ConsequenceConfiguration.Explicit(ConsequenceCandidate.Atomic("install",
                new ConsequenceEffectSpec { kind = ConsequenceKind.Infection, infectionKind = InfectionKind.PhantomLoad, parameter = 3f }));
            Check(director.TrySchedule("WEB", DuelVariant.CommandRace, 0, "witness", out _, config), "Duel with infection consequence scheduled");
            var overlay = UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
            var race = (CommandRaceSession)Field(overlay, "race");
            for (int i = 0; !race.PlayerWon.HasValue && i < 10000; i++) race.Advance(.02f);
            int duelId = director.Current.Id;
            director.Resolve(race); overlay.Abort();
            Check(host.Integrity.For(host.FindNodeResources("WEB")).Value == 1 && host.Infections.Count == 1 &&
                host.Recorder.CurrentLog.events.Count(e => e.type == "integrity_damage") == 1 &&
                host.Recorder.CurrentLog.events.First(e => e.type == "integrity_damage").integrity.cause == "duel:" + duelId,
                "lost Duel: exactly one attributable strike despite accepted infection consequence");
            director.Resolve(race);
            Check(host.Recorder.CurrentLog.events.Count(e => e.type == "integrity_damage") == 1, "repeated Duel resolution cannot duplicate its strike");
            host.TryInjectInfection("WEB", InfectionKind.Backdoor, 0, out _);
            Check(host.Integrity.For(host.FindNodeResources("WEB")).Value == 0, "later independent infection still strikes normally");
            var route = new[] { "EXT1", "WEB", "API", "DB" }.Select(id => host.FindNodeResources(id).View).ToArray();
            host.GetComponent<ConnectionTrafficDirector>().QueueSpawnPlan(new ConnectionSpawnPlan(
                host.SimulationTimeSeconds, "r1", route, "integrity-freeze-witness", "Long-running service",
                new[] { new OperationStage(OperationKind.Write, 1000f, 1f, "Service") }, true,
                identitySeed: 91, identityMode: ConnectionIdentityMode.ReactiveMisdirection));
            host.StepSimulation(.02f);
            var reactive = host.FindConnection("r1");
            string firstAlias = reactive.Identity.AliasIdentity;
            host.NotifyFailedIdentityAddress(firstAlias, "inspect " + firstAlias);
            Check(reactive.Identity.AliasIdentity != firstAlias, "live failed inspection still triggers existing reactive identity behavior");
            host.StartScan("DB", "scan DB", out _);
            host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 1, out _);
            host.StartClean("DB", "clean DB", out var clean);
            director.TrySchedule("API", DuelVariant.CommandRace, 0, "freeze-active", out _, config);
            var frozenRace = (CommandRaceSession)Field(overlay, "race");
            foreach (string id in new[] { "API", "AUTH" })
                for (int i = 0; i < 2; i++) host.DamageIntegrity(host.FindNodeResources(id), "development", "witness");
            Check(host.RunEnded && host.Integrity.Breach.criticalNode == "AUTH", "authoritative breach latches at damaged critical");
            float time = host.SimulationTimeSeconds; int events = host.Recorder.CurrentLog.events.Count;
            host.StepSimulation(100);
            string frozenAlias = reactive.Identity.AliasIdentity;
            host.NotifyFailedIdentityAddress(frozenAlias, "inspect " + frozenAlias);
            Check(reactive.Identity.AliasIdentity == frozenAlias, "read-only inspection after breach cannot mutate frozen reactive identities");
            frozenRace.Advance(1000);
            director.Resolve(frozenRace);
            Check(!host.TryInjectInfection("RPT", InfectionKind.Blackout, 0, out _) &&
                !host.TryActivateOverclock("DB", "overclock DB", out _) &&
                host.SelectCleanPid(clean.Processes[0].Pid, out _) == CleanSelectionResult.NoSession &&
                !host.TryRemoveInfection("DB", InfectionKind.PhantomLoad, out _) &&
                !director.TrySchedule("DB", out _), "terminal state rejects infection/overclock/CLEAN/removal/Duel scheduling");
            Check(host.SimulationTimeSeconds == time && host.Recorder.CurrentLog.events.Count(e => e.type == "core_breached") == 1 &&
                host.Recorder.CurrentLog.snapshots.Last().integrity.breach != null,
                "frozen clock; exact-once loss and final graph snapshot");
            Check(host.Integrity.Snapshot().links.Length == 7 && host.Integrity.Snapshot().links.All(l =>
                !string.IsNullOrEmpty(l.nodeA) && !string.IsNullOrEmpty(l.nodeB)), "final telemetry includes all physical edge endpoints");
            Check(JsonUtility.FromJson<IntegritySnapshot>(JsonUtility.ToJson(host.Integrity.Snapshot())).breached,
                "JSON round trip preserves explicit terminal flag despite Unity inline null serialization");
            Check(host.Recorder.Save(false) && File.Exists(host.Recorder.OutputPath), "final telemetry saved");
            host.ResetSimulation();
            Check(!JsonUtility.FromJson<IntegritySnapshot>(JsonUtility.ToJson(host.Integrity.Snapshot())).breached,
                "reset graph JSON explicitly distinguishes no breach from an empty inline breach object");
            Check(!host.RunEnded && host.ActiveClean == null && host.ActiveScan == null && director.Current == null,
                "restart clears terminal state and pending sessions");
            host.Recorder.EndRun();
        }
        private static void BindingsAndIsolation()
        {
            var host = Open();
            var presenter = UnityEngine.Object.FindFirstObjectByType<IntegrityPresenter>();
            typeof(IntegrityPresenter).GetMethod("OnEnable", Private).Invoke(presenter, null);
            var node = host.FindNodeResources("DB");
            Color neutral = node.View.GetComponent<Image>().color;
            host.Integrity.Reveal(node, "witness", 0); presenter.RenderNow();
            Check(node.View.GetComponent<Image>().color == neutral, "known2 and unrevealed share default appearance");
            host.DamageIntegrity(node, "witness", "orange"); presenter.RenderNow();
            Check(node.View.GetComponent<Outline>().effectColor.r == 1f && node.View.GetComponent<Image>().color == neutral,
                "known1: orange border, unchanged dark background");
            host.DamageIntegrity(node, "witness", "red"); presenter.RenderNow();
            Check(node.View.GetComponent<Image>().color.r > neutral.r && node.View.GetComponent<Outline>().effectColor.g < .3f,
                "zero: permanent red card and border");
            foreach (GameObject root in host.gameObject.scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "no missing script: " + child.name);
            var status = AssetDatabase.LoadAssetAtPath<GameObject>(IntegritySceneAuthoring.StatusPrefab);
            Check(status.GetComponentsInChildren<NodeStatusPanel>(true).All(p => new SerializedObject(p).FindProperty("labels").arraySize == 7),
                "experimental prefab supports seven simultaneous status rows");
            foreach (string scene in new[] { "Connections", "ConnectionsAdvanced", "ConnectionsAdvancedPressure" })
            {
                EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
                host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>(); host.ResetSimulation();
                Check(!host.IntegrityEnabled && host.Integrity == null && !host.RunEnded &&
                    UnityEngine.Object.FindFirstObjectByType<IntegrityPresenter>() == null, scene + ": Integrity disabled, original behavior retained");
            }
        }
        public static object Field(object owner, string name) => owner.GetType().GetField(name, Private).GetValue(owner);
        public static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("IntegrityWitness FAIL: " + message);
            results.Add("PASS expected=observed: " + message);
        }
    }
}
