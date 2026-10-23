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
        public TimedModifierBehaviorDefinition[] ConnectionBehaviors { get; }

        public ConnectionSpawnPlan(float scheduledSpawnTime, string connectionId, NodeView[] route,
            string profileId, string claimedPurpose, OperationStage[] operations,
            bool isHostile = false,
            TimedModifierBehaviorDefinition[] connectionBehaviors = null)
        {
            if (float.IsNaN(scheduledSpawnTime) || float.IsInfinity(scheduledSpawnTime) || scheduledSpawnTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(scheduledSpawnTime));
            ScheduledSpawnTime = scheduledSpawnTime;
            ConnectionId = connectionId;
            Route = route != null ? (NodeView[])route.Clone() : Array.Empty<NodeView>();
            ProfileId = profileId;
            IsHostile = isHostile;
            ClaimedPurpose = claimedPurpose;
            Operations = new OperationStage[operations?.Length ?? 0];
            for (int i = 0; i < Operations.Length; i++)
            {
                OperationStage stage = operations[i];
                Operations[i] = stage == null ? null : new OperationStage(stage.Operation,
                    stage.TotalWork, stage.DesiredLoad, stage.ActivityDescription,
                    stage.EffectOnCompletion, stage.Behaviors);
            }
            ConnectionBehaviors = OperationStage.CopyBehaviors(connectionBehaviors);
        }
    }
}
