using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Small rounded UI plate with a real colored outline; authored on IdentityPill.prefab.
    public sealed class PillGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float cornerRadius = 7f;
        [SerializeField, Min(0f)] private float borderThickness = 2f;
        // Legacy fallback fill. The antialiased material owns the shared pill fill color.
        [SerializeField, HideInInspector] private Color fillColor =
            new Color(0.025f, 0.075f, 0.10f, 1f);

        public void SetOutline(Color outline)
        {
            if (color != outline)
                color = outline;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (material != null && material.shader != null &&
                material.shader.name == "Uptime/Connections/AntialiasedPill")
            {
                PopulateAntialiasedMesh(mesh);
                return;
            }
            PopulatePolygonMesh(mesh);
        }

        private void PopulateAntialiasedMesh(VertexHelper mesh)
        {
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            float radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
            float border = Mathf.Min(borderThickness, rect.width * 0.5f, rect.height * 0.5f);
            float padding = 1.5f / Mathf.Max(0.01f, canvas != null ? canvas.scaleFactor : 1f);
            Vector2 halfSize = rect.size * 0.5f;
            AddAntialiasedVertex(mesh, rect.center, new Vector2(rect.xMin - padding,
                rect.yMin - padding), halfSize, radius, border);
            AddAntialiasedVertex(mesh, rect.center, new Vector2(rect.xMin - padding,
                rect.yMax + padding), halfSize, radius, border);
            AddAntialiasedVertex(mesh, rect.center, new Vector2(rect.xMax + padding,
                rect.yMax + padding), halfSize, radius, border);
            AddAntialiasedVertex(mesh, rect.center, new Vector2(rect.xMax + padding,
                rect.yMin - padding), halfSize, radius, border);
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(0, 2, 3);
        }

        private void AddAntialiasedVertex(VertexHelper mesh, Vector2 center, Vector2 position,
            Vector2 halfSize, float radius, float border)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = new Vector4(position.x - center.x, position.y - center.y,
                halfSize.x, halfSize.y);
            vertex.uv1 = new Vector4(radius, border, 0f, 0f);
            mesh.AddVert(vertex);
        }

        private void PopulatePolygonMesh(VertexHelper mesh)
        {
            Rect outer = GetPixelAdjustedRect();
            if (outer.width <= 0f || outer.height <= 0f)
                return;
            float border = Mathf.Min(borderThickness, outer.width * 0.5f, outer.height * 0.5f);
            Rect inner = new Rect(outer.xMin + border, outer.yMin + border,
                outer.width - border * 2f, outer.height - border * 2f);
            float outerRadius = Mathf.Min(cornerRadius, outer.width * 0.5f, outer.height * 0.5f);
            float innerRadius = Mathf.Max(0f, outerRadius - border);
            const int corners = 4;
            const int stepsPerCorner = 5;
            int points = corners * stepsPerCorner;
            Color32 outlineColor = color;
            Color32 insideColor = fillColor;
            for (int i = 0; i < points; i++)
            {
                int corner = i / stepsPerCorner;
                int step = i % stepsPerCorner;
                float angle = (180f + corner * 90f + step * 90f / (stepsPerCorner - 1)) * Mathf.Deg2Rad;
                Vector2 unit = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outerCenter = CornerCenter(outer, outerRadius, corner);
                Vector2 innerCenter = CornerCenter(inner, innerRadius, corner);
                mesh.AddVert(outerCenter + unit * outerRadius, outlineColor, Vector2.zero);
                mesh.AddVert(innerCenter + unit * innerRadius, outlineColor, Vector2.zero);
                mesh.AddVert(innerCenter + unit * innerRadius, insideColor, Vector2.zero);
            }
            for (int i = 0; i < points; i++)
            {
                int next = (i + 1) % points;
                mesh.AddTriangle(i * 3, next * 3, next * 3 + 1);
                mesh.AddTriangle(i * 3, next * 3 + 1, i * 3 + 1);
            }
            int centerIndex = mesh.currentVertCount;
            mesh.AddVert(inner.center, insideColor, Vector2.zero);
            for (int i = 0; i < points; i++)
                mesh.AddTriangle(centerIndex, i * 3 + 2, ((i + 1) % points) * 3 + 2);
        }

        private static Vector2 CornerCenter(Rect rect, float radius, int corner)
        {
            switch (corner)
            {
                case 0: return new Vector2(rect.xMin + radius, rect.yMin + radius);
                case 1: return new Vector2(rect.xMax - radius, rect.yMin + radius);
                case 2: return new Vector2(rect.xMax - radius, rect.yMax - radius);
                default: return new Vector2(rect.xMin + radius, rect.yMax - radius);
            }
        }
    }
}
