public enum BlockState
{
    Armed,
    Triggered,
    Failed,
    Cancelled
}

public class BlockOperation : Operation
{
    public string packetId;
    public string nodeId;
    public BlockState state = BlockState.Armed;

    public override string OperationType => "block";

    public override void Tick(CommandDirector context)
    {
        // Block operations are event-driven for now.
    }

    public override void OnPacketRemoved(string removedPacketId, string reason, CommandDirector context)
    {
        if (isFinished || state != BlockState.Armed)
            return;

        if (string.Equals(packetId, removedPacketId, System.StringComparison.OrdinalIgnoreCase))
        {
            state = BlockState.Failed;
            isFinished = true;
            lingerTicksRemaining = 3;
            context.Log($"{displayId} failed: {packetId} removed ({reason})");
        }
    }

    public void TryTrigger(PacketView packet, NodeView reachedNode, CommandDirector context)
    {
        if (isFinished || state != BlockState.Armed)
            return;

        if (!string.Equals(packet.packetId, packetId, System.StringComparison.OrdinalIgnoreCase))
            return;

        if (reachedNode == null)
            return;

        if (!string.Equals(reachedNode.nodeId, nodeId, System.StringComparison.OrdinalIgnoreCase))
            return;

        state = BlockState.Triggered;
        isFinished = true;
        lingerTicksRemaining = 3;
        context.Log($"{displayId} triggered: {packet.packetId} blocked at {nodeId}");
        context.trafficDirector.RemovePacket(packet, "blocked");
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
        context.Log($"{displayId} cancelled");
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

        return $"{displayId}  {packetId} @ {nodeId}  {status}";
    }
}