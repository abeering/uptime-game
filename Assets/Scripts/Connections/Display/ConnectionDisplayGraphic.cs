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
                startOffsets[i] = endOffsets[i] = normal * Mathf.Max(2, segmentWidths[i] * .5f);
            }
            for (int i = 1; i < n; i++)
            {
                if ((segmentEnds[i - 1] - segmentStarts[i]).sqrMagnitude > .01f) continue;
                Vector2 a = endOffsets[i - 1].normalized, b = startOffsets[i].normalized;
                float denominator = 1 + Vector2.Dot(a, b);
                // Bound sharp miters; reversals use separate caps rather than exploding geometry.
                if (denominator < .15f) continue;
                Vector2 join = (a + b) / denominator * Mathf.Max(2, (segmentWidths[i - 1] + segmentWidths[i]) * .25f);
                endOffsets[i - 1] = startOffsets[i] = join;
            }
            GeometryRevision++; SetVerticesDirty();
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
            if (style == null || lengths == null || PathLength <= 0) { LastVertexCount = 0; return; }
            Color rail = style.Rail(state, beats), body = style.ClassBody(state.trafficClass);
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
            Strip(mesh, PathLength - 1.5f, PathLength, -1, 1, rail, rail);
            float spacing = style.Spacing(state.requestedLoad);
            int direction = LogicalPathView.DirectionFor(state.operation);
            double phase = travel;
            float first = (float)((phase % spacing + spacing) % spacing);
            Color activity = style.Activity(state.trafficClass);
            if (speed <= .0001f) activity.a = .42f;
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
            from = Mathf.Max(0, from); to = Mathf.Min(PathLength, to);
            if (to <= from) return;
            float before = 0;
            for (int i = 0; i < SegmentCount; i++)
            {
                float length = lengths[i];
                if (length > .001f && from < before + length && to > before)
                {
                    float t0 = Mathf.Clamp01((from - before) / length), t1 = Mathf.Clamp01((to - before) / length);
                    Vector2 p = Vector2.Lerp(segmentStarts[i], segmentEnds[i], t0), q = Vector2.Lerp(segmentStarts[i], segmentEnds[i], t1);
                    Vector2 a = Vector2.Lerp(startOffsets[i], endOffsets[i], t0), b = Vector2.Lerp(startOffsets[i], endOffsets[i], t1);
                    int k = mesh.currentVertCount;
                    mesh.AddVert(p + a * lower, low, Vector2.zero); mesh.AddVert(p + a * upper, high, Vector2.zero);
                    mesh.AddVert(q + b * upper, high, Vector2.zero); mesh.AddVert(q + b * lower, low, Vector2.zero);
                    mesh.AddTriangle(k, k + 1, k + 2); mesh.AddTriangle(k, k + 2, k + 3);
                }
                before += length;
                if (before >= to) break;
            }
        }
    }
}
