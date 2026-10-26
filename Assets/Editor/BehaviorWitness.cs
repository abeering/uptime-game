using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
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
            FieldInfo travelField = typeof(SimulationHost).GetField(
                "connectionTravelSecondsPerLink", BindingFlags.Instance | BindingFlags.NonPublic);
            float originalTravel = (float)travelField.GetValue(host);
            travelField.SetValue(host, 0.000001f); // LifecycleWitness covers route travel.
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
                Check(workState.IsActive &&
                    workState.BaseWork == 5f && workState.CurrentTotalWork == 15f,
                    "WORK modifier raises effective requirement without changing base or progress");
                Near(workState.CompletedWork, 8f, "WORK accomplished before requirement drops");
                host.StepSimulation(1f);
                Check(workState.Resolution == ConnectionResolution.Completed &&
                    workState.Behaviors.History.Count == 2 &&
                    workState.Behaviors.ActiveModifiers.Count == 0,
                    "lowering required WORK below accomplished WORK completes without rewriting progress");
                RunEvent workCompletion = recorder.CurrentLog.events.Last(e =>
                    e.connectionId == "bw2" && e.type == "operation_completed");
                Check(Approx(workCompletion.completedWork, 8f) &&
                    workCompletion.requiredWork == 5f,
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
                    Approx(stalled.Behaviors.History[0].TimeSeconds, 1f),
                    "time behavior adds LOAD despite zero allocation");
                host.StepSimulation(1f);
                Check(stalled.DesiredLoad == 2f && stalled.CompletedWork == 0f &&
                    stalled.Behaviors.History.Count == 2 &&
                    Approx(stalled.Behaviors.History[1].TimeSeconds, 2f),
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
                RunKeywords(host, director, recorder, db, route, baseline, oldBaseline);
                RunWorkRate(host, director, recorder, db, route, baseline, oldBaseline);
                Debug.Log("BehaviorWitness PASS: expected 2+3+5=10 LOAD; 10 then 5 then 2 " +
                    "requested LOAD, zero-allocation timer changes at 1s/2s, 8 accomplished " +
                    "WORK retained when requirement drops to 5; observed matching allocation, " +
                    "history, kill, replay, OVERLOAD, and SPIKING.");
            }
            finally
            {
                baseline.SetValue(db, oldBaseline);
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                director.AutoGenerationEnabled = oldAuto;
                travelField.SetValue(host, originalTravel);
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static void RunKeywords(SimulationHost host, ConnectionTrafficDirector director,
            ConnectionsRunRecorder recorder, NodeResources db, NodeView[] route,
            FieldInfo baseline, float oldBaseline)
        {
            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("kw-overload", route, new[]
            {
                new OperationStage(OperationKind.Write, 200f, 2f, "Write",
                    CompletionEffect.InfectionDelivery, new[]
                    { TimedModifierBehaviorDefinition.Overload("op-overload", 0f, 10f) })
            }, TimedModifierBehaviorDefinition.Overload("connection-overload", 2f, 3f)));
            host.StepSimulation(1f);
            LogicalConnectionState overloaded = host.FindConnection("kw-overload");
            Check(overloaded.BaseLoad == 2f && overloaded.DesiredLoad == 12f &&
                overloaded.AllocatedLoad == 10f && Approx(overloaded.CompletedWork, 10f) &&
                overloaded.Behaviors.History.Count == 1 &&
                Approx(overloaded.Behaviors.History[0].TimeSeconds, 0f),
                "zero-delay OVERLOAD applies before first allocation through the normal allocator");
            host.StepSimulation(1f);
            Check(overloaded.Behaviors.History.Count == 1 && overloaded.DesiredLoad == 12f,
                "OVERLOAD activates only once before its delayed companion");
            host.StepSimulation(1f);
            Check(overloaded.DesiredLoad == 15f && overloaded.AllocatedLoad == 10f &&
                Approx(overloaded.CompletedWork, 30f) &&
                overloaded.Behaviors.History.Count == 2 &&
                Approx(overloaded.Behaviors.History[1].TimeSeconds, 2f) &&
                overloaded.Behaviors.History[1].Scope == BehaviorScope.Connection,
                "delayed connection OVERLOAD combines with operation OVERLOAD at exact boundary");
            Check(host.KillConnection("kw-overload") &&
                overloaded.Behaviors.ActiveModifiers.Count == 0 &&
                overloaded.Behaviors.History.Count == 4 && host.Occurrences.Count == 0,
                "kill removes both sustained modifiers and prevents unfinished delivery");

            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("kw-fraction", route, new[]
            {
                Stage(OperationKind.Write, 100f, 2f,
                    TimedModifierBehaviorDefinition.Spiking("fraction-spike", 0.1f, 0.1f, 5f))
            }, TimedModifierBehaviorDefinition.Overload("fraction-overload", 0.1f, 3f)));
            for (int i = 0; i < 5; i++) host.StepSimulation(0.02f);
            LogicalConnectionState fractional = host.FindConnection("kw-fraction");
            Check(fractional.DesiredLoad == 2f && fractional.Behaviors.History.Count == 0,
                "fractional delay stays normal before its exact boundary");
            host.StepSimulation(0.02f);
            Check(fractional.DesiredLoad == 10f && fractional.Behaviors.History.Count == 2 &&
                fractional.Behaviors.History[0].SourceId == "fraction-overload" &&
                fractional.Behaviors.History[1].SourceId == "fraction-spike",
                "fractional OVERLOAD and SPIKING boundaries apply before allocation");
            for (int i = 0; i < 4; i++) host.StepSimulation(0.02f);
            host.StepSimulation(0.02f);
            Check(fractional.DesiredLoad == 5f && fractional.Behaviors.History.Count == 3,
                "fractional SPIKING recovery occurs at its exact boundary");

            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("kw-spike", route, new[]
            {
                Stage(OperationKind.Read, 2f, 2f),
                Stage(OperationKind.Write, 300f, 2f,
                    TimedModifierBehaviorDefinition.Spiking("op-spike", 2f, 1f, 10f))
            }));
            host.StepSimulation(1f);
            LogicalConnectionState spiking = host.FindConnection("kw-spike");
            Check(spiking.CurrentOperationIndex == 1 && spiking.IsTransitioning &&
                spiking.Behaviors.History.Count == 0,
                "operation SPIKING does not begin during preceding READ");
            float[] expectedLoad = { 2f, 2f, 12f, 2f, 2f, 12f, 2f };
            float[] expectedWork = { 2f, 4f, 14f, 16f, 18f, 28f, 30f };
            for (int i = 0; i < expectedLoad.Length; i++)
            {
                host.StepSimulation(1f);
                Near(spiking.DesiredLoad, expectedLoad[i], "SPIKING phase " + i);
                Near(spiking.AllocatedLoad, Mathf.Min(10f, expectedLoad[i]),
                    "SPIKING allocation " + i);
                Near(spiking.CompletedWork, expectedWork[i], "SPIKING allocated WORK " + i);
            }
            Check(spiking.Behaviors.History.Count == 4 &&
                spiking.Behaviors.History[0].Action == ModifierChangeKind.Added &&
                Approx(spiking.Behaviors.History[0].TimeSeconds, 3f) &&
                spiking.Behaviors.History[1].Action == ModifierChangeKind.Removed &&
                Approx(spiking.Behaviors.History[1].TimeSeconds, 4f) &&
                spiking.Behaviors.History[2].ModifierId !=
                    spiking.Behaviors.History[0].ModifierId &&
                Approx(spiking.Behaviors.History[2].TimeSeconds, 6f) &&
                Approx(spiking.Behaviors.History[3].TimeSeconds, 7f) &&
                spiking.Behaviors.ActiveModifiers.Count == 0,
                "SPIKING repeats with distinct modifier IDs and exact boundary history");
            RunEvent[] spikeEvents = recorder.CurrentLog.events.Where(e =>
                e.connectionId == "kw-spike" && e.type.StartsWith("modifier_")).ToArray();
            Check(spikeEvents.Length == 4 &&
                spikeEvents.All(e => e.behaviorId == "op-spike" &&
                    e.modifierScope == "Operation" && e.operationIndex == 1) &&
                spikeEvents[0].modifierId == spikeEvents[1].modifierId &&
                spikeEvents[2].modifierId == spikeEvents[3].modifierId &&
                spikeEvents[0].modifierId != spikeEvents[2].modifierId,
                "recorder retains ordered operation SPIKING provenance");

            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("kw-spike-end", route, new[]
            {
                Stage(OperationKind.Read, 2f, 2f),
                Stage(OperationKind.Write, 11f, 2f,
                    TimedModifierBehaviorDefinition.Spiking("short-spike", 1f, 2f, 10f)),
                Stage(OperationKind.Read, 100f, 2f)
            }));
            host.StepSimulation(1f);
            host.StepSimulation(1f);
            host.StepSimulation(1f);
            LogicalConnectionState spikeEnd = host.FindConnection("kw-spike-end");
            Check(spikeEnd.CurrentOperationIndex == 2 && spikeEnd.IsTransitioning &&
                spikeEnd.Behaviors.ActiveModifiers.Count == 0 &&
                spikeEnd.Behaviors.History.Count == 2 &&
                spikeEnd.Behaviors.History[0].SourceId == "short-spike" &&
                spikeEnd.Behaviors.History[1].Action == ModifierChangeKind.Removed,
                "operation SPIKING removes its elevated modifier on normal stage completion");
            host.StepSimulation(1f);
            Near(spikeEnd.DesiredLoad, 2f, "completed SPIKING does not leak into next stage");

            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("kw-across", route, new[]
            {
                Stage(OperationKind.Read, 2f, 2f),
                Stage(OperationKind.Write, 100f, 2f,
                    TimedModifierBehaviorDefinition.Overload("op-load", 0f, 10f)),
                Stage(OperationKind.Read, 100f, 2f)
            }, TimedModifierBehaviorDefinition.Spiking("connection-spike", 2f, 2f, 3f)));
            host.StepSimulation(1f);
            host.StepSimulation(1f);
            LogicalConnectionState across = host.FindConnection("kw-across");
            Check(across.CurrentOperationIndex == 1 && across.DesiredLoad == 12f &&
                across.Behaviors.History.Count == 1,
                "operation OVERLOAD starts on following step while connection timer persists");
            host.StepSimulation(1f);
            Check(across.DesiredLoad == 15f && across.Behaviors.ActiveModifiers.Count == 2 &&
                across.Behaviors.History[1].Scope == BehaviorScope.Connection,
                "connection SPIKING and operation OVERLOAD compose to 15 requested LOAD");
            host.StepSimulation(1f);
            Check(across.DesiredLoad == 15f && across.Behaviors.History.Count == 2,
                "connection SPIKING remains elevated across its full interval");
            host.StepSimulation(1f);
            Check(across.DesiredLoad == 12f && across.Behaviors.History.Count == 3,
                "connection SPIKING recovery removes only its own modifier");
            host.StepSimulation(10f);
            Check(across.CurrentOperationIndex == 2 && across.IsTransitioning &&
                across.Behaviors.ActiveModifiers.Count == 0 &&
                across.Behaviors.History.Last().SourceId == "op-load" &&
                across.Behaviors.History.Last().Action == ModifierChangeKind.Removed,
                "operation OVERLOAD is removed on completion without leaking to the third stage");
            host.StepSimulation(1f);
            Check(across.DesiredLoad == 5f && across.Behaviors.History.Count == 9 &&
                across.Behaviors.ActiveModifiers.Count == 1 &&
                across.Behaviors.History.Last().SourceId == "connection-spike",
                "connection SPIKING catches up across a transition rather than restarting");

            host.ResetSimulation();
            baseline.SetValue(db, 10f);
            director.QueueSpawnPlan(Plan("kw-stalled", route, new[]
            {
                Stage(OperationKind.Write, 100f, 2f,
                    TimedModifierBehaviorDefinition.Spiking("stalled-spike", 1f, 1f, 10f))
            }));
            host.StepSimulation(1f);
            host.StepSimulation(1f);
            LogicalConnectionState stalled = host.FindConnection("kw-stalled");
            Check(stalled.DesiredLoad == 12f && stalled.AllocatedLoad == 0f &&
                stalled.CompletedWork == 0f && stalled.Behaviors.History.Count == 1,
                "zero allocated LOAD does not delay a SPIKING boundary");
            host.StepSimulation(1f);
            Check(stalled.DesiredLoad == 2f && stalled.CompletedWork == 0f &&
                stalled.Behaviors.History.Count == 2,
                "SPIKING recovers under zero allocation");
            director.QueueSpawnPlan(Plan("kw-stalled-overload", route, new[]
            {
                Stage(OperationKind.Write, 100f, 2f,
                    TimedModifierBehaviorDefinition.Overload("stalled-overload", 1f, 3f))
            }));
            host.StepSimulation(1f);
            host.StepSimulation(1f);
            LogicalConnectionState stalledOverload = host.FindConnection("kw-stalled-overload");
            Check(stalledOverload.DesiredLoad == 5f &&
                stalledOverload.AllocatedLoad == 0f &&
                stalledOverload.CompletedWork == 0f &&
                stalledOverload.Behaviors.History.Count == 1,
                "delayed OVERLOAD activates with zero allocated LOAD");
            baseline.SetValue(db, oldBaseline);

            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("kw-wide", route, new[]
            {
                Stage(OperationKind.Write, 300f, 2f,
                    TimedModifierBehaviorDefinition.Spiking("wide-spike", 1f, 1f, 10f))
            }));
            host.StepSimulation(5f);
            LogicalConnectionState wide = host.FindConnection("kw-wide");
            Near(wide.CompletedWork, 10f, "wide step uses its starting normal phase");
            host.StepSimulation(1f);
            Check(wide.Behaviors.History.Count == 5 &&
                wide.Behaviors.ActiveModifiers.Count == 1 &&
                wide.DesiredLoad == 12f &&
                wide.Behaviors.History.All(e => Approx(e.TimeSeconds, 5f)) &&
                wide.Behaviors.History.Select(e => e.ModifierId).Distinct().Count() == 3,
                "wide step reconciles every crossed boundary in order at next step start");
            Near(wide.CompletedWork, 20f,
                "wide step's 10 WORK plus following allocated 10 WORK");

            host.ResetSimulation();
            var shared = TimedModifierBehaviorDefinition.Spiking("shared-spike", 1f, 1f, 10f);
            director.QueueSpawnPlan(Plan("kw-twin-a", route,
                new[] { Stage(OperationKind.Read, 100f, 2f, shared) }));
            director.QueueSpawnPlan(Plan("kw-twin-b", route,
                new[] { Stage(OperationKind.Read, 100f, 2f, shared) }));
            host.StepSimulation(1f);
            host.StepSimulation(1f);
            LogicalConnectionState twinA = host.FindConnection("kw-twin-a");
            LogicalConnectionState twinB = host.FindConnection("kw-twin-b");
            Check(twinA.Behaviors != twinB.Behaviors &&
                twinA.Behaviors.History.Count == 1 && twinB.Behaviors.History.Count == 1 &&
                twinA.Behaviors.History[0].ModifierId == "m1" &&
                twinB.Behaviors.History[0].ModifierId == "m1" &&
                twinA.AllocatedLoad == 5f && twinB.AllocatedLoad == 5f,
                "shared SPIKING definition creates independent instances with proportional allocation");
            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("kw-twin-a", route,
                new[] { Stage(OperationKind.Read, 100f, 2f, shared) }));
            host.StepSimulation(1f);
            host.StepSimulation(1f);
            Check(host.FindConnection("kw-twin-a").Behaviors.History.Count == 1 &&
                host.FindConnection("kw-twin-a").Behaviors.History[0].ModifierId == "m1",
                "SPIKING reset/replay starts with fresh timing and modifier identity");

            host.ResetSimulation();
            ConnectionSpawnPlan threat = director.QueueProfile("spiking-threat");
            ConnectionSpawnPlan benign = director.QueueProfile("spiking-benign");
            ConnectionSpawnPlan overload = director.QueueProfile("overload-demo");
            Check(threat.IsHostile && !benign.IsHostile && !overload.IsHostile &&
                threat.Operations[0].Behaviors[0].Kind == ConnectionBehaviorKind.Spiking &&
                benign.Operations[0].Behaviors[0].Kind == ConnectionBehaviorKind.Spiking &&
                overload.Operations[0].Behaviors[0].Kind == ConnectionBehaviorKind.Overload,
                "manual profiles keep behavior independent of hostility and completion effect");
            ProfileConfiguration recordedBenign = recorder.CurrentLog.run.traffic.profiles
                .Single(p => p.id == "spiking-benign");
            Check(recordedBenign.operations[0].behaviors[0].kind == "Spiking" &&
                recordedBenign.operations[0].behaviors[0].normalIntervalSeconds == 5f &&
                recordedBenign.operations[0].behaviors[0].elevatedIntervalSeconds == 2f,
                "recorder metadata retains keyword kind and interval tuning");
            for (int i = 0; i < 250 && (host.FindConnection(threat.ConnectionId)?.Resolution == null ||
                host.FindConnection(benign.ConnectionId)?.Resolution == null); i++)
                host.StepSimulation(1f);
            Check(host.FindConnection(threat.ConnectionId)?.Resolution == ConnectionResolution.Completed &&
                host.FindConnection(benign.ConnectionId)?.Resolution == ConnectionResolution.Completed &&
                host.Occurrences.Count == 1 &&
                host.Occurrences[0].ConnectionId == threat.ConnectionId &&
                host.Occurrences[0].OperationIndex == 0 &&
                host.FindConnection(benign.ConnectionId).Behaviors.History.Count > 2,
                "benign SPIKING has no delivery; hostile SPIKING delivers once on normal completion");
        }

        private static void RunWorkRate(SimulationHost host, ConnectionTrafficDirector director,
            ConnectionsRunRecorder recorder, NodeResources db, NodeView[] route,
            FieldInfo baseline, float oldBaseline)
        {
            var half = TimedModifierBehaviorDefinition.Rate("half", 1f, 2f, 0.5f);
            var pause = TimedModifierBehaviorDefinition.Rate("pause", 2f, 4f, 0f);
            var doubleRate = TimedModifierBehaviorDefinition.Rate("double", 5f, 0f, 2f);
            Check(JsonUtility.FromJson<TimedModifierBehaviorDefinition>(
                "{\"id\":\"legacy\",\"loadDelta\":2}").WorkRateMultiplier == 1f &&
                JsonUtility.FromJson<TimedModifierBehaviorDefinition>(
                    "{\"id\":\"explicit-pause\",\"hasWorkRateMultiplier\":true," +
                    "\"workRateMultiplier\":0}").WorkRateMultiplier == 0f &&
                half.Copy().WorkRateMultiplier == 0.5f && pause.Copy().WorkRateMultiplier == 0f,
                "legacy serialized behavior is neutral; explicit zero and copies retain their factor");
            foreach (float invalid in new[] { -0.1f, float.NaN, float.PositiveInfinity })
            {
                bool rejected = false;
                try
                {
                    director.QueueSpawnPlan(Plan("invalid-rate", route,
                        new[] { Stage(OperationKind.Read, 10f, 4f,
                            TimedModifierBehaviorDefinition.Rate("invalid", 0f, 0f, invalid)) }));
                }
                catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "nonfinite or negative WORK rate factor rejected before queue");
            }
            host.ResetSimulation(); recorder.BeginRun();
            director.QueueSpawnPlan(Plan("rate", route,
                new[] { Stage(OperationKind.Read, 18f, 4f, half, pause, doubleRate) }));
            host.StepSimulation(1f);
            LogicalConnectionState state = host.FindConnection("rate");
            Near(state.DesiredLoad, 4f, "1x requested LOAD");
            Near(state.AllocatedLoad, 4f, "1x allocated LOAD");
            Near(state.ActualWorkRate, 4f, "1x productive rate");
            Near(state.CompletedWork, 4f, "1x WORK");
            host.StepSimulation(1f);
            Near(state.AllocatedLoad, 4f, "0.5x allocated LOAD unchanged");
            Near(state.EffectiveWorkRateMultiplier, 0.5f, "0.5x factor");
            Near(state.ActualWorkRate, 2f, "0.5x productive rate");
            Near(state.CompletedWork, 6f, "0.5x WORK");
            var halfInspection = InspectRate(state);
            Check(halfInspection.status.Contains("AT SPEED") &&
                halfInspection.status.Contains("WORK ×0.5") &&
                (halfInspection.eta == "ETA  00:06" ||
                 halfInspection.eta == "ETA  00:07"),
                "positive rate changes verbose status and ETA from authoritative progress");
            host.StepSimulation(1f);
            Near(state.AllocatedLoad, 4f, "0x allocated LOAD unchanged");
            Near(state.ActualWorkRate, 0f, "0x productive rate");
            Near(state.CompletedWork, 6f, "0x WORK stays paused");
            Check(state.IsActive && state.Resolution == null, "paused operation does not complete");
            recorder.CaptureSnapshot(host.SimulationTimeSeconds);
            ConnectionSnapshot paused = recorder.CurrentLog.snapshots.Last().connections
                .Single(c => c.id == "rate");
            Check(paused.desiredLoad == 4f && paused.allocatedLoad == 4f &&
                paused.effectiveWorkRateMultiplier == 0f && paused.actualWorkRate == 0f &&
                Approx(paused.completedWork, 6f),
                "paused snapshot separates consumed LOAD and WORK rate");
            var pausedInspection = InspectRate(state);
            Check(pausedInspection.status.Contains("AT SPEED") &&
                pausedInspection.status.Contains("WORK PAUSED") &&
                !pausedInspection.status.Contains("STALLED") &&
                pausedInspection.eta == "ETA  PAUSED",
                "verbose inspect distinguishes productive pause from LOAD starvation");
            host.StepSimulation(1f);
            Near(state.CompletedWork, 6f, "paused WORK stays fixed while timer advances");
            host.StepSimulation(1f);
            Near(state.EffectiveWorkRateMultiplier, 1f, "positive rate restored");
            Near(state.CompletedWork, 10f, "WORK resumes from accomplished amount");
            host.StepSimulation(1f);
            Near(state.CompletedWork, 18f, "2x completes with 8 WORK from 4 LOAD");
            Check(state.Resolution == ConnectionResolution.Completed &&
                state.Behaviors.ActiveModifiers.Count == 0,
                "rate change completes normally and scope cleanup removes active factor");
            Check(recorder.CurrentLog.events.Any(e => e.connectionId == "rate" &&
                e.type == "modifier_added" && e.behaviorId == "pause" &&
                e.workRateMultiplier == 0f) &&
                recorder.CurrentLog.events.Any(e => e.connectionId == "rate" &&
                    e.type == "modifier_added" && e.behaviorId == "double" &&
                    e.workRateMultiplier == 2f),
                "modifier events identify rate source and factor");
            Check(recorder.Save(true), "rate run saves as JSON");
            ConnectionsRunLog saved = JsonUtility.FromJson<ConnectionsRunLog>(
                File.ReadAllText(recorder.OutputPath));
            Check(saved.snapshots.Any(s => s.connections.Any(c => c.id == "rate" &&
                c.allocatedLoad == 4f && c.effectiveWorkRateMultiplier == 0f &&
                c.actualWorkRate == 0f)) &&
                saved.events.Any(e => e.connectionId == "rate" && e.behaviorId == "pause" &&
                    e.type == "modifier_added" && e.workRateMultiplier == 0f),
                "saved JSON preserves paused snapshot and factor provenance");

            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("scope-rate", route, new[]
            {
                Stage(OperationKind.Read, 2f, 4f,
                    TimedModifierBehaviorDefinition.Rate("operation-pause", 0f, 2f, 0f)),
                Stage(OperationKind.Write, 20f, 4f,
                    TimedModifierBehaviorDefinition.Rate("operation-double", 0f, 0f, 2f))
            }, TimedModifierBehaviorDefinition.Rate("connection-half", 0f, 0f, 0.5f)));
            host.StepSimulation(1f);
            var scope = host.FindConnection("scope-rate");
            Check(scope.Behaviors.ActiveModifiers.Count == 2 &&
                scope.EffectiveWorkRateMultiplier == 0f && scope.CompletedWork == 0f,
                "overlapping connection and operation factors multiply; zero dominates");
            host.StepSimulation(1f);
            host.StepSimulation(1f);
            Check(scope.CurrentOperationIndex == 1 && scope.IsTransitioning &&
                scope.Behaviors.ActiveModifiers.Count == 1 &&
                scope.EffectiveWorkRateMultiplier == 0.5f,
                "operation pause ends at transition; connection factor persists");
            host.StepSimulation(1f);
            Near(scope.EffectiveWorkRateMultiplier, 1f,
                "connection 0.5x and next operation 2x compose to 1x");
            Near(scope.CompletedWork, 4f, "next operation progresses at composed rate");
            Check(host.KillConnection("scope-rate") && scope.Behaviors.ActiveModifiers.Count == 0 &&
                scope.AllocatedLoad == 0f, "kill removes scoped rate factors and releases LOAD");

            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("paused-demand", route,
                new[] { Stage(OperationKind.Read, 100f, 4f) },
                TimedModifierBehaviorDefinition.Rate("pause", 0f, 0f, 0f)));
            director.QueueSpawnPlan(Plan("competitor", route,
                new[] { Stage(OperationKind.Read, 100f, 8f) }));
            host.StepSimulation(1f);
            var demand = host.FindConnection("paused-demand");
            var competitor = host.FindConnection("competitor");
            Near(db.RequestedLoad, 12f, "paused connection still requests LOAD");
            Near(demand.AllocatedLoad, 10f / 3f, "paused connection receives proportional LOAD");
            Near(competitor.AllocatedLoad, 20f / 3f, "peer competes with paused demand");
            Near(demand.CompletedWork, 0f, "paused contender makes no WORK");
            Check(host.KillConnection("paused-demand"), "paused connection can be killed");
            host.StepSimulation(1f);
            Near(competitor.AllocatedLoad, 8f, "kill releases capacity to peer");

            host.ResetSimulation();
            director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "effect-paused", route,
                "behavior-witness", "Paused delivery witness",
                new[] { new OperationStage(OperationKind.Write, 8f, 4f, "Deliver",
                    CompletionEffect.InfectionDelivery) }, true,
                new[] { TimedModifierBehaviorDefinition.Rate("pause", 0f, 0f, 0f) }));
            host.StepSimulation(10f);
            var delivery = host.FindConnection("effect-paused");
            Check(delivery.IsActive && delivery.CompletedWork == 0f &&
                host.Occurrences.Count == 0 && host.Infections.Count == 0,
                "paused operation does not naturally complete or deliver its effect");
            Check(host.KillConnection("effect-paused") && host.Occurrences.Count == 0,
                "killing paused delivery prevents its completion effect");

            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("spiking-paused", route,
                new[] { Stage(OperationKind.Read, 100f, 4f,
                    TimedModifierBehaviorDefinition.Spiking("spike", 1f, 1f, 2f)) },
                TimedModifierBehaviorDefinition.Rate("pause", 0f, 0f, 0f)));
            for (int i = 0; i < 4; i++)
            {
                host.StepSimulation(1f);
                var spiking = host.FindConnection("spiking-paused");
                Near(spiking.DesiredLoad, i % 2 == 0 ? 4f : 6f,
                    "SPIKING toggles LOAD during productive pause " + i);
                Near(spiking.CompletedWork, 0f, "SPIKING quiet and elevated phases remain paused " + i);
            }
            host.ResetSimulation();
            baseline.SetValue(db, 10f);
            director.QueueSpawnPlan(Plan("rate-no-load", route,
                new[] { Stage(OperationKind.Read, 100f, 4f) },
                TimedModifierBehaviorDefinition.Rate("double", 0f, 0f, 2f)));
            host.StepSimulation(1f);
            var noLoad = host.FindConnection("rate-no-load");
            Near(noLoad.AllocatedLoad, 0f, "zero allocation with 2x factor");
            Near(noLoad.ActualWorkRate, 0f, "zero allocation still has zero rate");
            Near(noLoad.CompletedWork, 0f, "zero allocation still has zero WORK");
            baseline.SetValue(db, oldBaseline);
            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("rate", route,
                new[] { Stage(OperationKind.Read, 18f, 4f, half, pause, doubleRate) }));
            host.StepSimulation(1f);
            Check(host.FindConnection("rate").Behaviors.History.Count == 0 &&
                host.FindConnection("rate").EffectiveWorkRateMultiplier == 1f &&
                Approx(host.FindConnection("rate").CompletedWork, 4f),
                "reset/replay starts at neutral rate and repeats first WORK step");
            host.ResetSimulation();
            director.QueueSpawnPlan(Plan("overflow-rate", route,
                new[] { Stage(OperationKind.Read, 100f, 4f,
                    TimedModifierBehaviorDefinition.Rate("huge", 0f, 0f, float.MaxValue)) }));
            bool overflowRejected = false;
            try { host.StepSimulation(1f); }
            catch (InvalidOperationException error)
            { overflowRejected = error.Message.Contains("Actual WORK rate"); }
            Check(overflowRejected, "finite factor cannot overflow actual productive rate");
            Debug.Log("BehaviorWitness WORK rate: 4 LOAD gives 4, 2, 0, 4, then 8 WORK/s " +
                "at 1x, 0.5x, 0x, restored 1x, and 2x; allocation stays 4.");
        }

        private static (string status, string eta) InspectRate(LogicalConnectionState state)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>(
                "Assets/Prefabs/Connections/InspectOutput.prefab");
            InspectOutput output = UnityEngine.Object.Instantiate(prefab);
            try
            {
                output.Bind(new[] { state }, presentationMode: InspectPresentationMode.Verbose);
                var entry = (InspectEntry)new SerializedObject(output).FindProperty("entries")
                    .GetArrayElementAtIndex(0).objectReferenceValue;
                var fields = new SerializedObject(entry);
                return (((TMP_Text)fields.FindProperty("throughput").objectReferenceValue).text,
                    ((TMP_Text)fields.FindProperty("eta").objectReferenceValue).text);
            }
            finally { UnityEngine.Object.DestroyImmediate(output.gameObject); }
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
        private static bool Approx(float actual, float expected) =>
            Mathf.Abs(actual - expected) < 0.001f;
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("BehaviorWitness failed: " + name);
        }
    }
}
