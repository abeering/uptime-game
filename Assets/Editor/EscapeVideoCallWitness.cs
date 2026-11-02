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

        internal static Vector2Int RequestedSize()
        {
            string[] args = Environment.GetCommandLineArgs();
            int w = Array.IndexOf(args, "-witnessWidth"), h = Array.IndexOf(args, "-witnessHeight");
            return new Vector2Int(w >= 0 ? int.Parse(args[w + 1]) : 1920,
                h >= 0 ? int.Parse(args[h + 1]) : 1080);
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
                ((RectTransform)call.transform).sizeDelta == new Vector2(520, 400),
                "inactive compact call has an authored network position");
            foreach (string part in new[] { "CallTitlebar", "CallWindowFrame" })
                Check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(call.transform.Find(part).gameObject) ==
                    "Assets/Prefabs/Connections/" + (part == "CallTitlebar" ? "DockedTitlebar" : "DockedWindowFrame") + ".prefab",
                    "call uses shared authored chrome " + part);
            Check(!call.transform.Find("CallTitlebar/Pinned").gameObject.activeSelf &&
                call.GetComponentsInChildren<Selectable>(true).Length == 0 &&
                call.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget),
                "call has no clickable controls or raycast surface");
            var placement = UnityEngine.Object.FindFirstObjectByType<ConnectionsWindowPlacement>();
            Check(placement != null && placement.transform == clean.transform.parent &&
                EscapeVideoCallChecks.Field<ConnectionsWindowPlacement>(call, "placement") == placement &&
                EscapeVideoCallChecks.Field<ConnectionsWindowPlacement>(clean, "placement") == placement,
                "both windows use the authored Connections placement helper");
            var body = EscapeVideoCallChecks.Field<TMP_Text>(call, "bodyText");
            var panel = body.transform.parent.GetComponent<Image>();
            var handset = EscapeVideoCallChecks.Field<Image>(call, "hangUpHandset");
            var feed = (RectTransform)call.transform.Find("VideoFeed");
            Check(body.transform.parent.parent.name == "VideoFeed" && Mathf.Approximately(panel.color.a, .6f) &&
                body.overflowMode == TextOverflowModes.Page && body.rectTransform.rect.height >= 60 &&
                Mathf.Abs(feed.rect.width / feed.rect.height - 16f / 9f) < .001f && feed.rect.width >= 480 &&
                body.rectTransform.offsetMin == new Vector2(16, 10) &&
                body.rectTransform.offsetMax == new Vector2(-16, -10),
                "dominant 16:9 feed contains a softer inset, padded and paginated subtitle overlay");
            var titleHint = EscapeVideoCallChecks.Field<TMP_Text>(call, "hangUpTitleHint");
            Check(AssetDatabase.GetAssetPath(handset.sprite) == "Assets/Images/phone.png" && handset.preserveAspect &&
                EscapeVideoCallChecks.Field<Image>(call, "hangUpCircle").sprite != handset.sprite &&
                EscapeVideoCallChecks.Field<TMP_Text>(call, "hangUpKeycap").text == "[ESC]" &&
                titleHint != null && titleHint.text == "[ESC] HANG UP" && titleHint.transform.parent.name == "CallTitlebar",
                "same supplied handset and both authored Escape indicators without clickable controls");
            Check(EscapeVideoCallChecks.Field<RectTransform[]>(placement, "cleanContent")
                .Contains(clean.transform.Find("CleanTitlebar/Title")), "CLEAN title is protected alongside process body and PID input");
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
        private Canvas witnessCanvas;
        private Camera captureCamera, originalCamera;
        private RenderTexture captureTarget;
        private RenderMode originalRenderMode;
        private double deadline;
        public static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private).GetValue(obj);
        private static void Check(bool ok, string text) => EscapeVideoCallWitness.Check(ok, text);
        private static WaitForSecondsRealtime Wait(float seconds = .2f) => new(seconds);
        private GameObject CleanHint => Field<GameObject>(clean, "cancelHint");
        private Image CallCircle => Field<Image>(call, "hangUpCircle");
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
                CallCircle.gameObject.activeInHierarchy == call.IsActive &&
                CallCircle.color == Field<Color>(call, ReferenceEquals(target, call) ? "activeCircle" : "inactiveCircle") &&
                Field<Image>(call, "hangUpHandset").color == Field<Color>(call, ReferenceEquals(target, call) ? "activeHandset" : "inactiveHandset") &&
                Field<TMP_Text>(call, "hangUpKeycap").color == Field<Color>(call, ReferenceEquals(target, call) ? "activeKeycap" : "inactiveKeycap") &&
                Field<TMP_Text>(call, "hangUpTitleHint").color == Field<TMP_Text>(call, "hangUpKeycap").color &&
                Field<TMP_Text>(call, "hangUpTitleHint").gameObject.activeInHierarchy == call.IsActive,
                "one Escape owner drives both titlebar and bottom indicators; the inactive call stays visible");
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
            deadline = EditorApplication.timeSinceStartup + 180;
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
            if (witnessCanvas != null)
            {
                witnessCanvas.renderMode = originalRenderMode; witnessCanvas.worldCamera = originalCamera;
            }
            if (captureCamera != null) Destroy(captureCamera.gameObject);
            if (captureTarget != null) Destroy(captureTarget);
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
            // Keep the Canvas at the reviewed size before any window placement, not just at capture time.
            Vector2Int requested = EscapeVideoCallWitness.RequestedSize();
            witnessCanvas = coordinator.GetComponent<Canvas>(); originalRenderMode = witnessCanvas.renderMode;
            originalCamera = witnessCanvas.worldCamera;
            captureCamera = new GameObject("Witness camera", typeof(Camera)).GetComponent<Camera>();
            captureCamera.orthographic = true; captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = new Color(.025f,.065f,.08f);
            captureCamera.transform.position = new Vector3(0,0,-100);
            captureCamera.nearClipPlane = .1f; captureCamera.farClipPlane = 1000;
            captureTarget = new RenderTexture(requested.x, requested.y, 24); captureCamera.targetTexture = captureTarget;
            witnessCanvas.renderMode = RenderMode.ScreenSpaceCamera; witnessCanvas.worldCamera = captureCamera;
            witnessCanvas.planeDistance = 10; Canvas.ForceUpdateCanvases(); yield return null;
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-capturePath");
            capturePath = at >= 0 ? args[at + 1] : null;
            UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>().AutoGenerationEnabled = false;
            host.ResetSimulation();
            var network = (RectTransform)clean.transform.parent;
            Rect networkRect = network.rect;
            Vector2 networkMin = network.offsetMin, networkMax = network.offsetMax;
            Vector3[] nodePositions = host.Nodes.Select(n => n.View.transform.localPosition).ToArray();
            Check(witnessCanvas.pixelRect.width == requested.x && witnessCanvas.pixelRect.height == requested.y,
                "placement and capture share fixed Canvas viewport " + requested.x + "x" + requested.y);
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
            Vector3 fixedClean = clean.transform.localPosition;
            Check(call.TryStartSampleCall(), "call can start programmatically while CLEAN owns typing");
            Check(clean.transform.localPosition == fixedClean, "call appearance keeps existing CLEAN position fixed");
            Owners(call); Preserved(pid, "527"); yield return Wait(.5f); Capture("call-over-clean");
            yield return Wait(); Press(pid); yield return Wait(.25f);
            Owners(clean); Check(clean.IsOpen && !call.IsActive, "first Escape hangs up and holding does not cascade into CLEAN");
            Preserved(pid, "527");
            Release(); yield return Wait(.05f); Press(pid); yield return Wait(.1f);
            Check(!clean.IsOpen && host.ActiveClean == null && command.isFocused, "next fresh Escape cancels CLEAN and restores terminal typing");
            Release(); yield return Wait(.05f);

            call.TryStartSampleCall();
            Vector3 fixedCall = call.transform.localPosition;
            Command("clean DB");
            yield return new WaitUntil(() => pid.gameObject.activeInHierarchy && pid.isFocused);
            Owners(clean); Capture("clean-over-call");
            Check(Vector3.Distance(call.transform.localPosition, fixedCall) < .01f && call.IsActive && body.maxVisibleCharacters > 0,
                "CLEAN opening keeps the older call fixed and its dialogue advancing or complete");
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
            var clip = new AnimationClip();
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Image), "m_Color.r"),
                AnimationCurve.Linear(0, .3f, 1, 1));
            animation.layers[0].stateMachine.AddState("Idle").motion = clip;
            Check(call.TryStartCall(new ConnectionsVideoCallData { CallerName = "PORTRAIT TEST",
                Body = string.Join(" ", Enumerable.Repeat("Portrait talking test.", 12)), Portrait = portrait, PortraitAnimation = animation }),
                "caller content accepts a sprite and optional animation controller");
            yield return Wait(.1f);
            var animator = Field<Animator>(call, "portraitAnimator");
            Check(Field<Image>(call, "portraitImage").sprite == portrait &&
                !Field<GameObject>(call, "portraitFallback").activeSelf &&
                Field<TMP_Text>(call, "callerText").text == "CALL / PORTRAIT TEST" && animator.GetBool("IsTalking"),
                "dynamic identity, portrait and talking hook bind without typing focus changes");
            float animatedRed = Field<Image>(call, "portraitImage").color.r;
            int disclosed = body.maxVisibleCharacters;
            Command("clean DB"); yield return Wait(.3f); Owners(clean);
            Check(animator.GetBool("IsTalking") && body.maxVisibleCharacters > disclosed &&
                Mathf.Abs(Field<Image>(call, "portraitImage").color.r - animatedRed) > .02f,
                "portrait animation and subtitle typewriter continue while the handset is gray");
            Capture("animated-portrait-inactive");
            clean.DismissFromEscape(); yield return Wait(.1f); Owners(call);
            yield return new WaitUntil(() => !call.IsTyping);
            Check(body.maxVisibleCharacters == body.textInfo.characterCount && !animator.GetBool("IsTalking"),
                "complete subtitle and talking-stop transition");
            call.EndCall(); Destroy(animation); Destroy(clip);
            var tuning = new SerializedObject(call);
            tuning.FindProperty("charactersPerSecond").floatValue = 10000;
            tuning.ApplyModifiedPropertiesWithoutUndo();
            call.TryStartCall(new ConnectionsVideoCallData { CallerName = new string('L', 100),
                Body = string.Join(" ", Enumerable.Repeat("Long subtitle test sentence.", 80)) });
            yield return new WaitUntil(() => !call.IsTyping);
            var page = body.textInfo.pageInfo[body.pageToDisplay - 1];
            int pageLines = body.textInfo.characterInfo[page.lastCharacterIndex].lineNumber -
                body.textInfo.characterInfo[page.firstCharacterIndex].lineNumber + 1;
            Check(pageLines <= 3, "long dialogue stays within three subtitle lines on the current page");
            Capture("long-dialogue");
            Check(body.textInfo.pageCount > 1 && body.pageToDisplay == body.textInfo.pageCount &&
                Field<TMP_Text>(call, "callerText").overflowMode == TextOverflowModes.Ellipsis,
                "long dialogue follows its current subtitle page and long caller names keep the hint clear");
            call.EndCall();
            tuning.FindProperty("charactersPerSecond").floatValue = 45;
            tuning.ApplyModifiedPropertiesWithoutUndo();

            // Reopening recomputes placement from actual view bindings, with no topology motion.
            Vector3 previousPosition = Vector3.zero;
            bool differentHomes = false, avoidedObstruction = false;
            Rect lastCleanBounds = default;
            var placement = Field<ConnectionsWindowPlacement>(call, "placement");
            var cleanParts = Field<RectTransform[]>(placement, "cleanContent");
            var callParts = Field<RectTransform[]>(placement, "callContent");
            foreach (NodeResources node in host.Nodes)
            {
                Command("clean " + node.View.NodeId);
                yield return new WaitUntil(() => pid.gameObject.activeInHierarchy && pid.isFocused);
                Rect bounds = LocalRect(network, (RectTransform)clean.transform);
                Check(bounds.xMin >= network.rect.xMin + 11.9f && bounds.xMax <= network.rect.xMax - 11.9f &&
                    bounds.yMin >= network.rect.yMin + 11.9f && bounds.yMax <= network.rect.yMax - 51.9f,
                    "node-bound CLEAN is within network content viewport: " + node.View.NodeId);
                if (previousPosition != Vector3.zero && Vector3.Distance(previousPosition, clean.transform.localPosition) > 10)
                    differentHomes = true;
                previousPosition = clean.transform.localPosition;
                Capture("clean-node-" + node.View.NodeId); lastCleanBounds = bounds;
                Vector3 cleanHome = clean.transform.localPosition;
                Vector2 callSize = ((RectTransform)call.transform).rect.size;
                Rect preferredCall = new(new Vector2(network.rect.xMax - 24 - callSize.x, network.rect.yMax - 64 - callSize.y), callSize);
                float preferredObstruction = ContentOverlap(network, preferredCall, cleanParts);
                call.TryStartSampleCall();
                float chosenObstruction = ContentOverlap(network, LocalRect(network, (RectTransform)call.transform), cleanParts);
                yield return Wait(.35f);
                Rect arrivingCall = LocalRect(network, (RectTransform)call.transform);
                Rect cleanTitle = LocalRect(network, Field<TMP_Text>(clean, "title").rectTransform);
                Rect headings = LocalRect(network, Field<TMP_Text>(clean, "body").rectTransform);
                headings.yMin = headings.yMax - 32;
                Debug.Log("CALL LABEL OVERLAP " + node.View.NodeId + ": title=" + Overlap(arrivingCall, cleanTitle) +
                    ", headings=" + Overlap(arrivingCall, headings) + ", content=" + chosenObstruction);
                Capture("call-over-clean-node-" + node.View.NodeId);
                Check(Vector3.Distance(cleanHome, clean.transform.localPosition) < .01f,
                    "call candidates preserve existing node-bound CLEAN position");
                if (preferredObstruction > chosenObstruction + 1)
                {
                    if (!avoidedObstruction) Capture("avoided-content-obstruction");
                    avoidedObstruction = true;
                    Check(chosenObstruction < preferredObstruction,
                        "call alternative reduces protected-content obstruction: " + preferredObstruction + " to " + chosenObstruction);
                }
                call.EndCall(); clean.DismissFromEscape(); yield return Wait(.05f);
            }
            Check(differentHomes, "different target nodes produce different CLEAN homes");
            Check(avoidedObstruction, "a better call candidate avoids important CLEAN content");

            // Targeted review fixture: the preferred call home clips CLEAN's title and PID/CPU row.
            // Stage the older window once, then let the unchanged open-time policy place the call.
            Command("clean " + host.Nodes.First().View.NodeId);
            yield return new WaitUntil(() => pid.gameObject.activeInHierarchy && pid.isFocused);
            Draft(pid, "527");
            Vector2 reviewCallSize = ((RectTransform)call.transform).rect.size;
            Rect reviewHome = new(new Vector2(network.rect.xMax - 24 - reviewCallSize.x,
                network.rect.yMax - 64 - reviewCallSize.y), reviewCallSize);
            Rect cleanBefore = LocalRect(network, (RectTransform)clean.transform);
            Rect stagedClean = new(new Vector2(network.rect.xMax - 12 - cleanBefore.width,
                reviewHome.yMin + 100 - cleanBefore.height), cleanBefore.size);
            clean.transform.position += network.TransformVector(stagedClean.center - cleanBefore.center);
            Vector3 stagedPosition = clean.transform.localPosition;
            Rect reviewTitle = LocalRect(network, Field<TMP_Text>(clean, "title").rectTransform);
            Rect reviewHeadings = LocalRect(network, Field<TMP_Text>(clean, "body").rectTransform);
            reviewHeadings.yMin = reviewHeadings.yMax - 32;
            Check(Overlap(reviewHome, reviewTitle) > 0 && Overlap(reviewHome, reviewHeadings) > 0,
                "review fixture reproduces preferred-home obstruction of CLEAN title and PID/CPU headings");
            call.TryStartSampleCall(); yield return Wait(.5f);
            Rect adjustedCall = LocalRect(network, (RectTransform)call.transform);
            Check(Overlap(adjustedCall, reviewTitle) < .1f && Overlap(adjustedCall, reviewHeadings) < .1f &&
                ContentOverlap(network, adjustedCall, cleanParts) < .1f &&
                Vector3.Distance(clean.transform.localPosition, stagedPosition) < .01f,
                "adjacent call candidate clears title, headings, listing and PID input without moving CLEAN");
            Owners(call); Preserved(pid, "527"); Capture("title-heading-collision");
            call.EndCall(); clean.DismissFromEscape(); yield return Wait(.1f);

            // Controlled fixture: stage an older call with four units of bottom-frame overlap.
            // Node positions and the candidate solver are untouched; only the existing call pose is staged.
            call.TryStartSampleCall();
            Rect callBounds = LocalRect(network, (RectTransform)call.transform);
            call.transform.position += network.TransformVector(new Vector2(0, lastCleanBounds.yMax - 4 - callBounds.yMin));
            fixedCall = call.transform.localPosition;
            Command("clean " + host.Nodes.Last().View.NodeId);
            yield return new WaitUntil(() => pid.gameObject.activeInHierarchy && pid.isFocused);
            Rect minorClean = LocalRect(network, (RectTransform)clean.transform);
            float minorArea = Overlap(minorClean, LocalRect(network, (RectTransform)call.transform));
            Check(minorArea > 0 && ContentOverlap(network, minorClean, callParts) < .1f &&
                Vector3.Distance(call.transform.localPosition, fixedCall) < .01f &&
                Vector2.Distance(minorClean.center, lastCleanBounds.center) < .1f,
                "four-unit harmless frame overlap keeps CLEAN near its node and the older call fixed: area=" + minorArea);
            Owners(clean); Capture("minor-frame-overlap");
            float beforePenalty = host.Health.Value;
            int innocent = host.ActiveClean.Processes.First(p => p.CpuPercent < 82).Pid;
            pid.onSubmit.Invoke(innocent.ToString());
            Check(!clean.CanDismissWithEscape && Mathf.Abs(host.Health.Value - (beforePenalty - 5)) < .001f &&
                Field<TMP_Text>(clean, "body").text.Contains("CLEAN FAILED"),
                "innocent PID retains exact five-Health penalty and noncancelable failure result");
            Owners(call); Capture("innocent-pid-result");
            yield return new WaitUntil(() => !clean.IsOpen); yield return Wait(.1f); call.EndCall();

            // A real delayed developer command arrives while the PID field owns typing.
            Command("calltest delay=5");
            Check(Field<Coroutine>(terminal, "delayedCall") != null, "bounded delayed call is scheduled");
            var pending = Field<Coroutine>(terminal, "delayedCall");
            Command("calltest delay=1"); Command("calltest");
            Check(Field<Coroutine>(terminal, "delayedCall") == pending && !call.IsActive,
                "duplicate delayed or immediate tests cannot replace a pending call");
            Command("clean DB");
            yield return new WaitUntil(() => pid.gameObject.activeInHierarchy && pid.isFocused);
            Draft(pid, "527"); fixedClean = clean.transform.localPosition;
            yield return new WaitUntil(() => call.IsActive);
            Owners(call); Preserved(pid, "527");
            Check(clean.transform.localPosition == fixedClean && Field<Coroutine>(terminal, "delayedCall") == null,
                "delayed call places once without moving CLEAN or changing its typing owner");
            yield return Wait(.7f); Capture("delayed-call-over-clean");
            call.EndCall(); yield return Wait(); Owners(clean); clean.DismissFromEscape(); yield return Wait(.1f);
            Command("calltest delay=1"); terminal.enabled = false;
            Check(Field<Coroutine>(terminal, "delayedCall") == null, "terminal disable cancels pending debug call");
            terminal.enabled = true; yield return Wait(1.1f);
            Check(!call.IsActive, "canceled delayed call does not appear later");
            foreach (string invalid in new[] { "calltest delay=0", "calltest delay=-1", "calltest delay=31",
                "calltest delay=NaN", "calltest delay=Infinity", "calltest delay=1 extra" }) Command(invalid);
            Check(!call.IsActive && Field<Coroutine>(terminal, "delayedCall") == null, "invalid delays leave no call or schedule");

            // Generated traffic continues behind the presentation using ordinary scene settings.
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            traffic.AutoGenerationEnabled = true; host.ResetSimulation();
            yield return new WaitUntil(() => host.RuntimeConnections.Any(c => c.IsActive));
            Command("calltest delay=5"); Command("clean " + host.Nodes.Last().View.NodeId);
            yield return new WaitUntil(() => pid.gameObject.activeInHierarchy && pid.isFocused && call.IsActive);
            Draft(pid, "527"); yield return Wait(.7f);
            Owners(call); Preserved(pid, "527"); Capture("generated-traffic-overlap");
            Check(host.SimulationTimeSeconds > 0 && host.RuntimeConnections.Any(c => c.IsActive),
                "ordinary generated traffic remains live behind both windows");
            call.EndCall(); clean.DismissFromEscape(); traffic.AutoGenerationEnabled = false;
            yield return Wait(.1f);

            // Protection runs in real time even if simulation time is stopped by a test.
            Time.timeScale = 0f; call.TryStartSampleCall();
            yield return Wait(.2f); coordinator.enabled = false;
            Owners(null);
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

        private static Rect LocalRect(RectTransform parent, RectTransform item)
        {
            var corners = new Vector3[4]; item.GetWorldCorners(corners);
            Vector2 min = parent.InverseTransformPoint(corners[0]);
            Vector2 max = parent.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static float Overlap(Rect a, Rect b) =>
            Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) *
            Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));

        private static float ContentOverlap(RectTransform parent, Rect window, RectTransform[] parts) =>
            parts.Sum(part => Overlap(window, LocalRect(parent, part)));

        private void Capture(string phase)
        {
            if (string.IsNullOrEmpty(capturePath)) return;
            Directory.CreateDirectory(capturePath);
            Canvas.ForceUpdateCanvases(); captureCamera.Render();
            var previous = RenderTexture.active; RenderTexture.active = captureTarget;
            var png = new Texture2D(captureTarget.width, captureTarget.height, TextureFormat.RGB24, false);
            png.ReadPixels(new Rect(0, 0, png.width, png.height), 0, 0); png.Apply();
            File.WriteAllBytes(Path.Combine(capturePath, gameObject.scene.name + "-" + phase + "-" +
                png.width + "x" + png.height + ".png"), png.EncodeToPNG());
            RenderTexture.active = previous; Destroy(png);
        }
    }
}
