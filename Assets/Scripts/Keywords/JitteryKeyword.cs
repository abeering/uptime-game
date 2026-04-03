using UnityEngine;

public class JitteryKeyword : IPacketKeyword
{
    public string KeywordId => "jittery";
    public string DisplayName => "Jittery";
    public string Description => "Moves with small timing jitter, arriving a little earlier or later than expected.";

    private readonly int jitterAmount;

    public JitteryKeyword(int jitterAmount = 1)
    {
        this.jitterAmount = Mathf.Max(1, jitterAmount);
    }

    public void OnTick(PacketView packet, KeywordContext context)
    {
        if (packet == null || packet.isRemoved || packet.hasArrived)
            return;

        int delta = Random.Range(-jitterAmount, jitterAmount + 1);

        if (delta == 0)
            return;

        packet.ticksUntilAdvance = Mathf.Max(1, packet.ticksUntilAdvance + delta);
    }

    public void OnScanned(PacketView packet, KeywordContext context)
    {
    }
}