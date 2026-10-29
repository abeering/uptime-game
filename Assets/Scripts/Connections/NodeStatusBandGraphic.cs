using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Three exterior frames; the card center stays transparent and readable.
    public sealed class NodeStatusBandGraphic : MaskableGraphic
    {
        [SerializeField, Min(1f)] private float strongWidth = 4f;
        [SerializeField, Min(1f)] private float glowWidth = 7f;

        public void SetAccent(Color accent, float opacity)
        {
            accent.a *= Mathf.Clamp01(opacity);
            if (color != accent) color = accent;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            if (r.width < 12f || r.height < 12f) return;
            Color main = color;
            Color halo = new Color(main.r, main.g, main.b, main.a * .17f);
            Color fine = new Color(main.r, main.g, main.b, main.a * .6f);
            AddFrame(mesh, r, glowWidth, halo);
            AddFrame(mesh, Inset(r, glowWidth + 1f), strongWidth, main);
            AddFrame(mesh, Inset(r, glowWidth + strongWidth + 4f), 1.5f, fine);
        }

        private static Rect Inset(Rect r, float amount) => new Rect(
            r.xMin + amount, r.yMin + amount,
            Mathf.Max(0f, r.width - amount * 2f),
            Mathf.Max(0f, r.height - amount * 2f));

        private static void AddFrame(VertexHelper mesh, Rect r, float width, Color tint)
        {
            if (r.width <= width * 2f || r.height <= width * 2f) return;
            AddQuad(mesh, new Rect(r.xMin, r.yMax - width, r.width, width), tint);
            AddQuad(mesh, new Rect(r.xMin, r.yMin, r.width, width), tint);
            AddQuad(mesh, new Rect(r.xMin, r.yMin + width, width, r.height - width * 2f), tint);
            AddQuad(mesh, new Rect(r.xMax - width, r.yMin + width,
                width, r.height - width * 2f), tint);
        }

        private static void AddQuad(VertexHelper mesh, Rect r, Color tint)
        {
            int i = mesh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.color = tint;
            v.position = new Vector3(r.xMin, r.yMin); mesh.AddVert(v);
            v.position = new Vector3(r.xMin, r.yMax); mesh.AddVert(v);
            v.position = new Vector3(r.xMax, r.yMax); mesh.AddVert(v);
            v.position = new Vector3(r.xMax, r.yMin); mesh.AddVert(v);
            mesh.AddTriangle(i, i + 1, i + 2);
            mesh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
