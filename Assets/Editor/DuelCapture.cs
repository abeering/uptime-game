using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class DuelCapture
    {
        private const string Pending = "Uptime.DuelCapture.Pending";
        private static int frames;
        static DuelCapture() => EditorApplication.update += Launch;

        [MenuItem("Tools/Connections/Capture Duel Play Mode Review")]
        public static void Run()
        {
            string scene = Argument("-duelScene", "Connections");
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            frames = 0;
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying ||
                ++frames < 20) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Duel capture runner").AddComponent<DuelCaptureRunner>().Begin();
        }

        public static string Argument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : fallback;
        }
    }

    public sealed class DuelCaptureRunner : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private SimulationHost host;
        private ConnectionsDuelDirector director;
        private ConnectionsDuelOverlay overlay;
        private ConnectionsCleanOverlay clean;
        private Terminal terminal;
        private NodeStatusPresenter statuses;
        private LogicalPathPresenter paths;
        private Canvas canvas;
        private RenderMode originalMode;
        private Camera originalCamera;
        private Camera cameraForCapture;
        private RenderTexture target;
        private float originalScale;
        private int width, height;
        private string output;
        private float[] referenceOrigins;
        private float[] referenceBaselines;
        private int[] referenceLines;

        public void Begin() => StartCoroutine(Guard(Cases()));

        private IEnumerator Guard(IEnumerator cases)
        {
            Exception failure = null;
            while (true)
            {
                object next;
                try
                {
                    if (!cases.MoveNext()) break;
                    next = cases.Current;
                }
                catch (Exception error) { failure = error; break; }
                yield return next;
            }
            if (failure != null) Debug.LogException(failure);
            Time.timeScale = originalScale;
            if (canvas != null)
            {
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCamera;
            }
            if (cameraForCapture != null) Destroy(cameraForCapture.gameObject);
            if (target != null) Destroy(target);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }

        private IEnumerator Cases()
        {
                yield return null;
                host = FindFirstObjectByType<SimulationHost>();
                director = host.GetComponent<ConnectionsDuelDirector>();
                overlay = FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
                clean = FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
                terminal = FindFirstObjectByType<Terminal>();
                statuses = FindFirstObjectByType<NodeStatusPresenter>();
                paths = FindFirstObjectByType<LogicalPathPresenter>();
                canvas = terminal.GetComponentInParent<Canvas>();
                if (director == null || overlay == null || clean == null || statuses == null ||
                    paths == null || canvas == null)
                    throw new InvalidOperationException("Duel capture bindings missing");
                width = int.Parse(DuelCapture.Argument("-duelWidth", "1920"));
                height = int.Parse(DuelCapture.Argument("-duelHeight", "1080"));
                output = DuelCapture.Argument("-duelCapturePath",
                    "Docs/Review/ConnectionsHackerDuelV0/Captures");
                Directory.CreateDirectory(output);
                originalScale = Time.timeScale;
                Time.timeScale = 0f;
                host.GetComponent<ConnectionTrafficDirector>().AutoGenerationEnabled = false;
                host.SetRunMode(ConnectionRunMode.Generated);

                originalMode = canvas.renderMode;
                originalCamera = canvas.worldCamera;
                cameraForCapture = new GameObject("Duel capture camera", typeof(Camera))
                    .GetComponent<Camera>();
                cameraForCapture.clearFlags = CameraClearFlags.SolidColor;
                cameraForCapture.backgroundColor = new Color(.025f, .065f, .08f);
                cameraForCapture.orthographic = true;
                cameraForCapture.transform.position = new Vector3(0f, 0f, -100f);
                cameraForCapture.nearClipPlane = .1f;
                cameraForCapture.farClipPlane = 1000f;
                target = new RenderTexture(width, height, 24);
                cameraForCapture.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cameraForCapture;
                canvas.planeDistance = 10f;
                Canvas.ForceUpdateCanvases();
                yield return null;

                if (!director.TrySchedule("AUTH", DuelVariant.CommandRace, 4f,
                    "scripted_capture", out string reason))
                    throw new InvalidOperationException(reason);
                host.StepSimulation(1f);
                Frame("01-intrusion-countdown");
                host.StepSimulation(3f);
                if (!overlay.IsOpen) throw new InvalidOperationException("Duel did not open");
                Frame("02-duel-start");
                float frozenClock = host.SimulationTimeSeconds;
                Set(director, "continueSimulationDuringDuel", false);
                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(.25f);
                if (Mathf.Abs(host.SimulationTimeSeconds - frozenClock) > .001f)
                    throw new InvalidOperationException("Paused Duel advanced simulation");
                Set(director, "continueSimulationDuringDuel", true);
                yield return new WaitForSecondsRealtime(.25f);
                if (host.SimulationTimeSeconds <= frozenClock)
                    throw new InvalidOperationException("Continuing Duel froze simulation");
                Time.timeScale = 0f;
                CommandRaceSession race = Race();
                foreach (char c in "ssh operator@") Type(c);
                race.Advance(5f);
                RenderOverlay();
                Frame("03-duel-partial");
                Type('x');
                if (race.Player.CorrectCharacters != "ssh operator@".Length)
                    throw new InvalidOperationException("Player error advanced correct progress");
                Frame("04-player-error");
                race.BackspacePlayer();
                RenderOverlay();
                Frame("05-player-correction");
                string first = race.Player.Commands[0];
                for (int i = race.Player.Draft.Length; i < first.Length; i++)
                    Type(first[i]);
                if (race.Player.Line != 1 || race.Player.Draft.Length != 0)
                    throw new InvalidOperationException("First command did not auto advance");
                Type('\n');
                Type('\r');
                if (race.Player.Line != 1 || race.Player.Draft.Length != 0)
                    throw new InvalidOperationException("Enter changed the command state");
                Frame("06-next-command");
                foreach (char c in "tail -f /var/") Type(c);
                if (race.Player.Draft != "tail -f /var/")
                    throw new InvalidOperationException("Next command rejected immediate typing");
                Frame("07-path-token");
                while (!race.Player.Complete)
                {
                    string command = race.Player.Commands[race.Player.Line];
                    for (int i = race.Player.Draft.Length; i < command.Length; i++)
                        Type(command[i]);
                }
                if (director.Current != null || !Race().PlayerWon.HasValue ||
                    Race().PlayerWon != true)
                    throw new InvalidOperationException("Scripted victory failed");
                Frame("08-duel-won");
                yield return new WaitForSecondsRealtime(1.3f);
                TMP_InputField commandInput = Field<TMP_InputField>(terminal, "commandInput");
                if (!commandInput.isFocused || !commandInput.interactable)
                    throw new InvalidOperationException("Terminal focus did not return after victory");

                host.ResetSimulation();
                if (!host.TryInjectPhantomLoad("DB", 2f, out reason))
                    throw new InvalidOperationException(reason);
                if (host.StartClean("DB", "clean db", out CleanSession session) !=
                    CleanStartResult.Started)
                    throw new InvalidOperationException("CLEAN did not start");
                clean.Open(session, host, terminal);
                yield return new WaitForSecondsRealtime(1.7f);
                TMP_InputField pid = Field<TMP_InputField>(clean, "pidInput");
                string draft = "123";
                pid.SetTextWithoutNotify(draft);
                if (!director.TrySchedule("AUTH", DuelVariant.CommandRace, 0f,
                    "scripted_capture", out reason))
                    throw new InvalidOperationException(reason);
                if (!clean.IsOpen || host.ActiveClean != session || pid.text != draft ||
                    pid.interactable)
                    throw new InvalidOperationException("CLEAN was not preserved under Duel");
                var escape = FindFirstObjectByType<EscapeCoordinator>();
                if (!ReferenceEquals(escape.CurrentTarget, overlay))
                    throw new InvalidOperationException("Duel did not own Escape");
                typeof(EscapeCoordinator).GetMethod("ProcessEscape", Private)
                    .Invoke(escape, new object[] { true, Time.unscaledTimeAsDouble + 1d });
                if (!overlay.IsOpen || !clean.IsOpen || pid.text != draft)
                    throw new InvalidOperationException("Escape leaked through Duel");
                typeof(EscapeCoordinator).GetMethod("ProcessEscape", Private)
                    .Invoke(escape, new object[] { false, Time.unscaledTimeAsDouble + 1.1d });
                CommandRaceSession defeat = Race();
                foreach (char c in "ssh opera") Type(c);
                if (pid.text != draft || commandInput.text.Length != 0)
                    throw new InvalidOperationException("Duel typing leaked to another field");
                defeat.Advance(3f);
                RenderOverlay();
                Frame("09-duel-over-clean");
                defeat.Advance(100f);
                yield return null;
                if (defeat.PlayerWon != false)
                    throw new InvalidOperationException("Scripted defeat failed");
                Frame("10-duel-lost");
                yield return new WaitForSecondsRealtime(1.3f);
                if (!clean.IsOpen || host.ActiveClean != session || pid.text != draft ||
                    !pid.interactable || !pid.isFocused ||
                    EventSystem.current.currentSelectedGameObject != pid.gameObject)
                    throw new InvalidOperationException("CLEAN did not resume intact");
                Frame("11-clean-restored");
                Debug.Log("DuelCapture PASS: " + gameObject.scene.name + " " + width +
                    "x" + height + " eleven scripted Play Mode frames in " + output);
        }

        private CommandRaceSession Race() => Field<CommandRaceSession>(overlay, "race");
        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static void Set(object owner, string name, object value) =>
            owner.GetType().GetField(name, Private).SetValue(owner, value);
        private void Type(char c) => typeof(ConnectionsDuelOverlay)
            .GetMethod("OnTextInput", Private).Invoke(overlay, new object[] { c });
        private void RenderOverlay() => typeof(ConnectionsDuelOverlay)
            .GetMethod("Render", Private).Invoke(overlay, null);

        private void Frame(string name)
        {
            FindFirstObjectByType<NetworkHealthPanel>()?.Render();
            statuses.RenderNow(Time.unscaledTime, 1f);
            paths.RefreshLayouts();
            Canvas.ForceUpdateCanvases();
            if (overlay.IsOpen) CheckReferenceLayout();
            cameraForCapture.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Path.Combine(output, name + "-" + width + "x" +
                        height + ".png"), image.EncodeToPNG());
                }
                finally { Destroy(image); }
            }
            finally { RenderTexture.active = previous; }
        }

        private void CheckReferenceLayout()
        {
            TMP_Text text = Field<TMP_Text>(overlay, "playerCommands");
            text.ForceMeshUpdate(true, true);
            var expected = new StringBuilder();
            foreach (string command in Race().Player.Commands)
            {
                if (expected.Length > 0) expected.Append('\n');
                expected.Append("> ").Append(command);
            }
            if (text.GetParsedText() != expected.ToString())
                throw new InvalidOperationException("Player reference changed while typing");
            int count = text.textInfo.characterCount;
            if (referenceOrigins == null)
            {
                referenceOrigins = new float[count];
                referenceBaselines = new float[count];
                referenceLines = new int[count];
                for (int i = 0; i < count; i++)
                {
                    referenceOrigins[i] = text.textInfo.characterInfo[i].origin;
                    referenceBaselines[i] = text.textInfo.characterInfo[i].baseLine;
                    referenceLines[i] = text.textInfo.characterInfo[i].lineNumber;
                }
                return;
            }
            if (count != referenceOrigins.Length)
                throw new InvalidOperationException("Player reference glyph count changed");
            for (int i = 0; i < count; i++)
                if (Mathf.Abs(text.textInfo.characterInfo[i].origin - referenceOrigins[i]) > .01f ||
                    Mathf.Abs(text.textInfo.characterInfo[i].baseLine - referenceBaselines[i]) > .01f ||
                    text.textInfo.characterInfo[i].lineNumber != referenceLines[i])
                    throw new InvalidOperationException("Player reference position or wrapping changed at glyph " + i);
        }
    }
}
