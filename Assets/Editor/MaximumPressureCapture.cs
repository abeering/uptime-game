using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class MaximumPressureCapture
    {
        private const string Pending = "Uptime.MaximumPressureCapture.Pending";
        static MaximumPressureCapture() => EditorApplication.update += Launch;

        [MenuItem("Tools/Connections/Capture Maximum Pressure Play Mode Review")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(MaximumPressureWitness.Pressure);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Maximum Pressure capture runner").AddComponent<MaximumPressureCaptureRunner>();
        }
    }

    public sealed class MaximumPressureCaptureRunner : MonoBehaviour
    {
        private Camera cameraForCapture;
        private Canvas canvas;
        private RenderMode oldMode;
        private Camera oldCamera;
        private SimulationHost host;
        private string output;

        private IEnumerator Start()
        {
            yield return null;
            Exception failure = null;
            IEnumerator run = CaptureRun();
            while (true)
            {
                object next;
                try { if (!run.MoveNext()) break; next = run.Current; }
                catch (Exception error) { failure = error; break; }
                yield return next;
            }
            if (failure != null) Debug.LogException(failure);
            if (canvas != null) { canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; }
            if (cameraForCapture != null) Destroy(cameraForCapture.gameObject);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }

        private IEnumerator CaptureRun()
        {
            host = FindFirstObjectByType<SimulationHost>();
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            traffic.RandomizeSeed = false;
            traffic.Seed = 9001;
            host.ResetSimulation();
            host.Recorder.BeginRun();
            canvas = FindFirstObjectByType<Terminal>().GetComponentInParent<Canvas>();
            oldMode = canvas.renderMode;
            oldCamera = canvas.worldCamera;
            output = MaximumPressureWitness.Evidence + "/Captures";
            Directory.CreateDirectory(output);
            cameraForCapture = new GameObject("Pressure review camera", typeof(Camera)).GetComponent<Camera>();
            cameraForCapture.clearFlags = CameraClearFlags.SolidColor;
            cameraForCapture.backgroundColor = new Color(.025f, .065f, .08f);
            cameraForCapture.orthographic = true;
            cameraForCapture.transform.position = new Vector3(0, 0, -100);
            cameraForCapture.nearClipPlane = .1f;
            cameraForCapture.farClipPlane = 1000;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cameraForCapture;
            canvas.planeDistance = 10;
            // Real 1x Play Mode: no forced opportunities, entity spawns, time skipping,
            // frozen races, hidden-state intervention, or alterations to scene tuning.
            foreach (int second in new[] { 90, 150, 175 })
            {
                while (host.SimulationTimeSeconds < second) yield return null;
                var terminal = FindFirstObjectByType<Terminal>();
                var input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                // Information requests only; onSubmit uses the normal focus/interrupt guard.
                string ids = string.Join(" ", host.RuntimeConnections.Where(c => c.IsActive).Take(2)
                    .Select(c => c.ConnectionId));
                input.onSubmit.Invoke(second == 150 ? "work db" : "inspect -v " + ids);
                yield return null;
                foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) })
                {
                    Frame(second, size.x, size.y);
                    yield return null;
                }
                host.Recorder.Mark("Maximum Pressure natural Play Mode capture at " + second);
                Debug.Log("PRESSURE CAPTURE sim=" + host.SimulationTimeSeconds + " health=" + host.Health.Value +
                    " liveConnections=" + host.RuntimeConnections.Count(c => c.IsActive || c.IsConnecting));
            }
            host.Recorder.EndRun();
            using (var source = File.OpenRead(host.Recorder.OutputPath))
            using (var file = File.Create(Path.Combine(output, "play-mode-run.json.gz")))
            using (var gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal))
                source.CopyTo(gzip);
            Debug.Log("MaximumPressureCapture PASS: six natural seeded Play Mode frames, both 16:9 sizes.");
        }

        private void Frame(int second, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                cameraForCapture.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                FindFirstObjectByType<NetworkHealthPanel>().Render();
                FindFirstObjectByType<NodeStatusPresenter>().RenderNow(Time.unscaledTime, 1);
                var paths = FindFirstObjectByType<LogicalPathPresenter>();
                paths.RefreshLayouts();
                FindFirstObjectByType<WormPresenter>().RenderNow();
                FindFirstObjectByType<PayloadPresenter>().RenderNow();
                Canvas.ForceUpdateCanvases();
                cameraForCapture.Render();
                RenderTexture.active = target;
                picture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                picture.Apply();
                File.WriteAllBytes(Path.Combine(output, "seed9001-" + second + "s-" + width + "x" + height + ".png"),
                    picture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                cameraForCapture.targetTexture = null;
                Destroy(picture);
                Destroy(target);
            }
        }
    }
}
