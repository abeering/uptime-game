using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Immutable receipt, independent of recording and presentation aliases.
    public sealed class BusinessFailure
    {
        internal BusinessFailure(LogicalConnectionState state, float time, int total)
        { ConnectionId = state.ConnectionId; Resolution = state.Resolution.Value;
            Cause = state.TerminationCause; TimeSeconds = time; CumulativeTotal = total;
            OperationIndex = state.CurrentOperationIndex; CompletedWork = state.CompletedWork;
            RequiredWork = state.TerminationRequiredWork; }
        public string ConnectionId { get; }
        public ConnectionResolution Resolution { get; }
        public ConnectionTerminationCause Cause { get; }
        public float TimeSeconds { get; }
        public int CumulativeTotal { get; }
        public int Units => 1;
        public int OperationIndex { get; }
        public float CompletedWork { get; }
        public float RequiredWork { get; }
    }

    public sealed partial class SimulationHost
    {
        [Header("Connection service (canonical scenes)")]
        [SerializeField] private bool serviceTimeoutsEnabled;
        [SerializeField, Min(.01f)] private float serviceTimeoutTolerance = 1.5f;
        private readonly List<BusinessFailure> businessFailures = new List<BusinessFailure>();
        public bool ServiceTimeoutsEnabled => serviceTimeoutsEnabled;
        public float ServiceTimeoutTolerance => serviceTimeoutTolerance;
        public int LegitimateFailureCount => businessFailures.Count;
        public IReadOnlyList<BusinessFailure> BusinessFailures => businessFailures.AsReadOnly();

        private void EstablishService(LogicalConnectionState state)
        {
            state.ServiceNominalDurationSeconds = NominalServiceDuration(state.Definition);
            state.ServiceBudgetSeconds = state.ServiceNominalDurationSeconds * serviceTimeoutTolerance;
            state.ServiceDeadlineSeconds = simulationTimeSeconds + state.ServiceBudgetSeconds;
            state.HasServiceTarget = serviceTimeoutsEnabled;
        }

        public static float NominalServiceDuration(LogicalConnectionDefinition definition)
        {
            float duration = 0f;
            for (int i = 0; i < definition.OperationCount; i++)
                duration += definition.LoadAt(i) > 0f ? definition.WorkAt(i) / definition.LoadAt(i) :
                    definition.WorkAt(i) > 0f ? float.PositiveInfinity : 0f;
            return duration;
        }

        private void ResolveDueTimeouts()
        {
            if (!serviceTimeoutsEnabled) return;
            var due = new List<LogicalConnectionState>();
            foreach (var state in runtimeConnections)
                if (state.IsActive && !state.IsHostile && state.HasServiceTarget &&
                    simulationTimeSeconds >= state.ServiceDeadlineSeconds) due.Add(state);
            due.Sort((a, b) => string.CompareOrdinal(a.ConnectionId, b.ConnectionId));
            foreach (var state in due)
            {
                state.TerminationPhase = state.Lifecycle;
                state.TerminationCause = ConnectionTerminationCause.ServiceTimeout;
                state.TerminationRequiredWork = state.CurrentTotalWork;
                state.Lifecycle = Lifecycle.Complete;
                state.AllocatedLoad = 0f;
                state.Behaviors.EndConnection(simulationTimeSeconds);
                state.NaturalLoadDelta = 0f;
                Resolve(state, ConnectionResolution.TimedOut);
                pathPresenter?.Retire(state);
            }
            if (due.Count == 0) return;
            if (runMode == ConnectionRunMode.Generated)
            {
                var live = new List<LogicalConnectionState>(runtimeConnections);
                live.RemoveAll(state => state.IsComplete);
                runtimeConnections = live.ToArray();
            }
            RefreshPayloadAllocations();
        }

        private void AccountBusinessFailure(LogicalConnectionState state)
        {
            // Resolve has already claimed the canonical state exactly once, before callbacks.
            if (state.IsHostile || state.Resolution == ConnectionResolution.Completed) return;
            var receipt = new BusinessFailure(state, simulationTimeSeconds, businessFailures.Count + 1);
            businessFailures.Add(receipt);
            recorder?.RecordBusinessFailure(receipt);
        }

        private void RefreshServiceDanger()
        {
            foreach (var state in runtimeConnections) state.UpdateServiceDanger(simulationTimeSeconds);
        }
    }
}
