using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Fixed memory reservations use stable-ID admission. Admitted transfers then
    // receive max-min fair satisfaction (equal fractions of desired rate).
    internal static class ConnectionsResourceAllocator
    {
        private const float Epsilon = 0.0001f;

        private sealed class RateConstraint
        {
            internal ConnectionsLimitKind Kind;
            internal string ResourceId;
            internal ConnectionsNodeResources Node;
            internal ConnectionsPhysicalLinkResources Link;
            internal float Available;
            internal float Used;
            internal float Slope;

            internal float Cost(ConnectionsLogicalConnectionState connection)
            {
                switch (Kind)
                {
                    case ConnectionsLimitKind.PhysicalLinkBandwidth:
                        return connection.Uses(Link) ? 1f : 0f;
                    case ConnectionsLimitKind.NodeCpu:
                        return connection.CpuCost(Node.View);
                    case ConnectionsLimitKind.NodeDiskIo:
                        return connection.DiskIoCost(Node.View);
                    default:
                        return 0f;
                }
            }
        }

        internal static void Step(
            ConnectionsNodeResources[] nodes,
            ConnectionsPhysicalLinkResources[] physicalLinks,
            ConnectionsLogicalConnectionState[] connections,
            float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            var ordered = new List<ConnectionsLogicalConnectionState>(connections);
            ordered.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));

            var nodeByView = new Dictionary<ConnectionsNodeView, ConnectionsNodeResources>();
            var memoryAvailable = new Dictionary<ConnectionsNodeResources, float>();
            foreach (ConnectionsNodeResources node in nodes)
            {
                nodeByView.Add(node.View, node);
                memoryAvailable.Add(node, node.MemoryCapacityGB - node.MemoryBaselineGB);
            }

            var admitted = new List<ConnectionsLogicalConnectionState>();
            foreach (ConnectionsLogicalConnectionState connection in ordered)
            {
                connection.ActualRateMBps = 0f;
                connection.MemoryAdmitted = false;
                connection.LimitKind = ConnectionsLimitKind.None;
                connection.LimitingResourceId = string.Empty;
                if (!connection.IsActive)
                    continue;

                ConnectionsNodeResources blockedNode = null;
                foreach (ConnectionsNodeRequirement requirement in connection.Definition.Route)
                {
                    ConnectionsNodeResources node = nodeByView[requirement.Node];
                    if (requirement.MemoryGB > memoryAvailable[node])
                    {
                        blockedNode = node;
                        break;
                    }
                }

                if (blockedNode != null)
                {
                    connection.LimitKind = ConnectionsLimitKind.NodeMemory;
                    connection.LimitingResourceId = blockedNode.View.NodeId;
                    continue;
                }

                connection.MemoryAdmitted = true;
                admitted.Add(connection);
                foreach (ConnectionsNodeRequirement requirement in connection.Definition.Route)
                {
                    ConnectionsNodeResources node = nodeByView[requirement.Node];
                    memoryAvailable[node] = Mathf.Max(0f, memoryAvailable[node] - requirement.MemoryGB);
                }
            }

            List<RateConstraint> constraints = BuildConstraints(nodes, physicalLinks);
            var growing = new List<ConnectionsLogicalConnectionState>(admitted);
            while (growing.Count > 0)
            {
                float increment = float.PositiveInfinity;
                foreach (ConnectionsLogicalConnectionState connection in growing)
                    increment = Mathf.Min(increment,
                        (connection.Definition.DesiredRateMBps - connection.ActualRateMBps) /
                        connection.Definition.DesiredRateMBps);

                foreach (RateConstraint constraint in constraints)
                {
                    constraint.Slope = 0f;
                    foreach (ConnectionsLogicalConnectionState connection in growing)
                        constraint.Slope += constraint.Cost(connection) * connection.Definition.DesiredRateMBps;
                    if (constraint.Slope > Epsilon)
                        increment = Mathf.Min(increment,
                            Mathf.Max(0f, constraint.Available - constraint.Used) / constraint.Slope);
                }

                if (increment > Epsilon)
                {
                    foreach (ConnectionsLogicalConnectionState connection in growing)
                        connection.ActualRateMBps += connection.Definition.DesiredRateMBps * increment;
                    foreach (RateConstraint constraint in constraints)
                        constraint.Used += constraint.Slope * increment;
                }

                int before = growing.Count;
                growing.RemoveAll(connection =>
                    connection.ActualRateMBps >= connection.Definition.DesiredRateMBps - Epsilon ||
                    UsesSaturatedConstraint(connection, constraints));
                if (growing.Count == before)
                    throw new InvalidOperationException("Resource allocation did not converge.");
            }

            foreach (ConnectionsLogicalConnectionState connection in admitted)
            {
                if (connection.ActualRateMBps >= connection.Definition.DesiredRateMBps - Epsilon)
                    continue;

                foreach (RateConstraint constraint in constraints)
                {
                    if (constraint.Cost(connection) <= Epsilon ||
                        constraint.Available - constraint.Used > Epsilon)
                        continue;

                    connection.LimitKind = constraint.Kind;
                    connection.LimitingResourceId = constraint.ResourceId;
                    break;
                }

                if (connection.LimitKind == ConnectionsLimitKind.None)
                    throw new InvalidOperationException($"No limiter found for {connection.ConnectionId}.");
            }

            foreach (ConnectionsLogicalConnectionState connection in admitted)
            {
                connection.CompletedWorkMB = Mathf.Min(connection.Definition.TotalWorkMB,
                    connection.CompletedWorkMB + connection.ActualRateMBps * deltaSeconds);
                if (connection.RemainingWorkMB > Epsilon)
                    continue;

                connection.CompletedWorkMB = connection.Definition.TotalWorkMB;
                connection.Lifecycle = ConnectionsLifecycle.Complete;
                connection.ActualRateMBps = 0f;
                connection.MemoryAdmitted = false;
                connection.LimitKind = ConnectionsLimitKind.None;
                connection.LimitingResourceId = string.Empty;
            }

            PublishUsage(nodes, physicalLinks, ordered, nodeByView);
        }

        private static List<RateConstraint> BuildConstraints(
            ConnectionsNodeResources[] nodes,
            ConnectionsPhysicalLinkResources[] physicalLinks)
        {
            var constraints = new List<RateConstraint>();
            foreach (ConnectionsPhysicalLinkResources link in physicalLinks)
                constraints.Add(new RateConstraint {
                    Kind = ConnectionsLimitKind.PhysicalLinkBandwidth,
                    ResourceId = link.View.LinkId,
                    Link = link,
                    Available = link.BandwidthCapacityMBps
                });
            foreach (ConnectionsNodeResources node in nodes)
            {
                constraints.Add(new RateConstraint {
                    Kind = ConnectionsLimitKind.NodeCpu,
                    ResourceId = node.View.NodeId,
                    Node = node,
                    Available = node.CpuCapacityUnits - node.CpuBaselineUnits
                });
                constraints.Add(new RateConstraint {
                    Kind = ConnectionsLimitKind.NodeDiskIo,
                    ResourceId = node.View.NodeId,
                    Node = node,
                    Available = node.DiskIoCapacityMBps - node.DiskIoBaselineMBps
                });
            }
            constraints.Sort((a, b) => {
                int kind = a.Kind.CompareTo(b.Kind);
                return kind != 0 ? kind : string.CompareOrdinal(a.ResourceId, b.ResourceId);
            });
            return constraints;
        }

        private static bool UsesSaturatedConstraint(
            ConnectionsLogicalConnectionState connection,
            List<RateConstraint> constraints)
        {
            foreach (RateConstraint constraint in constraints)
                if (constraint.Cost(connection) > Epsilon &&
                    constraint.Available - constraint.Used <= Epsilon)
                    return true;
            return false;
        }

        private static void PublishUsage(
            ConnectionsNodeResources[] nodes,
            ConnectionsPhysicalLinkResources[] physicalLinks,
            List<ConnectionsLogicalConnectionState> connections,
            Dictionary<ConnectionsNodeView, ConnectionsNodeResources> nodeByView)
        {
            foreach (ConnectionsNodeResources node in nodes)
                node.ClearAllocatedUsage();
            foreach (ConnectionsPhysicalLinkResources link in physicalLinks)
                link.ClearAllocatedUsage();

            foreach (ConnectionsLogicalConnectionState connection in connections)
            {
                if (!connection.IsActive || !connection.MemoryAdmitted)
                    continue;

                float rate = connection.ActualRateMBps;
                foreach (ConnectionsNodeRequirement requirement in connection.Definition.Route)
                    nodeByView[requirement.Node].AddAllocatedUsage(
                        rate * requirement.CpuUnitsPerMB,
                        requirement.MemoryGB,
                        rate * requirement.DiskIoMBPerMB);
                foreach (ConnectionsPhysicalLinkResources link in connection.RouteLinks)
                    link.AddAllocatedUsage(rate);
            }

            foreach (ConnectionsNodeResources node in nodes)
            {
                if (node.CpuUsageUnits > node.CpuCapacityUnits + Epsilon ||
                    node.MemoryUsageGB > node.MemoryCapacityGB + Epsilon ||
                    node.DiskIoUsageMBps > node.DiskIoCapacityMBps + Epsilon)
                    throw new InvalidOperationException($"Node {node.View.NodeId} exceeded capacity.");
            }
            foreach (ConnectionsPhysicalLinkResources link in physicalLinks)
                if (link.BandwidthUsageMBps > link.BandwidthCapacityMBps + Epsilon)
                    throw new InvalidOperationException($"Link {link.View.LinkId} exceeded capacity.");
        }
    }
}
