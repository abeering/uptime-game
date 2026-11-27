using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Uptime.Connections.Display;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad] public static class OperationRoutingCacheCapture
    {
        private const string Pending = "Uptime.OperationRoutingCacheCapture";
        static OperationRoutingCacheCapture() => EditorApplication.update += Launch;
        public static void Run()
        {
            EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        public static void RunTransitions() { SessionState.SetBool(Pending + ".Transitions", true); Run(); }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false); var runner = new GameObject("CACHE gameplay evidence").AddComponent<OperationRoutingCacheCaptureRunner>();
            runner.TransitionReview = SessionState.GetBool(Pending + ".Transitions", false); SessionState.SetBool(Pending + ".Transitions", false);
        }
    }
    public sealed class OperationRoutingCacheCaptureRunner : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<string> checks = new();
        public bool TransitionReview;
        private string Evidence => TransitionReview ? RouteTransitionWitness.Evidence : OperationRoutingCacheWitness.Evidence;
        private Keyboard keyboard;
        private ConnectionsSshOverlay ssh;
        private void Check(bool ok, string message) { checks.Add((ok ? "PASS " : "FAIL ") + message); if (!ok) throw new Exception(message); }
        private static void DynamicInputUpdate() => typeof(InputSystem).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic, null,
            new[] { typeof(InputUpdateType) }, null).Invoke(null, new object[] { InputUpdateType.Dynamic });
        private IEnumerator Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); DynamicInputUpdate(); keyboard.MakeCurrent();
            if (ssh != null && ssh.IsOpen) typeof(ConnectionsSshOverlay).GetMethod("Update", Private).Invoke(ssh, null); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); DynamicInputUpdate(); yield return null;
        }
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); yield return null;
            float oldScale = Time.timeScale; bool oldBackground = Application.runInBackground; Application.runInBackground = true;
            var oldFocus = InputSystem.settings.editorInputBehaviorInPlayMode; var oldBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            var capture = TransitionReview ? CaptureTransitions() : Capture(); Exception failure = null;
            while (true)
            {
                bool more; try { more = capture.MoveNext(); } catch (Exception e) { failure = e; break; }
                if (!more) break; yield return capture.Current;
            }
            (capture as IDisposable)?.Dispose(); InputSystem.RemoveDevice(keyboard); Time.timeScale = oldScale; Application.runInBackground = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldFocus; InputSystem.settings.backgroundBehavior = oldBehavior;
            Directory.CreateDirectory(Evidence); File.WriteAllLines(Evidence + "/graphical-checks.txt", checks);
            if (failure != null) Debug.LogException(failure); else Debug.Log("OperationRoutingCacheCapture PASS: " + checks.Count + " actual Play Mode checks.");
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1); else EditorApplication.isPlaying = false;
        }
        private static LogicalPathView View(LogicalConnectionState state) => FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None)
            .Single(v => ReferenceEquals(typeof(LogicalPathView).GetField("state", Private).GetValue(v), state));
        private static Rect Bounds(RectTransform area, RectTransform item)
        { var points = new Vector3[4]; item.GetWorldCorners(points); var a = area.InverseTransformPoint(points[0]); var b = area.InverseTransformPoint(points[2]); return Rect.MinMaxRect(a.x, a.y, b.x, b.y); }
        private IEnumerator Capture()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                if (SceneManager.GetActiveScene().path != scene) { EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single)); yield return null; }
                var h = FindFirstObjectByType<SimulationHost>(); h.enabled = false; Time.timeScale = 0;
                var traffic = h.GetComponent<ConnectionTrafficDirector>();
                Check(traffic.LegitimateWorkloadMultiplier == 2.5f && Math.Abs(traffic.LegitimateMeanArrivalSeconds - (scene == CanonicalSceneAuthoring.Scenes[0] ? 23.225807f : 20f)) < .001 && h.FindNodeResources("CACHE").LoadCapacity == 10, scene + " preserved authored workload/demand and CACHE10");
                var terminal = FindFirstObjectByType<Terminal>(); var input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                ssh = FindFirstObjectByType<ConnectionsSshOverlay>(FindObjectsInactive.Include);
                var canvas = terminal.GetComponentInParent<Canvas>(); var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
                var camera = new GameObject("CACHE evidence camera", typeof(Camera)).GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.025f, .065f, .08f); camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -100);
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                foreach (int width in new[] { 1920, 1280 })
                {
                    int height = width * 9 / 16; var viewport = new RenderTexture(width, height, 24); camera.targetTexture = viewport;
                    string prefix = h.gameObject.scene.name + "-" + width;
                    OperationRoutingCacheWitness.LoadBenefit(h); yield return null; yield return null;
                    Check(canvas.pixelRect.width == width && canvas.pixelRect.height == height, prefix + " native viewport");
                    var area = FindFirstObjectByType<LogicalPathPresenter>().NetworkArea;
                    foreach (var node in h.Nodes)
                        foreach (var other in h.Nodes)
                            if (node != other) Check(!Bounds(area, (RectTransform)node.View.transform).Overlaps(Bounds(area, (RectTransform)other.View.transform)), prefix + " authored node cards do not overlap: " + node.View.NodeId + "/" + other.View.NodeId);
                    input.onSubmit.Invoke("ssh cache"); yield return null; yield return Press();
                    Check(ssh.IsOpen && !input.interactable && EventSystem.current.currentSelectedGameObject == ssh.gameObject, prefix + " ssh cache reuses shared authored window and owns input");
                    int index = h.CachePurposeChoices.ToList().FindIndex(p => p.id == "product-lookup") + 1;
                    for (int i = 0; i < index; i++) yield return Press(Key.DownArrow); yield return Press(Key.Enter);
                    Check(h.CachePurposeId == "product-lookup" && h.ApiFavoredPurposeId == "", prefix + " keyboard CACHE selection independent of API slot");
                    float before = h.SimulationTimeSeconds; h.enabled = true; Time.timeScale = 1; yield return new WaitForSecondsRealtime(.12f); Time.timeScale = 0;
                    Check(h.SimulationTimeSeconds > before, prefix + " simulation continues during CACHE configuration");
                    yield return null; Canvas.ForceUpdateCanvases();
                    foreach (var label in ssh.GetComponentsInChildren<TMP_Text>()) Check(label.preferredWidth <= label.rectTransform.rect.width + .1f && label.preferredHeight <= label.rectTransform.rect.height + .1f, prefix + " SSH text fits: " + label.name);
                    var duelDirector = h.GetComponent<ConnectionsDuelDirector>(); var duel = FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
                    Check(duelDirector.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "cache-focus", out _) && duel.IsOpen && ssh.IsOpen, prefix + " Duel preempts CACHE SSH without closing it");
                    yield return Press(Key.UpArrow, Key.Enter); Check(h.CachePurposeId == "product-lookup", prefix + " CACHE ignores Duel keystrokes");
                    duelDirector.ResetDuel(); yield return null; Check(ssh.IsOpen && EventSystem.current.currentSelectedGameObject == ssh.gameObject && !input.interactable, prefix + " Duel abort restores CACHE focus");
                    Save(camera, viewport, prefix + "-cache-policy"); yield return new WaitForSecondsRealtime(.18f); yield return Press(Key.Escape); yield return null; yield return null;
                    Check(!ssh.IsOpen && input.isFocused && input.interactable && h.CachePurposeId == "product-lookup", prefix + " Escape returns terminal focus and preserves CACHE slot");
                    OperationRoutingCacheWitness.Until(h, () => h.RuntimeConnections.Count(c => c.IsActive) == 2);
                    var product = h.FindConnection(OperationRoutingCacheWitness.FixtureProduct); var sales = h.FindConnection(OperationRoutingCacheWitness.FixtureSales);
                    Check(product.IsCached && product.TargetNode.NodeId == "CACHE" && product.CurrentTotalWork == 20 && product.DesiredLoad == 4 && product.AllocatedLoad == 4 && sales.TargetNode.NodeId == "DB" && sales.AllocatedLoad == 8 && product.BusinessWorkValue == 32 && product.ServiceBudgetSeconds == 15, prefix + " actual generated efficiency relieves DB without changing value/budget");
                    input.onSubmit.Invoke("inspect -v " + product.ConnectionId + " " + sales.ConnectionId); yield return null; yield return null;
                    Check(FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("CACHE · Product")) && FindObjectsByType<InspectOperationToken>(FindObjectsSortMode.None).Any(t => t.Text.Contains("READ → CACHE")), prefix + " inspect explains cached identity, actual destination and requirements");
                    Save(camera, viewport, prefix + "-cache-allocation"); h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds);
                    File.WriteAllText(OperationRoutingCacheWitness.Evidence + "/" + prefix + "-allocation.json", JsonUtility.ToJson(h.Recorder.CurrentLog.snapshots.Last(), true));
                    h.StepSimulation(40); yield return null; yield return null;
                    Check(h.Ending == RunEnding.ShiftComplete && h.CreditedWork == 64 && h.WorkCreditCount == 2 && h.LegitimateFailureCount == 0, prefix + " actual CACHE shift delivers unchanged64 WORK in two whole credits");
                    if (width == 1280) Save(camera, viewport, prefix + "-cache-outcome");
                    // A mixed route fixture uses WEB's short bypass in Advanced to exercise different segment counts.
                    string target = scene == CanonicalSceneAuthoring.Scenes[0] ? "DB" : "WEB";
                    var mixed = OperationRoutingCacheWitness.LoadMixed(h);
                    terminal.AppendText("ROUTE FIXTURE / r0 / READ CACHE → WRITE " + target, new Color(.64f, .94f, .81f));
                    OperationRoutingCacheWitness.Until(h, () => mixed.IsActive); yield return null; yield return null;
                    input.onSubmit.Invoke("inspect -v " + mixed.ConnectionId); yield return null; yield return null;
                    var view = View(mixed); var marker = view.TerminalMarker; int viewId = view.GetInstanceID();
                    Check(view.UsesSharedDisplay && marker != null && mixed.IsCached, prefix + " mixed READ uses existing shared rail/pill/socket");
                    Save(camera, viewport, prefix + "-mixed-read-cache");
                    h.TrySetCachePurpose(null, out _); Check(mixed.IsCached, prefix + " clearing does not reprice/reroute committed READ");
                    OperationRoutingCacheWitness.Until(h, () => mixed.CurrentOperationIndex == 1 && mixed.Routing != null && mixed.IsActive); yield return new WaitForSecondsRealtime(.25f);
                    Check(FindObjectsByType<InspectOperationToken>(FindObjectsSortMode.None).Any(t => t.Text.Contains("WRITE → " + target)), prefix + " live inspect refreshes to committed WRITE destination");
                    Save(camera, viewport, prefix + "-mixed-write-ordinary");
                    Check(mixed.TargetNode.NodeId == target && !mixed.IsCached && View(mixed).GetInstanceID() == viewId && View(mixed).TerminalMarker == marker && mixed.KnownIdentity == "r0", prefix + " mixed WRITE changes actual route while retaining the same view, socket and connection identity");
                    Check(mixed.Route.Length == (target == "WEB" ? 2 : 3) && mixed.OperationDestinationAt(0).NodeId == "CACHE" && mixed.ServiceBudgetSeconds == 45 && h.CreditedWork == 0, prefix + " route segment count/history, original budget, no partial credit");
                    input.onSubmit.Invoke("ssh api"); yield return null; yield return Press();
                    Check(ssh.IsOpen && h.CachePurposeId == "" && h.ApiFavoredPurposeId == "", prefix + " shared SSH still opens independent API configuration");
                    int apiIndex = h.ApiPurposeChoices.ToList().FindIndex(p => p.id == "product-lookup") + 1;
                    for (int i = 0; i < apiIndex; i++) yield return Press(Key.DownArrow); yield return Press(Key.Enter);
                    Check(h.ApiFavoredPurposeId == "product-lookup" && h.CachePurposeId == "", prefix + " API keyboard policy remains independent of CACHE");
                    yield return new WaitForSecondsRealtime(.18f); yield return Press(Key.Escape); yield return null; yield return null;
                    Check(input.isFocused && !ssh.IsOpen, prefix + " API Escape restores focus after mixed routing");
                    h.Recorder.Save(false);
                    var routeEvents = h.Recorder.CurrentLog.events.Where(e => e.type == "operation_route_committed").ToArray();
                    File.WriteAllText(OperationRoutingCacheWitness.Evidence + "/" + prefix + "-mixed-routes.json", JsonUtility.ToJson(new CacheRoutingEvidence { events = routeEvents }, true));
                    if (width == 1280)
                    {
                        OperationRoutingCacheWitness.LoadBenefit(h); h.StepSimulation(40); yield return null; yield return null;
                        Check(h.Ending == RunEnding.Fired && h.CreditedWork == 0 && h.LegitimateFailureCount == 2, prefix + " actual empty CACHE comparison loses both jobs"); Save(camera, viewport, prefix + "-empty-outcome");
                    }
                    h.ResetSimulation(); Check(h.CachePurposeId == "" && h.ApiFavoredPurposeId == "", prefix + " restart clears both independent slots");
                    camera.targetTexture = null; Destroy(viewport);
                }
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; Destroy(camera.gameObject); yield return null;
            }
        }
        private IEnumerator CaptureTransitions()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                if (SceneManager.GetActiveScene().path != scene) { EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single)); yield return null; }
                var h = FindFirstObjectByType<SimulationHost>(); h.enabled = false; Time.timeScale = 0;
                var terminal = FindFirstObjectByType<Terminal>(); var input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                var canvas = terminal.GetComponentInParent<Canvas>(); var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
                var camera = new GameObject("Route transition evidence camera", typeof(Camera)).GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.025f, .065f, .08f); camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -100);
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                foreach (int width in new[] { 1920, 1280 })
                {
                    var viewport = new RenderTexture(width, width * 9 / 16, 24); camera.targetTexture = viewport; yield return null; yield return null;
                    foreach (string shape in width == 1280 ? new[] { "divergent", "shorter", "longer" } : new[] { "divergent" })
                    {
                        Time.timeScale = 0; var model = OperationRoutingCacheWitness.LoadMixed(h, 8, "DB");
                        if (shape != "divergent")
                        {
                            h.ResetSimulation(); h.Recorder.BeginRun(); model = RouteTransitionWitness.Arc(h, shape == "shorter" ? "CACHE" : "API", shape == "shorter" ? "API" : "CACHE", firstWork: 4);
                        }
                        RouteTransitionWitness.Until(h, () => model.IsActive); yield return null; yield return null;
                        var view = View(model); var marker = view.TerminalMarker; int id = view.GetInstanceID(); var graphic = view.SharedDisplay.path; var heartbeat = FindFirstObjectByType<ConnectionGameplayDisplay>().heartbeat;
                        var originalColor = view.DisplayAccent;
                        input.onSubmit.Invoke("inspect -v r0"); yield return null; yield return null;
                        string prefix = h.gameObject.scene.name + "-" + width + "-" + shape;
                        Save(camera, viewport, prefix + "-before", visualDelta: 0);
                        h.enabled = true; Time.timeScale = 1;
                        string directory = "/tmp/uptime-reroute-frames/" + prefix; var frames = new List<RouteMotionFrame>();
                        bool retracting = false, extending = false, sawRerouting = false, inspectRerouting = false, prefixPreserved = true;
                        for (int frame = 0; frame < 55; frame++)
                        {
                            Save(camera, viewport, frame.ToString("0000"), directory, 0);
                            if (model.RouteRevision > 1)
                            {
                                sawRerouting |= model.IsRerouting;
                                if (model.IsRerouting) Check(model.CompletedWork == 0 && model.AllocatedLoad == 0, prefix + " no productive WORK/allocation while REROUTING frame" + frame);
                                bool firstFull = view.TryGetVisualSegment(0, out _, out _, out float fraction, out _) && fraction >= .999f;
                                prefixPreserved &= firstFull;
                                if (view.IsRetractingRoute && !retracting) { retracting = true; Save(camera, viewport, prefix + "-retract", visualDelta: 0); }
                                if (!view.IsRetractingRoute && model.IsRerouting && !extending) { extending = true; Save(camera, viewport, prefix + "-extend", visualDelta: 0); }
                                inspectRerouting |= FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("REROUTING"));
                            }
                            frames.Add(new RouteMotionFrame { time = h.SimulationTimeSeconds, lifecycle = model.Lifecycle.ToString(), operation = model.CurrentOperationIndex,
                                target = model.TargetNode.NodeId, visibleTarget = view.PresentationRoute.Last().Node.NodeId, reveal = view.VisibleRouteFraction,
                                retracting = view.IsRetractingRoute, work = model.CompletedWork, allocated = model.AllocatedLoad });
                            yield return new WaitForSecondsRealtime(.05f);
                        }
                        Time.timeScale = 0;
                        Check(model.RouteRevision == 2 && model.IsActive && model.KnownIdentity == "r0" && View(model).GetInstanceID() == id && View(model).TerminalMarker == marker && View(model).SharedDisplay.path == graphic && FindFirstObjectByType<ConnectionGameplayDisplay>().heartbeat == heartbeat, prefix + " same model/identity/view/socket/shared renderer/heartbeat after transition");
                        Check(view.DisplayAccent == originalColor && prefixPreserved, prefix + " identity color and established prefix remain continuous");
                        Check(shape == "longer" ? !retracting && extending && sawRerouting && inspectRerouting : retracting && (shape == "shorter" || extending && sawRerouting && inspectRerouting), prefix + " actual retraction/extension and visible REROUTING inspection");
                        Check(shape == "shorter" ? model.RouteTransition.TravelSeconds == 0 : model.RouteTransition.TravelSeconds == .6f, prefix + " only new suffix traversal charged");
                        Save(camera, viewport, prefix + "-after", visualDelta: 0);
                        Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence + "/" + prefix + "-motion.json", JsonUtility.ToJson(new RouteMotionEvidence { frames = frames.ToArray() }, true));
                    }
                    camera.targetTexture = null; Destroy(viewport);
                }
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; Destroy(camera.gameObject); yield return null;
            }
        }

        private void Save(Camera camera, RenderTexture viewport, string name, string frameDirectory = null, float visualDelta = .2f)
        {
            Canvas.ForceUpdateCanvases(); FindFirstObjectByType<NetworkHealthPanel>().Render(); var paths = FindFirstObjectByType<LogicalPathPresenter>(); paths.RefreshLayouts(); paths.RefreshSocketSizes();
            foreach (var view in FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None)) view.AdvanceVisual(visualDelta);
            paths.RefreshLayouts(); paths.RefreshSocketSizes(); Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = viewport; var picture = new Texture2D(viewport.width, viewport.height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, viewport.width, viewport.height), 0, 0); picture.Apply(); string directory = frameDirectory ?? Evidence + "/Captures"; Directory.CreateDirectory(directory);
            File.WriteAllBytes(directory + "/" + name + ".png", picture.EncodeToPNG()); RenderTexture.active = previous; Destroy(picture);
        }
    }
    [Serializable] public sealed class RouteMotionFrame
    { public float time, reveal, work, allocated; public int operation; public string lifecycle, target, visibleTarget; public bool retracting; }
    [Serializable] public sealed class RouteMotionEvidence { public RouteMotionFrame[] frames; }
    [Serializable] public sealed class CacheRoutingEvidence { public RunEvent[] events; }
}
