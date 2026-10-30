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
        private int rowCount = 1;

        public float Height => 42f + (rowCount - 1) * 21f;
        public RectTransform Rect => (RectTransform)transform;

        public void SetContent(string first, string second, Color accent,
            float? firstTimerProgress = null, float? secondTimerProgress = null)
            => SetRows(first, second, null, accent, firstTimerProgress,
                secondTimerProgress, null);

        public void SetRows(string first, string second, string third, Color accent,
            float? firstTimerProgress = null, float? secondTimerProgress = null,
            float? thirdTimerProgress = null)
        {
            rowCount = string.IsNullOrEmpty(third) ?
                string.IsNullOrEmpty(second) ? 1 : 2 : 3;
            firstRow.text = first;
            firstRow.color = accent;
            firstRow.rectTransform.anchoredPosition = new Vector2(12f,
                rowCount == 3 ? 21f : rowCount == 2 ? 10f : 0f);
            secondRow.gameObject.SetActive(rowCount > 1);
            if (rowCount > 1)
            {
                secondRow.text = rowCount == 3 ? second + "\n" + third : second;
                secondRow.color = accent;
                secondRow.rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical, rowCount == 3 ? 42f : 21f);
                secondRow.rectTransform.anchoredPosition = new Vector2(12f,
                    rowCount == 3 ? -10f : -11f);
            }
            plate.SetOutline(accent);
            accentRail.color = accent;
            float?[] timers = { firstTimerProgress, secondTimerProgress, thirdTimerProgress };
            Image[] images = { firstRadialTimer, secondRadialTimer };
            int imageIndex = 0;
            for (int i = 0; i < rowCount && imageIndex < images.Length; i++)
                if (timers[i].HasValue)
                    SetTimer(images[imageIndex++], timers[i], accent,
                        rowCount == 3 ? 21f - 21f * i : rowCount == 2 ? 10f - 21f * i : 0f);
            while (imageIndex < images.Length)
                SetTimer(images[imageIndex++], null, accent, 0f);
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
