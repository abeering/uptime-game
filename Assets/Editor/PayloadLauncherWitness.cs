using System;
using System.Linq;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class PayloadLauncherWitness
    {
        [MenuItem("Tools/Connections/Run Payload Launcher Witness")]
        public static void Run()
        {
            CheckScene("Assets/Scenes/Connections.unity");
            CheckScene("Assets/Scenes/ConnectionsAdvanced.unity");
            Debug.Log("PayloadLauncherWitness PASS: both scenes, typed launch, kill/cancel, red one-hop, " +
                "targeted and node-wide intercept, infection, reset/replay, coarse/fine recorder.");
        }

        private static void CheckScene(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<PayloadPresenter>();
            var paths = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
            Check(host != null && traffic != null && recorder != null && presenter != null &&
                paths != null, scene + " services");
            traffic.AutoGenerationEnabled = false;
            traffic.RandomizeSeed = false;
            host.SetRunMode(ConnectionRunMode.Generated);
            host.ResetSimulation();
            recorder.BeginRun();

            ConnectionSpawnPlan killedPlan = traffic.QueueProfile("payload-launcher");
            Check(killedPlan.IsHostile && killedPlan.Route.Last().NodeId == "API" &&
                killedPlan.Operations.Length == 1 &&
                killedPlan.Operations[0].Operation == OperationKind.Execute &&
                killedPlan.Operations[0].EffectOnCompletion == CompletionEffect.PayloadLaunch &&
                killedPlan.Operations[0].LaunchSpec.DestinationNodeId == "DB" &&
                killedPlan.Operations[0].DesiredLoad == 3f &&
                killedPlan.Operations[0].TotalWork == 60f,
                scene + " manual profile and typed operation");
            host.StepSimulation(.02f);
            Check(host.KillConnection(killedPlan.ConnectionId), scene + " kill launcher");
            host.StepSimulation(35f);
            Check(host.Payloads.Payloads.Count == 0 && host.Occurrences.Count == 0 &&
                !recorder.CurrentLog.events.Any(e => e.type == "payload_launched"),
                scene + " kill prevents launch");

            host.ResetSimulation();
            recorder.BeginRun();
            ConnectionSpawnPlan canceledPlan = traffic.QueueProfile("payload-launcher");
            host.StepSimulation(.02f);
            Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _),
                scene + " test Blackout applies at launch origin");
            host.StepSimulation(35f);
            Check(host.FindConnection(canceledPlan.ConnectionId).Resolution ==
                    ConnectionResolution.Canceled &&
                host.Payloads.Payloads.Count == 0 &&
                !recorder.CurrentLog.events.Any(e => e.type == "payload_launched"),
                scene + " route cancellation prevents launch");

            host.ResetSimulation();
            recorder.BeginRun();
            ConnectionSpawnPlan plan = traffic.QueueProfile("payload-launcher");
            PayloadState payload = AwaitLaunch(host);
            Check(payload != null && payload.Route.Select(n => n.NodeId)
                .SequenceEqual(new[] { "API", "DB" }) &&
                payload.Phase == PayloadPhase.Establishing && payload.Hop == 0 &&
                payload.PhaseElapsed == 0f &&
                payload.TravelSeconds.Length == 1 &&
                payload.TravelSeconds[0] >= host.Payloads.FinalHopMinSeconds &&
                payload.TravelSeconds[0] <= host.Payloads.FinalHopMaxSeconds,
                scene + " one-hop final approach begins at operation completion");
            var launch = host.Occurrences.Single(o =>
                o.Type == CompletionEffect.PayloadLaunch);
            Check(launch.ConnectionId == plan.ConnectionId && launch.OperationIndex == 0 &&
                launch.TargetNodeId == "API" && launch.LaunchedPayloadId == payload.Id &&
                launch.LaunchSpec.DestinationNodeId == "DB" &&
                host.FindConnection(plan.ConnectionId).Definition.LaunchAt(0)
                    .DestinationNodeId == "DB",
                scene + " typed plan-to-definition-to-occurrence lineage");
            paths.Bind(host.RuntimeConnections.ToArray());
            paths.RefreshLayouts();
            presenter.RenderNow();
            PathGraphic line = UnityEngine.Object.FindObjectsByType<PathGraphic>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(v => v.name == "Payload " + payload.Id + " hop");
            Check(line.color == host.Payloads.StrikeColor,
                scene + " launcher Payload is red from establishment");
            RunEvent launchEvent = recorder.CurrentLog.events.Single(e =>
                e.type == "payload_launched");
            Check(launchEvent.connectionId == plan.ConnectionId &&
                launchEvent.operationIndex == 0 && launchEvent.payloadId == payload.Id &&
                launchEvent.launchOriginNodeId == "API" &&
                launchEvent.launchDestinationNodeId == "DB" &&
                recorder.CurrentLog.events.Single(e => e.type == "connection_spawned" &&
                    e.connectionId == plan.ConnectionId).definition.operations[0]
                    .launchDestinationNodeId == "DB" &&
                recorder.CurrentLog.events.Single(e => e.type == "payload_created" &&
                    e.payloadId == payload.Id).payloadRoute.SequenceEqual(new[] { "API", "DB" }),
                scene + " recorder causal lineage and private route");
            Check(host.Payloads.TryIntercept(payload.Id, "DB", out _),
                scene + " targeted interceptor arms");
            host.StepSimulation(8f);
            Check(payload.Intercepted && !payload.EffectApplied && host.Infections.Count == 0 &&
                recorder.CurrentLog.events.Count(e => e.type == "payload_launched") == 1,
                scene + " targeted interceptor catches once");

            host.ResetSimulation();
            recorder.BeginRun();
            traffic.QueueProfile("payload-launcher");
            PayloadState broad = AwaitLaunch(host);
            PayloadState companion = host.Payloads.Spawn("DB", PayloadMotionProfile.Variable,
                "API", new[] { 5.5f });
            Check(host.Payloads.TryIntercept(null, "DB", out _),
                scene + " node-wide interceptor arms");
            host.StepSimulation(8f);
            Check(broad.Intercepted && companion.Intercepted &&
                host.Infections.Count == 0 &&
                recorder.CurrentLog.events.Count(e => e.type == "intercept_succeeded" &&
                    e.targetNodeId == "DB") == 2 &&
                recorder.CurrentLog.events.Single(e => e.type == "intercept_expired" &&
                    e.targetNodeId == "DB").interceptCaughtCount == 2,
                scene + " node-wide interceptor catches both Payloads with one slot");

            host.ResetSimulation();
            recorder.BeginRun();
            traffic.QueueProfile("payload-launcher");
            PayloadState missed = AwaitLaunch(host);
            host.StepSimulation(8f);
            Check(missed.EffectApplied && !missed.Intercepted &&
                host.Infections.Count(i => i.Target.View.NodeId == "DB" &&
                    i.SourceConnectionId == missed.Id) == 1 &&
                host.Infections.All(i => i.Target.View.NodeId != "API") &&
                !host.Payloads.TryIntercept(missed.Id, "DB", out _),
                scene + " missed Payload infects DB only and late defense fails");

            host.ResetSimulation();
            recorder.BeginRun();
            traffic.QueueProfile("payload-launcher");
            host.StepSimulation(36f);
            RunEvent coarse = recorder.CurrentLog.events.Single(e => e.type == "payload_launched");
            RunEvent coarseCreated = recorder.CurrentLog.events.Single(e => e.type == "payload_created");
            host.ResetSimulation();
            recorder.BeginRun();
            traffic.QueueProfile("payload-launcher");
            for (int i = 0; i < 1800; i++) host.StepSimulation(.02f);
            RunEvent fine = recorder.CurrentLog.events.Single(e => e.type == "payload_launched");
            RunEvent fineCreated = recorder.CurrentLog.events.Single(e => e.type == "payload_created");
            Check(coarse.payloadId == fine.payloadId &&
                Mathf.Abs(coarse.timeSeconds - fine.timeSeconds) < .03f &&
                coarseCreated.payloadHopTravelSeconds.SequenceEqual(
                    fineCreated.payloadHopTravelSeconds) &&
                Mathf.Abs(coarse.timeSeconds - coarseCreated.timeSeconds) < .001f &&
                Mathf.Abs(fine.timeSeconds - fineCreated.timeSeconds) < .001f,
                scene + " coarse/fine seeded launch timing and replay");
        }

        private static PayloadState AwaitLaunch(SimulationHost host)
        {
            for (int i = 0; i < 2500; i++)
            {
                host.StepSimulation(.02f);
                if (host.Payloads.Payloads.Count > 0)
                    return host.Payloads.Payloads.Single();
            }
            throw new InvalidOperationException("PayloadLauncherWitness: launch timed out");
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("PayloadLauncherWitness: " + message); }
    }
}
