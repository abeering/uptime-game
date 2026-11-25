using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.Display
{
    // One SDF quad, not decorative child segments. TMP identity is authored above it.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ConnectionProgressPillGraphic : MaskableGraphic
    {
        [Range(0, 1)] public float progress;
        public PillProgressTreatment treatment;
        [Min(0)] public float radius = 7;
        [Min(.5f)] public float border = 2;
        public void Render(float workProgress, PillProgressTreatment mode, Color tint)
        { progress = Mathf.Clamp01(workProgress); treatment = mode; color = tint; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); Rect r = rectTransform.rect;
            float pad = 1.5f / Mathf.Max(.01f, canvas != null ? canvas.scaleFactor : 1);
            Vertex(mesh, new Vector2(r.xMin - pad, r.yMin - pad), r);
            Vertex(mesh, new Vector2(r.xMin - pad, r.yMax + pad), r);
            Vertex(mesh, new Vector2(r.xMax + pad, r.yMax + pad), r);
            Vertex(mesh, new Vector2(r.xMax + pad, r.yMin - pad), r);
            mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(0, 2, 3);
        }
        private void Vertex(VertexHelper mesh, Vector2 p, Rect rect)
        {
            var v = UIVertex.simpleVert; v.position = p; v.color = color;
            v.uv0 = new Vector4(p.x - rect.center.x, p.y - rect.center.y, rect.width * .5f, rect.height * .5f);
            v.uv1 = new Vector4(radius, border, progress, (int)treatment);
            mesh.AddVert(v);
        }
    }
}
