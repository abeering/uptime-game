using System.Globalization;
using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    // Authored row prefab; the panel only binds earned simulation observations.
    public sealed class LogsEntryRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text operationText;
        [SerializeField] private TMP_Text activityText;
        [SerializeField] private TMP_Text resultText;

        public int Sequence { get; private set; } = -1;
        public TMP_Text TimeText => timeText;
        public TMP_Text OperationText => operationText;
        public TMP_Text ActivityText => activityText;
        public TMP_Text ResultText => resultText;

        public void Bind(EmittedConnectionLog entry)
        {
            if (Sequence == entry.Sequence) return;
            Sequence = entry.Sequence;
            int minutes = Mathf.FloorToInt(entry.TimeSeconds / 60f);
            float seconds = entry.TimeSeconds - minutes * 60f;
            if (timeText != null)
                timeText.text = minutes.ToString("00", CultureInfo.InvariantCulture) + ":" +
                    seconds.ToString("00.0", CultureInfo.InvariantCulture);
            if (operationText != null) operationText.text = entry.Operation.ToString().ToUpperInvariant();
            if (activityText != null) activityText.text = entry.Activity ?? string.Empty;
            if (resultText != null) resultText.text = entry.Result ?? string.Empty;
        }

        public void FitWidth(float width)
        {
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            bool stacked = width < 330f;
            bool showResult = !stacked || width >= 240f;
            if (resultText != null) resultText.gameObject.SetActive(showResult);
            Place(timeText, 0f, 0f, 80f, 24f);
            Place(operationText, 88f, 0f, 76f, 24f);
            if (stacked)
            {
                Place(activityText, 0f, -24f, width, 24f);
                if (showResult) Place(resultText, width - 62f, 0f, 62f, 24f);
            }
            else
            {
                Place(activityText, 172f, 0f, Mathf.Max(0f, width - 242f), 24f);
                Place(resultText, width - 62f, 0f, 62f, 24f);
            }
        }

        public void SetResolved(Color quiet)
        {
            if (timeText != null) timeText.color = quiet;
            if (operationText != null) operationText.color = quiet;
            if (activityText != null) activityText.color = quiet;
            if (resultText != null) resultText.color = quiet;
        }

        private static void Place(TMP_Text field, float x, float y, float width, float height)
        {
            if (field == null) return;
            RectTransform rect = field.rectTransform;
            rect.anchoredPosition = new Vector2(x, y);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}
