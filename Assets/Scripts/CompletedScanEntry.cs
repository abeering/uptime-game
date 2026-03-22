public class CompletedScanEntry
{
    public string packetId;
    public ScanStage finalStage;
    public PacketClass reportedClass;
    public int lingerTicksRemaining;

    public CompletedScanEntry(
        string newPacketId,
        ScanStage newFinalStage,
        PacketClass newReportedClass,
        int newLingerTicksRemaining)
    {
        packetId = newPacketId;
        finalStage = newFinalStage;
        reportedClass = newReportedClass;
        lingerTicksRemaining = newLingerTicksRemaining;
    }
}