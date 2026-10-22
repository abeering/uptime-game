using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Only transient UI geometry lives here. The simulation never reads this path.
    public sealed class PathGraphic : MaskableGraphic
    {
        private Vector2[] starts;
        private Vector2[] ends;
        private float[] widths;
        private float reveal;
        private float pulseSpacing;
        private float pulsePhase;
        private float pulseOpacity;

        public float PulseSpacing => pulseSpacing;
        public float PulsePhase => pulsePhase;
        public float PulseOpacity => pulseOpacity;

        public void SetActivity(float spacing, float phase, float opacity)
        {
            spacing = Mathf.Max(0f, spacing);
            opacity = Mathf.Clamp01(opacity);
            if (Mathf.Abs(pulseSpacing - spacing) < 0.01f &&
                Mathf.Abs(pulsePhase - phase) < 0.01f &&
                Mathf.Abs(pulseOpacity - opacity) < 0.01f)
                return;
            pulseSpacing = spacing;
            pulsePhase = phase;
            pulseOpacity = opacity;
            SetVerticesDirty();
        }

        public void SetPath(Vector2[] segmentStarts, Vector2[] segmentEnds,
            float[] segmentWidths, float visibleFraction)
        {
            float nextReveal = Mathf.Clamp01(visibleFraction);
            bool changed = starts == null || starts.Length != segmentStarts.Length ||
                Mathf.Abs(reveal - nextReveal) > 0.0005f;
            if (!changed)
                for (int i = 0; i < starts.Length; i++)
                    if ((starts[i] - segmentStarts[i]).sqrMagnitude > 0.01f ||
                        (ends[i] - segmentEnds[i]).sqrMagnitude > 0.01f ||
                        Mathf.Abs(widths[i] - segmentWidths[i]) > 0.001f)
                    {
                        changed = true;
                        break;
                    }
            if (!changed)
                return;

            starts = (Vector2[])segmentStarts.Clone();
            ends = (Vector2[])segmentEnds.Clone();
            widths = (float[])segmentWidths.Clone();
            reveal = nextReveal;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (starts == null || reveal <= 0f)
                return;

            float total = 0f;
            for (int i = 0; i < starts.Length; i++)
                total += Vector2.Distance(starts[i], ends[i]);
            float remaining = total * reveal;
            float distanceBefore = 0f;
            for (int i = 0; i < starts.Length && remaining > 0f; i++)
            {
                Vector2 direction = ends[i] - starts[i];
                float length = direction.magnitude;
                if (length < 0.01f)
                    continue;
                Vector2 end = starts[i] + direction * Mathf.Min(1f, remaining / length);
                if (widths[i] <= 0f)
                {
                    remaining -= length;
                    distanceBefore += length;
                    continue;
                }
                Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (widths[i] * 0.5f);
                int index = mesh.currentVertCount;
                Color32 tint = color;
                mesh.AddVert(starts[i] - normal, tint, Vector2.zero);
                mesh.AddVert(starts[i] + normal, tint, Vector2.zero);
                mesh.AddVert(end + normal, tint, Vector2.zero);
                mesh.AddVert(end - normal, tint, Vector2.zero);
                mesh.AddTriangle(index, index + 1, index + 2);
                mesh.AddTriangle(index, index + 2, index + 3);
                if (pulseSpacing > 1f && pulseOpacity > 0f)
                {
                    float visibleLength = Mathf.Min(length, remaining);
                    float first = pulsePhase + Mathf.Ceil((distanceBefore - pulsePhase) / pulseSpacing) * pulseSpacing;
                    Color pulseTint = Color.Lerp(color, Color.white, 0.75f);
                    pulseTint.a = color.a * pulseOpacity;
                    for (float position = first; position < distanceBefore + visibleLength; position += pulseSpacing)
                    {
                        float from = Mathf.Max(0f, position - distanceBefore);
                        float to = Mathf.Min(visibleLength, from + Mathf.Min(12f, pulseSpacing * 0.42f));
                        if (to <= from)
                            continue;
                        Vector2 pulseNormal = normal * 0.72f;
                        int pulseIndex = mesh.currentVertCount;
                        mesh.AddVert(starts[i] + direction.normalized * from - pulseNormal, pulseTint, Vector2.zero);
                        mesh.AddVert(starts[i] + direction.normalized * from + pulseNormal, pulseTint, Vector2.zero);
                        mesh.AddVert(starts[i] + direction.normalized * to + pulseNormal, pulseTint, Vector2.zero);
                        mesh.AddVert(starts[i] + direction.normalized * to - pulseNormal, pulseTint, Vector2.zero);
                        mesh.AddTriangle(pulseIndex, pulseIndex + 1, pulseIndex + 2);
                        mesh.AddTriangle(pulseIndex, pulseIndex + 2, pulseIndex + 3);
                    }
                }
                remaining -= length;
                distanceBefore += length;
            }
        }
    }
}
