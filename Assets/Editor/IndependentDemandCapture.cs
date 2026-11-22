using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    public static class IndependentDemandCapture
    {
        private const string Pending = "Uptime.IndependentDemandCapture";
        static IndependentDemandCapture() => EditorApplication.update += Launch;
        public static void Run()
        {
            EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Independent demand graphical witness").AddComponent<IndependentDemandCaptureRunner>();
        }
    }
    public sealed class IndependentDemandCaptureRunner : MonoBehaviour
    {
        private SimulationHost host;
        private Terminal terminal;
        private ConnectionTrafficDirector traffic;
        private TMP_InputField input;
        private Camera cameraForCapture;
        private Canvas canvas;
        private LogicalConnectionState loader;
        private readonly List<string> checks = new();
        private string folder;
        private string evidence;
        private BenignArrivalPolicy policy;
        private float loadScale, dbWeight, loaderLoad, loaderSurvival;
        private float mean;
        private IEnumerator Start()
        {
            evidence = TextArgument("-demandEvidence", IndependentDemandWitness.Evidence);
            policy = (BenignArrivalPolicy)Enum.Parse(typeof(BenignArrivalPolicy), TextArgument("-demandPolicy", "IndependentDemand"));
            loadScale = Argument("-demandLoadScale", 1); dbWeight = Argument("-demandDbWeight", 1);
            loaderLoad = Argument("-demandLoaderLoad", 12); loaderSurvival = Argument("-demandLoaderSurvival", 10);
            DontDestroyOnLoad(gameObject); yield return null;
            Exception failure = null; var capture = Capture();
            while (true)
            {
                object next;
                try { if (!capture.MoveNext()) break; next = capture.Current; }
                catch (Exception error) { failure = error; break; }
                yield return next;
            }
            Directory.CreateDirectory(evidence);
            File.WriteAllLines(evidence + "/graphical-checks.txt", checks);
            if (failure != null) Debug.LogException(failure);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
        private void Check(bool value, string detail)
        { if (!value) throw new InvalidOperationException("Demand graphical FAIL: " + detail); checks.Add("PASS " + host.gameObject.scene.name + " " + detail); }
        private float Argument(string flag, float fallback)
        {
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, flag);
            return index >= 0 && index + 1 < args.Length ? float.Parse(args[index + 1], CultureInfo.InvariantCulture) : fallback;
        }
        private string TextArgument(string flag, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, flag);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }
        private void ResetCase()
        {
            traffic.RandomizeSeed = false; traffic.Seed = 1701; traffic.AutoGenerationEnabled = true; traffic.HostileGenerationEnabled = false;
            IntegrityWitness.Set(traffic, "benignArrivalPolicy", policy);
            IntegrityWitness.Set(traffic, "experimentalBenignLoadMultiplier", loadScale);
            CongestionAmbientWitness.Configure(host, new CongestionAmbientWitness.Configuration { loadScale = loadScale, dbWeight = dbWeight,
                smallGoal = Argument("-demandSmallGoal", 0), advancedGoal = Argument("-demandAdvancedGoal", 0) });
            IntegrityWitness.Set(traffic, "independentBenignMeanSeconds", mean);
            IntegrityWitness.Set(host, "shiftEnabled", true); IntegrityWitness.Set(host, "shiftDurationSeconds", 360f);
            IntegrityWitness.Set(host, "shiftWorkTarget", 1000000f); host.ResetSimulation(); loader = null;
            terminal.AppendText("DEMAND EXPERIMENT / " + policy + " / LOAD x" + loadScale.ToString("0.##"), new Color(.52f, .84f, .72f));
        }
        private void Advance(float until, bool overclock)
        {
            while (host.SimulationTimeSeconds < until && !host.RunEnded)
            {
                if (loader == null && host.SimulationTimeSeconds >= 90)
                    loader = IndependentDemandWitness.AddLoader(host, "zz", loaderLoad); // Short authored fixture ID for presentation.
                if (loader?.LoaderActivatedAtSeconds >= 0 && loader.IsActive &&
                    host.SimulationTimeSeconds >= loader.LoaderActivatedAtSeconds + loaderSurvival)
                {
                    host.KillConnection("zz"); if (overclock) host.TryActivateOverclock("DB", "overclock db", out _);
                }
                host.StepSimulation(.02f);
            }
        }
        private IEnumerator Capture()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                if (SceneManager.GetActiveScene().path != scene)
                { EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single)); yield return null; }
                host = FindFirstObjectByType<SimulationHost>(); host.enabled = false;
                traffic = host.GetComponent<ConnectionTrafficDirector>(); terminal = FindFirstObjectByType<Terminal>();
                mean = Argument(scene == CanonicalSceneAuthoring.Scenes[0] ? "-demandSmallMean" : "-demandAdvancedMean",
                    scene == CanonicalSceneAuthoring.Scenes[0] ? 8 : 6);
                input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                canvas = terminal.GetComponentInParent<Canvas>();
                cameraForCapture = new GameObject("Demand review camera", typeof(Camera)).GetComponent<Camera>();
                cameraForCapture.clearFlags = CameraClearFlags.SolidColor; cameraForCapture.backgroundColor = new Color(.025f, .065f, .08f);
                cameraForCapture.orthographic = true; cameraForCapture.transform.position = new Vector3(0, 0, -100);
                cameraForCapture.nearClipPlane = .1f; cameraForCapture.farClipPlane = 1000;
                RenderMode oldMode = canvas.renderMode; Camera oldCamera = canvas.worldCamera;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cameraForCapture; canvas.planeDistance = 10;
                var viewport = new RenderTexture(1920, 1080, 24); cameraForCapture.targetTexture = viewport;
                folder = evidence + "/Captures/" + host.gameObject.scene.name; Directory.CreateDirectory(folder);
                ResetCase(); Advance(88, false); yield return new WaitForSecondsRealtime(.65f); yield return null; Frames("01-healthy");
                Advance(100, false); yield return new WaitForSecondsRealtime(.65f); yield return null;
                Check(loader.IsActive && host.FindNodeResources("DB").RequestedLoad >= loaderLoad - .01f, "real active LOADER contributes target LOAD"); Frames("02-loader");
                Advance(Argument("-demandRecoveryAt", 132), false);
                string[] ids = host.RuntimeConnections.Where(c => !c.IsHostile && c.IsActive).Take(3).Select(c => c.ConnectionId).ToArray();
                Check(ids.Length > 0, "active legitimate demand available for inspect");
                input.onSubmit.Invoke("inspect -v " + string.Join(" ", ids)); yield return new WaitForSecondsRealtime(.65f); yield return null;
                Check(loader.Resolution == ConnectionResolution.Killed && input.isFocused, "source killed; inspect returns terminal focus"); Frames("03-after-kill");
                Advance(132, false); // Compare complete plans at the same horizon even when the recovery screenshot is earlier.
                string plans = string.Join("\n", host.Recorder.CurrentLog.events.Where(e => e.type == "plan_queued" && (e.source == "independent_benign" || e.source == "nominal_benign")).Select(e => JsonUtility.ToJson(e.definition)));
                ResetCase(); Advance(132, true); yield return new WaitForSecondsRealtime(.65f); yield return null;
                Check(host.OverclockedNode == null && host.OverclockCooldownRemaining > 0 && loader.Resolution == ConnectionResolution.Killed,
                    "Overclock intervention expired naturally before recovery capture");
                string paired = string.Join("\n", host.Recorder.CurrentLog.events.Where(e => e.type == "plan_queued" && (e.source == "independent_benign" || e.source == "nominal_benign")).Select(e => JsonUtility.ToJson(e.definition)));
                Check(plans == paired && traffic.Demand.arrived == traffic.Demand.admitted && traffic.Demand.unadmitted == 0,
                    "paired graphical cases preserve exact demand plans and admit every request"); Frames("04-after-kill-overclock");
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; Destroy(cameraForCapture.gameObject); Destroy(viewport); yield return null;
            }
            Debug.Log("IndependentDemandCapture PASS: both actual Play Mode scenes at both 16:9 resolutions; 16 frames, real demand/source/intervention and inspect focus.");
        }
        private void Frames(string name) { Frame(name, 1920, 1080); Frame(name, 1280, 720); }
        private void Frame(string name, int width, int height)
        {
            var target = new RenderTexture(width, height, 24); var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active, previousTarget = cameraForCapture.targetTexture;
            try
            {
                cameraForCapture.targetTexture = target; Canvas.ForceUpdateCanvases();
                FindFirstObjectByType<NetworkHealthPanel>().Render(); FindFirstObjectByType<IntegrityPresenter>().RenderNow();
                FindFirstObjectByType<LogicalPathPresenter>().RefreshLayouts(); FindFirstObjectByType<NodeStatusPresenter>().RenderNow(Time.unscaledTime, 1);
                Check(canvas.pixelRect.width == width && canvas.pixelRect.height == height, name + " real viewport " + width + "x" + height);
                Check(!host.RunEnded && traffic.BenignPolicy == policy, name + " diagnostic quota does not conceal recovery tail");
                checks.Add($"OBSERVED {host.gameObject.scene.name}/{name}/{width} time{host.SimulationTimeSeconds:0.##} policy{policy} loadScale{loadScale} admitted{traffic.Demand.admitted} visible{host.RuntimeConnections.Count(c => c.IsActive || c.IsConnecting)} DB_LOAD{host.FindNodeResources("DB").RequestedLoad:0.##}");
                Canvas.ForceUpdateCanvases(); cameraForCapture.Render(); RenderTexture.active = target;
                picture.ReadPixels(new Rect(0, 0, width, height), 0, 0); picture.Apply();
                File.WriteAllBytes(folder + "/" + name + "-" + width + "x" + height + ".png", picture.EncodeToPNG());
            }
            finally { cameraForCapture.targetTexture = previousTarget; RenderTexture.active = previous; Destroy(picture); Destroy(target); }
        }
    }
}
