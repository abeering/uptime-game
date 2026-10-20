using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class NodeRequirement
    {
        [SerializeField] private NodeView node;
        [FormerlySerializedAs("cpuUnitsPerMB")]
        [SerializeField, Min(0f), Tooltip("CPU units used per work unit at this node.")]
        private float cpuUnitsPerWork;
        [FormerlySerializedAs("diskIoMBPerMB")]
        [SerializeField, Min(0f), Tooltip("Disk I/O MB used per work unit at this node.")]
        private float diskIoMBPerWork;
        [SerializeField, Min(0f), Tooltip("GB reserved at this node while the connection is active.")]
        private float memoryGB;

        public NodeView Node => node;
        public float CpuUnitsPerWork => Mathf.Max(0f, cpuUnitsPerWork);
        public float DiskIoMBPerWork => Mathf.Max(0f, diskIoMBPerWork);
        public float MemoryGB => Mathf.Max(0f, memoryGB);

        internal void ClampNonnegative()
        {
            cpuUnitsPerWork = CpuUnitsPerWork;
            diskIoMBPerWork = DiskIoMBPerWork;
            memoryGB = MemoryGB;
        }
    }

    public enum OperationKind
    {
        Read,
        Write,
        Execute
    }

    [Serializable]
    public sealed class LogicalConnectionDefinition
    {
        // V0 has one current operation; its work and costs are authored here until stages exist.
        [SerializeField] private string connectionId;
        [SerializeField] private string claimedPurpose;
        [SerializeField] private string activityDescription;
        [SerializeField] private OperationKind currentOperation;
        [SerializeField] private NodeRequirement[] route;
        [FormerlySerializedAs("totalWorkMB")]
        [SerializeField, Min(0f)] private float totalWork;
        [FormerlySerializedAs("desiredRateMBps")]
        [SerializeField, Min(0f)] private float desiredWorkRate;
        [SerializeField, Min(0f), Tooltip("Network MB consumed on each traversed link per work unit.")]
        private float bandwidthMBPerWork = 1f;
        [SerializeField, Min(0f), Tooltip("Seconds after simulation start before this connection is established.")]
        private float startDelaySeconds;

        public string ConnectionId => connectionId;
        public string ClaimedPurpose => claimedPurpose;
        public string ActivityDescription => activityDescription;
        public OperationKind CurrentOperation => currentOperation;
        public NodeRequirement[] Route => route;
        public float TotalWork => Mathf.Max(0f, totalWork);
        public float DesiredWorkRate => Mathf.Max(0f, desiredWorkRate);
        public float BandwidthMBPerWork => Mathf.Max(0f, bandwidthMBPerWork);
        public float StartDelaySeconds => Mathf.Max(0f, startDelaySeconds);

        internal void ClampNonnegative()
        {
            totalWork = TotalWork;
            desiredWorkRate = DesiredWorkRate;
            bandwidthMBPerWork = BandwidthMBPerWork;
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
        public OperationKind CurrentOperation => Definition.CurrentOperation;
        public float CompletedWork { get; internal set; }
        public float RemainingWork => Mathf.Max(0f, Definition.TotalWork - CompletedWork);
        public float ActualWorkRate { get; internal set; }
        public float ActualBandwidthMBps => ActualWorkRate * Definition.BandwidthMBPerWork;
        public float DesiredBandwidthMBps => Definition.DesiredWorkRate * Definition.BandwidthMBPerWork;
        public float Satisfaction => IsComplete ? 1f : IsPending ? 0f : Definition.DesiredWorkRate > 0f ?
            ActualWorkRate / Definition.DesiredWorkRate : 0f;
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
                    return requirement.CpuUnitsPerWork;
            return 0f;
        }

        internal float DiskIoCost(NodeView node)
        {
            foreach (NodeRequirement requirement in Definition.Route)
                if (requirement.Node == node)
                    return requirement.DiskIoMBPerWork;
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
