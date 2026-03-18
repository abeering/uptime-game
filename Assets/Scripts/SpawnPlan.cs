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
}