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
            Place(timeText, 0f, 66f);
            Place(operationText, 70f, 66f);
            Place(activityText, 140f, Mathf.Max(20f, width - 210f));
            Place(resultText, Mathf.Max(160f, width - 64f), 64f);
        }

        private static void Place(TMP_Text field, float x, float width)
        {
            if (field == null) return;
            RectTransform rect = field.rectTransform;
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
    }
}
