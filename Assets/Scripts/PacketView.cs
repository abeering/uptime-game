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
    Class,
    Kind,
    InfectionType,
    Keyword,
    Source,
    Destination
}

public class PacketView : MonoBehaviour
{
    private struct PacketTagData
    {
        public string text;
        public Color backgroundColor;
        public Color textColor;

        public PacketTagData(string text, Color backgroundColor, Color textColor)
        {
            this.text = text;
            this.backgroundColor = backgroundColor;
            this.textColor = textColor;
        }
    }

    public string packetId = "a1";
    public string PacketId => packetId;
    public string visiblePacketId = "a1";

    [Header("Packet Behavior")]
    [Min(1)]
    public int baseSpeed = 1;
    public int boostCount = 0;
    [NonSerialized] public int auraBaseSpeedModifier = 0;

    [Header("Labels")]
    public TMPro.TextMeshPro label;

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
    public bool knowsClass = false;
    public PacketClass revealedClass = PacketClass.Benign;

    public bool knowsKind = false;
    public PacketKind revealedKind = PacketKind.None;

    public bool knowsInfectionType = false;
    public InfectionType revealedInfectionType = InfectionType.None;

    public bool knowsSource = false;
    public string revealedSource = null;

    public bool knowsDestination = false;
    public string revealedDestination = null;

    [Min(0)] public int revealedKeywordCount = 0;

    [Header("Progressive Scan")]
    public Color scanBorderColor = Color.green;
    public ScanStage scanStage = ScanStage.Unknown;
    [Min(0)] public int scanTicksIntoStage = 0;
    public bool isActivelyScanned = false;
    public bool isActivelyTraced = false;

    [Header("Body Colors")]
    public Color unknownColor = Color.gray;
    public Color benignColor = Color.green;
    public Color threatColor = Color.red;
    public Color priorityColor = Color.cyan;

    [Header("Visual References")]
    public PacketScanVisual scanVisual;
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer borderRenderer;
    [SerializeField] private SpriteRenderer gapFillRenderer;
    [SerializeField] private SpriteRenderer scanPulseRenderer;
    [SerializeField] private float scanPulseSpeed = 5f;
    [SerializeField] private float scanPulseMinAlpha = 0.35f;
    [SerializeField] private float scanPulseMaxAlpha = 1f;
    [SerializeField] private float scanPulseScaleMin = 1.05f;
    [SerializeField] private float scanPulseScaleMax = 1.28f;
    private Vector3 borderBaseScale = Vector3.one;
    private Vector3 pulseBaseScale = Vector3.one;

    [Header("Border Colors")]
    [SerializeField] private Color borderUnknownColor = new(0.90f, 0.92f, 0.94f, 1f);
    [SerializeField] private Color borderBenignColor = new(0.45f, 0.95f, 0.45f, 1f);
    [SerializeField] private Color borderThreatColor = new(1.00f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color borderPriorityColor = new(0.40f, 0.80f, 1.00f, 1f);

    [Header("Gap Visuals")]
    [SerializeField] private Color gapFillColor = new(0.18f, 0.19f, 0.21f, 1f);
    [SerializeField] private SpriteMask scanMask;

    [Header("Scan Tag Visuals")]
    [SerializeField] private float scanTagBackgroundAlpha = 0.92f;

    [Header("Block Tag Visuals")]
    [SerializeField] private float blockTagBackgroundAlpha = 0.92f;

    [Header("Tag Rail")]
    [SerializeField] private Transform tagAnchor;
    [SerializeField] private GameObject packetTagPrefab;
    [SerializeField] private Vector3 tagStartOffset = Vector3.zero;
    [SerializeField] private float tagSpacing = 0.42f;
    [SerializeField] private Color defaultTagTextColor = new(0.08f, 0.08f, 0.08f, 1f);
    private readonly List<PacketTagView> activeTagViews = new();

    [Header("Visual Lanes")]
    [SerializeField] private int visualLaneIndex = 0;
    [SerializeField] private ConnectionView visualLaneConnection;
    [SerializeField, Range(0f, 0.45f)] private float laneConvergeStartT = 0.14f;
    [SerializeField, Range(0.55f, 1f)] private float laneConvergeEndT = 0.86f;

    [Header("Step Easing")]
    [SerializeField] private bool enableStepEasing = true;
    [SerializeField, Range(0.05f, 0.75f)] private float stepEaseFraction = 0.35f;
    [SerializeField] private float minStepEaseDuration = 0.04f;

    private Vector3 visualStepFromPosition;
    private Vector3 visualStepToPosition;
    private float visualStepEaseElapsed = 0f;
    private float visualStepEaseDuration = 0f;
    private bool isStepEasing = false;

    [Header("Speed Tail")]
    [SerializeField] private bool enableSpeedTail = true;
    [SerializeField] private float tailMinLength = 0.06f;
    [SerializeField] private float tailMaxLength = 0.32f;
    [SerializeField] private float tailWidth = 0.045f;
    [SerializeField] private float tailAlpha = 0.28f;
    [SerializeField] private float tailRearOffset = 0.15f;
    [SerializeField] private float tailDirectionLerpSpeed = 12f;
    private Vector3 smoothedTailDirection = Vector3.right;
    private bool hasTailDirection = false;
    private Vector3 targetTailDirection = Vector3.right;
    private bool hasTargetTailDirection = false;
    // growing + pulsing 
    [SerializeField] private float tailGrowPerTick = 0.05f;
    [SerializeField] private int tailResolution = 8;
    [SerializeField] private bool enableTailPulse = true;
    [SerializeField] private float tailPulseTravelDuration = 0.22f;
    [SerializeField] private float tailPulseWidth01 = 0.18f;
    [SerializeField] private float tailPulseStrength = 0.32f;
    private float currentTailLength = 0f;
    private float tailPulseElapsed = 999f;
    private bool tailPulseActive = false;
    [SerializeField] private bool scaleTailPulseByPacketSpeed = true;
    [SerializeField] private float tailPulseDurationMin = 0.07f;
    [SerializeField] private float tailPulseDurationMax = 0.28f;
    [SerializeField] private float tailPulseDurationFractionOfMove = 0.45f;

    private LineRenderer speedTail;

    public event Action<PacketView, NodeView> OnReachedNode;
    public event Action<PacketView, PacketRemovalReason> OnRemoved;
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
        auraBaseSpeedModifier = 0;

        // reset speed tail 
        currentTailLength = 0f;
        tailPulseElapsed = tailPulseTravelDuration;
        tailPulseActive = false;
        smoothedTailDirection = Vector3.right;
        hasTailDirection = false;
        targetTailDirection = Vector3.right;
        hasTargetTailDirection = false;

        if (borderRenderer != null)
            borderBaseScale = borderRenderer.transform.localScale;

        if (scanPulseRenderer != null)
            pulseBaseScale = scanPulseRenderer.transform.localScale;

        if (label != null)
            label.text = visiblePacketId;

        routeIndex = 0;
        currentStep = 0;
        ticksUntilAdvance = 0;
        hasArrived = false;

        visualLaneIndex = 0;
        visualLaneConnection = null;

        ClearAllTagViews();
        RefreshVisuals();
        RefreshVisualLaneAssignment();
        SnapToCurrentPosition();
        StopStepEase();
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

    private void Update()
    {
        UpdateStepEaseVisual();
        UpdateTailPulseRealtime();
        UpdateSpeedTail();

        // TODO: replaced by gap animation soon
        // UpdateActiveScanPulse();
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
                // why are we doing this
                // reportedClass = PacketClass.Benign;
                break;

            case ScanStage.Probable:
                if (oldStage < ScanStage.Probable)
                    reportedClass = RollReportedClass();

                if (intelLevel < IntelLevel.Scanned)
                    intelLevel = IntelLevel.Scanned;

                RevealClass();
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

                RefreshRevealedClass(trueClass);
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
        ClearAllTagViews();
    }

    private void ResetRevealedIntel()
    {
        knowsClass = false;
        revealedClass = PacketClass.Benign;

        knowsKind = false;
        revealedKind = PacketKind.None;

        knowsInfectionType = false;
        revealedInfectionType = InfectionType.None;

        knowsSource = false;
        revealedSource = null;

        knowsDestination = false;
        revealedDestination = null;

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

    public void SetActiveIntelVisual(bool value, Color borderColor)
    {
        if (value)
        {
            scanBorderColor = borderColor;
            // TODO: replaced by gap animation soon 
            // ToggleScanBorder(true);
        }
        else
        {
            if (!isActivelyScanned && !isActivelyTraced)
            {
                // TODO: replaced by gap animation soon
                // ToggleScanBorder(false);
            }
        }

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

        switch (stage)
        {
            case ScanStage.Probable:
                RevealClass();
                break;

            case ScanStage.Likely:
                RevealClass();
                RevealKind();
                break;

            case ScanStage.Confirmed:
                RefreshRevealedClass(trueClass);
                RevealKind();
                RevealInfectionType();
                RevealAllKeywords();
                break;
        }

        scanTicksIntoStage = 0;
        RefreshVisuals();
    }

    private void UpdateActiveScanPulse()
    {
        if (!isActivelyScanned && !isActivelyTraced)
            return;

        float pulse01 = Mathf.InverseLerp(-1f, 1f, Mathf.Sin(Time.time * scanPulseSpeed));
        float alpha = Mathf.Lerp(scanPulseMinAlpha, scanPulseMaxAlpha, pulse01);
        float scale = Mathf.Lerp(scanPulseScaleMin, scanPulseScaleMax, pulse01);

        if (borderRenderer != null && borderRenderer.enabled)
        {
            Color c = scanBorderColor;
            c.a = alpha;
            borderRenderer.color = c;
            borderRenderer.transform.localScale = borderBaseScale;
        }

        if (scanPulseRenderer != null && scanPulseRenderer.enabled)
        {
            Color c = scanBorderColor;
            c.a = alpha * 0.7f;
            scanPulseRenderer.color = c;
            scanPulseRenderer.transform.localScale = pulseBaseScale * Mathf.Lerp(scale, scale + 0.12f, 0.6f);
        }
    }

    public void ToggleScanBorder(bool show)
    {
        if (borderRenderer != null)
        {
            borderRenderer.enabled = show;

            if (show)
            {
                borderRenderer.transform.localScale = borderBaseScale;

                Color c = scanBorderColor;
                c.a = scanPulseMaxAlpha;
                borderRenderer.color = c;
            }
            else
            {
                borderRenderer.transform.localScale = borderBaseScale;
                borderRenderer.color = Color.clear;
            }
        }

        if (scanPulseRenderer != null)
        {
            scanPulseRenderer.enabled = show;

            if (show)
            {
                scanPulseRenderer.transform.localScale = pulseBaseScale;

                Color c = scanBorderColor;
                c.a = scanPulseMinAlpha;
                scanPulseRenderer.color = c;
            }
            else
            {
                scanPulseRenderer.transform.localScale = pulseBaseScale;
                scanPulseRenderer.color = Color.clear;
            }
        }
    }

    public void RefreshIntelPresentation(ScanDirector scanDirector, CommandDirector commandDirector)
    {
        RefreshIntelBorderState(scanDirector);

        if (commandDirector != null)
            RefreshTags(scanDirector, commandDirector);
        else
            RefreshTags(scanDirector, null);
    }

    public void RefreshIntelBorderState(ScanDirector scanDirector)
    {
        ScanSlot scanSlot = scanDirector != null ? scanDirector.FindSlotForPacket(this) : null;
        ScanSlot traceSlot = scanDirector != null ? scanDirector.FindTraceSlotForPacket(this) : null;

        bool hasScan = scanSlot != null;
        bool hasTrace = traceSlot != null;

        isActivelyScanned = hasScan;
        isActivelyTraced = hasTrace;

        if (!hasScan && !hasTrace)
        {
            // TODO: replaced by gap animation soon
            // ToggleScanBorder(false);
            RefreshVisuals();
            return;
        }

        Color borderColor = hasTrace
            ? traceSlot.GetThemeColor()
            : scanSlot.GetThemeColor();

        SetActiveIntelVisual(true, borderColor);
    }

    private void EnsureTagPoolSize(int count)
    {
        if (packetTagPrefab == null || tagAnchor == null)
            return;

        while (activeTagViews.Count < count)
        {
            GameObject instance = Instantiate(packetTagPrefab, tagAnchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            PacketTagView tagView = instance.GetComponent<PacketTagView>();
            if (tagView == null)
            {
                Debug.LogWarning($"[PacketView] PacketTag prefab on {name} is missing PacketTagView.");
                Destroy(instance);
                return;
            }

            tagView.Hide();
            activeTagViews.Add(tagView);
        }
    }

    private void HideUnusedTagViews(int usedCount)
    {
        for (int i = usedCount; i < activeTagViews.Count; i++)
            activeTagViews[i].Hide();
    }

    private void ClearAllTagViews()
    {
        for (int i = 0; i < activeTagViews.Count; i++)
            activeTagViews[i].Hide();
    }

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

    private void RevealClass()
    {
        if (knowsClass)
            return;

        knowsClass = true;
        revealedClass = reportedClass;
        OnIntelRevealed?.Invoke(this, IntelRevealType.Class, revealedClass.ToString());
    }

    private void RefreshRevealedClass(PacketClass packetClass)
    {
        bool changed = !knowsClass || revealedClass != packetClass;

        knowsClass = true;
        revealedClass = packetClass;

        if (changed)
            OnIntelRevealed?.Invoke(this, IntelRevealType.Class, revealedClass.ToString());
    }

    public void RevealSource()
    {
        if (knowsSource)
            return;

        if (string.IsNullOrWhiteSpace(sourceAddress))
            return;

        knowsSource = true;
        revealedSource = sourceAddress;
        OnIntelRevealed?.Invoke(this, IntelRevealType.Source, revealedSource);
    }

    public void RevealDestination()
    {
        if (knowsDestination)
            return;

        string destination = GetDestinationName();
        if (string.IsNullOrWhiteSpace(destination))
            return;

        knowsDestination = true;
        revealedDestination = destination;
        OnIntelRevealed?.Invoke(this, IntelRevealType.Destination, revealedDestination);
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
        return knowsClass && revealedClass == PacketClass.Threat;
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
        float confidence01 = GetScanConfidence01();

        ApplyBodyVisual(visible, confidence01);
        ApplyGapVisual();
        ApplyBorderVisual(visible, confidence01);
    }

    private Color GetBodyTargetColor(VisibleClass visible)
    {
        return visible switch
        {
            VisibleClass.Unknown => unknownColor,
            VisibleClass.Benign => benignColor,
            VisibleClass.Threat => threatColor,
            VisibleClass.Priority => priorityColor,
            _ => unknownColor
        };
    }

    private Color GetBorderTargetColor(VisibleClass visible)
    {
        return visible switch
        {
            VisibleClass.Unknown => borderUnknownColor,
            VisibleClass.Benign => borderBenignColor,
            VisibleClass.Threat => borderThreatColor,
            VisibleClass.Priority => borderPriorityColor,
            _ => borderUnknownColor
        };
    }

    private void ApplyBodyVisual(VisibleClass visible, float confidence01)
    {
        if (spriteRenderer == null)
            return;

        Color target = GetBodyTargetColor(visible);

        Color final = visible == VisibleClass.Unknown
            ? target
            : Color.Lerp(unknownColor, target, confidence01);

        spriteRenderer.color = final;
    }

    private void ApplyGapVisual()
    {
        if (gapFillRenderer == null)
            return;

        gapFillRenderer.color = gapFillColor;
    }

   private void ApplyBorderVisual(VisibleClass visible, float confidence01)
    {
        if (borderRenderer == null)
            return;

        borderRenderer.enabled = true;

        Color target = GetBorderTargetColor(visible);

        Color final = visible == VisibleClass.Unknown
            ? target
            : Color.Lerp(borderUnknownColor, target, confidence01);

        borderRenderer.color = final;

        // Static for now while we tune shape/overlap/readability.
        borderRenderer.transform.localScale = borderBaseScale;
    }

    private void RefreshVisuals()
    {
        if (scanPulseRenderer != null)
            scanPulseRenderer.enabled = false;

        ApplyVisuals();
        RefreshScanVisual();
    }

    private void RefreshScanVisual()
    {
        if (scanVisual == null)
            return;

        if (isActivelyTraced && !isActivelyScanned)
        {
            scanVisual.ShowTrace(scanBorderColor);
            return;
        }

        if (!isActivelyScanned)
        {
            scanVisual.Hide();
            return;
        }

        if (scanStage == ScanStage.Confirmed)
        {
            scanVisual.Hide();
            return;
        }

        ScanStage visualStage = scanStage == ScanStage.Unknown
            ? ScanStage.Probable
            : scanStage;

        scanVisual.ShowScan(scanBorderColor, visualStage, GetScanConfidence01());
    }

    public void RefreshTags(ScanDirector scanDirector, CommandDirector commandDirector)
    {
        if (packetTagPrefab == null || tagAnchor == null)
            return;

        List<PacketTagData> tags = new();

        ScanSlot scanSlot = scanDirector != null ? scanDirector.FindSlotForPacket(this) : null;
        if (scanSlot != null)
        {
            tags.Add(new PacketTagData(
                scanSlot.PacketTagText,
                WithAlpha(scanSlot.GetThemeColor(), scanTagBackgroundAlpha),
                defaultTagTextColor
            ));
        }

        ScanSlot traceSlot = scanDirector != null ? scanDirector.FindTraceSlotForPacket(this) : null;
        if (traceSlot != null)
        {
            tags.Add(new PacketTagData(
                traceSlot.PacketTagText,
                WithAlpha(traceSlot.GetThemeColor(), scanTagBackgroundAlpha),
                defaultTagTextColor
            ));
        }

        if (commandDirector != null)
        {
            BlockOperation armedBlock = commandDirector.FindArmedBlockForPacket(this);
            if (armedBlock != null)
            {
                Color blockColor = commandDirector.GetBlockTagColor();
                tags.Add(new PacketTagData(
                    armedBlock.displayId,
                    WithAlpha(blockColor, blockTagBackgroundAlpha),
                    defaultTagTextColor
                ));
            }
        }

        if (tags.Count == 0)
        {
            ClearAllTagViews();
            return;
        }

        EnsureTagPoolSize(tags.Count);

        int order = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;

        for (int i = 0; i < tags.Count; i++)
        {
            PacketTagView view = activeTagViews[i];
            if (view == null)
                continue;

            view.transform.localPosition = tagStartOffset + new Vector3(i * tagSpacing, 0f, 0f);
            view.transform.localRotation = Quaternion.identity;
            view.transform.localScale = Vector3.one;

            view.SetTag(tags[i].text, tags[i].backgroundColor, tags[i].textColor);
            view.SetSortOrder(order + 1 + (i * 2), order + 2 + (i * 2));
        }

        HideUnusedTagViews(tags.Count);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void EnsureSpeedTail()
    {
        if (!enableSpeedTail || speedTail != null)
            return;

        GameObject tailObj = new GameObject("SpeedTail");
        tailObj.transform.SetParent(transform, false);
        tailObj.transform.localPosition = Vector3.zero;
        tailObj.transform.localRotation = Quaternion.identity;
        tailObj.transform.localScale = Vector3.one;

        speedTail = tailObj.AddComponent<LineRenderer>();
        speedTail.useWorldSpace = true;
        speedTail.positionCount = Mathf.Max(2, tailResolution);
        speedTail.alignment = LineAlignment.View;
        speedTail.textureMode = LineTextureMode.Stretch;
        speedTail.numCapVertices = 2;
        speedTail.numCornerVertices = 0;
        speedTail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        speedTail.receiveShadows = false;
        speedTail.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        speedTail.widthMultiplier = tailWidth;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        speedTail.material = mat;

        Color baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Color tailColor = new Color(baseColor.r, baseColor.g, baseColor.b, tailAlpha);

        if (spriteRenderer != null)
        {
            speedTail.sortingLayerID = spriteRenderer.sortingLayerID;
            speedTail.sortingOrder = spriteRenderer.sortingOrder - 1;
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(tailColor, 0f),
                new GradientColorKey(tailColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(tailAlpha, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        speedTail.colorGradient = gradient;
        speedTail.enabled = false;
    }

    private void UpdateSpeedTail()
    {
        if (!enableSpeedTail)
            return;

        EnsureSpeedTail();

        if (speedTail == null)
            return;

        ConnectionView edge = GetCurrentConnection();
        if (edge == null || hasArrived || isRemoved)
        {
            speedTail.enabled = false;
            return;
        }

        Vector3 fallbackDir = GetCurrentTravelDirection();
        Vector3 targetDir = hasTargetTailDirection ? targetTailDirection : fallbackDir;

        if (targetDir.sqrMagnitude <= 0.0001f)
        {
            speedTail.enabled = false;
            return;
        }

        targetDir.Normalize();

        if (!hasTailDirection)
        {
            smoothedTailDirection = targetDir;
            hasTailDirection = true;
        }
        else
        {
            Vector3 lerped = Vector3.Lerp(
                smoothedTailDirection,
                targetDir,
                Time.deltaTime * tailDirectionLerpSpeed
            );

            if (lerped.sqrMagnitude > 0.000001f)
                smoothedTailDirection = lerped.normalized;
        }

        float targetTailLength = GetSpeedTailLength(edge);
        currentTailLength = Mathf.Min(currentTailLength, targetTailLength);
        float tailLength = currentTailLength;
        Vector3 center = transform.position;
        Vector3 head = center - (smoothedTailDirection * tailRearOffset);
        Vector3 tail = head - (smoothedTailDirection * tailLength);
        
        speedTail.enabled = true;
        int pointCount = Mathf.Max(2, tailResolution);
        speedTail.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);
            Vector3 p = Vector3.Lerp(head, tail, t);
            speedTail.SetPosition(i, p);
        }

        if (spriteRenderer != null)
        {
            Color baseColor = spriteRenderer.color;
            Color tailColor = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

            Gradient gradient = BuildTailGradient(tailColor);
            speedTail.colorGradient = gradient;
        }
    }

    private Vector3 GetCurrentTravelDirection()
    {
        ConnectionView edge = GetCurrentConnection();
        if (edge == null || edge.nodeA == null || edge.nodeB == null)
            return Vector3.right;

        Vector3 from = IsMovingAToB() ? edge.nodeA.transform.position : edge.nodeB.transform.position;
        Vector3 to = IsMovingAToB() ? edge.nodeB.transform.position : edge.nodeA.transform.position;

        Vector3 dir = (to - from);
        if (dir.sqrMagnitude <= 0.0001f)
            return Vector3.right;

        return dir.normalized;
    }

    private Gradient BuildTailGradient(Color tailColor)
    {
        int steps = Mathf.Clamp(tailResolution, 2, 8);
        var colorKeys = new GradientColorKey[steps];
        var alphaKeys = new GradientAlphaKey[steps];

        float pulseCenter01 = GetTailPulseCenter01();

        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)(steps - 1);

            float baseA = Mathf.Lerp(tailAlpha, 0f, t);

            float pulseA = 0f;
            if (enableTailPulse && tailPulseActive)
            {
                float dist = Mathf.Abs(t - pulseCenter01);
                float normalized = 1f - Mathf.Clamp01(dist / Mathf.Max(0.001f, tailPulseWidth01));
                pulseA = normalized * normalized * tailPulseStrength;
            }

            float finalA = Mathf.Clamp01(baseA + pulseA);

            colorKeys[i] = new GradientColorKey(tailColor, t);
            alphaKeys[i] = new GradientAlphaKey(finalA, t);
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private float GetTailPulseCenter01()
    {
        if (!enableTailPulse || !tailPulseActive || tailPulseTravelDuration <= 0.001f)
            return -1f;

        return Mathf.Clamp01(tailPulseElapsed / tailPulseTravelDuration);
    }

    private float GetSpeedTailLength(ConnectionView edge)
    {
        if (edge == null)
            return tailMinLength;

        float effectiveInterval = Mathf.Max(1f, GetEffectiveMoveInterval(edge));

        // Smaller move interval = faster packet = longer tail.
        float normalizedFastness = Mathf.InverseLerp(6f, 1f, effectiveInterval);

        return Mathf.Lerp(tailMinLength, tailMaxLength, normalizedFastness);
    }

    private void GrowTailTowardTarget()
    {
        ConnectionView edge = GetCurrentConnection();
        if (edge == null)
            return;

        float targetLength = GetSpeedTailLength(edge);
        currentTailLength = Mathf.Min(targetLength, currentTailLength + tailGrowPerTick);
    }

    private void UpdateTailPulseRealtime()
    {
        if (!enableTailPulse || !tailPulseActive)
            return;

        tailPulseElapsed += Time.deltaTime;

        if (tailPulseElapsed >= tailPulseTravelDuration)
        {
            tailPulseElapsed = tailPulseTravelDuration;
            tailPulseActive = false;
        }
    }

    private void TriggerTailPulse()
    {
        if (!enableTailPulse)
            return;

        if (scaleTailPulseByPacketSpeed)
        {
            ConnectionView edge = GetCurrentConnection();
            if (edge != null)
            {
                float moveIntervalSeconds = GetCurrentMoveIntervalSeconds(edge);
                tailPulseTravelDuration = Mathf.Clamp(
                    moveIntervalSeconds * tailPulseDurationFractionOfMove,
                    tailPulseDurationMin,
                    tailPulseDurationMax
                );
            }
        }

        tailPulseElapsed = 0f;
        tailPulseActive = true;
    }

    private void SetTailTargetFromStep(Vector3 fromPosition, Vector3 toPosition)
    {
        Vector3 dir = toPosition - fromPosition;

        if (dir.sqrMagnitude <= 0.000001f)
            return;

        targetTailDirection = dir.normalized;
        hasTargetTailDirection = true;
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

        // apply aura speed modifiers after keyword effects, so they can modify the base speed for the tick they are applied on
        auraBaseSpeedModifier = 0;
        if (context.speedModifiers.TryGetValue(this, out int speedDelta))
        {
            auraBaseSpeedModifier = Mathf.Clamp(speedDelta, -2, 2);
        }

        GrowTailTowardTarget();

        ticksUntilAdvance--;

        if (ticksUntilAdvance > 0)
            return;

        AdvanceOneStep();
    }

    public virtual BlockResolution HandleBlocked(NodeView node)
    {
        return BlockResolution.Remove(PacketRemovalReason.Blocked, "blocked");
    }

    private void AdvanceOneStep()
    {
        ConnectionView edge = GetCurrentConnection();
        if (edge == null)
        {
            hasArrived = true;
            StopStepEase();
            return;
        }

        Vector3 previousVisualPosition = GetCurrentVisualPosition();

        currentStep++;

        if (currentStep >= edge.lengthSteps)
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
                StopStepEase();
                SnapToCurrentPosition();
                OnRouteCompleted?.Invoke(this);
                return;
            }

            currentStep = 0;
            edge = GetCurrentConnection();
            RefreshVisualLaneAssignment();
        }

        TriggerTailPulse();
        SnapToCurrentPosition();
        Vector3 newVisualPosition = transform.position;

        SetTailTargetFromStep(previousVisualPosition, newVisualPosition);

        if (enableStepEasing && edge != null)
        {
            BeginStepEase(previousVisualPosition, newVisualPosition, edge);
            transform.position = previousVisualPosition;
        }

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

        ticksUntilAdvance = Mathf.Max(1, GetEffectiveBaseSpeed() * edge.EffectiveLatency);
    }

    public void SnapToCurrentPosition()
    {
        ConnectionView edge = GetCurrentConnection();
        if (edge == null)
        {
            if (speedTail != null)
                speedTail.enabled = false;
            return;
        }

        RefreshVisualLaneAssignment();

        Vector3 snapped = GetCurrentVisualPosition();
        transform.position = snapped;

        if (!isStepEasing)
        {
            visualStepFromPosition = snapped;
            visualStepToPosition = snapped;
        }
    }

    private float GetCurrentTickDurationSeconds()
    {
        if (GameController.Instance == null)
            return 1f;

        return Mathf.Max(0.01f, GameController.Instance.tickIntervalSeconds);
    }

    private int GetEffectiveMoveInterval(ConnectionView edge)
    {
        int effectiveBaseSpeed = GetEffectiveBaseSpeed();

        if (edge == null)
            return effectiveBaseSpeed;

        return Mathf.Max(1, effectiveBaseSpeed * edge.EffectiveLatency);
    }

    public int GetEffectiveBaseSpeed()
    {
        return Mathf.Max(1, baseSpeed + auraBaseSpeedModifier);
    }

    private float GetCurrentMoveIntervalSeconds(ConnectionView edge)
    {
        int moveIntervalTicks = GetEffectiveMoveInterval(edge);
        return moveIntervalTicks * GetCurrentTickDurationSeconds();
    }

    private void BeginStepEase(Vector3 fromPosition, Vector3 toPosition, ConnectionView edge)
    {
        visualStepFromPosition = fromPosition;
        visualStepToPosition = toPosition;
        visualStepEaseElapsed = 0f;

        float moveIntervalSeconds = GetCurrentMoveIntervalSeconds(edge);
        visualStepEaseDuration = Mathf.Max(
            minStepEaseDuration,
            moveIntervalSeconds * stepEaseFraction
        );

        isStepEasing = true;
    }

    private void StopStepEase()
    {
        isStepEasing = false;
        visualStepEaseElapsed = 0f;
        visualStepEaseDuration = 0f;
        visualStepFromPosition = transform.position;
        visualStepToPosition = transform.position;
    }

    private void UpdateStepEaseVisual()
    {
        if (!enableStepEasing || !isStepEasing)
            return;

        if (isRemoved || hasArrived)
        {
            StopStepEase();
            return;
        }

        if (visualStepEaseDuration <= 0.0001f)
        {
            transform.position = visualStepToPosition;
            StopStepEase();
            return;
        }

        visualStepEaseElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(visualStepEaseElapsed / visualStepEaseDuration);

        transform.position = Vector3.Lerp(visualStepFromPosition, visualStepToPosition, t);

        if (t >= 1f)
            StopStepEase();
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

    public void NotifyRemoved(PacketRemovalReason reason)
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

        if (knowsClass)
            parts.Add($"class={revealedClass}");

        if (knowsKind && revealedKind != PacketKind.None)
            parts.Add($"kind={revealedKind}");

        if (knowsInfectionType && revealedInfectionType != InfectionType.None)
            parts.Add($"infection={revealedInfectionType}");

        if (knowsSource && !string.IsNullOrWhiteSpace(revealedSource))
            parts.Add($"src={revealedSource}");

        if (knowsDestination && !string.IsNullOrWhiteSpace(revealedDestination))
            parts.Add($"dest={revealedDestination}");

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
        int tailOrder = order - 4;
        int borderOrder = order - 3;
        int gapOrder = order - 2;
        int sweepOrder = order - 1;
        int bodyOrder = order;
        int labelOrder = order + 1;

        if (speedTail != null)
            speedTail.sortingOrder = tailOrder;

        if (borderRenderer != null)
            borderRenderer.sortingOrder = borderOrder;

        if (gapFillRenderer != null)
            gapFillRenderer.sortingOrder = gapOrder;

        if (scanVisual != null && gapFillRenderer != null)
            scanVisual.SetSorting(gapFillRenderer.sortingLayerID, sweepOrder);

        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = bodyOrder;

        if (label != null)
            label.sortingOrder = labelOrder;

        if (scanMask != null && gapFillRenderer != null)
        {
            scanMask.frontSortingLayerID = gapFillRenderer.sortingLayerID;
            scanMask.backSortingLayerID = gapFillRenderer.sortingLayerID;
            scanMask.isCustomRangeActive = true;
            scanMask.backSortingOrder = gapOrder;
            scanMask.frontSortingOrder = bodyOrder;
        }

        for (int i = 0; i < activeTagViews.Count; i++)
        {
            if (activeTagViews[i] == null)
                continue;

            activeTagViews[i].SetSortOrder(labelOrder + 1 + (i * 2), labelOrder + 2 + (i * 2));
        }
    }

    // used for packetoverlapcoordinator to determine distance between packets 
    public float GetVisualOverlapRadius()
    {
        if (borderRenderer != null)
        {
            Bounds b = borderRenderer.bounds;
            return Mathf.Max(b.extents.x, b.extents.y);
        }

        if (spriteRenderer != null)
        {
            Bounds b = spriteRenderer.bounds;
            return Mathf.Max(b.extents.x, b.extents.y);
        }

        return 0.2f;
    }

}