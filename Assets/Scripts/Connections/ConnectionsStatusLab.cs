using UnityEngine;

namespace Uptime.Connections
{
    // Opt-in, presentation-only scene driver. No simulation services or effects.
    public sealed class ConnectionsStatusLab : MonoBehaviour
    {
        [SerializeField] private NodeView node;
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private NodeStatusView statusView;
        [SerializeField] private NodeEventStamp stamp;

        private readonly NodeStatusState state = new();
        private bool offline;
        private bool infected;
        private float cleanUntil = -1f;
        private NodeCondition jam;
        private NodeCondition portScan;
        private IsolationState isolation;
        private InterceptionState interception;

        public NodeStatusState State => state;
        public NodeStatusView StatusView => statusView;
        public NodeEventStamp Stamp => stamp;
        public NodeView Node => node;
        public RectTransform NetworkArea => networkArea;

        private void OnEnable()
        {
            if (statusView != null) statusView.SetBandsVisible(false);
            ClearAll();
        }
        private void Update() => RenderNow(Time.unscaledTime, Time.unscaledDeltaTime);

        public void ToggleOffline() { offline = !offline; Refresh(); if (offline) Announce(NodeEventStamp.EventKind.Offline); }
        public void ToggleInfected() { infected = !infected; Refresh(); if (infected) Announce(NodeEventStamp.EventKind.Infected); }
        public void ScanJammed()
        {
            jam = Condition(ConditionKind.ScanJammed, 8f);
            Refresh();
            Announce(NodeEventStamp.EventKind.ScanJammed);
        }
        public void PortScanned()
        {
            portScan = Condition(ConditionKind.PortScanned, 20f);
            Refresh();
            Announce(NodeEventStamp.EventKind.PortScanned);
        }
        public void Clean() { cleanUntil = Time.unscaledTime + 2.5f; Refresh(); Announce(NodeEventStamp.EventKind.Clean); }
        public void Isolate()
        {
            isolation = new IsolationState
            {
                ActivatedAt = Time.unscaledTime,
                ExpiresAt = Time.unscaledTime + 8f
            };
            Refresh();
            Announce(NodeEventStamp.EventKind.Isolated);
        }
        public void Intercept() => ApplyInterceptor(false);
        public void Caught() => ApplyInterceptor(true);

        private void ApplyInterceptor(bool caught)
        {
            // These are two outcomes of the existing single interceptor slot.
            interception = new InterceptionState
            {
                PayloadId = "x1",
                ActivatedAt = Time.unscaledTime,
                ExpiresAt = Time.unscaledTime + 5f,
                Succeeded = caught
            };
            Refresh();
            Announce(caught ? NodeEventStamp.EventKind.Caught : NodeEventStamp.EventKind.Intercept);
        }

        private void Announce(NodeEventStamp.EventKind kind)
        {
            if (stamp != null) stamp.Play(kind);
        }

        private static NodeCondition Condition(ConditionKind kind, float seconds) =>
            new NodeCondition(null, new ConditionPayload(kind, seconds),
                Time.unscaledTime, null, 0);

        public void ClearAll()
        {
            offline = infected = false;
            cleanUntil = -1f;
            jam = portScan = null;
            isolation = null;
            interception = null;
            state.Reset();
            if (statusView != null) statusView.ClearVisuals();
            if (stamp != null) stamp.Cancel();
        }

        private void Refresh() => RenderNow(Time.unscaledTime, 0f);

        // Same clock drives expiry, radial fractions, pulse and entry/exit animation.
        // Explicit time also permits deterministic Editor checks without a host.
        public void RenderNow(float realTime, float deltaSeconds)
        {
            if (jam != null && realTime >= jam.ExpiresAtSeconds) jam = null;
            if (portScan != null && realTime >= portScan.ExpiresAtSeconds) portScan = null;
            if (isolation != null && realTime >= isolation.ExpiresAt) isolation = null;
            if (interception != null && realTime >= interception.ExpiresAt) interception = null;
            if (realTime >= cleanUntil) cleanUntil = -1f;

            state.SetPreviewDiagnosis(infected, cleanUntil);
            state.SetOffline(offline || isolation != null);
            state.SetScanJammed(jam, realTime);
            state.SetPortScanned(portScan, realTime);
            state.SetIsolation(isolation, realTime);
            state.SetInterceptor(interception, realTime);

            if (node == null || networkArea == null || statusView == null) return;
            Rect card = EdgeGeometry.CardRect(node, networkArea);
            statusView.SetNodeRect(card);
            statusView.SetBandsVisible(false);
            statusView.Render(state, realTime, deltaSeconds);
            // One unobstructed node: retain production stack geometry at its N anchor.
            statusView.PlaceStack(NodeStatusPlacement.At(NodeStatusAnchor.N,
                card, statusView.StackSize));
        }
    }
}
