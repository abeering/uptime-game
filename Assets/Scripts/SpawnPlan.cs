using System.Collections.Generic;

public class SpawnPlan
{
    public int spawnTick;
    public string packetId;

    public PacketClass packetClass;
    public PacketKind packetKind;
    public int scanDifficulty;

    public string sourceAddress;

    public int baseSpeed;
    public RouteStep[] route;
    public bool startsQuickScanned;

    public InfectionType? infectionOverride = null; // compatibility bridge for debug spawn / legacy paths
    public List<InfectionPayload> infections = new();
    public List<IPacketKeyword> keywords = new();
}