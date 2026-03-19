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
            context.Log($"SCAN {displayId} failed: {packetId} no longer exists");
            return;
        }

        float progress01 = 1f - ((float)remainingTicks / totalTicks);
        packet.UpdateScanVisual(progress01);

        if (remainingTicks <= 0)
        {
            state = ScanState.Completed;
            packet.ApplyQuickScan();
            isFinished = true;
            lingerTicksRemaining = 3;
            context.Log(
                $"INTEL {packet.packetId} = {packet.GetVisibleClass()} [{packet.GetConfidenceText()} confidence]"
            );
            packet.CompleteScanVisual($"{packet.GetVisibleClass()} {packet.GetConfidenceText()}");
            if(packet.IsKnownThreat())
            {
                context.AudioManager?.PlayThreatIdentified();
            } else {
                context.AudioManager?.PlayOperationComplete();
            }
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
            context.Log($"SCAN {displayId} failed: {packetId} removed ({reason})");
            PacketView packet = context.networkRuntime.GetPacket(packetId);
            if (packet != null)
            {
                packet.FailScanVisual("scan failed");
            }
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
        context.Log($"SCAN {displayId} cancelled");
        PacketView packet = context.networkRuntime.GetPacket(packetId);
        if (packet != null)
        {
            packet.CancelScanVisual("scan cancelled");
        }
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