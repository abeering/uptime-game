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
    public List<string> keywordSpecs = new();

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
                break;

            case PacketKind.Worm:
                profile.defaultClass = PacketClass.Threat;
                profile.canRollInfections = true;
                profile.infectionsAreRequired = true;
                profile.minKeywordCount = 0;
                profile.maxKeywordCount = 1;
                profile.infectionWeights = InfectionRules.GetDefaultInfectionTable(kind);
                break;

            case PacketKind.Spyware:
                profile.defaultClass = PacketClass.Threat;
                profile.canRollInfections = true;
                profile.infectionsAreRequired = true;
                profile.minKeywordCount = 0;
                profile.maxKeywordCount = 1;
                profile.infectionWeights = InfectionRules.GetDefaultInfectionTable(kind);
                break;

            case PacketKind.Ddos:
                profile.defaultClass = PacketClass.Threat;
                profile.canRollInfections = true;
                profile.infectionsAreRequired = true;
                profile.minKeywordCount = 0;
                profile.maxKeywordCount = 0;
                profile.infectionWeights = InfectionRules.GetDefaultInfectionTable(kind);
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