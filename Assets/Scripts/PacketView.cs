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
    [SerializeField] private SpriteRenderer scanTagBackgroundRenderer;

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
    [SerializeField] private int activeScanSlotIndex = -1;

    [Header("Visuals")]
    public Color unknownColor = Color.gray;
    public Color benignColor = Color.green;
    public Color threatColor = Color.red;
    public Color priorityColor = Color.cyan;
    public PacketScanVisual scanVisual;
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer borderRenderer;
    [SerializeField] private SpriteRenderer scanPulseRenderer;
    [SerializeField] private float scanPulseSpeed = 5f;
    [SerializeField] private float scanPulseMinAlpha = 0.35f;
    [SerializeField] private float scanPulseMaxAlpha = 1f;
    [SerializeField] private float scanPulseScaleMin = 1.05f;
    [SerializeField] private float scanPulseScaleMax = 1.28f;
    private Vector3 borderBaseScale = Vector3.one;
    private Vector3 pulseBaseScale = Vector3.one;

    [Header("Scan Tag Visuals")]
    [SerializeField] private float scanTagBackgroundAlpha = 0.92f;
    [SerializeField] private Color scanTagTextColor = new(0.08f, 0.08f, 0.08f, 1f);

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

    private LineRenderer speedTail;

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
        UpdateSpeedTail();
        UpdateActiveScanPulse();
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
        ToggleScanBorder(value);

        if (!value)
            HideActiveScanTag();

        RefreshVisuals();
    }

    public void ShowActiveScanTag(ScanSlot slot)
    {
        if (slot == null)
        {
            HideActiveScanTag();
            return;
        }

        activeScanSlotIndex = slot.slotIndex;
        ApplyActiveScanTag(slot);
    }

    public void HideActiveScanTag()
    {
        activeScanSlotIndex = -1;

        if (scanTagLabel != null)
        {
            scanTagLabel.text = "";
            scanTagLabel.gameObject.SetActive(false);
        }

        if (scanTagBackgroundRenderer != null)
        {
            scanTagBackgroundRenderer.enabled = false;
            scanTagBackgroundRenderer.color = Color.clear;
        }
    }

    private void ApplyActiveScanTag(ScanSlot slot)
    {
        if (scanTagLabel == null)
            return;

        if (!isActivelyScanned || slot == null)
        {
            HideActiveScanTag();
            return;
        }

        activeScanSlotIndex = slot.slotIndex;

        scanTagLabel.gameObject.SetActive(true);
        scanTagLabel.color = scanTagTextColor;
        scanTagLabel.text = $"S{slot.slotIndex + 1}";

        if (scanTagBackgroundRenderer != null)
        {
            Color bg = slot.GetThemeColor();
            bg.a = scanTagBackgroundAlpha;
            scanTagBackgroundRenderer.enabled = true;
            scanTagBackgroundRenderer.color = bg;
        }
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

    private void UpdateActiveScanPulse()
    {
        if (!isActivelyScanned)
            return;

        float pulse01 = Mathf.InverseLerp(-1f, 1f, Mathf.Sin(Time.time * scanPulseSpeed));
        float alpha = Mathf.Lerp(scanPulseMinAlpha, scanPulseMaxAlpha, pulse01);
        float scale = Mathf.Lerp(scanPulseScaleMin, scanPulseScaleMax, pulse01);

        if (borderRenderer != null && borderRenderer.enabled)
        {
            Color c = scanBorderColor;
            c.a = alpha;
            borderRenderer.color = c;
            borderRenderer.transform.localScale = borderBaseScale * scale;
        }

        if (scanPulseRenderer != null && scanPulseRenderer.enabled)
        {
            Color c = scanBorderColor;
            c.a = alpha * 0.7f;
            scanPulseRenderer.color = c;
            scanPulseRenderer.transform.localScale = pulseBaseScale * Mathf.Lerp(scale, scale + 0.12f, 0.6f);
        }
    }

    public void HideScanTag()
    {
        isActivelyScanned = false;
        ToggleScanBorder(false);
        HideActiveScanTag();
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

    public void RefreshScanTag(ScanDirector scanDirector)
    {
        if (scanTagLabel == null)
            return;

        if (!isActivelyScanned)
        {
            HideActiveScanTag();
            return;
        }

        ScanSlot slot = scanDirector != null ? scanDirector.FindSlotForPacket(this) : null;
        ApplyActiveScanTag(slot);
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
        speedTail.positionCount = 2;
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

        Vector3 moveDir = GetCurrentTravelDirection();
        if (moveDir.sqrMagnitude <= 0.0001f)
        {
            speedTail.enabled = false;
            return;
        }

        float tailLength = GetSpeedTailLength(edge);
        Vector3 center = transform.position;
        Vector3 head = center - (moveDir * tailRearOffset);
        Vector3 tail = head - (moveDir * tailLength);

        speedTail.enabled = true;
        speedTail.SetPosition(0, head);
        speedTail.SetPosition(1, tail);

        if (spriteRenderer != null)
        {
            Color baseColor = spriteRenderer.color;
            Color tailColor = new Color(baseColor.r, baseColor.g, baseColor.b, tailAlpha);

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

    private float GetSpeedTailLength(ConnectionView edge)
    {
        if (edge == null)
            return tailMinLength;

        float effectiveInterval = Mathf.Max(1f, GetEffectiveMoveInterval(edge));

        // Smaller move interval = faster packet = longer tail.
        float normalizedFastness = Mathf.InverseLerp(6f, 1f, effectiveInterval);

        return Mathf.Lerp(tailMinLength, tailMaxLength, normalizedFastness);
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
            StopStepEase();
            return;
        }

        Vector3 previousVisualPosition = GetCurrentVisualPosition();

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
                StopStepEase();
                SnapToCurrentPosition();
                OnRouteCompleted?.Invoke(this);
                return;
            }

            currentStep = 0;
            edge = GetCurrentConnection();
            RefreshVisualLaneAssignment();
        }

        SnapToCurrentPosition();
        Vector3 newVisualPosition = transform.position;

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

        ticksUntilAdvance = Mathf.Max(1, baseSpeed * edge.EffectiveLatency);
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
        if (edge == null)
            return baseSpeed;

        return Mathf.Max(1, baseSpeed * edge.EffectiveLatency);
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

        if (scanPulseRenderer != null)
            scanPulseRenderer.sortingOrder = order - 2;

        if (scanTagBackgroundRenderer != null)
            scanTagBackgroundRenderer.sortingOrder = order + 1;

        if (scanTagLabel != null)
            scanTagLabel.sortingOrder = order + 2;

        if (speedTail != null)
            speedTail.sortingOrder = order - 3;
    }

}