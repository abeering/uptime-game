using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // Fixed memory reservations use stable-ID admission. Admitted transfers then
    // receive max-min fair satisfaction (equal fractions of desired rate).
    internal static class ConnectionsToyResourceAllocator
    {
        private const float Epsilon = 0.0001f;

        private sealed class RateConstraint
        {
            internal ConnectionsToyLimitKind Kind;
            internal string ResourceId;
            internal ConnectionsToyNodeResources Node;
            internal ConnectionsToyPhysicalLinkResources Link;
            internal float Available;
            internal float Used;
            internal float Slope;

            internal float Cost(ConnectionsToyLogicalConnectionState connection)
            {
                switch (Kind)
                {
                    case ConnectionsToyLimitKind.PhysicalLinkBandwidth:
                        return connection.Uses(Link) ? 1f : 0f;
                    case ConnectionsToyLimitKind.NodeCpu:
                        return connection.CpuCost(Node.View);
                    case ConnectionsToyLimitKind.NodeDiskIo:
                        return connection.DiskIoCost(Node.View);
                    default:
                        return 0f;
                }
            }
        }

        internal static void Step(
            ConnectionsToyNodeResources[] nodes,
            ConnectionsToyPhysicalLinkResources[] physicalLinks,
            ConnectionsToyLogicalConnectionState[] connections,
            float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            var ordered = new List<ConnectionsToyLogicalConnectionState>(connections);
            ordered.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));

            var nodeByView = new Dictionary<ConnectionsToyNodeView, ConnectionsToyNodeResources>();
            var memoryAvailable = new Dictionary<ConnectionsToyNodeResources, float>();
            foreach (ConnectionsToyNodeResources node in nodes)
            {
                nodeByView.Add(node.View, node);
                memoryAvailable.Add(node, node.MemoryCapacityGB - node.MemoryBaselineGB);
            }

            var admitted = new List<ConnectionsToyLogicalConnectionState>();
            foreach (ConnectionsToyLogicalConnectionState connection in ordered)
            {
                connection.ActualRateMBps = 0f;
                connection.MemoryAdmitted = false;
                connection.LimitKind = ConnectionsToyLimitKind.None;
                connection.LimitingResourceId = string.Empty;
                if (connection.IsComplete)
                    continue;

                ConnectionsToyNodeResources blockedNode = null;
                foreach (ConnectionsToyNodeRequirement requirement in connection.Definition.Route)
                {
                    ConnectionsToyNodeResources node = nodeByView[requirement.Node];
                    if (requirement.MemoryGB > memoryAvailable[node])
                    {
                        blockedNode = node;
                        break;
                    }
                }

                if (blockedNode != null)
                {
                    connection.LimitKind = ConnectionsToyLimitKind.NodeMemory;
                    connection.LimitingResourceId = blockedNode.View.NodeId;
                    continue;
                }

                connection.MemoryAdmitted = true;
                admitted.Add(connection);
                foreach (ConnectionsToyNodeRequirement requirement in connection.Definition.Route)
                {
                    ConnectionsToyNodeResources node = nodeByView[requirement.Node];
                    memoryAvailable[node] = Mathf.Max(0f, memoryAvailable[node] - requirement.MemoryGB);
                }
            }

            List<RateConstraint> constraints = BuildConstraints(nodes, physicalLinks);
            var growing = new List<ConnectionsToyLogicalConnectionState>(admitted);
            while (growing.Count > 0)
            {
                float increment = float.PositiveInfinity;
                foreach (ConnectionsToyLogicalConnectionState connection in growing)
                    increment = Mathf.Min(increment,
                        (connection.Definition.DesiredRateMBps - connection.ActualRateMBps) /
                        connection.Definition.DesiredRateMBps);

                foreach (RateConstraint constraint in constraints)
                {
                    constraint.Slope = 0f;
                    foreach (ConnectionsToyLogicalConnectionState connection in growing)
                        constraint.Slope += constraint.Cost(connection) * connection.Definition.DesiredRateMBps;
                    if (constraint.Slope > Epsilon)
                        increment = Mathf.Min(increment,
                            Mathf.Max(0f, constraint.Available - constraint.Used) / constraint.Slope);
                }

                if (increment > Epsilon)
                {
                    foreach (ConnectionsToyLogicalConnectionState connection in growing)
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

            foreach (ConnectionsToyLogicalConnectionState connection in admitted)
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

                if (connection.LimitKind == ConnectionsToyLimitKind.None)
                    throw new InvalidOperationException($"No limiter found for {connection.ConnectionId}.");
            }

            foreach (ConnectionsToyLogicalConnectionState connection in admitted)
            {
                connection.CompletedWorkMB = Mathf.Min(connection.Definition.TotalWorkMB,
                    connection.CompletedWorkMB + connection.ActualRateMBps * deltaSeconds);
                if (connection.RemainingWorkMB > Epsilon)
                    continue;

                connection.CompletedWorkMB = connection.Definition.TotalWorkMB;
                connection.IsComplete = true;
                connection.ActualRateMBps = 0f;
                connection.MemoryAdmitted = false;
                connection.LimitKind = ConnectionsToyLimitKind.None;
                connection.LimitingResourceId = string.Empty;
            }

            PublishUsage(nodes, physicalLinks, ordered, nodeByView);
        }

        private static List<RateConstraint> BuildConstraints(
            ConnectionsToyNodeResources[] nodes,
            ConnectionsToyPhysicalLinkResources[] physicalLinks)
        {
            var constraints = new List<RateConstraint>();
            foreach (ConnectionsToyPhysicalLinkResources link in physicalLinks)
                constraints.Add(new RateConstraint {
                    Kind = ConnectionsToyLimitKind.PhysicalLinkBandwidth,
                    ResourceId = link.View.LinkId,
                    Link = link,
                    Available = link.BandwidthCapacityMBps
                });
            foreach (ConnectionsToyNodeResources node in nodes)
            {
                constraints.Add(new RateConstraint {
                    Kind = ConnectionsToyLimitKind.NodeCpu,
                    ResourceId = node.View.NodeId,
                    Node = node,
                    Available = node.CpuCapacityUnits - node.CpuBaselineUnits
                });
                constraints.Add(new RateConstraint {
                    Kind = ConnectionsToyLimitKind.NodeDiskIo,
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
            ConnectionsToyLogicalConnectionState connection,
            List<RateConstraint> constraints)
        {
            foreach (RateConstraint constraint in constraints)
                if (constraint.Cost(connection) > Epsilon &&
                    constraint.Available - constraint.Used <= Epsilon)
                    return true;
            return false;
        }

        private static void PublishUsage(
            ConnectionsToyNodeResources[] nodes,
            ConnectionsToyPhysicalLinkResources[] physicalLinks,
            List<ConnectionsToyLogicalConnectionState> connections,
            Dictionary<ConnectionsToyNodeView, ConnectionsToyNodeResources> nodeByView)
        {
            foreach (ConnectionsToyNodeResources node in nodes)
                node.ClearAllocatedUsage();
            foreach (ConnectionsToyPhysicalLinkResources link in physicalLinks)
                link.ClearAllocatedUsage();

            foreach (ConnectionsToyLogicalConnectionState connection in connections)
            {
                if (!connection.IsActive || !connection.MemoryAdmitted)
                    continue;

                float rate = connection.ActualRateMBps;
                foreach (ConnectionsToyNodeRequirement requirement in connection.Definition.Route)
                    nodeByView[requirement.Node].AddAllocatedUsage(
                        rate * requirement.CpuUnitsPerMB,
                        requirement.MemoryGB,
                        rate * requirement.DiskIoMBPerMB);
                foreach (ConnectionsToyPhysicalLinkResources link in connection.RouteLinks)
                    link.AddAllocatedUsage(rate);
            }

            foreach (ConnectionsToyNodeResources node in nodes)
            {
                if (node.CpuUsageUnits > node.CpuCapacityUnits + Epsilon ||
                    node.MemoryUsageGB > node.MemoryCapacityGB + Epsilon ||
                    node.DiskIoUsageMBps > node.DiskIoCapacityMBps + Epsilon)
                    throw new InvalidOperationException($"Node {node.View.NodeId} exceeded capacity.");
            }
            foreach (ConnectionsToyPhysicalLinkResources link in physicalLinks)
                if (link.BandwidthUsageMBps > link.BandwidthCapacityMBps + Epsilon)
                    throw new InvalidOperationException($"Link {link.View.LinkId} exceeded capacity.");
        }
    }
}
