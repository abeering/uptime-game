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

public enum QuickScanClass
{
    Benign,
    Suspicious,
    Threat,
    Priority
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
    Suspicious,
    Threat,
    Priority
}

public class PacketView : MonoBehaviour
{
    public string packetId = "a";
    public string PacketId => packetId;


    [Header("Packet Behavior")]
    [Min(1)]
    public int baseSpeed = 1; // ticks per step before edge latency

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
    public QuickScanClass quickScanClass;
    public IntelLevel intelLevel = IntelLevel.None;
    public string sourceAddress;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;

    public Color normalColor = Color.white;
    public Color malwareColor = Color.red;

    public event Action<PacketView, NodeView> OnReachedNode;
    public event Action<PacketView, string> OnRemoved;
    public event Action<PacketView> OnRouteCompleted;

    public void Initialize(
        string newPacketId,
        PacketClass newClass,
        PacketKind newKind,
        QuickScanClass newQuickScanClass,
        string newSourceAddress,
        int newBaseSpeed,
        RouteStep[] newRoute
    )
    {
        packetId = newPacketId;
        trueClass = newClass;
        trueKind = newKind;
        quickScanClass = newQuickScanClass;
        sourceAddress = newSourceAddress;

        intelLevel = IntelLevel.None;
        baseSpeed = Mathf.Max(1, newBaseSpeed);
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

    public VisibleClass GetVisibleClass()
    {
        if (intelLevel == IntelLevel.None)
            return VisibleClass.Unknown;

        if (intelLevel == IntelLevel.Scanned)
        {
            return quickScanClass switch
            {
                QuickScanClass.Benign => VisibleClass.Benign,
                QuickScanClass.Suspicious => VisibleClass.Suspicious,
                QuickScanClass.Threat => VisibleClass.Threat,
                QuickScanClass.Priority => VisibleClass.Priority,
                _ => VisibleClass.Unknown
            };
        }

        return trueClass switch
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

        Color bodyColor = visible switch
        {
            VisibleClass.Unknown => Color.gray,
            VisibleClass.Benign => Color.white,
            VisibleClass.Suspicious => Color.yellow,
            VisibleClass.Threat => Color.red,
            VisibleClass.Priority => Color.cyan,
            _ => Color.white
        };

        if (spriteRenderer != null)
            spriteRenderer.color = bodyColor;
        }

    private void RefreshVisuals()
    {
        ApplyVisuals();
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

    public void ApplyScan()
    {
        if (intelLevel < IntelLevel.Scanned)
            intelLevel = IntelLevel.Scanned;

        RefreshVisuals();
    }

    public void ApplyDeepScan()
    {
        intelLevel = IntelLevel.DeepScanned;
        RefreshVisuals();
    }
}
