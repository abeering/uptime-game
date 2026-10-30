using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Chooses activity at the existing hostile opportunity; owns one short event lifecycle.
    public sealed class HostileActivityDirector : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private ConnectionTrafficDirector traffic;
        [SerializeField, Min(0f)] private float ordinaryWeight = 1f;
        [SerializeField, Min(0f)] private float reconnaissanceWeight;
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

        public ConnectionSpawnPlan QueueAmbientActivity(float atTime)
        {
            if (random == null) ResetActivity(traffic.Seed);
            float total = Mathf.Max(0f, ordinaryWeight) + Mathf.Max(0f, reconnaissanceWeight);
            if (total <= 0f)
            {
                host.Recorder?.RecordHostileActivity(atTime, "none", ordinaryWeight,
                    reconnaissanceWeight, null, null, "ambient");
                return null;
            }
            bool recon = random.NextDouble() * total >= ordinaryWeight;
            ConnectionSpawnPlan plan = recon ? QueueReconnaissance(null, atTime) :
                traffic.GenerateHostilePlan(atTime);
            if (!recon && plan != null) traffic.QueueSpawnPlan(plan, "ambient_hostile");
            host.Recorder?.RecordHostileActivity(atTime, recon ? "reconnaissance" : "ordinary",
                ordinaryWeight, reconnaissanceWeight, plan?.ConnectionId,
                plan?.HostileEventId, "ambient");
            return plan;
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
