using System.Collections.Generic;
using UnityEngine;

public enum InfectionType
{
    None,
    Blackout,
    Spawner,
    Throttle
}

public readonly struct WeightedInfectionEntry
{
    public readonly InfectionType type;
    public readonly float weight;

    public WeightedInfectionEntry(InfectionType type, float weight)
    {
        this.type = type;
        this.weight = Mathf.Max(0f, weight);
    }
}

public static class InfectionRules
{
    public static InfectionType RollDefaultInfectionType(PacketKind kind)
    {
        List<WeightedInfectionEntry> table = GetDefaultInfectionTable(kind);
        return RollFromTable(table);
    }

    public static List<WeightedInfectionEntry> GetDefaultInfectionTable(PacketKind kind)
    {
        return kind switch
        {
            PacketKind.Virus => new List<WeightedInfectionEntry>
            {
                new(InfectionType.Blackout, 0.65f),
                new(InfectionType.Throttle, 0.25f),
                new(InfectionType.Spawner, 0.10f),
            },

            PacketKind.Worm => new List<WeightedInfectionEntry>
            {
                new(InfectionType.Spawner, 0.65f),
                new(InfectionType.Throttle, 0.20f),
                new(InfectionType.Blackout, 0.15f),
            },

            PacketKind.Spyware => new List<WeightedInfectionEntry>
            {
                new(InfectionType.Throttle, 0.70f),
                new(InfectionType.Spawner, 0.20f),
                new(InfectionType.Blackout, 0.10f),
            },

            PacketKind.Ddos => new List<WeightedInfectionEntry>
            {
                new(InfectionType.Throttle, 0.75f),
                new(InfectionType.Blackout, 0.25f),
            },

            _ => new List<WeightedInfectionEntry>()
        };
    }

    private static InfectionType RollFromTable(List<WeightedInfectionEntry> table)
    {
        if (table == null || table.Count == 0)
            return InfectionType.None;

        float totalWeight = 0f;
        for (int i = 0; i < table.Count; i++)
            totalWeight += Mathf.Max(0f, table[i].weight);

        if (totalWeight <= 0f)
            return InfectionType.None;

        float roll = Random.value * totalWeight;
        float running = 0f;

        for (int i = 0; i < table.Count; i++)
        {
            running += Mathf.Max(0f, table[i].weight);
            if (roll <= running)
                return table[i].type;
        }

        return table[table.Count - 1].type;
    }
}