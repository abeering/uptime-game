using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public enum NodeStatusAnchor { N, S }

    // A small offset keeps the stack floating while its region remains above or below the node.
    public readonly struct NodeStatusPlacementChoice
    {
        public readonly NodeStatusAnchor Anchor;
        public readonly float HorizontalOffset;
        public readonly float OutwardOffset;

        public NodeStatusPlacementChoice(NodeStatusAnchor anchor, float horizontalOffset,
            float outwardOffset)
        {
            Anchor = anchor;
            HorizontalOffset = horizontalOffset;
            OutwardOffset = outwardOffset;
        }
    }

    public static class NodeStatusPlacement
    {
        private const float Gap = 17f;
        private static readonly NodeStatusAnchor[] Regions =
            { NodeStatusAnchor.N, NodeStatusAnchor.S };
        private static readonly float[] HorizontalOffsets = { 0f, -30f, 30f, -60f, 60f };
        private static readonly float[] OutwardOffsets = { 0f, 24f, 48f };

        public static Rect At(NodeStatusAnchor anchor, Rect node, Vector2 size) =>
            At(new NodeStatusPlacementChoice(anchor, 0f, 0f), node, size);

        public static Rect At(NodeStatusPlacementChoice choice, Rect node, Vector2 size)
        {
            float x = node.center.x - size.x * .5f + choice.HorizontalOffset;
            float y = choice.Anchor == NodeStatusAnchor.N ?
                node.yMax + Gap + choice.OutwardOffset :
                node.yMin - Gap - size.y - choice.OutwardOffset;
            return new Rect(x, y, size.x, size.y);
        }

        public static NodeStatusPlacementChoice Choose(Rect node, Vector2 size, Rect viewport,
            IReadOnlyList<Rect> cards, IReadOnlyList<Rect> markers,
            IReadOnlyList<Rect> pills, IReadOnlyList<Rect> otherStacks,
            NodeStatusPlacementChoice? previous = null)
        {
            float bestScore = float.PositiveInfinity;
            int bestPriority = int.MaxValue;
            NodeStatusPlacementChoice best = new NodeStatusPlacementChoice(NodeStatusAnchor.N, 0f, 0f);
            foreach (NodeStatusAnchor anchor in Regions)
                foreach (float outward in OutwardOffsets)
                    foreach (float horizontal in HorizontalOffsets)
                    {
                        var choice = new NodeStatusPlacementChoice(anchor, horizontal, outward);
                        Rect candidate = At(choice, node, size);
                        int priority = Priority(candidate, viewport, cards, markers, pills);
                        if (priority > bestPriority) continue;
                        float score = Score(choice, candidate, viewport,
                            cards, markers, pills, otherStacks);
                        if (priority == bestPriority && score >= bestScore) continue;
                        bestPriority = priority;
                        bestScore = score;
                        best = choice;
                    }

            if (previous.HasValue)
            {
                NodeStatusPlacementChoice held = previous.Value;
                Rect heldRect = At(held, node, size);
                // Traffic can change every frame. Keep a clear placement unless the new
                // choice is materially better; move promptly when a marker reaches it.
                if (bestPriority <= 1 &&
                    Priority(heldRect, viewport, cards, markers, pills) == bestPriority &&
                    ConflictArea(otherStacks, heldRect) < 1f &&
                    Score(held, heldRect, viewport, cards, markers, pills, otherStacks) <=
                        bestScore + 200f)
                    return held;
            }
            return best;
        }

        private static int Priority(Rect candidate, Rect viewport,
            IReadOnlyList<Rect> cards, IReadOnlyList<Rect> markers,
            IReadOnlyList<Rect> pills)
        {
            // A terminal marker stays clear whenever any above/below candidate can
            // do so. A pill may overlap only after clear, on-screen choices run out.
            if (ConflictArea(markers, candidate) >= .01f) return 4;
            if (ConflictArea(cards, candidate) >= .01f) return 3;
            if (OutsideArea(candidate, viewport) >= .01f) return 2;
            return ConflictArea(pills, candidate) < .01f ? 0 : 1;
        }

        private static float Score(NodeStatusPlacementChoice choice, Rect candidate,
            Rect viewport, IReadOnlyList<Rect> cards, IReadOnlyList<Rect> markers,
            IReadOnlyList<Rect> pills, IReadOnlyList<Rect> otherStacks) =>
            OutsideArea(candidate, viewport) * 1000f +
            ConflictArea(markers, candidate) * 5000f +
            ConflictArea(pills, candidate) * 2000f +
            ConflictArea(cards, candidate) * 200f +
            ConflictArea(otherStacks, candidate) * 80f +
            Mathf.Abs(choice.HorizontalOffset) * 1.5f +
            choice.OutwardOffset * .7f +
            (choice.Anchor == NodeStatusAnchor.S ? 4f : 0f);

        private static float OutsideArea(Rect candidate, Rect viewport)
        {
            Rect inside = Intersection(candidate, viewport);
            return candidate.width * candidate.height - inside.width * inside.height;
        }

        private static float ConflictArea(IReadOnlyList<Rect> obstacles, Rect candidate)
        {
            if (obstacles == null) return 0f;
            float area = 0f;
            foreach (Rect obstacle in obstacles)
            {
                Rect overlap = Intersection(candidate, obstacle);
                area += overlap.width * overlap.height;
            }
            return area;
        }

        private static Rect Intersection(Rect a, Rect b)
        {
            float left = Mathf.Max(a.xMin, b.xMin);
            float bottom = Mathf.Max(a.yMin, b.yMin);
            return new Rect(left, bottom, Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - left),
                Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - bottom));
        }

        public static Rect Expanded(Rect rect, float padding) => new Rect(
            rect.xMin - padding, rect.yMin - padding,
            rect.width + padding * 2f, rect.height + padding * 2f);
    }
}
