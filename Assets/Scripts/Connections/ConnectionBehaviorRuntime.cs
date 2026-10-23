using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public enum BehaviorScope { Connection, Operation }
    public enum ModifierChangeKind { Added, Removed }

    // A small deterministic demonstration behavior. Zero stop time keeps its modifier
    // until its scope ends; this is not a player-facing keyword or scripting language.
    [Serializable]
    public sealed class TimedModifierBehaviorDefinition
    {
        [SerializeField] private string id;
        [SerializeField, Min(0f)] private float startAfterSeconds;
        [SerializeField, Min(0f)] private float stopAfterSeconds;
        [SerializeField] private float workDelta;
        [SerializeField] private float loadDelta;

        public string Id => id;
        public float StartAfterSeconds => startAfterSeconds;
        public float StopAfterSeconds => stopAfterSeconds;
        public float WorkDelta => workDelta;
        public float LoadDelta => loadDelta;
        public TimedModifierBehaviorDefinition(string id, float startAfterSeconds,
            float stopAfterSeconds, float workDelta, float loadDelta)
        {
            this.id = id;
            this.startAfterSeconds = startAfterSeconds;
            this.stopAfterSeconds = stopAfterSeconds;
            this.workDelta = workDelta;
            this.loadDelta = loadDelta;
        }
        public TimedModifierBehaviorDefinition Copy() => new TimedModifierBehaviorDefinition(
            id, startAfterSeconds, stopAfterSeconds, workDelta, loadDelta);
    }

    public sealed class ActiveRequirementModifier
    {
        internal ActiveRequirementModifier(string id, string sourceId, BehaviorScope scope,
            int operationIndex, float workDelta, float loadDelta)
        { Id = id; SourceId = sourceId; Scope = scope; OperationIndex = operationIndex;
            WorkDelta = workDelta; LoadDelta = loadDelta; }
        public string Id { get; }
        public string SourceId { get; }
        public BehaviorScope Scope { get; }
        public int OperationIndex { get; }
        public float WorkDelta { get; }
        public float LoadDelta { get; }
        public bool AppliesTo(int index) => Scope == BehaviorScope.Connection || OperationIndex == index;
    }

    public sealed class ModifierHistoryEntry
    {
        internal ModifierHistoryEntry(float time, ModifierChangeKind action,
            ActiveRequirementModifier modifier)
        { TimeSeconds = time; Action = action; ModifierId = modifier.Id;
            SourceId = modifier.SourceId; Scope = modifier.Scope;
            OperationIndex = modifier.OperationIndex; WorkDelta = modifier.WorkDelta;
            LoadDelta = modifier.LoadDelta; }
        public float TimeSeconds { get; }
        public ModifierChangeKind Action { get; }
        public string ModifierId { get; }
        public string SourceId { get; }
        public BehaviorScope Scope { get; }
        public int OperationIndex { get; }
        public float WorkDelta { get; }
        public float LoadDelta { get; }
    }

    // One instance per connection. Both scopes use the same timed behavior executor;
    // only their lifetime and modifier association differ.
    public sealed class ConnectionBehaviorRuntime
    {
        private sealed class TimedInstance
        {
            private readonly TimedModifierBehaviorDefinition definition;
            private readonly BehaviorScope scope;
            private readonly int operationIndex;
            private float elapsed;
            private string modifierId;
            private bool finished;

            public TimedInstance(TimedModifierBehaviorDefinition definition, BehaviorScope scope,
                int operationIndex)
            { this.definition = definition; this.scope = scope; this.operationIndex = operationIndex; }

            public void Advance(ConnectionBehaviorRuntime owner, float delta, float time)
            {
                if (finished) return;
                if (modifierId == null && elapsed >= definition.StartAfterSeconds)
                    modifierId = owner.AddModifier(definition, scope, operationIndex, time);
                if (modifierId != null && definition.StopAfterSeconds > 0f &&
                    elapsed >= definition.StopAfterSeconds)
                {
                    owner.RemoveModifier(modifierId, time);
                    modifierId = null;
                    finished = true;
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
            operationStarted = false;
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
            return value;
        }

        private string AddModifier(TimedModifierBehaviorDefinition definition,
            BehaviorScope scope, int index, float time)
        {
            var modifier = new ActiveRequirementModifier("m" + (++nextModifierId), definition.Id,
                scope, index, definition.WorkDelta, definition.LoadDelta);
            active.Add(modifier);
            // Validate the combined effective requirements before allocation.
            try
            {
                EffectiveWork(CurrentOperationIndex);
                EffectiveLoad(CurrentOperationIndex);
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
}
