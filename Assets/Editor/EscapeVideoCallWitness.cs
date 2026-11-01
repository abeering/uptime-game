using System;
using System.Collections;
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
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class EscapeVideoCallWitness
    {
        private static int frames;
        static EscapeVideoCallWitness() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Connections/Run Escape and Video Call Witness (Play Mode)")]
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-witnessScene");
            string scene = at >= 0 ? args[at + 1] : "Connections";
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            SessionState.SetBool("EscapeVideoCallWitness.pending", true);
            frames = 0;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool("EscapeVideoCallWitness.pending", false) || !Application.isPlaying || ++frames < 30) return;
            SessionState.SetBool("EscapeVideoCallWitness.pending", false);
            new GameObject("Escape/video call witness").AddComponent<EscapeVideoCallChecks>().Begin();
        }

        public static void CheckBindings()
        {
            var coordinator = UnityEngine.Object.FindFirstObjectByType<EscapeCoordinator>();
            var clean = UnityEngine.Object.FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
            var call = UnityEngine.Object.FindFirstObjectByType<ConnectionsVideoCall>(FindObjectsInactive.Include);
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            Check(coordinator != null && clean != null && call != null && terminal != null &&
                UnityEngine.Object.FindObjectsByType<EscapeCoordinator>(FindObjectsSortMode.None).Length == 1,
                "one coordinator and both authored windows");
            foreach (var window in new UnityEngine.Object[] { clean, call })
                Check(new SerializedObject(window).FindProperty("escapeCoordinator").objectReferenceValue == coordinator,
                    "window scene coordinator binding");
            Check(new SerializedObject(terminal).FindProperty("videoCall").objectReferenceValue == call,
                "terminal developer-call binding");
            Check(EscapeVideoCallChecks.Field<TMP_InputField>(terminal, "commandInput") is EscapeSafeInputField &&
                EscapeVideoCallChecks.Field<TMP_InputField>(clean, "pidInput") is EscapeSafeInputField,
                "both typing fields reserve native Escape");
            Check(!call.gameObject.activeSelf && call.transform.parent == clean.transform.parent &&
                ((RectTransform)call.transform).sizeDelta == new Vector2(560, 360),
                "inactive compact call has an authored network position");
            foreach (string part in new[] { "CallTitlebar", "CallWindowFrame" })
                Check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(call.transform.Find(part).gameObject) ==
                    "Assets/Prefabs/Connections/" + (part == "CallTitlebar" ? "DockedTitlebar" : "DockedWindowFrame") + ".prefab",
                    "call uses shared authored chrome " + part);
            Check(!call.transform.Find("CallTitlebar/Pinned").gameObject.activeSelf &&
                call.GetComponentsInChildren<Selectable>(true).Length == 0 &&
                call.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget),
                "call has no clickable controls or raycast surface");
            Debug.Log("EscapeVideoCallWitness bindings PASS: " + terminal.gameObject.scene.name);
        }

        internal static void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException("EscapeVideoCallWitness: " + message);
            Debug.Log("ESC/CALL CHECK: " + message);
        }
    }

    public sealed class EscapeVideoCallChecks : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private EscapeCoordinator coordinator;
        private ConnectionsVideoCall call;
        private ConnectionsCleanOverlay clean;
        private SimulationHost host;
        private Terminal terminal;
        private TMP_InputField command, pid;
        private Keyboard keyboard;
        private string capturePath;
        private float originalScale;
        private double deadline;
        public static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private).GetValue(obj);
        private static void Check(bool ok, string text) => EscapeVideoCallWitness.Check(ok, text);
        private static WaitForSecondsRealtime Wait(float seconds = .2f) => new(seconds);
        private GameObject CleanHint => Field<GameObject>(clean, "cancelHint");
        private GameObject CallHint => Field<GameObject>(call, "hangUpHint");
        private void Command(string text) => command.onSubmit.Invoke(text);
        private void Native(TMP_InputField input, KeyCode key, char character = '\0')
        {
            // Exercise the native IMGUI event path actually consumed by TMP. Only
            // this Editor witness uses Unity's internal event-injection API.
            typeof(Event).GetMethod("QueueEvent", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new Event { type = EventType.KeyDown, keyCode = key, character = character } });
            input.OnUpdateSelected(new BaseEventData(EventSystem.current));
        }
        private void Press(TMP_InputField input)
        {
            Native(input, KeyCode.Escape);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
        }
        private void Release() => InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        private void Owners(IEscapeTarget target)
        {
            Check(ReferenceEquals(coordinator.CurrentTarget, target) &&
                CleanHint.activeInHierarchy == ReferenceEquals(target, clean) &&
                CallHint.activeInHierarchy == ReferenceEquals(target, call),
                "LIFO target has the only active Escape affordance");
        }
        private void Draft(TMP_InputField input, string value)
        {
            input.SetTextWithoutNotify(value); input.textComponent.ForceMeshUpdate();
            input.selectionStringAnchorPosition = 1; input.selectionStringFocusPosition = 2;
        }
        private void Preserved(TMP_InputField input, string value)
        {
            Check(input.isFocused && EventSystem.current.currentSelectedGameObject == input.gameObject &&
                input.text == value && input.selectionStringAnchorPosition == 1 && input.selectionStringFocusPosition == 2,
                $"typing focus, draft and selection survive unrelated Escape ownership: {input.GetType().Name}, focused={input.isFocused}, selected={EventSystem.current.currentSelectedGameObject == input.gameObject}, text={input.text}, selection={input.selectionStringAnchorPosition}/{input.selectionStringFocusPosition}");
        }
        public void Begin()
        {
            deadline = EditorApplication.timeSinceStartup + 90;
            originalScale = Time.timeScale;
            StartCoroutine(Guard(Cases()));
        }
        private void Update()
        {
            if (EditorApplication.timeSinceStartup > deadline) Finish(new TimeoutException("Escape/video call witness timed out"));
        }
        private IEnumerator Guard(IEnumerator routine)
        {
            while (true)
            {
                object next;
                try { if (!routine.MoveNext()) break; next = routine.Current; }
                catch (Exception error) { Finish(error); yield break; }
                yield return next;
            }
            Finish(null);
        }
        private void Finish(Exception error)
        {
            enabled = false; Time.timeScale = originalScale;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (error != null) Debug.LogException(error);
            else Debug.Log("EscapeVideoCallWitness PLAY PASS: " + gameObject.scene.name);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
        private IEnumerator Cases()
        {
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            coordinator = UnityEngine.Object.FindFirstObjectByType<EscapeCoordinator>();
            call = UnityEngine.Object.FindFirstObjectByType<ConnectionsVideoCall>(FindObjectsInactive.Include);
            clean = UnityEngine.Object.FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
            host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            command = Field<TMP_InputField>(terminal, "commandInput"); pid = Field<TMP_InputField>(clean, "pidInput");
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-capturePath");
            capturePath = at >= 0 ? args[at + 1] : null;
            UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>().AutoGenerationEnabled = false;
            host.ResetSimulation();
            var network = (RectTransform)clean.transform.parent;
            Rect networkRect = network.rect;
            Vector2 networkMin = network.offsetMin, networkMax = network.offsetMax;
            Vector3[] nodePositions = host.Nodes.Select(n => n.View.transform.localPosition).ToArray();
            Check(command.isFocused, "startup terminal focus");
            Draft(command, "draft");
            Check(call.TryStartSampleCall(), "sample starts through public developer entry point");
            Check(!call.TryStartSampleCall() && !call.TryStartCall(null), "duplicate/null requests leave active call untouched");
            Owners(call); Preserved(command, "draft");
            float clock = host.SimulationTimeSeconds;
            yield return Wait(.8f);
            var body = Field<TMP_Text>(call, "bodyText");
            Check(call.IsTyping && body.maxVisibleCharacters > 0 && body.maxVisibleCharacters < body.textInfo.characterCount &&
                host.SimulationTimeSeconds > clock, "unscaled typewriter advances while network simulation continues");
            Capture("call-alone");
            Preserved(command, "draft");
            Press(command); yield return Wait(.1f);
            Check(!call.IsActive, "Escape hangs up during typing rather than skipping subtitles");
            Preserved(command, "draft"); Owners(null);
            Release(); yield return Wait(.05f);
            Native(command, KeyCode.X, 'x');
            Check(command.text == "dxaft", "native character input replaces selection after Escape");
            Native(command, KeyCode.LeftArrow); Native(command, KeyCode.Backspace);
            Check(command.text == "xaft", "native arrow and Backspace editing");
            command.SetTextWithoutNotify("help");
            Native(command, KeyCode.Return, '\n'); yield return Wait(.1f);
            Check(command.isFocused && command.text == "", "native editing, Enter and terminal focus restoration remain usable");

            Command("infect DB phantom-load"); Command("clean DB");
            yield return new WaitUntil(() => pid.gameObject.activeInHierarchy && pid.isFocused);
            Owners(clean); Capture("clean-alone"); Draft(pid, "527");
            Check(call.TryStartSampleCall(), "call can start programmatically while CLEAN owns typing");
            Owners(call); Preserved(pid, "527"); yield return Wait(.5f); Capture("call-over-clean");
            yield return Wait(); Press(pid); yield return Wait(.25f);
            Owners(clean); Check(clean.IsOpen && !call.IsActive, "first Escape hangs up and holding does not cascade into CLEAN");
            Preserved(pid, "527");
            Release(); yield return Wait(.05f); Press(pid); yield return Wait(.1f);
            Check(!clean.IsOpen && host.ActiveClean == null && command.isFocused, "next fresh Escape cancels CLEAN and restores terminal typing");
            Release(); yield return Wait(.05f);

            call.TryStartSampleCall(); Command("clean DB");
            yield return new WaitUntil(() => pid.gameObject.activeInHierarchy && pid.isFocused);
            Owners(clean); Capture("clean-over-call");
            Press(pid); yield return Wait(.25f);
            Check(call.IsActive && !clean.IsOpen && command.isFocused, "reverse order cancels CLEAN first without hanging up the older call");
            Owners(call); Release(); yield return Wait(.05f); Press(command); yield return Wait(.1f);
            Check(!call.IsActive, "next fresh press hangs up the older call");
            Release(); yield return Wait(.05f);

            Command("clean DB"); yield return new WaitUntil(() => pid.gameObject.activeInHierarchy && pid.isFocused);
            call.TryStartSampleCall(); Press(pid); yield return Wait(.25f);
            Check(call.IsActive && clean.IsOpen, "arrival protection consumes a fresh press without fallthrough or delayed replay");
            Release(); yield return Wait(.05f);
            call.EndCall(); Press(pid); yield return Wait(.25f);
            Check(clean.IsOpen, "automatic removal protects the newly exposed CLEAN target even after held grace expiry");
            Owners(clean); Release(); yield return Wait(.05f); Press(pid); yield return Wait(.1f);
            Check(!clean.IsOpen, "fresh press after protection dismisses normally");
            Release(); yield return Wait(.05f);

            call.TryStartSampleCall(); Command("clean DB");
            yield return new WaitUntil(() => pid.gameObject.activeInHierarchy && pid.isFocused);
            int malicious = host.ActiveClean.Processes.Single(p => p.CpuPercent >= 82).Pid;
            pid.onSubmit.Invoke(malicious.ToString());
            Check(clean.IsOpen && !clean.CanDismissWithEscape && !CleanHint.activeSelf && host.Infections.Count == 0,
                "CLEAN result leaves the stack and retains successful removal");
            Owners(call); Capture("clean-result-with-call");
            yield return Wait(); Press(command); yield return Wait(.1f);
            Check(!call.IsActive && clean.IsOpen, "Escape can hang up the call while CLEAN results remain noncancelable");
            Release(); yield return new WaitUntil(() => !clean.IsOpen); yield return Wait(.1f);
            Check(command.isFocused, "CLEAN result closes and restores typing after its existing hold");

            // All non-Escape exit paths must release ownership too.
            Command("clean DB"); yield return Wait(.25f); call.TryStartSampleCall();
            call.gameObject.SetActive(false); Owners(clean);
            call.TryStartSampleCall(); call.enabled = false; Owners(clean);
            Check(!call.gameObject.activeSelf, "disabling a call component also hides its window"); call.enabled = true;
            call.TryStartSampleCall(); call.EndCall(); call.EndCall(); Owners(clean);
            clean.gameObject.SetActive(false); yield return Wait(.1f);
            Check(host.ActiveClean == null && command.isFocused, "external CLEAN disable releases session, ownership and focus"); Owners(null);
            Command("clean DB"); yield return Wait(.25f); host.ResetSimulation(); yield return Wait(.1f);
            Check(!clean.IsOpen && command.isFocused, "simulation reset deregisters CLEAN without orphaning input"); Owners(null);
            var clone = Instantiate(call, call.transform.parent);
            Check(clone.TryStartSampleCall(), "temporary call instance starts for destruction check");
            Destroy(clone.gameObject); yield return Wait(.1f); Owners(null);

            var portrait = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Sprites/circle copy 128.png").OfType<Sprite>().First();
            var animation = new UnityEditor.Animations.AnimatorController();
            animation.AddParameter("IsTalking", AnimatorControllerParameterType.Bool);
            animation.AddLayer("Portrait");
            animation.layers[0].stateMachine.AddState("Idle");
            Check(call.TryStartCall(new ConnectionsVideoCallData { CallerName = "PORTRAIT TEST",
                Body = "Short subtitle test.", Portrait = portrait, PortraitAnimation = animation }),
                "caller content accepts a sprite and optional animation controller");
            yield return Wait(.1f);
            var animator = Field<Animator>(call, "portraitAnimator");
            Check(Field<Image>(call, "portraitImage").sprite == portrait &&
                !Field<GameObject>(call, "portraitFallback").activeSelf &&
                Field<TMP_Text>(call, "callerText").text == "CALL / PORTRAIT TEST" && animator.GetBool("IsTalking"),
                "dynamic identity, portrait and talking hook bind without typing focus changes");
            yield return new WaitUntil(() => !call.IsTyping);
            Check(body.maxVisibleCharacters == body.textInfo.characterCount && !animator.GetBool("IsTalking"),
                "complete subtitle and talking-stop transition");
            call.EndCall(); Destroy(animation);
            var tuning = new SerializedObject(call);
            tuning.FindProperty("charactersPerSecond").floatValue = 10000;
            tuning.ApplyModifiedPropertiesWithoutUndo();
            call.TryStartCall(new ConnectionsVideoCallData { CallerName = new string('L', 100),
                Body = string.Join(" ", Enumerable.Repeat("Long subtitle test sentence.", 80)) });
            yield return new WaitUntil(() => !call.IsTyping);
            Check(body.textInfo.pageCount > 1 && body.pageToDisplay == body.textInfo.pageCount &&
                Field<TMP_Text>(call, "callerText").overflowMode == TextOverflowModes.Ellipsis,
                "long dialogue follows its current subtitle page and long caller names keep the hint clear");
            call.EndCall();
            tuning.FindProperty("charactersPerSecond").floatValue = 45;
            tuning.ApplyModifiedPropertiesWithoutUndo();

            // Protection runs in real time even if simulation time is stopped by a test.
            Time.timeScale = 0f; call.TryStartSampleCall();
            yield return Wait(.2f); coordinator.enabled = false;
            Check(!CallHint.activeSelf, "disabled coordinator hides its affordance");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); yield return Wait(.05f);
            coordinator.enabled = true; yield return Wait(.25f);
            Check(call.IsActive, "re-enabling during a held Escape does not manufacture a press");
            Release(); yield return Wait(.05f); Press(command); yield return Wait(.1f);
            Check(!call.IsActive, "unscaled protection expires and fresh Escape still works at timeScale zero");
            Release(); Time.timeScale = originalScale; Owners(null);
            Draft(command, "unchanged"); Press(command); yield return Wait(.1f);
            Preserved(command, "unchanged"); Release();
            Check(!call.IsTyping, "all call exit paths stop subtitle playback");
            Check(network.rect == networkRect && network.offsetMin == networkMin && network.offsetMax == networkMax &&
                host.Nodes.Select((n, i) => Vector3.Distance(n.View.transform.localPosition, nodePositions[i]) < .001f).All(x => x),
                "call/CLEAN lifecycles preserve network geometry and node positions");
        }

        private void Capture(string phase)
        {
            if (string.IsNullOrEmpty(capturePath)) return;
            Directory.CreateDirectory(capturePath);
            foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720), new Vector2Int(1577,887) })
            {
                Canvas canvas = coordinator.GetComponent<Canvas>(); var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
                var camera = new GameObject("Witness camera", typeof(Camera)).GetComponent<Camera>();
                camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.025f,.065f,.08f); camera.transform.position = new Vector3(0,0,-100);
                camera.nearClipPlane = .1f; camera.farClipPlane = 1000;
                var target = new RenderTexture(size.x,size.y,24); camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                Canvas.ForceUpdateCanvases(); camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
                var png = new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                png.ReadPixels(new Rect(0,0,size.x,size.y),0,0); png.Apply();
                File.WriteAllBytes(Path.Combine(capturePath, gameObject.scene.name+"-"+phase+"-"+size.x+"x"+size.y+".png"), png.EncodeToPNG());
                RenderTexture.active = previous; canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
                Destroy(camera.gameObject); Destroy(target); Destroy(png); Canvas.ForceUpdateCanvases();
            }
        }
    }
}
