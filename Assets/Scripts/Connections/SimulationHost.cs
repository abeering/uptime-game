using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    // The only clock and owner of resource state for the Connections game.
    [ExecuteAlways]
    public sealed class SimulationHost : MonoBehaviour
    {
        [SerializeField] private NodeResources[] nodes;
        [SerializeField] private PhysicalLinkReference[] physicalLinks;
        [SerializeField] private LogicalConnectionDefinition[] connections;
        [SerializeField] private TMP_Text connectionDebugReadout;
        [SerializeField] private LogicalPathPresenter pathPresenter;

        private LogicalConnectionState[] runtimeConnections;
        private float simulationTimeSeconds;

        public NodeResources[] Nodes => nodes;
        public PhysicalLinkReference[] PhysicalLinks => physicalLinks;
        public LogicalConnectionDefinition[] Definitions => connections;
        public IReadOnlyList<LogicalConnectionState> RuntimeConnections => runtimeConnections;
        public float SimulationTimeSeconds => simulationTimeSeconds;

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
            runtimeConnections = null;
        }

        private void OnValidate()
        {
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
            if (runtimeConnections == null)
                ResetSimulation();

            foreach (LogicalConnectionState connection in runtimeConnections)
            {
                if (!connection.IsPending || simulationTimeSeconds < connection.Definition.StartDelaySeconds)
                    continue;
                connection.Lifecycle = Lifecycle.Active;
                connection.EstablishedAtSeconds = simulationTimeSeconds;
            }
            ResourceAllocator.Step(nodes, runtimeConnections, deltaSeconds);
            simulationTimeSeconds += deltaSeconds;
            RefreshDisplays();
        }

        public void ResetSimulation()
        {
            ClearAllocatedUsage();
            simulationTimeSeconds = 0f;
            var states = new List<LogicalConnectionState>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var nodeViews = new HashSet<NodeView>();
            var nodeByView = new Dictionary<NodeView, NodeResources>();
            foreach (NodeResources node in nodes)
            {
                if (node?.View == null || !nodeViews.Add(node.View))
                    throw new InvalidOperationException("Nodes require unique, assigned views.");
                nodeByView.Add(node.View, node);
            }

            if (connections != null)
            {
                foreach (LogicalConnectionDefinition definition in connections)
                {
                    if (definition == null || string.IsNullOrWhiteSpace(definition.ConnectionId) ||
                        !ids.Add(definition.ConnectionId))
                        throw new InvalidOperationException("Logical connections require unique, nonempty IDs.");
                    if (definition.TotalWork <= 0f || definition.DesiredLoad <= 0f ||
                        definition.Route == null || definition.Route.Length < 2)
                        throw new InvalidOperationException($"Connection {definition.ConnectionId} needs work, rate, and a route of at least two nodes.");

                    var visited = new HashSet<NodeView>();
                    foreach (RouteNode requirement in definition.Route)
                    {
                        if (requirement?.Node == null || !nodeViews.Contains(requirement.Node) ||
                            !visited.Add(requirement.Node))
                            throw new InvalidOperationException($"Connection {definition.ConnectionId} has an invalid or repeated route node.");
                    }

                    var routeLinks = new PhysicalLinkReference[definition.Route.Length - 1];
                    for (int i = 0; i < routeLinks.Length; i++)
                    {
                        NodeView a = definition.Route[i].Node;
                        NodeView b = definition.Route[i + 1].Node;
                        foreach (PhysicalLinkReference link in physicalLinks)
                        {
                            if (link?.View == null)
                                throw new InvalidOperationException("Physical links require assigned views.");
                            bool connects = (link.View.NodeA == a && link.View.NodeB == b) ||
                                (link.View.NodeA == b && link.View.NodeB == a);
                            if (!connects)
                                continue;
                            if (routeLinks[i] != null)
                                throw new InvalidOperationException($"Route {definition.ConnectionId} has ambiguous physical links.");
                            routeLinks[i] = link;
                        }
                        if (routeLinks[i] == null)
                            throw new InvalidOperationException($"Route {definition.ConnectionId} has no physical link from {a.NodeId} to {b.NodeId}.");
                    }
                    states.Add(new LogicalConnectionState(definition, routeLinks,
                        nodeByView[definition.TargetNode]));
                }
            }

            states.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            runtimeConnections = states.ToArray();
            if (Application.isPlaying && pathPresenter != null)
                pathPresenter.Bind(runtimeConnections);
            RefreshDisplays();
        }

        private void ClearAllocatedUsage()
        {
            if (nodes != null)
                foreach (NodeResources node in nodes)
                    node?.ClearUsage();
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
                return "CONNECTIONS  •  Play to run authored operations";

            var output = new StringBuilder("CONNECTIONS  •  allocated / desired LOAD; 1 LOAD = 1 WORK/s");
            foreach (LogicalConnectionState connection in runtimeConnections)
            {
                output.Append('\n').Append(connection.ConnectionId).Append(' ')
                    .Append(connection.Definition.CurrentOperation.ToString().ToUpperInvariant()).Append(' ');
                if (connection.IsComplete)
                {
                    output.Append("DONE  ").Append(connection.CompletedWork.ToString("0.#"))
                        .Append('/').Append(connection.Definition.TotalWork.ToString("0.#")).Append(" work units");
                    continue;
                }

                if (connection.IsPending)
                {
                    output.Append("PENDING  starts ")
                        .Append(connection.Definition.StartDelaySeconds.ToString("0.#")).Append(" s");
                    continue;
                }

                output.Append(connection.CompletedWork.ToString("0.#")).Append('/')
                    .Append(connection.Definition.TotalWork.ToString("0.#")).Append(" WORK  ")
                    .Append(connection.AllocatedLoad.ToString("0.#")).Append('/')
                    .Append(connection.Definition.DesiredLoad.ToString("0.#")).Append(" LOAD  ")
                    .Append(Mathf.RoundToInt(connection.Satisfaction * 100f)).Append('%')
                    .Append("  [").Append(connection.TargetNode.NodeId).Append(']');
            }
            return output.ToString();
        }
    }
}
