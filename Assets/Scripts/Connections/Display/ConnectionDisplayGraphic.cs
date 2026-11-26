using System;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.Display
{
    // A reusable path mesh, independent of lifecycle and simulation. Geometry uses
    // the same segment starts/ends/widths contract as PathGraphic and lane packing.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ConnectionDisplayGraphic : MaskableGraphic
    {
        [SerializeField] private Vector2[] segmentStarts = { new Vector2(-190, 0) };
        [SerializeField] private Vector2[] segmentEnds = { new Vector2(190, 0) };
        [SerializeField] private float[] segmentWidths = { 12 };
        private Vector2[] startOffsets, endOffsets;
        private float[] lengths;
        private ConnectionDisplayState state;
        private ConnectionDisplayStyle style;
        private double beats, travel;
        private float executePulse;
        private float reveal = 1;
        private float unitsPerPixel = 1;
        private bool activityVisible = true;
        private int probeSegment = -1;
        private Vector2 probeCenter, probeAlong, probeHalfSize;
        private Color probeTint;
        private bool motionReady;
        private double previousBeats;
        private float previousSpeed, previousPhase;
        private int previousDirection, previousSubdivision = 1;
        public float PathLength { get; private set; }
        public double ActivityTravel => travel;
        public float ActivitySpacing => style != null ? style.Spacing(state.requestedLoad) : 0;
        public int GeometryRevision { get; private set; }
        public int LastVertexCount { get; private set; }
        public int SegmentCount => segmentStarts?.Length ?? 0;
        public ConnectionDisplayState State => state;

        protected override void OnEnable() { base.OnEnable(); RebuildGeometry(); raycastTarget = false; }
#if UNITY_EDITOR
        protected override void OnValidate() { base.OnValidate(); RebuildGeometry(); }
#endif
        public void SetGeometry(Vector2[] starts, Vector2[] ends, float[] widths)
        {
            if (starts == null || ends == null || widths == null || starts.Length != ends.Length || starts.Length != widths.Length)
                throw new ArgumentException("Path segments need matching starts, ends and widths.");
            bool changed = segmentStarts == null || segmentStarts.Length != starts.Length;
            for (int i = 0; !changed && i < starts.Length; i++)
                changed = (segmentStarts[i] - starts[i]).sqrMagnitude > .000001f ||
                    (segmentEnds[i] - ends[i]).sqrMagnitude > .000001f || Mathf.Abs(segmentWidths[i] - widths[i]) > .0001f;
            if (!changed) return;
            if (segmentStarts == null || segmentStarts.Length != starts.Length)
            { segmentStarts = new Vector2[starts.Length]; segmentEnds = new Vector2[starts.Length]; segmentWidths = new float[starts.Length]; }
            Array.Copy(starts, segmentStarts, starts.Length); Array.Copy(ends, segmentEnds, ends.Length);
            Array.Copy(widths, segmentWidths, widths.Length); RebuildGeometry();
        }
        private void RebuildGeometry()
        {
            int n = SegmentCount;
            if (n == 0 || segmentEnds == null || segmentWidths == null || segmentEnds.Length != n || segmentWidths.Length != n) return;
            if (lengths == null || lengths.Length != n)
            { lengths = new float[n]; startOffsets = new Vector2[n]; endOffsets = new Vector2[n]; }
            PathLength = 0;
            for (int i = 0; i < n; i++)
            {
                lengths[i] = Vector2.Distance(segmentStarts[i], segmentEnds[i]); PathLength += lengths[i];
                Vector2 direction = (segmentEnds[i] - segmentStarts[i]).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                startOffsets[i] = endOffsets[i] = normal * Mathf.Max(0, segmentWidths[i] * .5f);
            }
            for (int i = 1; i < n; i++)
            {
                if ((segmentEnds[i - 1] - segmentStarts[i]).sqrMagnitude > .01f) continue;
                Vector2 a = endOffsets[i - 1].normalized, b = startOffsets[i].normalized;
                float denominator = 1 + Vector2.Dot(a, b);
                // Bound sharp miters; reversals use separate caps rather than exploding geometry.
                if (denominator < .15f) continue;
                Vector2 join = (a + b) / denominator * Mathf.Max(0, (segmentWidths[i - 1] + segmentWidths[i]) * .25f);
                endOffsets[i - 1] = startOffsets[i] = join;
            }
            GeometryRevision++; SetVerticesDirty();
        }
        public void SetVisibility(float fraction, bool showActivity)
        {
            fraction = Mathf.Clamp01(fraction);
            if (Mathf.Abs(reveal - fraction) < .000001f && activityVisible == showActivity) return;
            reveal = fraction; activityVisible = showActivity; SetVerticesDirty();
        }
        public void SetProbe(int segment, Vector2 center, Vector2 along, Vector2 halfSize, Color tint)
        {
            if (probeSegment == segment && probeCenter == center && probeAlong == along && probeHalfSize == halfSize && probeTint == tint) return;
            probeSegment = segment; probeCenter = center; probeAlong = along;
            probeHalfSize = halfSize; probeTint = tint; SetVerticesDirty();
        }
        public Vector2 PointAt(float fraction)
        {
            float distance = Mathf.Clamp01(fraction) * PathLength;
            for (int i = 0; i < SegmentCount; i++)
            {
                if (distance <= lengths[i] || i == SegmentCount - 1)
                    return Vector2.Lerp(segmentStarts[i], segmentEnds[i], lengths[i] > 0 ? distance / lengths[i] : 0);
                distance -= lengths[i];
            }
            return Vector2.zero;
        }
        public void Render(ConnectionDisplayState input, ConnectionDisplayStyle visualStyle,
            ConnectionHeartbeat clock, float phaseBeats = 0, int subdivision = 1)
        {
            beats = clock.Beats;
            int direction = LogicalPathView.DirectionFor(input.operation);
            if (!motionReady || beats < previousBeats)
                travel = direction * clock.Travel(beats, input.productiveSpeed, phaseBeats, subdivision);
            else
                // Accumulate under the previous inputs up to this frame, then switch.
                // Rate, cadence and operation changes never reposition existing activity.
                travel += previousDirection * (clock.Travel(beats, previousSpeed, previousPhase, previousSubdivision) -
                    clock.Travel(previousBeats, previousSpeed, previousPhase, previousSubdivision));
            motionReady = true; previousBeats = beats; previousSpeed = input.productiveSpeed;
            previousDirection = direction; previousPhase = phaseBeats; previousSubdivision = subdivision;
            state = input; style = visualStyle;
            double stride = clock.stepDistance * state.productiveSpeed * clock.Cadence(state.productiveSpeed) / Mathf.Clamp(subdivision, 1, 4);
            double phase = stride > .0001 ? clock.Travel(beats, state.productiveSpeed, phaseBeats, subdivision) / stride % 2 : 0;
            executePulse = (float)(phase <= 1 ? phase : 2 - phase);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (style == null || lengths == null || PathLength <= 0 || reveal <= 0) { LastVertexCount = 0; return; }
            unitsPerPixel = 1 / Mathf.Max(.001f, canvas != null ? canvas.scaleFactor : 1);
            Color rail = style.Rail(state, beats), body = style.Body(state, beats);
            Strip(mesh, 0, PathLength, -1, 1, body, body);
            // One coherent full-route warning pulse; no spatial sweep suggests a bad physical segment.
            float speed = state.productiveSpeed;
            float borderFraction = Mathf.Clamp(style.railThickness / Mathf.Max(2, segmentWidths[0] * .5f), .1f, .6f);
            // Established connections stay continuous; disrupted activity communicates performance.
            Strip(mesh, 0, PathLength, -1, -1 + borderFraction, rail, rail);
            Strip(mesh, 0, PathLength, 1 - borderFraction, 1, rail, rail);
            Color transparent = rail; transparent.a = 0;
            Strip(mesh, 0, PathLength, -1.12f, -1, transparent, rail);
            Strip(mesh, 0, PathLength, 1, 1.12f, rail, transparent);
            // End caps keep the persistent rectangular body bounded even during stall.
            Strip(mesh, 0, 1.5f, -1, 1, rail, rail);
            Strip(mesh, PathLength * reveal - 1.5f, PathLength * reveal, -1, 1, rail, rail);
            if (!activityVisible) { LastVertexCount = mesh.currentVertCount; return; }
            float spacing = style.Spacing(state.requestedLoad);
            int direction = LogicalPathView.DirectionFor(state.operation);
            double phase = travel;
            float first = (float)((phase % spacing + spacing) % spacing);
            Color activity = style.Activity(state.trafficClass, speed <= .0001f);
            for (float s = first; s < PathLength; s += spacing)
            {
                float halfLength = style.blipLength * .5f;
                if (direction == 0)
                {
                    // EXECUTE breathes symmetrically in place on the same heartbeat.
                    halfLength *= .35f + .65f * executePulse;
                    if (speed <= .0001f) halfLength = style.blipLength * .3f;
                }
                float across = style.blipWidthFraction;
                Strip(mesh, s - halfLength, s + halfLength, -across, across, activity, activity);
                Color clear = activity; clear.a = 0;
                Strip(mesh, s - halfLength, s + halfLength, -across - .12f, -across, clear, activity);
                Strip(mesh, s - halfLength, s + halfLength, across, across + .12f, activity, clear);
            }
            LastVertexCount = mesh.currentVertCount;
        }
        private void Strip(VertexHelper mesh, float from, float to, float lower, float upper, Color low, Color high)
        {
            from = Mathf.Max(0, from); to = Mathf.Min(PathLength * reveal, to);
            if (to <= from) return;
            float before = 0;
            for (int i = 0; i < SegmentCount; i++)
            {
                float length = lengths[i];
                if (length > .001f && segmentWidths[i] > 0 && from < before + length && to > before)
                {
                    float t0 = Mathf.Clamp01((from - before) / length), t1 = Mathf.Clamp01((to - before) / length);
                    Vector2 p = Vector2.Lerp(segmentStarts[i], segmentEnds[i], t0), q = Vector2.Lerp(segmentStarts[i], segmentEnds[i], t1);
                    Vector2 a = Vector2.Lerp(startOffsets[i], endOffsets[i], t0), b = Vector2.Lerp(startOffsets[i], endOffsets[i], t1);
                    float enter = 0, exit = 1;
                    if (i == probeSegment && ClipProbe(p, q, ref enter, ref exit))
                    {
                        if (enter > 0) Quad(mesh, p, Vector2.Lerp(p, q, enter), a, Vector2.Lerp(a, b, enter), lower, upper, low, high);
                        Color scanLow = probeTint, scanHigh = probeTint;
                        scanLow.a *= low.a; scanHigh.a *= high.a;
                        Quad(mesh, Vector2.Lerp(p, q, enter), Vector2.Lerp(p, q, exit),
                            Vector2.Lerp(a, b, enter), Vector2.Lerp(a, b, exit), lower, upper, scanLow, scanHigh);
                        if (exit < 1) Quad(mesh, Vector2.Lerp(p, q, exit), q, Vector2.Lerp(a, b, exit), b, lower, upper, low, high);
                    }
                    else Quad(mesh, p, q, a, b, lower, upper, low, high);
                }
                before += length;
                if (before >= to) break;
            }
        }
        private void Quad(VertexHelper mesh, Vector2 p, Vector2 q, Vector2 a, Vector2 b,
            float lower, float upper, Color low, Color high)
        {
            int k = mesh.currentVertCount;
            mesh.AddVert(EdgePoint(p, a, lower), low, Vector2.zero); mesh.AddVert(EdgePoint(p, a, upper), high, Vector2.zero);
            mesh.AddVert(EdgePoint(q, b, upper), high, Vector2.zero); mesh.AddVert(EdgePoint(q, b, lower), low, Vector2.zero);
            mesh.AddTriangle(k, k + 1, k + 2); mesh.AddTriangle(k, k + 2, k + 3);
        }
        private Vector2 EdgePoint(Vector2 center, Vector2 offset, float fraction)
        {
            Vector2 point = center + offset * fraction;
            if (Mathf.Abs(fraction) > 1)
            {
                // Only subpixel outer rails need extra AA support. Normal lab widths
                // remain identical; core geometry, activity and packing widths never expand.
                float halfWidth = offset.magnitude;
                float extra = style.minimumRailFeatherPixels * unitsPerPixel - halfWidth * .5f -
                    halfWidth * (Mathf.Abs(fraction) - 1);
                if (extra > 0) point += offset.normalized * (Mathf.Sign(fraction) * extra);
            }
            return point;
        }
        private bool ClipProbe(Vector2 p, Vector2 q, ref float enter, ref float exit)
        {
            Vector2 across = new Vector2(-probeAlong.y, probeAlong.x), relative = p - probeCenter, delta = q - p;
            return ClipAxis(Vector2.Dot(relative, probeAlong), Vector2.Dot(delta, probeAlong), probeHalfSize.x, ref enter, ref exit) &&
                ClipAxis(Vector2.Dot(relative, across), Vector2.Dot(delta, across), probeHalfSize.y, ref enter, ref exit) && exit > enter;
        }
        private static bool ClipAxis(float origin, float delta, float extent, ref float enter, ref float exit)
        {
            if (Mathf.Abs(delta) < .000001f) return Mathf.Abs(origin) <= extent;
            float a = (-extent - origin) / delta, b = (extent - origin) / delta;
            if (a > b) (a, b) = (b, a);
            enter = Mathf.Max(enter, a); exit = Mathf.Min(exit, b); return enter <= exit;
        }
    }
}
