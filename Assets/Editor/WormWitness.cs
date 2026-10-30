using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class WormWitness
    {
        [MenuItem("Tools/Connections/Run Worm Witness")]
        public static void Run()
        {
            CheckScene("Assets/Scenes/Connections.unity", 4);
            CheckScene("Assets/Scenes/ConnectionsAdvanced.unity", 8);
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var paths = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
            var presentation = UnityEngine.Object.FindFirstObjectByType<WormPresenter>();
            var status = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            traffic.AutoGenerationEnabled = false;
            traffic.RandomizeSeed = false;
            host.SetRunMode(ConnectionRunMode.Generated);
            recorder.BeginRun();
            var tuning = new WormTuning { propagationProbability = 1f, maximumNodes = 8,
                establishSeconds = 1f, travelSeconds = 4f, actionSeconds = 2f,
                conditionSeconds = 6f };

            Check(host.StartScan("API", "scan API", out _) == ScanStartResult.Started,
                "scan starts before jamming");
            WormOutbreak outbreak = host.Worms.Spawn("API", WormEffect.ScanJammed, tuning);
            Check(outbreak.Id == "w1" && outbreak.Branches.Count == 1 &&
                host.ConditionFor(Node(host, "API"), ConditionKind.ScanJammed)
                    .SourceWormBranchId == "w1.1" &&
                host.StartScan("API", "scan API", out _) == ScanStartResult.Jammed,
                "origin effect, provenance and new scan rejection");
            paths.Bind(host.RuntimeConnections.ToArray());
            host.StepSimulation(2f);
            Check(outbreak.Branches.Count >= 3 && outbreak.Branches.Skip(1)
                .All(b => b.Phase == WormPhase.Establishing && b.ParentId == "w1.1"),
                "API produces simultaneous child threads");
            recorder.CaptureSnapshot(host.SimulationTimeSeconds);
            Check(recorder.CurrentLog.snapshots.Last().worms.Length >= 3 &&
                recorder.CurrentLog.snapshots.Last().worms.Any(w =>
                    w.parentBranchId == "w1.1" && w.phase == "Establishing"),
                "snapshot retains active branch lineage and phase");
            WormBranch db = outbreak.Branches.Single(b => b.NextNode == Node(host, "DB"));
            paths.RefreshLayouts();
            presentation.RenderNow();
            Check(presentation.TryGetVisualSegment(db, out _, out _, out float reveal) &&
                reveal <= .001f, "authored presenter draws an unrevealed establishing thread");
            host.StepSimulation(.5f);
            paths.RefreshLayouts();
            presentation.RenderNow();
            Check(db.Phase == WormPhase.Establishing && db.Progress > .49f &&
                presentation.TryGetVisualSegment(db, out _, out _, out reveal) &&
                reveal > .49f && reveal < .51f, "thread grows before body emergence");
            host.StepSimulation(.5f);
            host.StepSimulation(3.9f);
            Check(db.Phase == WormPhase.Traversing &&
                host.ConditionFor(Node(host, "DB"), ConditionKind.ScanJammed) == null,
                "effect is withheld during traversal");
            host.StepSimulation(.1f);
            Check(db.Phase == WormPhase.Entering &&
                host.ConditionFor(Node(host, "DB"), ConditionKind.ScanJammed) == null,
                "head entry is distinct from full body entry");
            presentation.RenderNow();
            WormThreadGraphic graphic = UnityEngine.Object.FindObjectsByType<WormThreadGraphic>(
                FindObjectsSortMode.None).Single(g => g.name == "Worm " + db.Id + " thread");
            Check(graphic.Head == 1f && graphic.Tail < 1f,
                "head disappears while tail remains on the thread");
            host.StepSimulation(.49f);
            Check(db.Phase == WormPhase.Entering &&
                host.ConditionFor(Node(host, "DB"), ConditionKind.ScanJammed) == null,
                "effect waits for tail");
            host.StepSimulation(.01f);
            Check(db.Phase == WormPhase.Acting &&
                host.ConditionFor(Node(host, "DB"), ConditionKind.ScanJammed)
                    .SourceWormBranchId == db.Id, "full entry applies effect");
            status.RenderNow(1f, 1f);
            Check(status.StateFor(Node(host, "DB")).ScanJammed &&
                status.ViewFor(Node(host, "DB")).PainVisible,
                "jammed condition has Pain presentation");
            Check(recorder.CurrentLog.events.Any(e => e.type == "worm_head_entered" &&
                e.wormBranchId == db.Id) &&
                recorder.CurrentLog.events.Any(e => e.type == "condition_applied" &&
                e.sourceWormBranchId == db.Id), "head and effect provenance recorded");

            host.StepSimulation(45f);
            Check(outbreak.IsComplete && outbreak.ClaimedNodes.Count == 8 &&
                outbreak.Branches.Count == 8 &&
                outbreak.Branches.Select(b => b.CurrentNode).Distinct().Count() == 8,
                "coarse step reaches every node once on tree topology");
            Check(host.ActiveScan == null && recorder.CurrentLog.events.Any(e =>
                e.type == "scan_completed" && e.targetNodeId == "API"),
                "scan started before jamming completes normally");
            string decisions = string.Join("|", recorder.CurrentLog.events
                .Where(e => e.type == "worm_propagation_decision")
                .Select(e => e.wormBranchId + ":" + e.wormNextNodeId + ":" +
                    e.wormPropagationSelected));
            host.ResetSimulation();
            recorder.BeginRun();
            WormOutbreak replay = host.Worms.Spawn("API", WormEffect.ScanJammed, tuning);
            host.StepSimulation(45f);
            string replayDecisions = string.Join("|", recorder.CurrentLog.events
                .Where(e => e.type == "worm_propagation_decision")
                .Select(e => e.wormBranchId + ":" + e.wormNextNodeId + ":" +
                    e.wormPropagationSelected));
            Check(replay.IsComplete && decisions == replayDecisions,
                "seeded replay and branch ordering");

            host.ResetSimulation();
            var partial = tuning.Copy();
            partial.maximumNodes = 2;
            var limited = host.Worms.Spawn("API", WormEffect.PortScanned, partial);
            host.StepSimulation(45f);
            Check(limited.IsComplete && limited.ClaimedNodes.Count == 2 &&
                host.ConditionFor(Node(host, "API"), ConditionKind.ScanJammed) == null,
                "node budget limits spread and effect selection is independent");
            host.ResetSimulation();
            var stopped = tuning.Copy(); stopped.propagationProbability = 0f;
            Check(host.Worms.Spawn("API", WormEffect.PortScanned, stopped).Branches.Count == 1,
                "zero propagation still creates outbreak");
            host.StepSimulation(45f);
            Check(host.Worms.Outbreaks[0].ClaimedNodes.Count == 1,
                "probability can produce partial outbreak");

            // Add one temporary chord to turn EXT1-WEB-API into a cycle.
            host.ResetSimulation();
            var linkField = typeof(SimulationHost).GetField("physicalLinks",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var authoredLinks = host.PhysicalLinks;
            var chordObject = new GameObject("Worm witness cycle", typeof(PhysicalLinkView));
            try
            {
                var chord = chordObject.GetComponent<PhysicalLinkView>();
                chord.ConfigureStressLink("EZ", Node(host, "EXT1"), Node(host, "API"),
                    null, null, null);
                linkField.SetValue(host, authoredLinks.Concat(new[] {
                    new PhysicalLinkReference(chord) }).ToArray());
                var cycle = host.Worms.Spawn("API", WormEffect.PortScanned, tuning);
                host.StepSimulation(45f);
                Check(cycle.IsComplete && cycle.ClaimedNodes.Count == 8 &&
                    cycle.Branches.Count == 8,
                    "cycle converges without duplicate visits or unbounded branches");
            }
            finally
            {
                linkField.SetValue(host, authoredLinks);
                UnityEngine.Object.DestroyImmediate(chordObject);
            }

            host.ResetSimulation();
            recorder.BeginRun();
            var resident = host.Worms.Spawn("API", WormEffect.ScanJammed, tuning);
            Check(host.Worms.TryIsolate("API", "isolate API", out _) &&
                resident.IsComplete && resident.Branches.Count == 1 &&
                host.IsNodeOffline(Node(host, "API")) &&
                host.ConditionFor(Node(host, "API"), ConditionKind.ScanJammed) != null,
                "isolation kills resident but retains applied Condition");
            Check(recorder.CurrentLog.snapshots.Last().isolations.Any(i =>
                i.nodeId == "API") &&
                recorder.CurrentLog.snapshots.Last().conditions.Any(c =>
                    c.sourceWormBranchId == "w1.1"),
                "snapshot keeps active isolation and delivered Condition provenance");
            status.RenderNow(2f, 1f);
            var apiStatus = status.StateFor(Node(host, "API"));
            apiStatus.ScanResult(NodeScanResult.Infected, 2f, 2.5f);
            status.RenderNow(2.1f, 1f);
            Check(apiStatus.Offline && apiStatus.Isolated && apiStatus.ScanJammed &&
                status.ViewFor(Node(host, "API")).PainVisible &&
                status.ViewFor(Node(host, "API")).BenefitVisible &&
                status.ViewFor(Node(host, "API"))
                    .GetComponentsInChildren<NodeStatusPanel>(true)
                    .Any(p => p.Height == 84f),
                "OFFLINE, INFECTED and SCAN JAMMED fit one Pain stack with ISOLATED benefit");
            Check(host.TryInjectInfection("API", InfectionKind.PhantomLoad, 2f, out _) &&
                host.StartClean("API", "clean API", out CleanSession clean) ==
                    CleanStartResult.Started &&
                host.SelectCleanPid(clean.Processes.Single(p => p.CpuPercent >= 82).Pid,
                    out int removed) == CleanSelectionResult.Success && removed == 1 &&
                host.ConditionFor(Node(host, "API"), ConditionKind.ScanJammed) != null &&
                host.Worms.IsNodeIsolated(Node(host, "API")),
                "clean removes infection without erasing Worm Condition or isolation");
            Check(!host.Worms.TryIsolate("API", "isolate API", out _),
                "overlap rejected");
            host.StepSimulation(8f);
            Check(!host.Worms.IsNodeIsolated(Node(host, "API")) &&
                !host.IsNodeOffline(Node(host, "API")) &&
                !host.Worms.TryIsolate("API", "isolate API", out _),
                "availability restores but cooldown remains");
            host.StepSimulation(22f);
            Check(host.Worms.TryIsolate("API", "isolate API", out _),
                "cooldown begins at activation");

            host.ResetSimulation();
            var incoming = host.Worms.Spawn("API", WormEffect.PortScanned, tuning);
            host.StepSimulation(3f);
            WormBranch nextDb = incoming.Branches.Single(b => b.NextNode == Node(host, "DB"));
            Check(host.Worms.TryIsolate("DB", "isolate DB", out _),
                "isolate destination before arrival");
            host.StepSimulation(5f);
            Check(nextDb.Phase == WormPhase.Resolved &&
                nextDb.TerminationReason == "isolated_entry" &&
                host.ConditionFor(Node(host, "DB"), ConditionKind.PortScanned) == null &&
                incoming.Branches.Any(b => b.NextNode != Node(host, "DB") &&
                    b.Phase != WormPhase.Resolved),
                "isolated entry stops before effect, siblings continue");

            host.ResetSimulation();
            recorder.BeginRun();
            var benign = Add(host, "service", "DB", false);
            var hostile = Add(host, "threat", "DB", true);
            float healthBefore = host.Health.Value;
            Check(host.Worms.TryIsolate("DB", "isolate DB", out _) &&
                benign.Resolution == ConnectionResolution.Canceled &&
                benign.TerminationCause == ConnectionTerminationCause.Isolation &&
                hostile.Resolution == ConnectionResolution.Canceled &&
                hostile.TerminationCause == ConnectionTerminationCause.Isolation &&
                Math.Abs(host.Health.Value - (healthBefore - 5f)) < .001f &&
                host.Worms.Isolations.Single().ConnectionsCanceled == 2,
                "isolation cancels benign and hostile service with existing Health cost once");
            Check(recorder.CurrentLog.events.Any(e => e.type == "isolation_activated" &&
                e.connectionsCanceled == 2),
                "isolation disruptions recorded");

            host.ResetSimulation();
            Check(host.TryInjectInfection("DB", InfectionKind.Blackout, 0f, out _) &&
                host.Worms.TryIsolate("DB", "isolate DB", out _) &&
                host.TryRemoveInfection("DB", InfectionKind.Blackout, out _) &&
                host.IsNodeOffline(Node(host, "DB")),
                "cleaning Blackout does not clear active isolation");
            host.StepSimulation(8f);
            Check(!host.IsNodeOffline(Node(host, "DB")),
                "independent availability reasons both expire or clear");

            host.ResetSimulation();
            var payload = host.Payloads.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", new[] { 1f, 1f, 1f });
            Check(host.Worms.TryIsolate("DB", "isolate DB", out _),
                "Payload coexistence setup");
            host.StepSimulation(6.6f);
            Check(payload.Phase == PayloadPhase.Resolved && payload.EffectApplied &&
                !payload.Intercepted,
                "isolation does not silently intercept or destroy Payloads");
            host.ResetSimulation();
            Check(host.Worms.Outbreaks.Count == 0 && host.Worms.Isolations.Count == 0 &&
                host.Conditions.Count == 0, "reset clears outbreaks, isolation and Conditions");
            Check(Terminal.TryParseWormEffect("scan-jammed", out WormEffect jam) &&
                jam == WormEffect.ScanJammed &&
                Terminal.TryParseWormEffect("port-scanned", out WormEffect port) &&
                port == WormEffect.PortScanned &&
                !Terminal.TryParseWormEffect("infection", out _),
                "developer command accepts only the two selected effects");
            Debug.Log("WormWitness PASS: phase timing, thread/body, effects, branch coverage, replay, containment, service cost, status and reset.");
        }

        private static NodeView Node(SimulationHost host, string id) =>
            host.FindNodeResources(id).View;

        private static LogicalConnectionState Add(SimulationHost host, string id,
            string target, bool hostile)
        {
            var state = host.RegisterConnection(
                LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(
                    host.SimulationTimeSeconds, id,
                    new[] { Node(host, "EXT1"), Node(host, "WEB"), Node(host, "API"),
                        Node(host, target) }, "worm-witness", "Service",
                    new[] { new OperationStage(OperationKind.Read, 1000f, 1f, "Service") },
                    hostile)));
            typeof(LogicalConnectionState).GetProperty("Lifecycle").SetValue(state,
                Lifecycle.Active);
            typeof(LogicalConnectionState).GetProperty("HasEstablished").SetValue(state, true);
            typeof(LogicalConnectionState).GetProperty("EstablishedAtSeconds").SetValue(state,
                host.SimulationTimeSeconds);
            ((NetworkHealth)typeof(SimulationHost).GetField("health",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(host))
                .Activate(state, host.SimulationTimeSeconds);
            return state;
        }

        private static void CheckScene(string path, int nodeCount)
        {
            EditorSceneManager.OpenScene(path);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<WormDirector>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<WormPresenter>();
            var paths = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
            Check(host != null && host.Nodes.Length == nodeCount &&
                host.Worms == director && director != null && presenter != null &&
                new SerializedObject(paths).FindProperty("wormDirector")
                    .objectReferenceValue == director &&
                new SerializedObject(paths).FindProperty("wormPresenter")
                    .objectReferenceValue == presenter,
                path + " authored Worm bindings");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("WormWitness: " + message);
        }
    }
}
