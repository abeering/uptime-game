public class SpawnPlan
{
    public int spawnTick;
    public string packetId;

    public PacketClass packetClass;
    public PacketKind packetKind;
    public QuickScanClass quickScanClass;

    public string sourceAddress;

    public int baseSpeed;
    public RouteStep[] route;
}