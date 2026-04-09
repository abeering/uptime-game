using System;
using System.Collections.Generic;

public enum ScoreEventType
{
    HealthyPacketDelivered,
    HealthyPacketLost,
    PriorityPacketDelivered,
    PriorityPacketLost,
    ThreatBlocked,
    ThreatReachedNode,
    BenignBlocked,

    // reserved for later
    SuccessfulClean,
    FailedClean,
    BlockAtFirstNode
}

public enum ScoreModifierType
{
    Timing
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
    public string runId;
    public string levelId;
    public string reason;

    public int tick;

    public int? spawnTick;
    public int? expectedDurationTicks;
    public int? actualDurationTicks;
}

[Serializable]
public class ScoreLedgerEntry
{
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
}