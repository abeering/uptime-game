using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class FragilityCapture
    {
        private const string Pending = "Uptime.FragilityCapture";
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
            new GameObject("Fragility graphical witness").AddComponent<FragilityCaptureRunner>();
        }
    }
    public sealed class FragilityCaptureRunner : MonoBehaviour
    {
        private SimulationHost host;
        private TMP_InputField input;
        private Camera cameraForCapture;
        private RenderTexture captureViewport;
        private Canvas canvas;
        private string folder;
        private readonly List<string> checks = new();
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            yield return null;
            Exception failure = null; var run = Capture();
            while (true)
            {
                object next;
                try { if (!run.MoveNext()) break; next = run.Current; }
                catch (Exception error) { failure = error; break; }
                yield return next;
            }
            if (failure != null) Debug.LogException(failure);
            File.WriteAllLines(FragilityWitness.Evidence + "/graphical-checks.txt", checks);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
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
                folder = FragilityWitness.Evidence + "/Captures/" + host.gameObject.scene.name;
                Directory.CreateDirectory(folder);
                string[] ids = FragilityWitness.ConfigureFixture(host, 4, 1);
                FragilityWitness.Advance(host, 3f);
                Submit("inspect -v " + ids[0]); yield return null; yield return null;
                Check(!host.FindConnection(ids[0]).AtRisk, "healthy uncontended legitimate traffic");
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
                Check(FindObjectsByType<IdentityPillView>(FindObjectsSortMode.None).Any(p =>
                    p.transform.Find("ServiceDanger") is { } tag && tag.gameObject.activeInHierarchy &&
                    tag.GetComponent<TMP_Text>().text == "AT RISK"), "authored AT RISK text visible inside packed identity pill");
                Submit("overclock db"); FragilityWitness.Advance(host, .02f);
                yield return null; yield return null;
                Check(!host.FindConnection(ids[0]).AtRisk && host.OverclockedNode != null, "terminal overclock clears projected danger");
                Frames("04-rescue");
                FragilityWitness.Advance(host, 30);
                Check(host.FindConnection(ids[0]).Resolution == ConnectionResolution.Completed && host.LegitimateFailureCount == 0,
                    "rescued connection completes without failure");
                ids = FragilityWitness.ConfigureFixture(host, 4);
                FragilityWitness.Advance(host, 9.3f);
                Submit("inspect -v " + ids[0]); yield return null;
                FragilityWitness.Advance(host, host.FindConnection(ids[0]).ServiceDeadlineSeconds - host.SimulationTimeSeconds + .001f);
                yield return null; yield return null;
                Check(host.FindConnection(ids[0]).Resolution == ConnectionResolution.TimedOut && host.LegitimateFailureCount == 2,
                    "actual timeout and exact business count");
                FindFirstObjectByType<NetworkHealthPanel>().Render();
                Check(FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t => t.text == "BUSINESS FAILURES 2"),
                    "cumulative failure count visibly rendered");
                Frames("05-timeout-departure");
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
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
                Destroy(cameraForCapture.gameObject); Destroy(captureViewport); yield return null;
            }
            Debug.Log("FragilityCapture PASS: both Play Mode scenes, 32 frames at 1920x1080/1280x720, service danger/rescue/timeout/focus.");
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
                FindFirstObjectByType<LogicalPathPresenter>().RefreshLayouts();
                FindFirstObjectByType<WormPresenter>().RenderNow(); FindFirstObjectByType<PayloadPresenter>().RenderNow();
                foreach (var window in new Component[] { FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include),
                    FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include) })
                    if (window.gameObject.activeInHierarchy)
                        Check(InViewport((RectTransform)window.transform), name + " " + window.GetType().Name + " fully inside network viewport");
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
