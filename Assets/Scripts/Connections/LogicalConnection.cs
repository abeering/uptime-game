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
        public RouteNode(NodeView value) { node = value; }
    }

    public enum OperationKind
    {
        Read,
        Write,
        Execute
    }

    [Serializable]
    public sealed class OperationStage
    {
        [SerializeField] private OperationKind operation;
        [SerializeField, Min(0f)] private float totalWork;
        [SerializeField, Min(0f)] private float desiredLoad = 5f;
        [SerializeField] private string activityDescription;

        public OperationKind Operation => operation;
        public float TotalWork => Mathf.Max(0f, totalWork);
        public float DesiredLoad => Mathf.Max(0f, desiredLoad);
        public string ActivityDescription => activityDescription;
        public OperationStage() { }
        public OperationStage(OperationKind kind, float work, float load, string description)
        { operation = kind; totalWork = work; desiredLoad = load; activityDescription = description; }
    }

    [Serializable]
    public sealed class LogicalConnectionDefinition
    {
        [SerializeField] private string connectionId;
        [SerializeField] private string profileId;
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
        [SerializeField, Tooltip("Ordered operation arc. Empty preserves the legacy single-operation fields above.")]
        private OperationStage[] operations;

        public string ConnectionId => connectionId;
        public string ProfileId => profileId;
        public string ClaimedPurpose => claimedPurpose;
        public string ActivityDescription => activityDescription;
        public OperationKind CurrentOperation => currentOperation;
        public RouteNode[] Route => route;
        public NodeView TargetNode => route != null && route.Length > 0 ? route[route.Length - 1]?.Node : null;
        public float TotalWork => Mathf.Max(0f, totalWork);
        public float DesiredLoad => Mathf.Max(0f, desiredLoad);
        public float StartDelaySeconds => Mathf.Max(0f, startDelaySeconds);
        public int OperationCount => operations != null && operations.Length > 0 ? operations.Length : 1;
        public OperationKind OperationAt(int index) => HasArc ? StageAt(index).Operation : currentOperation;
        public float WorkAt(int index) => HasArc ? StageAt(index).TotalWork : TotalWork;
        public float LoadAt(int index) => HasArc ? StageAt(index).DesiredLoad : DesiredLoad;
        public string ActivityAt(int index) => HasArc ? StageAt(index).ActivityDescription : activityDescription;
        private bool HasArc => operations != null && operations.Length > 0;
        private OperationStage StageAt(int index) => operations[index] ??
            throw new InvalidOperationException($"Connection {connectionId} has an empty operation stage.");

        public static LogicalConnectionDefinition FromPlan(ConnectionSpawnPlan plan)
        {
            var result = new LogicalConnectionDefinition
            {
                connectionId = plan.ConnectionId,
                profileId = plan.ProfileId,
                claimedPurpose = plan.ClaimedPurpose,
                activityDescription = plan.Operations[0].ActivityDescription,
                currentOperation = plan.Operations[0].Operation,
                route = new RouteNode[plan.Route.Length],
                operations = new OperationStage[plan.Operations.Length]
            };
            for (int i = 0; i < result.route.Length; i++) result.route[i] = new RouteNode(plan.Route[i]);
            for (int i = 0; i < result.operations.Length; i++)
            {
                OperationStage stage = plan.Operations[i];
                result.operations[i] = new OperationStage(stage.Operation, stage.TotalWork,
                    stage.DesiredLoad, stage.ActivityDescription);
            }
            return result;
        }

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

    public enum ConnectionResolution { Completed, Blocked }

    public enum LoadPerformance
    {
        AtSpeed,
        Slowed,
        Stalled
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
        public int CurrentOperationIndex { get; internal set; }
        public int OperationCount => Definition.OperationCount;
        public OperationKind CurrentOperation => Definition.OperationAt(CurrentOperationIndex);
        public float CurrentTotalWork => Definition.WorkAt(CurrentOperationIndex);
        public float DesiredLoad => Definition.LoadAt(CurrentOperationIndex);
        public string ActivityDescription => Definition.ActivityAt(CurrentOperationIndex);
        public NodeView TargetNode => Definition.TargetNode;
        public float CompletedWork { get; internal set; }
        public float RemainingWork => Mathf.Max(0f, CurrentTotalWork - CompletedWork);
        public float AllocatedLoad { get; internal set; }
        public float ActualWorkRate => AllocatedLoad;
        public float Satisfaction => IsComplete ? 1f : IsPending || IsTransitioning ? 0f : DesiredLoad > 0f ?
            AllocatedLoad / DesiredLoad : 0f;
        public bool IsTransitioning { get; internal set; }
        public bool IsDemanding => IsActive && !IsTransitioning;
        public LoadPerformance Performance => IsComplete || IsTransitioning ? LoadPerformance.AtSpeed :
            AllocatedLoad <= 0.0001f ? LoadPerformance.Stalled :
            Satisfaction >= 0.98f ? LoadPerformance.AtSpeed : LoadPerformance.Slowed;
        public Lifecycle Lifecycle { get; internal set; }
        public bool IsPending => Lifecycle == Lifecycle.Pending;
        public bool IsComplete => Lifecycle == Lifecycle.Complete;
        public bool IsActive => Lifecycle == Lifecycle.Active;
        public float EstablishedAtSeconds { get; internal set; }
        public float ResolvedAtSeconds { get; internal set; }
        public ConnectionResolution? Resolution { get; internal set; }

        internal void FinishOperation()
        {
            CompletedWork = CurrentTotalWork;
            AllocatedLoad = 0f;
            if (CurrentOperationIndex + 1 == OperationCount)
                Lifecycle = Lifecycle.Complete;
            else
            {
                CurrentOperationIndex++;
                CompletedWork = 0f;
                IsTransitioning = true;
            }
        }

        internal bool Uses(PhysicalLinkReference link)
        {
            foreach (PhysicalLinkReference routeLink in RouteLinks)
                if (routeLink == link)
                    return true;
            return false;
        }
    }
}
