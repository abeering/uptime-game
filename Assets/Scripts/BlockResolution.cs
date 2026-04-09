public struct BlockResolution
{
    public bool shouldRemove;
    public PacketRemovalReason removeReason;
    public string logText;

    public static BlockResolution Remove(
        PacketRemovalReason reason = PacketRemovalReason.Blocked,
        string logText = "blocked")
    {
        return new BlockResolution
        {
            shouldRemove = true,
            removeReason = reason,
            logText = logText
        };
    }

    public static BlockResolution Survive(string logText = "intercepted")
    {
        return new BlockResolution
        {
            shouldRemove = false,
            removeReason = PacketRemovalReason.Unknown,
            logText = logText
        };
    }
}