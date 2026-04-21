using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TrafficProfile
{
    [Header("Spawn Cadence")]
    [Min(1)] public int startingSpawnIntervalTicks = 10;
    [Min(1)] public int minSpawnIntervalTicks = 5;
    [Min(1)] public int ticksPerSpawnIntervalStep = 200;
    [Min(0)] public int spawnIntervalJitter = 5;
    [Min(0)] public int openingGraceTicks = 5;

    [Header("Spawn Intel")]
    [Range(0f, 1f)] public float startingQuickScanChance = 0.0f;

    [Header("Threat Ramp")]
    [Range(0f, 1f)] public float startingMalwareChance = 0.05f;
    [Range(0f, 1f)] public float maxMalwareChance = 0.25f;
    public float malwareChanceRampPerTick = 0.001f;

    [Header("Threat Kind Weights")]
    public Dictionary<PacketKind, float> threatKindWeights = new()
    {
        { PacketKind.Virus, 0.40f },
        { PacketKind.Worm, 0.30f },
        { PacketKind.Spyware, 0.20f },
        { PacketKind.Ddos, 0.10f },
    };

    [Header("Priority Ramp")]
    [Range(0f, 1f)] public float startingPriorityChance = 0.10f;
    [Range(0f, 1f)] public float maxPriorityChance = 0.20f;
    public float priorityChanceRampPerTick = 0.00025f;

    [Header("Priority Kind Weights")]
    public Dictionary<PacketKind, float> priorityKindWeights = new()
    {
        { PacketKind.Auth, 0.40f },
        { PacketKind.Control, 0.35f },
        { PacketKind.FileTransfer, 0.25f },
    };

    [Header("Packet Move Interval")]
    [Min(1)] public int minBaseMoveInterval = 1;
    [Min(1)] public int maxBaseMoveInterval = 3;

    [Header("Scan Difficulty")]
    [Range(0, 100)] public int minScanDifficulty = 10;
    [Range(0, 100)] public int maxScanDifficulty = 40;

    [Header("Route Bias")]
    public string routeBias = "";

    public static TrafficProfile FromDirector(TrafficDirector director)
    {
        return new TrafficProfile
        {
            startingSpawnIntervalTicks = director.startingSpawnIntervalTicks,
            minSpawnIntervalTicks = director.minSpawnIntervalTicks,
            ticksPerSpawnIntervalStep = director.ticksPerSpawnIntervalStep,
            spawnIntervalJitter = director.spawnIntervalJitter,
            openingGraceTicks = director.openingGraceTicks,

            startingQuickScanChance = director.startingQuickScanChance,

            startingMalwareChance = director.startingMalwareChance,
            maxMalwareChance = director.maxMalwareChance,
            malwareChanceRampPerTick = director.malwareChanceRampPerTick,

            startingPriorityChance = director.startingPriorityChance,
            maxPriorityChance = director.maxPriorityChance,
            priorityChanceRampPerTick = director.priorityChanceRampPerTick,

            minBaseMoveInterval = director.minBaseMoveInterval,
            maxBaseMoveInterval = director.maxBaseMoveInterval,

            minScanDifficulty = director.minScanDifficulty,
            maxScanDifficulty = director.maxScanDifficulty,

            threatKindWeights = new Dictionary<PacketKind, float>
            {
                { PacketKind.Virus, 0.40f },
                { PacketKind.Worm, 0.30f },
                { PacketKind.Spyware, 0.20f },
                { PacketKind.Ddos, 0.10f },
            },

            priorityKindWeights = new Dictionary<PacketKind, float>
            {
                { PacketKind.Auth, 0.40f },
                { PacketKind.Control, 0.35f },
                { PacketKind.FileTransfer, 0.25f },
            },

            routeBias = ""
        };
    }

    public Dictionary<PacketKind, float> CloneThreatKindWeights()
    {
        return threatKindWeights != null
            ? new Dictionary<PacketKind, float>(threatKindWeights)
            : new Dictionary<PacketKind, float>();
    }

    public Dictionary<PacketKind, float> ClonePriorityKindWeights()
    {
        return priorityKindWeights != null
            ? new Dictionary<PacketKind, float>(priorityKindWeights)
            : new Dictionary<PacketKind, float>();
    }

}