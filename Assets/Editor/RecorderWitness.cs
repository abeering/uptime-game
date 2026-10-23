using System;
using System.IO;
using System.Linq;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class RecorderWitness
    {
        [MenuItem("Tools/Connections/Run Recorder Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && director != null && recorder != null && host.Recorder == recorder,
                "scene recorder binding");
            bool oldAuto = director.AutoGenerationEnabled;
            bool oldRecording = recorder.RecordingEnabled;
            try
            {
                director.AutoGenerationEnabled = false;
                recorder.RecordingEnabled = true;
                host.SetRunMode(ConnectionRunMode.Generated);
                recorder.BeginRun(); // Edit Mode witness; Play Mode calls this automatically from host reset.
                recorder.CaptureSnapshot(host.SimulationTimeSeconds);
                string firstPath = recorder.OutputPath;
                Check(File.Exists(firstPath) && firstPath.StartsWith(ConnectionsRunRecorder.LogDirectory),
                    "new recording is saved in the expected directory");
                Check(Path.GetFileName(firstPath).StartsWith("uptime_") &&
                    Path.GetFileName(firstPath).Contains("_seed-" + director.Seed.ToString("00000") + "_"),
                    "stable timestamped seed filename");
                ConnectionsRunLog initial = Read(firstPath);
                Check(initial.schemaVersion == 1 && !string.IsNullOrEmpty(initial.run.runId) &&
                    !string.IsNullOrEmpty(initial.run.startedUtc) &&
                    initial.run.seed == director.Seed && initial.run.mode == "Generated" &&
                    initial.run.nodes.Length == 4 && initial.run.traffic.profiles.Length == 10,
                    "copied run metadata and generator configuration");
                ProfileConfiguration threatProfile = initial.run.traffic.profiles.Single(p =>
                    p.id == "basic-threat");
                Check(threatProfile.isHostile && threatProfile.ambientWeight == 0f &&
                    threatProfile.operations[0].completionEffect == "InfectionDelivery",
                    "recorded catalog includes manual-only threat behavior");
                ProfileConfiguration delayedProfile = initial.run.traffic.profiles.Single(p =>
                    p.id == "delayed-threat");
                Check(delayedProfile.isHostile && delayedProfile.ambientWeight == 0f &&
                    delayedProfile.operations.Length == 2 &&
                    delayedProfile.operations[0].kind == "Read" &&
                    delayedProfile.operations[0].completionEffect == "None" &&
                    delayedProfile.operations[1].kind == "Write" &&
                    delayedProfile.operations[1].completionEffect == "InfectionDelivery",
                    "recorded catalog scopes delayed delivery to WRITE");
                ProfileConfiguration spikingThreat = initial.run.traffic.profiles.Single(p =>
                    p.id == "spiking-threat");
                ProfileConfiguration spikingBenign = initial.run.traffic.profiles.Single(p =>
                    p.id == "spiking-benign");
                ProfileConfiguration overloadDemo = initial.run.traffic.profiles.Single(p =>
                    p.id == "overload-demo");
                Check(spikingThreat.isHostile && !spikingBenign.isHostile &&
                    !overloadDemo.isHostile &&
                    spikingThreat.ambientWeight == 0f &&
                    spikingBenign.ambientWeight == 0f &&
                    overloadDemo.ambientWeight == 0f &&
                    spikingThreat.operations[0].behaviors[0].kind == "Spiking" &&
                    spikingBenign.operations[0].behaviors[0].kind == "Spiking" &&
                    overloadDemo.operations[0].behaviors[0].kind == "Overload",
                    "recorded manual keyword catalog keeps demand behavior separate from hostility");

                NodeView ext = Node(host, "EXT1");
                NodeView api = Node(host, "API");
                NodeView db = Node(host, "DB");
                var route = new[] { ext, api, db };
                var arc = new ConnectionSpawnPlan(0f, "rec1", route, "indexing", "Record Indexing",
                    new[] { new OperationStage(OperationKind.Write, 1f, 1f, "Write"),
                        new OperationStage(OperationKind.Read, 1f, 2f, "Read") });
                director.QueueSpawnPlan(arc);
                host.StepSimulation(0.5f);
                LogicalConnectionState live = host.FindConnection("rec1");
                Check(live.IsActive && live.CompletedWork == 0.5f, "recorded run retains gameplay progression");
                recorder.CaptureSnapshot(host.SimulationTimeSeconds);
                NetworkSnapshot liveSnapshot = recorder.CurrentLog.snapshots.Last();
                NodeSnapshot dbSnapshot = liveSnapshot.nodes.First(n => n.id == "DB");
                ConnectionSnapshot connectionSnapshot = liveSnapshot.connections.Single(c => c.id == "rec1");
                Near(dbSnapshot.requestedLoad, host.Nodes.First(n => n.View == db).RequestedLoad,
                    "authoritative requested LOAD snapshot");
                Near(dbSnapshot.allocatedLoad, host.Nodes.First(n => n.View == db).AllocatedLoad,
                    "authoritative allocated LOAD snapshot");
                Near(connectionSnapshot.completedWork, live.CompletedWork, "authoritative WORK snapshot");
                Check(recorder.Save(true) && Read(firstPath).snapshots.Any(s =>
                    s.connections.Any(c => c.id == "rec1" && c.lifecycle == "Active")),
                    "partway-through save is valid and includes live state");
                host.StepSimulation(0.5f);
                host.StepSimulation(0.5f);
                Check(live.IsComplete && host.RuntimeConnections.Count == 0, "arc completed and retired");
                Check(recorder.CurrentLog.events.Count(e => e.type == "operation_started" &&
                    e.connectionId == "rec1") == 2 &&
                    recorder.CurrentLog.events.Count(e => e.type == "operation_completed" &&
                    e.connectionId == "rec1") == 2,
                    "both operation boundaries recorded once");
                Check(recorder.CurrentLog.events.Where(e => e.type == "operation_completed" &&
                    e.connectionId == "rec1").Select(e => e.actualDurationSeconds)
                    .SequenceEqual(new[] { 1f, 0.5f }), "actual stage durations");
                RunEvent completion = recorder.CurrentLog.events.Single(e =>
                    e.type == "connection_resolved" && e.connectionId == "rec1");
                Check(completion.resolutionReason == "Completed" && completion.timeSeconds == 1.5f &&
                    completion.elapsedLifetimeSeconds == 1.5f,
                    "natural resolution and lifetime");
                RunEvent spawned = recorder.CurrentLog.events.Single(e =>
                    e.type == "connection_spawned" && e.connectionId == "rec1");
                Check(spawned.definition.route.SequenceEqual(new[] { "EXT1", "API", "DB" }) &&
                    spawned.definition.operations.Length == 2 &&
                    spawned.definition.operations[1].desiredLoad == 2f &&
                    spawned.definition.operations[1].totalWork == 1f,
                    "concrete spawned definition recorded");
                Check(recorder.CurrentLog.events.Select(e => e.sequence)
                    .SequenceEqual(Enumerable.Range(1, recorder.CurrentLog.events.Count)),
                    "strict event sequence");

                string note = "DB suddenly red with two connections";
                Check(recorder.Mark(note), "mark saved immediately");
                ConnectionsRunLog marked = Read(firstPath);
                RunEvent marker = marked.events.Last(e => e.type == "marker");
                Check(marker.note == note && marker.snapshotIndex >= 0 &&
                    marked.snapshots[marker.snapshotIndex].timeSeconds == host.SimulationTimeSeconds,
                    "exact note links immediate persisted snapshot");
                int savedEvents = marked.events.Count;
                int savedSnapshots = marked.snapshots.Count;
                Check(recorder.Save(true), "explicit save succeeded");
                ConnectionsRunLog repeated = Read(firstPath);
                Check(repeated.events.Count == savedEvents && repeated.snapshots.Count == savedSnapshots &&
                    recorder.OutputPath == firstPath, "repeat save updates one file without duplicate records");

                recorder.RecordPlayerAction("kill missing", "missing", false);
                recorder.SaveIfDue(Time.realtimeSinceStartupAsDouble + 31.0);
                ConnectionsRunLog autosaved = Read(firstPath);
                Check(autosaved.events.Count == savedEvents + 1 &&
                    autosaved.events.Last().command == "kill missing" &&
                    autosaved.events.Last().success == false,
                    "real-time autosave persists new events without advancing simulation");

                var pressure = new ConnectionSpawnPlan(host.SimulationTimeSeconds, "rec2", route,
                    "test", "Pressure", new[] { new OperationStage(OperationKind.Read, 100f, 10f, "Read") });
                director.QueueSpawnPlan(pressure);
                host.StepSimulation(0.1f);
                LogicalConnectionState stressed = host.FindConnection("rec2");
                float enabledWork = stressed.CompletedWork;
                float enabledLoad = stressed.AllocatedLoad;
                Check(host.KillConnection("rec2"), "kill active connection");
                Check(recorder.CurrentLog.events.Last(e => e.type == "connection_resolved")
                    .resolutionReason == "Killed", "killed resolution recorded separately");
                Check(recorder.CurrentLog.snapshots.Last().nodes.First(n => n.id == "DB")
                    .requestedLoad == 0f, "kill captures released LOAD immediately");
                ConnectionSpawnPlan threat = director.QueueProfile("basic-threat");
                Check(recorder.CurrentLog.events.Last(e => e.type == "plan_queued")
                    .source == "manual", "manual spawn records its separate producer");
                host.StepSimulation(24f);
                RunEvent[] threatEvents = recorder.CurrentLog.events.Where(e =>
                    e.connectionId == threat.ConnectionId &&
                    (e.type == "operation_completed" || e.type == "effect" ||
                        e.type == "connection_resolved")).ToArray();
                Check(threatEvents.Length == 3 &&
                    threatEvents[0].type == "operation_completed" &&
                    threatEvents[1].type == "effect" &&
                    threatEvents[2].type == "connection_resolved" &&
                    threatEvents.All(e => e.timeSeconds == host.SimulationTimeSeconds) &&
                    threatEvents[1].effectType == "InfectionDelivery" &&
                    threatEvents[1].operationIndex == 0 &&
                    threatEvents[1].targetNodeId == "DB",
                    "WORK completion records effect before final resolution at one step time");
                RunEvent threatSpawn = recorder.CurrentLog.events.Single(e =>
                    e.type == "connection_spawned" && e.connectionId == threat.ConnectionId);
                Check(threatSpawn.definition.isHostile &&
                    threatSpawn.definition.operations[0].completionEffect == "InfectionDelivery",
                    "concrete manual threat definition is recorded");
                ConnectionSpawnPlan delayed = director.QueueProfile("delayed-threat");
                host.StepSimulation(8f);
                Check(recorder.CurrentLog.events.Count(e => e.type == "operation_completed" &&
                    e.connectionId == delayed.ConnectionId && e.operationIndex == 0) == 1 &&
                    recorder.CurrentLog.events.All(e => e.connectionId != delayed.ConnectionId ||
                        e.type != "operation_started" || e.operationIndex == 0) &&
                    recorder.CurrentLog.events.All(e => e.type != "effect" ||
                        e.connectionId != delayed.ConnectionId),
                    "recorded READ completion has neither WRITE start nor delivery yet");
                host.StepSimulation(0.1f);
                host.StepSimulation(24f);
                RunEvent[] delayedEvents = recorder.CurrentLog.events.Where(e =>
                    e.connectionId == delayed.ConnectionId &&
                    (e.type == "operation_started" || e.type == "operation_completed" ||
                        e.type == "effect" || e.type == "connection_resolved")).ToArray();
                Check(delayedEvents.Length == 6 &&
                    delayedEvents.Select(e => e.type).SequenceEqual(new[]
                    {
                        "operation_started", "operation_completed", "operation_started",
                        "operation_completed", "effect", "connection_resolved"
                    }) &&
                    delayedEvents[1].operationIndex == 0 &&
                    delayedEvents[2].operationIndex == 1 &&
                    delayedEvents[2].timeSeconds == delayedEvents[1].timeSeconds &&
                    delayedEvents[3].operationIndex == 1 &&
                    delayedEvents[4].operationIndex == 1 &&
                    delayedEvents[4].effectType == "InfectionDelivery" &&
                    delayedEvents[4].targetNodeId == "DB" &&
                    delayedEvents[4].timeSeconds == delayedEvents[3].timeSeconds &&
                    delayedEvents[5].resolutionReason == "Completed",
                    "delayed arc records READ, next-step WRITE, one indexed delivery, then resolution");
                RunEvent delayedSpawn = recorder.CurrentLog.events.Single(e =>
                    e.type == "connection_spawned" && e.connectionId == delayed.ConnectionId);
                Check(delayedSpawn.definition.isHostile &&
                    delayedSpawn.definition.operations.Length == 2 &&
                    delayedSpawn.definition.operations[0].completionEffect == "None" &&
                    delayedSpawn.definition.operations[1].completionEffect == "InfectionDelivery",
                    "concrete delayed profile retains operation-scoped effect metadata");

                ConnectionSpawnPlan interrupted = director.QueueProfile("delayed-threat");
                host.StepSimulation(8f);
                host.StepSimulation(0.1f);
                Check(host.KillConnection(interrupted.ConnectionId),
                    "kill delayed threat during WRITE");
                Check(recorder.CurrentLog.events.Count(e => e.connectionId == interrupted.ConnectionId &&
                    e.type == "operation_completed" && e.operationIndex == 0) == 1 &&
                    recorder.CurrentLog.events.All(e => e.connectionId != interrupted.ConnectionId ||
                        e.type != "effect") &&
                    recorder.CurrentLog.events.Last(e => e.connectionId == interrupted.ConnectionId &&
                        e.type == "connection_resolved").operationIndex == 1,
                    "killed WRITE retains recorded READ completion without delivery");
                recorder.EndRun();
                Check(!string.IsNullOrEmpty(Read(firstPath).run.endedUtc), "final save contains end timestamp");

                recorder.RecordingEnabled = false;
                host.ResetSimulation();
                director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "rec2", route,
                    "test", "Pressure", pressure.Operations));
                host.StepSimulation(0.1f);
                LogicalConnectionState unrecorded = host.FindConnection("rec2");
                Near(unrecorded.CompletedWork, enabledWork, "recording does not change WORK");
                Near(unrecorded.AllocatedLoad, enabledLoad, "recording does not change LOAD");

                recorder.RecordingEnabled = true;
                host.ResetSimulation();
                recorder.BeginRun();
                Check(recorder.OutputPath != firstPath && File.Exists(recorder.OutputPath) &&
                    Read(recorder.OutputPath).run.runId != initial.run.runId,
                    "reset creates a distinct run file");
                recorder.EndRun();
                host.SetRunMode(ConnectionRunMode.AuthoredWitness);
                recorder.BeginRun();
                foreach (LogicalConnectionState state in host.RuntimeConnections)
                    recorder.RecordConnectionSpawned(state, host.SimulationTimeSeconds);
                Check(recorder.CurrentLog.run.mode == "AuthoredWitness" &&
                    recorder.CurrentLog.events.Count(e => e.type == "connection_spawned") == 3,
                    "authored mode records its own configuration and connections");
                recorder.EndRun();
                Debug.Log("RecorderWitness PASS: metadata, lifecycle, coherent LOAD/WORK, marker, " +
                    "JSON saves, autosave, kill, distinct runs, authored mode, and " +
                    "enabled/disabled parity. " + firstPath);
            }
            finally
            {
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                director.AutoGenerationEnabled = oldAuto;
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static ConnectionsRunLog Read(string path) =>
            JsonUtility.FromJson<ConnectionsRunLog>(File.ReadAllText(path));
        private static NodeView Node(SimulationHost host, string id) =>
            host.Nodes.First(n => n.View.NodeId == id).View;
        private static void Near(float actual, float expected, string name) =>
            Check(Mathf.Abs(actual - expected) < 0.0001f,
                $"{name}: expected {expected:0.###}, observed {actual:0.###}");
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("RecorderWitness failed: " + name);
        }
    }
}
