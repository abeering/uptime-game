using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public enum InspectPresentationMode { Compact, Verbose }

    // Each block owns its bindings. Destroying a history entry also stops its updates.
    public sealed class InspectOutput : MonoBehaviour
    {
        [SerializeField] private InspectEntry[] entries;
        [SerializeField] private LayoutElement layout;
        [SerializeField, Min(0.05f)] private float refreshSeconds = 0.2f;
        [SerializeField, Min(1f)] private float activeEntryHeight = 210f;
        [SerializeField, Min(1f)] private float completeEntryHeight = 128f;
        [SerializeField, Min(1f)] private float compactEntryHeight = 98f;
        [SerializeField, Min(0f)] private float resolvedHoldSeconds = 4f;
        [SerializeField, Min(1f)] private float resolvedRowHeight = 48f;
        [SerializeField, Min(0f)] private float topPadding = 12f;
        [SerializeField, Min(0f)] private float entryGap = 12f;
        [SerializeField, Min(0f)] private float bottomPadding = 12f;

        private readonly List<InspectEntry> cards = new List<InspectEntry>();
        private LogicalConnectionState[] states;
        private ConnectionIdentityPalette palette;
        private InspectPresentationMode mode;
        private bool[] frozen;
        private float[] collapseAt;
        private float nextRefresh;

        public void Bind(LogicalConnectionState[] selected,
            ConnectionIdentityPalette identityPalette = null,
            InspectPresentationMode presentationMode = InspectPresentationMode.Compact)
        {
            if (selected == null || selected.Length == 0 || entries == null || entries.Length == 0)
                return;
            if (cards.Count == 0)
                cards.AddRange(entries);
            while (cards.Count < selected.Length)
            {
                InspectEntry card = Instantiate(entries[0], transform);
                card.ClearRuntimeTokens();
                cards.Add(card);
            }
            states = selected;
            palette = identityPalette;
            mode = presentationMode;
            enabled = true;
            frozen = new bool[selected.Length];
            collapseAt = new float[selected.Length];
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].ShowFullCard();
                cards[i].gameObject.SetActive(i < states.Length);
            }
            Refresh();
            nextRefresh = Time.unscaledTime + refreshSeconds;
        }

        private void Update()
        {
            if (states == null || Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + refreshSeconds;
            Refresh();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (states == null || enabled) return;
            // Frozen history still needs one layout pass when the terminal width changes.
            nextRefresh = 0f;
            enabled = true;
        }

        private void Refresh()
        {
            RefreshPresentation(Time.unscaledTime);
        }

        private void RefreshPresentation(float now)
        {
            FitWidth();
            bool allFrozen = true;
            for (int i = 0; i < states.Length; i++)
            {
                if (!frozen[i])
                {
                    Color? accent = palette != null && palette.TryGet(states[i], out Color color) ?
                        color : (Color?)null;
                    cards[i].Render(states[i], accent, mode);
                    if (states[i].IsComplete)
                    {
                        frozen[i] = true;
                        collapseAt[i] = now + resolvedHoldSeconds;
                    }
                }
                if (frozen[i] && !cards[i].IsResolvedSummary && now >= collapseAt[i])
                    cards[i].ShowResolvedSummary(states[i]);
                if (!frozen[i] || now < collapseAt[i])
                    allFrozen = false;
                if (frozen[i] && !cards[i].IsResolvedSummary)
                    nextRefresh = Mathf.Min(nextRefresh, collapseAt[i]);
            }
            Reflow();
            if (allFrozen)
                enabled = false;
        }

        private void FitWidth()
        {
            RectTransform root = (RectTransform)transform;
            RectTransform parent = transform.parent as RectTransform;
            float available = parent != null && parent.rect.width > 0f ?
                Mathf.Min(580f, parent.rect.width) : root.rect.width;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, available);
            float cardWidth = Mathf.Max(0f, available - 28f);
            for (int i = 0; i < cards.Count; i++)
                cards[i].SetCardWidth(cardWidth,
                    i < states.Length && states[i].IsDemanding ? states[i].DesiredLoad : 0f);
        }

        private void Reflow()
        {
            bool changesHeight = false;
            for (int i = 0; i < states.Length; i++)
                if (Mathf.Abs(((RectTransform)cards[i].transform).rect.height - EntryHeight(i)) > 0.01f)
                    changesHeight = true;
            Terminal terminal = changesHeight ? GetComponentInParent<Terminal>() : null;
            if (terminal != null) terminal.UpdateHistoryLayout(ApplyReflow);
            else ApplyReflow();
        }

        private float EntryHeight(int i) => cards[i].IsResolvedSummary ? resolvedRowHeight :
            (mode == InspectPresentationMode.Compact ? compactEntryHeight :
                states[i].IsComplete ? completeEntryHeight : activeEntryHeight) + cards[i].ExtraHeight;

        private void ApplyReflow()
        {
            float y = topPadding;
            for (int i = 0; i < states.Length; i++)
            {
                float height = EntryHeight(i);
                RectTransform entry = (RectTransform)cards[i].transform;
                entry.anchoredPosition = new Vector2(14f, -y);
                cards[i].SetHeight(height);
                y += height + (i + 1 < states.Length ? entryGap : 0f);
            }
            float totalHeight = y + bottomPadding;
            if (layout != null)
                layout.preferredHeight = totalHeight;
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
        }

        private void OnDestroy()
        {
            states = null;
            palette = null;
            frozen = null;
            collapseAt = null;
        }
    }
}
