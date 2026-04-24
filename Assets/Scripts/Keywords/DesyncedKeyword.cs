using UnityEngine;

public class DesyncedKeyword : IPacketKeyword
{
    public string KeywordId => "desynced";
    public string DisplayName => "Desynced";
    public string Description => "Stalls out of cadence, then jumps ahead multiple steps at once.";
    public float AnomalyModifier01 => 0.45f;

    private readonly int stallTicks;
    private readonly int teleportSteps;

    private int stallTicksRemaining = 0;
    private bool shouldTeleportNextTick = false;
    private bool hasStartedCycle = false;

    public DesyncedKeyword(int stallTicks = 2, int teleportSteps = 3)
    {
        this.stallTicks = Mathf.Max(1, stallTicks);
        this.teleportSteps = Mathf.Max(1, teleportSteps);
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

        if (shouldTeleportNextTick)
        {
            shouldTeleportNextTick = false;
            packet.AdvanceMultipleSteps(teleportSteps);
            stallTicksRemaining = stallTicks;
            return;
        }

        if (stallTicksRemaining > 0)
        {
            stallTicksRemaining--;
            packet.ticksUntilAdvance = Mathf.Max(packet.ticksUntilAdvance, 2);

            if (stallTicksRemaining == 0)
                shouldTeleportNextTick = true;

            return;
        }
    }

    public void OnScanned(PacketView packet, KeywordContext context)
    {
    }
}