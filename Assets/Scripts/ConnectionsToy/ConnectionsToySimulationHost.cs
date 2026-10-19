using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // The only clock and owner of resource state for the connections toy.
    [ExecuteAlways]
    public sealed class ConnectionsToySimulationHost : MonoBehaviour
    {
        [SerializeField] private ConnectionsToyNodeResources[] nodes;
        [SerializeField] private ConnectionsToyPhysicalLinkResources[] physicalLinks;
        [SerializeField] private ConnectionsToyLogicalConnectionDefinition[] connections;
        [SerializeField] private TMP_Text connectionDebugReadout;

        private ConnectionsToyLogicalConnectionState[] runtimeConnections;

        public ConnectionsToyNodeResources[] Nodes => nodes;
        public ConnectionsToyPhysicalLinkResources[] PhysicalLinks => physicalLinks;
        public ConnectionsToyLogicalConnectionDefinition[] Definitions => connections;
        public IReadOnlyList<ConnectionsToyLogicalConnectionState> RuntimeConnections => runtimeConnections;

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
                foreach (ConnectionsToyNodeResources node in nodes)
                    node?.ClampToCapacity();
            }

            if (physicalLinks != null)
            {
                foreach (ConnectionsToyPhysicalLinkResources link in physicalLinks)
                    link?.ClampToCapacity();
            }

            if (connections != null)
            {
                foreach (ConnectionsToyLogicalConnectionDefinition connection in connections)
                    connection?.ClampNonnegative();
            }
        }

        private void Update() => RefreshDisplays();

        // Explicit step seam for deterministic Editor witnesses. Play mode calls it only from FixedUpdate.
        public void StepSimulation(float deltaSeconds)
        {
            if (runtimeConnections == null)
                ResetSimulation();

            ConnectionsToyResourceAllocator.Step(nodes, physicalLinks, runtimeConnections, deltaSeconds);
            RefreshDisplays();
        }

        public void ResetSimulation()
        {
            ClearAllocatedUsage();
            var states = new List<ConnectionsToyLogicalConnectionState>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var nodeViews = new HashSet<ConnectionsToyNodeView>();
            foreach (ConnectionsToyNodeResources node in nodes)
            {
                if (node?.View == null || !nodeViews.Add(node.View))
                    throw new InvalidOperationException("Toy nodes require unique, assigned views.");
            }

            if (connections != null)
            {
                foreach (ConnectionsToyLogicalConnectionDefinition definition in connections)
                {
                    if (definition == null || string.IsNullOrWhiteSpace(definition.ConnectionId) ||
                        !ids.Add(definition.ConnectionId))
                        throw new InvalidOperationException("Toy connections require unique, nonempty IDs.");
                    if (definition.TotalWorkMB <= 0f || definition.DesiredRateMBps <= 0f ||
                        definition.Route == null || definition.Route.Length < 2)
                        throw new InvalidOperationException($"Connection {definition.ConnectionId} needs work, rate, and a route of at least two nodes.");

                    var visited = new HashSet<ConnectionsToyNodeView>();
                    foreach (ConnectionsToyNodeRequirement requirement in definition.Route)
                    {
                        if (requirement?.Node == null || !nodeViews.Contains(requirement.Node) ||
                            !visited.Add(requirement.Node))
                            throw new InvalidOperationException($"Connection {definition.ConnectionId} has an invalid or repeated route node.");
                    }

                    var routeLinks = new ConnectionsToyPhysicalLinkResources[definition.Route.Length - 1];
                    for (int i = 0; i < routeLinks.Length; i++)
                    {
                        ConnectionsToyNodeView a = definition.Route[i].Node;
                        ConnectionsToyNodeView b = definition.Route[i + 1].Node;
                        foreach (ConnectionsToyPhysicalLinkResources link in physicalLinks)
                        {
                            if (link?.View == null)
                                throw new InvalidOperationException("Toy physical links require assigned views.");
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
                    states.Add(new ConnectionsToyLogicalConnectionState(definition, routeLinks));
                }
            }

            states.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            runtimeConnections = states.ToArray();
            RefreshDisplays();
        }

        private void ClearAllocatedUsage()
        {
            if (nodes != null)
                foreach (ConnectionsToyNodeResources node in nodes)
                    node?.ClearAllocatedUsage();
            if (physicalLinks != null)
                foreach (ConnectionsToyPhysicalLinkResources link in physicalLinks)
                    link?.ClearAllocatedUsage();
        }

        public void RefreshDisplays()
        {
            if (nodes != null)
            {
                foreach (ConnectionsToyNodeResources node in nodes)
                {
                    if (node?.View != null)
                        node.View.RenderResources(node);
                }
            }

            if (physicalLinks != null)
            {
                foreach (ConnectionsToyPhysicalLinkResources link in physicalLinks)
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
            foreach (ConnectionsToyLogicalConnectionState connection in runtimeConnections)
            {
                output.Append('\n').Append(connection.ConnectionId).Append(' ');
                if (connection.IsComplete)
                {
                    output.Append("DONE  ").Append(connection.CompletedWorkMB.ToString("0.#"))
                        .Append('/').Append(connection.Definition.TotalWorkMB.ToString("0.#")).Append(" MB");
                    continue;
                }

                output.Append(connection.CompletedWorkMB.ToString("0.#")).Append('/')
                    .Append(connection.Definition.TotalWorkMB.ToString("0.#")).Append(" MB  ")
                    .Append(connection.ActualRateMBps.ToString("0.#")).Append('/')
                    .Append(connection.Definition.DesiredRateMBps.ToString("0.#")).Append(" MB/s  ")
                    .Append(Mathf.RoundToInt(connection.Satisfaction * 100f)).Append('%');
                if (connection.LimitKind != ConnectionsToyLimitKind.None)
                    output.Append("  [").Append(connection.LimitingResourceId).Append(' ')
                        .Append(LimitLabel(connection.LimitKind)).Append(']');
            }
            return output.ToString();
        }

        private static string LimitLabel(ConnectionsToyLimitKind kind)
        {
            switch (kind)
            {
                case ConnectionsToyLimitKind.PhysicalLinkBandwidth: return "BW";
                case ConnectionsToyLimitKind.NodeCpu: return "CPU";
                case ConnectionsToyLimitKind.NodeDiskIo: return "DISK";
                case ConnectionsToyLimitKind.NodeMemory: return "MEM";
                default: return string.Empty;
            }
        }
    }
}
