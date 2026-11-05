using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class PayloadSpawnerCapture
    {
        private const string Pending = "Uptime.PayloadSpawnerCapture.Pending";
        static PayloadSpawnerCapture() => EditorApplication.update += Launch;

        [MenuItem("Tools/Connections/Capture Payload Spawner Play Mode Review")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Payload Spawner capture runner")
                .AddComponent<PayloadSpawnerCaptureRunner>().Begin();
        }
    }

    public sealed class PayloadSpawnerCaptureRunner : MonoBehaviour
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private SimulationHost host;
        private ConnectionTrafficDirector traffic;
        private ConnectionsRunRecorder recorder;
        private Terminal terminal;
        private ConnectionsCleanOverlay cleanOverlay;
        private LogicalPathPresenter paths;
        private PayloadPresenter presenter;
        private Canvas canvas;
        private Camera captureCamera;
        private RenderTexture target;
        private RenderMode oldRenderMode;
        private Camera oldCanvasCamera;
        private float oldTimeScale;
        private bool oldHostEnabled;
        private bool oldAuto;
        private string output;

        public void Begin() => StartCoroutine(Capture());

        private IEnumerator Capture()
        {
            yield return null;
            IEnumerator sequence = CaptureSequence();
            Exception failure = null;
            while (true)
            {
                object next;
                try
                {
                    if (!sequence.MoveNext()) break;
                    next = sequence.Current;
                }
                catch (Exception error)
                {
                    failure = error;
                    Debug.LogException(error);
                    break;
                }
                yield return next;
            }
            Cleanup();
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }

        private IEnumerator CaptureSequence()
        {
                host = FindFirstObjectByType<SimulationHost>();
                traffic = FindFirstObjectByType<ConnectionTrafficDirector>();
                recorder = FindFirstObjectByType<ConnectionsRunRecorder>();
                terminal = FindFirstObjectByType<Terminal>();
                cleanOverlay = FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
                paths = FindFirstObjectByType<LogicalPathPresenter>();
                presenter = FindFirstObjectByType<PayloadPresenter>();
                if (host == null || traffic == null || recorder == null || terminal == null ||
                    cleanOverlay == null || paths == null || presenter == null)
                    throw new InvalidOperationException("Payload Spawner capture services missing");
                string[] args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "-payloadSpawnerCapturePath");
                output = index < 0 ? "Docs/Review/ConnectionsPayloadSpawnerInfectionV0" :
                    args[index + 1];
                Directory.CreateDirectory(output);
                oldTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                oldHostEnabled = host.enabled;
                host.enabled = false;
                oldAuto = traffic.AutoGenerationEnabled;
                traffic.AutoGenerationEnabled = false;
                traffic.RandomizeSeed = false;
                host.SetRunMode(ConnectionRunMode.Generated);
                recorder.BeginRun();
                paths.Bind(host.RuntimeConnections.ToArray());
                canvas = paths.GetComponentInParent<Canvas>();
                if (canvas == null) throw new InvalidOperationException("Capture canvas missing");
                oldRenderMode = canvas.renderMode;
                oldCanvasCamera = canvas.worldCamera;
                captureCamera = new GameObject("Payload Spawner capture camera", typeof(Camera))
                    .GetComponent<Camera>();
                captureCamera.clearFlags = CameraClearFlags.SolidColor;
                captureCamera.backgroundColor = new Color(.025f, .065f, .08f);
                captureCamera.orthographic = true;
                captureCamera.transform.position = new Vector3(0f, 0f, -100f);
                captureCamera.nearClipPlane = .1f;
                captureCamera.farClipPlane = 1000f;
                target = new RenderTexture(1920, 1080, 24);
                captureCamera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = captureCamera;
                canvas.planeDistance = 10f;
                Canvas.ForceUpdateCanvases();

                Command("infect api payload-spawner 20");
                if (host.Infections.Count != 1 ||
                    host.Infections[0].Payload.Kind != InfectionKind.PayloadSpawner)
                    throw new InvalidOperationException("Manual infection command failed");
                host.StepSimulation(12f);
                Frame("01-hidden-source-before-first-launch");

                host.StepSimulation(8.4f);
                if (host.Payloads.Payloads.Count != 1 ||
                    host.Payloads.Payloads[0].Phase != PayloadPhase.Establishing)
                    throw new InvalidOperationException("First red launch timing drifted");
                Frame("02-first-red-establishing");
                host.StepSimulation(20f);
                if (host.Payloads.Payloads.Count != 2)
                    throw new InvalidOperationException("Second launch missing");
                PayloadState second = host.Payloads.Payloads[1];
                Frame("03-second-launch-over-time");

                Command("intercept " + second.Id + " @ " + second.FinalNode.NodeId);
                Frame("04-interceptor-armed");
                host.StepSimulation(7f);
                if (!second.Intercepted)
                    throw new InvalidOperationException("Second strike escaped interception");
                Command("scan api");
                host.StepSimulation(8f);
                if (recorder.CurrentLog.events.All(e =>
                    e.type != "scan_completed" || e.scanResult != "INFECTED"))
                    throw new InvalidOperationException("Scan did not discover source");
                Frame("05-scan-discovered-source");

                Command("clean api");
                float deadline = Time.realtimeSinceStartup + 6f;
                FieldInfo accepting = typeof(ConnectionsCleanOverlay)
                    .GetField("acceptingPid", Private);
                while (!(bool)accepting.GetValue(cleanOverlay) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                if (!(bool)accepting.GetValue(cleanOverlay) || host.ActiveClean == null)
                    throw new InvalidOperationException("PID-Select did not open");
                Frame("06-pid-select-source");
                int pid = host.ActiveClean.Processes.Single(p => p.CpuPercent >= 82).Pid;
                typeof(ConnectionsCleanOverlay).GetMethod("Submit", Private)
                    .Invoke(cleanOverlay, new object[] { pid.ToString() });
                if (host.Infections.Any(i => i.Payload.Kind == InfectionKind.PayloadSpawner))
                    throw new InvalidOperationException("PID-Select did not clean source");
                if (FindFirstObjectByType<NodeStatusPresenter>()
                    .StateFor(host.FindNodeResources("api").View).KnownInfected)
                    throw new InvalidOperationException("CLEAN left stale infection knowledge");
                Frame("07-clean-success");

                int launched = recorder.CurrentLog.events.Count(e =>
                    e.type == "payload_spawner_attempt" && e.reason == "launched");
                host.StepSimulation(45f);
                if (recorder.CurrentLog.events.Count(e =>
                    e.type == "payload_spawner_attempt" && e.reason == "launched") != launched)
                    throw new InvalidOperationException("Launch continued after CLEAN");
                cleanOverlay.gameObject.SetActive(false); // Retire the finished result window.
                Frame("08-no-further-launches");
                Debug.Log("PayloadSpawnerCapture PASS: eight 1920x1080 Advanced Play Mode frames in " + output);
        }

        private void Cleanup()
        {
            Time.timeScale = oldTimeScale;
            if (host != null) host.enabled = oldHostEnabled;
            if (traffic != null) traffic.AutoGenerationEnabled = oldAuto;
            if (canvas != null)
            {
                canvas.renderMode = oldRenderMode;
                canvas.worldCamera = oldCanvasCamera;
            }
            if (captureCamera != null) Destroy(captureCamera.gameObject);
            if (target != null) Destroy(target);
        }

        private void Command(string command) => typeof(Terminal)
            .GetMethod("HandleSubmit", Private).Invoke(terminal, new object[] { command });

        private void Frame(string name)
        {
            FindFirstObjectByType<NetworkHealthPanel>()?.Render();
            FindFirstObjectByType<NodeStatusPresenter>()?.RenderNow(
                Time.unscaledTime, 1f);
            paths.RefreshLayouts();
            foreach (LogicalPathView view in FindObjectsByType<LogicalPathView>(
                FindObjectsSortMode.None)) view.AdvanceVisual(view.RelayoutSeconds);
            presenter.RenderNow();
            Canvas.ForceUpdateCanvases();
            captureCamera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                try
                {
                    image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Path.Combine(output, name + "-1920x1080.png"),
                        image.EncodeToPNG());
                }
                finally { Destroy(image); }
            }
            finally { RenderTexture.active = previous; }
        }
    }
}
