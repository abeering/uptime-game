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
using UnityEngine.UI;
using Uptime.Connections.Display;

namespace Uptime.Connections.EditorTools
{
    // Adapted from #124's actual Play Mode camera/viewport/result witness.
    [InitializeOnLoad] public static class BusinessShiftCapture
    {
        private const string Pending = "Uptime.BusinessShiftCapture";
        static BusinessShiftCapture() => EditorApplication.update += Launch;
        public static void Run()
        {
            EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Business shift Play Mode evidence").AddComponent<BusinessShiftCaptureRunner>();
        }
    }
    public sealed class BusinessShiftCaptureRunner : MonoBehaviour
    {
        private SimulationHost host;
        private TMP_InputField input;
        private Canvas canvas;
        private Camera cameraForCapture;
        private RenderTexture viewport;
        private string folder;
        private readonly List<string> checks = new();
        private const string Evidence = BusinessShiftWitness.Evidence;
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); yield return null;
            var stack = new Stack<IEnumerator>(); stack.Push(Capture()); Exception failure = null;
            while (stack.Count > 0)
            {
                object next;
                try
                {
                    var current = stack.Peek(); if (!current.MoveNext()) { stack.Pop(); continue; }
                    next = current.Current; if (next is IEnumerator child) { stack.Push(child); continue; }
                }
                catch (Exception error) { failure = error; break; }
                yield return next;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            Directory.CreateDirectory(Evidence); File.WriteAllLines(Evidence + "/graphical-checks.txt", checks);
            if (failure != null) Debug.LogException(failure);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1); else EditorApplication.isPlaying = false;
        }
        private void Check(bool value, string detail)
        { checks.Add((value ? "PASS " : "FAIL ") + host.gameObject.scene.name + " " + detail); if (!value) throw new Exception(detail); }
        private void Submit(string command) => input.onSubmit.Invoke(command);
        private IEnumerator Capture()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                if (SceneManager.GetActiveScene().path != scene)
                { EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single)); yield return null; }
                host = FindFirstObjectByType<SimulationHost>(); host.enabled = false;
                var traffic = host.GetComponent<ConnectionTrafficDirector>(); traffic.RandomizeSeed = false; traffic.Seed = 1701;
                traffic.AutoGenerationEnabled = false; traffic.HostileGenerationEnabled = false; host.ResetSimulation();
                var terminal = FindFirstObjectByType<Terminal>(); input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                canvas = terminal.GetComponentInParent<Canvas>(); var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
                cameraForCapture = new GameObject("Business review camera", typeof(Camera)).GetComponent<Camera>();
                cameraForCapture.clearFlags = CameraClearFlags.SolidColor; cameraForCapture.backgroundColor = new Color(.025f, .065f, .08f);
                cameraForCapture.orthographic = true; cameraForCapture.transform.position = new Vector3(0, 0, -100);
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cameraForCapture; canvas.planeDistance = 10;
                viewport = new RenderTexture(1920, 1080, 24); cameraForCapture.targetTexture = viewport;
                folder = Evidence + "/Captures/" + host.gameObject.scene.name; Directory.CreateDirectory(folder);
                yield return null; yield return null; Frames("01-authored-objectives");

                BusinessShiftWitness.Fixture(300, 2);
                BusinessShiftWitness.QueueJob(host, "credited", new OperationStage(OperationKind.Read, 100, 5, "Completed business")); host.StepSimulation(23);
                foreach (string id in new[] { "a1", "a2", "a3" }) BusinessShiftWitness.QueueJob(host, id, BusinessShiftWitness.Arc());
                host.StepSimulation(3); Submit("inspect -v a1 a2 a3"); yield return null; yield return null;
                Check(host.CreditedWork == 100 && input.isFocused, "whole-job WORK receipt and multi-ID inspect preserve focus"); Frames("02-credit-and-inspect");

                BusinessShiftWitness.LoadSuccess(); host.StepSimulation(12); yield return null;
                Check(host.FindConnection("job").CurrentOperationIndex > 0 && host.CreditedWork == 0, "intermediate operation has no shift credit");
                Submit("inspect -v job"); yield return null; Frames("03-intermediate-zero-credit");
                Submit("clean db"); yield return new WaitForSecondsRealtime(2);
                var clean = FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
                Check(clean.gameObject.activeInHierarchy && host.ActiveClean != null, "CLEAN active before result");
                host.GetComponent<ConnectionsDuelDirector>().TrySchedule("AUTH", DuelVariant.CommandRace, 0, "business-review", out _);
                var encounter = host.GetComponent<ConnectionsDuelDirector>().Current;
                yield return null; input.SetTextWithoutNotify("inspect ");
                host.StepSimulation(50); yield return null; yield return null;
                Check(host.Ending == RunEnding.ShiftComplete && host.CreditedWork == 200 && encounter.Outcome == null &&
                    host.ActiveClean != null && !clean.gameObject.activeInHierarchy &&
                    !FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include).gameObject.activeInHierarchy,
                    "SHIFT COMPLETE aborts active Duel/CLEAN presentation without penalty or fabricated outcome");
                Check(input.isFocused && input.text == "inspect ", "ending restores focus and command draft");
                yield return Frozen(); Frames("04-shift-complete"); SaveRecorder("complete");
                yield return Restart("05-restart-after-success");

                BusinessShiftWitness.LoadFailedArc(); host.StepSimulation(26); yield return null;
                Check(host.CreditedWork == 0 && host.FindConnection("failed").CurrentOperationIndex == 2,
                    "two completed processing operations still give zero business WORK");
                Submit("inspect -v failed"); yield return null; Frames("06-final-operation-stall");
                host.StepSimulation(100); yield return null; yield return null;
                Check(host.Ending == RunEnding.Fired && host.CreditedWork == 0 && host.LegitimateFailureCount == 1,
                    "failed whole arc grants zero and causes dismissal"); Frames("07-fired-failed-arc"); SaveRecorder("failed-arc");

                BusinessShiftWitness.LoadReliability(); float travel = (BusinessShiftWitness.Route(host).Length - 1) * host.ConnectionTravelSecondsPerLink;
                host.StepSimulation(travel + 29); Submit("scan db"); var scan = host.ActiveScan;
                host.StepSimulation(5); yield return null; yield return null;
                Check(host.Ending == RunEnding.Fired && host.LegitimateFailureCount == 2 && scan != null && host.ActiveScan == scan && !scan.IsComplete,
                    "failure limit preserves unfinished scan without adding failures");
                yield return Frozen(); Frames("08-fired-reliability"); SaveRecorder("fired");
                yield return Restart("09-restart-after-fired");

                BusinessShiftWitness.LoadJustInTime(); host.StepSimulation(travel + 10); yield return null;
                Check(host.CreditedWork == 200 && host.LegitimateFailureCount == 1 && !host.RunEnded,
                    "close to success and failure limits with real earned/sacrificed jobs"); Frames("10-just-in-time-before");
                Submit("overclock db"); host.StepSimulation(40); yield return null; yield return null;
                Check(host.Ending == RunEnding.ShiftComplete && host.CreditedWork == 400 && host.LegitimateFailureCount == 1,
                    "operator Overclock changes just-in-time outcome to success"); Frames("11-just-in-time-saved"); SaveRecorder("just-in-time");

                BusinessShiftWitness.LoadCore(); yield return null; yield return null;
                Check(host.Ending == RunEnding.CoreBreached && host.CreditedWork == 0 && host.LegitimateFailureCount == 0 &&
                    host.Shift.unfinished == 1000, "CORE independent defeat preserves unfinished business");
                yield return Frozen(); Frames("12-core-breached"); SaveRecorder("core");
                yield return Restart("13-restart-after-core");
                BusinessShiftWitness.LoadUnbounded(); yield return null;
                Check(!host.RunEnded && host.SimulationTimeSeconds > 360 && host.CreditedWork == 0, "no elapsed-time defeat beyond six minutes"); Frames("14-unbounded-uptime");
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
                Destroy(cameraForCapture.gameObject); Destroy(viewport); yield return null;
            }
            Debug.Log("BusinessShiftCapture PASS: " + checks.Count + " scripted checks; both scenes and resolutions, whole-job progression, all outcomes and restart.");
        }
        private IEnumerator Frozen()
        {
            float time = host.SimulationTimeSeconds; double beats = host.GetComponent<ConnectionGameplayDisplay>().heartbeat.Beats;
            host.enabled = true; yield return new WaitForSecondsRealtime(.15f);
            Check(host.SimulationTimeSeconds == time && host.GetComponent<ConnectionGameplayDisplay>().heartbeat.Beats == beats && !host.Recorder.IsRecording,
                "actual frame loop freezes authoritative time and shared heartbeat; recorder finalized");
        }
        private IEnumerator Restart(string frame)
        {
            int seed = host.GetComponent<ConnectionTrafficDirector>().Seed;
            Submit("restart"); host.enabled = false; yield return null; yield return null;
            Check(!host.RunEnded && host.CreditedWork == 0 && host.LegitimateFailureCount == 0 && input.isFocused &&
                host.GetComponent<ConnectionTrafficDirector>().Seed == seed, "same-seed restart clears both objectives and result"); Frames(frame);
        }
        private void SaveRecorder(string name)
        {
            Directory.CreateDirectory(Evidence + "/Recorder");
            File.WriteAllText(Evidence + "/Recorder/" + host.gameObject.scene.name + "-" + name + ".json", JsonUtility.ToJson(host.Recorder.CurrentLog, true));
        }
        private void Frames(string name) { Frame(name, 1920, 1080); Frame(name, 1280, 720); }
        private void Frame(string name, int width, int height)
        {
            var target = new RenderTexture(width, height, 24); var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active; var oldTarget = cameraForCapture.targetTexture;
            try
            {
                cameraForCapture.targetTexture = target; Canvas.ForceUpdateCanvases();
                var strip = FindFirstObjectByType<NetworkHealthPanel>(); strip.Render(); FindFirstObjectByType<IntegrityPresenter>().RenderNow();
                var paths = FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None); var presenter = FindFirstObjectByType<LogicalPathPresenter>();
                presenter.RefreshLayouts(); foreach (var path in paths) path.AdvanceVisual(path.RelayoutSeconds);
                presenter.RefreshLayouts(); presenter.RefreshSocketSizes(); foreach (var path in paths) path.TerminalMarker?.AdvanceVisual(path.RelayoutSeconds);
                var fields = new SerializedObject(strip);
                var texts = new[] { "clockText", "valueText", "businessFailureText", "rateText" }.Select(n => (TMP_Text)fields.FindProperty(n).objectReferenceValue).ToArray();
                Check(canvas.pixelRect.width == width && canvas.pixelRect.height == height, name + " actual viewport " + width + "x" + height);
                foreach (var text in texts)
                { text.ForceMeshUpdate(); Check(text.gameObject.activeInHierarchy && text.textBounds.size.x <= text.rectTransform.rect.width + 1, name + " readable " + text.name); }
                var stripRect = (RectTransform)strip.transform;
                foreach (var text in texts)
                {
                    var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                    Check(corners.All(c => { Vector3 point = stripRect.InverseTransformPoint(c); var r = stripRect.rect;
                        return point.y >= r.yMin - 1 && point.y <= r.yMax + 1; }), name + " status text stays inside compact strip: " + text.name);
                }
                float Left(RectTransform r) { var corners = new Vector3[4]; r.GetWorldCorners(corners); return corners[0].x; }
                float Right(RectTransform r) { var corners = new Vector3[4]; r.GetWorldCorners(corners); return corners[2].x; }
                var bar = (RectTransform)((Image)fields.FindProperty("healthFill").objectReferenceValue).transform.parent;
                var tools = strip.transform.Find("ToolReadinessRow").GetComponent<RectTransform>();
                Check(Right(texts[0].rectTransform) <= Left(texts[1].rectTransform) && Right(texts[1].rectTransform) <= Left(bar) &&
                    Right(bar) <= Left(texts[2].rectTransform) && Right(texts[2].rectTransform) <= Left(texts[3].rectTransform) &&
                    Right(texts[3].rectTransform) < Left(tools), name + " objective columns and tools do not overlap");
                var result = new SerializedObject(FindFirstObjectByType<IntegrityPresenter>());
                var root = (GameObject)result.FindProperty("breachPanel").objectReferenceValue;
                Check(root.activeSelf == host.RunEnded, name + " single authoritative result panel visibility");
                if (host.RunEnded)
                {
                    var title = (TMP_Text)result.FindProperty("resultTitle").objectReferenceValue;
                    var body = (TMP_Text)result.FindProperty("breachPath").objectReferenceValue;
                    Check(title.text == host.EndingTitle && title.preferredWidth <= title.rectTransform.rect.width + 1 &&
                        body.preferredHeight <= body.rectTransform.rect.height + 1, name + " outcome title and final totals fit");
                }
                Canvas.ForceUpdateCanvases(); cameraForCapture.Render(); RenderTexture.active = target;
                picture.ReadPixels(new Rect(0, 0, width, height), 0, 0); picture.Apply(); File.WriteAllBytes(folder + "/" + name + "-" + width + "x" + height + ".png", picture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; cameraForCapture.targetTexture = oldTarget; Destroy(picture); Destroy(target); }
        }
    }
}
