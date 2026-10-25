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
        [SerializeField] private NetworkHealthTuning healthTuning = new NetworkHealthTuning();
        [SerializeField, Min(0.01f)] private float defaultPhantomLoad = 8f;

        private LogicalConnectionState[] runtimeConnections;
        private readonly Dictionary<string, LogicalConnectionState> history =
            new Dictionary<string, LogicalConnectionState>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ConnectionEffectOccurrence> occurrences =
            new List<ConnectionEffectOccurrence>();
        private readonly List<NodeInfection> infections = new List<NodeInfection>();
        private float simulationTimeSeconds;
        private NetworkHealth health;

        public NodeResources[] Nodes => nodes;
        public PhysicalLinkReference[] PhysicalLinks => physicalLinks;
        public LogicalConnectionDefinition[] Definitions => connections;
        public IReadOnlyList<LogicalConnectionState> RuntimeConnections => runtimeConnections;
        public float SimulationTimeSeconds => simulationTimeSeconds;
        public NetworkHealthSnapshot Health => health?.Snapshot;
        public ConnectionRunMode RunMode => runMode;
        public ConnectionsRunRecorder Recorder => recorder;
        public IReadOnlyList<ConnectionEffectOccurrence> Occurrences => occurrences;
        public IReadOnlyList<NodeInfection> Infections => infections;
        public float DefaultPhantomLoad => defaultPhantomLoad;
        public ConnectionIdentityPalette PresentationPalette => pathPresenter != null ?
            pathPresenter.IdentityPalette : null;
        public event Action<LogicalConnectionState, ConnectionResolution> ConnectionResolved;
        public event Action<ConnectionEffectOccurrence> EffectOccurred;
        public bool HasEverUsedId(string id) => usedIds.Contains(id);
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
                ResetSimulation();
        }

        private void FixedUpdate()
        {
            if (Application.isPlaying)
                StepSimulation(Time.fixedDeltaTime);
        }

        private void OnDisable()
        {
            ClearAllocatedUsage();
            infections.Clear();
            runtimeConnections = null;
        }

        private void OnValidate()
        {
            if (float.IsNaN(defaultPhantomLoad) || float.IsInfinity(defaultPhantomLoad) ||
                defaultPhantomLoad <= 0f) defaultPhantomLoad = 8f;
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
            foreach (LogicalConnectionState connection in runtimeConnections)
            {
                if (connection.IsPending && simulationTimeSeconds >= connection.Definition.StartDelaySeconds)
                {
                    connection.Lifecycle = Lifecycle.Active;
                    connection.EstablishedAtSeconds = simulationTimeSeconds;
                    health.Activate(connection, simulationTimeSeconds);
                    connection.Behaviors.StartConnection();
                    connection.Behaviors.StartOperation(connection.CurrentOperationIndex);
                    recorder?.RecordOperationStarted(connection, simulationTimeSeconds);
                }
                else if (connection.IsTransitioning)
                {
                    connection.Behaviors.StartOperation(connection.CurrentOperationIndex);
                    recorder?.RecordOperationStarted(connection, simulationTimeSeconds);
                }
                if (connection.IsActive)
                    connection.Behaviors.Advance(deltaSeconds, simulationTimeSeconds);
            }
            List<ResourceAllocator.OperationCompletion> completed =
                ResourceAllocator.Step(nodes, runtimeConnections, infections, deltaSeconds,
                    allocated => health.Step(allocated, simulationTimeSeconds, deltaSeconds));
            simulationTimeSeconds += deltaSeconds;
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
                        ApplyInfection(FindNodeResources(occurrence.TargetNodeId),
                            occurrence.Payload, occurrence.ConnectionId,
                            occurrence.OperationIndex);
                    EffectOccurred?.Invoke(occurrence);
                }
                completion.State.Behaviors.EndOperation(simulationTimeSeconds);
                if (completion.State.IsComplete)
                {
                    completion.State.Behaviors.EndConnection(simulationTimeSeconds);
                    Resolve(completion.State, ConnectionResolution.Completed);
                }
            }
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
            recorder?.CaptureDueSnapshot(simulationTimeSeconds);
            RefreshDisplays();
        }

        public LogicalConnectionState RegisterConnection(LogicalConnectionDefinition definition)
        {
            if (runtimeConnections == null) ResetSimulation();
            LogicalConnectionState state = BuildState(definition);
            if (!usedIds.Add(state.ConnectionId))
                throw new InvalidOperationException("Connection ID has already been used: " + state.ConnectionId);
            state.EstablishedAtSeconds = simulationTimeSeconds;
            if (state.IsActive)
            {
                health.Activate(state, simulationTimeSeconds);
                state.Behaviors.StartConnection();
                state.Behaviors.StartOperation(state.CurrentOperationIndex);
            }
            var next = new List<LogicalConnectionState>(runtimeConnections) { state };
            runtimeConnections = next.ToArray();
            history.Add(state.ConnectionId, state);
            recorder?.RecordConnectionSpawned(state, simulationTimeSeconds);
            if (state.IsActive)
                recorder?.RecordOperationStarted(state, simulationTimeSeconds);
            if (Application.isPlaying) pathPresenter?.Add(state);
            return state;
        }

        public void ValidatePlan(ConnectionSpawnPlan plan)
        {
            if (plan == null || plan.Operations.Length == 0 ||
                string.IsNullOrWhiteSpace(plan.ProfileId) || HasEverUsedId(plan.ConnectionId))
                throw new InvalidOperationException("Spawn plan needs operations, profile ID, and an unused ID.");
            BuildState(LogicalConnectionDefinition.FromPlan(plan));
        }

        public bool KillConnection(string id)
        {
            if (runtimeConnections == null || id == null) return false;
            foreach (LogicalConnectionState state in runtimeConnections)
                if (string.Equals(state.ConnectionId, id, StringComparison.OrdinalIgnoreCase) && state.IsActive)
                {
                    state.Lifecycle = Lifecycle.Complete;
                    state.AllocatedLoad = 0f;
                    state.Behaviors.EndConnection(simulationTimeSeconds);
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
            state.Resolution = reason;
            state.ResolvedAtSeconds = simulationTimeSeconds;
            NetworkHealthEvent penalty = health.Resolve(state, reason, simulationTimeSeconds);
            if (penalty != null) recorder?.RecordHealthPenalty(penalty, health.Snapshot.Value);
            recorder?.RecordResolution(state, reason, simulationTimeSeconds);
            ConnectionResolved?.Invoke(state, reason);
        }

        public bool TryInjectPhantomLoad(string nodeId, float load, out string error)
        {
            NodeResources target = FindNodeResources(nodeId);
            if (target == null)
            {
                error = "Unknown node: " + nodeId;
                return false;
            }
            if (load <= 0f || float.IsNaN(load) || float.IsInfinity(load))
            {
                error = "Phantom LOAD requires a finite positive LOAD amount.";
                return false;
            }
            if (runtimeConnections == null) ResetSimulation();
            bool applied = ApplyInfection(target, new InfectionPayload(InfectionKind.PhantomLoad,
                load), null, -1);
            error = applied ? null : "Phantom LOAD is already active on " + target.View.NodeId;
            return applied;
        }

        public bool TryRemovePhantomLoad(string nodeId, out string error)
        {
            NodeResources target = FindNodeResources(nodeId);
            if (target == null)
            {
                error = "Unknown node: " + nodeId;
                return false;
            }
            NodeInfection existing = infections.Find(infection =>
                infection.Target == target && infection.Payload.Kind == InfectionKind.PhantomLoad);
            if (existing == null)
            {
                error = "No Phantom LOAD on " + target.View.NodeId;
                return false;
            }
            infections.Remove(existing);
            recorder?.RecordInfection("infection_removed", existing, simulationTimeSeconds,
                "debug_removal");
            ResourceAllocator.RefreshAllocations(nodes, runtimeConnections,
                infections);
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
            ResourceAllocator.RefreshAllocations(nodes, runtimeConnections, infections);
            RefreshDisplays();
            return true;
        }

        private NodeResources FindNodeResources(string id)
        {
            if (id == null || nodes == null) return null;
            foreach (NodeResources node in nodes)
                if (node?.View != null && string.Equals(node.View.NodeId, id,
                    StringComparison.OrdinalIgnoreCase)) return node;
            return null;
        }

        public void ResetSimulation()
        {
            ClearAllocatedUsage();
            simulationTimeSeconds = 0f;
            health = new NetworkHealth(healthTuning ?? new NetworkHealthTuning());
            history.Clear();
            usedIds.Clear();
            occurrences.Clear();
            infections.Clear();
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
                    if (state.IsActive)
                    {
                        health.Activate(state, simulationTimeSeconds);
                        state.Behaviors.StartConnection();
                        state.Behaviors.StartOperation(state.CurrentOperationIndex);
                    }
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
                    if (state.IsActive) recorder?.RecordOperationStarted(state, simulationTimeSeconds);
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
                    (payload == null || payload.Kind != InfectionKind.PhantomLoad ||
                     payload.RequestedLoad <= 0f ||
                     float.IsNaN(payload.RequestedLoad) ||
                     float.IsInfinity(payload.RequestedLoad)))
                    throw new InvalidOperationException("Delivery needs a valid typed infection payload.");
                ValidateBehaviors(definition.BehaviorsAt(i), BehaviorScope.Operation);
            }
            ValidateBehaviors(definition.ConnectionBehaviors, BehaviorScope.Connection);
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
            state.Behaviors.ModifierChanged += change => recorder?.RecordModifierChange(state, change);
            return state;
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
                    (behavior.Kind != ConnectionBehaviorKind.TimedAdjustment &&
                        (behavior.WorkDelta != 0f || behavior.LoadDelta <= 0f)) ||
                    (behavior.Kind == ConnectionBehaviorKind.Overload &&
                        behavior.StopAfterSeconds != 0f) ||
                    (behavior.Kind == ConnectionBehaviorKind.Spiking &&
                        (float.IsNaN(behavior.NormalIntervalSeconds) ||
                         float.IsInfinity(behavior.NormalIntervalSeconds) ||
                         float.IsNaN(behavior.ElevatedIntervalSeconds) ||
                         float.IsInfinity(behavior.ElevatedIntervalSeconds) ||
                         behavior.NormalIntervalSeconds <= 0f ||
                         behavior.ElevatedIntervalSeconds <= 0f)))
                    throw new InvalidOperationException("Behavior needs a unique ID, finite deltas, " +
                        "valid timing and positive keyword LOAD, and connection-scope WORK delta zero.");
        }

        private void ClearAllocatedUsage()
        {
            if (nodes != null)
                foreach (NodeResources node in nodes)
                {
                    node?.ClearConnectionUsage();
                    node?.SetInfectionUsage(0f, 0f);
                }
        }

        public void RefreshDisplays()
        {
            if (nodes != null)
            {
                foreach (NodeResources node in nodes)
                {
                    if (node?.View != null)
                        node.View.RenderResources(node);
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
