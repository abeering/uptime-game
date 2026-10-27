using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public enum BehaviorScope { Connection, Operation }
    public enum ModifierChangeKind { Added, Removed }
    public enum ConnectionBehaviorKind { TimedAdjustment, Overload, Spiking }

    // One serialized definition shape for timed adjustments and two LOAD keywords.
    // The original constructor retains the Slice 3A timed adjustment behavior.
    [Serializable]
    public sealed class TimedModifierBehaviorDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private ConnectionBehaviorKind kind;
        [SerializeField, Min(0f)] private float startAfterSeconds;
        [SerializeField, Min(0f)] private float stopAfterSeconds;
        [SerializeField, Min(0f)] private float normalIntervalSeconds;
        [SerializeField, Min(0f)] private float elevatedIntervalSeconds;
        [SerializeField, Min(0f)] private float spikeInitialDelaySeconds;
        [SerializeField] private float workDelta;
        [SerializeField] private float loadDelta;
        // The presence flag keeps definitions serialized before rate factors at neutral 1x.
        // A serialized zero is meaningful only when explicitly enabled.
        [SerializeField] private bool hasWorkRateMultiplier;
        [SerializeField] private float workRateMultiplier = 1f;

        public string Id => id;
        public ConnectionBehaviorKind Kind => kind;
        public float StartAfterSeconds => startAfterSeconds;
        public float StopAfterSeconds => stopAfterSeconds;
        public float NormalIntervalSeconds => normalIntervalSeconds;
        public float ElevatedIntervalSeconds => elevatedIntervalSeconds;
        public float SpikeInitialDelaySeconds => spikeInitialDelaySeconds > 0f ?
            spikeInitialDelaySeconds : normalIntervalSeconds;
        public float SpikeInitialDelayOverrideSeconds => spikeInitialDelaySeconds;
        public float WorkDelta => workDelta;
        public float LoadDelta => loadDelta;
        public float WorkRateMultiplier => hasWorkRateMultiplier ? workRateMultiplier : 1f;
        public TimedModifierBehaviorDefinition(string id, float startAfterSeconds,
            float stopAfterSeconds, float workDelta, float loadDelta)
        {
            this.id = id;
            this.startAfterSeconds = startAfterSeconds;
            this.stopAfterSeconds = stopAfterSeconds;
            this.workDelta = workDelta;
            this.loadDelta = loadDelta;
        }
        public static TimedModifierBehaviorDefinition Rate(string id, float startAfterSeconds,
            float stopAfterSeconds, float multiplier) =>
            new TimedModifierBehaviorDefinition(id, startAfterSeconds, stopAfterSeconds, 0f, 0f)
            { hasWorkRateMultiplier = true, workRateMultiplier = multiplier };
        public static TimedModifierBehaviorDefinition Overload(string id, float delaySeconds,
            float loadDelta) => new TimedModifierBehaviorDefinition(id, delaySeconds, 0f, 0f,
                loadDelta) { kind = ConnectionBehaviorKind.Overload };
        public static TimedModifierBehaviorDefinition Spiking(string id, float normalSeconds,
            float elevatedSeconds, float loadDelta, float firstDelaySeconds = 0f) =>
            new TimedModifierBehaviorDefinition(
                id, 0f, 0f, 0f, loadDelta)
                { kind = ConnectionBehaviorKind.Spiking, normalIntervalSeconds = normalSeconds,
                    elevatedIntervalSeconds = elevatedSeconds,
                    spikeInitialDelaySeconds = firstDelaySeconds };
        public TimedModifierBehaviorDefinition Copy() => new TimedModifierBehaviorDefinition(
            id, startAfterSeconds, stopAfterSeconds, workDelta, loadDelta)
            { kind = kind, normalIntervalSeconds = normalIntervalSeconds,
                elevatedIntervalSeconds = elevatedIntervalSeconds,
                spikeInitialDelaySeconds = spikeInitialDelaySeconds,
                hasWorkRateMultiplier = hasWorkRateMultiplier,
                workRateMultiplier = workRateMultiplier };
    }

    public sealed class ActiveRequirementModifier
    {
        internal ActiveRequirementModifier(string id, string sourceId, BehaviorScope scope,
            int operationIndex, float workDelta, float loadDelta, float workRateMultiplier)
        { Id = id; SourceId = sourceId; Scope = scope; OperationIndex = operationIndex;
            WorkDelta = workDelta; LoadDelta = loadDelta;
            WorkRateMultiplier = workRateMultiplier; }
        public string Id { get; }
        public string SourceId { get; }
        public BehaviorScope Scope { get; }
        public int OperationIndex { get; }
        public float WorkDelta { get; }
        public float LoadDelta { get; }
        public float WorkRateMultiplier { get; }
        public bool AppliesTo(int index) => Scope == BehaviorScope.Connection || OperationIndex == index;
    }

    public sealed class ModifierHistoryEntry
    {
        internal ModifierHistoryEntry(float time, ModifierChangeKind action,
            ActiveRequirementModifier modifier)
        { TimeSeconds = time; Action = action; ModifierId = modifier.Id;
            SourceId = modifier.SourceId; Scope = modifier.Scope;
            OperationIndex = modifier.OperationIndex; WorkDelta = modifier.WorkDelta;
            LoadDelta = modifier.LoadDelta; WorkRateMultiplier = modifier.WorkRateMultiplier; }
        public float TimeSeconds { get; }
        public ModifierChangeKind Action { get; }
        public string ModifierId { get; }
        public string SourceId { get; }
        public BehaviorScope Scope { get; }
        public int OperationIndex { get; }
        public float WorkDelta { get; }
        public float LoadDelta { get; }
        public float WorkRateMultiplier { get; }
    }

    // One instance per connection. Both scopes and all three behavior kinds use the
    // same executor; only their lifetime and modifier association differ.
    public sealed class ConnectionBehaviorRuntime
    {
        private sealed class TimedInstance
        {
            // Float fixed deltas such as 0.02f accumulate slightly below authored
            // boundaries in double precision; keep a sub-millisecond tolerance.
            private const double BoundaryTolerance = 0.00001;
            private readonly TimedModifierBehaviorDefinition definition;
            private readonly BehaviorScope scope;
            private readonly int operationIndex;
            private double elapsed;
            private double nextSpikeTransition;
            private bool elevated;
            private string modifierId;
            private bool finished;

            public TimedInstance(TimedModifierBehaviorDefinition definition, BehaviorScope scope,
                int operationIndex)
            { this.definition = definition; this.scope = scope; this.operationIndex = operationIndex;
                nextSpikeTransition = definition.SpikeInitialDelaySeconds; }

            public void Advance(ConnectionBehaviorRuntime owner, float delta, float time)
            {
                if (finished) return;
                if (definition.Kind == ConnectionBehaviorKind.Spiking)
                {
                    // Reconcile every boundary crossed by earlier steps at this step's
                    // start. Only the resulting phase participates in this allocation.
                    while (elapsed + BoundaryTolerance >= nextSpikeTransition)
                    {
                        if (elevated)
                        {
                            owner.RemoveModifier(modifierId, time);
                            modifierId = null;
                            elevated = false;
                            nextSpikeTransition += definition.NormalIntervalSeconds;
                        }
                        else
                        {
                            modifierId = owner.AddModifier(definition, scope, operationIndex, time);
                            elevated = true;
                            nextSpikeTransition += definition.ElevatedIntervalSeconds;
                        }
                    }
                }
                else
                {
                    if (modifierId == null &&
                        elapsed + BoundaryTolerance >= definition.StartAfterSeconds)
                        modifierId = owner.AddModifier(definition, scope, operationIndex, time);
                    if (definition.Kind == ConnectionBehaviorKind.TimedAdjustment &&
                        modifierId != null && definition.StopAfterSeconds > 0f &&
                        elapsed + BoundaryTolerance >= definition.StopAfterSeconds)
                    {
                        owner.RemoveModifier(modifierId, time);
                        modifierId = null;
                        finished = true;
                    }
                }
                elapsed += delta;
            }

            public void Stop(ConnectionBehaviorRuntime owner, float time)
            {
                if (modifierId != null) owner.RemoveModifier(modifierId, time);
                modifierId = null;
                finished = true;
            }
        }

        private readonly LogicalConnectionDefinition plan;
        private readonly List<TimedInstance> connectionBehaviors = new List<TimedInstance>();
        private readonly List<TimedInstance> operationBehaviors = new List<TimedInstance>();
        private readonly List<ActiveRequirementModifier> active = new List<ActiveRequirementModifier>();
        private readonly List<ModifierHistoryEntry> history = new List<ModifierHistoryEntry>();
        // Sampled once by the simulation before allocation. Separate keys allow later
        // time-varying LOAD sources to compose without creating modifier log events.
        private readonly SortedDictionary<string, float> dynamicLoadDeltas =
            new SortedDictionary<string, float>(StringComparer.Ordinal);
        private int nextModifierId;
        private bool connectionStarted;
        private bool operationStarted;

        internal ConnectionBehaviorRuntime(LogicalConnectionDefinition plan) { this.plan = plan; }
        public IReadOnlyList<ActiveRequirementModifier> ActiveModifiers => active;
        public IReadOnlyList<ModifierHistoryEntry> History => history;
        public event Action<ModifierHistoryEntry> ModifierChanged;

        internal void StartConnection()
        {
            if (connectionStarted) return;
            connectionStarted = true;
            foreach (TimedModifierBehaviorDefinition definition in plan.ConnectionBehaviors)
                connectionBehaviors.Add(new TimedInstance(definition, BehaviorScope.Connection, -1));
        }

        internal void StartOperation(int index)
        {
            if (!connectionStarted) StartConnection();
            if (operationStarted) throw new InvalidOperationException("Operation behavior already active.");
            CurrentOperationIndex = index;
            operationStarted = true;
            foreach (TimedModifierBehaviorDefinition definition in plan.BehaviorsAt(index))
                operationBehaviors.Add(new TimedInstance(definition, BehaviorScope.Operation, index));
        }

        internal void Advance(float delta, float time)
        {
            if (!operationStarted) return;
            foreach (TimedInstance behavior in connectionBehaviors) behavior.Advance(this, delta, time);
            foreach (TimedInstance behavior in operationBehaviors) behavior.Advance(this, delta, time);
        }

        internal void EndOperation(float time)
        {
            foreach (TimedInstance behavior in operationBehaviors) behavior.Stop(this, time);
            operationBehaviors.Clear();
            dynamicLoadDeltas.Clear();
            operationStarted = false;
        }

        internal void SetDynamicLoadDelta(string source, float delta)
        {
            if (string.IsNullOrEmpty(source) || float.IsNaN(delta) || float.IsInfinity(delta))
                throw new ArgumentException("Dynamic LOAD source and delta must be valid.");
            dynamicLoadDeltas[source] = delta;
        }

        internal void EndConnection(float time)
        {
            EndOperation(time);
            foreach (TimedInstance behavior in connectionBehaviors) behavior.Stop(this, time);
            connectionBehaviors.Clear();
            connectionStarted = false;
        }

        internal float EffectiveWork(int index) => Effective(index, true);
        internal float EffectiveLoad(int index) => Effective(index, false);
        internal float EffectiveWorkRateMultiplier(int index)
        {
            double value = 1d;
            bool zero = false;
            foreach (ActiveRequirementModifier modifier in active)
                if (modifier.AppliesTo(index))
                {
                    float factor = modifier.WorkRateMultiplier;
                    if (float.IsNaN(factor) || float.IsInfinity(factor) || factor < 0f)
                        throw new InvalidOperationException(
                            "WORK rate factors must be finite and nonnegative.");
                    if (factor == 0f) zero = true;
                    else if (!zero) value *= factor;
                }
            if (zero) return 0f;
            if (double.IsInfinity(value) || value > float.MaxValue)
                throw new InvalidOperationException(
                    "Effective WORK rate multiplier must be finite and nonnegative.");
            return (float)value;
        }
        private float Effective(int index, bool work)
        {
            float value = work ? plan.WorkAt(index) : plan.LoadAt(index);
            foreach (ActiveRequirementModifier modifier in active)
                if (modifier.AppliesTo(index))
                    value += work ? modifier.WorkDelta : modifier.LoadDelta;
            if (float.IsNaN(value) || float.IsInfinity(value) ||
                (work ? value <= 0f : value < 0f))
                throw new InvalidOperationException("Effective WORK must be finite and positive; " +
                    "effective LOAD must be finite and nonnegative.");
            if (!work)
            {
                double requested = value;
                foreach (float delta in dynamicLoadDeltas.Values) requested += delta;
                if (double.IsInfinity(requested) || requested > float.MaxValue)
                    throw new InvalidOperationException("Effective LOAD must be finite.");
                return (float)Math.Max(0d, requested);
            }
            return value;
        }

        private string AddModifier(TimedModifierBehaviorDefinition definition,
            BehaviorScope scope, int index, float time)
        {
            var modifier = new ActiveRequirementModifier("m" + (++nextModifierId), definition.Id,
                scope, index, definition.WorkDelta, definition.LoadDelta,
                definition.WorkRateMultiplier);
            active.Add(modifier);
            // Validate the combined effective requirements before allocation.
            try
            {
                EffectiveWork(CurrentOperationIndex);
                EffectiveLoad(CurrentOperationIndex);
                EffectiveWorkRateMultiplier(CurrentOperationIndex);
            }
            catch
            {
                active.Remove(modifier);
                throw;
            }
            Record(time, ModifierChangeKind.Added, modifier);
            return modifier.Id;
        }

        internal int CurrentOperationIndex { get; set; }

        private void RemoveModifier(string id, float time)
        {
            int index = active.FindIndex(modifier => modifier.Id == id);
            if (index < 0) throw new InvalidOperationException("Unknown active modifier: " + id);
            ActiveRequirementModifier removed = active[index];
            active.RemoveAt(index);
            try
            {
                EffectiveWork(CurrentOperationIndex);
                EffectiveLoad(CurrentOperationIndex);
                EffectiveWorkRateMultiplier(CurrentOperationIndex);
            }
            catch
            {
                active.Insert(index, removed);
                throw;
            }
            Record(time, ModifierChangeKind.Removed, removed);
        }

        private void Record(float time, ModifierChangeKind action, ActiveRequirementModifier modifier)
        {
            var entry = new ModifierHistoryEntry(time, action, modifier);
            history.Add(entry);
            ModifierChanged?.Invoke(entry);
        }
    }

    // Hash-interpolated LOAD variation. No traffic RNG draws or frame state are consumed.
    internal static class NaturalLoadVariation
    {
        internal static float Delta(int seed, string id, int operationIndex,
            float elapsedSeconds, float baseLoad, float amplitude,
            float minPeriodSeconds, float maxPeriodSeconds)
        {
            if (baseLoad <= 0f || amplitude <= 0f) return 0f;
            float period = Mathf.Lerp(minPeriodSeconds, maxPeriodSeconds,
                Unit(Hash(seed, id, operationIndex, -1)));
            float position = Mathf.Max(0f, elapsedSeconds) / period;
            int anchor = Mathf.FloorToInt(position);
            float phase = position - anchor;
            phase = phase * phase * (3f - 2f * phase);
            float from = anchor == 0 ? 0f :
                2f * Unit(Hash(seed, id, operationIndex, anchor)) - 1f;
            float to = 2f * Unit(Hash(seed, id, operationIndex, anchor + 1)) - 1f;
            return baseLoad * amplitude * Mathf.Lerp(from, to, phase);
        }

        private static float Unit(uint bits) => (bits >> 8) / 16777215f;

        private static uint Hash(int seed, string id, int operationIndex, int anchor)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)seed) * 16777619u;
                for (int i = 0; i < id.Length; i++)
                    hash = (hash ^ char.ToLowerInvariant(id[i])) * 16777619u;
                hash = (hash ^ (uint)operationIndex) * 16777619u;
                hash = (hash ^ (uint)anchor) * 16777619u;
                // Avalanche neighboring anchors before taking high bits as a fraction.
                hash ^= hash >> 16;
                hash *= 0x7feb352du;
                hash ^= hash >> 15;
                hash *= 0x846ca68bu;
                return hash ^ (hash >> 16);
            }
        }
    }
}
