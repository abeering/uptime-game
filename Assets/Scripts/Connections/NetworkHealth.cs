using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class NetworkHealthTuning
    {
        // Provisional V0 values; tune against actual play sessions.
        [Min(0f)] public float deadlineTolerance = 1.5f;
        [Min(0f)] public float slowdownDrainPerSecond = 0.25f;
        [Min(0f)] public float overdueDrainPerSecond = 0.50f;
        [Min(0f)] public float overdueEscalationPerSecond = 0.02f;
        [Min(0f)] public float recoveryPerSecond = 0.35f;
        [Min(0f)] public float incorrectKillPenalty = 10f;
        [Range(0f, 1f)] public float substantialSlowdownThreshold = 0.98f;
    }

    public sealed class NetworkHealthImpact
    {
        internal NetworkHealthImpact(string connectionId, float rate, bool overdue, bool slowed)
        { ConnectionId = connectionId; Rate = rate; Overdue = overdue; Slowed = slowed; }
        public string ConnectionId { get; }
        public float Rate { get; }
        public bool Overdue { get; }
        public bool Slowed { get; }
    }

    public sealed class NetworkHealthEvent
    {
        internal NetworkHealthEvent(string connectionId, float timeSeconds, float amount)
        { ConnectionId = connectionId; TimeSeconds = timeSeconds; Amount = amount; }
        public string ConnectionId { get; }
        public float TimeSeconds { get; }
        public float Amount { get; }
    }

    public sealed class NetworkHealthSnapshot
    {
        internal NetworkHealthSnapshot(float value, float degradationRate, float recoveryRate,
            float netRate, int slowedCount, int overdueCount, bool failed,
            NetworkHealthImpact[] impacts, NetworkHealthEvent[] events)
        { Value = value; DegradationRate = degradationRate; RecoveryRate = recoveryRate;
            NetRate = netRate; SlowedCount = slowedCount; OverdueCount = overdueCount;
            Failed = failed; Impacts = Array.AsReadOnly(impacts);
            RecentEvents = Array.AsReadOnly(events); }
        public float Value { get; }
        public float DegradationRate { get; }
        public float RecoveryRate { get; }
        public float NetRate { get; }
        public int SlowedCount { get; }
        public int OverdueCount { get; }
        public bool Failed { get; }
        // Connection IDs are internal causal evidence. The panel deliberately presents aggregates.
        public IReadOnlyList<NetworkHealthImpact> Impacts { get; }
        public IReadOnlyList<NetworkHealthEvent> RecentEvents { get; }
    }

    // Owned by SimulationHost and updated only from its simulation step and resolution seam.
    public sealed class NetworkHealth
    {
        private readonly struct ServiceCondition
        {
            public ServiceCondition(bool slowed, bool overdue)
            { Slowed = slowed; Overdue = overdue; }
            public bool Slowed { get; }
            public bool Overdue { get; }
        }

        private readonly NetworkHealthTuning tuning;
        private readonly List<NetworkHealthEvent> events = new List<NetworkHealthEvent>();
        private readonly Dictionary<string, ServiceCondition> observed =
            new Dictionary<string, ServiceCondition>(StringComparer.OrdinalIgnoreCase);
        private NetworkHealthSnapshot snapshot;
        private float value = 100f;
        private bool failed;

        public NetworkHealth(NetworkHealthTuning tuning)
        { this.tuning = tuning ?? throw new ArgumentNullException(nameof(tuning)); Reset(); }
        public NetworkHealthSnapshot Snapshot => snapshot;

        public void Reset()
        {
            value = 100f;
            failed = false;
            events.Clear();
            observed.Clear();
            snapshot = new NetworkHealthSnapshot(value, 0f, 0f, 0f, 0, 0, false,
                Array.Empty<NetworkHealthImpact>(), Array.Empty<NetworkHealthEvent>());
        }

        public void Activate(LogicalConnectionState connection, float timeSeconds)
        {
            float nominalSeconds = 0f;
            for (int i = 0; i < connection.OperationCount; i++)
                nominalSeconds += connection.Definition.WorkAt(i) / connection.Definition.LoadAt(i);
            connection.HealthNominalDurationSeconds = nominalSeconds;
            connection.HealthDeadlineSeconds = timeSeconds + nominalSeconds *
                Mathf.Max(0f, tuning.deadlineTolerance);
        }

        // Called after grants are computed and before completion clears the final operation grant.
        public void Step(IReadOnlyList<LogicalConnectionState> connections,
            float startSeconds, float durationSeconds)
        {
            var impacts = new List<NetworkHealthImpact>();
            float degradation = 0f;
            int slowed = 0, overdue = 0;
            observed.Clear();
            foreach (LogicalConnectionState connection in connections)
            {
                if (!connection.IsDemanding) continue;
                float activeDuration = connection.ActualWorkRate > 0f ?
                    Mathf.Min(durationSeconds, connection.RemainingWork / connection.ActualWorkRate) :
                    durationSeconds;
                if (activeDuration <= 0f) continue;
                float satisfaction = Mathf.Clamp01(connection.Satisfaction);
                bool isSlowed = satisfaction < tuning.substantialSlowdownThreshold;
                bool isOverdue = startSeconds + activeDuration > connection.HealthDeadlineSeconds;
                if (isSlowed) { slowed++; connection.HealthWasSubstantiallySlowed = true; }
                if (isOverdue) { overdue++; connection.HealthBecameLate = true; }
                observed.Add(connection.ConnectionId, new ServiceCondition(isSlowed, isOverdue));
                if (connection.IsHostile) continue;

                float deadline = connection.HealthDeadlineSeconds;
                float ordinaryDuration = Mathf.Clamp(deadline - startSeconds, 0f, activeDuration);
                float overdueDuration = activeDuration - ordinaryDuration;
                float ordinaryDamage = isSlowed ?
                    Mathf.Max(0f, tuning.slowdownDrainPerSecond) *
                    (1f - satisfaction) * ordinaryDuration : 0f;
                float overdueStart = Mathf.Max(startSeconds, deadline) - deadline;
                float overdueDamage = overdueDuration *
                    (Mathf.Max(0f, tuning.overdueDrainPerSecond) +
                     Mathf.Max(0f, tuning.overdueEscalationPerSecond) *
                     (overdueStart + overdueDuration * 0.5f));
                float rate = (ordinaryDamage + overdueDamage) / durationSeconds;
                if (rate > 0f)
                {
                    degradation += rate;
                    impacts.Add(new NetworkHealthImpact(connection.ConnectionId, rate,
                        isOverdue, isSlowed));
                }
            }
            float recovery = degradation == 0f && value < 100f ?
                Mathf.Max(0f, tuning.recoveryPerSecond) : 0f;
            float oldValue = value;
            value = Mathf.Clamp(value + (recovery - degradation) * durationSeconds, 0f, 100f);
            if (value <= 0f) failed = true;
            snapshot = new NetworkHealthSnapshot(value, degradation, recovery,
                (value - oldValue) / durationSeconds, slowed, overdue, failed,
                impacts.ToArray(), events.ToArray());
        }

        public NetworkHealthEvent Resolve(LogicalConnectionState connection, ConnectionResolution reason,
            float timeSeconds)
        {
            NetworkHealthEvent penalty = null;
            observed.Remove(connection.ConnectionId);
            var remaining = new List<NetworkHealthImpact>();
            foreach (NetworkHealthImpact impact in snapshot.Impacts)
                if (impact.ConnectionId != connection.ConnectionId) remaining.Add(impact);
            if (reason == ConnectionResolution.Killed && !connection.IsHostile)
            {
                float amount = Mathf.Max(0f, tuning.incorrectKillPenalty);
                value = Mathf.Max(0f, value - amount);
                if (value <= 0f) failed = true;
                penalty = new NetworkHealthEvent(connection.ConnectionId, timeSeconds, -amount);
                events.Insert(0, penalty);
                if (events.Count > 5) events.RemoveAt(events.Count - 1);
            }
            float degradation = 0f;
            int overdue = 0, slowed = 0;
            foreach (NetworkHealthImpact impact in remaining)
                degradation += impact.Rate;
            foreach (ServiceCondition condition in observed.Values)
            {
                if (condition.Overdue) overdue++;
                if (condition.Slowed) slowed++;
            }
            float recovery = degradation == 0f && value < 100f ?
                Mathf.Max(0f, tuning.recoveryPerSecond) : 0f;
            float net = recovery - degradation;
            if ((value <= 0f && net < 0f) || (value >= 100f && net > 0f)) net = 0f;
            snapshot = new NetworkHealthSnapshot(value, degradation, recovery,
                net, slowed, overdue, failed,
                remaining.ToArray(), events.ToArray());
            return penalty;
        }
    }
}
