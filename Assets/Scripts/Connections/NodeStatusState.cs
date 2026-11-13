using UnityEngine;

namespace Uptime.Connections
{
    // Player-visible diagnostic knowledge, independent of the host's infection list.
    public sealed class NodeStatusState
    {
        private float cleanUntil = -1f;

        public bool Offline { get; private set; }
        public bool IntrusionPending { get; private set; }
        public float IntrusionRemainingSeconds { get; private set; }
        public bool KnownInfected { get; private set; }
        public bool AwarenessPreview { get; private set; }
        public bool PortScanned { get; private set; }
        public float PortScanRemainingFraction { get; private set; }
        public float PortScanRemainingSeconds { get; private set; }
        public bool ScanJammed { get; private set; }
        public float ScanJamRemainingFraction { get; private set; }
        public float ScanJamRemainingSeconds { get; private set; }
        public bool Backdoor { get; private set; }
        public float BackdoorRemainingSeconds { get; private set; }
        public bool Isolated { get; private set; }
        public float IsolationRemainingFraction { get; private set; }
        public float IsolationRemainingSeconds { get; private set; }
        public string InterceptPayloadId { get; private set; }
        public bool InterceptActive { get; private set; }
        public bool InterceptAllPayloads { get; private set; }
        public bool InterceptSucceeded { get; private set; }
        public float InterceptRemainingFraction { get; private set; }
        public float InterceptRemainingSeconds { get; private set; }
        public bool Overclocked { get; private set; }
        public float OverclockRemainingSeconds { get; private set; }
        public bool CleanVisible(float realTime) => realTime < cleanUntil;
        public bool HasAny(float realTime) => Offline || IntrusionPending || KnownInfected ||
            AwarenessPreview || PortScanned || ScanJammed || Backdoor || Isolated ||
            CleanVisible(realTime) || InterceptActive || Overclocked;

        public void SetOverclock(NodeResources node, NodeResources activeNode,
            float remainingSeconds)
        {
            Overclocked = node != null && node == activeNode;
            OverclockRemainingSeconds = Overclocked ? Mathf.Max(0f, remainingSeconds) : 0f;
        }

        public void SetOffline(bool offline) => Offline = offline;

        public void SetIntrusion(float remainingSeconds)
        {
            IntrusionPending = remainingSeconds >= 0f;
            IntrusionRemainingSeconds = IntrusionPending ? remainingSeconds : 0f;
        }

        // Presentation lab only: permit synthetic knowledge/acknowledgment overlap.
        // Gameplay continues to acquire these through ScanResult/CleanSucceeded.
        internal void SetPreviewDiagnosis(bool infected, float cleanExpiresAt)
        {
            KnownInfected = infected;
            cleanUntil = cleanExpiresAt;
        }

        public void ScanResult(NodeScanResult result, float realTime, float cleanSeconds)
        {
            if (result == NodeScanResult.Infected)
            {
                KnownInfected = true;
                cleanUntil = -1f;
            }
            else
            {
                KnownInfected = false;
                cleanUntil = realTime + Mathf.Max(0f, cleanSeconds);
            }
        }

        // Cleaning clears acquired knowledge even when a newer, unscanned infection survives.
        public void CleanSucceeded()
        {
            KnownInfected = false;
            cleanUntil = -1f;
        }

        public void SetAwarenessPreview(bool visible) => AwarenessPreview = visible;

        public void SetPortScanned(NodeCondition condition, float simulationTime)
        {
            PortScanned = condition != null;
            PortScanRemainingSeconds = condition == null ? 0f : Mathf.Max(0f,
                condition.ExpiresAtSeconds - simulationTime);
            PortScanRemainingFraction = condition == null ? 0f : Mathf.Clamp01(
                (condition.ExpiresAtSeconds - simulationTime) /
                (condition.ExpiresAtSeconds - condition.AppliedAtSeconds));
        }

        public void SetScanJammed(NodeCondition condition, float simulationTime)
        {
            ScanJammed = condition != null;
            ScanJamRemainingSeconds = condition == null ? 0f : Mathf.Max(0f,
                condition.ExpiresAtSeconds - simulationTime);
            ScanJamRemainingFraction = condition == null ? 0f : Mathf.Clamp01(
                (condition.ExpiresAtSeconds - simulationTime) /
                (condition.ExpiresAtSeconds - condition.AppliedAtSeconds));
        }

        public void SetBackdoor(NodeCondition condition, float simulationTime)
        {
            Backdoor = condition != null;
            BackdoorRemainingSeconds = condition == null ? 0f : Mathf.Max(0f,
                condition.ExpiresAtSeconds - simulationTime);
        }

        public void SetIsolation(IsolationState isolation, float simulationTime)
        {
            Isolated = isolation != null;
            IsolationRemainingSeconds = isolation == null ? 0f : Mathf.Max(0f,
                isolation.ExpiresAt - simulationTime);
            IsolationRemainingFraction = isolation == null ? 0f : Mathf.Clamp01(
                (isolation.ExpiresAt - simulationTime) /
                (isolation.ExpiresAt - isolation.ActivatedAt));
        }

        public void SetInterceptor(InterceptionState interceptor, float simulationTime)
        {
            InterceptPayloadId = interceptor?.PayloadId;
            InterceptActive = interceptor != null;
            InterceptAllPayloads = interceptor != null && interceptor.AllPayloads;
            InterceptSucceeded = interceptor != null && interceptor.Succeeded;
            InterceptRemainingSeconds = interceptor == null ? 0f : Mathf.Max(0f,
                interceptor.ExpiresAt - simulationTime);
            InterceptRemainingFraction = interceptor == null ? 0f : Mathf.Clamp01(
                (interceptor.ExpiresAt - simulationTime) /
                (interceptor.ExpiresAt - interceptor.ActivatedAt));
        }

        public void Reset()
        {
            Offline = false;
            IntrusionPending = false;
            IntrusionRemainingSeconds = 0f;
            KnownInfected = false;
            AwarenessPreview = false;
            PortScanned = false;
            PortScanRemainingFraction = 0f;
            PortScanRemainingSeconds = 0f;
            ScanJammed = false;
            Backdoor = false;
            BackdoorRemainingSeconds = 0f;
            ScanJamRemainingFraction = 0f;
            ScanJamRemainingSeconds = 0f;
            Isolated = false;
            IsolationRemainingFraction = 0f;
            IsolationRemainingSeconds = 0f;
            InterceptPayloadId = null;
            InterceptActive = false;
            InterceptAllPayloads = false;
            InterceptSucceeded = false;
            InterceptRemainingFraction = 0f;
            InterceptRemainingSeconds = 0f;
            Overclocked = false;
            OverclockRemainingSeconds = 0f;
            cleanUntil = -1f;
        }
    }
}
