using UnityEngine;

namespace Uptime.Connections
{
    // Authored socket graphics; the parent path only supplies position and accent.
    public sealed class TerminalMarkerStyle : MonoBehaviour
    {
        [SerializeField] private PillGraphic backing;
        [SerializeField] private PillGraphic ring;
        [SerializeField] private PillGraphic innerDot;
        [SerializeField, Min(12f)] private float preferredDiameter = 26f;
        [SerializeField, Min(4f)] private float ringDiameter = 20f;
        [SerializeField, Min(1f)] private float dotDiameter = 4f;
        [SerializeField, Min(0f)] private float borderOutset = 2f;
        [SerializeField] private Color backingColor = new Color(0.018f, 0.055f, 0.071f);
        [SerializeField, Range(0f, 1f)] private float dotOpacity = 0.8f;

        private float shownDiameter;
        private float fromDiameter;
        private float targetDiameter;
        private float resizeElapsed;
        private float resizeSeconds;

        public RectTransform rectTransform => (RectTransform)transform;
        public Color color => ring != null ? ring.color : Color.clear;
        public float PreferredDiameter => preferredDiameter;
        public float BorderOutset => borderOutset;
        public PillGraphic Backing => backing;
        public PillGraphic Ring => ring;
        public PillGraphic InnerDot => innerDot;

        public void SetMaximumDiameter(float maximum, float seconds)
        {
            float diameter = Mathf.Clamp(maximum, 12f, preferredDiameter);
            if (shownDiameter <= 0f)
                shownDiameter = preferredDiameter;
            if (Mathf.Approximately(targetDiameter, diameter))
                return;
            fromDiameter = shownDiameter;
            targetDiameter = diameter;
            resizeElapsed = 0f;
            resizeSeconds = Mathf.Max(0.01f, seconds);
            if (!Application.isPlaying)
                AdvanceVisual(resizeSeconds);
        }

        public void AdvanceVisual(float deltaSeconds)
        {
            if (shownDiameter <= 0f || Mathf.Approximately(shownDiameter, targetDiameter))
                return;
            resizeElapsed += Mathf.Max(0f, deltaSeconds);
            float t = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(resizeElapsed / resizeSeconds));
            shownDiameter = Mathf.Lerp(fromDiameter, targetDiameter, t);
            float scale = shownDiameter / preferredDiameter;
            rectTransform.sizeDelta = Vector2.one * shownDiameter;
            if (ring != null)
                ring.rectTransform.sizeDelta = Vector2.one * (ringDiameter * scale);
            if (innerDot != null)
                innerDot.rectTransform.sizeDelta = Vector2.one * (dotDiameter * scale);
        }

        public void SetAccent(Color accent)
        {
            if (backing != null)
                backing.SetOutline(new Color(backingColor.r, backingColor.g,
                    backingColor.b, accent.a));
            if (ring != null)
                ring.SetOutline(accent);
            if (innerDot != null)
                innerDot.SetOutline(new Color(accent.r, accent.g, accent.b,
                    accent.a * dotOpacity));
        }
    }
}
