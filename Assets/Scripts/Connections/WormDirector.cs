using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public enum WormEffect { ScanJammed, PortScanned }
    public enum WormPhase { Establishing, Traversing, Entering, Acting, Resolved }

    [Serializable]
    public sealed class WormTuning
    {
        [Range(0f, 1f)] public float propagationProbability = .65f;
        [Min(1)] public int maximumNodes = 8;
        [Min(.1f)] public float establishSeconds = 1.2f;
        [Min(.1f)] public float travelSeconds = 12f;
        [Min(.1f)] public float actionSeconds = 8f;
        [Min(.1f)] public float conditionSeconds = 8f;

        public WormTuning Copy() => new WormTuning
        {
            propagationProbability = propagationProbability,
            maximumNodes = maximumNodes,
            establishSeconds = establishSeconds,
            travelSeconds = travelSeconds,
            actionSeconds = actionSeconds,
            conditionSeconds = conditionSeconds
        };

        public bool IsValid => propagationProbability >= 0f && propagationProbability <= 1f &&
            maximumNodes >= 1 && Positive(establishSeconds) && Positive(travelSeconds) &&
            Positive(actionSeconds) && Positive(conditionSeconds);
        private static bool Positive(float value) => value > 0f &&
            !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public sealed class WormBranch
    {
        public string Id { get; internal set; }
        public string ParentId { get; internal set; }
        public WormOutbreak Outbreak { get; internal set; }
        public NodeView CurrentNode { get; internal set; }
        public NodeView NextNode { get; internal set; }
        public PhysicalLinkView Link { get; internal set; }
        public WormPhase Phase { get; internal set; }
        public float PhaseElapsed { get; internal set; }
        public string TerminationReason { get; internal set; }
        public float PhaseDuration => Phase == WormPhase.Establishing ?
            Outbreak.Tuning.establishSeconds : Phase == WormPhase.Traversing ?
            Outbreak.Tuning.travelSeconds : Phase == WormPhase.Entering ?
            Mathf.Max(.1f, Outbreak.Tuning.travelSeconds * .125f) :
            Phase == WormPhase.Acting ? Outbreak.Tuning.actionSeconds : 0f;
        public float Progress => Phase == WormPhase.Resolved ? 1f :
            Mathf.Clamp01(PhaseElapsed / Mathf.Max(.0001f, PhaseDuration));
        public bool HasHop => Phase == WormPhase.Establishing ||
            Phase == WormPhase.Traversing || Phase == WormPhase.Entering;
    }

    public sealed class WormOutbreak
    {
        internal readonly List<WormBranch> branches = new();
        internal readonly HashSet<NodeView> claimed = new();
        internal System.Random random;
        internal int nextBranch;
        internal bool completed;
        public string Id { get; internal set; }
        public NodeView Origin { get; internal set; }
        public WormEffect Effect { get; internal set; }
        public int Seed { get; internal set; }
        public WormTuning Tuning { get; internal set; }
        public IReadOnlyList<WormBranch> Branches => branches;
        public IReadOnlyCollection<NodeView> ClaimedNodes => claimed;
        public bool IsComplete => completed;
    }

    public sealed class IsolationState
    {
        public NodeResources Node { get; internal set; }
        public float ActivatedAt { get; internal set; }
        public float ExpiresAt { get; internal set; }
        public float CooldownUntil { get; internal set; }
        public int ActionSequence { get; internal set; }
        public int WormsKilled { get; internal set; }
        public int ConnectionsCanceled { get; internal set; }
    }

    // Authoritative seeded propagation and isolation. Drawing reads these states only.
    public sealed class WormDirector : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private WormTuning tuning = new WormTuning();
        [SerializeField, Min(.1f)] private float isolationSeconds = 8f;
        [SerializeField, Min(.1f)] private float isolationCooldownSeconds = 30f;
        [SerializeField] private Color wormColor = new Color(.85f, .92f, .37f, 1f);

        private readonly List<WormOutbreak> outbreaks = new();
        private readonly Dictionary<NodeView, IsolationState> isolations = new();
        private readonly Dictionary<NodeView, float> cooldowns = new();
        private int rootSeed;
        private int nextOutbreak;

        public IReadOnlyList<WormOutbreak> Outbreaks => outbreaks;
        public IReadOnlyCollection<IsolationState> Isolations => isolations.Values;
        public Color WormColor => wormColor;
        public float IsolationSeconds => isolationSeconds;
        public float IsolationCooldownSeconds => isolationCooldownSeconds;
        public WormTuning DefaultTuning => tuning.Copy();

        private void Awake() { if (host == null) host = GetComponent<SimulationHost>(); }
        private void OnValidate()
        {
            if (tuning == null || !tuning.IsValid) tuning = new WormTuning();
            if (float.IsNaN(isolationSeconds) || float.IsInfinity(isolationSeconds) ||
                isolationSeconds <= 0f) isolationSeconds = 8f;
            if (float.IsNaN(isolationCooldownSeconds) || float.IsInfinity(isolationCooldownSeconds) ||
                isolationCooldownSeconds <= 0f) isolationCooldownSeconds = 30f;
        }

        public void ResetDirector(int seed)
        {
            outbreaks.Clear();
            isolations.Clear();
            cooldowns.Clear();
            rootSeed = seed;
            nextOutbreak = 1;
        }

        public WormOutbreak Spawn(string originId, WormEffect effect,
            WormTuning witnessTuning = null)
        {
            if (!Enum.IsDefined(typeof(WormEffect), effect))
                throw new InvalidOperationException("Unknown Worm effect.");
            if (host == null || host.RuntimeConnections == null)
                throw new InvalidOperationException("Simulation unavailable.");
            NodeResources origin = host.FindNodeResources(originId);
            if (origin == null) throw new InvalidOperationException("Unknown node: " + originId);
            if (IsNodeIsolated(origin.View))
                throw new InvalidOperationException("Origin is isolated: " + originId);
            WormTuning settings = (witnessTuning ?? tuning)?.Copy();
            if (settings == null || !settings.IsValid)
                throw new InvalidOperationException("Worm tuning is invalid.");
            if (nextOutbreak == int.MaxValue)
                throw new InvalidOperationException("Worm outbreak IDs exhausted.");
            int ordinal = nextOutbreak++;
            int seed = unchecked(rootSeed ^ (int)0x8D71BA53 ^ ordinal * 104729);
            var outbreak = new WormOutbreak { Id = "w" + ordinal, Origin = origin.View,
                Effect = effect, Seed = seed, Tuning = settings, random = new System.Random(seed) };
            outbreak.claimed.Add(origin.View);
            outbreaks.Add(outbreak);
            host.Recorder?.RecordWormOutbreak("worm_outbreak_started", outbreak,
                host.SimulationTimeSeconds);
            WormBranch root = NewBranch(outbreak, null, origin.View, null, null);
            host.Recorder?.RecordWormBranch("worm_body_entered", root,
                host.SimulationTimeSeconds);
            ApplyEffect(root);
            host.Recorder?.RecordWormBranch("worm_action_started", root,
                host.SimulationTimeSeconds);
            return outbreak;
        }

        private WormBranch NewBranch(WormOutbreak outbreak, WormBranch parent,
            NodeView current, NodeView next, PhysicalLinkView link)
        {
            var branch = new WormBranch { Id = outbreak.Id + "." + (++outbreak.nextBranch),
                ParentId = parent?.Id, Outbreak = outbreak, CurrentNode = current,
                NextNode = next, Link = link, Phase = next == null ?
                    WormPhase.Acting : WormPhase.Establishing };
            outbreak.branches.Add(branch);
            host.Recorder?.RecordWormBranch("worm_branch_created", branch,
                host.SimulationTimeSeconds);
            return branch;
        }

        private void ApplyEffect(WormBranch branch)
        {
            ConditionKind kind = branch.Outbreak.Effect == WormEffect.ScanJammed ?
                ConditionKind.ScanJammed : ConditionKind.PortScanned;
            NodeResources target = host.FindNodeResources(branch.CurrentNode.NodeId);
            host.ApplyCondition(target,
                new ConditionPayload(kind, branch.Outbreak.Tuning.conditionSeconds),
                sourceWormBranchId: branch.Id);
            host.Recorder?.RecordWormBranch("worm_effect_applied", branch,
                host.SimulationTimeSeconds);
        }

        public float TimeToNextBoundary(float now)
        {
            float next = float.PositiveInfinity;
            foreach (WormOutbreak outbreak in outbreaks)
                foreach (WormBranch branch in outbreak.branches)
                    if (branch.Phase != WormPhase.Resolved)
                        next = Mathf.Min(next, Mathf.Max(.000001f,
                            branch.PhaseDuration - branch.PhaseElapsed));
            foreach (IsolationState isolation in isolations.Values)
                next = Mathf.Min(next, Mathf.Max(.000001f,
                    isolation.ExpiresAt - now));
            return next;
        }

        public void Advance(float deltaSeconds, float now)
        {
            var expired = new List<NodeView>();
            foreach (var pair in isolations)
                if (now + .00001f >= pair.Value.ExpiresAt) expired.Add(pair.Key);
            expired.Sort((a, b) => StringComparer.Ordinal.Compare(a.NodeId, b.NodeId));
            foreach (NodeView node in expired)
            {
                IsolationState state = isolations[node];
                isolations.Remove(node);
                host.Recorder?.RecordIsolation("isolation_expired", state, now);
            }

            foreach (WormOutbreak outbreak in outbreaks)
            {
                WormBranch[] current = outbreak.branches.ToArray();
                Array.Sort(current, (a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
                foreach (WormBranch branch in current)
                {
                    if (branch.Phase == WormPhase.Resolved) continue;
                    branch.PhaseElapsed += deltaSeconds;
                    if (branch.PhaseElapsed + .00001f < branch.PhaseDuration) continue;
                    branch.PhaseElapsed = 0f;
                    switch (branch.Phase)
                    {
                        case WormPhase.Establishing:
                            branch.Phase = WormPhase.Traversing;
                            host.Recorder?.RecordWormBranch("worm_traversing", branch, now);
                            break;
                        case WormPhase.Traversing:
                            if (IsNodeIsolated(branch.NextNode))
                                KillBranch(branch, "isolated_entry", now);
                            else
                            {
                                branch.Phase = WormPhase.Entering;
                                host.Recorder?.RecordWormBranch("worm_head_entered", branch, now);
                            }
                            break;
                        case WormPhase.Entering:
                            if (IsNodeIsolated(branch.NextNode))
                            { KillBranch(branch, "isolated_entry", now); break; }
                            branch.CurrentNode = branch.NextNode;
                            branch.NextNode = null;
                            branch.Link = null;
                            branch.Phase = WormPhase.Acting;
                            host.Recorder?.RecordWormBranch("worm_body_entered", branch, now);
                            ApplyEffect(branch);
                            host.Recorder?.RecordWormBranch("worm_action_started", branch, now);
                            break;
                        case WormPhase.Acting:
                            host.Recorder?.RecordWormBranch("worm_action_completed", branch, now);
                            Propagate(branch, now);
                            KillBranch(branch, "propagated", now);
                            break;
                    }
                }
                FinishIfDone(outbreak, now);
            }
        }

        private void Propagate(WormBranch branch, float now)
        {
            WormOutbreak outbreak = branch.Outbreak;
            var links = new List<PhysicalLinkView>();
            foreach (PhysicalLinkReference reference in host.PhysicalLinks)
                if (reference?.View != null &&
                    (reference.View.NodeA == branch.CurrentNode ||
                     reference.View.NodeB == branch.CurrentNode)) links.Add(reference.View);
            links.Sort((a, b) => StringComparer.Ordinal.Compare(a.LinkId, b.LinkId));
            NodeView parentNode = null;
            if (branch.ParentId != null)
                foreach (WormBranch previous in outbreak.branches)
                    if (previous.Id == branch.ParentId)
                    { parentNode = previous.CurrentNode; break; }
            foreach (PhysicalLinkView link in links)
            {
                NodeView next = link.NodeA == branch.CurrentNode ? link.NodeB : link.NodeA;
                string reason = null;
                float roll = -1f;
                if (next == parentNode) reason = "backtrack";
                else if (outbreak.claimed.Contains(next)) reason = "already_claimed";
                else if (outbreak.claimed.Count >= outbreak.Tuning.maximumNodes) reason = "node_budget";
                else
                {
                    roll = (float)outbreak.random.NextDouble();
                    if (roll >= outbreak.Tuning.propagationProbability)
                        reason = "probability";
                }
                bool selected = reason == null;
                host.Recorder?.RecordWormDecision(outbreak, branch, link, next,
                    selected, roll, reason, now);
                if (!selected) continue;
                outbreak.claimed.Add(next);
                NewBranch(outbreak, branch, branch.CurrentNode, next, link);
            }
        }

        private void KillBranch(WormBranch branch, string reason, float now)
        {
            if (branch.Phase == WormPhase.Resolved) return;
            branch.TerminationReason = reason;
            branch.Phase = WormPhase.Resolved;
            branch.PhaseElapsed = 0f;
            host.Recorder?.RecordWormBranch("worm_branch_terminated", branch, now);
        }

        private void FinishIfDone(WormOutbreak outbreak, float now)
        {
            if (outbreak.completed) return;
            foreach (WormBranch branch in outbreak.branches)
                if (branch.Phase != WormPhase.Resolved) return;
            outbreak.completed = true;
            host.Recorder?.RecordWormOutbreak("worm_outbreak_completed", outbreak, now);
        }

        public bool TryIsolate(string nodeId, string command, out string error)
        {
            NodeResources node = host?.FindNodeResources(nodeId);
            if (node == null) { error = "Unknown node: " + nodeId; return false; }
            float now = host.SimulationTimeSeconds;
            if (isolations.ContainsKey(node.View))
            { error = "Already isolated: " + node.View.NodeId; return false; }
            if (cooldowns.TryGetValue(node.View, out float until) && now < until - .00001f)
            { error = "Isolation cooling down at " + node.View.NodeId; return false; }
            var state = new IsolationState { Node = node, ActivatedAt = now,
                ExpiresAt = now + isolationSeconds,
                CooldownUntil = now + isolationCooldownSeconds };
            if (command != null)
                state.ActionSequence = host.Recorder?.RecordPlayerAction(command, null,
                    true, node.View.NodeId, "isolation_activated") ?? 0;
            isolations.Add(node.View, state);
            cooldowns[node.View] = state.CooldownUntil;
            foreach (WormOutbreak outbreak in outbreaks)
            {
                foreach (WormBranch branch in outbreak.branches)
                    if (branch.Phase == WormPhase.Acting && branch.CurrentNode == node.View ||
                        branch.Phase == WormPhase.Entering && branch.NextNode == node.View)
                    {
                        KillBranch(branch, "isolated", now);
                        state.WormsKilled++;
                    }
                FinishIfDone(outbreak, now);
            }
            state.ConnectionsCanceled = host.InterruptForIsolation(node);
            host.Recorder?.RecordIsolation("isolation_activated", state, now);
            host.Recorder?.CaptureSnapshot(now);
            error = null;
            return true;
        }

        public bool IsNodeIsolated(NodeView node) => node != null && isolations.ContainsKey(node);
        public IsolationState IsolationAt(NodeView node) => node != null &&
            isolations.TryGetValue(node, out IsolationState state) ? state : null;
    }
}
