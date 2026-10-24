using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Chooses one presentation layout per physical edge. Cycling only changes the visible pills.
    public static class IdentityPacking
    {
        public enum ReadabilityStage { Centered, Staggered, Overlaid, Rotating, Unavailable }

        public struct Lane
        {
            public string Id;
            public Vector2 Start;
            public Vector2 End;
            public float Width;

            public Lane(string id, Vector2 start, Vector2 end, float width)
            {
                Id = id;
                Start = start;
                End = end;
                Width = width;
            }
        }

        public struct PillBounds
        {
            public Vector2 Center;
            public Vector2 Along;
            public Vector2 Across;
            public Vector2 HalfSize;

            public PillBounds(Vector2 center, Vector2 direction, Vector2 size)
            {
                Center = center;
                Along = direction.normalized;
                Across = new Vector2(-Along.y, Along.x);
                HalfSize = size * 0.5f;
            }
        }

        public struct Placement
        {
            public int LaneIndex;
            public float Fraction;
            public PillBounds Bounds;

            public Placement(int laneIndex, float fraction, PillBounds bounds)
            {
                LaneIndex = laneIndex;
                Fraction = fraction;
                Bounds = bounds;
            }
        }

        public sealed class Layout
        {
            public ReadabilityStage Stage;
            public float LineWidth;
            public float LineGap;
            public Lane[] Lanes;
            public List<List<Placement>> Schedule;

            public Layout(ReadabilityStage stage, float lineWidth, float lineGap,
                Lane[] lanes, List<List<Placement>> schedule)
            {
                Stage = stage;
                LineWidth = lineWidth;
                LineGap = lineGap;
                Lanes = lanes;
                Schedule = schedule;
            }
        }

        public static Layout Solve(Vector2 start, Vector2 end, float budget, int count,
            float maximumLineWidth, float minimumReadableLineWidth, float minimumLineGap,
            Vector2 pillSize, float pillGap,
            float staggerStep, float endInset, float lineClearance,
            IReadOnlyList<PillBounds> obstacles, Rect? sourceCard = null,
            Rect? destinationCard = null)
        {
            if (count <= 0 || (end - start).sqrMagnitude < 0.01f)
                return new Layout(ReadabilityStage.Unavailable, 0f, minimumLineGap,
                    new Lane[0], new List<List<Placement>>());

            // Keep centered pills only when they do not make the paths thinner
            // than a staggered layout that can still show every identity.
            Layout centeredLayout = null;
            if (TryPillAwareLines(budget, count, maximumLineWidth, minimumLineGap,
                pillSize.y, pillGap, lineClearance, minimumReadableLineWidth,
                true, out float width, out float gap))
            {
                Lane[] lanes = MakeLanes(start, end, count, width, gap,
                    sourceCard, destinationCard);
                List<Placement> centered = Pack(lanes, Sequential(count), pillSize,
                    pillGap, staggerStep, endInset, obstacles, true, lineClearance, 3);
                if (centered.Count == count)
                    centeredLayout = Single(ReadabilityStage.Centered, width, gap, lanes, centered);
            }

            if (TryPillAwareLines(budget, count, maximumLineWidth, minimumLineGap,
                pillSize.y, pillGap, lineClearance, minimumReadableLineWidth,
                false, out width, out gap))
            {
                Lane[] lanes = MakeLanes(start, end, count, width, gap,
                    sourceCard, destinationCard);
                List<Placement> staggered = BestPack(lanes, Sequential(count), pillSize,
                    pillGap, staggerStep, endInset, obstacles, true, lineClearance);
                if (staggered != null && staggered.Count == count &&
                    (centeredLayout == null || width > centeredLayout.LineWidth + 0.01f))
                    return Single(ReadabilityStage.Staggered, width, gap, lanes, staggered);
            }
            if (centeredLayout != null)
                return centeredLayout;

            width = EdgeGeometry.LineWidth(budget, count, minimumLineGap, maximumLineWidth);
            gap = minimumLineGap;
            Lane[] compressed = MakeLanes(start, end, count, width, gap,
                sourceCard, destinationCard);
            List<Placement> overlaid = BestPack(compressed, Sequential(count), pillSize,
                pillGap, staggerStep, endInset, obstacles, false, lineClearance);
            if (overlaid != null && overlaid.Count == count)
                return Single(ReadabilityStage.Overlaid, width, gap, compressed, overlaid);

            List<List<Placement>> rotation = BuildRotation(compressed, pillSize, pillGap,
                staggerStep, endInset, obstacles);
            return new Layout(rotation.Count > 0 ? ReadabilityStage.Rotating : ReadabilityStage.Unavailable,
                width, gap, compressed, rotation);
        }

        private static Layout Single(ReadabilityStage stage, float width, float gap,
            Lane[] lanes, List<Placement> placements)
        {
            placements.Sort((a, b) => a.LaneIndex.CompareTo(b.LaneIndex));
            return new Layout(stage, width, gap, lanes,
                new List<List<Placement>> { placements });
        }

        // A asks for full pill separation; B only needs clearance from neighboring line strips.
        private static bool TryPillAwareLines(float budget, int count, float maximumWidth,
            float minimumGap, float pillHeight, float pillGap, float lineClearance,
            float minimumReadableWidth, bool centered, out float width, out float gap)
        {
            float low = 0f;
            float high = maximumWidth;
            for (int i = 0; i < 18; i++)
            {
                float candidate = (low + high) * 0.5f;
                float separation = centered ? pillHeight + pillGap :
                    pillHeight * 0.5f + candidate * 0.5f + lineClearance + 0.5f;
                float candidateGap = Mathf.Max(minimumGap, separation - candidate);
                if (count * candidate + (count - 1) * candidateGap <= budget + 0.001f)
                    low = candidate;
                else
                    high = candidate;
            }
            width = low;
            float needed = centered ? pillHeight + pillGap :
                pillHeight * 0.5f + width * 0.5f + lineClearance + 0.5f;
            gap = Mathf.Max(minimumGap, needed - width);
            return width >= minimumReadableWidth &&
                count * width + (count - 1) * gap <= budget + 0.01f;
        }

        private static Lane[] MakeLanes(Vector2 start, Vector2 end, int count, float width,
            float gap, Rect? sourceCard, Rect? destinationCard)
        {
            var lanes = new Lane[count];
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            for (int i = 0; i < count; i++)
            {
                float offset = EdgeGeometry.Offset(i, count, width, gap);
                // Plan against the same covered endpoints drawn by LogicalPathView.
                Vector2 laneStart = sourceCard.HasValue ?
                    EdgeGeometry.CardUnderlap(sourceCard.Value, direction, offset, width) :
                    start + normal * offset;
                Vector2 laneEnd = destinationCard.HasValue ?
                    EdgeGeometry.CardUnderlap(destinationCard.Value, -direction, -offset, width) :
                    end + normal * offset;
                lanes[i] = new Lane("c" + (i + 1), laneStart, laneEnd, width);
            }
            return lanes;
        }

        private static List<List<Placement>> BuildRotation(Lane[] lanes, Vector2 size,
            float pillGap, float step, float inset, IReadOnlyList<PillBounds> obstacles)
        {
            var schedule = new List<List<Placement>>();
            var remaining = Sequential(lanes.Length);
            List<Placement> fullest = BestPack(lanes, remaining, size, pillGap, step,
                inset, obstacles, false, 0f);
            if (fullest == null || fullest.Count == 0)
                return schedule;
            int phases = Mathf.CeilToInt((float)lanes.Length / fullest.Count);
            for (int phase = 0; phase < phases; phase++)
            {
                var priority = new List<int>();
                for (int i = phase; i < lanes.Length; i += phases)
                    priority.Add(i);
                List<Placement> cycle = BestPackWithPriority(lanes, priority, size,
                    pillGap, step, inset, obstacles);
                if (cycle == null || cycle.Count == 0)
                    continue;
                schedule.Add(cycle);
                foreach (Placement placement in cycle)
                    remaining.Remove(placement.LaneIndex);
            }
            while (remaining.Count > 0)
            {
                List<Placement> cycle = BestPackWithPriority(lanes, remaining, size,
                    pillGap, step, inset, obstacles);
                if (cycle == null || cycle.Count == 0)
                    return new List<List<Placement>>();
                int before = remaining.Count;
                schedule.Add(cycle);
                foreach (Placement placement in cycle)
                    remaining.Remove(placement.LaneIndex);
                // A fallback cycle can contain only lanes already scheduled. That
                // cannot cover the pending IDs, so report Unavailable instead of
                // looping forever or presenting an incomplete rotation schedule.
                if (remaining.Count == before)
                    return new List<List<Placement>>();
            }
            foreach (List<Placement> cycle in schedule)
                cycle.Sort((a, b) => a.LaneIndex.CompareTo(b.LaneIndex));
            return schedule;
        }

        private static List<Placement> BestPackWithPriority(Lane[] lanes, List<int> priority,
            Vector2 size, float gap, float step, float inset, IReadOnlyList<PillBounds> obstacles)
        {
            var others = Sequential(lanes.Length);
            foreach (int index in priority)
                others.Remove(index);
            List<Placement> best = null;
            foreach (int anchor in priority)
            {
                List<int> order = SpreadOrder(priority, anchor, lanes.Length);
                if (others.Count > 0)
                    order.AddRange(SpreadOrder(others, others[0], lanes.Length));
                for (int style = 0; style < 3; style++)
                {
                    List<Placement> trial = Pack(lanes, order, size, gap, step, inset,
                        obstacles, false, 0f, style);
                    if (best == null || trial.Count > best.Count)
                        best = trial;
                }
            }
            return best;
        }

        private static List<Placement> BestPack(Lane[] lanes, List<int> eligible,
            Vector2 size, float gap, float step, float inset, IReadOnlyList<PillBounds> obstacles,
            bool avoidOtherLines, float lineClearance)
        {
            List<Placement> best = null;
            foreach (int anchor in eligible)
            {
                List<int> order = SpreadOrder(eligible, anchor, lanes.Length);
                for (int style = 0; style < 3; style++)
                {
                    List<Placement> trial = Pack(lanes, order, size, gap, step, inset,
                        obstacles, avoidOtherLines, lineClearance, style);
                    if (best == null || trial.Count > best.Count)
                        best = trial;
                    if (best.Count == eligible.Count)
                        return best;
                }
            }
            return best;
        }

        private static List<Placement> Pack(Lane[] lanes, List<int> order, Vector2 size,
            float gap, float step, float inset, IReadOnlyList<PillBounds> obstacles,
            bool avoidOtherLines, float lineClearance, int style)
        {
            var placed = new List<Placement>();
            foreach (int index in order)
                TryPlace(lanes, index, placed, size, gap, step, inset, obstacles,
                    avoidOtherLines, lineClearance, style);
            return placed;
        }

        private static bool TryPlace(Lane[] lanes, int index, List<Placement> placed,
            Vector2 size, float gap, float step, float inset, IReadOnlyList<PillBounds> obstacles,
            bool avoidOtherLines, float lineClearance, int style)
        {
            Lane lane = lanes[index];
            float length = Vector2.Distance(lane.Start, lane.End);
            if (length < 0.01f)
                return false;
            float margin = (size.x * 0.5f + inset) / length;
            if (margin > 0.5f)
                return false;
            float fine = 4f / length;
            float coarse = Mathf.Max(4f, step) / length;
            int passes = style == 0 ? 2 : 1;
            for (int pass = 0; pass < passes; pass++)
            {
                float increment = pass == 0 && style == 0 ? coarse : fine;
                int steps = style == 3 ? 0 : Mathf.CeilToInt(1f / increment);
                for (int i = 0; i <= steps; i++)
                {
                    int sides = style == 0 && i > 0 ? 2 : 1;
                    for (int side = 0; side < sides; side++)
                    {
                        float fraction = style == 3 ? 0.5f : style == 1 ? margin + i * increment
                            : style == 2 ? 1f - margin - i * increment
                            : 0.5f + (side == 0 ? 1f : -1f) * i * increment;
                        if (fraction < margin - 0.0001f || fraction > 1f - margin + 0.0001f)
                            continue;
                        PillBounds bounds = Bounds(lane, fraction, size);
                        if (!CanShow(bounds, index, lanes, placed, obstacles, gap,
                            avoidOtherLines, lineClearance))
                            continue;
                        placed.Add(new Placement(index, fraction, bounds));
                        return true;
                    }
                }
            }
            return false;
        }

        public static PillBounds Bounds(Lane lane, float fraction, Vector2 size) =>
            new PillBounds(Vector2.Lerp(lane.Start, lane.End, fraction), lane.End - lane.Start, size);

        public static bool CanShow(PillBounds bounds, int laneIndex, IReadOnlyList<Lane> lanes,
            IReadOnlyList<Placement> placed, IReadOnlyList<PillBounds> obstacles, float gap,
            bool avoidOtherLines, float lineClearance)
        {
            if (obstacles != null)
                foreach (PillBounds obstacle in obstacles)
                    if (Intersects(bounds, obstacle, gap))
                        return false;
            if (placed != null)
                foreach (Placement other in placed)
                    if (Intersects(bounds, other.Bounds, gap))
                        return false;
            if (avoidOtherLines)
                for (int i = 0; i < lanes.Count; i++)
                    if (i != laneIndex && SegmentIntersects(bounds, lanes[i].Start,
                        lanes[i].End, lanes[i].Width * 0.5f + lineClearance))
                        return false;
            return true;
        }

        public static bool Intersects(PillBounds a, PillBounds b, float gap)
        {
            Vector2 delta = b.Center - a.Center;
            float growth = gap * 0.5f;
            return !Separated(a, b, delta, a.Along, growth) &&
                !Separated(a, b, delta, a.Across, growth) &&
                !Separated(a, b, delta, b.Along, growth) &&
                !Separated(a, b, delta, b.Across, growth);
        }

        private static bool Separated(PillBounds a, PillBounds b, Vector2 delta,
            Vector2 axis, float growth)
        {
            float reachA = Mathf.Abs(Vector2.Dot(axis, a.Along)) * (a.HalfSize.x + growth) +
                Mathf.Abs(Vector2.Dot(axis, a.Across)) * (a.HalfSize.y + growth);
            float reachB = Mathf.Abs(Vector2.Dot(axis, b.Along)) * (b.HalfSize.x + growth) +
                Mathf.Abs(Vector2.Dot(axis, b.Across)) * (b.HalfSize.y + growth);
            return Mathf.Abs(Vector2.Dot(delta, axis)) >= reachA + reachB - 0.0001f;
        }

        public static bool SegmentIntersects(PillBounds pill, Vector2 start, Vector2 end, float radius)
        {
            Vector2 from = start - pill.Center;
            Vector2 to = end - pill.Center;
            Vector2 localStart = new Vector2(Vector2.Dot(from, pill.Along), Vector2.Dot(from, pill.Across));
            Vector2 localEnd = new Vector2(Vector2.Dot(to, pill.Along), Vector2.Dot(to, pill.Across));
            float low = 0f;
            float high = 1f;
            return Clip(localStart.x, localEnd.x, pill.HalfSize.x + radius, ref low, ref high) &&
                Clip(localStart.y, localEnd.y, pill.HalfSize.y + radius, ref low, ref high);
        }

        private static bool Clip(float start, float end, float halfExtent, ref float low, ref float high)
        {
            float delta = end - start;
            if (Mathf.Abs(delta) < 0.0001f)
                return Mathf.Abs(start) < halfExtent;
            float first = (-halfExtent - start) / delta;
            float last = (halfExtent - start) / delta;
            if (first > last)
            {
                float swap = first;
                first = last;
                last = swap;
            }
            low = Mathf.Max(low, first);
            high = Mathf.Min(high, last);
            return low < high;
        }

        private static List<int> Sequential(int count)
        {
            var order = new List<int>(count);
            for (int i = 0; i < count; i++)
                order.Add(i);
            return order;
        }

        private static List<int> SpreadOrder(IReadOnlyList<int> eligible, int anchor, int total)
        {
            var order = new List<int> { anchor };
            while (order.Count < eligible.Count)
            {
                int choice = -1;
                int bestDistance = -1;
                foreach (int candidate in eligible)
                {
                    if (order.Contains(candidate))
                        continue;
                    int nearest = total;
                    foreach (int selected in order)
                    {
                        int distance = Mathf.Abs(candidate - selected);
                        nearest = Mathf.Min(nearest, Mathf.Min(distance, total - distance));
                    }
                    if (nearest > bestDistance || (nearest == bestDistance && candidate < choice))
                    {
                        bestDistance = nearest;
                        choice = candidate;
                    }
                }
                order.Add(choice);
            }
            return order;
        }
    }
}
