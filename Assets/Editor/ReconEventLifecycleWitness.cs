using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class ReconEventLifecycleWitness
    {
        [MenuItem("Tools/Connections/Run Recon Event Lifecycle Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var activities = UnityEngine.Object.FindFirstObjectByType<HostileActivityDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            traffic.AutoGenerationEnabled = false;
            traffic.RandomizeSeed = false;
            traffic.Seed = 1701;

            foreach (ConnectionResolution outcome in new[] { ConnectionResolution.Completed,
                ConnectionResolution.Killed, ConnectionResolution.Canceled })
            {
                ConnectionSpawnPlan scan = Start(host, traffic, activities, recorder,
                    false, outcome);
                Check(activities.ActiveReconnaissanceCount == 0,
                    "no-followup event retires after " + outcome);
                CheckReceipt(recorder, scan.HostileEventId, "recon_scan_resolved",
                    scan.ConnectionId, outcome);
                Check(!recorder.CurrentLog.events.Any(e =>
                    e.hostileEventId == scan.HostileEventId &&
                    e.type == "recon_followup_scheduled"),
                    "no-followup event has no schedule");
            }

            ConnectionSpawnPlan killedScan = Start(host, traffic, activities, recorder,
                true, ConnectionResolution.Killed);
            CheckActiveOutcome(activities, ConnectionResolution.Killed);
            string completedFollowup = LaunchFollowup(host, activities, recorder,
                killedScan.HostileEventId);
            LogicalConnectionState completed = host.FindConnection(completedFollowup);
            for (int i = 0; i < 300 && !completed.IsComplete; i++)
                host.StepSimulation(1f);
            Check(completed.IsComplete && completed.Resolution == ConnectionResolution.Completed &&
                activities.ActiveReconnaissanceCount == 0,
                "completed follow-up retires event after killed scan");
            CheckReceipt(recorder, killedScan.HostileEventId, "recon_followup_resolved",
                completedFollowup, ConnectionResolution.Completed);

            ConnectionSpawnPlan completedScan = Start(host, traffic, activities, recorder,
                true, ConnectionResolution.Completed);
            CheckActiveOutcome(activities, ConnectionResolution.Completed);
            string killedFollowup = LaunchFollowup(host, activities, recorder,
                completedScan.HostileEventId);
            Check(host.KillConnection(killedFollowup) &&
                activities.ActiveReconnaissanceCount == 0,
                "player-killed follow-up retires event");
            CheckReceipt(recorder, completedScan.HostileEventId, "recon_followup_resolved",
                killedFollowup, ConnectionResolution.Killed);

            ConnectionSpawnPlan canceledScan = Start(host, traffic, activities, recorder,
                true, ConnectionResolution.Canceled);
            CheckActiveOutcome(activities, ConnectionResolution.Canceled);
            string canceledFollowup = LaunchFollowup(host, activities, recorder,
                canceledScan.HostileEventId);
            Check(host.FindConnection(canceledFollowup).Resolution ==
                ConnectionResolution.Canceled && activities.ActiveReconnaissanceCount == 0,
                "Blackout-canceled follow-up retires event");
            CheckReceipt(recorder, canceledScan.HostileEventId,
                "recon_followup_resolved", canceledFollowup,
                ConnectionResolution.Canceled);

            ConnectionSpawnPlan unavailableScan = Start(host, traffic, activities, recorder,
                true, ConnectionResolution.Killed);
            EffectiveWeights(traffic).Clear();
            host.StepSimulation(8f);
            host.StepSimulation(.1f);
            Check(activities.ActiveReconnaissanceCount == 0 &&
                recorder.CurrentLog.events.Any(e =>
                    e.type == "recon_followup_unavailable" &&
                    e.hostileEventId == unavailableScan.HostileEventId &&
                    e.scanConnectionId == unavailableScan.ConnectionId &&
                    e.reason == "no_plan"),
                "failed follow-up generation records and retires event");

            Start(host, traffic, activities, recorder, true, ConnectionResolution.Killed);
            Check(activities.ActiveReconnaissanceCount == 1,
                "selected follow-up remains active before reset");
            host.ResetSimulation();
            Check(activities.ActiveReconnaissanceCount == 0,
                "reset clears all active events");
            recorder.BeginRun();
            host.StepSimulation(9f);
            host.StepSimulation(.1f);
            Check(!recorder.CurrentLog.events.Any(e => e.type == "recon_followup_queued"),
                "reset cannot launch a stale follow-up");
            Debug.Log("ReconEventLifecycleWitness PASS: scan and follow-up Completed/Killed/" +
                "Canceled, generation failure, resolution provenance, safe retirement, reset.");
        }

        private static ConnectionSpawnPlan Start(SimulationHost host,
            ConnectionTrafficDirector traffic, HostileActivityDirector activities,
            ConnectionsRunRecorder recorder, bool followup, ConnectionResolution outcome)
        {
            traffic.AutoGenerationEnabled = false;
            activities.FollowupChance = followup ? 1f : 0f;
            host.ResetSimulation();
            recorder.BeginRun();
            if (outcome == ConnectionResolution.Canceled)
                Check(host.TryInjectInfection("EXT1", InfectionKind.Blackout, 0f,
                    out _), "Blackout origin injection");
            ConnectionSpawnPlan scan = activities.QueueReconnaissance("DB");
            Check(scan != null && activities.ActiveReconnaissanceCount == 1,
                "event is active after plan queue");
            host.StepSimulation(.1f);
            if (outcome == ConnectionResolution.Killed)
                Check(host.KillConnection(scan.ConnectionId), "kill connecting scan");
            else if (outcome == ConnectionResolution.Completed)
                host.StepSimulation(10f);
            LogicalConnectionState state = host.FindConnection(scan.ConnectionId);
            Check(state.IsComplete && state.Resolution == outcome,
                "scan outcome " + outcome);
            return scan;
        }

        private static string LaunchFollowup(SimulationHost host,
            HostileActivityDirector activities, ConnectionsRunRecorder recorder,
            string eventId)
        {
            host.StepSimulation(8f);
            host.StepSimulation(.1f);
            RunEvent queued = recorder.CurrentLog.events.Single(e =>
                e.type == "recon_followup_queued" && e.hostileEventId == eventId);
            Check(recorder.CurrentLog.events.Any(e => e.type == "plan_queued" &&
                e.connectionId == queued.followupConnectionId &&
                e.hostileEventId == eventId && e.source == "recon_followup"),
                "follow-up plan retains event linkage");
            return queued.followupConnectionId;
        }

        private static void CheckActiveOutcome(HostileActivityDirector activities,
            ConnectionResolution reason)
        {
            IList active = (IList)typeof(HostileActivityDirector).GetField("events",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(activities);
            Check(active.Count == 1 && activities.ActiveReconnaissanceCount == 1 &&
                active[0].GetType().GetField("ScanResolution").GetValue(active[0])
                    .Equals(reason),
                "active event retains typed scan resolution " + reason);
        }

        private static Dictionary<string, float> EffectiveWeights(ConnectionTrafficDirector traffic) =>
            (Dictionary<string, float>)typeof(ConnectionTrafficDirector)
                .GetField("effectiveWeights", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(traffic);

        private static void CheckReceipt(ConnectionsRunRecorder recorder, string eventId,
            string type, string connectionId, ConnectionResolution reason)
        {
            RunEvent receipt = recorder.CurrentLog.events.Single(e => e.type == type &&
                e.hostileEventId == eventId);
            Check(receipt.reason == reason.ToString() &&
                (type == "recon_scan_resolved" ? receipt.scanConnectionId :
                    receipt.followupConnectionId) == connectionId,
                "recorder links " + type + " outcome " + reason);
        }

        private static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException(
                "ReconEventLifecycleWitness: " + message);
        }
    }
}
