using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ScoreDirector : MonoBehaviour
{
    [Header("Debug")]
    public bool logScoreEvents = true;

    [Header("Scope")]
    [SerializeField] private string currentRunId = "debug-run-001";
    [SerializeField] private string currentLevelId = "level-001";

    [Header("Totals")]
    [SerializeField] private int totalScore = 0;

    [Header("Timing Tuning")]
    [Min(0)] public int earlyThresholdTicks = 2;
    [Min(0)] public int veryLateThresholdTicks = 4;
    public int earlyBonus = 4;
    public int latePenalty = -3;
    public int veryLatePenalty = -8;

    [Header("Event Values")]
    public int healthyPacketDeliveredValue = 10;
    public int healthyPacketLostValue = -10;
    public int priorityPacketDeliveredValue = 18;
    public int priorityPacketLostValue = -18;
    public int threatBlockedValue = 15;
    public int threatReachedNodeValue = -20;
    public int benignBlockedValue = -12;
    public int successfulCleanValue = 12;
    public int failedCleanValue = -8;
    public int blockAtFirstNodeValue = 5;

    private readonly Dictionary<string, PacketTimingSnapshot> packetSnapshots = new();
    private readonly List<ScoreLedgerEntry> ledger = new();

    public string CurrentRunId => currentRunId;
    public string CurrentLevelId => currentLevelId;
    public int TotalScore => totalScore;
    public IReadOnlyList<ScoreLedgerEntry> Ledger => ledger;

    public void SetScoreScope(string runId, string levelId)
    {
        if (!string.IsNullOrWhiteSpace(runId))
            currentRunId = runId;

        if (!string.IsNullOrWhiteSpace(levelId))
            currentLevelId = levelId;
    }

    public void RegisterPacketSpawn(PacketView packet, int spawnTick)
    {
        if (packet == null || string.IsNullOrWhiteSpace(packet.PacketId))
            return;

        packetSnapshots[packet.PacketId] = new PacketTimingSnapshot
        {
            packetId = packet.PacketId,
            packetClass = packet.trueClass,
            spawnTick = spawnTick,
            expectedDurationTicks = ComputeExpectedDurationTicks(packet),
            routeConnectionCount = packet.route != null ? packet.route.Length : 0
        };

        if (logScoreEvents)
        {
            PacketTimingSnapshot snapshot = packetSnapshots[packet.PacketId];
            Debug.Log(
                $"[Score] snapshot {snapshot.packetId} " +
                $"spawn={snapshot.spawnTick} expected={snapshot.expectedDurationTicks} " +
                $"routeEdges={snapshot.routeConnectionCount} class={snapshot.packetClass}"
            );
        }
    }

    public void RecordPacketRemoval(PacketView packet, PacketRemovalReason reason, int currentTick, NodeView node = null)
    {
        if (packet == null || string.IsNullOrWhiteSpace(packet.PacketId))
            return;

        if (!packetSnapshots.TryGetValue(packet.PacketId, out PacketTimingSnapshot snapshot))
            return;

        ScoreEventType? eventType = ResolvePacketEventType(packet, reason);
        if (!eventType.HasValue)
        {
            packetSnapshots.Remove(packet.PacketId);
            return;
        }

        int actualDurationTicks = Mathf.Max(0, currentTick - snapshot.spawnTick);
        bool wasFirstNodeOutcome = node != null && packet.nodesReachedCount == 1;

        ScoreEventContext context = new ScoreEventContext
        {
            packetId = packet.PacketId,
            nodeId = node != null ? node.nodeId : null,
            infectionType = packet.GetPrimaryInfectionType().ToString(),
            runId = currentRunId,
            levelId = currentLevelId,
            reason = ToReasonString(reason),
            tick = currentTick,
            spawnTick = snapshot.spawnTick,
            expectedDurationTicks = snapshot.expectedDurationTicks,
            actualDurationTicks = actualDurationTicks,
            timingDeltaTicks = snapshot.expectedDurationTicks - actualDurationTicks,
            nodesReachedCount = packet.nodesReachedCount,
            routeConnectionCount = snapshot.routeConnectionCount,
            wasFirstNodeOutcome = wasFirstNodeOutcome,
            timingBand = ScoreTimingBand.None
        };

        RecordEvent(eventType.Value, context);

        if (eventType.Value == ScoreEventType.ThreatBlocked && wasFirstNodeOutcome)
        {
            RecordEvent(
                ScoreEventType.BlockAtFirstNode,
                CloneContext(context)
            );
        }

        packetSnapshots.Remove(packet.PacketId);
    }

    public void RecordCleanResult(NodeView node, InfectionType infectionType, bool wasSuccessful, int currentTick)
    {
        ScoreEventContext context = new ScoreEventContext
        {
            nodeId = node != null ? node.nodeId : null,
            infectionType = infectionType.ToString(),
            runId = currentRunId,
            levelId = currentLevelId,
            reason = wasSuccessful ? "clean-success" : "clean-failed",
            tick = currentTick,
            timingBand = ScoreTimingBand.None
        };

        RecordEvent(
            wasSuccessful ? ScoreEventType.SuccessfulClean : ScoreEventType.FailedClean,
            context
        );
    }

    public int GetScoreForLevel(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return 0;

        int total = 0;

        for (int i = 0; i < ledger.Count; i++)
        {
            if (ledger[i].context.levelId == levelId)
                total += ledger[i].finalValue;
        }

        return total;
    }

    public int GetScoreForRun(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return 0;

        int total = 0;

        for (int i = 0; i < ledger.Count; i++)
        {
            if (ledger[i].context.runId == runId)
                total += ledger[i].finalValue;
        }

        return total;
    }

    public void AppendOperationsPanel(StringBuilder sb)
    {
        if (sb == null)
            return;

        sb.AppendLine("=== SCORE ===");
        sb.AppendLine($"total   {totalScore}");
        sb.AppendLine($"level   {GetScoreForLevel(currentLevelId)}");
        sb.AppendLine($"run     {GetScoreForRun(currentRunId)}");

        int throughput = GetCategoryScore(ScoreCategory.Throughput);
        int threat = GetCategoryScore(ScoreCategory.ThreatHandling);
        int mistakes = GetCategoryScore(ScoreCategory.Mistakes);
        int maintenance = GetCategoryScore(ScoreCategory.Maintenance);

        sb.AppendLine($"flow    {FormatSigned(throughput)}");
        sb.AppendLine($"threat  {FormatSigned(threat)}");
        sb.AppendLine($"mistake {FormatSigned(mistakes)}");
        sb.AppendLine($"clean   {FormatSigned(maintenance)}");

        if (ledger.Count == 0)
        {
            sb.AppendLine("recent  none");
            return;
        }

        int count = Mathf.Min(4, ledger.Count);
        sb.AppendLine("recent");

        for (int i = ledger.Count - count; i < ledger.Count; i++)
        {
            ScoreLedgerEntry entry = ledger[i];

            string packetId = string.IsNullOrWhiteSpace(entry.context.packetId)
                ? "--"
                : entry.context.packetId;

            string timingSuffix = entry.context.timingBand != ScoreTimingBand.None
                ? $" [{entry.context.timingBand.ToString().ToLowerInvariant()}]"
                : "";

            sb.AppendLine(
                $"  {packetId}  {entry.eventType}  {FormatSigned(entry.finalValue)}{timingSuffix}"
            );
        }
    }

    [ContextMenu("Score Debug / Dump Ledger")]
    public void DebugDumpLedger()
    {
        Debug.Log("===== SCORE LEDGER =====");

        if (ledger.Count == 0)
        {
            Debug.Log("[Score] ledger empty");
            return;
        }

        for (int i = 0; i < ledger.Count; i++)
        {
            ScoreLedgerEntry entry = ledger[i];

            Debug.Log(
                $"[{i}] {entry.eventType} " +
                $"cat={entry.category} " +
                $"packet={entry.context.packetId ?? "--"} " +
                $"node={entry.context.nodeId ?? "--"} " +
                $"base={entry.baseValue} final={entry.finalValue} " +
                $"delta={entry.context.timingDeltaTicks?.ToString() ?? "--"} " +
                $"band={entry.context.timingBand}"
            );
        }
    }

    [ContextMenu("Score Debug / Clear Ledger")]
    public void DebugClearLedger()
    {
        ledger.Clear();
        packetSnapshots.Clear();
        totalScore = 0;
        Debug.Log("[Score] ledger cleared");
    }

    private void RecordEvent(ScoreEventType eventType, ScoreEventContext context)
    {
        ScoreLedgerEntry entry = new ScoreLedgerEntry
        {
            category = ResolveCategory(eventType),
            eventType = eventType,
            context = context,
            baseValue = GetBaseValue(eventType),
            finalValue = GetBaseValue(eventType)
        };

        ApplyModifiers(entry);

        ledger.Add(entry);
        totalScore += entry.finalValue;

        if (logScoreEvents)
        {
            Debug.Log(
                $"[Score] {entry.eventType} cat={entry.category} " +
                $"base={entry.baseValue} final={entry.finalValue} " +
                $"packet={entry.context.packetId ?? "--"} node={entry.context.nodeId ?? "--"} " +
                $"reason={entry.context.reason}"
            );
        }
    }

    private void ApplyModifiers(ScoreLedgerEntry entry)
    {
        bool isDelivery =
            entry.eventType == ScoreEventType.HealthyPacketDelivered ||
            entry.eventType == ScoreEventType.PriorityPacketDelivered;

        if (!isDelivery)
            return;

        if (!entry.context.expectedDurationTicks.HasValue || !entry.context.actualDurationTicks.HasValue)
            return;

        int expected = entry.context.expectedDurationTicks.Value;
        int actual = entry.context.actualDurationTicks.Value;
        int deltaTicks = expected - actual;

        entry.context.timingDeltaTicks = deltaTicks;

        int modifierDelta = 0;
        ScoreTimingBand band = ScoreTimingBand.OnTime;
        string label = "on-time";

        if (deltaTicks >= earlyThresholdTicks)
        {
            modifierDelta = earlyBonus;
            band = ScoreTimingBand.Early;
            label = "early";
        }
        else if (deltaTicks < 0)
        {
            int lateBy = -deltaTicks;

            if (lateBy >= veryLateThresholdTicks)
            {
                modifierDelta = veryLatePenalty;
                band = ScoreTimingBand.VeryLate;
                label = "very-late";
            }
            else
            {
                modifierDelta = latePenalty;
                band = ScoreTimingBand.Late;
                label = "late";
            }
        }

        entry.context.timingBand = band;

        if (modifierDelta == 0)
            return;

        entry.modifiers.Add(new ScoreModifierEntry(
            ScoreModifierType.Timing,
            label,
            modifierDelta
        ));

        entry.finalValue += modifierDelta;
    }

    private int ComputeExpectedDurationTicks(PacketView packet)
    {
        if (packet == null || packet.route == null || packet.route.Length == 0)
            return 0;

        int totalTicks = 0;

        for (int i = 0; i < packet.route.Length; i++)
        {
            RouteStep step = packet.route[i];
            if (step == null || step.connection == null)
                continue;

            ConnectionView connection = step.connection;
            int moveIntervalTicks = Mathf.Max(1, packet.baseSpeed * connection.EffectiveLatency);
            totalTicks += connection.lengthSteps * moveIntervalTicks;
        }

        return totalTicks;
    }

    private ScoreEventType? ResolvePacketEventType(PacketView packet, PacketRemovalReason reason)
    {
        if (packet == null)
            return null;

        bool isBenign = packet.trueClass == PacketClass.Benign;
        bool isThreat = packet.trueClass == PacketClass.Threat;
        bool isPriority = packet.trueClass == PacketClass.Priority;

        switch (reason)
        {
            case PacketRemovalReason.Arrived:
                if (isPriority) return ScoreEventType.PriorityPacketDelivered;
                if (isBenign) return ScoreEventType.HealthyPacketDelivered;
                return null;

            case PacketRemovalReason.Blocked:
                if (isThreat) return ScoreEventType.ThreatBlocked;
                if (isBenign) return ScoreEventType.BenignBlocked;
                if (isPriority) return ScoreEventType.PriorityPacketLost;
                return null;

            case PacketRemovalReason.Infected:
                if (isThreat) return ScoreEventType.ThreatReachedNode;
                return null;

            default:
                if (isPriority) return ScoreEventType.PriorityPacketLost;
                if (isBenign) return ScoreEventType.HealthyPacketLost;
                return null;
        }
    }

    private ScoreCategory ResolveCategory(ScoreEventType eventType)
    {
        switch (eventType)
        {
            case ScoreEventType.HealthyPacketDelivered:
            case ScoreEventType.HealthyPacketLost:
            case ScoreEventType.PriorityPacketDelivered:
            case ScoreEventType.PriorityPacketLost:
                return ScoreCategory.Throughput;

            case ScoreEventType.ThreatBlocked:
            case ScoreEventType.ThreatReachedNode:
            case ScoreEventType.BlockAtFirstNode:
                return ScoreCategory.ThreatHandling;

            case ScoreEventType.BenignBlocked:
                return ScoreCategory.Mistakes;

            case ScoreEventType.SuccessfulClean:
            case ScoreEventType.FailedClean:
                return ScoreCategory.Maintenance;

            default:
                return ScoreCategory.Throughput;
        }
    }

    private int GetBaseValue(ScoreEventType eventType)
    {
        switch (eventType)
        {
            case ScoreEventType.HealthyPacketDelivered:
                return healthyPacketDeliveredValue;

            case ScoreEventType.HealthyPacketLost:
                return healthyPacketLostValue;

            case ScoreEventType.PriorityPacketDelivered:
                return priorityPacketDeliveredValue;

            case ScoreEventType.PriorityPacketLost:
                return priorityPacketLostValue;

            case ScoreEventType.ThreatBlocked:
                return threatBlockedValue;

            case ScoreEventType.ThreatReachedNode:
                return threatReachedNodeValue;

            case ScoreEventType.BenignBlocked:
                return benignBlockedValue;

            case ScoreEventType.SuccessfulClean:
                return successfulCleanValue;

            case ScoreEventType.FailedClean:
                return failedCleanValue;

            case ScoreEventType.BlockAtFirstNode:
                return blockAtFirstNodeValue;

            default:
                return 0;
        }
    }

    private int GetCategoryScore(ScoreCategory category)
    {
        int total = 0;

        for (int i = 0; i < ledger.Count; i++)
        {
            if (ledger[i].category == category)
                total += ledger[i].finalValue;
        }

        return total;
    }

    private static string FormatSigned(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static string ToReasonString(PacketRemovalReason reason)
    {
        return reason switch
        {
            PacketRemovalReason.Arrived => "arrived",
            PacketRemovalReason.Blocked => "blocked",
            PacketRemovalReason.Infected => "infected",
            _ => "unknown"
        };
    }

    private static ScoreEventContext CloneContext(ScoreEventContext source)
    {
        return new ScoreEventContext
        {
            packetId = source.packetId,
            nodeId = source.nodeId,
            infectionType = source.infectionType,
            runId = source.runId,
            levelId = source.levelId,
            reason = source.reason,
            tick = source.tick,
            spawnTick = source.spawnTick,
            expectedDurationTicks = source.expectedDurationTicks,
            actualDurationTicks = source.actualDurationTicks,
            timingDeltaTicks = source.timingDeltaTicks,
            nodesReachedCount = source.nodesReachedCount,
            routeConnectionCount = source.routeConnectionCount,
            wasFirstNodeOutcome = source.wasFirstNodeOutcome,
            timingBand = source.timingBand
        };
    }
}