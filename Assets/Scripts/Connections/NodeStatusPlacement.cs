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
        private const float MarkerClearance = 2f;
        private static readonly NodeStatusAnchor[] Regions =
            { NodeStatusAnchor.N, NodeStatusAnchor.S };
        private static readonly float[] HorizontalOffsets = { 0f, -30f, 30f, -60f, 60f };
        private static readonly float[] OutwardOffsets = { 0f, 12f, 24f, 36f, 48f };

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
            IReadOnlyList<Rect> otherStacks,
            NodeStatusPlacementChoice? previous = null)
        {
            float bestScore = float.PositiveInfinity;
            int bestPriority = int.MaxValue;
            NodeStatusPlacementChoice best = new NodeStatusPlacementChoice(NodeStatusAnchor.N, 0f, 0f);
            float sameSideScore = float.PositiveInfinity;
            int sameSidePriority = int.MaxValue;
            NodeStatusPlacementChoice sameSide = best;
            foreach (NodeStatusAnchor anchor in Regions)
                foreach (float outward in OutwardOffsets)
                    foreach (float horizontal in HorizontalOffsets)
                    {
                        var choice = ClearMarkers(new NodeStatusPlacementChoice(anchor,
                            horizontal, outward), node, size, markers);
                        Rect candidate = At(choice, node, size);
                        int priority = Priority(candidate, viewport, cards, markers);
                        float score = Score(choice, candidate, viewport,
                            cards, markers, otherStacks);
                        if (priority < bestPriority ||
                            (priority == bestPriority && score < bestScore))
                        {
                            bestPriority = priority;
                            bestScore = score;
                            best = choice;
                        }
                        if (previous.HasValue && anchor == previous.Value.Anchor &&
                            (priority < sameSidePriority ||
                             (priority == sameSidePriority && score < sameSideScore)))
                        {
                            sameSidePriority = priority;
                            sameSideScore = score;
                            sameSide = choice;
                        }
                    }
            // Keep the chosen side, while allowing its gap to return to the
            // closest clear position as sockets appear and disappear.
            if (sameSidePriority == 0 &&
                ConflictArea(otherStacks, At(sameSide, node, size)) < 1f)
                return sameSide;
            return best;
        }

        private static NodeStatusPlacementChoice ClearMarkers(NodeStatusPlacementChoice choice,
            Rect node, Vector2 size, IReadOnlyList<Rect> markers)
        {
            if (markers == null) return choice;
            float outward = choice.OutwardOffset;
            for (int pass = 0; pass < markers.Count; pass++)
            {
                Rect candidate = At(new NodeStatusPlacementChoice(choice.Anchor,
                    choice.HorizontalOffset, outward), node, size);
                float extra = 0f;
                foreach (Rect marker in markers)
                {
                    if (candidate.xMin >= marker.xMax || candidate.xMax <= marker.xMin ||
                        candidate.yMin >= marker.yMax + MarkerClearance ||
                        candidate.yMax <= marker.yMin - MarkerClearance) continue;
                    extra = Mathf.Max(extra, choice.Anchor == NodeStatusAnchor.N ?
                        marker.yMax + MarkerClearance - candidate.yMin :
                        candidate.yMax - marker.yMin + MarkerClearance);
                }
                if (extra <= 0f) break;
                outward += extra;
            }
            return new NodeStatusPlacementChoice(choice.Anchor, choice.HorizontalOffset,
                outward);
        }

        private static int Priority(Rect candidate, Rect viewport,
            IReadOnlyList<Rect> cards, IReadOnlyList<Rect> markers)
        {
            if (ConflictArea(markers, candidate) >= .01f) return 3;
            if (ConflictArea(cards, candidate) >= .01f) return 2;
            return OutsideArea(candidate, viewport) >= .01f ? 1 : 0;
        }

        private static float Score(NodeStatusPlacementChoice choice, Rect candidate,
            Rect viewport, IReadOnlyList<Rect> cards, IReadOnlyList<Rect> markers,
            IReadOnlyList<Rect> otherStacks) =>
            OutsideArea(candidate, viewport) * 1000f +
            ConflictArea(markers, candidate) * 5000f +
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
    }
}
