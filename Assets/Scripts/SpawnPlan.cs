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

    public List<IPacketKeyword> keywords = new();
}