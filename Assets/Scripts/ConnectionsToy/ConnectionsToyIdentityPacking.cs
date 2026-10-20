using System.Collections.Generic;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // Plans identity positions in screen-aligned UI space. These bounds never feed back into line layout.
    public static class ConnectionsToyIdentityPacking
    {
        public struct Lane
        {
            public string Id;
            public Vector2 Start;
            public Vector2 End;

            public Lane(string id, Vector2 start, Vector2 end)
            {
                Id = id;
                Start = start;
                End = end;
            }
        }

        public struct Placement
        {
            public int LaneIndex;
            public float Fraction;
            public Rect Bounds;

            public Placement(int laneIndex, float fraction, Rect bounds)
            {
                LaneIndex = laneIndex;
                Fraction = fraction;
                Bounds = bounds;
            }
        }

        public static List<List<Placement>> BuildSchedule(IReadOnlyList<Lane> lanes, Vector2 size,
            float pillGap, float staggerStep, float endInset, IReadOnlyList<Rect> obstacles)
        {
            var schedule = new List<List<Placement>>();
            if (lanes == null || lanes.Count == 0)
                return schedule;

            var remaining = new List<int>();
            for (int i = 0; i < lanes.Count; i++)
                remaining.Add(i);
            List<Placement> fullest = BestPack(lanes, remaining, size, pillGap,
                staggerStep, endInset, obstacles);
            if (fullest == null || fullest.Count == 0)
                return schedule;
            if (fullest.Count == lanes.Count)
            {
                fullest.Sort((a, b) => a.LaneIndex.CompareTo(b.LaneIndex));
                schedule.Add(fullest);
                return schedule;
            }

            // Interleave indices across the bundle based on simultaneous capacity.
            // For 12 lanes and 3 pills, phases start 0/4/8, 1/5/9, and so on.
            int phases = Mathf.CeilToInt((float)lanes.Count / fullest.Count);
            for (int phase = 0; phase < phases; phase++)
            {
                var group = new List<int>();
                for (int index = phase; index < lanes.Count; index += phases)
                    group.Add(index);
                List<Placement> packed = BestPackWithPriority(lanes, group, size, pillGap,
                    staggerStep, endInset, obstacles);
                if (packed == null || packed.Count == 0)
                    continue;
                schedule.Add(packed);
                foreach (Placement placement in packed)
                    remaining.Remove(placement.LaneIndex);
            }

            // A group may contain an awkward combination. Extra passes guarantee coverage.
            while (remaining.Count > 0)
            {
                List<Placement> best = BestPackWithPriority(lanes, remaining, size, pillGap,
                    staggerStep, endInset, obstacles);
                if (best == null || best.Count == 0)
                    break; // An edge too short for even one readable pill is reported by the presenter.
                foreach (Placement placement in best)
                    remaining.Remove(placement.LaneIndex);
                schedule.Add(best);
            }

            var all = new List<int>();
            for (int i = 0; i < lanes.Count; i++)
                all.Add(i);
            foreach (List<Placement> cycle in schedule)
            {
                int anchor = cycle[0].LaneIndex;
                foreach (int index in SpreadOrder(all, anchor, lanes.Count))
                    if (!Contains(cycle, index))
                        TryPlace(lanes[index], index, cycle, size, pillGap, staggerStep,
                            endInset, obstacles);
                cycle.Sort((a, b) => a.LaneIndex.CompareTo(b.LaneIndex));
            }
            return schedule;
        }

        private static List<Placement> BestPack(IReadOnlyList<Lane> lanes, List<int> eligible,
            Vector2 size, float gap, float step, float inset, IReadOnlyList<Rect> obstacles)
        {
            List<Placement> best = null;
            foreach (int anchor in eligible)
            {
                List<int> order = SpreadOrder(eligible, anchor, lanes.Count);
                for (int style = 0; style < 3; style++)
                {
                    List<Placement> trial = Pack(lanes, order,
                        size, gap, step, inset, obstacles, style);
                    if (best == null || trial.Count > best.Count)
                        best = trial;
                }
            }
            return best;
        }

        private static List<Placement> BestPackWithPriority(IReadOnlyList<Lane> lanes,
            List<int> priority, Vector2 size, float gap, float step, float inset,
            IReadOnlyList<Rect> obstacles)
        {
            var others = new List<int>();
            for (int i = 0; i < lanes.Count; i++)
                if (!priority.Contains(i))
                    others.Add(i);
            List<Placement> best = null;
            foreach (int anchor in priority)
            {
                List<int> order = SpreadOrder(priority, anchor, lanes.Count);
                if (others.Count > 0)
                    order.AddRange(SpreadOrder(others, others[0], lanes.Count));
                for (int style = 0; style < 3; style++)
                {
                    List<Placement> trial = Pack(lanes, order, size, gap, step, inset,
                        obstacles, style);
                    if (best == null || trial.Count > best.Count)
                        best = trial;
                }
            }
            return best;
        }

        private static bool Contains(List<Placement> placements, int index)
        {
            foreach (Placement placement in placements)
                if (placement.LaneIndex == index)
                    return true;
            return false;
        }

        private static List<Placement> Pack(IReadOnlyList<Lane> lanes, List<int> order,
            Vector2 size, float gap, float step, float inset, IReadOnlyList<Rect> obstacles,
            int style)
        {
            var placements = new List<Placement>();
            foreach (int index in order)
                TryPlace(lanes[index], index, placements, size, gap, step, inset, obstacles,
                    style);
            return placements;
        }

        private static List<int> SpreadOrder(IReadOnlyList<int> eligible, int anchor, int total)
        {
            var order = new List<int>();
            if (eligible.Count == 0)
                return order;
            order.Add(anchor);
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

        private static bool TryPlace(Lane lane, int index, List<Placement> placed,
            Vector2 size, float gap, float step, float inset, IReadOnlyList<Rect> obstacles,
            int style = 0)
        {
            float length = Vector2.Distance(lane.Start, lane.End);
            if (length < 0.01f || length <= inset * 2f)
                return false;
            float min = inset / length;
            float max = 1f - min;
            float fractionStep = (style == 0 ? Mathf.Max(4f, step) : 4f) / length;
            int steps = style == 0 ? Mathf.CeilToInt(0.5f / fractionStep)
                : Mathf.CeilToInt((max - min) / fractionStep);
            for (int i = 0; i <= steps; i++)
            {
                for (int side = 0; side < (style == 0 && i > 0 ? 2 : 1); side++)
                {
                    float fraction = style == 1 ? min + i * fractionStep
                        : style == 2 ? max - i * fractionStep
                        : 0.5f + (side == 0 ? 1f : -1f) * i * fractionStep;
                    if (fraction < min || fraction > max)
                        continue;
                    Vector2 center = Vector2.Lerp(lane.Start, lane.End, fraction);
                    Rect bounds = new Rect(center - size * 0.5f, size);
                    bool blocked = false;
                    if (obstacles != null)
                        foreach (Rect obstacle in obstacles)
                            if (Inflate(bounds, gap).Overlaps(obstacle))
                            {
                                blocked = true;
                                break;
                            }
                    if (blocked)
                        continue;
                    foreach (Placement other in placed)
                        if (Inflate(bounds, gap).Overlaps(other.Bounds))
                        {
                            blocked = true;
                            break;
                        }
                    if (blocked)
                        continue;
                    placed.Add(new Placement(index, fraction, bounds));
                    return true;
                }
            }
            return false;
        }

        private static Rect Inflate(Rect rect, float amount)
        {
            return new Rect(rect.xMin - amount, rect.yMin - amount,
                rect.width + amount * 2f, rect.height + amount * 2f);
        }
    }
}
