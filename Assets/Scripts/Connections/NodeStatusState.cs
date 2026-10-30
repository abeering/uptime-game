using UnityEngine;

namespace Uptime.Connections
{
    // Player-visible diagnostic knowledge, independent of the host's infection list.
    public sealed class NodeStatusState
    {
        private float cleanUntil = -1f;

        public bool Offline { get; private set; }
        public bool KnownInfected { get; private set; }
        public bool AwarenessPreview { get; private set; }
        public bool PortScanned { get; private set; }
        public float PortScanRemainingFraction { get; private set; }
        public bool CleanVisible(float realTime) => realTime < cleanUntil;
        public bool HasAny(float realTime) => Offline || KnownInfected ||
            AwarenessPreview || PortScanned || CleanVisible(realTime);

        public void SetOffline(bool offline) => Offline = offline;

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
            PortScanRemainingFraction = condition == null ? 0f : Mathf.Clamp01(
                (condition.ExpiresAtSeconds - simulationTime) /
                (condition.ExpiresAtSeconds - condition.AppliedAtSeconds));
        }

        public void Reset()
        {
            Offline = false;
            KnownInfected = false;
            AwarenessPreview = false;
            PortScanned = false;
            PortScanRemainingFraction = 0f;
            cleanUntil = -1f;
        }
    }
}
