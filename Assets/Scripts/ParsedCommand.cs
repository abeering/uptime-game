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
    AutoSpawn
}

public class ParsedCommand
{
    public CommandType type = CommandType.Unknown;
    public string packetId;
    public string nodeId;
    public string operationId;
    public string rawText;

    // for Spawn
    public PacketClass packetClass = PacketClass.Benign;
    public PacketKind packetKind = PacketKind.None;
    public string[] routeNodeIds;

    // for autospawn / disable spawn 
    public string autoSpawnMode;

    public List<string> spawnKeywordSpecs = new();
    public InfectionType? spawnInfectionOverride = null;
}