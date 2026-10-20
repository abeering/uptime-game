using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public sealed class LogicalPathPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform pathLayer;
        [SerializeField] private LogicalPathView pathPrefab;
        [SerializeField] private IdentityPresenter identityPresenter;
        [Header("Logical line geometry (UI pixels)")]
        [SerializeField, Min(0.1f)] private float maxLineWidth = 12f;
        [SerializeField, Min(0f)] private float minLineGap = 3f;
        [SerializeField, Min(0.1f), Tooltip("Diagnostic threshold only; narrower lines are reported in the Console.")]
        private float minLineWidth = 2.5f;

        private LogicalConnectionState[] connections;
        private readonly Dictionary<LogicalConnectionState, LogicalPathView> views =
            new Dictionary<LogicalConnectionState, LogicalPathView>();
        private readonly HashSet<PhysicalLinkView> warnedLinks = new HashSet<PhysicalLinkView>();
        private bool previewMode;

        public RectTransform NetworkArea => networkArea;
        public RectTransform PathLayer => pathLayer;
        public PathGraphic PathGraphicPrefab => pathPrefab != null ? pathPrefab.GetComponent<PathGraphic>() : null;
        public float MaxLineWidth => Mathf.Max(0.1f, maxLineWidth);
        public float MinLineGap => Mathf.Max(0f, minLineGap);
        public float MinLineWidth => Mathf.Max(0.1f, minLineWidth);

        public void SetPreviewMode(bool enabled)
        {
            if (previewMode == enabled)
                return;
            previewMode = enabled;
            foreach (LogicalPathView view in views.Values)
                if (view != null)
                    view.gameObject.SetActive(!enabled);
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
                    Destroy(oldView.gameObject);
            views.Clear();
            warnedLinks.Clear();
            connections = states;
            foreach (LogicalConnectionState connection in connections)
            {
                LogicalPathView view = Instantiate(pathPrefab, pathLayer, false);
                view.name = "Logical path " + connection.ConnectionId;
                view.Bind(connection, networkArea);
                view.gameObject.SetActive(!previewMode);
                views.Add(connection, view);
            }
            UpdateLayouts();
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
            var usersByLink = new Dictionary<PhysicalLinkResources,
                List<LogicalConnectionState>>();
            foreach (LogicalConnectionState connection in connections)
            {
                if (!connection.IsActive)
                    continue;
                foreach (PhysicalLinkResources link in connection.RouteLinks)
                {
                    if (!usersByLink.TryGetValue(link, out List<LogicalConnectionState> users))
                    {
                        users = new List<LogicalConnectionState>();
                        usersByLink.Add(link, users);
                    }
                    users.Add(connection);
                }
            }
            foreach (List<LogicalConnectionState> users in usersByLink.Values)
                users.Sort((a, b) => StringComparer.Ordinal.Compare(a.ConnectionId, b.ConnectionId));

            var layouts = new Dictionary<PhysicalLinkResources,
                IdentityPacking.Layout>();
            if (identityPresenter != null)
                foreach (KeyValuePair<PhysicalLinkResources,
                    List<LogicalConnectionState>> entry in usersByLink)
                    layouts.Add(entry.Key, identityPresenter.ResolveLayout(entry.Key.View,
                        entry.Value.Count, maxLineWidth, minLineWidth, minLineGap));

            var currentlyOverflowed = new HashSet<PhysicalLinkView>();
            var identitiesByLink = new Dictionary<PhysicalLinkView,
                List<IdentityPresenter.LiveLine>>();
            if (identityPresenter != null)
                identityPresenter.BeginFrame();
            foreach (LogicalConnectionState connection in connections)
            {
                if (!connection.IsActive)
                    continue;
                float[] offsets = new float[connection.RouteLinks.Length];
                float[] widths = new float[offsets.Length];
                for (int edge = 0; edge < offsets.Length; edge++)
                {
                    PhysicalLinkResources link = connection.RouteLinks[edge];
                    PhysicalLinkView linkView = link.View;
                    List<LogicalConnectionState> users = usersByLink[link];
                    float budget = linkView.BundleWidth(networkArea);
                    layouts.TryGetValue(link, out IdentityPacking.Layout layout);
                    float width = layout != null ? layout.LineWidth :
                        EdgeGeometry.LineWidth(budget, users.Count, minLineGap, maxLineWidth);
                    float gap = layout != null ? layout.LineGap : minLineGap;
                    float offset = EdgeGeometry.Offset(users.IndexOf(connection), users.Count, width, gap);
                    // The lane is stable in the physical edge's A-to-B frame, including reverse routes.
                    offsets[edge] = connection.Definition.Route[edge].Node == linkView.NodeA ? offset : -offset;
                    widths[edge] = width;
                    if (identityPresenter != null)
                    {
                        NodeView routeA = connection.Definition.Route[edge].Node;
                        NodeView routeB = connection.Definition.Route[edge + 1].Node;
                        Vector2 a = networkArea.InverseTransformPoint(routeA.transform.position);
                        Vector2 b = networkArea.InverseTransformPoint(routeB.transform.position);
                        Vector2 direction = (b - a).normalized;
                        Vector2 targetStart = EdgeGeometry.CardEdge(routeA,
                            networkArea, direction, offsets[edge]);
                        Vector2 targetEnd = EdgeGeometry.CardEdge(routeB,
                            networkArea, -direction, -offsets[edge]);
                        bool hasVisual = views[connection].TryGetVisualSegment(edge, out Vector2 visualStart,
                            out Vector2 visualEnd, out float visibleFraction, out Color lineColor);
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
                            targetStart, targetEnd, visualStart, visualEnd, lineColor, visibleFraction,
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
                views[connection].SetLayout(offsets, widths);
            }
            warnedLinks.IntersectWith(currentlyOverflowed);
            if (identityPresenter != null)
            {
                foreach (KeyValuePair<PhysicalLinkView,
                    List<IdentityPresenter.LiveLine>> entry in identitiesByLink)
                    identityPresenter.PresentEdge(entry.Key, entry.Value);
                identityPresenter.EndFrame();
            }
        }
    }
}
