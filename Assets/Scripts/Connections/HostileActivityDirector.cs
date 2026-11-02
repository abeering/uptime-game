using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // One hostile opportunity produces exactly one of these outcomes. Non-connection
    // identities stay distinct from connection plans in both scheduling and recording.
    public sealed class AmbientHostileActivityResult
    {
        public string Activity { get; internal set; }
        public float SelectionRoll { get; internal set; }
        public ConnectionSpawnPlan ConnectionPlan { get; internal set; }
        public string ConnectionId => ConnectionPlan?.ConnectionId;
        public string EventId => ConnectionPlan?.HostileEventId;
        public string PayloadId { get; internal set; }
        public string WormOutbreakId { get; internal set; }
        public string OriginNodeId { get; internal set; }
        public string TargetNodeId { get; internal set; }
        public string PayloadProfile { get; internal set; }
        public string WormEffect { get; internal set; }
        public string Reason { get; internal set; }
        public bool Succeeded => ConnectionId != null || PayloadId != null ||
            WormOutbreakId != null;
    }

    // Chooses activity at the existing hostile opportunity; owns one short event lifecycle.
    public sealed class HostileActivityDirector : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private ConnectionTrafficDirector traffic;
        [SerializeField, Min(0f)] private float ordinaryWeight = 1f;
        [SerializeField, Min(0f)] private float reconnaissanceWeight;
        [SerializeField, Min(0f)] private float payloadWeight;
        [SerializeField, Min(0f)] private float wormWeight;
        [SerializeField, Range(0f, 1f)] private float variablePayloadChance = .5f;
        [SerializeField, Range(0f, 1f)] private float scanJammedWormChance = .7f;
        [SerializeField, Range(0f, 1f)] private float followupChance = .4f;
        [SerializeField, Min(0f)] private float followupDelaySeconds = 8f;
        [SerializeField, Min(.01f)] private float portScanLoad = 8f;
        [SerializeField, Min(.01f)] private float portScanWork = 56f;
        [SerializeField, Min(.01f)] private float portScannedSeconds = 20f;

        private sealed class ReconEvent
        {
            public string Id;
            public string Target;
            public string ScanId;
            public bool FollowupSelected;
            public float FollowupAt = -1f;
            public string FollowupId;
            public ConnectionResolution? ScanResolution;
            public ConnectionResolution? FollowupResolution;
            public bool Retired;
        }

        private readonly List<ReconEvent> events = new();
        private System.Random random;
        private int nextEventId;
        private bool visitingEvents;

        public float OrdinaryWeight { get => ordinaryWeight; set => ordinaryWeight = Mathf.Max(0f, value); }
        public float ReconnaissanceWeight { get => reconnaissanceWeight; set => reconnaissanceWeight = Mathf.Max(0f, value); }
        public float PayloadWeight { get => payloadWeight; set => payloadWeight = Mathf.Max(0f, value); }
        public float WormWeight { get => wormWeight; set => wormWeight = Mathf.Max(0f, value); }
        public float VariablePayloadChance { get => variablePayloadChance; set => variablePayloadChance = Mathf.Clamp01(value); }
        public float ScanJammedWormChance { get => scanJammedWormChance; set => scanJammedWormChance = Mathf.Clamp01(value); }
        public float FollowupChance { get => followupChance; set => followupChance = Mathf.Clamp01(value); }
        public float FollowupDelaySeconds { get => followupDelaySeconds; set => followupDelaySeconds = Mathf.Max(0f, value); }
        public float PortScanLoad => portScanLoad;
        public float PortScanWork => portScanWork;
        public float PortScannedSeconds => portScannedSeconds;
        public int ActiveReconnaissanceCount => events.Count;

        private void OnDisable()
        {
            if (host != null) host.ConnectionResolved -= OnConnectionResolved;
        }

        public void ResetActivity(int seed)
        {
            if (host == null) host = GetComponent<SimulationHost>();
            if (traffic == null) traffic = GetComponent<ConnectionTrafficDirector>();
            if (host != null)
            {
                host.ConnectionResolved -= OnConnectionResolved;
                host.ConnectionResolved += OnConnectionResolved;
            }
            random = new System.Random(unchecked(seed ^ (int)0x7265636f));
            events.Clear();
            visitingEvents = false;
            nextEventId = 0;
        }

        public AmbientHostileActivityResult QueueAmbientActivity(float atTime)
        {
            if (random == null) ResetActivity(traffic.Seed);
            float total = Mathf.Max(0f, ordinaryWeight) + Mathf.Max(0f, reconnaissanceWeight) +
                Mathf.Max(0f, payloadWeight) + Mathf.Max(0f, wormWeight);
            var result = new AmbientHostileActivityResult();
            if (total <= 0f)
            {
                result.Activity = "none";
                result.Reason = "no_weighted_activity";
            }
            else
            {
                result.SelectionRoll = (float)random.NextDouble();
                float choice = result.SelectionRoll * total;
                result.Activity = choice < ordinaryWeight ? "ordinary" :
                    choice < ordinaryWeight + reconnaissanceWeight ? "reconnaissance" :
                    choice < ordinaryWeight + reconnaissanceWeight + payloadWeight ?
                        "payload" : "worm";
                try
                {
                    switch (result.Activity)
                    {
                        case "ordinary":
                            result.ConnectionPlan = traffic.GenerateHostilePlan(atTime);
                            if (result.ConnectionPlan != null)
                                traffic.QueueSpawnPlan(result.ConnectionPlan, "ambient_hostile");
                            else result.Reason = traffic.ConnectionIdsExhausted ?
                                "ids_exhausted" : "no_weighted_purpose";
                            break;
                        case "reconnaissance":
                            result.ConnectionPlan = QueueReconnaissance(null, atTime);
                            if (result.ConnectionPlan == null)
                                result.Reason = traffic.ConnectionIdsExhausted ?
                                    "ids_exhausted" : "no_scan_plan";
                            break;
                        case "payload":
                            SpawnAmbientPayload(result);
                            break;
                        case "worm":
                            SpawnAmbientWorm(result);
                            break;
                    }
                }
                catch (InvalidOperationException error)
                {
                    result.Reason = "spawn_unavailable: " + error.Message;
                }
                if (result.ConnectionPlan != null)
                {
                    result.OriginNodeId = result.ConnectionPlan.Route[0].NodeId;
                    result.TargetNodeId = result.ConnectionPlan.Target.NodeId;
                }
            }
            host.Recorder?.RecordHostileActivity(host.SimulationTimeSeconds, atTime,
                result, this);
            return result;
        }

        private void SpawnAmbientPayload(AmbientHostileActivityResult result)
        {
            if (host.Payloads == null)
            { result.Reason = "payload_director_unavailable"; return; }
            var origins = EligibleOrigins(requireWormAvailability: false);
            if (origins.Count == 0)
            { result.Reason = "no_reachable_internal_target"; return; }
            NodeView origin = ChooseOrigin(origins);
            var targets = ReachableInternalTargets(origin);
            NodeView target = targets[random.Next(targets.Count)];
            PayloadMotionProfile profile = random.NextDouble() < variablePayloadChance ?
                PayloadMotionProfile.Variable : PayloadMotionProfile.Steady;
            result.OriginNodeId = origin.NodeId;
            result.TargetNodeId = target.NodeId;
            result.PayloadProfile = profile.ToString();
            result.PayloadId = host.Payloads.Spawn(target.NodeId, profile, origin.NodeId).Id;
        }

        private void SpawnAmbientWorm(AmbientHostileActivityResult result)
        {
            if (host.Worms == null)
            { result.Reason = "worm_director_unavailable"; return; }
            var origins = EligibleOrigins(requireWormAvailability: true);
            if (origins.Count == 0)
            { result.Reason = "no_available_ingress"; return; }
            NodeView origin = ChooseOrigin(origins);
            WormEffect effect = random.NextDouble() < scanJammedWormChance ?
                WormEffect.ScanJammed : WormEffect.PortScanned;
            result.OriginNodeId = origin.NodeId;
            result.WormEffect = effect.ToString();
            result.WormOutbreakId = host.Worms.Spawn(origin.NodeId, effect).Id;
        }

        private List<(NodeView node, float weight)> EligibleOrigins(bool requireWormAvailability)
        {
            var result = new List<(NodeView, float)>();
            foreach (AmbientOriginWeight entry in traffic.EffectiveAmbientOrigins)
            {
                if (entry == null || entry.weight <= 0f) continue;
                NodeView node = host.FindNodeResources(entry.nodeId)?.View;
                if (node == null || requireWormAvailability &&
                    host.Worms.IsNodeIsolated(node)) continue;
                if (!requireWormAvailability && ReachableInternalTargets(node).Count == 0)
                    continue;
                result.Add((node, entry.weight));
            }
            return result;
        }

        private NodeView ChooseOrigin(List<(NodeView node, float weight)> choices)
        {
            float total = 0f;
            foreach (var choice in choices) total += choice.weight;
            float roll = (float)random.NextDouble() * total;
            foreach (var choice in choices)
            {
                roll -= choice.weight;
                if (roll < 0f) return choice.node;
            }
            return choices[choices.Count - 1].node;
        }

        private List<NodeView> ReachableInternalTargets(NodeView origin)
        {
            var visited = new HashSet<NodeView> { origin };
            var pending = new Queue<NodeView>();
            pending.Enqueue(origin);
            while (pending.Count > 0)
            {
                NodeView current = pending.Dequeue();
                foreach (PhysicalLinkReference edge in host.PhysicalLinks)
                {
                    NodeView next = edge?.View?.NodeA == current ? edge.View.NodeB :
                        edge?.View?.NodeB == current ? edge.View.NodeA : null;
                    if (next != null && visited.Add(next)) pending.Enqueue(next);
                }
            }
            var targets = new List<NodeView>();
            foreach (NodeView node in visited)
            {
                bool ingress = false;
                foreach (AmbientOriginWeight entry in traffic.EffectiveAmbientOrigins)
                    if (entry != null && string.Equals(entry.nodeId, node.NodeId,
                        StringComparison.OrdinalIgnoreCase)) { ingress = true; break; }
                if (!ingress) targets.Add(node);
            }
            targets.Sort((a, b) => StringComparer.Ordinal.Compare(a.NodeId, b.NodeId));
            return targets;
        }

        public ConnectionSpawnPlan QueuePortScan(string targetNodeId)
        {
            if (random == null) ResetActivity(traffic.Seed);
            ConnectionSpawnPlan plan = traffic.GeneratePortScanPlan(host.SimulationTimeSeconds,
                portScanLoad, portScanWork, portScannedSeconds, targetNodeId);
            if (plan != null) traffic.QueueSpawnPlan(plan, "manual_port_scan");
            return plan;
        }

        public ConnectionSpawnPlan QueueReconnaissance(string targetNodeId,
            float? atTime = null)
        {
            if (random == null) ResetActivity(traffic.Seed);
            float scheduled = atTime ?? host.SimulationTimeSeconds;
            ConnectionSpawnPlan scan = traffic.GeneratePortScanPlan(scheduled,
                portScanLoad, portScanWork, portScannedSeconds, targetNodeId);
            if (scan == null) return null;
            var item = new ReconEvent
            {
                Id = "recon-" + (++nextEventId), Target = scan.Target.NodeId,
                ScanId = scan.ConnectionId, FollowupSelected = random.NextDouble() < followupChance
            };
            scan = scan.ForEvent(item.Id);
            traffic.QueueSpawnPlan(scan, atTime.HasValue ? "ambient_reconnaissance" : "explicit_reconnaissance");
            events.Add(item);
            host.Recorder?.RecordReconnaissance("recon_planned", host.SimulationTimeSeconds,
                item.Id, item.Target, item.ScanId, item.FollowupSelected,
                followupDelaySeconds, null, null);
            return scan;
        }

        public void Tick(float now)
        {
            visitingEvents = true;
            try
            {
                foreach (ReconEvent item in events)
                {
                    if (item.Retired || item.FollowupAt < 0f || item.FollowupId != null ||
                        now + .00001f < item.FollowupAt) continue;
                    try
                    {
                        ConnectionSpawnPlan plan = traffic.GenerateTargetedInfectorPlan(now,
                            item.Target);
                        if (plan == null)
                        {
                            RecordUnavailable(item, now, "no_plan");
                            continue;
                        }
                        plan = plan.ForEvent(item.Id);
                        traffic.QueueSpawnPlan(plan, "recon_followup");
                        item.FollowupId = plan.ConnectionId;
                        host.Recorder?.RecordReconnaissance("recon_followup_queued", now,
                            item.Id, item.Target, item.ScanId, true,
                            followupDelaySeconds, item.FollowupId, null);
                    }
                    catch (InvalidOperationException error)
                    {
                        RecordUnavailable(item, now, error.Message);
                    }
                }
            }
            finally { visitingEvents = false; RetireFinished(); }
        }

        private void RecordUnavailable(ReconEvent item, float now, string reason)
        {
            host.Recorder?.RecordReconnaissance("recon_followup_unavailable", now,
                item.Id, item.Target, item.ScanId, true,
                followupDelaySeconds, null, reason);
            item.Retired = true;
        }

        private void OnConnectionResolved(LogicalConnectionState state,
            ConnectionResolution reason)
        {
            foreach (ReconEvent item in events)
            {
                if (item.Retired) continue;
                if (item.ScanId == state.ConnectionId && !item.ScanResolution.HasValue)
                {
                    item.ScanResolution = reason;
                    host.Recorder?.RecordReconnaissance("recon_scan_resolved",
                        host.SimulationTimeSeconds, item.Id, item.Target,
                        item.ScanId, item.FollowupSelected, followupDelaySeconds,
                        item.FollowupId, reason.ToString());
                    if (item.FollowupSelected)
                    {
                        item.FollowupAt = host.SimulationTimeSeconds + followupDelaySeconds;
                        host.Recorder?.RecordReconnaissance("recon_followup_scheduled",
                            host.SimulationTimeSeconds, item.Id, item.Target,
                            item.ScanId, true, followupDelaySeconds, null,
                            reason.ToString(), item.FollowupAt);
                    }
                    else item.Retired = true;
                }
                else if (item.FollowupId == state.ConnectionId &&
                    !item.FollowupResolution.HasValue)
                {
                    item.FollowupResolution = reason;
                    host.Recorder?.RecordReconnaissance("recon_followup_resolved",
                        host.SimulationTimeSeconds, item.Id, item.Target,
                        item.ScanId, true, followupDelaySeconds,
                        item.FollowupId, reason.ToString());
                    item.Retired = true;
                }
            }
            if (!visitingEvents) RetireFinished();
        }

        private void RetireFinished()
        {
            for (int i = events.Count - 1; i >= 0; i--)
                if (events[i].Retired) events.RemoveAt(i);
        }
    }
}
