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

    public void ResolveForTick(TrafficProfile profile, int currentTick)
    {
        if (profile == null)
            return;

        if (currentTick == lastResolvedTick)
            return;

        currentMalwareChance = Mathf.Min(
            profile.maxMalwareChance,
            profile.startingMalwareChance + (profile.malwareChanceRampPerTick * currentTick)
        );

        currentPriorityChance = Mathf.Min(
            profile.maxPriorityChance,
            profile.startingPriorityChance + (profile.priorityChanceRampPerTick * currentTick)
        );

        int reduction = currentTick / Mathf.Max(1, profile.ticksPerSpawnIntervalStep);
        int interval = profile.startingSpawnIntervalTicks - reduction;
        currentSpawnInterval = Mathf.Max(profile.minSpawnIntervalTicks, interval);

        lastResolvedTick = currentTick;
    }
}