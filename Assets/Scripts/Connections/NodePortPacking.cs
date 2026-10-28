using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Presentation-only attachment space. Active links on the same side of a card
    // divide its usable perimeter instead of each claiming a centered fixed strip.
    public static class NodePortPacking
    {
        public struct Port
        {
            public float Budget;
            public float AtA;
            public float AtB;

            public Port(float budget, float atA, float atB)
            {
                Budget = budget;
                AtA = atA;
                AtB = atB;
            }
        }

        private enum Face { Top, Bottom, Left, Right }

        private sealed class Entry
        {
            public PhysicalLinkView Link;
            public bool IsA;
            public int Count;
            public Rect Card;
            public Vector2 Direction;
            public Face Face;
            public float Low;
            public float High;
            public float Nominal;
            public float Budget;
            public float Bias;
        }

        public static Dictionary<PhysicalLinkView, Port> Plan(
            IReadOnlyDictionary<PhysicalLinkView, int> counts, RectTransform area)
        {
            var byNode = new Dictionary<NodeView, List<Entry>>();
            var result = new Dictionary<PhysicalLinkView, Port>();
            foreach (KeyValuePair<PhysicalLinkView, int> pair in counts)
            {
                if (pair.Key == null || pair.Value <= 0) continue;
                Add(byNode, pair.Key, pair.Key.NodeA, pair.Key.NodeB, true, pair.Value, area);
                Add(byNode, pair.Key, pair.Key.NodeB, pair.Key.NodeA, false, pair.Value, area);
            }
            var byLink = new Dictionary<PhysicalLinkView, Entry[]>();
            foreach (List<Entry> entries in byNode.Values)
            {
                foreach (Face face in new[] { Face.Top, Face.Bottom, Face.Left, Face.Right })
                {
                    var group = entries.FindAll(e => e.Face == face);
                    if (group.Count == 0) continue;
                    Allocate(group);
                }
                foreach (Entry entry in entries)
                {
                    if (!byLink.TryGetValue(entry.Link, out Entry[] pair))
                    {
                        pair = new Entry[2];
                        byLink.Add(entry.Link, pair);
                    }
                    pair[entry.IsA ? 0 : 1] = entry;
                }
            }
            foreach (KeyValuePair<PhysicalLinkView, Entry[]> pair in byLink)
            {
                Entry a = pair.Value[0], b = pair.Value[1];
                float budget = Mathf.Min(a.Budget, b.Budget);
                // Store both biases in the physical A-to-B lane frame.
                result.Add(pair.Key, budget > 0f ? new Port(budget, a.Bias, -b.Bias) :
                    new Port(0f, 0f, 0f));
            }
            return result;
        }

        private static void Add(Dictionary<NodeView, List<Entry>> byNode,
            PhysicalLinkView link, NodeView node, NodeView other, bool isA,
            int count, RectTransform area)
        {
            if (!byNode.TryGetValue(node, out List<Entry> entries))
            {
                entries = new List<Entry>();
                byNode.Add(node, entries);
            }
            Rect card = EdgeGeometry.CardRect(node, area);
            Vector2 direction = (Vector2)(area.InverseTransformPoint(other.transform.position) -
                area.InverseTransformPoint(node.transform.position));
            direction.Normalize();
            Vector2 nominal = EdgeGeometry.CardEdge(card, direction, 0f);
            Face face = FaceAt(card, nominal, direction);
            float negative = Limit(card, direction, -1f);
            float positive = Limit(card, direction, 1f);
            Vector2 lowPoint = EdgeGeometry.CardEdge(card, direction, negative);
            Vector2 highPoint = EdgeGeometry.CardEdge(card, direction, positive);
            float low = Coordinate(card, face, lowPoint);
            float high = Coordinate(card, face, highPoint);
            entries.Add(new Entry {
                Link = link, IsA = isA, Count = count, Card = card,
                Direction = direction, Face = face,
                Low = Mathf.Min(low, high), High = Mathf.Max(low, high),
                Nominal = Coordinate(card, face, nominal)
            });
        }

        private static void Allocate(List<Entry> group)
        {
            group.Sort((a, b) =>
            {
                int order = a.Nominal.CompareTo(b.Nominal);
                return order != 0 ? order : string.CompareOrdinal(a.Link.LinkId, b.Link.LinkId);
            });
            if (group.Count == 1)
            {
                // An uncontested link stays on its physical center line.
                Entry only = group[0];
                Vector2 normal = Normal(only.Direction);
                float low = Vector2.Dot(Point(only.Card, only.Face, only.Low) - only.Card.center, normal);
                float high = Vector2.Dot(Point(only.Card, only.Face, only.High) - only.Card.center, normal);
                only.Budget = 2f * Mathf.Min(Mathf.Abs(low), Mathf.Abs(high));
                return;
            }
            float start = float.PositiveInfinity, end = float.NegativeInfinity;
            float totalWeight = 0f;
            foreach (Entry entry in group)
            {
                start = Mathf.Min(start, entry.Low);
                end = Mathf.Max(end, entry.High);
                totalWeight += Weight(entry.Count);
            }
            // A path may use the near half of either adjoining side. Reaching the
            // far half makes a diagonal appear to wrap around the node.
            float side = group[0].Face == Face.Top || group[0].Face == Face.Bottom ?
                group[0].Card.height : group[0].Card.width;
            float front = group[0].Face == Face.Top || group[0].Face == Face.Bottom ?
                group[0].Card.width : group[0].Card.height;
            start = Mathf.Max(start, side * 0.5f);
            end = Mathf.Min(end, side * 1.5f + front);
            float usedWeight = 0f;
            foreach (Entry entry in group)
            {
                float begin = start + (end - start) * usedWeight / totalWeight;
                usedWeight += Weight(entry.Count);
                float finish = start + (end - start) * usedWeight / totalWeight;
                // Keep neighboring strips apart at the card border. The node face
                // hides their square ends, including the small underlap.
                float low = Mathf.Max(entry.Low, begin + 3f);
                float high = Mathf.Min(entry.High, finish - 3f);
                if (high <= low)
                    continue;
                Vector2 normal = Normal(entry.Direction);
                float a = Vector2.Dot(Point(entry.Card, entry.Face, low) - entry.Card.center, normal);
                float b = Vector2.Dot(Point(entry.Card, entry.Face, high) - entry.Card.center, normal);
                entry.Bias = (a + b) * 0.5f;
                entry.Budget = Mathf.Abs(b - a);
            }
        }

        private static float Weight(int count) => 16f + 15f * count;
        private static Vector2 Normal(Vector2 direction) =>
            new Vector2(-direction.y, direction.x);

        private static float Limit(Rect card, Vector2 direction, float sign)
        {
            Vector2 normal = Normal(direction);
            float radius = Mathf.Abs(normal.x) * card.width * 0.5f +
                Mathf.Abs(normal.y) * card.height * 0.5f;
            if (Valid(card, direction, normal * (sign * radius)))
                return sign * radius;
            float low = 0f, high = radius;
            for (int i = 0; i < 20; i++)
            {
                float middle = (low + high) * 0.5f;
                if (Valid(card, direction, normal * (sign * middle))) low = middle;
                else high = middle;
            }
            return sign * low;
        }

        private static bool Valid(Rect card, Vector2 direction, Vector2 offset)
        {
            Vector2 point = EdgeGeometry.CardEdge(card, direction,
                Vector2.Dot(offset, Normal(direction)));
            return point.x >= card.xMin - 0.01f && point.x <= card.xMax + 0.01f &&
                point.y >= card.yMin - 0.01f && point.y <= card.yMax + 0.01f &&
                Vector2.Dot(point - card.center - offset, direction) >= -0.01f;
        }

        private static Face FaceAt(Rect card, Vector2 point, Vector2 direction)
        {
            if (Mathf.Abs(point.y - card.yMax) < 0.01f) return Face.Top;
            if (Mathf.Abs(point.y - card.yMin) < 0.01f) return Face.Bottom;
            return direction.x < 0f ? Face.Left : Face.Right;
        }

        // Each face uses a continuous coordinate over itself and its two adjacent
        // faces. This lets a diagonal bundle take an unused corner or side.
        private static float Coordinate(Rect card, Face face, Vector2 point)
        {
            float w = card.width, h = card.height;
            switch (face)
            {
                case Face.Top:
                    if (Mathf.Abs(point.x - card.xMin) < 0.01f) return point.y - card.yMin;
                    if (Mathf.Abs(point.x - card.xMax) < 0.01f) return h + w + card.yMax - point.y;
                    return h + point.x - card.xMin;
                case Face.Bottom:
                    if (Mathf.Abs(point.x - card.xMin) < 0.01f) return card.yMax - point.y;
                    if (Mathf.Abs(point.x - card.xMax) < 0.01f) return h + w + point.y - card.yMin;
                    return h + point.x - card.xMin;
                case Face.Right:
                    if (Mathf.Abs(point.y - card.yMin) < 0.01f) return point.x - card.xMin;
                    if (Mathf.Abs(point.y - card.yMax) < 0.01f) return w + h + card.xMax - point.x;
                    return w + point.y - card.yMin;
                default:
                    if (Mathf.Abs(point.y - card.yMin) < 0.01f) return card.xMax - point.x;
                    if (Mathf.Abs(point.y - card.yMax) < 0.01f) return w + h + point.x - card.xMin;
                    return w + point.y - card.yMin;
            }
        }

        private static Vector2 Point(Rect card, Face face, float value)
        {
            float w = card.width, h = card.height;
            switch (face)
            {
                case Face.Top:
                    if (value < h) return new Vector2(card.xMin, card.yMin + value);
                    if (value < h + w) return new Vector2(card.xMin + value - h, card.yMax);
                    return new Vector2(card.xMax, card.yMax - (value - h - w));
                case Face.Bottom:
                    if (value < h) return new Vector2(card.xMin, card.yMax - value);
                    if (value < h + w) return new Vector2(card.xMin + value - h, card.yMin);
                    return new Vector2(card.xMax, card.yMin + value - h - w);
                case Face.Right:
                    if (value < w) return new Vector2(card.xMin + value, card.yMin);
                    if (value < w + h) return new Vector2(card.xMax, card.yMin + value - w);
                    return new Vector2(card.xMax - (value - w - h), card.yMax);
                default:
                    if (value < w) return new Vector2(card.xMax - value, card.yMin);
                    if (value < w + h) return new Vector2(card.xMin, card.yMin + value - w);
                    return new Vector2(card.xMin + value - w - h, card.yMax);
            }
        }
    }
}
