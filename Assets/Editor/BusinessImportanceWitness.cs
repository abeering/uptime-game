using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Uptime.Connections.Display;

namespace Uptime.Connections.EditorTools
{
    public static class BusinessImportanceWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsBusinessImportanceV0";
        private static readonly List<string> checks = new();
        private static void Check(bool ok, string label) { checks.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); }
        private static void Near(double a, double b, string label) => Check(Math.Abs(a - b) < .001, label + $": expected {b}, observed {a}");
        public static ConnectionSpawnPlan Plan(SimulationHost h, string id, bool important, OperationStage[] stages = null) =>
            new ConnectionSpawnPlan(0, id, BusinessShiftWitness.Route(h), "product-lookup", "Product Detail Lookups",
                stages ?? new[] { new OperationStage(OperationKind.Read, 40, 4, "Importance review") },
                importance: important ? BusinessImportance.Important : BusinessImportance.Ordinary);
        public static LogicalConnectionState Add(SimulationHost h, string id, bool important, OperationStage[] stages = null) =>
            h.RegisterConnection(LogicalConnectionDefinition.FromPlan(Plan(h, id, important, stages)));

        [MenuItem("Tools/Connections/Load Business Importance Comparison")]
        public static void LoadComparison()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter a canonical gameplay scene in Play Mode first.");
            ConfigureComparison(UnityEngine.Object.FindFirstObjectByType<SimulationHost>());
        }
        public static void ConfigureComparison(SimulationHost h)
        {
            var t = h.GetComponent<ConnectionTrafficDirector>(); t.AutoGenerationEnabled = t.HostileGenerationEnabled = false;
            BusinessShiftWitness.Set(h, "shiftWorkTarget", 100000f); BusinessShiftWitness.Set(h, "businessFailureLimit", 100);
            h.SetRunMode(ConnectionRunMode.Generated); h.TrySetCachePurpose("product-lookup", out _);
            var mixed = new[] { new OperationStage(OperationKind.Read, 16, 4, "Cached READ"), new OperationStage(OperationKind.Write, 80, 4, "Ordinary WRITE") };
            Add(h, "o0", false, mixed); Add(h, "i0", true, mixed);
            var load = new[] { new OperationStage(OperationKind.Write, 480, 8, "DB competition") };
            Add(h, "o1", false, load); Add(h, "i1", true, load);
        }
        [MenuItem("Tools/Connections/Run Business Importance Witness")]
        public static void Run()
        {
            checks.Clear(); Directory.CreateDirectory(Evidence);
            try
            {
                foreach (string scene in CanonicalSceneAuthoring.Scenes)
                {
                    var h = BusinessShiftWitness.Open(scene); var t = h.GetComponent<ConnectionTrafficDirector>();
                    Check(t.ImportantConnectionChance == .15f && h.ImportantFailureMultiplier == 3 && t.LegitimateWorkloadMultiplier == 2.5f, scene + " authored importance defaults and preserved workload");
                    var o = Add(h, "o0", false); var a = Add(h, "i0", true);
                    Check(h.KillConnection("o0") && h.BusinessFailures.Count == 1 && h.BusinessFailureUnits == 1, "ordinary kill one receipt / one unit");
                    Check(h.KillConnection("i0") && h.BusinessFailures.Count == 2 && h.LegitimateFailureCount == 2 && h.BusinessFailureUnits == 4, "IMPORTANT kill one receipt / three units; job count separate");
                    Check(!h.KillConnection("i0") && h.BusinessFailures.Last().Units == 3 && h.BusinessFailures.Last().CumulativeUnits == 4 && h.BusinessFailures.Last().CumulativeTotal == 2, "exact-once weighted receipt");
                    h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds); Check(h.Recorder.Save(false), "save real weighted-failure run");
                    var saved = JsonUtility.FromJson<ConnectionsRunLog>(File.ReadAllText(h.Recorder.OutputPath));
                    Check(saved.run.importantConnectionChance == .15f && saved.run.importantFailureMultiplier == 3 && saved.events.Count(e => e.type == "business_failure") == 2 &&
                        saved.events.Last(e => e.type == "business_failure").businessFailureTotalUnits == 4 && saved.events.Last(e => e.type == "business_failure").importance == "Important" &&
                        saved.snapshots.Last().businessFailureTotalUnits == 4 && saved.snapshots.Last().legitimateFailureTotal == 2, "disk JSON distinguishes designation/configured cost/incurred units/jobs");
                    File.WriteAllText(Evidence + "/" + Path.GetFileNameWithoutExtension(scene) + "-failures.json", JsonUtility.ToJson(new ConnectionsRunLog {
                        run = saved.run, events = saved.events.Where(e => e.type == "business_failure" || e.type == "connection_resolved").ToList(), snapshots = new() { saved.snapshots.Last() } }, true));
                    h.ResetSimulation(); Check(h.BusinessFailureUnits == 0 && h.LegitimateFailureCount == 0 && h.CreditedWork == 0, "reset clears sole receipt ledger and objective totals");
                    h = BusinessShiftWitness.Open(scene, limit: 3); Add(h, "o0", false); Add(h, "i0", true); h.KillConnection("o0"); h.KillConnection("i0");
                    Check(h.Ending == RunEnding.Fired && h.BusinessFailureUnits == 4 && h.LegitimateFailureCount == 2 && h.Shift.failures == 4 && h.Shift.failedJobs == 2, "cross allowance with only two remaining; no clipping or extra job receipts");
                    h = BusinessShiftWitness.Open(scene); BusinessShiftWitness.Set(h, "importantFailureMultiplier", 5); a = Add(h, "i0", true);
                    BusinessShiftWitness.Set(h, "importantFailureMultiplier", 7); h.KillConnection("i0");
                    Check(a.BusinessFailureCost == 5 && h.BusinessFailureUnits == 5, "configurable failure cost frozen at admission");
                    h = BusinessShiftWitness.Open(scene); o = Add(h, "o0", false); a = Add(h, "i0", true); h.StepSimulation(4);
                    Check(o.IsActive && a.IsActive && o.DesiredLoad == a.DesiredLoad && o.CurrentTotalWork == a.CurrentTotalWork && o.AllocatedLoad == a.AllocatedLoad && o.ActualWorkRate == a.ActualWorkRate &&
                        o.ServiceBudgetSeconds == a.ServiceBudgetSeconds && o.ServiceDeadlineSeconds == a.ServiceDeadlineSeconds && o.AllocationWeight == a.AllocationWeight && o.Route.Select(n => n.Node).SequenceEqual(a.Route.Select(n => n.Node)), "importance has no demand/work/rate/deadline/allocation/route effect");
                    var palette = h.PresentationPalette; var display = UnityEngine.Object.FindFirstObjectByType<ConnectionGameplayDisplay>();
                    var color = palette.Assign(a); Check(color == display.style.IdentityColor(DisplayTrafficClass.Important, palette.AssignSlot(a)) && palette.Assign(o) == display.style.IdentityColor(DisplayTrafficClass.Ordinary, palette.AssignSlot(o)), "terminal palette delegates to same shared style/family/slot as network");
                    RouteTransitionWitness.Until(h, () => a.IsComplete && o.IsComplete);
                    Near(h.CreditedWork, 80, "successful IMPORTANT earns identical unscaled business WORK");
                    Check(h.WorkCreditCount == 2 && h.BusinessFailureUnits == 0, "two whole jobs credit exactly once without bonus");
                    palette.Release(a); Check(palette.TryGet(a, out var historical) && historical == color, "resolved/history identity survives live slot release");
                    h = BusinessShiftWitness.Open(scene); h.TrySetCachePurpose("product-lookup", out _); h.TrySetApiPurpose("product-lookup", out _);
                    var plan = Plan(h, "i0", true, new[] { new OperationStage(OperationKind.Read, 4, 4, "Cached"), new OperationStage(OperationKind.Write, 40, 4, "DB") });
                    var authored = JsonUtility.FromJson<LogicalConnectionDefinition>(JsonUtility.ToJson(LogicalConnectionDefinition.FromPlan(plan)));
                    Check(authored.Importance == BusinessImportance.Important, "authored serialized importance field persists");
                    var copy = Scale(plan.ForEvent("company-fixture"), 2.5f).WithAmbientIdentity(ConnectionIdentityMode.Normal, 0, 123, .1f);
                    Check(copy.Importance == BusinessImportance.Important && copy.BusinessWorkValue == plan.BusinessWorkValue && copy.Operations[1].TotalWork == 100, "copy/event/workload/identity retain authored importance and value");
                    a = h.RegisterConnection(LogicalConnectionDefinition.FromPlan(copy)); var known = a.KnownIdentity; color = h.PresentationPalette.Assign(a);
                    RouteTransitionWitness.Until(h, () => a.IsRerouting);
                    Check(a.IsImportant && a.BusinessFailureCost == 3 && !a.IsDemanding && a.CompletedWork == 0 && a.TargetNode.NodeId == "DB" && a.OperationDestinationAt(0).NodeId == "CACHE" && h.PresentationPalette.Assign(a) == color && a.KnownIdentity == known, "importance/color persist through identity copying, CACHE READ and real WRITE rerouting");
                    var deadline = a.ServiceDeadlineSeconds; RouteTransitionWitness.Until(h, () => a.IsActive);
                    Check(a.IsImportant && a.AllocationWeight == 2 && a.ServiceDeadlineSeconds == deadline, "rerouting resumes with independent live API priority and original deadline");
                    h.KillConnection(a.KnownIdentity); Check(h.BusinessFailureUnits == 3 && h.CreditedWork == 0 && h.WorkCreditCount == 0, "mixed unfinished important arc one failure, no partial business WORK");
                    h = BusinessShiftWitness.Open(scene); BusinessShiftWitness.Set(h.FindNodeResources("DB"), "baselineLoad", h.FindNodeResources("DB").LoadCapacity); a = Add(h, "i0", true);
                    RouteTransitionWitness.Until(h, () => a.IsComplete); Check(a.Resolution == ConnectionResolution.TimedOut && h.BusinessFailureUnits == 3 && h.LegitimateFailureCount == 1, "IMPORTANT timeout uses same service budget and one weighted failure");
                    h = BusinessShiftWitness.Open(scene); a = Add(h, "i0", true); RouteTransitionWitness.Until(h, () => a.IsActive); h.TryInjectInfection("DB", InfectionKind.Blackout, 0, out _);
                    Check(a.Resolution == ConnectionResolution.Canceled && h.BusinessFailureUnits == 3, "BLACKOUT uses existing interruption and weighted accounting");
                    Generation(scene);
                }
            }
            finally { File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks); }
            Debug.Log("BusinessImportanceWitness PASS: " + checks.Count + " assertions.");
        }
        private static ConnectionSpawnPlan Scale(ConnectionSpawnPlan p, float factor) => (ConnectionSpawnPlan)typeof(ConnectionSpawnPlan)
            .GetMethod("WithLegitimateWorkload", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(p, new object[] { factor });
        private static string Recipe(ConnectionSpawnPlan p) => p.ConnectionId + ":" + p.ProfileId + ":" + p.BusinessWorkValue + ":" + p.NaturalLoadSeed + ":" + p.IdentitySeed + ":" + string.Join(",", p.Route.Select(n => n.NodeId)) + ":" + JsonUtility.ToJson(p.GenerationTrace) + ":" + string.Join("|", p.Operations.Select(s => JsonUtility.ToJson(s)));
        private static void Generation(string scene)
        {
            List<string> baseline = null; List<string> replay = null;
            foreach (float chance in new[] { 0f, .15f, .15f, 1f })
            {
                var h = BusinessShiftWitness.Open(scene); var t = h.GetComponent<ConnectionTrafficDirector>(); BusinessShiftWitness.Set(t, "importantConnectionChance", chance); t.Seed = 1701; t.ResetDirector();
                var recipes = new List<string>(); var assignments = new List<string>(); int important = 0;
                for (int i = 0; i < 24; i++)
                {
                    var p = t.GenerateBenignPlan(i); recipes.Add(Recipe(p)); assignments.Add(p.Importance + ":" + p.ImportanceSelectionRoll);
                    Check(!p.IsHostile && (p.Importance == BusinessImportance.Important) == (p.ImportanceSelectionRoll < chance) && p.ImportanceSelectionChance == chance &&
                        Scale(p, 1.5f).ForEvent("copy").ImportanceSelectionRoll == p.ImportanceSelectionRoll, "seeded assignment threshold and copies / " + chance + " / " + i);
                    if (p.Importance == BusinessImportance.Important) important++;
                    if (i % 3 == 0) { var hostile = t.GenerateHostilePlan(i); recipes.Add(Recipe(hostile)); Check(hostile.IsHostile && hostile.Importance == BusinessImportance.Ordinary && hostile.ImportanceSelectionRoll == -1, "hostile never assigned importance or draws importance RNG"); }
                    if (i == 0 && chance == 1) { t.QueueSpawnPlan(p); Check(h.Recorder.CurrentLog.events.Last(e => e.type == "plan_queued").definition.importance == "Important", "assignment recorded at concrete-plan queue time"); }
                }
                if (baseline == null) baseline = recipes; else Check(baseline.SequenceEqual(recipes), "importance chance leaves concrete benign/hostile recipes and existing random streams unchanged");
                if (chance == .15f) { Check(important > 0 && important < 24, "provisional 15% produces both classes in fixed sample"); if (replay == null) replay = assignments; else Check(replay.SequenceEqual(assignments), "reset deterministic importance replay"); }
            }
            bool rejected = false; try { new ConnectionSpawnPlan(0, "hostile", BusinessShiftWitness.Route(UnityEngine.Object.FindFirstObjectByType<SimulationHost>()), "test", "test", new[] { new OperationStage(OperationKind.Read, 4, 1, "test") }, isHostile: true, importance: BusinessImportance.Important); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "explicit hostile IMPORTANT designation rejected");
        }
        public static void PrepareAndRegressions() { ConnectionDisplayLabAuthoring.Build(); Regressions(); }
        public static void Regressions()
        {
            Run(); RecorderPolicyEvidenceWitness.Regressions(); ConnectionDisplayLabWitness.Run(); OperationRoutingCacheWitness.Regressions();
        }
    }
}
