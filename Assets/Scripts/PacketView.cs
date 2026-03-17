using UnityEngine;
using System;

public enum PacketKind
{
    Normal,
    Malware
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
    public PacketKind kind = PacketKind.Normal;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;

    public Color normalColor = Color.white;
    public Color malwareColor = Color.red;

    public event Action<PacketView, NodeView> OnReachedNode;
    public event Action<PacketView, string> OnRemoved;
    public event Action<PacketView> OnRouteCompleted;

    public void Initialize(string newPacketId, PacketKind newKind, int newBaseSpeed, RouteStep[] newRoute)
    {
        packetId = newPacketId;
        kind = newKind;
        baseSpeed = Mathf.Max(1, newBaseSpeed);
        route = newRoute;
        ApplyVisuals();

        if (label != null)
            label.text = newPacketId;

        routeIndex = 0;
        currentStep = 0;
        ticksUntilAdvance = 0;
        hasArrived = false;

        SnapToCurrentPosition();
        ResetAdvanceTimer();
    }

    private void ApplyVisuals()
    {
        if (spriteRenderer == null)
            return;

        switch (kind)
        {
            case PacketKind.Normal:
                spriteRenderer.color = normalColor;
                break;

            case PacketKind.Malware:
                spriteRenderer.color = malwareColor;
                break;
        }
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
}
