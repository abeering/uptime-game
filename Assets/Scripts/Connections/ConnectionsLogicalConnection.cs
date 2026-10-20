using System;
using UnityEngine;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class ConnectionsNodeRequirement
    {
        [SerializeField] private ConnectionsNodeView node;
        [SerializeField, Min(0f), Tooltip("CPU units used per MB transferred at this node.")]
        private float cpuUnitsPerMB;
        [SerializeField, Min(0f), Tooltip("Disk I/O MB used per MB transferred at this node.")]
        private float diskIoMBPerMB;
        [SerializeField, Min(0f), Tooltip("GB reserved at this node while the connection is active.")]
        private float memoryGB;

        public ConnectionsNodeView Node => node;
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
    public sealed class ConnectionsLogicalConnectionDefinition
    {
        [SerializeField] private string connectionId;
        [SerializeField] private ConnectionsNodeRequirement[] route;
        [SerializeField, Min(0f)] private float totalWorkMB;
        [SerializeField, Min(0f)] private float desiredRateMBps;
        [SerializeField, Min(0f), Tooltip("Seconds after simulation start before this connection is established.")]
        private float startDelaySeconds;

        public string ConnectionId => connectionId;
        public ConnectionsNodeRequirement[] Route => route;
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

            foreach (ConnectionsNodeRequirement requirement in route)
                requirement?.ClampNonnegative();
        }
    }

    public enum ConnectionsLimitKind
    {
        None,
        PhysicalLinkBandwidth,
        NodeCpu,
        NodeDiskIo,
        NodeMemory
    }

    public enum ConnectionsLifecycle
    {
        Pending,
        Active,
        Complete
    }

    // Runtime state belongs to the simulation, never to a UI component.
    public sealed class ConnectionsLogicalConnectionState
    {
        internal ConnectionsLogicalConnectionState(
            ConnectionsLogicalConnectionDefinition definition,
            ConnectionsPhysicalLinkResources[] routeLinks)
        {
            Definition = definition;
            RouteLinks = routeLinks;
            Lifecycle = definition.StartDelaySeconds > 0f ? ConnectionsLifecycle.Pending : ConnectionsLifecycle.Active;
        }

        public ConnectionsLogicalConnectionDefinition Definition { get; }
        internal ConnectionsPhysicalLinkResources[] RouteLinks { get; }
        internal bool MemoryAdmitted { get; set; }

        public string ConnectionId => Definition.ConnectionId;
        public float CompletedWorkMB { get; internal set; }
        public float RemainingWorkMB => Mathf.Max(0f, Definition.TotalWorkMB - CompletedWorkMB);
        public float ActualRateMBps { get; internal set; }
        public float Satisfaction => IsComplete ? 1f : IsPending ? 0f : Definition.DesiredRateMBps > 0f ?
            ActualRateMBps / Definition.DesiredRateMBps : 0f;
        public ConnectionsLifecycle Lifecycle { get; internal set; }
        public bool IsPending => Lifecycle == ConnectionsLifecycle.Pending;
        public bool IsComplete => Lifecycle == ConnectionsLifecycle.Complete;
        public bool IsActive => Lifecycle == ConnectionsLifecycle.Active;
        public float EstablishedAtSeconds { get; internal set; }
        public ConnectionsLimitKind LimitKind { get; internal set; }
        public string LimitingResourceId { get; internal set; }

        internal float CpuCost(ConnectionsNodeView node)
        {
            foreach (ConnectionsNodeRequirement requirement in Definition.Route)
                if (requirement.Node == node)
                    return requirement.CpuUnitsPerMB;
            return 0f;
        }

        internal float DiskIoCost(ConnectionsNodeView node)
        {
            foreach (ConnectionsNodeRequirement requirement in Definition.Route)
                if (requirement.Node == node)
                    return requirement.DiskIoMBPerMB;
            return 0f;
        }

        internal bool Uses(ConnectionsPhysicalLinkResources link)
        {
            foreach (ConnectionsPhysicalLinkResources routeLink in RouteLinks)
                if (routeLink == link)
                    return true;
            return false;
        }
    }
}
