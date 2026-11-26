using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Uptime.Connections.Display;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class ConnectionDisplayLabCapture
    {
        private const string Pending = "Uptime.DisplayLab.Capture";
        static ConnectionDisplayLabCapture() => EditorApplication.update += Launch;
        public static void BuildAndRun()
        { ConnectionDisplayLabAuthoring.Build(); ConnectionDisplayLabWitness.Run(); Run(); }
        [MenuItem("Tools/Connections/Capture Connection Display Lab Play Mode")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(ConnectionDisplayLabAuthoring.ScenePath);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Display Lab Play Mode evidence").AddComponent<ConnectionDisplayLabPlayRunner>();
        }
    }
    public sealed class ConnectionDisplayLabPlayRunner : MonoBehaviour
    {
        private readonly List<string> checks = new();
        private ConnectionDisplayLab lab;
        private Canvas canvas;
        private Camera cameraForCapture;
        private RenderTexture target;
        private Texture2D picture;
        private float oldTimeScale;
        private void Check(bool value, string detail)
        { checks.Add((value ? "PASS " : "FAIL ") + detail); if (!value) throw new Exception("DisplayLabPlay: " + detail); }
        private IEnumerator Start()
        {
            oldTimeScale = Time.timeScale; Directory.CreateDirectory(ConnectionDisplayLabWitness.Evidence);
            yield return null;
            Exception error = null; var steps = Capture();
            while (true)
            {
                object next;
                try { if (!steps.MoveNext()) break; next = steps.Current; }
                catch (Exception e) { error = e; break; }
                yield return next;
            }
            Time.timeScale = oldTimeScale;
            if (error != null) Debug.LogException(error);
            File.WriteAllLines(ConnectionDisplayLabWitness.Evidence + "/play-mode-checks.txt", checks);
            if (target != null) Destroy(target); if (picture != null) Destroy(picture);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
        private IEnumerator Capture()
        {
            Application.runInBackground = true;
            lab = FindFirstObjectByType<ConnectionDisplayLab>();
            canvas = lab.GetComponent<Canvas>(); cameraForCapture = FindFirstObjectByType<Camera>();
            Check(lab != null && cameraForCapture != null && lab.SampleCount == 26, "real Play Mode opens useful authored lab immediately");
            Check(FindFirstObjectByType<SimulationHost>() == null, "actual Play Mode remains independent of simulation");
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cameraForCapture; canvas.planeDistance = 10;
            cameraForCapture.cullingMask = ~0;
            SetViewport(1920, 1080);
            Time.timeScale = 0;
            double before = lab.heartbeat.Beats;
            yield return new WaitForSecondsRealtime(.25f);
            Check(lab.heartbeat.Beats > before, "unscaled heartbeat moves with gameplay timeScale zero");
            Click("PauseResume"); double frozen = lab.heartbeat.Beats;
            yield return new WaitForSecondsRealtime(.2f);
            Check(lab.heartbeat.paused && lab.heartbeat.Beats == frozen, "pointer click pauses presentation clock");
            Click("Replay");
            Check(lab.heartbeat.Beats == 0 && lab.samples.All(s => s.PhaseIndex == 0), "pointer replay resets every transition while paused");
            Click("PauseResume"); yield return new WaitForSecondsRealtime(.15f);
            Check(!lab.heartbeat.paused && lab.heartbeat.Beats > 0, "pointer resumes presentation animation");
            // From here, sample exact presentation timestamps in actual Play Mode.
            lab.heartbeat.enabled = false; lab.heartbeat.paused = false;
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) })
            {
                SetViewport(size.x, size.y);
                lab.heartbeat.SetReviewTime(0); lab.RenderNow(); Canvas.ForceUpdateCanvases(); yield return null;
                cameraForCapture.Render(); Canvas.ForceUpdateCanvases();
                Check(canvas.pixelRect.width == size.x && canvas.pixelRect.height == size.y, "true capture viewport " + size);
                foreach (var sample in lab.samples)
                {
                    Check(InViewport(sample.GetComponent<RectTransform>()), size + " sample bounds: " + sample.name);
                    Check(sample.title.preferredWidth <= sample.title.rectTransform.rect.width + 1,
                        size + " title fits: " + sample.name);
                    Check(sample.diagnostic.preferredWidth <= sample.diagnostic.rectTransform.rect.width + 1 &&
                        sample.phaseLabel.preferredWidth <= sample.phaseLabel.rectTransform.rect.width + 1,
                        size + " diagnostic/phase labels fit: " + sample.name);
                    if (sample.view.path.LastVertexCount == 0)
                        Debug.Log("Unrendered path " + sample.name + " active=" + sample.view.path.isActiveAndEnabled +
                            " cull=" + sample.view.path.canvasRenderer.cull + " length=" + sample.view.path.PathLength +
                            " rect=" + sample.view.path.rectTransform.rect + " world=" + sample.view.path.transform.position);
                    Check(sample.view.path.LastVertexCount > 0, size + " rail/activity mesh rendered: " + sample.name);
                }
                foreach (double t in new[] { 0.0, 8.2, 16.4 })
                {
                    lab.heartbeat.SetReviewTime(t); lab.RenderNow(); Canvas.ForceUpdateCanvases();
                    Save(ConnectionDisplayLabWitness.Evidence + "/overview-" + t.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s-" + size.x + "x" + size.y + ".png");
                }
            }
            // Disposable comparisons use the same six authored rows and shared renderer.
            var paletteRows = lab.samples.Where(s => s.phases[0].state.identity.StartsWith("a")).ToArray();
            var states = paletteRows.Select(s => s.phases[0].state).ToArray();
            var titles = paletteRows.Select(s => s.title.text).ToArray();
            try
            {
                foreach (DisplayTrafficClass family in Enum.GetValues(typeof(DisplayTrafficClass)))
                {
                    for (int i = 0; i < paletteRows.Length; i++)
                    {
                        var state = states[i]; state.trafficClass = family; state.identitySlot = i;
                        paletteRows[i].phases[0].state = state;
                        paletteRows[i].title.SetText(family.ToString().ToUpperInvariant() + " " + (i + 1) + " / " + state.operation.ToString().ToUpperInvariant());
                    }
                    foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) })
                    {
                        SetViewport(size.x, size.y); lab.heartbeat.SetReviewTime(0); lab.RenderNow(); Canvas.ForceUpdateCanvases();
                        Save(ConnectionDisplayLabWitness.Evidence + "/palette-" + family.ToString().ToLowerInvariant() + "-" + size.x + "x" + size.y + ".png");
                    }
                    Check(paletteRows.Select(s => s.Current.identitySlot).Distinct().Count() == 6,
                        family + " palette comparison uses six stable shared identity colors");
                }
            }
            finally
            {
                for (int i = 0; i < paletteRows.Length; i++)
                { paletteRows[i].phases[0].state = states[i]; paletteRows[i].title.SetText(titles[i]); }
            }
            Color[] originalRail = lab.style.ordinaryIdentityColors;
            try
            {
                lab.style.ordinaryIdentityColors = Enumerable.Repeat(Color.white, 6).ToArray();
                lab.heartbeat.SetReviewTime(0); lab.RenderNow(); Canvas.ForceUpdateCanvases();
                SetViewport(1920, 1080);
                Save(ConnectionDisplayLabWitness.Evidence + "/shared-style-proof.png");
                Check(lab.samples[0].view.style == lab.style, "shared style proof uses the same asset in the lab");
            }
            finally { lab.style.ordinaryIdentityColors = originalRail; }
            // A reproducible real Unity rendering of an 18-second presentation timeline.
            // PNG encoding allocations are capture instrumentation, outside renderer measurements.
            string frames = "/tmp/uptime-display-lab-motion"; Directory.CreateDirectory(frames);
            SetViewport(1280, 720);
            for (int i = 0; i < 30; i++) { lab.heartbeat.SetReviewTime(i * .13); lab.RenderNow(); Canvas.ForceUpdateCanvases(); yield return null; }
            int objects = canvas.GetComponentsInChildren<Transform>(true).Length;
            int maxVertices = 0;
            for (int frame = 0; frame < 432; frame++)
            {
                lab.heartbeat.SetReviewTime(frame / 24.0); lab.RenderNow(); Canvas.ForceUpdateCanvases();
                Save(frames + "/frame-" + frame.ToString("0000") + ".png");
                int total = 0; foreach (var sample in lab.samples) total += sample.view.path.LastVertexCount;
                maxVertices = Mathf.Max(maxVertices, total);
                yield return null;
            }
            Check(canvas.GetComponentsInChildren<Transform>(true).Length == objects,
                "432 motion frames create no presentation GameObjects; hierarchy=" + objects);
            Check(maxVertices < 50000, "shared path meshes remain bounded; peak path vertices=" + maxVertices);
            Check(lab.samples.Where(s => s.name.Contains("AT SPEED / SEVERELY")).All(s => s.Current.productiveSpeed == 1 && s.Current.dangerSeverity > .7f),
                "full-speed endangered comparison remains present throughout motion capture");
            Debug.Log("ConnectionDisplayLabCapture PASS: " + checks.Count + " assertions, six overview PNGs and 432 actual Play Mode frames at 24fps.");
        }
        private void SetViewport(int width, int height)
        {
            if (target != null) Destroy(target); if (picture != null) Destroy(picture);
            target = new RenderTexture(width, height, 24);
            picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            cameraForCapture.targetTexture = target; Canvas.ForceUpdateCanvases();
        }
        private void Save(string file)
        {
            cameraForCapture.Render(); RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
            picture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); picture.Apply();
            File.WriteAllBytes(file, picture.EncodeToPNG()); RenderTexture.active = previous;
        }
        private bool InViewport(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var c in corners)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cameraForCapture, c);
                if (p.x < -1 || p.y < -1 || p.x > target.width + 1 || p.y > target.height + 1) return false;
            }
            return true;
        }
        private void Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            var button = canvas.GetComponentsInChildren<Button>().Single(b => b.name == name);
            var rect = button.GetComponent<RectTransform>();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(cameraForCapture,
                rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Check(hits.Count > 0 && hits[0].gameObject == button.gameObject, "unobstructed pointer hit: " + name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
