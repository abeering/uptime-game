using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Each block owns its bindings. Destroying a history entry also stops its updates.
    public sealed class InspectOutput : MonoBehaviour
    {
        [SerializeField] private InspectEntry[] entries;
        [SerializeField] private LayoutElement layout;
        [SerializeField, Min(0.05f)] private float refreshSeconds = 0.2f;
        [SerializeField, Min(1f)] private float activeEntryHeight = 186f;
        [SerializeField, Min(1f)] private float completeEntryHeight = 124f;
        [SerializeField, Min(0f)] private float topPadding = 8f;
        [SerializeField, Min(0f)] private float entryGap = 10f;
        [SerializeField, Min(0f)] private float bottomPadding = 8f;

        private LogicalConnectionState[] states;
        private bool[] frozen;
        private float nextRefresh;

        public void Bind(LogicalConnectionState[] selected)
        {
            if (selected == null || selected.Length == 0 || selected.Length > entries.Length)
                return;
            states = selected;
            enabled = true;
            frozen = new bool[selected.Length];
            for (int i = 0; i < entries.Length; i++)
                entries[i].gameObject.SetActive(i < states.Length);
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
            bool allFrozen = true;
            for (int i = 0; i < states.Length; i++)
            {
                if (frozen[i])
                    continue;
                entries[i].Render(states[i]);
                if (states[i].IsComplete)
                    frozen[i] = true;
                else
                    allFrozen = false;
            }
            Reflow();
            if (allFrozen)
                enabled = false;
        }

        private void Reflow()
        {
            float y = topPadding;
            for (int i = 0; i < states.Length; i++)
            {
                float height = states[i].IsComplete ? completeEntryHeight : activeEntryHeight;
                RectTransform entry = (RectTransform)entries[i].transform;
                entry.anchoredPosition = new Vector2(entry.anchoredPosition.x, -y);
                entries[i].SetHeight(height);
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
            frozen = null;
        }
    }
}
