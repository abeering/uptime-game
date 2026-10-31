using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class AmbientHostileActivityWitness
    {
        [MenuItem("Tools/Connections/Run Ambient Hostile Activity Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var fourNode = UnityEngine.Object.FindFirstObjectByType<HostileActivityDirector>();
            var fourTraffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            Check(fourNode != null && fourTraffic != null &&
                fourNode.PayloadWeight == 0f && fourNode.WormWeight == 0f &&
                fourTraffic.HostileMeanIntervalSeconds == 54f,
                "four-node ordinary ambient baseline is retained");
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var activities = UnityEngine.Object.FindFirstObjectByType<HostileActivityDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && traffic != null && activities != null && recorder != null &&
                traffic.HostileMeanIntervalSeconds == 30f &&
                activities.OrdinaryWeight == 4f && activities.ReconnaissanceWeight == 2f &&
                activities.PayloadWeight == 2f && activities.WormWeight == 1f &&
                activities.VariablePayloadChance == .5f &&
                activities.ScanJammedWormChance == .7f,
                "advanced authored selector and provisional tuning");
            traffic.RandomizeSeed = false;
            traffic.AutoGenerationEnabled = false;
            host.SetRunMode(ConnectionRunMode.Generated);

            SetWeights(activities, 1, 0, 0, 0);
            Reset(host, traffic, recorder, 1701);
            var ordinary = activities.QueueAmbientActivity(0f);
            Check(ordinary.Activity == "ordinary" && ordinary.Succeeded &&
                ordinary.ConnectionPlan.IsHostile && ordinary.ConnectionId != null &&
                ordinary.PayloadId == null && ordinary.WormOutbreakId == null,
                "ordinary opportunity creates a hostile connection plan");

            SetWeights(activities, 0, 1, 0, 0);
            Reset(host, traffic, recorder, 1701);
            var recon = activities.QueueAmbientActivity(0f);
            Check(recon.Activity == "reconnaissance" && recon.Succeeded &&
                recon.EventId == "recon-1" && recon.ConnectionId != null &&
                recorder.CurrentLog.events.Any(e => e.type == "recon_planned" &&
                    e.hostileEventId == recon.EventId),
                "reconnaissance retains event/scan linkage");

            SetWeights(activities, 0, 0, 1, 0);
            activities.VariablePayloadChance = 1f;
            Reset(host, traffic, recorder, 1701);
            var payload = activities.QueueAmbientActivity(0f);
            PayloadState payloadState = host.Payloads.Payloads.Single();
            Check(payload.Activity == "payload" && payload.Succeeded &&
                payload.ConnectionPlan == null && payload.PayloadId == payloadState.Id &&
                payload.PayloadProfile == "Variable" &&
                payloadState.Profile == PayloadMotionProfile.Variable &&
                IsIngress(traffic, payload.OriginNodeId) &&
                !IsIngress(traffic, payload.TargetNodeId) &&
                payloadState.Route[0].NodeId == payload.OriginNodeId &&
                payloadState.FinalNode.NodeId == payload.TargetNodeId &&
                RouteIsPhysical(host, payloadState.Route),
                "ambient Payload uses a reachable internal target and authored ingress");
            Check(recorder.CurrentLog.events.Any(e => e.type == "hostile_activity_selected" &&
                e.success && e.payloadId == payload.PayloadId && e.connectionId == null &&
                e.activityOriginNodeId == payload.OriginNodeId &&
                e.targetNodeId == payload.TargetNodeId && e.payloadProfile == "Variable" &&
                e.payloadActivityWeight == 1f),
                "Payload selection is successful and keeps its real identity in the recorder");
            activities.VariablePayloadChance = 0f;
            Reset(host, traffic, recorder, 1701);
            Check(activities.QueueAmbientActivity(0f).PayloadProfile == "Steady",
                "Steady profile remains selectable");
            activities.VariablePayloadChance = .5f;

            SetWeights(activities, 0, 0, 0, 1);
            activities.ScanJammedWormChance = 1f;
            Reset(host, traffic, recorder, 1701);
            var worm = activities.QueueAmbientActivity(0f);
            Check(worm.Activity == "worm" && worm.Succeeded &&
                worm.ConnectionPlan == null && worm.WormOutbreakId == "w1" &&
                worm.WormEffect == "ScanJammed" &&
                IsIngress(traffic, worm.OriginNodeId) &&
                host.Worms.Outbreaks.Single().Origin.NodeId == worm.OriginNodeId &&
                host.Worms.Outbreaks.Single().Tuning.propagationProbability == .65f &&
                recorder.CurrentLog.events.Any(e => e.type == "hostile_activity_selected" &&
                    e.success && e.wormOutbreakId == worm.WormOutbreakId &&
                    e.wormEffect == "ScanJammed" && e.wormActivityWeight == 1f),
                "ambient Worm reuses authored propagation and records its effect");
            activities.ScanJammedWormChance = 0f;
            Reset(host, traffic, recorder, 1701);
            Check(activities.QueueAmbientActivity(0f).WormEffect == "PortScanned",
                "PORT SCANNED Worm remains selectable");
            activities.ScanJammedWormChance = .7f;

            string[] names = { "ordinary", "reconnaissance", "payload", "worm" };
            for (int disabled = 0; disabled < names.Length; disabled++)
            {
                SetWeights(activities, disabled == 0 ? 0 : 1,
                    disabled == 1 ? 0 : 1, disabled == 2 ? 0 : 1,
                    disabled == 3 ? 0 : 1);
                Reset(host, traffic, recorder, 1701);
                for (int i = 0; i < 30; i++)
                    Check(activities.QueueAmbientActivity(i).Activity != names[disabled],
                        "zero weight disables " + names[disabled]);
            }
            SetWeights(activities, 0, 0, 0, 0);
            Reset(host, traffic, recorder, 1701);
            var empty = activities.QueueAmbientActivity(0f);
            Check(!empty.Succeeded && empty.Activity == "none" &&
                empty.Reason == "no_weighted_activity", "zero total weight gives a clear skip");

            SetWeights(activities, 4, 2, 2, 1);
            Reset(host, traffic, recorder, 1701);
            string benignBaseline = BenignSignature(traffic.GenerateBenignPlan(0f));
            Reset(host, traffic, recorder, 1701);
            string[] first = Enumerable.Range(0, 80).Select(i =>
                Signature(activities.QueueAmbientActivity(i), host)).ToArray();
            Check(names.All(name => first.Any(item => item.StartsWith(name + ":"))) &&
                first.Select(item => item.Split(':')[1]).Where(id => id != "")
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count() == 80,
                "weighted selection reaches all four activities with unique identities");
            Check(BenignSignature(traffic.GenerateBenignPlan(0f)) == benignBaseline,
                "hostile activity does not consume benign recipe randomness");
            Reset(host, traffic, recorder, 1701);
            string[] replay = Enumerable.Range(0, 80).Select(i =>
                Signature(activities.QueueAmbientActivity(i), host)).ToArray();
            Check(first.SequenceEqual(replay), "seeded activity/entity replay");

            SetWeights(activities, 0, 0, 1, 0);
            Reset(host, traffic, recorder, 1701);
            var linkField = typeof(SimulationHost).GetField("physicalLinks",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var links = host.PhysicalLinks;
            try
            {
                linkField.SetValue(host, Array.Empty<PhysicalLinkReference>());
                var unavailable = activities.QueueAmbientActivity(0f);
                Check(!unavailable.Succeeded && unavailable.Reason ==
                    "no_reachable_internal_target" && host.Payloads.Payloads.Count == 0,
                    "disconnected network produces no false Payload success");
            }
            finally { linkField.SetValue(host, links); }

            SetWeights(activities, 0, 0, 0, 1);
            Reset(host, traffic, recorder, 1701);
            Check(host.Worms.TryIsolate("EXT1", null, out _) &&
                host.Worms.TryIsolate("EXT2", null, out _),
                "unavailable Worm ingress setup");
            var unavailableWorm = activities.QueueAmbientActivity(0f);
            Check(!unavailableWorm.Succeeded && unavailableWorm.Reason ==
                "no_available_ingress" && host.Worms.Outbreaks.Count == 0,
                "isolated ingress prevents ambient Worm origin selection");

            SetWeights(activities, 0, 0, 1, 0);
            Reset(host, traffic, recorder, 1701);
            traffic.AutoGenerationEnabled = true;
            traffic.HostileGenerationEnabled = true;
            typeof(ConnectionTrafficDirector).GetField("nextIdIndex",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(traffic, 260);
            typeof(ConnectionTrafficDirector).GetField("idsExhausted",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(traffic, true);
            var nextAt = typeof(ConnectionTrafficDirector).GetField("nextHostileAt",
                BindingFlags.Instance | BindingFlags.NonPublic);
            nextAt.SetValue(traffic, 0f);
            host.StepSimulation(1f);
            Check(recorder.CurrentLog.events.Any(e => e.type == "hostile_opportunity" &&
                e.hostileActivity == "payload" && e.success && e.payloadId != null),
                "connection-ID exhaustion does not suppress Payload opportunities");
            SetWeights(activities, 0, 0, 0, 1);
            nextAt.SetValue(traffic, host.SimulationTimeSeconds);
            host.StepSimulation(1f);
            Check(recorder.CurrentLog.events.Any(e => e.type == "hostile_opportunity" &&
                e.hostileActivity == "worm" && e.success && e.wormOutbreakId != null),
                "connection-ID exhaustion does not suppress Worm opportunities");
            SetWeights(activities, 1, 0, 0, 0);
            nextAt.SetValue(traffic, host.SimulationTimeSeconds);
            host.StepSimulation(1f);
            Check(recorder.CurrentLog.events.Any(e => e.type == "hostile_opportunity" &&
                e.hostileActivity == "ordinary" && !e.success &&
                e.reason == "ids_exhausted"),
                "exhausted ordinary plans report failure without blocking later activities");

            SetWeights(activities, 4, 2, 2, 1);
            traffic.AutoGenerationEnabled = false;
            traffic.HostileGenerationEnabled = false;
            Reset(host, traffic, recorder, 1701);
            host.StepSimulation(120f);
            Check(!recorder.CurrentLog.events.Any(e => e.type ==
                "hostile_activity_selected"), "ambient toggle prevents all hostile types");
            Check(host.Payloads.Spawn("DB", PayloadMotionProfile.Steady, "EXT1") != null &&
                host.Worms.Spawn("EXT1", WormEffect.PortScanned) != null &&
                activities.QueueReconnaissance("DB") != null &&
                traffic.QueueProfile("product-lookup") != null,
                "developer spawning remains available when ambient hostiles are disabled");
            traffic.AutoGenerationEnabled = true;
            Reset(host, traffic, recorder, 1701);
            host.StepSimulation(120f);
            Check(!recorder.CurrentLog.events.Any(e => e.type ==
                "hostile_activity_selected"),
                "hostile-generation toggle alone disables all four activities");
            traffic.AutoGenerationEnabled = false;
            traffic.HostileGenerationEnabled = true;
            Reset(host, traffic, recorder, 1701);
            host.StepSimulation(120f);
            Check(!recorder.CurrentLog.events.Any(e => e.type ==
                "hostile_activity_selected"),
                "auto-generation toggle alone disables all four activities");
            traffic.AutoGenerationEnabled = true;
            Reset(host, traffic, recorder, 1701);
            host.StepSimulation(120f);
            string coarse = string.Join("|", recorder.CurrentLog.events
                .Where(e => e.type == "hostile_activity_selected")
                .Select(e => e.timeSeconds.ToString("R") + ":" + e.hostileActivity +
                    ":" + e.connectionId + ":" + e.payloadId + ":" + e.wormOutbreakId));
            int coarsePayloads = host.Payloads.Payloads.Count;
            int coarseWorms = host.Worms.Outbreaks.Count;
            Reset(host, traffic, recorder, 1701);
            for (int i = 0; i < 120; i++) host.StepSimulation(1f);
            string fine = string.Join("|", recorder.CurrentLog.events
                .Where(e => e.type == "hostile_activity_selected")
                .Select(e => e.timeSeconds.ToString("R") + ":" + e.hostileActivity +
                    ":" + e.connectionId + ":" + e.payloadId + ":" + e.wormOutbreakId));
            Check(coarse == fine && host.Payloads.Payloads.Count == coarsePayloads &&
                host.Worms.Outbreaks.Count == coarseWorms,
                "coarse step matches repeated one-second ambient simulation");

            RunExperiments(host, traffic, activities, recorder);
            Debug.Log("AmbientHostileActivityWitness PASS: selection, zero weights, routes, " +
                "replay, toggles, exhaustion, coarse stepping, and four 600-second runs.");
        }

        private static void RunExperiments(SimulationHost host,
            ConnectionTrafficDirector traffic, HostileActivityDirector activities,
            ConnectionsRunRecorder recorder)
        {
            SetWeights(activities, 4, 2, 2, 1);
            traffic.AutoGenerationEnabled = true;
            traffic.HostileGenerationEnabled = true;
            foreach (int seed in new[] { 1701, 1702, 1703, 1704 })
            {
                Reset(host, traffic, recorder, seed);
                float minimumHealth = 100f;
                int allConnectionSlowOrOverdueSampledSeconds = 0;
                int benignHealthImpactSampledSeconds = 0;
                int firstFailedSampleSecond = -1, peakConnections = 0;
                for (int second = 0; second < 600; second++)
                {
                    host.StepSimulation(1f);
                    NetworkHealthSnapshot health = host.Health;
                    minimumHealth = Mathf.Min(minimumHealth, health.Value);
                    if (health.SlowedCount > 0 || health.OverdueCount > 0)
                        allConnectionSlowOrOverdueSampledSeconds++;
                    if (health.Impacts.Count > 0)
                        benignHealthImpactSampledSeconds++;
                    if (health.Failed && firstFailedSampleSecond < 0)
                        firstFailedSampleSecond = second + 1;
                    peakConnections = Mathf.Max(peakConnections,
                        host.RuntimeConnections.Count(c => c.IsActive || c.IsConnecting));
                }
                var events = recorder.CurrentLog.events;
                var selected = events.Where(e => e.type == "hostile_activity_selected").ToArray();
                int Count(string type) => events.Count(e => e.type == type);
                int Activity(string name) => selected.Count(e => e.hostileActivity == name && e.success);
                var ordinaryIds = new HashSet<string>(selected.Where(e =>
                    e.hostileActivity == "ordinary" && e.success).Select(e => e.connectionId),
                    StringComparer.OrdinalIgnoreCase);
                int ordinaryResolved = events.Count(e => e.type == "connection_resolved" &&
                    ordinaryIds.Contains(e.connectionId));
                int ordinaryWithdrawn = events.Count(e => e.type == "connection_resolved" &&
                    ordinaryIds.Contains(e.connectionId) && e.resolutionReason == "Withdrawn");
                int canceledBenign = events.Count(e => e.type == "connection_resolved" &&
                    e.resolutionReason == "Canceled" && e.definition != null &&
                    !e.definition.isHostile);
                string metric = $"AMBIENT V2 seed={seed} opportunities={selected.Length} " +
                    $"ordinary={Activity("ordinary")} recon={Activity("reconnaissance")} " +
                    $"payload={Activity("payload")} worm={Activity("worm")} " +
                    $"selectionFailures={selected.Count(e => !e.success)} " +
                    $"ordinaryResolved={ordinaryResolved} ordinaryWithdrawn={ordinaryWithdrawn} " +
                    $"reconScansResolved={Count("recon_scan_resolved")} " +
                    $"reconFollowups={Count("recon_followup_queued")} " +
                    $"payloadDelivered={Count("payload_effect_delivered")} " +
                    $"payloadRejected={Count("payload_effect_rejected")} " +
                    $"wormBranches={Count("worm_branch_created")} " +
                    $"wormBranchesTerminated={Count("worm_branch_terminated")} " +
                    $"wormCompleted={Count("worm_outbreak_completed")} " +
                    $"infectionApplied={Count("infection_applied")} " +
                    $"conditionsApplied={Count("condition_applied")} " +
                    $"benignCanceled={canceledBenign} " +
                    $"allConnectionSlowOrOverdueSampledSeconds={allConnectionSlowOrOverdueSampledSeconds} " +
                    $"benignHealthImpactSampledSeconds={benignHealthImpactSampledSeconds} " +
                    $"peakConnections={peakConnections} healthFinal={host.Health.Value:0.##} " +
                    $"healthMinimum={minimumHealth:0.##} healthFailed={host.Health.Failed} " +
                    $"firstFailedSampleSecond={firstFailedSampleSecond}";
                Debug.Log(metric);
                Check(benignHealthImpactSampledSeconds <= allConnectionSlowOrOverdueSampledSeconds &&
                    (minimumHealth > 0f || host.Health.Failed) &&
                    (host.Health.Failed == (firstFailedSampleSecond > 0)) &&
                    (seed != 1701 || !host.Health.Failed) &&
                    (seed == 1701 || host.Health.Failed),
                    "sampled Health impacts and latched failure reporting");
                Check(selected.All(e => e.success || e.reason != null) &&
                    selected.All(e => e.ordinaryActivityWeight == 4f &&
                        e.reconnaissanceActivityWeight == 2f &&
                        e.payloadActivityWeight == 2f && e.wormActivityWeight == 1f),
                    "600-second selection evidence carries outcomes and weights");
                recorder.EndRun();
            }
        }

        private static void Reset(SimulationHost host, ConnectionTrafficDirector traffic,
            ConnectionsRunRecorder recorder, int seed)
        {
            traffic.Seed = seed;
            host.ResetSimulation();
            recorder.BeginRun();
        }

        private static void SetWeights(HostileActivityDirector director,
            float ordinary, float recon, float payload, float worm)
        {
            director.OrdinaryWeight = ordinary;
            director.ReconnaissanceWeight = recon;
            director.PayloadWeight = payload;
            director.WormWeight = worm;
        }

        private static bool IsIngress(ConnectionTrafficDirector traffic, string id) =>
            traffic.EffectiveAmbientOrigins.Any(entry => entry.nodeId == id);

        private static bool RouteIsPhysical(SimulationHost host, NodeView[] route)
        {
            for (int i = 0; i < route.Length - 1; i++)
                if (!host.PhysicalLinks.Any(link => link.View.NodeA == route[i] &&
                    link.View.NodeB == route[i + 1] || link.View.NodeB == route[i] &&
                    link.View.NodeA == route[i + 1])) return false;
            return route.Length > 1;
        }

        private static string Signature(AmbientHostileActivityResult result,
            SimulationHost host)
        {
            string concrete = "";
            if (result.PayloadId != null)
            {
                PayloadState payload = host.Payloads.Payloads.Single(p =>
                    p.Id == result.PayloadId);
                concrete = string.Join(",", payload.Route.Select(n => n.NodeId)) + "/" +
                    string.Join(",", payload.TravelSeconds.Select(t => t.ToString("R")));
            }
            else if (result.WormOutbreakId != null)
                concrete = host.Worms.Outbreaks.Single(w =>
                    w.Id == result.WormOutbreakId).Seed.ToString();
            return result.Activity + ":" + (result.ConnectionId ?? result.PayloadId ??
                result.WormOutbreakId ?? "") + ":" + result.EventId + ":" +
            result.OriginNodeId + ":" + result.TargetNodeId + ":" +
            result.PayloadProfile + ":" + result.WormEffect + ":" +
            result.Reason + ":" + concrete;
        }

        private static string BenignSignature(ConnectionSpawnPlan plan) =>
            plan.ProfileId + ":" + plan.Target.NodeId + ":" +
            string.Join("|", plan.Operations.Select(op =>
                op.Operation + "/" + op.DesiredLoad.ToString("R") + "/" +
                op.TotalWork.ToString("R")));

        private static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException(
                "AmbientHostileActivityWitness: " + message);
        }
    }
}
