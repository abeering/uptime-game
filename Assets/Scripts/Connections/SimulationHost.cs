using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    public enum ConnectionRunMode { AuthoredWitness, Generated }

    // The only clock and owner of resource state for the Connections game.
    [ExecuteAlways]
    public sealed class SimulationHost : MonoBehaviour
    {
        [SerializeField] private NodeResources[] nodes;
        [SerializeField] private PhysicalLinkReference[] physicalLinks;
        [SerializeField] private LogicalConnectionDefinition[] connections;
        [SerializeField] private TMP_Text connectionDebugReadout;
        [SerializeField] private LogicalPathPresenter pathPresenter;
        [SerializeField] private ConnectionRunMode runMode;
        [SerializeField] private ConnectionTrafficDirector trafficDirector;
        [SerializeField] private ConnectionsRunRecorder recorder;
        [SerializeField] private PayloadDirector payloadDirector;
        [SerializeField] private WormDirector wormDirector;
        [SerializeField] private NetworkHealthTuning healthTuning = new NetworkHealthTuning();
        [SerializeField, Min(0.01f)] private float defaultPhantomLoad = 8f;
        [SerializeField, Min(0.01f)] private float defaultSpawnerCadence = 20f;
        [SerializeField, Min(0.01f)] private float connectionTravelSecondsPerLink = 0.6f;
        [SerializeField] private bool naturalLoadNoiseEnabled = true;
        [SerializeField, Range(0f, 0.2f)] private float naturalLoadNoiseAmplitude = 0.1f;
        [SerializeField, Min(0.1f)] private float naturalLoadNoiseMinPeriodSeconds = 3f;
        [SerializeField, Min(0.1f)] private float naturalLoadNoiseMaxPeriodSeconds = 6f;
        [SerializeField, Min(0.1f)] private float scanDurationSeconds = 8f;

        private LogicalConnectionState[] runtimeConnections;
        private readonly Dictionary<string, LogicalConnectionState> history =
            new Dictionary<string, LogicalConnectionState>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ConnectionEffectOccurrence> occurrences =
            new List<ConnectionEffectOccurrence>();
        private readonly List<NodeInfection> infections = new List<NodeInfection>();
        private readonly List<NodeCondition> conditions = new List<NodeCondition>();
        private float simulationTimeSeconds;
        private NetworkHealth health;
        private CleanSession activeClean;
        private int nextCleanSessionId;
        private NodeScanSession activeScan;
        private int nextScanId;

        public NodeResources[] Nodes => nodes;
        public PhysicalLinkReference[] PhysicalLinks => physicalLinks;
        public LogicalConnectionDefinition[] Definitions => connections;
        public IReadOnlyList<LogicalConnectionState> RuntimeConnections => runtimeConnections;
        public float SimulationTimeSeconds => simulationTimeSeconds;
        public NetworkHealthSnapshot Health => health?.Snapshot;
        public ConnectionRunMode RunMode => runMode;
        public ConnectionsRunRecorder Recorder => recorder;
        public PayloadDirector Payloads => payloadDirector;
        public WormDirector Worms => wormDirector;
        public IReadOnlyList<ConnectionEffectOccurrence> Occurrences => occurrences;
        public IReadOnlyList<NodeInfection> Infections => infections;
        public IReadOnlyList<NodeCondition> Conditions => conditions.AsReadOnly();
        public NodeCondition ConditionFor(NodeView view, ConditionKind kind)
        {
            foreach (NodeCondition condition in conditions)
                if (condition.Target.View == view && condition.Kind == kind) return condition;
            return null;
        }
        public CleanSession ActiveClean => activeClean;
        public NodeScanSession ActiveScan => activeScan;
        public float ScanDurationSeconds => scanDurationSeconds;
        public float DefaultPhantomLoad => defaultPhantomLoad;
        public float DefaultSpawnerCadence => defaultSpawnerCadence;
        public float ConnectionTravelSecondsPerLink => connectionTravelSecondsPerLink;
        public bool NaturalLoadNoiseEnabled => naturalLoadNoiseEnabled;
        public float NaturalLoadNoiseAmplitude => naturalLoadNoiseAmplitude;
        public float NaturalLoadNoiseMinPeriodSeconds => naturalLoadNoiseMinPeriodSeconds;
        public float NaturalLoadNoiseMaxPeriodSeconds => naturalLoadNoiseMaxPeriodSeconds;
        public ConnectionIdentityPalette PresentationPalette => pathPresenter != null ?
            pathPresenter.IdentityPalette : null;
        public event Action<LogicalConnectionState, ConnectionResolution> ConnectionResolved;
        public event Action<ConnectionEffectOccurrence> EffectOccurred;
        public event Action<NodeScanSession> ScanCompleted;
        public event Action<CleanSession, int> CleanSucceeded;
        public event Action SimulationReset;
        public bool HasEverUsedId(string id) => usedIds.Contains(id) ||
            payloadDirector != null && payloadDirector.HasEverUsedId(id);
        public bool IsTrafficIdReserved(string id) => trafficDirector != null &&
            trafficDirector.IsIdReserved(id);
        public LogicalConnectionState FindConnection(string id) =>
            id != null && history.TryGetValue(id, out LogicalConnectionState state) ? state : null;

        // Awake-time configuration for the separate generated-topology stress scene.
        public void ConfigureStressTopology(NodeResources[] generatedNodes,
            PhysicalLinkReference[] generatedLinks)
        {
            if (runtimeConnections != null)
                throw new InvalidOperationException("Configure stress topology before the simulation starts.");
            nodes = generatedNodes ?? throw new ArgumentNullException(nameof(generatedNodes));
            physicalLinks = generatedLinks ?? throw new ArgumentNullException(nameof(generatedLinks));
            runMode = ConnectionRunMode.Generated;
        }

        public void SetRunMode(ConnectionRunMode mode)
        {
            runMode = mode;
            ResetSimulation();
        }

        private void OnEnable() => RefreshDisplays();

        private void Start()
        {
            if (Application.isPlaying)
            {
                trafficDirector?.BeginPlaySession();
                ResetSimulation();
            }
        }

        private void FixedUpdate()
        {
            if (Application.isPlaying)
                StepSimulation(Time.fixedDeltaTime);
        }

        private void OnDisable()
        {
            activeClean = null;
            activeScan?.Reset(simulationTimeSeconds);
            activeScan = null;
            ClearAllocatedUsage();
            infections.Clear();
            payloadDirector?.ResetDirector(trafficDirector != null ? trafficDirector.Seed : 1701);
            wormDirector?.ResetDirector(trafficDirector != null ? trafficDirector.Seed : 1701);
            RefreshDisplays();
            runtimeConnections = null;
        }

        private void OnValidate()
        {
            if (float.IsNaN(defaultPhantomLoad) || float.IsInfinity(defaultPhantomLoad) ||
                defaultPhantomLoad <= 0f) defaultPhantomLoad = 8f;
            if (float.IsNaN(defaultSpawnerCadence) || float.IsInfinity(defaultSpawnerCadence) ||
                defaultSpawnerCadence <= 0f) defaultSpawnerCadence = 20f;
            if (float.IsNaN(connectionTravelSecondsPerLink) ||
                float.IsInfinity(connectionTravelSecondsPerLink) ||
                connectionTravelSecondsPerLink <= 0f) connectionTravelSecondsPerLink = 0.6f;
            if (float.IsNaN(naturalLoadNoiseAmplitude) || float.IsInfinity(naturalLoadNoiseAmplitude))
                naturalLoadNoiseAmplitude = 0.1f;
            naturalLoadNoiseAmplitude = Mathf.Clamp(naturalLoadNoiseAmplitude, 0f, 0.2f);
            if (float.IsNaN(naturalLoadNoiseMinPeriodSeconds) ||
                float.IsInfinity(naturalLoadNoiseMinPeriodSeconds) ||
                naturalLoadNoiseMinPeriodSeconds <= 0f) naturalLoadNoiseMinPeriodSeconds = 3f;
            if (float.IsNaN(naturalLoadNoiseMaxPeriodSeconds) ||
                float.IsInfinity(naturalLoadNoiseMaxPeriodSeconds) ||
                naturalLoadNoiseMaxPeriodSeconds < naturalLoadNoiseMinPeriodSeconds)
                naturalLoadNoiseMaxPeriodSeconds = naturalLoadNoiseMinPeriodSeconds;
            if (float.IsNaN(scanDurationSeconds) || float.IsInfinity(scanDurationSeconds) ||
                scanDurationSeconds <= 0f) scanDurationSeconds = 8f;
            if (nodes != null)
            {
                foreach (NodeResources node in nodes)
                    node?.ClampToCapacity();
            }

            if (connections != null)
            {
                foreach (LogicalConnectionDefinition connection in connections)
                    connection?.ClampNonnegative();
            }
        }

        private void Update() => RefreshDisplays();

        // Explicit step seam for deterministic Editor witnesses. Play mode calls it only from FixedUpdate.
        public void StepSimulation(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (runtimeConnections == null)
                ResetSimulation();

            trafficDirector?.Tick(simulationTimeSeconds, runMode == ConnectionRunMode.Generated);
            float remaining = deltaSeconds;
            var completedThisStep = new HashSet<LogicalConnectionState>();
            while (remaining > 0.000001f)
            {
                BeginDueConnections();
                WithdrawDueLoaders();
                float segment = remaining;
                foreach (LogicalConnectionState connection in runtimeConnections)
                {
                    if (connection.IsActive && connection.Definition.LoaderAttack != null)
                    {
                        // A coarse witness step must still observe timed attack activation
                        // and withdrawal rather than run through the entire attack at once.
                        segment = Mathf.Min(segment, 1f);
                        if (connection.LoaderWithdrawAtSeconds >= 0f)
                            segment = Mathf.Min(segment, Mathf.Max(0.000001f,
                                connection.LoaderWithdrawAtSeconds - simulationTimeSeconds));
                    }
                    if (connection.IsConnecting)
                        segment = Mathf.Min(segment, Mathf.Max(0.000001f,
                            connection.TravelSecondsPerLink - connection.LinkTravelElapsedSeconds));
                    if (connection.IsPending)
                        segment = Mathf.Min(segment, Mathf.Max(0.000001f,
                            connection.Definition.StartDelaySeconds - simulationTimeSeconds));
                }
                if (activeScan != null)
                    segment = Mathf.Min(segment, Mathf.Max(0.000001f,
                        activeScan.ScheduledCompletionAtSeconds - simulationTimeSeconds));
                foreach (NodeCondition condition in conditions)
                    segment = Mathf.Min(segment, Mathf.Max(0.000001f,
                        condition.ExpiresAtSeconds - simulationTimeSeconds));
                if (payloadDirector != null)
                    segment = Mathf.Min(segment,
                        payloadDirector.TimeToNextBoundary(simulationTimeSeconds));
                if (wormDirector != null)
                    segment = Mathf.Min(segment,
                        wormDirector.TimeToNextBoundary(simulationTimeSeconds));
                StepSegment(segment, completedThisStep);
                AdvanceConnecting(segment);
                payloadDirector?.Advance(segment, simulationTimeSeconds);
                wormDirector?.Advance(segment, simulationTimeSeconds);
                CompleteDueScan();
                ExpireConditions();
                remaining -= segment;
            }
            WithdrawDueLoaders();
            BeginDueConnections();
            StepSpawners();
            recorder?.CaptureDueSnapshot(simulationTimeSeconds);
            RefreshDisplays();
        }

        private void BeginDueConnections()
        {
            foreach (LogicalConnectionState connection in runtimeConnections)
                if (connection.IsPending && simulationTimeSeconds + 0.00001f >=
                    connection.Definition.StartDelaySeconds)
                {
                    connection.Lifecycle = Lifecycle.Connecting;
                    if (IsNodeOffline(connection.Definition.Route[0].Node))
                        CancelConnection(connection, connection.Definition.Route[0].Node.NodeId,
                            UnavailabilityCause(connection.Definition.Route[0].Node));
                }
        }

        private void StepSegment(float deltaSeconds,
            HashSet<LogicalConnectionState> completedThisStep)
        {
            foreach (LogicalConnectionState connection in runtimeConnections)
            {
                if (connection.IsTransitioning && !completedThisStep.Contains(connection))
                {
                    StartOperation(connection);
                    recorder?.RecordOperationStarted(connection, simulationTimeSeconds);
                }
                if (connection.IsActive && !completedThisStep.Contains(connection))
                {
                    UpdateNaturalLoad(connection);
                    connection.Behaviors.Advance(deltaSeconds, simulationTimeSeconds);
                }
            }
            List<ResourceAllocator.OperationCompletion> completed =
                ResourceAllocator.Step(nodes, runtimeConnections, infections, deltaSeconds,
                    allocated => health.Step(allocated, simulationTimeSeconds, deltaSeconds),
                    connection => !completedThisStep.Contains(connection),
                    payloadDirector != null ? payloadDirector.DemandAt : null);
            simulationTimeSeconds += deltaSeconds;
            var newlyBlackout = new List<NodeResources>();
            foreach (ResourceAllocator.OperationCompletion completion in completed)
            {
                recorder?.RecordOperationCompleted(completion.State, completion.Index,
                    completion.CompletedWork, completion.RequiredWork,
                    completion.DesiredLoad, simulationTimeSeconds);
                ConnectionEffectOccurrence occurrence = completion.State.OnWorkCompleted(
                    completion.Index, simulationTimeSeconds);
                if (occurrence != null)
                {
                    occurrences.Add(occurrence);
                    recorder?.RecordEffect(occurrence);
                    if (occurrence.Type == CompletionEffect.InfectionDelivery)
                    {
                        NodeResources target = FindNodeResources(occurrence.TargetNodeId);
                        bool applied = ApplyInfection(target,
                            occurrence.Payload, occurrence.ConnectionId,
                            occurrence.OperationIndex);
                        occurrence.InfectionApplied = applied;
                        if (applied && occurrence.Payload.Kind == InfectionKind.Blackout)
                            newlyBlackout.Add(target);
                    }
                    else if (occurrence.Type == CompletionEffect.ConditionApplication)
                        ApplyCondition(FindNodeResources(occurrence.TargetNodeId),
                            occurrence.ConditionPayload, occurrence.ConnectionId,
                            occurrence.OperationIndex);
                    EffectOccurred?.Invoke(occurrence);
                }
                completion.State.Behaviors.EndOperation(simulationTimeSeconds);
                completion.State.NaturalLoadDelta = 0f;
                if (completion.State.IsTransitioning)
                    completedThisStep.Add(completion.State);
                if (completion.State.IsComplete)
                {
                    completion.State.TerminationPhase = Lifecycle.Active;
                    completion.State.Behaviors.EndConnection(simulationTimeSeconds);
                    Resolve(completion.State, ConnectionResolution.Completed);
                }
            }
            foreach (NodeResources offline in newlyBlackout)
                InterruptRoutesThrough(offline);
            if (runMode == ConnectionRunMode.Generated)
            {
                var remaining = new List<LogicalConnectionState>(runtimeConnections.Length);
                foreach (LogicalConnectionState state in runtimeConnections)
                {
                    if (state.IsComplete)
                    {
                        pathPresenter?.Retire(state);
                    }
                    else remaining.Add(state);
                }
                runtimeConnections = remaining.ToArray();
            }
        }

        private void AdvanceConnecting(float deltaSeconds)
        {
            var ordered = new List<LogicalConnectionState>(runtimeConnections);
            ordered.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            bool establishedAny = false;
            foreach (LogicalConnectionState state in ordered)
            {
                if (!state.IsConnecting) continue;
                state.LinkTravelElapsedSeconds += deltaSeconds;
                if (state.LinkTravelElapsedSeconds + 0.00001f < state.TravelSecondsPerLink)
                    continue;
                state.LinkTravelElapsedSeconds = 0f;
                state.ReachedRouteNodeIndex++;
                NodeView arrived = state.Definition.Route[state.ReachedRouteNodeIndex].Node;
                recorder?.RecordNodeArrival(state, arrived.NodeId, simulationTimeSeconds);
                if (IsNodeOffline(arrived))
                {
                    CancelConnection(state, arrived.NodeId, UnavailabilityCause(arrived));
                    continue;
                }
                if (state.ReachedRouteNodeIndex != state.RouteLinks.Length) continue;
                state.Lifecycle = Lifecycle.Active;
                establishedAny = true;
                state.HasEstablished = true;
                state.EstablishedAtSeconds = simulationTimeSeconds;
                health.Activate(state, simulationTimeSeconds);
                state.Behaviors.StartConnection();
                StartOperation(state);
                recorder?.RecordConnectionEstablished(state, simulationTimeSeconds);
                recorder?.RecordOperationStarted(state, simulationTimeSeconds);
            }
            // Arrival can end the outer step. Publish the first real allocation now so
            // presentation never interprets an unallocated Active state as stalled.
            if (establishedAny)
                RefreshPayloadAllocations();
        }

        private void StartOperation(LogicalConnectionState state)
        {
            state.Behaviors.StartOperation(state.CurrentOperationIndex);
            state.OperationStartedAtSeconds = simulationTimeSeconds;
            state.NaturalLoadDelta = 0f;
        }

        private void UpdateNaturalLoad(LogicalConnectionState state)
        {
            float delta = naturalLoadNoiseEnabled && state.Definition.NaturalLoadNoise ?
                NaturalLoadVariation.Delta(state.Definition.NaturalLoadSeed,
                    state.ConnectionId, state.CurrentOperationIndex,
                    simulationTimeSeconds - state.OperationStartedAtSeconds,
                    state.BaseLoad, naturalLoadNoiseAmplitude,
                    naturalLoadNoiseMinPeriodSeconds, naturalLoadNoiseMaxPeriodSeconds) : 0f;
            state.NaturalLoadDelta = delta;
            state.Behaviors.SetDynamicLoadDelta("natural", delta);
        }

        public LogicalConnectionState RegisterConnection(LogicalConnectionDefinition definition)
        {
            if (runtimeConnections == null) ResetSimulation();
            LogicalConnectionState state = BuildState(definition);
            if (HasEverUsedId(state.ConnectionId) || !usedIds.Add(state.ConnectionId))
                throw new InvalidOperationException("Connection ID has already been used: " + state.ConnectionId);
            state.SpawnedAtSeconds = simulationTimeSeconds;
            state.TravelSecondsPerLink = connectionTravelSecondsPerLink;
            var next = new List<LogicalConnectionState>(runtimeConnections) { state };
            runtimeConnections = next.ToArray();
            history.Add(state.ConnectionId, state);
            recorder?.RecordConnectionSpawned(state, simulationTimeSeconds);
            if (Application.isPlaying) pathPresenter?.Add(state);
            if (state.IsConnecting && IsNodeOffline(state.Definition.Route[0].Node))
                CancelConnection(state, state.Definition.Route[0].Node.NodeId,
                    UnavailabilityCause(state.Definition.Route[0].Node));
            return state;
        }

        public void ValidatePlan(ConnectionSpawnPlan plan)
        {
            if (plan == null || plan.Operations.Length == 0 ||
                string.IsNullOrWhiteSpace(plan.ProfileId) || HasEverUsedId(plan.ConnectionId))
                throw new InvalidOperationException("Spawn plan needs operations, profile ID, and an unused ID.");
            BuildState(LogicalConnectionDefinition.FromPlan(plan));
        }

        public NodeCondition ApplyCondition(NodeResources target, ConditionPayload payload,
            string sourceConnectionId = null, int sourceOperationIndex = -1,
            string sourceWormBranchId = null)
        {
            if (target == null || payload == null || !payload.IsValid ||
                Array.IndexOf(nodes, target) < 0)
                throw new InvalidOperationException("Condition needs a configured target and valid payload.");
            foreach (NodeCondition existing in conditions)
                if (existing.Target == target && existing.Kind == payload.Kind)
                {
                    existing.Refresh(payload, simulationTimeSeconds, sourceConnectionId,
                        sourceOperationIndex, sourceWormBranchId);
                    recorder?.RecordCondition("condition_refreshed", existing,
                        simulationTimeSeconds);
                    return existing;
                }
            var condition = new NodeCondition(target, payload, simulationTimeSeconds,
                sourceConnectionId, sourceOperationIndex, sourceWormBranchId);
            conditions.Add(condition);
            recorder?.RecordCondition("condition_applied", condition,
                simulationTimeSeconds);
            return condition;
        }

        private void ExpireConditions()
        {
            for (int i = conditions.Count - 1; i >= 0; i--)
                if (conditions[i].ExpiresAtSeconds <= simulationTimeSeconds + 0.00001f)
                {
                    recorder?.RecordCondition("condition_expired", conditions[i],
                        conditions[i].ExpiresAtSeconds);
                    conditions.RemoveAt(i);
                }
        }

        public bool KillConnection(string id)
        {
            if (runtimeConnections == null || id == null) return false;
            foreach (LogicalConnectionState state in runtimeConnections)
                if (string.Equals(state.ConnectionId, id, StringComparison.OrdinalIgnoreCase) &&
                    (state.IsActive || state.IsConnecting))
                {
                    bool wasActive = state.IsActive;
                    state.TerminationPhase = state.Lifecycle;
                    state.TerminationCause = ConnectionTerminationCause.Player;
                    state.Lifecycle = Lifecycle.Complete;
                    state.AllocatedLoad = 0f;
                    if (wasActive) state.Behaviors.EndConnection(simulationTimeSeconds);
                    state.NaturalLoadDelta = 0f;
                    Resolve(state, ConnectionResolution.Killed);
                    var next = new List<LogicalConnectionState>(runtimeConnections);
                    next.Remove(state);
                    runtimeConnections = next.ToArray();
                    pathPresenter?.Retire(state);
                    // Publish released LOAD immediately; peers receive new allocations next step.
                    foreach (NodeResources node in nodes) node.ClearConnectionUsage();
                    foreach (LogicalConnectionState live in runtimeConnections)
                        if (live.IsDemanding)
                            live.TargetResources.AddUsage(live.DesiredLoad, live.AllocatedLoad);
                    recorder?.CaptureSnapshot(simulationTimeSeconds);
                    RefreshDisplays();
                    return true;
                }
            return false;
        }

        private void Resolve(LogicalConnectionState state, ConnectionResolution reason)
        {
            if (state.Resolution.HasValue) return;
            state.Resolution = reason;
            state.ResolvedAtSeconds = simulationTimeSeconds;
            NetworkHealthEvent penalty = health.Resolve(state, reason, simulationTimeSeconds);
            state.HealthPenaltyAmount = penalty?.Amount ?? 0f;
            if (penalty != null) recorder?.RecordHealthPenalty(penalty, health.Snapshot.Value);
            recorder?.RecordResolution(state, reason, simulationTimeSeconds);
            ConnectionResolved?.Invoke(state, reason);
        }

        public bool TryInjectPhantomLoad(string nodeId, float load, out string error)
            => TryInjectInfection(nodeId, InfectionKind.PhantomLoad, load, out error);

        public bool TryInjectInfection(string nodeId, InfectionKind kind, float parameter,
            out string error)
        {
            NodeResources target = FindNodeResources(nodeId);
            if (target == null)
            {
                error = "Unknown node: " + nodeId;
                return false;
            }
            if (!Enum.IsDefined(typeof(InfectionKind), kind) ||
                (kind != InfectionKind.Blackout &&
                 (parameter <= 0f || float.IsNaN(parameter) || float.IsInfinity(parameter))) ||
                kind == InfectionKind.Blackout && parameter != 0f)
            {
                error = "Infection parameter must be finite and positive for Phantom LOAD or Spawner; Blackout takes none.";
                return false;
            }
            if (runtimeConnections == null) ResetSimulation();
            var payload = new InfectionPayload(kind,
                kind == InfectionKind.PhantomLoad ? parameter : 0f,
                kind == InfectionKind.Spawner ? parameter : 0f);
            bool applied = ApplyInfection(target, payload, null, -1);
            if (applied && kind == InfectionKind.Blackout) InterruptRoutesThrough(target);
            error = applied ? null : kind + " is already active on " + target.View.NodeId;
            return applied;
        }

        public bool TryRemovePhantomLoad(string nodeId, out string error)
            => TryRemoveInfection(nodeId, InfectionKind.PhantomLoad, out error);

        public CleanStartResult StartClean(string nodeId, string command,
            out CleanSession session)
        {
            session = null;
            NodeResources target = FindNodeResources(nodeId);
            if (target == null)
            {
                recorder?.RecordPlayerAction(command, null, false, nodeId, "unknown_node");
                return CleanStartResult.UnknownNode;
            }
            if (runtimeConnections == null) ResetSimulation();
            if (activeClean != null)
            {
                recorder?.RecordPlayerAction(command, null, false, target.View.NodeId,
                    "clean_already_active");
                return CleanStartResult.AlreadyActive;
            }
            var captured = infections.FindAll(infection => infection.Target == target).ToArray();
            int actionSequence = recorder != null ? recorder.RecordPlayerAction(command,
                null, true, target.View.NodeId, "clean_started") : 0;
            int id = ++nextCleanSessionId;
            CleanProcess[] processes = BuildCleanProcesses(target.View.NodeId, id,
                captured.Length > 0, out int maliciousPid);
            activeClean = new CleanSession(id, target, captured, processes,
                maliciousPid, actionSequence);
            recorder?.RecordClean("clean_started", activeClean, simulationTimeSeconds);
            session = activeClean;
            return CleanStartResult.Started;
        }

        public ScanStartResult StartScan(string nodeId, string command,
            out NodeScanSession session)
        {
            session = null;
            NodeResources target = FindNodeResources(nodeId);
            if (target == null)
            {
                recorder?.RecordPlayerAction(command, null, false, nodeId, "unknown_node");
                return ScanStartResult.UnknownNode;
            }
            if (runtimeConnections == null) ResetSimulation();
            if (ConditionFor(target.View, ConditionKind.ScanJammed) != null)
            {
                recorder?.RecordPlayerAction(command, null, false, target.View.NodeId,
                    "scan_jammed");
                return ScanStartResult.Jammed;
            }
            if (activeScan != null)
            {
                bool sameNode = activeScan.Target == target;
                recorder?.RecordPlayerAction(command, null, false, target.View.NodeId,
                    sameNode ? "already_scanning" : "scan_busy");
                return sameNode ? ScanStartResult.AlreadyScanning : ScanStartResult.Busy;
            }
            int action = recorder != null ? recorder.RecordPlayerAction(command, null,
                true, target.View.NodeId, "scan_started") : 0;
            activeScan = new NodeScanSession(++nextScanId, target,
                simulationTimeSeconds, scanDurationSeconds, action);
            recorder?.RecordScanStarted(activeScan);
            session = activeScan;
            return ScanStartResult.Started;
        }

        private void CompleteDueScan()
        {
            NodeScanSession session = activeScan;
            if (session == null || simulationTimeSeconds + 0.000001f <
                session.ScheduledCompletionAtSeconds) return;
            bool infected = infections.Exists(infection => infection.Target == session.Target);
            session.Complete(infected ? NodeScanResult.Infected : NodeScanResult.Clean,
                session.ScheduledCompletionAtSeconds);
            activeScan = null;
            recorder?.RecordScanCompleted(session);
            ScanCompleted?.Invoke(session);
        }

        public CleanSelectionResult SelectCleanPid(int pid, out int removedCount)
        {
            removedCount = 0;
            CleanSession session = activeClean;
            if (session == null) return CleanSelectionResult.NoSession;
            bool listed = false;
            foreach (CleanProcess process in session.Processes)
                if (process.Pid == pid) { listed = true; break; }
            if (!listed)
            {
                recorder?.RecordClean("clean_invalid_pid", session, simulationTimeSeconds, pid);
                return CleanSelectionResult.InvalidPid;
            }
            if (pid != session.MaliciousPid)
            {
                NetworkHealthEvent penalty = health.PenalizeInnocentProcess(session.NodeId,
                    simulationTimeSeconds);
                recorder?.RecordHealthPenalty(penalty, health.Snapshot.Value);
                recorder?.RecordClean("clean_failed", session, simulationTimeSeconds, pid);
                recorder?.CaptureSnapshot(simulationTimeSeconds);
                activeClean = null;
                return CleanSelectionResult.InnocentProcess;
            }
            foreach (NodeInfection infection in session.StartingInfections)
            {
                if (!infections.Remove(infection)) continue;
                removedCount++;
                recorder?.RecordInfection("infection_removed", infection, simulationTimeSeconds,
                    "player_clean", session.ActionSequence);
            }
            RefreshPayloadAllocations();
            RefreshDisplays();
            recorder?.RecordClean("clean_succeeded", session, simulationTimeSeconds, pid,
                removedCount);
            recorder?.CaptureSnapshot(simulationTimeSeconds);
            activeClean = null;
            CleanSucceeded?.Invoke(session, removedCount);
            return CleanSelectionResult.Success;
        }

        public bool CancelClean()
        {
            if (activeClean == null) return false;
            recorder?.RecordClean("clean_canceled", activeClean, simulationTimeSeconds);
            activeClean = null;
            return true;
        }

        private CleanProcess[] BuildCleanProcesses(string nodeId, int sessionId,
            bool infected, out int maliciousPid)
        {
            unchecked
            {
                int seed = trafficDirector != null ? trafficDirector.Seed : 1701;
                foreach (char character in nodeId.ToUpperInvariant())
                    seed = seed * 31 + character;
                seed = seed * 31 + sessionId;
                var random = new System.Random(seed);
                int suspiciousIndex = infected ? random.Next(5) : -1;
                string[] names = { "systemd", "node-agent", "cache-sync", "log-rotate", "db-worker" };
                var processes = new CleanProcess[5];
                maliciousPid = 0;
                int basePid = 1100 + random.Next(7000);
                for (int i = 0; i < processes.Length; i++)
                {
                    int pid = basePid + i * 37 + random.Next(10);
                    bool suspicious = i == suspiciousIndex;
                    processes[i] = new CleanProcess(pid,
                        suspicious ? random.Next(82, 95) : random.Next(8, 45),
                        suspicious ? "sys-update-helper" : names[i]);
                    if (suspicious) maliciousPid = pid;
                }
                return processes;
            }
        }

        public bool TryRemoveInfection(string nodeId, InfectionKind kind, out string error)
        {
            NodeResources target = FindNodeResources(nodeId);
            if (target == null)
            {
                error = "Unknown node: " + nodeId;
                return false;
            }
            NodeInfection existing = infections.Find(infection =>
                infection.Target == target && infection.Payload.Kind == kind);
            if (existing == null)
            {
                error = "No " + kind + " on " + target.View.NodeId;
                return false;
            }
            infections.Remove(existing);
            recorder?.RecordInfection("infection_removed", existing, simulationTimeSeconds,
                "debug_removal");
            RefreshPayloadAllocations();
            RefreshDisplays();
            error = null;
            return true;
        }

        private bool ApplyInfection(NodeResources target, InfectionPayload payload,
            string sourceConnectionId, int sourceOperationIndex)
        {
            if (target == null || payload == null)
                throw new InvalidOperationException("Infection delivery needs a target and typed payload.");
            var infection = new NodeInfection(target, payload, simulationTimeSeconds,
                sourceConnectionId, sourceOperationIndex);
            if (infections.Exists(existing => existing.Target == target &&
                existing.Payload.Kind == payload.Kind))
            {
                recorder?.RecordInfection("infection_rejected", infection,
                    simulationTimeSeconds, "already_active");
                return false;
            }
            infections.Add(infection);
            recorder?.RecordInfection("infection_applied", infection,
                simulationTimeSeconds, sourceConnectionId == null ? "debug_injection" : "delivery");
            RefreshPayloadAllocations();
            RefreshDisplays();
            return true;
        }

        internal bool ApplyPayloadEffect(NodeView targetNode, string payloadId)
        {
            NodeResources target = FindNodeResources(targetNode.NodeId);
            return ApplyInfection(target,
                new InfectionPayload(InfectionKind.PhantomLoad, defaultPhantomLoad),
                payloadId, -1);
        }

        public void RefreshPayloadAllocations()
        {
            if (runtimeConnections == null || nodes == null) return;
            ResourceAllocator.RefreshAllocations(nodes, runtimeConnections, infections,
                payloadDirector != null ? payloadDirector.DemandAt : null);
            RefreshDisplays();
        }

        public bool IsNodeOffline(NodeView view) => view != null &&
            (infections.Exists(infection => infection.Target.View == view &&
                infection.Payload.Kind == InfectionKind.Blackout) ||
             wormDirector != null && wormDirector.IsNodeIsolated(view));

        private ConnectionTerminationCause UnavailabilityCause(NodeView view) =>
            wormDirector != null && wormDirector.IsNodeIsolated(view) ?
                ConnectionTerminationCause.Isolation : ConnectionTerminationCause.Blackout;

        public int InterruptForIsolation(NodeResources node) =>
            InterruptRoutesThrough(node, ConnectionTerminationCause.Isolation);

        private int InterruptRoutesThrough(NodeResources offline,
            ConnectionTerminationCause cause = ConnectionTerminationCause.Blackout)
        {
            var interrupted = new List<LogicalConnectionState>();
            foreach (LogicalConnectionState state in runtimeConnections)
            {
                if (state.IsActive) {
                    foreach (RouteNode routeNode in state.Definition.Route)
                        if (routeNode.Node == offline.View)
                        { interrupted.Add(state); break; }
                }
                else if (state.IsConnecting)
                    for (int i = 0; i <= state.ReachedRouteNodeIndex; i++)
                        if (state.Definition.Route[i].Node == offline.View)
                        { interrupted.Add(state); break; }
            }
            interrupted.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            foreach (LogicalConnectionState state in interrupted)
            {
                CancelConnection(state, offline.View.NodeId, cause);
            }
            RefreshDisplays();
            return interrupted.Count;
        }

        private void CancelConnection(LogicalConnectionState state, string failureNodeId,
            ConnectionTerminationCause cause)
        {
            if (state.Resolution.HasValue) return;
            bool wasActive = state.IsActive;
            state.TerminationPhase = state.Lifecycle;
            state.TerminationCause = cause;
            state.FailureNodeId = failureNodeId;
            state.Lifecycle = Lifecycle.Complete;
            state.AllocatedLoad = 0f;
            if (wasActive) state.Behaviors.EndConnection(simulationTimeSeconds);
            state.NaturalLoadDelta = 0f;
            Resolve(state, ConnectionResolution.Canceled);
            var next = new List<LogicalConnectionState>(runtimeConnections);
            next.Remove(state);
            runtimeConnections = next.ToArray();
            pathPresenter?.Retire(state);
            RefreshPayloadAllocations();
        }

        private void StepSpawners()
        {
            if (trafficDirector == null) return;
            // Copy avoids effects from subsequently queued or delivered children changing iteration.
            NodeInfection[] current = infections.ToArray();
            foreach (NodeInfection infection in current)
            {
                if (infection.Payload.Kind != InfectionKind.Spawner || !infections.Contains(infection))
                    continue;
                int attempts = 0;
                while (simulationTimeSeconds + 0.00001f >= infection.NextSpawnAtSeconds &&
                    attempts++ < 128)
                {
                    float attemptAt = infection.NextSpawnAtSeconds;
                    infection.NextSpawnAtSeconds += infection.Payload.CadenceSeconds;
                    ConnectionSpawnPlan child = trafficDirector.QueueSpawnerChild(infection,
                        simulationTimeSeconds, out string reason);
                    recorder?.RecordSpawnerAttempt(infection, child, reason, attemptAt);
                }
                if (simulationTimeSeconds + 0.00001f >= infection.NextSpawnAtSeconds)
                {
                    recorder?.RecordSpawnerAttempt(infection, null,
                        "step_attempt_limit", simulationTimeSeconds);
                    infection.NextSpawnAtSeconds = simulationTimeSeconds +
                        Mathf.Max(Time.fixedDeltaTime, infection.Payload.CadenceSeconds);
                }
            }
        }

        public NodeResources FindNodeResources(string id)
        {
            if (id == null || nodes == null) return null;
            foreach (NodeResources node in nodes)
                if (node?.View != null && string.Equals(node.View.NodeId, id,
                    StringComparison.OrdinalIgnoreCase)) return node;
            return null;
        }

        public void ResetSimulation()
        {
            activeClean = null;
            activeScan?.Reset(simulationTimeSeconds);
            activeScan = null;
            nextScanId = 0;
            nextCleanSessionId = 0;
            ClearAllocatedUsage();
            simulationTimeSeconds = 0f;
            health = new NetworkHealth(healthTuning ?? new NetworkHealthTuning());
            history.Clear();
            usedIds.Clear();
            occurrences.Clear();
            infections.Clear();
            conditions.Clear();
            if (payloadDirector == null) payloadDirector = GetComponent<PayloadDirector>();
            payloadDirector?.ResetDirector(trafficDirector != null ? trafficDirector.Seed : 1701);
            if (wormDirector == null) wormDirector = GetComponent<WormDirector>();
            wormDirector?.ResetDirector(trafficDirector != null ? trafficDirector.Seed : 1701);
            SimulationReset?.Invoke();
            var states = new List<LogicalConnectionState>();
            if (runMode == ConnectionRunMode.AuthoredWitness && connections != null)
            {
                foreach (LogicalConnectionDefinition definition in connections)
                {
                    LogicalConnectionState state = BuildState(definition);
                    if (!usedIds.Add(state.ConnectionId))
                        throw new InvalidOperationException("Logical connections require unique IDs.");
                    states.Add(state);
                    history.Add(state.ConnectionId, state);
                    state.SpawnedAtSeconds = simulationTimeSeconds;
                    state.TravelSecondsPerLink = connectionTravelSecondsPerLink;
                }
            }

            states.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            runtimeConnections = states.ToArray();
            trafficDirector?.ResetDirector();
            if (Application.isPlaying)
            {
                recorder?.BeginRun();
                foreach (LogicalConnectionState state in runtimeConnections)
                {
                    recorder?.RecordConnectionSpawned(state, simulationTimeSeconds);
                }
                recorder?.CaptureSnapshot(simulationTimeSeconds);
                recorder?.Save(false);
            }
            if (Application.isPlaying && pathPresenter != null)
                pathPresenter.Bind(runtimeConnections);
            RefreshDisplays();
        }

        private LogicalConnectionState BuildState(LogicalConnectionDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.ConnectionId) ||
                definition.Route == null || definition.Route.Length < 2)
                throw new InvalidOperationException("Connection needs an ID and route of at least two nodes.");
            var nodeByView = new Dictionary<NodeView, NodeResources>();
            foreach (NodeResources node in nodes)
            {
                if (node?.View == null || nodeByView.ContainsKey(node.View))
                    throw new InvalidOperationException("Nodes require unique, assigned views.");
                nodeByView.Add(node.View, node);
            }
            var visited = new HashSet<NodeView>();
            foreach (RouteNode requirement in definition.Route)
                if (requirement?.Node == null || !nodeByView.ContainsKey(requirement.Node) ||
                    !visited.Add(requirement.Node))
                    throw new InvalidOperationException("Connection has an invalid or repeated route node.");
            for (int i = 0; i < definition.OperationCount; i++)
            {
                if (definition.WorkAt(i) <= 0f || definition.LoadAt(i) <= 0f ||
                    float.IsNaN(definition.WorkAt(i)) || float.IsInfinity(definition.WorkAt(i)) ||
                    float.IsNaN(definition.LoadAt(i)) || float.IsInfinity(definition.LoadAt(i)) ||
                    !Enum.IsDefined(typeof(OperationKind), definition.OperationAt(i)) ||
                    !Enum.IsDefined(typeof(CompletionEffect), definition.EffectAt(i)))
                    throw new InvalidOperationException("Connection operation needs finite positive WORK/LOAD and valid kinds/effects.");
                InfectionPayload payload = definition.PayloadAt(i);
                if (definition.EffectAt(i) == CompletionEffect.InfectionDelivery &&
                    (payload == null || !payload.IsValid))
                    throw new InvalidOperationException("Delivery needs a valid typed infection payload.");
                if (definition.EffectAt(i) == CompletionEffect.ConditionApplication &&
                    (definition.ConditionAt(i) == null || !definition.ConditionAt(i).IsValid))
                    throw new InvalidOperationException("Condition application needs a valid typed payload.");
                ValidateBehaviors(definition.BehaviorsAt(i), BehaviorScope.Operation);
            }
            ValidateBehaviors(definition.ConnectionBehaviors, BehaviorScope.Connection);
            LoaderAttackPlan attack = definition.LoaderAttack;
            if (attack != null && (!definition.IsHostile ||
                !Enum.IsDefined(typeof(LoaderStructure), attack.structure) ||
                !Enum.IsDefined(typeof(LoaderPattern), attack.pattern) ||
                attack.attackOperationIndex < 0 ||
                attack.attackOperationIndex >= definition.OperationCount ||
                attack.attackOperationIndex != definition.OperationCount - 1 ||
                !FinitePositive(attack.targetCapacity) ||
                !FinitePositive(attack.normalLoad) ||
                !FinitePositive(attack.attackLoad) ||
                attack.attackLoad <= attack.normalLoad ||
                !FiniteNonnegative(attack.onsetSeconds) ||
                !FinitePositive(attack.durationSeconds) ||
                !FiniteNonnegative(attack.spikeQuietSeconds) ||
                !FiniteNonnegative(attack.spikeElevatedSeconds) ||
                !Array.Exists(definition.BehaviorsAt(attack.attackOperationIndex),
                    behavior => behavior.Id == "LOADER_ACTIVE" &&
                        behavior.WorkRateMultiplier == 0f &&
                        behavior.StartAfterSeconds == attack.onsetSeconds)))
                throw new InvalidOperationException("Generated LOADER needs a valid attack plan and activation behavior.");
            var routeLinks = new PhysicalLinkReference[definition.Route.Length - 1];
            for (int i = 0; i < routeLinks.Length; i++)
            {
                NodeView a = definition.Route[i].Node;
                NodeView b = definition.Route[i + 1].Node;
                foreach (PhysicalLinkReference link in physicalLinks)
                {
                    if (link?.View == null) throw new InvalidOperationException("Physical link lacks a view.");
                    if (!((link.View.NodeA == a && link.View.NodeB == b) ||
                        (link.View.NodeA == b && link.View.NodeB == a))) continue;
                    if (routeLinks[i] != null) throw new InvalidOperationException("Ambiguous route link.");
                    routeLinks[i] = link;
                }
                if (routeLinks[i] == null) throw new InvalidOperationException("Route has no physical link.");
            }
            var state = new LogicalConnectionState(definition, routeLinks,
                nodeByView[definition.TargetNode]);
            state.Behaviors.ModifierChanged += change =>
            {
                recorder?.RecordModifierChange(state, change);
                if (definition.LoaderAttack == null ||
                    change.Action != ModifierChangeKind.Added ||
                    change.SourceId != "LOADER_ACTIVE" ||
                    change.OperationIndex != definition.LoaderAttack.attackOperationIndex ||
                    state.LoaderActivatedAtSeconds >= 0f) return;
                state.LoaderActivatedAtSeconds = change.TimeSeconds;
                state.LoaderWithdrawAtSeconds = change.TimeSeconds +
                    definition.LoaderAttack.durationSeconds;
                recorder?.RecordLoaderActivated(state, change.TimeSeconds);
            };
            return state;
        }

        private void WithdrawDueLoaders()
        {
            if (runtimeConnections == null) return;
            var due = new List<LogicalConnectionState>();
            foreach (LogicalConnectionState state in runtimeConnections)
                if (state.IsActive && state.Definition.LoaderAttack != null &&
                    state.LoaderWithdrawAtSeconds >= 0f &&
                    state.LoaderWithdrawAtSeconds <= simulationTimeSeconds + 0.00001f)
                    due.Add(state);
            due.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            foreach (LogicalConnectionState state in due)
            {
                if (state.Resolution.HasValue) continue;
                state.TerminationPhase = state.Lifecycle;
                state.TerminationCause = ConnectionTerminationCause.AttackerWithdrawal;
                state.Lifecycle = Lifecycle.Complete;
                state.AllocatedLoad = 0f;
                state.Behaviors.EndConnection(simulationTimeSeconds);
                state.NaturalLoadDelta = 0f;
                recorder?.RecordLoaderWithdrawal(state, simulationTimeSeconds);
                Resolve(state, ConnectionResolution.Withdrawn);
                var next = new List<LogicalConnectionState>(runtimeConnections);
                next.Remove(state);
                runtimeConnections = next.ToArray();
                pathPresenter?.Retire(state);
            }
            if (due.Count > 0)
                RefreshPayloadAllocations();
        }

        private static void ValidateBehaviors(TimedModifierBehaviorDefinition[] definitions,
            BehaviorScope scope)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (TimedModifierBehaviorDefinition behavior in definitions)
                if (behavior == null || string.IsNullOrWhiteSpace(behavior.Id) ||
                    !ids.Add(behavior.Id) ||
                    !Enum.IsDefined(typeof(ConnectionBehaviorKind), behavior.Kind) ||
                    float.IsNaN(behavior.StartAfterSeconds) ||
                    float.IsInfinity(behavior.StartAfterSeconds) ||
                    behavior.StartAfterSeconds < 0f ||
                    float.IsNaN(behavior.StopAfterSeconds) ||
                    float.IsInfinity(behavior.StopAfterSeconds) ||
                    behavior.StopAfterSeconds < 0f ||
                    (behavior.StopAfterSeconds > 0f &&
                        behavior.StopAfterSeconds <= behavior.StartAfterSeconds) ||
                    float.IsNaN(behavior.WorkDelta) || float.IsInfinity(behavior.WorkDelta) ||
                    (scope == BehaviorScope.Connection && behavior.WorkDelta != 0f) ||
                    float.IsNaN(behavior.LoadDelta) || float.IsInfinity(behavior.LoadDelta) ||
                    float.IsNaN(behavior.WorkRateMultiplier) ||
                    float.IsInfinity(behavior.WorkRateMultiplier) ||
                    behavior.WorkRateMultiplier < 0f ||
                    ((behavior.Kind == ConnectionBehaviorKind.Overload ||
                        behavior.Kind == ConnectionBehaviorKind.Spiking) &&
                        (behavior.WorkDelta != 0f || behavior.LoadDelta <= 0f)) ||
                    (behavior.Kind == ConnectionBehaviorKind.Waving &&
                        (behavior.WorkDelta != 0f || behavior.LoadDelta != 0f ||
                         behavior.WorkRateMultiplier != 1f ||
                         behavior.StopAfterSeconds != 0f ||
                         !FinitePositive(behavior.WavePeriodSeconds) ||
                         float.IsNaN(behavior.MinimumLoadOffset) ||
                         float.IsInfinity(behavior.MinimumLoadOffset) ||
                         float.IsNaN(behavior.MaximumLoadOffset) ||
                         float.IsInfinity(behavior.MaximumLoadOffset) ||
                         behavior.MinimumLoadOffset > 0f ||
                         behavior.MaximumLoadOffset < 0f ||
                         behavior.MinimumLoadOffset > behavior.MaximumLoadOffset)) ||
                    (behavior.Kind == ConnectionBehaviorKind.Overload &&
                        behavior.StopAfterSeconds != 0f) ||
                    (behavior.Kind == ConnectionBehaviorKind.Spiking &&
                        (float.IsNaN(behavior.NormalIntervalSeconds) ||
                         float.IsInfinity(behavior.NormalIntervalSeconds) ||
                         float.IsNaN(behavior.ElevatedIntervalSeconds) ||
                         float.IsInfinity(behavior.ElevatedIntervalSeconds) ||
                         float.IsNaN(behavior.SpikeInitialDelayOverrideSeconds) ||
                         float.IsInfinity(behavior.SpikeInitialDelayOverrideSeconds) ||
                         behavior.SpikeInitialDelayOverrideSeconds < 0f ||
                         behavior.NormalIntervalSeconds <= 0f ||
                         behavior.ElevatedIntervalSeconds <= 0f)))
                    throw new InvalidOperationException("Behavior needs a unique ID, finite deltas, " +
                        "valid timing and keyword LOAD bounds, and connection-scope WORK delta zero.");
        }

        private static bool FinitePositive(float value) =>
            value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool FiniteNonnegative(float value) =>
            value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);

        private void ClearAllocatedUsage()
        {
            if (nodes != null)
                foreach (NodeResources node in nodes)
                {
                    node?.ClearConnectionUsage();
                    node?.SetInfectionUsage(0f, 0f);
                    node?.SetDefensiveUsage(0f, 0f);
                }
        }

        public void RefreshDisplays()
        {
            if (nodes != null)
            {
                foreach (NodeResources node in nodes)
                {
                    if (node?.View != null)
                    {
                        node.View.RenderResources(node);
                    }
                }
            }

            if (physicalLinks != null)
            {
                foreach (PhysicalLinkReference link in physicalLinks)
                {
                    if (link?.View != null)
                        link.View.RenderLink();
                }
            }

            if (connectionDebugReadout != null)
            {
                string value = BuildDebugText();
                if (connectionDebugReadout.text != value)
                    connectionDebugReadout.text = value;
            }
        }

        private string BuildDebugText()
        {
            if (runtimeConnections == null)
                return "CONNECTIONS  •  Play to run operations";

            var output = new StringBuilder("CONNECTIONS  •  allocated / desired LOAD; 1 LOAD = 1 WORK/s");
            foreach (LogicalConnectionState connection in runtimeConnections)
            {
                output.Append('\n').Append(connection.ConnectionId.ToLowerInvariant()).Append(' ')
                    .Append(connection.CurrentOperation.ToString().ToUpperInvariant())
                    .Append(' ').Append(connection.CurrentOperationIndex + 1)
                    .Append('/').Append(connection.OperationCount).Append(' ');
                if (connection.IsComplete)
                {
                    output.Append("DONE  ").Append(connection.CompletedWork.ToString("0.#"))
                        .Append('/').Append(connection.CurrentTotalWork.ToString("0.#")).Append(" work units");
                    continue;
                }

                if (connection.IsPending)
                {
                    output.Append("PENDING  starts ")
                        .Append(connection.Definition.StartDelaySeconds.ToString("0.#")).Append(" s");
                    continue;
                }

                if (connection.IsConnecting)
                {
                    output.Append("CONNECTING  ")
                        .Append(Mathf.RoundToInt(connection.RouteReveal * 100f))
                        .Append("% route");
                    continue;
                }

                if (connection.IsTransitioning)
                    output.Append("NEXT OP  ");
                output.Append(connection.CompletedWork.ToString("0.#")).Append('/')
                    .Append(connection.CurrentTotalWork.ToString("0.#")).Append(" WORK  ")
                    .Append(connection.AllocatedLoad.ToString("0.#")).Append('/')
                    .Append(connection.DesiredLoad.ToString("0.#")).Append(" LOAD  ")
                    .Append(Mathf.RoundToInt(connection.Satisfaction * 100f)).Append('%')
                    .Append("  [").Append(connection.TargetNode.NodeId).Append(']');
            }
            return output.ToString();
        }
    }
}
