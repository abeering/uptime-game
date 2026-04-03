using System;
using UnityEngine;

[Serializable]
public class InfectionParameters
{
    public SpawnerInfectionParameters spawner = new();
    public ThrottleInfectionParameters throttle = new();
}

[Serializable]
public class SpawnerInfectionParameters
{
    public int cadenceTicks = 8;
    public int burstSize = 1;
    public PacketKind spawnKind = PacketKind.Virus;
    public int scanDifficulty = 35;
    public int? baseSpeedOverride = null;

    public int GetSafeCadenceTicks() => Mathf.Max(1, cadenceTicks);
    public int GetSafeBurstSize() => Mathf.Max(1, burstSize);
    public int GetSafeScanDifficulty() => Mathf.Clamp(scanDifficulty, 0, 100);
}

[Serializable]
public class ThrottleInfectionParameters
{
    public int latencyPenalty = 1;
}