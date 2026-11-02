using System;

namespace Uptime.Connections
{
    // One stable visual order for the LOAD and WORK views of a target node.
    internal static class NodePanelRowOrder
    {
        internal static int Compare(LogicalConnectionState left, LogicalConnectionState right)
        {
            int established = left.EstablishedAtSeconds.CompareTo(right.EstablishedAtSeconds);
            return established != 0 ? established :
                string.Compare(left.ConnectionId, right.ConnectionId, StringComparison.Ordinal);
        }
    }
}
