public struct BlockResolution
{
    public bool shouldRemove;
    public string removeReason;
    public string logText;

    public static BlockResolution Remove(string reason = "blocked", string logText = "blocked")
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
            removeReason = null,
            logText = logText
        };
    }
}