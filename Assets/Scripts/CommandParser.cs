using System;

public static class CommandParser
{
    private static bool IsSpawnModifierToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return token.StartsWith("kw:", StringComparison.OrdinalIgnoreCase)
            || token.StartsWith("inf:", StringComparison.OrdinalIgnoreCase);
    }

    private static void ParseSpawnModifierToken(string token, ParsedCommand result)
    {
        if (string.IsNullOrWhiteSpace(token) || result == null)
            return;

        if (token.StartsWith("kw:", StringComparison.OrdinalIgnoreCase))
        {
            string spec = token.Substring(3).Trim();

            if (!string.IsNullOrWhiteSpace(spec))
                result.spawnKeywordSpecs.Add(spec);

            return;
        }

        if (token.StartsWith("inf:", StringComparison.OrdinalIgnoreCase))
        {
            string raw = token.Substring(4).Trim();

            if (Enum.TryParse(raw, true, out InfectionType infectionType))
                result.spawnInfectionOverride = infectionType;

            return;
        }
    }

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

        if (verb == "spawn")
        {
            if (parts.Length >= 5
                && Enum.TryParse(parts[1], true, out PacketClass packetClass)
                && Enum.TryParse(parts[2], true, out PacketKind packetKind))
            {
                result.type = CommandType.Spawn;
                result.packetClass = packetClass;
                result.packetKind = packetKind;

                var routeNodes = new System.Collections.Generic.List<string>();

                for (int i = 3; i < parts.Length; i++)
                {
                    string token = parts[i];

                    if (IsSpawnModifierToken(token))
                        ParseSpawnModifierToken(token, result);
                    else
                        routeNodes.Add(token);
                }

                result.routeNodeIds = routeNodes.ToArray();
            }

            return result;
        }

        if (verb == "autospawn")
        {
            result.type = CommandType.AutoSpawn;
            result.autoSpawnMode = parts.Length >= 2 ? parts[1] : null;
            return result;
        }

        return result;
    }
}