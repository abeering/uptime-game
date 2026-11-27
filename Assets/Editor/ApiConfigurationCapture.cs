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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Uptime.Connections.Display;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad] public static class ApiConfigurationCapture
    {
        private const string Pending = "Uptime.ApiConfigurationCapture";
        static ApiConfigurationCapture() => EditorApplication.update += Launch;
        public static void Run()
        {
            ApiConfigurationAuthoring.FitText();
            EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("API configuration gameplay evidence").AddComponent<ApiConfigurationCaptureRunner>();
        }
    }
    public sealed class ApiConfigurationCaptureRunner : MonoBehaviour
    {
        private readonly List<string> checks = new();
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private Keyboard keyboard;
        private ConnectionsSshOverlay currentSsh;
        private void Check(bool ok, string message) { checks.Add((ok ? "PASS " : "FAIL ") + message); if (!ok) throw new Exception(message); }
        private static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        // The package's public Update() chooses Editor updates in a batch Editor.
        // This Editor-only driver requests its internal dynamic update to produce
        // the runtime press edges consumed by the unchanged window Update.
        private static void DynamicInputUpdate() => typeof(InputSystem).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(InputUpdateType) }, null).Invoke(null, new object[] { InputUpdateType.Dynamic });
        private IEnumerator Press(params Key[] keys)
        {
            // Batch Editors may not have a focused Game view. Process the injected
            // dynamic input and the same window Update immediately, once per press.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.EnableDevice(keyboard); DynamicInputUpdate(); keyboard.MakeCurrent();
            if (currentSsh != null && currentSsh.IsOpen)
            {
                Invoke(currentSsh, "Update");
            }
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); DynamicInputUpdate(); yield return null;
        }
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); yield return null;
            float oldTimeScale = Time.timeScale; bool oldRunBackground = Application.runInBackground; Application.runInBackground = true;
            var oldFocus = InputSystem.settings.editorInputBehaviorInPlayMode; var oldBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            var run = Capture(); Exception failure = null;
            while (true)
            {
                bool more; try { more = run.MoveNext(); } catch (Exception e) { failure = e; break; }
                if (!more) break; yield return run.Current;
            }
            (run as IDisposable)?.Dispose(); InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.editorInputBehaviorInPlayMode = oldFocus; InputSystem.settings.backgroundBehavior = oldBackground; Application.runInBackground = oldRunBackground; Time.timeScale = oldTimeScale;
            Directory.CreateDirectory(ApiConfigurationWitness.Evidence);
            File.WriteAllLines(ApiConfigurationWitness.Evidence + "/graphical-checks.txt", checks);
            if (failure != null) Debug.LogException(failure); else Debug.Log("ApiConfigurationCapture PASS: " + checks.Count + " checks; native Play Mode at both supported resolutions.");
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1); else EditorApplication.isPlaying = false;
        }
        private IEnumerator Capture()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                if (SceneManager.GetActiveScene().path != scene)
                { EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single)); yield return null; }
                var h = FindFirstObjectByType<SimulationHost>(); h.enabled = false;
                var terminal = FindFirstObjectByType<Terminal>(); var input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                var ssh = FindFirstObjectByType<ConnectionsSshOverlay>(FindObjectsInactive.Include); currentSsh = ssh;
                var duel = FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
                var escape = FindFirstObjectByType<EscapeCoordinator>();
                var canvas = terminal.GetComponentInParent<Canvas>(); var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
                var camera = new GameObject("API evidence camera", typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .065f, .08f); camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -100);
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                foreach (int width in new[] { 1920, 1280 })
                {
                    int height = width * 9 / 16; var viewport = new RenderTexture(width, height, 24); camera.targetTexture = viewport;
                    ApiConfigurationWitness.LoadFixture(h); h.StepSimulation(4); yield return null; yield return null;
                    string prefix = h.gameObject.scene.name + "-" + width;
                    Check(canvas.pixelRect.width == width && canvas.pixelRect.height == height, prefix + " native viewport");
                    var product = h.FindConnection(ApiConfigurationWitness.FixtureProduct); var sales = h.FindConnection(ApiConfigurationWitness.FixtureSales);
                    Check(Math.Abs(product.AllocatedLoad - 5) < .001 && Math.Abs(sales.AllocatedLoad - 5) < .001, prefix + " empty policy5/5");
                    input.onSubmit.Invoke("inspect " + product.ConnectionId + " " + sales.ConnectionId); yield return null; yield return null;
                    Save(camera, viewport, prefix + "-before");
                    input.onSubmit.Invoke("ssh api"); yield return null; yield return Press();
                    Check(ssh.IsOpen && EventSystem.current.currentSelectedGameObject == ssh.gameObject && !input.interactable, prefix + " ssh command opens authored window and owns typing");
                    string selected = scene == CanonicalSceneAuthoring.Scenes[0] ? "product-lookup" : "sales-export";
                    int index = h.ApiPurposeChoices.ToList().FindIndex(p => p.id == selected) + 1;
                    for (int i = 0; i < index; i++) yield return Press(Key.DownArrow);
                    yield return Press(Key.Enter); h.StepSimulation(.02f);
                    Check(h.ApiFavoredPurposeId == selected && ssh.IsOpen, prefix + " keyboard selects/applies PURPOSE and remains open; cursor=" + ssh.SelectedIndex + ", selected=" + h.ApiFavoredPurposeId);
                    float before = h.SimulationTimeSeconds; h.enabled = true; Time.timeScale = 1;
                    yield return new WaitForSecondsRealtime(.12f); Time.timeScale = 0;
                    // Pause only the test clock for controlled captures. Disabling
                    // the host would intentionally tear down runtime connection state.
                    Check(h.SimulationTimeSeconds > before, prefix + " simulation advances during configuration");
                    yield return null; yield return null; Canvas.ForceUpdateCanvases();
                    Save(camera, viewport, prefix + "-policy");
                    var content = ssh.GetComponentsInChildren<TMP_Text>();
                    foreach (var label in content)
                        Check(!label.isTextOverflowing && label.preferredWidth <= label.rectTransform.rect.width + .1f, prefix + " readable SSH text: " + label.name + " preferred=" + label.preferredHeight + " rect=" + label.rectTransform.rect.height);
                    var corners = new Vector3[4]; ((RectTransform)ssh.transform).GetWorldCorners(corners);
                    Check(corners.All(c => { var p = camera.WorldToViewportPoint(c); return p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1; }), prefix + " SSH fully within viewport");
                    // Escape follows the same coordinator path as its native LateUpdate.
                    yield return new WaitForSecondsRealtime(.18f);
                    yield return Press(Key.Escape);
                    yield return null; yield return null;
                    Check(!ssh.IsOpen && h.ApiFavoredPurposeId == selected && input.interactable && input.isFocused, prefix + " Escape closes, persists policy, restores command focus");
                    input.onSubmit.Invoke("inspect -v " + product.ConnectionId + " " + sales.ConnectionId); yield return null; yield return null;
                    Check(FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(x => x.text.Contains("API ×2")), prefix + " verbose inspect identifies applied API benefit");
                    Save(camera, viewport, prefix + "-allocation");
                    h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds);
                    File.WriteAllText(ApiConfigurationWitness.Evidence + "/" + prefix + "-allocation.json", JsonUtility.ToJson(h.Recorder.CurrentLog.snapshots.Last(), true));
                    h.StepSimulation(30); yield return null; yield return null;
                    Check(h.CreditedWork == 32 && h.LegitimateFailureCount == 1 && !h.RunEnded && (selected == "product-lookup" ? product.Resolution == ConnectionResolution.Completed && sales.Resolution == ConnectionResolution.TimedOut : sales.Resolution == ConnectionResolution.Completed && product.Resolution == ConnectionResolution.TimedOut), prefix + " actual Play Mode saves selected32 WORK, other times out; work=" + h.CreditedWork + " failures=" + h.LegitimateFailureCount + " ending=" + h.Ending + " product=" + product.Resolution + " sales=" + sales.Resolution);
                    Save(camera, viewport, prefix + "-outcome");
                    h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds);
                    File.WriteAllText(ApiConfigurationWitness.Evidence + "/" + prefix + "-outcome.json", JsonUtility.ToJson(h.Recorder.CurrentLog.snapshots.Last(), true));
                    input.onSubmit.Invoke("ssh api"); yield return null; yield return Press();
                    // Duel preemption must restore SSH rather than leak typing to the terminal.
                    var director = h.GetComponent<ConnectionsDuelDirector>();
                    Check(director.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "api-focus-witness", out _), prefix + " existing Duel can preempt SSH");
                    Check(duel.IsOpen && ssh.IsOpen && ReferenceEquals(escape.CurrentTarget, duel), prefix + " Duel takes exclusive focus above SSH");
                    yield return Press(Key.UpArrow, Key.Enter); Check(h.ApiFavoredPurposeId == selected, prefix + " suspended SSH ignores Duel keystrokes");
                    // Abort test encounter without consequences, preserving the policy and configuration window.
                    Invoke(director, "OnDisable"); yield return null;
                    Check(!duel.IsOpen && ssh.IsOpen && EventSystem.current.currentSelectedGameObject == ssh.gameObject && !input.interactable, prefix + " Duel teardown restores SSH focus");
                    yield return Press(); while (ssh.SelectedIndex != 0) yield return Press(Key.UpArrow); yield return Press(Key.Enter); h.StepSimulation(.02f);
                    Check(h.ApiFavoredPurposeId == "", prefix + " keyboard clears policy");
                    h.TrySetApiPurpose(selected, out _); h.ResetSimulation(); yield return null; yield return null;
                    Check(!ssh.IsOpen && h.ApiFavoredPurposeId == "" && input.interactable && input.isFocused, prefix + " restart clears policy and closes configuration");
                    // Existing FIRED ending closes the window and rejects further mutation.
                    ApiConfigurationWitness.LoadFixture(h); input.onSubmit.Invoke("ssh api"); h.StepSimulation(30); yield return null; yield return null;
                    Check(h.Ending == RunEnding.Fired && !ssh.IsOpen && !h.TrySetApiPurpose(selected, out _), prefix + " shift ending aborts configuration and freezes mutations");
                    camera.targetTexture = null; Destroy(viewport);
                }
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; Destroy(camera.gameObject); yield return null;
            }
        }
        private void Save(Camera camera, RenderTexture viewport, string name)
        {
            Canvas.ForceUpdateCanvases(); FindFirstObjectByType<NetworkHealthPanel>().Render();
            var paths = FindFirstObjectByType<LogicalPathPresenter>(); paths.RefreshLayouts(); paths.RefreshSocketSizes();
            foreach (var path in FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None)) path.AdvanceVisual(.2f);
            paths.RefreshLayouts(); paths.RefreshSocketSizes(); Canvas.ForceUpdateCanvases(); camera.Render();
            var old = RenderTexture.active; RenderTexture.active = viewport; var picture = new Texture2D(viewport.width, viewport.height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, viewport.width, viewport.height), 0, 0); picture.Apply(); Directory.CreateDirectory(ApiConfigurationWitness.Evidence + "/Captures");
            File.WriteAllBytes(ApiConfigurationWitness.Evidence + "/Captures/" + name + ".png", picture.EncodeToPNG()); RenderTexture.active = old; Destroy(picture);
        }
    }
}
