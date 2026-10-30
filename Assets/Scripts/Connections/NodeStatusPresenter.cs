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
        [SerializeField, Min(.1f)] private float cleanAcknowledgmentSeconds = 2.5f;

        private readonly Dictionary<NodeView, NodeStatusState> states = new();
        private readonly Dictionary<NodeView, NodeStatusView> views = new();
        private readonly Dictionary<NodeView, NodeStatusAnchor> anchors = new();
        private readonly Dictionary<NodeView, Vector2> lastSizes = new();
        private Vector2 lastViewportSize;

        public SimulationHost Host => host;
        public RectTransform NetworkArea => networkArea;
        public RectTransform StatusLayer => statusLayer;
        public NodeStatusView StatusPrefab => statusPrefab;
        public float CleanAcknowledgmentSeconds => cleanAcknowledgmentSeconds;
        public NodeStatusState StateFor(NodeView node) => states.TryGetValue(node,
            out NodeStatusState state) ? state : null;
        public NodeStatusView ViewFor(NodeView node) => views.TryGetValue(node,
            out NodeStatusView view) ? view : null;

        private void OnEnable()
        {
            if (host == null) return;
            host.ScanCompleted += OnScanCompleted;
            host.CleanSucceeded += OnCleanSucceeded;
            host.SimulationReset += OnSimulationReset;
        }

        private void OnDisable()
        {
            if (host != null)
            {
                host.ScanCompleted -= OnScanCompleted;
                host.CleanSucceeded -= OnCleanSucceeded;
                host.SimulationReset -= OnSimulationReset;
            }
            foreach (NodeStatusView view in views.Values)
                if (view != null)
                {
                    if (Application.isPlaying) Destroy(view.gameObject);
                    else DestroyImmediate(view.gameObject);
                }
            views.Clear();
            states.Clear();
            anchors.Clear();
            lastSizes.Clear();
            lastViewportSize = Vector2.zero;
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
            Vector2 viewportSize = networkArea.rect.size;
            if ((viewportSize - lastViewportSize).sqrMagnitude > 1f)
            {
                anchors.Clear();
                lastSizes.Clear();
                lastViewportSize = viewportSize;
            }
            foreach (NodeResources node in host.Nodes)
            {
                if (node?.View == null || !views.TryGetValue(node.View, out NodeStatusView view))
                    continue;
                NodeView card = node.View;
                NodeStatusState state = states[card];
                state.SetOffline(host.IsNodeOffline(card));
                state.SetPortScanned(host.ConditionFor(card, ConditionKind.PortScanned),
                    host.SimulationTimeSeconds);
                Rect cardRect = EdgeGeometry.CardRect(card, networkArea);
                view.SetNodeRect(cardRect);
                view.Render(state, realTime, deltaSeconds);
                if (!view.HasVisibleFamily)
                {
                    anchors.Remove(card);
                    lastSizes.Remove(card);
                    continue;
                }
                Vector2 size = view.StackSize;
                bool changedSize = !lastSizes.TryGetValue(card, out Vector2 last) ||
                    (last - size).sqrMagnitude > .01f;
                if (!anchors.TryGetValue(card, out NodeStatusAnchor anchor) || changedSize)
                {
                    anchor = ChooseAnchor(card, cardRect, size);
                    anchors[card] = anchor;
                    lastSizes[card] = size;
                }
                view.PlaceStack(NodeStatusPlacement.At(anchor, cardRect, size));
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
                views.Add(card, view);
                states.Add(card, new NodeStatusState());
            }
        }

        private NodeStatusAnchor ChooseAnchor(NodeView card, Rect cardRect, Vector2 size)
        {
            var cards = new List<Rect>();
            foreach (NodeResources node in host.Nodes)
                if (node?.View != null && node.View != card)
                    cards.Add(EdgeGeometry.CardRect(node.View, networkArea));
            var critical = new List<Rect>();
            var busyDirections = new List<Vector2>();
            foreach (TerminalMarkerStyle socket in networkArea.GetComponentsInChildren<TerminalMarkerStyle>())
                if (socket.isActiveAndEnabled)
                {
                    Rect socketRect = RelativeRect(socket.rectTransform);
                    critical.Add(NodeStatusPlacement.Expanded(socketRect, 12f));
                    if (NodeStatusPlacement.Expanded(cardRect, 20f)
                        .Contains(socketRect.center))
                        busyDirections.Add(socketRect.center - cardRect.center);
                }
            foreach (IdentityPillView pill in networkArea.GetComponentsInChildren<IdentityPillView>())
                if (pill.isActiveAndEnabled)
                    critical.Add(NodeStatusPlacement.Expanded(
                        RelativeRect((RectTransform)pill.transform), 8f));
            var beams = new List<NodeStatusPlacement.BeamSegment>();
            foreach (LogicalPathView path in networkArea.GetComponentsInChildren<LogicalPathView>())
                if (path.isActiveAndEnabled)
                    for (int i = 0; i < path.VisualSegmentCount; i++)
                        if (path.TryGetVisualSegment(i, out Vector2 start, out Vector2 end,
                            out float visible, out _) && visible > .01f)
                            beams.Add(new NodeStatusPlacement.BeamSegment(start,
                                Vector2.Lerp(start, end, visible)));
            var otherStacks = new List<Rect>();
            foreach (var pair in views)
                if (pair.Key != card && anchors.TryGetValue(pair.Key, out _) &&
                    pair.Value.HasVisibleFamily)
                    otherStacks.Add(RelativeRect(pair.Value.StackRoot));
            Rect viewport = networkArea.rect;
            viewport.xMin += 8f; viewport.xMax -= 8f;
            viewport.yMin += 8f; viewport.yMax -= 8f;
            return NodeStatusPlacement.Choose(cardRect, size, viewport,
                cards, critical, otherStacks, busyDirections, beams);
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
                state.ScanResult(session.Result, Time.unscaledTime,
                    cleanAcknowledgmentSeconds);
        }

        private void OnCleanSucceeded(CleanSession session, int removedCount)
        {
            if (session != null && states.TryGetValue(session.Target.View,
                out NodeStatusState state))
                state.CleanSucceeded();
        }

        private void OnSimulationReset()
        {
            foreach (NodeStatusState state in states.Values) state.Reset();
            foreach (NodeStatusView view in views.Values) view.ClearVisuals();
            anchors.Clear();
            lastSizes.Clear();
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
