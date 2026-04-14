using UnityEngine;
using System.Collections.Generic;
using TMPro;

[ExecuteAlways]
public class ConnectionView : MonoBehaviour
{
    public string connectionId;

    public NodeView nodeA;
    public NodeView nodeB;

    [Min(1)]
    public int lengthSteps = 5;

    [Min(1)]
    public int latency = 1;

    public LineRenderer lineRenderer;

    [Header("Label")]
    public TextMeshPro edgeLabel;
    public float labelAlongEdge = 0.5f;
    public float labelNormalOffset = 0.18f;
    public bool rotateLabelWithEdge = true;
    public int labelSortingOrder = 5;

    [Header("Latency Modifiers")]
    private readonly Dictionary<string, int> latencyModifiers = new();
    private const string ManualThrottleSourceId = "command:throttle";

    [Header("Throttle")]
    [Min(1)]
    public int throttleDurationTicks = 20;
    [Min(0)]
    public int throttleMinAmount = 1;
    [Min(0)]
    public int throttleMaxAmount = 3;
    [Min(0)]
    public int throttleDefaultAmount = 2;

    private int throttleRemainingTicks = 0;

    [Header("Label Colors")]
    public Color baseLabelColor = new(0.67f, 0.67f, 0.67f, 0.53f);
    public Color modifiedLabelColor = new(0.72f, 1.00f, 0.72f, 0.95f);

    [Header("Throttle Pulse Visual")]
    public TextMeshPro slowPulseLabel;
    public float slowPulseAlongEdge = 0.5f;
    public float slowPulseNormalOffset = -0.10f;
    public bool rotateSlowPulseWithEdge = true;
    public int slowPulseSortingOrder = 4;
    public Color slowPulseColor = new(0.72f, 1.00f, 0.72f, 0.95f);
    public float slowPulseSpeed = 3.0f;
    public float slowPulseAlphaMin = 0.20f;
    public float slowPulseAlphaMax = 0.50f;
    public float slowPulseScaleMin = 0.90f;
    public float slowPulseScaleMax = 1.10f;

    [Header("Throttle Line Pulse")]
    public float throttledWidthMultiplierMin = 1.0f;
    public float throttledWidthMultiplierMax = 1.8f;
    public float throttledAlphaMin = 0.25f;
    public float throttledAlphaMax = 0.70f;

    private float baseLineWidthStart = 0.02f;
    private float baseLineWidthEnd = 0.02f;
    private Color baseLineStartColor = new(1f, 1f, 1f, 0.25f);
    private Color baseLineEndColor = new(1f, 1f, 1f, 0.25f);

    public int EffectiveLatency => Mathf.Max(1, latency + GetTotalLatencyModifier());

    private void Awake()
    {
        EnsureLineRenderer();
        EnsureEdgeLabel();
        EnsureSlowPulseLabel();
        RefreshVisuals();
    }

    private void Reset()
    {
        lineRenderer = GetComponent<LineRenderer>();
        EnsureEdgeLabel();
        EnsureSlowPulseLabel();
        RefreshVisuals();
    }

    private void OnValidate()
    {
        EnsureLineRenderer();
        EnsureEdgeLabel();
        EnsureSlowPulseLabel();
        RefreshVisuals();
    }

    private void Update()
    {
        // Visual-only pulse. Gameplay duration is tick-driven.
        UpdateThrottleVisuals();
    }

    private void OnEnable()
    {
        RefreshVisuals();
    }

    public void RefreshLine()
    {
        if (lineRenderer == null || nodeA == null || nodeB == null)
            return;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, nodeA.transform.position);
        lineRenderer.SetPosition(1, nodeB.transform.position);

        // Preserve your current defaults as the baseline look.
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;

        Color c = lineRenderer.startColor;
        c.a = 0.25f;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;

        CacheBaseLineVisuals();
        RefreshLabel();
        RefreshSlowPulseLabelTransform();
    }

    private void ApplySortingLayer()
    {
        if (lineRenderer != null)
        {
            lineRenderer.sortingLayerName = "Connections";
        }

        if (edgeLabel != null)
        {
            edgeLabel.sortingLayerID = SortingLayer.NameToID("Connections");
        }

        if (slowPulseLabel != null)
        {
            slowPulseLabel.sortingLayerID = SortingLayer.NameToID("Connections");
        }
    }

    public Vector3 GetWorldPositionAtStep(int step, bool aToB)
    {
        if (nodeA == null || nodeB == null)
            return transform.position;

        int clampedStep = Mathf.Clamp(step, 0, lengthSteps);

        Vector3 start = aToB ? nodeA.transform.position : nodeB.transform.position;
        Vector3 end = aToB ? nodeB.transform.position : nodeA.transform.position;

        float t = lengthSteps == 0 ? 0f : (float)clampedStep / lengthSteps;
        return Vector3.Lerp(start, end, t);
    }

    public Vector3 GetLaneNormal()
    {
        if (nodeA == null || nodeB == null)
            return Vector3.up;

        Vector3 dir = nodeB.transform.position - nodeA.transform.position;

        if (dir.sqrMagnitude <= 0.0001f)
            return Vector3.up;

        Vector3 dirNorm = dir.normalized;
        return new Vector3(-dirNorm.y, dirNorm.x, 0f);
    }

    public NodeView GetStartNode(bool aToB)
    {
        return aToB ? nodeA : nodeB;
    }

    public NodeView GetEndNode(bool aToB)
    {
        return aToB ? nodeB : nodeA;
    }

    public void RefreshVisuals()
    {
        RefreshLine();
        RefreshLabel();
        RefreshSlowPulseLabelTransform();
        UpdateThrottleVisuals();
        ApplySortingLayer();
    }

    public void SetThrottle(int amount)
    {
        amount = NormalizeThrottleAmount(amount);

        if (IsThrottleActive())
        {
            throttleRemainingTicks = throttleDurationTicks;
            RefreshLabel();
            return;
        }

        AddLatencyModifier(ManualThrottleSourceId, amount);
        throttleRemainingTicks = throttleDurationTicks;
        RefreshVisuals();
    }

    public void ClearThrottle()
    {
        throttleRemainingTicks = 0;
        RemoveLatencyModifier(ManualThrottleSourceId);
        RefreshVisuals();
    }

    public int NormalizeThrottleAmount(int requestedAmount)
    {
        int min = Mathf.Min(throttleMinAmount, throttleMaxAmount);
        int max = Mathf.Max(throttleMinAmount, throttleMaxAmount);

        if (requestedAmount <= 0)
            requestedAmount = throttleDefaultAmount;

        return Mathf.Clamp(requestedAmount, min, max);
    }

    public void ProcessTick()
    {
        if (throttleRemainingTicks <= 0)
            return;

        throttleRemainingTicks--;

        if (throttleRemainingTicks <= 0)
        {
            throttleRemainingTicks = 0;
            RemoveLatencyModifier(ManualThrottleSourceId);
        }

        RefreshLabel();
    }

    public bool IsThrottleActive()
    {
        return throttleRemainingTicks > 0 && GetLatencyModifier(ManualThrottleSourceId) > 0;
    }

    public int GetThrottleRemainingTicks()
    {
        return throttleRemainingTicks;
    }

    public int GetManualThrottleAmount()
    {
        return GetLatencyModifier(ManualThrottleSourceId);
    }

    public void AddLatencyModifier(string sourceId, int amount)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        latencyModifiers[sourceId] = Mathf.Max(0, amount);
        RefreshLabel();
    }

    public void RemoveLatencyModifier(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        if (latencyModifiers.Remove(sourceId))
            RefreshLabel();
    }

    public int GetLatencyModifier(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return 0;

        return latencyModifiers.TryGetValue(sourceId, out int amount) ? amount : 0;
    }

    public int GetTotalLatencyModifier()
    {
        int total = 0;

        foreach (var kvp in latencyModifiers)
            total += Mathf.Max(0, kvp.Value);

        return total;
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    private void EnsureEdgeLabel()
    {
        if (edgeLabel != null)
            return;

        Transform existing = transform.Find("EdgeLabel");
        if (existing != null)
        {
            edgeLabel = existing.GetComponent<TextMeshPro>();
            if (edgeLabel != null)
                return;
        }

        GameObject go = new GameObject("EdgeLabel");
        go.transform.SetParent(transform, false);

        edgeLabel = go.AddComponent<TextMeshPro>();
        edgeLabel.alignment = TextAlignmentOptions.Center;
        edgeLabel.fontSize = 2.2f;
        edgeLabel.text = "";
        edgeLabel.sortingOrder = labelSortingOrder;
        edgeLabel.color = baseLabelColor;
    }

    private void EnsureSlowPulseLabel()
    {
        if (slowPulseLabel != null)
            return;

        Transform existing = transform.Find("SlowPulseLabel");
        if (existing != null)
        {
            slowPulseLabel = existing.GetComponent<TextMeshPro>();
            if (slowPulseLabel != null)
                return;
        }

        GameObject go = new GameObject("SlowPulseLabel");
        go.transform.SetParent(transform, false);

        slowPulseLabel = go.AddComponent<TextMeshPro>();
        slowPulseLabel.alignment = TextAlignmentOptions.Center;
        slowPulseLabel.fontSize = 1.8f;
        slowPulseLabel.sortingOrder = slowPulseSortingOrder;
        slowPulseLabel.color = new Color(slowPulseColor.r, slowPulseColor.g, slowPulseColor.b, 0f);
    }

    public void RefreshLabel()
    {
        if (edgeLabel == null || nodeA == null || nodeB == null)
            return;

        Vector3 a = nodeA.transform.position;
        Vector3 b = nodeB.transform.position;

        Vector3 dir = (b - a);
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        Vector3 dirNorm = dir.normalized;
        Vector3 normal = new Vector3(-dirNorm.y, dirNorm.x, 0f);

        float t = Mathf.Clamp01(labelAlongEdge);
        Vector3 basePos = Vector3.Lerp(a, b, t);
        edgeLabel.transform.position = basePos + (normal * labelNormalOffset);

        if (rotateLabelWithEdge)
        {
            float angle = Mathf.Atan2(dirNorm.y, dirNorm.x) * Mathf.Rad2Deg;

            if (angle > 90f || angle < -90f)
                angle += 180f;

            edgeLabel.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            edgeLabel.transform.rotation = Quaternion.identity;
        }

        bool modified = GetTotalLatencyModifier() > 0;
        edgeLabel.color = modified ? modifiedLabelColor : baseLabelColor;
        edgeLabel.sortingOrder = labelSortingOrder;
        edgeLabel.text = BuildLatencyLabel();
    }

    private int GetDisplaySecondsFromTicks(int ticks)
    {
        float tickSeconds = 1f;

        if (GameController.Instance != null)
            tickSeconds = Mathf.Max(0.01f, GameController.Instance.tickIntervalSeconds);

        return Mathf.CeilToInt(ticks * tickSeconds);
    }

    public string GetThrottleRemainingDisplayText()
    {
        return $"{GetDisplaySecondsFromTicks(throttleRemainingTicks)}s";
    }

    private string BuildLatencyLabel()
    {
        int modifier = GetTotalLatencyModifier();

        if (modifier <= 0)
            return $"{connectionId} L{latency}";

        if (IsThrottleActive())
            return $"{connectionId} L{latency}+{modifier}\n[{GetThrottleRemainingDisplayText()}]";

        return $"{connectionId} L{latency}+{modifier}";
    }

    private void RefreshSlowPulseLabelTransform()
    {
        if (slowPulseLabel == null || nodeA == null || nodeB == null)
            return;

        Vector3 a = nodeA.transform.position;
        Vector3 b = nodeB.transform.position;

        Vector3 dir = (b - a);
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        Vector3 dirNorm = dir.normalized;
        Vector3 normal = new Vector3(-dirNorm.y, dirNorm.x, 0f);

        float t = Mathf.Clamp01(slowPulseAlongEdge);
        Vector3 basePos = Vector3.Lerp(a, b, t);
        slowPulseLabel.transform.position = basePos + (normal * slowPulseNormalOffset);

        if (rotateSlowPulseWithEdge)
        {
            float angle = Mathf.Atan2(dirNorm.y, dirNorm.x) * Mathf.Rad2Deg;

            if (angle > 90f || angle < -90f)
                angle += 180f;

            slowPulseLabel.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            slowPulseLabel.transform.rotation = Quaternion.identity;
        }
    }

    private void UpdateThrottleVisuals()
    {
        RefreshSlowPulseLabelTransform();

        bool throttled = IsThrottleActive();

        if (slowPulseLabel != null)
        {
            if (!throttled)
            {
                Color hidden = slowPulseColor;
                hidden.a = 0f;
                slowPulseLabel.color = hidden;
                slowPulseLabel.transform.localScale = Vector3.one * slowPulseScaleMin;
            }
            else
            {
                float pulse01 = 0.5f + (0.5f * Mathf.Sin(Time.time * slowPulseSpeed));
                float alpha = Mathf.Lerp(slowPulseAlphaMin, slowPulseAlphaMax, pulse01);
                float scale = Mathf.Lerp(slowPulseScaleMin, slowPulseScaleMax, pulse01);

                Color c = slowPulseColor;
                c.a = alpha;
                slowPulseLabel.color = c;
                slowPulseLabel.sortingOrder = slowPulseSortingOrder;
                slowPulseLabel.transform.localScale = Vector3.one * scale;
            }
        }

        if (lineRenderer != null)
        {
            if (!throttled)
            {
                lineRenderer.startWidth = baseLineWidthStart;
                lineRenderer.endWidth = baseLineWidthEnd;
                lineRenderer.startColor = baseLineStartColor;
                lineRenderer.endColor = baseLineEndColor;
            }
            else
            {
                float pulse01 = 0.5f + (0.5f * Mathf.Sin(Time.time * slowPulseSpeed));
                float widthMul = Mathf.Lerp(throttledWidthMultiplierMin, throttledWidthMultiplierMax, pulse01);
                float alpha = Mathf.Lerp(throttledAlphaMin, throttledAlphaMax, pulse01);

                lineRenderer.startWidth = baseLineWidthStart * widthMul;
                lineRenderer.endWidth = baseLineWidthEnd * widthMul;

                Color start = baseLineStartColor;
                Color end = baseLineEndColor;
                start.a = alpha;
                end.a = alpha;

                lineRenderer.startColor = start;
                lineRenderer.endColor = end;
            }
        }
    }

    private void CacheBaseLineVisuals()
    {
        if (lineRenderer == null)
            return;

        baseLineWidthStart = lineRenderer.startWidth;
        baseLineWidthEnd = lineRenderer.endWidth;
        baseLineStartColor = lineRenderer.startColor;
        baseLineEndColor = lineRenderer.endColor;
    }
}