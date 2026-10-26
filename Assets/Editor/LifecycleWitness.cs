using System;
using System.Linq;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class LifecycleWitness
    {
        [MenuItem("Tools/Connections/Run Lifecycle Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && director != null && recorder != null, "scene bindings");
            var pathPrefab = AssetDatabase.LoadAssetAtPath<LogicalPathView>(
                "Assets/Prefabs/Connections/LogicalPath.prefab");
            Check(pathPrefab != null, "authored path prefab");
            var pathFields = new SerializedObject(pathPrefab);
            Check(pathFields.FindProperty("path").objectReferenceValue != null &&
                pathFields.FindProperty("terminalMarkerPrefab").objectReferenceValue != null &&
                Math.Abs(pathFields.FindProperty("failureFlashHalfCycleSeconds").floatValue -
                    0.12f) < 0.001f &&
                pathFields.FindProperty("failureColor").colorValue.r > 0.9f,
                "authored path and tunable red failure flash bindings");
            bool oldAuto = director.AutoGenerationEnabled;
            bool oldRecording = recorder.RecordingEnabled;
            try
            {
                director.AutoGenerationEnabled = false;
                recorder.RecordingEnabled = true;
                host.SetRunMode(ConnectionRunMode.Generated);
                recorder.BeginRun();
                var first = Spawn(host, "travel", false);
                Check(first.IsConnecting && first.CompletedWork == 0f &&
                    first.AllocatedLoad == 0f && first.HealthDeadlineSeconds == 0f,
                    "spawn is an inspectable, non-demanding attempt");
                host.StepSimulation(0.6f);
                Check(first.IsConnecting && first.ReachedRouteNodeIndex == 1 &&
                    first.CompletedWork == 0f && first.HealthDeadlineSeconds == 0f,
                    "intermediate arrival precedes service");
                host.StepSimulation(0.6f);
                Check(first.IsActive && first.HasEstablished &&
                    Math.Abs(first.EstablishedAtSeconds - 1.2f) < 0.001f &&
                    first.CompletedWork == 0f && first.HealthDeadlineSeconds > 1.2f,
                    "destination arrival begins service and deadline");
                Near(first.AllocatedLoad, 1f, "arrival publishes LOAD before presentation");
                Check(first.Performance == LoadPerformance.AtSpeed,
                    "healthy arrival does not display a stalled red frame");
                var presenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
                Check(presenter != null, "path presenter for arrival tint");
                LogicalPathView arrivalView = UnityEngine.Object.Instantiate(pathPrefab,
                    presenter.PathLayer, false);
                try
                {
                    Color identity = presenter.IdentityPalette.ColorAt(0);
                    arrivalView.Bind(first, presenter.NetworkArea, presenter.MarkerLayer);
                    arrivalView.SetIdentityColor(identity);
                    arrivalView.SetLayout(new[] { 0f, 0f }, new[] { 8f, 8f });
                    arrivalView.AdvanceVisual(0f);
                    Check(arrivalView.GetComponent<PathGraphic>().color == identity,
                        "first established frame renders identity color, not failure red");
                }
                finally { UnityEngine.Object.DestroyImmediate(arrivalView.gameObject); }
                Check(recorder.CurrentLog.events.Where(e => e.type == "connection_node_arrived" &&
                    e.connectionId == "travel").Select(e => e.targetNodeId)
                    .SequenceEqual(new[] { "API", "DB" }), "arrival order");
                host.StepSimulation(0.2f);
                Check(first.CompletedWork > 0f, "WORK begins after establishment");
                float beforeKill = host.Health.Value;
                Check(host.KillConnection("travel") && first.Resolution == ConnectionResolution.Killed &&
                    first.TerminationPhase == Lifecycle.Active, "active kill outcome");
                Near(host.Health.Value, beforeKill - 10f, "active benign kill penalty");

                host.ResetSimulation(); recorder.BeginRun();
                var simultaneousA = Spawn(host, "arrival-a", false);
                var simultaneousB = Spawn(host, "arrival-b", false);
                host.StepSimulation(1.2f);
                Check(simultaneousA.IsActive && simultaneousB.IsActive &&
                    simultaneousA.CompletedWork == 0f && simultaneousB.CompletedWork == 0f,
                    "same-step arrivals start service without advancing WORK");
                Near(simultaneousA.AllocatedLoad, 1f, "first same-step arrival allocated");
                Near(simultaneousB.AllocatedLoad, 1f, "second same-step arrival allocated");

                host.ResetSimulation(); recorder.BeginRun();
                var connecting = Spawn(host, "connecting-kill", false);
                host.StepSimulation(0.3f);
                Check(host.KillConnection(connecting.ConnectionId) &&
                    connecting.TerminationPhase == Lifecycle.Connecting &&
                    connecting.RouteReveal > 0f && connecting.RouteReveal < 1f,
                    "partial path can be killed");
                Near(host.Health.Value, 90f, "connecting benign kill penalty");
                RunEvent killReceipt = recorder.CurrentLog.events.Single(e =>
                    e.type == "connection_resolved" && e.connectionId == connecting.ConnectionId);
                Check(killReceipt.resolutionReason == "Killed" &&
                    killReceipt.resolutionCause == "Player" &&
                    killReceipt.terminationPhase == "Connecting" &&
                    !killReceipt.established && killReceipt.healthPenaltyAmount == -10f,
                    "connecting kill has one complete player receipt");

                host.ResetSimulation(); recorder.BeginRun();
                Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _),
                    "idle Blackout setup");
                host.StepSimulation(5f);
                Near(host.Health.Value, 100f, "idle Blackout has no direct Health drain");

                host.ResetSimulation(); recorder.BeginRun();
                var benign = Spawn(host, "blocked-benign", false);
                var hostile = Spawn(host, "blocked-hostile", true);
                Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _),
                    "Blackout applies");
                Near(host.Health.Value, 100f, "idle infrastructure causes no Health loss");
                host.StepSimulation(0.6f);
                Check(benign.Resolution == ConnectionResolution.Canceled &&
                    hostile.Resolution == ConnectionResolution.Canceled &&
                    benign.FailureNodeId == "API" && hostile.FailureNodeId == "API" &&
                    benign.TerminationPhase == Lifecycle.Connecting &&
                    benign.TerminationCause == ConnectionTerminationCause.Blackout,
                    "arrival at offline intermediate node cancels both attempts");
                Near(host.Health.Value, 95f, "one benign cancellation, hostile zero");
                Check(!host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _),
                    "duplicate Blackout application is rejected");
                Near(host.Health.Value, 95f, "cancellation penalty is not repeated");
                RunEvent receipt = recorder.CurrentLog.events.Single(e =>
                    e.type == "connection_resolved" && e.connectionId == benign.ConnectionId);
                Check(receipt.resolutionReason == "Canceled" && receipt.resolutionCause == "Blackout" &&
                    receipt.failureNodeId == "API" && receipt.terminationPhase == "Connecting" &&
                    receipt.definition.route.SequenceEqual(new[] { "EXT1", "API", "DB" }) &&
                    !receipt.established && receipt.healthPenaltyAmount == -5f &&
                    receipt.resolvedAtSeconds == 0.6f,
                    "complete internal cancellation receipt");
                host.StepSimulation(5f);
                Near(host.Health.Value, 96.75f, "Health recovers during idle Blackout");

                host.ResetSimulation(); recorder.BeginRun();
                var traversed = Spawn(host, "traversed", false);
                host.StepSimulation(0.7f);
                Check(traversed.IsConnecting && traversed.ReachedRouteNodeIndex == 1 &&
                    host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _) &&
                    traversed.Resolution == ConnectionResolution.Canceled &&
                    traversed.FailureNodeId == "API",
                    "Blackout immediately cancels a Connecting route that passed its node");

                host.ResetSimulation(); recorder.BeginRun();
                Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _),
                    "preexisting Blackout");
                var queued = Spawn(host, "offline-route", false);
                Check(queued.IsConnecting, "offline route is still a recorded attempt");
                host.StepSimulation(0.3f);
                Check(host.TryRemoveInfection("API", InfectionKind.Blackout, out _),
                    "clear before arrival");
                host.StepSimulation(0.9f);
                Check(queued.IsActive && queued.HasEstablished,
                    "clearing before arrival permits service");
                Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _) &&
                    queued.Resolution == ConnectionResolution.Canceled &&
                    queued.TerminationPhase == Lifecycle.Active,
                    "established dependent route cancels immediately");

                host.ResetSimulation(); recorder.BeginRun();
                director.AutoGenerationEnabled = true;
                Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _),
                    "ambient offline-route setup");
                host.StepSimulation(director.InitialGraceSeconds + 0.01f);
                host.StepSimulation(0.01f);
                RunEvent ambient = recorder.CurrentLog.events.First(e =>
                    e.type == "connection_spawned");
                Check(host.FindConnection(ambient.connectionId)?.IsConnecting == true,
                    "ambient plan becomes Connecting despite an offline transit node");
                host.StepSimulation(0.6f);
                Check(host.FindConnection(ambient.connectionId)?.Resolution ==
                    ConnectionResolution.Canceled, "ambient attempt cancels at blocked node");
                director.AutoGenerationEnabled = false;

                host.ResetSimulation(); recorder.BeginRun();
                var large = Spawn(host, "large", false);
                host.StepSimulation(1.5f);
                Check(large.IsActive && Math.Abs(large.EstablishedAtSeconds - 1.2f) < 0.001f &&
                    Math.Abs(large.CompletedWork - 0.3f) < 0.001f,
                    "large step splits travel and service deterministically");
                Check(host.TryInjectInfection("DB", InfectionKind.Spawner, 1f, out _) &&
                    host.TryInjectInfection("DB", InfectionKind.Blackout, 0f, out _),
                    "offline Spawner origin setup");
                host.StepSimulation(1f);
                RunEvent child = recorder.CurrentLog.events.Last(e =>
                    e.type == "plan_queued" && e.source == "spawner");
                host.StepSimulation(0.01f);
                Check(host.FindConnection(child.connectionId)?.Resolution == ConnectionResolution.Canceled &&
                    recorder.CurrentLog.events.Any(e => e.type == "connection_spawned" &&
                        e.connectionId == child.connectionId),
                    "offline-origin child gets attempt and cancellation receipt");
                string[] replay = new string[2];
                for (int i = 0; i < replay.Length; i++)
                {
                    host.ResetSimulation(); recorder.BeginRun();
                    Spawn(host, "replay", false);
                    host.StepSimulation(0.3f);
                    Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _),
                        "replay Blackout injection");
                    host.StepSimulation(0.3f);
                    replay[i] = string.Join("|", recorder.CurrentLog.events.Where(e =>
                        e.type == "connection_node_arrived" ||
                        e.type == "connection_resolved").Select(e =>
                        e.type + ":" + e.timeSeconds.ToString("R") + ":" +
                        e.targetNodeId + ":" + e.resolutionReason));
                }
                Check(replay[0] == replay[1], "reset and replay preserve arrival and receipt timing");
                Debug.Log("LifecycleWitness PASS: travel, Health, Blackout, receipts, large steps, Spawner accounting.");
            }
            finally
            {
                recorder.EndRun();
                director.AutoGenerationEnabled = oldAuto;
                recorder.RecordingEnabled = oldRecording;
                host.ResetSimulation();
            }
        }

        private static LogicalConnectionState Spawn(SimulationHost host, string id, bool hostile)
        {
            NodeView Node(string name) => host.Nodes.Single(n => n.View.NodeId == name).View;
            var route = new[] { Node("EXT1"), Node("API"), Node("DB") };
            var plan = new ConnectionSpawnPlan(host.SimulationTimeSeconds, id, route,
                "witness", "Service", new[] { new OperationStage(OperationKind.Read,
                    100f, 1f, "Read") }, hostile);
            return host.RegisterConnection(LogicalConnectionDefinition.FromPlan(plan));
        }

        private static void Near(float actual, float expected, string label) =>
            Check(Math.Abs(actual - expected) < 0.002f,
                label + ": expected " + expected + ", observed " + actual);
        private static void Check(bool condition, string label)
        {
            if (!condition) throw new Exception("LifecycleWitness: " + label);
        }
    }
}
