using System;
using System.Collections;
using System.IO;
using System.Linq;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class PayloadLauncherCapture
    {
        private const string Pending = "Uptime.PayloadLauncherCapture.Pending";
        static PayloadLauncherCapture() => EditorApplication.update += Launch;

        [MenuItem("Tools/Connections/Capture Payload Launcher Play Mode Review")]
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-payloadLauncherScene");
            string scene = index < 0 ? "ConnectionsAdvanced" : args[index + 1];
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Payload Launcher capture runner")
                .AddComponent<PayloadLauncherCaptureRunner>().Begin();
        }
    }

    public sealed class PayloadLauncherCaptureRunner : MonoBehaviour
    {
        private SimulationHost host;
        private ConnectionTrafficDirector traffic;
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
            Exception failure = null;
            try
            {
                host = FindFirstObjectByType<SimulationHost>();
                traffic = FindFirstObjectByType<ConnectionTrafficDirector>();
                paths = FindFirstObjectByType<LogicalPathPresenter>();
                presenter = FindFirstObjectByType<PayloadPresenter>();
                if (host == null || traffic == null || paths == null || presenter == null)
                    throw new InvalidOperationException("Payload Launcher capture services missing");
                string[] args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "-payloadLauncherCapturePath");
                output = index < 0 ? "Docs/Review/ConnectionsPayloadLauncherV0" : args[index + 1];
                Directory.CreateDirectory(output);
                oldTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                oldHostEnabled = host.enabled;
                host.enabled = false;
                oldAuto = traffic.AutoGenerationEnabled;
                traffic.AutoGenerationEnabled = false;
                host.SetRunMode(ConnectionRunMode.Generated);
                host.ResetSimulation();
                paths.Bind(host.RuntimeConnections.ToArray());
                canvas = paths.GetComponentInParent<Canvas>();
                if (canvas == null) throw new InvalidOperationException("Capture canvas missing");
                oldRenderMode = canvas.renderMode;
                oldCanvasCamera = canvas.worldCamera;
                captureCamera = new GameObject("Payload Launcher capture camera", typeof(Camera))
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

                ConnectionSpawnPlan plan = traffic.QueueProfile("payload-launcher");
                for (int i = 0; i < 1500 &&
                    host.FindConnection(plan.ConnectionId)?.IsActive != true; i++)
                    host.StepSimulation(.02f);
                if (!host.FindConnection(plan.ConnectionId).IsActive)
                    throw new InvalidOperationException("Launcher did not become active");
                Frame("01-launcher-active");
                for (int i = 0; i < 2000 && host.Payloads.Payloads.Count == 0; i++)
                    host.StepSimulation(.02f);
                if (host.Payloads.Payloads.Count != 1)
                    throw new InvalidOperationException("Launcher did not produce one Payload");
                PayloadState payload = host.Payloads.Payloads.Single();
                if (payload.Phase != PayloadPhase.Establishing ||
                    payload.CurrentNode.NodeId != "API" || payload.NextNode.NodeId != "DB")
                    throw new InvalidOperationException("Launch final approach state drifted");
                host.StepSimulation(.4f);
                Frame("02-red-establishing");
                host.StepSimulation(1.8f);
                if (payload.Phase != PayloadPhase.Traversing)
                    throw new InvalidOperationException("Red traversal capture timing drifted");
                Frame("03-red-traversing");
                if (!host.Payloads.TryIntercept(payload.Id, "DB", out _))
                    throw new InvalidOperationException("Capture interception failed to arm");
                Frame("04-interceptor-armed");
                host.StepSimulation(6f);
                if (!payload.Intercepted || host.Infections.Count != 0)
                    throw new InvalidOperationException("Capture interception did not catch strike");
                Frame("05-intercepted");
                Debug.Log("PayloadLauncherCapture PASS: five 1920x1080 Play Mode frames in " + output);
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
                if (captureCamera != null) Destroy(captureCamera.gameObject);
                if (target != null) Destroy(target);
            }
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }

        private void Frame(string name)
        {
            paths.RefreshLayouts();
            // The capture pauses game time, so explicitly settle frame-driven path
            // geometry before rendering the active launcher connection.
            foreach (LogicalPathView view in FindObjectsByType<LogicalPathView>(
                FindObjectsSortMode.None))
                view.AdvanceVisual(view.RelayoutSeconds);
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
