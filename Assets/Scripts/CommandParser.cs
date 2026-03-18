using System;

public static class CommandParser
{
    public static ParsedCommand Parse(string raw)
    {
        ParsedCommand result = new ParsedCommand();
        result.rawText = raw;

        if (string.IsNullOrWhiteSpace(raw))
            return result;

        string trimmed = raw.Trim();
        string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return result;

        string verb = parts[0].ToLowerInvariant();

        if (verb == "scan")
        {
            if (parts.Length >= 2)
            {
                result.type = CommandType.Scan;
                result.packetId = parts[1];
            }

            return result;
        }

        if (verb == "deepscan")
        {
            if (parts.Length >= 2)
            {
                result.type = CommandType.DeepScan;
                result.packetId = parts[1];
            }

            return result;
        }

        if (verb == "trace")
        {
            if (parts.Length >= 2)
            {
                result.type = CommandType.Trace;
                result.packetId = parts[1];
            }

            return result;
        }

        if (verb == "block")
        {
            if (parts.Length >= 4 && parts[2] == "@")
            {
                result.type = CommandType.Block;
                result.packetId = parts[1];
                result.nodeId = parts[3];
            }

            return result;
        }

        if (verb == "cancel")
        {
            if (parts.Length >= 2)
            {
                result.type = CommandType.Cancel;
                result.operationId = parts[1];
            }

            return result;
        }

        if (verb == "boost")
        {
            if (parts.Length >= 2)
            {
                result.type = CommandType.Boost;
                result.packetId = parts[1];
            }

            return result;
        }

        return result;
    }
}