using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

public class ScanDirector : MonoBehaviour
{
    [Header("Scan Slots")]
    [Min(1)] public int maxActiveScans = 2;

    [Header("Tuning")]
    public int baseScanDurationTicksSingle = 30;
    public int baseScanDurationTicksDual = 45;

    [Header("UI Theme")]
    [SerializeField] private ScanLogTheme logTheme = new();

    private CommandDirector commandDirector;

    private readonly List<ScanSlot> slots = new();
    private readonly List<ActiveScanCompletion> completionLinger = new();
    private const int completionLingerTicks = 6;

    private int tickCounter = 0;

    private class ActiveScanCompletion
    {
        public int slotIndex;
        public string packetId;
        public ScanStage stage;
        public VisibleClass visibleClass;
        public string barText;
        public string percentText;
        public string kindText;
        public int lingerTicks;
        public bool wasReplacementCandidate;
    }

    void Awake()
    {
        slots.Clear();

        for (int i = 0; i < maxActiveScans; i++)
            slots.Add(new ScanSlot(i, logTheme));
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
            replacementSlot.Assign(packet, tickCounter);
            SubscribeToPacket(packet);
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

        packet.OnIntelRevealed -= HandlePacketIntelRevealed;
        packet.OnIntelRevealed += HandlePacketIntelRevealed;
    }

    private void UnsubscribeFromPacket(PacketView packet)
    {
        if (packet == null)
            return;

        packet.OnScanStageChanged -= HandlePacketScanStageChanged;
        packet.OnIntelRevealed -= HandlePacketIntelRevealed;
    }

    private void HandlePacketScanStageChanged(PacketView packet, ScanStage oldStage, ScanStage newStage)
    {
        if (packet == null || newStage == ScanStage.Unknown)
            return;

        commandDirector?.LogIntelStageChange(packet, newStage);
    }

    private void HandlePacketIntelRevealed(PacketView packet, IntelRevealType revealType, string revealedValue)
    {
        if (packet == null || string.IsNullOrWhiteSpace(revealedValue))
            return;

        commandDirector?.LogIntelReveal(packet, revealType, revealedValue);
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
                continue;
            }

            if (!packet.CanAdvanceScanStage())
            {
                slot.Clear();
                continue;
            }

            packet.AddScanProgress(packet.GetScanProgressPerTick(baseDurationTicks));

            if (!packet.CanAdvanceScanStage())
            {
                bool willBeDropped = WouldBeDropped(packet);

                string bar = ScanBarFormatter.BuildOperationsScanBarOnly(
                    packet.GetScanDisplayStageIndex(),
                    packet.GetScanConfidence01(),
                    true,
                    false,
                    activeStageChar: '='
                );

                completionLinger.Add(new ActiveScanCompletion
                {
                    slotIndex = i,
                    packetId = packet.packetId,
                    stage = packet.scanStage,
                    visibleClass = packet.GetVisibleClass(),
                    barText = bar,
                    percentText = "100%",
                    kindText = GetKindLine(packet),
                    lingerTicks = completionLingerTicks,
                    wasReplacementCandidate = willBeDropped
                });

                UnsubscribeFromPacket(packet);
                slot.Clear();
            }
        }

        RefreshActiveScanTags();
    }

    private void TickCompletedEntries()
    {
        for (int i = completionLinger.Count - 1; i >= 0; i--)
        {
            completionLinger[i].lingerTicks--;

            if (completionLinger[i].lingerTicks <= 0)
                completionLinger.RemoveAt(i);
        }
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

    public ScanSlot FindSlotForPacket(PacketView packet)
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
        if (FindEmptySlot() != null)
            return null;

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

    private string GetVisibleClassShortLabel(VisibleClass visibleClass)
    {
        return visibleClass switch
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

    private ScanPanelRowData BuildEmptyRow(int slotIndex)
    {
        return new ScanPanelRowData
        {
            slotIndex = slotIndex,
            state = ScanPanelRowState.Empty,
            packetId = "--",
            barText = "[---]",
            percentText = "--%",
            etaText = "--",
            stage = ScanStage.Unknown,
            visibleClass = VisibleClass.Unknown,
            showDone = false,
            willBeDropped = false,
            secondaryIntelLine = null
        };
    }

    private ScanPanelRowData BuildLingerRow(ActiveScanCompletion linger)
    {
        if (linger == null)
            return null;

        return new ScanPanelRowData
        {
            slotIndex = linger.slotIndex,
            state = ScanPanelRowState.CompletedLinger,
            packetId = linger.packetId,
            barText = linger.barText,
            percentText = linger.percentText,
            etaText = "--",
            stage = linger.stage,
            visibleClass = linger.visibleClass,
            showDone = true,
            willBeDropped = linger.wasReplacementCandidate,
            // TODO 
            // secondaryIntelLine = GetKindLine(linger)
            secondaryIntelLine = null
        };
    }

    private ScanPanelRowData BuildActiveRow(int slotIndex, PacketView p, int activeCount)
    {
        bool willBeDropped = WouldBeDropped(p);

        string bar = ScanBarFormatter.BuildOperationsScanBarOnly(
            p.GetScanDisplayStageIndex(),
            p.GetScanConfidence01(),
            p.IsScanComplete(),
            false,
            activeStageChar: '='
        );

        int currentPct = Mathf.RoundToInt(p.GetScanConfidence01() * 100f);
        string percentText = p.IsScanComplete() ? "100%" : $"{currentPct}%";

        int etaTicks = GetEtaTicksToNextStage(p, activeCount);
        string etaText = p.IsScanComplete() ? "--" : etaTicks.ToString();

        return new ScanPanelRowData
        {
            slotIndex = slotIndex,
            state = ScanPanelRowState.Active,
            packetId = p.packetId,
            barText = bar,
            percentText = percentText,
            etaText = etaText,
            stage = p.scanStage,
            visibleClass = p.GetVisibleClass(),
            showDone = false,
            willBeDropped = willBeDropped,
            // TODO Fix 
            // secondaryIntelLine = GetKindLine(p)
            secondaryIntelLine = null
        };
    }

    private ScanPanelRowData BuildRowForSlot(int slotIndex, int activeCount)
    {
        ActiveScanCompletion linger = completionLinger.Find(c => c.slotIndex == slotIndex);
        if (linger != null)
            return BuildLingerRow(linger);

        ScanSlot slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty() || slot.target == null)
            return BuildEmptyRow(slotIndex);

        return BuildActiveRow(slotIndex, slot.target, activeCount);
    }

    // this has to do a weird thing to maintain alignment, we colorize the ! alpha=0 inside Colorize()
    // so we are double coloring, but its necessary for alignment
    private string FormatWarnPrefix(bool showWarning)
    {
        string glyph = showWarning
            ? "!"
            : "<color=#FFFFFF00>!</color>";

        return RichTextUtil.Colorize($"{glyph} ", logTheme.stageProbable, true);
    }

    private void AppendFormattedScanRow(StringBuilder sb, ScanPanelRowData row)
    {
        string warnPrefix = FormatWarnPrefix(row.willBeDropped);
        string slotTag = FormatInlineSlotTag(row.slotIndex);

        if (row.state == ScanPanelRowState.Empty)
        {
            sb.Append(warnPrefix);
            sb.Append(" ");
            sb.Append(slotTag);
            sb.Append(" ");
            sb.Append(ColorizeMutedPadded(PadRightSafe(row.packetId, 3)));
            sb.Append("  ");
            sb.Append(ColorizeMutedPadded(PadRightSafe(row.barText, 5)));
            sb.Append("  ");
            sb.Append(ColorizeMutedPadded(PadLeftSafe(row.percentText, 4)));
            sb.Append("  ");
            sb.Append(ColorizeMuted("ETA"));
            sb.Append(" ");
            sb.Append(ColorizeMuted(PadLeftSafe(row.etaText, 2)));
            sb.Append("  ");
            sb.Append(ColorizeMutedPadded(PadRightSafe("empty", 9)));
            sb.AppendLine();
            return;
        }

        string stageText = ColorizeStagePadded(
            row.stage,
            PadRightSafe(GetStageShortLabel(row.stage), 9)
        );

        string classText = ColorizeVisibleClassPadded(
            row.visibleClass,
            PadRightSafe(GetVisibleClassShortLabel(row.visibleClass), 8)
        );

        sb.Append(warnPrefix);
        sb.Append(" ");
        sb.Append(slotTag);
        sb.Append(" ");
        sb.Append(PadRightSafe(row.packetId, 3));
        sb.Append("  ");
        sb.Append(PadRightSafe(row.barText, 5));
        sb.Append("  ");
        sb.Append(PadLeftSafe(row.percentText, 4));
        sb.Append("  ");

        if (row.showDone)
        {
            sb.Append(ColorizeMuted("DONE", true));
            sb.Append(" ");
            sb.Append("  ");
        }
        else
        {
            sb.Append(ColorizeMuted("ETA"));
            sb.Append(" ");
            sb.Append(PadLeftSafe(row.etaText, 2));
            sb.Append("  ");
        }

        sb.Append(stageText);
        sb.Append(" ");
        sb.Append(classText);
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(row.secondaryIntelLine))
        {
            sb.Append("  ");   // warning col + spacer
            sb.Append("    "); // [S1]
            sb.Append(" ");    // spacer after tag
            sb.Append("    "); // packet id area
            sb.Append(ColorizeVisibleClassPadded(
                row.visibleClass,
                row.secondaryIntelLine.Trim()
            ));
            sb.AppendLine();
        }
    }

    public void AppendScanPanel(StringBuilder sb)
    {
        int activeCount = GetActiveScanCount();

        for (int i = 0; i < slots.Count; i++)
        {
            ScanPanelRowData row = BuildRowForSlot(i, activeCount);
            if (row == null)
                continue;

            AppendFormattedScanRow(sb, row);
        }
    }

    public void AppendOperationsPanel(StringBuilder sb)
    {
        AppendScanPanel(sb);
    }
    
    public void AppendKnownThreatsSection(StringBuilder sb, NetworkRuntime networkRuntime)
    {
        sb.AppendLine("<b>KNOWN THREATS</b>");

        if (networkRuntime == null)
        {
            sb.AppendLine(RichTextUtil.Colorize("none", logTheme.muted));
            return;
        }

        List<PacketView> knownThreats = networkRuntime.GetKnownThreatPackets();

        if (knownThreats.Count == 0)
        {
            sb.AppendLine(RichTextUtil.Colorize("none", logTheme.muted));
            return;
        }

        for (int i = 0; i < knownThreats.Count; i++)
        {
            PacketView packet = knownThreats[i];
            if (packet == null)
                continue;

            string stageLabel = GetStageRichLabel(packet.scanStage);
            string classLabel = GetVisibleClassRichLabel(packet);

            sb.AppendLine($"T{i + 1}  <b>{packet.packetId}</b>  {stageLabel}  {classLabel}");
            sb.AppendLine($"    src={packet.sourceAddress}  dest={packet.GetDestinationName()}");

            string intelSummary = packet.BuildOperationsIntelSummary();
            if (!string.IsNullOrWhiteSpace(intelSummary))
                sb.AppendLine($"    {intelSummary}");

            sb.AppendLine();
        }
    }

    private enum ScanPanelRowState
    {
        Empty,
        Active,
        CompletedLinger
    }

    private class ScanPanelRowData
    {
        public int slotIndex;
        public ScanPanelRowState state;

        public string packetId;
        public string barText;
        public string percentText;
        public string etaText;

        public ScanStage stage;
        public VisibleClass visibleClass;

        public bool showDone;
        public bool willBeDropped;

        public string secondaryIntelLine;
    }

    private Color GetSlotColor(int slotIndex)
    {
        return slotIndex switch
        {
            0 => logTheme.slot1,
            1 => logTheme.slot2,
            2 => logTheme.slot3,
            3 => logTheme.slot4,
            _ => logTheme.muted
        };
    }

    private string FormatInlineSlotTag(int slotIndex)
    {
        return RichTextUtil.Colorize($"[S{slotIndex + 1}]", GetSlotColor(slotIndex), true);
    }

    private string GetKindLine(PacketView p)
    {
        if (p == null || !p.knowsKind || p.revealedKind == PacketKind.None)
            return "";

        return p.revealedKind.ToString();
    }

    private string GetKindLine(ActiveScanCompletion linger)
    {
        if (linger == null || string.IsNullOrWhiteSpace(linger.kindText))
            return "";

        return linger.kindText;
    }

    private static string PadRightSafe(string value, int width)
    {
        value ??= "";
        return value.Length >= width ? value.Substring(0, width) : value.PadRight(width);
    }

    private static string PadLeftSafe(string value, int width)
    {
        value ??= "";
        return value.Length >= width ? value.Substring(0, width) : value.PadLeft(width);
    }

    private string ColorizeMuted(string text, bool bold = false)
    {
        return RichTextUtil.Colorize(text, logTheme.muted, bold);
    }

    private string ColorizeMutedPadded(string paddedLabel, bool bold = false)
    {
        return RichTextUtil.Colorize(paddedLabel, logTheme.muted, bold);
    }

    private string ColorizeStagePadded(ScanStage stage, string paddedLabel)
    {
        return stage switch
        {
            ScanStage.Unknown   => RichTextUtil.Colorize(paddedLabel, logTheme.stageUnknown, true),
            ScanStage.Probable  => RichTextUtil.Colorize(paddedLabel, logTheme.stageProbable, true),
            ScanStage.Likely    => RichTextUtil.Colorize(paddedLabel, logTheme.stageLikely, true),
            ScanStage.Confirmed => RichTextUtil.Colorize(paddedLabel, logTheme.stageConfirmed, true),
            _ => paddedLabel
        };
    }

    private string ColorizeVisibleClassPadded(VisibleClass visibleClass, string paddedLabel)
    {
        return visibleClass switch
        {
            VisibleClass.Unknown  => RichTextUtil.Colorize(paddedLabel, logTheme.classUnknown, true),
            VisibleClass.Benign   => RichTextUtil.Colorize(paddedLabel, logTheme.classBenign, true),
            VisibleClass.Threat   => RichTextUtil.Colorize(paddedLabel, logTheme.classThreat, true),
            VisibleClass.Priority => RichTextUtil.Colorize(paddedLabel, logTheme.classPriority, true),
            _ => paddedLabel
        };
    }

}