using System;

public enum BlockState
{
    Armed,
    Triggered,
    Failed,
    Cancelled
}

public enum BlockMatchType
{
    PacketId,
    SourceAddress
}

public class BlockOperation : Operation
{
    public string packetId;
    public string nodeId;
    public BlockMatchType matchType = BlockMatchType.PacketId;
    public string sourceAddress;

    public BlockState state = BlockState.Armed;

    public override string OperationType => "block";

    public override void Tick(CommandDirector context)
    {
        // Block operations are event-driven for now.
    }

    public override void OnPacketRemoved(string removedPacketId, PacketRemovalReason reason, CommandDirector context)
    {
        if (isFinished || state != BlockState.Armed)
            return;

        bool matches =
            matchType == BlockMatchType.PacketId
                ? string.Equals(packetId, removedPacketId, StringComparison.OrdinalIgnoreCase)
                : false; // we don't resolve source-based failure here (safe for now)

        if (matches)
        {
            state = BlockState.Failed;
            isFinished = true;
            lingerTicksRemaining = 3;
            context.LogBlockFailed(displayId, packetId, $"removed ({reason.ToString().ToLowerInvariant()})");
        }
    }

    public void TryTrigger(PacketView packet, NodeView reachedNode, CommandDirector context)
    {
        if (isFinished || state != BlockState.Armed)
            return;

        if (packet == null || reachedNode == null || context == null)
            return;

        bool matches = matchType switch
        {
            BlockMatchType.PacketId =>
                string.Equals(packet.packetId, packetId, StringComparison.OrdinalIgnoreCase),

            BlockMatchType.SourceAddress =>
                !string.IsNullOrWhiteSpace(sourceAddress) &&
                string.Equals(packet.sourceAddress, sourceAddress, StringComparison.OrdinalIgnoreCase),

            _ => false
        };

        if (!matches)
            return;

        if (!string.Equals(reachedNode.nodeId, nodeId, System.StringComparison.OrdinalIgnoreCase))
            return;

        var resolution = packet.HandleBlocked(reachedNode);

        var verb = string.IsNullOrWhiteSpace(resolution.logText) ? "blocked" : resolution.logText;

        string targetLabel = matchType == BlockMatchType.SourceAddress
            ? $"src={sourceAddress}"
            : packet.packetId;

        // Mark state BEFORE packet removal so OnPacketRemoved does not misfire.
        if (matchType == BlockMatchType.PacketId)
        {
            state = BlockState.Triggered;
            isFinished = true;
        }
        else
        {
            // persistent source rule: stays armed after firing
            state = BlockState.Armed;
            isFinished = false;
        }

        if (resolution.shouldRemove)
        {
            PacketRemovalReason removeReason = resolution.removeReason == PacketRemovalReason.Unknown
                ? PacketRemovalReason.Blocked
                : resolution.removeReason;

            context.trafficDirector.RemovePacket(packet, removeReason);
        }

        context.LogBlockTriggered(displayId, targetLabel, nodeId, verb);
    }

    public override bool CanCancel()
    {
        return state == BlockState.Armed;
    }

    public override void Cancel(CommandDirector context)
    {
        if (!CanCancel())
            return;
        lingerTicksRemaining = 3;
        state = BlockState.Cancelled;
        isFinished = true;
        context.LogBlockCancelled(displayId);
    }

    public override string GetOperationsLine()
    {
        string status = state switch
        {
            BlockState.Armed => "armed",
            BlockState.Triggered => "triggered",
            BlockState.Failed => "failed",
            BlockState.Cancelled => "cancelled",
            _ => "unknown"
        };

        string target = matchType == BlockMatchType.SourceAddress
            ? $"src={sourceAddress}"
            : packetId;

        return $"{displayId}  {target} @ {nodeId}  {status}";
    }
    
}