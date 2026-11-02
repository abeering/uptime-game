using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class NodeStatusLabWitness
    {
        private const string Pending = "Uptime.StatusLab.PlayWitness";
        static NodeStatusLabWitness() => EditorApplication.update += StartPlayChecks;

        [MenuItem("Tools/Connections/Run Status Lab Play Mode Witness")]
        public static void RunPlayMode()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsStatusLab.unity");
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void StartPlayChecks()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Status Lab Play Checks").AddComponent<NodeStatusLabPlayChecks>().Begin();
        }

        [MenuItem("Tools/Connections/Run Status Lab Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsStatusLab.unity");
            var lab = UnityEngine.Object.FindFirstObjectByType<ConnectionsStatusLab>();
            Check(lab != null && lab.Node != null && lab.Node.NodeId == "AUTH" &&
                lab.NetworkArea != null && lab.StatusView != null && lab.Stamp != null,
                "authored driver and stamp bindings");
            Check(UnityEngine.Object.FindObjectsByType<NodeView>(FindObjectsSortMode.None).Length == 1 &&
                UnityEngine.Object.FindFirstObjectByType<SimulationHost>() == null &&
                UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>() == null,
                "one production node; no simulation or production observer");
            Check(PrefabUtility.GetCorrespondingObjectFromSource(lab.StatusView) ==
                AssetDatabase.LoadAssetAtPath<NodeStatusView>(
                    "Assets/Prefabs/Connections/NodeStatus.prefab"), "genuine linked status prefab");
            Check(PrefabUtility.GetCorrespondingObjectFromSource(lab.Stamp) ==
                AssetDatabase.LoadAssetAtPath<NodeEventStamp>(
                    "Assets/Prefabs/Connections/NodeEventStamp.prefab"),
                "genuine linked stamp prefab");
            Check(lab.Stamp.transform.parent == lab.StatusView.transform &&
                lab.Stamp.transform.GetSiblingIndex() >
                    lab.StatusView.StackRoot.GetSiblingIndex() &&
                lab.Stamp.Rect.sizeDelta.y > lab.StatusView.Rect.sizeDelta.y &&
                lab.Stamp.GetComponentsInChildren<Image>(true).All(i => !i.raycastTarget),
                "large centered stamp above panels with no raycast interception");
            var stampData = new SerializedObject(lab.Stamp);
            var frames = stampData.FindProperty("frames");
            var symbols = stampData.FindProperty("symbols");
            Check(frames.arraySize == 24 && symbols.arraySize == 8,
                "24 background frames and eight separate symbols");
            for (int i = 0; i < 24; i++)
            {
                var frame = (Sprite)frames.GetArrayElementAtIndex(i).objectReferenceValue;
                Check(frame != null && frame.name == "background_sheet_6x4_" + i &&
                    frame.rect.size == new Vector2(320f, 384f),
                    "ordered full-cell background frame " + i);
            }
            string[] symbolNames = { "offline", "infected", "scan_jammed",
                "port_scanned", "clean", "isolated", "intercept", "caught" };
            for (int i = 0; i < symbolNames.Length; i++)
                Check(symbols.GetArrayElementAtIndex(i).objectReferenceValue is Sprite sprite &&
                    sprite.name == symbolNames[i], "symbol binding " + symbolNames[i]);
            Check(UnityEngine.Object.FindFirstObjectByType<EventSystem>()?.GetComponent<BaseInputModule>() != null,
                "authored input module");
            string[] methods = { "ToggleOffline", "ToggleInfected", "ScanJammed", "PortScanned",
                "Clean", "Isolate", "Intercept", "Caught", "ClearAll" };
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            Check(buttons.Length == methods.Length, "nine authored buttons");
            foreach (string method in methods)
                Check(buttons.Count(b => b.onClick.GetPersistentEventCount() == 1 &&
                    b.onClick.GetPersistentTarget(0) == lab &&
                    b.onClick.GetPersistentMethodName(0) == method && b.interactable &&
                    b.targetGraphic != null && b.targetGraphic.raycastTarget) == 1,
                    "button binding " + method);
            Check(lab.gameObject.scene.GetRootGameObjects().SelectMany(r =>
                r.GetComponentsInChildren<Transform>(true)).All(t =>
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0),
                "driver has no missing scripts");

            float now = Time.unscaledTime;
            Check(!lab.State.HasAny(now) && !lab.StatusView.HasVisibleFamily,
                "initial state and visuals completely clean");
            lab.ToggleOffline(); lab.RenderNow(now, 1f);
            Check(lab.State.Offline && lab.StatusView.PainVisible && lab.Stamp.IsPlaying &&
                lab.Stamp.CurrentKind == NodeEventStamp.EventKind.Offline &&
                lab.StatusView.GetComponentsInChildren<NodeStatusBandGraphic>(true)
                    .All(b => !b.gameObject.activeSelf),
                "OFFLINE persistent toggle, stamp and lab-only band suppression");
            lab.ToggleOffline(); lab.RenderNow(now, 1f);
            Check(!lab.State.HasAny(now) && !lab.StatusView.HasVisibleFamily, "OFFLINE toggle off");
            lab.ToggleInfected(); lab.RenderNow(now + 100f, 1f);
            Check(lab.State.KnownInfected, "INFECTED remains untimed");
            lab.ToggleInfected(); lab.RenderNow(now, 1f);
            Check(!lab.State.HasAny(now), "INFECTED toggle off");

            Timed(lab, lab.ScanJammed, 8f, s => s.ScanJammed,
                s => s.ScanJamRemainingFraction, "SCAN JAMMED");
            Timed(lab, lab.PortScanned, 20f, s => s.PortScanned,
                s => s.PortScanRemainingFraction, "PORT SCANNED");
            Timed(lab, lab.Isolate, 8f, s => s.Isolated,
                s => s.IsolationRemainingFraction, "ISOLATED");
            Timed(lab, lab.Intercept, 5f, s => s.InterceptPayloadId == "x1" && !s.InterceptSucceeded,
                s => s.InterceptRemainingFraction, "INTERCEPT x1");
            Timed(lab, lab.Caught, 5f, s => s.InterceptPayloadId == "x1" && s.InterceptSucceeded,
                s => s.InterceptRemainingFraction, "CAUGHT x1");

            lab.ClearAll(); now = Time.unscaledTime;
            lab.Clean(); lab.RenderNow(now, .16f);
            Check(lab.State.CleanVisible(now) && lab.StatusView.BenefitVisible &&
                lab.StatusView.GetComponentsInChildren<NodeStatusPanel>(true)
                    .Single(p => p.name == "BenefitPanel").transform.localScale.x > 1f,
                "CLEAN genuine entry pop");
            lab.RenderNow(now + 2.49f, 1f);
            Check(lab.State.CleanVisible(now + 2.49f), "CLEAN before 2.5 seconds");
            lab.Clean(); lab.RenderNow(Time.unscaledTime, 1f);
            Check(lab.State.CleanVisible(Time.unscaledTime + 2.49f), "CLEAN refresh");
            lab.RenderNow(Time.unscaledTime + 2.51f, 1f);
            Check(!lab.State.CleanVisible(Time.unscaledTime + 2.51f) &&
                !lab.StatusView.HasVisibleFamily, "CLEAN expiry and exit");

            lab.Clean(); now = Time.unscaledTime;
            lab.RenderNow(now, 1f); lab.RenderNow(now + 2.51f, .1f);
            Check(!lab.State.CleanVisible(now + 2.51f) && lab.StatusView.HasVisibleFamily,
                "natural expiry retains the production exit fade");
            lab.ClearAll();
            Check(!lab.StatusView.HasVisibleFamily, "CLEAR ALL interrupts an exit fade");

            lab.ClearAll(); now = Time.unscaledTime;
            lab.ToggleOffline(); lab.ToggleInfected(); lab.ScanJammed(); lab.PortScanned();
            lab.Clean(); lab.Isolate(); lab.Intercept(); lab.RenderNow(now, 1f);
            Check(lab.StatusView.PainVisible && lab.StatusView.AwarenessVisible &&
                lab.StatusView.BenefitVisible && lab.State.KnownInfected &&
                lab.State.CleanVisible(now), "all three families, synthetic INFECTED plus CLEAN");
            Check(lab.StatusView.GetComponentsInChildren<TMP_Text>()
                .Any(t => t.text.Contains("SCAN JAMMED")) &&
                lab.StatusView.GetComponentsInChildren<TMP_Text>()
                .Any(t => t.text.Contains("INTERCEPT x1")), "three-row family stacks");
            lab.Caught(); lab.RenderNow(now, 1f);
            Check(lab.State.InterceptSucceeded && lab.Stamp.IsPlaying &&
                lab.Stamp.CurrentKind == NodeEventStamp.EventKind.Caught,
                "CAUGHT replaces existing interceptor outcome and stamp");
            lab.RenderNow(now + 8.01f, 1f);
            Check(!lab.State.Isolated && lab.State.Offline && lab.State.KnownInfected &&
                lab.State.PortScanned && !lab.State.ScanJammed,
                "isolation expiry preserves independent OFFLINE; other timers independent");
            lab.ClearAll(); lab.Isolate(); lab.RenderNow(Time.unscaledTime + 8.01f, 1f);
            Check(!lab.State.Offline, "isolation-only OFFLINE expires");
            lab.ToggleOffline(); lab.ToggleInfected(); lab.ScanJammed(); lab.PortScanned();
            lab.Clean(); lab.Isolate(); lab.Caught(); lab.RenderNow(Time.unscaledTime, 1f);
            lab.ClearAll();
            Check(!lab.State.HasAny(Time.unscaledTime) && !lab.StatusView.HasVisibleFamily &&
                !lab.Stamp.IsPlaying,
                "CLEAR ALL clears state and visuals synchronously");
            lab.RenderNow(Time.unscaledTime + 100f, 1f);
            Check(!lab.State.HasAny(Time.unscaledTime + 100f) && !lab.StatusView.HasVisibleFamily,
                "cleared timers never resurrect");
            Debug.Log("NodeStatusLabWitness PASS: authored bindings, production prefab, toggles, " +
                "all timed statuses at half/expiry/refresh, entry/exit, three families, " +
                "synthetic diagnosis overlap, isolation ownership and immediate clear.");
        }

        private static void Timed(ConnectionsStatusLab lab, Action apply, float seconds,
            Func<NodeStatusState, bool> active, Func<NodeStatusState, float> fraction, string label)
        {
            lab.ClearAll(); float now = Time.unscaledTime;
            apply(); lab.RenderNow(now + seconds * .5f, 1f);
            Check(active(lab.State) && Mathf.Abs(fraction(lab.State) - .5f) < .01f,
                label + " half-time expected .5 observed " + fraction(lab.State));
            apply(); lab.RenderNow(Time.unscaledTime, 1f);
            Check(active(lab.State) && Mathf.Abs(fraction(lab.State) - 1f) < .01f,
                label + " refresh expected 1 observed " + fraction(lab.State));
            lab.RenderNow(Time.unscaledTime + seconds + .01f, 1f);
            Check(!active(lab.State) && fraction(lab.State) == 0f &&
                !lab.StatusView.HasVisibleFamily, label + " expiry expected inactive/0/hidden");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("NodeStatusLabWitness: " + message);
        }
    }

    // Real frames and real expiry, including when Unity's scaled clock is paused.
    public sealed class NodeStatusLabPlayChecks : MonoBehaviour
    {
        private ConnectionsStatusLab lab;
        private Canvas canvas;
        private Camera cameraForCapture;
        private RenderTexture target;
        private float oldScale;
        private double deadline;

        public void Begin()
        {
            oldScale = Time.timeScale;
            deadline = EditorApplication.timeSinceStartup + 90f;
            StartCoroutine(Guard(Cases()));
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup > deadline)
                Finish(new TimeoutException("Status Lab Play Mode witness timed out"));
        }

        private IEnumerator Guard(IEnumerator cases)
        {
            while (true)
            {
                object next;
                try { if (!cases.MoveNext()) break; next = cases.Current; }
                catch (Exception error) { Finish(error); yield break; }
                yield return next;
            }
            Finish(null);
        }

        private IEnumerator Cases()
        {
            yield return null;
            Application.runInBackground = true;
            lab = UnityEngine.Object.FindFirstObjectByType<ConnectionsStatusLab>();
            Check(!lab.State.HasAny(Time.unscaledTime) && !lab.StatusView.HasVisibleFamily,
                "Play starts clean");
            canvas = lab.GetComponentInParent<Canvas>();
            if (canvas == null) canvas = lab.Node.GetComponentInParent<Canvas>();
            cameraForCapture = new GameObject("Status lab capture camera", typeof(Camera)).GetComponent<Camera>();
            cameraForCapture.transform.position = new Vector3(0, 0, -100);
            cameraForCapture.orthographic = true;
            cameraForCapture.clearFlags = CameraClearFlags.SolidColor;
            cameraForCapture.backgroundColor = new Color(.028f, .075f, .09f);
            target = new RenderTexture(ArgInt("-witnessWidth", 1920), ArgInt("-witnessHeight", 1080), 24);
            cameraForCapture.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cameraForCapture;
            canvas.planeDistance = 10;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(.2f);
            Capture("clean");
            string[] buttons = { "Offline", "Infected", "ScanJammed", "PortScanned", "Clean",
                "Isolate", "Intercept", "Caught" };
            string[] labels = { "OFFLINE", "INFECTED", "SCAN JAMMED", "PORT SCANNED", "CLEAN",
                "ISOLATED", "INTERCEPT x1", "CAUGHT x1" };
            for (int i = 0; i < buttons.Length; i++)
            {
                Click(buttons[i]);
                yield return new WaitForSecondsRealtime(.12f);
                Check(lab.StatusView.GetComponentsInChildren<TMP_Text>()
                    .Any(t => t.text.Contains(labels[i])), "pointer click displays " + labels[i]);
                Check(lab.Stamp.IsPlaying && (int)lab.Stamp.CurrentKind == i,
                    "pointer click plays matching stamp " + labels[i]);
                if (i == 2) Capture("stamp-scan-jammed");
                if (i == 4) Capture("stamp-clean");
                Click("ClearAll");
                Check(!lab.State.HasAny(Time.unscaledTime) && !lab.StatusView.HasVisibleFamily &&
                    !lab.Stamp.IsPlaying,
                    "CLEAR ALL immediate after " + labels[i]);
            }
            Click("Offline"); Click("Infected"); Click("ScanJammed"); Click("PortScanned");
            Click("Clean"); Click("Isolate"); Click("Intercept");
            yield return new WaitForSecondsRealtime(.16f);
            Capture("stamp-rapid-intercept");
            var painPanel = lab.StatusView.GetComponentsInChildren<NodeStatusPanel>(true)
                .Single(p => p.name == "PainPanel");
            Check(painPanel.transform.localScale.x > 1f, "entry pop in real frames at timeScale zero");
            yield return new WaitForSecondsRealtime(.4f);
            Check(lab.StatusView.PainVisible && lab.StatusView.AwarenessVisible &&
                lab.StatusView.BenefitVisible, "all three families coexist");
            Check(lab.StatusView.GetComponentsInChildren<TMP_Text>()
                .Any(t => t.text.EndsWith("s") && t.gameObject.activeInHierarchy),
                "remaining seconds timers count down");
            Capture("all-families");
            Click("Caught");
            yield return new WaitForSecondsRealtime(.2f);
            Check(lab.State.InterceptSucceeded, "CAUGHT replaces INTERCEPT");
            Capture("caught");
            yield return new WaitForSecondsRealtime(2.1f);
            Check(!lab.State.CleanVisible(Time.unscaledTime) && lab.State.Isolated,
                "CLEAN expires independently in real time");
            float before = lab.State.ScanJamRemainingFraction;
            Click("ScanJammed"); Click("PortScanned"); Click("Isolate"); Click("Caught");
            Check(before < .8f && lab.State.ScanJamRemainingFraction > .99f &&
                lab.State.PortScanRemainingFraction > .99f &&
                lab.State.IsolationRemainingFraction > .99f &&
                lab.State.InterceptRemainingFraction > .99f, "repeat application restarts all timers");
            float refreshedAt = Time.unscaledTime;
            yield return new WaitForSecondsRealtime(4f);
            Check(Mathf.Abs(lab.State.ScanJamRemainingFraction - .5f) < .03f &&
                Mathf.Abs(lab.State.InterceptRemainingFraction - .2f) < .03f,
                "real countdown expected jam .5 and caught .2");
            Capture("countdown");
            yield return new WaitForSecondsRealtime(4.7f);
            Check(!lab.State.Isolated && !lab.State.ScanJammed && lab.State.InterceptPayloadId == null &&
                lab.State.Offline && lab.State.KnownInfected && lab.State.PortScanned,
                "5/8-second expiry preserves untimed OFFLINE and INFECTED plus 20-second awareness");
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, refreshedAt + 20.7f - Time.unscaledTime));
            Check(!lab.State.PortScanned && !lab.StatusView.AwarenessVisible &&
                !lab.StatusView.BenefitVisible && lab.StatusView.PainVisible,
                "20-second expiry leaves persistent Pain");
            Capture("expired");
            Click("ClearAll"); Capture("cleared");
            Check(!lab.State.HasAny(Time.unscaledTime) && !lab.StatusView.HasVisibleFamily,
                "final clear is immediate");
        }

        private void Click(string name)
        {
            var button = canvas.GetComponentsInChildren<Button>().Single(b => b.name == name);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cameraForCapture,
                button.transform.TransformPoint(((RectTransform)button.transform).rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = screen,
                button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Check(hits.Count > 0 && hits[0].gameObject == button.gameObject,
                "unobstructed button raycast " + name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static int ArgInt(string key, int fallback)
        {
            string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, key);
            return i < 0 ? fallback : int.Parse(args[i + 1]);
        }

        private void Capture(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-statusLabCapturePath");
            if (i < 0) return;
            Directory.CreateDirectory(args[i + 1]);
            Canvas.ForceUpdateCanvases(); cameraForCapture.Render();
            RenderTexture old = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(args[i + 1], name + "-" + target.width + "x" + target.height + ".png"),
                texture.EncodeToPNG());
            Destroy(texture); RenderTexture.active = old;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("StatusLabPlay: " + message);
            Debug.Log("STATUS LAB PLAY CHECK: " + message);
        }

        private void Finish(Exception error)
        {
            enabled = false; Time.timeScale = oldScale;
            if (canvas != null) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            if (cameraForCapture != null) Destroy(cameraForCapture.gameObject);
            if (target != null) Destroy(target);
            if (error == null) Debug.Log("NodeStatusLabWitness PLAY PASS: real-time UI clicks, countdown, expiry, refresh and clear.");
            else Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
    }
}
