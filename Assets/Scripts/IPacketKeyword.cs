public interface IPacketKeyword
{
    void OnTick(PacketView packet, KeywordContext context);

    // optional hooks (no-op by default via extension methods later if needed)
    void OnScanned(PacketView packet, KeywordContext context) { }
}