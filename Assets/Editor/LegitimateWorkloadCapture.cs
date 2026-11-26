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
using Uptime.Connections.Display;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad] public static class LegitimateWorkloadCapture
    {
        private const string Pending = "Uptime.LegitimateWorkloadCapture";
        static LegitimateWorkloadCapture() => EditorApplication.update += Launch;
        public static void Run()
        {
            EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Workload Play Mode comparison").AddComponent<LegitimateWorkloadCaptureRunner>();
        }
    }
    public sealed class LegitimateWorkloadCaptureRunner : MonoBehaviour
    {
        private readonly List<string> checks = new();
        private readonly List<string> rows = new() { "scene,multiplier,id,requested_load,allocated_load,processing_work,completed_processing,business_work,service_budget,credited_work" };
        private void Check(bool ok, string label)
        { checks.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); }
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); yield return null;
            var run = Capture(); Exception failure = null;
            while (true)
            {
                bool more; try { more = run.MoveNext(); } catch (Exception e) { failure = e; break; }
                if (!more) break; yield return run.Current;
            }
            (run as IDisposable)?.Dispose();
            Directory.CreateDirectory(LegitimateWorkloadWitness.Evidence);
            File.WriteAllLines(LegitimateWorkloadWitness.Evidence + "/graphical-checks.txt", checks);
            File.WriteAllLines(LegitimateWorkloadWitness.Evidence + "/play-mode-comparison.csv", rows);
            if (failure != null) Debug.LogException(failure);
            else Debug.Log("LegitimateWorkloadCapture PASS: " + checks.Count + " checks, four actual Play Mode frames.");
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1); else EditorApplication.isPlaying = false;
        }
        private IEnumerator Capture()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                if (SceneManager.GetActiveScene().path != scene)
                { EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single)); yield return null; }
                var h = FindFirstObjectByType<SimulationHost>(); h.enabled = false;
                var t = h.GetComponent<ConnectionTrafficDirector>();
                Check(t.LegitimateWorkloadMultiplier == 1.5f && h.ShiftEnabled, scene + " authored workload and objectives enabled");
                t.RandomizeSeed = false; t.Seed = 1701;
                var terminal = FindFirstObjectByType<Terminal>();
                var input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                var canvas = terminal.GetComponentInParent<Canvas>(); var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
                var camera = new GameObject("Workload comparison camera", typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .065f, .08f);
                camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -100);
                var viewport = new RenderTexture(1280, 720, 24); camera.targetTexture = viewport;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                yield return null; yield return null; // Settle the actual viewport before identity packing caches dimensions.
                foreach (float multiplier in new[] { 1f, 1.5f })
                {
                    LegitimateWorkloadWitness.ConfigureQueue(h, multiplier);
                    var a = t.QueueProfile("product-lookup"); var b = t.QueueProfile("product-lookup");
                    h.StepSimulation((a.Route.Length - 1) * h.ConnectionTravelSecondsPerLink + 10);
                    input.onSubmit.Invoke("inspect -v " + a.ConnectionId + " " + b.ConnectionId);
                    yield return null; yield return null;
                    Check(h.Recorder.CurrentLog.run.legitimateWorkloadMultiplier == multiplier && !h.RunEnded && input.isFocused,
                        scene + " " + multiplier + "x metadata, live shift and terminal focus");
                    foreach (var plan in new[] { a, b })
                    {
                        var c = h.FindConnection(plan.ConnectionId);
                        Check(c.IsActive && c.DesiredLoad == 5 * multiplier && c.CurrentTotalWork == 100 * multiplier && c.BusinessWorkValue == 100 &&
                            Math.Abs(c.ServiceBudgetSeconds - 30) < .001 && h.CreditedWork == 0,
                            scene + " " + multiplier + "x two real queued jobs: scaled processing, same business value/budget, no partial credit");
                        Check(Math.Abs(c.AllocatedLoad - 5) < .001 && c.Performance == (multiplier == 1 ? LoadPerformance.AtSpeed : LoadPerformance.Slowed),
                            scene + " " + multiplier + "x existing allocator/performance treatment");
                        rows.Add(FormattableString.Invariant($"{h.gameObject.scene.name},{multiplier},{c.ConnectionId},{c.DesiredLoad},{c.AllocatedLoad},{c.CurrentTotalWork},{c.CompletedWork},{c.BusinessWorkValue},{c.ServiceBudgetSeconds},{h.CreditedWork}"));
                    }
                    Canvas.ForceUpdateCanvases(); FindFirstObjectByType<NetworkHealthPanel>().Render();
                    var paths = FindFirstObjectByType<LogicalPathPresenter>(); paths.RefreshLayouts(); paths.RefreshSocketSizes();
                    foreach (var path in FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None)) path.AdvanceVisual(1);
                    paths.RefreshLayouts(); paths.RefreshSocketSizes(); Canvas.ForceUpdateCanvases();
                    camera.Render(); var old = RenderTexture.active; RenderTexture.active = viewport;
                    var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                    Directory.CreateDirectory(LegitimateWorkloadWitness.Evidence + "/Captures");
                    File.WriteAllBytes(LegitimateWorkloadWitness.Evidence + "/Captures/" + h.gameObject.scene.name + (multiplier == 1 ? "-1x.png" : "-1.5x.png"), image.EncodeToPNG());
                    RenderTexture.active = old; Destroy(image);
                }
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; Destroy(camera.gameObject); Destroy(viewport); yield return null;
            }
        }
    }
}
