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
            context.Log($"DEEPSCAN {displayId} failed: {packetId} no longer exists");
            return;
        }

        float progress01 = 1f - ((float)remainingTicks / totalTicks);
        packet.UpdateScanVisual(progress01);

        if (remainingTicks <= 0)
        {
            state = DeepScanState.Completed;
            packet.ApplyDeepScan();
            isFinished = true;
            lingerTicksRemaining = 3;
            context.Log(
                $"INTEL {packet.packetId} = {packet.trueClass}/{packet.trueKind} (100%)"
            );
            packet.CompleteScanVisual($"{packet.GetVisibleClass()} {packet.GetConfidenceText()}");
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
            context.Log($"DEEPSCAN {displayId} failed: {packetId} removed ({reason})");
            PacketView packet = context.networkRuntime.GetPacket(packetId);
            if (packet != null)
            {
                packet.FailScanVisual("deep scan failed");
            }
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
        context.Log($"DEEPSCAN {displayId} cancelled");
        PacketView packet = context.networkRuntime.GetPacket(packetId);
        if (packet != null)
        {
            packet.CancelScanVisual("deep scan cancelled");
        }
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