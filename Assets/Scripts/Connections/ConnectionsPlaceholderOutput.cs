using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public sealed class ConnectionsPlaceholderOutput : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text detail;
        [SerializeField] private TMP_Text firstTag;
        [SerializeField] private TMP_Text secondTag;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private Image progressFill;

        public void Bind(string heading, string body, string tagA, string tagB, float progress)
        {
            float amount = Mathf.Clamp01(progress);
            if (title != null) title.text = heading;
            if (detail != null) detail.text = body;
            if (firstTag != null) firstTag.text = tagA;
            if (secondTag != null) secondTag.text = tagB;
            if (progressLabel != null) progressLabel.text = $"{amount * 100f:0}%";
            if (progressFill != null)
            {
                RectTransform fill = progressFill.rectTransform;
                fill.anchorMax = new Vector2(amount, 1f);
            }
        }
    }
}
