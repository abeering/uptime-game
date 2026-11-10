using System;
using System.Collections.Generic;
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

    public enum CompletionEffect { None, InfectionDelivery, ConditionApplication, PayloadLaunch,
        DuelSchedule }

    [Serializable]
    public sealed class PayloadLaunchSpec
    {
        [SerializeField] private string destinationNodeId;
        [SerializeField] private PayloadMotionProfile motionProfile;

        public string DestinationNodeId => destinationNodeId;
        public PayloadMotionProfile MotionProfile => motionProfile;
        public bool IsValid => !string.IsNullOrWhiteSpace(destinationNodeId) &&
            Enum.IsDefined(typeof(PayloadMotionProfile), motionProfile);

        public PayloadLaunchSpec(string destinationNodeId, PayloadMotionProfile motionProfile)
        {
            this.destinationNodeId = destinationNodeId;
            this.motionProfile = motionProfile;
        }

        public PayloadLaunchSpec Copy() => new PayloadLaunchSpec(destinationNodeId, motionProfile);
    }

    public enum InfectionKind { PhantomLoad, Blackout, Spawner, PayloadSpawner, Backdoor }

    [Serializable]
    public sealed class InfectionPayload
    {
        [SerializeField] private InfectionKind kind;
        [SerializeField] private float requestedLoad;
        [SerializeField] private float cadenceSeconds;

        public InfectionKind Kind => kind;
        public float RequestedLoad => requestedLoad;
        public float CadenceSeconds => cadenceSeconds;
        public bool IsValid => kind == InfectionKind.PhantomLoad ?
            requestedLoad > 0f && !float.IsNaN(requestedLoad) &&
            !float.IsInfinity(requestedLoad) && cadenceSeconds == 0f :
            kind == InfectionKind.Blackout || kind == InfectionKind.Backdoor ?
                requestedLoad == 0f && cadenceSeconds == 0f :
            (kind == InfectionKind.Spawner || kind == InfectionKind.PayloadSpawner) &&
            requestedLoad == 0f &&
            cadenceSeconds > 0f && !float.IsNaN(cadenceSeconds) &&
            !float.IsInfinity(cadenceSeconds);

        public InfectionPayload(InfectionKind kind, float requestedLoad)
        {
            if (kind != InfectionKind.PhantomLoad)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (requestedLoad <= 0f || float.IsNaN(requestedLoad) ||
                float.IsInfinity(requestedLoad))
                throw new ArgumentOutOfRangeException(nameof(requestedLoad),
                    "Phantom LOAD requires a finite positive LOAD amount.");
            this.kind = kind;
            this.requestedLoad = requestedLoad;
        }

        public InfectionPayload(InfectionKind kind, float requestedLoad, float cadenceSeconds)
        {
            if (!Enum.IsDefined(typeof(InfectionKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == InfectionKind.PhantomLoad &&
                (requestedLoad <= 0f || float.IsNaN(requestedLoad) || float.IsInfinity(requestedLoad)))
                throw new ArgumentOutOfRangeException(nameof(requestedLoad));
            if ((kind == InfectionKind.Spawner || kind == InfectionKind.PayloadSpawner) &&
                (cadenceSeconds <= 0f || float.IsNaN(cadenceSeconds) || float.IsInfinity(cadenceSeconds)))
                throw new ArgumentOutOfRangeException(nameof(cadenceSeconds));
            if (kind != InfectionKind.PhantomLoad && requestedLoad != 0f ||
                kind != InfectionKind.Spawner && kind != InfectionKind.PayloadSpawner &&
                    cadenceSeconds != 0f)
                throw new ArgumentException("Infection parameters do not match the kind.");
            this.kind = kind;
            this.requestedLoad = requestedLoad;
            this.cadenceSeconds = cadenceSeconds;
        }

        public InfectionPayload Copy() => new InfectionPayload(kind, requestedLoad, cadenceSeconds);
    }

    public sealed class NodeInfection
    {
        public NodeInfection(NodeResources target, InfectionPayload payload, float appliedAtSeconds,
            string sourceConnectionId, int sourceOperationIndex)
        {
            Target = target;
            Payload = payload.Copy();
            AppliedAtSeconds = appliedAtSeconds;
            SourceConnectionId = sourceConnectionId;
            SourceOperationIndex = sourceOperationIndex;
            NextSpawnAtSeconds = payload.Kind == InfectionKind.Spawner ||
                payload.Kind == InfectionKind.PayloadSpawner ?
                appliedAtSeconds + payload.CadenceSeconds : 0f;
        }

        public NodeResources Target { get; }
        public InfectionPayload Payload { get; }
        public float AppliedAtSeconds { get; }
        public string SourceConnectionId { get; }
        public int SourceOperationIndex { get; }
        public float NextSpawnAtSeconds { get; internal set; }
    }

    public sealed class ConnectionEffectOccurrence
    {
        public ConnectionEffectOccurrence(CompletionEffect type, string connectionId,
            int operationIndex, string targetNodeId, float timeSeconds,
            InfectionPayload payload = null, ConditionPayload conditionPayload = null,
            PayloadLaunchSpec launchSpec = null)
        {
            Type = type;
            ConnectionId = connectionId;
            OperationIndex = operationIndex;
            TargetNodeId = targetNodeId;
            TimeSeconds = timeSeconds;
            Payload = payload?.Copy();
            ConditionPayload = conditionPayload?.Copy();
            LaunchSpec = launchSpec?.Copy();
        }

        public CompletionEffect Type { get; }
        public string ConnectionId { get; }
        public int OperationIndex { get; }
        public string TargetNodeId { get; }
        public float TimeSeconds { get; }
        public InfectionPayload Payload { get; }
        public ConditionPayload ConditionPayload { get; }
        public PayloadLaunchSpec LaunchSpec { get; }
        public string LaunchedPayloadId { get; internal set; }
        // Filled by the host before publishing the existing completion occurrence.
        public bool? InfectionApplied { get; internal set; }
    }

    [Serializable]
    public sealed class PlannedOperationLog
    {
        [SerializeField, Range(0f, 1f)] private float workFraction;
        [SerializeField] private string activity;
        [SerializeField] private string result;

        public float WorkFraction => workFraction;
        public string Activity => activity;
        public string Result => result;

        public PlannedOperationLog(float workFraction, string activity, string result = null)
        {
            if (float.IsNaN(workFraction) || float.IsInfinity(workFraction) ||
                workFraction < 0f || workFraction > 1f)
                throw new ArgumentOutOfRangeException(nameof(workFraction));
            this.workFraction = workFraction;
            this.activity = activity;
            this.result = result;
        }

        public PlannedOperationLog Copy() => new PlannedOperationLog(workFraction, activity, result);
    }

    // Only this earned observation is exposed to future terminal presentation.
    public sealed class EmittedConnectionLog
    {
        public EmittedConnectionLog(string connectionId, int sequence, int operationIndex,
            OperationKind operation, int authoredIndex, float timeSeconds,
            float workThreshold, string activity, string result)
        {
            ConnectionId = connectionId; Sequence = sequence;
            OperationIndex = operationIndex; Operation = operation;
            AuthoredIndex = authoredIndex; TimeSeconds = timeSeconds;
            WorkThreshold = workThreshold; Activity = activity; Result = result;
        }

        public string ConnectionId { get; }
        public int Sequence { get; }
        public int OperationIndex { get; }
        public OperationKind Operation { get; }
        public int AuthoredIndex { get; }
        public float TimeSeconds { get; }
        public float WorkThreshold { get; }
        public string Activity { get; }
        public string Result { get; }
    }

    [Serializable]
    public sealed class OperationStage
    {
        [SerializeField] private OperationKind operation;
        [SerializeField, Min(0f)] private float totalWork;
        [SerializeField, Min(0f)] private float desiredLoad = 5f;
        [SerializeField] private string activityDescription;
        [SerializeField] private CompletionEffect completionEffect;
        [SerializeField] private InfectionPayload infectionPayload;
        [SerializeField] private ConditionPayload conditionPayload;
        [SerializeField] private PayloadLaunchSpec launchSpec;
        [SerializeField] private TimedModifierBehaviorDefinition[] behaviors;
        [SerializeField] private PlannedOperationLog[] plannedLogs;

        public OperationKind Operation => operation;
        public float TotalWork => Mathf.Max(0f, totalWork);
        public float DesiredLoad => Mathf.Max(0f, desiredLoad);
        public string ActivityDescription => activityDescription;
        public CompletionEffect EffectOnCompletion => completionEffect;
        public InfectionPayload InfectionPayload => infectionPayload;
        public ConditionPayload ConditionPayload => conditionPayload;
        public PayloadLaunchSpec LaunchSpec => launchSpec;
        public TimedModifierBehaviorDefinition[] Behaviors => behaviors ?? Array.Empty<TimedModifierBehaviorDefinition>();
        public PlannedOperationLog[] PlannedLogs => plannedLogs ?? Array.Empty<PlannedOperationLog>();
        public OperationStage() { }
        public OperationStage(OperationKind kind, float work, float load, string description,
            CompletionEffect effect = CompletionEffect.None,
            TimedModifierBehaviorDefinition[] behaviors = null,
            InfectionPayload payload = null, ConditionPayload condition = null,
            PayloadLaunchSpec launch = null,
            PlannedOperationLog[] logs = null)
        { operation = kind; totalWork = work; desiredLoad = load; activityDescription = description;
            completionEffect = effect; this.behaviors = CopyBehaviors(behaviors);
            infectionPayload = effect == CompletionEffect.InfectionDelivery ?
                (payload ?? new InfectionPayload(InfectionKind.PhantomLoad, 8f)).Copy() : null;
            conditionPayload = effect == CompletionEffect.ConditionApplication ? condition?.Copy() : null;
            launchSpec = effect == CompletionEffect.PayloadLaunch ? launch?.Copy() : null;
            plannedLogs = CopyLogs(logs); }

        internal static PlannedOperationLog[] CopyLogs(PlannedOperationLog[] source)
        {
            if (source == null || source.Length == 0) return Array.Empty<PlannedOperationLog>();
            var result = new PlannedOperationLog[source.Length];
            for (int i = 0; i < result.Length; i++) result[i] = source[i]?.Copy();
            return result;
        }

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
        [SerializeField, Min(0f), Tooltip("Requested LOAD; productive WORK also depends on the effective WORK rate multiplier.")]
        private float desiredLoad = 5f;
        [SerializeField, Min(0f), Tooltip("Seconds after simulation start before this connection is established.")]
        private float startDelaySeconds;
        [SerializeField, Tooltip("Ordered operation arc. Empty preserves the legacy single-operation fields above.")]
        private OperationStage[] operations;
        [SerializeField] private TimedModifierBehaviorDefinition[] connectionBehaviors;
        [SerializeReference] private LoaderAttackPlan loaderAttack;
        [SerializeField] private bool naturalLoadNoise;
        [SerializeField] private int naturalLoadSeed;
        [SerializeField, Min(0f)] private float identityHopSeconds;
        [SerializeField] private int identitySeed;
        [SerializeField] private ConnectionIdentityMode identityMode;

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
        public InfectionPayload PayloadAt(int index) => HasArc ? StageAt(index).InfectionPayload : null;
        public ConditionPayload ConditionAt(int index) => HasArc ? StageAt(index).ConditionPayload : null;
        public PayloadLaunchSpec LaunchAt(int index) => HasArc ? StageAt(index).LaunchSpec : null;
        public TimedModifierBehaviorDefinition[] ConnectionBehaviors =>
            connectionBehaviors ?? Array.Empty<TimedModifierBehaviorDefinition>();
        public LoaderAttackPlan LoaderAttack => loaderAttack;
        public bool NaturalLoadNoise => naturalLoadNoise;
        public int NaturalLoadSeed => naturalLoadSeed;
        public float IdentityHopSeconds => identityHopSeconds;
        public int IdentitySeed => identitySeed;
        public ConnectionIdentityMode IdentityMode => identityMode == ConnectionIdentityMode.Normal &&
            identityHopSeconds > 0f ? ConnectionIdentityMode.ContinuousHopping : identityMode;
        public TimedModifierBehaviorDefinition[] BehaviorsAt(int index) => HasArc ?
            StageAt(index).Behaviors : Array.Empty<TimedModifierBehaviorDefinition>();
        public PlannedOperationLog[] PlannedLogsAt(int index) => HasArc ?
            StageAt(index).PlannedLogs : Array.Empty<PlannedOperationLog>();
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
                connectionBehaviors = OperationStage.CopyBehaviors(plan.ConnectionBehaviors),
                loaderAttack = plan.LoaderAttack?.Copy(),
                naturalLoadNoise = plan.NaturalLoadNoise,
                naturalLoadSeed = plan.NaturalLoadSeed,
                identityHopSeconds = plan.IdentityHopSeconds,
                identitySeed = plan.IdentitySeed,
                identityMode = plan.IdentityMode
            };
            for (int i = 0; i < result.route.Length; i++) result.route[i] = new RouteNode(plan.Route[i]);
            for (int i = 0; i < result.operations.Length; i++)
            {
                OperationStage stage = plan.Operations[i];
                result.operations[i] = new OperationStage(stage.Operation, stage.TotalWork,
                    stage.DesiredLoad, stage.ActivityDescription, stage.EffectOnCompletion,
                    stage.Behaviors, stage.InfectionPayload, stage.ConditionPayload, stage.LaunchSpec,
                    stage.PlannedLogs);
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
        Connecting,
        Active,
        Complete
    }

    public enum ConnectionResolution { Completed, Killed, Canceled, Withdrawn }
    public enum ConnectionTerminationCause { None, Player, Blackout, Isolation, AttackerWithdrawal }

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
            logMilestones = new LogMilestone[definition.OperationCount][];
            nextLogMilestone = new int[definition.OperationCount];
            for (int operationIndex = 0; operationIndex < definition.OperationCount; operationIndex++)
            {
                PlannedOperationLog[] source = definition.PlannedLogsAt(operationIndex);
                var milestones = new List<LogMilestone>(source.Length);
                for (int authoredIndex = 0; authoredIndex < source.Length; authoredIndex++)
                    if (source[authoredIndex] != null)
                        milestones.Add(new LogMilestone(source[authoredIndex].Copy(),
                            authoredIndex, definition.WorkAt(operationIndex)));
                milestones.Sort((a, b) =>
                {
                    int byWork = a.Threshold.CompareTo(b.Threshold);
                    return byWork != 0 ? byWork : a.AuthoredIndex.CompareTo(b.AuthoredIndex);
                });
                logMilestones[operationIndex] = milestones.ToArray();
            }
            Behaviors = new ConnectionBehaviorRuntime(definition);
            for (int index = 0; index < definition.OperationCount; index++)
                if (definition.IsHostile &&
                    definition.EffectAt(index) == CompletionEffect.InfectionDelivery)
                { IsInfector = true; break; }
            Identity = new ConnectionIdentityRuntime(definition.ConnectionId,
                definition.IdentityHopSeconds, definition.IdentitySeed, definition.IdentityMode);
            Lifecycle = definition.StartDelaySeconds > 0f ? Lifecycle.Pending : Lifecycle.Connecting;
        }

        private readonly bool[] deliveredEffects;
        private readonly LogMilestone[][] logMilestones;
        private readonly int[] nextLogMilestone;
        private readonly List<EmittedConnectionLog> emittedLogs = new List<EmittedConnectionLog>();
        public IReadOnlyList<EmittedConnectionLog> EmittedLogs => emittedLogs;

        private readonly struct LogMilestone
        {
            public LogMilestone(PlannedOperationLog entry, int authoredIndex, float baseWork)
            { Entry = entry; AuthoredIndex = authoredIndex; Threshold = baseWork * entry.WorkFraction; }
            public PlannedOperationLog Entry { get; }
            public int AuthoredIndex { get; }
            public float Threshold { get; }
        }

        internal void EmitReachedLogs(float previousWork, float reachedWork, float timeSeconds,
            Action<EmittedConnectionLog> onEmitted)
        {
            if (!IsActive || CompletedWork <= previousWork) return;
            int operationIndex = CurrentOperationIndex;
            LogMilestone[] milestones = logMilestones[operationIndex];
            while (nextLogMilestone[operationIndex] < milestones.Length &&
                milestones[nextLogMilestone[operationIndex]].Threshold <= reachedWork)
            {
                LogMilestone milestone = milestones[nextLogMilestone[operationIndex]++];
                var emitted = new EmittedConnectionLog(ConnectionId, emittedLogs.Count + 1,
                    operationIndex, Definition.OperationAt(operationIndex), milestone.AuthoredIndex,
                    timeSeconds, milestone.Threshold, milestone.Entry.Activity,
                    milestone.Entry.Result);
                emittedLogs.Add(emitted);
                onEmitted?.Invoke(emitted);
            }
        }

        public LogicalConnectionDefinition Definition { get; }
        public NodeResources TargetResources { get; }
        public ConnectionBehaviorRuntime Behaviors { get; }
        public ConnectionIdentityRuntime Identity { get; }
        internal PhysicalLinkReference[] RouteLinks { get; }
        public string ConnectionId => Definition.ConnectionId;
        public string KnownIdentity => Identity.KnownIdentity;
        public bool IsHostile => Definition.IsHostile;
        public int CurrentOperationIndex { get; internal set; }
        public int OperationCount => Definition.OperationCount;
        public OperationKind CurrentOperation => Definition.OperationAt(CurrentOperationIndex);
        public float BaseWork => Definition.WorkAt(CurrentOperationIndex);
        public float BaseLoad => Definition.LoadAt(CurrentOperationIndex);
        public float NaturalLoadDelta { get; internal set; }
        public float OperationStartedAtSeconds { get; internal set; }
        public float CurrentTotalWork => Behaviors.EffectiveWork(CurrentOperationIndex);
        public float DesiredLoad => Behaviors.EffectiveLoad(CurrentOperationIndex);
        public string ActivityDescription => Definition.ActivityAt(CurrentOperationIndex);
        public NodeView TargetNode => Definition.TargetNode;
        public float CompletedWork { get; internal set; }
        public float RemainingWork => Mathf.Max(0f, CurrentTotalWork - CompletedWork);
        public float AllocatedLoad { get; internal set; }
        internal float BackdoorWorkFactor { get; set; } = 1f;
        // Scope is a plan property: every operation of a hostile delivery carrier qualifies.
        internal bool IsInfector { get; }
        public float EffectiveWorkRateMultiplier =>
            Behaviors.EffectiveWorkRateMultiplier(CurrentOperationIndex) * BackdoorWorkFactor;
        public float ActualWorkRate
        {
            get
            {
                float rate = AllocatedLoad * EffectiveWorkRateMultiplier;
                if (float.IsNaN(rate) || float.IsInfinity(rate))
                    throw new InvalidOperationException("Actual WORK rate must be finite.");
                return rate;
            }
        }
        public float Satisfaction => IsComplete ? 1f : !IsActive || IsTransitioning ? 0f : DesiredLoad > 0f ?
            AllocatedLoad / DesiredLoad : 0f;
        public bool IsTransitioning { get; internal set; }
        public bool IsDemanding => IsActive && !IsTransitioning;
        public LoadPerformance Performance => IsComplete || IsTransitioning ? LoadPerformance.AtSpeed :
            AllocatedLoad <= 0.0001f ? LoadPerformance.Stalled :
            Satisfaction >= 0.98f ? LoadPerformance.AtSpeed : LoadPerformance.Slowed;
        public Lifecycle Lifecycle { get; internal set; }
        public bool IsPending => Lifecycle == Lifecycle.Pending;
        public bool IsConnecting => Lifecycle == Lifecycle.Connecting;
        public bool IsComplete => Lifecycle == Lifecycle.Complete;
        public bool IsNormalDeparture => Resolution == ConnectionResolution.Completed ||
            Resolution == ConnectionResolution.Withdrawn;
        public bool IsActive => Lifecycle == Lifecycle.Active;
        public float EstablishedAtSeconds { get; internal set; }
        public bool HasEstablished { get; internal set; }
        public float SpawnedAtSeconds { get; internal set; }
        public float LinkTravelElapsedSeconds { get; internal set; }
        public int ReachedRouteNodeIndex { get; internal set; }
        public float RouteReveal => IsActive ? 1f :
            Mathf.Clamp01((ReachedRouteNodeIndex +
                (RouteLinks.Length > ReachedRouteNodeIndex ? LinkTravelElapsedSeconds /
                    Mathf.Max(0.001f, TravelSecondsPerLink) : 0f)) /
                Mathf.Max(1, RouteLinks.Length));
        public float TravelSecondsPerLink { get; internal set; } = 0.6f;
        public float ResolvedAtSeconds { get; internal set; }
        public ConnectionResolution? Resolution { get; internal set; }
        public ConnectionTerminationCause TerminationCause { get; internal set; }
        public Lifecycle TerminationPhase { get; internal set; }
        public string FailureNodeId { get; internal set; }
        public float HealthPenaltyAmount { get; internal set; }
        public float LoaderActivatedAtSeconds { get; internal set; } = -1f;
        public float LoaderWithdrawAtSeconds { get; internal set; } = -1f;
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
                TargetNode.NodeId, timeSeconds, Definition.PayloadAt(operationIndex),
                Definition.ConditionAt(operationIndex), Definition.LaunchAt(operationIndex));
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
