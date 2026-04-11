using UnityEngine;

public class AcceleratingKeyword : IPacketKeyword
{
    public string KeywordId => "accelerating";
    public string DisplayName => "Accelerating";
    public string Description => "Speeds up nearby packets on the same connection.";

    private readonly int radiusSteps;
    private readonly int speedDelta;
    private readonly bool ignoreSameClassAndKind;

    public AcceleratingKeyword(int radiusSteps = 2, int speedDelta = -1, bool ignoreSameClassAndKind = true)
    {
        this.radiusSteps = Mathf.Max(1, radiusSteps);
        this.speedDelta = Mathf.Min(-1, speedDelta);
        this.ignoreSameClassAndKind = ignoreSameClassAndKind;
    }

    public void OnTick(PacketView packet, KeywordContext context)
    {
        if (packet == null || packet.isRemoved || packet.hasArrived)
            return;

        var connection = packet.GetCurrentConnection();
        if (connection == null)
            return;

        var neighbors = context.runtime.GetPacketsOnConnection(connection);

        foreach (var other in neighbors)
        {
            if (other == null || other == packet)
                continue;

            if (ignoreSameClassAndKind &&
                other.trueClass == packet.trueClass &&
                other.trueKind == packet.trueKind)
            {
                continue;
            }

            int dist = Mathf.Abs(other.currentStep - packet.currentStep);
            if (dist > radiusSteps)
                continue;

            context.AddSpeedModifier(other, speedDelta);
        }
    }

    public void OnScanned(PacketView packet, KeywordContext context) { }
}