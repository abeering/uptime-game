using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public enum NodeStatusAnchor { N, NE, E, SE, S, SW, W, NW }

    public static class NodeStatusPlacement
    {
        private const float Gap = 17f;

        public static Rect At(NodeStatusAnchor anchor, Rect node, Vector2 size)
        {
            float x = node.center.x - size.x * .5f;
            float y = node.center.y - size.y * .5f;
            switch (anchor)
            {
                case NodeStatusAnchor.N: y = node.yMax + Gap; break;
                case NodeStatusAnchor.NE:
                    x = node.xMax + Gap; y = node.yMax + Gap; break;
                case NodeStatusAnchor.E: x = node.xMax + Gap; break;
                case NodeStatusAnchor.SE:
                    x = node.xMax + Gap; y = node.yMin - Gap - size.y; break;
                case NodeStatusAnchor.S: y = node.yMin - Gap - size.y; break;
                case NodeStatusAnchor.SW:
                    x = node.xMin - Gap - size.x; y = node.yMin - Gap - size.y; break;
                case NodeStatusAnchor.W: x = node.xMin - Gap - size.x; break;
                case NodeStatusAnchor.NW:
                    x = node.xMin - Gap - size.x; y = node.yMax + Gap; break;
            }
            return new Rect(new Vector2(x, y), size);
        }

        public static NodeStatusAnchor Choose(Rect node, Vector2 size, Rect viewport,
            IReadOnlyList<Rect> cards, IReadOnlyList<Rect> critical,
            IReadOnlyList<Rect> otherStacks)
        {
            float best = float.PositiveInfinity;
            NodeStatusAnchor winner = NodeStatusAnchor.N;
            for (int i = 0; i < 8; i++)
            {
                NodeStatusAnchor anchor = (NodeStatusAnchor)i;
                Rect candidate = At(anchor, node, size);
                Rect inside = Intersection(candidate, viewport);
                float score = (candidate.width * candidate.height -
                    inside.width * inside.height) * 40f + i * 3f;
                AddConflicts(cards, candidate, 20f, ref score);
                AddConflicts(critical, candidate, 60f, ref score);
                AddConflicts(otherStacks, candidate, 25f, ref score);
                if (score < best)
                {
                    best = score;
                    winner = anchor;
                }
            }
            return winner;
        }

        private static void AddConflicts(IReadOnlyList<Rect> obstacles, Rect candidate,
            float weight, ref float score)
        {
            if (obstacles == null) return;
            foreach (Rect obstacle in obstacles)
            {
                Rect overlap = Intersection(candidate, obstacle);
                score += overlap.width * overlap.height * weight;
            }
        }

        private static Rect Intersection(Rect a, Rect b)
        {
            float left = Mathf.Max(a.xMin, b.xMin);
            float bottom = Mathf.Max(a.yMin, b.yMin);
            return new Rect(left, bottom, Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - left),
                Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - bottom));
        }
    }
}
