using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public enum NodeStatusAnchor { N, NE, E, SE, S, SW, W, NW }

    public static class NodeStatusPlacement
    {
        public readonly struct BeamSegment
        {
            public readonly Vector2 Start;
            public readonly Vector2 End;

            public BeamSegment(Vector2 start, Vector2 end)
            { Start = start; End = end; }
        }

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
            IReadOnlyList<Rect> otherStacks,
            IReadOnlyList<Vector2> busyDirections = null,
            IReadOnlyList<BeamSegment> beams = null)
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
                if (busyDirections != null)
                {
                    Vector2 direction = (candidate.center - node.center).normalized;
                    foreach (Vector2 busy in busyDirections)
                    {
                        float alignment = Vector2.Dot(direction, busy.normalized);
                        score += 120f * Mathf.Max(0f, alignment) -
                            30f * Mathf.Pow(Mathf.Max(0f, -alignment), 2f);
                    }
                }
                if (beams != null)
                    foreach (BeamSegment beam in beams)
                        score += SegmentLengthInside(beam, Expanded(candidate, 6f)) * 2f;
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

        public static Rect Expanded(Rect rect, float padding) => new Rect(
            rect.xMin - padding, rect.yMin - padding,
            rect.width + padding * 2f, rect.height + padding * 2f);

        private static float SegmentLengthInside(BeamSegment beam, Rect rect)
        {
            Vector2 delta = beam.End - beam.Start;
            float enter = 0f, leave = 1f;
            if (!Clip(-delta.x, beam.Start.x - rect.xMin, ref enter, ref leave) ||
                !Clip(delta.x, rect.xMax - beam.Start.x, ref enter, ref leave) ||
                !Clip(-delta.y, beam.Start.y - rect.yMin, ref enter, ref leave) ||
                !Clip(delta.y, rect.yMax - beam.Start.y, ref enter, ref leave))
                return 0f;
            return Mathf.Max(0f, leave - enter) * delta.magnitude;
        }

        private static bool Clip(float p, float q, ref float enter, ref float leave)
        {
            if (Mathf.Abs(p) < .0001f) return q >= 0f;
            float t = q / p;
            if (p < 0f) enter = Mathf.Max(enter, t);
            else leave = Mathf.Min(leave, t);
            return enter <= leave;
        }
    }
}
