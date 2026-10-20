using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // Pixel geometry only. Resource demand and connection status never enter this calculation.
    public static class ConnectionsToyEdgeGeometry
    {
        public static Vector2 CardEdge(ConnectionsToyNodeView node, RectTransform area, Vector2 direction)
        {
            RectTransform card = (RectTransform)node.transform;
            Vector2 center = area.InverseTransformPoint(node.transform.position);
            Vector2 size = card.rect.size;
            float x = Mathf.Abs(direction.x) > 0.001f ? size.x * 0.5f / Mathf.Abs(direction.x) : float.PositiveInfinity;
            float y = Mathf.Abs(direction.y) > 0.001f ? size.y * 0.5f / Mathf.Abs(direction.y) : float.PositiveInfinity;
            return center + direction * (Mathf.Min(x, y) + 5f);
        }

        public static float LineWidth(float bundleWidth, int count, float gap, float maxLineWidth)
        {
            if (count <= 0)
                return 0f;
            return Mathf.Max(0f, Mathf.Min(maxLineWidth,
                (bundleWidth - (count - 1) * gap) / count));
        }

        public static float Offset(int index, int count, float lineWidth, float gap)
        {
            return (index - (count - 1) * 0.5f) * (lineWidth + gap);
        }

        public static bool BelowMinimum(float bundleWidth, int count, float gap,
            float maxLineWidth, float minLineWidth)
        {
            return count > 0 && LineWidth(bundleWidth, count, gap, maxLineWidth) < minLineWidth;
        }
    }
}
