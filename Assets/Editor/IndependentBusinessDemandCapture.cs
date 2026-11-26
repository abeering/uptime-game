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
    [InitializeOnLoad] public static class IndependentBusinessDemandCapture
    {
        private const string Pending = "Uptime.IndependentBusinessDemandCapture";
        static IndependentBusinessDemandCapture() => EditorApplication.update += Launch;
        public static void Run()
        {
            EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Independent business gameplay evidence").AddComponent<IndependentBusinessDemandCaptureRunner>();
        }
    }
    [Serializable] public sealed class BusinessGameplayReceipt
    {
        public string type, id, reason;
        public int sequence;
        public double idealDue, nextDue;
        public float observedAt;
        public ConnectionDefinitionRecord definition;
    }
    [Serializable] public sealed class BusinessGameplayEvidence
    {
        public string scene;
        public int seed;
        public float workload;
        public TrafficConfiguration traffic;
        public BusinessDemandSnapshot demand;
        public ShiftSnapshot shift;
        public BusinessGameplayReceipt[] receipts;
    }
    public sealed class IndependentBusinessDemandCaptureRunner : MonoBehaviour
    {
        private readonly List<string> checks = new();
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
            (run as IDisposable)?.Dispose(); Directory.CreateDirectory(IndependentBusinessDemandWitness.Evidence);
            File.WriteAllLines(IndependentBusinessDemandWitness.Evidence + "/graphical-checks.txt", checks);
            if (failure != null) Debug.LogException(failure);
            else Debug.Log("IndependentBusinessDemandCapture PASS: " + checks.Count + " checks; actual canonical Play Mode samples.");
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1); else EditorApplication.isPlaying = false;
        }
        private IEnumerator Capture()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                if (SceneManager.GetActiveScene().path != scene)
                { EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single)); yield return null; }
                var h = FindFirstObjectByType<SimulationHost>(); h.enabled = false; var t = h.GetComponent<ConnectionTrafficDirector>();
                Check(t.LegitimateWorkloadMultiplier == 2.5f && t.AutoGenerationEnabled && t.HostileGenerationEnabled && h.ShiftEnabled && h.IntegrityEnabled,
                    scene + " actual canonical mixed traffic/objectives/Integrity and accepted workload");
                t.RandomizeSeed = false; t.Seed = 1701; h.ResetSimulation();
                var terminal = FindFirstObjectByType<Terminal>(); var input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                var canvas = terminal.GetComponentInParent<Canvas>(); var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
                var camera = new GameObject("Business demand evidence camera", typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .065f, .08f); camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -100);
                var viewport = new RenderTexture(1280, 720, 24); camera.targetTexture = viewport;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; yield return null; yield return null;
                foreach (float target in new[] { 45f, 90f })
                {
                    var duel = h.GetComponent<ConnectionsDuelDirector>();
                    while (h.SimulationTimeSeconds + .0001f < target && !h.RunEnded && !duel.PausesSimulation) h.StepSimulation(.02f);
                    string[] ids = h.RuntimeConnections.Where(c => !c.IsHostile && c.IsActive).Take(2).Select(c => c.ConnectionId).ToArray();
                    if (!duel.PausesSimulation && ids.Length > 0) input.onSubmit.Invoke("inspect -v " + string.Join(" ", ids));
                    yield return null; yield return null;
                    var d = t.BusinessDemand;
                    Check(d.arrived > 0 && d.admitted + d.waiting + d.unadmitted == d.arrived &&
                        h.Recorder.CurrentLog.run.traffic.legitimateMeanArrivalSeconds == t.LegitimateMeanArrivalSeconds,
                        scene + " sample " + target + ": actual time=" + h.SimulationTimeSeconds + ", demand=" + d.arrived + ", admitted=" + d.admitted + ", ending=" + h.Ending + ", Duel pause=" + duel.PausesSimulation);
                    Check(canvas.pixelRect.width == 1280 && canvas.pixelRect.height == 720, scene + " actual 1280x720 viewport");
                    Canvas.ForceUpdateCanvases(); FindFirstObjectByType<NetworkHealthPanel>().Render();
                    var paths = FindFirstObjectByType<LogicalPathPresenter>(); paths.RefreshLayouts(); paths.RefreshSocketSizes();
                    foreach (var path in FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None)) path.AdvanceVisual(.2f);
                    paths.RefreshLayouts(); paths.RefreshSocketSizes(); Canvas.ForceUpdateCanvases(); camera.Render();
                    var old = RenderTexture.active; RenderTexture.active = viewport; var picture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                    picture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); picture.Apply(); Directory.CreateDirectory(IndependentBusinessDemandWitness.Evidence + "/Captures");
                    File.WriteAllBytes(IndependentBusinessDemandWitness.Evidence + "/Captures/" + h.gameObject.scene.name + "-" + target + ".png", picture.EncodeToPNG());
                    RenderTexture.active = old; Destroy(picture);
                    if (h.RunEnded || duel.PausesSimulation) break; // Preserve real stopping/pause boundaries, never invent later gameplay.
                }
                h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds);
                var log = h.Recorder.CurrentLog;
                var evidence = new BusinessGameplayEvidence { scene = h.gameObject.scene.name, seed = t.Seed, workload = t.LegitimateWorkloadMultiplier,
                    traffic = log.run.traffic, demand = t.BusinessDemand, shift = h.Shift,
                    receipts = log.events.Where(e => e.type.StartsWith("business_demand_") || e.type == "plan_queued").Select(e =>
                        new BusinessGameplayReceipt { type = e.type, id = e.connectionId, sequence = e.demandSequence, idealDue = e.demandDueAtSeconds,
                            nextDue = e.demandNextAtSeconds, observedAt = e.timeSeconds, reason = e.reason, definition = e.definition }).ToArray() };
                File.WriteAllText(IndependentBusinessDemandWitness.Evidence + "/" + h.gameObject.scene.name + "-gameplay.json", JsonUtility.ToJson(evidence, true));
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; Destroy(camera.gameObject); Destroy(viewport); yield return null;
            }
        }
    }
}
