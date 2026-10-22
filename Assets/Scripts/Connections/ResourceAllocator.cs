using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Fixed memory reservations use stable-ID admission. Admitted operations then
    // receive max-min fair satisfaction (equal fractions of desired rate).
    internal static class ResourceAllocator
    {
        private const float Epsilon = 0.0001f;

        private sealed class RateConstraint
        {
            internal LimitKind Kind;
            internal string ResourceId;
            internal NodeResources Node;
            internal PhysicalLinkResources Link;
            internal float Available;
            internal float Used;
            internal float Slope;

            internal float Cost(LogicalConnectionState connection)
            {
                switch (Kind)
                {
                    case LimitKind.PhysicalLinkBandwidth:
                        return connection.Uses(Link) ? connection.Definition.BandwidthMBPerWork : 0f;
                    case LimitKind.NodeCpu:
                        return connection.CpuCost(Node.View);
                    case LimitKind.NodeDiskIo:
                        return connection.DiskIoCost(Node.View);
                    default:
                        return 0f;
                }
            }
        }

        internal static void Step(
            NodeResources[] nodes,
            PhysicalLinkResources[] physicalLinks,
            LogicalConnectionState[] connections,
            float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            var ordered = new List<LogicalConnectionState>(connections);
            ordered.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));

            var nodeByView = new Dictionary<NodeView, NodeResources>();
            var memoryAvailable = new Dictionary<NodeResources, float>();
            foreach (NodeResources node in nodes)
            {
                nodeByView.Add(node.View, node);
                memoryAvailable.Add(node, node.MemoryCapacityGB - node.MemoryBaselineGB);
            }

            var admitted = new List<LogicalConnectionState>();
            foreach (LogicalConnectionState connection in ordered)
            {
                connection.ActualWorkRate = 0f;
                connection.MemoryAdmitted = false;
                connection.LimitKind = LimitKind.None;
                connection.LimitingResourceId = string.Empty;
                if (!connection.IsActive)
                    continue;

                NodeResources blockedNode = null;
                foreach (NodeRequirement requirement in connection.Definition.Route)
                {
                    NodeResources node = nodeByView[requirement.Node];
                    if (requirement.MemoryGB > memoryAvailable[node])
                    {
                        blockedNode = node;
                        break;
                    }
                }

                if (blockedNode != null)
                {
                    connection.LimitKind = LimitKind.NodeMemory;
                    connection.LimitingResourceId = blockedNode.View.NodeId;
                    continue;
                }

                connection.MemoryAdmitted = true;
                admitted.Add(connection);
                foreach (NodeRequirement requirement in connection.Definition.Route)
                {
                    NodeResources node = nodeByView[requirement.Node];
                    memoryAvailable[node] = Mathf.Max(0f, memoryAvailable[node] - requirement.MemoryGB);
                }
            }

            List<RateConstraint> constraints = BuildConstraints(nodes, physicalLinks);
            var growing = new List<LogicalConnectionState>(admitted);
            while (growing.Count > 0)
            {
                float increment = float.PositiveInfinity;
                foreach (LogicalConnectionState connection in growing)
                    increment = Mathf.Min(increment,
                        (connection.Definition.DesiredWorkRate - connection.ActualWorkRate) /
                        connection.Definition.DesiredWorkRate);

                foreach (RateConstraint constraint in constraints)
                {
                    constraint.Slope = 0f;
                    foreach (LogicalConnectionState connection in growing)
                        constraint.Slope += constraint.Cost(connection) * connection.Definition.DesiredWorkRate;
                    if (constraint.Slope > Epsilon)
                        increment = Mathf.Min(increment,
                            Mathf.Max(0f, constraint.Available - constraint.Used) / constraint.Slope);
                }

                if (increment > Epsilon)
                {
                    foreach (LogicalConnectionState connection in growing)
                        connection.ActualWorkRate += connection.Definition.DesiredWorkRate * increment;
                    foreach (RateConstraint constraint in constraints)
                        constraint.Used += constraint.Slope * increment;
                }

                int before = growing.Count;
                growing.RemoveAll(connection =>
                    connection.ActualWorkRate >= connection.Definition.DesiredWorkRate - Epsilon ||
                    UsesSaturatedConstraint(connection, constraints));
                if (growing.Count == before)
                    throw new InvalidOperationException("Resource allocation did not converge.");
            }

            foreach (LogicalConnectionState connection in admitted)
            {
                if (connection.ActualWorkRate >= connection.Definition.DesiredWorkRate - Epsilon)
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

                if (connection.LimitKind == LimitKind.None)
                    throw new InvalidOperationException($"No limiter found for {connection.ConnectionId}.");
            }

            foreach (LogicalConnectionState connection in admitted)
            {
                connection.CompletedWork = Mathf.Min(connection.Definition.TotalWork,
                    connection.CompletedWork + connection.ActualWorkRate * deltaSeconds);
                if (connection.RemainingWork > Epsilon)
                    continue;

                connection.CompletedWork = connection.Definition.TotalWork;
                connection.Lifecycle = Lifecycle.Complete;
                connection.ActualWorkRate = 0f;
                connection.MemoryAdmitted = false;
                connection.LimitKind = LimitKind.None;
                connection.LimitingResourceId = string.Empty;
            }

            PublishUsage(nodes, physicalLinks, ordered, nodeByView);
        }

        private static List<RateConstraint> BuildConstraints(
            NodeResources[] nodes,
            PhysicalLinkResources[] physicalLinks)
        {
            var constraints = new List<RateConstraint>();
            foreach (PhysicalLinkResources link in physicalLinks)
                constraints.Add(new RateConstraint {
                    Kind = LimitKind.PhysicalLinkBandwidth,
                    ResourceId = link.View.LinkId,
                    Link = link,
                    Available = link.BandwidthCapacityMBps
                });
            foreach (NodeResources node in nodes)
            {
                constraints.Add(new RateConstraint {
                    Kind = LimitKind.NodeCpu,
                    ResourceId = node.View.NodeId,
                    Node = node,
                    Available = node.CpuCapacityUnits - node.CpuBaselineUnits
                });
                constraints.Add(new RateConstraint {
                    Kind = LimitKind.NodeDiskIo,
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
            LogicalConnectionState connection,
            List<RateConstraint> constraints)
        {
            foreach (RateConstraint constraint in constraints)
                if (constraint.Cost(connection) > Epsilon &&
                    constraint.Available - constraint.Used <= Epsilon)
                    return true;
            return false;
        }

        private static void PublishUsage(
            NodeResources[] nodes,
            PhysicalLinkResources[] physicalLinks,
            List<LogicalConnectionState> connections,
            Dictionary<NodeView, NodeResources> nodeByView)
        {
            foreach (NodeResources node in nodes)
                node.ClearAllocatedUsage();
            foreach (PhysicalLinkResources link in physicalLinks)
                link.ClearAllocatedUsage();

            foreach (LogicalConnectionState connection in connections)
            {
                if (!connection.IsActive || !connection.MemoryAdmitted)
                    continue;

                float rate = connection.ActualWorkRate;
                foreach (NodeRequirement requirement in connection.Definition.Route)
                    nodeByView[requirement.Node].AddAllocatedUsage(
                        rate * requirement.CpuUnitsPerWork,
                        requirement.MemoryGB,
                        rate * requirement.DiskIoMBPerWork);
                foreach (PhysicalLinkResources link in connection.RouteLinks)
                    link.AddAllocatedUsage(connection.ActualBandwidthMBps);
            }

            foreach (NodeResources node in nodes)
            {
                if (node.CpuUsageUnits > node.CpuCapacityUnits + Epsilon ||
                    node.MemoryUsageGB > node.MemoryCapacityGB + Epsilon ||
                    node.DiskIoUsageMBps > node.DiskIoCapacityMBps + Epsilon)
                    throw new InvalidOperationException($"Node {node.View.NodeId} exceeded capacity.");
            }
            foreach (PhysicalLinkResources link in physicalLinks)
                if (link.BandwidthUsageMBps > link.BandwidthCapacityMBps + Epsilon)
                    throw new InvalidOperationException($"Link {link.View.LinkId} exceeded capacity.");
        }
    }
}
