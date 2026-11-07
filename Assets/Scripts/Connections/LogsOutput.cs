using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // One history card bound to one connection instance, never to a reusable ID lookup.
    public sealed class LogsOutput : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private LayoutElement layout;
        [SerializeField] private RectTransform fullContent;
        [SerializeField] private CanvasGroup fullAlpha;
        [SerializeField] private TMP_Text identityText;
        [SerializeField] private TMP_Text contextText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text tailText;
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private LogsEntryRow rowPrefab;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private CanvasGroup summaryAlpha;
        [SerializeField, Min(.05f)] private float refreshSeconds = .12f;
        [SerializeField, Min(0f)] private float resolvedHoldSeconds = 4f;
        [SerializeField, Min(.01f)] private float collapseSeconds = .22f;
        [SerializeField, Min(1f)] private float baseHeight = 70f;
        [SerializeField, Min(1f)] private float rowHeight = 23f;
        [SerializeField, Min(1f)] private float summaryHeight = 48f;
        [SerializeField] private Color activeColor = new(.35f, .92f, .72f);
        [SerializeField] private Color quietColor = new(.47f, .63f, .62f);

        private readonly List<LogsEntryRow> rows = new();
        private LogicalConnectionState state;
        private SimulationHost host;
        private int tailCount;
        private int lastEmittedCount = -1;
        private string lastIdentity;
        private bool historical;
        private bool collapsed;
        private float collapseAt = -1f;
        private float collapseFromHeight;
        private float nextRefresh;
        private float contentWidth;

        public int DisplayedRowCount => rows.Count;
        public int TailCount => tailCount;
        public bool IsSummary => collapsed;
        public bool IsHistoricalSnapshot => historical;

        public void Bind(LogicalConnectionState selected, SimulationHost owner, int requestedTail)
        {
            if (selected == null || owner == null || requestedTail < 1)
                throw new ArgumentException("LOGS panel needs a connection, host, and positive tail.");
            if (host != null) host.SimulationReset -= OnSimulationReset;
            ClearRows();
            state = selected;
            host = owner;
            tailCount = requestedTail;
            historical = selected.IsComplete;
            collapsed = false;
            collapseAt = -1f;
            lastEmittedCount = -1;
            lastIdentity = null;
            host.SimulationReset += OnSimulationReset;
            if (fullContent != null) fullContent.gameObject.SetActive(true);
            if (summaryText != null) summaryText.gameObject.SetActive(false);
            if (fullAlpha != null) fullAlpha.alpha = 1f;
            if (summaryAlpha != null) summaryAlpha.alpha = 0f;
            RefreshNow(Time.unscaledTime);
            nextRefresh = Time.unscaledTime + refreshSeconds;
        }

        private void Update()
        {
            if (state == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshSeconds;
            RefreshNow(Time.unscaledTime);
        }

        // Kept callable by Editor witnesses; refresh does not advance simulation.
        public void RefreshNow(float now)
        {
            if (state == null || collapsed) return;
            float width = FitWidth();
            string identity = state.KnownIdentity.ToUpperInvariant();
            if (identity != lastIdentity)
            {
                lastIdentity = identity;
                if (identityText != null) identityText.text = identity;
            }
            bool resolved = state.IsComplete;
            string outcome = resolved ? state.Resolution?.ToString().ToUpperInvariant() ?? "COMPLETE" : "LIVE";
            if (statusText != null) statusText.text = outcome;
            if (statusText != null) statusText.color = resolved ? quietColor : activeColor;
            if (identityText != null)
            {
                Color accent = host.PresentationPalette != null &&
                    host.PresentationPalette.TryGet(state, out Color assigned) ? assigned : activeColor;
                identityText.color = resolved ? quietColor : accent;
            }
            if (contextText != null)
                contextText.text = state.TargetNode.NodeId + " / " + state.Definition.ClaimedPurpose;
            if (tailText != null) tailText.text = "TAIL " + tailCount;
            if (summaryText != null) summaryText.text = identity + "  /  LOGS  /  " + outcome;

            int emittedCount = state.EmittedLogs.Count;
            int visible = Mathf.Min(tailCount, emittedCount);
            bool rowCountChanged = rows.Count != visible;
            while (rows.Count < visible)
                rows.Add(Instantiate(rowPrefab, rowsRoot));
            while (rows.Count > visible)
            {
                LogsEntryRow removed = rows[rows.Count - 1];
                rows.RemoveAt(rows.Count - 1);
                RemoveRow(removed);
            }
            if (emittedCount != lastEmittedCount || rowCountChanged)
            {
                int first = emittedCount - visible;
                for (int i = 0; i < visible; i++)
                    rows[i].Bind(state.EmittedLogs[first + i]);
                lastEmittedCount = emittedCount;
            }
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(visible == 0);
                emptyText.text = state.IsPending ? "WAITING FOR CONNECTION" :
                    state.IsConnecting ? "CONNECTING" : "NO LOGS EMITTED";
            }
            if (rowCountChanged || Mathf.Abs(contentWidth - width) > .01f)
                FitRows(width);
            float fullHeight = baseHeight + visible * rowHeight;
            if (resolved && !historical && collapseAt < 0f)
            {
                collapseAt = now + resolvedHoldSeconds;
                collapseFromHeight = fullHeight;
            }
            if (collapseAt >= 0f && now >= collapseAt)
            {
                float progress = Mathf.Clamp01((now - collapseAt) / Mathf.Max(.01f, collapseSeconds));
                if (summaryText != null) summaryText.gameObject.SetActive(true);
                if (fullAlpha != null) fullAlpha.alpha = 1f - progress;
                if (summaryAlpha != null) summaryAlpha.alpha = progress;
                SetHeightAnchored(Mathf.Lerp(collapseFromHeight, summaryHeight,
                    Mathf.SmoothStep(0f, 1f, progress)));
                if (progress >= 1f)
                {
                    collapsed = true;
                    if (fullContent != null) fullContent.gameObject.SetActive(false);
                    enabled = false;
                }
            }
            else SetHeightAnchored(fullHeight);
        }

        private float FitWidth()
        {
            RectTransform root = (RectTransform)transform;
            RectTransform parent = transform.parent as RectTransform;
            ScrollRect scroll = GetComponentInParent<ScrollRect>();
            float available = parent != null && parent.rect.width > 0f ? parent.rect.width :
                root.rect.width;
            if (scroll != null && scroll.viewport != null && scroll.viewport.rect.width > 12f)
                available = Mathf.Min(available, scroll.viewport.rect.width - 12f);
            float width = Mathf.Min(580f, available);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (layout != null) layout.preferredWidth = width;
            float cardWidth = Mathf.Max(0f, width - 14f);
            if (card != null) card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardWidth);
            if (fullContent != null)
                fullContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardWidth);
            float inner = Mathf.Max(0f, cardWidth - 32f);
            if (rowsRoot != null)
                rowsRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, inner);
            if (contextText != null)
            {
                contextText.gameObject.SetActive(cardWidth >= 400f);
                contextText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    Mathf.Max(0f, cardWidth - 278f));
            }
            if (statusText != null)
                statusText.rectTransform.anchoredPosition = new Vector2(cardWidth - 172f, -10f);
            if (tailText != null)
                tailText.rectTransform.anchoredPosition = new Vector2(cardWidth - 84f, -10f);
            return inner;
        }

        private void FitRows(float width)
        {
            contentWidth = width;
            for (int i = 0; i < rows.Count; i++)
            {
                RectTransform rect = (RectTransform)rows[i].transform;
                rect.anchoredPosition = new Vector2(0f, -i * rowHeight);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);
                rows[i].FitWidth(width);
            }
        }

        private void SetHeightAnchored(float height)
        {
            RectTransform root = (RectTransform)transform;
            if (Mathf.Abs(root.rect.height - height) < .01f) return;
            Terminal terminal = GetComponentInParent<Terminal>();
            if (terminal != null) terminal.UpdateHistoryLayout(() => SetHeight(height));
            else SetHeight(height);
        }

        private void SetHeight(float height)
        {
            RectTransform root = (RectTransform)transform;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (card != null)
                card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 12f);
            if (layout != null) layout.preferredHeight = height;
        }

        private void OnSimulationReset()
        {
            // Editor history eviction can destroy an instantiated card before its
            // queued callback is removed. Never dereference a dead Unity object.
            if (!this)
            {
                if (host != null) host.SimulationReset -= OnSimulationReset;
                return;
            }
            host.SimulationReset -= OnSimulationReset;
            host = null;
            state = null;
            ClearRows();
            if (fullContent != null) fullContent.gameObject.SetActive(false);
            if (summaryText != null)
            {
                summaryText.text = "LOGS  /  RUN RESET";
                summaryText.gameObject.SetActive(true);
            }
            if (summaryAlpha != null) summaryAlpha.alpha = 1f;
            collapsed = true;
            SetHeightAnchored(summaryHeight);
            enabled = false;
        }

        private void OnDestroy()
        {
            if (host != null) host.SimulationReset -= OnSimulationReset;
        }

        private void ClearRows()
        {
            foreach (LogsEntryRow row in rows) RemoveRow(row);
            rows.Clear();
        }

        private static void RemoveRow(LogsEntryRow row)
        {
            if (row == null) return;
            row.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(row.gameObject);
            else DestroyImmediate(row.gameObject);
        }
    }
}
