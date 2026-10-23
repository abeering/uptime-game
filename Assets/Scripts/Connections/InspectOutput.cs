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
        [SerializeField, Min(1f)] private float activeEntryHeight = 188f;
        [SerializeField, Min(1f)] private float completeEntryHeight = 126f;
        [SerializeField, Min(1f)] private float compactEntryHeight = 94f;
        [SerializeField, Min(0f)] private float topPadding = 12f;
        [SerializeField, Min(0f)] private float entryGap = 12f;
        [SerializeField, Min(0f)] private float bottomPadding = 12f;

        private readonly List<InspectEntry> cards = new List<InspectEntry>();
        private LogicalConnectionState[] states;
        private ConnectionIdentityPalette palette;
        private InspectPresentationMode mode;
        private bool[] frozen;
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
            for (int i = 0; i < cards.Count; i++)
                cards[i].gameObject.SetActive(i < states.Length);
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

        private void Refresh()
        {
            FitWidth();
            bool allFrozen = true;
            for (int i = 0; i < states.Length; i++)
            {
                if (frozen[i])
                    continue;
                Color? accent = palette != null && palette.TryGet(states[i], out Color color) ?
                    color : (Color?)null;
                cards[i].Render(states[i], accent, mode);
                if (states[i].IsComplete)
                    frozen[i] = true;
                else
                    allFrozen = false;
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
            foreach (InspectEntry entry in cards)
                entry.SetCardWidth(cardWidth);
        }

        private void Reflow()
        {
            float y = topPadding;
            for (int i = 0; i < states.Length; i++)
            {
                float height = mode == InspectPresentationMode.Compact ? compactEntryHeight :
                    states[i].IsComplete ? completeEntryHeight : activeEntryHeight;
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
        }
    }
}
