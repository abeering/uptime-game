public interface IPacketKeyword
{
    string KeywordId { get; }
    string DisplayName { get; }
    string Description { get; }

    // Visual-only contribution to packet border instability.
    // PacketView sums these and clamps to 0..1.
    float AnomalyModifier01 { get; }

    void OnTick(PacketView packet, KeywordContext context);

    void OnScanned(PacketView packet, KeywordContext context) { }
}