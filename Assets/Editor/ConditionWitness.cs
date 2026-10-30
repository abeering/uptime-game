using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class ConditionWitness
    {
        [MenuItem("Tools/Connections/Run Condition Witness")]
        public static void Run()
        {
            CheckScene("Assets/Scenes/Connections.unity", 4, 54f, 0f);
            CheckScene("Assets/Scenes/ConnectionsAdvanced.unity", 8, 30f, 2f);
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var activities = UnityEngine.Object.FindFirstObjectByType<HostileActivityDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            traffic.AutoGenerationEnabled = false;
            traffic.RandomizeSeed = false;
            host.SetRunMode(ConnectionRunMode.Generated);
            recorder.BeginRun();
            NodeResources db = host.Nodes.First(n => n.View.NodeId == "DB");
            var payload = new ConditionPayload(ConditionKind.PortScanned, 20f);
            NodeCondition condition = host.ApplyCondition(db, payload, "test", 0);
            Check(host.Conditions.Count == 1 && condition.ExpiresAtSeconds == 20f,
                "typed condition application");
            host.StepSimulation(5f);
            host.ApplyCondition(db, payload, "refresh", 1);
            Check(host.Conditions.Count == 1 && condition.AppliedAtSeconds == 5f &&
                condition.ExpiresAtSeconds == 25f && condition.SourceConnectionId == "refresh",
                "reapplication refreshes one condition with provenance");
            presenter.RenderNow(1f, 1f);
            NodeStatusState state = presenter.StateFor(db.View);
            Check(state.PortScanned && Mathf.Approximately(state.PortScanRemainingFraction, 1f) &&
                presenter.ViewFor(db.View).AwarenessVisible,
                "presenter reads condition and shows Awareness");
            var awarenessPanel = presenter.ViewFor(db.View).GetComponentsInChildren<NodeStatusPanel>(true)[1];
            var timer = (Image)new SerializedObject(awarenessPanel)
                .FindProperty("firstRadialTimer").objectReferenceValue;
            Check(timer.gameObject.activeSelf && Mathf.Approximately(timer.fillAmount, 1f),
                "authored radial timer follows condition");
            host.StepSimulation(10f);
            presenter.RenderNow(2f, 1f);
            Check(Mathf.Abs(state.PortScanRemainingFraction - .5f) < .001f &&
                Mathf.Abs(timer.fillAmount - .5f) < .001f, "simulation-time radial countdown");
            host.StepSimulation(10f);
            presenter.RenderNow(3f, 1f);
            Check(host.Conditions.Count == 0 && !state.PortScanned,
                "condition expires at exact simulated time");
            Check(recorder.CurrentLog.events.Any(e => e.type == "condition_refreshed" &&
                e.conditionExpiresAtSeconds == 25f && e.sourceConnectionId == "refresh") &&
                recorder.CurrentLog.events.Any(e => e.type == "condition_expired" &&
                    e.timeSeconds == 25f), "condition refresh and expiry recorded");

            host.ResetSimulation();
            recorder.BeginRun();
            ConnectionSpawnPlan killed = activities.QueuePortScan("DB");
            host.StepSimulation(.1f);
            Check(host.KillConnection(killed.ConnectionId), "Port Scan is killable while Connecting");
            host.StepSimulation(20f);
            Check(host.Conditions.Count == 0, "interrupted Port Scan cannot deliver");
            ConnectionSpawnPlan scan = activities.QueuePortScan("DB");
            Check(scan.IsHostile && scan.Operations.Length == 1 &&
                scan.Operations[0].Operation == OperationKind.Read &&
                scan.Operations[0].EffectOnCompletion == CompletionEffect.ConditionApplication &&
                scan.Operations[0].ConditionPayload.Kind == ConditionKind.PortScanned &&
                scan.Operations[0].DesiredLoad == 8f && scan.Operations[0].TotalWork == 56f,
                "PORT SCAN concrete plan and payload");
            host.StepSimulation(.1f);
            host.StepSimulation(10f);
            Check(host.Conditions.Count == 1 &&
                host.Conditions[0].SourceConnectionId == scan.ConnectionId &&
                host.FindConnection(scan.ConnectionId).Resolution == ConnectionResolution.Completed,
                "WORK completion delivers persistent condition");
            Check(recorder.CurrentLog.events.Any(e => e.type == "condition_applied" &&
                e.sourceConnectionId == scan.ConnectionId) &&
                recorder.CurrentLog.events.Any(e => e.type == "plan_queued" &&
                e.connectionId == scan.ConnectionId &&
                e.definition.operations[0].conditionDurationSeconds == 20f),
                "recorder condition source and concrete plan");
            Check(host.TryInjectInfection("DB", InfectionKind.Blackout, 0f, out _),
                "Blackout can coexist with PortScanned");
            Check(host.StartScan("DB", "scan DB", out _) == ScanStartResult.Started,
                "diagnostic scan starts during PortScanned");
            host.StepSimulation(host.ScanDurationSeconds);
            presenter.RenderNow(4f, 1f);
            Check(presenter.ViewFor(db.View).PainVisible &&
                presenter.ViewFor(db.View).AwarenessVisible && state.KnownInfected,
                "OFFLINE, known INFECTED and Awareness coexist");
            Check(host.StartClean("DB", "clean DB", out CleanSession clean) ==
                CleanStartResult.Started, "clean starts during PortScanned");
            int malicious = clean.Processes.First(p => p.CpuPercent >= 82).Pid;
            Check(host.SelectCleanPid(malicious, out _) == CleanSelectionResult.Success &&
                host.Conditions.Count == 1, "clean leaves Condition active");
            Check(host.StartScan("DB", "scan DB", out _) == ScanStartResult.Started,
                "clean diagnostic starts while Condition remains");
            host.StepSimulation(host.ScanDurationSeconds);
            presenter.RenderNow(Time.unscaledTime, 1f);
            Check(presenter.ViewFor(db.View).AwarenessVisible &&
                presenter.ViewFor(db.View).BenefitVisible && host.Conditions.Count == 1,
                "PORT SCANNED and transient CLEAN presentation coexist");
            host.ResetSimulation();
            Check(host.Conditions.Count == 0, "reset clears conditions");

            traffic.AutoGenerationEnabled = false;
            activities.FollowupChance = 0f;
            ConnectionSpawnPlan noFollowup = activities.QueueReconnaissance("DB");
            host.StepSimulation(.1f);
            Check(host.KillConnection(noFollowup.ConnectionId), "explicit recon scan kill");
            host.StepSimulation(10f);
            host.StepSimulation(.1f);
            Check(host.RuntimeConnections.All(c => c.ConnectionId != noFollowup.ConnectionId || c.IsComplete),
                "no-followup event does not launch attack");
            activities.FollowupChance = 1f;
            ConnectionSpawnPlan followupScan = activities.QueueReconnaissance("DB");
            host.StepSimulation(.1f);
            Check(host.KillConnection(followupScan.ConnectionId), "followup scan can be interrupted");
            host.StepSimulation(8f);
            host.StepSimulation(.1f);
            Check(host.RuntimeConnections.Any(c => c.IsHostile &&
                c.ConnectionId != followupScan.ConnectionId && c.TargetNode.NodeId == "DB" &&
                Enumerable.Range(0, c.Definition.OperationCount).Any(i =>
                    c.Definition.EffectAt(i) == CompletionEffect.InfectionDelivery)),
                "preselected followup launches real target-matched INFECTOR after scan kill");
            activities.OrdinaryWeight = 2f;
            activities.ReconnaissanceWeight = 1f;
            activities.PayloadWeight = 0f;
            activities.WormWeight = 0f;
            activities.FollowupChance = .4f;
            host.ResetSimulation();
            ConnectionSpawnPlan baselineBenign = traffic.GenerateBenignPlan(0f);
            string benignSignature = PlanSignature(baselineBenign);
            host.ResetSimulation();
            string[] sequence = Enumerable.Range(0, 24)
                .Select(i => PlanSignature(activities.QueueAmbientActivity(i).ConnectionPlan)).ToArray();
            Check(sequence.Any(s => s.Contains("port-scan")) &&
                sequence.Any(s => !s.Contains("port-scan")),
                "ambient opportunity selects both activity types");
            Check(PlanSignature(traffic.GenerateBenignPlan(0f)) == benignSignature,
                "activity decisions leave seeded benign generation independent");
            host.ResetSimulation();
            string[] replay = Enumerable.Range(0, 24)
                .Select(i => PlanSignature(activities.QueueAmbientActivity(i).ConnectionPlan)).ToArray();
            Check(sequence.SequenceEqual(replay), "seeded activity and concrete plans replay");
            activities.OrdinaryWeight = 0f;
            activities.ReconnaissanceWeight = 0f;
            Check(!activities.QueueAmbientActivity(25f).Succeeded,
                "zero activity weights disable ambient hostile plans");
            string firstFollowups = ReplayFollowups(host, traffic, activities, recorder);
            string secondFollowups = ReplayFollowups(host, traffic, activities, recorder);
            Check(firstFollowups == secondFollowups,
                "seeded reconnaissance followup decisions and schedule replay");
            Debug.Log("ConditionWitness PASS: both scenes, lifecycle, Port Scan delivery and kill, " +
                "Awareness timer, clean coexistence, recording, recon followup and seeded replay.");
        }

        private static string ReplayFollowups(SimulationHost host,
            ConnectionTrafficDirector traffic, HostileActivityDirector activities,
            ConnectionsRunRecorder recorder)
        {
            traffic.AutoGenerationEnabled = false;
            activities.FollowupChance = .4f;
            host.ResetSimulation();
            recorder.BeginRun();
            string[] scanIds = Enumerable.Range(0, 12)
                .Select(_ => activities.QueueReconnaissance("DB").ConnectionId).ToArray();
            host.StepSimulation(.1f);
            foreach (string id in scanIds)
                Check(host.KillConnection(id), "replay scan is interruptible");
            host.StepSimulation(8f);
            host.StepSimulation(.1f);
            var evidence = recorder.CurrentLog.events.Where(e =>
                e.type == "recon_planned" || e.type == "recon_scan_resolved" ||
                e.type == "recon_followup_scheduled" ||
                e.type == "recon_followup_queued").ToArray();
            Check(evidence.Count(e => e.type == "recon_followup_queued") > 0 &&
                evidence.Count(e => e.type == "recon_followup_queued") ==
                evidence.Count(e => e.type == "recon_followup_scheduled"),
                "preselected followups queue after the same recorded delay");
            return string.Join("|", evidence.Select(e => e.type + ":" +
                e.hostileEventId + ":" + e.targetNodeId + ":" +
                e.scanConnectionId + ":" + e.followupSelected + ":" +
                e.followupAtSeconds.ToString("R") + ":" +
                e.followupConnectionId + ":" + e.reason));
        }

        private static string PlanSignature(ConnectionSpawnPlan plan) => plan == null ?
            "none" : plan.ProfileId + ":" + plan.Target.NodeId + ":" +
            plan.HostileEventId + ":" + string.Join(",", plan.Operations.Select(o =>
                o.Operation + "/" + o.TotalWork.ToString("R") + "/" +
                o.DesiredLoad.ToString("R") + "/" + o.EffectOnCompletion));

        private static void CheckScene(string path, int nodeCount, float mean, float reconWeight)
        {
            EditorSceneManager.OpenScene(path);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var activities = UnityEngine.Object.FindFirstObjectByType<HostileActivityDirector>();
            Check(host != null && traffic != null && activities != null &&
                host.Nodes.Length == nodeCount && traffic.HostileMeanIntervalSeconds == mean &&
                activities.ReconnaissanceWeight == reconWeight &&
                activities.PortScannedSeconds == 20f, path + " authored configuration");
            Check(new SerializedObject(traffic).FindProperty("hostileActivities")
                .objectReferenceValue == activities &&
                new SerializedObject(activities).FindProperty("host")
                    .objectReferenceValue == host &&
                new SerializedObject(activities).FindProperty("traffic")
                    .objectReferenceValue == traffic,
                path + " serialized activity bindings");
        }

        private static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException("ConditionWitness: " + message);
        }
    }
}
