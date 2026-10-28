using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Authored token prefab shared by the active, completed, and future timeline states.
    public sealed class InspectOperationToken : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private PillGraphic outline;
        [SerializeField] private GameObject checkmark;
        [SerializeField] private LayoutElement layout;
        [SerializeField, Min(0f)] private float horizontalPadding = 12f;
        [SerializeField, Min(0f)] private float checkmarkGap = 6f;
        [SerializeField] private Color completedColor = new Color(0.52f, 0.61f, 0.61f);
        [SerializeField] private Color futureColor = new Color(0.62f, 0.73f, 0.72f);

        public string Text => label != null ? label.text : string.Empty;
        public bool HasOutline => outline != null && outline.gameObject.activeSelf;
        public bool HasCheckmark => checkmark != null && checkmark.activeSelf;
        public Color OutlineColor => outline != null ? outline.color : Color.clear;

        private float CheckmarkSpace => checkmark != null ?
            ((RectTransform)checkmark.transform).rect.width + checkmarkGap : 0f;

        public float Measure(string text, bool completed = false) =>
            label != null ? Mathf.Ceil(label.GetPreferredValues(text).x +
                horizontalPadding * 2f + (completed ? CheckmarkSpace : 0f)) : 0f;

        public static string LoadLabel(float requested) => "LOAD " +
            requested.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        public static string OperationLabel(OperationKind operation, string target, bool shortLabel = false) =>
            operation.ToString().ToUpperInvariant() + (shortLabel ? string.Empty : " → " + target);

        public void ShowOperation(OperationKind operation, string target, bool completed, Color activeColor,
            bool shortLabel = false)
        {
            string text = OperationLabel(operation, target, shortLabel);
            Configure(text, completed ? completedColor : activeColor, completed);
            if (!completed) SetActiveColor(activeColor);
        }

        public void ShowCompletedCount(int count)
        {
            Configure("+" + count, completedColor, true);
        }

        public void ShowFuture(int count)
        {
            Configure("+" + count, futureColor, false);
        }

        public void ShowLoad(float requested, Color color)
        {
            Configure(LoadLabel(requested), color, false);
        }

        public void SetActiveColor(Color color)
        {
            if (!HasOutline) return;
            outline.SetOutline(color);
            if (label != null) label.color = color;
        }

        public void ConstrainWidth(float width)
        {
            if (layout != null) layout.preferredWidth = Mathf.Max(0f, width);
            if (label != null) label.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void Configure(string text, Color textColor, bool showCheck)
        {
            if (label == null || layout == null) return;
            label.text = text;
            label.color = textColor;
            if (outline != null)
            {
                outline.gameObject.SetActive(true);
                outline.SetOutline(textColor);
            }
            if (checkmark != null)
            {
                checkmark.SetActive(showCheck);
                var checkRect = (RectTransform)checkmark.transform;
                checkRect.anchoredPosition = new Vector2(horizontalPadding, 0f);
            }
            RectTransform labelRect = label.rectTransform;
            labelRect.offsetMin = new Vector2(horizontalPadding +
                (showCheck ? CheckmarkSpace : 0f), labelRect.offsetMin.y);
            labelRect.offsetMax = new Vector2(-horizontalPadding, labelRect.offsetMax.y);
            layout.preferredWidth = Measure(text, showCheck);
        }
    }
}
