using UnityEngine;
using System.Collections.Generic;

public enum LevelFailureReason
{
    None,
    TrafficLossExceeded,
    CoreNodeCompromised
}

public enum LevelFlowState
{
    Stable,
    Elevated,
    Danger,
    Critical,
    Failed
}

public class LevelDirector : MonoBehaviour
{
    [Header("References")]
    public TrafficDirector trafficDirector;
    public NotificationDirector notificationDirector;
    [SerializeField] private GlitchDirector glitchDirector;

    [Header("Failure Thresholds")]
    [Min(0)] public int benignLossWeight = 1;
    [Min(0)] public int priorityLossWeight = 3;
    [Min(1)] public int trafficLossThreshold = 10;

    [Header("Warnings")]
    [SerializeField, Range(0f, 1f)] private float elevatedAtFraction = 0.50f;
    [SerializeField, Range(0f, 1f)] private float dangerAtFraction = 0.75f;
    [SerializeField, Range(0f, 1f)] private float criticalAtFraction = 0.90f;

    private bool elevatedNotificationSent = false;
    private bool dangerNotificationSent = false;
    private bool criticalNotificationSent = false;
    private LevelFlowState flowState = LevelFlowState.Stable;

    public float TrafficLossFraction => trafficLossThreshold <= 0
        ? 0f
        : Mathf.Clamp01((float)weightedTrafficLoss / trafficLossThreshold);

    public LevelFlowState FlowState => levelFailed
        ? LevelFlowState.Failed
        : flowState;

    [Header("Debug")]
    public bool logFailureState = true;

    private readonly List<ILevelEvent> events = new();
    private LevelEventContext context;
    private int currentTick;

    private int weightedTrafficLoss = 0;
    private bool levelFailed = false;
    private LevelFailureReason failureReason = LevelFailureReason.None;
    private NodeView failedCoreNode;

    public int WeightedTrafficLoss => weightedTrafficLoss;
    public int TrafficLossThreshold => trafficLossThreshold;
    public bool LevelFailed => levelFailed;
    public LevelFailureReason FailureReason => failureReason;
    public NodeView FailedCoreNode => failedCoreNode;

    public void Initialize()
    {
        context = new LevelEventContext(trafficDirector, notificationDirector);
        glitchDirector?.SetAll(false);

        events.Clear();
        events.Add(new InfectionBurstEvent(startTick: 50, duration: 40));
        // events.Add(new DdosSwarmEvent(startTick: 120, secondBurstTick: 300));

        weightedTrafficLoss = 0;
        levelFailed = false;
        failureReason = LevelFailureReason.None;
        failedCoreNode = null;
        elevatedNotificationSent = false;
        dangerNotificationSent = false;
        criticalNotificationSent = false;
        flowState = LevelFlowState.Stable;
    }

    public void ProcessTick(int tick)
    {
        if (levelFailed)
            return;

        currentTick = tick;

        foreach (var e in events)
        {
            if (!e.IsActive(tick))
                continue;

            int localTick = tick - e.StartTick;
            e.OnTick(tick, localTick, context);
        }
    }

    public void RecordPacketRemoval(PacketView packet, PacketRemovalReason reason, NodeView node = null)
    {
        if (levelFailed || packet == null)
            return;

        if (reason == PacketRemovalReason.Arrived)
            return;

        int lossWeight = GetTrafficLossWeight(packet);
        if (lossWeight <= 0)
            return;

        weightedTrafficLoss += lossWeight;
        UpdateFlowStateAndNotifications();

        if (logFailureState)
        {
            Debug.Log(
                $"[LevelDirector] traffic loss +{lossWeight} from {packet.PacketId} ({packet.trueClass}) " +
                $"total={weightedTrafficLoss}/{trafficLossThreshold}"
            );
        }

        if (weightedTrafficLoss >= trafficLossThreshold)
        {
            FailLevel(LevelFailureReason.TrafficLossExceeded);
        }
    }

    public void RecordNodeCompromised(NodeView node, PacketView packet = null, InfectionPayload payload = null)
    {
        if (levelFailed || node == null)
            return;

        if (!node.isCritical)
            return;

        if (logFailureState)
        {
            Debug.Log(
                $"[LevelDirector] core node compromised: {node.nodeId}" +
                (packet != null ? $" by {packet.PacketId}" : "")
            );
        }

        FailLevel(LevelFailureReason.CoreNodeCompromised, node);
    }

    private int GetTrafficLossWeight(PacketView packet)
    {
        if (packet == null)
            return 0;

        switch (packet.trueClass)
        {
            case PacketClass.Benign:
                return benignLossWeight;

            case PacketClass.Priority:
                return priorityLossWeight;

            default:
                return 0;
        }
    }

    private void FailLevel(LevelFailureReason reason, NodeView coreNode = null)
    {
        if (levelFailed)
            return;

        levelFailed = true;
        failureReason = reason;
        failedCoreNode = coreNode;

        if (logFailureState)
        {
            string detail = reason == LevelFailureReason.CoreNodeCompromised && coreNode != null
                ? $" core={coreNode.nodeId}"
                : $" loss={weightedTrafficLoss}/{trafficLossThreshold}";

            Debug.Log($"[LevelDirector] LEVEL FAILED - {reason}{detail}");
        }

        flowState = LevelFlowState.Failed;

        if (reason == LevelFailureReason.CoreNodeCompromised && coreNode != null)
        {
            notificationDirector?.PushDebugMessage("STATUS", $"CORE LOST: {coreNode.nodeId.ToUpper()}");
        }
        else
        {
            notificationDirector?.PushDebugMessage("STATUS", "FAILURE: traffic loss threshold exceeded.");
        }

        if (GameController.Instance != null)
            GameController.Instance.PauseTicks();
    }

    private void UpdateFlowStateAndNotifications()
    {
        float fraction = TrafficLossFraction;

        if (fraction >= criticalAtFraction)
        {
            flowState = LevelFlowState.Critical;

            if (!criticalNotificationSent)
            {
                criticalNotificationSent = true;
                notificationDirector?.PushDebugMessage("STATUS", "Operational traffic loss is at critical levels.");
            }

            return;
        }

        if (fraction >= dangerAtFraction)
        {
            flowState = LevelFlowState.Danger;

            if (!dangerNotificationSent)
            {
                dangerNotificationSent = true;
                notificationDirector?.PushDebugMessage("STATUS", "Traffic degradation is becoming dangerous.");
            }

            return;
        }

        if (fraction >= elevatedAtFraction)
        {
            flowState = LevelFlowState.Elevated;

            if (!elevatedNotificationSent)
            {
                elevatedNotificationSent = true;
                notificationDirector?.PushDebugMessage("STATUS", "Flow degradation rising.");
            }

            return;
        }

        flowState = LevelFlowState.Stable;
    }

}