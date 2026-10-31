using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // One independent, live terminal history panel. The allocator remains the source
    // of the node total and every connection's effective requested LOAD.
    public sealed class LoadOutput : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private LayoutElement layout;
        [SerializeField] private PillGraphic nodePill;
        [SerializeField] private TMP_Text nodeIdText;
        [SerializeField] private TMP_Text requestedText;
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private LoadRequestRow rowPrefab;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField, Min(0.05f)] private float refreshSeconds = 0.12f;
        [SerializeField, Min(1f)] private float rowHeight = 32f;
        [SerializeField, Min(0f)] private float rowGap = 4f;
        [SerializeField, Min(0.1f)] private float initialScaleCapacityMultiplier = 1.5f;
        [SerializeField, Min(1f)] private float scaleGrowthHeadroom = 1.15f;
        [SerializeField, Min(0f)] private float elevatedAtCapacityFraction = 0.5f;
        [SerializeField, Min(0f)] private float extremeAtCapacityFraction = 1f;
        [SerializeField] private Color nodeColor = new(0.35f, 0.92f, 0.72f);
        [SerializeField] private Color mutedIdentityColor = new(0.47f, 0.63f, 0.62f);
        [SerializeField] private Color modestColor = new(0.36f, 0.9f, 0.73f);
        [SerializeField] private Color elevatedColor = new(1f, 0.74f, 0.38f);
        [SerializeField] private Color extremeColor = new(0.96f, 0.29f, 0.26f);

        private readonly List<LogicalConnectionState> order = new();
        private readonly Dictionary<LogicalConnectionState, LoadRequestRow> rows = new();
        private LoadRequestRow otherRow;
        private SimulationHost host;
        private string nodeId;
        private float scale;
        private float nextRefresh;

        public int ConnectionRowCount => order.Count;
        public bool HasOtherRow => otherRow != null;
        public float CurrentScale => scale;

        public void Bind(string selectedNodeId, SimulationHost owner)
        {
            if (owner == null || owner.FindNodeResources(selectedNodeId) == null)
                throw new ArgumentException("LOAD panel needs a configured node and host.");
            if (host != null)
            {
                host.SimulationReset -= OnSimulationReset;
                ClearRows();
            }
            host = owner;
            nodeId = owner.FindNodeResources(selectedNodeId).View.NodeId;
            host.SimulationReset += OnSimulationReset;
            if (nodeIdText != null) nodeIdText.text = nodeId;
            nodePill?.SetOutline(nodeColor);
            scale = 0f;
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
            scale = 0f;
            // ResetSimulation raises this before replacing RuntimeConnections.
            // Wait until the next UI refresh to read the new run's states.
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
            var demanding = new List<LogicalConnectionState>();
            float visibleRequested = 0f;
            if (host.RuntimeConnections != null)
                foreach (LogicalConnectionState state in host.RuntimeConnections)
                    if (state.IsDemanding && state.TargetNode == node.View)
                    {
                        demanding.Add(state);
                        visibleRequested += state.DesiredLoad;
                    }
            // Remaining published node demand includes baseline, defense, and infections.
            // Its source is deliberately not disclosed by the diagnostic.
            float otherRequested = Mathf.Max(0f, node.RequestedLoad - visibleRequested);
            bool showOther = otherRequested >= 0.05f;

            for (int i = order.Count - 1; i >= 0; i--)
                if (!demanding.Contains(order[i]))
                {
                    RemoveRow(rows[order[i]]);
                    rows.Remove(order[i]);
                    order.RemoveAt(i);
                }
            if (order.Count == 0)
                demanding.Sort((a, b) => b.DesiredLoad.CompareTo(a.DesiredLoad));
            foreach (LogicalConnectionState state in demanding)
            {
                if (rows.ContainsKey(state)) continue;
                LoadRequestRow row = Instantiate(rowPrefab, rowsRoot);
                Color color = host.PresentationPalette != null &&
                    host.PresentationPalette.TryGet(state, out Color accent) ?
                    accent : mutedIdentityColor;
                // This is the same player-visible identity currently used by inspect.
                // Future concealment can change this one binding without exposing the
                // simulation ID through the accounting model.
                row.Bind(state.ConnectionId, color);
                order.Add(state);
                rows.Add(state, row);
            }
            if (showOther && otherRow == null)
            {
                otherRow = Instantiate(rowPrefab, rowsRoot);
                otherRow.Bind("OTHER", mutedIdentityColor);
            }
            else if (!showOther && otherRow != null)
            {
                RemoveRow(otherRow);
                otherRow = null;
            }

            float capacity = node.LoadCapacity;
            if (scale <= 0f) scale = Mathf.Max(1f, capacity * initialScaleCapacityMultiplier);
            foreach (LogicalConnectionState state in order)
                GrowScale(state.DesiredLoad);
            if (showOther) GrowScale(otherRequested);
            foreach (LogicalConnectionState state in order)
                rows[state].Render(state.DesiredLoad, scale, BarColor(state.DesiredLoad, capacity));
            if (otherRow != null)
                otherRow.Render(otherRequested, scale, BarColor(otherRequested, capacity));

            FitWidth();
            if (requestedText != null)
            {
                bool narrow = rowsRoot != null && rowsRoot.rect.width < 330f;
                requestedText.text = (narrow ? "REQ " : "REQUESTED ") +
                    Format(node.RequestedLoad) + (narrow ? " / CAP " : " / CAPACITY ") +
                    Format(capacity);
            }
            int count = order.Count + (otherRow != null ? 1 : 0);
            if (emptyText != null) emptyText.gameObject.SetActive(count == 0);
            float height = 102f + Mathf.Max(1, count) * rowHeight +
                Mathf.Max(0, count - 1) * rowGap;
            RectTransform root = (RectTransform)transform;
            if (Mathf.Abs(root.rect.height - height) > 0.01f)
            {
                Terminal terminal = GetComponentInParent<Terminal>();
                if (terminal != null) terminal.UpdateHistoryLayout(() => SetHeight(height));
                else SetHeight(height);
            }
        }

        private void GrowScale(float requested)
        {
            if (requested > scale)
                scale = requested * Mathf.Max(1f, scaleGrowthHeadroom);
        }

        private Color BarColor(float requested, float capacity)
        {
            float ratio = capacity > 0f ? requested / capacity :
                requested > 0f ? float.PositiveInfinity : 0f;
            return ratio >= extremeAtCapacityFraction ? extremeColor :
                ratio >= elevatedAtCapacityFraction ? elevatedColor : modestColor;
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
            if (requestedText != null)
            {
                requestedText.rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, Mathf.Max(0f, contentWidth - 84f));
                requestedText.fontSize = contentWidth < 330f ? 12f : 15f;
            }
            for (int i = 0; i < order.Count; i++)
                PlaceRow(rows[order[i]], i, contentWidth);
            if (otherRow != null) PlaceRow(otherRow, order.Count, contentWidth);
        }

        private void PlaceRow(LoadRequestRow row, int index, float width)
        {
            RectTransform rect = (RectTransform)row.transform;
            rect.anchoredPosition = new Vector2(0f, -index * (rowHeight + rowGap));
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);
            row.FitWidth(width);
        }

        private void SetHeight(float height)
        {
            RectTransform root = (RectTransform)transform;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (card != null)
                card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 12f);
            if (layout != null) layout.preferredHeight = height;
        }

        private void ClearRows()
        {
            foreach (LoadRequestRow row in rows.Values) RemoveRow(row);
            if (otherRow != null) RemoveRow(otherRow);
            rows.Clear();
            order.Clear();
            otherRow = null;
        }

        private static void RemoveRow(LoadRequestRow row)
        {
            if (row == null) return;
            row.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(row.gameObject);
            else DestroyImmediate(row.gameObject);
        }

        private static string Format(float value) =>
            value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
