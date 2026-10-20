using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    public sealed class ConnectionsToyLogicalPathPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform pathLayer;
        [SerializeField] private ConnectionsToyLogicalPathView pathPrefab;
        [Header("Logical line geometry (UI pixels)")]
        [SerializeField, Min(0.1f)] private float maxLineWidth = 12f;
        [SerializeField, Min(0f)] private float minLineGap = 3f;
        [SerializeField, Min(0.1f), Tooltip("Diagnostic threshold only; narrower lines are reported in the Console.")]
        private float minLineWidth = 2.5f;

        private ConnectionsToyLogicalConnectionState[] connections;
        private readonly Dictionary<ConnectionsToyLogicalConnectionState, ConnectionsToyLogicalPathView> views =
            new Dictionary<ConnectionsToyLogicalConnectionState, ConnectionsToyLogicalPathView>();
        private readonly HashSet<ConnectionsToyPhysicalLinkView> warnedLinks = new HashSet<ConnectionsToyPhysicalLinkView>();

        public RectTransform NetworkArea => networkArea;
        public RectTransform PathLayer => pathLayer;
        public ConnectionsToyPathGraphic PathGraphicPrefab => pathPrefab != null ? pathPrefab.GetComponent<ConnectionsToyPathGraphic>() : null;
        public float MaxLineWidth => Mathf.Max(0.1f, maxLineWidth);
        public float MinLineGap => Mathf.Max(0f, minLineGap);
        public float MinLineWidth => Mathf.Max(0.1f, minLineWidth);

        public void Bind(ConnectionsToyLogicalConnectionState[] states)
        {
            if (networkArea == null || pathLayer == null || pathPrefab == null)
                return;
            foreach (ConnectionsToyLogicalPathView oldView in views.Values)
                if (oldView != null)
                    Destroy(oldView.gameObject);
            views.Clear();
            warnedLinks.Clear();
            connections = states;
            foreach (ConnectionsToyLogicalConnectionState connection in connections)
            {
                ConnectionsToyLogicalPathView view = Instantiate(pathPrefab, pathLayer, false);
                view.name = "Logical path " + connection.ConnectionId;
                view.Bind(connection, networkArea);
                views.Add(connection, view);
            }
            UpdateLayouts();
        }

        private void LateUpdate()
        {
            if (connections != null)
                UpdateLayouts();
        }

        // Recompute from active presentation state. Pending and departing paths hold no lane.
        private void UpdateLayouts()
        {
            var usersByLink = new Dictionary<ConnectionsToyPhysicalLinkResources,
                List<ConnectionsToyLogicalConnectionState>>();
            foreach (ConnectionsToyLogicalConnectionState connection in connections)
            {
                if (!connection.IsActive)
                    continue;
                foreach (ConnectionsToyPhysicalLinkResources link in connection.RouteLinks)
                {
                    if (!usersByLink.TryGetValue(link, out List<ConnectionsToyLogicalConnectionState> users))
                    {
                        users = new List<ConnectionsToyLogicalConnectionState>();
                        usersByLink.Add(link, users);
                    }
                    users.Add(connection);
                }
            }
            foreach (List<ConnectionsToyLogicalConnectionState> users in usersByLink.Values)
                users.Sort((a, b) => StringComparer.Ordinal.Compare(a.ConnectionId, b.ConnectionId));

            var currentlyOverflowed = new HashSet<ConnectionsToyPhysicalLinkView>();
            foreach (ConnectionsToyLogicalConnectionState connection in connections)
            {
                if (!connection.IsActive)
                    continue;
                float[] offsets = new float[connection.RouteLinks.Length];
                float[] widths = new float[offsets.Length];
                for (int edge = 0; edge < offsets.Length; edge++)
                {
                    ConnectionsToyPhysicalLinkResources link = connection.RouteLinks[edge];
                    ConnectionsToyPhysicalLinkView linkView = link.View;
                    List<ConnectionsToyLogicalConnectionState> users = usersByLink[link];
                    float budget = linkView.BundleWidth(networkArea);
                    float width = ConnectionsToyEdgeGeometry.LineWidth(budget, users.Count, minLineGap, maxLineWidth);
                    float offset = ConnectionsToyEdgeGeometry.Offset(users.IndexOf(connection), users.Count, width, minLineGap);
                    // The lane is stable in the physical edge's A-to-B frame, including reverse routes.
                    offsets[edge] = connection.Definition.Route[edge].Node == linkView.NodeA ? offset : -offset;
                    widths[edge] = width;
                    if (ConnectionsToyEdgeGeometry.BelowMinimum(budget, users.Count, minLineGap,
                        maxLineWidth, minLineWidth))
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
        }
    }
}
