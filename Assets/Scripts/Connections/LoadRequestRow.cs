using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Authored row used by the live terminal LOAD diagnostic.
    public sealed class LoadRequestRow : MonoBehaviour
    {
        [SerializeField] private IdentityPillView identityPill;
        [SerializeField] private RectTransform track;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text valueText;
        [SerializeField, Min(0.01f)] private float responseSeconds = 0.12f;
        [SerializeField, Min(0f)] private float leftInset = 66f;
        [SerializeField, Min(0f)] private float valueWidth = 64f;
        [SerializeField, Min(0f)] private float gap = 10f;

        private float targetFraction;
        private bool hasRendered;
        private bool isOther;

        public string DisplayedId => identityPill != null ? identityPill.DisplayedId : string.Empty;
        public string DisplayedValue => valueText != null ? valueText.text : string.Empty;
        public float FillFraction => fill != null ? fill.rectTransform.anchorMax.x : 0f;

        public void Bind(string visibleId, Color identityColor)
        {
            isOther = visibleId == "OTHER";
            identityPill?.Bind(visibleId, identityColor);
            if (isOther && identityPill != null)
            {
                ((RectTransform)identityPill.transform).SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, 66f);
                TMP_Text label = identityPill.GetComponentInChildren<TMP_Text>();
                if (label != null) label.fontSize = 14f;
            }
        }

        public void FitWidth(float width)
        {
            RectTransform root = (RectTransform)transform;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            float inset = leftInset + (isOther ? 12f : 0f);
            if (track != null)
            {
                track.anchoredPosition = new Vector2(inset, track.anchoredPosition.y);
                track.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    Mathf.Max(0f, width - inset - valueWidth - gap));
            }
            if (valueText != null)
                valueText.rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, valueWidth);
        }

        public void Render(float requested, float scale, Color barColor)
        {
            requested = Mathf.Max(0f, requested);
            targetFraction = Mathf.Clamp01(requested / Mathf.Max(0.001f, scale));
            if (valueText != null)
            {
                string value = requested.ToString("0.0", CultureInfo.InvariantCulture);
                if (valueText.text != value) valueText.text = value;
            }
            if (fill != null && fill.color != barColor) fill.color = barColor;
            if (!Application.isPlaying || !hasRendered) SetFraction(targetFraction);
            hasRendered = true;
        }

        private void Update()
        {
            if (fill == null || Mathf.Abs(FillFraction - targetFraction) < 0.0005f)
                return;
            SetFraction(Mathf.MoveTowards(FillFraction, targetFraction,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, responseSeconds)));
        }

        private void SetFraction(float fraction)
        {
            RectTransform rect = fill.rectTransform;
            rect.anchorMax = new Vector2(fraction, rect.anchorMax.y);
        }
    }
}
