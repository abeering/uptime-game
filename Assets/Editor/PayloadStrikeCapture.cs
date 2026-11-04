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
    // Scripted Play Mode frames for visual review; interactive judgment is still required.
    [InitializeOnLoad]
    public static class PayloadStrikeCapture
    {
        private const string Pending = "Uptime.PayloadStrikeCapture.Pending";

        static PayloadStrikeCapture() => EditorApplication.update += Launch;

        [MenuItem("Tools/Connections/Capture Payload Strike Play Mode Review")]
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            int sceneIndex = Array.IndexOf(args, "-payloadStrikeScene");
            string scene = sceneIndex < 0 ? "ConnectionsAdvanced" : args[sceneIndex + 1];
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Payload Strike capture runner")
                .AddComponent<PayloadStrikeCaptureRunner>().Begin();
        }
    }

    public sealed class PayloadStrikeCaptureRunner : MonoBehaviour
    {
        private SimulationHost host;
        private LogicalPathPresenter paths;
        private PayloadPresenter presenter;
        private ConnectionTrafficDirector traffic;
        private Canvas canvas;
        private Camera captureCamera;
        private RenderTexture target;
        private RenderMode previousMode;
        private Camera previousCamera;
        private float previousTimeScale;
        private bool previousAuto;
        private bool previousHostEnabled;
        private string output;

        public void Begin() => StartCoroutine(CaptureSequence());

        private IEnumerator CaptureSequence()
        {
            yield return null; // Let the scene's normal Play Mode initialization complete.
            Exception failure = null;
            try
            {
                host = FindFirstObjectByType<SimulationHost>();
                paths = FindFirstObjectByType<LogicalPathPresenter>();
                presenter = FindFirstObjectByType<PayloadPresenter>();
                traffic = FindFirstObjectByType<ConnectionTrafficDirector>();
                if (host == null || paths == null || presenter == null || traffic == null)
                    throw new InvalidOperationException("Payload capture scene services missing");
                string[] args = Environment.GetCommandLineArgs();
                int outputIndex = Array.IndexOf(args, "-payloadStrikeCapturePath");
                output = outputIndex < 0 ?
                    "Docs/Review/ConnectionsPayloadStrikeTelegraphV0" : args[outputIndex + 1];
                Directory.CreateDirectory(output);
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                previousHostEnabled = host.enabled;
                host.enabled = false;
                previousAuto = traffic.AutoGenerationEnabled;
                traffic.AutoGenerationEnabled = false;
                host.SetRunMode(ConnectionRunMode.Generated);
                host.ResetSimulation();
                paths.Bind(host.RuntimeConnections.ToArray());
                canvas = paths.GetComponentInParent<Canvas>();
                if (canvas == null) throw new InvalidOperationException("Payload capture canvas missing");
                previousMode = canvas.renderMode;
                previousCamera = canvas.worldCamera;
                captureCamera = new GameObject("Payload Strike capture camera", typeof(Camera))
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

                PayloadState multi = host.Payloads.Spawn("DB", PayloadMotionProfile.Steady,
                    "EXT1", new[] { 8f, 8f, 20f });
                host.StepSimulation(4.2f); // Establishment plus three seconds of orange travel.
                Capture("01-intermediate-orange");
                host.StepSimulation(5f); // First arrival, second hop begins.
                host.StepSimulation(9.2f); // Second arrival, final hop begins establishing.
                if (multi.Hop != 2 || multi.Phase != PayloadPhase.Establishing)
                    throw new InvalidOperationException("Final establishment capture timing drifted");
                host.StepSimulation(.4f);
                Capture("02-final-establishing-red");
                host.StepSimulation(4.3f); // Complete establishment, then advance red body.
                Capture("03-final-traversing-red");

                host.ResetSimulation();
                paths.Bind(host.RuntimeConnections.ToArray());
                PayloadState oneHop = host.Payloads.Spawn("DB", PayloadMotionProfile.Variable,
                    "API", new[] { 20f });
                host.StepSimulation(.4f);
                if (oneHop.Hop != 0 || oneHop.Phase != PayloadPhase.Establishing)
                    throw new InvalidOperationException("One-hop capture timing drifted");
                Capture("04-one-hop-immediate-red");

                host.Payloads.Spawn("DB", PayloadMotionProfile.Steady,
                    "EXT1", new[] { 20f, 20f, 20f });
                host.StepSimulation(3.2f);
                host.Payloads.Spawn("DB", PayloadMotionProfile.Steady,
                    "API", new[] { 20f });
                host.StepSimulation(2f);
                host.Payloads.Spawn("DB", PayloadMotionProfile.Variable,
                    "EXT1", new[] { 20f, 20f, 20f });
                host.StepSimulation(2f);
                host.Payloads.Spawn("DB", PayloadMotionProfile.Variable,
                    "API", new[] { 20f });
                host.StepSimulation(1.6f);
                Capture("05-crowded-orange-red");
                Debug.Log("PayloadStrikeCapture PASS: five 1920x1080 Play Mode frames in " + output);
            }
            catch (Exception error) { failure = error; Debug.LogException(error); }
            finally
            {
                Time.timeScale = previousTimeScale;
                if (host != null)
                {
                    host.enabled = previousHostEnabled;
                    traffic.AutoGenerationEnabled = previousAuto;
                }
                if (canvas != null)
                {
                    canvas.renderMode = previousMode;
                    canvas.worldCamera = previousCamera;
                }
                if (captureCamera != null) Destroy(captureCamera.gameObject);
                if (target != null) Destroy(target);
            }
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }

        private void Capture(string name)
        {
            paths.RefreshLayouts();
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
