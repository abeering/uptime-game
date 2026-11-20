using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class HackerDirectiveWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsHackerDirectivesV0";
        private static readonly List<string> results = new List<string>();
        public static void ConfigureScene()
        {
            EditorSceneManager.OpenScene(IntegritySceneAuthoring.Scene);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var component = host.GetComponent<HackerDirectives>() ?? host.gameObject.AddComponent<HackerDirectives>();
            var fields = new SerializedObject(component);
            fields.FindProperty("host").objectReferenceValue = host;
            fields.FindProperty("directive").enumValueIndex = (int)HackerDirective.SPREAD;
            fields.FindProperty("commitmentSeconds").floatValue = 60;
            fields.FindProperty("preferenceProbability").floatValue = .7f;
            fields.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
            EditorSceneManager.SaveScene(host.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
        public static void ConfigureAndRun() { ConfigureScene(); Run(); }
        [MenuItem("Tools/Connections/Run Hacker Directive Witness")]
        public static void Run()
        {
            results.Clear();
            ModelCases();
            RecipeAndRouteCases();
            ActivitiesAndRecon();
            ClockAndCategoryCases();
            SceneIsolation();
            Directory.CreateDirectory(Evidence);
            File.WriteAllLines(Evidence + "/deterministic-checks.txt", results);
            Debug.Log("HackerDirectiveWitness PASS: " + results.Count + " assertions.");
        }
        private sealed class World : IDisposable
        {
            private readonly GameObject root = new GameObject("Directive numerical witness");
            public readonly NodeResources[] Nodes;
            public readonly PhysicalLinkReference[] Links;
            public readonly IntegrityState Integrity;
            public World(bool cycle = false, bool disconnected = false)
            {
                string[] ids = { "outside-a", "outside-b", "service-a", "service-b", "core-a", "core-b" };
                Nodes = ids.Select(id =>
                {
                    var obj = new GameObject(id, typeof(RectTransform), typeof(NodeView)); obj.transform.SetParent(root.transform);
                    var view = obj.GetComponent<NodeView>(); IntegrityWitness.Set(view, "nodeId", id);
                    return new NodeResources(view, 10);
                }).ToArray();
                var edges = new List<(string a, string b)>();
                if (!disconnected) edges.AddRange(new[] { ("outside-a", "service-a"), ("service-a", "core-a"),
                    ("outside-b", "service-b"), ("service-b", "core-b") });
                if (cycle) edges.AddRange(new[] { ("service-a", "service-b"), ("outside-a", "outside-b") });
                Links = edges.Select((edge, i) =>
                {
                    var obj = new GameObject("link" + i, typeof(RectTransform), typeof(PhysicalLinkView)); obj.transform.SetParent(root.transform);
                    var view = obj.GetComponent<PhysicalLinkView>();
                    IntegrityWitness.Set(view, "linkId", "link" + i);
                    IntegrityWitness.Set(view, "nodeA", Node(edge.a).View); IntegrityWitness.Set(view, "nodeB", Node(edge.b).View);
                    return new PhysicalLinkReference(view);
                }).ToArray();
                Integrity = new IntegrityState(Nodes, Links, n => n.View.NodeId.StartsWith("outside", StringComparison.Ordinal),
                    new IntegrityTuning { enabled = true, criticalNodeIds = new[] { "core-a", "core-b" } });
            }
            public NodeResources Node(string id) => Nodes.First(n => n.View.NodeId == id);
            public void Hit(string id, int count = 1) { for (int i = 0; i < count; i++) Integrity.Damage(Node(id), "witness", id, 0, false); }
            public NodeView[] Legal => Nodes.Where(n => !n.View.NodeId.StartsWith("outside", StringComparison.Ordinal)).Select(n => n.View).ToArray();
            public HackerDirectiveSelector Selector(int seed = 1701, float preference = 1) =>
                new HackerDirectiveSelector(Integrity, Links, HackerDirective.BREACH, seed, 60, preference);
            public void Dispose() => UnityEngine.Object.DestroyImmediate(root);
        }
        private static void ModelCases()
        {
            using (var world = new World())
            {
                world.Hit("service-b"); world.Hit("core-b");
                var selector = world.Selector(); selector.EnsureOffensive(0);
                Check(selector.Offensive.critical == "core-b" && selector.Offensive.remainingCostAtSelection == 2 &&
                    selector.Offensive.path.SequenceEqual(new[] { "outside-b", "service-b", "core-b" }),
                    "remaining damage: expected outside-b/service-b/core-b cost2 versus other cost4; observed exact");
                Check(!world.Integrity.For(world.Node("service-b")).Revealed, "offensive uses actual hidden damage without revealing it");
                world.Hit("service-a"); world.Hit("core-a", 2);
                selector.EnsureOffensive(59.99f);
                Check(selector.Offensive.id == 1 && selector.Offensive.critical == "core-b", "commitment retains cost2 objective despite alternative cost1 before60s");
                selector.EnsureOffensive(60);
                Check(selector.Offensive.id == 2 && selector.Offensive.critical == "core-a" && selector.Offensive.remainingCostAtSelection == 1,
                    "at60s reevaluate; expected core-a cost1, observed exact");
                var pick = selector.Choose("ordinary", 60, world.Node("core-b").View, world.Legal);
                Check(pick.finalTarget == "service-a" && !pick.candidates.Contains("core-a"), "preferred targets exclude already compromised core");
                world.Hit("service-a"); selector.EnsureOffensive(61);
                Check(selector.Offensive.reason == "objective_complete" && selector.Offensive.selectedAt == 61,
                    "completed offensive reevaluates before commitment expires");
            }
            using (var world = new World())
            {
                world.Hit("service-a"); var selector = world.Selector(); selector.EnsureOffensive(0);
                world.Links[1] = null; selector.EnsureOffensive(1);
                Check(selector.Offensive.reason == "objective_invalid" && selector.Offensive.critical == "core-b", "removed physical edge invalidates objective and selects reachable alternative");
            }
            using (var world = new World(cycle: true))
            {
                world.Hit("service-a", 2); world.Hit("service-b", 2); world.Hit("core-a");
                var selector = world.Selector(); selector.EnsureOffensive(0);
                Check(selector.Offensive.critical == "core-a" && selector.Offensive.remainingCostAtSelection == 1 &&
                    selector.Offensive.path.Distinct().Count() == selector.Offensive.path.Length,
                    "multiple roots/criticals and zero-cost cycle terminate with valid cost1 simple path");
            }
            using (var world = new World(disconnected: true))
            {
                var selector = world.Selector();
                var pick = selector.Choose("ordinary", 0, world.Node("service-a").View, world.Legal);
                Check(!pick.eligible && !pick.influenced && pick.reason == "no_reachable_critical", "disconnected topology falls back without creating an attack or damage");
            }
            using (var world = new World())
            {
                var selected = new HashSet<string>();
                for (int seed = 1; seed <= 32; seed++)
                {
                    var a = world.Selector(seed, .7f); var b = world.Selector(seed, .7f);
                    for (int i = 0; i < 10; i++)
                        Check(JsonUtility.ToJson(a.Choose("payload", i, world.Node("core-a").View, world.Legal)) ==
                            JsonUtility.ToJson(b.Choose("payload", i, world.Node("core-a").View, world.Legal)), "seeded reset replay seed=" + seed + " choice=" + i);
                    selected.Add(a.Offensive.critical);
                }
                Check(selected.Count == 2, "seeded tie-breaking represents both equal-cost critical objectives");
                var control = new HackerDirectiveSelector(world.Integrity, world.Links, HackerDirective.SPREAD, 12);
                Check(control.Choose("ordinary", 0, world.Node("core-a").View, world.Legal).finalTarget == "core-a" && control.Offensive == null,
                    "SPREAD returns ambient target without constructing an offensive");
                var zero = world.Selector(preference: 0);
                Check(!zero.Choose("ordinary", 0, world.Node("core-a").View, world.Legal).preferred, "preference0 preserves normal ambient branch");
                Check(world.Selector().Choose("worm", 0, null, null, "propagating_worm").reason == "propagating_worm", "propagation is not given an artificial target");
                world.Hit("service-a");
                var bias = world.Selector(); int one = 0, two = 0;
                for (int i = 0; i < 4096; i++)
                {
                    string id = bias.Choose("ordinary", 0, world.Node("core-b").View, world.Legal).finalTarget;
                    if (id == "service-a") one++; else if (id == "core-a") two++;
                }
                Check(one > two * 1.6f && one < two * 2.4f && two > 0,
                    "inverse-Integrity weighting expects roughly2:1 without guarantee; observed " + one + ":" + two);
            }
        }
        public static SimulationHost Open(HackerDirective mode, int seed = 1701, float preference = .7f) =>
            IntegrityWitness.Open(false, seed, host => host.GetComponent<HackerDirectives>().ConfigureForDevelopment(mode, 60, preference));
        private static string Recipe(ConnectionSpawnPlan plan) => JsonUtility.ToJson(plan.GenerationTrace) +
            string.Join("/", plan.Operations.Select(op => op.Operation + ":" + op.TotalWork.ToString("R") + ":" + op.DesiredLoad.ToString("R") + ":" + op.EffectOnCompletion));
        private static void RecipeAndRouteCases()
        {
            foreach (float delivery in new[] { 0f, 1f })
            {
                var host = Open(HackerDirective.BREACH); var traffic = host.GetComponent<ConnectionTrafficDirector>();
                IntegrityWitness.Set(traffic, "deliveryChance", delivery);
                var original = traffic.GenerateHostilePlan(0); string recipe = Recipe(original);
                var redirected = traffic.RetargetHostilePlan(original, "WEB");
                Check(redirected.Target.NodeId == "WEB" && redirected.Origin == original.Origin &&
                    redirected.ConnectionId == original.ConnectionId && Recipe(redirected) == recipe &&
                    redirected.NaturalLoadSeed == original.NaturalLoadSeed && redirected.IdentityMode == original.IdentityMode,
                    "targeted hostile recipe retains sampled objective/payload/WORK/LOAD/noise/identity deliveryChance=" + delivery);
                host.ValidatePlan(redirected);
                Check(host.PhysicalLinks.Any(e => e.View.NodeA == redirected.Route[0] && e.View.NodeB == redirected.Route[1] ||
                    e.View.NodeB == redirected.Route[0] && e.View.NodeA == redirected.Route[1]), "redirected route uses actual legal physical adjacency");
                var next = Recipe(traffic.GenerateHostilePlan(0));
                host = Open(HackerDirective.SPREAD); traffic = host.GetComponent<ConnectionTrafficDirector>();
                IntegrityWitness.Set(traffic, "deliveryChance", delivery); traffic.GenerateHostilePlan(0);
                Check(Recipe(traffic.GenerateHostilePlan(0)) == next, "retarget consumes no existing recipe random draws");
                var launcher = traffic.GeneratePayloadLauncherPlan(0);
                Check(traffic.EligibleDirectiveTargets(launcher).Length == 0, "launcher retains fixed destination semantics");
                bool rejected = false; try { traffic.RetargetHostilePlan(launcher, "WEB"); } catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "fixed launcher cannot be silently redirected");
                var benign = traffic.QueueProfile("product-lookup");
                Check(!benign.IsHostile && !traffic.EligibleDirectiveTargets(benign).Any() && benign.Target.NodeId == "DB", "canonical benign recipe remains fixed at its legitimate destination");
                rejected = false; try { traffic.RetargetHostilePlan(original, "EXT1"); } catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "external ingress is not a legal direct damage target");
            }
        }
        private static void Only(HostileActivityDirector activity, string name)
        {
            activity.OrdinaryWeight = name == "ordinary" ? 1 : 0; activity.ReconnaissanceWeight = name == "reconnaissance" ? 1 : 0;
            activity.PayloadWeight = name == "payload" ? 1 : 0; activity.WormWeight = name == "worm" ? 1 : 0;
            activity.PayloadLauncherWeight = name == "payload_launcher" ? 1 : 0; activity.DirectDuelWeight = name == "direct_duel" ? 1 : 0;
            activity.ConnectionDuelWeight = name == "connection_duel" ? 1 : 0;
        }
        private static void ActivitiesAndRecon()
        {
            foreach (string kind in new[] { "ordinary", "reconnaissance", "payload", "worm", "payload_launcher", "direct_duel", "connection_duel" })
            {
                var host = Open(HackerDirective.BREACH, preference: 1); var activity = host.GetComponent<HostileActivityDirector>(); Only(activity, kind);
                var result = activity.QueueAmbientActivity(0);
                var decision = host.Recorder.CurrentLog.events.Last(e => e.type == "hacker_target_decision").hackerTarget;
                Check(result.Activity == kind && result.Succeeded, "existing activity category generates normally: " + kind);
                if (kind == "worm" || kind == "payload_launcher")
                    Check(!decision.eligible && !decision.influenced && decision.reason == (kind == "worm" ? "propagating_worm" : "fixed_launcher_destination"), "explicit unsupported activity fallback: " + kind);
                else Check(decision.eligible && decision.preferred && decision.candidates.Contains(result.TargetNodeId), "eligible offensive target applied through existing generator: " + kind);
                Check(!host.RunEnded && host.Integrity.Nodes.Where(n => !n.External).All(n => n.Value == 2), "target selection causes no direct damage: " + kind);
                if (kind == "direct_duel") UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include).Abort();
            }
            {
                var host = Open(HackerDirective.BREACH, preference: 1); var activity = host.GetComponent<HostileActivityDirector>();
                Only(activity, "reconnaissance"); activity.FollowupChance = 1;
                var scan = activity.QueueAmbientActivity(0).ConnectionPlan;
                string target = scan.Target.NodeId;
                host.GetComponent<HackerDirectives>().ConfigureForDevelopment(HackerDirective.SPREAD);
                host.StepSimulation(30);
                var followup = host.Recorder.CurrentLog.events.First(e => e.type == "recon_followup_queued");
                Check(followup.targetNodeId == target && host.FindConnection(followup.followupConnectionId)?.TargetNode.NodeId == target,
                    "delayed reconnaissance follow-up keeps original selected target across directive switch");
            }
        }
        private static void ClockAndCategoryCases()
        {
            string Signature(bool enabled, HackerDirective mode)
            {
                var host = Open(mode);
                host.GetComponent<HackerDirectives>().enabled = enabled;
                var traffic = host.GetComponent<ConnectionTrafficDirector>(); traffic.AutoGenerationEnabled = true;
                // Controlled clock/generator comparison: don't advance WORK or damage. Natural runs below test closed-loop outcomes.
                for (int i = 0; i <= 600; i++) traffic.Tick(i, true);
                return string.Join("\n", host.Recorder.CurrentLog.events.Where(e => e.type == "hostile_activity_selected" || e.type == "hostile_opportunity")
                    .Select(e => e.type + ":" + e.scheduledSpawnTime.ToString("R") + ":" + e.hostileActivity + ":" + e.hostileActivityRoll.ToString("R") + ":" + e.nextHostileAtSeconds.ToString("R")));
            }
            string baseline = Signature(false, HackerDirective.SPREAD);
            Check(baseline == Signature(true, HackerDirective.SPREAD), "SPREAD enabled/disabled retains identical opportunity timing, category and category roll");
            Check(baseline == Signature(true, HackerDirective.BREACH), "BREACH target RNG does not advance opportunity/category streams");
        }
        private static void SceneIsolation()
        {
            foreach (string scene in new[] { "Connections", "ConnectionsAdvanced", "ConnectionsAdvancedPressure" })
            {
                EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
                Check((UnityEngine.Object.FindFirstObjectByType<HackerDirectives>() != null) == (scene != "ConnectionsAdvancedPressure"),
                    scene + " canonical directive availability / historical Pressure opt-out");
            }
        }
        private static void Check(bool value, string label)
        { if (!value) throw new InvalidOperationException("HackerDirectiveWitness FAIL: " + label); results.Add("PASS expected=observed: " + label); }
    }
}
