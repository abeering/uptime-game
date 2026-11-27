using UnityEngine;

namespace Uptime.Connections.Display
{
    // Read-only presentation adapter. Never advances the host or changes connection facts.
    [ExecuteAlways]
    public sealed class ConnectionGameplayDisplay : MonoBehaviour
    {
        public SimulationHost host;
        public ConnectionHeartbeat heartbeat;
        public ConnectionDisplayStyle style;
        public ConnectionsDuelDirector duel;
        public float FrameDelta { get; private set; }
        public bool Frozen => host == null || !host.isActiveAndEnabled || host.RunEnded ||
            (duel != null && duel.PausesSimulation);
        private void OnEnable() { if (host != null) host.SimulationReset += Replay; }
        private void OnDisable() { if (host != null) host.SimulationReset -= Replay; FrameDelta = 0; }
        private void Update()
        {
            if (!Application.isPlaying) return;
            FrameDelta = Frozen ? 0 : Time.deltaTime;
            heartbeat.Advance(FrameDelta);
        }
        public void Replay() { if (heartbeat != null) heartbeat.Replay(); FrameDelta = 0; }

        public ConnectionDisplayState StateFor(LogicalConnectionState connection, float departureAge = 0)
            => Map(connection, style, departureAge);

        public static ConnectionDisplayState Map(LogicalConnectionState c, ConnectionDisplayStyle style, float age = 0)
        {
            float required = c.IsComplete ? c.TerminationRequiredWork : c.CurrentTotalWork;
            if (required <= 0) required = c.CurrentTotalWork;
            var state = new ConnectionDisplayState
            {
                identity = c.KnownIdentity, trafficClass = c.IsImportant ? DisplayTrafficClass.Important : DisplayTrafficClass.Ordinary,
                operation = c.CurrentOperation, requestedLoad = c.DesiredLoad,
                productiveSpeed = c.IsDemanding && c.DesiredLoad > .0001f ?
                    Mathf.Max(0, c.ActualWorkRate / c.DesiredLoad) : 0,
                workProgress = required > 0 ? Mathf.Clamp01(c.CompletedWork / required) : 0,
                resolution = Resolution(c), resolutionAge = age
            };
            if ((!c.IsActive && !c.IsRerouting) || !c.HasServiceTarget) return state;
            float normal = NormalSeconds(c);
            float remaining = c.ServiceRemainingSeconds;
            float finiteNormal = float.IsInfinity(normal) ? c.ServiceBudgetSeconds : normal;
            float band = Mathf.Max(style.riskSoftBandSeconds, finiteNormal * style.riskSoftBandFraction);
            // A soft spatial band, not a delayed/smoothed timeout or gameplay risk predicate.
            float currentPressure = float.IsInfinity(c.ProjectedServiceSeconds) ? 1 :
                Mathf.Clamp01((band - (remaining - c.ProjectedServiceSeconds)) / (2 * band));
            float opportunity = Mathf.Max(band, finiteNormal * style.rescueHeadroomFraction);
            float urgency = 1 - Mathf.Clamp01((remaining - finiteNormal) / opportunity);
            state.dangerSeverity = currentPressure * Mathf.Lerp(style.riskFloor, 1, urgency);
            state.exceedsNormalBudget = normal > remaining;
            if (state.exceedsNormalBudget)
                state.normalBudgetWarningMix = Mathf.Max(.000001f,
                    Mathf.SmoothStep(0, 1, (normal - remaining) / band));
            return state;
        }
        // Same nominal future-operation assumption as Fragility; current effective WORK
        // is compared with authored LOAD at 1x productivity, independent of LOAD noise.
        public static float NormalSeconds(LogicalConnectionState c)
        {
            float seconds = 0;
            for (int i = c.CurrentOperationIndex; i < c.OperationCount; i++)
            {
                float work = i == c.CurrentOperationIndex && !c.IsTransitioning ? c.RemainingWork : c.Definition.WorkAt(i);
                float load = c.Definition.LoadAt(i);
                seconds += load > 0 ? work / load : work > .0001f ? float.PositiveInfinity : 0;
            }
            return seconds;
        }
        private static DisplayResolution Resolution(LogicalConnectionState c) => c.Resolution switch
        {
            ConnectionResolution.Completed => DisplayResolution.Completed,
            ConnectionResolution.Killed => DisplayResolution.Killed,
            ConnectionResolution.TimedOut => DisplayResolution.TimedOut,
            ConnectionResolution.Canceled => DisplayResolution.Canceled,
            ConnectionResolution.Withdrawn => DisplayResolution.Withdrawn,
            _ => DisplayResolution.Active
        };
    }
}
