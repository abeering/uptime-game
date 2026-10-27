using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class WavingWitness
    {
        [MenuItem("Tools/Connections/Run WAVING Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && director != null && recorder != null, "scene bindings");
            bool oldAuto = director.AutoGenerationEnabled;
            bool oldRecording = recorder.RecordingEnabled;
            FieldInfo travel = typeof(SimulationHost).GetField("connectionTravelSecondsPerLink",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float oldTravel = (float)travel.GetValue(host);
            try
            {
                director.AutoGenerationEnabled = false;
                recorder.RecordingEnabled = true;
                travel.SetValue(host, 0.000001f);
                host.SetRunMode(ConnectionRunMode.Generated);
                NodeView[] route = Route(host);
                CheckManualProfiles(host, director, recorder);
                CheckWaveValues(host, director, recorder, route);
                CheckFrameStepIndependence(host, director, recorder, route);
                CheckScopes(host, director, recorder, route);
                CheckComposition(host, director, recorder, route);
                CheckSignedAndLifecycle(host, director, recorder, route);
                CheckInvalid(host, route);
                Debug.Log("WavingWitness PASS: authored commands, exact waveform and onset, " +
                    "step-size replay, scope continuity, additive LOAD/WORK, signed clamp, " +
                    "lifecycle cleanup, validation, and recorder provenance.");
            }
            finally
            {
                director.AutoGenerationEnabled = oldAuto;
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                travel.SetValue(host, oldTravel);
                host.ResetSimulation();
            }
        }

        private static void CheckManualProfiles(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            host.ResetSimulation(); recorder.BeginRun();
            foreach (string id in new[] { "waving-moderate", "waving-strong" })
            {
                var profile = director.Profiles.Single(p => p.id == id);
                Check(!profile.isHostile && profile.ambientWeight == 0f &&
                    profile.operations.Length == 1 &&
                    profile.operations[0].completionEffect == CompletionEffect.None &&
                    Terminal.TryParseSpawn(new[] { "spawn", id }, out string parsed,
                        out ConnectionSpawnOverrides options, out _) &&
                    parsed == id && options == null,
                    id + " is a benign, non-ambient authored-scene command");
            }
            ConnectionSpawnPlan moderate = director.QueueProfile("waving-moderate");
            ConnectionSpawnPlan strong = director.QueueProfile("waving-strong");
            Check(moderate.Operations[0].DesiredLoad == 2f &&
                moderate.Operations[0].TotalWork >= 360f &&
                strong.Operations[0].DesiredLoad == 2f &&
                strong.Operations[0].TotalWork >= 720f &&
                !moderate.IsHostile && !strong.IsHostile &&
                moderate.Operations.All(o => o.EffectOnCompletion == CompletionEffect.None) &&
                strong.Operations.All(o => o.EffectOnCompletion == CompletionEffect.None),
                "manual waves retain fixed base LOAD, long WORK, and no infection");
            var moderateWave = moderate.Operations[0].Behaviors.Single();
            var strongWave = strong.Operations[0].Behaviors.Single();
            Check(moderateWave.Kind == ConnectionBehaviorKind.Waving &&
                moderateWave.MinimumLoadOffset == -1f &&
                moderateWave.MaximumLoadOffset == 1f &&
                moderateWave.WavePeriodSeconds == 20f &&
                moderateWave.StartAfterSeconds == 3f &&
                strongWave.MinimumLoadOffset == 0f &&
                strongWave.MaximumLoadOffset == 8f &&
                strongWave.WavePeriodSeconds == 30f &&
                strongWave.StartAfterSeconds == 3f,
                "copied concrete plans preserve both waveform recipes");
            BehaviorRecord queued = recorder.CurrentLog.events.Single(e =>
                e.type == "plan_queued" && e.connectionId == strong.ConnectionId)
                .definition.operations[0].behaviors.Single();
            Check(queued.kind == "Waving" && queued.minimumLoadOffset == 0f &&
                queued.maximumLoadOffset == 8f && queued.wavePeriodSeconds == 30f &&
                queued.startAfterSeconds == 3f &&
                recorder.CurrentLog.run.traffic.profiles.Single(p =>
                    p.id == "waving-moderate").operations[0].behaviors[0].minimumLoadOffset == -1f,
                "queued plan and run configuration record waveform parameters");
            host.StepSimulation(0.00001f);
            Check(host.FindConnection(moderate.ConnectionId)?.IsActive == true &&
                host.FindConnection(strong.ConnectionId)?.IsActive == true,
                "both authored-scene manual plans become real connections");
            Check(recorder.Save(false), "WAVING run log saves");
            var saved = JsonUtility.FromJson<ConnectionsRunLog>(File.ReadAllText(recorder.OutputPath));
            Check(saved.events.Any(e => e.type == "plan_queued" &&
                e.connectionId == strong.ConnectionId &&
                e.definition.operations[0].behaviors[0].maximumLoadOffset == 8f),
                "serialized recorder JSON preserves waveform bounds");
        }

        private static void CheckWaveValues(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder, NodeView[] route)
        {
            var wave = TimedModifierBehaviorDefinition.Waving("moderate", 3f, -1f, 1f, 20f);
            var state = Start(host, director, recorder, Plan("wave-values", route, 10000f, 2f, wave));
            Near(state.DesiredLoad, 2f, "before WAVING onset");
            Near(SampleAt(host, state, 2f), 2f, "onset delay");
            Near(SampleAt(host, state, 3f), 2f, "zero offset at onset");
            Near(SampleAt(host, state, 5.5f), 2.707107f, "ascending intermediate");
            Near(SampleAt(host, state, 8f), 3f, "positive extremum at quarter cycle");
            float before = state.CompletedWork;
            host.StepSimulation(0.1f);
            Near(state.CompletedWork - before, state.AllocatedLoad * 0.1f,
                "productive WORK follows actual allocated LOAD");
            float previous = state.DesiredLoad;
            for (int i = 0; i < 8; i++)
            {
                host.StepSimulation(0.25f);
                Check(Mathf.Abs(state.DesiredLoad - previous) < 0.1f,
                    "quarter-second samples change smoothly");
                previous = state.DesiredLoad;
            }
            Near(SampleAt(host, state, 13f), 2f, "half-cycle baseline");
            Near(SampleAt(host, state, 18f), 1f, "negative extremum");
            before = state.CompletedWork;
            host.StepSimulation(0.1f);
            Near(state.CompletedWork - before, state.AllocatedLoad * 0.1f,
                "low-wave WORK follows its smaller allocation");
            Check(state.EffectiveWorkRateMultiplier == 1f,
                "WAVING does not pause productive WORK");
            Near(SampleAt(host, state, 23f), 2f, "full-cycle boundary is continuous");
            Near(SampleAt(host, state, 28f), 3f, "wave repeats at the next extremum");
            Check(state.Behaviors.History.Count == 1 &&
                state.Behaviors.History[0].SourceId == "moderate" &&
                state.Behaviors.History[0].Scope == BehaviorScope.Operation &&
                recorder.CurrentLog.events.Count(e => e.connectionId == state.ConnectionId &&
                    e.type.StartsWith("modifier_")) == 1,
                "one activation receipt without per-sample event flooding");

            state = Start(host, director, recorder, Plan("wave-strong-values", route,
                10000f, 2f, TimedModifierBehaviorDefinition.Waving(
                    "strong", 3f, 0f, 8f, 30f)));
            Near(SampleAt(host, state, 3f), 2f,
                "asymmetric wave begins at its zero-offset bound");
            Near(SampleAt(host, state, 10.5f), 6f,
                "asymmetric quarter-cycle intermediate");
            Near(SampleAt(host, state, 18f), 10f,
                "asymmetric positive extremum at half-cycle");
            Near(SampleAt(host, state, 33f), 2f,
                "asymmetric full-cycle boundary returns to zero offset");
        }

        private static void CheckFrameStepIndependence(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder, NodeView[] route)
        {
            float[] observed = new float[2];
            for (int run = 0; run < 2; run++)
            {
                var wave = TimedModifierBehaviorDefinition.Waving("replay", 3f, 0f, 8f, 30f);
                var state = Start(host, director, recorder,
                    Plan("wave-replay", route, 10000f, 2f, wave));
                float step = run == 0 ? 0.1f : 0.25f;
                int count = run == 0 ? 100 : 40;
                for (int i = 0; i < count; i++) host.StepSimulation(step);
                host.StepSimulation(0.0001f);
                observed[run] = state.DesiredLoad;
            }
            Near(observed[0], observed[1],
                "identical simulation time samples the same LOAD across step sizes");
        }

        private static void CheckScopes(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder, NodeView[] route)
        {
            var stages = new[] {
                new OperationStage(OperationKind.Read, 1f, 2f, "First"),
                new OperationStage(OperationKind.Write, 10000f, 2f, "Second") };
            var connection = Start(host, director, recorder,
                new ConnectionSpawnPlan(0f, "wave-connection", route, "wave-witness",
                    "Wave witness", stages, connectionBehaviors: new[] {
                        TimedModifierBehaviorDefinition.Waving("across", 0f, -1f, 1f, 8f) }));
            host.StepSimulation(0.5f);
            Check(connection.CurrentOperationIndex == 1 && connection.IsTransitioning &&
                connection.Behaviors.ActiveModifiers.Count == 1,
                "connection WAVING survives first operation completion");
            host.StepSimulation(0.1f);
            Near(connection.DesiredLoad, 2f + Mathf.Sin(Mathf.PI / 8f),
                "connection WAVING phase continues in second operation");
            Check(connection.Behaviors.History.Count == 1,
                "connection WAVING does not reactivate on transition");

            var operation = Start(host, director, recorder,
                new ConnectionSpawnPlan(0f, "wave-operation", route, "wave-witness",
                    "Wave witness", new[] {
                        new OperationStage(OperationKind.Read, 1f, 2f, "First",
                            behaviors: new[] { TimedModifierBehaviorDefinition.Waving(
                                "first-only", 0f, -1f, 1f, 8f) }),
                        stages[1] }));
            host.StepSimulation(0.5f);
            Check(operation.CurrentOperationIndex == 1 && operation.IsTransitioning &&
                operation.Behaviors.ActiveModifiers.Count == 0 &&
                operation.Behaviors.History.Count == 2 &&
                operation.Behaviors.History[1].Action == ModifierChangeKind.Removed,
                "operation WAVING removes its modifier at stage completion");
            host.StepSimulation(0.1f);
            Near(operation.DesiredLoad, 2f, "operation WAVING does not leak into next stage");
        }

        private static void CheckComposition(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder, NodeView[] route)
        {
            var stage = new OperationStage(OperationKind.Read, 10000f, 2f, "Mixed",
                behaviors: new[] {
                    TimedModifierBehaviorDefinition.Waving("WAVING", 0f, -1f, 1f, 8f),
                    TimedModifierBehaviorDefinition.Overload("OVERLOAD", 0f, 2f),
                    new TimedModifierBehaviorDefinition("fixed", 0f, 0f, 0f, 1f) });
            var plan = new ConnectionSpawnPlan(0f, "wave-mixed", route, "wave-witness",
                "Wave witness", new[] { stage },
                connectionBehaviors: new[] {
                    TimedModifierBehaviorDefinition.Spiking("SPIKING", 1f, 1f, 3f) },
                naturalLoadNoise: true, naturalLoadSeed: 1701);
            var state = Start(host, director, recorder, plan);
            SampleAt(host, state, 1f);
            float expected = 2f + 1f + 2f + 3f + Mathf.Sin(Mathf.PI / 4f) +
                state.NaturalLoadDelta;
            Near(state.DesiredLoad, expected,
                "WAVING + fixed + OVERLOAD + SPIKING + natural noise compose once");
            Near(host.Nodes.Single(n => n.View.NodeId == "DB").ConnectionRequestedLoad,
                state.DesiredLoad, "node publishes actual composed demand");
            recorder.CaptureSnapshot(host.SimulationTimeSeconds);
            Near(recorder.CurrentLog.snapshots.Last().connections.Single(c =>
                c.id == state.ConnectionId).desiredLoad, state.DesiredLoad,
                "snapshot stores composed authoritative LOAD");
        }

        private static void CheckSignedAndLifecycle(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder, NodeView[] route)
        {
            var negative = TimedModifierBehaviorDefinition.Waving("negative", 0f, -3f, 0f, 8f);
            var state = Start(host, director, recorder,
                Plan("wave-negative", route, 10000f, 2f, negative));
            Near(SampleAt(host, state, 4f), 0f,
                "signed trough clamps requested LOAD to zero");
            Check(state.AllocatedLoad == 0f && state.ActualWorkRate == 0f &&
                state.EffectiveWorkRateMultiplier == 1f,
                "zero demand stalls allocation without pausing WORK machinery");
            Near(SampleAt(host, state, 8f), 2f,
                "signed wave recovers after full cycle");
            Check(host.KillConnection(state.ConnectionId) &&
                state.Behaviors.ActiveModifiers.Count == 0 &&
                state.Behaviors.History.Count == 2 &&
                recorder.CurrentLog.events.Count(e => e.connectionId == state.ConnectionId &&
                    e.type.StartsWith("modifier_")) == 2 &&
                host.Nodes.Single(n => n.View.NodeId == "DB").ConnectionRequestedLoad == 0f,
                "kill removes the active wave and releases demand");

            state = Start(host, director, recorder,
                Plan("wave-complete", route, 1f, 2f,
                    TimedModifierBehaviorDefinition.Waving("complete", 0f, -1f, 1f, 8f)));
            host.StepSimulation(0.5f);
            Check(state.Resolution == ConnectionResolution.Completed &&
                state.Behaviors.ActiveModifiers.Count == 0 &&
                state.Behaviors.History.Count == 2,
                "normal completion removes the active wave once");

            state = Start(host, director, recorder,
                Plan("wave-cancel", route, 10000f, 2f,
                    TimedModifierBehaviorDefinition.Waving("cancel", 0f, -1f, 1f, 8f)));
            Check(host.TryInjectInfection("DB", InfectionKind.Blackout, 0f, out _) &&
                state.Resolution == ConnectionResolution.Canceled &&
                state.Behaviors.ActiveModifiers.Count == 0 &&
                state.Behaviors.History.Count == 2,
                "Blackout cancellation removes the active wave once");

            state = Start(host, director, recorder,
                Plan("wave-reset", route, 10000f, 2f,
                    TimedModifierBehaviorDefinition.Waving("reset", 0f, -1f, 1f, 8f)));
            Near(SampleAt(host, state, 2f), 3f, "first run reaches quarter-cycle peak");
            state = Start(host, director, recorder,
                Plan("wave-reset", route, 10000f, 2f,
                    TimedModifierBehaviorDefinition.Waving("reset", 0f, -1f, 1f, 8f)));
            Near(SampleAt(host, state, 2f), 3f,
                "reset replays the waveform and modifier lifetime");
            Check(state.Behaviors.History.Single().ModifierId == "m1",
                "reset starts a fresh modifier identity");
        }

        private static void CheckInvalid(SimulationHost host, NodeView[] route)
        {
            var invalid = new[] {
                TimedModifierBehaviorDefinition.Waving("bad-period", 0f, -1f, 1f, 0f),
                TimedModifierBehaviorDefinition.Waving("bad-bounds", 0f, 1f, 2f, 8f),
                TimedModifierBehaviorDefinition.Waving("bad-nan", 0f, float.NaN, 1f, 8f),
                TimedModifierBehaviorDefinition.Waving("bad-onset", -1f, -1f, 1f, 8f) };
            foreach (TimedModifierBehaviorDefinition behavior in invalid)
            {
                bool rejected = false;
                try { host.ValidatePlan(Plan("invalid-wave", route, 100f, 2f, behavior)); }
                catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "invalid WAVING parameters rejected: " + behavior.Id);
            }
        }

        private static LogicalConnectionState Start(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder,
            ConnectionSpawnPlan plan)
        {
            host.ResetSimulation(); recorder.BeginRun();
            director.QueueSpawnPlan(plan);
            host.StepSimulation(0.00001f);
            LogicalConnectionState state = host.FindConnection(plan.ConnectionId);
            Check(state != null && state.IsActive, "connection established: " + plan.ConnectionId);
            return state;
        }

        private static float SampleAt(SimulationHost host, LogicalConnectionState state,
            float elapsedSeconds)
        {
            float remaining = elapsedSeconds -
                (host.SimulationTimeSeconds - state.EstablishedAtSeconds);
            if (remaining > 0.0002f) host.StepSimulation(remaining);
            host.StepSimulation(0.0001f);
            return state.DesiredLoad;
        }

        private static ConnectionSpawnPlan Plan(string id, NodeView[] route,
            float work, float load, params TimedModifierBehaviorDefinition[] behaviors) =>
            new ConnectionSpawnPlan(0f, id, route, "wave-witness", "Wave witness",
                new[] { new OperationStage(OperationKind.Read, work, load, "Wave witness",
                    behaviors: behaviors) });

        private static NodeView[] Route(SimulationHost host) => new[] {
            host.Nodes.Single(n => n.View.NodeId == "EXT1").View,
            host.Nodes.Single(n => n.View.NodeId == "API").View,
            host.Nodes.Single(n => n.View.NodeId == "DB").View };

        private static void Near(float actual, float expected, string name) =>
            Check(Mathf.Abs(actual - expected) < 0.003f,
                name + $": expected {expected:0.###}, observed {actual:0.###}");

        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("WavingWitness failed: " + name);
        }
    }
}
