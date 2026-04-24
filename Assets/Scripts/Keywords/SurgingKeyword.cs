using UnityEngine;

public class SurgingKeyword : IPacketKeyword
{
    public string KeywordId => "surging";
    public string DisplayName => "Surging";
    public string Description => "Stalls briefly, then surges forward with a short burst of unusually fast movement.";
    public float AnomalyModifier01 => 0.35f;

    private readonly int stallTicks;
    private readonly int burstTicks;
    private readonly int burstMoveInterval;

    private int stallTicksRemaining = 0;
    private int burstTicksRemaining = 0;
    private bool hasStartedCycle = false;

    public SurgingKeyword(int stallTicks = 2, int burstTicks = 2, int burstMoveInterval = 1)
    {
        this.stallTicks = Mathf.Max(1, stallTicks);
        this.burstTicks = Mathf.Max(1, burstTicks);
        this.burstMoveInterval = Mathf.Max(1, burstMoveInterval);
    }

    public void OnTick(PacketView packet, KeywordContext context)
    {
        if (packet == null || packet.isRemoved || packet.hasArrived)
            return;

        if (!hasStartedCycle)
        {
            hasStartedCycle = true;
            stallTicksRemaining = stallTicks;
        }

        if (stallTicksRemaining > 0)
        {
            stallTicksRemaining--;
            packet.ticksUntilAdvance = Mathf.Max(packet.ticksUntilAdvance, 2);

            if (stallTicksRemaining == 0)
                burstTicksRemaining = burstTicks;

            return;
        }

        if (burstTicksRemaining > 0)
        {
            burstTicksRemaining--;
            packet.ticksUntilAdvance = Mathf.Min(packet.ticksUntilAdvance, burstMoveInterval);

            if (burstTicksRemaining == 0)
                stallTicksRemaining = stallTicks;

            return;
        }
    }

    public void OnScanned(PacketView packet, KeywordContext context)
    {
    }
}