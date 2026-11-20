using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class CanonicalSceneWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsCanonicalSceneParityV0";
        public static readonly string[] Families = { "ordinary", "reconnaissance", "payload", "worm", "payload_launcher", "direct_duel", "connection_duel" };
        private static readonly List<string> checks = new();
        [Serializable] public sealed class RunResult
        {
            public string scene, directive, difficulty;
            public IntegrityPressureWitness.Result result;
        }
        [Serializable] public sealed class Results { public RunResult[] runs; }
        public static SimulationHost Open(string scene, HackerDirective mode = HackerDirective.SPREAD, bool auto = false,
            int seed = 1701, float? interval = null) => IntegrityWitness.Open(auto, seed, host =>
            {
                host.GetComponent<HackerDirectives>().ConfigureForDevelopment(mode);
                if (interval.HasValue) IntegrityWitness.Set(host.GetComponent<ConnectionTrafficDirector>(), "hostileMeanIntervalSeconds", interval.Value);
            }, scene);
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("CanonicalSceneWitness FAIL: " + message); checks.Add("PASS " + message); }
        private static void Bound(UnityEngine.Object owner, params string[] names)
        {
            var fields = new SerializedObject(owner);
            foreach (string name in names)
                Check(fields.FindProperty(name)?.objectReferenceValue != null, owner.GetType().Name + "." + name + " serialized binding");
        }
        private static void Weights(HostileActivityDirector activity, string family)
        {
            activity.OrdinaryWeight = family == "ordinary" ? 1 : 0;
            activity.ReconnaissanceWeight = family == "reconnaissance" ? 1 : 0;
            activity.PayloadWeight = family == "payload" ? 1 : 0;
            activity.WormWeight = family == "worm" ? 1 : 0;
            activity.PayloadLauncherWeight = family == "payload_launcher" ? 1 : 0;
            activity.DirectDuelWeight = family == "direct_duel" ? 1 : 0;
            activity.ConnectionDuelWeight = family == "connection_duel" ? 1 : 0;
        }
        [MenuItem("Tools/Connections/Run Canonical Scene Parity Witness")]
        public static void Run()
        {
            checks.Clear();
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                Configuration(scene);
                foreach (HackerDirective mode in new[] { HackerDirective.SPREAD, HackerDirective.BREACH })
                    foreach (float interval in new[] { 54f, 8f })
                        foreach (string family in Families) Activity(scene, mode, interval, family);
                KnowledgeAndEnding(scene);
            }
            Directory.CreateDirectory(Evidence);
            File.WriteAllLines(Evidence + "/parity-checks.txt", checks);
            Debug.Log("CanonicalSceneWitness PASS: " + checks.Count + " serialized/functional checks.");
        }
        private static void Configuration(string scene)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene);
            var authored = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var authoredDirective = authored.GetComponent<HackerDirectives>();
            Check(authored.AutoGenerationEnabled && authored.HostileGenerationEnabled && authored.RandomizeSeed &&
                new SerializedObject(authored).FindProperty("profiles").arraySize > 0 &&
                authoredDirective.Directive == HackerDirective.SPREAD && authoredDirective.CommitmentSeconds == 60 &&
                authoredDirective.PreferenceProbability == .7f, scene + " actual authored generation/catalog/directive defaults before witness overrides");
            var host = Open(scene, auto: true); bool small = scene == CanonicalSceneAuthoring.Scenes[0];
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            var activity = host.GetComponent<HostileActivityDirector>();
            Check(host.Nodes.Length == (small ? 4 : 8) && host.PhysicalLinks.Length == (small ? 3 : 7), scene + " authored topology counts");
            Check(host.IntegrityEnabled && IntegrityState.Maximum == 2 && host.Integrity.Nodes.Where(n => !n.External).All(n => n.Value == 2), scene + " two-point Integrity enabled");
            Check(host.Integrity.SevereLoadRatio == 1.5f && host.Integrity.WarningSeconds == 10, scene + " shared severe-load rule");
            Check(new SerializedObject(traffic).FindProperty("profiles").arraySize > 0 &&
                traffic.Profiles.Count(p => p.purposeTemplate && traffic.EffectiveAmbientWeight(p.id) > 0) == (small ? 6 : 7), scene + " explicit legal canonical catalog");
            Check(traffic.AutoGenerationEnabled && traffic.HostileGenerationEnabled && traffic.RandomizeSeed == false &&
                traffic.BenignPopulationGoal == (small ? 3 : 4) && traffic.HostileMeanIntervalSeconds == (small ? 54 : 30), scene + " generated scenario pressure/goal retained (seed fixed only by witness)");
            Check(new[] { activity.OrdinaryWeight, activity.ReconnaissanceWeight, activity.PayloadWeight, activity.WormWeight,
                activity.PayloadLauncherWeight, activity.DirectDuelWeight, activity.ConnectionDuelWeight }.All(w => w > 0) &&
                traffic.PayloadSpawnerWeight > 0 && traffic.BackdoorWeight > 0, scene + " every ambient activity/infection family enabled");
            var legal = host.Nodes.Where(n => traffic.IsInternalNode(n.View)).Select(n => n.View.NodeId).ToHashSet();
            var served = traffic.Profiles.Where(p => p.purposeTemplate && traffic.EffectiveAmbientWeight(p.id) > 0)
                .SelectMany(p => traffic.EffectiveTargetIds(p)).ToHashSet();
            Check(served.SetEquals(legal), scene + " every internal node has legitimate canonical destination cover");
            foreach (var profile in traffic.Profiles.Where(p => p.purposeTemplate))
                Check(traffic.EffectiveTargetIds(profile).All(legal.Contains) && profile.operations.All(o => o.meanWork > 0 && o.logs?.Length > 0),
                    scene + " valid PURPOSE/log/WORK content: " + profile.id);
            foreach (GameObject root in host.gameObject.scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, scene + " no missing script: " + child.name);
            var presenter = UnityEngine.Object.FindFirstObjectByType<IntegrityPresenter>();
            Bound(presenter, "host", "terminal", "duelOverlay", "cleanOverlay", "breachPanel", "breachPath");
            var presentation = new SerializedObject(presenter);
            foreach (string array in new[] { "nodes", "links" })
            {
                var bindings = presentation.FindProperty(array);
                Check(bindings.arraySize == (array == "nodes" ? host.Nodes.Length : host.PhysicalLinks.Length), scene + " full " + array + " binding array");
                foreach (string field in array == "nodes" ? new[] { "node", "background", "border", "accent", "marker" } : new[] { "link", "line", "label" })
                    for (int i = 0; i < bindings.arraySize; i++)
                        Check(bindings.GetArrayElementAtIndex(i).FindPropertyRelative(field).objectReferenceValue != null, scene + " " + array + i + "." + field);
            }
            Bound(host.GetComponent<HackerDirectives>(), "host");
            var status = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            Bound(status, "host", "duelDirector", "networkArea", "statusLayer", "statusPrefab", "stampLayer", "stampPrefab");
            Check(AssetDatabase.GetAssetPath(status.StatusPrefab) == IntegritySceneAuthoring.StatusPrefab, scene + " shared seven-row Integrity status prefab");
            Check(status.StatusPrefab.GetComponentsInChildren<NodeStatusPanel>(true).All(p =>
                new SerializedObject(p).FindProperty("labels").arraySize == 7 && new SerializedObject(p).FindProperty("timers").arraySize == 7),
                scene + " actual expanded status label/timer bindings");
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            Bound(terminal, "commandInput", "historyScroll", "historyContent", "inspectPrefab", "scanPrefab", "simulationHost", "trafficDirector", "cleanOverlay");
            InfectionWitness.CheckCleanOverlayBindings(); EscapeVideoCallWitness.CheckBindings();
            var strip = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>());
            Check(((TMP_Text)strip.FindProperty("clockText").objectReferenceValue).gameObject.activeInHierarchy &&
                !((TMP_Text)strip.FindProperty("valueText").objectReferenceValue).gameObject.activeInHierarchy &&
                !((TMP_Text)strip.FindProperty("rateText").objectReferenceValue).gameObject.activeInHierarchy &&
                !((Image)strip.FindProperty("healthFill").objectReferenceValue).transform.parent.gameObject.activeInHierarchy,
                scene + " clock/tools retained and Health survival gauge hidden");
        }
        private static void Activity(string scene, HackerDirective mode, float interval, string family)
        {
            var host = Open(scene, mode, interval: interval);
            var activity = host.GetComponent<HostileActivityDirector>(); Weights(activity, family);
            var produced = activity.QueueAmbientActivity(0);
            string label = scene + "/" + mode + "/" + interval + "/" + family;
            Check(produced.Activity == family && produced.Succeeded, label + " existing generator constructs selected family");
            if (produced.ConnectionPlan != null)
            {
                host.ValidatePlan(produced.ConnectionPlan);
                Check(produced.ConnectionPlan.Route.All(n => host.FindNodeResources(n.NodeId) != null), label + " legal real-node route");
            }
            var decision = host.Recorder.CurrentLog.events.Last(e => e.type == "hacker_target_decision").hackerTarget;
            Check(decision.directive == mode.ToString() && (mode != HackerDirective.BREACH ||
                host.GetComponent<HackerDirectives>().Snapshot.hasOffensive), label + " directive/intent active");
            Check(host.Integrity.Nodes.Where(n => !n.External).All(n => n.Value == 2), label + " preference/construction does not cause direct damage");
            if (family == "direct_duel")
            {
                Check(host.GetComponent<ConnectionsDuelDirector>().Current.LossPlan != null, label + " frozen Surprise consequence plan");
                UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include).Abort();
            }
        }
        private static void KnowledgeAndEnding(string scene)
        {
            var host = Open(scene); var db = host.FindNodeResources("DB");
            Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 3, out _) &&
                host.Integrity.For(db).Value == 1 && !host.Integrity.For(db).Revealed, scene + " accepted covert infection leaves hidden one-point damage");
            Check(host.StartScan("DB", "scan db", out _) == ScanStartResult.Started, scene + " async scan starts promptly");
            Check(!host.Integrity.For(db).Revealed, scene + " scan has not prematurely revealed condition");
            host.StepSimulation(host.ScanDurationSeconds);
            Check(host.Integrity.For(db).Revealed && host.IntegrityReceipt(db).Contains("1/2"), scene + " real scan completion reveals Integrity");
            host.StartClean("DB", "clean db", out var clean);
            Check(host.SelectCleanPid(clean.Processes.First(p => p.CpuPercent > 80).Pid, out _) == CleanSelectionResult.Success &&
                host.Infections.Count == 0 && host.Integrity.For(db).Value == 1, scene + " PID CLEAN removes infection without repairing Integrity");
            Check(host.TryActivateProbe(host.PhysicalLinks[0].View.LinkId, "probe", out _), scene + " PROBE operates");
            Check(host.Payloads.TryIntercept(null, "DB", out _) && host.Worms.TryIsolate("AUTH", "isolate auth", out _), scene + " interception/isolation operate");
            host.ResetSimulation();
            var selector = new HackerDirectiveSelector(host.Integrity, host.PhysicalLinks, HackerDirective.BREACH, 1701);
            selector.EnsureOffensive(0); string[] route = selector.Offensive.path;
            foreach (string id in route.Where(id => !host.Integrity.For(host.FindNodeResources(id)).External))
                for (int i = 0; i < 2; i++) host.DamageIntegrity(host.FindNodeResources(id), "parity_fixture", "path");
            Check(host.RunEnded && host.Integrity.Breach.path.Length >= 2 && host.Integrity.Breach.criticalNode == route.Last(), scene + " actual topology establishes CORE breach");
            float frozen = host.SimulationTimeSeconds; host.StepSimulation(50);
            Check(host.SimulationTimeSeconds == frozen && host.Recorder.CurrentLog.events.Count(e => e.type == "core_breached") == 1, scene + " exact-once loss freezes simulation");
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            typeof(Terminal).GetMethod("HandleSubmit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(terminal, new object[] { "restart" });
            Check(!host.RunEnded && host.Integrity.Nodes.Where(n => !n.External).All(n => n.Value == 2 && !n.Revealed) &&
                !host.GetComponent<HackerDirectives>().Snapshot.hasOffensive, scene + " real terminal restart clears loss/knowledge/offensive");
        }
        public static void Natural()
        {
            Directory.CreateDirectory(Evidence);
            var results = new List<RunResult>(); var replays = new List<string>();
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
                foreach (HackerDirective mode in new[] { HackerDirective.SPREAD, HackerDirective.BREACH })
                    foreach (bool high in new[] { false, true })
                        foreach (int seed in new[] { 1701, 3141 })
                        {
                            string difficulty = high ? "high8s" : "authored";
                            string folder = Evidence + "/" + Path.GetFileNameWithoutExtension(scene) + "/" + mode + "/" + difficulty;
                            string Observe(bool save, out IntegrityPressureWitness.Result result) =>
                                IntegrityPressureWitness.Observe(seed, "unattended", save, out result, host =>
                                {
                                    host.GetComponent<HackerDirectives>().ConfigureForDevelopment(mode);
                                    if (high) IntegrityWitness.Set(host.GetComponent<ConnectionTrafficDirector>(), "hostileMeanIntervalSeconds", 8f);
                                }, folder, scene);
                            string signature = Observe(true, out var result);
                            results.Add(new RunResult { scene = scene, directive = mode.ToString(), difficulty = difficulty, result = result });
                            if (!high && seed == 1701)
                            {
                                if (signature != Observe(false, out _)) throw new InvalidOperationException("Canonical natural replay differs: " + scene + "/" + mode);
                                replays.Add("PASS exact seeded event/snapshot/graph replay " + scene + "/" + mode + "/authored/1701");
                            }
                        }
            File.WriteAllText(Evidence + "/natural-summary.json", JsonUtility.ToJson(new Results { runs = results.ToArray() }, true));
            File.WriteAllLines(Evidence + "/replay-checks.txt", replays);
            Debug.Log("CanonicalSceneWitness PASS: 16 natural runs and four exact seeded replays.");
        }
        public static void Regressions()
        {
            var reports = new List<string>();
            var cases = new (string name, Action run)[]
            {
                ("Traffic", TrafficWitness.Run), ("AmbientHostileActivity", AmbientHostileActivityWitness.Run),
                ("NetworkHealth", NetworkHealthWitness.Run), ("Duel", DuelWitness.Run),
                ("Consequence", ConsequenceWitness.Run), ("AdvancedScene", AdvancedSceneWitness.Run),
                ("Recorder", RecorderWitness.Run), ("Payload", PayloadWitness.Run),
                ("Worm", WormWitness.Run), ("AmbientPayloadThreat", AmbientPayloadThreatWitness.Run),
                ("Scan", ScanWitness.Run), ("Infection", InfectionWitness.Run),
                ("Overclock", OverclockWitness.Run), ("NodeStatus", NodeStatusWitness.Run),
                ("Geometry", GeometryWitness.Run), ("Resource", ResourceWitness.Run),
                ("Integrity", IntegrityWitness.Run), ("HackerDirective", HackerDirectiveWitness.Run),
                ("Storefront", StorefrontTrafficWitness.Run), ("MaximumPressure", MaximumPressureWitness.Run)
            };
            foreach (var check in cases)
            {
                try { check.run(); reports.Add("PASS " + check.name); }
                catch (Exception error) { reports.Add("FAIL " + check.name + ": " + error.Message); Debug.LogException(error); }
            }
            Directory.CreateDirectory(Evidence);
            File.WriteAllLines(Evidence + "/regression-checks.txt", reports);
            foreach (string report in reports) Debug.Log("CANONICAL REGRESSION " + report);
            if (reports.Any(r => r.StartsWith("FAIL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Canonical regression failures; see report, including inherited/obsolete scene assumptions.");
        }
    }
}
