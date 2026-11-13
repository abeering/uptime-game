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
    public static class SurpriseCapture
    {
        private const string Pending = "Uptime.SurpriseCapture.Pending";
        private static int frames;

        static SurpriseCapture() => EditorApplication.update += Launch;

        [MenuItem("Tools/Connections/Capture Surprise Play Mode Review")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            frames = 0;
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying ||
                ++frames < 20) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Surprise capture runner")
                .AddComponent<SurpriseCaptureRunner>().Begin();
        }

        public static string Argument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : fallback;
        }
    }

    public sealed class SurpriseCaptureRunner : MonoBehaviour
    {
        private SimulationHost host;
        private ConnectionTrafficDirector traffic;
        private NodeStatusPresenter statuses;
        private LogicalPathPresenter paths;
        private WormPresenter worms;
        private PayloadPresenter payloads;
        private Canvas canvas;
        private Camera captureCamera;
        private RenderTexture target;
        private RenderMode oldMode;
        private Camera oldCamera;
        private string output;
        private int width, height;
        private float oldScale;

        public void Begin() => StartCoroutine(Guard(Cases()));

        private IEnumerator Guard(IEnumerator cases)
        {
            Exception failure = null;
            while (true)
            {
                object next;
                try { if (!cases.MoveNext()) break; next = cases.Current; }
                catch (Exception error) { failure = error; break; }
                yield return next;
            }
            if (failure != null) Debug.LogException(failure);
            Time.timeScale = oldScale;
            if (canvas != null)
            { canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; }
            if (captureCamera != null) Destroy(captureCamera.gameObject);
            if (target != null) Destroy(target);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }

        private IEnumerator Cases()
        {
            yield return null;
            host = FindFirstObjectByType<SimulationHost>();
            traffic = host.GetComponent<ConnectionTrafficDirector>();
            statuses = FindFirstObjectByType<NodeStatusPresenter>();
            paths = FindFirstObjectByType<LogicalPathPresenter>();
            worms = FindFirstObjectByType<WormPresenter>();
            payloads = FindFirstObjectByType<PayloadPresenter>();
            canvas = FindFirstObjectByType<Terminal>().GetComponentInParent<Canvas>();
            if (host == null || traffic == null || statuses == null || paths == null ||
                worms == null || payloads == null || canvas == null)
                throw new InvalidOperationException("Surprise capture bindings missing");
            width = int.Parse(SurpriseCapture.Argument("-surpriseWidth", "1920"));
            height = int.Parse(SurpriseCapture.Argument("-surpriseHeight", "1080"));
            output = SurpriseCapture.Argument("-surpriseCapturePath",
                "Docs/Review/ConnectionsSurpriseV0/Captures");
            Directory.CreateDirectory(output);
            oldScale = Time.timeScale;
            Time.timeScale = 0f;
            oldMode = canvas.renderMode;
            oldCamera = canvas.worldCamera;
            captureCamera = new GameObject("Surprise capture camera", typeof(Camera))
                .GetComponent<Camera>();
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = new Color(.025f, .065f, .08f);
            captureCamera.orthographic = true;
            captureCamera.transform.position = new Vector3(0f, 0f, -100f);
            captureCamera.nearClipPlane = .1f;
            captureCamera.farClipPlane = 1000f;
            target = new RenderTexture(width, height, 24);
            captureCamera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = captureCamera;
            canvas.planeDistance = 10f;
            yield return null;

            CaptureTactic("breach", ConsequenceTactic.BreachLockdown, 1f);
            CaptureTactic("worm", ConsequenceTactic.ConditionWormOutbreak, 12f);
            CaptureTactic("burst", ConsequenceTactic.PayloadBurst, 2f);
            Debug.Log("SurpriseCapture PASS: Advanced " + width + "x" + height +
                " three scripted Play Mode frames in " + output);
        }

        private void CaptureTactic(string id, ConsequenceTactic tactic, float afterSeconds)
        {
            traffic.RandomizeSeed = false;
            traffic.Seed = 1701;
            host.SetRunMode(ConnectionRunMode.Generated);
            traffic.AutoGenerationEnabled = true;
            traffic.HostileGenerationEnabled = false;
            host.StepSimulation(35f); // Keep ordinary generated traffic visible.
            ConnectionSpawnPlan benign = traffic.GenerateBenignPlan(
                host.SimulationTimeSeconds);
            if (benign == null) throw new InvalidOperationException(
                "No ordinary benign capture plan available");
            traffic.QueueSpawnPlan(benign, "scripted_benign_capture");
            host.StepSimulation(.5f);
            if (!ConsequencePlanner.TryPlan(host, "capture-" + id,
                "scripted_capture", "API", null, ConsequenceConfiguration.Explicit(
                    ConsequenceCandidate.Tactic(id, tactic)), 77,
                out ConsequencePlan plan, out string reason))
                throw new InvalidOperationException(reason);
            ConsequenceExecution execution = ConsequenceExecutor.Execute(host, plan);
            if (!execution.AnySucceeded)
                throw new InvalidOperationException("Tactic did not execute: " + id);
            host.StepSimulation(afterSeconds);
            Frame(id);
        }

        private void Frame(string name)
        {
            FindFirstObjectByType<NetworkHealthPanel>()?.Render();
            statuses.RenderNow(Time.unscaledTime, 1f);
            paths.Bind(host.RuntimeConnections.ToArray());
            paths.RefreshLayouts();
            worms.RenderNow();
            payloads.RenderNow();
            Canvas.ForceUpdateCanvases();
            captureCamera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    picture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    picture.Apply();
                    File.WriteAllBytes(Path.Combine(output, name + "-" + width + "x" +
                        height + ".png"), picture.EncodeToPNG());
                }
                finally { Destroy(picture); }
            }
            finally { RenderTexture.active = previous; }
        }
    }
}
