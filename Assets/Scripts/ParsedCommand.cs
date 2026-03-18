public enum CommandType
{
    Unknown,
    Scan,
    DeepScan,
    Trace,
    Block,
    Cancel,
    Boost,
    Spawn
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
}