using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Uptime.Connections.Display;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Unity.Profiling;

namespace Uptime.Connections.EditorTools
{
    public static class ConnectionVisualIntegrationWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsVisualIntegrationV0";
        private static readonly List<string> checks = new();
        private static void Check(bool condition, string label)
        { checks.Add((condition ? "PASS " : "FAIL ") + label); if (!condition) throw new Exception(label); }
        private static void Near(float a, float b, string label) => Check(Mathf.Abs(a - b) < .003f, $"{label}: expected {b}, observed {a}");
        public static void Refresh(float delta = 0)
        {
            UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>().RefreshLayouts();
            foreach (var view in UnityEngine.Object.FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None)) view.AdvanceVisual(delta);
            UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>().RefreshLayouts();
            UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>().RefreshSocketSizes();
            Canvas.ForceUpdateCanvases();
        }
        private static void BindFixture(SimulationHost host)
        {
            foreach (var view in UnityEngine.Object.FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(view.gameObject);
            UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>().Bind(host.RuntimeConnections.ToArray());
        }
        public static void Run()
        {
            checks.Clear(); Directory.CreateDirectory(Evidence);
            try
            {
                var labPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConnectionDisplayLabAuthoring.PrefabFolder + "ConnectionDisplay.prefab").GetComponent<ConnectionDisplayView>();
                var sharedPill = AssetDatabase.LoadAssetAtPath<GameObject>(ConnectionDisplayLabAuthoring.PrefabFolder + "DisplayIdentityPill.prefab").GetComponent<IdentityPillView>();
                Check(sharedPill.UsesSharedDisplay && sharedPill.GetComponentsInChildren<TMP_Text>(true).Length == 1,
                    "same authored progress pill; temporary network danger text removed");
                foreach (string scene in new[] { "Connections", "ConnectionsAdvanced" })
                {
                    var host = FragilityWitness.Open("Assets/Scenes/" + scene + ".unity", false);
                    var adapter = host.GetComponent<ConnectionGameplayDisplay>();
                    var presenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
                    var prefab = (LogicalPathView)new SerializedObject(presenter).FindProperty("pathPrefab").objectReferenceValue;
                    var packing = UnityEngine.Object.FindFirstObjectByType<IdentityPresenter>();
                    Check(prefab != null && prefab.SharedPathPrefab == labPrefab.path && prefab.SharedDisplay.style == labPrefab.style,
                        scene + " gameplay instantiates the lab's exact authored rail component and style");
                    Check(new SerializedObject(packing).FindProperty("pillPrefab").objectReferenceValue == sharedPill && adapter.style == labPrefab.style,
                        scene + " same identity prefab, style, shader and material as lab");
                    Check(adapter.heartbeat.externallyDriven && adapter.heartbeat.bpm == 120 && adapter.heartbeat.stepDistance == 18 &&
                        adapter.heartbeat.movingFraction == .58f && adapter.heartbeat.easing == HeartbeatEasing.SmoothStep,
                        scene + " accepted heartbeat defaults, controlled by gameplay clock");
                    string[] ids = FragilityWitness.ConfigureFixture(host, 4);
                    FragilityWitness.Advance(host, 3); BindFixture(host); Refresh(1);
                    var c = host.FindConnection(ids[0]); var state = adapter.StateFor(c);
                    Near(state.requestedLoad, c.DesiredLoad, scene + " effective requested LOAD");
                    Near(state.productiveSpeed, .6f, scene + " actual productive slowdown");
                    Near(state.workProgress, c.CompletedWork / c.CurrentTotalWork, scene + " current operation WORK fill");
                    Check(c.AtRisk && state.dangerSeverity > .2f && state.dangerSeverity < .95f && !state.exceedsNormalBudget,
                        scene + " current-rate failure with meaningful rescue headroom");
                    Check(state.identity == c.KnownIdentity && state.trafficClass == DisplayTrafficClass.Ordinary,
                        scene + " visible identity and neutral class; no invented priority");
                    var paths = UnityEngine.Object.FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None);
                    Check(paths.Length == 2 && paths.All(v => v.UsesSharedDisplay && v.GetComponent<PathGraphic>() == null),
                        scene + " shared path implementation; no parallel legacy renderer per connection");
                    Check(paths.Select(v => v.DisplayState.identitySlot).Distinct().Count() == 2,
                        scene + " existing lifetime slot allocator differentiates adjacent connection ribbons");
                    foreach (var view in paths)
                    {
                        var display = view.DisplayState;
                        Check(adapter.style.IdentityColor(display) == adapter.style.ordinaryIdentityColors[display.identitySlot] &&
                            view.TerminalMarker.color == view.DisplayAccent,
                            scene + " every route segment and terminal socket shares its lifetime identity/status");
                    }
                    Check(paths.All(v => v.TerminalMarker != null && v.TerminalMarker.gameObject.activeSelf), scene + " established target sockets retained");
                    var pills = packing.GetComponentsInChildren<IdentityPillView>(true).Where(p => p.UsesSharedDisplay).ToArray();
                    Check(pills.Length >= 2 && pills.All(p => p.ProgressPlate.treatment == PillProgressTreatment.Interior), scene + " actual packed pills use interior fill");
                    float work = c.CompletedWork, time = host.SimulationTimeSeconds;
                    adapter.heartbeat.SetReviewTime(3.25); Refresh();
                    Check(c.CompletedWork == work && host.SimulationTimeSeconds == time, scene + " presentation cannot advance simulation");
                    Color[] saved = adapter.style.ordinaryIdentityColors;
                    try
                    {
                        adapter.style.ordinaryIdentityColors = Enumerable.Repeat(Color.magenta, 6).ToArray(); Refresh();
                        var safe = state; safe.dangerSeverity = 0;
                        Check(labPrefab.style.Rail(safe, 0) == Color.magenta && sharedPill.ProgressPlate.material == labPrefab.pill.material &&
                            pills.All(p => p.ProgressPlate.color.r > .99f && p.ProgressPlate.color.b > .99f),
                            scene + " one live style edit propagates to gameplay and lab asset references");
                    }
                    finally { adapter.style.ordinaryIdentityColors = saved; Refresh(); }
                    long allocated = PresentationAllocationProbe.Measure(() => { for (int i = 0; i < 1000; i++) adapter.StateFor(c); });
                    if (allocated >= 0) Check(allocated == 0, scene + " 1000 mapping calls allocate " + allocated + " managed bytes (calibrated GC.Alloc)");
                    else checks.Add("UNAVAILABLE " + scene + " allocation recorder failed sentinel calibration; no zero-allocation claim");
                    FragilityWitness.Advance(host, 10);
                    Check(host.TryActivateOverclock("DB", "overclock db", out _), scene + " real Overclock rescue accepted");
                    host.StepSimulation(.02f); state = adapter.StateFor(c);
                    Near(state.productiveSpeed, 1, scene + " rescue restores full productive activity");
                    Check(!c.AtRisk && state.dangerSeverity < .1f, scene + " authoritative recovery promptly clears danger");
                    host.ResetSimulation(); Check(adapter.heartbeat.Beats == 0, scene + " restart resets shared visual phase");

                    FragilityWitness.ConfigureFixture(host, 2); FragilityWitness.Advance(host, 3); BindFixture(host); Refresh(1);
                    c = host.RuntimeConnections.First(x => x.IsActive); state = adapter.StateFor(c);
                    Check(state.productiveSpeed < 1 && state.dangerSeverity == 0, scene + " slowed yet safe");
                }
                var h = FragilityWitness.Open();
                var zero = FragilityWitness.Add(h, "zero", behaviors: new[] { TimedModifierBehaviorDefinition.Rate("zero", 0, 0, 0) });
                h.StepSimulation(1.2f); h.StepSimulation(.02f);
                var gateway = h.GetComponent<ConnectionGameplayDisplay>(); var z = gateway.StateFor(zero);
                Check(zero.Satisfaction > .99f && zero.ActualWorkRate == 0 && z.productiveSpeed == 0 && z.requestedLoad == 5,
                    "full LOAD grant with zero WORK produces stationary dense activity");
                Check(z.dangerSeverity < .7f && !z.exceedsNormalBudget, "new stall can retain substantial normal-rate recovery opportunity");
                float previous = z.dangerSeverity;
                h.StepSimulation(22); z = gateway.StateFor(zero);
                Check(z.dangerSeverity > previous && z.exceedsNormalBudget && z.normalBudgetWarningMix > 0,
                    "danger increases as normal-rate opportunity diminishes");
                h.StepSimulation(30); z = gateway.StateFor(zero);
                Check(z.resolution == DisplayResolution.TimedOut && z.workProgress == 0, "timeout never fabricates completed WORK");

                h = FragilityWitness.Open(); var late = FragilityWitness.Add(h, "late");
                foreach (string id in new[] { "peer1", "peer2", "peer3" }) FragilityWitness.Add(h, id);
                h.StepSimulation(1.2f); h.StepSimulation(28);
                foreach (string id in new[] { "peer1", "peer2", "peer3" }) h.KillConnection(id);
                h.StepSimulation(.02f); var lateState = h.GetComponent<ConnectionGameplayDisplay>().StateFor(late);
                Check(late.AtRisk && lateState.productiveSpeed == 1 && lateState.dangerSeverity > .9f && lateState.exceedsNormalBudget,
                    "at-speed but endangered after lost time retains full activity");

                h = FragilityWitness.Open(); var arc = FragilityWitness.Add(h, "arc", stages: new[] {
                    new OperationStage(OperationKind.Read, 10, 5, "read"), new OperationStage(OperationKind.Write, 100, 5, "write") });
                h.StepSimulation(1.2f); h.StepSimulation(2); var arcState = h.GetComponent<ConnectionGameplayDisplay>().StateFor(arc);
                Check(arc.CurrentOperationIndex == 1 && arcState.workProgress == 0 && arcState.operation == OperationKind.Write && arcState.productiveSpeed == 0,
                    "next operation resets actual fill; transition makes no productive activity");
                h.StepSimulation(.02f); Check(h.GetComponent<ConnectionGameplayDisplay>().StateFor(arc).productiveSpeed == 1,
                    "next operation starts productive activity without moving the deadline");
                BindFixture(h); Refresh(1);
                Check(h.KillConnection(arc.ConnectionId), "legitimate sacrifice resolves through existing host");
                Refresh();
                var killedView = UnityEngine.Object.FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None).Single();
                using (var mesh = new VertexHelper())
                {
                    typeof(ConnectionDisplayGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new[] { typeof(VertexHelper) }, null).Invoke(killedView.SharedDisplay.path, new object[] { mesh });
                    Color activityColor = killedView.SharedDisplay.style.Activity(DisplayTrafficClass.Ordinary, true);
                    var vertex = new UIVertex(); bool activityRetained = false;
                    for (int i = 0; i < mesh.currentVertCount; i++)
                    { mesh.PopulateUIVertex(ref vertex, i); activityRetained |= vertex.color.Equals((Color32)activityColor); }
                    Check(activityRetained, "resolved established paths retain stationary activity through shared departure");
                }
                var killed = h.GetComponent<ConnectionGameplayDisplay>().StateFor(arc);
                Check(killed.resolution == DisplayResolution.Killed && killed.workProgress < 1 && h.LegitimateFailureCount == 1,
                    "kill preserves partial WORK and one business failure");
                h = FragilityWitness.Open();
                var canceled = FragilityWitness.Add(h, "canceled"); h.StepSimulation(1.2f);
                Check(h.Worms.TryIsolate("DB", "isolate DB", out _), "actual isolation accepted");
                var canceledState = h.GetComponent<ConnectionGameplayDisplay>().StateFor(canceled, .4f);
                Check(canceled.Resolution == ConnectionResolution.Canceled && canceledState.resolution == DisplayResolution.Canceled &&
                    canceledState.workProgress < 1 && h.LegitimateFailureCount == 1, "isolation is failed partial service, not completion");
                h = FragilityWitness.Open(); canceled = FragilityWitness.Add(h, "blackout"); h.StepSimulation(1.2f);
                Check(h.TryInjectInfection("DB", InfectionKind.Blackout, 0, out _) &&
                    h.GetComponent<ConnectionGameplayDisplay>().StateFor(canceled).resolution == DisplayResolution.Canceled,
                    "Blackout cancellation retains failure departure and partial progress");
                h = FragilityWitness.Open();
                var fast = FragilityWitness.Add(h, "fast", behaviors: new[] { TimedModifierBehaviorDefinition.Rate("fast", 0, 0, 4) });
                h.StepSimulation(1.2f); h.StepSimulation(.02f);
                Near(h.GetComponent<ConnectionGameplayDisplay>().StateFor(fast).productiveSpeed, 4, "productive speed preserves actual factors above 2x");
                h = FragilityWitness.Open();
                var benign = FragilityWitness.Add(h, "benign"); var hostile = FragilityWitness.Add(h, "hostile", hostile: true);
                FragilityWitness.Set(h.FindNodeResources("DB"), "baselineLoad", 4f); h.StepSimulation(1.2f); h.StepSimulation(.02f);
                var b = h.GetComponent<ConnectionGameplayDisplay>().StateFor(benign); var bad = h.GetComponent<ConnectionGameplayDisplay>().StateFor(hostile);
                Check(b.dangerSeverity == bad.dangerSeverity && b.normalBudgetWarningMix == bad.normalBudgetWarningMix &&
                    b.workProgress == bad.workProgress && b.productiveSpeed == bad.productiveSpeed && b.trafficClass == bad.trafficClass,
                    "identical benign/hostile service facts receive identical progress, activity and danger");
                h = FragilityWitness.Open();
                var director = h.GetComponent<ConnectionTrafficDirector>();
                var loader = director.GenerateLoaderPlanForWitness(0, LoaderStructure.Single, LoaderPattern.Immediate);
                director.QueueSpawnPlan(loader); FragilityWitness.Advance(h, loader.LoaderAttack.durationSeconds + 10);
                var withdrawn = h.FindConnection(loader.ConnectionId); var withdrawal = h.GetComponent<ConnectionGameplayDisplay>().StateFor(withdrawn);
                Check(withdrawn.Resolution == ConnectionResolution.Withdrawn && withdrawal.resolution == DisplayResolution.Withdrawn &&
                    withdrawal.workProgress < 1 && withdrawal.productiveSpeed == 0, "actual LOADER withdrawal fades without fabricated operation completion");
                CheckSharedMeshInputs();
                Debug.Log("ConnectionVisualIntegrationWitness PASS: " + checks.Count(s => s.StartsWith("PASS")) + " assertions; " + checks.Count(s => s.StartsWith("UNAVAILABLE")) + " unavailable measurements.");
            }
            finally { File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks); }
        }
        public static void Regressions()
        {
            Directory.CreateDirectory(Evidence); var report = new List<string>();
            foreach (var test in new (string name, Action run)[] {
                ("VisualIntegration", Run), ("Fragility", FragilityWitness.Run),
                ("ExistingBroadRegressions", FragilityWitness.Regressions),
                ("DisplayLab", ConnectionDisplayLabWitness.Run), ("IdHopping", IdHoppingWitness.Run),
                ("ReactiveIdentity", ReactiveIdentityWitness.Run), ("GeneratedIdentity", GeneratedIdentityWitness.Run),
                ("Probe", ProbeWitness.Run), ("ProbeSweep", ProbeSweepWitness.Run) })
            {
                try { test.run(); report.Add("PASS " + test.name); }
                catch (Exception error) { report.Add("FAIL " + test.name + ": " + error.Message); Debug.LogException(error); }
            }
            foreach (var pair in new[] { ("regression-checks.txt", "fragility-regression-checks.txt"),
                ("canonical-regression-checks.txt", "canonical-regression-checks.txt"), ("deterministic-checks.txt", "fragility-checks.txt") })
                File.Copy(FragilityWitness.Evidence + "/" + pair.Item1, Evidence + "/" + pair.Item2, true);
            File.WriteAllLines(Evidence + "/regression-checks.txt", report);
            if (report.Any(r => r.StartsWith("FAIL"))) throw new Exception("Integrated regression failures; compare baseline reports to distinguish inherited UI assertions.");
        }
        public static void Sockets()
        {
            Run(); GeometryWitness.Run(); CanonicalSceneWitness.Run(); ConnectionDisplayLabWitness.Run();
            checks.Clear(); string folder = Evidence + "/SocketRefinement"; Directory.CreateDirectory(folder);
            try
            {
                foreach (string scene in new[] { "Connections", "ConnectionsAdvanced" })
                {
                    foreach (int count in new[] { 1, 2, 8, 16 })
                    {
                        var host = FragilityWitness.Open("Assets/Scenes/" + scene + ".unity");
                        FragilityWitness.ConfigureFixture(host, 0, count); FragilityWitness.Advance(host, 3);
                        BindFixture(host); Refresh(1);
                        var presenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
                        var paths = UnityEngine.Object.FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None);
                        var markers = paths.Select(v => v.TerminalMarker).ToArray();
                        Check(markers.Length == count && markers.All(m => m.FitsSocketNeighbors && m.PreferredDiameter == 32),
                            scene + " shared 32-unit socket; established count=" + count);
                        if (count == 1) Check(markers[0].rectTransform.rect.width == 32 && markers[0].Ring.rectTransform.rect.width == 28 &&
                            markers[0].InnerDot.rectTransform.rect.width == 7 && markers[0].Ring.BorderThickness == 3.5f, scene + " full authored socket where room permits");
                        bool placement = true, separation = true;
                        foreach (var path in paths)
                        {
                            var c = host.RuntimeConnections.Single(c => c.KnownIdentity == path.DisplayState.identity);
                            var offsets = (float[])typeof(LogicalPathView).GetField("endOffsets", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(path);
                            Vector2 from = presenter.NetworkArea.InverseTransformPoint(c.Definition.Route[c.Definition.Route.Length - 2].Node.transform.position);
                            Vector2 target = presenter.NetworkArea.InverseTransformPoint(c.TargetNode.transform.position);
                            Vector2 expected = LogicalPathView.TerminalSocketCenter(EdgeGeometry.CardRect(c.TargetNode, presenter.NetworkArea),
                                (from - target).normalized, -offsets[offsets.Length - 1], path.TerminalMarker.BorderOutset);
                            Vector2 actual = presenter.NetworkArea.InverseTransformPoint(path.TerminalMarker.transform.position);
                            placement &= Vector2.Distance(expected, actual) < .01f && path.TerminalMarker.color == path.DisplayAccent;
                        }
                        for (int i = 0; i < markers.Length; i++)
                        for (int j = i + 1; j < markers.Length; j++)
                        {
                            Vector2 a = presenter.NetworkArea.InverseTransformPoint(markers[i].transform.position);
                            Vector2 b = presenter.NetworkArea.InverseTransformPoint(markers[j].transform.position);
                            separation &= Vector2.Distance(a, b) + .01f >= (markers[i].rectTransform.rect.width + markers[j].rectTransform.rect.width) * .5f + 3;
                        }
                        Check(placement, scene + " endpoint and resolved identity/status unchanged; count=" + count);
                        Check(separation, scene + " backing circles do not overlap; count=" + count + "; diameter=" + markers[0].rectTransform.rect.width);
                        Check(markers.All(m => m.Ring.BorderThickness <= m.Ring.rectTransform.rect.width * .18f + .01f),
                            scene + " compact ring leaves a visible dark annulus; count=" + count);
                        var marker = markers[0]; marker.SetMaximumDiameter(0, .2f); marker.SetMaximumDiameter(0, .2f); marker.AdvanceVisual(.1f);
                        Check(marker.rectTransform.rect.width == 0, scene + " no forced 12-unit floor or oversized shrink tween");
                        marker.SetMaximumDiameter(32, .2f); marker.AdvanceVisual(.2f);
                        Check(marker.rectTransform.rect.width == 32, scene + " socket recovers from zero when space returns");
                    }
                    var h = FragilityWitness.Open("Assets/Scenes/" + scene + ".unity");
                    var routeBuilder = typeof(ConnectionTrafficDirector).GetMethod("FindRoute", BindingFlags.Instance | BindingFlags.NonPublic);
                    int id = 0;
                    foreach (string destination in new[] { "DB", "API", "AUTH" })
                    {
                        var route = (NodeView[])routeBuilder.Invoke(h.GetComponent<ConnectionTrafficDirector>(),
                            new object[] { h.FindNodeResources("EXT1").View, h.FindNodeResources(destination).View });
                        h.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0, "s" + id++, route, "socket-review", "Socket review",
                            new[] { new OperationStage(OperationKind.Read, 100, 1, "Read") })));
                    }
                    FragilityWitness.Advance(h, 3); BindFixture(h); Refresh(1);
                    Check(UnityEngine.Object.FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None).All(v => v.TerminalMarker.rectTransform.rect.width == 32),
                        scene + " transit lanes and different-node destinations do not unnecessarily shrink isolated sockets");
                }
            }
            finally { File.WriteAllLines(folder + "/deterministic-checks.txt", checks); }
        }

        public static void AuthoringSafety()
        {
            Directory.CreateDirectory(Evidence);
            string folder = ConnectionDisplayLabAuthoring.PrefabFolder;
            string[] assets = { folder + "ConnectionDisplay.prefab", folder + "DisplayIdentityPill.prefab", folder + "DisplayLabSample.prefab",
                folder + "DisplayStyle.asset", ConnectionVisualIntegrationAuthoring.GameplayPath,
                "Assets/Shaders/ConnectionsDisplay/ProgressPill.shader", "Assets/Shaders/ConnectionsDisplay/ProgressPill.mat",
                "Assets/Scenes/Connections.unity", "Assets/Scenes/ConnectionsAdvanced.unity" };
            var before = assets.ToDictionary(p => p, File.ReadAllBytes);
            byte[] lab = File.ReadAllBytes(ConnectionDisplayLabAuthoring.ScenePath);
            var report = new List<string>();
            try
            {
                bool guarded = false;
                try { ConnectionVisualIntegrationAuthoring.Apply(); }
                catch (InvalidOperationException) { guarded = true; }
                if (!guarded) throw new Exception("Repeated integration migration was not guarded.");
                report.Add("PASS repeated shared-asset migration refuses regeneration");
                ConnectionDisplayLabAuthoring.Build();
                foreach (string asset in assets)
                {
                    bool same = before[asset].SequenceEqual(File.ReadAllBytes(asset));
                    report.Add((same ? "PASS " : "FAIL ") + "fixture authoring leaves shared/canonical asset unchanged: " + asset);
                    if (!same) throw new Exception("Lab authoring changed " + asset);
                }
                ConnectionDisplayLabWitness.Run(); report.Add("PASS regenerated fixtures retain all focused lab checks");
            }
            finally
            {
                File.WriteAllBytes(ConnectionDisplayLabAuthoring.ScenePath, lab);
                AssetDatabase.ImportAsset(ConnectionDisplayLabAuthoring.ScenePath, ImportAssetOptions.ForceSynchronousImport);
                EditorSceneManager.OpenScene(ConnectionDisplayLabAuthoring.ScenePath);
                File.WriteAllLines(Evidence + "/authoring-safety-checks.txt", report);
            }
        }
        private static void CheckSharedMeshInputs()
        {
            EditorSceneManager.OpenScene(ConnectionDisplayLabAuthoring.ScenePath);
            var lab = UnityEngine.Object.FindFirstObjectByType<ConnectionDisplayLab>(); lab.RenderNow();
            var path = lab.samples[0].view.path;
            var populate = typeof(ConnectionDisplayGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null);
            path.SetGeometry(new[] { Vector2.zero }, new[] { new Vector2(100, 0) }, new[] { 6f });
            path.SetVisibility(.3f, false);
            using var helper = new VertexHelper(); populate.Invoke(path, new object[] { helper });
            var v = new UIVertex(); bool clipped = true;
            for (int i = 0; i < helper.currentVertCount; i++) { helper.PopulateUIVertex(ref v, i); clipped &= v.position.x <= 30.001f; }
            Check(clipped && helper.currentVertCount > 0, "connecting/interrupted rails contain only the actually revealed prefix");
            path.SetVisibility(1, true); path.SetProbe(0, new Vector2(50, 0), Vector2.right, new Vector2(20, 10), Color.red);
            populate.Invoke(path, new object[] { helper }); bool redInside = false, ordinaryOutside = false;
            for (int i = 0; i < helper.currentVertCount; i++)
            {
                helper.PopulateUIVertex(ref v, i);
                if (v.color.r == 255 && v.color.g == 0) { redInside = true; Check(v.position.x >= 29.999f && v.position.x <= 70.001f, "Probe tint bounded by scanner rectangle"); }
                else if (v.position.x < 29) ordinaryOutside = true;
            }
            Check(redInside && ordinaryOutside, "shared mesh retains Probe classification only inside scanner");
        }
    }
    internal static class PresentationAllocationProbe
    {
        public static long Measure(Action action)
        {
            bool wasEnabled = UnityEngine.Profiling.Profiler.enabled;
            UnityEngine.Profiling.Profiler.enabled = true;
            try
            {
                using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 1,
                    ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.SumAllSamplesInFrame | ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                var sentinel = new byte[4096]; GC.KeepAlive(sentinel); recorder.Stop();
                if (!recorder.Valid || recorder.Count == 0 || recorder.GetSample(0).Value < 4096) return -1;
                recorder.Reset(); recorder.Start(); action(); recorder.Stop();
                return recorder.Count > 0 ? recorder.GetSample(0).Value : 0;
            }
            finally { UnityEngine.Profiling.Profiler.enabled = wasEnabled; }
        }
    }

}
