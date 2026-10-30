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
        public bool CleanVisible(float realTime) => realTime < cleanUntil;
        public bool HasAny(float realTime) => Offline || KnownInfected ||
            AwarenessPreview || CleanVisible(realTime);

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

        public void Reset()
        {
            Offline = false;
            KnownInfected = false;
            AwarenessPreview = false;
            cleanUntil = -1f;
        }
    }
}
