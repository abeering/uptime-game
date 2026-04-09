using System.Collections.Generic;
using UnityEngine;
using System.Text;

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

        int expectedDurationTicks = ComputeExpectedDurationTicks(packet);

        packetSnapshots[packet.PacketId] = new PacketTimingSnapshot
        {
            packetId = packet.PacketId,
            packetClass = packet.trueClass,
            spawnTick = spawnTick,
            expectedDurationTicks = expectedDurationTicks
        };

        if (logScoreEvents)
        {
            Debug.Log(
                $"[Score] snapshot {packet.PacketId} " +
                $"spawn={spawnTick} expected={expectedDurationTicks} class={packet.trueClass}"
            );
        }
    }

    public void RecordPacketRemoval(PacketView packet, string reason, int currentTick, NodeView node = null)
    {
        if (packet == null || string.IsNullOrWhiteSpace(packet.PacketId))
            return;

        if (!packetSnapshots.TryGetValue(packet.PacketId, out PacketTimingSnapshot snapshot))
            return;

        int actualDurationTicks = Mathf.Max(0, currentTick - snapshot.spawnTick);

        ScoreEventType? eventType = ResolvePacketEventType(packet, reason);
        if (!eventType.HasValue)
        {
            packetSnapshots.Remove(packet.PacketId);
            return;
        }

        int baseValue = GetBaseValue(eventType.Value);

        ScoreLedgerEntry entry = new ScoreLedgerEntry
        {
            eventType = eventType.Value,
            baseValue = baseValue,
            finalValue = baseValue,
            context = new ScoreEventContext
            {
                packetId = packet.PacketId,
                nodeId = node != null ? node.nodeId : null,
                runId = currentRunId,
                levelId = currentLevelId,
                reason = reason,
                tick = currentTick,
                spawnTick = snapshot.spawnTick,
                expectedDurationTicks = snapshot.expectedDurationTicks,
                actualDurationTicks = actualDurationTicks
            }
        };

        ApplyTimingModifier(entry);

        ledger.Add(entry);
        totalScore += entry.finalValue;
        packetSnapshots.Remove(packet.PacketId);

        if (logScoreEvents)
        {
            Debug.Log(
                $"[Score] {entry.eventType} packet={entry.context.packetId} " +
                $"base={entry.baseValue} final={entry.finalValue} " +
                $"expected={entry.context.expectedDurationTicks} actual={entry.context.actualDurationTicks} " +
                $"reason={entry.context.reason}"
            );
        }
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

    private void ApplyTimingModifier(ScoreLedgerEntry entry)
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

        int modifierDelta = 0;
        string label = "on-time";

        if (deltaTicks >= earlyThresholdTicks)
        {
            modifierDelta = earlyBonus;
            label = "early";
        }
        else if (deltaTicks < 0)
        {
            int lateBy = -deltaTicks;

            if (lateBy >= veryLateThresholdTicks)
            {
                modifierDelta = veryLatePenalty;
                label = "very-late";
            }
            else
            {
                modifierDelta = latePenalty;
                label = "late";
            }
        }

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

    private ScoreEventType? ResolvePacketEventType(PacketView packet, string reason)
    {
        if (packet == null)
            return null;

        bool isBenign = packet.trueClass == PacketClass.Benign;
        bool isThreat = packet.trueClass == PacketClass.Threat;
        bool isPriority = packet.trueClass == PacketClass.Priority;

        switch (reason)
        {
            case "arrived":
                if (isPriority) return ScoreEventType.PriorityPacketDelivered;
                if (isBenign) return ScoreEventType.HealthyPacketDelivered;
                return null;

            case "blocked":
                if (isThreat) return ScoreEventType.ThreatBlocked;
                if (isBenign) return ScoreEventType.BenignBlocked;
                if (isPriority) return ScoreEventType.PriorityPacketLost;
                return null;

            case "infected":
                if (isThreat) return ScoreEventType.ThreatReachedNode;
                return null;

            default:
                if (isPriority) return ScoreEventType.PriorityPacketLost;
                if (isBenign) return ScoreEventType.HealthyPacketLost;
                return null;
        }
    }

    private int GetBaseValue(ScoreEventType eventType)
    {
        switch (eventType)
        {
            case ScoreEventType.HealthyPacketDelivered:
                return 10;

            case ScoreEventType.HealthyPacketLost:
                return -10;

            case ScoreEventType.PriorityPacketDelivered:
                return 18;

            case ScoreEventType.PriorityPacketLost:
                return -18;

            case ScoreEventType.ThreatBlocked:
                return 15;

            case ScoreEventType.ThreatReachedNode:
                return -20;

            case ScoreEventType.BenignBlocked:
                return -12;

            default:
                return 0;
        }
    }

    public void AppendOperationsPanel(StringBuilder sb)
    {
        if (sb == null)
            return;

        sb.AppendLine("=== SCORE ===");

        sb.AppendLine($"total   {totalScore}");
        sb.AppendLine($"level   {GetScoreForLevel(currentLevelId)}");
        sb.AppendLine($"run     {GetScoreForRun(currentRunId)}");

        if (ledger == null || ledger.Count == 0)
        {
            sb.AppendLine("recent  none");
            return;
        }

        int count = Mathf.Min(3, ledger.Count);
        sb.AppendLine("recent");

        for (int i = ledger.Count - count; i < ledger.Count; i++)
        {
            ScoreLedgerEntry entry = ledger[i];

            string sign = entry.finalValue >= 0 ? "+" : "";
            string packetId = string.IsNullOrWhiteSpace(entry.context.packetId)
                ? "--"
                : entry.context.packetId;

            sb.AppendLine(
                $"  {packetId}  {entry.eventType}  {sign}{entry.finalValue}"
            );
        }
    }
    
}