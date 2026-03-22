using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class ScanDirector : MonoBehaviour
{
    [Header("Scan Slots")]
    [Min(1)] public int maxActiveScans = 2;

    [Header("Attention Tuning")]
    [Min(1)] public int ticksPerScanAdvanceSingle = 1;
    [Min(1)] public int ticksPerScanAdvanceDual = 2;

    [Header("Completion")]
    [Min(0)] public int completedScanLingerTicks = 3;

    private readonly List<ScanSlot> slots = new();
    private readonly List<CompletedScanEntry> completedEntries = new();

    private int tickCounter = 0;

    void Awake()
    {
        slots.Clear();

        for (int i = 0; i < maxActiveScans; i++)
            slots.Add(new ScanSlot(i));
    }

    public void Tick()
    {
        tickCounter++;
        TickCompletedEntries();
        TickActiveScans();
    }

    public void StartScan(PacketView packet)
    {
        if (packet == null)
            return;

        if (!packet.CanAdvanceScanStage())
            return;

        ScanSlot existingSlot = FindSlotForPacket(packet);
        if (existingSlot != null)
            return;

        ScanSlot emptySlot = FindEmptySlot();
        if (emptySlot != null)
        {
            emptySlot.Assign(packet, tickCounter);
            return;
        }

        ScanSlot oldestSlot = FindOldestSlot();
        if (oldestSlot != null)
            oldestSlot.Assign(packet, tickCounter);
    }

    public void RemovePacket(PacketView packet)
    {
        if (packet == null)
            return;

        ScanSlot slot = FindSlotForPacket(packet);
        if (slot != null)
            slot.Clear();
    }

    public IReadOnlyList<ScanSlot> GetSlots()
    {
        return slots;
    }

    public int GetActiveScanCount()
    {
        int count = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty())
                count++;
        }

        return count;
    }

    private void TickActiveScans()
    {
        int activeCount = GetActiveScanCount();
        if (activeCount <= 0)
            return;

        int ticksPerAdvance = GetTicksPerAdvance(activeCount);

        if (ticksPerAdvance <= 0)
            ticksPerAdvance = 1;

        if (tickCounter % ticksPerAdvance != 0)
            return;

        for (int i = 0; i < slots.Count; i++)
        {
            ScanSlot slot = slots[i];
            if (slot.IsEmpty())
                continue;

            PacketView packet = slot.target;
            if (packet == null)
            {
                slot.Clear();
                continue;
            }

            if (!packet.CanAdvanceScanStage())
            {
                slot.Clear();
                continue;
            }

            packet.AddScanTicks(1);

            if (!packet.CanAdvanceScanStage())
            {
                AddCompletedEntry(packet);
                slot.Clear();
            }
        }
    }

    private void TickCompletedEntries()
    {
        for (int i = completedEntries.Count - 1; i >= 0; i--)
        {
            completedEntries[i].lingerTicksRemaining--;

            if (completedEntries[i].lingerTicksRemaining <= 0)
                completedEntries.RemoveAt(i);
        }
    }

    private void AddCompletedEntry(PacketView packet)
    {
        if (packet == null)
            return;

        completedEntries.Add(new CompletedScanEntry(
            packet.packetId,
            packet.scanStage,
            packet.reportedClass,
            completedScanLingerTicks
        ));

        while (completedEntries.Count > 4)
            completedEntries.RemoveAt(0);
    }

    private int GetTicksPerAdvance(int activeCount)
    {
        if (activeCount <= 1)
            return ticksPerScanAdvanceSingle;

        return ticksPerScanAdvanceDual;
    }

    private ScanSlot FindEmptySlot()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty())
                return slots[i];
        }

        return null;
    }

    private ScanSlot FindSlotForPacket(PacketView packet)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].Matches(packet))
                return slots[i];
        }

        return null;
    }

    private ScanSlot FindOldestSlot()
    {
        ScanSlot oldest = null;

        for (int i = 0; i < slots.Count; i++)
        {
            ScanSlot slot = slots[i];
            if (slot.IsEmpty())
                continue;

            if (oldest == null || slot.assignedTick < oldest.assignedTick)
                oldest = slot;
        }

        return oldest;
    }

    public void AppendOperationsPanel(StringBuilder sb)
    {
        sb.AppendLine("SCANS");

        for (int i = 0; i < completedEntries.Count; i++)
        {
            var entry = completedEntries[i];
            sb.AppendLine($"completed: {entry.packetId} {entry.finalStage}");
        }

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];

            if (slot.IsEmpty())
            {
                sb.AppendLine($"[scan {i + 1}] empty");
                continue;
            }

            var p = slot.target;
            int required = p.GetTicksRequiredForNextScanStage();
            int current = p.scanTicksIntoStage;

            sb.AppendLine(
                $"[scan {i + 1}] {p.packetId} {p.scanStage} → ({current}/{required})"
            );
        }
    }

}