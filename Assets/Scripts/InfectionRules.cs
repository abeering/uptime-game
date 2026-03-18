public enum InfectionType
{
    None,
    Blackout,
    Spawner
}

public static class InfectionRules
{
    public static InfectionType FromPacketKind(PacketKind kind)
    {
        return kind switch
        {
            PacketKind.Virus => InfectionType.Blackout,
            PacketKind.Worm => InfectionType.Spawner,
            _ => InfectionType.None
        };
    }
}