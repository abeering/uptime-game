using System;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    [Serializable]
    public sealed class ConnectionsToyNodeRequirement
    {
        [SerializeField] private ConnectionsToyNodeView node;
        [SerializeField, Min(0f), Tooltip("CPU units used per MB transferred at this node.")]
        private float cpuUnitsPerMB;
        [SerializeField, Min(0f), Tooltip("Disk I/O MB used per MB transferred at this node.")]
        private float diskIoMBPerMB;
        [SerializeField, Min(0f), Tooltip("GB reserved at this node while the connection is active.")]
        private float memoryGB;

        public ConnectionsToyNodeView Node => node;
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
    public sealed class ConnectionsToyLogicalConnectionDefinition
    {
        [SerializeField] private string connectionId;
        [SerializeField] private ConnectionsToyNodeRequirement[] route;
        [SerializeField, Min(0f)] private float totalWorkMB;
        [SerializeField, Min(0f)] private float desiredRateMBps;

        public string ConnectionId => connectionId;
        public ConnectionsToyNodeRequirement[] Route => route;
        public float TotalWorkMB => Mathf.Max(0f, totalWorkMB);
        public float DesiredRateMBps => Mathf.Max(0f, desiredRateMBps);

        internal void ClampNonnegative()
        {
            totalWorkMB = TotalWorkMB;
            desiredRateMBps = DesiredRateMBps;
            if (route == null)
                return;

            foreach (ConnectionsToyNodeRequirement requirement in route)
                requirement?.ClampNonnegative();
        }
    }

    public enum ConnectionsToyLimitKind
    {
        None,
        PhysicalLinkBandwidth,
        NodeCpu,
        NodeDiskIo,
        NodeMemory
    }

    // Runtime state belongs to the simulation, never to a UI component.
    public sealed class ConnectionsToyLogicalConnectionState
    {
        internal ConnectionsToyLogicalConnectionState(
            ConnectionsToyLogicalConnectionDefinition definition,
            ConnectionsToyPhysicalLinkResources[] routeLinks)
        {
            Definition = definition;
            RouteLinks = routeLinks;
        }

        public ConnectionsToyLogicalConnectionDefinition Definition { get; }
        internal ConnectionsToyPhysicalLinkResources[] RouteLinks { get; }
        internal bool MemoryAdmitted { get; set; }

        public string ConnectionId => Definition.ConnectionId;
        public float CompletedWorkMB { get; internal set; }
        public float RemainingWorkMB => Mathf.Max(0f, Definition.TotalWorkMB - CompletedWorkMB);
        public float ActualRateMBps { get; internal set; }
        public float Satisfaction => IsComplete ? 1f : Definition.DesiredRateMBps > 0f ?
            ActualRateMBps / Definition.DesiredRateMBps : 0f;
        public bool IsComplete { get; internal set; }
        public bool IsActive => !IsComplete;
        public ConnectionsToyLimitKind LimitKind { get; internal set; }
        public string LimitingResourceId { get; internal set; }

        internal float CpuCost(ConnectionsToyNodeView node)
        {
            foreach (ConnectionsToyNodeRequirement requirement in Definition.Route)
                if (requirement.Node == node)
                    return requirement.CpuUnitsPerMB;
            return 0f;
        }

        internal float DiskIoCost(ConnectionsToyNodeView node)
        {
            foreach (ConnectionsToyNodeRequirement requirement in Definition.Route)
                if (requirement.Node == node)
                    return requirement.DiskIoMBPerMB;
            return 0f;
        }

        internal bool Uses(ConnectionsToyPhysicalLinkResources link)
        {
            foreach (ConnectionsToyPhysicalLinkResources routeLink in RouteLinks)
                if (routeLink == link)
                    return true;
            return false;
        }
    }
}
