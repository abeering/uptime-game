using UnityEngine;

namespace Uptime.Connections
{
    public enum ScanStartResult { Started, UnknownNode, AlreadyScanning, Busy }
    public enum NodeScanResult { Clean, Infected }

    // Host-owned timing and result. A history card can retain this session after the slot is free.
    public sealed class NodeScanSession
    {
        internal NodeScanSession(int id, NodeResources target, float startTime,
            float duration, int actionSequence)
        {
            Id = id;
            Target = target;
            StartedAtSeconds = startTime;
            ScheduledCompletionAtSeconds = startTime + duration;
            ActionSequence = actionSequence;
        }

        public int Id { get; }
        public string NodeId => Target.View.NodeId;
        public float StartedAtSeconds { get; }
        public float ScheduledCompletionAtSeconds { get; }
        public float CompletedAtSeconds { get; private set; } = -1f;
        public bool IsComplete { get; private set; }
        public bool WasReset { get; private set; }
        public NodeScanResult Result { get; private set; }
        public int ActionSequence { get; }
        internal NodeResources Target { get; }
        private float resetAtSeconds = -1f;

        public float ProgressAt(float simulationTimeSeconds)
        {
            if (IsComplete) return 1f;
            float time = WasReset ? resetAtSeconds : simulationTimeSeconds;
            return Mathf.Clamp01((time - StartedAtSeconds) /
                (ScheduledCompletionAtSeconds - StartedAtSeconds));
        }

        internal void Complete(NodeScanResult result, float timeSeconds)
        {
            if (IsComplete || WasReset) return;
            Result = result;
            CompletedAtSeconds = timeSeconds;
            IsComplete = true;
        }

        internal void Reset(float timeSeconds)
        {
            if (IsComplete || WasReset) return;
            resetAtSeconds = timeSeconds;
            WasReset = true;
        }
    }
}
