public interface IPacketKeyword
{
    string KeywordId { get; }
    string DisplayName { get; }
    string Description { get; }

    void OnTick(PacketView packet, KeywordContext context);

    // optional hooks (no-op by default via extension methods later if needed)
    void OnScanned(PacketView packet, KeywordContext context) { }
}