using UnityEngine;

namespace Uptime.Connections
{
    // Pixel geometry only. Resource demand and connection status never enter this calculation.
    public static class ConnectionsEdgeGeometry
    {
        public static Vector2 CardEdge(ConnectionsNodeView node, RectTransform area, Vector2 direction)
            => CardEdge(node, area, direction, 0f);

        public static Vector2 CardEdge(ConnectionsNodeView node, RectTransform area,
            Vector2 direction, float laneOffset)
            => CardEdge(CardRect(node, area), direction, laneOffset);

        public static Rect CardRect(ConnectionsNodeView node, RectTransform area)
        {
            var corners = new Vector3[4];
            ((RectTransform)node.transform).GetWorldCorners(corners);
            Vector2 min = area.InverseTransformPoint(corners[0]);
            Vector2 max = area.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // Intersect the offset lane itself with the card, rather than offsetting a single
        // center-line endpoint. Outer diagonal lanes otherwise stop short of the card.
        public static Vector2 CardEdge(Rect card, Vector2 direction, float laneOffset)
        {
            Vector2 along = direction.normalized;
            Vector2 across = new Vector2(-along.y, along.x);
            Vector2 origin = card.center + across * laneOffset;
            float x = Mathf.Abs(along.x) > 0.001f ?
                ((along.x > 0f ? card.xMax : card.xMin) - origin.x) / along.x :
                float.PositiveInfinity;
            float y = Mathf.Abs(along.y) > 0.001f ?
                ((along.y > 0f ? card.yMax : card.yMin) - origin.y) / along.y :
                float.PositiveInfinity;
            return origin + along * Mathf.Max(0f, Mathf.Min(x, y));
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
