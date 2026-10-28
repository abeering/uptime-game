using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class ScanWitness
    {
        [MenuItem("Tools/Connections/Run Scan Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            Check(host != null && director != null && recorder != null && terminal != null,
                "scene services");
            CheckPrefabAndScene(host, terminal);
            bool previousAuto = director.AutoGenerationEnabled;
            bool previousRecording = recorder.RecordingEnabled;
            bool previousHostiles = director.HostileGenerationEnabled;
            int previousSeed = director.Seed;
            try
            {
                director.AutoGenerationEnabled = false;
                director.HostileGenerationEnabled = false;
                director.Seed = 1701;
                recorder.RecordingEnabled = true;
                host.SetRunMode(ConnectionRunMode.Generated);
                HealthyAndSlot(host, director, recorder);
                InfectionChanges(host, director, recorder);
                DeliveryBoundary(host, director, recorder);
                ResetAndRecorder(host, director, recorder);
                Debug.Log("ScanWitness PASS: clean/infected, delivery/removal, multiple kinds, " +
                    "Blackout, contention, exact/coarse deadline, clean overlap, reset, " +
                    "recorder JSON, prefab and scene bindings.");
            }
            finally
            {
                recorder.EndRun();
                recorder.RecordingEnabled = previousRecording;
                director.AutoGenerationEnabled = previousAuto;
                director.HostileGenerationEnabled = previousHostiles;
                director.Seed = previousSeed;
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static void HealthyAndSlot(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            Fresh(host, recorder);
            float health = host.Health.Value;
            float load = Node(host, "DB").RequestedLoad;
            int plans = Plans(recorder);
            int completions = 0;
            void Completed(NodeScanSession _) => completions++;
            host.ScanCompleted += Completed;
            try
            {
                Check(host.StartScan("missing", "scan missing", out _) ==
                    ScanStartResult.UnknownNode && host.ActiveScan == null,
                    "unknown node does not acquire slot");
                Check(host.StartScan("db", "scan db", out NodeScanSession first) ==
                    ScanStartResult.Started && first.NodeId == "DB" &&
                    first.ScheduledCompletionAtSeconds == 8f, "healthy scan starts");
                Check(host.StartScan("DB", "scan DB", out _) ==
                    ScanStartResult.AlreadyScanning &&
                    host.StartScan("API", "scan API", out _) == ScanStartResult.Busy &&
                    ReferenceEquals(host.ActiveScan, first) &&
                    first.ScheduledCompletionAtSeconds == 8f,
                    "duplicate and busy requests leave first scan untouched");
                var ui = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Connections/ScanOutput.prefab")).GetComponent<ScanOutput>();
                try
                {
                    ui.Bind(first, host);
                    host.StepSimulation(2f);
                    typeof(ScanOutput).GetMethod("Refresh", BindingFlags.Instance |
                        BindingFlags.NonPublic).Invoke(ui, null);
                    var fill = new SerializedObject(ui).FindProperty("progressFill")
                        .objectReferenceValue as Image;
                    Check(Mathf.Abs(fill.rectTransform.rect.width - 130f) < .01f,
                        "progress bar uses authoritative quarter-duration width");
                }
                finally { UnityEngine.Object.DestroyImmediate(ui.gameObject); }
                host.StepSimulation(5.9f);
                Check(!first.IsComplete && first.ProgressAt(host.SimulationTimeSeconds) > .98f &&
                    completions == 0, "simulation-time progress before deadline");
                host.StepSimulation(.1f);
                Check(first.IsComplete && first.Result == NodeScanResult.Clean &&
                    first.CompletedAtSeconds == 8f && host.ActiveScan == null &&
                    completions == 1 && first.ProgressAt(100f) == 1f,
                    "exact boundary and one completion notification");
                host.StepSimulation(5f);
                Check(completions == 1 && host.Health.Value == health &&
                    Node(host, "DB").RequestedLoad == load && Plans(recorder) == plans,
                    "no repeat notification or Health/LOAD/generation side effects");
                Check(host.StartScan("API", "scan API", out NodeScanSession second) ==
                    ScanStartResult.Started && second.Id != first.Id,
                    "slot is reusable; completed card state remains available");
            }
            finally { host.ScanCompleted -= Completed; }
        }

        private static void InfectionChanges(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            Fresh(host, recorder);
            Check(host.StartScan("DB", "scan DB", out NodeScanSession applied) ==
                ScanStartResult.Started, "start before injection");
            host.StepSimulation(2f);
            Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 3f, out _),
                "infection applied during scan");
            host.StepSimulation(6f);
            Check(applied.Result == NodeScanResult.Infected,
                "completion samples infection applied during scan");

            Fresh(host, recorder);
            Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 3f, out _),
                "inject before scan");
            Check(host.StartScan("DB", "scan DB", out NodeScanSession removed) ==
                ScanStartResult.Started, "start on infected node");
            host.StepSimulation(2f);
            Check(host.TryRemoveInfection("DB", InfectionKind.PhantomLoad, out _),
                "infection removed during scan");
            host.StepSimulation(6f);
            Check(removed.Result == NodeScanResult.Clean,
                "completion samples removal, not starting infection");

            Fresh(host, recorder);
            Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 3f, out _) &&
                host.TryInjectInfection("DB", InfectionKind.Blackout, 0f, out _) &&
                host.Infections.Count == 2, "multiple infection kinds on offline node");
            float health = host.Health.Value;
            Check(host.StartScan("DB", "scan DB", out NodeScanSession offline) ==
                ScanStartResult.Started, "offline node can be scanned");
            Check(host.StartClean("DB", "clean DB", out CleanSession cleaning) ==
                CleanStartResult.Started, "clean can overlap scan");
            host.StepSimulation(8f);
            Check(offline.Result == NodeScanResult.Infected &&
                ReferenceEquals(host.ActiveClean, cleaning) &&
                host.Health.Value == health && host.Infections.Count == 2,
                "scan completes during clean without changing infections or Health");
        }

        private static void DeliveryBoundary(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            Fresh(host, recorder);
            Check(host.StartScan("DB", "scan DB", out NodeScanSession during) ==
                ScanStartResult.Started, "start delivery witness scan");
            QueueDelivery(host, director, "scan-deliver", 1f);
            host.StepSimulation(3f);
            Check(host.Infections.Count == 1 && !during.IsComplete,
                "operation completion delivers during scan");
            host.StepSimulation(9f);
            Check(during.Result == NodeScanResult.Infected,
                "delivered infection observed at deadline");

            Fresh(host, recorder);
            Check(host.StartScan("DB", "scan DB", out NodeScanSession before) ==
                ScanStartResult.Started, "start coarse-step witness scan");
            QueueDelivery(host, director, "scan-later", 9f);
            host.StepSimulation(12f);
            Check(before.Result == NodeScanResult.Clean &&
                before.CompletedAtSeconds == 8f && host.Infections.Count == 1,
                "coarse step samples at deadline before later delivery");
        }

        private static void ResetAndRecorder(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            Fresh(host, recorder);
            Check(host.StartScan("DB", "scan DB", out NodeScanSession pending) ==
                ScanStartResult.Started, "start pending scan");
            host.StepSimulation(1f);
            host.ResetSimulation();
            Check(pending.WasReset && !pending.IsComplete && host.ActiveScan == null,
                "reset clears slot and freezes old card");
            host.StepSimulation(10f);
            Check(!pending.IsComplete, "reset cannot emit phantom completion");
            recorder.BeginRun();
            Check(host.StartScan("DB", "scan DB", out NodeScanSession session) ==
                ScanStartResult.Started, "recorded scan starts");
            host.StepSimulation(8f);
            RunEvent started = recorder.CurrentLog.events.Single(e => e.type == "scan_started");
            RunEvent completed = recorder.CurrentLog.events.Single(e => e.type == "scan_completed");
            RunEvent action = recorder.CurrentLog.events.Single(e =>
                e.type == "player_action" && e.reason == "scan_started");
            Check(started.actionSequence == action.sequence &&
                completed.actionSequence == action.sequence &&
                started.scanSessionId == session.Id &&
                completed.scanSessionId == session.Id &&
                started.targetNodeId == "DB" && completed.targetNodeId == "DB" &&
                started.scanStartedAtSeconds == session.StartedAtSeconds &&
                started.scanScheduledCompletionAtSeconds == session.ScheduledCompletionAtSeconds &&
                completed.scanCompletedAtSeconds == session.CompletedAtSeconds &&
                completed.scanResult == "CLEAN", "linked recorder events");
            Check(recorder.Save(false), "scan log saves");
            ConnectionsRunLog saved = JsonUtility.FromJson<ConnectionsRunLog>(
                File.ReadAllText(recorder.OutputPath));
            Check(saved.run.scanDurationSeconds == 8f &&
                saved.events.Count(e => e.type == "scan_started") == 1 &&
                saved.events.Count(e => e.type == "scan_completed") == 1 &&
                saved.events.Single(e => e.type == "scan_completed").scanResult == "CLEAN",
                "scan fields survive JSON persistence");
        }

        private static void CheckPrefabAndScene(SimulationHost host, Terminal terminal)
        {
            var hostData = new SerializedObject(host);
            Check(Mathf.Abs(hostData.FindProperty("scanDurationSeconds").floatValue - 8f) < .001f,
                "scene scan duration");
            var terminalData = new SerializedObject(terminal);
            var prefab = terminalData.FindProperty("scanPrefab").objectReferenceValue as ScanOutput;
            Check(prefab != null && AssetDatabase.GetAssetPath(prefab) ==
                "Assets/Prefabs/Connections/ScanOutput.prefab", "scene scan prefab binding");
            var data = new SerializedObject(prefab);
            foreach (string field in new[] { "card", "layout", "nodePill", "nodeIdText",
                "statusText", "timeText", "progressFill", "accentRail" })
                Check(data.FindProperty(field).objectReferenceValue != null,
                    "prefab binding " + field);
            var pill = data.FindProperty("nodePill").objectReferenceValue as PillGraphic;
            var card = data.FindProperty("card").objectReferenceValue as RectTransform;
            var fill = data.FindProperty("progressFill").objectReferenceValue as Image;
            Check(pill.material != null && AssetDatabase.GetAssetPath(pill.material) ==
                "Assets/Shaders/ConnectionsPill.mat" &&
                pill.rectTransform.rect.width >= 100f && pill.rectTransform.rect.height >= 60f &&
                card.rect.width >= 500f && fill.type == Image.Type.Simple,
                "inspect-style large pill, antialiased material, compact full-width progress");
            Check(prefab.GetComponent<LayoutElement>().preferredHeight <= 170f &&
                prefab.GetComponent<RectTransform>().localScale == Vector3.one,
                "compact unscaled terminal block");
        }

        private static void QueueDelivery(SimulationHost host,
            ConnectionTrafficDirector director, string id, float work)
        {
            var route = new[] { Node(host, "EXT1").View, Node(host, "API").View,
                Node(host, "DB").View };
            var stage = new OperationStage(OperationKind.Write, work, 1f, "Scan witness",
                CompletionEffect.InfectionDelivery, null,
                new InfectionPayload(InfectionKind.PhantomLoad, 3f));
            director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, id, route,
                "scan-witness", "Scan witness", new[] { stage }, true));
        }

        private static void Fresh(SimulationHost host, ConnectionsRunRecorder recorder)
        {
            host.ResetSimulation();
            recorder.BeginRun();
        }

        private static int Plans(ConnectionsRunRecorder recorder) =>
            recorder.CurrentLog.events.Count(e => e.type == "plan_queued");

        private static NodeResources Node(SimulationHost host, string id) =>
            host.Nodes.Single(n => n.View.NodeId == id);

        private static void Check(bool condition, string description)
        {
            if (!condition) throw new Exception("ScanWitness FAILED: " + description);
        }
    }
}
