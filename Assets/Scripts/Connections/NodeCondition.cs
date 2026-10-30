using System;
using UnityEngine;

namespace Uptime.Connections
{
    public enum ConditionKind { PortScanned }

    [Serializable]
    public sealed class ConditionPayload
    {
        [SerializeField] private ConditionKind kind;
        [SerializeField] private float durationSeconds;

        public ConditionKind Kind => kind;
        public float DurationSeconds => durationSeconds;
        public bool IsValid => Enum.IsDefined(typeof(ConditionKind), kind) &&
            durationSeconds > 0f && !float.IsNaN(durationSeconds) &&
            !float.IsInfinity(durationSeconds);

        public ConditionPayload(ConditionKind kind, float durationSeconds)
        {
            this.kind = kind;
            this.durationSeconds = durationSeconds;
            if (!IsValid) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        }

        public ConditionPayload Copy() => new ConditionPayload(kind, durationSeconds);
    }

    // Owned by SimulationHost, independent of the connection that applied it.
    public sealed class NodeCondition
    {
        public NodeCondition(NodeResources target, ConditionPayload payload, float appliedAtSeconds,
            string sourceConnectionId, int sourceOperationIndex)
        {
            Target = target;
            Kind = payload.Kind;
            Refresh(payload, appliedAtSeconds, sourceConnectionId, sourceOperationIndex);
        }

        public NodeResources Target { get; }
        public ConditionKind Kind { get; }
        public float AppliedAtSeconds { get; private set; }
        public float ExpiresAtSeconds { get; private set; }
        public string SourceConnectionId { get; private set; }
        public int SourceOperationIndex { get; private set; }

        internal void Refresh(ConditionPayload payload, float now, string sourceConnectionId,
            int sourceOperationIndex)
        {
            AppliedAtSeconds = now;
            ExpiresAtSeconds = now + payload.DurationSeconds;
            SourceConnectionId = sourceConnectionId;
            SourceOperationIndex = sourceOperationIndex;
        }
    }
}
