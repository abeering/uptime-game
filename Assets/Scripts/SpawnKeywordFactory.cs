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

            case "jittery":
            case "jitter":
            {
                int jitterAmount = 1;

                if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedJitter))
                    jitterAmount = Mathf.Max(1, parsedJitter);

                keyword = new JitteryKeyword(jitterAmount);
                return true;
            }

            case "surging":
            case "surge":
            {
                int stallTicks = 2;
                int burstTicks = 2;
                int burstMoveInterval = 1;

                if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedStall))
                    stallTicks = Mathf.Max(1, parsedStall);

                if (parts.Length >= 3 && int.TryParse(parts[2], out int parsedBurstTicks))
                    burstTicks = Mathf.Max(1, parsedBurstTicks);

                if (parts.Length >= 4 && int.TryParse(parts[3], out int parsedBurstMoveInterval))
                    burstMoveInterval = Mathf.Max(1, parsedBurstMoveInterval);

                keyword = new SurgingKeyword(stallTicks, burstTicks, burstMoveInterval);
                return true;
            }

            case "desynced":
            case "desync":
            {
                int stallTicks = 2;
                int teleportSteps = 3;

                if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedStall))
                    stallTicks = Mathf.Max(1, parsedStall);

                if (parts.Length >= 3 && int.TryParse(parts[2], out int parsedTeleportSteps))
                    teleportSteps = Mathf.Max(1, parsedTeleportSteps);

                keyword = new DesyncedKeyword(stallTicks, teleportSteps);
                return true;
            }

            case "accelerating":
            case "accelerate":
            {
                int radius = 2;
                int delta = -1;
                bool ignoreSameClassAndKind = true;

                if (parts.Length >= 2 && int.TryParse(parts[1], out int r))
                    radius = Mathf.Max(1, r);

                if (parts.Length >= 3 && int.TryParse(parts[2], out int d))
                    delta = Mathf.Min(-1, d);

                if (parts.Length >= 4 && bool.TryParse(parts[3], out bool parsedIgnore))
                    ignoreSameClassAndKind = parsedIgnore;

                keyword = new AcceleratingKeyword(radius, delta, ignoreSameClassAndKind);
                return true;
            }

            case "dragging":
            case "drag":
            {
                int radius = 2;
                int slow = 1;
                bool ignoreSameClassAndKind = true;

                if (parts.Length >= 2 && int.TryParse(parts[1], out int r))
                    radius = Mathf.Max(1, r);

                if (parts.Length >= 3 && int.TryParse(parts[2], out int s))
                    slow = Mathf.Max(1, s);

                if (parts.Length >= 4 && bool.TryParse(parts[3], out bool parsedIgnore))
                    ignoreSameClassAndKind = parsedIgnore;

                keyword = new DraggingKeyword(radius, slow, ignoreSameClassAndKind);
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

    public static List<string> RollUniqueSpecs(
    List<WeightedKeywordSpecEntry> table,
    int count)
    {
        List<string> rolled = new();

        if (table == null || table.Count == 0 || count <= 0)
            return rolled;

        List<WeightedKeywordSpecEntry> pool = new(table);

        count = Mathf.Min(count, pool.Count);

        for (int i = 0; i < count; i++)
        {
            string spec = RollOneSpec(pool);
            if (string.IsNullOrWhiteSpace(spec))
                break;

            rolled.Add(spec);

            int removeIndex = pool.FindIndex(entry => entry.spec == spec);
            if (removeIndex >= 0)
                pool.RemoveAt(removeIndex);
        }

        return rolled;
    }

    private static string RollOneSpec(List<WeightedKeywordSpecEntry> table)
    {
        if (table == null || table.Count == 0)
            return null;

        float totalWeight = 0f;
        for (int i = 0; i < table.Count; i++)
            totalWeight += Mathf.Max(0f, table[i].weight);

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.value * totalWeight;
        float running = 0f;

        for (int i = 0; i < table.Count; i++)
        {
            running += Mathf.Max(0f, table[i].weight);
            if (roll <= running)
                return table[i].spec;
        }

        return table[table.Count - 1].spec;
    }
    
}