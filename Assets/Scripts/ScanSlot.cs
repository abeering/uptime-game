using UnityEngine;

public class ScanSlot
{
    public int slotIndex;
    public PacketView target;
    public int assignedTick = -1;

    public ScanSlot(int newSlotIndex)
    {
        slotIndex = newSlotIndex;
    }

    public bool IsEmpty()
    {
        return target == null;
    }

    public void Assign(PacketView packet, int currentTick)
    {
        Clear();

        target = packet;
        assignedTick = currentTick;

        if (target != null)
        {
            target.SetActivelyScanned(true);
            target.SetActiveScanSlot(slotIndex);
        }
    }

    public void Clear()
    {
        if (target != null)
        {
            target.ClearActiveScanSlot();
            target.SetActivelyScanned(false);
            target.HideScanTag();
        }

        target = null;
        assignedTick = -1;
    }

    public bool Matches(PacketView packet)
    {
        return target == packet;
    }
}