using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

public enum IntelAssignResult
{
    Started,
    Replaced,
    AlreadyTracking,
    AlreadyComplete,
    InvalidSlot,
    NoAvailableSlot
}

public class ScanDirector : MonoBehaviour
{
    [Header("Scan Slots")]
    [Min(1)] public int maxActiveScans = 2;

    [Header("Trace Slots")]
    [Min(1)] public int maxActiveTraces = 1;

    [Header("Tuning")]
    public int baseScanDurationTicksSingle = 30;
    public int baseScanDurationTicksDual = 45;
    public int baseTraceDurationTicks = 12;

    [Header("UI Theme")]
    [SerializeField] private ScanLogTheme logTheme = new();

    private CommandDirector commandDirector;

    private readonly List<ScanSlot> scanSlots = new();
    private readonly List<ScanSlot> traceSlots = new();
    private readonly List<ActiveIntelCompletion> completionLinger = new();
    private const int completionLingerTicks = 6;

    private int tickCounter = 0;

    private enum IntelSlotMode
    {
        Scan,
        Trace
    }

    private class ActiveIntelCompletion
    {
        public IntelSlotMode mode;
        public int slotIndex;
        public string slotLabel;
        public Color slotColor;
        public string packetId;
        public ScanStage stage;
        public VisibleClass visibleClass;
        public string barText;
        public string percentText;
        public string secondaryText;
        public int lingerTicks;
        public bool wasReplacementCandidate;
    }

    void Awake()
    {
        scanSlots.Clear();
        traceSlots.Clear();

        for (int i = 0; i < maxActiveScans; i++)
        {
            Color slotColor = i switch
            {
                0 => logTheme.slot1,
                1 => logTheme.slot2,
                2 => logTheme.slot3,
                3 => logTheme.slot4,
                _ => logTheme.muted
            };

            scanSlots.Add(new ScanSlot(i, $"S{i + 1}", slotColor));
        }

        for (int i = 0; i < maxActiveTraces; i++)
            traceSlots.Add(new ScanSlot(i, $"T{i + 1}", logTheme.trace1));
    }

    public void Tick()
    {
        tickCounter++;
        TickCompletedEntries();
        TickActiveScans();
        TickActiveTraces();
    }

    public void SetCommandDirector(CommandDirector director)
    {
        commandDirector = director;
    }

    public IntelAssignResult StartScan(PacketView packet)
    {
        return StartScan(packet, null);
    }

    public IntelAssignResult StartScan(PacketView packet, int? preferredSlotIndex)
    {
        if (packet == null)
            return IntelAssignResult.NoAvailableSlot;

        if (!packet.CanAdvanceScanStage())
            return IntelAssignResult.AlreadyComplete;

        ScanSlot existingSlot = FindSlotForPacket(packet);
        if (existingSlot != null)
        {
            if (!preferredSlotIndex.HasValue || existingSlot.slotIndex == preferredSlotIndex.Value)
                return IntelAssignResult.AlreadyTracking;
        }

        if (preferredSlotIndex.HasValue)
        {
            int slotIndex = preferredSlotIndex.Value;

            if (!IsValidScanSlotIndex(slotIndex))
                return IntelAssignResult.InvalidSlot;

            ScanSlot targetSlot = scanSlots[slotIndex];

            if (targetSlot.target == packet)
                return IntelAssignResult.AlreadyTracking;

            if (existingSlot != null)
                ClearSlotAndMaybeUnsubscribe(existingSlot);

            bool wasReplacing = !targetSlot.IsEmpty();
            if (wasReplacing)
                ClearSlotAndMaybeUnsubscribe(targetSlot);

            SubscribeToPacket(packet);
            targetSlot.Assign(packet, tickCounter);
            RefreshAllPacketTags();

            return wasReplacing ? IntelAssignResult.Replaced : IntelAssignResult.Started;
        }

        ScanSlot emptySlot = FindEmptySlot(scanSlots);
        if (emptySlot != null)
        {
            SubscribeToPacket(packet);
            emptySlot.Assign(packet, tickCounter);
            RefreshAllPacketTags();
            return IntelAssignResult.Started;
        }

        ScanSlot replacementSlot = GetReplacementCandidateSlot();
        if (replacementSlot != null)
        {
            ClearSlotAndMaybeUnsubscribe(replacementSlot);

            SubscribeToPacket(packet);
            replacementSlot.Assign(packet, tickCounter);
            RefreshAllPacketTags();
            return IntelAssignResult.Replaced;
        }

        RefreshAllPacketTags();
        return IntelAssignResult.NoAvailableSlot;
    }

    public IntelAssignResult StartTrace(PacketView packet)
    {
        return StartTrace(packet, null);
    }

    public IntelAssignResult StartTrace(PacketView packet, int? preferredSlotIndex)
    {
        if (packet == null)
            return IntelAssignResult.NoAvailableSlot;

        if (packet.knowsSource && packet.knowsDestination)
            return IntelAssignResult.AlreadyComplete;

        ScanSlot existingSlot = FindTraceSlotForPacket(packet);
        if (existingSlot != null)
        {
            if (!preferredSlotIndex.HasValue || existingSlot.slotIndex == preferredSlotIndex.Value)
                return IntelAssignResult.AlreadyTracking;
        }

        if (preferredSlotIndex.HasValue)
        {
            int slotIndex = preferredSlotIndex.Value;

            if (!IsValidTraceSlotIndex(slotIndex))
                return IntelAssignResult.InvalidSlot;

            ScanSlot targetSlot = traceSlots[slotIndex];

            if (targetSlot.target == packet)
                return IntelAssignResult.AlreadyTracking;

            if (existingSlot != null)
                ClearSlotAndMaybeUnsubscribe(existingSlot);

            bool wasReplacing = !targetSlot.IsEmpty();
            if (wasReplacing)
                ClearSlotAndMaybeUnsubscribe(targetSlot);

            SubscribeToPacket(packet);
            targetSlot.Assign(packet, tickCounter);
            RefreshAllPacketTags();

            return wasReplacing ? IntelAssignResult.Replaced : IntelAssignResult.Started;
        }

        ScanSlot emptySlot = FindEmptySlot(traceSlots);
        if (emptySlot != null)
        {
            SubscribeToPacket(packet);
            emptySlot.Assign(packet, tickCounter);
            RefreshAllPacketTags();
            return IntelAssignResult.Started;
        }

        ScanSlot replacementSlot = FindOldestSlot(traceSlots);
        if (replacementSlot != null)
        {
            ClearSlotAndMaybeUnsubscribe(replacementSlot);

            SubscribeToPacket(packet);
            replacementSlot.Assign(packet, tickCounter);
            RefreshAllPacketTags();
            return IntelAssignResult.Replaced;
        }

        RefreshAllPacketTags();
        return IntelAssignResult.NoAvailableSlot;
    }

    private void RefreshAllPacketTags()
    {
        RefreshSlotTags(scanSlots);
        RefreshSlotTags(traceSlots);
    }

    private void RefreshSlotTags(List<ScanSlot> slotList)
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            ScanSlot slot = slotList[i];
            if (slot == null || slot.target == null)
                continue;

            slot.target.RefreshIntelPresentation(this, commandDirector);
        }
    }

    public void RemovePacket(PacketView packet)
    {
        if (packet == null)
            return;

        ScanSlot scanSlot = FindSlotForPacket(packet);
        if (scanSlot != null)
            ClearSlotAndMaybeUnsubscribe(scanSlot);

        ScanSlot traceSlot = FindTraceSlotForPacket(packet);
        if (traceSlot != null)
            ClearSlotAndMaybeUnsubscribe(traceSlot);

        RefreshAllPacketTags();
    }

    public int GetActiveScanCount()
    {
        return CountActiveSlots(scanSlots);
    }

    public int GetActiveTraceCount()
    {
        return CountActiveSlots(traceSlots);
    }

    private int CountActiveSlots(List<ScanSlot> slotList)
    {
        int count = 0;

        for (int i = 0; i < slotList.Count; i++)
        {
            if (!slotList[i].IsEmpty())
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

        for (int i = 0; i < scanSlots.Count; i++)
        {
            ScanSlot slot = scanSlots[i];
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
                ClearSlotAndMaybeUnsubscribe(slot);
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

                completionLinger.Add(new ActiveIntelCompletion
                {
                    mode = IntelSlotMode.Scan,
                    slotIndex = i,
                    slotLabel = slot.PacketTagText,
                    slotColor = slot.GetThemeColor(),
                    packetId = packet.packetId,
                    stage = packet.scanStage,
                    visibleClass = packet.GetVisibleClass(),
                    barText = bar,
                    percentText = "100%",
                    // secondaryText = GetKindLine(packet),
                    secondaryText = null,
                    lingerTicks = completionLingerTicks,
                    wasReplacementCandidate = willBeDropped
                });

                ClearSlotAndMaybeUnsubscribe(slot);
            }
        }

        RefreshAllPacketTags();
    }

    private void TickActiveTraces()
    {
        if (traceSlots.Count <= 0)
            return;

        for (int i = 0; i < traceSlots.Count; i++)
        {
            ScanSlot slot = traceSlots[i];
            if (slot.IsEmpty())
                continue;

            PacketView packet = slot.target;
            if (packet == null)
            {
                slot.Clear();
                continue;
            }

            if (packet.knowsSource && packet.knowsDestination)
            {
                ClearSlotAndMaybeUnsubscribe(slot);
                continue;
            }

            int ticksElapsed = tickCounter - slot.assignedTick;
            int ticksRemaining = Mathf.Max(0, baseTraceDurationTicks - ticksElapsed);

            if (ticksRemaining > 0)
                continue;

            bool willBeDropped = WouldTraceBeDropped(packet);

            packet.RevealSource();
            packet.RevealDestination();

            string bar = ScanBarFormatter.BuildOperationsScanBarOnly(
                2,
                1f,
                true,
                false,
                activeStageChar: '='
            );

            completionLinger.Add(new ActiveIntelCompletion
            {
                mode = IntelSlotMode.Trace,
                slotIndex = i,
                slotLabel = slot.PacketTagText,
                slotColor = slot.GetThemeColor(),
                packetId = packet.packetId,
                stage = ScanStage.Confirmed,
                visibleClass = packet.GetVisibleClass(),
                barText = bar,
                percentText = "100%",
                secondaryText = $"src={packet.revealedSource}  dest={packet.revealedDestination}",
                lingerTicks = completionLingerTicks,
                wasReplacementCandidate = willBeDropped
            });

            ClearSlotAndMaybeUnsubscribe(slot);
        }

        RefreshAllPacketTags();
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

    private bool WouldTraceBeDropped(PacketView packet)
    {
        ScanSlot candidate = FindOldestSlot(traceSlots);
        return candidate != null && candidate.target == packet;
    }

    public bool IsPacketActivelyScanned(PacketView packet)
    {
        if (packet == null)
            return false;

        return FindSlotForPacket(packet) != null;
    }

    private bool IsPacketTracked(PacketView packet)
    {
        if (packet == null)
            return false;

        return FindSlotForPacket(packet) != null || FindTraceSlotForPacket(packet) != null;
    }

    // Clear() - but also in case the packet is being traced/scanned simultaneously 
    // then check for unsubscribe 
    private void ClearSlotAndMaybeUnsubscribe(ScanSlot slot)
    {
        if (slot == null || slot.target == null)
            return;

        PacketView packet = slot.target;

        slot.Clear();
        packet.RefreshIntelPresentation(this, commandDirector);

        if (!IsPacketTracked(packet))
            UnsubscribeFromPacket(packet);
    }

    private ScanSlot FindEmptySlot(List<ScanSlot> slotList)
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            if (slotList[i].IsEmpty())
                return slotList[i];
        }

        return null;
    }

    public ScanSlot FindSlotForPacket(PacketView packet)
    {
        for (int i = 0; i < scanSlots.Count; i++)
        {
            if (scanSlots[i].Matches(packet))
                return scanSlots[i];
        }

        return null;
    }

    public ScanSlot FindTraceSlotForPacket(PacketView packet)
    {
        for (int i = 0; i < traceSlots.Count; i++)
        {
            if (traceSlots[i].Matches(packet))
                return traceSlots[i];
        }

        return null;
    }

    public ScanSlot FindAnySlotForPacket(PacketView packet)
    {
        return FindSlotForPacket(packet) ?? FindTraceSlotForPacket(packet);
    }

    private bool IsValidScanSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < scanSlots.Count;
    }

    private bool IsValidTraceSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < traceSlots.Count;
    }

    private ScanSlot GetReplacementCandidateSlot()
    {
        if (FindEmptySlot(scanSlots) != null)
            return null;

        return FindOldestSlot(scanSlots);
    }

    private ScanSlot FindOldestSlot(List<ScanSlot> slotList)
    {
        ScanSlot oldest = null;

        for (int i = 0; i < slotList.Count; i++)
        {
            ScanSlot slot = slotList[i];
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

    private string GetBestKnownIdentityShortLabel(PacketView packet)
    {
        if (packet == null)
            return "UNKNOWN";

        if (packet.knowsKind && packet.revealedKind != PacketKind.None)
            return packet.revealedKind.ToString().ToUpperInvariant();

        if (packet.knowsClass)
            return GetPacketClassShortLabel(packet.revealedClass);

        return GetVisibleClassShortLabel(packet);
    }

    private string GetBestKnownIdentityRichLabel(PacketView packet)
    {
        if (packet == null)
            return RichTextUtil.Colorize("UNKNOWN", logTheme.classUnknown, true);

        if (packet.knowsKind && packet.revealedKind != PacketKind.None)
        {
            return RichTextUtil.Colorize(
                packet.revealedKind.ToString().ToUpperInvariant(),
                logTheme.classThreat,
                true
            );
        }

        if (packet.knowsClass)
            return GetPacketClassRichLabel(packet.revealedClass);

        return GetVisibleClassRichLabel(packet);
    }

    private string GetBestKnownIdentityRichLabelPadded(PacketView packet, int width)
    {
        string label = GetBestKnownIdentityShortLabel(packet);
        return GetBestKnownIdentityRichLabelPadded(packet, label, width);
    }

    private string GetBestKnownIdentityRichLabelPadded(PacketView packet, string label, int width)
    {
        string paddedLabel = PadRightSafe(label, width);

        if (packet == null)
            return RichTextUtil.Colorize(paddedLabel, logTheme.classUnknown, true);

        if (packet.knowsKind && packet.revealedKind != PacketKind.None)
            return RichTextUtil.Colorize(paddedLabel, logTheme.classThreat, true);

        if (packet.knowsClass)
        {
            return packet.revealedClass switch
            {
                PacketClass.Benign   => RichTextUtil.Colorize(paddedLabel, logTheme.classBenign, true),
                PacketClass.Threat   => RichTextUtil.Colorize(paddedLabel, logTheme.classThreat, true),
                PacketClass.Priority => RichTextUtil.Colorize(paddedLabel, logTheme.classPriority, true),
                _ => paddedLabel
            };
        }

        return ColorizeVisibleClassPadded(packet.GetVisibleClass(), paddedLabel);
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

    private ScanPanelRowData BuildEmptyScanRow(int slotIndex)
    {
        ScanSlot slot = scanSlots[slotIndex];

        return new ScanPanelRowData
        {
            slotIndex = slotIndex,
            slotLabel = slot.PacketTagText,
            slotColor = slot.GetThemeColor(),
            state = ScanPanelRowState.EmptyScan,
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

    private ScanPanelRowData BuildScanLingerRow(ActiveIntelCompletion linger)
    {
        if (linger == null)
            return null;

        return new ScanPanelRowData
        {
            slotIndex = linger.slotIndex,
            slotLabel = linger.slotLabel,
            slotColor = linger.slotColor,
            state = ScanPanelRowState.CompletedScanLinger,
            packetId = linger.packetId,
            barText = linger.barText,
            percentText = linger.percentText,
            etaText = "--",
            stage = linger.stage,
            visibleClass = linger.visibleClass,
            showDone = true,
            willBeDropped = linger.wasReplacementCandidate,
            secondaryIntelLine = linger.secondaryText
        };
    }

    private ScanPanelRowData BuildActiveScanRow(int slotIndex, PacketView p, int activeCount)
    {
        bool willBeDropped = WouldBeDropped(p);
        ScanSlot slot = scanSlots[slotIndex];

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
            slotLabel = slot.PacketTagText,
            slotColor = slot.GetThemeColor(),
            state = ScanPanelRowState.ActiveScan,
            packetId = p.packetId,
            barText = bar,
            percentText = percentText,
            etaText = etaText,
            stage = p.scanStage,
            visibleClass = p.GetVisibleClass(),
            showDone = false,
            willBeDropped = willBeDropped,
            // secondaryIntelLine = GetKindLine(p)
            secondaryIntelLine = null
        };
    }

    private ScanPanelRowData BuildEmptyTraceRow(int slotIndex)
    {
        ScanSlot slot = traceSlots[slotIndex];

        return new ScanPanelRowData
        {
            slotIndex = slotIndex,
            slotLabel = slot.PacketTagText,
            slotColor = slot.GetThemeColor(),
            state = ScanPanelRowState.EmptyTrace,
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

    private ScanPanelRowData BuildActiveTraceRow(int slotIndex, PacketView p)
    {
        ScanSlot slot = traceSlots[slotIndex];
        int ticksElapsed = tickCounter - slot.assignedTick;
        float progress01 = Mathf.Clamp01((float)ticksElapsed / Mathf.Max(1, baseTraceDurationTicks));
        int pct = Mathf.RoundToInt(progress01 * 100f);
        int ticksRemaining = Mathf.Max(0, baseTraceDurationTicks - ticksElapsed);

        return new ScanPanelRowData
        {
            slotIndex = slotIndex,
            slotLabel = slot.PacketTagText,
            slotColor = slot.GetThemeColor(),
            state = ScanPanelRowState.ActiveTrace,
            packetId = p.packetId,
            barText = ScanBarFormatter.BuildOperationsScanBarOnly(
                Mathf.Clamp(Mathf.FloorToInt(progress01 * 3f), 0, 2),
                progress01,
                false,
                false,
                activeStageChar: '='
            ),
            percentText = $"{pct}%",
            etaText = ticksRemaining.ToString(),
            stage = ScanStage.Unknown,
            visibleClass = p.GetVisibleClass(),
            showDone = false,
            willBeDropped = false,
            secondaryIntelLine = "TRACE"
        };
    }

    private ScanPanelRowData BuildTraceLingerRow(ActiveIntelCompletion linger)
    {
        if (linger == null)
            return null;

        return new ScanPanelRowData
        {
            slotIndex = linger.slotIndex,
            slotLabel = linger.slotLabel,
            slotColor = linger.slotColor,
            state = ScanPanelRowState.CompletedTraceLinger,
            packetId = linger.packetId,
            barText = linger.barText,
            percentText = linger.percentText,
            etaText = "--",
            stage = linger.stage,
            visibleClass = linger.visibleClass,
            showDone = true,
            willBeDropped = linger.wasReplacementCandidate,
            secondaryIntelLine = linger.secondaryText
        };
    }
    
    private ScanPanelRowData BuildScanRowForSlot(int slotIndex, int activeCount)
    {
        ActiveIntelCompletion linger = completionLinger.Find(c => c.mode == IntelSlotMode.Scan && c.slotIndex == slotIndex);
        if (linger != null)
            return BuildScanLingerRow(linger);

        ScanSlot slot = scanSlots[slotIndex];
        if (slot == null || slot.IsEmpty() || slot.target == null)
            return BuildEmptyScanRow(slotIndex);

        return BuildActiveScanRow(slotIndex, slot.target, activeCount);
    }

    private ScanPanelRowData BuildTraceRowForSlot(int slotIndex)
    {
        ActiveIntelCompletion linger = completionLinger.Find(c => c.mode == IntelSlotMode.Trace && c.slotIndex == slotIndex);
        if (linger != null)
            return BuildTraceLingerRow(linger);

        ScanSlot slot = traceSlots[slotIndex];
        if (slot == null || slot.IsEmpty() || slot.target == null)
            return BuildEmptyTraceRow(slotIndex);

        return BuildActiveTraceRow(slotIndex, slot.target);
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
        string slotTag = FormatInlineSlotTag(row.slotLabel, row.slotColor);

        bool isTraceRow =
            row.state == ScanPanelRowState.EmptyTrace ||
            row.state == ScanPanelRowState.ActiveTrace ||
            row.state == ScanPanelRowState.CompletedTraceLinger;

        if (row.state == ScanPanelRowState.EmptyScan || row.state == ScanPanelRowState.EmptyTrace)
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

        if (isTraceRow)
        {
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

            sb.Append(RichTextUtil.Colorize(PadRightSafe("TRACE", 9), row.slotColor, true));
            sb.Append(" ");

            string classText = ColorizeVisibleClassPadded(
                row.visibleClass,
                PadRightSafe(GetVisibleClassShortLabel(row.visibleClass), 8)
            );
            sb.Append(classText);
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(row.secondaryIntelLine))
            {
                sb.Append("  ");
                sb.Append("    ");
                sb.Append(" ");
                sb.Append("    ");
                sb.Append(row.secondaryIntelLine.Trim());
                sb.AppendLine();
            }

            return;
        }

        string stageText = ColorizeStagePadded(
            row.stage,
            PadRightSafe(GetStageShortLabel(row.stage), 9)
        );

        PacketView scanPacket = null;

        if (row.state == ScanPanelRowState.ActiveScan)
        {
            ScanSlot slot = scanSlots[row.slotIndex];
            scanPacket = slot != null ? slot.target : null;
        }

        string classTextScan = scanPacket != null
            ? GetBestKnownIdentityRichLabelPadded(scanPacket, 8)
            : ColorizeVisibleClassPadded(
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
        sb.Append(classTextScan);
        sb.AppendLine();
    }

    public void AppendScanPanel(StringBuilder sb)
    {
        int activeScanCount = GetActiveScanCount();

        for (int i = 0; i < scanSlots.Count; i++)
        {
            ScanPanelRowData row = BuildScanRowForSlot(i, activeScanCount);
            if (row != null)
                AppendFormattedScanRow(sb, row);
        }

        for (int i = 0; i < traceSlots.Count; i++)
        {
            ScanPanelRowData row = BuildTraceRowForSlot(i);
            if (row != null)
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

        if (knownThreats == null || knownThreats.Count == 0)
        {
            sb.AppendLine(RichTextUtil.Colorize("none", logTheme.muted));
            return;
        }

        for (int i = 0; i < knownThreats.Count; i++)
        {
            PacketView packet = knownThreats[i];
            if (packet == null)
                continue;

            // header: packet id + scan stage + best known identity
            string stageLabel = GetStageRichLabel(packet.scanStage);
            string identityLabel = BuildKnownThreatIdentityLabel(packet);

            sb.AppendLine($"<b>{packet.packetId}</b>  {stageLabel}  {identityLabel}");

            // detail lines only if known
            string keywordLine = BuildKnownThreatKeywordLine(packet);
            if (!string.IsNullOrWhiteSpace(keywordLine))
                sb.AppendLine($"    {keywordLine}");

            string infectionLine = BuildKnownThreatInfectionLine(packet);
            if (!string.IsNullOrWhiteSpace(infectionLine))
                sb.AppendLine($"    {infectionLine}");

            string traceLine = BuildKnownThreatTraceLine(packet);
            if (!string.IsNullOrWhiteSpace(traceLine))
                sb.AppendLine($"    {traceLine}");

            string blockedLine = BuildKnownThreatBlockedLine(packet);
            if (!string.IsNullOrWhiteSpace(blockedLine))
                sb.AppendLine($"    {blockedLine}");

            if (i < knownThreats.Count - 1)
                sb.AppendLine();
        }
    }

    private string BuildKnownThreatIdentityLabel(PacketView packet)
    {
        return GetBestKnownIdentityRichLabel(packet);
    }

    private string BuildKnownThreatKeywordLine(PacketView packet)
    {
        if (packet == null)
            return null;

        var keywordIds = packet.GetRevealedKeywordIds();
        if (keywordIds == null || keywordIds.Count == 0)
            return null;

        return $"kw={string.Join(",", keywordIds)}";
    }

    private string BuildKnownThreatInfectionLine(PacketView packet)
    {
        if (packet == null || !packet.knowsInfectionType || packet.revealedInfectionType == InfectionType.None)
            return null;

        return $"inf={packet.revealedInfectionType.ToString().ToLowerInvariant()}";
    }

    private string BuildKnownThreatTraceLine(PacketView packet)
    {
        if (packet == null)
            return null;

        bool knowsSrc = packet.knowsSource && !string.IsNullOrWhiteSpace(packet.revealedSource);
        bool knowsDest = packet.knowsDestination && !string.IsNullOrWhiteSpace(packet.revealedDestination);

        if (!knowsSrc && !knowsDest)
            return null;

        if (knowsSrc && knowsDest)
            return $"src={packet.revealedSource}  dest={packet.revealedDestination}";

        if (knowsSrc)
            return $"src={packet.revealedSource}";

        return $"dest={packet.revealedDestination}";
    }

    private string BuildKnownThreatBlockedLine(PacketView packet)
    {
        if (packet == null || commandDirector == null)
            return null;

        BlockOperation armedBlock = commandDirector.FindArmedBlockForPacket(packet);
        if (armedBlock == null)
            return null;

        if (armedBlock.nodeId != null && !string.IsNullOrWhiteSpace(armedBlock.nodeId))
            return $"blocked @ {armedBlock.nodeId}";

        return "blocked";
    }

    private enum ScanPanelRowState
    {
        EmptyScan,
        ActiveScan,
        CompletedScanLinger,
        EmptyTrace,
        ActiveTrace,
        CompletedTraceLinger
    }

    private class ScanPanelRowData
    {
        public int slotIndex;
        public string slotLabel;
        public Color slotColor;
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

    private string FormatInlineSlotTag(string slotLabel, Color slotColor)
    {
        return RichTextUtil.Colorize($"[{slotLabel}]", slotColor, true);
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