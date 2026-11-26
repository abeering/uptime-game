using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Each target shares available LOAD by requested LOAD × policy weight, capped at the request.
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
            Action<IReadOnlyList<LogicalConnectionState>> afterAllocation = null,
            Func<LogicalConnectionState, bool> mayStartTransition = null,
            Func<NodeView, float> defensiveDemand = null,
            float stepEndTimeSeconds = 0f,
            Action<EmittedConnectionLog> onLogEmitted = null, Action afterDemandPublished = null)
        {
            if (deltaSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            var ordered = new List<LogicalConnectionState>(connections);
            var completed = new List<OperationCompletion>();
            ordered.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            foreach (LogicalConnectionState connection in ordered)
            {
                // The host holds transitions completed in this outer fixed step,
                // even when route arrivals split it into smaller allocation segments.
                if (mayStartTransition == null || mayStartTransition(connection))
                    connection.IsTransitioning = false;
            }
            Allocate(nodes, ordered, infections, defensiveDemand);
            afterAllocation?.Invoke(ordered);
            if (afterDemandPublished != null)
            {
                Publish(nodes, ordered);
                afterDemandPublished();
            }
            foreach (LogicalConnectionState connection in ordered)
            {
                if (!connection.IsDemanding) continue;
                float previousWork = connection.CompletedWork;
                // A reduced requirement cannot erase WORK already accomplished.
                connection.CompletedWork = Mathf.Max(connection.CompletedWork,
                    Mathf.Min(connection.CurrentTotalWork,
                        connection.CompletedWork + connection.ActualWorkRate * deltaSeconds));
                // Match the existing completion tolerance, but never pass the effective
                // requirement when it is lower than an authored base-WORK milestone.
                float logWork = connection.RemainingWork <= Epsilon ?
                    Mathf.Min(connection.CurrentTotalWork, connection.CompletedWork + Epsilon) :
                    connection.CompletedWork;
                connection.EmitReachedLogs(previousWork, logWork, stepEndTimeSeconds,
                    onLogEmitted);
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
            LogicalConnectionState[] connections, IReadOnlyList<NodeInfection> infections,
            Func<NodeView, float> defensiveDemand = null)
        {
            var ordered = new List<LogicalConnectionState>(connections);
            ordered.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            Allocate(nodes, ordered, infections, defensiveDemand);
            Publish(nodes, ordered);
        }

        private static void Allocate(NodeResources[] nodes, List<LogicalConnectionState> ordered,
            IReadOnlyList<NodeInfection> infections, Func<NodeView, float> defensiveDemand)
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
                // Baseline and infection retain their previous priority. Defense takes
                // the next share, before proportional allocation to productive WORK.
                float requestedDefense = Mathf.Max(0f, defensiveDemand?.Invoke(node.View) ?? 0f);
                node.SetDefensiveUsage(requestedDefense,
                    Mathf.Min(requestedDefense, available - infectionAllocated));
            }
            foreach (NodeResources target in nodes)
            {
                float available = Mathf.Max(0f, target.LoadCapacity - target.BaselineLoad -
                    target.InfectionAllocatedLoad - target.DefensiveAllocatedLoad);
                var pending = new List<LogicalConnectionState>(); bool weighted = false;
                foreach (var c in ordered)
                    if (c.IsDemanding && c.TargetNode == target.View)
                    { pending.Add(c); weighted |= c.AllocationWeight != 1f; }
                if (!weighted)
                {
                    // Preserve the original arithmetic exactly for an empty policy.
                    float demand = connectionDemand[target];
                    float fraction = demand > 0f ? Mathf.Min(1f, available / demand) : 0f;
                    foreach (var c in pending) c.AllocatedLoad = c.DesiredLoad * fraction;
                    continue;
                }
                double remaining = available;
                while (pending.Count > 0 && remaining > 0)
                {
                    double total = 0;
                    foreach (var c in pending) total += (double)c.DesiredLoad * c.AllocationWeight;
                    if (total <= 0) break;
                    double budget = remaining; bool capped = false;
                    for (int i = pending.Count - 1; i >= 0; i--)
                    {
                        var c = pending[i];
                        if (budget * c.DesiredLoad * c.AllocationWeight / total < c.DesiredLoad) continue;
                        c.AllocatedLoad = c.DesiredLoad; remaining -= c.DesiredLoad;
                        pending.RemoveAt(i); capped = true;
                    }
                    if (capped) continue; // Redistribute unused weighted shares among uncapped requests.
                    foreach (var c in pending)
                        c.AllocatedLoad = (float)Math.Min(c.DesiredLoad, remaining * c.DesiredLoad * c.AllocationWeight / total);
                    break;
                }
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
