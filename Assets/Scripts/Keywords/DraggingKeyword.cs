using UnityEngine;

public class DraggingKeyword : IPacketKeyword
{
    public string KeywordId => "dragging";
    public string DisplayName => "Dragging";
    public string Description => "Slows nearby packets on the same connection.";

    private readonly int radiusSteps;
    private readonly int slowAmount;
    private readonly bool ignoreSameClassAndKind;

    public DraggingKeyword(int radiusSteps = 2, int slowAmount = 1, bool ignoreSameClassAndKind = true)
    {
        this.radiusSteps = Mathf.Max(1, radiusSteps);
        this.slowAmount = Mathf.Max(1, slowAmount);
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

            context.AddSpeedModifier(other, slowAmount);
        }
    }

    public void OnScanned(PacketView packet, KeywordContext context) { }
}