using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public sealed class LogicalPathPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform pathLayer;
        [SerializeField] private RectTransform markerLayer;
        [SerializeField] private LogicalPathView pathPrefab;
        [SerializeField] private IdentityPresenter identityPresenter;
        [SerializeField] private ConnectionIdentityPalette identityPalette = new ConnectionIdentityPalette();
        [Header("Logical line geometry (UI pixels)")]
        [SerializeField, Min(0.1f)] private float maxLineWidth = 12f;
        [SerializeField, Min(0f)] private float minLineGap = 3f;
        [SerializeField, Min(0.1f), Tooltip("Diagnostic threshold only; narrower lines are reported in the Console.")]
        private float minLineWidth = 2.5f;
        [SerializeField] private bool adaptiveNodePorts;

        private LogicalConnectionState[] connections;
        private readonly Dictionary<LogicalConnectionState, LogicalPathView> views =
            new Dictionary<LogicalConnectionState, LogicalPathView>();
        private readonly HashSet<PhysicalLinkView> warnedLinks = new HashSet<PhysicalLinkView>();
        private readonly RouteLaneOrder laneOrder = new RouteLaneOrder();
        private bool previewMode;

        public RectTransform NetworkArea => networkArea;
        public RectTransform PathLayer => pathLayer;
        public RectTransform MarkerLayer => markerLayer;
        public PathGraphic PathGraphicPrefab => pathPrefab != null ? pathPrefab.GetComponent<PathGraphic>() : null;
        public float MaxLineWidth => Mathf.Max(0.1f, maxLineWidth);
        public float MinLineGap => Mathf.Max(0f, minLineGap);
        public float MinLineWidth => Mathf.Max(0.1f, minLineWidth);
        public ConnectionIdentityPalette IdentityPalette => identityPalette;

        public void SetPreviewMode(bool enabled)
        {
            if (previewMode == enabled)
                return;
            previewMode = enabled;
            foreach (LogicalPathView view in views.Values)
                if (view != null)
                    view.SetPresentationActive(!enabled);
            if (enabled && identityPresenter != null)
            {
                identityPresenter.BeginFrame();
                identityPresenter.EndFrame();
            }
        }

        public void Bind(LogicalConnectionState[] states)
        {
            if (networkArea == null || pathLayer == null || pathPrefab == null)
                return;
            foreach (LogicalPathView oldView in views.Values)
                if (oldView != null)
                {
                    oldView.Destroyed -= OnPathDestroyed;
                    oldView.ReleaseTerminalMarker();
                    Destroy(oldView.gameObject);
                }
            views.Clear();
            warnedLinks.Clear();
            laneOrder.Clear();
            identityPalette.Reset();
            connections = states;
            foreach (LogicalConnectionState connection in connections)
            {
                LogicalPathView view = Instantiate(pathPrefab, pathLayer, false);
                view.name = "Logical path " + connection.ConnectionId;
                view.Bind(connection, networkArea, markerLayer);
                view.Destroyed += OnPathDestroyed;
                view.SetPresentationActive(!previewMode);
                views.Add(connection, view);
            }
            UpdateLayouts();
        }

        public void Add(LogicalConnectionState state)
        {
            if (state == null || views.ContainsKey(state)) return;
            var next = new List<LogicalConnectionState>(connections ?? Array.Empty<LogicalConnectionState>()) { state };
            connections = next.ToArray();
            if (networkArea == null || pathLayer == null || pathPrefab == null) return;
            LogicalPathView view = Instantiate(pathPrefab, pathLayer, false);
            view.name = "Logical path " + state.ConnectionId;
            view.Bind(state, networkArea, markerLayer);
            view.Destroyed += OnPathDestroyed;
            view.SetPresentationActive(!previewMode);
            views.Add(state, view);
            UpdateLayouts();
        }

        public void Retire(LogicalConnectionState state)
        {
            if (connections == null) return;
            if (!state.IsNormalDeparture && views.ContainsKey(state))
                return; // Keep the lane and ID visible through both failure flashes.
            var next = new List<LogicalConnectionState>(connections);
            next.Remove(state);
            connections = next.ToArray();
            // The view keeps its state through its own departure animation.
            views.Remove(state);
            UpdateLayouts();
        }

        private void OnPathDestroyed(LogicalConnectionState state)
        {
            identityPalette.Release(state);
            views.Remove(state);
            if (connections != null)
            {
                var next = new List<LogicalConnectionState>(connections);
                next.Remove(state);
                connections = next.ToArray();
                UpdateLayouts();
            }
        }

        private void LateUpdate()
        {
            if (connections != null && !previewMode)
                UpdateLayouts();
        }

        // Recompute from active presentation state. Pending and departing paths hold no lane.
        private void UpdateLayouts()
        {
            if (previewMode)
                return;
            var usersByLink = new Dictionary<PhysicalLinkReference,
                List<LogicalConnectionState>>();
            var routes = new List<RouteLaneOrder.Route>();
            foreach (LogicalConnectionState connection in connections)
            {
                if (!connection.IsActive && !connection.IsConnecting &&
                    !(connection.IsComplete && !connection.IsNormalDeparture))
                    continue;
                Color accent = identityPalette.Assign(connection);
                views[connection].SetIdentityColor(accent);
                var nodes = new NodeView[connection.Definition.Route.Length];
                var links = new PhysicalLinkView[connection.RouteLinks.Length];
                for (int i = 0; i < nodes.Length; i++)
                    nodes[i] = connection.Definition.Route[i].Node;
                for (int i = 0; i < links.Length; i++)
                    links[i] = connection.RouteLinks[i].View;
                routes.Add(new RouteLaneOrder.Route(connection.ConnectionId, nodes, links));
                foreach (PhysicalLinkReference link in connection.RouteLinks)
                {
                    if (!usersByLink.TryGetValue(link, out List<LogicalConnectionState> users))
                    {
                        users = new List<LogicalConnectionState>();
                        usersByLink.Add(link, users);
                    }
                    users.Add(connection);
                }
            }
            laneOrder.Update(routes, networkArea);
            foreach (KeyValuePair<PhysicalLinkReference, List<LogicalConnectionState>> entry in usersByLink)
            {
                List<string> order = laneOrder.Orders[entry.Key.View];
                entry.Value.Sort((a, b) => order.IndexOf(a.ConnectionId).CompareTo(
                    order.IndexOf(b.ConnectionId)));
            }

            var portCounts = new Dictionary<PhysicalLinkView, int>();
            foreach (KeyValuePair<PhysicalLinkReference, List<LogicalConnectionState>> entry in usersByLink)
                portCounts[entry.Key.View] = entry.Value.Count;
            Dictionary<PhysicalLinkView, NodePortPacking.Port> ports = adaptiveNodePorts ?
                NodePortPacking.Plan(portCounts, networkArea) :
                new Dictionary<PhysicalLinkView, NodePortPacking.Port>();

            var layouts = new Dictionary<PhysicalLinkReference,
                IdentityPacking.Layout>();
            if (identityPresenter != null)
                foreach (KeyValuePair<PhysicalLinkReference,
                    List<LogicalConnectionState>> entry in usersByLink)
                {
                    ports.TryGetValue(entry.Key.View, out NodePortPacking.Port port);
                    layouts.Add(entry.Key, identityPresenter.ResolveLayout(entry.Key.View,
                        entry.Value.Count, maxLineWidth, minLineWidth, minLineGap, port));
                }

            var currentlyOverflowed = new HashSet<PhysicalLinkView>();
            var identitiesByLink = new Dictionary<PhysicalLinkView,
                List<IdentityPresenter.LiveLine>>();
            if (identityPresenter != null)
                identityPresenter.BeginFrame();
            foreach (LogicalConnectionState connection in connections)
            {
                if (!connection.IsActive && !connection.IsConnecting &&
                    !(connection.IsComplete && !connection.IsNormalDeparture))
                    continue;
                Color accent = identityPalette.Assign(connection);
                float[] starts = new float[connection.RouteLinks.Length];
                float[] ends = new float[starts.Length];
                float[] widths = new float[starts.Length];
                for (int edge = 0; edge < starts.Length; edge++)
                {
                    PhysicalLinkReference link = connection.RouteLinks[edge];
                    PhysicalLinkView linkView = link.View;
                    List<LogicalConnectionState> users = usersByLink[link];
                    ports.TryGetValue(linkView, out NodePortPacking.Port port);
                    float budget = port.Budget > 0f ? port.Budget : linkView.BundleWidth(networkArea);
                    layouts.TryGetValue(link, out IdentityPacking.Layout layout);
                    float width = layout != null ? layout.LineWidth :
                        EdgeGeometry.LineWidth(budget, users.Count, minLineGap, maxLineWidth);
                    float gap = layout != null ? layout.LineGap : minLineGap;
                    float offset = EdgeGeometry.Offset(users.IndexOf(connection), users.Count, width, gap);
                    // The lane is stable in the physical edge's A-to-B frame, including reverse routes.
                    bool forward = connection.Definition.Route[edge].Node == linkView.NodeA;
                    float sign = forward ? 1f : -1f;
                    starts[edge] = sign * (offset + (forward ? port.AtA : port.AtB));
                    ends[edge] = sign * (offset + (forward ? port.AtB : port.AtA));
                    widths[edge] = width;
                    if (identityPresenter != null)
                    {
                        NodeView routeA = connection.Definition.Route[edge].Node;
                        NodeView routeB = connection.Definition.Route[edge + 1].Node;
                        Vector2 a = networkArea.InverseTransformPoint(routeA.transform.position);
                        Vector2 b = networkArea.InverseTransformPoint(routeB.transform.position);
                        Vector2 direction = (b - a).normalized;
                        Vector2 targetStart = EdgeGeometry.CardEdge(routeA,
                            networkArea, direction, starts[edge]);
                        Vector2 targetEnd = EdgeGeometry.CardEdge(routeB,
                            networkArea, -direction, -ends[edge]);
                        bool hasVisual = views[connection].TryGetVisualSegment(edge, out Vector2 visualStart,
                            out Vector2 visualEnd, out float visibleFraction, out _);
                        if (!hasVisual)
                        {
                            visualStart = targetStart;
                            visualEnd = targetEnd;
                        }
                        if (!identitiesByLink.TryGetValue(linkView,
                            out List<IdentityPresenter.LiveLine> lines))
                        {
                            lines = new List<IdentityPresenter.LiveLine>();
                            identitiesByLink.Add(linkView, lines);
                        }
                        lines.Add(new IdentityPresenter.LiveLine(connection.ConnectionId,
                            targetStart, targetEnd, visualStart, visualEnd,
                            views[connection].DisplayAccent, visibleFraction,
                            width));
                    }
                    if (width < minLineWidth)
                    {
                        currentlyOverflowed.Add(linkView);
                        if (warnedLinks.Add(linkView))
                            Debug.LogWarning($"Logical path width below {minLineWidth:0.#} px on {linkView.LinkId}: " +
                                $"{users.Count} lines in {budget:0.#} px ({width:0.##} px each). " +
                                "Overflow presentation is not implemented yet.", linkView);
                    }
                }
                views[connection].SetLayout(starts, ends, widths);
            }
            warnedLinks.IntersectWith(currentlyOverflowed);
            if (identityPresenter != null)
            {
                foreach (KeyValuePair<PhysicalLinkView,
                    List<IdentityPresenter.LiveLine>> entry in identitiesByLink)
                {
                    List<string> order = laneOrder.Orders[entry.Key];
                    entry.Value.Sort((a, b) => order.IndexOf(a.Id).CompareTo(order.IndexOf(b.Id)));
                    identityPresenter.PresentEdge(entry.Key, entry.Value);
                }
                identityPresenter.EndFrame();
            }
        }
    }
}
