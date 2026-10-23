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
        [SerializeField] private Color completedColor = new Color(0.52f, 0.61f, 0.61f);
        [SerializeField] private Color futureColor = new Color(0.62f, 0.73f, 0.72f);

        public string Text => label != null ? label.text : string.Empty;
        public bool HasOutline => outline != null && outline.gameObject.activeSelf;
        public bool HasCheckmark => checkmark != null && checkmark.activeSelf;
        public Color OutlineColor => outline != null ? outline.color : Color.clear;

        public float Measure(string text, float padding) =>
            label != null ? Mathf.Ceil(label.GetPreferredValues(text).x + padding) : 0f;

        public static string OperationLabel(OperationKind operation, string target, bool shortLabel = false) =>
            operation.ToString().ToUpperInvariant() + (shortLabel ? string.Empty : " → " + target);

        public void ShowOperation(OperationKind operation, string target, bool completed, Color activeColor,
            bool shortLabel = false)
        {
            string text = OperationLabel(operation, target, shortLabel);
            Configure(text, completed ? completedColor : activeColor, true, completed,
                completed ? 32f : 24f, completed ? 24f : 12f);
            if (!completed) SetActiveColor(activeColor);
        }

        public void ShowCompletedCount(int count)
        {
            Configure("+" + count, completedColor, true, true, 32f, 24f);
        }

        public void ShowFuture(int count)
        {
            Configure("+" + count, futureColor, true, false, 20f, 10f);
        }

        public void ShowLoad(float requested, Color color)
        {
            Configure($"LOAD {requested:0.#}", color, true, false, 20f, 10f);
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

        private void Configure(string text, Color textColor, bool showOutline, bool showCheck,
            float totalPadding, float leftInset)
        {
            if (label == null || layout == null) return;
            label.text = text;
            label.color = textColor;
            if (outline != null) outline.gameObject.SetActive(showOutline);
            if (showOutline && outline != null) outline.SetOutline(textColor);
            if (checkmark != null) checkmark.SetActive(showCheck);
            RectTransform labelRect = label.rectTransform;
            labelRect.offsetMin = new Vector2(leftInset, labelRect.offsetMin.y);
            labelRect.offsetMax = new Vector2(-(totalPadding - leftInset), labelRect.offsetMax.y);
            layout.preferredWidth = Mathf.Ceil(label.GetPreferredValues(text).x + totalPadding);
        }
    }
}
