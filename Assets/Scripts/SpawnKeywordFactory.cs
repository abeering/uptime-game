using System;
using System.Collections.Generic;
using UnityEngine;

public static class SpawnKeywordFactory
{
    public static bool TryBuild(string spec, out IPacketKeyword keyword, out string error)
    {
        keyword = null;
        error = null;

        if (string.IsNullOrWhiteSpace(spec))
        {
            error = "empty keyword spec";
            return false;
        }

        string[] parts = spec.Split(':', StringSplitOptions.RemoveEmptyEntries);
        string keywordName = parts[0].Trim().ToLowerInvariant();

        switch (keywordName)
        {
            case "mutating":
            case "mutate":
            {
                int ticksPerMutation = 3;

                if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedTicks))
                    ticksPerMutation = Mathf.Max(1, parsedTicks);

                keyword = new MutatingKeyword(ticksPerMutation);
                return true;
            }

            default:
                error = $"unknown keyword '{keywordName}'";
                return false;
        }
    }

    public static List<IPacketKeyword> BuildMany(IEnumerable<string> specs, out string error)
    {
        error = null;
        List<IPacketKeyword> built = new();

        if (specs == null)
            return built;

        foreach (string spec in specs)
        {
            if (!TryBuild(spec, out IPacketKeyword keyword, out error))
                return null;

            built.Add(keyword);
        }

        return built;
    }
}