using System;
using System.Collections.Generic;

public enum PacketRemovalReason
{
    Unknown,
    Arrived,
    Blocked,
    Infected
}

public enum ScoreCategory
{
    Throughput,
    ThreatHandling,
    Mistakes,
    Maintenance
}

public enum ScoreEventType
{
    HealthyPacketDelivered,
    HealthyPacketLost,
    PriorityPacketDelivered,
    PriorityPacketLost,
    ThreatBlocked,
    ThreatReachedNode,
    BenignBlocked,

    SuccessfulClean,
    FailedClean,
    BlockAtFirstNode
}

public enum ScoreModifierType
{
    Timing
}

public enum ScoreTimingBand
{
    None,
    Early,
    OnTime,
    Late,
    VeryLate
}

[Serializable]
public class ScoreModifierEntry
{
    public ScoreModifierType type;
    public string label;
    public int delta;

    public ScoreModifierEntry(ScoreModifierType type, string label, int delta)
    {
        this.type = type;
        this.label = label;
        this.delta = delta;
    }
}

[Serializable]
public class ScoreEventContext
{
    public string packetId;
    public string nodeId;
    public string infectionType;
    public string runId;
    public string levelId;
    public string reason;

    public int tick;

    public int? spawnTick;
    public int? expectedDurationTicks;
    public int? actualDurationTicks;
    public int? timingDeltaTicks;

    public int? nodesReachedCount;
    public int? routeConnectionCount;

    public bool? wasFirstNodeOutcome;

    public ScoreTimingBand timingBand = ScoreTimingBand.None;
}

[Serializable]
public class ScoreLedgerEntry
{
    public ScoreCategory category;
    public ScoreEventType eventType;
    public ScoreEventContext context = new();

    public int baseValue;
    public int finalValue;

    public List<ScoreModifierEntry> modifiers = new();
}

[Serializable]
public class PacketTimingSnapshot
{
    public string packetId;
    public PacketClass packetClass;
    public int spawnTick;
    public int expectedDurationTicks;
    public int routeConnectionCount;
}