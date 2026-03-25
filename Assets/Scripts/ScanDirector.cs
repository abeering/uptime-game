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
            RefreshActiveScanTags();
            return;
        }

        ScanSlot replacementSlot = GetReplacementCandidateSlot();
        if (replacementSlot != null)
            replacementSlot.Assign(packet, tickCounter);

        RefreshActiveScanTags();
    }

    private void RefreshActiveScanTags()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            ScanSlot slot = slots[i];
            if (slot == null || slot.target == null)
                continue;

            slot.target.RefreshScanTag(this);
        }
    }

    public void RemovePacket(PacketView packet)
    {
        if (packet == null)
            return;

        ScanSlot slot = FindSlotForPacket(packet);
        if (slot != null){
            slot.Clear();
            RefreshActiveScanTags();
        }
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
                RefreshActiveScanTags();
                continue;
            }

            if (!packet.CanAdvanceScanStage())
            {
                slot.Clear();
                RefreshActiveScanTags();
                continue;
            }

            packet.AddScanTicks(1);

            if (!packet.CanAdvanceScanStage())
            {
                AddCompletedEntry(packet);
                slot.Clear();
                RefreshActiveScanTags();
            }
        }

        RefreshActiveScanTags();
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

    public bool WouldBeDropped(PacketView packet)
    {
        if (packet == null)
            return false;

        ScanSlot candidate = GetReplacementCandidateSlot();
        return candidate != null && candidate.target == packet;
    }

    public bool IsPacketActivelyScanned(PacketView packet)
    {
        if (packet == null)
            return false;

        return FindSlotForPacket(packet) != null;
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

    private ScanSlot GetReplacementCandidateSlot()
    {
        return FindOldestSlot();
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

    private string GetStageShortLabel(ScanStage stage)
    {
        return stage switch
        {
            ScanStage.Unknown => "UNKN",
            ScanStage.Probable => "PROB",
            ScanStage.Likely => "LKLY",
            ScanStage.Confirmed => "CONF",
            _ => "----"
        };
    }

    private string GetVisibleClassShortLabel(PacketView packet)
    {
        if (packet == null)
            return "----";

        return packet.GetVisibleClass() switch
        {
            VisibleClass.Unknown => "UNKN",
            VisibleClass.Benign => "BEN ",
            VisibleClass.Threat => "THRT",
            VisibleClass.Priority => "PRIO",
            _ => "----"
        };
    }

    private string GetPacketClassShortLabel(PacketClass packetClass)
    {
        return packetClass switch
        {
            PacketClass.Benign => "BEN ",
            PacketClass.Threat => "THRT",
            PacketClass.Priority => "PRIO",
            _ => "----"
        };
    }

    private int GetEtaTicksToNextStage(PacketView packet, int activeCount)
    {
        if (packet == null || !packet.CanAdvanceScanStage())
            return 0;

        int ticksPerAdvance = GetTicksPerAdvance(activeCount);
        if (ticksPerAdvance <= 0)
            ticksPerAdvance = 1;

        int requiredStageTicks = packet.GetTicksRequiredForNextScanStage();
        int remainingStageTicks = Mathf.Max(0, requiredStageTicks - packet.scanTicksIntoStage);

        return remainingStageTicks * ticksPerAdvance;
    }

    public void AppendOperationsPanel(StringBuilder sb)
    {
        sb.AppendLine("SCANS");

        int activeCount = GetActiveScanCount();

        if (completedEntries.Count > 0)
        {
            sb.AppendLine("recent:");

            for (int i = 0; i < completedEntries.Count; i++)
            {
                var entry = completedEntries[i];
                sb.AppendLine(
                    $"  {entry.packetId.PadRight(6)}  {GetStageShortLabel(entry.finalStage)}  {GetPacketClassShortLabel(entry.reportedClass)}  [■■■]"
                );
            }

            sb.AppendLine();
        }

        for (int i = 0; i < slots.Count; i++)
        {
            ScanSlot slot = slots[i];

            if (slot.IsEmpty())
            {
                sb.AppendLine($"S{i + 1}  empty");
                continue;
            }

            PacketView p = slot.target;

            if (p == null)
            {
                sb.AppendLine($"S{i + 1}  null");
                continue;
            }

            bool willBeDropped = WouldBeDropped(p);

            string bar = ScanBarFormatter.BuildOperationsScanBar(
                p.GetScanDisplayStageIndex(),
                p.GetScanConfidence01(),
                p.IsScanComplete(),
                willBeDropped,
                activeStageChar: '='
            );

            string stageLabel = GetStageShortLabel(p.scanStage);
            string classLabel = GetVisibleClassShortLabel(p);

            int current = p.scanTicksIntoStage;
            int required = p.GetTicksRequiredForNextScanStage();
            int etaTicks = GetEtaTicksToNextStage(p, activeCount);

            string stageTicksText = p.IsScanComplete()
                ? "--/--"
                : $"{current}/{required}";

            string etaText = p.IsScanComplete()
                ? "--"
                : etaTicks.ToString();

            sb.AppendLine(
                $"S{i + 1}  " +
                $"{p.packetId.PadRight(6)}  " +
                $"{stageLabel}  " +
                $"{classLabel}  " +
                $"{bar.PadRight(12)}  " +
                $"STG {stageTicksText.PadLeft(5)}  " +
                $"ETA {etaText}"
            );
        }
    }

}