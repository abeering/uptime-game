using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class DuelWitness
    {
        [MenuItem("Tools/Connections/Run Duel Witness")]
        public static void Run()
        {
            CheckRaceRules();
            CheckReferenceFormatting();
            CheckScene("Assets/Scenes/Connections.unity");
            CheckScene("Assets/Scenes/ConnectionsAdvanced.unity");
            Debug.Log("DuelWitness PASS: race state, immutable typing reference, " +
                "token/character/error highlights, pending/active exclusivity, " +
                "connection effect, status countdown, outcomes, infection rejection, " +
                "pause policy, reset, and both authored scene bindings.");
        }

        private static void CheckRaceRules()
        {
            var race = new CommandRaceSession(new[] { "abc", "de" },
                new[] { "xyz" }, 123, .1f, 0f, 1f, 0f);
            race.TypePlayer('a');
            race.TypePlayer('q');
            Check(race.Player.CorrectCharacters == 1 && race.Player.Mistakes == 1 &&
                race.Player.Draft == "aq", "player error blocks actual progress");
            race.TypePlayer('c'); // Matches the later reference, but remains in the error run.
            Check(race.Player.Draft == "aqc" && race.Player.ErrorStartIndex == 1 &&
                race.Player.Mistakes == 2 && race.Player.CorrectCharacters == 1,
                "continued mistypes extend the red run without granting progress");
            race.TypePlayer('z');
            Check(race.Player.Draft == "aqc", "error run stops at the authored line end");
            race.BackspacePlayer();
            Check(race.Player.Draft == "aq" && race.Player.ErrorStartIndex == 1 &&
                race.Player.Corrections == 1, "first Backspace shortens the error run");
            race.BackspacePlayer();
            Check(race.Player.Draft == "a" && race.Player.ErrorStartIndex == -1 &&
                race.Player.Corrections == 2, "last Backspace clears error state");
            foreach (char c in "bcde") race.TypePlayer(c);
            Check(race.PlayerWon == true && race.Player.Line == 2 &&
                race.Player.CorrectCharacters == 5 && race.Player.Corrections == 2 &&
                race.PlayerCompletedAt == 0f, "automatic line advance and immediate win");

            var opponent = new CommandRaceSession(new[] { "abcdef" },
                new[] { "xyz" }, 17, .1f, 0f, 1f, 0f);
            opponent.Advance(.1f);
            Check(opponent.Hacker.Mistakes > 0 && opponent.Hacker.CorrectCharacters == 0 &&
                opponent.Hacker.Draft.Length == 1, "hacker mistake affects actual progress");
            var corrected = new CommandRaceSession(new[] { "abcdef" },
                new[] { "xyzxyz" }, 17, .1f, 0f, .3f, 0f);
            corrected.Advance(100f);
            Check(corrected.PlayerWon == false && corrected.Hacker.Corrections > 0 &&
                corrected.HackerCompletedAt > .6f,
                "hacker backspaces before completing its sequence");
        }

        private static void CheckScene(string path)
        {
            EditorSceneManager.OpenScene(path);
            SimulationHost host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = host.GetComponent<ConnectionsDuelDirector>();
            var overlay = UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(
                FindObjectsInactive.Include);
            var clean = UnityEngine.Object.FindFirstObjectByType<ConnectionsCleanOverlay>(
                FindObjectsInactive.Include);
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            var placement = UnityEngine.Object.FindFirstObjectByType<ConnectionsWindowPlacement>();
            var status = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            Check(host != null && director != null && overlay != null && clean != null &&
                terminal != null && placement != null && status != null,
                "authored services exist in " + path);
            Check(((RectTransform)overlay.transform).sizeDelta == new Vector2(710, 465) &&
                overlay.transform.parent == clean.transform.parent &&
                overlay.GetComponentsInChildren<TMP_Text>(true).Length >= 5 &&
                !overlay.gameObject.activeSelf, "authored CLEAN-sized Duel window");
            Check(new SerializedObject(director).FindProperty("overlay").objectReferenceValue == overlay &&
                new SerializedObject(overlay).FindProperty("terminal").objectReferenceValue == terminal &&
                new SerializedObject(placement).FindProperty("duelOverlay").objectReferenceValue == overlay,
                "scene references connect director, window, placement, terminal");

            ConnectionTrafficDirector traffic = host.GetComponent<ConnectionTrafficDirector>();
            bool oldAuto = traffic.AutoGenerationEnabled;
            // Edit Mode does not invoke gameplay MonoBehaviour enable callbacks.
            typeof(ConnectionsDuelDirector).GetMethod("OnEnable", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(director, null);
            try
            {
                traffic.AutoGenerationEnabled = false;
                host.SetRunMode(ConnectionRunMode.Generated);
                Check(director.TrySchedule("AUTH", DuelVariant.CommandRace, 2f,
                    "witness", out _), "director API schedules Duel");
                Check(!director.TrySchedule("DB", DuelVariant.CommandRace, 1f,
                    "second", out string busy) && busy.Contains("pending"),
                    "pending slot rejects second Duel");
                host.StepSimulation(1.25f);
                var auth = host.FindNodeResources("AUTH").View;
                status.RenderNow(1f, 1f);
                Check(Mathf.Abs(director.RemainingFor(auth) - .75f) < .02f &&
                    status.StateFor(auth).IntrusionPending &&
                    Mathf.Abs(status.StateFor(auth).IntrusionRemainingSeconds - .75f) < .02f,
                    "status reads authoritative simulation countdown");
                host.StepSimulation(.75f);
                Check(director.Current?.Lifecycle == DuelLifecycle.Active && overlay.IsOpen,
                    "countdown automatically opens authored window");
                Check(!director.TrySchedule("DB", DuelVariant.CommandRace, 1f,
                    "second", out busy) && busy.Contains("active"),
                    "active slot rejects second Duel");
                Set(director, "continueSimulationDuringDuel", false);
                Check(director.PausesSimulation, "configurable simulation pause policy");
                Set(director, "continueSimulationDuringDuel", true);
                Check(!director.PausesSimulation, "default continuation policy");
                host.ResetSimulation();
                Check(director.Current == null && !overlay.IsOpen,
                    "reset releases slot and closes Duel");

                ConnectionSpawnPlan carrier = traffic.QueueProfile("duel-auth");
                Check(carrier.IsHostile && carrier.Operations.Last().EffectOnCompletion ==
                    CompletionEffect.DuelSchedule, "explicit connection completion effect");
                for (int i = 0; i < 100 && director.Current == null; i++)
                    host.StepSimulation(.5f);
                Check(director.Current?.Lifecycle == DuelLifecycle.Pending &&
                    director.Current.Source.StartsWith("connection:"),
                    "connection completion schedules same Duel slot");
                host.ResetSimulation();

                Check(director.TrySchedule("AUTH", DuelVariant.CommandRace, 0f,
                    "win", out _), "zero-warning test opens directly");
                var win = new CommandRaceSession(new[] { "abc" }, new[] { "xyz" },
                    7, .2f, 0f, 0f, 0f);
                DuelEncounter wonEncounter = director.Current;
                foreach (char c in "abc") win.TypePlayer(c);
                director.Resolve(win);
                Check(wonEncounter.PlayerWon == true && host.Infections.Count == 0 &&
                    director.Current == null, "victory prevents infection and releases slot");
                director.FinishResult();
                host.ResetSimulation();

                Check(director.TrySchedule("AUTH", DuelVariant.CommandRace, 0f,
                    "loss", out _), "loss encounter opens");
                var loss = new CommandRaceSession(new[] { "abc" }, new[] { "x" },
                    7, .1f, 0f, 0f, 0f);
                DuelEncounter lostEncounter = director.Current;
                loss.Advance(1f);
                director.Resolve(loss);
                Check(lostEncounter.PlayerWon == false &&
                    lostEncounter.InfectionApplied == true && host.Infections.Count == 1 &&
                    host.Infections[0].SourceConnectionId.StartsWith("duel:"),
                    "loss applies authoritative infection with Duel provenance");
                director.FinishResult();
                host.ResetSimulation();
                Check(host.TryInjectPhantomLoad("AUTH", 2f, out _), "preexisting infection installed");
                Check(director.TrySchedule("AUTH", DuelVariant.CommandRace, 0f,
                    "duplicate", out _), "duplicate case opens");
                var duplicate = new CommandRaceSession(new[] { "abc" }, new[] { "x" },
                    7, .1f, 0f, 0f, 0f);
                DuelEncounter rejectedEncounter = director.Current;
                duplicate.Advance(1f);
                director.Resolve(duplicate);
                Check(rejectedEncounter.InfectionApplied == false &&
                    rejectedEncounter.ApplicationReason == "infection_already_active" &&
                    host.Infections.Count == 1, "duplicate application rejected honestly");
                host.ResetSimulation();
            }
            finally
            {
                typeof(ConnectionsDuelDirector).GetMethod("OnDisable", BindingFlags.Instance |
                    BindingFlags.NonPublic).Invoke(director, null);
                traffic.AutoGenerationEnabled = oldAuto;
                Set(director, "continueSimulationDuringDuel", true);
                host.ResetSimulation();
            }
        }

        private static void CheckReferenceFormatting()
        {
            const string expected = "> ssh operator@auth\n> tail -f /var/log/auth.log";
            var race = new CommandRaceSession(
                new[] { "ssh operator@auth", "tail -f /var/log/auth.log" },
                new[] { "echo x" }, 17, .2f, 0f, 0f, 0f);
            string start = RenderReference(race);
            Check(PlainReference(start) == expected && UnderlinedToken(start) == "ssh" &&
                start.Contains("<mark=#55E9C140>"),
                "first command, token, and expected character are marked");
            foreach (char c in "ssh operator@") race.TypePlayer(c);
            string partial = RenderReference(race);
            Check(PlainReference(partial) == expected &&
                UnderlinedToken(partial) == "operator@auth" &&
                partial.Contains("<mark=#55E9C140>"),
                "typed prefix keeps the complete reference and current token");
            race.TypePlayer('x');
            string error = RenderReference(race);
            Check(PlainReference(error) == expected &&
                UnderlinedToken(error) == "operator@auth" &&
                error.Contains("<mark=#B4404E80>") &&
                error.Contains("<mark=#E94D5988>"),
                "error marks the expected character without replacing it");
            race.TypePlayer('u');
            race.TypePlayer('t');
            string run = RenderReference(race);
            Check(PlainReference(run) == expected &&
                Regex.Matches(run, "<mark=#B4404E80>").Count == 3 &&
                race.Player.CorrectCharacters == "ssh operator@".Length,
                "three expected characters turn red while correct progress stays blocked");
            race.BackspacePlayer();
            string shorter = RenderReference(race);
            Check(Regex.Matches(shorter, "<mark=#B4404E80>").Count == 2 &&
                PlainReference(shorter) == expected,
                "Backspace removes one red character at a time");
            race.BackspacePlayer();
            race.BackspacePlayer();
            string corrected = RenderReference(race);
            Check(PlainReference(corrected) == expected &&
                corrected.Contains("<mark=#55E9C140>") &&
                !corrected.Contains("<mark=#B4404E80>") &&
                race.Player.Corrections == 3,
                "clearing the error run restores the readable normal cursor");
            foreach (char c in "auth") race.TypePlayer(c);
            race.TypePlayer('\n');
            race.TypePlayer('t');
            string next = RenderReference(race);
            Check(race.Player.Line == 1 && race.Player.Draft == "t" &&
                PlainReference(next) == expected && UnderlinedToken(next) == "tail",
                "next command accepts its first character without Enter");
            foreach (char c in "ail -f /var/") race.TypePlayer(c);
            string path = RenderReference(race);
            Check(PlainReference(path) == expected &&
                UnderlinedToken(path) == "/var/log/auth.log",
                "punctuation-heavy path remains one active token");
        }

        private static string RenderReference(CommandRaceSession race)
        {
            return (string)typeof(ConnectionsDuelOverlay)
                .GetMethod("Format", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { race.Player, true });
        }

        private static string PlainReference(string formatted) =>
            Regex.Replace(formatted, "<[^>]+>", "");

        private static string UnderlinedToken(string formatted)
        {
            int start = formatted.IndexOf("<u>", StringComparison.Ordinal) + 3;
            int end = formatted.IndexOf("</u>", start, StringComparison.Ordinal);
            return start < 3 || end < start ? string.Empty :
                Regex.Replace(formatted.Substring(start, end - start), "<[^>]+>", "");
        }

        private static void Set(object owner, string field, object value) =>
            owner.GetType().GetField(field, BindingFlags.Instance |
                BindingFlags.NonPublic).SetValue(owner, value);
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("DuelWitness: " + message);
            Debug.Log("DUEL CHECK: " + message);
        }
    }
}
