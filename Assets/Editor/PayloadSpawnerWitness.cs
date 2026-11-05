using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class PayloadSpawnerWitness
    {
        [MenuItem("Tools/Connections/Run Payload Spawner Witness")]
        public static void Run()
        {
            CheckScene("Assets/Scenes/Connections.unity");
            CheckScene("Assets/Scenes/ConnectionsAdvanced.unity");
            Debug.Log("PayloadSpawnerWitness PASS: both scenes, cadence, destinations, red " +
                "telegraph, interception, delivery, scan/CLEAN, in-flight independence, " +
                "replay, coarse/fine positions, missing neighbors, recorder provenance.");
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
                paths != null,
                scene + " services");
            traffic.AutoGenerationEnabled = false;
            traffic.RandomizeSeed = false;
            host.SetRunMode(ConnectionRunMode.Generated);
            recorder.BeginRun();
            Check(host.DefaultPayloadSpawnerCadence == 20f &&
                Terminal.TryParseInfectionKind("payload-spawner", out InfectionKind parsed) &&
                parsed == InfectionKind.PayloadSpawner &&
                host.TryInjectInfection("api", parsed, 20f, out _),
                scene + " Inspector default and manual infection parser");
            NodeInfection source = host.Infections.Single();
            Check(source.NextSpawnAtSeconds == 20f &&
                source.Payload.RequestedLoad == 0f &&
                source.Payload.CadenceSeconds == 20f,
                scene + " full first cadence and no infection LOAD");
            host.StepSimulation(19.98f);
            Check(host.Payloads.Payloads.Count == 0 &&
                !recorder.CurrentLog.events.Any(e => e.type == "payload_spawner_attempt"),
                scene + " no early launch");
            host.StepSimulation(.02f);
            Check(host.Payloads.Payloads.Count == 1 &&
                Mathf.Abs(host.SimulationTimeSeconds - 20f) < .001f,
                scene + " first due cadence launches once");
            PayloadState first = host.Payloads.Payloads[0];
            Check(first.Route.Length == 2 && first.CurrentNode.NodeId == "API" &&
                traffic.IsInternalNode(first.FinalNode) &&
                host.PhysicalLinks.Any(l => l.View != null &&
                    (l.View.NodeA == first.CurrentNode && l.View.NodeB == first.FinalNode ||
                     l.View.NodeB == first.CurrentNode && l.View.NodeA == first.FinalNode)) &&
                first.Phase == PayloadPhase.Establishing && first.PhaseElapsed == 0f &&
                host.Payloads.PresentationColor(first) == host.Payloads.StrikeColor,
                scene + " adjacent internal one-hop red Payload at launch");
            paths.Bind(host.RuntimeConnections.ToArray());
            paths.RefreshLayouts();
            presenter.RenderNow();
            PathGraphic line = UnityEngine.Object.FindObjectsByType<PathGraphic>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(g => g.name == "Payload " + first.Id + " hop");
            Check(line.color == host.Payloads.StrikeColor,
                scene + " red line at initial establishment");
            RunEvent receipt = recorder.CurrentLog.events.Single(e =>
                e.type == "payload_spawner_attempt");
            Check(receipt.reason == "launched" && receipt.payloadId == first.Id &&
                receipt.launchOriginNodeId == "API" &&
                receipt.launchDestinationNodeId == first.FinalNode.NodeId &&
                receipt.infectionKind == "PayloadSpawner" &&
                receipt.sourceConnectionId == null && receipt.sourceOperationIndex == -1 &&
                receipt.infectionAppliedAtSeconds == 0f &&
                Mathf.Abs(receipt.timeSeconds - 20f) < .001f,
                scene + " launch receipt links debug infection and Payload");
            host.StepSimulation(20f);
            Check(host.Payloads.Payloads.Count == 2 &&
                recorder.CurrentLog.events.Count(e => e.type == "payload_spawner_attempt") == 2,
                scene + " second due cadence launches exactly once");
            host.StepSimulation(20f);
            Check(host.Payloads.Payloads.Count == 3 &&
                recorder.CurrentLog.events.Count(e => e.type == "payload_spawner_attempt") == 3 &&
                host.Payloads.Payloads.Select(p => p.Id).Distinct().Count() == 3,
                scene + " third cadence and independent IDs");

            if (scene.EndsWith("Connections.unity", StringComparison.Ordinal))
            {
                host.ResetSimulation();
                recorder.BeginRun();
                Check(host.TryInjectInfection("DB", InfectionKind.PayloadSpawner, 10f, out _),
                    "duplicate-destination setup");
                host.StepSimulation(28f);
                Check(host.Payloads.Payloads.Count == 2 &&
                    host.Payloads.Payloads.All(p => p.FinalNode.NodeId == "API") &&
                    host.Infections.Count(i => i.Target.View.NodeId == "API" &&
                        i.Payload.Kind == InfectionKind.PhantomLoad) == 1 &&
                    recorder.CurrentLog.events.Any(e => e.type == "infection_rejected" &&
                        e.sourceConnectionId == host.Payloads.Payloads[1].Id &&
                        e.reason == "already_active"),
                    "repeated strikes keep nonstacking Phantom LOAD and record rejection");
            }

            host.ResetSimulation();
            recorder.BeginRun();
            Check(host.TryInjectInfection("API", InfectionKind.PayloadSpawner, 20f, out _),
                scene + " interception setup");
            host.StepSimulation(20f);
            PayloadState intercepted = host.Payloads.Payloads.Single();
            Check(host.Payloads.TryIntercept(intercepted.Id,
                intercepted.FinalNode.NodeId, out _), scene + " targeted intercept arms");
            host.StepSimulation(8f);
            Check(intercepted.Intercepted && !intercepted.EffectApplied &&
                host.Infections.Count == 1 &&
                host.Infections.Single().Payload.Kind == InfectionKind.PayloadSpawner,
                scene + " interception blocks strike but leaves source infection");

            host.ResetSimulation();
            recorder.BeginRun();
            Check(host.TryInjectInfection("API", InfectionKind.PayloadSpawner, 20f, out _),
                scene + " scan and CLEAN setup");
            Check(host.StartScan("API", "scan api", out NodeScanSession scan) ==
                ScanStartResult.Started, scene + " scan starts");
            host.StepSimulation(8f);
            Check(scan.Result == NodeScanResult.Infected && scan.IsComplete,
                scene + " ordinary asynchronous scan discovers infection");
            host.StepSimulation(12f);
            PayloadState inFlight = host.Payloads.Payloads.Single();
            Check(host.StartClean("API", "clean api", out CleanSession clean) ==
                CleanStartResult.Started, scene + " PID-Select starts");
            int pid = clean.Processes.Single(p => p.CpuPercent >= 82).Pid;
            Check(host.SelectCleanPid(pid, out int removed) == CleanSelectionResult.Success &&
                removed == 1 && host.Infections.Count == 0,
                scene + " successful PID removes source immediately");
            host.StepSimulation(8f);
            Check(inFlight.EffectApplied && !inFlight.Intercepted &&
                host.Infections.Count(i => i.Target.View == inFlight.FinalNode &&
                    i.Payload.Kind == InfectionKind.PhantomLoad) == 1 &&
                host.Infections.All(i => i.Target.View.NodeId != "API"),
                scene + " in-flight strike survives source cleaning and delivers Phantom LOAD");
            host.StepSimulation(40f);
            Check(host.Payloads.Payloads.Count == 1 &&
                recorder.CurrentLog.events.Count(e => e.type == "payload_spawner_attempt") == 1,
                scene + " cleaning stops all future launches");

            host.ResetSimulation();
            recorder.BeginRun();
            host.TryInjectInfection("API", InfectionKind.PayloadSpawner, 20f, out _);
            host.StepSimulation(45f);
            string[] coarse = recorder.CurrentLog.events.Where(e =>
                e.type == "payload_spawner_attempt").Select(e =>
                e.timeSeconds.ToString("F2") + ":" + e.launchDestinationNodeId +
                ":" + e.payloadId).ToArray();
            float coarseProgress = host.Payloads.Payloads.Last().Progress;
            float[] coarseTravel = host.Payloads.Payloads.Select(p => p.TravelSeconds[0]).ToArray();
            host.ResetSimulation();
            recorder.BeginRun();
            host.TryInjectInfection("API", InfectionKind.PayloadSpawner, 20f, out _);
            for (int i = 0; i < 2250; i++) host.StepSimulation(.02f);
            string[] fine = recorder.CurrentLog.events.Where(e =>
                e.type == "payload_spawner_attempt").Select(e =>
                e.timeSeconds.ToString("F2") + ":" + e.launchDestinationNodeId +
                ":" + e.payloadId).ToArray();
            Check(coarse.SequenceEqual(fine) && coarse.Length == 2 &&
                host.Payloads.Payloads.Select(p => p.TravelSeconds[0])
                    .SequenceEqual(coarseTravel) &&
                Mathf.Abs(host.Payloads.Payloads.Last().Progress - coarseProgress) < .03f,
                scene + " coarse/fine cadence, seeded destination, travel, and position");

            host.ResetSimulation();
            recorder.BeginRun();
            host.TryInjectInfection("API", InfectionKind.PayloadSpawner, 20f, out _);
            FieldInfo linksField = typeof(SimulationHost).GetField("physicalLinks",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var savedLinks = (PhysicalLinkReference[])linksField.GetValue(host);
            try
            {
                linksField.SetValue(host, Array.Empty<PhysicalLinkReference>());
                host.StepSimulation(20f);
                Check(host.Payloads.Payloads.Count == 0 &&
                    recorder.CurrentLog.events.Single(e =>
                        e.type == "payload_spawner_attempt").reason ==
                        "no_adjacent_internal_node",
                    scene + " missing neighbors skip safely with reason");
            }
            finally { linksField.SetValue(host, savedLinks); }

            host.ResetSimulation();
            recorder.BeginRun();
            NodeView api = host.FindNodeResources("API").View;
            NodeView db = host.FindNodeResources("DB").View;
            var carrier = new ConnectionSpawnPlan(0f, "p1", new[] { api, db },
                "payload-spawner-provenance", "Maintenance",
                new[] { new OperationStage(OperationKind.Execute, 1f, 1f, "Apply update",
                    CompletionEffect.InfectionDelivery, payload:
                        new InfectionPayload(InfectionKind.PayloadSpawner, 0f, 3f)) }, true);
            traffic.QueueSpawnPlan(carrier);
            host.StepSimulation(.02f);
            host.StepSimulation(2f);
            Check(host.Infections.Count == 1 &&
                host.Infections[0].SourceConnectionId == "p1" &&
                host.Infections[0].SourceOperationIndex == 0,
                scene + " typed carrier provenance is retained");
            host.StepSimulation(3f);
            RunEvent lineage = recorder.CurrentLog.events.Single(e =>
                e.type == "payload_spawner_attempt");
            Check(lineage.sourceConnectionId == "p1" &&
                lineage.sourceOperationIndex == 0 &&
                lineage.launchOriginNodeId == "DB" &&
                lineage.payloadId == host.Payloads.Payloads.Single().Id,
                scene + " launch receipt links source connection operation and Payload");

            host.ResetSimulation();
            recorder.BeginRun();
            traffic.QueueSpawnPlan(carrier);
            host.StepSimulation(6f);
            RunEvent carrierCoarse = recorder.CurrentLog.events.Single(e =>
                e.type == "payload_spawner_attempt");
            PayloadState carriedCoarse = host.Payloads.Payloads.Single();
            float carrierProgress = carriedCoarse.Progress;
            float carrierTravel = carriedCoarse.TravelSeconds[0];
            host.ResetSimulation();
            recorder.BeginRun();
            traffic.QueueSpawnPlan(carrier);
            for (int i = 0; i < 300; i++) host.StepSimulation(.02f);
            RunEvent carrierFine = recorder.CurrentLog.events.Single(e =>
                e.type == "payload_spawner_attempt");
            PayloadState carriedFine = host.Payloads.Payloads.Single();
            Check(carrierCoarse.payloadId == carrierFine.payloadId &&
                carrierCoarse.launchDestinationNodeId == carrierFine.launchDestinationNodeId &&
                Mathf.Abs(carrierCoarse.timeSeconds - carrierFine.timeSeconds) < .03f &&
                carriedFine.TravelSeconds[0] == carrierTravel &&
                Mathf.Abs(carriedFine.Progress - carrierProgress) < .03f,
                scene + " carrier delivery also preserves coarse/fine launch and travel");
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("PayloadSpawnerWitness: " + message); }
    }
}
