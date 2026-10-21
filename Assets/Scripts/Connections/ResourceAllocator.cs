using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Each target grants the same fraction of every active request when overloaded.
    internal static class ResourceAllocator
    {
        private const float Epsilon = 0.0001f;

        internal static void Step(NodeResources[] nodes, LogicalConnectionState[] connections, float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            var ordered = new List<LogicalConnectionState>(connections);
            ordered.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            var nodeByView = new Dictionary<NodeView, NodeResources>();
            var demandByNode = new Dictionary<NodeResources, float>();
            foreach (NodeResources node in nodes)
            {
                nodeByView.Add(node.View, node);
                demandByNode.Add(node, 0f);
            }

            foreach (LogicalConnectionState connection in ordered)
            {
                connection.AllocatedLoad = 0f;
                // A stage completed on the previous step begins requesting now.
                connection.IsTransitioning = false;
                if (connection.IsDemanding)
                    demandByNode[nodeByView[connection.TargetNode]] += connection.DesiredLoad;
            }

            foreach (LogicalConnectionState connection in ordered)
            {
                if (!connection.IsDemanding)
                    continue;
                NodeResources target = nodeByView[connection.TargetNode];
                float demand = demandByNode[target];
                float available = Mathf.Max(0f, target.LoadCapacity - target.BaselineLoad);
                float fraction = demand > 0f ? Mathf.Min(1f, available / demand) : 0f;
                connection.AllocatedLoad = connection.DesiredLoad * fraction;
                connection.CompletedWork = Mathf.Min(connection.CurrentTotalWork,
                    connection.CompletedWork + connection.ActualWorkRate * deltaSeconds);
                if (connection.RemainingWork > Epsilon)
                    continue;

                connection.FinishOperation();
            }

            foreach (NodeResources node in nodes)
                node.ClearUsage();
            foreach (LogicalConnectionState connection in ordered)
            {
                if (!connection.IsDemanding)
                    continue;
                nodeByView[connection.TargetNode].AddUsage(connection.DesiredLoad,
                    connection.AllocatedLoad);
            }
            foreach (NodeResources node in nodes)
                if (node.AllocatedLoad > node.LoadCapacity + Epsilon)
                    throw new InvalidOperationException($"Node {node.View.NodeId} exceeded LOAD capacity.");
        }
    }
}
