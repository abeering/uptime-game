using UnityEngine;
using System;

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

public class PacketView : MonoBehaviour
{
    public string packetId = "a";
    public string PacketId => packetId;

    [Header("Packet Behavior")]
    [Min(1)]
    public int baseSpeed = 1;
    public int boostCount = 0;

    [Header("Debug State")]
    public int routeIndex = 0;
    public int currentStep = 0;
    public int ticksUntilAdvance = 0;
    public bool movingAToB = true;
    public bool hasArrived = false;
    public TMPro.TextMeshPro label;

    [HideInInspector] public RouteStep[] route;

    [Header("Packet Type")]
    public PacketClass trueClass;
    public PacketKind trueKind;
    public string sourceAddress;

    [Header("Intel")]
    public IntelLevel intelLevel = IntelLevel.None;
    [Range(0, 100)] public int confidencePercent = 0;
    [Range(0, 100)] public int scanDifficulty = 25;
    public PacketClass reportedClass = PacketClass.Benign;

    [Header("Visuals")]
    public PacketScanVisual scanVisual;
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer borderRenderer;

    public event Action<PacketView, NodeView> OnReachedNode;
    public event Action<PacketView, string> OnRemoved;
    public event Action<PacketView> OnRouteCompleted;

    public void Initialize(
        string newPacketId,
        PacketClass newClass,
        PacketKind newKind,
        string newSourceAddress,
        int newBaseSpeed,
        int newScanDifficulty,
        RouteStep[] newRoute
    )
    {
        packetId = newPacketId;
        trueClass = newClass;
        trueKind = newKind;
        sourceAddress = newSourceAddress;
        scanDifficulty = Mathf.Clamp(newScanDifficulty, 0, 100);

        reportedClass = PacketClass.Benign;
        confidencePercent = 0;
        intelLevel = IntelLevel.None;

        baseSpeed = Mathf.Max(1, newBaseSpeed);
        boostCount = 0;
        route = newRoute;

        RefreshVisuals();

        if (label != null)
            label.text = newPacketId;

        routeIndex = 0;
        currentStep = 0;
        ticksUntilAdvance = 0;
        hasArrived = false;

        SnapToCurrentPosition();
        ResetAdvanceTimer();
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

    public int GetClassConfidence()
    {
        return confidencePercent;
    }

    public string GetConfidenceText()
    {
        return $"{confidencePercent}%";
    }

    public bool IsFullyIdentified()
    {
        return confidencePercent >= 100;
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

    private void ApplyVisuals()
    {
        VisibleClass visible = GetVisibleClass();

        Color identityColor = visible switch
        {
            VisibleClass.Unknown => Color.gray,
            VisibleClass.Benign => Color.green,
            VisibleClass.Threat => Color.red,
            VisibleClass.Priority => Color.cyan,
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
    }

    public void ApplyQuickScan()
    {
        PacketClass newReportedClass = RollReportedClass();
        int newConfidence = RollConfidence(newReportedClass);

        reportedClass = newReportedClass;
        confidencePercent = Mathf.Clamp(newConfidence, 1, 99);

        if (intelLevel < IntelLevel.Scanned)
            intelLevel = IntelLevel.Scanned;

        RefreshVisuals();
    }

    public void ApplyDeepScan()
    {
        reportedClass = trueClass;
        confidencePercent = 100;
        intelLevel = IntelLevel.DeepScanned;
        RefreshVisuals();
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

    private int RollConfidence(PacketClass newReportedClass)
    {
        float difficulty01 = Mathf.Clamp01(scanDifficulty / 100f);
        bool isCorrect = newReportedClass == trueClass;

        int minConfidence;
        int maxConfidence;

        if (isCorrect)
        {
            minConfidence = Mathf.RoundToInt(Mathf.Lerp(70f, 30f, difficulty01));
            maxConfidence = Mathf.RoundToInt(Mathf.Lerp(95f, 65f, difficulty01));
        }
        else
        {
            minConfidence = Mathf.RoundToInt(Mathf.Lerp(5f, 20f, difficulty01));
            maxConfidence = Mathf.RoundToInt(Mathf.Lerp(25f, 55f, difficulty01));
        }

        return UnityEngine.Random.Range(minConfidence, maxConfidence + 1);
    }

    public void Tick()
    {
        if (hasArrived || route == null || route.Length == 0)
            return;

        ticksUntilAdvance--;

        if (ticksUntilAdvance > 0)
            return;

        AdvanceOneStep();
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
                OnReachedNode?.Invoke(this, reachedNode);
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
        }

        SnapToCurrentPosition();
        ResetAdvanceTimer();
    }

    private void ResetAdvanceTimer()
    {
        ConnectionView edge = GetCurrentConnection();

        if (edge == null)
        {
            ticksUntilAdvance = 0;
            return;
        }

        ticksUntilAdvance = Mathf.Max(1, baseSpeed * edge.latency);
    }

    public void SnapToCurrentPosition()
    {
        ConnectionView edge = GetCurrentConnection();
        if (edge == null)
            return;

        transform.position = edge.GetWorldPositionAtStep(currentStep, IsMovingAToB());
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

    public void SetVisualSortOrder(int order)
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = order;

        if (label != null)
            label.sortingOrder = order;

        if (borderRenderer != null)
            borderRenderer.sortingOrder = order - 1;
    }

    public void BeginQuickScanVisual()
    {
        if (scanVisual != null)
            scanVisual.BeginQuickScan();
    }

    public void BeginDeepScanVisual()
    {
        if (scanVisual != null)
            scanVisual.BeginDeepScan();
    }

    public void UpdateScanVisual(float progress01)
    {
        if (scanVisual != null)
            scanVisual.SetScanProgress(progress01);
    }

    public void CompleteScanVisual(string text)
    {
        if (scanVisual != null)
            scanVisual.CompleteScan(text);
    }

    public void FailScanVisual(string text = "scan failed")
    {
        if (scanVisual != null)
            scanVisual.FailScan(text);
    }

    public void CancelScanVisual(string text = "cancelled")
    {
        if (scanVisual != null)
            scanVisual.CancelScan(text);
    }

}