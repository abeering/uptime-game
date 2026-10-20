using System;
using UnityEngine;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class NodeRequirement
    {
        [SerializeField] private NodeView node;
        [SerializeField, Min(0f), Tooltip("CPU units used per MB transferred at this node.")]
        private float cpuUnitsPerMB;
        [SerializeField, Min(0f), Tooltip("Disk I/O MB used per MB transferred at this node.")]
        private float diskIoMBPerMB;
        [SerializeField, Min(0f), Tooltip("GB reserved at this node while the connection is active.")]
        private float memoryGB;

        public NodeView Node => node;
        public float CpuUnitsPerMB => Mathf.Max(0f, cpuUnitsPerMB);
        public float DiskIoMBPerMB => Mathf.Max(0f, diskIoMBPerMB);
        public float MemoryGB => Mathf.Max(0f, memoryGB);

        internal void ClampNonnegative()
        {
            cpuUnitsPerMB = CpuUnitsPerMB;
            diskIoMBPerMB = DiskIoMBPerMB;
            memoryGB = MemoryGB;
        }
    }

    [Serializable]
    public sealed class LogicalConnectionDefinition
    {
        [SerializeField] private string connectionId;
        [SerializeField] private NodeRequirement[] route;
        [SerializeField, Min(0f)] private float totalWorkMB;
        [SerializeField, Min(0f)] private float desiredRateMBps;
        [SerializeField, Min(0f), Tooltip("Seconds after simulation start before this connection is established.")]
        private float startDelaySeconds;

        public string ConnectionId => connectionId;
        public NodeRequirement[] Route => route;
        public float TotalWorkMB => Mathf.Max(0f, totalWorkMB);
        public float DesiredRateMBps => Mathf.Max(0f, desiredRateMBps);
        public float StartDelaySeconds => Mathf.Max(0f, startDelaySeconds);

        internal void ClampNonnegative()
        {
            totalWorkMB = TotalWorkMB;
            desiredRateMBps = DesiredRateMBps;
            startDelaySeconds = StartDelaySeconds;
            if (route == null)
                return;

            foreach (NodeRequirement requirement in route)
                requirement?.ClampNonnegative();
        }
    }

    public enum LimitKind
    {
        None,
        PhysicalLinkBandwidth,
        NodeCpu,
        NodeDiskIo,
        NodeMemory
    }

    public enum Lifecycle
    {
        Pending,
        Active,
        Complete
    }

    // Runtime state belongs to the simulation, never to a UI component.
    public sealed class LogicalConnectionState
    {
        internal LogicalConnectionState(
            LogicalConnectionDefinition definition,
            PhysicalLinkResources[] routeLinks)
        {
            Definition = definition;
            RouteLinks = routeLinks;
            Lifecycle = definition.StartDelaySeconds > 0f ? Lifecycle.Pending : Lifecycle.Active;
        }

        public LogicalConnectionDefinition Definition { get; }
        internal PhysicalLinkResources[] RouteLinks { get; }
        internal bool MemoryAdmitted { get; set; }

        public string ConnectionId => Definition.ConnectionId;
        public float CompletedWorkMB { get; internal set; }
        public float RemainingWorkMB => Mathf.Max(0f, Definition.TotalWorkMB - CompletedWorkMB);
        public float ActualRateMBps { get; internal set; }
        public float Satisfaction => IsComplete ? 1f : IsPending ? 0f : Definition.DesiredRateMBps > 0f ?
            ActualRateMBps / Definition.DesiredRateMBps : 0f;
        public Lifecycle Lifecycle { get; internal set; }
        public bool IsPending => Lifecycle == Lifecycle.Pending;
        public bool IsComplete => Lifecycle == Lifecycle.Complete;
        public bool IsActive => Lifecycle == Lifecycle.Active;
        public float EstablishedAtSeconds { get; internal set; }
        public LimitKind LimitKind { get; internal set; }
        public string LimitingResourceId { get; internal set; }

        internal float CpuCost(NodeView node)
        {
            foreach (NodeRequirement requirement in Definition.Route)
                if (requirement.Node == node)
                    return requirement.CpuUnitsPerMB;
            return 0f;
        }

        internal float DiskIoCost(NodeView node)
        {
            foreach (NodeRequirement requirement in Definition.Route)
                if (requirement.Node == node)
                    return requirement.DiskIoMBPerMB;
            return 0f;
        }

        internal bool Uses(PhysicalLinkResources link)
        {
            foreach (PhysicalLinkResources routeLink in RouteLinks)
                if (routeLink == link)
                    return true;
            return false;
        }
    }
}
