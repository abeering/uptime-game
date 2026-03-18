public enum DeepScanState
{
    Running,
    Completed,
    Failed,
    Cancelled
}

public class DeepScanOperation : Operation
{
    public string packetId;
    public int remainingTicks;
    public int totalTicks;
    public DeepScanState state = DeepScanState.Running;

    public override string OperationType => "deepscan";

    public override void Tick(CommandDirector context)
    {
        if (isFinished || state != DeepScanState.Running)
            return;

        remainingTicks--;

        PacketView packet = context.networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            state = DeepScanState.Failed;
            isFinished = true;
            context.Log($"{displayId} failed: {packetId} no longer exists");
            return;
        }

        if (remainingTicks <= 0)
        {
            state = DeepScanState.Completed;
            packet.ApplyDeepScan();
            isFinished = true;
            lingerTicksRemaining = 3;
            context.Log($"{displayId} complete: {packet.packetId} = {packet.quickScanClass} source={packet.sourceAddress} destination={packet.GetDestinationName()}");
        }
    }

    public override void OnPacketRemoved(string removedPacketId, string reason, CommandDirector context)
    {
        if (isFinished || state != DeepScanState.Running)
            return;

        if (string.Equals(packetId, removedPacketId, System.StringComparison.OrdinalIgnoreCase))
        {
            state = DeepScanState.Failed;
            isFinished = true;
            lingerTicksRemaining = 3;
            context.Log($"{displayId} failed: {packetId} removed ({reason})");
        }
    }

    public override bool CanCancel()
    {
        return state == DeepScanState.Running;
    }

    public override void Cancel(CommandDirector context)
    {
        if (!CanCancel())
            return;

        state = DeepScanState.Cancelled;
        isFinished = true;
        lingerTicksRemaining = 3;
        context.Log($"{displayId} cancelled");
    }

    public override string GetOperationsLine()
    {
        string status = state switch
        {
            DeepScanState.Running => $"[{remainingTicks}s]",
            DeepScanState.Completed => "complete",
            DeepScanState.Failed => "failed",
            DeepScanState.Cancelled => "cancelled",
            _ => "unknown"
        };

        return $"{displayId}  {packetId}  {status}";
    }
}