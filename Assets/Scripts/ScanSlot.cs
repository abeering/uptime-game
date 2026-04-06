using UnityEngine;

public class ScanSlot
{
    public int slotIndex;
    public PacketView target;
    public int assignedTick = -1;

    private readonly ScanLogTheme theme;

    public ScanSlot(int newSlotIndex, ScanLogTheme logTheme)
    {
        slotIndex = newSlotIndex;
        theme = logTheme;
    }

    public bool IsEmpty()
    {
        return target == null;
    }

    public Color GetThemeColor()
    {
        if (theme == null)
            return Color.white;

        return slotIndex switch
        {
            0 => theme.slot1,
            1 => theme.slot2,
            2 => theme.slot3,
            3 => theme.slot4,
            _ => theme.muted
        };
    }

    public void Assign(PacketView packet, int currentTick)
    {
        Clear();

        target = packet;
        assignedTick = currentTick;

        if (target != null)
        {
            target.SetActivelyScanned(true);
            target.ShowActiveScanTag(this);
        }
    }

    public void Clear()
    {
        if (target != null)
        {
            target.SetActivelyScanned(false);
        }

        target = null;
        assignedTick = -1;
    }

    public bool Matches(PacketView packet)
    {
        return target == packet;
    }
}