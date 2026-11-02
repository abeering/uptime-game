using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class TextCommunicationWitness
    {
        static TextCommunicationWitness() { EditorApplication.update += Tick; }
        private static int frames;
        public static string Arg(string key, string fallback = null)
        {
            string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, key);
            return at < 0 ? fallback : args[at + 1];
        }
        [MenuItem("Tools/Connections/Run Text Communication Witness (Play Mode)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + Arg("-witnessScene", "Connections") + ".unity");
            SessionState.SetBool("TextCommunicationWitness.pending", true); frames = 0;
            EditorApplication.isPlaying = true;
        }
        private static void Tick()
        {
            if (!SessionState.GetBool("TextCommunicationWitness.pending", false) || !Application.isPlaying || ++frames < 30) return;
            SessionState.SetBool("TextCommunicationWitness.pending", false);
            new GameObject("Text communication checks").AddComponent<TextCommunicationChecks>().Begin();
        }
        internal static T Field<T>(object obj, string field) => EscapeVideoCallChecks.Field<T>(obj, field);
        internal static void Check(bool ok, string text)
        {
            if (!ok) throw new InvalidOperationException("TextCommunicationWitness: " + text);
            Debug.Log("TEXT CHECK: " + text);
        }
        [MenuItem("Tools/Connections/Check Text Communication Bindings")]
        public static void CheckBindings()
        {
            foreach (string scene in new[] { "Connections", "ConnectionsAdvanced" })
            {
                EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
                var director = UnityEngine.Object.FindFirstObjectByType<TextCommunicationDirector>();
                var placement = UnityEngine.Object.FindFirstObjectByType<ConnectionsWindowPlacement>();
                var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
                Check(director != null && director.transform.parent == placement.transform &&
                    Field<TextCommunicationDirector>(placement, "textCommunications") == director &&
                    Field<TextCommunicationDirector>(terminal, "textCommunications") == director, scene + " authored scene bindings");
                Check(Field<SimulationHost>(director,"simulationHost") != null &&
                    Field<NetworkHealthPanel>(director,"workstationClock") != null &&
                    Field<int>(director,"maxVisibleMessages") == 4, "host/clock and configurable four-message cap");
                Check(((RectTransform)director.transform).offsetMax.y == -64 &&
                    director.GetComponent<RectMask2D>() != null, "clipped stack below titlebar");
                var prefab = Field<TextCommunicationView>(director,"entryPrefab");
                Check(AssetDatabase.GetAssetPath(prefab) == "Assets/Prefabs/Connections/TextCommunicationEntry.prefab" &&
                    prefab.GetComponentsInChildren<Selectable>(true).Length == 0 &&
                    prefab.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget) &&
                    !prefab.GetComponent<CanvasGroup>().blocksRaycasts && !prefab.GetComponent<CanvasGroup>().interactable &&
                    !typeof(IEscapeTarget).IsAssignableFrom(typeof(TextCommunicationView)), "passive authored Connections prefab; no Escape target or mouse controls");
                Check(prefab.transform.Find("AccentRail") != null && prefab.transform.Find("Avatar") != null &&
                    prefab.GetComponentsInChildren<TMP_Text>(true).All(t => !t.richText), "rail, portrait and literal supplied text");
                EscapeVideoCallWitness.CheckBindings();
            }
            Debug.Log("TextCommunicationWitness BINDINGS PASS");
        }
    }

    public sealed class TextCommunicationChecks : MonoBehaviour
    {
        private TextCommunicationDirector director;
        private SimulationHost host;
        private Terminal terminal;
        private ConnectionsVideoCall call;
        private ConnectionsCleanOverlay clean;
        private EscapeCoordinator escape;
        private TMP_InputField command, pid;
        private Keyboard keyboard;
        private Canvas canvas;
        private Camera cameraForCapture, originalCamera;
        private RenderTexture target;
        private RenderMode originalMode;
        private float originalTimeScale;
        private double deadline;
        private static T Field<T>(object obj,string field) => TextCommunicationWitness.Field<T>(obj,field);
        private static void Check(bool ok,string text) => TextCommunicationWitness.Check(ok,text);
        private static WaitForSecondsRealtime Wait(float seconds = .3f) => new(seconds);
        public void Begin() { deadline = EditorApplication.timeSinceStartup + 90; originalTimeScale = Time.timeScale; StartCoroutine(Guard(Cases())); }
        private void Update() { if (EditorApplication.timeSinceStartup > deadline) Finish(new TimeoutException("Text communication witness timeout")); }
        private IEnumerator Guard(IEnumerator cases)
        {
            while (true)
            {
                object next;
                try { if (!cases.MoveNext()) break; next = cases.Current; }
                catch (Exception e) { Finish(e); yield break; }
                yield return next;
            }
            Finish(null);
        }
        private void Finish(Exception error)
        {
            enabled = false; Time.timeScale = originalTimeScale;
            if (canvas != null) { canvas.renderMode = originalMode; canvas.worldCamera = originalCamera; }
            if (cameraForCapture != null) Destroy(cameraForCapture.gameObject);
            if (target != null) Destroy(target);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (error == null) Debug.Log("TextCommunicationWitness PLAY PASS: " + gameObject.scene.name);
            else Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
        private void Command(string text) => command.onSubmit.Invoke(text);
        private static void Draft(TMP_InputField field, string text)
        {
            field.SetTextWithoutNotify(text); field.textComponent.ForceMeshUpdate();
            field.selectionStringAnchorPosition = 1; field.selectionStringFocusPosition = 3;
        }
        private static void Preserved(TMP_InputField field,string text)
        {
            Check(field.isFocused && EventSystem.current.currentSelectedGameObject == field.gameObject &&
                field.text == text && field.selectionStringAnchorPosition == 1 && field.selectionStringFocusPosition == 3,
                "arrival preserves typing owner, draft and selection: " + text);
        }
        private bool Push(string body, float lifetime = 20, TextCommunicationType type = TextCommunicationType.InstantMessage,
            Sprite avatar = null, string timestamp = "09:42") => director.PushMessage(new TextCommunication {
                Sender = type == TextCommunicationType.InstantMessage ? "Sarah Mitchell / demo" : "Network Monitor / demo",
                Body = body, Avatar = avatar, Type = type, Timestamp = timestamp, LifetimeSeconds = lifetime });
        private void StackFits()
        {
            var root = (RectTransform)director.transform;
            float previousBottom = 0;
            foreach (var entry in director.ActiveEntries)
            {
                Vector2 pos = entry.Rect.anchoredPosition;
                Check(Mathf.Abs(pos.x) < .01f && pos.y <= previousBottom + .01f &&
                    -pos.y + entry.Height <= root.rect.height + .01f && entry.Rect.rect.width <= root.rect.width + .01f,
                    "settled newest-first entries fit network viewport without overlap");
                previousBottom = pos.y - entry.Height;
            }
        }
        private IEnumerator Cases()
        {
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            director = UnityEngine.Object.FindFirstObjectByType<TextCommunicationDirector>();
            host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            call = UnityEngine.Object.FindFirstObjectByType<ConnectionsVideoCall>(FindObjectsInactive.Include);
            clean = UnityEngine.Object.FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
            escape = UnityEngine.Object.FindFirstObjectByType<EscapeCoordinator>();
            command = Field<TMP_InputField>(terminal,"commandInput"); pid = Field<TMP_InputField>(clean,"pidInput");
            canvas = escape.GetComponent<Canvas>(); originalMode = canvas.renderMode; originalCamera = canvas.worldCamera;
            cameraForCapture = new GameObject("Text witness camera",typeof(Camera)).GetComponent<Camera>();
            cameraForCapture.orthographic = true; cameraForCapture.transform.position = new(0,0,-100);
            cameraForCapture.clearFlags = CameraClearFlags.SolidColor; cameraForCapture.backgroundColor = new(.025f,.065f,.08f);
            cameraForCapture.nearClipPlane = .1f; cameraForCapture.farClipPlane = 1000;
            Vector2Int size = EscapeVideoCallWitness.RequestedSize(); target = new RenderTexture(size.x,size.y,24);
            cameraForCapture.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cameraForCapture; canvas.planeDistance = 10; Canvas.ForceUpdateCanvases();
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent(); yield return Wait();
            var network = (RectTransform)director.transform.parent; Rect geometry = network.rect;
            var nodes = host.Nodes.Select(n => n.View.transform.localPosition).ToArray();
            int links = UnityEngine.Object.FindObjectsByType<PhysicalLinkView>(FindObjectsSortMode.None).Length;
            Check(!director.PushMessage(null), "null delivery rejected");
            Check(director.LifetimeFor(new TextCommunication { Body = "Hi" }) >= 6 &&
                director.LifetimeFor(new TextCommunication { Body = new string('x',2000) }) == 16 &&
                director.LifetimeFor(new TextCommunication { LifetimeSeconds = .5f }) == .5f &&
                director.LifetimeFor(new TextCommunication { LifetimeSeconds = float.NaN }) >= 6,
                "bounded reading-time default and finite positive lifetime override");
            Draft(command,"inspect c1");
            // A supplied existing Sprite verifies the API; no character art is required.
            Sprite supplied = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Push("Developer sample: sale rehearsal in five minutes. Ready?", avatar: supplied);
            var im = director.ActiveEntries[0];
            Check(Field<TMP_Text>(im,"bodyText").maxVisibleCharacters == int.MaxValue &&
                Field<TMP_Text>(im,"bodyText").text.EndsWith("Ready?"), "entire body is assigned immediately; no typewriter");
            Check(Field<Image>(im,"avatarImage").sprite == supplied && Field<Image>(im,"avatarImage").enabled &&
                !Field<GameObject>(im,"avatarFallback").activeSelf && Field<Image>(im,"accentRail").color == Field<Color>(director,"instantMessageAccent"),
                "supplied avatar and authored teal rail");
            Check(Field<TMP_Text>(im,"timestampText").text == "09:42", "explicit display timestamp");
            yield return Wait(.08f);
            Check(im.GetComponent<CanvasGroup>().alpha > 0 && im.GetComponent<CanvasGroup>().alpha < 1, "brief arrival fade in progress");
            yield return Wait(); Preserved(command,"inspect c1"); Capture("im-avatar");
            Push("Developer sample: workstation service message. This is not a live condition.",type:TextCommunicationType.SystemNotification,timestamp:null);
            var system = director.ActiveEntries[0];
            Check(Field<GameObject>(system,"systemFallback").activeSelf &&
                !Field<GameObject>(system,"avatarFallback").activeSelf &&
                Field<Image>(system,"accentRail").color == Field<Color>(director,"systemNotificationAccent") &&
                Field<TMP_Text>(system,"timestampText").text == UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>().ClockText,
                "system monitor icon, authored amber rail and workstation-clock snapshot");
            Push("Developer sample: handoff notes.\nPlease check the rehearsal plan when you have a moment.");
            var fallback = director.ActiveEntries[0];
            Check(Field<GameObject>(fallback,"avatarFallback").activeSelf && !Field<Image>(fallback,"avatarImage").enabled,
                "missing avatar has readable neutral person fallback");
            string longText = "Developer sample: " + string.Join(" ",Enumerable.Repeat("The rehearsal plan includes a short briefing and time for questions.",4));
            Push(longText); yield return Wait();
            var body = Field<TMP_Text>(director.ActiveEntries[0],"bodyText"); body.ForceMeshUpdate();
            Check(!body.isTextTruncated && body.textInfo.lineCount > 3 && director.ActiveEntries[0].Height > im.Height,
                "short, multiline and long text dynamically size with complete readable content");
            StackFits(); Capture("mixed-stack");
            Push("Newest demonstration message.");
            Check(director.ActiveEntries.Count <= 4 && im.IsExiting, "fifth arrival gracefully retires oldest");
            Push("Arrival during an earlier departure."); yield return Wait(); StackFits();
            Check(director.ExitingCount == 0 && director.ActiveEntries.All(e => e.GetComponent<CanvasGroup>().alpha == 1),
                "arrival/reflow does not strand fades while older entries exit");
            director.Clear(); yield return null;
            Time.timeScale = 0; float simTime = host.SimulationTimeSeconds;
            Push("Unscaled expiry sample.", .15f); yield return Wait(.38f);
            Check(director.ActiveEntries.Count == 0 && director.ExitingCount == 1, "expiry starts departure while scaled time is stopped");
            yield return Wait();
            Check(director.transform.childCount == 0 && host.SimulationTimeSeconds == simTime, "unscaled departure cleans up without advancing simulation");
            Time.timeScale = originalTimeScale;
            for (int i=0;i<100;i++) Push("Burst demo " + i);
            Check(director.ActiveEntries.Count == 4 && director.ExitingCount <= 4, "rapid bursts bound live and departing entries");
            yield return Wait(); Check(director.transform.childCount == 4, "rapid-burst deferred destruction leaves four entries");
            director.Clear(); Command("notifytest im delay=1"); Command("notifytest system delay=1");
            Check(Field<Coroutine>(terminal,"delayedNotification") != null, "one pending developer delivery");
            Command("clean " + host.Nodes.Last().View.NodeId); yield return Wait(1.6f); Draft(pid,"5279");
            // Direct delivery after selection and a delayed delivery while PID owns focus.
            Command("notifytest system delay=1"); Vector3 cleanPosition = clean.transform.position;
            yield return Wait(1.2f); Preserved(pid,"5279");
            Check(clean.transform.position == cleanPosition && ReferenceEquals(escape.CurrentTarget,clean),
                "notifications preserve CLEAN position and Escape eligibility");
            call.TryStartSampleCall(); yield return Wait(.3f);
            Vector3 callPosition = call.transform.position; Push("Demo: incoming message during a call."); yield return Wait();
            Preserved(pid,"5279");
            Check(call.IsActive && ReferenceEquals(escape.CurrentTarget,call) &&
                clean.transform.position == cleanPosition && call.transform.position == callPosition,
                "call and CLEAN stay fixed when text arrives; Escape owner unchanged");
            Capture("windows-and-messages");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); yield return Wait(.1f);
            Check(!call.IsActive && clean.IsOpen && director.ActiveEntries.Count > 0, "fresh Escape hangs up call without dismissing text");
            yield return Wait(); Check(clean.IsOpen, "held Escape does not cascade");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return Wait(.05f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); yield return Wait(.1f);
            Check(!clean.IsOpen && director.ActiveEntries.Count > 0, "next fresh Escape cancels CLEAN without dismissing text");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return Wait();
            var placement = UnityEngine.Object.FindFirstObjectByType<ConnectionsWindowPlacement>();
            director.Clear(); call.TryStartSampleCall(); Vector3 emptyHome = call.transform.position; call.EndCall();
            Push("Placement demo: protect this actual message content."); yield return Wait();
            call.TryStartSampleCall();
            Check(ReadableOverlap((RectTransform)call.transform) <= OverlapAt((RectTransform)call.transform,emptyHome) + .01f,
                "new call prefers no greater actual readable-message overlap than its empty-stack home");
            call.EndCall();
            Command("notifytest im delay=1"); host.ResetSimulation(); yield return Wait(1.3f);
            Check(director.transform.childCount == 0 && Field<Coroutine>(terminal,"delayedNotification") == null,
                "simulation reset clears entries and cancels pending sample");
            foreach (string invalid in new[]{"notifytest", "notifytest unknown", "notifytest im delay=0", "notifytest im delay=31", "notifytest im delay=NaN", "notifytest im delay=Infinity", "notifytest im extra extra"}) Command(invalid);
            Check(director.ActiveEntries.Count == 0 && Field<Coroutine>(terminal,"delayedNotification") == null,
                "malformed developer commands create neither messages nor pending jobs");
            Command("notifytest im delay=1"); terminal.enabled = false; yield return Wait(1.3f); terminal.enabled = true;
            Check(director.ActiveEntries.Count == 0, "terminal disable cancels scheduled test");
            Push("Disable test."); director.enabled = false; yield return null;
            Check(director.transform.childCount == 0 && director.ExitingCount == 0 && !Push("Unavailable"), "component disable destroys entries and rejects delivery");
            director.enabled = true; Push("Parent disable test."); director.gameObject.SetActive(false); yield return null;
            director.gameObject.SetActive(true); Check(director.transform.childCount == 0, "scene/container disable leaves no old entries");
            yield return new WaitUntil(() => host.RuntimeConnections.Any(c => c.IsActive));
            Command("notifytest burst"); yield return Wait(.3f);
            Check(host.SimulationTimeSeconds > 0 && host.RuntimeConnections.Any(c => c.IsActive), "ordinary generated traffic and simulation continue with messages visible");
            StackFits(); Capture("generated-traffic");
            Check(network.rect == geometry && host.Nodes.Select((n,i) => n.View.transform.localPosition == nodes[i]).All(v=>v) &&
                UnityEngine.Object.FindObjectsByType<PhysicalLinkView>(FindObjectsSortMode.None).Length == links,
                "network viewport, node positions and physical topology unchanged");
            director.Clear(); yield return null;
            Check(director.transform.childCount == 0, "final teardown has no orphaned entries or notification coroutines");
        }
        private float ReadableOverlap(RectTransform window) => director.ReadableContent().Sum(r => Area(Bounds(window),Bounds(r)));
        private float OverlapAt(RectTransform window,Vector3 position)
        { Vector3 saved = window.position; window.position=position; float area=ReadableOverlap(window); window.position=saved; return area; }
        private Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 a = director.transform.parent.InverseTransformPoint(corners[0]);
            Vector2 b = director.transform.parent.InverseTransformPoint(corners[2]); return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
        }
        private static float Area(Rect a,Rect b) => Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
        private void Capture(string phase)
        {
            string path = TextCommunicationWitness.Arg("-capturePath"); if (path == null) return;
            Directory.CreateDirectory(path); Canvas.ForceUpdateCanvases(); cameraForCapture.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var png = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            png.ReadPixels(new Rect(0,0,png.width,png.height),0,0); png.Apply();
            File.WriteAllBytes(Path.Combine(path,gameObject.scene.name+"-"+phase+"-"+png.width+"x"+png.height+".png"),png.EncodeToPNG());
            RenderTexture.active = previous; Destroy(png);
        }
    }
}
