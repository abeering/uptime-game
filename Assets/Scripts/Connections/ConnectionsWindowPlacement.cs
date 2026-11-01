using UnityEngine;

namespace Uptime.Connections
{
    // Two Connections surfaces, placed only on open. No input or Escape ownership.
    public sealed class ConnectionsWindowPlacement : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private ConnectionsVideoCall videoCall;
        [SerializeField] private ConnectionsCleanOverlay cleanOverlay;
        [SerializeField] private RectTransform[] callContent;
        [SerializeField] private RectTransform[] cleanContent;

        public void PlaceCall() => Place((RectTransform)videoCall.transform,
            cleanOverlay.IsOpen ? (RectTransform)cleanOverlay.transform : null, cleanContent, null);

        public void PlaceClean(NodeView target) => Place((RectTransform)cleanOverlay.transform,
            videoCall.IsActive ? (RectTransform)videoCall.transform : null, callContent, target);

        private void Place(RectTransform window, RectTransform other,
            RectTransform[] otherContent, NodeView target)
        {
            Canvas.ForceUpdateCanvases();
            Rect current = InViewport(window);
            Vector2 size = current.size;
            // Leave the permanent network titlebar visible. No corner is reserved for future UI.
            Rect bounds = Rect.MinMaxRect(viewport.rect.xMin + 12, viewport.rect.yMin + 12,
                viewport.rect.xMax - 12, viewport.rect.yMax - 52);
            Vector2 home = target != null ? (Vector2)viewport.InverseTransformPoint(target.transform.position) :
                new Vector2(viewport.rect.xMax - 24 - size.x / 2, viewport.rect.yMax - 64 - size.y / 2);
            Vector2[] candidates;
            if (target != null)
            {
                Vector2 nodeSize = InViewport((RectTransform)target.transform).size;
                float dx = (size.x + nodeSize.x) / 2 + 16;
                float dy = (size.y + nodeSize.y) / 2 + 16;
                candidates = new[] { home + new Vector2(dx, 0), home + new Vector2(-dx, 0),
                    home + new Vector2(0, dy), home + new Vector2(0, -dy), home,
                    home + new Vector2(dx, dy), home + new Vector2(-dx, dy),
                    home + new Vector2(dx, -dy), home + new Vector2(-dx, -dy),
                    bounds.min + size / 2, bounds.max - size / 2,
                    new Vector2(bounds.xMin + size.x / 2, bounds.yMax - size.y / 2),
                    new Vector2(bounds.xMax - size.x / 2, bounds.yMin + size.y / 2) };
            }
            else
            {
                // Home, then nearby vertical/horizontal alternatives, then viewport fallbacks.
                candidates = new[] { home, home + new Vector2(0, -size.y / 2),
                    home + new Vector2(-size.x / 2, 0), home + new Vector2(0, -size.y),
                    home + new Vector2(-size.x, 0), bounds.max - size / 2,
                    new Vector2(bounds.xMax - size.x / 2, bounds.yMin + size.y / 2),
                    new Vector2(bounds.xMin + size.x / 2, bounds.yMax - size.y / 2) };
            }

            Vector2 best = current.center;
            float bestScore = float.PositiveInfinity;
            foreach (Vector2 candidate in candidates)
            {
                Vector2 center = new(ClampCenter(candidate.x, bounds.xMin, bounds.xMax, size.x),
                    ClampCenter(candidate.y, bounds.yMin, bounds.yMax, size.y));
                Rect rect = new(center - size / 2, size);
                float score = (center - home).sqrMagnitude * .08f;
                if (other != null)
                {
                    // Margins/chrome can overlap cheaply. Useful content is much more expensive.
                    score += Overlap(rect, InViewport(other)) * .03f;
                    foreach (RectTransform existingPart in otherContent)
                        score += Overlap(rect, InViewport(existingPart)) * 10f;
                }
                if (score >= bestScore) continue; // Stable deterministic tie order.
                bestScore = score;
                best = center;
            }
            window.position += viewport.TransformVector(best - current.center);
        }

        private static float ClampCenter(float value, float min, float max, float size) =>
            max - min >= size ? Mathf.Clamp(value, min + size / 2, max - size / 2) : (min + max) / 2;

        private Rect InViewport(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = viewport.InverseTransformPoint(corners[0]);
            Vector2 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = viewport.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static float Overlap(Rect a, Rect b) =>
            Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) *
            Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
    }
}
