using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Uptime.Connections.Display;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad] public static class BusinessImportanceCapture
    {
        private const string Pending = "Uptime.BusinessImportanceCapture";
        static BusinessImportanceCapture() => EditorApplication.update += Launch;
        public static void Run() { EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
        private static void Launch()
        {
            if (!Application.isPlaying || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false); new GameObject("Business Importance review").AddComponent<BusinessImportanceCaptureRunner>();
        }
    }
    public sealed class BusinessImportanceCaptureRunner : MonoBehaviour
    {
        private readonly List<string> checks = new();
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private void Check(bool ok, string label) { checks.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); }
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); Application.runInBackground = true; Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0; yield return null;
            Exception failure = null; var flow = Capture();
            while (true) { bool more; try { more = flow.MoveNext(); } catch (Exception e) { failure = e; break; } if (!more) break; yield return flow.Current; }
            (flow as IDisposable)?.Dispose(); Directory.CreateDirectory(BusinessImportanceWitness.Evidence);
            File.WriteAllLines(BusinessImportanceWitness.Evidence + "/graphical-checks.txt", checks);
            if (failure != null) Debug.LogException(failure); else Debug.Log("BusinessImportanceCapture PASS: " + checks.Count + " actual Play Mode checks.");
            Time.timeScale = 1; if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1); else EditorApplication.isPlaying = false;
        }
        private IEnumerator Capture()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            foreach (int width in new[] { 1920, 1280 })
            {
                EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                var h = FindFirstObjectByType<SimulationHost>(); var t = h.GetComponent<ConnectionTrafficDirector>();
                Check(t.LegitimateWorkloadMultiplier == 2.5f && t.ImportantConnectionChance == .15f && h.ImportantFailureMultiplier == 3 && h.BusinessFailureLimit == 10, scene + " authored tuning preserved + importance defaults");
                BusinessImportanceWitness.ConfigureComparison(h); h.enabled = true; Time.timeScale = 1;
                var terminal = FindFirstObjectByType<Terminal>(); var input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                var canvas = terminal.GetComponentInParent<Canvas>();
                var camera = new GameObject("Importance evidence camera", typeof(Camera)).GetComponent<Camera>(); camera.orthographic = true;
                camera.transform.position = new Vector3(0, 0, -100); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.065f,.08f);
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                var target = new RenderTexture(width, width * 9 / 16, 24); camera.targetTexture = target; yield return null;
                string prefix = h.gameObject.scene.name + "-" + width;
                input.onSubmit.Invoke("inspect i0 o0"); yield return null; yield return null;
                Check(canvas.pixelRect.width == width && canvas.pixelRect.height == width * 9 / 16, prefix + " native supported viewport");
                Save(camera, target, prefix + "-connecting");
                float until = Time.realtimeSinceStartup + 90; bool capturedReroute = false;
                while (Time.realtimeSinceStartup < until)
                {
                    var i = h.FindConnection("i0");
                    if (i != null && i.IsRerouting && !capturedReroute)
                    {
                        capturedReroute = true; Check(i.IsImportant && !i.IsDemanding && h.PresentationPalette.TryGet(i, out _), prefix + " actual REROUTING preserves designation and identity owner");
                        Save(camera, target, prefix + "-rerouting");
                    }
                    if (i != null && capturedReroute && i.IsDemanding && i.CurrentOperationIndex == 1) break;
                    yield return null;
                }
                Check(capturedReroute, prefix + " actual real-time mixed-route transition observed");
                var important = h.FindConnection("i0"); var ordinary = h.FindConnection("o0");
                Check(important.IsImportant && !ordinary.IsImportant && important.Performance == LoadPerformance.Slowed && ordinary.Performance == LoadPerformance.Slowed, prefix + " comparable ordinary/IMPORTANT slowed traffic");
                CheckColors(h, prefix); Save(camera, target, prefix + "-inspect-colors");
                input.onSubmit.Invoke("load DB"); yield return null; yield return null;
                CheckColors(h, prefix + " LOAD"); Save(camera, target, prefix + "-load-colors");
                input.onSubmit.Invoke("work DB"); yield return null; yield return null;
                CheckColors(h, prefix + " WORK"); Save(camera, target, prefix + "-work-colors");
                // Advance through the ordinary host Update, recording an actual danger state.
                until = Time.realtimeSinceStartup + 240;
                while (!important.IsComplete && important.ServiceRemainingSeconds > 12 && Time.realtimeSinceStartup < until) yield return null;
                var display = FindFirstObjectByType<ConnectionGameplayDisplay>(); var state = display.StateFor(important);
                Check(!important.IsComplete && state.dangerSeverity > 0 && state.trafficClass == DisplayTrafficClass.Important, prefix + " IMPORTANT remains marked in real service danger");
                input.onSubmit.Invoke("inspect i0 o0"); yield return null; yield return null;
                Save(camera, target, prefix + "-danger");
                h.KillConnection("o0"); h.KillConnection("i0");
                Check(h.BusinessFailureUnits == 4 && h.LegitimateFailureCount == 2 && h.BusinessFailures.Count == 2, prefix + " actual HUD source records two jobs / four failure units");
                yield return new WaitForSecondsRealtime(4.5f); CheckColors(h, prefix + " resolved history"); Save(camera, target, prefix + "-resolved-four-units");
                var panel = FindFirstObjectByType<NetworkHealthPanel>(); var counter = (TMP_Text)new SerializedObject(panel).FindProperty("businessFailureText").objectReferenceValue;
                Check(counter.text == "FAILURES 4 / 100", prefix + " existing HUD displays four units");
                h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds); Check(h.Recorder.Save(false), prefix + " ordinary gameplay recording saved");
                h.enabled = false; Destroy(camera.gameObject); Destroy(target); yield return null;
            }
            EditorSceneManager.LoadSceneInPlayMode(ConnectionDisplayLabAuthoring.ScenePath, new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            var lab = FindFirstObjectByType<ConnectionDisplayLab>(); var labCanvas = lab.GetComponent<Canvas>(); var labCamera = FindFirstObjectByType<Camera>();
            lab.heartbeat.paused = true; labCanvas.renderMode = RenderMode.ScreenSpaceCamera; labCanvas.worldCamera = labCamera; labCanvas.planeDistance = 10; labCamera.cullingMask = ~0;
            foreach (int width in new[] { 1920, 1280 })
            {
                var target = new RenderTexture(width,width*9/16,24); labCamera.targetTexture = target;
                foreach (double time in new[] { 2d, 8d, 14d })
                {
                    lab.heartbeat.SetReviewTime(time); lab.RenderNow(); yield return null; Canvas.ForceUpdateCanvases();
                    Check(lab.samples.Any(s => s.Current.trafficClass == DisplayTrafficClass.Important) && lab.samples.Any(s => s.Current.trafficClass == DisplayTrafficClass.Important && s.Current.dangerSeverity > 0), "lab important nominal/slow/danger samples " + width + " / " + time);
                    Save(labCamera, target, "Lab-" + width + "-" + time);
                }
                Destroy(target);
            }
        }
        private void CheckColors(SimulationHost h, string label)
        {
            int count = 0;
            foreach (var pill in FindObjectsByType<IdentityPillView>(FindObjectsSortMode.None))
            {
                var state = h.FindConnection(pill.DisplayedId); if (state == null || !h.PresentationPalette.TryGet(state, out var expected)) continue;
                var actual = (Color)typeof(IdentityPillView).GetField(pill.UsesSharedDisplay ? "displayIdentityColor" : "baseAccent", Private).GetValue(pill);
                Check(actual == expected, label + " shared identity " + pill.DisplayedId + " / " + pill.transform.parent.name); count++;
            }
            Check(count >= 2, label + " meaningful identity-pill coverage");
        }
        private void Save(Camera camera, RenderTexture target, string name)
        {
            Canvas.ForceUpdateCanvases(); var paths = FindFirstObjectByType<LogicalPathPresenter>(); paths?.RefreshLayouts(); paths?.RefreshSocketSizes();
            FindFirstObjectByType<NetworkHealthPanel>()?.Render(); Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target; var image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); var directory=BusinessImportanceWitness.Evidence+"/Captures";Directory.CreateDirectory(directory);
            File.WriteAllBytes(directory+"/"+name+".png",image.EncodeToPNG()); RenderTexture.active=previous; Destroy(image);
        }
    }
}
