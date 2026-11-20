using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class IntegrityTuning
    {
        public bool enabled;
        public string[] criticalNodeIds = { "DB", "AUTH" };
        [Min(1f)] public float severeLoadRatio = 1.5f;
        [Min(.02f)] public float warningSeconds = 10f;
    }

    [Serializable]
    public sealed class IntegrityNodeSnapshot
    {
        public string id;
        public bool external, critical, revealed, connected, overloaded;
        public int integrity;
        public float overloadElapsed, overloadRemaining;
    }

    [Serializable]
    public sealed class IntegrityLinkSnapshot
    {
        public string id, nodeA, nodeB;
        public bool connected;
    }

    [Serializable]
    public sealed class IntegritySnapshot
    {
        public IntegrityNodeSnapshot[] nodes;
        public IntegrityLinkSnapshot[] links;
        public string[] connectedLinks;
        public IntegrityBreach breach;
        public bool breached;
    }

    [Serializable]
    public sealed class IntegrityEvent
    {
        public string type, target, source, cause;
        public float time;
        public int before, after;
        public bool visible, revealed;
    }

    [Serializable]
    public sealed class IntegrityBreach
    {
        public string criticalNode;
        public string[] path;
        public string[] links;
        public float time;
        public IntegrityEvent trigger;
    }

    public sealed class IntegrityNode
    {
        public NodeResources Resources { get; }
        public bool External { get; }
        public bool Critical { get; }
        public int Value { get; internal set; } = 2;
        public bool Revealed { get; internal set; }
        public bool Connected { get; internal set; }
        public bool Overloaded { get; internal set; }
        public float OverloadElapsed { get; internal set; }
        public bool Compromised => !External && Value == 0;
        internal IntegrityNode(NodeResources node, bool external, bool critical)
        { Resources = node; External = external; Critical = critical; if (external) Value = -1; }
    }

    // Simulation-owned state. Presentation never infers damage from colors or infection symptoms.
    public sealed class IntegrityState
    {
        public const int Maximum = 2;
        private readonly IntegrityTuning tuning;
        private readonly Dictionary<NodeResources, IntegrityNode> byNode;
        private readonly PhysicalLinkReference[] links;
        private readonly HashSet<string> connectedLinks = new HashSet<string>();
        private IntegrityEvent pendingTrigger;
        private IntegrityBreach pendingBreach;
        public IEnumerable<IntegrityNode> Nodes => byNode.Values.OrderBy(n => n.Resources.View.NodeId, StringComparer.Ordinal);
        public IntegrityBreach Breach { get; private set; }
        public event Action<IntegrityEvent> Occurred;
        public float WarningSeconds => tuning.warningSeconds;
        public float SevereLoadRatio => tuning.severeLoadRatio;
        public IntegrityNode For(NodeResources node) => node != null && byNode.TryGetValue(node, out var state) ? state : null;
        public bool IsConnectedLink(string id) => connectedLinks.Contains(id);

        public IntegrityState(NodeResources[] nodes, PhysicalLinkReference[] physicalLinks,
            Func<NodeResources, bool> external, IntegrityTuning settings)
        {
            tuning = settings;
            if (!float.IsFinite(tuning.severeLoadRatio) || tuning.severeLoadRatio < 1f ||
                !float.IsFinite(tuning.warningSeconds) || tuning.warningSeconds <= 0f)
                throw new InvalidOperationException("Integrity needs finite overload ratio >= 1 and positive warning duration.");
            links = physicalLinks ?? Array.Empty<PhysicalLinkReference>();
            var critical = new HashSet<string>(settings.criticalNodeIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            byNode = nodes.ToDictionary(n => n, n => new IntegrityNode(n, external(n), critical.Contains(n.View.NodeId)));
            foreach (string id in critical)
                if (!Nodes.Any(n => n.Critical && !n.External && string.Equals(n.Resources.View.NodeId, id, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Critical node must be an internal node: " + id);
            RebuildRegion();
        }

        public bool Damage(NodeResources resources, string source, string cause, float time, bool overt)
        {
            IntegrityNode node = For(resources);
            if (Breach != null || node == null || node.External || node.Value == 0) return false;
            int before = node.Value;
            node.Value--;
            bool visible = overt || node.Revealed || before < Maximum || node.Value == 0;
            node.Revealed |= overt || node.Value == 0;
            var entry = new IntegrityEvent { type = "integrity_damage", target = resources.View.NodeId,
                source = source, cause = cause, time = time, before = before, after = node.Value,
                visible = visible, revealed = node.Revealed };
            if (node.Value == 0)
            {
                node.Overloaded = false;
                node.OverloadElapsed = 0;
                RebuildRegion();
                // Retain the first causal event that establishes a complete path at this boundary.
                if (pendingTrigger == null)
                {
                    pendingBreach = FindBreach();
                    if (pendingBreach != null) pendingTrigger = entry;
                }
            }
            Occurred?.Invoke(entry);
            return true;
        }

        public void Reveal(NodeResources resources, string source, float time)
        {
            IntegrityNode node = For(resources);
            if (Breach != null || node == null || node.External) return;
            node.Revealed = true;
            Occurred?.Invoke(new IntegrityEvent { type = "integrity_revealed", target = resources.View.NodeId,
                source = source, time = time, before = node.Value, after = node.Value, revealed = true });
        }

        public void AdvanceOverload(float seconds, float boundaryTime)
        {
            if (Breach != null) return;
            foreach (IntegrityNode node in Nodes)
            {
                if (node.External || node.Compromised) continue;
                bool severe = node.Resources.RequestedLoad > node.Resources.LoadCapacity * tuning.severeLoadRatio;
                if (severe != node.Overloaded)
                    Occurred?.Invoke(new IntegrityEvent { type = severe ? "integrity_overload_started" : "integrity_overload_reset",
                        target = node.Resources.View.NodeId, time = boundaryTime, source = "overload" });
                node.Overloaded = severe;
                if (!severe) { node.OverloadElapsed = 0; continue; }
                node.OverloadElapsed += seconds;
                if (node.OverloadElapsed + .00001f < tuning.warningSeconds) continue;
                node.OverloadElapsed = 0;
                Damage(node.Resources, "overload", "sustained_severe_load", boundaryTime, true);
            }
        }

        // Called only after all effects at an authoritative simulation boundary have settled.
        public bool LatchBreach(float time)
        {
            if (Breach != null || pendingTrigger == null) return false;
            Breach = pendingBreach;
            Breach.time = time;
            Breach.trigger = pendingTrigger;
            return true;
        }

        private IEnumerable<(IntegrityNode node, string link)> Neighbors(IntegrityNode node)
        {
            foreach (PhysicalLinkReference edge in links.OrderBy(e => e.View.LinkId, StringComparer.Ordinal))
            {
                NodeView other = edge.View.NodeA == node.Resources.View ? edge.View.NodeB :
                    edge.View.NodeB == node.Resources.View ? edge.View.NodeA : null;
                IntegrityNode adjacent = other == null ? null : byNode.Values.FirstOrDefault(n => n.Resources.View == other);
                if (adjacent != null) yield return (adjacent, edge.View.LinkId);
            }
        }

        private void RebuildRegion()
        {
            connectedLinks.Clear();
            var queue = new Queue<IntegrityNode>();
            foreach (IntegrityNode node in Nodes)
            {
                node.Connected = node.External;
                if (node.External) queue.Enqueue(node);
            }
            while (queue.Count > 0)
            {
                IntegrityNode current = queue.Dequeue();
                foreach (var next in Neighbors(current))
                {
                    if (!next.node.External && !next.node.Compromised) continue;
                    connectedLinks.Add(next.link);
                    if (next.node.Connected) continue;
                    next.node.Connected = true;
                    queue.Enqueue(next.node);
                }
            }
        }

        private IntegrityBreach FindBreach()
        {
            var parents = new Dictionary<IntegrityNode, (IntegrityNode node, string link)>();
            var queue = new Queue<IntegrityNode>();
            foreach (IntegrityNode node in Nodes.Where(n => n.External))
            { queue.Enqueue(node); parents[node] = (null, null); }
            while (queue.Count > 0)
            {
                IntegrityNode current = queue.Dequeue();
                if (current.Critical && current.Compromised)
                {
                    var path = new List<string>();
                    var pathLinks = new List<string>();
                    for (IntegrityNode step = current; step != null; step = parents[step].node)
                    { path.Add(step.Resources.View.NodeId); if (parents[step].link != null) pathLinks.Add(parents[step].link); }
                    path.Reverse(); pathLinks.Reverse();
                    return new IntegrityBreach { criticalNode = current.Resources.View.NodeId, path = path.ToArray(), links = pathLinks.ToArray() };
                }
                foreach (var next in Neighbors(current))
                {
                    if (parents.ContainsKey(next.node) || !next.node.External && !next.node.Compromised) continue;
                    parents[next.node] = (current, next.link);
                    queue.Enqueue(next.node);
                }
            }
            return null;
        }

        public IntegritySnapshot Snapshot() => new IntegritySnapshot
        {
            nodes = Nodes.Select(n => new IntegrityNodeSnapshot { id = n.Resources.View.NodeId,
                external = n.External, critical = n.Critical, integrity = n.External ? -1 : n.Value,
                revealed = n.Revealed, connected = n.Connected, overloaded = n.Overloaded,
                overloadElapsed = n.OverloadElapsed,
                overloadRemaining = n.Overloaded ? Mathf.Max(0, tuning.warningSeconds - n.OverloadElapsed) : 0 }).ToArray(),
            links = links.OrderBy(l => l.View.LinkId, StringComparer.Ordinal).Select(l => new IntegrityLinkSnapshot
                { id = l.View.LinkId, nodeA = l.View.NodeA.NodeId, nodeB = l.View.NodeB.NodeId,
                    connected = connectedLinks.Contains(l.View.LinkId) }).ToArray(),
            connectedLinks = connectedLinks.OrderBy(id => id, StringComparer.Ordinal).ToArray(), breach = Breach, breached = Breach != null
        };
    }
}
