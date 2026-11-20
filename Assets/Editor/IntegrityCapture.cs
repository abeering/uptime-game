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
    public static class IntegrityCapture
    {
        private const string Pending = "Uptime.IntegrityCapture.Pending";
        static IntegrityCapture() => EditorApplication.update += Launch;
        public static void Run()
        {
            EditorSceneManager.OpenScene(IntegritySceneAuthoring.Scene);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Integrity visual witness").AddComponent<IntegrityCaptureRunner>();
        }
    }
    public sealed class IntegrityCaptureRunner : MonoBehaviour
    {
        private Camera cameraForCapture;
        private Canvas canvas;
        private SimulationHost host;
        private Terminal terminal;
        private TMP_InputField input;
        private RenderMode oldMode;
        private Camera oldCamera;
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
            traffic.RandomizeSeed = false; traffic.Seed = 9001; traffic.AutoGenerationEnabled = false;
            // Deliberate development fixture, not a natural run. Step only through the actual host.
            host.enabled = false;
            host.ResetSimulation();
            terminal = FindFirstObjectByType<Terminal>();
            input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
            canvas = terminal.GetComponentInParent<Canvas>();
            oldMode = canvas.renderMode; oldCamera = canvas.worldCamera;
            cameraForCapture = new GameObject("Integrity review camera", typeof(Camera)).GetComponent<Camera>();
            cameraForCapture.clearFlags = CameraClearFlags.SolidColor;
            cameraForCapture.backgroundColor = new Color(.025f, .065f, .08f);
            cameraForCapture.orthographic = true;
            cameraForCapture.transform.position = new Vector3(0, 0, -100);
            cameraForCapture.nearClipPlane = .1f; cameraForCapture.farClipPlane = 1000;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cameraForCapture; canvas.planeDistance = 10;
            Directory.CreateDirectory(IntegrityWitness.Evidence + "/Captures");
            Submit("scan db"); host.StepSimulation(8);
            host.TryInjectInfection("API", InfectionKind.Backdoor, 0, out _);
            terminal.AppendText("VISUAL WITNESS: DB confirmed 2/2; API damage is unrevealed.", new Color(.7f, .8f, .8f));
            yield return null;
            Frames("01-neutral-hidden-and-healthy");
            Submit("scan api"); host.StepSimulation(8);
            Submit("integrityhit db");
            host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 1, out _);
            Submit("clean db");
            yield return new WaitForSecondsRealtime(2f);
            var clean = FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
            var pid = (TMP_InputField)new SerializedObject(clean).FindProperty("pidInput").objectReferenceValue;
            pid.onSubmit.Invoke(host.ActiveClean.Processes.First(p => p.CpuPercent > 80).Pid.ToString());
            yield return new WaitForSecondsRealtime(1.3f);
            Frames("02-orange-isolated-compromise-after-clean");
            Submit("integrityhit web"); Submit("integrityhit web");
            for (int i = 0; i < 6; i++) Submit("spawn product-lookup");
            host.StepSimulation(3f);
            Submit("inspect -v " + string.Join(" ", host.RuntimeConnections.Take(2).Select(c => c.ConnectionId)));
            yield return null;
            Frames("03-connected-region-crowded-links");
            host.TryInjectInfection("JOBS", InfectionKind.PhantomLoad, 16, out _);
            host.StepSimulation(3f);
            yield return null;
            Frames("04-overload-with-existing-statuses");
            Submit("clean jobs");
            host.GetComponent<ConnectionsDuelDirector>().TrySchedule("API", DuelVariant.CommandRace, 0, "capture", out _);
            yield return new WaitForSecondsRealtime(2f);
            Frames("05-duel-over-clean-before-breach");
            host.DamageIntegrity(host.FindNodeResources("API"), "development", "visual_breach");
            FindFirstObjectByType<IntegrityPresenter>().RenderNow();
            yield return null;
            Frames("06-core-breached-frozen");
            float time = host.SimulationTimeSeconds;
            host.StepSimulation(100);
            if (!host.RunEnded || host.SimulationTimeSeconds != time)
                throw new InvalidOperationException("Visual witness did not freeze.");
            host.Recorder.Save(false);
            using (var source = File.OpenRead(host.Recorder.OutputPath))
            using (var file = File.Create(IntegrityWitness.Evidence + "/Captures/development-fixture.json.gz"))
            using (var gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal))
                source.CopyTo(gzip);
            Submit("restart");
            yield return null;
            if (host.RunEnded) throw new InvalidOperationException("Restart command failed.");
            Frames("07-restart");
            Debug.Log("IntegrityCapture PASS: fourteen scripted Play Mode frames at both16:9 resolutions; freeze and terminal restart.");
        }
        private void Submit(string command) => input.onSubmit.Invoke(command);
        private void Frames(string name)
        {
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) }) Frame(name, size.x, size.y);
            host.Recorder.Mark("Integrity visual fixture: " + name);
        }
        private void Frame(string name, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                cameraForCapture.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                FindFirstObjectByType<NetworkHealthPanel>().Render();
                FindFirstObjectByType<IntegrityPresenter>().RenderNow();
                FindFirstObjectByType<NodeStatusPresenter>().RenderNow(Time.unscaledTime, 1);
                FindFirstObjectByType<LogicalPathPresenter>().RefreshLayouts();
                FindFirstObjectByType<WormPresenter>().RenderNow();
                FindFirstObjectByType<PayloadPresenter>().RenderNow();
                Canvas.ForceUpdateCanvases(); cameraForCapture.Render(); RenderTexture.active = target;
                picture.ReadPixels(new Rect(0, 0, width, height), 0, 0); picture.Apply();
                File.WriteAllBytes(IntegrityWitness.Evidence + "/Captures/" + name + "-" + width + "x" + height + ".png", picture.EncodeToPNG());
            }
            finally
            { RenderTexture.active = previous; cameraForCapture.targetTexture = null; Destroy(picture); Destroy(target); }
        }
    }
}
