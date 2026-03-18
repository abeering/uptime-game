public enum CommandType
{
    Unknown,
    Scan,
    DeepScan,
    Block,
    Cancel
}

public class ParsedCommand
{
    public CommandType type = CommandType.Unknown;
    public string packetId;
    public string nodeId;
    public string operationId;
    public string rawText;
}