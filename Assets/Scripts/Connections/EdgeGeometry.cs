using UnityEngine;

namespace Uptime.Connections
{
    // Pixel geometry only. Resource demand and connection status never enter this calculation.
    public static class EdgeGeometry
    {
        public static Vector2 CardEdge(NodeView node, RectTransform area, Vector2 direction)
            => CardEdge(node, area, direction, 0f);

        public static Vector2 CardEdge(NodeView node, RectTransform area,
            Vector2 direction, float laneOffset)
            => CardEdge(CardRect(node, area), direction, laneOffset);

        public static Rect CardRect(NodeView node, RectTransform area)
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

        // The path is drawn behind the card. Put both corners of its square end
        // beneath the card face so a diagonal line cannot leave a bright sliver
        // beside the card border.
        public static Vector2 CardUnderlap(NodeView node, RectTransform area,
            Vector2 direction, float laneOffset, float lineWidth)
            => CardUnderlap(CardRect(node, area), direction, laneOffset, lineWidth);

        public static Vector2 CardUnderlap(Rect card, Vector2 direction,
            float laneOffset, float lineWidth)
        {
            const float borderCover = 2f;
            Vector2 along = direction.normalized;
            Vector2 across = new Vector2(-along.y, along.x);
            Vector2 edge = CardEdge(card, direction, laneOffset);
            float halfWidth = Mathf.Max(0f, lineWidth) * 0.5f;
            float depth = 0f;
            if (along.x > 0.001f)
                depth = Mathf.Max(depth, (edge.x + Mathf.Abs(across.x) * halfWidth -
                    (card.xMax - borderCover)) / along.x);
            else if (along.x < -0.001f)
                depth = Mathf.Max(depth, ((card.xMin + borderCover) - edge.x +
                    Mathf.Abs(across.x) * halfWidth) / -along.x);
            if (along.y > 0.001f)
                depth = Mathf.Max(depth, (edge.y + Mathf.Abs(across.y) * halfWidth -
                    (card.yMax - borderCover)) / along.y);
            else if (along.y < -0.001f)
                depth = Mathf.Max(depth, ((card.yMin + borderCover) - edge.y +
                    Mathf.Abs(across.y) * halfWidth) / -along.y);
            return edge - along * Mathf.Max(0f, depth);
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
