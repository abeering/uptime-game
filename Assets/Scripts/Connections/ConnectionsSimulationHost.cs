using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    // The only clock and owner of resource state for the Connections game.
    [ExecuteAlways]
    public sealed class ConnectionsSimulationHost : MonoBehaviour
    {
        [SerializeField] private ConnectionsNodeResources[] nodes;
        [SerializeField] private ConnectionsPhysicalLinkResources[] physicalLinks;
        [SerializeField] private ConnectionsLogicalConnectionDefinition[] connections;
        [SerializeField] private TMP_Text connectionDebugReadout;
        [SerializeField] private ConnectionsLogicalPathPresenter pathPresenter;

        private ConnectionsLogicalConnectionState[] runtimeConnections;
        private float simulationTimeSeconds;

        public ConnectionsNodeResources[] Nodes => nodes;
        public ConnectionsPhysicalLinkResources[] PhysicalLinks => physicalLinks;
        public ConnectionsLogicalConnectionDefinition[] Definitions => connections;
        public IReadOnlyList<ConnectionsLogicalConnectionState> RuntimeConnections => runtimeConnections;
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
                foreach (ConnectionsNodeResources node in nodes)
                    node?.ClampToCapacity();
            }

            if (physicalLinks != null)
            {
                foreach (ConnectionsPhysicalLinkResources link in physicalLinks)
                    link?.ClampToCapacity();
            }

            if (connections != null)
            {
                foreach (ConnectionsLogicalConnectionDefinition connection in connections)
                    connection?.ClampNonnegative();
            }
        }

        private void Update() => RefreshDisplays();

        // Explicit step seam for deterministic Editor witnesses. Play mode calls it only from FixedUpdate.
        public void StepSimulation(float deltaSeconds)
        {
            if (runtimeConnections == null)
                ResetSimulation();

            foreach (ConnectionsLogicalConnectionState connection in runtimeConnections)
            {
                if (!connection.IsPending || simulationTimeSeconds < connection.Definition.StartDelaySeconds)
                    continue;
                connection.Lifecycle = ConnectionsLifecycle.Active;
                connection.EstablishedAtSeconds = simulationTimeSeconds;
            }
            ConnectionsResourceAllocator.Step(nodes, physicalLinks, runtimeConnections, deltaSeconds);
            simulationTimeSeconds += deltaSeconds;
            RefreshDisplays();
        }

        public void ResetSimulation()
        {
            ClearAllocatedUsage();
            simulationTimeSeconds = 0f;
            var states = new List<ConnectionsLogicalConnectionState>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var nodeViews = new HashSet<ConnectionsNodeView>();
            foreach (ConnectionsNodeResources node in nodes)
            {
                if (node?.View == null || !nodeViews.Add(node.View))
                    throw new InvalidOperationException("Nodes require unique, assigned views.");
            }

            if (connections != null)
            {
                foreach (ConnectionsLogicalConnectionDefinition definition in connections)
                {
                    if (definition == null || string.IsNullOrWhiteSpace(definition.ConnectionId) ||
                        !ids.Add(definition.ConnectionId))
                        throw new InvalidOperationException("Logical connections require unique, nonempty IDs.");
                    if (definition.TotalWorkMB <= 0f || definition.DesiredRateMBps <= 0f ||
                        definition.Route == null || definition.Route.Length < 2)
                        throw new InvalidOperationException($"Connection {definition.ConnectionId} needs work, rate, and a route of at least two nodes.");

                    var visited = new HashSet<ConnectionsNodeView>();
                    foreach (ConnectionsNodeRequirement requirement in definition.Route)
                    {
                        if (requirement?.Node == null || !nodeViews.Contains(requirement.Node) ||
                            !visited.Add(requirement.Node))
                            throw new InvalidOperationException($"Connection {definition.ConnectionId} has an invalid or repeated route node.");
                    }

                    var routeLinks = new ConnectionsPhysicalLinkResources[definition.Route.Length - 1];
                    for (int i = 0; i < routeLinks.Length; i++)
                    {
                        ConnectionsNodeView a = definition.Route[i].Node;
                        ConnectionsNodeView b = definition.Route[i + 1].Node;
                        foreach (ConnectionsPhysicalLinkResources link in physicalLinks)
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
                    states.Add(new ConnectionsLogicalConnectionState(definition, routeLinks));
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
                foreach (ConnectionsNodeResources node in nodes)
                    node?.ClearAllocatedUsage();
            if (physicalLinks != null)
                foreach (ConnectionsPhysicalLinkResources link in physicalLinks)
                    link?.ClearAllocatedUsage();
        }

        public void RefreshDisplays()
        {
            if (nodes != null)
            {
                foreach (ConnectionsNodeResources node in nodes)
                {
                    if (node?.View != null)
                        node.View.RenderResources(node);
                }
            }

            if (physicalLinks != null)
            {
                foreach (ConnectionsPhysicalLinkResources link in physicalLinks)
                {
                    if (link?.View != null)
                        link.View.RenderResources(link);
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
                return "CONNECTIONS  •  Play to run authored transfers";

            var output = new StringBuilder("CONNECTIONS  •  actual / desired MB/s");
            foreach (ConnectionsLogicalConnectionState connection in runtimeConnections)
            {
                output.Append('\n').Append(connection.ConnectionId).Append(' ');
                if (connection.IsComplete)
                {
                    output.Append("DONE  ").Append(connection.CompletedWorkMB.ToString("0.#"))
                        .Append('/').Append(connection.Definition.TotalWorkMB.ToString("0.#")).Append(" MB");
                    continue;
                }

                if (connection.IsPending)
                {
                    output.Append("PENDING  starts ")
                        .Append(connection.Definition.StartDelaySeconds.ToString("0.#")).Append(" s");
                    continue;
                }

                output.Append(connection.CompletedWorkMB.ToString("0.#")).Append('/')
                    .Append(connection.Definition.TotalWorkMB.ToString("0.#")).Append(" MB  ")
                    .Append(connection.ActualRateMBps.ToString("0.#")).Append('/')
                    .Append(connection.Definition.DesiredRateMBps.ToString("0.#")).Append(" MB/s  ")
                    .Append(Mathf.RoundToInt(connection.Satisfaction * 100f)).Append('%');
                if (connection.LimitKind != ConnectionsLimitKind.None)
                    output.Append("  [").Append(connection.LimitingResourceId).Append(' ')
                        .Append(LimitLabel(connection.LimitKind)).Append(']');
            }
            return output.ToString();
        }

        private static string LimitLabel(ConnectionsLimitKind kind)
        {
            switch (kind)
            {
                case ConnectionsLimitKind.PhysicalLinkBandwidth: return "BW";
                case ConnectionsLimitKind.NodeCpu: return "CPU";
                case ConnectionsLimitKind.NodeDiskIo: return "DISK";
                case ConnectionsLimitKind.NodeMemory: return "MEM";
                default: return string.Empty;
            }
        }
    }
}
