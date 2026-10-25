using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Each target grants the same fraction of every active request when overloaded.
    internal static class ResourceAllocator
    {
        private const float Epsilon = 0.0001f;

        internal readonly struct OperationCompletion
        {
            public OperationCompletion(LogicalConnectionState state, int index,
                float completedWork, float requiredWork, float desiredLoad)
            { State = state; Index = index; CompletedWork = completedWork;
                RequiredWork = requiredWork; DesiredLoad = desiredLoad; }
            public LogicalConnectionState State { get; }
            public int Index { get; }
            public float CompletedWork { get; }
            public float RequiredWork { get; }
            public float DesiredLoad { get; }
        }

        internal static List<OperationCompletion> Step(NodeResources[] nodes,
            LogicalConnectionState[] connections, IReadOnlyList<NodeInfection> infections,
            float deltaSeconds,
            Action<IReadOnlyList<LogicalConnectionState>> afterAllocation = null)
        {
            if (deltaSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            var ordered = new List<LogicalConnectionState>(connections);
            var completed = new List<OperationCompletion>();
            ordered.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            foreach (LogicalConnectionState connection in ordered)
            {
                // A stage completed on the previous step begins requesting now.
                connection.IsTransitioning = false;
            }
            Allocate(nodes, ordered, infections);
            afterAllocation?.Invoke(ordered);
            foreach (LogicalConnectionState connection in ordered)
            {
                if (!connection.IsDemanding) continue;
                // A reduced requirement cannot erase WORK already accomplished.
                connection.CompletedWork = Mathf.Max(connection.CompletedWork,
                    Mathf.Min(connection.CurrentTotalWork,
                        connection.CompletedWork + connection.ActualWorkRate * deltaSeconds));
                if (connection.RemainingWork > Epsilon)
                    continue;

                completed.Add(new OperationCompletion(connection, connection.CurrentOperationIndex,
                    connection.CompletedWork, connection.CurrentTotalWork, connection.DesiredLoad));
                connection.FinishOperation();
            }

            Publish(nodes, ordered);
            return completed;
        }

        // Recompute authoritative allocation after a delivery or developer intervention,
        // without advancing WORK or Health until the next fixed step.
        internal static void RefreshAllocations(NodeResources[] nodes,
            LogicalConnectionState[] connections, IReadOnlyList<NodeInfection> infections)
        {
            var ordered = new List<LogicalConnectionState>(connections);
            ordered.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            Allocate(nodes, ordered, infections);
            Publish(nodes, ordered);
        }

        private static void Allocate(NodeResources[] nodes, List<LogicalConnectionState> ordered,
            IReadOnlyList<NodeInfection> infections)
        {
            var nodeByView = new Dictionary<NodeView, NodeResources>();
            var connectionDemand = new Dictionary<NodeResources, float>();
            var infectionDemand = new Dictionary<NodeResources, float>();
            foreach (NodeResources node in nodes)
            {
                nodeByView.Add(node.View, node);
                connectionDemand.Add(node, 0f);
                infectionDemand.Add(node, 0f);
            }
            if (infections != null)
                foreach (NodeInfection infection in infections)
                    infectionDemand[infection.Target] += infection.Payload.RequestedLoad;
            foreach (LogicalConnectionState connection in ordered)
            {
                connection.AllocatedLoad = 0f;
                if (connection.IsDemanding)
                    connectionDemand[nodeByView[connection.TargetNode]] += connection.DesiredLoad;
            }
            foreach (NodeResources node in nodes)
            {
                float available = Mathf.Max(0f, node.LoadCapacity - node.BaselineLoad);
                float infectionAllocated = Mathf.Min(available, infectionDemand[node]);
                node.SetInfectionUsage(infectionDemand[node], infectionAllocated);
            }
            foreach (LogicalConnectionState connection in ordered)
            {
                if (!connection.IsDemanding) continue;
                NodeResources target = nodeByView[connection.TargetNode];
                float demand = connectionDemand[target];
                float available = Mathf.Max(0f, target.LoadCapacity - target.BaselineLoad -
                    target.InfectionAllocatedLoad);
                float fraction = demand > 0f ? Mathf.Min(1f, available / demand) : 0f;
                connection.AllocatedLoad = connection.DesiredLoad * fraction;
            }
        }

        private static void Publish(NodeResources[] nodes, List<LogicalConnectionState> ordered)
        {
            var nodeByView = new Dictionary<NodeView, NodeResources>();
            foreach (NodeResources node in nodes)
            {
                node.ClearConnectionUsage();
                nodeByView.Add(node.View, node);
            }
            foreach (LogicalConnectionState connection in ordered)
                if (connection.IsDemanding)
                    nodeByView[connection.TargetNode].AddUsage(connection.DesiredLoad,
                        connection.AllocatedLoad);
            foreach (NodeResources node in nodes)
                if (node.AllocatedLoad > node.LoadCapacity + Epsilon)
                    throw new InvalidOperationException($"Node {node.View.NodeId} exceeded LOAD capacity.");
        }
    }
}
