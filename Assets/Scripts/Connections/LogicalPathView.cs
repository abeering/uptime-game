using UnityEngine;

namespace Uptime.Connections
{
    public sealed class LogicalPathView : MonoBehaviour
    {
        [SerializeField] private PathGraphic path;
        [SerializeField, Range(0.1f, 2f)] private float establishmentSeconds = 0.6f;
        [SerializeField, Range(0.1f, 2f)] private float departureSeconds = 0.45f;
        [SerializeField, Range(0.05f, 1f)] private float relayoutSeconds = 0.3f;
        [SerializeField] private Color healthyColor = new Color(0.36f, 0.95f, 0.74f);
        [SerializeField] private Color constrainedColor = new Color(1f, 0.69f, 0.34f);

        private LogicalConnectionState state;
        private RectTransform networkArea;
        private float[] laneOffsets;
        private float[] lineWidths;
        private float[] fromOffsets;
        private float[] fromWidths;
        private float[] targetOffsets;
        private float[] targetWidths;
        private float relayoutElapsed;
        private Vector2[] starts;
        private Vector2[] ends;
        private float establishmentElapsed;
        private float departureElapsed;
        private bool establishedVisual;

        public void Bind(LogicalConnectionState connection, RectTransform area)
        {
            state = connection;
            networkArea = area;
            int count = connection.RouteLinks.Length;
            starts = new Vector2[count];
            ends = new Vector2[count];
            path.raycastTarget = false;
            path.SetPath(starts, ends, new float[count], 0f);
        }

        public bool TryGetVisualSegment(int segment, out Vector2 start, out Vector2 end,
            out float visibleFraction, out Color lineColor)
        {
            start = end = Vector2.zero;
            visibleFraction = 0f;
            lineColor = path != null ? path.color : Color.white;
            if (!establishedVisual || segment < 0 || segment >= starts.Length)
                return false;
            start = starts[segment];
            end = ends[segment];
            float total = 0f;
            float before = 0f;
            for (int i = 0; i < starts.Length; i++)
            {
                float length = Vector2.Distance(starts[i], ends[i]);
                if (i < segment)
                    before += length;
                total += length;
            }
            float segmentLength = Vector2.Distance(start, end);
            if (segmentLength < 0.01f)
                return false;
            visibleFraction = Mathf.Clamp01((total * Mathf.Clamp01(establishmentElapsed / establishmentSeconds) - before) /
                segmentLength);
            return true;
        }

        public void SetLayout(float[] offsets, float[] widths)
        {
            if (targetOffsets == null)
            {
                laneOffsets = (float[])offsets.Clone();
                lineWidths = (float[])widths.Clone();
                fromOffsets = (float[])offsets.Clone();
                fromWidths = (float[])widths.Clone();
            }
            else
            {
                bool changed = false;
                for (int i = 0; i < offsets.Length; i++)
                    changed |= Mathf.Abs(targetOffsets[i] - offsets[i]) > 0.001f ||
                        Mathf.Abs(targetWidths[i] - widths[i]) > 0.001f;
                if (!changed)
                    return;
                fromOffsets = (float[])laneOffsets.Clone();
                fromWidths = (float[])lineWidths.Clone();
            }
            targetOffsets = (float[])offsets.Clone();
            targetWidths = (float[])widths.Clone();
            relayoutElapsed = 0f;
        }

        private void LateUpdate()
        {
            if (state == null || networkArea == null || path == null || state.IsPending || laneOffsets == null)
                return;

            if (!state.IsComplete)
            {
                relayoutElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(relayoutElapsed / relayoutSeconds));
                for (int i = 0; i < laneOffsets.Length; i++)
                {
                    laneOffsets[i] = Mathf.Lerp(fromOffsets[i], targetOffsets[i], t);
                    lineWidths[i] = Mathf.Lerp(fromWidths[i], targetWidths[i], t);
                }
            }

            if (!establishedVisual)
            {
                establishedVisual = true;
                establishmentElapsed = 0f;
            }

            if (state.IsComplete)
            {
                departureElapsed += Time.deltaTime;
                if (departureElapsed >= departureSeconds)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            else
                establishmentElapsed += Time.deltaTime;

            float reveal = Mathf.Clamp01(establishmentElapsed / establishmentSeconds);
            float fade = state.IsComplete ? 1f - Mathf.Clamp01(departureElapsed / departureSeconds) : 1f;
            Color tint = state.Satisfaction >= 0.98f ? healthyColor : constrainedColor;
            tint.a *= fade;
            if (path.color != tint)
                path.color = tint;

            for (int i = 0; i < starts.Length; i++)
            {
                Vector2 a = networkArea.InverseTransformPoint(state.Definition.Route[i].Node.transform.position);
                Vector2 b = networkArea.InverseTransformPoint(state.Definition.Route[i + 1].Node.transform.position);
                Vector2 direction = (b - a).normalized;
                starts[i] = EdgeGeometry.CardEdge(state.Definition.Route[i].Node,
                    networkArea, direction, laneOffsets[i]);
                ends[i] = EdgeGeometry.CardEdge(state.Definition.Route[i + 1].Node,
                    networkArea, -direction, -laneOffsets[i]);
            }
            path.SetPath(starts, ends, lineWidths, reveal);
        }

    }
}
