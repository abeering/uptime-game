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
    public static class WorkShiftCapture
    {
        private const string Pending = "Uptime.WorkShiftCapture";
        static WorkShiftCapture() => EditorApplication.update += Launch;
        public static void Run()
        {
            WorkShiftWitness.Author();
            EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("WORK graphical witness").AddComponent<WorkShiftCaptureRunner>();
        }
    }
    public sealed class WorkShiftCaptureRunner : MonoBehaviour
    {
        private SimulationHost host;
        private TMP_InputField input;
        private Camera cameraForCapture;
        private Canvas canvas;
        private readonly List<string> checks = new();
        private string folder;
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            yield return null;
            Exception failure = null; var capture = Capture();
            while (true)
            {
                object next;
                try { if (!capture.MoveNext()) break; next = capture.Current; }
                catch (Exception error) { failure = error; break; }
                yield return next;
            }
            Directory.CreateDirectory(WorkShiftWitness.Evidence);
            File.WriteAllLines(WorkShiftWitness.Evidence + "/graphical-checks.txt", checks);
            if (failure != null) Debug.LogException(failure);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
        private void Check(bool value, string detail)
        { if (!value) throw new InvalidOperationException("WORK graphical FAIL: " + detail); checks.Add("PASS " + host.gameObject.scene.name + " " + detail); }
        private void Set(string name, object value) => IntegrityWitness.Set(host, name, value);
        private IEnumerator Capture()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                if (SceneManager.GetActiveScene().path != scene)
                { EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single)); yield return null; }
                host = FindFirstObjectByType<SimulationHost>(); host.enabled = false;
                var traffic = host.GetComponent<ConnectionTrafficDirector>();
                traffic.RandomizeSeed = false; traffic.Seed = 1701; traffic.AutoGenerationEnabled = false;
                host.ResetSimulation();
                float canonicalTarget = host.ShiftWorkTarget;
                var terminal = FindFirstObjectByType<Terminal>();
                input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                canvas = terminal.GetComponentInParent<Canvas>();
                cameraForCapture = new GameObject("WORK review camera", typeof(Camera)).GetComponent<Camera>();
                cameraForCapture.clearFlags = CameraClearFlags.SolidColor; cameraForCapture.backgroundColor = new Color(.025f, .065f, .08f);
                cameraForCapture.orthographic = true; cameraForCapture.transform.position = new Vector3(0, 0, -100);
                cameraForCapture.nearClipPlane = .1f; cameraForCapture.farClipPlane = 1000;
                RenderMode oldMode = canvas.renderMode; Camera oldCamera = canvas.worldCamera;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cameraForCapture; canvas.planeDistance = 10;
                var viewport = new RenderTexture(1920, 1080, 24); cameraForCapture.targetTexture = viewport;
                folder = WorkShiftWitness.Evidence + "/Captures/" + host.gameObject.scene.name; Directory.CreateDirectory(folder);
                yield return null; yield return null; Frames("01-start");
                WorkShiftWitness.Add(host, "w1", new[] { 720f }); host.StepSimulation(152);
                for (int i = 0; i < 6; i++) traffic.QueueProfile("product-lookup"); host.StepSimulation(3);
                var ids = host.RuntimeConnections.Where(c => c.IsActive).Take(3).Select(c => c.ConnectionId).ToArray();
                input.onSubmit.Invoke("inspect -v " + string.Join(" ", ids));
                yield return null; yield return null;
                Check(input.isFocused, "inspect returns terminal focus with crowded network"); Frames("02-work-and-inspect");
                Set("shiftWorkTarget", canonicalTarget); Set("connectionTravelSecondsPerLink", .000001f); host.ResetSimulation();
                input.onSubmit.Invoke("clean db"); yield return new WaitForSecondsRealtime(2);
                var clean = FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
                var pid = (TMP_InputField)new SerializedObject(clean).FindProperty("pidInput").objectReferenceValue;
                Check(pid.isFocused && host.ActiveClean != null, "CLEAN owns input before ending");
                host.GetComponent<ConnectionsDuelDirector>().TrySchedule("AUTH", DuelVariant.CommandRace, 0, "work-graphical", out _);
                var encounter = host.GetComponent<ConnectionsDuelDirector>().Current;
                yield return new WaitForSecondsRealtime(1);
                WorkShiftWitness.Add(host, "win", new[] { canonicalTarget }); host.StepSimulation(350);
                yield return null; yield return null;
                Check(host.Ending == RunEnding.WorkComplete && input.isFocused && !clean.gameObject.activeInHierarchy &&
                    !FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include).gameObject.activeInHierarchy &&
                    encounter.Outcome == null, "WORK success aborts active Duel/CLEAN without concession and restores input");
                Frames("03-complete");
                int seed = traffic.Seed; input.onSubmit.Invoke("restart"); yield return null; yield return null;
                Check(!host.RunEnded && host.CreditedWork == 0 && traffic.Seed == seed && input.isFocused,
                    "terminal restart clears WORK/ending and retains seed/focus"); Frames("04-restart");
                Set("shiftDurationSeconds", 1f); Set("shiftWorkTarget", 1000f); host.ResetSimulation();
                input.onSubmit.Invoke("scan db"); host.StepSimulation(2); yield return null; yield return null;
                Check(host.Ending == RunEnding.InsufficientWork && host.ActiveScan != null && input.isFocused,
                    "timeout freezes scan and restores result/restart input"); Frames("05-insufficient");
                host.ResetSimulation();
                var selector = new HackerDirectiveSelector(host.Integrity, host.PhysicalLinks, HackerDirective.BREACH, 1701); selector.EnsureOffensive(0);
                foreach (string id in selector.Offensive.path)
                {
                    var node = host.FindNodeResources(id); var state = host.Integrity.For(node); if (state.External) continue;
                    while (state.Value > 0) host.DamageIntegrity(node, "graphical", "CORE");
                }
                yield return null; yield return null;
                Check(host.Ending == RunEnding.CoreBreached && input.isFocused, "CORE uses same result/restart interface"); Frames("06-core");
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
                Destroy(cameraForCapture.gameObject); Destroy(viewport); yield return null;
            }
            Debug.Log("WorkShiftCapture PASS: real Play Mode both scenes, 24 frames at two 16:9 resolutions, all endings and focus.");
        }
        private void Frames(string name)
        { Frame(name, 1920, 1080); Frame(name, 1280, 720); }
        private void Frame(string name, int width, int height)
        {
            var target = new RenderTexture(width, height, 24); var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active, previousTarget = cameraForCapture.targetTexture;
            try
            {
                cameraForCapture.targetTexture = target; Canvas.ForceUpdateCanvases();
                var panel = FindFirstObjectByType<NetworkHealthPanel>(); panel.Render(); FindFirstObjectByType<IntegrityPresenter>().RenderNow();
                FindFirstObjectByType<LogicalPathPresenter>().RefreshLayouts();
                var fields = new SerializedObject(panel);
                var texts = new[] { "clockText", "valueText", "rateText" }.Select(n => (TMP_Text)fields.FindProperty(n).objectReferenceValue).ToArray();
                Check(canvas.pixelRect.width == width && canvas.pixelRect.height == height, name + " actual viewport " + width + "x" + height);
                foreach (TMP_Text text in texts)
                {
                    text.ForceMeshUpdate(); Check(text.gameObject.activeInHierarchy && text.preferredWidth <= text.rectTransform.rect.width + 1,
                        name + "/" + width + " readable " + text.gameObject.name);
                }
                var tools = panel.transform.Find("ToolReadinessRow").GetComponent<RectTransform>();
                float Right(RectTransform r) { var corners = new Vector3[4]; r.GetWorldCorners(corners); return corners[2].x; }
                var toolCorners = new Vector3[4]; tools.GetWorldCorners(toolCorners);
                float Left(RectTransform r) { var corners = new Vector3[4]; r.GetWorldCorners(corners); return corners[0].x; }
                var fill = (UnityEngine.UI.Image)fields.FindProperty("healthFill").objectReferenceValue;
                var bar = (RectTransform)fill.transform.parent;
                Check(Right(texts[0].rectTransform) < Left(texts[1].rectTransform) &&
                    Right(texts[1].rectTransform) < Left(bar) && Right(bar) < Left(texts[2].rectTransform),
                    name + "/" + width + " clock, WORK, bar and delta have separate bounds");
                Check(Right(texts[2].rectTransform) < toolCorners[0].x, name + "/" + width + " objective clears tool indicators");
                Canvas.ForceUpdateCanvases(); cameraForCapture.Render(); RenderTexture.active = target;
                picture.ReadPixels(new Rect(0, 0, width, height), 0, 0); picture.Apply();
                File.WriteAllBytes(folder + "/" + name + "-" + width + "x" + height + ".png", picture.EncodeToPNG());
            }
            finally { cameraForCapture.targetTexture = previousTarget; RenderTexture.active = previous; Destroy(picture); Destroy(target); }
        }
    }
}
