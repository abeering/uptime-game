using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class AmbientPayloadThreatWitness
    {
        [MenuItem("Tools/Connections/Run Ambient Payload Threat Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var smallActivity = UnityEngine.Object.FindFirstObjectByType<HostileActivityDirector>();
            var smallTraffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            Check(smallActivity.PayloadLauncherWeight == 0f &&
                smallTraffic.PayloadSpawnerWeight == 0f, "four-node ambient exclusions");

            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var activity = UnityEngine.Object.FindFirstObjectByType<HostileActivityDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && traffic != null && activity != null && recorder != null &&
                traffic.HostileMeanIntervalSeconds == 30f &&
                activity.PayloadLauncherWeight == 1f && traffic.PayloadSpawnerWeight == 1f,
                "Advanced authored weights and unchanged opportunity interval");
            traffic.RandomizeSeed = false;
            traffic.AutoGenerationEnabled = false;
            host.SetRunMode(ConnectionRunMode.Generated);

            SetActivity(activity, 0, 0, 0, 0, 1);
            Reset(host, traffic, recorder, 1701);
            AmbientHostileActivityResult launcher = activity.QueueAmbientActivity(5f);
            ConnectionSpawnPlan plan = launcher.ConnectionPlan;
            Check(launcher.Activity == "payload_launcher" && launcher.Succeeded &&
                plan.ScheduledSpawnTime == 5f && plan.ProfileId == "payload-launcher" &&
                plan.Target.NodeId == "API" && plan.Operations.Length == 1 &&
                plan.Operations[0].EffectOnCompletion == CompletionEffect.PayloadLaunch &&
                plan.Operations[0].LaunchSpec.DestinationNodeId == "DB" &&
                recorder.CurrentLog.events.Any(e => e.type == "plan_queued" &&
                    e.connectionId == plan.ConnectionId &&
                    e.source == "ambient_payload_launcher" && e.scheduledSpawnTime == 5f) &&
                recorder.CurrentLog.events.Any(e => e.type == "hostile_activity_selected" &&
                    e.hostileActivity == "payload_launcher" &&
                    e.connectionId == plan.ConnectionId && e.profileId == "payload-launcher" &&
                    e.operationIndex == 0 && e.operationKind == "Execute" &&
                    e.effectType == "PayloadLaunch" && e.payloadLauncherActivityWeight == 1f),
                "ambient launcher uses profile, due time, and causal recorder source");
            host.StepSimulation(5.1f);
            Check(host.KillConnection(plan.ConnectionId), "launcher can be killed before WORK");
            host.StepSimulation(30f);
            Check(host.Payloads.Payloads.Count == 0, "killed launcher creates no Payload");

            Reset(host, traffic, recorder, 1701);
            launcher = activity.QueueAmbientActivity(0f);
            host.StepSimulation(35f);
            var launched = recorder.CurrentLog.events.Single(e => e.type ==
                "payload_launched" && e.connectionId == launcher.ConnectionId);
            Check(host.Payloads.Payloads.Count == 1 &&
                launched.operationIndex == 0 && launched.payloadId == host.Payloads.Payloads[0].Id &&
                host.Payloads.Payloads[0].Route.Length == 2 &&
                host.Payloads.Payloads[0].CurrentNode.NodeId == "API" &&
                host.Payloads.Payloads[0].FinalNode.NodeId == "DB" &&
                host.Infections.All(i => i.Target.View.NodeId != "API"),
                "completed launcher starts one API-to-DB strike without infecting API");

            var weights = new[] { "phantomLoadWeight", "blackoutWeight", "spawnerWeight",
                "payloadSpawnerWeight" }.Select(name => typeof(ConnectionTrafficDirector)
                    .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)).ToArray();
            float[] originals = weights.Select(field => (float)field.GetValue(traffic)).ToArray();
            try
            {
                for (int i = 0; i < weights.Length; i++) weights[i].SetValue(traffic, i == 3 ? 1f : 0f);
                Reset(host, traffic, recorder, 1701);
                ConnectionSpawnPlan carrier = traffic.GenerateInfectorPlanForWitness(0f);
                Check(carrier.Operations.Last().InfectionPayload.Kind == InfectionKind.PayloadSpawner &&
                    carrier.Operations.Last().InfectionPayload.CadenceSeconds == 20f &&
                    carrier.GenerationTrace.selectedInfectionKind == "PayloadSpawner" &&
                    carrier.GenerationTrace.selectedInfectionCadence == 20f,
                    "INFECTOR composer supplies valid default cadence and selection evidence");
                traffic.QueueSpawnPlan(carrier, "ambient_hostile");
                for (int second = 0; second < 200 && host.Infections.Count == 0; second++)
                    host.StepSimulation(1f);
                Check(host.Infections.Count == 1 && host.Payloads.Payloads.Count == 0 &&
                    host.Infections[0].SourceConnectionId == carrier.ConnectionId &&
                    host.Infections[0].SourceOperationIndex == carrier.Operations.Length - 1 &&
                    host.Infections[0].NextSpawnAtSeconds ==
                        host.Infections[0].AppliedAtSeconds + 20f,
                    "generated carrier installs once and does not launch immediately");
                NodeInfection source = host.Infections[0];
                host.StepSimulation(20f);
                Check(host.Payloads.Payloads.Count == 1 &&
                    recorder.CurrentLog.events.Any(e => e.type == "payload_spawner_attempt" &&
                        e.sourceConnectionId == carrier.ConnectionId &&
                        e.sourceOperationIndex == carrier.Operations.Length - 1 &&
                        e.payloadId == host.Payloads.Payloads[0].Id),
                    "first periodic launch retains carrier lineage");
                Check(host.StartClean(source.Target.View.NodeId, "clean source", out CleanSession clean)
                    == CleanStartResult.Started, "CLEAN opens on generated infection");
                int pid = clean.Processes.Single(p => p.CpuPercent >= 82).Pid;
                Check(host.SelectCleanPid(pid, out int removed) == CleanSelectionResult.Success &&
                    removed == 1, "PID-Select removes generated source");
                host.StepSimulation(45f);
                Check(host.Payloads.Payloads.Count == 1 &&
                    host.Payloads.Payloads[0].EffectApplied &&
                    recorder.CurrentLog.events.Count(e => e.type == "payload_spawner_attempt") == 1,
                    "cleaning stops future launches and leaves the in-flight Payload independent");

                // The connection Spawner keeps its original three-way child selection.
                for (int i = 0; i < weights.Length; i++) weights[i].SetValue(traffic, i == 0 ? 1f : i == 3 ? 100f : 0f);
                Reset(host, traffic, recorder, 1701);
                Check(host.TryInjectInfection("DB", InfectionKind.Spawner, 1f, out _),
                    "connection Spawner setup");
                host.StepSimulation(5f);
                Check(recorder.CurrentLog.events.Where(e => e.type == "plan_queued" &&
                    e.source == "spawner").All(e =>
                    e.definition.operations.Last().infectionKind == "PhantomLoad") &&
                    recorder.CurrentLog.events.Count(e => e.type == "plan_queued" &&
                        e.source == "spawner") >= 4,
                    "connection Spawner excludes recursive Payload Spawner outcome");
            }
            finally
            {
                for (int i = 0; i < weights.Length; i++) weights[i].SetValue(traffic, originals[i]);
            }

            SetActivity(activity, 0, 0, 0, 0, 1);
            traffic.AutoGenerationEnabled = true;
            traffic.HostileGenerationEnabled = false;
            Reset(host, traffic, recorder, 1701);
            host.StepSimulation(120f);
            Check(!recorder.CurrentLog.events.Any(e => e.type == "hostile_activity_selected"),
                "hostile toggle suppresses ambient Launcher");
            traffic.HostileGenerationEnabled = true;
            traffic.AutoGenerationEnabled = false;
            Reset(host, traffic, recorder, 1701);
            host.StepSimulation(120f);
            Check(!recorder.CurrentLog.events.Any(e => e.type == "hostile_activity_selected"),
                "auto toggle suppresses ambient Launcher");
            traffic.AutoGenerationEnabled = true;
            Reset(host, traffic, recorder, 1701);
            host.StepSimulation(120f);
            RunEvent clockSelection = recorder.CurrentLog.events.First(e =>
                e.type == "hostile_activity_selected" &&
                e.hostileActivity == "payload_launcher");
            Check(recorder.CurrentLog.events.Any(e => e.type == "hostile_opportunity" &&
                e.hostileActivity == "payload_launcher" && e.success &&
                e.connectionId == clockSelection.connectionId &&
                e.scheduledSpawnTime == clockSelection.scheduledSpawnTime),
                "hostile clock records the same Launcher ID and scheduled opportunity");

            SetActivity(activity, 4, 2, 2, 1, 1);
            string first = RunSignature(host, traffic, recorder, 1701);
            string replay = RunSignature(host, traffic, recorder, 1701);
            Check(first == replay, "seeded ambient reset/replay");
            foreach (int seed in new[] { 1702, 1703, 1704 })
                RunSignature(host, traffic, recorder, seed);
            Debug.Log("AmbientPayloadThreatWitness PASS: both scene weights, Launcher lineage/kill, " +
                "INFECTOR cadence, CLEAN independence, Spawner exclusion, toggles, replay, " +
                "and four 600-second Advanced simulations.");
        }

        private static string RunSignature(SimulationHost host,
            ConnectionTrafficDirector traffic, ConnectionsRunRecorder recorder, int seed)
        {
            Reset(host, traffic, recorder, seed);
            for (int i = 0; i < 600; i++) host.StepSimulation(1f);
            var events = recorder.CurrentLog.events;
            var selected = events.Where(e => e.type == "hostile_activity_selected").ToArray();
            int launcherSelections = selected.Count(e => e.hostileActivity == "payload_launcher");
            int spawnerPlans = events.Count(e => e.type == "plan_queued" &&
                e.definition?.operations != null && e.definition.operations.Any(o =>
                    o.infectionKind == "PayloadSpawner"));
            int spawnerInstalled = events.Count(e => e.type == "infection_applied" &&
                e.infectionKind == "PayloadSpawner");
            int periodic = events.Count(e => e.type == "payload_spawner_attempt" &&
                e.reason == "launched");
            Debug.Log($"AMBIENT PAYLOAD V0 seed={seed} opportunities={selected.Length} " +
                $"launcherSelections={launcherSelections} launcherPayloads=" +
                $"{events.Count(e => e.type == "payload_launched")} " +
                $"payloadSpawnerPlans={spawnerPlans} payloadSpawnerInstalled={spawnerInstalled} " +
                $"periodicLaunches={periodic} infectionApplied=" +
                $"{events.Count(e => e.type == "infection_applied")} infectionRejected=" +
                $"{events.Count(e => e.type == "infection_rejected")} " +
                $"interceptions={events.Count(e => e.type == "intercept_succeeded")}");
            Check(selected.All(e => e.payloadLauncherActivityWeight == 1f &&
                e.ordinaryActivityWeight == 4f && e.reconnaissanceActivityWeight == 2f &&
                e.payloadActivityWeight == 2f && e.wormActivityWeight == 1f) &&
                selected.All(e => e.success || e.reason != null),
                "600-second selection evidence retains authored weights and outcomes");
            return string.Join("|", events.Where(e => e.type == "hostile_activity_selected" ||
                e.type == "payload_launched" || e.type == "payload_spawner_attempt" ||
                e.type == "infection_applied").Select(e => e.type + ":" +
                e.timeSeconds.ToString("R") + ":" + e.connectionId + ":" +
                e.payloadId + ":" + e.infectionKind + ":" + e.hostileActivity));
        }

        private static void Reset(SimulationHost host, ConnectionTrafficDirector traffic,
            ConnectionsRunRecorder recorder, int seed)
        {
            traffic.Seed = seed;
            host.ResetSimulation();
            recorder.BeginRun();
        }

        private static void SetActivity(HostileActivityDirector activity,
            float ordinary, float recon, float payload, float worm, float launcher)
        {
            activity.OrdinaryWeight = ordinary;
            activity.ReconnaissanceWeight = recon;
            activity.PayloadWeight = payload;
            activity.WormWeight = worm;
            activity.PayloadLauncherWeight = launcher;
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("AmbientPayloadThreatWitness: " + message); }
    }
}
