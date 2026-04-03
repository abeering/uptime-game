using System.Collections.Generic;

public enum CommandType
{
    Unknown,
    Scan,
    DeepScan,
    Trace,
    Block,
    Cancel,
    Boost,
    Spawn,
    AutoSpawn,
    Throttle
}

public class ParsedCommand
{
    public CommandType type = CommandType.Unknown;
    public string packetId;
    public string nodeId;
    public string operationId;
    public string rawText;
    public string connectionId;
    public int throttleAmount;

    // for Spawn
    public PacketClass packetClass = PacketClass.Benign;
    public PacketKind packetKind = PacketKind.None;
    public string[] routeNodeIds;
    public List<string> spawnKeywordSpecs = new();
    public InfectionType? spawnInfectionOverride = null;
    public InfectionTargetRule? spawnInfectionTargetRule = null;
    public int spawnInfectionNthNode = 1;
    public bool spawnAllowAlreadyInfectedNode = false;

    // for autospawn / disable spawn 
    public string autoSpawnMode;
}