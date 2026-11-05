using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Presentation-only observer. It never treats the infection list as scan knowledge.
    [ExecuteAlways]
    public sealed class NodeStatusPresenter : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform statusLayer;
        [SerializeField] private NodeStatusView statusPrefab;
        [SerializeField] private RectTransform stampLayer;
        [SerializeField] private NodeEventStamp stampPrefab;
        [SerializeField, Min(.1f)] private float cleanAcknowledgmentSeconds = 2.5f;

        private readonly Dictionary<NodeView, NodeStatusState> states = new();
        private readonly Dictionary<NodeView, NodeStatusView> views = new();
        private readonly Dictionary<NodeView, NodeEventStamp> stamps = new();
        private readonly Dictionary<NodeView, NodeStatusPlacementChoice> placements = new();

        public SimulationHost Host => host;
        public RectTransform NetworkArea => networkArea;
        public RectTransform StatusLayer => statusLayer;
        public NodeStatusView StatusPrefab => statusPrefab;
        public RectTransform StampLayer => stampLayer;
        public NodeEventStamp StampPrefab => stampPrefab;
        public float CleanAcknowledgmentSeconds => cleanAcknowledgmentSeconds;
        public NodeStatusState StateFor(NodeView node) => states.TryGetValue(node,
            out NodeStatusState state) ? state : null;
        public NodeStatusView ViewFor(NodeView node) => views.TryGetValue(node,
            out NodeStatusView view) ? view : null;
        public NodeEventStamp StampFor(NodeView node) => stamps.TryGetValue(node,
            out NodeEventStamp stamp) ? stamp : null;

        private void OnEnable()
        {
            if (host == null) return;
            host.ScanCompleted += OnScanCompleted;
            host.CleanSucceeded += OnCleanSucceeded;
            host.BlackoutApplied += OnBlackoutApplied;
            host.ConditionApplied += OnConditionApplied;
            host.SimulationReset += OnSimulationReset;
            if (host.Worms != null) host.Worms.IsolationActivated += OnIsolationActivated;
            if (host.Payloads != null)
            {
                host.Payloads.InterceptorActivated += OnInterceptorActivated;
                host.Payloads.InterceptSucceeded += OnInterceptSucceeded;
            }
        }

        private void OnDisable()
        {
            if (host != null)
            {
                host.ScanCompleted -= OnScanCompleted;
                host.CleanSucceeded -= OnCleanSucceeded;
                host.BlackoutApplied -= OnBlackoutApplied;
                host.ConditionApplied -= OnConditionApplied;
                host.SimulationReset -= OnSimulationReset;
                if (host.Worms != null) host.Worms.IsolationActivated -= OnIsolationActivated;
                if (host.Payloads != null)
                {
                    host.Payloads.InterceptorActivated -= OnInterceptorActivated;
                    host.Payloads.InterceptSucceeded -= OnInterceptSucceeded;
                }
            }
            foreach (NodeStatusView view in views.Values)
                if (view != null)
                {
                    if (Application.isPlaying) Destroy(view.gameObject);
                    else DestroyImmediate(view.gameObject);
                }
            views.Clear();
            foreach (NodeEventStamp stamp in stamps.Values)
                if (stamp != null)
                {
                    if (Application.isPlaying) Destroy(stamp.gameObject);
                    else DestroyImmediate(stamp.gameObject);
                }
            stamps.Clear();
            states.Clear();
            placements.Clear();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            RenderNow(Time.unscaledTime, Time.unscaledDeltaTime);
        }

        // A deterministic seam for the Editor witness and scripted Play Mode captures.
        public void RenderNow(float realTime, float deltaSeconds)
        {
            EnsureViews();
            if (host == null || networkArea == null) return;
            var markers = new List<Rect>();
            foreach (TerminalMarkerStyle socket in networkArea.GetComponentsInChildren<TerminalMarkerStyle>())
                if (socket.isActiveAndEnabled &&
                    socket.GetComponentInParent<PayloadBodyView>() == null)
                    markers.Add(RelativeRect(socket.rectTransform));
            foreach (NodeResources node in host.Nodes)
            {
                if (node?.View == null || !views.TryGetValue(node.View, out NodeStatusView view))
                    continue;
                NodeView card = node.View;
                NodeStatusState state = states[card];
                state.SetOffline(host.IsNodeOffline(card));
                state.SetPortScanned(host.ConditionFor(card, ConditionKind.PortScanned),
                    host.SimulationTimeSeconds);
                state.SetScanJammed(host.ConditionFor(card, ConditionKind.ScanJammed),
                    host.SimulationTimeSeconds);
                state.SetBackdoor(host.ConditionFor(card, ConditionKind.Backdoor),
                    host.SimulationTimeSeconds);
                state.SetIsolation(host.Worms?.IsolationAt(card), host.SimulationTimeSeconds);
                state.SetInterceptor(host.Payloads?.InterceptorAt(card),
                    host.SimulationTimeSeconds);
                state.SetOverclock(node, host.OverclockedNode,
                    host.OverclockActiveRemaining);
                Rect cardRect = EdgeGeometry.CardRect(card, networkArea);
                view.SetNodeRect(cardRect);
                if (stamps.TryGetValue(card, out NodeEventStamp stamp))
                    stamp.Rect.anchoredPosition = cardRect.center;
                view.Render(state, realTime, deltaSeconds);
                if (!view.HasVisibleFamily)
                {
                    placements.Remove(card);
                    continue;
                }
                Vector2 size = view.StackSize;
                NodeStatusPlacementChoice? previous =
                    placements.TryGetValue(card, out NodeStatusPlacementChoice held) ? held : null;
                NodeStatusPlacementChoice placement = ChoosePlacement(card, cardRect,
                    size, markers, previous);
                placements[card] = placement;
                view.PlaceStack(NodeStatusPlacement.At(placement, cardRect, size),
                    previous.HasValue && previous.Value.Anchor == placement.Anchor ?
                        deltaSeconds : 0f);
            }
        }

        private void EnsureViews()
        {
            if (host?.Nodes == null || networkArea == null || statusLayer == null ||
                statusPrefab == null) return;
            foreach (NodeResources node in host.Nodes)
            {
                NodeView card = node?.View;
                if (card == null || views.ContainsKey(card)) continue;
                NodeStatusView view = Instantiate(statusPrefab, statusLayer, false);
                view.name = "Node status " + card.NodeId;
                view.ClearVisuals();
                view.SetBandsVisible(false);
                views.Add(card, view);
                states.Add(card, new NodeStatusState());
                if (stampLayer != null && stampPrefab != null)
                {
                    NodeEventStamp stamp = Instantiate(stampPrefab, stampLayer, false);
                    stamp.name = "Node event " + card.NodeId;
                    stamp.Cancel();
                    stamps.Add(card, stamp);
                }
            }
        }

        private void PlayStamp(NodeView card, NodeEventStamp.EventKind kind)
        {
            if (card == null) return;
            EnsureViews();
            if (!stamps.TryGetValue(card, out NodeEventStamp stamp)) return;
            if (networkArea != null)
                stamp.Rect.anchoredPosition = EdgeGeometry.CardRect(card, networkArea).center;
            stamp.Play(kind);
        }

        private NodeStatusPlacementChoice ChoosePlacement(NodeView card, Rect cardRect,
            Vector2 size, IReadOnlyList<Rect> markers,
            NodeStatusPlacementChoice? previous)
        {
            var cards = new List<Rect>();
            foreach (NodeResources node in host.Nodes)
                if (node?.View != null && node.View != card)
                    cards.Add(EdgeGeometry.CardRect(node.View, networkArea));
            var otherStacks = new List<Rect>();
            foreach (var pair in views)
                if (pair.Key != card && placements.TryGetValue(pair.Key, out _) &&
                    pair.Value.HasVisibleFamily)
                    otherStacks.Add(RelativeRect(pair.Value.StackRoot));
            Rect viewport = networkArea.rect;
            viewport.xMin += 8f; viewport.xMax -= 8f;
            viewport.yMin += 8f; viewport.yMax -= 8f;
            return NodeStatusPlacement.Choose(cardRect, size, viewport,
                cards, markers, otherStacks, previous);
        }

        private Rect RelativeRect(RectTransform rect)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                networkArea, rect);
            return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
        }

        private void OnScanCompleted(NodeScanSession session)
        {
            if (session == null || !session.IsComplete) return;
            EnsureViews();
            if (states.TryGetValue(session.Target.View, out NodeStatusState state))
            {
                state.ScanResult(session.Result, Time.unscaledTime,
                    cleanAcknowledgmentSeconds);
                PlayStamp(session.Target.View, session.Result == NodeScanResult.Infected ?
                    NodeEventStamp.EventKind.Infected : NodeEventStamp.EventKind.Clean);
            }
        }

        private void OnCleanSucceeded(CleanSession session, int removedCount)
        {
            EnsureViews();
            if (session != null && states.TryGetValue(session.Target.View,
                out NodeStatusState state))
            {
                state.CleanSucceeded();
                if (removedCount > 0)
                    PlayStamp(session.Target.View, NodeEventStamp.EventKind.Clean);
            }
        }

        private void OnBlackoutApplied(NodeInfection infection) =>
            PlayStamp(infection?.Target?.View, NodeEventStamp.EventKind.Offline);

        private void OnConditionApplied(NodeCondition condition)
        {
            if (condition == null) return;
            PlayStamp(condition.Target?.View, condition.Kind == ConditionKind.ScanJammed ?
                NodeEventStamp.EventKind.ScanJammed : condition.Kind == ConditionKind.Backdoor ?
                NodeEventStamp.EventKind.Backdoor : NodeEventStamp.EventKind.PortScanned);
        }

        private void OnIsolationActivated(IsolationState isolation) =>
            PlayStamp(isolation?.Node?.View, NodeEventStamp.EventKind.Isolated);

        private void OnInterceptorActivated(InterceptionState interceptor) =>
            PlayStamp(interceptor?.Node?.View, NodeEventStamp.EventKind.Intercept);

        private void OnInterceptSucceeded(PayloadState payload) =>
            PlayStamp(payload?.FinalNode, NodeEventStamp.EventKind.Caught);

        private void OnSimulationReset()
        {
            foreach (NodeStatusState state in states.Values) state.Reset();
            foreach (NodeStatusView view in views.Values) view.ClearVisuals();
            foreach (NodeEventStamp stamp in stamps.Values) stamp.Cancel();
            placements.Clear();
        }

#if UNITY_EDITOR
        public void SetAwarenessPreview(string nodeId, bool visible)
        {
            EnsureViews();
            foreach (var pair in states)
                if (pair.Key.NodeId == nodeId)
                    pair.Value.SetAwarenessPreview(visible);
        }
#endif
    }
}
