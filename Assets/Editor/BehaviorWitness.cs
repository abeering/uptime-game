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
    public static class BehaviorWitness
    {
        [MenuItem("Tools/Connections/Run Behavior Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && director != null && recorder != null, "scene bindings");
            bool oldAuto = director.AutoGenerationEnabled;
            bool oldRecording = recorder.RecordingEnabled;
            NodeResources db = host.Nodes.Single(n => n.View.NodeId == "DB");
            FieldInfo baseline = typeof(NodeResources).GetField("baselineLoad",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float oldBaseline = (float)baseline.GetValue(db);
            try
            {
                director.AutoGenerationEnabled = false;
                recorder.RecordingEnabled = true;
                host.SetRunMode(ConnectionRunMode.Generated);
                recorder.BeginRun();
                NodeView[] route = Route(host);

                var across = Behavior("across", 0f, 4f, 0f, 3f);
                var secondOnly = Behavior("second-only", 0f, 2f, 0f, 5f);
                var plan = Plan("bw1", route, new[]
                {
                    Stage(OperationKind.Read, 5f, 2f),
                    Stage(OperationKind.Write, 40f, 2f, secondOnly),
                    Stage(OperationKind.Read, 100f, 2f)
                }, across);
                director.QueueSpawnPlan(plan);
                host.StepSimulation(1f);
                LogicalConnectionState state = host.FindConnection("bw1");
                Check(state.IsActive && state.CurrentOperationIndex == 1 && state.IsTransitioning &&
                    state.Definition.LoadAt(0) == 2f && state.Definition.LoadAt(1) == 2f &&
                    state.Behaviors.History.Count == 1 && state.Behaviors.ActiveModifiers.Count == 1,
                    "connection modifier changes first operation before allocation and survives transition");
                Near(state.Behaviors.History[0].TimeSeconds, 0f, "connection modifier starts at step zero");
                Near(db.RequestedLoad, 0f, "completed stage releases LOAD before next stage");
                host.StepSimulation(1f);
                Check(state.CurrentOperationIndex == 1 && !state.IsTransitioning &&
                    state.Behaviors.ActiveModifiers.Count == 2 &&
                    state.Behaviors.History[1].SourceId == "second-only" &&
                    state.Behaviors.History[1].Scope == BehaviorScope.Operation &&
                    state.Behaviors.History[1].OperationIndex == 1 &&
                    state.BaseLoad == 2f && state.DesiredLoad == 10f,
                    "connection +3 and second-operation +5 compose to 10 LOAD");
                Near(db.RequestedLoad, 10f, "combined demand published in same step");
                Near(state.AllocatedLoad, 10f, "combined demand allocated in same step");
                Near(state.CompletedWork, 10f, "WORK advances from allocated LOAD");
                recorder.CaptureSnapshot(host.SimulationTimeSeconds);
                ConnectionSnapshot snapshot = recorder.CurrentLog.snapshots.Last().connections
                    .Single(c => c.id == "bw1");
                Check(snapshot.baseLoad == 2f && snapshot.desiredLoad == 10f &&
                    snapshot.baseWork == 40f && snapshot.totalWork == 40f,
                    "snapshot distinguishes authored and effective requirements");
                host.StepSimulation(1f);
                host.StepSimulation(1f);
                Check(state.Behaviors.ActiveModifiers.Count == 1 &&
                    state.Behaviors.History[2].Action == ModifierChangeKind.Removed &&
                    state.Behaviors.History[2].SourceId == "second-only" &&
                    state.Behaviors.History[2].ModifierId == state.Behaviors.History[1].ModifierId &&
                    state.DesiredLoad == 5f,
                    "operation modifier removal restores connection-only demand");
                Near(state.AllocatedLoad, 5f, "removed operation modifier affects allocation immediately");
                host.StepSimulation(1f);
                Check(state.Behaviors.ActiveModifiers.Count == 0 && state.DesiredLoad == 2f &&
                    state.Behaviors.History[3].SourceId == "across" &&
                    state.Behaviors.History[3].Action == ModifierChangeKind.Removed,
                    "connection timer persists across stages and later removes its modifier");
                host.StepSimulation(6.5f);
                Check(state.CurrentOperationIndex == 2 && state.IsTransitioning &&
                    state.Behaviors.ActiveModifiers.Count == 0,
                    "operation modifier does not leak into third stage");
                host.StepSimulation(1f);
                Check(state.CurrentOperationIndex == 2 && state.DesiredLoad == 2f &&
                    state.Definition.LoadAt(1) == 2f && state.Behaviors.History.Count == 4,
                    "authored values and append-only modifier history remain intact");
                Check(recorder.CurrentLog.events.Count(e => e.connectionId == "bw1" &&
                    e.type.StartsWith("modifier_")) == 4 &&
                    recorder.CurrentLog.events.Where(e => e.connectionId == "bw1" &&
                        e.type.StartsWith("modifier_")).Select(e => e.behaviorId)
                        .SequenceEqual(new[] { "across", "second-only", "second-only", "across" }),
                    "run recorder observes ordered attributable modifier history");
                Check(recorder.Save(true), "modifier run saves as JSON");
                ConnectionsRunLog saved = JsonUtility.FromJson<ConnectionsRunLog>(
                    File.ReadAllText(recorder.OutputPath));
                Check(saved.events.Any(e => e.connectionId == "bw1" &&
                    e.type == "modifier_added" && e.behaviorId == "second-only" &&
                    e.modifierScope == "Operation" && e.operationIndex == 1 &&
                    e.loadDelta == 5f) &&
                    saved.snapshots.Any(s => s.connections.Any(c => c.id == "bw1" &&
                        c.baseLoad == 2f && c.desiredLoad == 10f)),
                    "saved recorder file preserves modifier provenance and effective snapshot");

                host.ResetSimulation();
                director.QueueSpawnPlan(Plan("scope-end", route, new[]
                {
                    Stage(OperationKind.Read, 2f, 2f),
                    Stage(OperationKind.Write, 3f, 2f,
                        Behavior("stage-only", 0f, 0f, 0f, 5f)),
                    Stage(OperationKind.Read, 20f, 2f)
                }));
                host.StepSimulation(1f);
                host.StepSimulation(1f);
                LogicalConnectionState scopeEnd = host.FindConnection("scope-end");
                Check(scopeEnd.CurrentOperationIndex == 2 && scopeEnd.IsTransitioning &&
                    scopeEnd.Behaviors.ActiveModifiers.Count == 0 &&
                    scopeEnd.Behaviors.History.Count == 2 &&
                    scopeEnd.Behaviors.History[0].Action == ModifierChangeKind.Added &&
                    scopeEnd.Behaviors.History[1].Action == ModifierChangeKind.Removed,
                    "unfinished operation modifier is removed on natural operation completion");
                host.StepSimulation(1f);
                Near(scopeEnd.DesiredLoad, 2f,
                    "operation modifier does not leak into the next stage");

                host.ResetSimulation();
                var work = Behavior("work-window", 0f, 4f, 10f, 0f);
                director.QueueSpawnPlan(Plan("bw2", route,
                    new[] { Stage(OperationKind.Read, 5f, 2f, work) }));
                for (int i = 0; i < 4; i++) host.StepSimulation(1f);
                LogicalConnectionState workState = host.FindConnection("bw2");
                Check(workState.IsActive && workState.CompletedWork == 8f &&
                    workState.BaseWork == 5f && workState.CurrentTotalWork == 15f,
                    "WORK modifier raises effective requirement without changing base or progress");
                host.StepSimulation(1f);
                Check(workState.Resolution == ConnectionResolution.Completed &&
                    workState.CompletedWork == 8f && workState.Behaviors.History.Count == 2 &&
                    workState.Behaviors.ActiveModifiers.Count == 0,
                    "lowering required WORK below accomplished WORK completes without rewriting progress");
                RunEvent workCompletion = recorder.CurrentLog.events.Last(e =>
                    e.connectionId == "bw2" && e.type == "operation_completed");
                Check(workCompletion.completedWork == 8f && workCompletion.requiredWork == 5f,
                    "completion record preserves accomplished and effective required WORK separately");

                host.ResetSimulation();
                bool connectionWorkRejected = false;
                try
                {
                    director.QueueSpawnPlan(Plan("invalid-work", route,
                        new[] { Stage(OperationKind.Read, 10f, 2f) },
                        Behavior("ambiguous-work", 0f, 0f, 1f, 0f)));
                }
                catch (InvalidOperationException error)
                { connectionWorkRejected = error.Message.Contains("connection-scope WORK"); }
                Check(connectionWorkRejected,
                    "connection-wide WORK is rejected until ownership semantics are designed");
                director.QueueSpawnPlan(Plan("invalid-load", route,
                    new[] { Stage(OperationKind.Read, 10f, 2f,
                        Behavior("negative-load", 0f, 0f, 0f, -3f)) }));
                bool invalidEffectiveRejected = false;
                try { host.StepSimulation(1f); }
                catch (InvalidOperationException error)
                { invalidEffectiveRejected = error.Message.Contains("Effective WORK"); }
                Check(invalidEffectiveRejected,
                    "invalid effective LOAD is rejected before allocation");

                host.ResetSimulation();
                director.QueueSpawnPlan(Plan("invalid-removal", route,
                    new[] { Stage(OperationKind.Read, 100f, 2f) },
                    Behavior("positive", 0f, 1f, 0f, 3f),
                    Behavior("negative", 0f, 2f, 0f, -4f)));
                host.StepSimulation(1f);
                LogicalConnectionState invalidRemoval = host.FindConnection("invalid-removal");
                bool removalRejected = false;
                try { host.StepSimulation(1f); }
                catch (InvalidOperationException error)
                { removalRejected = error.Message.Contains("Effective WORK"); }
                Check(removalRejected && invalidRemoval.Behaviors.ActiveModifiers.Count == 2 &&
                    invalidRemoval.Behaviors.History.Count == 2 && invalidRemoval.DesiredLoad == 1f,
                    "invalid removal rolls back instead of desynchronizing modifiers and history");

                host.ResetSimulation();
                baseline.SetValue(db, 10f);
                director.QueueSpawnPlan(Plan("bw3", route,
                    new[] { Stage(OperationKind.Read, 100f, 2f) },
                    Behavior("zero-allocation-clock", 1f, 2f, 0f, 3f)));
                host.StepSimulation(1f);
                LogicalConnectionState stalled = host.FindConnection("bw3");
                Check(stalled.AllocatedLoad == 0f && stalled.CompletedWork == 0f &&
                    stalled.Behaviors.History.Count == 0,
                    "zero allocation stalls WORK, not the behavior clock");
                host.StepSimulation(1f);
                Check(stalled.DesiredLoad == 5f && stalled.AllocatedLoad == 0f &&
                    stalled.Behaviors.History.Count == 1 &&
                    stalled.Behaviors.History[0].TimeSeconds == 1f,
                    "time behavior adds LOAD despite zero allocation");
                host.StepSimulation(1f);
                Check(stalled.DesiredLoad == 2f && stalled.CompletedWork == 0f &&
                    stalled.Behaviors.History.Count == 2 &&
                    stalled.Behaviors.History[1].TimeSeconds == 2f,
                    "time behavior removes LOAD despite zero allocation");
                baseline.SetValue(db, oldBaseline);

                host.ResetSimulation();
                var killPlan = Plan("bw4", route, new[]
                {
                    Stage(OperationKind.Read, 2f, 2f),
                    new OperationStage(OperationKind.Write, 100f, 2f, "Write",
                        CompletionEffect.InfectionDelivery,
                        new[] { Behavior("write-only", 0f, 0f, 0f, 3f) })
                }, Behavior("lifetime", 0f, 0f, 0f, 1f));
                director.QueueSpawnPlan(killPlan);
                host.StepSimulation(1f);
                host.StepSimulation(1f);
                LogicalConnectionState killed = host.FindConnection("bw4");
                Check(killed.CurrentOperationIndex == 1 && killed.Behaviors.ActiveModifiers.Count == 2 &&
                    host.KillConnection("bw4") && killed.Behaviors.ActiveModifiers.Count == 0 &&
                    killed.Behaviors.History.Count == 4 && host.Occurrences.Count == 0,
                    "kill stops both behavior scopes and unfinished completion effect");
                host.StepSimulation(100f);
                Check(killed.Behaviors.History.Count == 4 && host.Occurrences.Count == 0,
                    "killed behaviors never execute again");

                host.ResetSimulation();
                director.QueueSpawnPlan(Plan("bw5", route,
                    new[] { Stage(OperationKind.Read, 100f, 2f) },
                    Behavior("per-instance", 0f, 0f, 0f, 1f)));
                director.QueueSpawnPlan(Plan("bw6", route,
                    new[] { Stage(OperationKind.Read, 100f, 2f) },
                    Behavior("per-instance", 0f, 0f, 0f, 1f)));
                host.StepSimulation(1f);
                LogicalConnectionState first = host.FindConnection("bw5");
                LogicalConnectionState second = host.FindConnection("bw6");
                Check(first != second && first.Behaviors != second.Behaviors &&
                    first.Behaviors.History.Count == 1 && second.Behaviors.History.Count == 1 &&
                    first.Behaviors.History[0].ModifierId == "m1" &&
                    second.Behaviors.History[0].ModifierId == "m1" &&
                    first.AllocatedLoad == 3f && second.AllocatedLoad == 3f,
                    "concurrent instances own independent modifiers and share capacity proportionally");
                host.ResetSimulation();
                Check(host.FindConnection("bw5") == null && host.Occurrences.Count == 0,
                    "reset clears live state and history ownership");
                director.QueueSpawnPlan(Plan("bw5", route,
                    new[] { Stage(OperationKind.Read, 100f, 2f) },
                    Behavior("per-instance", 0f, 0f, 0f, 1f)));
                host.StepSimulation(1f);
                Check(host.FindConnection("bw5").Behaviors.History.Count == 1 &&
                    host.FindConnection("bw5").Behaviors.History[0].ModifierId == "m1",
                    "reset/replay starts with fresh behavior state");
                Debug.Log("BehaviorWitness PASS: expected 2+3+5=10 LOAD; 10 then 5 then 2 " +
                    "requested LOAD, zero-allocation timer changes at 1s/2s, 8 accomplished " +
                    "WORK retained when requirement drops to 5; observed matching allocation, " +
                    "history, kill, and replay.");
            }
            finally
            {
                baseline.SetValue(db, oldBaseline);
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                director.AutoGenerationEnabled = oldAuto;
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static TimedModifierBehaviorDefinition Behavior(string id, float start,
            float stop, float work, float load) =>
            new TimedModifierBehaviorDefinition(id, start, stop, work, load);
        private static OperationStage Stage(OperationKind kind, float work, float load,
            params TimedModifierBehaviorDefinition[] behaviors) =>
            new OperationStage(kind, work, load, kind.ToString(), CompletionEffect.None, behaviors);
        private static ConnectionSpawnPlan Plan(string id, NodeView[] route,
            OperationStage[] stages, params TimedModifierBehaviorDefinition[] behaviors) =>
            new ConnectionSpawnPlan(0f, id, route, "behavior-witness", "Behavior Witness",
                stages, false, behaviors);
        private static NodeView[] Route(SimulationHost host) => new[]
        {
            host.Nodes.Single(n => n.View.NodeId == "EXT1").View,
            host.Nodes.Single(n => n.View.NodeId == "API").View,
            host.Nodes.Single(n => n.View.NodeId == "DB").View
        };
        private static void Near(float actual, float expected, string name) =>
            Check(Mathf.Abs(actual - expected) < 0.001f,
                name + $": expected {expected:0.###}, observed {actual:0.###}");
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("BehaviorWitness failed: " + name);
        }
    }
}
