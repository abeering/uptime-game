using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class OperationLogsWitness
    {
        [MenuItem("Tools/Connections/Run Operation Logs Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            traffic.AutoGenerationEnabled = false;
            traffic.RandomizeSeed = false;
            host.SetRunMode(ConnectionRunMode.Generated);
            typeof(SimulationHost).GetField("connectionTravelSecondsPerLink",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(host, .000001f);
            var db = host.FindNodeResources("DB");
            var api = host.FindNodeResources("API");
            var ext = host.FindNodeResources("EXT1");
            var dbRoute = new[] { ext.View, api.View, db.View };
            var apiRoute = new[] { ext.View, api.View };

            // Fixture A: burst, identical thresholds, final boundary, immutable history.
            Reset(host, recorder);
            Queue(traffic, dbRoute, "single", Stage(OperationKind.Read, 10f, 2f,
                Log(.2f, "first"), Log(.4f, "second"),
                Log(.4f, "third"), Log(.45f, "fourth"), Log(1f, "final")));
            host.StepSimulation(.01f);
            var single = host.FindConnection("single");
            Check(single.EmittedLogs.Count == 0, "planned entries remain hidden");
            host.StepSimulation(1f);
            Check(single.EmittedLogs.Count == 1 && single.EmittedLogs[0].Activity == "first",
                "first WORK boundary");
            Near(single.EmittedLogs[0].TimeSeconds, host.SimulationTimeSeconds, "first timestamp");
            host.StepSimulation(2f);
            Check(single.EmittedLogs.Count == 4 &&
                single.EmittedLogs[1].Activity == "second" &&
                single.EmittedLogs[2].Activity == "third" &&
                single.EmittedLogs[3].Activity == "fourth" &&
                single.EmittedLogs.Select(e => e.Sequence).SequenceEqual(new[] { 1, 2, 3, 4 }),
                "burst and equal-threshold authored ordering");
            Near(single.EmittedLogs[3].TimeSeconds, host.SimulationTimeSeconds, "burst timestamp");
            host.StepSimulation(3f);
            Check(single.IsComplete && single.EmittedLogs.Count == 5 &&
                single.EmittedLogs[4].Activity == "final" &&
                ReferenceEquals(single, host.FindConnection("single")),
                "completion boundary and retained lookup");
            Check(recorder.CurrentLog.events.Where(e => e.type == "connection_log_emitted" &&
                e.connectionId == "single").Select(e => e.logAuthoredIndex)
                .SequenceEqual(new[] { 0, 1, 2, 3, 4 }), "recorder emission order");
            Check(recorder.CurrentLog.events.Any(e => e.type == "plan_queued" &&
                e.connectionId == "single" && e.definition.operations[0].plannedLogs.Length == 5),
                "queued plan carries authored logs");
            host.StepSimulation(5f);
            Check(single.EmittedLogs.Count == 5, "resolved connection cannot emit");

            // Normal completion may be just short of the authored final WORK.
            Reset(host, recorder);
            Queue(traffic, dbRoute, "fractional", Stage(OperationKind.Read, 2.00005f, 1f,
                Log(.99999f, "near-final"), Log(1f, "final-a"),
                Log(1f, "final-b")));
            host.StepSimulation(2f);
            var fractional = host.FindConnection("fractional");
            var boundary = recorder.CurrentLog.events.Single(e =>
                e.type == "operation_completed" && e.connectionId == "fractional");
            Check(fractional.IsComplete && boundary.completedWork < boundary.requiredWork &&
                boundary.requiredWork - boundary.completedWork <= .0001f,
                "fractional operation completes inside existing tolerance");
            Check(fractional.EmittedLogs.Select(e => e.Activity).SequenceEqual(
                    new[] { "near-final", "final-a", "final-b" }) &&
                fractional.EmittedLogs.Select(e => e.AuthoredIndex).SequenceEqual(
                    new[] { 0, 1, 2 }) &&
                fractional.EmittedLogs.All(e => e.TimeSeconds == boundary.timeSeconds),
                "all final milestones emit once in stable order at completion time");
            Check(recorder.CurrentLog.events.Where(e => e.type == "connection_log_emitted" &&
                e.connectionId == "fractional").Select(e => e.logAuthoredIndex)
                .SequenceEqual(new[] { 0, 1, 2 }), "recorder preserves boundary order");
            Debug.Log($"OperationLogsWitness fractional completion: required " +
                $"{boundary.requiredWork:0.#####}, completed {boundary.completedWork:0.#####}; " +
                $"expected/observed final logs 3/{fractional.EmittedLogs.Count}");
            host.StepSimulation(3f);
            Check(fractional.EmittedLogs.Count == 3 &&
                ReferenceEquals(fractional, host.FindConnection("fractional")),
                "resolved fractional operation emits no duplicates");

            Reset(host, recorder);
            Queue(traffic, dbRoute, "reduced-tail", Stage(OperationKind.Read,
                2.00005f, 1f,
                new[] { new TimedModifierBehaviorDefinition("reduce-tail", 0f, 0f,
                    -.00005f, 0f) }, Log(1f, "beyond-effective-work")));
            host.StepSimulation(2f);
            Check(host.FindConnection("reduced-tail").IsComplete &&
                host.FindConnection("reduced-tail").EmittedLogs.Count == 0,
                "completion tolerance cannot pass a reduced effective requirement");
            Debug.Log($"OperationLogsWitness reduced effective WORK: expected/observed " +
                $"unearned logs 0/{host.FindConnection("reduced-tail").EmittedLogs.Count}");

            // Fixture B: the previous operation survives the transition.
            Reset(host, recorder);
            Queue(traffic, dbRoute, "arc", Stage(OperationKind.Read, 4f, 2f,
                Log(.5f, "read"), Log(1f, "read-done")),
                Stage(OperationKind.Write, 4f, 2f, Log(.5f, "write")));
            host.StepSimulation(.01f);
            var arc = host.FindConnection("arc");
            host.StepSimulation(2f);
            Check(arc.CurrentOperationIndex == 1 && arc.EmittedLogs.Count == 2 &&
                arc.EmittedLogs.All(e => e.OperationIndex == 0), "transition retains READ logs");
            host.StepSimulation(1f);
            Check(arc.EmittedLogs.Count == 3 && arc.EmittedLogs[2].OperationIndex == 1 &&
                arc.EmittedLogs[2].Operation == OperationKind.Write, "WRITE starts next step");
            host.StepSimulation(2f);
            Check(arc.IsComplete && arc.EmittedLogs.Count == 3, "arc completion keeps whole stream");

            // Fixture C: kill and cancellation freeze earned history.
            Reset(host, recorder);
            Queue(traffic, dbRoute, "killed", Stage(OperationKind.Read, 20f, 2f,
                Log(.1f, "early"), Log(.9f, "late")));
            host.StepSimulation(.01f); host.StepSimulation(1f);
            var killed = host.FindConnection("killed");
            Check(killed.EmittedLogs.Count == 1 && host.KillConnection("killed"), "early kill");
            host.StepSimulation(20f);
            Check(killed.EmittedLogs.Count == 1 && killed.Resolution == ConnectionResolution.Killed &&
                ReferenceEquals(killed, host.FindConnection("killed")), "kill retention");
            Reset(host, recorder);
            Queue(traffic, dbRoute, "canceled", Stage(OperationKind.Read, 20f, 2f,
                Log(.1f, "early"), Log(.9f, "late")));
            host.StepSimulation(.01f); host.StepSimulation(1f);
            var canceled = host.FindConnection("canceled");
            Check(host.InterruptForIsolation(db) == 1 &&
                canceled.Resolution == ConnectionResolution.Canceled, "route cancellation");
            host.StepSimulation(20f);
            Check(canceled.EmittedLogs.Count == 1 &&
                ReferenceEquals(canceled, host.FindConnection("canceled")), "cancel retention");

            Reset(host, recorder);
            traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "withdrawn", dbRoute,
                "logs-witness", "Logs Witness", new[] {
                    Stage(OperationKind.Read, 20f, 2f,
                        new[] { TimedModifierBehaviorDefinition.Rate("LOADER_ACTIVE", 1f, 0f, 0f) },
                        Log(.1f, "early"), Log(.9f, "late"))
                }, true, loaderAttack: new LoaderAttackPlan {
                    structure = LoaderStructure.Single, pattern = LoaderPattern.Immediate,
                    attackOperationIndex = 0, targetCapacity = 10f,
                    normalLoad = 2f, attackLoad = 8f,
                    onsetSeconds = 1f, durationSeconds = 1f
                }));
            host.StepSimulation(.01f); host.StepSimulation(1f);
            var withdrawn = host.FindConnection("withdrawn");
            Check(withdrawn.EmittedLogs.Count == 1, "early log before withdrawal");
            host.StepSimulation(2f);
            Check(withdrawn.Resolution == ConnectionResolution.Withdrawn &&
                withdrawn.EmittedLogs.Count == 1 &&
                ReferenceEquals(withdrawn, host.FindConnection("withdrawn")),
                "withdrawal retains earned logs");

            // Shared target LOAD delays WORK; a different target remains independent.
            Reset(host, recorder);
            Queue(traffic, dbRoute, "share-a", Stage(OperationKind.Read, 40f, 8f, Log(.25f, "shared")));
            Queue(traffic, dbRoute, "share-b", Stage(OperationKind.Read, 40f, 8f, Log(.25f, "shared")));
            Queue(traffic, apiRoute, "independent", Stage(OperationKind.Read, 8f, 2f,
                Log(.25f, "independent")));
            host.StepSimulation(.01f); host.StepSimulation(1f);
            Check(host.FindConnection("share-a").EmittedLogs.Count == 0 &&
                host.FindConnection("share-b").EmittedLogs.Count == 0 &&
                host.FindConnection("independent").EmittedLogs.Count == 1,
                "contention delays only the shared target");
            host.StepSimulation(1f);
            Check(host.FindConnection("share-a").EmittedLogs.Count == 1 &&
                host.FindConnection("share-b").EmittedLogs.Count == 1,
                "proportional allocation reaches both thresholds");

            var baselineField = typeof(NodeResources).GetField("baselineLoad",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float originalBaseline = (float)baselineField.GetValue(db);
            baselineField.SetValue(db, 2f);
            Reset(host, recorder);
            Queue(traffic, dbRoute, "base-a", Stage(OperationKind.Read, 40f, 8f,
                Log(.25f, "base-a")));
            Queue(traffic, dbRoute, "base-b", Stage(OperationKind.Read, 40f, 8f,
                Log(.25f, "base-b")));
            host.StepSimulation(.01f); host.StepSimulation(1f);
            var baseA = host.FindConnection("base-a");
            var baseB = host.FindConnection("base-b");
            Near(baseA.AllocatedLoad, 4f, "baseline share A allocated LOAD");
            Near(baseB.AllocatedLoad, 4f, "baseline share B allocated LOAD");
            Near(db.AllocatedLoad, 10f, "baseline caps total allocated LOAD");
            Check(baseA.EmittedLogs.Count == 0 && baseB.EmittedLogs.Count == 0,
                "baseline delays both milestones");
            Debug.Log($"OperationLogsWitness baseline: expected 4/4 productive LOAD, " +
                $"observed {baseA.AllocatedLoad:0.###}/{baseB.AllocatedLoad:0.###}");
            Check(host.KillConnection("base-a"), "release shared LOAD");
            host.StepSimulation(1f);
            Near(baseB.AllocatedLoad, 8f, "released LOAD restores peer allocation");
            Check(baseB.EmittedLogs.Count == 1, "restored allocation reaches milestone");
            baselineField.SetValue(db, originalBaseline);

            Reset(host, recorder);
            Queue(traffic, dbRoute, "zero", Stage(OperationKind.Read, 20f, 2f,
                new[] { TimedModifierBehaviorDefinition.Rate("zero-rate", 0f, 0f, 0f) },
                Log(.1f, "never")));
            host.StepSimulation(.01f); host.StepSimulation(10f);
            Check(host.FindConnection("zero").EmittedLogs.Count == 0 &&
                host.FindConnection("zero").CompletedWork == 0f,
                "zero productive WORK emits nothing");

            Reset(host, recorder);
            Queue(traffic, dbRoute, "accelerated", true,
                new OperationStage(OperationKind.Write, 20f, 2f, "witness",
                    CompletionEffect.InfectionDelivery,
                    payload: new InfectionPayload(InfectionKind.Backdoor, 0f, 0f),
                    logs: new[] { Log(.5f, "fast") }));
            host.StepSimulation(.01f);
            host.ApplyCondition(db, new ConditionPayload(ConditionKind.Backdoor, 20f));
            host.StepSimulation(2.5f);
            Check(host.FindConnection("accelerated").EmittedLogs.Count == 1 &&
                host.FindConnection("accelerated").EffectiveWorkRateMultiplier == 2f,
                "BACKDOOR acceleration crosses fixed milestone sooner");

            Reset(host, recorder);
            Queue(traffic, dbRoute, "reduced", Stage(OperationKind.Read, 20f, 2f,
                new[] { new TimedModifierBehaviorDefinition("reduce", 1f, 0f, -15f, 0f) },
                Log(.1f, "earned"), Log(.75f, "unearned")));
            host.StepSimulation(.01f); host.StepSimulation(1f);
            host.StepSimulation(1f); host.StepSimulation(.5f);
            var reduced = host.FindConnection("reduced");
            Check(reduced.IsComplete && reduced.EmittedLogs.Count == 1 &&
                reduced.EmittedLogs[0].Activity == "earned",
                $"reduced requirement does not fabricate logs (complete={reduced.IsComplete}, " +
                $"count={reduced.EmittedLogs.Count}, work={reduced.CompletedWork:0.###}, " +
                $"required={reduced.CurrentTotalWork:0.###})");
            host.StepSimulation(5f);
            Check(reduced.EmittedLogs.Count == 1,
                "resolved reduced-WORK operation emits no unearned late entry");

            Reset(host, recorder);
            Queue(traffic, dbRoute, "empty", Stage(OperationKind.Read, 4f, 2f));
            host.StepSimulation(.01f); host.StepSimulation(3f);
            Check(host.FindConnection("empty").IsComplete &&
                host.FindConnection("empty").EmittedLogs.Count == 0,
                "legacy no-log plan remains unchanged");
            host.ResetSimulation();
            Check(host.FindConnection("empty") == null, "reset clears history");
            Queue(traffic, dbRoute, "replay", Stage(OperationKind.Read, 4f, 2f, Log(.5f, "same")));
            host.StepSimulation(.01f); host.StepSimulation(1f);
            float firstTime = host.FindConnection("replay").EmittedLogs[0].TimeSeconds;
            host.ResetSimulation();
            Queue(traffic, dbRoute, "replay", Stage(OperationKind.Read, 4f, 2f, Log(.5f, "same")));
            host.StepSimulation(.01f); host.StepSimulation(1f);
            Near(host.FindConnection("replay").EmittedLogs[0].TimeSeconds, firstTime,
                "deterministic replay timestamp");
            Check(host.FindConnection("replay").EmittedLogs[0].Sequence == 1,
                "replay starts a fresh sequence");
            Debug.Log("OperationLogsWitness PASS: bursts, arcs, interruptions, contention, " +
                "zero/accelerated WORK, requirement changes, recorder, reset/replay.");
        }

        private static PlannedOperationLog Log(float fraction, string activity) =>
            new PlannedOperationLog(fraction, activity, "OK");
        private static OperationStage Stage(OperationKind kind, float work, float load,
            params PlannedOperationLog[] logs) =>
            new OperationStage(kind, work, load, "witness", logs: logs);
        private static OperationStage Stage(OperationKind kind, float work, float load,
            TimedModifierBehaviorDefinition[] behaviors, params PlannedOperationLog[] logs) =>
            new OperationStage(kind, work, load, "witness", behaviors: behaviors, logs: logs);
        private static void Queue(ConnectionTrafficDirector traffic, NodeView[] route,
            string id, params OperationStage[] operations) =>
            Queue(traffic, route, id, false, operations);
        private static void Queue(ConnectionTrafficDirector traffic, NodeView[] route,
            string id, bool hostile, params OperationStage[] operations) =>
            traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, id, route, "logs-witness",
                "Logs Witness", operations, hostile));
        private static void Reset(SimulationHost host, ConnectionsRunRecorder recorder)
        { host.ResetSimulation(); recorder.BeginRun(); }
        private static void Near(float actual, float expected, string label) =>
            Check(Mathf.Abs(actual - expected) < .001f,
                label + $": expected {expected:0.###}, observed {actual:0.###}");
        private static void Check(bool condition, string label)
        { if (!condition) throw new InvalidOperationException("OperationLogsWitness failed: " + label); }
    }
}
