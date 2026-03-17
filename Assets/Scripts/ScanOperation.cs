public enum ScanState
{
    Running,
    Completed,
    Failed,
    Cancelled
}

public class ScanOperation : Operation
{
    public string packetId;
    public int remainingTicks;
    public int totalTicks;
    public ScanState state = ScanState.Running;

    public override string OperationType => "scan";

    public override void Tick(CommandDirector context)
    {
        if (isFinished || state != ScanState.Running)
            return;

        remainingTicks--;

        PacketView packet = context.networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            state = ScanState.Failed;
            isFinished = true;
            context.Log($"{displayId} failed: {packetId} no longer exists");
            return;
        }

        if (remainingTicks <= 0)
        {
            state = ScanState.Completed;
            isFinished = true;
            lingerTicksRemaining = 3;
            context.Log($"{displayId} complete: {packet.packetId} = {packet.kind}");
        }
    }

    public override void OnPacketRemoved(string removedPacketId, string reason, CommandDirector context)
    {
        if (isFinished || state != ScanState.Running)
            return;

        if (string.Equals(packetId, removedPacketId, System.StringComparison.OrdinalIgnoreCase))
        {
            state = ScanState.Failed;
            isFinished = true;
            lingerTicksRemaining = 3;
            context.Log($"{displayId} failed: {packetId} removed ({reason})");
        }
    }

    public override bool CanCancel()
    {
        return state == ScanState.Running;
    }

    public override void Cancel(CommandDirector context)
    {
        if (!CanCancel())
            return;

        state = ScanState.Cancelled;
        isFinished = true;
        lingerTicksRemaining = 3;
        context.Log($"{displayId} cancelled");
    }

    public override string GetOperationsLine()
    {
        string status = state switch
        {
            ScanState.Running => $"[{remainingTicks}s]",
            ScanState.Completed => "complete",
            ScanState.Failed => "failed",
            ScanState.Cancelled => "cancelled",
            _ => "unknown"
        };

        return $"{displayId}  {packetId}  {status}";
    }
}