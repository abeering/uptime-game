using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // The authored Probe prefab uses the same screen-space coverage as physical links
    // and logical paths, including when its rectangle rotates with a diagonal edge.
    public sealed class ProbeRegionGraphic : AntialiasedLineGraphic
    {
        [SerializeField] private Color fillColor = new Color(0.25f, 0.48f, 0.62f, 0.16f);
        [SerializeField] private Color borderColor = new Color(0.39f, 0.69f, 0.82f, 0.52f);
        [SerializeField, Min(0.1f)] private float borderWidth = 2f;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            AddLine(mesh, new Vector2(rect.xMin, rect.center.y),
                new Vector2(rect.xMax, rect.center.y), rect.height, fillColor * color);
            float width = Mathf.Min(borderWidth, rect.width, rect.height);
            Color edge = borderColor * color;
            AddLine(mesh, new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax), width, edge);
            AddLine(mesh, new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin), width, edge);
            AddLine(mesh, new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMin, rect.yMax), width, edge);
            AddLine(mesh, new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax), width, edge);
        }
    }
}
