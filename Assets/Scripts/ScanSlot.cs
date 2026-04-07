using UnityEngine;

public class ScanSlot
{
    public int slotIndex;
    public PacketView target;
    public int assignedTick = -1;

    private readonly string packetTagText;
    private readonly Color slotColor;

    public string PacketTagText => packetTagText;
    public Color SlotColor => slotColor;

    public ScanSlot(int newSlotIndex, string newPacketTagText, Color newSlotColor)
    {
        slotIndex = newSlotIndex;
        packetTagText = newPacketTagText;
        slotColor = newSlotColor;
    }

    public bool IsEmpty()
    {
        return target == null;
    }

    public Color GetThemeColor()
    {
        return slotColor;
    }

    public void Assign(PacketView packet, int currentTick)
    {
        Clear();

        target = packet;
        assignedTick = currentTick;

        if (target != null)
        {
            target.SetActiveIntelVisual(true, slotColor);
            target.ShowActiveIntelTag(packetTagText, slotColor);
        }
    }

    public void Clear()
    {
        if (target != null)
            target.SetActiveIntelVisual(false, slotColor);

        target = null;
        assignedTick = -1;
    }

    public bool Matches(PacketView packet)
    {
        return target == packet;
    }
}