using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class ScanDirector : MonoBehaviour
{
    [Header("Scan Slots")]
    [Min(1)] public int maxActiveScans = 2;

    [Header("Tuning")]
    public int baseScanDurationTicksSingle = 30;
    public int baseScanDurationTicksDual = 45;

    [Header("Completion")]
    [Min(0)] public int completedScanLingerTicks = 8;

    [Header("UI Theme")]
    [SerializeField] private ScanLogTheme logTheme = new();

    private CommandDirector commandDirector;

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

    public void SetCommandDirector(CommandDirector director)
    {
        commandDirector = director;
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
            SubscribeToPacket(packet);
            emptySlot.Assign(packet, tickCounter);
            RefreshActiveScanTags();
            return;
        }

        ScanSlot replacementSlot = GetReplacementCandidateSlot();
        if (replacementSlot != null)
        {
            UnsubscribeFromPacket(replacementSlot.target);
            AddCompletedEntry(replacementSlot.target);
            SubscribeToPacket(packet);
            replacementSlot.Assign(packet, tickCounter);
        }

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
        if (slot != null)
        {
            UnsubscribeFromPacket(slot.target);
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

    private void SubscribeToPacket(PacketView packet)
    {
        if (packet == null)
            return;

        packet.OnScanStageChanged -= HandlePacketScanStageChanged;
        packet.OnScanStageChanged += HandlePacketScanStageChanged;
    }

    private void UnsubscribeFromPacket(PacketView packet)
    {
        if (packet == null)
            return;

        packet.OnScanStageChanged -= HandlePacketScanStageChanged;
    }

    private void HandlePacketScanStageChanged(PacketView packet, ScanStage oldStage, ScanStage newStage)
    {
        if (packet == null || newStage == ScanStage.Unknown)
            return;

        commandDirector?.LogIntelStageChange(packet, newStage);
    }

    private void TickActiveScans()
    {
        int activeCount = GetActiveScanCount();
        if (activeCount <= 0)
            return;

        int baseDurationTicks = GetBaseScanDurationTicks(activeCount);

        if (baseDurationTicks <= 0)
            baseDurationTicks = 1;

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
                UnsubscribeFromPacket(packet);
                slot.Clear();
                RefreshActiveScanTags();
                continue;
            }

            packet.AddScanProgress(packet.GetScanProgressPerTick(baseDurationTicks));

            if (!packet.CanAdvanceScanStage())
            {
                AddCompletedEntry(packet);
                UnsubscribeFromPacket(packet);
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

    private int GetBaseScanDurationTicks(int activeCount)
    {
        return activeCount <= 1 ? baseScanDurationTicksSingle : baseScanDurationTicksDual;
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
            ScanStage.Unknown => "UNKNOWN",
            ScanStage.Probable => "PROBABLE",
            ScanStage.Likely => "LIKELY",
            ScanStage.Confirmed => "CONFIRMED",
            _ => "----"
        };
    }

    private string GetVisibleClassShortLabel(PacketView packet)
    {
        if (packet == null)
            return "----";

        return packet.GetVisibleClass() switch
        {
            VisibleClass.Unknown => "UNKNOWN",
            VisibleClass.Benign => "BENIGN",
            VisibleClass.Threat => "THREAT",
            VisibleClass.Priority => "PRIORITY",
            _ => "----"
        };
    }

    private string GetPacketClassShortLabel(PacketClass packetClass)
    {
        return packetClass switch
        {
            PacketClass.Benign => "BENIGN",
            PacketClass.Threat => "THREAT",
            PacketClass.Priority => "PRIORITY",
            _ => "----"
        };
    }

    private int GetEtaTicksToNextStage(PacketView packet, int activeCount)
    {
        if (packet == null || !packet.CanAdvanceScanStage())
            return 0;

        int baseDurationTicks = GetBaseScanDurationTicks(activeCount);
        if (baseDurationTicks <= 0)
            baseDurationTicks = 1;

        float current = packet.GetScanConfidence01();
        float target = packet.GetCurrentStageEndConfidence01();
        float gainPerTick = packet.GetScanProgressPerTick(baseDurationTicks);

        if (gainPerTick <= 0f)
            return 0;

        float remaining = Mathf.Max(0f, target - current);
        return Mathf.CeilToInt(remaining / gainPerTick);
    }

    private string GetStageRichLabel(ScanStage stage)
    {
        string shortLabel = GetStageShortLabel(stage);

        return stage switch
        {
            ScanStage.Unknown   => RichTextUtil.Colorize(shortLabel, logTheme.stageUnknown),
            ScanStage.Probable  => RichTextUtil.Colorize(shortLabel, logTheme.stageProbable, true),
            ScanStage.Likely    => RichTextUtil.Colorize(shortLabel, logTheme.stageLikely, true),
            ScanStage.Confirmed => RichTextUtil.Colorize(shortLabel, logTheme.stageConfirmed, true),
            _ => shortLabel
        };
    }

    private string GetVisibleClassRichLabel(PacketView packet)
    {
        if (packet == null)
            return "----";

        string shortLabel = GetVisibleClassShortLabel(packet);

        return packet.GetVisibleClass() switch
        {
            VisibleClass.Unknown  => RichTextUtil.Colorize(shortLabel, logTheme.classUnknown),
            VisibleClass.Benign   => RichTextUtil.Colorize(shortLabel, logTheme.classBenign, true),
            VisibleClass.Threat   => RichTextUtil.Colorize(shortLabel, logTheme.classThreat, true),
            VisibleClass.Priority => RichTextUtil.Colorize(shortLabel, logTheme.classPriority, true),
            _ => shortLabel
        };
    }

    private string GetPacketClassRichLabel(PacketClass packetClass)
    {
        string shortLabel = GetPacketClassShortLabel(packetClass);

        return packetClass switch
        {
            PacketClass.Benign   => RichTextUtil.Colorize(shortLabel, logTheme.classBenign, true),
            PacketClass.Threat   => RichTextUtil.Colorize(shortLabel, logTheme.classThreat, true),
            PacketClass.Priority => RichTextUtil.Colorize(shortLabel, logTheme.classPriority, true),
            _ => shortLabel
        };
    }

    public ScanLogTheme GetLogTheme()
    {
        return logTheme;
    }

    public string GetStageShortLabelPublic(ScanStage stage)
    {
        return GetStageShortLabel(stage);
    }

    public string GetVisibleClassShortLabelPublic(PacketView packet)
    {
        return GetVisibleClassShortLabel(packet);
    }

    public string GetPacketClassShortLabelPublic(PacketClass packetClass)
    {
        return GetPacketClassShortLabel(packetClass);
    }

    public void AppendOperationsPanel(StringBuilder sb)
    {
        int activeCount = GetActiveScanCount();

        sb.AppendLine("<b>SCANS</b>   " + activeCount + " / " + maxActiveScans);
        sb.AppendLine();

        for (int i = 0; i < slots.Count; i++)
        {
            ScanSlot slot = slots[i];

            if (slot.IsEmpty())
            {
                sb.AppendLine($"S{i + 1}  {RichTextUtil.Colorize("empty", logTheme.muted)}");
                continue;
            }

            PacketView p = slot.target;

            if (p == null)
            {
                sb.AppendLine($"S{i + 1}  {RichTextUtil.Colorize("null", logTheme.muted)}");
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

            string stageLabel = GetStageRichLabel(p.scanStage);
            string classLabel = GetVisibleClassRichLabel(p);

            int etaTicks = GetEtaTicksToNextStage(p, activeCount);

            int currentPct = Mathf.RoundToInt(p.GetScanConfidence01() * 100f);
            string percentText = p.IsScanComplete()
                ? "100%"
                : $"{currentPct}%";

            string etaText = p.IsScanComplete()
                ? "--"
                : etaTicks.ToString();

            string dropMarker = willBeDropped ? "  <b>!</b>" : "";

            sb.AppendLine(
                $"S{i + 1}  <b>{p.packetId}</b>  {stageLabel}  {classLabel}{dropMarker}"
            );
            sb.AppendLine(
                $"    {bar.PadRight(12)}  {percentText.PadLeft(4)}  ETA {etaText}"
            );
            sb.AppendLine();
        }

        if (completedEntries.Count > 0)
        {
            sb.AppendLine(RichTextUtil.Colorize("recent", logTheme.muted));

            for (int i = 0; i < completedEntries.Count; i++)
            {
                var entry = completedEntries[i];

                string finalStage = GetStageRichLabel(entry.finalStage);
                string finalClass = GetPacketClassRichLabel(entry.reportedClass);

                sb.AppendLine(
                    $"  {entry.packetId.PadRight(6)}  {finalStage}  {finalClass}  [■■■]"
                );
            }

            sb.AppendLine();
        }
    }

}