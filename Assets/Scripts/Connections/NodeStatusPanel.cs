using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public sealed class NodeStatusPanel : MonoBehaviour
    {
        [SerializeField] private PillGraphic plate;
        [SerializeField] private Image accentRail;
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private TMP_Text[] timers;
        [SerializeField] private CanvasGroup group;
        private int rowCount = 1;

        public float Height => 12f + rowCount * 22f;
        public RectTransform Rect => (RectTransform)transform;

        public void SetContent(string first, string second, Color accent,
            float? firstRemainingSeconds = null, float? secondRemainingSeconds = null)
            => SetRows(first, second, null, accent, firstRemainingSeconds,
                secondRemainingSeconds, null);

        public void SetRows(string first, string second, string third, Color accent,
            float? firstRemainingSeconds = null, float? secondRemainingSeconds = null,
            float? thirdRemainingSeconds = null)
        {
            rowCount = string.IsNullOrEmpty(third) ?
                string.IsNullOrEmpty(second) ? 1 : 2 : 3;
            string[] text = { first, second, third };
            float?[] remaining = { firstRemainingSeconds, secondRemainingSeconds,
                thirdRemainingSeconds };
            for (int i = 0; i < 3; i++)
            {
                bool visible = i < rowCount;
                labels[i].gameObject.SetActive(visible);
                timers[i].gameObject.SetActive(visible && remaining[i].HasValue);
                if (!visible) continue;
                float y = (rowCount - 1) * 11f - i * 22f;
                labels[i].rectTransform.anchoredPosition = new Vector2(16f, y);
                labels[i].text = text[i];
                labels[i].color = accent;
                timers[i].rectTransform.anchoredPosition = new Vector2(-14f, y);
                if (remaining[i].HasValue)
                {
                    timers[i].text = Mathf.CeilToInt(Mathf.Max(0f,
                        remaining[i].Value)).ToString() + "s";
                    timers[i].color = accent;
                }
            }
            plate.SetOutline(accent);
            accentRail.color = accent;
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Height);
            accentRail.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Height - 8f);
        }

        public void SetOpacity(float opacity)
        {
            if (group != null) group.alpha = Mathf.Clamp01(opacity);
        }
    }
}
