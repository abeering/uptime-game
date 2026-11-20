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
    public static class CanonicalSceneCapture
    {
        private const string Pending = "Uptime.CanonicalSceneCapture";
        static CanonicalSceneCapture() => EditorApplication.update += Launch;
        public static void Run()
        {
            EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Canonical graphical witness").AddComponent<CanonicalCaptureRunner>();
        }
    }
    public sealed class CanonicalCaptureRunner : MonoBehaviour
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
            File.WriteAllLines(CanonicalSceneWitness.Evidence + "/graphical-checks.txt", checks);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
        private void Check(bool value, string detail)
        { if (!value) throw new InvalidOperationException("CanonicalCapture FAIL: " + detail); checks.Add("PASS " + host.gameObject.scene.name + " " + detail); }
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
                host.GetComponent<HackerDirectives>().ConfigureForDevelopment(HackerDirective.BREACH);
                host.ResetSimulation();
                var terminal = FindFirstObjectByType<Terminal>();
                input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                canvas = terminal.GetComponentInParent<Canvas>();
                cameraForCapture = new GameObject("Canonical review camera", typeof(Camera)).GetComponent<Camera>();
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
                folder = CanonicalSceneWitness.Evidence + "/Captures/" + host.gameObject.scene.name;
                Directory.CreateDirectory(folder);
                Submit("scan db"); host.StepSimulation(8);
                host.TryInjectInfection("API", InfectionKind.Backdoor, 0, out _);
                yield return null; yield return null;
                Check(input.isFocused, "terminal input focused after scan");
                Frames("01-neutral-hidden");
                Submit("scan api"); host.StepSimulation(8);
                host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 1, out _);
                host.TryInjectInfection("DB", InfectionKind.Backdoor, 0, out _);
                Submit("clean db"); yield return new WaitForSecondsRealtime(2);
                var clean = FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
                var pid = (TMP_InputField)new SerializedObject(clean).FindProperty("pidInput").objectReferenceValue;
                Check(pid.isFocused && host.ActiveClean != null, "PID CLEAN overlay takes input focus");
                Frames("02-clean-orange-and-isolated-red");
                pid.onSubmit.Invoke(host.ActiveClean.Processes.First(p => p.CpuPercent > 80).Pid.ToString());
                yield return new WaitForSecondsRealtime(1.3f);
                Check(host.Integrity.For(host.FindNodeResources("DB")).Compromised && host.Infections.Count == 1, "CLEAN removes DB infection and preserves compromised card");
                for (int i = 0; i < 6; i++) traffic.QueueProfile("product-lookup");
                host.StepSimulation(3);
                Submit("inspect -v " + string.Join(" ", host.RuntimeConnections.Take(2).Select(c => c.ConnectionId)));
                Submit("load db");
                yield return null; Frames("03-crowded-inspect");
                host.TryInjectInfection("AUTH", InfectionKind.PhantomLoad, 16, out _);
                host.StepSimulation(3);
                yield return null; Frames("04-overload");
                Check(host.Integrity.For(host.FindNodeResources("AUTH")).Overloaded, "actual severe-load countdown presented");
                host.GetComponent<ConnectionsDuelDirector>().TrySchedule("API", DuelVariant.CommandRace, 0, "graphical_fixture", out _);
                yield return new WaitForSecondsRealtime(2);
                Frames("05-duel-overlay");
                Check(host.GetComponent<ConnectionsDuelDirector>().Current != null, "Duel overlay over live network");
                var selector = new HackerDirectiveSelector(host.Integrity, host.PhysicalLinks, HackerDirective.BREACH, 1701);
                selector.EnsureOffensive(host.SimulationTimeSeconds);
                foreach (string id in selector.Offensive.path)
                {
                    var node = host.FindNodeResources(id); var state = host.Integrity.For(node);
                    if (state.External) continue;
                    while (state.Value > 0) host.DamageIntegrity(node, "graphical_fixture", "winning_path");
                }
                FindFirstObjectByType<IntegrityPresenter>().RenderNow();
                yield return null; yield return null;
                Check(host.RunEnded && input.isFocused, "CORE ending closes overlays and restores terminal focus");
                Frames("06-core-breached");
                float frozen = host.SimulationTimeSeconds; host.StepSimulation(50);
                Check(host.SimulationTimeSeconds == frozen, "loss freezes authoritative simulation");
                Submit("restart"); yield return null; yield return null;
                Check(!host.RunEnded && input.isFocused && host.Integrity.Nodes.Where(n => !n.External).All(n => n.Value == 2 && !n.Revealed),
                    "terminal restart restores clean two-point state and focus");
                Frames("07-restart");
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
                Destroy(cameraForCapture.gameObject); Destroy(captureViewport); yield return null;
            }
            Debug.Log("CanonicalSceneCapture PASS: both real Play Mode scenes, 28 frames at 1920x1080/1280x720, overlays/focus/loss/restart.");
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
