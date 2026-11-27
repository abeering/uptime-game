using System;

namespace Uptime.Connections
{
    public enum ManualObjective { Delivery, Loader }

    public sealed class ConnectionSpawnOverrides
    {
        public ManualObjective Objective { get; }
        public InfectionKind? InfectionKind { get; }
        public float? InfectionLoad { get; }
        public float? InfectionCadence { get; }

        public ConnectionSpawnOverrides(ManualObjective objective,
            InfectionKind? infectionKind = null, float? infectionLoad = null,
            float? infectionCadence = null)
        {
            Objective = objective;
            InfectionKind = infectionKind;
            InfectionLoad = infectionLoad;
            InfectionCadence = infectionCadence;
        }
    }

    // A fully generated arrival. Copies its route and operations so a queued plan cannot drift.
    public sealed class ConnectionSpawnPlan
    {
        public float ScheduledSpawnTime { get; }
        public string ConnectionId { get; }
        public NodeView Origin => Route.Length > 0 ? Route[0] : null;
        public NodeView Target => Route.Length > 0 ? Route[Route.Length - 1] : null;
        public NodeView[] Route { get; }
        public string ProfileId { get; }
        public bool IsHostile { get; }
        public BusinessImportance Importance { get; }
        public float ImportanceSelectionRoll { get; }
        public float ImportanceSelectionChance { get; }
        public double BusinessWorkValue { get; }
        public string ClaimedPurpose { get; }
        public OperationStage[] Operations { get; }
        public TimedModifierBehaviorDefinition[] ConnectionBehaviors { get; }
        public ConnectionGenerationTrace GenerationTrace { get; }
        public LoaderAttackPlan LoaderAttack { get; }
        public string ParentNodeId { get; }
        public string ParentConnectionId { get; }
        public float ParentInfectionAppliedAtSeconds { get; }
        public bool NaturalLoadNoise { get; }
        public int NaturalLoadSeed { get; }
        public string HostileEventId { get; }
        public float IdentityHopSeconds { get; }
        public int IdentitySeed { get; }
        public ConnectionIdentityMode IdentityMode { get; }

        public ConnectionSpawnPlan(float scheduledSpawnTime, string connectionId, NodeView[] route,
            string profileId, string claimedPurpose, OperationStage[] operations,
            bool isHostile = false,
            TimedModifierBehaviorDefinition[] connectionBehaviors = null,
            ConnectionGenerationTrace generationTrace = null,
            string parentNodeId = null, string parentConnectionId = null,
            float parentInfectionAppliedAtSeconds = 0f,
            LoaderAttackPlan loaderAttack = null,
            bool naturalLoadNoise = false, int naturalLoadSeed = 0,
            string hostileEventId = null,
            float identityHopSeconds = 0f, int identitySeed = 0,
            ConnectionIdentityMode identityMode = ConnectionIdentityMode.Normal,
            double? businessWorkValue = null,
            BusinessImportance importance = BusinessImportance.Ordinary,
            float importanceSelectionRoll = -1f, float importanceSelectionChance = 0f)
        {
            if (float.IsNaN(scheduledSpawnTime) || float.IsInfinity(scheduledSpawnTime) || scheduledSpawnTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(scheduledSpawnTime));
            ScheduledSpawnTime = scheduledSpawnTime;
            ConnectionId = connectionId;
            Route = route != null ? (NodeView[])route.Clone() : Array.Empty<NodeView>();
            ProfileId = profileId;
            IsHostile = isHostile;
            if (isHostile && importance != BusinessImportance.Ordinary)
                throw new ArgumentException("Hostile connections cannot be IMPORTANT.");
            if (!Enum.IsDefined(typeof(BusinessImportance), importance)) throw new ArgumentOutOfRangeException(nameof(importance));
            Importance = importance;
            ImportanceSelectionRoll = importanceSelectionRoll;
            ImportanceSelectionChance = importanceSelectionChance;
            ClaimedPurpose = claimedPurpose;
            Operations = new OperationStage[operations?.Length ?? 0];
            for (int i = 0; i < Operations.Length; i++)
            {
                OperationStage stage = operations[i];
                Operations[i] = stage == null ? null : new OperationStage(stage.Operation,
                    stage.TotalWork, stage.DesiredLoad, stage.ActivityDescription,
                    stage.EffectOnCompletion, stage.Behaviors, stage.InfectionPayload,
                    stage.ConditionPayload, stage.LaunchSpec, stage.PlannedLogs, stage.IntendedDestination);
            }
            if (businessWorkValue.HasValue) BusinessWorkValue = businessWorkValue.Value;
            else foreach (var stage in Operations) BusinessWorkValue += stage?.TotalWork ?? 0f;
            ConnectionBehaviors = OperationStage.CopyBehaviors(connectionBehaviors);
            GenerationTrace = generationTrace?.Copy();
            LoaderAttack = loaderAttack?.Copy();
            ParentNodeId = parentNodeId;
            ParentConnectionId = parentConnectionId;
            ParentInfectionAppliedAtSeconds = parentInfectionAppliedAtSeconds;
            NaturalLoadNoise = naturalLoadNoise;
            NaturalLoadSeed = naturalLoadSeed;
            HostileEventId = hostileEventId;
            IdentityHopSeconds = identityHopSeconds;
            IdentitySeed = identitySeed;
            IdentityMode = identityMode == ConnectionIdentityMode.Normal && identityHopSeconds > 0f ?
                ConnectionIdentityMode.ContinuousHopping : identityMode;
        }

        // Only the traffic generator exposes retargeting; its validation preserves legal hostile routes.
        internal ConnectionSpawnPlan WithHostileRoute(NodeView[] route) => new ConnectionSpawnPlan(
            ScheduledSpawnTime, ConnectionId, route, ProfileId, ClaimedPurpose, Operations,
            IsHostile, ConnectionBehaviors, GenerationTrace, ParentNodeId, ParentConnectionId,
            ParentInfectionAppliedAtSeconds, LoaderAttack, NaturalLoadNoise, NaturalLoadSeed,
            HostileEventId, IdentityHopSeconds, IdentitySeed, IdentityMode, BusinessWorkValue, Importance, ImportanceSelectionRoll, ImportanceSelectionChance);

        public ConnectionSpawnPlan ForEvent(string eventId) => new ConnectionSpawnPlan(
            ScheduledSpawnTime, ConnectionId, Route, ProfileId, ClaimedPurpose,
            Operations, IsHostile, ConnectionBehaviors, GenerationTrace, ParentNodeId,
            ParentConnectionId, ParentInfectionAppliedAtSeconds, LoaderAttack,
            NaturalLoadNoise, NaturalLoadSeed, eventId,
            IdentityHopSeconds, IdentitySeed, IdentityMode, BusinessWorkValue, Importance, ImportanceSelectionRoll, ImportanceSelectionChance);

        public ConnectionSpawnPlan WithAmbientIdentity(ConnectionIdentityMode mode,
            float hopSeconds, int aliasSeed, float selectionRoll)
        {
            ConnectionGenerationTrace trace = GenerationTrace?.Copy();
            if (trace != null)
            {
                trace.identitySelectionEligible = true;
                trace.identitySelectionRoll = selectionRoll;
                trace.identitySelectionMode = mode.ToString();
            }
            return new ConnectionSpawnPlan(ScheduledSpawnTime, ConnectionId, Route,
                ProfileId, ClaimedPurpose, Operations, IsHostile, ConnectionBehaviors,
                trace, ParentNodeId, ParentConnectionId, ParentInfectionAppliedAtSeconds,
                LoaderAttack, NaturalLoadNoise, NaturalLoadSeed, HostileEventId,
                hopSeconds, aliasSeed, mode, BusinessWorkValue, Importance, ImportanceSelectionRoll, ImportanceSelectionChance);
        }

        // Explicit assignment for authored requests and future company events; no live mutation.
        public ConnectionSpawnPlan WithBusinessImportance(BusinessImportance importance,
            float selectionRoll = -1f, float selectionChance = 0f) => new ConnectionSpawnPlan(
                ScheduledSpawnTime, ConnectionId, Route, ProfileId, ClaimedPurpose, Operations,
                IsHostile, ConnectionBehaviors, GenerationTrace, ParentNodeId, ParentConnectionId,
                ParentInfectionAppliedAtSeconds, LoaderAttack, NaturalLoadNoise, NaturalLoadSeed,
                HostileEventId, IdentityHopSeconds, IdentitySeed, IdentityMode, BusinessWorkValue,
                importance, selectionRoll, selectionChance);

        // Called once after recipe/anomaly composition, never on shared PURPOSE definitions.
        internal ConnectionSpawnPlan WithLegitimateWorkload(float multiplier)
        {
            if (IsHostile) throw new InvalidOperationException("Hostile workload must remain unchanged.");
            var scaled = new OperationStage[Operations.Length];
            for (int i = 0; i < scaled.Length; i++)
            {
                var stage = Operations[i];
                scaled[i] = new OperationStage(stage.Operation, stage.TotalWork * multiplier,
                    stage.DesiredLoad * multiplier, stage.ActivityDescription, stage.EffectOnCompletion,
                    stage.Behaviors, stage.InfectionPayload, stage.ConditionPayload, stage.LaunchSpec, stage.PlannedLogs, stage.IntendedDestination);
            }
            var trace = GenerationTrace?.Copy();
            if (trace != null) trace.finalOperations = ConnectionGenerationTrace.Capture(scaled);
            return new ConnectionSpawnPlan(ScheduledSpawnTime, ConnectionId, Route, ProfileId,
                ClaimedPurpose, scaled, false, ConnectionBehaviors, trace, ParentNodeId,
                ParentConnectionId, ParentInfectionAppliedAtSeconds, LoaderAttack, NaturalLoadNoise,
                NaturalLoadSeed, HostileEventId, IdentityHopSeconds, IdentitySeed, IdentityMode, BusinessWorkValue, Importance, ImportanceSelectionRoll, ImportanceSelectionChance);
        }
    }

    public enum LoaderStructure { Single, Delayed }
    public enum LoaderPattern { Immediate, Overload, Spiking }
    public enum InfectorStructure { Standard, Delayed }
    public enum InfectorSignature { None, HighLoad, LowLoad, OperationDeviation, WorkLong, Waving }

    // Internal generation evidence; the concrete operation plan remains authoritative.
    [Serializable]
    public sealed class InfectorGenerationRecord
    {
        public string structure;
        public string primarySignature;
        public string secondarySignature;
        public int deliveryOperationIndex;
        public float normalNominalDeliverySeconds;
        public float composedNominalDeliverySeconds;
        public float finalNominalDeliverySeconds;
        public float finalOperationNominalSeconds;
        public float composedConservativeDeliverySeconds;
        public float finalConservativeDeliverySeconds;
        public float finalOperationConservativeSeconds;
        public float deliveryWindowWorkAdded;
        public string deliveryWindowCorrectionReason;

        public InfectorGenerationRecord Copy() => (InfectorGenerationRecord)MemberwiseClone();
    }

    // Internal, concrete attack intent; ordinary inspection never reads this plan.
    [Serializable]
    public sealed class LoaderAttackPlan
    {
        public LoaderStructure structure;
        public LoaderPattern pattern;
        public int attackOperationIndex;
        public float targetCapacity;
        public float normalLoad;
        public float attackLoad;
        public float onsetSeconds;
        public float spikeQuietSeconds;
        public float spikeElevatedSeconds;
        public float durationSeconds;

        public LoaderAttackPlan Copy() => (LoaderAttackPlan)MemberwiseClone();
    }

    // Internal generation evidence. This travels with the queued plan and recorder, never inspection.
    [Serializable]
    public sealed class ConnectionGenerationTrace
    {
        public string purposeId;
        public string objective;
        public bool identitySelectionEligible;
        public float identitySelectionRoll;
        public string identitySelectionMode;
        // Explains whether anomaly omission was intentional, and distinguishes benign outliers.
        public string generationDecision;
        public string selectedInfectionKind;
        public float selectedInfectionLoad;
        public float selectedInfectionCadence;
        public GeneratedKeywordSelection[] selectedKeywords;
        public string[] anomalies;
        public GeneratedOperationValue[] normalOperations;
        public GeneratedOperationValue[] finalOperations;
        public ForcedGenerationOptions forcedOptions;
        public LoaderAttackPlan loaderAttack;
        public bool hasInfector;
        public InfectorGenerationRecord infector;

        public ConnectionGenerationTrace Copy() => new ConnectionGenerationTrace
        {
            purposeId = purposeId, objective = objective,
            identitySelectionEligible = identitySelectionEligible,
            identitySelectionRoll = identitySelectionRoll,
            identitySelectionMode = identitySelectionMode,
            generationDecision = generationDecision,
            selectedInfectionKind = selectedInfectionKind,
            selectedInfectionLoad = selectedInfectionLoad,
            selectedInfectionCadence = selectedInfectionCadence,
            selectedKeywords = CopyKeywords(selectedKeywords),
            anomalies = anomalies != null ? (string[])anomalies.Clone() : Array.Empty<string>(),
            normalOperations = CopyValues(normalOperations),
            finalOperations = CopyValues(finalOperations),
            forcedOptions = forcedOptions?.Copy(),
            loaderAttack = loaderAttack?.Copy(),
            hasInfector = hasInfector,
            infector = infector?.Copy()
        };

        private static GeneratedOperationValue[] CopyValues(GeneratedOperationValue[] values)
        {
            if (values == null) return Array.Empty<GeneratedOperationValue>();
            var copy = new GeneratedOperationValue[values.Length];
            for (int i = 0; i < values.Length; i++) copy[i] = values[i]?.Copy();
            return copy;
        }

        private static GeneratedKeywordSelection[] CopyKeywords(GeneratedKeywordSelection[] values)
        {
            if (values == null) return Array.Empty<GeneratedKeywordSelection>();
            var copy = new GeneratedKeywordSelection[values.Length];
            for (int i = 0; i < values.Length; i++) copy[i] = values[i]?.Copy();
            return copy;
        }

        public static GeneratedOperationValue[] Capture(OperationStage[] stages)
        {
            var result = new GeneratedOperationValue[stages.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                OperationStage stage = stages[i];
                result[i] = new GeneratedOperationValue
                {
                    kind = stage.Operation.ToString(), desiredLoad = stage.DesiredLoad,
                    totalWork = stage.TotalWork,
                    completionEffect = stage.EffectOnCompletion.ToString(),
                    behaviorIds = Array.ConvertAll(stage.Behaviors, behavior => behavior.Id)
                };
            }
            return result;
        }
    }

    [Serializable]
    public sealed class ForcedGenerationOptions
    {
        public string objective;
        public string infectionType;
        public bool hasLoadParameter;
        public float loadParameter;
        public bool hasCadenceParameter;
        public float cadenceParameter;

        public ForcedGenerationOptions Copy() => new ForcedGenerationOptions
        {
            objective = objective, infectionType = infectionType,
            hasLoadParameter = hasLoadParameter, loadParameter = loadParameter,
            hasCadenceParameter = hasCadenceParameter, cadenceParameter = cadenceParameter
        };
    }

    [Serializable]
    public sealed class GeneratedKeywordSelection
    {
        public int operationIndex;
        public string behaviorId;
        public string behaviorKind;

        public GeneratedKeywordSelection Copy() => new GeneratedKeywordSelection
        {
            operationIndex = operationIndex, behaviorId = behaviorId, behaviorKind = behaviorKind
        };
    }

    [Serializable]
    public sealed class GeneratedOperationValue
    {
        public string kind;
        public float desiredLoad;
        public float totalWork;
        public string completionEffect;
        public string[] behaviorIds;

        public GeneratedOperationValue Copy() => new GeneratedOperationValue
        {
            kind = kind, desiredLoad = desiredLoad, totalWork = totalWork,
            completionEffect = completionEffect,
            behaviorIds = behaviorIds != null ? (string[])behaviorIds.Clone() : Array.Empty<string>()
        };
    }
}
