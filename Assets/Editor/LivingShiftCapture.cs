using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class LivingShiftCapture
    {
        private const string Pending = "Uptime.LivingShiftCapture.Pending";
        private static int frames;
        static LivingShiftCapture() => EditorApplication.update += Launch;
        [MenuItem("Tools/Connections/Capture Living Shift Play Mode Review")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            frames = 0; SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying || ++frames < 20) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Living Shift review runner").AddComponent<LivingShiftCaptureRunner>().Begin();
        }
    }

    public sealed class LivingShiftCaptureRunner : MonoBehaviour
    {
        private SimulationHost host;
        private ConnectionTrafficDirector traffic;
        private LivingShiftDirector shift;
        private Canvas canvas;
        private Camera cameraForCapture;
        private RenderTexture target;
        private RenderMode oldMode;
        private Camera oldCamera;
        private string output;
        private int width, height;
        private float oldScale;
        private LivingShiftOperatorPolicy operatorPolicy;
        public void Begin() => StartCoroutine(Guard(Cases()));
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("LivingShift PLAY: " + message); Debug.Log("SHIFT PLAY CHECK: " + message); }
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
            if (canvas != null) { canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; }
            if (cameraForCapture != null) Destroy(cameraForCapture.gameObject);
            if (target != null) Destroy(target);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
        private IEnumerator Cases()
        {
            host = FindFirstObjectByType<SimulationHost>(); traffic = host.GetComponent<ConnectionTrafficDirector>(); shift = host.LivingShift;
            canvas = FindFirstObjectByType<Terminal>().GetComponentInParent<Canvas>();
            width = int.Parse(SurpriseCapture.Argument("-witnessWidth", "1920"));
            height = int.Parse(SurpriseCapture.Argument("-witnessHeight", "1080"));
            output = SurpriseCapture.Argument("-capturePath", "Docs/Review/ConnectionsLivingShiftV0/Captures");
            Directory.CreateDirectory(output);
            oldScale = Time.timeScale; Time.timeScale = 0f;
            oldMode = canvas.renderMode; oldCamera = canvas.worldCamera;
            cameraForCapture = new GameObject("Shift capture camera", typeof(Camera)).GetComponent<Camera>();
            cameraForCapture.clearFlags = CameraClearFlags.SolidColor;
            cameraForCapture.backgroundColor = new Color(.025f, .065f, .08f); cameraForCapture.orthographic = true;
            cameraForCapture.transform.position = new Vector3(0,0,-100); cameraForCapture.nearClipPlane=.1f; cameraForCapture.farClipPlane=1000;
            target = new RenderTexture(width,height,24); cameraForCapture.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=cameraForCapture; canvas.planeDistance=10f;
            yield return null;
            Reset();
            var input = FindFirstObjectByType<Terminal>().GetComponentInChildren<TMPro.TMP_InputField>();
            input.SetTextWithoutNotify("inspect draft"); input.ActivateInputField(); input.Select();
            StepTo(14f, false);
            yield return new WaitForSecondsRealtime(.5f);
            Check(input.text == "inspect draft" && input.isFocused, "opening call and real messages preserve terminal draft/focus");
            Check(host.Recorder.CurrentLog.events.Any(e => e.type == "shift_communication" && e.shift.delivered), "authored opening actually delivered");
            StepTo(22f, false); yield return new WaitForSecondsRealtime(.35f);
            yield return Frame("early-context");
            StepTo(130f, false); yield return new WaitForSecondsRealtime(.35f); yield return Frame("overlap");
            StepTo(290f, false); yield return new WaitForSecondsRealtime(.35f); yield return Frame("objective-jeopardy");
            StepTo(359f, false);
            // Deliberate boundary overlap: normal public APIs, no objective manipulation.
            host.GetComponent<ConnectionsDuelDirector>().ResetDuel();
            host.CancelClean();
            var clean = FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
            if (host.StartClean("AUTH", "clean AUTH", out CleanSession session) == CleanStartResult.Started) clean.Open(session, host, FindFirstObjectByType<Terminal>());
            var call = FindFirstObjectByType<ConnectionsVideoCall>(FindObjectsInactive.Include);
            call.TryStartCall(new ConnectionsVideoCallData { CallerName="Operations", Body="Handoff is approaching." });
            var duel=host.GetComponent<ConnectionsDuelDirector>();
            Check(duel.TrySchedule("API", DuelVariant.CommandRace, 0f, "shift_boundary_witness", out _), "Duel opened over CLEAN/call at boundary");
            StepTo(360f, false); yield return new WaitForSecondsRealtime(.35f);
            Check(shift.IsFinished && !shift.Succeeded, "fixed-target failed ending");
            Check(host.ActiveClean == null && host.ActiveScan == null && !clean.IsOpen && !call.IsActive && duel.Current == null &&
                !FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include).IsOpen, "all gameplay overlays canceled at deadline");
            Check(!input.interactable && EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.Button>() != null, "result gives keyboard focus to restart");
            float at=host.SimulationTimeSeconds; host.StepSimulation(20f);
            Check(host.SimulationTimeSeconds == at, "simulation remains frozen behind result");
            yield return Frame("failed-ending");
            Reset();
            StepTo(360f, true); yield return new WaitForSecondsRealtime(.35f);
            Check(shift.IsFinished && shift.Succeeded, "same seed, unchanged target, intervention produces success");
            yield return Frame("successful-ending");
            shift.Restart(); yield return null;
            Check(shift.IsRunning && shift.CreditedWork == 0f && traffic.Seed == 100 && input.interactable, "restart selects new seed, clears work and restores input");
            Debug.Log("LivingShiftCapture PLAY PASS " + width + "x" + height);
        }
        private void Reset()
        {
            traffic.RandomizeSeed=false; traffic.Seed=99; traffic.AutoGenerationEnabled=true; traffic.HostileGenerationEnabled=true;
            host.ResetSimulation();
            operatorPolicy = new LivingShiftOperatorPolicy();
        }
        private void StepTo(float time, bool intervene)
        {
            var clock = FindFirstObjectByType<NetworkHealthPanel>();
            while (host.SimulationTimeSeconds < time && !shift.IsFinished)
            {
                if (intervene) operatorPolicy.Act(host);
                var duel=host.GetComponent<ConnectionsDuelDirector>();
                if (duel.Current?.Lifecycle == DuelLifecycle.Active)
                {
                    var o=FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
                    var r=(CommandRaceSession)typeof(ConnectionsDuelOverlay).GetField("race",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
                    r?.Advance(.1f); if(r?.PlayerWon.HasValue == true) duel.Resolve(r);
                }
                clock.Render();
                host.StepSimulation(Mathf.Min(.1f,time-host.SimulationTimeSeconds));
            }
        }
        private IEnumerator Frame(string name)
        {
            FindFirstObjectByType<NetworkHealthPanel>()?.Render();
            FindFirstObjectByType<LivingShiftPanel>()?.Render();
            FindFirstObjectByType<NodeStatusPresenter>().RenderNow(Time.unscaledTime,1f);
            var paths=FindFirstObjectByType<LogicalPathPresenter>(); paths.RefreshLayouts();
            foreach (var view in FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None)) view.AdvanceVisual(1f);
            paths.RefreshLayouts();
            FindFirstObjectByType<WormPresenter>().RenderNow(); FindFirstObjectByType<PayloadPresenter>().RenderNow();
            yield return null;
            Canvas.ForceUpdateCanvases(); cameraForCapture.Render();
            RenderTexture previous=RenderTexture.active;
            try
            {
                RenderTexture.active=target; var pic=new Texture2D(width,height,TextureFormat.RGB24,false);
                try { pic.ReadPixels(new Rect(0,0,width,height),0,0);pic.Apply();File.WriteAllBytes(Path.Combine(output,name+"-"+width+"x"+height+".png"),pic.EncodeToPNG()); }
                finally { Destroy(pic); }
            }
            finally { RenderTexture.active=previous; }
        }
    }
}
