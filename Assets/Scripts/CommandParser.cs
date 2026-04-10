using System;
using System.Text.RegularExpressions;

public static class CommandParser
{
    private static readonly Regex ScanVerbRegex = new(@"^(scan|s)(\d+)?$", RegexOptions.IgnoreCase);
    private static readonly Regex TraceVerbRegex = new(@"^(trace|t)(\d+)?$", RegexOptions.IgnoreCase);

    private static bool IsSpawnModifierToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return token.StartsWith("kw:", StringComparison.OrdinalIgnoreCase)
            || token.StartsWith("inf:", StringComparison.OrdinalIgnoreCase)
            || token.StartsWith("infrule:", StringComparison.OrdinalIgnoreCase)
            || token.StartsWith("infallowreinfect:", StringComparison.OrdinalIgnoreCase)
            || token.StartsWith("infp:", StringComparison.OrdinalIgnoreCase);
    }

    private static void ParseSpawnInfectionParamToken(string token, ParsedCommand result)
    {
        if (string.IsNullOrWhiteSpace(token) || result == null)
            return;

        string raw = token.Substring("infp:".Length).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return;

        int equalsIndex = raw.IndexOf('=');
        if (equalsIndex <= 0 || equalsIndex >= raw.Length - 1)
            return;

        string key = raw.Substring(0, equalsIndex).Trim().ToLowerInvariant();
        string value = raw.Substring(equalsIndex + 1).Trim();

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            return;

        result.spawnInfectionParams[key] = value;
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
                result.spawnInfectionType = infectionType;

            return;
        }

        if (token.StartsWith("infrule:", StringComparison.OrdinalIgnoreCase))
        {
            string raw = token.Substring(8).Trim();
            string[] parts = raw.Split(':', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return;

            string ruleName = parts[0].Trim().ToLowerInvariant();

            switch (ruleName)
            {
                case "first":
                    result.spawnInfectionTargetRule = InfectionTargetRule.FirstReachedNode;
                    result.spawnInfectionNthNode = 1;
                    return;

                case "nth":
                    result.spawnInfectionTargetRule = InfectionTargetRule.NthReachedNode;

                    if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedNth))
                        result.spawnInfectionNthNode = Math.Max(1, parsedNth);
                    else
                        result.spawnInfectionNthNode = 1;

                    return;

                case "any":
                    result.spawnInfectionTargetRule = InfectionTargetRule.AnyReachedNode;
                    result.spawnInfectionNthNode = 1;
                    return;

                case "destination":
                case "dest":
                    result.spawnInfectionTargetRule = InfectionTargetRule.DestinationNode;
                    result.spawnInfectionNthNode = 1;
                    return;
            }

            return;
        }

        if (token.StartsWith("infallowreinfect:", StringComparison.OrdinalIgnoreCase))
        {
            string raw = token.Substring("infallowreinfect:".Length).Trim();

            if (bool.TryParse(raw, out bool allow))
                result.spawnAllowAlreadyInfectedNode = allow;

            return;
        }

        if (token.StartsWith("infp:", StringComparison.OrdinalIgnoreCase))
        {
            ParseSpawnInfectionParamToken(token, result);
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

        Match scanMatch = ScanVerbRegex.Match(verb);
        if (scanMatch.Success)
        {
            if (parts.Length >= 2)
            {
                result.type = CommandType.Scan;
                result.packetId = parts[1];

                if (scanMatch.Groups[2].Success &&
                    int.TryParse(scanMatch.Groups[2].Value, out int parsedSlot))
                {
                    result.intelSlotIndex = parsedSlot - 1;
                }
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

        Match traceMatch = TraceVerbRegex.Match(verb);
        if (traceMatch.Success)
        {
            if (parts.Length >= 2)
            {
                result.type = CommandType.Trace;
                result.packetId = parts[1];

                if (traceMatch.Groups[2].Success &&
                    int.TryParse(traceMatch.Groups[2].Value, out int parsedSlot))
                {
                    result.intelSlotIndex = parsedSlot - 1;
                }
            }

            return result;
        }

        if (verb == "block" || verb == "b")
        {
            if (parts.Length >= 4 && parts[2] == "@")
            {
                result.type = CommandType.Block;
                result.packetId = parts[1];
                result.nodeId = parts[3];
            }

            return result;
        }

        if (verb == "clean")
        {
            if (parts.Length >= 2)
            {
                result.type = CommandType.Clean;
                result.nodeId = parts[1];
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

        if (verb == "throttle")
        {
            if (parts.Length >= 3 && int.TryParse(parts[2], out int amount))
            {
                result.type = CommandType.Throttle;
                result.connectionId = parts[1];
                result.throttleAmount = amount;
            }

            return result;
        }

        return result;
    }
}