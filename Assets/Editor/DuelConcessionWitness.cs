using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class DuelConcessionWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsDuelConcessionV0";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static ConsequenceConfiguration Infection => ConsequenceConfiguration.Explicit(
            ConsequenceCandidate.Atomic("infection", new ConsequenceEffectSpec
            { kind = ConsequenceKind.Infection, infectionKind = InfectionKind.PhantomLoad, parameter = 3 }));

        [MenuItem("Tools/Connections/Run Duel Concession Witness")]
        public static void Run()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                var host = IntegrityWitness.Open(scene: scene);
                var director = host.GetComponent<ConnectionsDuelDirector>();
                var overlay = UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
                var auth = host.FindNodeResources("AUTH");
                director.TrySchedule("AUTH", DuelVariant.CommandRace, 5, "pending", out _, Infection);
                var pending = director.Current;
                Check(!director.Concede(pending, Race()) && pending.Lifecycle == DuelLifecycle.Pending &&
                    host.Infections.Count == 0, "pending cannot concede");
                overlay.DismissFromEscape();
                Check(director.Current == pending, "Escape before overlay preserves pending encounter");
                host.ResetSimulation(); host.Recorder.BeginRun();

                foreach (bool concede in new[] { false, true })
                {
                    host.ResetSimulation(); host.Recorder.BeginRun();
                    director.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "paired-loss", out _, Infection);
                    var encounter = director.Current;
                    var plan = encounter.LossPlan;
                    var race = Race();
                    race.Advance(.25f); race.TypePlayer('a'); race.TypePlayer('x'); race.BackspacePlayer(); race.TypePlayer('q');
                    if (!concede) race.Advance(100);
                    // Reentry while the direct strike notifies listeners cannot resolve twice.
                    Action<IntegrityEvent> reenter = entry =>
                    {
                        if (entry.source != "duel") return;
                        Check(!director.Concede(encounter, race), "reentrant concession rejected");
                        director.Resolve(race);
                    };
                    host.IntegrityOccurred += reenter;
                    if (concede) Check(director.Concede(encounter, race), "unfinished race concedes");
                    else director.Resolve(race);
                    host.IntegrityOccurred -= reenter;
                    Check(encounter.Outcome == (concede ? DuelOutcome.Conceded : DuelOutcome.Lost) &&
                        encounter.LossPlan == plan && encounter.LossExecution.Components.Count == 1 &&
                        encounter.InfectionApplied == true && host.Infections.Count == 1 &&
                        host.Integrity.For(auth).Value == 1 && host.Integrity.For(auth).Revealed,
                        "loss/concession: frozen plan executes, expected one infection and overt Integrity 1/2");
                    var events = host.Recorder.CurrentLog.events;
                    Check(events.Count(e => e.type == "integrity_damage") == 1 &&
                        events.Single(e => e.type == "integrity_damage").integrity.cause == "duel:" + encounter.Id &&
                        events.Single(e => e.type == "integrity_damage").integrity.source == "duel" &&
                        events.Count(e => e.type == "consequence_executed") == 1 &&
                        events.Count(e => e.type == "duel_slot_released") == 1 && director.Current == null,
                        "exactly one consequence execution, Duel-attributed strike and slot release");
                    var record = JsonUtility.FromJson<RunEvent>(JsonUtility.ToJson(events.Single(e => e.type == "duel_resolved")));
                    Check(record.duelOutcome == (concede ? "conceded" : "lost") &&
                        record.duelElapsedSeconds == race.ElapsedSeconds &&
                        record.duelPlayerProgress == race.Player.Progress && record.duelHackerProgress == race.Hacker.Progress &&
                        record.duelPlayerMistakes == 2 && record.duelPlayerCorrections == 1 &&
                        record.duelHackerCompleted == !concede && !record.duelPlayerCompleted &&
                        (concede ? race.PlayerWon == null && race.PlayerCompletedAt == null &&
                            race.HackerCompletedAt == null && record.duelHackerSeconds == -1 : race.PlayerWon == false),
                        "JSON outcome, elapsed, partial progress, mistakes/corrections and completion remain truthful");
                    Check(!director.Concede(encounter, race), "repeat concession rejected");
                    director.Resolve(race); overlay.DismissFromEscape();
                    Check(events.Count(e => e.type == "consequence_executed") == 1 &&
                        events.Count(e => e.type == "integrity_damage") == 1, "resolved Escape/repeated resolution cannot add loss");
                    overlay.Abort();
                    Check(director.TrySchedule("DB", DuelVariant.CommandRace, 5, "after-release", out _, Infection),
                        "released global slot immediately accepts another encounter");
                    Check(host.TryInjectInfection("AUTH", InfectionKind.Backdoor, 0, out _) &&
                        host.Integrity.For(auth).Value == 0, "later independent infection retains normal Integrity strike");
                }

                host.ResetSimulation(); host.Recorder.BeginRun();
                host.TryInjectPhantomLoad("AUTH", 2, out _);
                director.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "duplicate", out _, Infection);
                var rejected = director.Current;
                director.Concede(rejected, Race()); overlay.Abort();
                Check(rejected.LossExecution.Components.Single().Reason == "infection_already_active" &&
                    !rejected.LossExecution.AnySucceeded && rejected.IntegrityDamaged && host.Integrity.For(auth).Value == 0,
                    "duplicate infection rejection still receives one direct strike");

                host.ResetSimulation(); host.Recorder.BeginRun();
                var burst = ConsequenceConfiguration.Explicit(ConsequenceCandidate.Tactic("burst", ConsequenceTactic.PayloadBurst));
                director.TrySchedule("API", DuelVariant.CommandRace, 0, "secondary", out _, burst);
                var secondary = director.Current;
                director.Concede(secondary, Race()); overlay.Abort();
                Check(secondary.LossExecution.Components.All(c => c.Succeeded) &&
                    secondary.LossExecution.Components.Count >= 2, "concession launches normal independent Payload burst");
                host.StepSimulation(20);
                Check(host.Recorder.CurrentLog.events.Any(e => e.type == "integrity_damage" && e.integrity.source == "infection"),
                    "later real Payload deliveries retain independent infection damage");

                host.ResetSimulation(); host.Recorder.BeginRun();
                director.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "win", out _, Infection);
                var win = Race(); foreach (char c in "abcdef") win.TypePlayer(c);
                var won = director.Current; director.Resolve(win);
                Check(won.Outcome == DuelOutcome.Won && won.PlayerWon == true && host.Infections.Count == 0 &&
                    host.Recorder.CurrentLog.events.Single(e => e.type == "duel_resolved").duelOutcome == "won",
                    "ordinary win still prevents consequences and records won");
                overlay.DismissFromEscape();
                Check(overlay.IsOpen && host.Integrity.For(auth).Value == 2, "resolved result consumes Escape without another loss");
                host.ResetSimulation(); host.Recorder.BeginRun();
                director.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "reset", out _, Infection);
                var stale = director.Current; var unfinished = Race();
                host.ResetSimulation();
                Check(!overlay.IsOpen && director.Current == null && !director.Concede(stale, unfinished) &&
                    host.Infections.Count == 0 && host.Integrity.For(auth).Value == 2,
                    "reset aborts presentation, releases slot and invalidates old concession");
                director.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "replacement", out _, Infection);
                Check(!director.Concede(stale, unfinished) && director.Current.Lifecycle == DuelLifecycle.Active,
                    "stale overlay cannot concede a replacement encounter");
                director.enabled = false;
                typeof(ConnectionsDuelDirector).GetMethod("OnDisable", Private).Invoke(director, null);
                Check(director.Current == null && !overlay.IsOpen && host.Infections.Count == 0,
                    "director disable aborts without consequences");

                // Prepare the actual ingress path with its final critical node at 1.
                host = IntegrityWitness.Open(scene: scene); director = host.GetComponent<ConnectionsDuelDirector>();
                var selector = new HackerDirectiveSelector(host.Integrity, host.PhysicalLinks, HackerDirective.BREACH, 1701);
                selector.EnsureOffensive(0); var path = selector.Offensive.path;
                foreach (string id in path.Skip(1))
                    for (int strike = 0; strike < (id == path.Last() ? 1 : 2); strike++)
                        host.DamageIntegrity(host.FindNodeResources(id), "fixture", "path");
                director.TrySchedule(path.Last(), DuelVariant.CommandRace, 0, "core", out _, Infection);
                var fatal = director.Current; var fatalRace = Race();
                host.CoreBreached += () =>
                {
                    UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include).Abort();
                    Check(!director.Concede(fatal, fatalRace), "CORE callback cannot reapply concession");
                };
                Check(director.Concede(fatal, fatalRace) && host.RunEnded && director.Current == null,
                    "concession settles and releases slot before terminal CORE boundary");
                Check(host.Recorder.CurrentLog.events.Count(e => e.type == "core_breached") == 1 &&
                    host.Recorder.CurrentLog.events.Count(e => e.type == "duel_resolved") == 1 &&
                    host.Recorder.CurrentLog.events.Count(e => e.type == "consequence_executed") == 1,
                    "reentrant CORE ending retains exactly one complete resolution");
                Debug.Log("DuelConcessionWitness PASS: " + scene);
            }
        }
        // Reuse the current canonical suite and add the adjacent terminal witnesses.
        public static void Regressions()
        {
            var reports = new List<string>();
            var cases = new (string name, Action run)[]
            {
                ("CanonicalRegressions", CanonicalSceneWitness.Regressions),
                ("CanonicalParity", CanonicalSceneWitness.Run),
                ("DuelConcession", Run),
                ("InspectV2", InspectV2Witness.Run),
                ("ResolvedInspect", ResolvedInspectWitness.Run),
                ("LoadOutput", LoadOutputWitness.Run),
                ("WorkOutput", WorkOutputWitness.Run),
                ("LogsOutput", LogsOutputWitness.Run)
            };
            foreach (var test in cases)
            {
                try { test.run(); reports.Add("PASS " + test.name); }
                catch (Exception error) { reports.Add("FAIL " + test.name + ": " + error.Message); Debug.LogException(error); }
            }
            Directory.CreateDirectory(Evidence);
            File.WriteAllLines(Evidence + "/regression-checks.txt", reports);
            foreach (string report in reports) Debug.Log("CONCESSION REGRESSION " + report);
            if (reports.Any(r => r.StartsWith("FAIL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Concession regression failures; inspect canonical and focused reports.");
        }
        private static CommandRaceSession Race() => new(new[] { "abcdef" }, new[] { "xyzxyzxyz" }, 17, .2f, 0, 0, 0);
        private static void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException("DuelConcessionWitness: " + message);
            Debug.Log("CONCESSION CHECK: " + message);
        }
    }
}
