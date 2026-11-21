using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
        private Keyboard concessionKeyboard;
        private Keyboard previousKeyboard;

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
            if (concessionKeyboard != null) InputSystem.RemoveDevice(concessionKeyboard);
            previousKeyboard?.MakeCurrent();
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
                CommandRaceSession race = Race();
                if (overlay.IsRacing || race.ElapsedSeconds != 0f ||
                    race.Hacker.CorrectCharacters != 0)
                    throw new InvalidOperationException("Race started during startup splash");
                Frame("02-splash-start");
                Type('s');
                if (race.Player.Draft.Length != 0)
                    throw new InvalidOperationException("Splash accepted race typing");
                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(.35f);
                TMP_Text splash = Field<TMP_Text>(overlay, "splashText");
                if (!splash.text.Contains("INTRUSION DETECTED / AUTH") ||
                    splash.text.Contains("INITIALIZING COUNTERMEASURES..."))
                    throw new InvalidOperationException("Splash did not reveal progressively");
                if (race.ElapsedSeconds != 0f || race.Hacker.Draft.Length != 0)
                    throw new InvalidOperationException("Hacker progressed during splash");
                Frame("02-splash-mid");
                for (int i = 0; i < 120 &&
                    !splash.text.Contains("INITIALIZING COUNTERMEASURES..."); i++)
                    yield return new WaitForSecondsRealtime(.025f);
                if (!splash.text.Contains("ssh operator@auth\nAUTHENTICATING... OK\n" +
                    "INITIALIZING COUNTERMEASURES...") || overlay.IsRacing ||
                    race.ElapsedSeconds != 0f || race.Hacker.Draft.Length != 0)
                    throw new InvalidOperationException("Complete splash or frozen race missing");
                Frame("02-splash-ready");
                for (int i = 0; i < 120 && !overlay.IsRacing; i++)
                    yield return new WaitForSecondsRealtime(.025f);
                if (!overlay.IsRacing || !splash.text.Contains("ssh operator@auth") ||
                    race.ElapsedSeconds > .1f || race.Hacker.CorrectCharacters != 0 ||
                    race.Player.Draft.Length != 0)
                    throw new InvalidOperationException("Splash handoff or equal race start failed");
                Time.timeScale = 0f;
                Frame("02-splash-complete");
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
                foreach (char c in "ss -tnp | grep ") Type(c);
                race.Advance(5f);
                RenderOverlay();
                Frame("03-duel-partial");
                Type('x');
                if (race.Player.CorrectCharacters != "ss -tnp | grep ".Length)
                    throw new InvalidOperationException("Player error advanced correct progress");
                Frame("04-player-error");
                Type('u');
                Type('t');
                if (race.Player.ErrorStartIndex != "ss -tnp | grep ".Length ||
                    race.Player.Mistakes != 3 ||
                    race.Player.CorrectCharacters != "ss -tnp | grep ".Length)
                    throw new InvalidOperationException("Extended error run granted progress");
                Frame("05-error-run");
                race.BackspacePlayer();
                RenderOverlay();
                if (race.Player.ErrorStartIndex < 0 || race.Player.Corrections != 1)
                    throw new InvalidOperationException("First Backspace cleared the whole error run");
                Frame("06-partial-correction");
                race.BackspacePlayer();
                race.BackspacePlayer();
                RenderOverlay();
                if (race.Player.ErrorStartIndex >= 0 || race.Player.Corrections != 3)
                    throw new InvalidOperationException("Backspace did not clear the error run");
                Frame("07-player-correction");
                string first = race.Player.Commands[0];
                for (int i = race.Player.Draft.Length; i < first.Length; i++)
                    Type(first[i]);
                if (race.Player.Line != 1 || race.Player.Draft.Length != 0)
                    throw new InvalidOperationException("First command did not auto advance");
                Type('\n');
                Type('\r');
                if (race.Player.Line != 1 || race.Player.Draft.Length != 0)
                    throw new InvalidOperationException("Enter changed the command state");
                Frame("08-next-command");
                foreach (char c in "tail -f /var/") Type(c);
                if (race.Player.Draft != "tail -f /var/")
                    throw new InvalidOperationException("Next command rejected immediate typing");
                Frame("09-path-token");
                while (!race.Player.Complete)
                {
                    string command = race.Player.Commands[race.Player.Line];
                    for (int i = race.Player.Draft.Length; i < command.Length; i++)
                        Type(command[i]);
                }
                if (director.Current != null || !Race().PlayerWon.HasValue ||
                    Race().PlayerWon != true)
                    throw new InvalidOperationException("Scripted victory failed");
                Frame("10-duel-won");
                overlay.DismissFromEscape();
                if (!overlay.IsOpen || host.Infections.Count != 0)
                    throw new InvalidOperationException("Resolved victory Escape changed the result");
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
                if (!director.TrySchedule("API", DuelVariant.CommandRace, 0f,
                    "scripted_capture", out reason))
                    throw new InvalidOperationException(reason);
                if (!clean.IsOpen || host.ActiveClean != session || pid.text != draft ||
                    pid.interactable)
                    throw new InvalidOperationException("CLEAN was not preserved under Duel");
                var escape = FindFirstObjectByType<EscapeCoordinator>();
                if (!ReferenceEquals(escape.CurrentTarget, overlay))
                    throw new InvalidOperationException("Duel did not own Escape");
                CommandRaceSession defeat = Race();
                for (int i = 0; i < 120 && !overlay.IsRacing; i++)
                    yield return new WaitForSecondsRealtime(.025f);
                if (!overlay.IsRacing || !Field<TMP_Text>(overlay, "splashText").text
                    .Contains("ssh operator@api") || pid.text != draft ||
                    commandInput.text.Length != 0)
                    throw new InvalidOperationException("CLEAN or focus changed during Duel startup");
                foreach (char c in "ss -tnp") Type(c);
                if (pid.text != draft || commandInput.text.Length != 0)
                    throw new InvalidOperationException("Duel typing leaked to another field");
                defeat.Advance(3f);
                RenderOverlay();
                Frame("11-duel-over-clean");
                defeat.Advance(100f);
                yield return null;
                if (defeat.PlayerWon != false)
                    throw new InvalidOperationException("Scripted defeat failed");
                Frame("12-duel-lost");
                int lossEvents = host.Recorder.CurrentLog.events.Count(e => e.type == "duel_resolved");
                overlay.DismissFromEscape();
                if (!overlay.IsOpen || host.Recorder.CurrentLog.events.Count(e => e.type == "duel_resolved") != lossEvents)
                    throw new InvalidOperationException("Resolved loss Escape applied another outcome");
                yield return new WaitForSecondsRealtime(1.3f);
                if (!clean.IsOpen || host.ActiveClean != session || pid.text != draft ||
                    !pid.interactable || !pid.isFocused ||
                    EventSystem.current.currentSelectedGameObject != pid.gameObject)
                    throw new InvalidOperationException("CLEAN did not resume intact");
                Frame("13-clean-restored");
                if (DuelCapture.Argument("-duelConcession", "false") == "true")
                {
                    var concession = ConcessionCases();
                    while (concession.MoveNext()) yield return concession.Current;
                }
                Debug.Log("DuelCapture PASS: " + gameObject.scene.name + " " + width +
                    "x" + height + " scripted Play Mode frames in " + output);
        }

        private IEnumerator ConcessionCases()
        {
            // The same authored scenes and real Play Mode frames as the ordinary
            // Duel capture. Input System state drives the actual coordinator.
            host.enabled = false;
            previousKeyboard = Keyboard.current;
            concessionKeyboard = InputSystem.AddDevice<Keyboard>();
            concessionKeyboard.MakeCurrent();
            var command = Field<TMP_InputField>(terminal, "commandInput");
            var pid = Field<TMP_InputField>(clean, "pidInput");
            var hint = Field<GameObject>(overlay, "concedeHint");
            host.ResetSimulation(); host.Recorder.BeginRun();
            QueueKeys(Key.Escape); yield return null; yield return null;
            director.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "held-before-open", out _, DuelConcessionWitness.Infection);
            yield return new WaitForSecondsRealtime(.25f);
            CheckConcession(overlay.IsOpen && director.Current.Outcome == null,
                "Escape held before Duel opens cannot manufacture a concession");
            QueueKeys(); yield return null; yield return null;
            QueueKeys(Key.Escape); yield return null; yield return null;
            CheckConcession(!overlay.IsOpen && director.Current == null,
                "release and fresh Escape concede after held-key opening");
            QueueKeys(); yield return null; yield return null;
            host.ResetSimulation(); host.Recorder.BeginRun();
            terminal.RestoreKeyboardFocus(); yield return null; yield return null;
            command.SetTextWithoutNotify("inspect db");
            command.selectionStringAnchorPosition = 2; command.selectionStringFocusPosition = 7;
            director.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "splash-concession", out _, DuelConcessionWitness.Infection);
            var splashEncounter = director.Current; var splashRace = Race();
            CheckConcession(hint.activeInHierarchy && !overlay.IsRacing, "concede hint visible during splash");
            Frame("14-concede-splash-hint");
            yield return new WaitForSecondsRealtime(.2f);
            QueueKeys(Key.Escape, Key.X);
            yield return null; yield return null;
            CheckConcession(!overlay.IsOpen && director.Current == null && splashEncounter.Outcome == DuelOutcome.Conceded &&
                splashRace.PlayerWon == null && splashRace.ElapsedSeconds == 0 && host.Integrity.For(splashEncounter.Target).Value == 1,
                "Escape during splash immediately concedes, with unfinished zero-time race and one strike");
            yield return null;
            CheckConcession(command.isFocused && command.text == "inspect db" &&
                command.selectionStringAnchorPosition == 2 && command.selectionStringFocusPosition == 7,
                "terminal focus, draft and selection restored while keys held: focused=" + command.isFocused +
                    " draft=" + command.text + " selection=" + command.selectionStringAnchorPosition + "/" + command.selectionStringFocusPosition);
            Native(command, KeyCode.X, 'x');
            CheckConcession(command.text == "inspect db", "held character cannot edit resumed terminal");
            QueueKeys(); yield return null; yield return null;
            Native(command, KeyCode.Z, 'z');
            CheckConcession(command.text.Contains("z"), "fresh character edits resumed terminal after release");
            Frame("15-conceded-terminal-receipt");
            CheckConcession(Field<RectTransform>(terminal, "historyContent").GetComponentsInChildren<TMP_Text>()
                .Any(t => t.text.Contains("DUEL CONCEDED") && t.text.Contains("DAMAGED") && t.text.Contains("INTEGRITY 1/2")), "truthful concession receipt visible");
            Canvas.ForceUpdateCanvases();
            var receiptRows = Field<RectTransform>(terminal, "historyContent").GetComponentsInChildren<TMP_Text>()
                .Where(t => t.text.StartsWith("DUEL CONCEDED") || t.text.StartsWith("DUEL EFFECTS")).ToArray();
            foreach (var row in receiptRows) row.ForceMeshUpdate();
            CheckConcession(receiptRows.Length >= 2 && receiptRows.All(t => !t.isTextOverflowing),
                "concession, Integrity and effect receipts fit without clipping");
            yield return new WaitForSecondsRealtime(1.5f);
            CheckConcession(!overlay.IsOpen && splashRace.ElapsedSeconds == 0,
                "canceled splash never resumes and retired race never advances");

            host.ResetSimulation(); host.Recorder.BeginRun();
            host.TryInjectPhantomLoad("DB", 2, out _);
            host.StartClean("DB", "clean db", out var session);
            clean.Open(session, host, terminal);
            yield return new WaitForSecondsRealtime(1.7f);
            pid.SetTextWithoutNotify("123"); pid.selectionStringAnchorPosition = 1; pid.selectionStringFocusPosition = 2;
            director.TrySchedule("API", DuelVariant.CommandRace, 0, "typing-concession", out _, DuelConcessionWitness.Infection);
            var active = director.Current; var race = Race();
            for (int i = 0; i < 200 && !overlay.IsRacing; i++) yield return new WaitForSecondsRealtime(.025f);
            CheckConcession(overlay.IsRacing && hint.activeInHierarchy, "active race retains visible concede hint");
            Type('s'); Type('x'); race.BackspacePlayer(); Type('q');
            float elapsed = race.ElapsedSeconds;
            Frame("16-concede-typing-over-clean");
            QueueKeys(Key.Escape, Key.Backspace);
            yield return null; yield return null; yield return null;
            CheckConcession(!overlay.IsOpen && director.Current == null && active.Outcome == DuelOutcome.Conceded &&
                race.PlayerWon == null && race.ElapsedSeconds >= elapsed && race.Player.Mistakes == 2 && race.Player.Corrections == 1,
                "active typing concedes without fabricating completion or losing correction data");
            CheckConcession(clean.IsOpen && host.ActiveClean == session && pid.isFocused && pid.text == "123" &&
                pid.selectionStringAnchorPosition == 1 && pid.selectionStringFocusPosition == 2,
                "CLEAN resumes original session, focus, draft and selection while Escape held: open=" + clean.IsOpen +
                    " session=" + (host.ActiveClean == session) + " focused=" + pid.isFocused + " draft=" + pid.text +
                    " selection=" + pid.selectionStringAnchorPosition + "/" + pid.selectionStringFocusPosition +
                    " selected=" + EventSystem.current.currentSelectedGameObject?.name);
            Native(pid, KeyCode.Backspace);
            CheckConcession(pid.text == "123", "held Backspace cannot edit resumed CLEAN");
            yield return new WaitForSecondsRealtime(.25f);
            CheckConcession(clean.IsOpen, "held Escape cannot cascade into CLEAN");
            Frame("17-conceded-clean-restored");
            QueueKeys(); yield return null; yield return null;
            Native(pid, KeyCode.Backspace);
            CheckConcession(pid.text == "13", "fresh CLEAN Backspace edits the restored selection after release");
            QueueKeys(Key.Escape); yield return null; yield return null;
            CheckConcession(!clean.IsOpen && host.ActiveClean == null,
                "next fresh Escape cancels CLEAN through exclusive coordinator");
            QueueKeys(); yield return null; yield return null;
            int resolved = host.Recorder.CurrentLog.events.Count(e => e.type == "duel_resolved");
            overlay.DismissFromEscape();
            CheckConcession(host.Recorder.CurrentLog.events.Count(e => e.type == "duel_resolved") == resolved,
                "repeated Escape cannot reapply conceded outcome");

            host.ResetSimulation(); host.Recorder.BeginRun();
            host.TryInjectPhantomLoad("DB", 2, out _);
            host.StartClean("DB", "clean db", out var expiring);
            clean.Open(expiring, host, terminal);
            yield return new WaitForSecondsRealtime(1.7f);
            director.TrySchedule("API", DuelVariant.CommandRace, 0, "invalid-clean", out _, DuelConcessionWitness.Infection);
            host.CancelClean(); yield return null; yield return null;
            yield return new WaitForSecondsRealtime(.2f);
            QueueKeys(Key.Escape); yield return null; yield return null; yield return null;
            CheckConcession(!clean.WantsKeyboardFocus && !overlay.IsOpen && command.isFocused,
                "CLEAN ended under Duel: concession restores terminal instead");
            QueueKeys(); yield return null; yield return null;

            host.ResetSimulation(); host.Recorder.BeginRun();
            director.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "reset-splash", out _, DuelConcessionWitness.Infection);
            host.ResetSimulation();
            yield return new WaitForSecondsRealtime(1.5f);
            CheckConcession(!overlay.IsOpen && director.Current == null && host.Infections.Count == 0 &&
                Field<Coroutine>(overlay, "splashRoutine") == null && Field<Coroutine>(overlay, "resultRoutine") == null &&
                Field<Keyboard>(overlay, "subscribedKeyboard") == null,
                "reset cancels splash coroutine and input subscription without conceding");
            director.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "disable", out _, DuelConcessionWitness.Infection);
            overlay.gameObject.SetActive(false);
            yield return null;
            CheckConcession(!overlay.IsOpen && host.Infections.Count == 0 && director.Current.Outcome == null,
                "external presentation disable aborts without concession");
            director.ResetDuel();

            host.ResetSimulation(); host.Recorder.BeginRun();
            var selector = new HackerDirectiveSelector(host.Integrity, host.PhysicalLinks, HackerDirective.BREACH, 1701);
            selector.EnsureOffensive(0); var path = selector.Offensive.path;
            foreach (string id in path.Skip(1))
                for (int strike = 0; strike < (id == path.Last() ? 1 : 2); strike++)
                    host.DamageIntegrity(host.FindNodeResources(id), "fixture", "path");
            director.TrySchedule(path.Last(), DuelVariant.CommandRace, 0, "concession-core", out _, DuelConcessionWitness.Infection);
            yield return new WaitForSecondsRealtime(.2f);
            QueueKeys(Key.Escape); yield return null; yield return null; yield return null;
            CheckConcession(host.RunEnded && !overlay.IsOpen && director.Current == null &&
                host.Recorder.CurrentLog.events.Count(e => e.type == "core_breached") == 1 &&
                host.Recorder.CurrentLog.events.Count(e => e.type == "duel_resolved") == 1,
                "simultaneous CORE loss closes overlay and settles exactly once");
            Frame("18-concession-core-breach");
            QueueKeys(); yield return null; yield return null;
            CheckConcession(command.isFocused, "CORE ending leaves terminal ready for restart");
            Debug.Log("DuelConcessionCapture PASS: " + host.gameObject.scene.name + " " + width + "x" + height);
        }

        private void QueueKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(concessionKeyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
        private static void Native(TMP_InputField field, KeyCode key, char character = '\0')
        {
            typeof(Event).GetMethod("QueueEvent", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new Event { type = EventType.KeyDown, keyCode = key, character = character } });
            field.OnUpdateSelected(new BaseEventData(EventSystem.current));
        }
        private static void CheckConcession(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("DuelConcessionCapture: " + message);
            Debug.Log("CONCESSION PLAY CHECK: " + message);
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
            if (overlay.IsRacing) CheckReferenceLayout();
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
