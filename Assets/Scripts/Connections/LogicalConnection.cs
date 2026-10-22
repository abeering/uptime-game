using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class RouteNode
    {
        [SerializeField] private NodeView node;
        public NodeView Node => node;
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
        [SerializeField] private string connectionId;
        [SerializeField] private string claimedPurpose;
        [SerializeField] private string activityDescription;
        [SerializeField] private OperationKind currentOperation;
        [SerializeField] private RouteNode[] route;
        [SerializeField, Min(0f)] private float totalWork;
        [FormerlySerializedAs("desiredWorkRate")]
        [SerializeField, Min(0f), Tooltip("Requested LOAD; one allocated LOAD advances one WORK unit per second.")]
        private float desiredLoad = 5f;
        [SerializeField, Min(0f), Tooltip("Seconds after simulation start before this connection is established.")]
        private float startDelaySeconds;

        public string ConnectionId => connectionId;
        public string ClaimedPurpose => claimedPurpose;
        public string ActivityDescription => activityDescription;
        public OperationKind CurrentOperation => currentOperation;
        public RouteNode[] Route => route;
        public NodeView TargetNode => route != null && route.Length > 0 ? route[route.Length - 1]?.Node : null;
        public float TotalWork => Mathf.Max(0f, totalWork);
        public float DesiredLoad => Mathf.Max(0f, desiredLoad);
        public float StartDelaySeconds => Mathf.Max(0f, startDelaySeconds);

        internal void ClampNonnegative()
        {
            totalWork = TotalWork;
            desiredLoad = DesiredLoad;
            startDelaySeconds = StartDelaySeconds;
        }
    }

    public enum Lifecycle
    {
        Pending,
        Active,
        Complete
    }

    // Runtime state belongs to the fixed-step simulation, never to a view.
    public sealed class LogicalConnectionState
    {
        internal LogicalConnectionState(LogicalConnectionDefinition definition,
            PhysicalLinkReference[] routeLinks, NodeResources targetResources)
        {
            Definition = definition;
            RouteLinks = routeLinks;
            TargetResources = targetResources;
            Lifecycle = definition.StartDelaySeconds > 0f ? Lifecycle.Pending : Lifecycle.Active;
        }

        public LogicalConnectionDefinition Definition { get; }
        public NodeResources TargetResources { get; }
        internal PhysicalLinkReference[] RouteLinks { get; }
        public string ConnectionId => Definition.ConnectionId;
        public OperationKind CurrentOperation => Definition.CurrentOperation;
        public NodeView TargetNode => Definition.TargetNode;
        public float CompletedWork { get; internal set; }
        public float RemainingWork => Mathf.Max(0f, Definition.TotalWork - CompletedWork);
        public float AllocatedLoad { get; internal set; }
        public float ActualWorkRate => AllocatedLoad;
        public float Satisfaction => IsComplete ? 1f : IsPending ? 0f : Definition.DesiredLoad > 0f ?
            AllocatedLoad / Definition.DesiredLoad : 0f;
        public Lifecycle Lifecycle { get; internal set; }
        public bool IsPending => Lifecycle == Lifecycle.Pending;
        public bool IsComplete => Lifecycle == Lifecycle.Complete;
        public bool IsActive => Lifecycle == Lifecycle.Active;
        public float EstablishedAtSeconds { get; internal set; }

        internal bool Uses(PhysicalLinkReference link)
        {
            foreach (PhysicalLinkReference routeLink in RouteLinks)
                if (routeLink == link)
                    return true;
            return false;
        }
    }
}
