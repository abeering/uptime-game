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
    public static class AmbientPayloadThreatCapture
    {
        private const string Pending = "Uptime.AmbientPayloadThreatCapture.Pending";
        static AmbientPayloadThreatCapture() => EditorApplication.update += Launch;

        [MenuItem("Tools/Connections/Capture Ambient Payload Threats Play Mode Review")]
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
            new GameObject("Ambient Payload threats capture runner")
                .AddComponent<AmbientPayloadThreatCaptureRunner>().Begin();
        }
    }

    public sealed class AmbientPayloadThreatCaptureRunner : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private SimulationHost host;
        private ConnectionTrafficDirector traffic;
        private HostileActivityDirector activity;
        private ConnectionsRunRecorder recorder;
        private LogicalPathPresenter paths;
        private PayloadPresenter presenter;
        private Canvas canvas;
        private Camera cameraForCapture;
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
            Exception failure = null;
            try
            {
                host = FindFirstObjectByType<SimulationHost>();
                traffic = FindFirstObjectByType<ConnectionTrafficDirector>();
                activity = FindFirstObjectByType<HostileActivityDirector>();
                recorder = FindFirstObjectByType<ConnectionsRunRecorder>();
                paths = FindFirstObjectByType<LogicalPathPresenter>();
                presenter = FindFirstObjectByType<PayloadPresenter>();
                if (host == null || traffic == null || activity == null || recorder == null ||
                    paths == null || presenter == null)
                    throw new InvalidOperationException("Ambient Payload capture services missing");
                string[] args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "-ambientPayloadCapturePath");
                output = index < 0 ? "Docs/Review/ConnectionsAmbientPayloadThreatsV0" :
                    args[index + 1];
                Directory.CreateDirectory(output);
                oldTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                oldHostEnabled = host.enabled;
                host.enabled = false;
                oldAuto = traffic.AutoGenerationEnabled;
                traffic.RandomizeSeed = false;
                traffic.Seed = 1701;
                host.SetRunMode(ConnectionRunMode.Generated);
                paths.Bind(host.RuntimeConnections.ToArray());
                canvas = paths.GetComponentInParent<Canvas>();
                oldRenderMode = canvas.renderMode;
                oldCanvasCamera = canvas.worldCamera;
                cameraForCapture = new GameObject("Ambient Payload capture camera",
                    typeof(Camera)).GetComponent<Camera>();
                cameraForCapture.clearFlags = CameraClearFlags.SolidColor;
                cameraForCapture.backgroundColor = new Color(.025f, .065f, .08f);
                cameraForCapture.orthographic = true;
                cameraForCapture.transform.position = new Vector3(0f, 0f, -100f);
                cameraForCapture.nearClipPlane = .1f;
                cameraForCapture.farClipPlane = 1000f;
                target = new RenderTexture(1920, 1080, 24);
                cameraForCapture.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cameraForCapture;
                canvas.planeDistance = 10f;

                // Force only the activity weight, then let the real hostile clock select it.
                SetActivity(0f, 0f, 0f, 0f, 1f);
                ResetForOpportunity();
                RunEvent launcherChoice = Selection();
                if (launcherChoice.hostileActivity != "payload_launcher")
                    throw new InvalidOperationException("Hostile clock missed Launcher");
                string launcherId = launcherChoice.connectionId;
                traffic.AutoGenerationEnabled = false;
                for (int i = 0; i < 40 &&
                    host.FindConnection(launcherId)?.IsActive != true; i++)
                    host.StepSimulation(1f);
                if (host.FindConnection(launcherId)?.IsActive != true)
                    throw new InvalidOperationException("Ambient Launcher never activated");
                Frame("01-ambient-launcher-active");
                for (int i = 0; i < 60 && host.Payloads.Payloads.Count == 0; i++)
                    host.StepSimulation(.5f);
                if (host.Payloads.Payloads.Count != 1)
                    throw new InvalidOperationException("Ambient Launcher did not launch");
                host.StepSimulation(.3f);
                Frame("02-ambient-launcher-red-payload");

                // Select an ordinary hostile INFECTOR through the same opportunity
                // clock, with only PayloadSpawner eligible as its infection kind.
                SetActivity(1f, 0f, 0f, 0f, 0f);
                Field("deliveryChance").SetValue(traffic, 1f);
                foreach (string name in new[] { "phantomLoadWeight", "blackoutWeight",
                    "spawnerWeight" }) Field(name).SetValue(traffic, 0f);
                Field("payloadSpawnerWeight").SetValue(traffic, 1f);
                ResetForOpportunity();
                RunEvent infectorChoice = Selection();
                if (infectorChoice.hostileActivity != "ordinary" ||
                    !recorder.CurrentLog.events.Any(e => e.type == "plan_queued" &&
                        e.connectionId == infectorChoice.connectionId &&
                        e.definition.operations.Last().infectionKind == "PayloadSpawner"))
                    throw new InvalidOperationException("Hostile clock missed Payload Spawner INFECTOR");
                traffic.AutoGenerationEnabled = false;
                for (int i = 0; i < 40 &&
                    host.FindConnection(infectorChoice.connectionId)?.IsActive != true; i++)
                    host.StepSimulation(1f);
                Frame("03-ambient-infector-active");
                for (int i = 0; i < 200 && host.Infections.Count == 0; i++)
                    host.StepSimulation(1f);
                if (host.Infections.Count != 1 || host.Payloads.Payloads.Count != 0)
                    throw new InvalidOperationException("INFECTOR did not install before launch");
                NodeInfection infection = host.Infections.Single();
                Frame("04-payload-spawner-installed");
                host.StepSimulation(infection.NextSpawnAtSeconds -
                    host.SimulationTimeSeconds + .3f);
                if (host.Payloads.Payloads.Count != 1 ||
                    host.Payloads.Payloads[0].Phase != PayloadPhase.Establishing)
                    throw new InvalidOperationException("First ambient spawner launch timing drifted");
                Frame("05-payload-spawner-first-red");
                Debug.Log("AmbientPayloadThreatCapture PASS: five 1920x1080 Advanced Play Mode frames in " + output);
            }
            catch (Exception error) { failure = error; Debug.LogException(error); }
            finally
            {
                Time.timeScale = oldTimeScale;
                if (host != null) host.enabled = oldHostEnabled;
                if (traffic != null) traffic.AutoGenerationEnabled = oldAuto;
                if (canvas != null)
                {
                    canvas.renderMode = oldRenderMode;
                    canvas.worldCamera = oldCanvasCamera;
                }
                if (cameraForCapture != null) Destroy(cameraForCapture.gameObject);
                if (target != null) Destroy(target);
            }
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }

        private RunEvent Selection()
        {
            host.StepSimulation(.02f);
            return recorder.CurrentLog.events.Single(e => e.type == "hostile_activity_selected");
        }

        private void ResetForOpportunity()
        {
            traffic.AutoGenerationEnabled = true;
            traffic.HostileGenerationEnabled = true;
            host.ResetSimulation();
            recorder.BeginRun();
            Field("nextHostileAt").SetValue(traffic, 0f);
        }

        private FieldInfo Field(string name) => typeof(ConnectionTrafficDirector)
            .GetField(name, Private);

        private void SetActivity(float ordinary, float recon, float payload,
            float worm, float launcher)
        {
            activity.OrdinaryWeight = ordinary;
            activity.ReconnaissanceWeight = recon;
            activity.PayloadWeight = payload;
            activity.WormWeight = worm;
            activity.PayloadLauncherWeight = launcher;
        }

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
            cameraForCapture.Render();
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
