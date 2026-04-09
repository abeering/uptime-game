public enum TraceState
{
    Running,
    Completed,
    Failed,
    Cancelled
}

public class TraceOperation : Operation
{
    public string packetId;
    public int remainingTicks;
    public int totalTicks;
    public TraceState state = TraceState.Running;

    public override string OperationType => "trace";

    public override void Tick(CommandDirector context)
    {
        if (isFinished || state != TraceState.Running)
            return;

        remainingTicks--;

        PacketView packet = context.networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            state = TraceState.Failed;
            isFinished = true;
            context.LogTraceFailed(displayId, packetId, "no longer exists");
            return;
        }

        if (remainingTicks <= 0)
        {
            state = TraceState.Completed;
            isFinished = true;
            lingerTicksRemaining = 3;
            context.LogTraceReveal(packet);
        }
    }

    public override void OnPacketRemoved(string removedPacketId, PacketRemovalReason reason, CommandDirector context)
    {
        if (isFinished || state != TraceState.Running)
            return;

        if (string.Equals(packetId, removedPacketId, System.StringComparison.OrdinalIgnoreCase))
        {
            state = TraceState.Failed;
            isFinished = true;
            lingerTicksRemaining = 3;
            context.LogTraceFailed(displayId, packetId, $"removed ({reason.ToString().ToLowerInvariant()})");
        }
    }

    public override bool CanCancel()
    {
        return state == TraceState.Running;
    }

    public override void Cancel(CommandDirector context)
    {
        if (!CanCancel())
            return;

        state = TraceState.Cancelled;
        isFinished = true;
        lingerTicksRemaining = 3;
        context.LogTraceCancelled(displayId);
    }

    public override string GetOperationsLine()
    {
        string status = state switch
        {
            TraceState.Running => $"[{remainingTicks}s]",
            TraceState.Completed => "complete",
            TraceState.Failed => "failed",
            TraceState.Cancelled => "cancelled",
            _ => "unknown"
        };

        return $"{displayId}  {packetId}  {status}";
    }
}