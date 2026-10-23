using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Uses the shared ConnectionsLine material. Each line stays a single quad;
    // its UVs describe the nominal rectangle for screen-space shader coverage.
    public class AntialiasedLineGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            AddLine(mesh, new Vector2(rect.xMin, rect.center.y),
                new Vector2(rect.xMax, rect.center.y), rect.height, color);
        }

        protected void AddLine(VertexHelper mesh, Vector2 start, Vector2 end,
            float width, Color tint)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.001f || width <= 0f)
                return;

            Vector2 direction = delta / length;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Vector2 center = (start + end) * 0.5f;
            Vector2 halfSize = new Vector2(length, width) * 0.5f;
            // Rasterize beyond the nominal edge so the shader's outer half of
            // the coverage ramp survives, including on diagonal pulse end caps.
            float padding = 1.5f / Mathf.Max(0.01f, canvas != null ? canvas.scaleFactor : 1f);
            Vector2 extent = halfSize + Vector2.one * padding;
            int index = mesh.currentVertCount;
            AddVertex(mesh, center, direction, normal, -extent.x, -extent.y, halfSize, tint);
            AddVertex(mesh, center, direction, normal, -extent.x, extent.y, halfSize, tint);
            AddVertex(mesh, center, direction, normal, extent.x, extent.y, halfSize, tint);
            AddVertex(mesh, center, direction, normal, extent.x, -extent.y, halfSize, tint);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddVertex(VertexHelper mesh, Vector2 center, Vector2 direction,
            Vector2 normal, float x, float y, Vector2 halfSize, Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = center + direction * x + normal * y;
            vertex.color = tint;
            vertex.uv0 = new Vector4(x, y, halfSize.x, halfSize.y);
            mesh.AddVert(vertex);
        }
    }
}
