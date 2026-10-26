using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Uptime.Connections
{
    [Serializable] public sealed class ConnectionsRunLog
    {
        public int schemaVersion = 2;
        public RunMetadata run;
        public List<RunEvent> events = new List<RunEvent>();
        public List<NetworkSnapshot> snapshots = new List<NetworkSnapshot>();
    }

    [Serializable] public sealed class RunMetadata
    {
        public string runId;
        public string startedUtc;
        public string endedUtc;
        public string lastSavedUtc;
        public string unityVersion;
        public string sceneName;
        public string mode;
        public int seed;
        public float fixedStepSeconds;
        public float defaultPhantomLoad;
        public float defaultSpawnerCadence;
        public float connectionTravelSecondsPerLink;
        public NodeConfiguration[] nodes;
        public TrafficConfiguration traffic;
    }

    [Serializable] public sealed class NodeConfiguration
    {
        public string id;
        public float loadCapacity;
        public float baselineLoad;
    }

    [Serializable] public sealed class TrafficConfiguration
    {
        public bool autoGenerationEnabled;
        public string ambientOriginId;
        public float openingGraceSeconds;
        public bool hostileGenerationEnabled;
        public float hostileMeanIntervalSeconds;
        public float benignPopulationGoal;
        public float coverageStartupGoal;
        public float coverageRampSeconds;
        public float coverageMeanIntervalSeconds;
        public float coverageResponse;
        public float coverageCancellationMemorySeconds;
        public float coverageAtGoalChance;
        public float coverageOutageRetryMaxSeconds;
        public float coverageImminentPlanSeconds;
        public int coverageCanceledAttemptWeight;
        public float deliveryChance;
        public float benignOutlierChance;
        public float secondAnomalyChance;
        public float subtleDeliveryChance;
        public float normalWorkSpread;
        public float phantomLoadWeight;
        public float blackoutWeight;
        public float spawnerWeight;
        public ProfileConfiguration[] profiles;
    }

    [Serializable] public sealed class ProfileConfiguration
    {
        public string id;
        public bool isHostile;
        public bool purposeTemplate;
        public string claimedPurpose;
        public string[] eligibleTargetIds;
        public float ambientWeight;
        public BehaviorRecord[] connectionBehaviors;
        public OperationRange[] operations;
    }

    [Serializable] public sealed class OperationRange
    {
        public string kind;
        public string completionEffect;
        public string activityDescription;
        public BehaviorRecord[] behaviors;
        public float minimumLoad;
        public float maximumLoad;
        public float minimumDurationSeconds;
        public float maximumDurationSeconds;
        public float meanWork;
    }

    [Serializable] public sealed class BehaviorRecord
    {
        public string id;
        public string kind;
        public float startAfterSeconds;
        public float stopAfterSeconds;
        public float normalIntervalSeconds;
        public float elevatedIntervalSeconds;
        public float workDelta;
        public float loadDelta;
        public float workRateMultiplier;
    }

    [Serializable] public sealed class RunEvent
    {
        public int sequence;
        public float timeSeconds;
        public string type;
        public string connectionId;
        public string source;
        public float scheduledSpawnTime;
        public string profileId;
        public ConnectionGenerationTrace generation;
        public ConnectionDefinitionRecord definition;
        public int operationIndex;
        public string operationKind;
        public string effectType;
        public string targetNodeId;
        public string infectionKind;
        public float infectionLoad;
        public float infectionCadence;
        public float infectionAppliedAtSeconds;
        public float parentInfectionAppliedAtSeconds;
        public string parentNodeId;
        public string parentConnectionId;
        public string sourceConnectionId;
        public int sourceOperationIndex = -1;
        public string reason;
        public string modifierId;
        public string behaviorId;
        public string modifierScope;
        public float workDelta;
        public float loadDelta;
        public float workRateMultiplier;
        public float completedWork;
        public float requiredWork;
        public float actualDurationSeconds;
        public float desiredLoad;
        public string resolutionReason;
        public string resolutionCause;
        public string terminationPhase;
        public string failureNodeId;
        public float attemptTimeSeconds;
        public bool established;
        public float establishedAtSeconds;
        public float resolvedAtSeconds;
        public float elapsedLifetimeSeconds;
        public string command;
        public bool success;
        public int actionSequence;
        public float healthPenaltyAmount;
        public float healthAfter;
        public string note;
        public int snapshotIndex = -1;
        public int establishedBenign;
        public int anticipatedBenign;
        public int recentCanceledBenign;
        public float effectiveCoverageGoal;
        public float coverageSpawnChance;
        public float coverageRoll;
        public int nextCoveragePressure;
        public float nextCoverageMeanSeconds;
        public float nextCoverageIntervalSeconds;
        public float nextCoverageAtSeconds;
        public float hostileMeanIntervalSeconds;
        public float nextHostileIntervalSeconds;
        public float nextHostileAtSeconds;
    }

    [Serializable] public sealed class ConnectionDefinitionRecord
    {
        public string profileId;
        public bool isHostile;
        public string claimedPurpose;
        public string origin;
        public string target;
        public string[] route;
        public BehaviorRecord[] connectionBehaviors;
        public OperationRecord[] operations;
    }

    [Serializable] public sealed class OperationRecord
    {
        public string kind;
        public string completionEffect;
        public string infectionKind;
        public float infectionLoad;
        public float infectionCadence;
        public string activityDescription;
        public BehaviorRecord[] behaviors;
        public float desiredLoad;
        public float totalWork;
        public float nominalDurationSeconds;
    }

    [Serializable] public sealed class NetworkSnapshot
    {
        public float timeSeconds;
        public int activeConnections;
        public int establishedBenign;
        public int establishedHostile;
        public int anticipatedBenign;
        public HealthSnapshotRecord health;
        public NodeSnapshot[] nodes;
        public InfectionSnapshot[] infections;
        public ConnectionSnapshot[] connections;
    }

    [Serializable] public sealed class InfectionSnapshot
    {
        public string targetNodeId;
        public string kind;
        public float requestedLoad;
        public float cadenceSeconds;
        public float nextSpawnAtSeconds;
        public float appliedAtSeconds;
        public string sourceConnectionId;
        public int sourceOperationIndex;
    }

    [Serializable] public sealed class HealthSnapshotRecord
    {
        public float value;
        public float netRate;
        public float recoveryRate;
        public float degradationRate;
        public int slowedCount;
        public int overdueCount;
        public bool failed;
    }

    [Serializable] public sealed class NodeSnapshot
    {
        public string id;
        public float loadCapacity;
        public float baselineLoad;
        public float infectionRequestedLoad;
        public float infectionAllocatedLoad;
        public float connectionRequestedLoad;
        public float connectionAllocatedLoad;
        public float requestedLoad;
        public float allocatedLoad;
    }

    [Serializable] public sealed class ConnectionSnapshot
    {
        public string id;
        public string lifecycle;
        public int operationIndex;
        public string operationKind;
        public string target;
        public float desiredLoad;
        public float baseLoad;
        public float allocatedLoad;
        public float completedWork;
        public float totalWork;
        public float baseWork;
        public float effectiveWorkRateMultiplier;
        public float actualWorkRate;
        public string performance;
    }

    // Observes authoritative state and writes one replaceable JSON file per Play Mode run.
    public sealed class ConnectionsRunRecorder : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private ConnectionTrafficDirector trafficDirector;
        [SerializeField] private bool recordingEnabled = true;
        [SerializeField, Min(0.1f)] private float snapshotIntervalSeconds = 1f;
        [SerializeField, Min(1f)] private float autosaveIntervalSeconds = 30f;

        private ConnectionsRunLog log;
        private readonly Dictionary<string, float> operationStartedAt =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private string outputPath;
        private float nextSnapshotAt;
        private double nextAutosaveAt;
        private bool ended;

        public bool RecordingEnabled
        {
            get => recordingEnabled;
            set { if (!value && IsRecording) EndRun(); recordingEnabled = value; }
        }
        public bool IsRecording => recordingEnabled && log != null && !ended;
        public ConnectionsRunLog CurrentLog => log;
        public string OutputPath => outputPath;
        public string LastError { get; private set; }
        public float SnapshotIntervalSeconds => snapshotIntervalSeconds;
        public float AutosaveIntervalSeconds => autosaveIntervalSeconds;
        public static string LogDirectory => Path.GetFullPath(Path.Combine(
#if UNITY_EDITOR
            Application.dataPath, "..", "Logs", "UptimeRuns"));
#else
            Application.persistentDataPath, "UptimeRuns"));
#endif

        public void BeginRun()
        {
            if (IsRecording) EndRun();
            log = null;
            outputPath = null;
            operationStartedAt.Clear();
            ended = false;
            LastError = null;
            if (!recordingEnabled || host == null) return;

            DateTime started = DateTime.UtcNow;
            string runId = Guid.NewGuid().ToString("N").Substring(0, 8);
            int seed = trafficDirector != null ? trafficDirector.Seed : 0;
            outputPath = Path.Combine(LogDirectory, "uptime_" +
                started.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) +
                "_seed-" + seed.ToString("00000", CultureInfo.InvariantCulture) + "_" + runId + ".json");
            log = new ConnectionsRunLog { run = BuildMetadata(runId, started, seed) };
            nextSnapshotAt = Mathf.Max(0.1f, snapshotIntervalSeconds);
            nextAutosaveAt = Time.realtimeSinceStartupAsDouble + Mathf.Max(1f, autosaveIntervalSeconds);
            Save(false); // Establish the file immediately, even before the first connection arrives.
        }

        public void EndRun()
        {
            if (!IsRecording) return;
            log.run.endedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            ended = true;
            Save(false);
        }

        private void OnApplicationQuit() => EndRun();
#if UNITY_EDITOR
        private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode) EndRun();
        }
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EndRun();
        }
#else
        private void OnDisable() => EndRun();
#endif

        private void Update() => SaveIfDue(Time.realtimeSinceStartupAsDouble);

        // Real elapsed time remains independent of the simulation clock and Time.timeScale.
        public void SaveIfDue(double realtime)
        {
            if (IsRecording && realtime >= nextAutosaveAt)
            {
                Save(false);
                nextAutosaveAt = realtime +
                    Mathf.Max(1f, autosaveIntervalSeconds);
            }
        }

        public bool Save(bool explicitRequest)
        {
            if (log == null || string.IsNullOrEmpty(outputPath))
            {
                LastError = "No run recording is active.";
                return false;
            }
            string temporary = outputPath + ".tmp";
            try
            {
                Directory.CreateDirectory(LogDirectory);
                log.run.lastSavedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                string json = JsonUtility.ToJson(log, true);
                File.WriteAllText(temporary, json, new UTF8Encoding(false));
                if (File.Exists(outputPath))
                {
                    try { File.Replace(temporary, outputPath, null); }
                    catch (IOException)
                    {
                        // Some Windows Editor sessions refuse Replace on the existing log.
                        // Preserve the current run by updating that file from the complete temp JSON.
                        File.Copy(temporary, outputPath, true);
                        File.Delete(temporary);
                    }
                }
                else File.Move(temporary, outputPath);
                LastError = null;
                if (explicitRequest) Debug.Log("[ConnectionsRunRecorder] Saved " + outputPath);
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                Debug.LogError("[ConnectionsRunRecorder] Save failed at " + outputPath + ": " + exception);
                return false;
            }
        }

        public bool Mark(string note)
        {
            if (!IsRecording || string.IsNullOrWhiteSpace(note))
            {
                LastError = "A running recording and a note are required.";
                return false;
            }
            int snapshotIndex = CaptureSnapshot(host.SimulationTimeSeconds);
            RunEvent marker = NewEvent("marker", host.SimulationTimeSeconds);
            marker.note = note;
            marker.snapshotIndex = snapshotIndex;
            return Save(true);
        }

        public static bool RevealDirectory(out string error)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
#if UNITY_EDITOR
                EditorUtility.RevealInFinder(LogDirectory);
                error = null;
                return true;
#else
                error = "log folder is available in the Unity Editor only";
                return false;
#endif
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public void RecordPlanQueued(ConnectionSpawnPlan plan, string source, float time)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("plan_queued", time);
            entry.connectionId = plan.ConnectionId;
            entry.source = source;
            entry.scheduledSpawnTime = plan.ScheduledSpawnTime;
            entry.profileId = plan.ProfileId;
            entry.generation = plan.GenerationTrace?.Copy();
            entry.parentNodeId = plan.ParentNodeId;
            entry.parentConnectionId = plan.ParentConnectionId;
            entry.parentInfectionAppliedAtSeconds = plan.ParentInfectionAppliedAtSeconds;
            // A queued plan is complete evidence even if the connection never spawns.
            entry.definition = Definition(LogicalConnectionDefinition.FromPlan(plan));
        }

        public void RecordCoverageOpportunity(float time, int establishedBenign,
            int anticipatedBenign, int recentCanceledBenign, float effectiveGoal,
            float spawnChance, float roll, int nextCoveragePressure,
            float nextMean, float nextInterval,
            float nextAt, string connectionId, string reason)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("coverage_opportunity", time);
            entry.connectionId = connectionId;
            entry.establishedBenign = establishedBenign;
            entry.anticipatedBenign = anticipatedBenign;
            entry.recentCanceledBenign = recentCanceledBenign;
            entry.effectiveCoverageGoal = effectiveGoal;
            entry.coverageSpawnChance = spawnChance;
            entry.coverageRoll = roll;
            entry.nextCoveragePressure = nextCoveragePressure;
            entry.nextCoverageMeanSeconds = nextMean;
            entry.nextCoverageIntervalSeconds = nextInterval;
            entry.nextCoverageAtSeconds = nextAt;
            entry.reason = reason;
        }

        public void RecordHostileOpportunity(float time, float dueAt, float mean,
            float nextInterval, float nextAt, string connectionId, string reason)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("hostile_opportunity", time);
            entry.scheduledSpawnTime = dueAt;
            entry.connectionId = connectionId;
            entry.hostileMeanIntervalSeconds = mean;
            entry.nextHostileIntervalSeconds = nextInterval;
            entry.nextHostileAtSeconds = nextAt;
            entry.reason = reason;
        }

        public void RecordConnectionSpawned(LogicalConnectionState state, float time)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("connection_spawned", time);
            entry.connectionId = state.ConnectionId;
            entry.profileId = state.Definition.ProfileId;
            entry.definition = Definition(state.Definition);
        }

        public void RecordOperationStarted(LogicalConnectionState state, float time)
        {
            if (!IsRecording) return;
            int index = state.CurrentOperationIndex;
            operationStartedAt[OperationKey(state.ConnectionId, index)] = time;
            RunEvent entry = NewEvent("operation_started", time);
            entry.connectionId = state.ConnectionId;
            entry.operationIndex = index;
            entry.operationKind = state.CurrentOperation.ToString();
            entry.completedWork = state.CompletedWork;
            entry.desiredLoad = state.DesiredLoad;
        }

        public void RecordOperationCompleted(LogicalConnectionState state, int completedIndex,
            float completedWork, float requiredWork, float desiredLoad, float time)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("operation_completed", time);
            entry.connectionId = state.ConnectionId;
            entry.operationIndex = completedIndex;
            entry.operationKind = state.Definition.OperationAt(completedIndex).ToString();
            entry.completedWork = completedWork;
            entry.requiredWork = requiredWork;
            entry.desiredLoad = desiredLoad;
            if (operationStartedAt.TryGetValue(OperationKey(state.ConnectionId, completedIndex), out float started))
                entry.actualDurationSeconds = Mathf.Max(0f, time - started);
        }

        public void RecordEffect(ConnectionEffectOccurrence occurrence)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("effect", occurrence.TimeSeconds);
            entry.connectionId = occurrence.ConnectionId;
            entry.operationIndex = occurrence.OperationIndex;
            entry.effectType = occurrence.Type.ToString();
            entry.targetNodeId = occurrence.TargetNodeId;
            entry.infectionKind = occurrence.Payload?.Kind.ToString();
            entry.infectionLoad = occurrence.Payload?.RequestedLoad ?? 0f;
            entry.infectionCadence = occurrence.Payload?.CadenceSeconds ?? 0f;
        }

        public void RecordInfection(string type, NodeInfection infection, float time,
            string reason, int actionSequence = 0)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent(type, time);
            entry.connectionId = infection.SourceConnectionId;
            entry.operationIndex = infection.SourceOperationIndex;
            entry.targetNodeId = infection.Target.View.NodeId;
            entry.infectionKind = infection.Payload.Kind.ToString();
            entry.infectionLoad = infection.Payload.RequestedLoad;
            entry.infectionCadence = infection.Payload.CadenceSeconds;
            entry.infectionAppliedAtSeconds = infection.AppliedAtSeconds;
            entry.sourceConnectionId = infection.SourceConnectionId;
            entry.sourceOperationIndex = infection.SourceOperationIndex;
            entry.source = reason == "player_clean" ? "player_clean" :
                (reason.StartsWith("debug_", StringComparison.Ordinal) ||
                infection.SourceConnectionId == null ? "debug" : "delivery");
            entry.reason = reason;
            entry.actionSequence = actionSequence;
        }

        public void RecordSpawnerAttempt(NodeInfection parent, ConnectionSpawnPlan child,
            string reason, float time)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("spawner_attempt", time);
            entry.targetNodeId = parent.Target.View.NodeId;
            entry.sourceConnectionId = parent.SourceConnectionId;
            entry.infectionAppliedAtSeconds = parent.AppliedAtSeconds;
            entry.infectionCadence = parent.Payload.CadenceSeconds;
            entry.connectionId = child?.ConnectionId;
            entry.reason = child == null ? reason : "queued";
            if (child != null)
            {
                entry.definition = Definition(LogicalConnectionDefinition.FromPlan(child));
                entry.generation = child.GenerationTrace?.Copy();
                entry.infectionKind = child.Operations[child.Operations.Length - 1].InfectionPayload?.Kind.ToString();
            }
        }

        public void RecordModifierChange(LogicalConnectionState state, ModifierHistoryEntry change)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("modifier_" + change.Action.ToString().ToLowerInvariant(),
                change.TimeSeconds);
            entry.connectionId = state.ConnectionId;
            entry.operationIndex = change.OperationIndex;
            entry.modifierId = change.ModifierId;
            entry.behaviorId = change.SourceId;
            entry.modifierScope = change.Scope.ToString();
            entry.workDelta = change.WorkDelta;
            entry.loadDelta = change.LoadDelta;
            entry.workRateMultiplier = change.WorkRateMultiplier;
        }

        public void RecordResolution(LogicalConnectionState state, ConnectionResolution reason, float time)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("connection_resolved", time);
            entry.connectionId = state.ConnectionId;
            entry.resolutionReason = reason.ToString();
            entry.resolutionCause = state.TerminationCause.ToString();
            entry.terminationPhase = state.TerminationPhase.ToString();
            entry.failureNodeId = state.FailureNodeId;
            entry.definition = Definition(state.Definition);
            entry.attemptTimeSeconds = state.SpawnedAtSeconds;
            entry.established = state.HasEstablished;
            entry.establishedAtSeconds = state.EstablishedAtSeconds;
            entry.resolvedAtSeconds = time;
            entry.healthPenaltyAmount = state.HealthPenaltyAmount;
            entry.healthAfter = host.Health?.Value ?? 0f;
            entry.operationIndex = state.CurrentOperationIndex;
            entry.operationKind = state.CurrentOperation.ToString();
            entry.completedWork = state.CompletedWork;
            entry.elapsedLifetimeSeconds = Mathf.Max(0f, time - state.SpawnedAtSeconds);
        }

        public void RecordNodeArrival(LogicalConnectionState state, string nodeId, float time)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("connection_node_arrived", time);
            entry.connectionId = state.ConnectionId;
            entry.targetNodeId = nodeId;
        }

        public void RecordConnectionEstablished(LogicalConnectionState state, float time)
        {
            if (!IsRecording) return;
            RunEvent entry = NewEvent("connection_established", time);
            entry.connectionId = state.ConnectionId;
            entry.establishedAtSeconds = time;
        }

        public int RecordPlayerAction(string command, string connectionId, bool success,
            string targetNodeId = null, string reason = null)
        {
            if (!IsRecording) return 0;
            RunEvent entry = NewEvent("player_action", host.SimulationTimeSeconds);
            entry.command = command;
            entry.connectionId = connectionId;
            entry.targetNodeId = targetNodeId;
            entry.success = success;
            entry.reason = reason;
            return entry.sequence;
        }

        public void RecordHealthPenalty(NetworkHealthEvent penalty, float resultingHealth)
        {
            if (!IsRecording || penalty == null) return;
            RunEvent entry = NewEvent("health_penalty", penalty.TimeSeconds);
            entry.connectionId = penalty.ConnectionId;
            entry.healthPenaltyAmount = penalty.Amount;
            entry.healthAfter = resultingHealth;
        }

        public void CaptureDueSnapshot(float simulationTime)
        {
            if (!IsRecording || simulationTime + 0.00001f < nextSnapshotAt) return;
            CaptureSnapshot(simulationTime);
            float interval = Mathf.Max(0.1f, snapshotIntervalSeconds);
            do nextSnapshotAt += interval;
            while (nextSnapshotAt <= simulationTime + 0.00001f);
        }

        public int CaptureSnapshot(float simulationTime)
        {
            if (!IsRecording || host.RuntimeConnections == null) return -1;
            var snapshot = new NetworkSnapshot { timeSeconds = simulationTime,
                nodes = new NodeSnapshot[host.Nodes.Length] };
            NetworkHealthSnapshot health = host.Health;
            if (health != null)
                snapshot.health = new HealthSnapshotRecord { value = health.Value,
                    netRate = health.NetRate, recoveryRate = health.RecoveryRate,
                    degradationRate = health.DegradationRate,
                    slowedCount = health.SlowedCount, overdueCount = health.OverdueCount,
                    failed = health.Failed };
            for (int i = 0; i < host.Nodes.Length; i++)
            {
                NodeResources node = host.Nodes[i];
                snapshot.nodes[i] = new NodeSnapshot { id = node.View.NodeId,
                    loadCapacity = node.LoadCapacity, baselineLoad = node.BaselineLoad,
                    infectionRequestedLoad = node.InfectionRequestedLoad,
                    infectionAllocatedLoad = node.InfectionAllocatedLoad,
                    connectionRequestedLoad = node.ConnectionRequestedLoad,
                    connectionAllocatedLoad = node.ConnectionAllocatedLoad,
                    requestedLoad = node.RequestedLoad, allocatedLoad = node.AllocatedLoad };
            }
            snapshot.infections = new InfectionSnapshot[host.Infections.Count];
            for (int i = 0; i < snapshot.infections.Length; i++)
            {
                NodeInfection infection = host.Infections[i];
                snapshot.infections[i] = new InfectionSnapshot
                {
                    targetNodeId = infection.Target.View.NodeId,
                    kind = infection.Payload.Kind.ToString(),
                    requestedLoad = infection.Payload.RequestedLoad,
                    cadenceSeconds = infection.Payload.CadenceSeconds,
                    nextSpawnAtSeconds = infection.NextSpawnAtSeconds,
                    appliedAtSeconds = infection.AppliedAtSeconds,
                    sourceConnectionId = infection.SourceConnectionId,
                    sourceOperationIndex = infection.SourceOperationIndex
                };
            }
            var active = new List<ConnectionSnapshot>();
            foreach (LogicalConnectionState state in host.RuntimeConnections)
            {
                if (!state.IsPending && !state.IsConnecting && !state.IsActive) continue;
                if (state.IsActive)
                {
                    snapshot.activeConnections++;
                    if (state.IsHostile) snapshot.establishedHostile++;
                    else snapshot.establishedBenign++;
                }
                else if (!state.IsHostile) snapshot.anticipatedBenign++;
                active.Add(new ConnectionSnapshot { id = state.ConnectionId,
                    lifecycle = state.Lifecycle.ToString(), operationIndex = state.CurrentOperationIndex,
                    operationKind = state.CurrentOperation.ToString(), target = state.TargetNode.NodeId,
                    desiredLoad = state.DesiredLoad, baseLoad = state.BaseLoad,
                    allocatedLoad = state.AllocatedLoad,
                    completedWork = state.CompletedWork, totalWork = state.CurrentTotalWork,
                    baseWork = state.BaseWork,
                    effectiveWorkRateMultiplier = state.EffectiveWorkRateMultiplier,
                    actualWorkRate = state.ActualWorkRate,
                    performance = state.IsActive ? state.Performance.ToString() : null });
            }
            snapshot.connections = active.ToArray();
            log.snapshots.Add(snapshot);
            return log.snapshots.Count - 1;
        }

        private RunEvent NewEvent(string type, float time)
        {
            var entry = new RunEvent { sequence = log.events.Count + 1, timeSeconds = time, type = type };
            log.events.Add(entry);
            return entry;
        }

        private RunMetadata BuildMetadata(string runId, DateTime started, int seed)
        {
            var metadata = new RunMetadata { runId = runId,
                startedUtc = started.ToString("O", CultureInfo.InvariantCulture),
                unityVersion = Application.unityVersion,
                sceneName = SceneManager.GetActiveScene().name,
                mode = host.RunMode.ToString(), seed = seed,
                fixedStepSeconds = Time.fixedDeltaTime,
                defaultPhantomLoad = host.DefaultPhantomLoad,
                defaultSpawnerCadence = host.DefaultSpawnerCadence,
                connectionTravelSecondsPerLink = host.ConnectionTravelSecondsPerLink,
                nodes = new NodeConfiguration[host.Nodes.Length] };
            for (int i = 0; i < host.Nodes.Length; i++)
            {
                NodeResources node = host.Nodes[i];
                metadata.nodes[i] = new NodeConfiguration { id = node.View.NodeId,
                    loadCapacity = node.LoadCapacity, baselineLoad = node.BaselineLoad };
            }
            if (trafficDirector == null) return metadata;
            IReadOnlyList<ConnectionTrafficProfile> configuredProfiles = trafficDirector.Profiles;
            var traffic = new TrafficConfiguration { autoGenerationEnabled = trafficDirector.AutoGenerationEnabled,
                ambientOriginId = trafficDirector.AmbientOriginId,
                openingGraceSeconds = trafficDirector.InitialGraceSeconds,
                hostileGenerationEnabled = trafficDirector.HostileGenerationEnabled,
                hostileMeanIntervalSeconds = trafficDirector.HostileMeanIntervalSeconds,
                benignPopulationGoal = trafficDirector.BenignPopulationGoal,
                coverageStartupGoal = trafficDirector.CoverageStartupGoal,
                coverageRampSeconds = trafficDirector.CoverageRampSeconds,
                coverageMeanIntervalSeconds = trafficDirector.CoverageMeanIntervalSeconds,
                coverageResponse = trafficDirector.CoverageResponse,
                coverageCancellationMemorySeconds =
                    trafficDirector.CoverageCancellationMemorySeconds,
                coverageAtGoalChance = trafficDirector.CoverageAtGoalChance,
                coverageOutageRetryMaxSeconds = trafficDirector.CoverageOutageRetryMaxSeconds,
                coverageImminentPlanSeconds = trafficDirector.CoverageImminentPlanSeconds,
                coverageCanceledAttemptWeight =
                    trafficDirector.CoverageCanceledAttemptWeight,
                deliveryChance = trafficDirector.DeliveryChance,
                benignOutlierChance = trafficDirector.BenignOutlierChance,
                secondAnomalyChance = trafficDirector.SecondAnomalyChance,
                subtleDeliveryChance = trafficDirector.SubtleDeliveryChance,
                normalWorkSpread = trafficDirector.NormalWorkSpread,
                phantomLoadWeight = trafficDirector.PhantomLoadWeight,
                blackoutWeight = trafficDirector.BlackoutWeight,
                spawnerWeight = trafficDirector.SpawnerWeight,
                profiles = new ProfileConfiguration[configuredProfiles != null ? configuredProfiles.Count : 0] };
            for (int i = 0; i < traffic.profiles.Length; i++)
            {
                ConnectionTrafficProfile source = configuredProfiles[i];
                var profile = new ProfileConfiguration { id = source.id,
                    isHostile = source.isHostile,
                    purposeTemplate = source.purposeTemplate,
                    claimedPurpose = source.claimedPurpose,
                    ambientWeight = trafficDirector.EffectiveAmbientWeight(source.id),
                    eligibleTargetIds = (string[])source.eligibleTargetIds.Clone(),
                    connectionBehaviors = Behaviors(source.connectionBehaviors),
                    operations = new OperationRange[source.operations.Length] };
                for (int j = 0; j < profile.operations.Length; j++)
                {
                    ConnectionOperationProfile operation = source.operations[j];
                    profile.operations[j] = new OperationRange { kind = operation.operation.ToString(),
                        completionEffect = operation.completionEffect.ToString(),
                        activityDescription = operation.activityDescription,
                        behaviors = Behaviors(operation.behaviors),
                        minimumLoad = operation.minimumLoad, maximumLoad = operation.maximumLoad,
                        minimumDurationSeconds = operation.minimumDuration,
                        maximumDurationSeconds = operation.maximumDuration,
                        meanWork = operation.meanWork };
                }
                traffic.profiles[i] = profile;
            }
            metadata.traffic = traffic;
            return metadata;
        }

        private static ConnectionDefinitionRecord Definition(LogicalConnectionDefinition definition)
        {
            var result = new ConnectionDefinitionRecord { profileId = definition.ProfileId,
                isHostile = definition.IsHostile,
                claimedPurpose = definition.ClaimedPurpose,
                route = new string[definition.Route.Length],
                connectionBehaviors = Behaviors(definition.ConnectionBehaviors),
                operations = new OperationRecord[definition.OperationCount] };
            for (int i = 0; i < result.route.Length; i++)
                result.route[i] = definition.Route[i].Node.NodeId;
            result.origin = result.route[0];
            result.target = result.route[result.route.Length - 1];
            for (int i = 0; i < result.operations.Length; i++)
            {
                float load = definition.LoadAt(i);
                result.operations[i] = new OperationRecord { kind = definition.OperationAt(i).ToString(),
                    completionEffect = definition.EffectAt(i).ToString(),
                    infectionKind = definition.PayloadAt(i)?.Kind.ToString(),
                    infectionLoad = definition.PayloadAt(i)?.RequestedLoad ?? 0f,
                    infectionCadence = definition.PayloadAt(i)?.CadenceSeconds ?? 0f,
                    activityDescription = definition.ActivityAt(i), desiredLoad = load,
                    behaviors = Behaviors(definition.BehaviorsAt(i)),
                    totalWork = definition.WorkAt(i),
                    nominalDurationSeconds = load > 0f ? definition.WorkAt(i) / load : 0f };
            }
            return result;
        }

        private static BehaviorRecord[] Behaviors(TimedModifierBehaviorDefinition[] source)
        {
            if (source == null) return Array.Empty<BehaviorRecord>();
            var result = new BehaviorRecord[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = new BehaviorRecord { id = source[i].Id,
                    kind = source[i].Kind.ToString(),
                    startAfterSeconds = source[i].StartAfterSeconds,
                    stopAfterSeconds = source[i].StopAfterSeconds,
                    normalIntervalSeconds = source[i].NormalIntervalSeconds,
                    elevatedIntervalSeconds = source[i].ElevatedIntervalSeconds,
                    workDelta = source[i].WorkDelta, loadDelta = source[i].LoadDelta,
                    workRateMultiplier = source[i].WorkRateMultiplier };
            return result;
        }

        private static string OperationKey(string id, int index) => id + ":" + index;
    }
}
