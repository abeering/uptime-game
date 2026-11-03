using System;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Only transient UI geometry lives here. The simulation never reads this path.
    public sealed class PathGraphic : AntialiasedLineGraphic
    {
        private Vector2[] starts;
        private Vector2[] ends;
        private float[] widths;
        private float reveal;
        private float pulseSpacing;
        private float pulsePhase;
        private float pulseOpacity;
        private int probeSegment = -1;
        private Vector2 probeCenter;
        private Vector2 probeAlong;
        private Vector2 probeHalfSize;
        private Color probeTint;

        public float PulseSpacing => pulseSpacing;
        public float PulsePhase => pulsePhase;
        public float PulseOpacity => pulseOpacity;

        public void SetProbe(int segment, Vector2 center, Vector2 along,
            Vector2 halfSize, Color tint)
        {
            if (probeSegment == segment && (probeCenter - center).sqrMagnitude < 0.01f &&
                (probeAlong - along).sqrMagnitude < 0.00001f &&
                (probeHalfSize - halfSize).sqrMagnitude < 0.01f && probeTint == tint)
                return;
            probeSegment = segment;
            probeCenter = center;
            probeAlong = along;
            probeHalfSize = halfSize;
            probeTint = tint;
            SetVerticesDirty();
        }

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

            if (starts == null || starts.Length != segmentStarts.Length)
            {
                starts = new Vector2[segmentStarts.Length];
                ends = new Vector2[segmentStarts.Length];
                widths = new float[segmentStarts.Length];
            }
            Array.Copy(segmentStarts, starts, starts.Length);
            Array.Copy(segmentEnds, ends, ends.Length);
            Array.Copy(segmentWidths, widths, widths.Length);
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
                AddProbeLine(mesh, i, starts[i], end, widths[i], color);
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
                        AddProbeLine(mesh, i, starts[i] + direction / length * from,
                            starts[i] + direction / length * to, widths[i] * 0.72f, pulseTint);
                    }
                }
                remaining -= length;
                distanceBefore += length;
            }
        }

        // Clip each physical segment and its activity pulses in the scanner's rotated frame.
        // The ordinary tint survives unchanged on either side of the X-ray boundary.
        private void AddProbeLine(VertexHelper mesh, int segment, Vector2 start,
            Vector2 end, float width, Color ordinary)
        {
            if (segment != probeSegment || !ClipProbe(start, end, out float enter, out float exit))
            { AddLine(mesh, start, end, width, ordinary); return; }
            Vector2 delta = end - start;
            if (enter > 0.0001f)
                AddLine(mesh, start, start + delta * enter, width, ordinary);
            Color scanned = probeTint;
            scanned.a *= ordinary.a;
            AddLine(mesh, start + delta * enter, start + delta * exit, width, scanned);
            if (exit < 0.9999f)
                AddLine(mesh, start + delta * exit, end, width, ordinary);
        }

        private bool ClipProbe(Vector2 start, Vector2 end, out float enter, out float exit)
        {
            enter = 0f;
            exit = 1f;
            Vector2 across = new Vector2(-probeAlong.y, probeAlong.x);
            Vector2 relative = start - probeCenter;
            Vector2 delta = end - start;
            return ClipAxis(Vector2.Dot(relative, probeAlong), Vector2.Dot(delta, probeAlong),
                probeHalfSize.x, ref enter, ref exit) &&
                ClipAxis(Vector2.Dot(relative, across), Vector2.Dot(delta, across),
                    probeHalfSize.y, ref enter, ref exit) && exit > enter;
        }

        private static bool ClipAxis(float origin, float delta, float halfExtent,
            ref float enter, ref float exit)
        {
            if (Mathf.Abs(delta) < 0.000001f)
                return Mathf.Abs(origin) <= halfExtent;
            float a = (-halfExtent - origin) / delta;
            float b = (halfExtent - origin) / delta;
            if (a > b) { float swap = a; a = b; b = swap; }
            enter = Mathf.Max(enter, a);
            exit = Mathf.Min(exit, b);
            return enter <= exit;
        }
    }
}
