using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // One independent, live terminal history panel for current-operation WORK.
    public sealed class WorkOutput : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private LayoutElement layout;
        [SerializeField] private PillGraphic nodePill;
        [SerializeField] private TMP_Text nodeIdText;
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private WorkRequestRow rowPrefab;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField, Min(0.05f)] private float refreshSeconds = 0.12f;
        [SerializeField, Min(1f)] private float rowHeight = 76f;
        [SerializeField, Min(0f)] private float rowGap = 4f;
        [SerializeField] private Color nodeColor = new(0.35f, 0.92f, 0.72f);
        [SerializeField] private Color mutedIdentityColor = new(0.47f, 0.63f, 0.62f);

        private readonly List<LogicalConnectionState> order = new();
        private readonly Dictionary<LogicalConnectionState, WorkRequestRow> rows = new();
        private SimulationHost host;
        private string nodeId;
        private float nextRefresh;

        public int ConnectionRowCount => order.Count;

        public void Bind(string selectedNodeId, SimulationHost owner)
        {
            NodeResources selected = owner != null ? owner.FindNodeResources(selectedNodeId) : null;
            if (selected == null)
                throw new ArgumentException("WORK panel needs a configured node and host.");
            if (host != null)
            {
                host.SimulationReset -= OnSimulationReset;
                ClearRows();
            }
            host = owner;
            nodeId = selected.View.NodeId;
            host.SimulationReset += OnSimulationReset;
            if (nodeIdText != null) nodeIdText.text = nodeId;
            nodePill?.SetOutline(nodeColor);
            Refresh();
            nextRefresh = Time.unscaledTime + refreshSeconds;
        }

        private void Update()
        {
            if (host == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshSeconds;
            Refresh();
        }

        private void OnSimulationReset()
        {
            ClearRows();
            // Reset fires before RuntimeConnections is replaced.
            nextRefresh = 0f;
        }

        private void OnDestroy()
        {
            if (host != null) host.SimulationReset -= OnSimulationReset;
        }

        private void Refresh()
        {
            NodeResources node = host?.FindNodeResources(nodeId);
            if (node == null) return;
            var current = new List<LogicalConnectionState>();
            if (host.RuntimeConnections != null)
                foreach (LogicalConnectionState state in host.RuntimeConnections)
                    if (state.TargetNode == node.View &&
                        (state.IsDemanding || (state.IsTransitioning && rows.ContainsKey(state))))
                        current.Add(state);

            for (int i = order.Count - 1; i >= 0; i--)
                if (!current.Contains(order[i]))
                {
                    RemoveRow(rows[order[i]]);
                    rows.Remove(order[i]);
                    order.RemoveAt(i);
                }
            foreach (LogicalConnectionState state in current)
            {
                if (rows.ContainsKey(state)) continue;
                WorkRequestRow row = Instantiate(rowPrefab, rowsRoot);
                Color accent = host.PresentationPalette != null &&
                    host.PresentationPalette.TryGet(state, out Color color) ?
                    color : mutedIdentityColor;
                row.Bind(state.ConnectionId, accent);
                order.Add(state);
                rows.Add(state, row);
            }
            FitWidth();
            foreach (LogicalConnectionState state in order)
                rows[state].Render(state);

            int count = order.Count;
            if (emptyText != null) emptyText.gameObject.SetActive(count == 0);
            float height = count == 0 ? 146f :
                114f + count * rowHeight + Mathf.Max(0, count - 1) * rowGap;
            RectTransform root = (RectTransform)transform;
            if (Mathf.Abs(root.rect.height - height) > 0.01f)
            {
                Terminal terminal = GetComponentInParent<Terminal>();
                if (terminal != null) terminal.UpdateHistoryLayout(() => SetHeight(height));
                else SetHeight(height);
            }
        }

        private void FitWidth()
        {
            RectTransform root = (RectTransform)transform;
            RectTransform parent = transform.parent as RectTransform;
            ScrollRect scroll = GetComponentInParent<ScrollRect>();
            float viewportWidth = scroll != null && scroll.viewport != null ?
                scroll.viewport.rect.width - 12f : 0f;
            float available = viewportWidth > 0f ? viewportWidth :
                parent != null && parent.rect.width > 0f ? parent.rect.width : root.rect.width;
            float width = Mathf.Min(580f, available);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (layout != null) layout.preferredWidth = width;
            float cardWidth = Mathf.Max(0f, width - 28f);
            if (card != null)
                card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardWidth);
            float contentWidth = Mathf.Max(0f, cardWidth - 32f);
            if (rowsRoot != null)
                rowsRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
            for (int i = 0; i < order.Count; i++)
            {
                WorkRequestRow row = rows[order[i]];
                RectTransform rect = (RectTransform)row.transform;
                rect.anchoredPosition = new Vector2(0f, -i * (rowHeight + rowGap));
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);
                row.FitWidth(contentWidth);
            }
        }

        private void SetHeight(float height)
        {
            RectTransform root = (RectTransform)transform;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (card != null)
                card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 24f);
            if (layout != null) layout.preferredHeight = height;
        }

        private void ClearRows()
        {
            foreach (WorkRequestRow row in rows.Values) RemoveRow(row);
            rows.Clear();
            order.Clear();
        }

        private static void RemoveRow(WorkRequestRow row)
        {
            if (row == null) return;
            row.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(row.gameObject);
            else DestroyImmediate(row.gameObject);
        }
    }
}
