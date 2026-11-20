using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Uptime.Connections
{
    public enum HackerDirective { SPREAD, BREACH }

    [Serializable] public sealed class HackerOffensive
    {
        public int id;
        public string directive, critical, reason;
        public string[] path;
        public int remainingCostAtSelection;
        public float selectedAt, expiresAt;
    }
    [Serializable] public sealed class HackerTargetDecision
    {
        public string directive, activity, originalTarget, finalTarget, reason, connectionId;
        public int offensiveId;
        public float time, preferenceRoll = -1f;
        public bool eligible, preferred, influenced;
        public string[] candidates;
    }
    [Serializable] public sealed class HackerDirectiveSnapshot
    {
        public string directive;
        public bool hasOffensive;
        public HackerOffensive offensive;
    }

    // A target preference only: owns no attack budget, generation, damage, or public diagnostics.
    public sealed class HackerDirectiveSelector
    {
        private readonly IntegrityState integrity;
        private readonly PhysicalLinkReference[] links;
        private readonly System.Random random;
        private readonly float commitmentSeconds, preferenceProbability;
        private readonly Action<HackerOffensive> selected;
        private int nextOffensiveId;
        public HackerDirective Mode { get; }
        public HackerOffensive Offensive { get; private set; }

        public HackerDirectiveSelector(IntegrityState state, PhysicalLinkReference[] physicalLinks,
            HackerDirective mode, int seed, float commitment = 60f, float preference = .7f,
            Action<HackerOffensive> onSelected = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!Enum.IsDefined(typeof(HackerDirective), mode) || !float.IsFinite(commitment) ||
                commitment <= 0 || !float.IsFinite(preference) || preference < 0 || preference > 1)
                throw new ArgumentOutOfRangeException(nameof(commitment));
            integrity = state; links = physicalLinks ?? Array.Empty<PhysicalLinkReference>();
            Mode = mode; commitmentSeconds = commitment; preferenceProbability = preference;
            random = new System.Random(unchecked(seed ^ (int)0x41D1CE73));
            selected = onSelected;
        }

        private IntegrityNode Node(string id) => integrity.Nodes.FirstOrDefault(n => n.Resources.View.NodeId == id);
        private bool Adjacent(string a, string b) => links.Any(e => e?.View != null &&
            (e.View.NodeA?.NodeId == a && e.View.NodeB?.NodeId == b || e.View.NodeA?.NodeId == b && e.View.NodeB?.NodeId == a));
        private IEnumerable<IntegrityNode> Neighbors(IntegrityNode node) => integrity.Nodes.Where(n =>
            n != node && Adjacent(node.Resources.View.NodeId, n.Resources.View.NodeId));

        private string ReassessmentReason(float time)
        {
            if (Offensive == null) return "initial";
            if (Offensive.path == null || Offensive.path.Length == 0)
                return time >= Offensive.expiresAt ? "commitment_expired" : null;
            var route = Offensive.path.Select(Node).ToArray();
            if (route.Any(n => n == null) || !route[0].External || !route[route.Length - 1].Critical ||
                Enumerable.Range(1, route.Length - 1).Any(i => !Adjacent(Offensive.path[i - 1], Offensive.path[i])))
                return "objective_invalid";
            if (route.Where(n => !n.External).All(n => n.Compromised)) return "objective_complete";
            return time >= Offensive.expiresAt ? "commitment_expired" : null;
        }

        public void EnsureOffensive(float time)
        {
            if (Mode != HackerDirective.BREACH || integrity.Breach != null) return;
            string reason = ReassessmentReason(time);
            if (reason == null) return;
            IntegrityNode[] nodes = integrity.Nodes.ToArray();
            // Stable input order, then a dedicated seeded permutation resolves equal-distance predecessors.
            var priority = nodes.ToList();
            for (int i = priority.Count - 1; i > 0; i--)
            { int j = random.Next(i + 1); (priority[i], priority[j]) = (priority[j], priority[i]); }
            var rank = priority.Select((n, i) => (n, i)).ToDictionary(p => p.n, p => p.i);
            var candidates = new List<(int cost, string critical, string[] route)>();
            foreach (IntegrityNode root in nodes.Where(n => n.External))
            {
                var distance = nodes.ToDictionary(n => n, n => n == root ? 0 : int.MaxValue);
                var parent = new Dictionary<IntegrityNode, IntegrityNode>();
                var visited = new HashSet<IntegrityNode>();
                while (true)
                {
                    IntegrityNode current = nodes.Where(n => !visited.Contains(n) && distance[n] < int.MaxValue)
                        .OrderBy(n => distance[n]).ThenBy(n => rank[n]).FirstOrDefault();
                    if (current == null) break;
                    visited.Add(current);
                    foreach (IntegrityNode neighbor in Neighbors(current).OrderBy(n => rank[n]))
                    {
                        if (visited.Contains(neighbor)) continue;
                        int cost = distance[current] + (neighbor.External ? 0 : neighbor.Value);
                        if (cost >= distance[neighbor]) continue;
                        distance[neighbor] = cost; parent[neighbor] = current;
                    }
                }
                foreach (IntegrityNode critical in nodes.Where(n => n.Critical && !n.External && distance[n] < int.MaxValue))
                {
                    var route = new List<string>();
                    for (IntegrityNode step = critical; step != null; step = parent.TryGetValue(step, out var p) ? p : null)
                        route.Add(step.Resources.View.NodeId);
                    route.Reverse(); candidates.Add((distance[critical], critical.Resources.View.NodeId, route.ToArray()));
                }
            }
            int minimum = candidates.Count == 0 ? -1 : candidates.Min(c => c.cost);
            var best = candidates.Where(c => c.cost == minimum).ToArray();
            var chosen = best.Length == 0 ? (cost: -1, critical: (string)null, route: Array.Empty<string>()) : best[random.Next(best.Length)];
            Offensive = new HackerOffensive { id = ++nextOffensiveId, directive = Mode.ToString(),
                critical = chosen.critical, path = chosen.route, remainingCostAtSelection = chosen.cost,
                selectedAt = time, expiresAt = time + commitmentSeconds,
                reason = candidates.Count == 0 ? reason + ":no_reachable_critical" : reason };
            selected?.Invoke(Offensive);
        }

        public HackerTargetDecision Choose(string activity, float time, NodeView ambient,
            IReadOnlyList<NodeView> legalTargets, string unsupportedReason = null)
        {
            EnsureOffensive(time);
            var decision = new HackerTargetDecision { directive = Mode.ToString(), activity = activity,
                time = time, originalTarget = ambient?.NodeId, finalTarget = ambient?.NodeId,
                offensiveId = Offensive?.id ?? 0, reason = unsupportedReason };
            if (unsupportedReason != null) return decision;
            var legal = new HashSet<NodeView>(legalTargets ?? Array.Empty<NodeView>());
            IntegrityNode[] candidates = integrity.Nodes.Where(n => !n.External && !n.Compromised && legal.Contains(n.Resources.View) &&
                (Mode == HackerDirective.SPREAD || Offensive?.path.Contains(n.Resources.View.NodeId) == true)).ToArray();
            decision.eligible = candidates.Length > 0;
            decision.candidates = candidates.Select(n => n.Resources.View.NodeId).ToArray();
            if (Mode == HackerDirective.SPREAD) { decision.reason = "spread_control"; return decision; }
            if (!decision.eligible)
            { decision.reason = Offensive?.path.Length > 0 ? "no_eligible_offensive_target" : "no_reachable_critical"; return decision; }
            decision.preferenceRoll = (float)random.NextDouble();
            if (decision.preferenceRoll >= preferenceProbability)
            { decision.reason = "ambient_probability_branch"; return decision; }
            // One remaining point gets twice the preference weight of two points, never a guaranteed hit.
            float total = candidates.Sum(n => 1f / n.Value);
            float choice = (float)random.NextDouble() * total;
            IntegrityNode target = candidates[candidates.Length - 1];
            foreach (var candidate in candidates)
            { choice -= 1f / candidate.Value; if (choice < 0) { target = candidate; break; } }
            decision.preferred = true;
            decision.finalTarget = target.Resources.View.NodeId;
            decision.influenced = decision.finalTarget != decision.originalTarget;
            decision.reason = decision.influenced ? "offensive_target" : "ambient_already_selected";
            return decision;
        }
        public HackerDirectiveSnapshot Snapshot() => new HackerDirectiveSnapshot
        { directive = Mode.ToString(), hasOffensive = Offensive?.path.Length > 0, offensive = Offensive };
    }
}
