using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Uptime.Connections.Display;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Profiling;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class FragilityCapture
    {
        private const string Pending = "Uptime.FragilityCapture";
        private const string Integration = "Uptime.VisualIntegrationCapture";
        public static void RunVisualIntegration() { SessionState.SetBool(Integration, true); Run(); }
        static FragilityCapture() => EditorApplication.update += Launch;
        public static void Run()
        {
            EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            var runner = new GameObject("Fragility graphical witness").AddComponent<FragilityCaptureRunner>();
            runner.integration = SessionState.GetBool(Integration, false); SessionState.SetBool(Integration, false);
        }
    }
    public sealed class FragilityCaptureRunner : MonoBehaviour
    {
        public bool integration;
        private string Evidence => integration ? ConnectionVisualIntegrationWitness.Evidence : FragilityWitness.Evidence;
        private SimulationHost host;
        private TMP_InputField input;
        private Camera cameraForCapture;
        private RenderTexture captureViewport;
        private Canvas canvas;
        private string folder;
        private readonly List<string> checks = new();
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Evidence);
            if (integration) File.WriteAllText(Evidence + "/presentation-profile.txt",
                "Actual Play Mode samples: two existing layout passes + all path updates + forced Canvas work. Excludes simulation stepping, object discovery, PNG encoding and GPU timing. Includes inherited layout/packing allocations; not whole-engine zero allocations or standalone FPS.\n");
            DontDestroyOnLoad(gameObject);
            yield return null;
            Exception failure = null; var stack = new Stack<IEnumerator>(); stack.Push(Capture());
            while (stack.Count > 0)
            {
                object next;
                try
                {
                    var current = stack.Peek();
                    if (!current.MoveNext()) { stack.Pop(); continue; }
                    next = current.Current;
                    if (next is IEnumerator child) { stack.Push(child); continue; }
                }
                catch (Exception error) { failure = error; break; }
                yield return next;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            Time.timeScale = 1;
            if (failure != null) Debug.LogException(failure);
            File.WriteAllLines(Evidence + "/graphical-checks.txt", checks);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
        private void Update()
        {
            // The original still-capture mode also owns its cosmetic review clock
            // while authoritative stepping is disabled; gameplay policy stays unchanged.
            if (!integration && host != null && !host.enabled)
                host.GetComponent<ConnectionGameplayDisplay>()?.heartbeat.Advance(Time.deltaTime);
        }
        private void Check(bool value, string detail)
        { if (!value) throw new InvalidOperationException("FragilityCapture FAIL: " + detail); checks.Add("PASS " + host.gameObject.scene.name + " " + detail); }
        private IEnumerator Capture()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                if (SceneManager.GetActiveScene().path != scene)
                {
                    EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single));
                    yield return null;
                }
                host = FindFirstObjectByType<SimulationHost>(); host.enabled = false;
                var traffic = host.GetComponent<ConnectionTrafficDirector>();
                traffic.RandomizeSeed = false; traffic.Seed = 1701; traffic.AutoGenerationEnabled = false;
                host.ResetSimulation();
                var terminal = FindFirstObjectByType<Terminal>();
                input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                canvas = terminal.GetComponentInParent<Canvas>();
                cameraForCapture = new GameObject("Fragility review camera", typeof(Camera)).GetComponent<Camera>();
                cameraForCapture.clearFlags = CameraClearFlags.SolidColor;
                cameraForCapture.backgroundColor = new Color(.025f, .065f, .08f);
                cameraForCapture.orthographic = true;
                cameraForCapture.transform.position = new Vector3(0, 0, -100);
                cameraForCapture.nearClipPlane = .1f; cameraForCapture.farClipPlane = 1000;
                RenderMode oldMode = canvas.renderMode; Camera oldCamera = canvas.worldCamera;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cameraForCapture; canvas.planeDistance = 10;
                // Placement must see the reviewed 16:9 viewport before a window opens, not only during ReadPixels.
                captureViewport = new RenderTexture(1920, 1080, 24);
                cameraForCapture.targetTexture = captureViewport;
                Canvas.ForceUpdateCanvases(); yield return null;
                folder = Evidence + "/Captures/" + host.gameObject.scene.name;
                Directory.CreateDirectory(folder);
                if (integration)
                {
                    traffic.AutoGenerationEnabled = true; traffic.HostileGenerationEnabled = true; host.ResetSimulation();
                    FragilityWitness.Advance(host, 15);
                    yield return Motion("ambient", 6);
                    Frames("00-generated-gameplay");
                }
                string[] ids = FragilityWitness.ConfigureFixture(host, 4, 1);
                FragilityWitness.Advance(host, 3f);
                Submit("inspect -v " + ids[0]); yield return null; yield return null;
                Check(!host.FindConnection(ids[0]).AtRisk, "healthy uncontended legitimate traffic");
                if (integration)
                {
                    var packing = FindFirstObjectByType<IdentityPresenter>();
                    foreach (var pill in packing.GetComponentsInChildren<IdentityPillView>(true))
                        Debug.Log("Packed pill " + pill.DisplayedId + " visible=" + pill.gameObject.activeInHierarchy +
                            " alpha=" + pill.ProgressPlate.color.a + " position=" + pill.transform.position + " anchors=" + ((RectTransform)pill.transform).anchorMin);
                    Debug.Log("Packing plan " + packing.StageDiagnostic);
                    Check(packing.GetComponentsInChildren<IdentityPillView>().Any(), "healthy network identity is actually visible");
                }
                Frames("01-healthy");
                ids = FragilityWitness.ConfigureFixture(host, 2);
                FragilityWitness.Advance(host, 3f);
                Submit("inspect -v " + ids[0]); yield return null; yield return null;
                Check(!host.FindConnection(ids[0]).AtRisk && host.FindConnection(ids[0]).Performance == LoadPerformance.Slowed,
                    "slowed service can be safe");
                Frames("02-slowed-safe");
                ids = FragilityWitness.ConfigureFixture(host, 4);
                FragilityWitness.Advance(host, 9.3f);
                Submit("inspect -v " + ids[0]); yield return null; yield return null;
                Check(host.FindConnection(ids[0]).AtRisk && input.isFocused, "danger visible with 22s remaining and command focus");
                yield return new WaitForSecondsRealtime(.5f);
                Frames("03-at-risk");
                Check(FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None).Any(p => p.UsesSharedDisplay && p.DisplayState.dangerSeverity > 0) &&
                    FindFirstObjectByType<IdentityPresenter>().GetComponentsInChildren<IdentityPillView>(true)
                        .All(p => p.UsesSharedDisplay && p.transform.Find("ServiceDanger") == null),
                    "continuous warning rails replace redundant network AT RISK text");
                if (integration) yield return Motion("overclock-rescue", 8, 48);
                else Submit("overclock db");
                FragilityWitness.Advance(host, .02f);
                yield return null; yield return null;
                Check(!host.FindConnection(ids[0]).AtRisk && host.OverclockedNode != null, "terminal overclock clears projected danger");
                Frames("04-rescue");
                FragilityWitness.Advance(host, 30);
                Check(host.FindConnection(ids[0]).Resolution == ConnectionResolution.Completed && host.LegitimateFailureCount == 0,
                    "rescued connection completes without failure");
                ids = FragilityWitness.ConfigureFixture(host, 4);
                FragilityWitness.Advance(host, 9.3f);
                Submit("inspect -v " + ids[0]); yield return null;
                float toDeadline = host.FindConnection(ids[0]).ServiceDeadlineSeconds - host.SimulationTimeSeconds;
                if (integration)
                {
                    FragilityWitness.Advance(host, toDeadline - 1);
                    yield return Motion("timeout", 2.6f);
                }
                else FragilityWitness.Advance(host, toDeadline + .001f);
                yield return null; yield return null;
                Check(host.FindConnection(ids[0]).Resolution == ConnectionResolution.TimedOut && host.LegitimateFailureCount == 2,
                    "actual timeout and exact business count");
                FindFirstObjectByType<NetworkHealthPanel>().Render();
                Check(FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t => t.text == "BUSINESS FAILURES 2"),
                    "cumulative failure count visibly rendered");
                if (!integration) Frames("05-timeout-departure");
                Submit("inspect -v " + ids[0]); yield return null; yield return null;
                Frames("06-timeout-history");
                yield return new WaitForSecondsRealtime(4.5f);
                Check(FindObjectsByType<ResolvedInspectRow>(FindObjectsSortMode.None).Any(r =>
                    r.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "TIMED OUT")), "collapsed history explicitly retains TIMED OUT");
                input.text = "inspect "; input.caretPosition = input.text.Length;
                input.ActivateInputField(); yield return null;
                Frames("07-resolved-summary-and-prompt");
                Check(input.isFocused && input.text == "inspect ", "persistent prompt preserves partial command and focus");
                ids = FragilityWitness.ConfigureFixture(host, 4);
                FragilityWitness.Advance(host, 9.3f);
                Submit("kill " + ids[1]); FragilityWitness.Advance(host, .02f);
                Submit("inspect -v " + ids[0]); yield return null; yield return null;
                Check(host.LegitimateFailureCount == 1 && !host.FindConnection(ids[0]).AtRisk, "legitimate sacrifice clears peer danger");
                Frames("08-sacrifice");
                if (integration) yield return IntegrationCases();
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
                Destroy(cameraForCapture.gameObject); Destroy(captureViewport); yield return null;
            }
            Debug.Log(integration ? "ConnectionVisualIntegrationCapture PASS: " + checks.Count + " checks; both canonical scenes, both resolutions and actual gameplay motion." :
                "FragilityCapture PASS: both Play Mode scenes, 32 frames at 1920x1080/1280x720, service danger/rescue/timeout/focus.");
        }
        private IEnumerator Motion(string name, float seconds, int overclockFrame = -1)
        {
            string frames = "/tmp/uptime-visual-motion/" + host.gameObject.scene.name + "-" + name;
            Directory.CreateDirectory(frames);
            var clock = host.GetComponent<ConnectionGameplayDisplay>().heartbeat;
            clock.SetReviewTime(host.SimulationTimeSeconds);
            var target = new RenderTexture(1280, 720, 24);
            var picture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var oldTarget = cameraForCapture.targetTexture; cameraForCapture.targetTexture = target;
            int peakVertices = 0, peakConnections = 0;
            long allocations = 0, ticks = 0;
            bool profilerWasEnabled = UnityEngine.Profiling.Profiler.enabled;
            UnityEngine.Profiling.Profiler.enabled = true;
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 1,
                ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.SumAllSamplesInFrame | ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            var allocationSentinel = new byte[4096]; GC.KeepAlive(allocationSentinel); recorder.Stop();
            bool allocationCounterWorks = recorder.Valid && recorder.Count > 0 && recorder.GetSample(0).Value >= 4096;
            int count = Mathf.CeilToInt(seconds * 24);
            for (int frame = 0; frame < count; frame++)
            {
                if (frame == overclockFrame) Submit("overclock db");
                host.StepSimulation(1f / 24);
                if (!host.RunEnded) clock.Advance(1.0 / 24);
                var views = FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None);
                foreach (var view in views) view.enabled = false;
                var presenter = FindFirstObjectByType<LogicalPathPresenter>();
                recorder.Reset(); recorder.Start();
                long start = System.Diagnostics.Stopwatch.GetTimestamp();
                presenter.RefreshLayouts();
                foreach (var view in views) view.AdvanceVisual(host.RunEnded ? 0 : 1f / 24);
                presenter.RefreshLayouts(); Canvas.ForceUpdateCanvases();
                ticks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
                recorder.Stop();
                if (recorder.Count > 0) allocations += recorder.GetSample(0).Value;
                peakConnections = Mathf.Max(peakConnections, views.Length);
                peakVertices = Mathf.Max(peakVertices, views.Sum(v => v.SharedDisplay.path.LastVertexCount));
                FindFirstObjectByType<WormPresenter>().RenderNow(); FindFirstObjectByType<PayloadPresenter>().RenderNow();
                cameraForCapture.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
                picture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); picture.Apply();
                File.WriteAllBytes(frames + "/frame-" + frame.ToString("0000") + ".png", picture.EncodeToPNG());
                RenderTexture.active = previous;
                if (name == "timeout" && frame == 25) Frames("05-timeout-departure");
                yield return null;
            }
            foreach (var view in FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None)) view.enabled = true;
            cameraForCapture.targetTexture = oldTarget; Destroy(picture); Destroy(target);
            UnityEngine.Profiling.Profiler.enabled = profilerWasEnabled;
            string allocationReading = allocationCounterWorks ? (allocations / count).ToString() : "UNAVAILABLE";
            File.AppendAllText(Evidence + "/presentation-profile.txt",
                $"{host.gameObject.scene.name}/{name}: {count} frames, peak connections={peakConnections}, peak path vertices={peakVertices}; " +
                $"sampled presentation mean={1000.0 * ticks / System.Diagnostics.Stopwatch.Frequency / count:0.000}ms, {allocationReading} managed bytes/pass; GC.Alloc recorder calibrated={allocationCounterWorks}.\n");
            Check(peakConnections > 0, name + " records actual rendered gameplay connections");
            if (name == "timeout") Check(FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None).Length == 0,
                "timed-out views, activity meshes and external markers retire after visual departure");
        }
        private IEnumerator ProfileGameplayFrames()
        {
            bool wasEnabled = UnityEngine.Profiling.Profiler.enabled;
            UnityEngine.Profiling.Profiler.enabled = true;
            using var counter = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 64);
            host.enabled = true;
            yield return null;
            var sentinel = new byte[1024 * 1024]; GC.KeepAlive(sentinel);
            yield return null;
            bool calibrated = counter.Valid && counter.LastValue >= 1024 * 1024;
            long total = 0, maximum = 0;
            for (int i = 0; i < 30; i++)
            { yield return null; long value = counter.LastValue; total += value; maximum = Math.Max(maximum, value); }
            host.enabled = false; UnityEngine.Profiling.Profiler.enabled = wasEnabled;
            File.AppendAllText(Evidence + "/presentation-profile.txt", host.gameObject.scene.name +
                "/native crowded gameplay (30 ordinary PlayerLoop frames, no PNG capture): GC Allocated In Frame calibrated=" + calibrated +
                ", mean=" + (calibrated ? (total / 30).ToString() : "UNAVAILABLE") + ", max=" + (calibrated ? maximum.ToString() : "UNAVAILABLE") +
                " bytes; includes engine/Editor/UI/simulation, not a per-renderer allocation claim.\n");
        }
        private IEnumerator IntegrationCases()
        {
            var adapter = host.GetComponent<ConnectionGameplayDisplay>();
            string[] ids = FragilityWitness.ConfigureFixture(host, 4, 1);
            FragilityWitness.Advance(host, .3f); yield return null;
            Frames("09-connecting");
            Check(host.FindConnection(ids[0]).IsConnecting && FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None)
                .All(v => !v.TerminalMarker.gameObject.activeSelf), "connecting route has no premature destination marker");
            Submit("kill " + ids[0]); Frames("10-interrupted-establishment");
            Check(host.FindConnection(ids[0]).TerminationPhase == Lifecycle.Connecting,
                "interrupted connecting departure preserves established prefix only");

            ids = FragilityWitness.ConfigureFixture(host, 0, 8);
            FragilityWitness.Advance(host, 3); yield return null; yield return null;
            Frames("11-crowded-paths");
            yield return Motion("crowded", 2);
            yield return ProfileGameplayFrames();
            Check(FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None).Length == 8 &&
                FindObjectsByType<ConnectionDisplayGraphic>(FindObjectsSortMode.None).Length == 8,
                "eight crowded logical paths use one shared mesh each");

            ids = FragilityWitness.ConfigureFixture(host, 4, 2); FragilityWitness.Advance(host, 3);
            var late = host.FindConnection(ids[0]);
            FragilityWitness.Advance(host, late.EstablishedAtSeconds + 27 - host.SimulationTimeSeconds);
            foreach (string id in ids.Skip(1)) Submit("kill " + id);
            host.StepSimulation(.02f); yield return null;
            Check(late.AtRisk && adapter.StateFor(late).productiveSpeed == 1 && adapter.StateFor(late).dangerSeverity > .9f,
                "full-speed endangered activity survives earlier lost time");
            Submit("inspect -v " + ids[0]); yield return null;
            Frames("17-at-speed-endangered");

            var route = late.Definition.Route.Select(r => r.Node).ToArray();
            FragilityWitness.ConfigureFixture(host, 4, 0);
            var zeroPlan = new ConnectionSpawnPlan(0, "z0", route, "visual-review", "Zero productive WORK review",
                new[] { new OperationStage(OperationKind.Read, 100, 5, "Read", behaviors: new[] { TimedModifierBehaviorDefinition.Rate("zero", 0, 0, 0) }) });
            var zero = host.RegisterConnection(LogicalConnectionDefinition.FromPlan(zeroPlan));
            FragilityWitness.Advance(host, 3); yield return null;
            Check(zero.Satisfaction > .99f && adapter.StateFor(zero).productiveSpeed == 0 && adapter.StateFor(zero).dangerSeverity < .7f,
                "fully allocated zero-WORK connection is stalled with recovery opportunity");
            Submit("inspect -v z0"); yield return null;
            Frames("18-zero-work-stall");
            FragilityWitness.ConfigureFixture(host, 4, 0);
            var arcPlan = new ConnectionSpawnPlan(0, "a0", route, "visual-review", "Operation progress review", new[] {
                new OperationStage(OperationKind.Read, 10, 5, "First"), new OperationStage(OperationKind.Write, 100, 5, "Second") });
            var arc = host.RegisterConnection(LogicalConnectionDefinition.FromPlan(arcPlan));
            FragilityWitness.Advance(host, route.Length * host.ConnectionTravelSecondsPerLink + .5f); yield return null;
            Submit("inspect -v a0"); yield return null;
            Frames("19-operation-before");
            while (arc.CurrentOperationIndex == 0) host.StepSimulation(.02f);
            host.StepSimulation(.02f); yield return null;
            Check(arc.CurrentOperationIndex == 1 && adapter.StateFor(arc).workProgress < .01f && arc.CurrentOperation == OperationKind.Write,
                "actual operation transition resets interior WORK fill and changes direction");
            Submit("inspect -v a0"); yield return null;
            Frames("20-operation-reset");

            FragilityWitness.ConfigureFixture(host, 0, 0);
            Submit("spawn basic-threat idhop"); FragilityWitness.Advance(host, 3); yield return null; yield return null;
            var hopping = host.RuntimeConnections.Single(c => c.IsActive);
            Check(hopping.KnownIdentity != hopping.ConnectionId && adapter.StateFor(hopping).identity == hopping.KnownIdentity,
                "adapter displays hopping alias, never concealed canonical identity");
            Frames("12-hopping-alias");
            var firstA = hopping.Definition.Route[0].Node; var firstB = hopping.Definition.Route[1].Node;
            var firstLink = host.PhysicalLinks.First(l => l.View.NodeA == firstA && l.View.NodeB == firstB ||
                l.View.NodeA == firstB && l.View.NodeB == firstA);
            Submit("probe " + firstLink.View.LinkId); yield return null; yield return null;
            Check(hopping.KnownIdentity == hopping.ConnectionId && adapter.StateFor(hopping).identity == hopping.KnownIdentity,
                "existing Probe reveals command identity through the same adapter");
            Frames("13-probe-revelation");
            Submit("payload db steady"); Submit("worm ext1 port-scanned");
            FragilityWitness.Advance(host, 1.1f); yield return null;
            Frames("14-existing-moving-attacks");
            Check(host.Payloads.Payloads.Count > 0 && host.Worms.Outbreaks.Any(o => o.Branches.Count > 0),
                "Payload and Worm retain independent presentation beside shared logical paths");

            // Real frame clock semantics, separate from deterministic motion sampling.
            FragilityWitness.ConfigureFixture(host, 4, 1); FragilityWitness.Advance(host, 3);
            double frozen = adapter.heartbeat.Beats; yield return new WaitForSecondsRealtime(.1f);
            Check(adapter.heartbeat.Beats == frozen, "disabled simulation host freezes cosmetic heartbeat");
            host.enabled = true; Time.timeScale = 0; frozen = adapter.heartbeat.Beats;
            yield return new WaitForSecondsRealtime(.12f);
            Check(adapter.heartbeat.Beats == frozen, "gameplay timeScale pause freezes heartbeat");
            Time.timeScale = 1; yield return new WaitForSecondsRealtime(.15f);
            Check(adapter.heartbeat.Beats > frozen, "ordinary simulation resumes shared heartbeat");
            Submit("infect db phantom-load 3"); Submit("clean db");
            frozen = adapter.heartbeat.Beats; yield return new WaitForSecondsRealtime(.15f);
            Check(FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include).gameObject.activeInHierarchy &&
                adapter.heartbeat.Beats > frozen, "CLEAN overlay leaves gameplay heartbeat running");
            host.enabled = false;
            FragilityWitness.ConfigureFixture(host, 4, 1); FragilityWitness.Advance(host, 3);
            FragilityWitness.Set(adapter.duel, "continueSimulationDuringDuel", false);
            Submit("duel db 0"); host.StepSimulation(.02f); host.enabled = true;
            yield return null; frozen = adapter.heartbeat.Beats;
            yield return new WaitForSecondsRealtime(.15f);
            Check(adapter.duel.PausesSimulation && adapter.heartbeat.Beats == frozen, "configured Duel pause freezes heartbeat");
            Frames("15-duel-pause");
            host.enabled = false; host.ResetSimulation();
            Check(adapter.heartbeat.Beats == 0, "actual Play Mode restart resets heartbeat");
            ids = FragilityWitness.ConfigureFixture(host, 4, 1); FragilityWitness.Advance(host, 3);
            foreach (var node in host.Nodes.Where(n => !n.View.NodeId.StartsWith("EXT", StringComparison.OrdinalIgnoreCase)))
            { host.DamageIntegrity(node, "review", "freeze"); host.DamageIntegrity(node, "review", "freeze"); }
            host.enabled = true;
            Check(host.RunEnded && adapter.Frozen && host.FindConnection(ids[0]).IsActive,
                "CORE freezes unresolved work without inventing resolution or progress");
            Frames("16-core-frozen");
            host.ResetSimulation(); input.text = "inspect "; input.caretPosition = input.text.Length;
            input.ActivateInputField(); yield return null;
            Check(input.isFocused && input.text == "inspect ", "prompt and draft remain available after overlay/reset review");
            FragilityWitness.ConfigureFixture(host, 4, 1); FragilityWitness.Advance(host, 3); yield return null;
            Color original = adapter.style.ordinaryRail;
            try { adapter.style.ordinaryRail = Color.white; Frames("21-shared-style-proof"); }
            finally { adapter.style.ordinaryRail = original; }
        }
        private void Submit(string command) => input.onSubmit.Invoke(command);
        private void Frames(string name)
        { foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) }) Frame(name, size.x, size.y); }
        private void Frame(string name, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture old = RenderTexture.active;
            RenderTexture oldTarget = cameraForCapture.targetTexture;
            try
            {
                cameraForCapture.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                Check(canvas.pixelRect.width == width && canvas.pixelRect.height == height, name + " placement/capture viewport " + width + "x" + height);
                FindFirstObjectByType<NetworkHealthPanel>().Render();
                FindFirstObjectByType<IntegrityPresenter>().RenderNow();
                FindFirstObjectByType<NodeStatusPresenter>().RenderNow(Time.unscaledTime, 1);
                var display = host.GetComponent<ConnectionGameplayDisplay>();
                var paths = FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None);
                if (display != null)
                {
                    if (paths.All(v => v.DisplayState.resolution == DisplayResolution.Active))
                        display.heartbeat.SetReviewTime(host.SimulationTimeSeconds);
                    var presenter = FindFirstObjectByType<LogicalPathPresenter>(); presenter.RefreshLayouts();
                    foreach (var view in paths)
                        view.AdvanceVisual(view.DisplayState.resolution == DisplayResolution.Active ? view.RelayoutSeconds : 0);
                    presenter.RefreshLayouts();
                }
                else FindFirstObjectByType<LogicalPathPresenter>().RefreshLayouts();
                FindFirstObjectByType<WormPresenter>().RenderNow(); FindFirstObjectByType<PayloadPresenter>().RenderNow();
                foreach (var window in new Component[] { FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include),
                    FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include) })
                    if (window.gameObject.activeInHierarchy)
                        Check(InViewport((RectTransform)window.transform), name + " " + window.GetType().Name + " fully inside network viewport");
                if (integration)
                    foreach (var pill in FindFirstObjectByType<IdentityPresenter>().GetComponentsInChildren<IdentityPillView>())
                        Check(InViewport((RectTransform)pill.transform), name + " packed identity stays in network overlay: " + pill.DisplayedId);
                Canvas.ForceUpdateCanvases(); cameraForCapture.Render(); RenderTexture.active = target;
                picture.ReadPixels(new Rect(0, 0, width, height), 0, 0); picture.Apply();
                File.WriteAllBytes(folder + "/" + name + "-" + width + "x" + height + ".png", picture.EncodeToPNG());
            }
            finally { RenderTexture.active = old; cameraForCapture.targetTexture = oldTarget; Canvas.ForceUpdateCanvases(); Destroy(picture); Destroy(target); }
        }
        private static bool InViewport(RectTransform window)
        {
            var viewport = (RectTransform)window.parent; var corners = new Vector3[4]; window.GetWorldCorners(corners);
            return corners.All(c =>
            {
                Vector3 point = viewport.InverseTransformPoint(c); Rect rect = viewport.rect;
                return point.x >= rect.xMin - 1 && point.x <= rect.xMax + 1 && point.y >= rect.yMin - 1 && point.y <= rect.yMax + 1;
            });
        }
    }
}
