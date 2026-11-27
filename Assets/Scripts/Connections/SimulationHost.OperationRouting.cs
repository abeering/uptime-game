using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // One immutable execution decision per operation. The logical plan remains unchanged.
    public sealed class OperationRouteResolution
    {
        internal OperationRouteResolution(int operationIndex, NodeView intended, NodeResources target,
            RouteNode[] route, PhysicalLinkReference[] links, bool cached, float work, float load,
            string reason, float time, string selectedCachePurpose)
        { OperationIndex = operationIndex; IntendedDestination = intended; Target = target; Route = route;
            Links = links; Cached = cached; WorkMultiplier = work; LoadMultiplier = load; Reason = reason; CommittedAtSeconds = time; SelectedCachePurpose = selectedCachePurpose; }
        public int OperationIndex { get; }
        public NodeView IntendedDestination { get; }
        public NodeResources Target { get; }
        public RouteNode[] Route { get; }
        internal PhysicalLinkReference[] Links { get; }
        public bool Cached { get; }
        public float WorkMultiplier { get; }
        public float LoadMultiplier { get; }
        public string Reason { get; }
        public string SelectedCachePurpose { get; }
        public float CommittedAtSeconds { get; }
    }

    // Mechanical route-transition evidence; the view owns the cosmetic retraction.
    public sealed class OperationRouteTransition
    {
        public RouteNode[] PreviousRoute { get; }
        public PhysicalLinkReference[] PreviousLinks { get; }
        public int SharedNodeCount { get; }
        public int SharedLinkCount => Math.Max(0, SharedNodeCount - 1);
        public int NewLinkCount { get; }
        public float StartedAtSeconds { get; }
        public float TravelSeconds { get; }
        public int RouteRevision { get; }
        public float? FinishedAtSeconds { get; internal set; }
        internal OperationRouteTransition(RouteNode[] previous, PhysicalLinkReference[] links, int common, int newLinks, float time, float perLink, int revision)
        { PreviousRoute = previous; PreviousLinks = links; SharedNodeCount = common; NewLinkCount = newLinks;
            StartedAtSeconds = time; TravelSeconds = newLinks * perLink; RouteRevision = revision; }
        public static int CommonPrefix(RouteNode[] a, RouteNode[] b)
        { int count = 0; while (count < a.Length && count < b.Length && a[count].Node == b[count].Node) count++; return count; }
    }

    public sealed partial class SimulationHost
    {
        [Header("CACHE configuration: one READ PURPOSE slot")]
        [SerializeField] private NodeView cacheConfigurationNode;
        [SerializeField, Min(.001f)] private float cachedWorkMultiplier = .25f;
        [SerializeField, Min(.001f)] private float cachedLoadMultiplier = .5f;
        private string cachePurposeId;
        public NodeView CacheConfigurationNode => cacheConfigurationNode;
        public float CachedWorkMultiplier => cachedWorkMultiplier;
        public float CachedLoadMultiplier => cachedLoadMultiplier;
        public string CachePurposeId => cachePurposeId ?? string.Empty;
        public event Action CachePolicyChanged;
        public IReadOnlyList<ConnectionTrafficProfile> CachePurposeChoices
        {
            get
            {
                var result = new List<ConnectionTrafficProfile>();
                foreach (var profile in ApiPurposeChoices)
                    if (Array.Exists(profile.operations, op => op.operation == OperationKind.Read)) result.Add(profile);
                return result;
            }
        }
        public bool TrySetCachePurpose(string purposeId, out string result)
        {
            if (RunEnded) { result = RunEndedMessage; return false; }
            if (cacheConfigurationNode == null) { result = "CACHE CONFIGURATION UNAVAILABLE"; return false; }
            string selected = null;
            if (!string.IsNullOrEmpty(purposeId))
            {
                foreach (var profile in CachePurposeChoices)
                    if (string.Equals(profile.id, purposeId, StringComparison.OrdinalIgnoreCase)) { selected = profile.id; break; }
                if (selected == null) { result = "UNKNOWN READ PURPOSE " + purposeId; return false; }
            }
            string previous = cachePurposeId; cachePurposeId = selected;
            result = selected == null ? "CACHE PURPOSE CLEARED" : "CACHE SERVES " + selected + $"  WORK ×{cachedWorkMultiplier:G3}  LOAD ×{cachedLoadMultiplier:G3}";
            if (previous != selected)
            {
                recorder?.RecordCachePolicyChange(cacheConfigurationNode.NodeId, previous, selected, simulationTimeSeconds);
                CachePolicyChanged?.Invoke();
            }
            return true;
        }
        private void ResetOperationRouting()
        {
            if (!FinitePositive(cachedWorkMultiplier) || !FinitePositive(cachedLoadMultiplier))
                throw new InvalidOperationException("CACHE multipliers must be finite and positive.");
            cachePurposeId = null;
        }
        // Resolution is the boundary for operation requirements, topology and node configuration.
        // It is called once per operation; allocation policies remain independent and live.
        private void CommitOperationRoute(LogicalConnectionState state, bool record = true)
        {
            if (state.Routing != null) return;
            var definition = state.Definition; int index = state.CurrentOperationIndex;
            NodeView intended = definition.DestinationAt(index), actual = intended;
            RouteNode[] route = intended == definition.TargetNode ? definition.Route : FindOperationRoute(definition.Route[0].Node, intended, false);
            if (route == null) throw new InvalidOperationException("Ordinary operation destination has no physical route.");
            bool cached = false; string reason = "ordinary";
            if (cacheConfigurationNode != null && definition.OperationAt(index) == OperationKind.Read &&
                !string.IsNullOrEmpty(cachePurposeId) && string.Equals(definition.ProfileId, cachePurposeId, StringComparison.OrdinalIgnoreCase))
            {
                var candidate = FindOperationRoute(definition.Route[0].Node, cacheConfigurationNode, true);
                if (definition.Route[0].Node == cacheConfigurationNode) reason = "cache_local_source_unsupported";
                else if (candidate == null) reason = "cache_unavailable_or_unreachable";
                else if (!CanFulfillEffect(definition, index, cacheConfigurationNode)) reason = "cache_effect_route_incompatible";
                else { actual = cacheConfigurationNode; route = candidate; cached = true; reason = "cache_read_match"; }
            }
            var links = RouteLinksFor(route);
            var previousRoute = state.Route; var previousLinks = state.RouteLinks;
            int common = OperationRouteTransition.CommonPrefix(previousRoute, route);
            bool changed = common != previousRoute.Length || common != route.Length;
            state.CommitRoute(new OperationRouteResolution(index, intended, FindNodeResources(actual.NodeId), route,
                links, cached, cached ? cachedWorkMultiplier : 1, cached ? cachedLoadMultiplier : 1, reason, simulationTimeSeconds, cachePurposeId));
            if (state.HasEstablished && changed)
            {
                state.RouteTransition = new OperationRouteTransition(previousRoute, previousLinks, common,
                    links.Length - Math.Max(0, common - 1), simulationTimeSeconds, state.TravelSecondsPerLink, state.RouteRevision);
                state.Lifecycle = Lifecycle.Rerouting; state.IsTransitioning = false;
                state.ReachedRouteNodeIndex = Math.Max(0, common - 1); state.LinkTravelElapsedSeconds = 0; state.AllocatedLoad = 0;
            }
            if (record) recorder?.RecordOperationRoute(state, simulationTimeSeconds);
            if (state.IsRerouting)
            {
                recorder?.RecordRerouting("connection_rerouting_started", state, simulationTimeSeconds);
                // A shorter route may already be fully established. Retraction never charges simulation time.
                if (state.ReachedRouteNodeIndex == state.RouteLinks.Length) EstablishOperationRoute(state);
            }
        }
        private bool CanFulfillEffect(LogicalConnectionDefinition definition, int index, NodeView target)
        {
            if (definition.EffectAt(index) != CompletionEffect.PayloadLaunch) return true;
            var destination = FindNodeResources(definition.LaunchAt(index)?.DestinationNodeId)?.View;
            foreach (var link in physicalLinks)
                if (link.View.NodeA == target && link.View.NodeB == destination || link.View.NodeB == target && link.View.NodeA == destination) return true;
            return false;
        }
        private PhysicalLinkReference[] RouteLinksFor(RouteNode[] route)
        {
            var result = new PhysicalLinkReference[route.Length - 1];
            for (int i = 0; i < result.Length; i++)
            {
                foreach (var link in physicalLinks)
                    if (link.View.NodeA == route[i].Node && link.View.NodeB == route[i + 1].Node ||
                        link.View.NodeB == route[i].Node && link.View.NodeA == route[i + 1].Node) { result[i] = link; break; }
                if (result[i] == null) throw new InvalidOperationException("Resolved route has no physical link.");
            }
            return result;
        }
        private RouteNode[] FindOperationRoute(NodeView origin, NodeView target, bool requireAvailable)
        {
            if (origin == null || target == null || requireAvailable && (IsNodeOffline(origin) || IsNodeOffline(target))) return null;
            // CACHE is a leaf in each authored topology. Stable breadth-first traversal does not consume generation RNG.
            var pending = new Queue<NodeView>(); var previous = new Dictionary<NodeView, NodeView> { [origin] = null }; pending.Enqueue(origin);
            while (pending.Count > 0)
            {
                var node = pending.Dequeue(); if (node == target) break;
                foreach (var link in physicalLinks)
                {
                    NodeView next = link.View.NodeA == node ? link.View.NodeB : link.View.NodeB == node ? link.View.NodeA : null;
                    if (next == null || previous.ContainsKey(next) || requireAvailable && IsNodeOffline(next)) continue;
                    previous.Add(next, node); pending.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(target) || origin == target) return null;
            var reverse = new List<RouteNode>(); for (var node = target; node != null; node = previous[node]) reverse.Add(new RouteNode(node));
            reverse.Reverse(); return reverse.ToArray();
        }
    }
}
