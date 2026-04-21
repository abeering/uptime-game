using UnityEngine;

[System.Serializable]
public class TrafficRuntimeState
{
    public int currentSpawnInterval;
    public float currentMalwareChance;
    public float currentPriorityChance;

    public int ticksUntilNextSpawn;
    public int lastResolvedTick = -1;

    public void ResetFromProfile(TrafficProfile profile)
    {
        if (profile == null)
            return;

        currentSpawnInterval = Mathf.Max(1, profile.startingSpawnIntervalTicks);
        currentMalwareChance = Mathf.Clamp01(profile.startingMalwareChance);
        currentPriorityChance = Mathf.Clamp01(profile.startingPriorityChance);
        ticksUntilNextSpawn = 0;
        lastResolvedTick = -1;
    }

    public void ResolveForTick(
        TrafficProfile profile,
        int currentTick,
        System.Collections.Generic.List<TrafficModifier> modifiers)
    {
        if (profile == null)
            return;

        if (currentTick == lastResolvedTick)
            return;

        float malware = Mathf.Min(
            profile.maxMalwareChance,
            profile.startingMalwareChance + (profile.malwareChanceRampPerTick * currentTick)
        );

        float priority = Mathf.Min(
            profile.maxPriorityChance,
            profile.startingPriorityChance + (profile.priorityChanceRampPerTick * currentTick)
        );

        int reduction = currentTick / Mathf.Max(1, profile.ticksPerSpawnIntervalStep);
        int interval = profile.startingSpawnIntervalTicks - reduction;
        interval = Mathf.Max(profile.minSpawnIntervalTicks, interval);

        // ✅ APPLY MODIFIERS
        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                var mod = modifiers[i];

                malware += mod.malwareChanceDelta;
                priority += mod.priorityChanceDelta;
                interval += mod.spawnIntervalDelta;
            }
        }

        currentMalwareChance = Mathf.Clamp01(malware);
        currentPriorityChance = Mathf.Clamp01(priority);
        currentSpawnInterval = Mathf.Max(1, interval);

        lastResolvedTick = currentTick;
    }
}