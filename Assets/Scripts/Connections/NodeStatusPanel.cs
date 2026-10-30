using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public sealed class NodeStatusPanel : MonoBehaviour
    {
        [SerializeField] private PillGraphic plate;
        [SerializeField] private Image accentRail;
        [SerializeField] private TMP_Text firstRow;
        [SerializeField] private TMP_Text secondRow;
        [SerializeField] private Image firstRadialTimer;
        [SerializeField] private Image secondRadialTimer;
        [SerializeField] private CanvasGroup group;

        public float Height => secondRow != null && secondRow.gameObject.activeSelf ? 63f : 42f;
        public RectTransform Rect => (RectTransform)transform;

        public void SetContent(string first, string second, Color accent,
            float? firstTimerProgress = null, float? secondTimerProgress = null)
        {
            firstRow.text = first;
            firstRow.color = accent;
            bool twoRows = !string.IsNullOrEmpty(second);
            firstRow.rectTransform.anchoredPosition = new Vector2(12f,
                twoRows ? 10f : 0f);
            secondRow.gameObject.SetActive(twoRows);
            if (twoRows)
            {
                secondRow.text = second;
                secondRow.color = accent;
            }
            plate.SetOutline(accent);
            accentRail.color = accent;
            SetTimer(firstRadialTimer, firstTimerProgress, accent,
                twoRows ? 10f : 0f);
            SetTimer(secondRadialTimer, twoRows ? secondTimerProgress : null,
                accent, -11f);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Height);
            accentRail.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Height - 8f);
        }

        private static void SetTimer(Image timer, float? progress, Color accent, float y)
        {
            if (timer == null) return;
            timer.gameObject.SetActive(progress.HasValue);
            if (!progress.HasValue) return;
            timer.rectTransform.anchoredPosition = new Vector2(77f, y);
            timer.color = accent;
            timer.fillAmount = Mathf.Clamp01(progress.Value);
        }

        public void SetOpacity(float opacity)
        {
            if (group != null) group.alpha = Mathf.Clamp01(opacity);
        }
    }
}
