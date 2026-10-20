using UnityEngine;
using UnityEngine.UI;

namespace Uptime.ConnectionsToy
{
    // Only transient UI geometry lives here. The simulation never reads this path.
    public sealed class ConnectionsToyPathGraphic : MaskableGraphic
    {
        private Vector2[] starts;
        private Vector2[] ends;
        private float[] widths;
        private float reveal;

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
                remaining -= length;
            }
        }
    }
}
