using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class WorkShiftWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsWorkObjectiveV0";
        private static readonly List<string> checks = new();
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static void Author()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                EditorSceneManager.OpenScene(scene);
                var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                var fields = new SerializedObject(host);
                fields.FindProperty("shiftEnabled").boolValue = true;
                fields.FindProperty("shiftDurationSeconds").floatValue = 360;
                fields.FindProperty("shiftWorkTarget").floatValue = scene == CanonicalSceneAuthoring.Scenes[0] ? 900 : 1200;
                fields.ApplyModifiedPropertiesWithoutUndo();
                var panel = UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>();
                var strip = new SerializedObject(panel);
                TMP_Text Text(string name) => (TMP_Text)strip.FindProperty(name).objectReferenceValue;
                void Place(TMP_Text text, float x, float width)
                {
                    text.gameObject.SetActive(true);
                    text.rectTransform.pivot = new Vector2(0, .5f);
                    text.rectTransform.anchoredPosition = new Vector2(x, -25);
                    text.rectTransform.sizeDelta = new Vector2(width, 40);
                    text.fontSize = 22; text.enableAutoSizing = false;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text.rectTransform);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text.gameObject);
                }
                Place(Text("clockText"), 24, 285); Text("clockText").text = "UPTIME 00:00 / 06:00";
                Place(Text("valueText"), 320, 230); Text("valueText").text = $"WORK 0 / {host.ShiftWorkTarget:0}";
                Place(Text("rateText"), 748, 150); Text("rateText").text = "";
                var fill = (Image)strip.FindProperty("healthFill").objectReferenceValue;
                var bar = (RectTransform)fill.transform.parent;
                bar.gameObject.SetActive(true); bar.anchoredPosition = new Vector2(560, -25); bar.sizeDelta = new Vector2(168, 18);
                foreach (var obj in new UnityEngine.Object[] { Text("clockText"), Text("valueText"), Text("rateText"), bar, bar.gameObject })
                    PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                var presenter = UnityEngine.Object.FindFirstObjectByType<IntegrityPresenter>();
                var result = new SerializedObject(presenter);
                var root = (GameObject)result.FindProperty("breachPanel").objectReferenceValue;
                var title = root.transform.Find("Title").GetComponent<TMP_Text>();
                result.FindProperty("resultTitle").objectReferenceValue = title;
                result.ApplyModifiedPropertiesWithoutUndo();
                root.name = "Shift result";
                ((RectTransform)root.transform).sizeDelta = new Vector2(880, 250);
                title.fontSize = 28; title.text = "SHIFT COMPLETE";
                ((TMP_Text)result.FindProperty("breachPath").objectReferenceValue).fontSize = 18;
                EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
                EditorSceneManager.SaveScene(host.gameObject.scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("WorkShift authoring complete: both existing status strips/result panels, no generated runtime UI.");
        }
        public static SimulationHost Open(string scene, float duration = 10, float target = 1000, bool generated = false, int seed = 1701)
            => IntegrityWitness.Open(generated, seed, host =>
            {
                IntegrityWitness.Set(host, "shiftEnabled", true);
                IntegrityWitness.Set(host, "shiftDurationSeconds", duration);
                IntegrityWitness.Set(host, "shiftWorkTarget", target);
                if (!generated) { IntegrityWitness.Set(host, "connectionTravelSecondsPerLink", .000001f); IntegrityWitness.Set(host, "naturalLoadNoiseEnabled", false); }
            }, scene);
        public static LogicalConnectionState Add(SimulationHost host, string id, float[] work, bool hostile = false,
            CompletionEffect effect = CompletionEffect.None, TimedModifierBehaviorDefinition[] behaviors = null)
        {
            var plan = host.GetComponent<ConnectionTrafficDirector>().GenerateBenignPlan(0);
            var stages = work.Select((w, i) => new OperationStage(OperationKind.Read, w, 5, "WORK witness",
                i == work.Length - 1 ? effect : CompletionEffect.None,
                behaviors: behaviors, payload: effect == CompletionEffect.InfectionDelivery && i == work.Length - 1 ? new InfectionPayload(InfectionKind.Backdoor, 0, 0) : null)).ToArray();
            return host.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0, id,
                plan.Route, "work-witness", "WORK witness", stages, hostile)));
        }
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("WORK FAIL: " + message); checks.Add("PASS " + message); }
        [MenuItem("Tools/Connections/Run WORK Shift Witness")]
        public static void Run()
        {
            checks.Clear();
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                var host = Open(scene); var first = Add(host, "w1", new[] { 1f, 2f });
                host.StepSimulation(.3f);
                Check(host.CreditedWork == 1 && first.CurrentOperationIndex == 1, scene + " first operation expected credit1, incomplete second0");
                host.KillConnection("w1");
                Check(host.CreditedWork == 1 && host.Shift.lost == 2 && host.Shift.unfinished == 0 && host.Shift.eligible == 3,
                    "partial kill retains1, lost2, unfinished0, eligible3");
                Add(host, "w2", new[] { 2f, 3f }); host.StepSimulation(2);
                Add(host, "h1", new[] { 1f, 2f }, true); host.StepSimulation(2);
                Check(host.CreditedWork == 6 && host.WorkCreditCount == 3, "multi-operation benign adds5; hostile arc adds0");
                var strip = UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>();
                for (int i = 0; i < 5; i++) { strip.Render(); host.Recorder.CaptureSnapshot(host.SimulationTimeSeconds); }
                typeof(SimulationHost).GetMethod("CreditCompletedOperation", Private).Invoke(host, new object[] { first, 0, 1f });
                Check(host.CreditedWork == 6 && host.WorkCreditCount == 3, "duplicate completion, recorder and renders cannot duplicate credit");
                host.ResetSimulation(); host.Recorder.BeginRun();
                Check(host.CreditedWork == 0 && host.WorkCreditCount == 0 && host.Shift.eligible == 0, "reset clears accounting and retained history");
                Add(host, "w1", new[] { 1f }); host.StepSimulation(.5f);
                Check(host.CreditedWork == 1, "same ID credits normally after restart");

                host = Open(scene); Add(host, "modified", new[] { 2f }, behaviors: new[] {
                    new TimedModifierBehaviorDefinition("extra", 0, 100, 3, 0) }); host.StepSimulation(2);
                Check(host.CreditedWork == 5, "actual effective requirement5 credited instead of authored base2");
                host = Open(scene); var reduced = Add(host, "reduced", new[] { 10f }, behaviors: new[] {
                    new TimedModifierBehaviorDefinition("shrink", 1.4f, 100, -8, 0) }); host.StepSimulation(2);
                Check(host.CreditedWork > 2 && host.CreditedWork < 10 && host.CreditedWork == reduced.CompletedWork,
                    "requirement reduction preserves actual accomplished WORK above final requirement2");

                host = Open(scene, target: 1); Add(host, "early", new[] { 1f }); host.StepSimulation(1);
                Check(host.Ending == RunEnding.WorkComplete && host.SimulationTimeSeconds < 10, "quota immediately ends shift before deadline");
                Frozen(host);
                host = Open(scene, duration: .125f, target: .625f); Add(host, "boundary", new[] { .625f }); host.StepSimulation(1);
                Check(host.Ending == RunEnding.WorkComplete && host.SimulationTimeSeconds == .125f && host.CreditedWork >= .625f,
                    "operation completing on final boundary credited before deadline success: " + JsonUtility.ToJson(host.Shift));
                host = Open(scene, duration: .125f, target: .626f); Add(host, "rounded", new[] { .625f }); host.StepSimulation(1);
                Check(host.Ending == RunEnding.InsufficientWork && host.SimulationTimeSeconds == .125f,
                    "precise quota comparison: .625 credit cannot satisfy .626");
                host = Open(scene, duration: .04f, target: 1); var partial = Add(host, "partial", new[] { 5f }); host.StepSimulation(1);
                Check(host.Ending == RunEnding.InsufficientWork && host.CreditedWork == 0 && partial.CompletedWork > 0 && host.Shift.unfinished == 5,
                    "deadline: partial progress grants0; entire uncredited operation remains unfinished5");
                Frozen(host);

                host = Open(scene, target: .1f);
                var selector = new HackerDirectiveSelector(host.Integrity, host.PhysicalLinks, HackerDirective.BREACH, 1701);
                selector.EnsureOffensive(0);
                string[] route = selector.Offensive.path;
                foreach (string id in route.Where(id => !host.Integrity.For(host.FindNodeResources(id)).External))
                {
                    host.DamageIntegrity(host.FindNodeResources(id), "work-witness", "prepare", completeBoundary: false);
                    if (id != route.Last()) host.DamageIntegrity(host.FindNodeResources(id), "work-witness", "prepare", completeBoundary: false);
                }
                Add(host, "simultaneous", new[] { .1f });
                Action<LogicalConnectionState, int, float> strike = (state, index, work) =>
                    host.DamageIntegrity(host.FindNodeResources(route.Last()), "work-witness", "same-boundary", completeBoundary: false);
                host.OperationCompleted += strike; host.StepSimulation(1); host.OperationCompleted -= strike;
                Check(host.Ending == RunEnding.CoreBreached && host.CreditedWork >= .1f && host.Integrity.Breach != null,
                    "simultaneous boundary retains credit but CORE breach wins");
                Check(host.Recorder.CurrentLog.events.Count(e => e.type == "core_breached") == 1 &&
                    host.Recorder.CurrentLog.events.Count(e => e.type == "shift_ended") == 1, "one CORE event and one terminal result");
                Frozen(host);
                host = Open(scene); host.StartScan("DB", "scan db", out var scan); host.StartClean("DB", "clean db", out var clean);
                var duel = host.GetComponent<ConnectionsDuelDirector>(); duel.TrySchedule("AUTH", DuelVariant.CommandRace, 0, "shift-witness", out _);
                var encounter = duel.Current; host.StepSimulation(11);
                Check(host.Ending == RunEnding.InsufficientWork && host.ActiveClean == clean && encounter.Outcome == null &&
                    !duel.Concede(encounter, new CommandRaceSession(new[] { "a" }, new[] { "b" }, 1, 1, 0, 0, 0)) &&
                    host.SelectCleanPid(clean.Processes[0].Pid, out _) == CleanSelectionResult.NoSession,
                    "ending preserves diagnostic CLEAN/Duel, rejects concession and late PID without penalties");
                Check(host.Recorder.CurrentLog.events.Count(e => e.type == "duel_aborted") == 1, "active Duel abort recorded once without invented outcome");
                host = Open(scene, duration: 1); host.StartScan("DB", "scan db", out scan); host.StepSimulation(2);
                Check(host.ActiveScan == scan && !host.Recorder.CurrentLog.events.Any(e => e.type == "scan_completed"), "ending freezes incomplete scan");
                host.Recorder.EndRun();
            }
            Directory.CreateDirectory(Evidence); File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks);
            Debug.Log("WorkShiftWitness PASS: " + checks.Count + " assertions.");
        }
        private static void Frozen(SimulationHost host)
        {
            string before = JsonUtility.ToJson(host.Shift); int count = host.Recorder.CurrentLog.events.Count;
            host.StepSimulation(100); host.GetComponent<ConnectionTrafficDirector>().Tick(1000, true);
            Check(JsonUtility.ToJson(host.Shift) == before && !host.Recorder.IsRecording && host.Recorder.CurrentLog.run.endedUtc != null &&
                host.Recorder.CurrentLog.events.Count == count, "ending freezes time/generation/accounting and finalizes recorder once");
            Check(!host.KillConnection("partial") && !host.TryInjectInfection("DB", InfectionKind.Backdoor, 0, out _) &&
                !host.DamageIntegrity(host.FindNodeResources("DB"), "test", "after-end"), "post-ending mutations rejected");
            var restored = JsonUtility.FromJson<ConnectionsRunLog>(JsonUtility.ToJson(host.Recorder.CurrentLog));
            Check(restored.run.shiftEnabled && restored.run.finalShift.ending == host.Ending.ToString() &&
                restored.snapshots.Last().shift.credited == host.CreditedWork, "recorder JSON has initial config, cause and exact final snapshot");
        }
        [Serializable] public sealed class Credit
        {
            public string connectionId;
            public int operationIndex;
            public float timeSeconds, completedWork;
            public double creditedWork;
        }
        [Serializable] public sealed class Measurement
        {
            public string scene, policy, ending, signature;
            public int seed, commands;
            public float time, coreBreachTime = -1;
            public ShiftSnapshot final;
            public double work180, work270, work360;
            public Credit[] credits;
        }
        [Serializable] public sealed class Measurements { public Measurement[] runs; }
        public static void ValidateAndCalibrate() { Run(); Calibrate(); }
        public static void Calibrate()
        {
            var results = new List<Measurement>();
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
                foreach (int seed in new[] { 1701, 1702, 3141, 9001, 42, 99, 31146359, 1987295192 })
                    foreach (string policy in new[] { "unattended", "visible-defense", "kill-all" })
                    {
                        var result = Observe(scene, seed, policy);
                        results.Add(result);
                        if (seed == 1701 && result.signature != Observe(scene, seed, policy).signature)
                            throw new InvalidOperationException("WORK calibration replay mismatch: " + scene + "/" + policy);
                    }
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + "/calibration.json", JsonUtility.ToJson(new Measurements { runs = results.ToArray() }, true));
            Debug.Log("WORK calibration PASS: 48 natural runs and six exact event/snapshot replays.");
        }
        public static void VerifyTargets()
        {
            var baseline = JsonUtility.FromJson<Measurements>(File.ReadAllText(Evidence + "/calibration.json"));
            // Retain just the exact credit facts used by the calibration, rather than
            // hundreds of irrelevant default RunEvent fields per credit.
            File.WriteAllText(Evidence + "/calibration.json", JsonUtility.ToJson(baseline, true));
            var actual = new List<Measurement>(); var verified = new List<string>();
            foreach (Measurement original in baseline.runs.Where(r => r.seed == 1701 || r.seed == 9001 || r.seed == 31146359))
            {
                float target = original.scene == "Connections" ? 900 : 1200;
                Credit crossing = original.credits.FirstOrDefault(c => c.creditedWork >= target &&
                    (original.coreBreachTime < 0 || c.timeSeconds < original.coreBreachTime));
                string expected = crossing != null ? "WorkComplete" : original.coreBreachTime >= 0 ? "CoreBreached" : "InsufficientWork";
                Measurement observed = Observe("Assets/Scenes/" + original.scene + ".unity", original.seed, original.policy, target);
                if (observed.ending != expected || crossing != null && observed.time != crossing.timeSeconds ||
                    observed.credits.Where((c, i) => c.connectionId != original.credits[i].connectionId ||
                        c.operationIndex != original.credits[i].operationIndex || c.completedWork != original.credits[i].completedWork).Any())
                    throw new InvalidOperationException("Actual quota behavior differs from measured credit prefix: " + original.scene + "/" + original.seed + "/" + original.policy);
                actual.Add(observed);
                verified.Add($"PASS {original.scene}/{original.seed}/{original.policy} quota {target}: expected/observed {expected} at {observed.time:R}, exact credit prefix");
            }
            File.WriteAllText(Evidence + "/actual-target-runs.json", JsonUtility.ToJson(new Measurements { runs = actual.ToArray() }, true));
            File.WriteAllLines(Evidence + "/target-checks.txt", verified);
            Debug.Log("WORK actual quota verification PASS: " + actual.Count + " natural runs match measured predictions and exact credit prefixes.");
        }
        public static Measurement Observe(string scene, int seed, string policy, float target = 1000000)
        {
            var host = Open(scene, 360, target, true, seed);
            var duel = host.GetComponent<ConnectionsDuelDirector>();
            var overlay = UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>(); IntegrityWitness.Set(terminal, "maxEntries", 10000);
            var result = new Measurement { scene = Path.GetFileNameWithoutExtension(scene), seed = seed, policy = policy };
            float nextDecision = 6, splashRemaining = 0, blockedUntil = 0; int encounter = -1;
            for (int tick = 0; tick < 18010 && !host.RunEnded; tick++)
            {
                host.StepSimulation(.02f); float now = host.SimulationTimeSeconds;
                if (duel.Current?.Lifecycle == DuelLifecycle.Active && !host.RunEnded)
                {
                    if (encounter != duel.Current.Id)
                    {
                        encounter = duel.Current.Id; string id = duel.Current.Target.View.NodeId;
                        string splash = "INTRUSION DETECTED / " + id + "\n\nssh operator@" + id.ToLowerInvariant() +
                            "\nAUTHENTICATING... OK\nINITIALIZING COUNTERMEASURES...";
                        splashRemaining = splash.Length * Mathf.Max(.02f, (float)IntegrityWitness.Field(overlay, "splashCharacterSeconds")) + .25f;
                    }
                    if (splashRemaining > 0) splashRemaining -= .02f;
                    else
                    {
                        var race = (CommandRaceSession)IntegrityWitness.Field(overlay, "race"); race.Advance(.02f);
                        if (race.PlayerWon.HasValue)
                        { duel.Resolve(race); blockedUntil = now + (float)IntegrityWitness.Field(overlay, "resultSeconds"); overlay.Abort(); }
                    }
                }
                if (!host.RunEnded && policy != "unattended" && now >= nextDecision && now >= blockedUntil &&
                    duel.Current?.Lifecycle != DuelLifecycle.Active)
                {
                    nextDecision = now + (policy == "kill-all" ? 1 : 6);
                    string command = null;
                    if (policy == "kill-all")
                    {
                        string[] ids = host.RuntimeConnections.Where(c => c.IsActive || c.IsConnecting).Select(c => c.ConnectionId).ToArray();
                        if (ids.Length > 0) command = "kill " + string.Join(" ", ids);
                    }
                    else
                    {
                        var payload = host.Payloads.Payloads.FirstOrDefault(p => p.Phase != PayloadPhase.Resolved && p.NextNode == p.FinalNode);
                        var busy = host.Nodes.OrderByDescending(n => n.RequestedLoad / n.LoadCapacity).First();
                        command = payload != null ? "intercept @ " + payload.NextNode.NodeId :
                            busy.RequestedLoad > busy.LoadCapacity && host.OverclockCooldownRemaining <= 0 ? "overclock " + busy.View.NodeId : null;
                    }
                    if (command != null) { result.commands++; typeof(Terminal).GetMethod("HandleSubmit", Private).Invoke(terminal, new object[] { command }); }
                }
                if (now <= 180) result.work180 = host.CreditedWork;
                if (now <= 270) result.work270 = host.CreditedWork;
            }
            result.work360 = host.CreditedWork; result.final = host.Shift; result.time = host.SimulationTimeSeconds;
            result.ending = host.Ending.ToString(); result.coreBreachTime = host.Integrity.Breach?.time ?? -1;
            result.credits = host.Recorder.CurrentLog.events.Where(e => e.type == "shift_work_credit").Select(e => new Credit
            { connectionId = e.connectionId, operationIndex = e.operationIndex, timeSeconds = e.timeSeconds,
                completedWork = e.completedWork, creditedWork = e.creditedWork }).ToArray();
            string signature = string.Join("\n", host.Recorder.CurrentLog.events.Select(JsonUtility.ToJson)) +
                string.Join("\n", host.Recorder.CurrentLog.snapshots.Select(JsonUtility.ToJson));
            using (var hash = SHA256.Create()) result.signature = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(signature))).Replace("-", "");
            host.Recorder.EndRun(); typeof(ConnectionsDuelDirector).GetMethod("OnDisable", Private).Invoke(duel, null);
            Debug.Log($"WORK measure {result.scene}/{seed}/{policy}: {result.final.credited:0.##} at {result.time:0.##}, {result.ending}");
            return result;
        }
    }
}
