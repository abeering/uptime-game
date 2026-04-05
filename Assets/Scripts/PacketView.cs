using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public enum PacketClass
{
    Benign,
    Threat,
    Priority
}

public enum PacketKind
{
    None,

    // Threats
    Virus,
    Worm,
    Spyware,
    Ddos,

    // Priority
    Auth,
    Control,
    FileTransfer
}

public enum IntelLevel
{
    None,
    Scanned,
    DeepScanned
}

public enum VisibleClass
{
    Unknown,
    Benign,
    Threat,
    Priority
}

public enum ScanStage
{
    Unknown = 0,
    Probable = 1,
    Likely = 2,
    Confirmed = 3
}

public enum IntelRevealType
{
    Kind,
    InfectionType,
    Keyword
}

public class PacketView : MonoBehaviour
{
    public string packetId = "a1";
    public string PacketId => packetId;
    public string visiblePacketId = "a1";

    [Header("Packet Behavior")]
    [Min(1)]
    public int baseSpeed = 1;
    public int boostCount = 0;

    [Header("Labels")]
    public TMPro.TextMeshPro label;
    public TMPro.TextMeshPro scanTagLabel;

    [Header("Debug State")]
    public int routeIndex = 0;
    public int currentStep = 0;
    public int ticksUntilAdvance = 0;
    public int nodesReachedCount = 0;
    // public bool movingAToB = true; NECESSARY?
    public bool hasArrived = false;
    public bool isRemoved { get; private set; }

    [HideInInspector] public RouteStep[] route;

    [Header("Packet Type")]
    public PacketClass trueClass;
    public PacketKind trueKind;
    public List<InfectionPayload> infections = new();
    public string sourceAddress;

    [Header("Intel")]
    public IntelLevel intelLevel = IntelLevel.None;
    [Range(0, 100)] public int confidencePercent = 0;
    [Range(0, 100)] public int scanDifficulty = 25;
    public PacketClass reportedClass = PacketClass.Benign;

    [Header("Revealed Intel")]
    public bool knowsKind = false;
    public PacketKind revealedKind = PacketKind.None;
    public bool knowsInfectionType = false;
    public InfectionType revealedInfectionType = InfectionType.None;
    [Min(0)] public int revealedKeywordCount = 0;

    [Header("Progressive Scan")]
    public Color scanBorderColor = Color.green;
    public ScanStage scanStage = ScanStage.Unknown;
    [Min(0)] public int scanTicksIntoStage = 0;
    public bool isActivelyScanned = false;

    [Header("Visuals")]
    public Color unknownColor = Color.gray;
    public Color benignColor = Color.green;
    public Color threatColor = Color.red;
    public Color priorityColor = Color.cyan;
    public PacketScanVisual scanVisual;
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer borderRenderer;

    [Header("Visual Lanes")]
    [SerializeField] private int visualLaneIndex = 0;
    [SerializeField] private ConnectionView visualLaneConnection;
    [SerializeField, Range(0f, 0.45f)] private float laneConvergeStartT = 0.14f;
    [SerializeField, Range(0.55f, 1f)] private float laneConvergeEndT = 0.86f;

    public event Action<PacketView, NodeView> OnReachedNode;
    public event Action<PacketView, string> OnRemoved;
    public event Action<PacketView> OnRouteCompleted;
    public event Action<PacketView, ScanStage, ScanStage> OnScanStageChanged;
    public event Action<PacketView, IntelRevealType, string> OnIntelRevealed;

    // keywords 
    public List<IPacketKeyword> keywords = new();

    public void Initialize(
        string newPacketId,
        PacketClass newClass,
        PacketKind newKind,
        string newSourceAddress,
        int newBaseSpeed,
        int newScanDifficulty,
        RouteStep[] newRoute,
        bool startsQuickScanned = false,
        List<InfectionPayload> newInfections = null
    )
    {
        packetId = newPacketId;
        visiblePacketId = newPacketId;

        trueClass = newClass;
        trueKind = newKind;
        sourceAddress = newSourceAddress;
        infections = newInfections != null
            ? new List<InfectionPayload>(newInfections)
            : new List<InfectionPayload>();
        scanDifficulty = Mathf.Clamp(newScanDifficulty, 0, 100);

        reportedClass = PacketClass.Benign;
        confidencePercent = 0;
        intelLevel = IntelLevel.None;
        ResetRevealedIntel();

        scanStage = ScanStage.Unknown;
        scanTicksIntoStage = 0;
        isActivelyScanned = false;

        baseSpeed = Mathf.Max(1, newBaseSpeed);
        boostCount = 0;
        route = newRoute;

        HideScanTag();
        RefreshVisuals();

        if (label != null)
            label.text = visiblePacketId;

        routeIndex = 0;
        currentStep = 0;
        ticksUntilAdvance = 0;
        hasArrived = false;

        visualLaneIndex = 0;
        visualLaneConnection = null;
        RefreshVisualLaneAssignment();
        SnapToCurrentPosition();
        ResetAdvanceTimer();

        if (startsQuickScanned)
            SetInitialScanState(ScanStage.Probable, RollReportedClass());
        else
            ResetProgressiveScanState();

        // debug output 
        if (infections != null && infections.Count > 0)
        {
            for (int i = 0; i < infections.Count; i++)
            {
                Debug.Log($"[Packet][{packetId}] attached payload {i}: {infections[i]}");
            }
        }
    }

    public bool CanAdvanceScanStage()
    {
        return scanStage < ScanStage.Confirmed;
    }

    public float GetScanProgressPerTick(int baseScanDurationTicks)
    {
        if (baseScanDurationTicks <= 0)
            return 1f;

        float difficultyMultiplier = GetScanDifficultyMultiplier();
        float effectiveDurationTicks = baseScanDurationTicks * difficultyMultiplier;

        if (effectiveDurationTicks <= 0f)
            return 1f;

        return 1f / effectiveDurationTicks;
    }

    public float GetScanDifficultyMultiplier()
    {
        float difficulty01 = Mathf.Clamp01(scanDifficulty / 100f);

        // First-pass mapping:
        // easy packets scan faster than baseline
        // hard packets scan slower than baseline
        //
        // difficulty 0   -> 0.65x duration
        // difficulty 25  -> 0.8625x duration
        // difficulty 50  -> 1.075x duration
        // difficulty 75  -> 1.2875x duration
        // difficulty 100 -> 1.5x duration
        return Mathf.Lerp(0.65f, 1.5f, difficulty01);
    }

    public void AddScanProgress(float progress01)
    {
        if (progress01 <= 0f)
            return;

        if (scanStage == ScanStage.Confirmed)
            return;

        float oldConfidence01 = GetScanConfidence01();
        ScanStage oldStage = scanStage;

        float newConfidence01 = Mathf.Clamp01(oldConfidence01 + progress01);
        confidencePercent = Mathf.RoundToInt(newConfidence01 * 100f);

        RecomputeScanStageFromConfidence();

        if (scanStage != oldStage)
        {
            ApplyScanStageEffects(oldStage, scanStage);
            OnScanStageChanged?.Invoke(this, oldStage, scanStage);
        }

        RefreshVisuals();
    }

    private void RecomputeScanStageFromConfidence()
    {
        float confidence01 = GetScanConfidence01();

        // Phase 1 thresholds:
        // simple on purpose so we can invert the architecture cleanly first.
        if (confidence01 >= 1f)
            scanStage = ScanStage.Confirmed;
        else if (confidence01 >= 0.55f)
            scanStage = ScanStage.Likely;
        else if (confidence01 >= 0.20f)
            scanStage = ScanStage.Probable;
        else
            scanStage = ScanStage.Unknown;
    }

    private void ApplyScanStageEffects(ScanStage oldStage, ScanStage newStage)
    {
        switch (newStage)
        {
            case ScanStage.Unknown:
                intelLevel = IntelLevel.None;
                reportedClass = PacketClass.Benign;
                break;

            case ScanStage.Probable:
                if (oldStage < ScanStage.Probable)
                    reportedClass = RollReportedClass();

                if (intelLevel < IntelLevel.Scanned)
                    intelLevel = IntelLevel.Scanned;

                break;

            case ScanStage.Likely:
                if (intelLevel < IntelLevel.Scanned)
                    intelLevel = IntelLevel.Scanned;

                RevealKind();
                break;

            case ScanStage.Confirmed:
                reportedClass = trueClass;
                confidencePercent = 100;
                intelLevel = IntelLevel.DeepScanned;

                RevealKind();
                RevealInfectionType();
                RevealAllKeywords();
                break;
        }
    }

    public void ResetProgressiveScanState()
    {
        scanStage = ScanStage.Unknown;
        scanTicksIntoStage = 0;
        isActivelyScanned = false;

        reportedClass = PacketClass.Benign;
        confidencePercent = 0;
        intelLevel = IntelLevel.None;

        ResetRevealedIntel();

        RefreshVisuals();
        HideScanTag();
    }

    private void ResetRevealedIntel()
    {
        knowsKind = false;
        revealedKind = PacketKind.None;
        knowsInfectionType = false;
        revealedInfectionType = InfectionType.None;
        revealedKeywordCount = 0;
    }

    public float GetScanStageProgress01()
    {
        float confidence01 = GetScanConfidence01();

        return scanStage switch
        {
            ScanStage.Unknown   => Mathf.InverseLerp(0.00f, 0.20f, confidence01),
            ScanStage.Probable  => Mathf.InverseLerp(0.20f, 0.55f, confidence01),
            ScanStage.Likely    => Mathf.InverseLerp(0.55f, 1.00f, confidence01),
            ScanStage.Confirmed => 1f,
            _ => 0f
        };
    }

    public void SetActivelyScanned(bool value)
    {
        isActivelyScanned = value;
        RefreshVisuals();
    }

    public bool IsScanComplete()
    {
        return scanStage == ScanStage.Confirmed;
    }

    public int GetScanDisplayStageIndex()
    {
        // Unknown = 0 → first stage active
        // Probable = 1 → second stage active
        // Likely = 2 → third stage active
        return Mathf.Clamp((int)scanStage, 0, 2);
    }

    public void SetInitialScanState(ScanStage stage, PacketClass initialReportedClass, int ticksIntoStage = 0)
    {
        scanStage = stage;
        isActivelyScanned = false;
        reportedClass = PacketClass.Benign;
        confidencePercent = 0;
        intelLevel = IntelLevel.None;
        ResetRevealedIntel();

        switch (stage)
        {
            case ScanStage.Unknown:
                reportedClass = PacketClass.Benign;
                confidencePercent = 0;
                intelLevel = IntelLevel.None;
                break;

            case ScanStage.Probable:
                reportedClass = initialReportedClass;
                confidencePercent = 20;
                intelLevel = IntelLevel.Scanned;
                break;

            case ScanStage.Likely:
                reportedClass = initialReportedClass;
                confidencePercent = 55;
                intelLevel = IntelLevel.Scanned;
                break;

            case ScanStage.Confirmed:
                reportedClass = trueClass;
                confidencePercent = 100;
                intelLevel = IntelLevel.DeepScanned;
                break;
        }

        scanTicksIntoStage = 0;
        RefreshVisuals();
    }

    public void HideScanTag()
    {
        if (scanTagLabel == null)
            return;

        scanTagLabel.text = "";
        scanTagLabel.gameObject.SetActive(false);
        ToggleScanBorder(false);
    }

    public void ToggleScanBorder(bool show)
    {
        if (borderRenderer == null)
            return;
            
        if(show){
            borderRenderer.color = scanBorderColor;
        } else {
            borderRenderer.color = Color.black;
        }
    }

    // TODO temporary RefreshScanTag until crosshair project - hide scan tag, show border 
    public void RefreshScanTag(ScanDirector scanDirector)
    {
        if (scanTagLabel != null)
        {
            scanTagLabel.text = "";
            scanTagLabel.gameObject.SetActive(false);
        }
    }

    // TODO - bring back when we do crosshair scan 
    // public void RefreshScanTag(ScanDirector scanDirector)
    // {
    //     if (scanTagLabel == null || scanDirector == null)
    //         return;

    //     if (!scanDirector.IsPacketActivelyScanned(this))
    //     {
    //         HideScanTag();
    //         return;
    //     }

    //     scanTagLabel.gameObject.SetActive(true);
    //     ToggleScanBorder(true);

    //     bool blinkOn = Mathf.FloorToInt(Time.time * 4f) % 2 == 0;
    //     // TODO - tie blinks to ticks 
    //     // char activeStageChar = blinkOn ? '▣' : '□';
    //     char activeStageChar = '=';

    //     bool willBeDropped = scanDirector.WouldBeDropped(this);

    //     scanTagLabel.text = ScanBarFormatter.BuildWorldScanTag(
    //         GetScanDisplayStageIndex(),
    //         GetScanConfidence01(),
    //         IsScanComplete(),
    //         willBeDropped,
    //         activeStageChar: activeStageChar
    //     );
    // }

    public float GetScanConfidence01()
    {
        return Mathf.Clamp01(confidencePercent / 100f);
    }

    public float GetCurrentStageStartConfidence01()
    {
        return scanStage switch
        {
            ScanStage.Unknown => 0.00f,
            ScanStage.Probable => 0.20f,
            ScanStage.Likely => 0.55f,
            ScanStage.Confirmed => 1.00f,
            _ => 0.00f
        };
    }

    public float GetCurrentStageEndConfidence01()
    {
        return scanStage switch
        {
            ScanStage.Unknown => 0.20f,
            ScanStage.Probable => 0.55f,
            ScanStage.Likely => 1.00f,
            ScanStage.Confirmed => 1.00f,
            _ => 1.00f
        };
    }

    private void RevealKind()
    {
        if (knowsKind)
            return;

        if (trueKind == PacketKind.None)
            return;

        knowsKind = true;
        revealedKind = trueKind;
        OnIntelRevealed?.Invoke(this, IntelRevealType.Kind, revealedKind.ToString());
    }

    private void RevealInfectionType()
    {
        if (knowsInfectionType)
            return;

        InfectionType infectionType = GetPrimaryInfectionType();

        if (infectionType == InfectionType.None)
            return;

        knowsInfectionType = true;
        revealedInfectionType = infectionType;
        OnIntelRevealed?.Invoke(this, IntelRevealType.InfectionType, revealedInfectionType.ToString());
    }

    private void RevealAllKeywords()
    {
        while (revealedKeywordCount < keywords.Count)
            RevealNextKeyword();
    }

    private void RevealNextKeyword()
    {
        if (revealedKeywordCount < 0 || revealedKeywordCount >= keywords.Count)
            return;

        IPacketKeyword keyword = keywords[revealedKeywordCount];
        revealedKeywordCount++;

        string keywordName = GetKeywordDisplayName(keyword);
        OnIntelRevealed?.Invoke(this, IntelRevealType.Keyword, keywordName);
    }

    private string GetKeywordDisplayName(IPacketKeyword keyword)
    {
        if (keyword == null)
            return "Unknown";

        if (!string.IsNullOrWhiteSpace(keyword.DisplayName))
            return keyword.DisplayName;

        if (!string.IsNullOrWhiteSpace(keyword.KeywordId))
            return keyword.KeywordId;

        return "Unknown";
    }

    public bool HasKeyword<T>() where T : IPacketKeyword
    {
        return keywords.Any(k => k is T);
    }

    public bool IsPriority()
    {
        return trueClass == PacketClass.Priority;
    }

    public bool IsVisiblePriority()
    {
        return GetVisibleClass() == VisibleClass.Priority;
    }

    public bool IsTrueThreat()
    {
        return trueClass == PacketClass.Threat;
    }

    public bool IsVisibleThreat()
    {
        return GetVisibleClass() == VisibleClass.Threat;
    }

    public bool IsKnownThreat()
    {
        return GetVisibleClass() == VisibleClass.Threat;
    }

    public bool HasInfections()
    {
        return infections != null && infections.Count > 0;
    }

    public InfectionPayload GetPrimaryInfectionPayload()
    {
        if (!HasInfections())
            return null;

        return infections[0];
    }

    public InfectionType GetPrimaryInfectionType()
    {
        InfectionPayload payload = GetPrimaryInfectionPayload();
        return payload != null ? payload.type : InfectionType.None;
    }

    public VisibleClass GetVisibleClass()
    {
        if (confidencePercent <= 0)
            return VisibleClass.Unknown;

        return reportedClass switch
        {
            PacketClass.Benign => VisibleClass.Benign,
            PacketClass.Threat => VisibleClass.Threat,
            PacketClass.Priority => VisibleClass.Priority,
            _ => VisibleClass.Unknown
        };
    }

    public List<string> GetRevealedKeywordIds()
    {
        var ids = new List<string>();

        if (keywords == null || keywords.Count == 0 || revealedKeywordCount <= 0)
            return ids;

        int count = Mathf.Clamp(revealedKeywordCount, 0, keywords.Count);

        for (int i = 0; i < count; i++)
        {
            if (keywords[i] == null || string.IsNullOrWhiteSpace(keywords[i].KeywordId))
                continue;

            ids.Add(keywords[i].KeywordId);
        }

        return ids;
    }

    private void ApplyVisuals()
    {
        VisibleClass visible = GetVisibleClass();

        Color identityColor = visible switch
        {
            VisibleClass.Unknown => unknownColor,
            VisibleClass.Benign => benignColor,
            VisibleClass.Threat => threatColor,
            VisibleClass.Priority => priorityColor,
            _ => Color.white
        };

        float confidence01 = Mathf.Clamp01(confidencePercent / 100f);

        Color bodyColor = visible == VisibleClass.Unknown
            ? Color.gray
            : Color.Lerp(Color.gray, identityColor, confidence01);

        if (spriteRenderer != null)
            spriteRenderer.color = bodyColor;
    }

    private void RefreshVisuals()
    {
        ApplyVisuals();
        RefreshScanVisual();
    }

    private void RefreshScanVisual()
    {
        if (scanVisual == null)
            return;

        if (!isActivelyScanned)
        {
            scanVisual.HideScanVisual();
            return;
        }

        if (scanStage == ScanStage.Confirmed)
        {
            scanVisual.HideScanVisual();
            return;
        }

        ScanStage visualStage = scanStage == ScanStage.Unknown
            ? ScanStage.Probable
            : scanStage;

        // scanVisual.ShowProgressiveScan(
        //     visualStage,
        //     GetScanStageProgress01()
        // );
    }

    public bool TryBoost()
    {
        if (!IsPriority())
            return false;

        if (baseSpeed <= 1)
            return false;

        baseSpeed = Mathf.Max(1, baseSpeed - 1);
        boostCount++;
        ResetAdvanceTimer();
        return true;
    }

    private PacketClass RollReportedClass()
    {
        float difficulty01 = Mathf.Clamp01(scanDifficulty / 100f);

        float correctChance = Mathf.Lerp(0.95f, 0.60f, difficulty01);
        bool isCorrect = UnityEngine.Random.value < correctChance;

        if (isCorrect)
            return trueClass;

        return trueClass switch
        {
            PacketClass.Benign => UnityEngine.Random.value < 0.85f ? PacketClass.Threat : PacketClass.Priority,
            PacketClass.Threat => UnityEngine.Random.value < 0.90f ? PacketClass.Benign : PacketClass.Priority,
            PacketClass.Priority => UnityEngine.Random.value < 0.50f ? PacketClass.Benign : PacketClass.Threat,
            _ => trueClass
        };
    }

    public void Tick(KeywordContext context)
    {
        if (hasArrived || route == null || route.Length == 0)
            return;

        foreach (var keyword in keywords)
        {
            keyword.OnTick(this, context);
        }

        ticksUntilAdvance--;

        if (ticksUntilAdvance > 0)
            return;

        AdvanceOneStep();
    }

    public virtual BlockResolution HandleBlocked(NodeView node)
    {
        return BlockResolution.Remove("blocked", "blocked");
    }

    private void AdvanceOneStep()
    {
        ConnectionView edge = GetCurrentConnection();
        if (edge == null)
        {
            hasArrived = true;
            return;
        }

        currentStep++;

        if (currentStep > edge.lengthSteps)
        {
            NodeView reachedNode = GetCurrentDestinationNode();

            if (reachedNode != null)
            {
                nodesReachedCount++;
                OnReachedNode?.Invoke(this, reachedNode);

                if (isRemoved)
                    return;

                Debug.Log($"[Runtime] reached node {reachedNode.nodeId}");
            }

            routeIndex++;

            if (routeIndex >= route.Length)
            {
                hasArrived = true;
                currentStep = edge.lengthSteps;
                SnapToCurrentPosition();
                OnRouteCompleted?.Invoke(this);
                return;
            }

            currentStep = 0;
            edge = GetCurrentConnection();
            RefreshVisualLaneAssignment();
        }

        SnapToCurrentPosition();
        ResetAdvanceTimer();
    }

    public void AdvanceMultipleSteps(int stepCount)
    {
        if (stepCount <= 0 || hasArrived || isRemoved)
            return;

        for (int i = 0; i < stepCount; i++)
        {
            AdvanceOneStep();

            if (hasArrived || isRemoved)
                return;
        }
    }

    public void ForceAdvanceTimer(int ticks)
    {
        ticksUntilAdvance = Mathf.Max(1, ticks);
    }

    private void ResetAdvanceTimer()
    {
        ConnectionView edge = GetCurrentConnection();

        if (edge == null)
        {
            ticksUntilAdvance = 0;
            return;
        }

        ticksUntilAdvance = Mathf.Max(1, baseSpeed * edge.EffectiveLatency);
    }

    public void SnapToCurrentPosition()
    {
        ConnectionView edge = GetCurrentConnection();
        if (edge == null)
            return;

        RefreshVisualLaneAssignment();
        transform.position = GetCurrentVisualPosition();
    }

    public RouteStep GetCurrentRouteStep()
    {
        if (route == null || routeIndex < 0 || routeIndex >= route.Length)
            return null;

        return route[routeIndex];
    }

    public ConnectionView GetCurrentConnection()
    {
        RouteStep step = GetCurrentRouteStep();
        return step != null ? step.connection : null;
    }

    public bool IsMovingAToB()
    {
        RouteStep step = GetCurrentRouteStep();
        return step != null && step.aToB;
    }

    public string GetDebugStatus()
    {
        ConnectionView edge = GetCurrentConnection();
        if (edge == null)
            return $"{packetId}: arrived";

        return $"{packetId}: {edge.connectionId} step {currentStep}/{edge.lengthSteps}, next move in {ticksUntilAdvance}";
    }

    public int GetVisualLaneIndex()
    {
        return visualLaneIndex;
    }

    private void RefreshVisualLaneAssignment()
    {
        ConnectionView currentConnection = GetCurrentConnection();

        if (currentConnection == null)
        {
            visualLaneConnection = null;
            visualLaneIndex = 0;
            return;
        }

        if (visualLaneConnection == currentConnection)
            return;

        visualLaneConnection = currentConnection;

        if (PacketLaneCoordinator.Instance != null)
            visualLaneIndex = PacketLaneCoordinator.Instance.AssignLaneForEdge(this, currentConnection);
        else
            visualLaneIndex = 0;
    }

    private float GetCurrentEdgeProgress01()
    {
        ConnectionView edge = GetCurrentConnection();
        if (edge == null || edge.lengthSteps <= 0)
            return 0f;

        return Mathf.Clamp01((float)currentStep / edge.lengthSteps);
    }

    private float GetLaneConvergenceWeight()
    {
        float t = GetCurrentEdgeProgress01();

        if (laneConvergeStartT >= laneConvergeEndT)
            return 1f;

        if (t <= laneConvergeStartT)
        {
            return Mathf.InverseLerp(0f, laneConvergeStartT, t);
        }

        if (t >= laneConvergeEndT)
        {
            return 1f - Mathf.InverseLerp(laneConvergeEndT, 1f, t);
        }

        return 1f;
    }

    private Vector3 GetCurrentVisualPosition()
    {
        ConnectionView edge = GetCurrentConnection();
        if (edge == null)
            return transform.position;

        Vector3 basePosition = edge.GetWorldPositionAtStep(currentStep, IsMovingAToB());

        if (PacketLaneCoordinator.Instance == null)
            return basePosition;

        Vector3 laneNormal = edge.GetLaneNormal();
        float laneOffset = PacketLaneCoordinator.Instance.GetLaneOffsetWorld(visualLaneIndex);
        float convergenceWeight = GetLaneConvergenceWeight();

        return basePosition + (laneNormal * laneOffset * convergenceWeight);
    }

    public NodeView GetDestination()
    {
        if (route == null || route.Length == 0)
            return null;

        RouteStep lastStep = route[route.Length - 1];

        return lastStep.aToB
            ? lastStep.connection.nodeB
            : lastStep.connection.nodeA;
    }

    public string GetDestinationName()
    {
        NodeView node = GetDestination();
        return node != null ? node.name : "unknown";
    }

    public NodeView GetCurrentDestinationNode()
    {
        RouteStep step = GetCurrentRouteStep();
        if (step == null || step.connection == null)
            return null;

        return step.aToB ? step.connection.nodeB : step.connection.nodeA;
    }

    public void NotifyRemoved(string reason)
    {
        if (isRemoved)
            return;

        isRemoved = true;
        OnRemoved?.Invoke(this, reason);
    }

    public string GetOperationsLine()
    {
        if (confidencePercent <= 0)
            return $"{packetId} - Unknown - src={sourceAddress} dest={GetDestinationName()}";

        if (intelLevel < IntelLevel.DeepScanned)
            return $"{packetId} - {reportedClass} ({confidencePercent}%) - src={sourceAddress} dest={GetDestinationName()}";

        return $"{packetId} - {trueClass}/{trueKind} ({confidencePercent}%) - src={sourceAddress} dest={GetDestinationName()}";
    }

    public string BuildOperationsIntelSummary()
    {
        List<string> parts = new();

        if (knowsKind && revealedKind != PacketKind.None)
            parts.Add($"kind={revealedKind}");

        if (knowsInfectionType && revealedInfectionType != InfectionType.None)
            parts.Add($"infection={revealedInfectionType}");

        if (revealedKeywordCount > 0)
            parts.Add($"keywords={BuildRevealedKeywordSummary()}");

        return string.Join("  ", parts);
    }

    public string BuildRevealedKeywordSummary()
    {
        if (keywords == null || keywords.Count == 0 || revealedKeywordCount <= 0)
            return "none";

        int count = Mathf.Clamp(revealedKeywordCount, 0, keywords.Count);
        var names = new List<string>(count);

        for (int i = 0; i < count; i++)
            names.Add(GetKeywordDisplayName(keywords[i]));

        return "[" + string.Join(", ", names) + "]";
    }

    public void SetVisiblePacketId(string newVisiblePacketId)
    {
        visiblePacketId = newVisiblePacketId;

        if (label != null)
            label.text = visiblePacketId;
    }

    public void SetVisualSortOrder(int order)
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = order;

        if (label != null)
            label.sortingOrder = order;

        if (borderRenderer != null)
            borderRenderer.sortingOrder = order - 1;
    }

}