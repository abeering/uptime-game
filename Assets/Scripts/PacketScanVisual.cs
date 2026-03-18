using UnityEngine;
using TMPro;

public enum PacketScanVisualMode
{
    None,
    QuickScan,
    DeepScan
}

public class PacketScanVisual : MonoBehaviour
{
    [Header("Ring")]
    public LineRenderer ringRenderer;
    [Min(8)] public int ringSegments = 48;
    public float ringRadius = 0.42f;
    public float ringWidth = 0.04f;

    [Header("Colors")]
    public Color quickScanColor = Color.green;
    public Color deepScanColor = Color.cyan;
    public Color failedColor = Color.red;

    [Header("Result Label")]
    public TextMeshPro resultLabel;
    public Vector3 resultLocalOffset = new Vector3(0f, -0.42f, 0f);
    public float resultRiseDistance = 0.08f;
    public float resultDuration = 1.2f;

    private PacketScanVisualMode activeMode = PacketScanVisualMode.None;
    private float activeProgress01 = 0f;
    private bool scanVisible = false;

    private bool resultPlaying = false;
    private float resultTimer = 0f;
    private Vector3 resultStartLocalPos;
    private Color resultBaseColor;

    private void Awake()
    {
        if (ringRenderer != null)
        {
            ringRenderer.loop = false;
            ringRenderer.useWorldSpace = false;
            ringRenderer.startWidth = ringWidth;
            ringRenderer.endWidth = ringWidth;
            ringRenderer.positionCount = 0;
            ringRenderer.enabled = false;
        }

        if (resultLabel != null)
        {
            resultStartLocalPos = resultLocalOffset;
            resultLabel.transform.localPosition = resultLocalOffset;
            resultLabel.text = "";
            SetResultAlpha(0f);
        }
    }

    private void Update()
    {
        UpdateResultLabel();
    }

    public void BeginQuickScan()
    {
        activeMode = PacketScanVisualMode.QuickScan;
        SetRingProgress(0f);
        scanVisible = true;
        ApplyRingColor(quickScanColor);
    }

    public void BeginDeepScan()
    {
        activeMode = PacketScanVisualMode.DeepScan;
        SetRingProgress(0f);
        scanVisible = true;
        ApplyRingColor(deepScanColor);
    }

    public void SetScanProgress(float progress01)
    {
        if (!scanVisible)
            return;
        float eased = Mathf.SmoothStep(0f, 1f, progress01);
        SetRingProgress(eased);
    }

    public void CompleteScan(string text)
    {
        Color resultColor = GetModeColor();
        SetRingProgress(1f);
        HideRing();
        ShowResult(text, resultColor);
    }

    public void FailScan(string text = "scan failed")
    {
        HideRing();
        ShowResult(text, failedColor);
    }

    public void CancelScan(string text = "cancelled")
    {
        HideRing();
        ShowResult(text, Color.gray);
    }

    private void SetRingProgress(float progress01)
    {
        activeProgress01 = Mathf.Clamp01(progress01);

        if (ringRenderer == null)
            return;

        if (activeProgress01 <= 0f)
        {
            ringRenderer.enabled = true;
            ringRenderer.positionCount = 2;
            Vector3 start = GetCirclePoint(0f);
            ringRenderer.SetPosition(0, start);
            ringRenderer.SetPosition(1, start);
            return;
        }

        int pointCount = Mathf.Max(2, Mathf.CeilToInt(ringSegments * activeProgress01) + 1);
        ringRenderer.enabled = true;
        ringRenderer.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float t = pointCount == 1 ? 0f : (float)i / (pointCount - 1);
            float angle01 = t * activeProgress01;
            ringRenderer.SetPosition(i, GetCirclePoint(angle01));
        }
    }

    private Vector3 GetCirclePoint(float angle01)
    {
        float angleRad = (-90f - (angle01 * 360f)) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angleRad) * ringRadius, Mathf.Sin(angleRad) * ringRadius, 0f);
    }

    private void HideRing()
    {
        scanVisible = false;
        activeMode = PacketScanVisualMode.None;

        if (ringRenderer != null)
        {
            ringRenderer.positionCount = 0;
            ringRenderer.enabled = false;
        }
    }

    private void ApplyRingColor(Color color)
    {
        if (ringRenderer == null)
            return;

        ringRenderer.startColor = color;
        ringRenderer.endColor = color;
    }

    private Color GetModeColor()
    {
        return activeMode switch
        {
            PacketScanVisualMode.QuickScan => quickScanColor,
            PacketScanVisualMode.DeepScan => deepScanColor,
            _ => Color.white
        };
    }

    private void ShowResult(string text, Color color)
    {
        if (resultLabel == null)
            return;

        resultPlaying = true;
        resultTimer = 0f;
        resultBaseColor = color;

        resultLabel.text = text;
        resultLabel.transform.localPosition = resultLocalOffset;
        resultLabel.color = color;
        SetResultAlpha(1f);
    }

    private void UpdateResultLabel()
    {
        if (!resultPlaying || resultLabel == null)
            return;

        resultTimer += Time.deltaTime;
        float t = Mathf.Clamp01(resultTimer / resultDuration);

        float y = Mathf.Lerp(0f, resultRiseDistance, t);
        resultLabel.transform.localPosition = resultLocalOffset + new Vector3(0f, -y, 0f);

        float alpha = 1f - t;
        SetResultAlpha(alpha);

        if (t >= 1f)
        {
            resultPlaying = false;
            resultLabel.text = "";
            SetResultAlpha(0f);
            resultLabel.transform.localPosition = resultLocalOffset;
        }
    }

    private void SetResultAlpha(float alpha)
    {
        if (resultLabel == null)
            return;

        Color c = resultLabel.color;
        c.a = alpha;
        resultLabel.color = c;
    }
}