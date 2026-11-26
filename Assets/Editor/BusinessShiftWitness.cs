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
    public static class BusinessShiftWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsBusinessReliabilityShiftV0";
        private static readonly List<string> checks = new();
        public static void Set(object value, string field, object data) => value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, data);
        private static void Check(bool condition, string label)
        { checks.Add((condition ? "PASS " : "FAIL ") + label); if (!condition) throw new Exception("Business shift: " + label); }
        private static void Near(double actual, double expected, string label) => Check(Math.Abs(actual - expected) < .001, label + ": expected " + expected + ", observed " + actual);

        // Adapted from #124's existing strip/result authoring, with no runtime hierarchy construction.
        public static void Author()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                EditorSceneManager.OpenScene(scene);
                var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                Set(host, "shiftEnabled", true); Set(host, "shiftWorkTarget", scene == CanonicalSceneAuthoring.Scenes[0] ? 1000f : 1500f); Set(host, "businessFailureLimit", 10);
                var panel = UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>(); var fields = new SerializedObject(panel);
                TMP_Text Text(string name) => (TMP_Text)fields.FindProperty(name).objectReferenceValue;
                void Place(TMP_Text text, float x, float width, int font = 22)
                {
                    text.gameObject.SetActive(true); text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0, 1);
                    text.alignment = TextAlignmentOptions.MidlineLeft; text.rectTransform.pivot = new Vector2(0, .5f);
                    text.rectTransform.anchoredPosition = new Vector2(x, -25); text.rectTransform.sizeDelta = new Vector2(width, 40);
                    text.fontSize = font; text.enableAutoSizing = false; text.textWrappingMode = TextWrappingModes.NoWrap;
                    foreach (var obj in new UnityEngine.Object[] { text, text.rectTransform, text.gameObject }) PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                }
                Place(Text("clockText"), 24, 200);
                Text("clockText").enableAutoSizing = true; Text("clockText").fontSizeMin = 14; Text("clockText").fontSizeMax = 22; Text("clockText").text = "UPTIME 00:00";
                Place(Text("valueText"), 240, 230); Text("valueText").text = $"WORK 0 / {host.ShiftWorkTarget:0}";
                Place(Text("businessFailureText"), 635, 220); Text("businessFailureText").text = "FAILURES 0 / 10";
                Place(Text("rateText"), 875, 230, 20); Text("rateText").text = "";
                var fill = (Image)fields.FindProperty("healthFill").objectReferenceValue;
                var bar = (RectTransform)fill.transform.parent; bar.gameObject.SetActive(true);
                bar.anchorMin = bar.anchorMax = new Vector2(0, 1); bar.pivot = new Vector2(0, .5f);
                bar.anchoredPosition = new Vector2(485, -25); bar.sizeDelta = new Vector2(130, 12);
                foreach (var obj in new UnityEngine.Object[] { Text("clockText"), Text("valueText"), Text("businessFailureText"), Text("rateText"), bar, bar.gameObject }) PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                var presenter = UnityEngine.Object.FindFirstObjectByType<IntegrityPresenter>(); var result = new SerializedObject(presenter);
                var root = (GameObject)result.FindProperty("breachPanel").objectReferenceValue;
                result.FindProperty("resultTitle").objectReferenceValue = root.transform.Find("Title").GetComponent<TMP_Text>();
                result.ApplyModifiedPropertiesWithoutUndo();
                ((RectTransform)root.transform).sizeDelta = new Vector2(880, 280);
                root.transform.Find("Title").GetComponent<TMP_Text>().fontSize = 28;
                ((TMP_Text)result.FindProperty("breachPath").objectReferenceValue).fontSize = 18;
                EditorSceneManager.MarkSceneDirty(host.gameObject.scene); EditorSceneManager.SaveScene(host.gameObject.scene);
            }
        }

        public static SimulationHost Open(string scene = "Assets/Scenes/Connections.unity", float target = 10000, int limit = 100)
        {
            var host = FragilityWitness.Open(scene);
            Set(host, "shiftEnabled", true); Set(host, "shiftWorkTarget", target); Set(host, "businessFailureLimit", limit);
            host.ResetSimulation(); host.Recorder.BeginRun(); return host;
        }
        public static NodeView[] Route(SimulationHost host) => (NodeView[])typeof(ConnectionTrafficDirector)
            .GetMethod("FindRoute", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(host.GetComponent<ConnectionTrafficDirector>(),
                new object[] { host.FindNodeResources("EXT1").View, host.FindNodeResources("DB").View });
        public static LogicalConnectionState Add(SimulationHost host, string id, float[] work, bool hostile = false,
            TimedModifierBehaviorDefinition[] modifiers = null)
            => AddStages(host, id, work.Select((w, i) => new OperationStage((OperationKind)(i % 3), w, 5,
                "Business review", behaviors: modifiers)).ToArray(), hostile);
        public static LogicalConnectionState AddStages(SimulationHost host, string id, OperationStage[] stages, bool hostile = false)
            => host.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0, id, Route(host), "business-review", "Business review", stages, hostile)));
        public static void Establish(SimulationHost host, LogicalConnectionState c)
            => host.StepSimulation((c.Definition.Route.Length - 1) * host.ConnectionTravelSecondsPerLink + .02f);

        public static void Run()
        {
            checks.Clear(); Directory.CreateDirectory(Evidence);
            try
            {
                foreach (string scene in CanonicalSceneAuthoring.Scenes)
                {
                    var h = Open(scene); Check(h.CreditedWork == 0 && h.LegitimateFailureCount == 0 && !h.RunEnded, scene + " initial objectives");
                    var arc = Add(h, "arc", new[] { 40f, 60f, 100f }); Establish(h, arc); h.StepSimulation(8);
                    Check(arc.CurrentOperationIndex == 1 && h.CreditedWork == 0, scene + " first operation grants no business credit");
                    h.StepSimulation(12.02f); Check(arc.CurrentOperationIndex == 2 && h.CreditedWork == 0, scene + " second operation grants no business credit");
                    h.StepSimulation(21); Near(h.CreditedWork, 200, scene + " whole arc credit");
                    Check(h.WorkCreditCount == 1 && h.LegitimateFailureCount == 0, scene + " exact successful job receipt");
                    var receipt = h.Recorder.CurrentLog.events.Single(e => e.type == "business_work_credit");
                    Check(receipt.connectionId == arc.ConnectionId && receipt.operationIndex == -1 && receipt.businessWork == 200 &&
                        h.Recorder.CurrentLog.events.Any(e => e.type == "connection_resolved" && e.connectionId == arc.ConnectionId && e.sequence < receipt.sequence),
                        scene + " canonical whole-job receipt follows completed resolution, distinct from operation evidence");
                    typeof(SimulationHost).GetMethod("Resolve", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(h, new object[] { arc, ConnectionResolution.Completed });
                    Near(h.CreditedWork, 200, scene + " duplicate resolution cannot recredit");
                    Add(h, "hostile", new[] { 5f }, true); h.StepSimulation(4); Near(h.CreditedWork, 200, scene + " hostile completion excluded");
                    h = Open(scene, target: 5); Add(h, "win", new[] { 5f }); h.StepSimulation(100);
                    Check(h.Ending == RunEnding.ShiftComplete && h.SimulationTimeSeconds < 3 && h.CreditedWork == 5, scene + " success and coarse-step early stop");
                    float frozen = h.SimulationTimeSeconds; h.StepSimulation(20); Check(h.SimulationTimeSeconds == frozen && !h.Recorder.IsRecording, scene + " frozen and recorder finalized");
                    h = Open(scene, limit: 1); Add(h, "kill", new[] { 100f }); h.StepSimulation(1.3f); h.KillConnection("kill");
                    Check(h.Ending == RunEnding.Fired && h.LegitimateFailureCount == 1 && h.CreditedWork == 0, scene + " failure boundary dismissal");
                    h.ResetSimulation(); Check(!h.RunEnded && h.CreditedWork == 0 && h.LegitimateFailureCount == 0, scene + " complete reset");
                    MoreCases(scene);
                }
            }
            finally { File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks); }
        }
        private static void MoreCases(string scene)
        {
            var h = Open(scene);
            var c = Add(h, "inflated", new[] { 40f }, modifiers: new[] { new TimedModifierBehaviorDefinition("extra", 0, 100, 10, 0) });
            h.StepSimulation(20); Near(c.BusinessWorkValue, 40, scene + " fixed authored business value");
            Check(c.Resolution == ConnectionResolution.Completed && c.CompletedWork >= 49.99f && h.CreditedWork == 40,
                scene + " processing modifier does not inflate credit; resolution=" + c.Resolution + ", processing=" + c.CompletedWork + ", credit=" + h.CreditedWork);
            h = Open(scene); c = Add(h, "reduced", new[] { 40f }, modifiers: new[] { new TimedModifierBehaviorDefinition("less", 0, 100, -20, 0) });
            h.StepSimulation(10); Check(c.Resolution == ConnectionResolution.Completed && h.CreditedWork == 40, scene + " cheaper processing retains business value");

            h = Open(scene);
            c = AddStages(h, "failed-arc", new[] { new OperationStage(OperationKind.Read, 40, 5, "Read"),
                new OperationStage(OperationKind.Execute, 60, 5, "Execute"),
                new OperationStage(OperationKind.Write, 100, 5, "Write", behaviors: new[] { TimedModifierBehaviorDefinition.Rate("stall", 0, 0, 0) }) });
            h.StepSimulation(25); Check(c.CurrentOperationIndex == 2 && h.CreditedWork == 0, scene + " two completed operations still grant zero");
            h.StepSimulation(100); Check(c.Resolution == ConnectionResolution.TimedOut && h.CreditedWork == 0 && h.LegitimateFailureCount == 1,
                scene + " failed multi-operation job loses whole credit and counts one failure");

            h = Open(scene, limit: 2); FragilityWitness.ConfigureFixture(h, 4, 2); h.StepSimulation(100);
            Check(h.Ending == RunEnding.Fired && h.LegitimateFailureCount == 2 && h.SimulationTimeSeconds < 33,
                scene + " simultaneous timeouts settle before failure-limit ending");
            Check(h.Recorder.CurrentLog.events.Count(e => e.type == "business_failure") == 2 &&
                h.Recorder.CurrentLog.events.Count(e => e.type == "shift_ended") == 1,
                scene + " existing failure receipts are not duplicated and result recorded once");

            foreach (bool reverse in new[] { false, true })
            {
                h = Open(scene, 5, 1); FragilityWitness.ConfigureFixture(h, 0, 0);
                bool premature = false;
                h.ConnectionResolved += (_, _) => { h.CompleteIntegrityBoundary(); premature |= h.RunEnded; };
                SimultaneousJobs(h, reverse);
                h.StepSimulation(100);
                Check(h.Ending == RunEnding.Fired && h.CreditedWork == 5 && h.LegitimateFailureCount == 1 && !premature,
                    scene + " failure beats same-boundary success independent of ID/order and observer callbacks");
                Check(h.Recorder.CurrentLog.run.finalShift.credited == 5 && h.Recorder.CurrentLog.run.finalShift.failures == 1,
                    scene + " final recorder retains both successful WORK and failure");
            }

            h = Open(scene, target: .1f); Set(h, "serviceTimeoutTolerance", 1f); h.ResetSimulation(); h.Recorder.BeginRun();
            c = Add(h, "exact", new[] { .1f }); h.StepSimulation(100);
            Check(c.Resolution == ConnectionResolution.Completed && h.Ending == RunEnding.ShiftComplete && h.LegitimateFailureCount == 0 &&
                c.ResolvedAtSeconds == c.ServiceDeadlineSeconds,
                scene + " exact-deadline completion succeeds before timeout at identical authoritative time");

            h = Open(scene); h.StepSimulation(400);
            Check(!h.RunEnded && h.SimulationTimeSeconds > 360 && h.CreditedWork == 0 && h.LegitimateFailureCount == 0,
                scene + " elapsed time beyond six minutes does not end an empty shift");
            h = Open(scene, target: 5); h.Recorder.RecordingEnabled = false;
            c = Add(h, "unrecorded", new[] { 5f }); h.StepSimulation(10);
            Check(h.Ending == RunEnding.ShiftComplete && h.CreditedWork == 5 && h.WorkCreditCount == 1,
                scene + " recorder-disabled authoritative credit");
            string replay = JsonUtility.ToJson(h.Shift); h.ResetSimulation(); Add(h, "unrecorded", new[] { 5f }); h.StepSimulation(10);
            Check(JsonUtility.ToJson(h.Shift) == replay, scene + " exact deterministic accounting/reset replay with reused canonical ID");

            h = Open(scene, target: 5); FragilityWitness.ConfigureFixture(h, 0, 0);
            Add(h, "win", new[] { 5f }); var unfinished = Add(h, "unfinished", new[] { 1000f }); h.StepSimulation(100);
            Check(h.ShiftSucceeded && !h.RunFailed && unfinished.IsActive && h.LegitimateFailureCount == 0 && h.Shift.unfinished == 1000,
                scene + " frozen unfinished job is not failed or partially credited");
            float work = unfinished.CompletedWork, time = h.SimulationTimeSeconds; h.StepSimulation(100);
            Check(h.SimulationTimeSeconds == time && unfinished.CompletedWork == work && !h.KillConnection("unfinished") &&
                !h.DamageIntegrity(h.FindNodeResources("DB"), "late", "late"), scene + " ended simulation and direct mutation gates");
            Check(!h.TryInjectInfection("DB", InfectionKind.Blackout, 0, out _) &&
                !h.TryActivateOverclock("DB", "overclock db", out _) && !h.Worms.TryIsolate("DB", "isolate db", out _),
                scene + " post-ending infection/Overclock/isolation rejected");
            bool rejected = false; int queue = QueueCount(h);
            try { h.GetComponent<ConnectionTrafficDirector>().QueueProfile("product-lookup"); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected && QueueCount(h) == queue, scene + " post-ending traffic queue rejected before mutation");

            foreach (bool blackout in new[] { false, true })
            {
                h = Open(scene, limit: 1); FragilityWitness.ConfigureFixture(h, 0, 2); h.StepSimulation(3);
                h.ConnectionResolved += (_, _) => h.CompleteIntegrityBoundary();
                bool applied = blackout ? h.TryInjectInfection("DB", InfectionKind.Blackout, 0, out _) : h.Worms.TryIsolate("DB", "isolate db", out _);
                Check(applied && h.Ending == RunEnding.Fired && h.LegitimateFailureCount == 2 && h.CreditedWork == 0,
                    scene + " whole interruption batch settled before firing: " + (blackout ? "Blackout" : "isolation"));
                Check(h.Recorder.CurrentLog.events.Count(e => e.type == "business_failure") == 2,
                    scene + " interruption uses one existing receipt per job");
            }
            h = Open(scene, target: 5); FragilityWitness.ConfigureFixture(h, 0, 0);
            h.StartClean("DB", "clean db", out var clean);
            h.StartScan("API", "scan api", out var scan);
            h.GetComponent<ConnectionsDuelDirector>().TrySchedule("AUTH", DuelVariant.CommandRace, 100, "business-witness", out _);
            var duel = h.GetComponent<ConnectionsDuelDirector>().Current;
            Add(h, "finish", new[] { 5f }); h.StepSimulation(10);
            Check(h.RunEnded && h.ActiveClean == clean && h.ActiveScan == scan && !scan.IsComplete && duel.Outcome == null,
                scene + " ending preserves diagnostic sessions without fabricated cancellation/concession");
            Check(h.Recorder.CurrentLog.events.Count(e => e.type == "duel_aborted") == 1 &&
                h.Recorder.CurrentLog.events.All(e => e.type != "duel_conceded"), scene + " exactly one clean Duel abort receipt");
            foreach (bool rescue in new[] { false, true })
            {
                h = Open(scene, 300, 2); FragilityWitness.ConfigureFixture(h, 0, 0);
                Add(h, "earned", new[] { 200f }); h.StepSimulation(43);
                Add(h, "sacrifice", new[] { 100f }); h.StepSimulation(.02f); h.KillConnection("sacrifice");
                Set(h.FindNodeResources("DB"), "baselineLoad", 4f);
                var t = h.GetComponent<ConnectionTrafficDirector>(); t.QueueProfile("product-lookup"); t.QueueProfile("product-lookup");
                h.StepSimulation((Route(h).Length - 1) * h.ConnectionTravelSecondsPerLink + 10);
                Check(h.CreditedWork == 200 && h.LegitimateFailureCount == 1 && !h.RunEnded, scene + " just-in-time fixture reaches both objective margins");
                if (rescue) h.TryActivateOverclock("DB", "overclock db", out _);
                h.StepSimulation(100);
                Check(rescue ? h.Ending == RunEnding.ShiftComplete && h.CreditedWork == 400 && h.LegitimateFailureCount == 1 :
                    h.Ending == RunEnding.Fired && h.CreditedWork == 200 && h.LegitimateFailureCount == 3,
                    scene + " real operator intervention changes just-in-time outcome; Overclock=" + rescue);
            }
            CorePrecedence(scene);
        }

        public static SimulationHost Fixture(float target, int limit, float baseline = 0)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter canonical Play Mode; review tuning is never saved.");
            var h = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            Set(h, "shiftEnabled", true); Set(h, "shiftWorkTarget", target); Set(h, "businessFailureLimit", limit);
            FragilityWitness.ConfigureFixture(h, baseline, 0); return h;
        }
        public static void QueueJob(SimulationHost h, string id, params OperationStage[] stages)
            => h.GetComponent<ConnectionTrafficDirector>().QueueSpawnPlan(new ConnectionSpawnPlan(h.SimulationTimeSeconds,
                id, Route(h), "business-review", "Business review", stages), "business_shift_review");
        public static OperationStage[] Arc(bool stallFinal = false) => new[] {
            new OperationStage(OperationKind.Read, 40, 5, "Read records"),
            new OperationStage(OperationKind.Execute, 60, 5, "Process records"),
            new OperationStage(OperationKind.Write, 100, 5, "Write records", behaviors: stallFinal ?
                new[] { TimedModifierBehaviorDefinition.Rate("review-stall", 0, 0, 0) } : null) };
        [MenuItem("Tools/Connections/Business Shift/Successful whole-connection WORK")]
        public static void LoadSuccess()
        { var h = Fixture(200, 2); QueueJob(h, "job", Arc()); Debug.Log("Whole job: READ40/EXECUTE60/WRITE100; only final completion credits200. Intermediate WORK remains0."); }
        [MenuItem("Tools/Connections/Business Shift/Reliability defeat or rescue")]
        public static void LoadReliability()
        {
            var h = Fixture(200, 2, 4); var traffic = h.GetComponent<ConnectionTrafficDirector>();
            var a = traffic.QueueProfile("product-lookup"); var b = traffic.QueueProfile("product-lookup");
            Debug.Log("Two existing benign PURPOSE jobs: " + a.ConnectionId + ", " + b.ConnectionId + ". No action: two timeouts/FIRED; Overclock can rescue them.");
        }
        [MenuItem("Tools/Connections/Business Shift/Just-in-time success versus dismissal")]
        public static void LoadJustInTime()
        {
            var h = Fixture(300, 2);
            QueueJob(h, "earned", new OperationStage(OperationKind.Read, 200, 5, "Completed business")); h.StepSimulation(43);
            QueueJob(h, "sacrifice", new OperationStage(OperationKind.Read, 100, 5, "Sacrificed business")); h.StepSimulation(.02f); h.KillConnection("sacrifice");
            Set(h.FindNodeResources("DB"), "baselineLoad", 4f);
            var t = h.GetComponent<ConnectionTrafficDirector>(); var a = t.QueueProfile("product-lookup"); var b = t.QueueProfile("product-lookup");
            Debug.Log("200/300 WORK, 1/2 failures. Review " + a.ConnectionId + " / " + b.ConnectionId + "; overclock DB after ~10 service seconds saves both. No action reaches FIRED with all same-boundary failures retained.");
        }
        [MenuItem("Tools/Connections/Business Shift/Failed multi-operation job earns zero")]
        public static void LoadFailedArc()
        { var h = Fixture(200, 1); QueueJob(h, "failed", Arc(true)); Debug.Log("First40+60 processing completes; final WRITE stalls. Zero business credit, one timeout failure, FIRED."); }
        [MenuItem("Tools/Connections/Business Shift/CORE breach with unfinished business")]
        public static void LoadCore()
        {
            var h = Fixture(1000, 10); QueueJob(h, "unfinished", new OperationStage(OperationKind.Read, 1000, 5, "Unfinished business")); h.StepSimulation(3);
            var selector = new HackerDirectiveSelector(h.Integrity, h.PhysicalLinks, HackerDirective.BREACH, 1701); selector.EnsureOffensive(h.SimulationTimeSeconds);
            foreach (string id in selector.Offensive.path)
            {
                var node = h.FindNodeResources(id); var state = h.Integrity.For(node); if (state.External) continue;
                while (state.Value > 0) h.DamageIntegrity(node, "review", "CORE fixture");
            }
        }
        [MenuItem("Tools/Connections/Business Shift/Beyond six minutes without a deadline")]
        public static void LoadUnbounded()
        { var h = Fixture(1000, 10); h.StepSimulation(400); Debug.Log("Empty controlled fixture beyond six minutes: no time-based ending. This is not ambient-balance evidence."); }

        public static void SamplePlans()
        {
            Directory.CreateDirectory(Evidence); var rows = new List<string> { "scene,plans,min_business_work,median_business_work,max_business_work,mean_business_work,target,failures" };
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                var h = Open(scene); var t = h.GetComponent<ConnectionTrafficDirector>(); var values = new List<double>();
                for (int i = 0; i < 200; i++)
                {
                    var plan = t.GenerateBenignPlan(0);
                    var c = (LogicalConnectionState)typeof(SimulationHost).GetMethod("BuildState", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(h, new object[] { LogicalConnectionDefinition.FromPlan(plan) });
                    double expected = plan.Operations.Sum(op => (double)op.TotalWork);
                    if (c.BusinessWorkValue != expected) throw new Exception("Sampled concrete plan value differs from frozen job value.");
                    values.Add(c.BusinessWorkValue);
                }
                values.Sort(); double median = (values[99] + values[100]) / 2;
                rows.Add(FormattableString.Invariant($"{Path.GetFileNameWithoutExtension(scene)},200,{values.Min():0.###},{median:0.###},{values.Max():0.###},{values.Average():0.###},{(scene == CanonicalSceneAuthoring.Scenes[0] ? 1000 : 1500)},10"));
            }
            File.WriteAllLines(Evidence + "/plan-value-sample.csv", rows);
        }

        public static void Regressions()
        {
            Directory.CreateDirectory(Evidence); var report = new List<string>();
            try { Run(); report.Add("PASS BusinessShift"); } catch (Exception e) { report.Add("FAIL BusinessShift: " + e.Message); }
            try { ConnectionVisualIntegrationWitness.Regressions(); report.Add("PASS ExistingBroadAndVisual"); }
            catch (Exception e) { report.Add("FAIL ExistingBroadAndVisual: " + e.Message); }
            foreach (string name in new[] { "regression-checks.txt", "canonical-regression-checks.txt", "fragility-checks.txt", "fragility-regression-checks.txt" })
                File.Copy(ConnectionVisualIntegrationWitness.Evidence + "/" + name, Evidence + "/existing-" + name, true);
            File.Copy(ConnectionDisplayLabWitness.Evidence + "/deterministic-checks.txt", Evidence + "/lab-checks.txt", true);
            File.WriteAllLines(Evidence + "/regression-checks.txt", report);
            if (report.Any(r => r.StartsWith("FAIL"))) throw new Exception("Business shift regressions contain failures; inspect retained reports.");
        }

        private static int QueueCount(SimulationHost h) => ((System.Collections.ICollection)typeof(ConnectionTrafficDirector)
            .GetField("queue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(h.GetComponent<ConnectionTrafficDirector>())).Count;

        private static void SimultaneousJobs(SimulationHost h, bool reverse = false)
        {
            // Equal productive requests finish in the same allocator boundary. The
            // hostile delivery cancels the unfinished legitimate peer at that boundary.
            Add(h, reverse ? "z-success" : "a-success", new[] { 5f });
            Add(h, "peer", new[] { 100f }, modifiers: new[] { TimedModifierBehaviorDefinition.Rate("stall", 0, 0, 0) });
            AddStages(h, reverse ? "a-delivery" : "z-delivery", new[] { new OperationStage(OperationKind.Execute, 5, 5, "Delivery",
                CompletionEffect.InfectionDelivery, payload: new InfectionPayload(InfectionKind.Blackout, 0, 0)) }, true);
        }

        private static void CorePrecedence(string scene)
        {
            var h = Open(scene, 5, 1); Set(h, "integrityTuning", new IntegrityTuning { enabled = true });
            FragilityWitness.ConfigureFixture(h, 0, 0);
            var selector = new HackerDirectiveSelector(h.Integrity, h.PhysicalLinks, HackerDirective.BREACH, 1701); selector.EnsureOffensive(0);
            NodeResources last = null;
            foreach (string id in selector.Offensive.path)
            {
                var node = h.FindNodeResources(id); var state = h.Integrity.For(node); if (state.External) continue;
                if (state.Critical) { last = node; h.DamageIntegrity(node, "review", "prepare", completeBoundary: false); }
                else while (state.Value > 0) h.DamageIntegrity(node, "review", "prepare", completeBoundary: false);
            }
            Check(last != null && !h.RunEnded, scene + " real physical path prepared one strike short of CORE");
            h.ConnectionResolved += (state, resolution) => { if (resolution == ConnectionResolution.Completed) h.DamageIntegrity(last, "review", "same-boundary", completeBoundary: false); };
            SimultaneousJobs(h);
            h.StepSimulation(100);
            Check(h.Ending == RunEnding.CoreBreached && h.CreditedWork == 5 && h.LegitimateFailureCount == 1 && h.RunFailed && !h.ShiftSucceeded,
                scene + " CORE beats simultaneous failure limit and WORK success without dropping receipts");
            Check(h.Recorder.CurrentLog.events.Count(e => e.type == "shift_ended") == 1 && !h.Recorder.IsRecording,
                scene + " CORE finalizes unified recorder exactly once");
        }
    }
}
