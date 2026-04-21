using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PacketKindProfile
{
    public PacketKind kind;
    public PacketClass defaultClass = PacketClass.Threat;

    public bool canRollInfections = false;
    public bool infectionsAreRequired = false;

    [Min(0)] public int minKeywordCount = 0;
    [Min(0)] public int maxKeywordCount = 0;

    public List<WeightedInfectionEntry> infectionWeights = new();
    public List<WeightedKeywordSpecEntry> keywordWeights = new();

    public InfectionType RollInfectionType()
    {
        if (!canRollInfections || infectionWeights == null || infectionWeights.Count == 0)
            return InfectionType.None;

        return InfectionRules.RollFromTable(infectionWeights);
    }

    public static PacketKindProfile CreateDefault(PacketKind kind)
    {
        PacketKindProfile profile = new PacketKindProfile
        {
            kind = kind
        };

        switch (kind)
        {
            case PacketKind.Virus:
                profile.defaultClass = PacketClass.Threat;
                profile.canRollInfections = true;
                profile.infectionsAreRequired = true;
                profile.minKeywordCount = 0;
                profile.maxKeywordCount = 1;
                profile.infectionWeights = InfectionRules.GetDefaultInfectionTable(kind);
                profile.keywordWeights = new List<WeightedKeywordSpecEntry>
                {
                    new("mutating:3", 0.45f),
                    new("jittery:1", 0.20f),
                    new("desynced:2:2", 0.15f),
                    new("surging:2:2:1", 0.20f),
                };
                break;

            case PacketKind.Worm:
                profile.defaultClass = PacketClass.Threat;
                profile.canRollInfections = true;
                profile.infectionsAreRequired = true;
                profile.minKeywordCount = 0;
                profile.maxKeywordCount = 2;
                profile.infectionWeights = InfectionRules.GetDefaultInfectionTable(kind);
                profile.keywordWeights = new List<WeightedKeywordSpecEntry>
                {
                    new("surging:2:2:1", 0.35f),
                    new("desynced:2:3", 0.25f),
                    new("accelerating:2:-1:true", 0.20f),
                    new("dragging:2:1:true", 0.20f),
                };
                break;

            case PacketKind.Spyware:
                profile.defaultClass = PacketClass.Threat;
                profile.canRollInfections = true;
                profile.infectionsAreRequired = true;
                profile.minKeywordCount = 0;
                profile.maxKeywordCount = 1;
                profile.infectionWeights = InfectionRules.GetDefaultInfectionTable(kind);
                profile.keywordWeights = new List<WeightedKeywordSpecEntry>
                {
                    new("jittery:1", 0.35f),
                    new("desynced:2:2", 0.35f),
                    new("mutating:4", 0.15f),
                    new("surging:2:2:1", 0.15f),
                };
                break;

            case PacketKind.Ddos:
                profile.defaultClass = PacketClass.Threat;
                profile.canRollInfections = true;
                profile.infectionsAreRequired = true;
                profile.minKeywordCount = 1;
                profile.maxKeywordCount = 2;
                profile.infectionWeights = InfectionRules.GetDefaultInfectionTable(kind);
                profile.keywordWeights = new List<WeightedKeywordSpecEntry>
                {
                    new("dragging:2:1:true", 0.45f),
                    new("accelerating:2:-1:true", 0.35f),
                    new("surging:1:2:1", 0.20f),
                };
                break;

            case PacketKind.Auth:
            case PacketKind.Control:
            case PacketKind.FileTransfer:
                profile.defaultClass = PacketClass.Priority;
                profile.canRollInfections = false;
                profile.infectionsAreRequired = false;
                profile.minKeywordCount = 0;
                profile.maxKeywordCount = 0;
                break;

            default:
                profile.defaultClass = PacketClass.Benign;
                profile.canRollInfections = false;
                profile.infectionsAreRequired = false;
                profile.minKeywordCount = 0;
                profile.maxKeywordCount = 0;
                break;
        }

        return profile;
    }
}