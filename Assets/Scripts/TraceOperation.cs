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
            context.Log($"TRACE {displayId} failed: {packetId} no longer exists");
            return;
        }

        if (remainingTicks <= 0)
        {
            state = TraceState.Completed;
            isFinished = true;
            lingerTicksRemaining = 3;
            context.Log($"INTEL {packet.packetId} = source={packet.sourceAddress} destination={packet.GetDestinationName()}");
        }
    }

    public override void OnPacketRemoved(string removedPacketId, string reason, CommandDirector context)
    {
        if (isFinished || state != TraceState.Running)
            return;

        if (string.Equals(packetId, removedPacketId, System.StringComparison.OrdinalIgnoreCase))
        {
            state = TraceState.Failed;
            isFinished = true;
            lingerTicksRemaining = 3;
            context.Log($"TRACE {displayId} failed: {packetId} removed ({reason})");
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
        context.Log($"TRACE {displayId} cancelled");
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