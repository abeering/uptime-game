using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public sealed class NodeLoadBar : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image fill;
        [SerializeField, Min(0f)] private float elevatedAtFraction = 0.5f;
        [SerializeField, Min(0f)] private float overloadedAtFraction = 1f;
        [SerializeField] private Color ordinaryColor = new Color(0.36f, 0.9f, 0.73f);
        [SerializeField] private Color elevatedColor = new Color(1f, 0.74f, 0.38f);
        [SerializeField] private Color overloadedColor = new Color(0.96f, 0.29f, 0.26f);

        public string LabelText => label != null ? label.text : string.Empty;
        public float FillFraction => fill != null ? fill.rectTransform.anchorMax.x : 0f;
        public Color FillColor => fill != null ? fill.color : Color.clear;

        public void Render(NodeResources resources)
        {
            float requested = resources.RequestedLoad;
            float capacity = resources.LoadCapacity;
            string value = $"LOAD - {requested:0.#} / {capacity:0.#}";
            if (label != null && label.text != value)
                label.text = value;
            if (fill == null)
                return;

            float ratio = capacity > 0f ? requested / capacity : requested > 0f ? float.PositiveInfinity : 0f;
            float fraction = Mathf.Clamp01(ratio);
            RectTransform rect = fill.rectTransform;
            if (Mathf.Abs(rect.anchorMax.x - fraction) > 0.0005f)
                rect.anchorMax = new Vector2(fraction, rect.anchorMax.y);
            Color color = ratio > Mathf.Max(elevatedAtFraction, overloadedAtFraction) ? overloadedColor :
                ratio >= elevatedAtFraction ? elevatedColor : ordinaryColor;
            if (fill.color != color)
                fill.color = color;
        }
    }
}
