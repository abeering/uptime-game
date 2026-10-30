using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public enum PayloadMotionProfile { Steady, Variable }
    public enum PayloadPhase { Establishing, Traversing, Resolved }

    // Separate from logical connections: a Payload has no operation or LOAD demand.
    public sealed class PayloadState
    {
        public string Id { get; internal set; }
        public NodeView[] Route { get; internal set; }
        public float[] TravelSeconds { get; internal set; }
        public PayloadMotionProfile Profile { get; internal set; }
        public PayloadPhase Phase { get; internal set; }
        public int Hop { get; internal set; }
        public float PhaseElapsed { get; internal set; }
        public float EstablishSeconds { get; internal set; }
        public bool Intercepted { get; internal set; }
        public bool EffectApplied { get; internal set; }
        public NodeView CurrentNode => Route[Hop];
        public NodeView NextNode => Route[Hop + 1];
        public NodeView FinalNode => Route[Route.Length - 1];
        public float PhaseDuration => Phase == PayloadPhase.Establishing ?
            EstablishSeconds : TravelSeconds[Hop];
        public float Progress => Phase == PayloadPhase.Resolved ? 1f :
            Mathf.Clamp01(PhaseElapsed / PhaseDuration);
    }

    public sealed class InterceptionState
    {
        public string PayloadId { get; internal set; }
        public NodeResources Node { get; internal set; }
        public float ActivatedAt { get; internal set; }
        public float ExpiresAt { get; internal set; }
        public float CooldownUntil { get; internal set; }
        public bool Succeeded { get; internal set; }
        public int ActionSequence { get; internal set; }
    }

    // Simulation-owned movement and defense. Presentation reads these states only.
    public sealed class PayloadDirector : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField, Min(.1f)] private float establishmentSeconds = 1.2f;
        [SerializeField, Min(.1f)] private float interceptWindowSeconds = 5f;
        [SerializeField, Min(.1f)] private float interceptCooldownSeconds = 15f;
        [SerializeField, Min(0f)] private float interceptRequestedLoad = 4f;
        [SerializeField] private Color payloadColor = new Color(1f, .72f, .35f, 1f);

        private readonly List<PayloadState> payloads = new();
        private readonly Dictionary<NodeView, InterceptionState> interceptors = new();
        private readonly Dictionary<NodeView, float> cooldowns = new();
        private readonly HashSet<string> usedIds = new(StringComparer.OrdinalIgnoreCase);
        private System.Random random;
        private int nextId;
        public event Action<PayloadState> InterceptSucceeded;

        public IReadOnlyList<PayloadState> Payloads => payloads;
        public IReadOnlyCollection<InterceptionState> Interceptors => interceptors.Values;
        public float InterceptRequestedLoad => interceptRequestedLoad;
        public float InterceptWindowSeconds => interceptWindowSeconds;
        public float InterceptCooldownSeconds => interceptCooldownSeconds;
        public Color PayloadColor => payloadColor;

        private void Awake() { if (host == null) host = GetComponent<SimulationHost>(); }
        private void OnValidate()
        {
            if (!Positive(establishmentSeconds)) establishmentSeconds = 1.2f;
            if (!Positive(interceptWindowSeconds)) interceptWindowSeconds = 5f;
            if (!Positive(interceptCooldownSeconds)) interceptCooldownSeconds = 15f;
            if (float.IsNaN(interceptRequestedLoad) ||
                float.IsInfinity(interceptRequestedLoad) || interceptRequestedLoad < 0f)
                interceptRequestedLoad = 4f;
        }

        private static bool Positive(float value) => value > 0f &&
            !float.IsNaN(value) && !float.IsInfinity(value);
        public void ResetDirector(int seed)
        {
            payloads.Clear();
            interceptors.Clear();
            cooldowns.Clear();
            usedIds.Clear();
            random = new System.Random(unchecked(seed ^ (int)0xA4D102EF));
            nextId = 1;
        }

        public PayloadState FindActive(string id) => payloads.Find(p =>
            p.Phase != PayloadPhase.Resolved && string.Equals(p.Id, id,
                StringComparison.OrdinalIgnoreCase));
        public bool HasEverUsedId(string id) => id != null && usedIds.Contains(id);

        public InterceptionState InterceptorAt(NodeView node) => node != null &&
            interceptors.TryGetValue(node, out InterceptionState state) ? state : null;

        public PayloadState Spawn(string targetId, PayloadMotionProfile profile,
            string originId = null, float[] witnessTravelSeconds = null)
        {
            if (!Enum.IsDefined(typeof(PayloadMotionProfile), profile))
                throw new InvalidOperationException("Unknown Payload movement profile.");
            if (host == null || host.RuntimeConnections == null)
                throw new InvalidOperationException("Simulation unavailable.");
            NodeResources target = host.FindNodeResources(targetId);
            if (target == null) throw new InvalidOperationException("Unknown node: " + targetId);
            var candidates = new List<NodeView>();
            foreach (NodeResources node in host.Nodes)
                if (node?.View != null && node.View != target.View &&
                    (originId == null || string.Equals(node.View.NodeId, originId,
                        StringComparison.OrdinalIgnoreCase))) candidates.Add(node.View);
            if (originId != null && candidates.Count == 0)
                throw new InvalidOperationException("Unknown or identical origin: " + originId);
            // One independent seeded stream; changing ambient generation cannot change a Payload route.
            for (int i = candidates.Count - 1; i > 0; i--)
            { int j = random.Next(i + 1); (candidates[i], candidates[j]) = (candidates[j], candidates[i]); }
            NodeView[] route = null;
            foreach (NodeView origin in candidates)
            {
                route = FindRoute(origin, target.View);
                if (route != null) break;
            }
            if (route == null) throw new InvalidOperationException("No physical route to " + targetId);
            string id;
            do
            {
                if (nextId == int.MaxValue) throw new InvalidOperationException("Payload IDs exhausted.");
                id = "x" + nextId++;
            }
            while (host.HasEverUsedId(id) || host.IsTrafficIdReserved(id));
            int hops = route.Length - 1;
            if (witnessTravelSeconds != null && witnessTravelSeconds.Length != hops)
                throw new InvalidOperationException("Witness timing must specify every hop.");
            var durations = new float[hops];
            for (int i = 0; i < hops; i++)
            {
                float duration = witnessTravelSeconds == null ?
                    profile == PayloadMotionProfile.Steady ? 15f + 5f * (float)random.NextDouble() :
                    7f + 18f * (float)random.NextDouble() : witnessTravelSeconds[i];
                if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
                    throw new InvalidOperationException("Payload travel timing must be finite and positive.");
                durations[i] = duration;
            }
            var state = new PayloadState { Id = id, Route = route,
                TravelSeconds = durations, Profile = profile, Phase = PayloadPhase.Establishing,
                EstablishSeconds = Mathf.Max(.1f, establishmentSeconds) };
            payloads.Add(state);
            usedIds.Add(id);
            host.Recorder?.RecordPayload("payload_created", state, host.SimulationTimeSeconds);
            host.Recorder?.RecordPayload("payload_establishing", state, host.SimulationTimeSeconds);
            return state;
        }

        private NodeView[] FindRoute(NodeView origin, NodeView target)
        {
            var previous = new Dictionary<NodeView, NodeView> { [origin] = null };
            var queue = new Queue<NodeView>(); queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                NodeView node = queue.Dequeue();
                if (node == target) break;
                foreach (PhysicalLinkReference link in host.PhysicalLinks)
                {
                    NodeView next = link?.View?.NodeA == node ? link.View.NodeB :
                        link?.View?.NodeB == node ? link.View.NodeA : null;
                    if (next == null || previous.ContainsKey(next)) continue;
                    previous.Add(next, node); queue.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(target)) return null;
            var route = new List<NodeView>();
            for (NodeView node = target; node != null; node = previous[node]) route.Add(node);
            route.Reverse(); return route.ToArray();
        }

        public bool TryIntercept(string payloadId, string nodeId, out string error,
            string command = null)
        {
            PayloadState payload = FindActive(payloadId);
            if (payload == null) { error = "Unknown or resolved Payload ID."; return false; }
            NodeResources node = host.FindNodeResources(nodeId);
            if (node == null) { error = "Unknown node: " + nodeId; return false; }
            float now = host.SimulationTimeSeconds;
            if (interceptors.ContainsKey(node.View))
            { error = "Interceptor already active at " + node.View.NodeId; return false; }
            if (cooldowns.TryGetValue(node.View, out float until) && now < until - .00001f)
            { error = "Interceptor cooling down at " + node.View.NodeId; return false; }
            var state = new InterceptionState { PayloadId = payload.Id, Node = node,
                ActivatedAt = now, ExpiresAt = now + interceptWindowSeconds,
                CooldownUntil = now + interceptCooldownSeconds };
            if (command != null)
                state.ActionSequence = host.Recorder?.RecordPlayerAction(command,
                    payload.Id, true, node.View.NodeId, "intercept_activated") ?? 0;
            interceptors.Add(node.View, state);
            cooldowns[node.View] = state.CooldownUntil;
            host.RefreshPayloadAllocations();
            host.Recorder?.RecordIntercept("intercept_activated", state, now,
                interceptRequestedLoad, node.DefensiveAllocatedLoad);
            error = null; return true;
        }

        public float TimeToNextBoundary(float now)
        {
            float next = float.PositiveInfinity;
            foreach (PayloadState payload in payloads)
                if (payload.Phase != PayloadPhase.Resolved)
                    next = Mathf.Min(next, Mathf.Max(.000001f,
                        payload.PhaseDuration - payload.PhaseElapsed));
            foreach (InterceptionState state in interceptors.Values)
                next = Mathf.Min(next, Mathf.Max(.000001f, state.ExpiresAt - now));
            return next;
        }

        // Arrival wins at the exact expiry timestamp; expiry is processed afterward.
        public void Advance(float deltaSeconds, float now)
        {
            foreach (PayloadState payload in payloads)
            {
                if (payload.Phase == PayloadPhase.Resolved) continue;
                payload.PhaseElapsed += deltaSeconds;
                if (payload.PhaseElapsed + .00001f < payload.PhaseDuration) continue;
                payload.PhaseElapsed = 0f;
                if (payload.Phase == PayloadPhase.Establishing)
                {
                    payload.Phase = PayloadPhase.Traversing;
                    host.Recorder?.RecordPayload("payload_traversing", payload, now);
                    continue;
                }
                host.Recorder?.RecordPayload("payload_arrived", payload, now);
                if (payload.NextNode == payload.FinalNode)
                {
                    host.Recorder?.RecordPayload("payload_final_action_attempt", payload, now);
                    InterceptionState interceptor = InterceptorAt(payload.FinalNode);
                    if (interceptor != null && interceptor.PayloadId == payload.Id &&
                        now <= interceptor.ExpiresAt + .00001f)
                    {
                        interceptor.Succeeded = true;
                        payload.Intercepted = true;
                        host.Recorder?.RecordIntercept("intercept_succeeded", interceptor, now,
                            interceptRequestedLoad, interceptor.Node.DefensiveAllocatedLoad);
                        InterceptSucceeded?.Invoke(payload);
                    }
                    else payload.EffectApplied = host.ApplyPayloadEffect(payload.FinalNode, payload.Id);
                    payload.Phase = PayloadPhase.Resolved;
                    host.Recorder?.RecordPayload(payload.Intercepted ? "payload_intercepted" :
                        payload.EffectApplied ? "payload_effect_delivered" : "payload_effect_rejected",
                        payload, now);
                }
                else
                {
                    payload.Hop++;
                    payload.Phase = PayloadPhase.Establishing;
                    host.Recorder?.RecordPayload("payload_establishing", payload, now);
                }
            }
            var expired = new List<NodeView>();
            foreach (var pair in interceptors)
                if (now + .00001f >= pair.Value.ExpiresAt) expired.Add(pair.Key);
            foreach (NodeView node in expired)
            {
                InterceptionState state = interceptors[node];
                interceptors.Remove(node);
                host.Recorder?.RecordIntercept("intercept_expired", state, now, 0f, 0f);
            }
            if (expired.Count > 0) host.RefreshPayloadAllocations();
        }

        public float DemandAt(NodeView node) => interceptors.ContainsKey(node) ?
            Mathf.Max(0f, interceptRequestedLoad) : 0f;
    }
}
