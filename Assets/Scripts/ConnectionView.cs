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

    [Header("Label Colors")]
    public Color baseLabelColor = new(0.67f, 0.67f, 0.67f, 0.53f);
    public Color modifiedLabelColor = new(0.72f, 1.00f, 0.72f, 0.95f);

    public int EffectiveLatency => Mathf.Max(1, latency + GetTotalLatencyModifier());

    private void Awake()
    {
        EnsureEdgeLabel();
        RefreshVisuals();
    }

    private void Reset()
    {
        lineRenderer = GetComponent<LineRenderer>();
        EnsureEdgeLabel();
        RefreshVisuals();
    }

    private void OnValidate()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        EnsureEdgeLabel();
        RefreshVisuals();
    }

    public void RefreshLine()
    {
        if (lineRenderer == null || nodeA == null || nodeB == null)
            return;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, nodeA.transform.position);
        lineRenderer.SetPosition(1, nodeB.transform.position);

        RefreshLabel();
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
    }

    public void SetThrottle(int amount)
    {
        AddLatencyModifier(ManualThrottleSourceId, Mathf.Max(0, amount));
    }

    public void ClearThrottle()
    {
        RemoveLatencyModifier(ManualThrottleSourceId);
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

    private string BuildLatencyLabel()
    {
        int modifier = GetTotalLatencyModifier();

        return modifier > 0
            ? $"L{latency}+{modifier}"
            : $"L{latency}";
    }

}