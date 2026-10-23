using System;

namespace Uptime.Connections
{
    // A fully generated arrival. Copies its route and operations so a queued plan cannot drift.
    public sealed class ConnectionSpawnPlan
    {
        public float ScheduledSpawnTime { get; }
        public string ConnectionId { get; }
        public NodeView Origin => Route.Length > 0 ? Route[0] : null;
        public NodeView Target => Route.Length > 0 ? Route[Route.Length - 1] : null;
        public NodeView[] Route { get; }
        public string ProfileId { get; }
        public bool IsHostile { get; }
        public string ClaimedPurpose { get; }
        public OperationStage[] Operations { get; }

        public ConnectionSpawnPlan(float scheduledSpawnTime, string connectionId, NodeView[] route,
            string profileId, string claimedPurpose, OperationStage[] operations,
            bool isHostile = false)
        {
            if (float.IsNaN(scheduledSpawnTime) || float.IsInfinity(scheduledSpawnTime) || scheduledSpawnTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(scheduledSpawnTime));
            ScheduledSpawnTime = scheduledSpawnTime;
            ConnectionId = connectionId;
            Route = route != null ? (NodeView[])route.Clone() : Array.Empty<NodeView>();
            ProfileId = profileId;
            IsHostile = isHostile;
            ClaimedPurpose = claimedPurpose;
            Operations = operations != null ? (OperationStage[])operations.Clone() : Array.Empty<OperationStage>();
        }
    }
}
