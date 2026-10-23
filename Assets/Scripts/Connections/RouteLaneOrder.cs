using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Presentation-only ordering in each physical link's NodeA-to-NodeB frame.
    // Routes are small in the current scene, so a bounded global adjacent-swap search is sufficient.
    public sealed class RouteLaneOrder
    {
        public sealed class Route
        {
            public readonly string Id;
            public readonly NodeView[] Nodes;
            public readonly PhysicalLinkView[] Links;

            public Route(string id, NodeView[] nodes, PhysicalLinkView[] links)
            {
                Id = id;
                Nodes = nodes;
                Links = links;
            }
        }

        private readonly Dictionary<PhysicalLinkView, List<string>> orders =
            new Dictionary<PhysicalLinkView, List<string>>();
        private readonly List<Route> previousRoutes = new List<Route>();
        private readonly Dictionary<NodeView, Vector2> previousPositions =
            new Dictionary<NodeView, Vector2>();
        private readonly Dictionary<PhysicalLinkView, List<string>> previousOrders =
            new Dictionary<PhysicalLinkView, List<string>>();

        public IReadOnlyDictionary<PhysicalLinkView, List<string>> Orders => orders;
        public int SolveCount { get; private set; }

        public void Clear()
        {
            orders.Clear();
            previousRoutes.Clear();
            previousPositions.Clear();
            previousOrders.Clear();
            SolveCount = 0;
        }

        public void Update(IReadOnlyList<Route> routes, RectTransform area)
        {
            var active = new List<Route>(routes);
            active.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
            if (!Changed(active, area))
                return;

            previousOrders.Clear();
            foreach (var entry in orders)
                previousOrders.Add(entry.Key, new List<string>(entry.Value));
            orders.Clear();
            previousRoutes.Clear();
            previousPositions.Clear();
            foreach (Route route in active)
            {
                previousRoutes.Add(new Route(route.Id, (NodeView[])route.Nodes.Clone(),
                    (PhysicalLinkView[])route.Links.Clone()));
                foreach (NodeView node in route.Nodes)
                    previousPositions[node] = Position(node, area);
                foreach (PhysicalLinkView link in route.Links)
                {
                    if (!orders.TryGetValue(link, out List<string> users))
                    {
                        users = new List<string>();
                        orders.Add(link, users);
                    }
                    if (!users.Contains(route.Id))
                        users.Add(route.Id);
                }
            }

            var links = new List<PhysicalLinkView>(orders.Keys);
            links.Sort((a, b) => StringComparer.Ordinal.Compare(a.LinkId, b.LinkId));
            foreach (PhysicalLinkView link in links)
            {
                List<string> users = orders[link];
                if (!previousOrders.TryGetValue(link, out List<string> old))
                    continue;
                users.Sort((a, b) =>
                {
                    int rank = PreviousRank(old, a).CompareTo(PreviousRank(old, b));
                    return rank != 0 ? rank : StringComparer.Ordinal.Compare(a, b);
                });
            }

            // The score considers all occupied links after each candidate change. Repeatedly
            // take the best improvement, with stable link/ID order resolving exact ties.
            int placements = 0;
            foreach (PhysicalLinkView link in links) placements += orders[link].Count;
            int limit = Mathf.Min(256, Mathf.Max(16, placements * 8));
            for (int pass = 0; pass < limit; pass++)
            {
                float baseline = Score(active, links, area);
                float best = baseline - 0.001f;
                PhysicalLinkView bestLink = null;
                int bestIndex = -1;
                foreach (PhysicalLinkView link in links)
                {
                    List<string> users = orders[link];
                    for (int i = 0; i + 1 < users.Count; i++)
                    {
                        Swap(users, i);
                        float candidate = Score(active, links, area);
                        Swap(users, i);
                        if (candidate < best - 0.0001f)
                        {
                            best = candidate;
                            bestLink = link;
                            bestIndex = i;
                        }
                    }
                }
                if (bestLink == null)
                    break;
                Swap(orders[bestLink], bestIndex);
            }
            SolveCount++;
        }

        private bool Changed(List<Route> active, RectTransform area)
        {
            if (active.Count != previousRoutes.Count)
                return true;
            for (int i = 0; i < active.Count; i++)
            {
                Route a = active[i];
                Route b = previousRoutes[i];
                if (a.Id != b.Id || a.Nodes.Length != b.Nodes.Length ||
                    a.Links.Length != b.Links.Length)
                    return true;
                for (int n = 0; n < a.Nodes.Length; n++)
                    if (a.Nodes[n] != b.Nodes[n] ||
                        !previousPositions.TryGetValue(a.Nodes[n], out Vector2 old) ||
                        (Position(a.Nodes[n], area) - old).sqrMagnitude > 0.25f)
                        return true;
                for (int n = 0; n < a.Links.Length; n++)
                    if (a.Links[n] != b.Links[n])
                        return true;
            }
            return false;
        }

        private float Score(List<Route> routes, List<PhysicalLinkView> links, RectTransform area)
        {
            float score = 0f;
            foreach (PhysicalLinkView link in links)
            {
                List<string> users = orders[link];
                Vector2 normal = Normal(link, area);
                for (int i = 0; i < users.Count; i++)
                {
                    Route a = Find(routes, users[i]);
                    for (int j = i + 1; j < users.Count; j++)
                    {
                        Route b = Find(routes, users[j]);
                        if (previousOrders.TryGetValue(link, out List<string> old) &&
                            old.IndexOf(a.Id) >= 0 && old.IndexOf(b.Id) >= 0 &&
                            old.IndexOf(a.Id) > old.IndexOf(b.Id))
                            score += 0.15f;
                        foreach (NodeView node in new[] { link.NodeA, link.NodeB })
                        {
                            PhysicalLinkView nextA = OtherLink(a, link, node);
                            PhysicalLinkView nextB = OtherLink(b, link, node);
                            if (nextA == nextB)
                            {
                                if (nextA != null)
                                    for (int between = i + 1; between < j; between++)
                                        if (OtherLink(Find(routes, users[between]), link, node) != nextA)
                                            score += 0.55f;
                                continue;
                            }
                            float sideA = BranchSide(nextA, node, normal, area);
                            float sideB = BranchSide(nextB, node, normal, area);
                            if (sideA > sideB + 0.1f)
                                score += 2f;
                        }
                    }
                }
            }

            // A pair using the same two ports should keep its relative side through the node.
            // The sign of the canonical normals accounts for reverse traversals.
            for (int i = 0; i < routes.Count; i++)
                for (int j = i + 1; j < routes.Count; j++)
                    foreach (PhysicalLinkView first in routes[i].Links)
                    {
                        if (!orders.TryGetValue(first, out List<string> firstUsers) ||
                            firstUsers.IndexOf(routes[j].Id) < 0)
                            continue;
                        foreach (NodeView node in new[] { first.NodeA, first.NodeB })
                        {
                            PhysicalLinkView second = OtherLink(routes[i], first, node);
                            if (second == null || second != OtherLink(routes[j], first, node) ||
                                !orders.TryGetValue(second, out List<string> secondUsers))
                                continue;
                            float alignment = Vector2.Dot(Normal(first, area), Normal(second, area));
                            if (Mathf.Abs(alignment) < 0.15f)
                                continue;
                            int firstSign = Math.Sign(firstUsers.IndexOf(routes[i].Id) -
                                firstUsers.IndexOf(routes[j].Id));
                            int secondSign = Math.Sign(secondUsers.IndexOf(routes[i].Id) -
                                secondUsers.IndexOf(routes[j].Id));
                            if (firstSign * secondSign * Mathf.Sign(alignment) < 0f)
                                score += 8f;
                        }
                    }
            return score;
        }

        private static Route Find(List<Route> routes, string id)
        {
            foreach (Route route in routes)
                if (route.Id == id) return route;
            throw new InvalidOperationException("Lane route missing: " + id);
        }

        private static PhysicalLinkView OtherLink(Route route, PhysicalLinkView link, NodeView node)
        {
            for (int i = 0; i < route.Links.Length; i++)
                if (route.Links[i] == link)
                {
                    if (route.Nodes[i] == node && i > 0) return route.Links[i - 1];
                    if (route.Nodes[i + 1] == node && i + 1 < route.Links.Length)
                        return route.Links[i + 1];
                }
            return null;
        }

        private static float BranchSide(PhysicalLinkView branch, NodeView node,
            Vector2 normal, RectTransform area)
        {
            if (branch == null) return 0f; // A route ending here takes a central position.
            NodeView other = branch.NodeA == node ? branch.NodeB : branch.NodeA;
            return Vector2.Dot((Position(other, area) - Position(node, area)).normalized, normal);
        }

        private static Vector2 Normal(PhysicalLinkView link, RectTransform area)
        {
            Vector2 direction = (Position(link.NodeB, area) - Position(link.NodeA, area)).normalized;
            return new Vector2(-direction.y, direction.x);
        }

        private static Vector2 Position(NodeView node, RectTransform area) =>
            area.InverseTransformPoint(node.transform.position);

        private static int PreviousRank(List<string> old, string id)
        {
            int rank = old.IndexOf(id);
            return rank >= 0 ? rank : old.Count;
        }

        private static void Swap(List<string> users, int index)
        {
            string item = users[index];
            users[index] = users[index + 1];
            users[index + 1] = item;
        }
    }
}
