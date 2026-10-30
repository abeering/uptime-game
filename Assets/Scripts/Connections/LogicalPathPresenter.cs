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
        [SerializeField] private PayloadDirector payloadDirector;
        [SerializeField] private PayloadPresenter payloadPresenter;
        [SerializeField] private WormDirector wormDirector;
        [SerializeField] private WormPresenter wormPresenter;
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
        private readonly HashSet<PhysicalLinkView> positionedLinks = new HashSet<PhysicalLinkView>();
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
        public void RefreshLayouts() => UpdateLayouts();

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
            if (enabled) ClearPhysicalPositions();
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
            ClearPhysicalPositions();
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
            }
            var payloadLinks = new Dictionary<PayloadState, PhysicalLinkView>();
            var pilllessIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (payloadDirector != null)
                foreach (PayloadState payload in payloadDirector.Payloads)
                {
                    if (payload.Phase == PayloadPhase.Resolved) continue;
                    PhysicalLinkView link = null;
                    foreach (PhysicalLinkReference reference in payloadDirector.Host.PhysicalLinks)
                        if (reference?.View != null &&
                            (reference.View.NodeA == payload.CurrentNode &&
                             reference.View.NodeB == payload.NextNode ||
                             reference.View.NodeB == payload.CurrentNode &&
                             reference.View.NodeA == payload.NextNode))
                        { link = reference.View; break; }
                    if (link == null) continue;
                    payloadLinks.Add(payload, link);
                    pilllessIds.Add(payload.Id);
                    routes.Add(new RouteLaneOrder.Route(payload.Id,
                        new[] { payload.CurrentNode, payload.NextNode }, new[] { link }));
                }
            var wormLinks = new Dictionary<WormBranch, PhysicalLinkView>();
            if (wormDirector != null)
                foreach (WormOutbreak outbreak in wormDirector.Outbreaks)
                    foreach (WormBranch branch in outbreak.Branches)
                    {
                        if (!branch.HasHop || branch.Link == null) continue;
                        wormLinks.Add(branch, branch.Link);
                        pilllessIds.Add(branch.Id);
                        routes.Add(new RouteLaneOrder.Route(branch.Id,
                            new[] { branch.CurrentNode, branch.NextNode },
                            new[] { branch.Link }));
                    }
            laneOrder.Update(routes, networkArea);

            var portCounts = new Dictionary<PhysicalLinkView, int>();
            foreach (KeyValuePair<PhysicalLinkView, List<string>> entry in laneOrder.Orders)
                portCounts[entry.Key] = entry.Value.Count;
            Dictionary<PhysicalLinkView, NodePortPacking.Port> ports = adaptiveNodePorts ?
                NodePortPacking.Plan(portCounts, networkArea, maxLineWidth, minLineGap) :
                new Dictionary<PhysicalLinkView, NodePortPacking.Port>();
            float relayoutSeconds = pathPrefab.RelayoutSeconds;
            var departedLinks = new List<PhysicalLinkView>();
            foreach (PhysicalLinkView link in positionedLinks)
                if (!portCounts.ContainsKey(link)) departedLinks.Add(link);
            foreach (PhysicalLinkView link in departedLinks)
            {
                if (link != null) link.ClearAttachmentOffsets(relayoutSeconds);
                positionedLinks.Remove(link);
            }
            foreach (PhysicalLinkView link in portCounts.Keys)
            {
                ports.TryGetValue(link, out NodePortPacking.Port port);
                link.SetAttachmentOffsets(port.AtA, port.AtB, relayoutSeconds);
                positionedLinks.Add(link);
            }

            var layouts = new Dictionary<PhysicalLinkView,
                IdentityPacking.Layout>();
            if (identityPresenter != null)
                foreach (KeyValuePair<PhysicalLinkView, List<string>> entry in laneOrder.Orders)
                {
                    ports.TryGetValue(entry.Key, out NodePortPacking.Port port);
                    var pillEligible = new bool[entry.Value.Count];
                    for (int i = 0; i < pillEligible.Length; i++)
                        pillEligible[i] = !pilllessIds.Contains(entry.Value[i]);
                    layouts.Add(entry.Key, identityPresenter.ResolveLayout(entry.Key,
                        entry.Value.Count, maxLineWidth, minLineWidth, minLineGap, port,
                        pillEligible));
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
                float maximumSocketDiameter = float.PositiveInfinity;
                for (int edge = 0; edge < starts.Length; edge++)
                {
                    PhysicalLinkReference link = connection.RouteLinks[edge];
                    PhysicalLinkView linkView = link.View;
                    List<string> users = laneOrder.Orders[linkView];
                    ports.TryGetValue(linkView, out NodePortPacking.Port port);
                    float budget = port.Budget > 0f ? port.Budget : linkView.BundleWidth(networkArea);
                    layouts.TryGetValue(linkView, out IdentityPacking.Layout layout);
                    float width = layout != null ? layout.LineWidth :
                        EdgeGeometry.LineWidth(budget, users.Count, minLineGap, maxLineWidth);
                    float gap = layout != null ? layout.LineGap : minLineGap;
                    if (edge == starts.Length - 1 && users.Count > 1)
                        maximumSocketDiameter = Mathf.Max(12f, width + gap - 2f);
                    float offset = EdgeGeometry.Offset(users.IndexOf(connection.ConnectionId),
                        users.Count, width, gap);
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
                views[connection].SetLayout(starts, ends, widths, maximumSocketDiameter);
            }
            foreach (KeyValuePair<PayloadState, PhysicalLinkView> entry in payloadLinks)
            {
                PayloadState payload = entry.Key;
                PhysicalLinkView link = entry.Value;
                List<string> users = laneOrder.Orders[link];
                ports.TryGetValue(link, out NodePortPacking.Port port);
                layouts.TryGetValue(link, out IdentityPacking.Layout layout);
                float budget = port.Budget > 0f ? port.Budget : link.BundleWidth(networkArea);
                float width = layout != null ? layout.LineWidth :
                    EdgeGeometry.LineWidth(budget, users.Count, minLineGap, maxLineWidth);
                float gap = layout != null ? layout.LineGap : minLineGap;
                float offset = EdgeGeometry.Offset(users.IndexOf(payload.Id), users.Count,
                    width, gap);
                bool forward = payload.CurrentNode == link.NodeA;
                float sign = forward ? 1f : -1f;
                float startOffset = sign * (offset + (forward ? port.AtA : port.AtB));
                float endOffset = sign * (offset + (forward ? port.AtB : port.AtA));
                payloadPresenter?.SetLayout(payload, startOffset, endOffset, width);
                if (identityPresenter != null)
                {
                    Vector2 a = networkArea.InverseTransformPoint(payload.CurrentNode.transform.position);
                    Vector2 b = networkArea.InverseTransformPoint(payload.NextNode.transform.position);
                    Vector2 direction = (b - a).normalized;
                    Vector2 targetStart = EdgeGeometry.CardEdge(payload.CurrentNode,
                        networkArea, direction, startOffset);
                    Vector2 targetEnd = EdgeGeometry.CardEdge(payload.NextNode,
                        networkArea, -direction, -endOffset);
                    if (!identitiesByLink.TryGetValue(link, out List<IdentityPresenter.LiveLine> lines))
                    {
                        lines = new List<IdentityPresenter.LiveLine>();
                        identitiesByLink.Add(link, lines);
                    }
                    lines.Add(new IdentityPresenter.LiveLine(payload.Id,
                        targetStart, targetEnd, targetStart, targetEnd,
                        payloadDirector.PayloadColor,
                        payload.Phase == PayloadPhase.Establishing ? payload.Progress : 1f,
                        width, false));
                }
            }
            foreach (KeyValuePair<WormBranch, PhysicalLinkView> entry in wormLinks)
            {
                WormBranch branch = entry.Key;
                PhysicalLinkView link = entry.Value;
                List<string> users = laneOrder.Orders[link];
                ports.TryGetValue(link, out NodePortPacking.Port port);
                layouts.TryGetValue(link, out IdentityPacking.Layout layout);
                float budget = port.Budget > 0f ? port.Budget : link.BundleWidth(networkArea);
                float width = layout != null ? layout.LineWidth :
                    EdgeGeometry.LineWidth(budget, users.Count, minLineGap, maxLineWidth);
                float gap = layout != null ? layout.LineGap : minLineGap;
                float offset = EdgeGeometry.Offset(users.IndexOf(branch.Id), users.Count,
                    width, gap);
                bool forward = branch.CurrentNode == link.NodeA;
                float sign = forward ? 1f : -1f;
                float startOffset = sign * (offset + (forward ? port.AtA : port.AtB));
                float endOffset = sign * (offset + (forward ? port.AtB : port.AtA));
                wormPresenter?.SetLayout(branch, startOffset, endOffset, width, gap);
                if (identityPresenter != null)
                {
                    Vector2 a = networkArea.InverseTransformPoint(branch.CurrentNode.transform.position);
                    Vector2 b = networkArea.InverseTransformPoint(branch.NextNode.transform.position);
                    Vector2 direction = (b - a).normalized;
                    Vector2 targetStart = EdgeGeometry.CardEdge(branch.CurrentNode,
                        networkArea, direction, startOffset);
                    Vector2 targetEnd = EdgeGeometry.CardEdge(branch.NextNode,
                        networkArea, -direction, -endOffset);
                    if (!identitiesByLink.TryGetValue(link, out List<IdentityPresenter.LiveLine> lines))
                    {
                        lines = new List<IdentityPresenter.LiveLine>();
                        identitiesByLink.Add(link, lines);
                    }
                    float visible = branch.Phase == WormPhase.Establishing ? branch.Progress : 1f;
                    lines.Add(new IdentityPresenter.LiveLine(branch.Id,
                        targetStart, targetEnd, targetStart, targetEnd,
                        wormDirector.WormColor, visible, width, false));
                }
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

        private void ClearPhysicalPositions()
        {
            foreach (PhysicalLinkView link in positionedLinks)
                if (link != null) link.ClearAttachmentOffsets(0.01f);
            positionedLinks.Clear();
        }

        private void OnDisable() => ClearPhysicalPositions();
    }
}
