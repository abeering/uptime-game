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

    public enum CompletionEffect { None, InfectionDelivery }

    public sealed class ConnectionEffectOccurrence
    {
        public ConnectionEffectOccurrence(CompletionEffect type, string connectionId,
            int operationIndex, string targetNodeId, float timeSeconds)
        {
            Type = type;
            ConnectionId = connectionId;
            OperationIndex = operationIndex;
            TargetNodeId = targetNodeId;
            TimeSeconds = timeSeconds;
        }

        public CompletionEffect Type { get; }
        public string ConnectionId { get; }
        public int OperationIndex { get; }
        public string TargetNodeId { get; }
        public float TimeSeconds { get; }
    }

    [Serializable]
    public sealed class OperationStage
    {
        [SerializeField] private OperationKind operation;
        [SerializeField, Min(0f)] private float totalWork;
        [SerializeField, Min(0f)] private float desiredLoad = 5f;
        [SerializeField] private string activityDescription;
        [SerializeField] private CompletionEffect completionEffect;
        [SerializeField] private TimedModifierBehaviorDefinition[] behaviors;

        public OperationKind Operation => operation;
        public float TotalWork => Mathf.Max(0f, totalWork);
        public float DesiredLoad => Mathf.Max(0f, desiredLoad);
        public string ActivityDescription => activityDescription;
        public CompletionEffect EffectOnCompletion => completionEffect;
        public TimedModifierBehaviorDefinition[] Behaviors => behaviors ?? Array.Empty<TimedModifierBehaviorDefinition>();
        public OperationStage() { }
        public OperationStage(OperationKind kind, float work, float load, string description,
            CompletionEffect effect = CompletionEffect.None,
            TimedModifierBehaviorDefinition[] behaviors = null)
        { operation = kind; totalWork = work; desiredLoad = load; activityDescription = description;
            completionEffect = effect; this.behaviors = CopyBehaviors(behaviors); }

        internal static TimedModifierBehaviorDefinition[] CopyBehaviors(
            TimedModifierBehaviorDefinition[] source)
        {
            if (source == null || source.Length == 0) return Array.Empty<TimedModifierBehaviorDefinition>();
            var result = new TimedModifierBehaviorDefinition[source.Length];
            for (int i = 0; i < result.Length; i++) result[i] = source[i]?.Copy();
            return result;
        }
    }

    [Serializable]
    public sealed class LogicalConnectionDefinition
    {
        [SerializeField] private string connectionId;
        [SerializeField] private string profileId;
        [SerializeField] private bool isHostile;
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
        [SerializeField] private TimedModifierBehaviorDefinition[] connectionBehaviors;

        public string ConnectionId => connectionId;
        public string ProfileId => profileId;
        public bool IsHostile => isHostile;
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
        public CompletionEffect EffectAt(int index) => HasArc ? StageAt(index).EffectOnCompletion :
            CompletionEffect.None;
        public TimedModifierBehaviorDefinition[] ConnectionBehaviors =>
            connectionBehaviors ?? Array.Empty<TimedModifierBehaviorDefinition>();
        public TimedModifierBehaviorDefinition[] BehaviorsAt(int index) => HasArc ?
            StageAt(index).Behaviors : Array.Empty<TimedModifierBehaviorDefinition>();
        private bool HasArc => operations != null && operations.Length > 0;
        private OperationStage StageAt(int index) => operations[index] ??
            throw new InvalidOperationException($"Connection {connectionId} has an empty operation stage.");

        public static LogicalConnectionDefinition FromPlan(ConnectionSpawnPlan plan)
        {
            var result = new LogicalConnectionDefinition
            {
                connectionId = plan.ConnectionId,
                profileId = plan.ProfileId,
                isHostile = plan.IsHostile,
                claimedPurpose = plan.ClaimedPurpose,
                activityDescription = plan.Operations[0].ActivityDescription,
                currentOperation = plan.Operations[0].Operation,
                route = new RouteNode[plan.Route.Length],
                operations = new OperationStage[plan.Operations.Length],
                connectionBehaviors = OperationStage.CopyBehaviors(plan.ConnectionBehaviors)
            };
            for (int i = 0; i < result.route.Length; i++) result.route[i] = new RouteNode(plan.Route[i]);
            for (int i = 0; i < result.operations.Length; i++)
            {
                OperationStage stage = plan.Operations[i];
                result.operations[i] = new OperationStage(stage.Operation, stage.TotalWork,
                    stage.DesiredLoad, stage.ActivityDescription, stage.EffectOnCompletion,
                    stage.Behaviors);
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

    public enum ConnectionResolution { Completed, Killed }

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
            deliveredEffects = new bool[definition.OperationCount];
            Behaviors = new ConnectionBehaviorRuntime(definition);
            Lifecycle = definition.StartDelaySeconds > 0f ? Lifecycle.Pending : Lifecycle.Active;
        }

        private readonly bool[] deliveredEffects;

        public LogicalConnectionDefinition Definition { get; }
        public NodeResources TargetResources { get; }
        public ConnectionBehaviorRuntime Behaviors { get; }
        internal PhysicalLinkReference[] RouteLinks { get; }
        public string ConnectionId => Definition.ConnectionId;
        public bool IsHostile => Definition.IsHostile;
        public int CurrentOperationIndex { get; internal set; }
        public int OperationCount => Definition.OperationCount;
        public OperationKind CurrentOperation => Definition.OperationAt(CurrentOperationIndex);
        public float BaseWork => Definition.WorkAt(CurrentOperationIndex);
        public float BaseLoad => Definition.LoadAt(CurrentOperationIndex);
        public float CurrentTotalWork => Behaviors.EffectiveWork(CurrentOperationIndex);
        public float DesiredLoad => Behaviors.EffectiveLoad(CurrentOperationIndex);
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
        // Service evidence is retained with the resolved connection for later evaluation systems.
        public float HealthNominalDurationSeconds { get; internal set; }
        public float HealthDeadlineSeconds { get; internal set; }
        public bool HealthWasSubstantiallySlowed { get; internal set; }
        public bool HealthBecameLate { get; internal set; }

        internal ConnectionEffectOccurrence OnWorkCompleted(int operationIndex, float timeSeconds)
        {
            CompletionEffect effect = Definition.EffectAt(operationIndex);
            if (effect == CompletionEffect.None || deliveredEffects[operationIndex]) return null;
            deliveredEffects[operationIndex] = true;
            return new ConnectionEffectOccurrence(effect, ConnectionId, operationIndex,
                TargetNode.NodeId, timeSeconds);
        }

        internal void FinishOperation()
        {
            CompletedWork = Mathf.Max(CompletedWork, CurrentTotalWork);
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
