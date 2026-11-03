using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // The authored Probe prefab uses the same screen-space coverage as physical links
    // and logical paths, including when its rectangle rotates with a diagonal edge.
    public sealed class ProbeRegionGraphic : AntialiasedLineGraphic
    {
        [SerializeField] private Color fillColor = new Color(0.25f, 0.48f, 0.62f, 0.16f);
        [SerializeField] private Color borderColor = new Color(0.39f, 0.69f, 0.82f, 0.52f);
        [SerializeField, Min(0.1f)] private float borderWidth = 2f;
        [Header("Visual sweep")]
        [SerializeField, Min(0.1f)] private float sweepPeriodSeconds = 2f;
        [SerializeField, Min(1f)] private float sweepWidth = 18f;
        [SerializeField, Range(0f, 1f)] private float sweepOpacity = 0.16f;
        [SerializeField, Range(0f, 1f)] private float leadingOpacity = 0.26f;
        [SerializeField, Min(0.1f)] private float leadingWidth = 1.5f;
        [SerializeField] private Color sweepColor = new Color(0.67f, 0.9f, 1f, 1f);
        [Header("Power on / shutdown")]
        [SerializeField, Min(0.01f)] private float activationDuration = 0.18f;
        [SerializeField, Range(0f, 1f)] private float activationOpacity = 0.12f;
        [SerializeField, Min(0.01f)] private float expirationFadeDuration = 0.22f;

        private enum VisualPhase { Active, ShuttingDown, Hidden }
        private VisualPhase phase = VisualPhase.Active;
        private float sweepAge;
        private float activationAge;
        private float shutdownAge;

        public bool IsShuttingDown => phase == VisualPhase.ShuttingDown;
        public bool IsActiveVisual => phase == VisualPhase.Active;
        private float SweepProgress => Mathf.Repeat(sweepAge /
            Mathf.Max(0.1f, sweepPeriodSeconds), 1f);
        public float SweepLocalY
        {
            get
            {
                Rect rect = rectTransform.rect;
                return Mathf.Lerp(rect.yMin, rect.yMax, SweepProgress);
            }
        }

        private void Update()
        {
            if (Application.isPlaying) AdvanceVisual(Time.deltaTime);
        }

        public void BeginActivation()
        {
            phase = VisualPhase.Active;
            sweepAge = 0f;
            activationAge = 0f;
            shutdownAge = 0f;
            SetVerticesDirty();
        }

        public void BeginShutdown()
        {
            if (phase != VisualPhase.Active) return;
            phase = VisualPhase.ShuttingDown;
            shutdownAge = 0f;
            SetVerticesDirty();
        }

        public void HideImmediately()
        {
            phase = VisualPhase.Hidden;
            gameObject.SetActive(false);
        }

        // Explicit stepping makes the cosmetic lifecycle deterministic in Editor witnesses.
        public void AdvanceVisual(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || phase == VisualPhase.Hidden) return;
            if (phase == VisualPhase.ShuttingDown)
            {
                shutdownAge += deltaSeconds;
                if (shutdownAge >= Mathf.Max(0.01f, expirationFadeDuration))
                {
                    HideImmediately();
                    return;
                }
            }
            else
            {
                activationAge += deltaSeconds;
                sweepAge += deltaSeconds;
            }
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (phase == VisualPhase.Hidden) return;
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            float opacity = phase == VisualPhase.ShuttingDown ?
                1f - Mathf.Clamp01(shutdownAge / Mathf.Max(0.01f, expirationFadeDuration)) : 1f;

            AddLine(mesh, new Vector2(rect.xMin, rect.center.y),
                new Vector2(rect.xMax, rect.center.y), rect.height,
                WithOpacity(fillColor * color, opacity));
            if (phase == VisualPhase.Active && activationAge < activationDuration)
            {
                Color flash = new Color(0.78f, 0.94f, 1f,
                    activationOpacity * (1f - activationAge / Mathf.Max(0.01f, activationDuration)));
                AddLine(mesh, new Vector2(rect.xMin, rect.center.y),
                    new Vector2(rect.xMax, rect.center.y), rect.height,
                    WithOpacity(flash * color, opacity));
            }
            if (phase == VisualPhase.Active) AddSweep(mesh, rect, opacity);
            float width = Mathf.Min(borderWidth, rect.width, rect.height);
            Color edge = WithOpacity(borderColor * color, opacity);
            AddLine(mesh, new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax), width, edge);
            AddLine(mesh, new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin), width, edge);
            AddLine(mesh, new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMin, rect.yMax), width, edge);
            AddLine(mesh, new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax), width, edge);
        }

        private void AddSweep(VertexHelper mesh, Rect rect, float opacity)
        {
            // Fade the band at the turnaround so repeating the pass does not pop.
            float phaseFade = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(Mathf.Min(SweepProgress, 1f - SweepProgress) * 8f));
            if (phaseFade <= 0f) return;
            float leading = SweepLocalY;
            float trail = Mathf.Max(1f, sweepWidth);
            float low = Mathf.Max(rect.yMin, leading - trail);
            float high = Mathf.Min(rect.yMax, leading);
            if (high > low)
            {
                float lowStrength = Mathf.Clamp01(1f - (leading - low) / trail);
                float highStrength = Mathf.Clamp01(1f - (leading - high) / trail);
                Color lowTint = sweepColor * color;
                Color highTint = lowTint;
                lowTint.a *= sweepOpacity * lowStrength * opacity * phaseFade;
                highTint.a *= sweepOpacity * highStrength * opacity * phaseFade;
                AddLine(mesh, new Vector2(rect.xMin, (low + high) * 0.5f),
                    new Vector2(rect.xMax, (low + high) * 0.5f), high - low,
                    lowTint, highTint);
            }
            float edgeHalf = Mathf.Max(0.1f, leadingWidth) * 0.5f;
            low = Mathf.Max(rect.yMin, leading - edgeHalf);
            high = Mathf.Min(rect.yMax, leading + edgeHalf);
            if (high > low)
            {
                Color edge = sweepColor * color;
                edge.a *= leadingOpacity * opacity * phaseFade;
                AddLine(mesh, new Vector2(rect.xMin, (low + high) * 0.5f),
                    new Vector2(rect.xMax, (low + high) * 0.5f), high - low, edge);
            }
        }

        private static Color WithOpacity(Color tint, float opacity)
        {
            tint.a *= opacity;
            return tint;
        }
    }
}
