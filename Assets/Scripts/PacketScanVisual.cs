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

    [Header("Completion")]
    public float completeHoldDuration = 0.5f;
    public float completePulseSpeed = 8f;
    public float completePulseAmount = 0.12f;

    private bool isHoldingComplete = false;
    private float completeHoldTimer = 0f;
    private string pendingResultText = "";
    private Color pendingResultColor = Color.white;

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

    private float displayedProgress01 = 0f;
    private float targetProgress01 = 0f;
    public float fillLerpSpeed = 8f;

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
        displayedProgress01 = Mathf.Lerp(
            displayedProgress01,
            targetProgress01,
            Time.deltaTime * fillLerpSpeed
        );

        if (scanVisible)
            SetRingProgress(displayedProgress01);

        UpdateRingAnimation();
        UpdateResultLabel();
    }

    private void UpdateRingAnimation()
    {
        if (ringRenderer == null)
            return;

        if (isHoldingComplete)
        {
            completeHoldTimer += Time.deltaTime;

            float pulse = 1f + Mathf.Sin(Time.time * completePulseSpeed) * completePulseAmount;
            ringRenderer.widthMultiplier = pulse;

            if (completeHoldTimer >= completeHoldDuration)
            {
                isHoldingComplete = false;
                ringRenderer.widthMultiplier = 1f;
                HideRing();
                ShowResult(pendingResultText, pendingResultColor);
            }

            return;
        }

        ringRenderer.widthMultiplier = 1f;

        if (!scanVisible)
            return;

        displayedProgress01 = Mathf.Lerp(
            displayedProgress01,
            targetProgress01,
            Time.deltaTime * fillLerpSpeed
        );

        SetRingProgress(displayedProgress01);
    }

    public void BeginQuickScan()
    {
        activeMode = PacketScanVisualMode.QuickScan;
        displayedProgress01 = 0f;
        targetProgress01 = 0f;
        scanVisible = true;
        isHoldingComplete = false;
        completeHoldTimer = 0f;
        pendingResultText = "";
        pendingResultColor = Color.white;
        ApplyRingColor(quickScanColor);
        SetRingProgress(0f);
    }

    public void BeginDeepScan()
    {
        activeMode = PacketScanVisualMode.DeepScan;
        SetRingProgress(0f);
        scanVisible = true;
        isHoldingComplete = false;
        completeHoldTimer = 0f;
        pendingResultText = "";
        pendingResultColor = Color.white;
        ApplyRingColor(deepScanColor);
    }

    public void CompleteScan(string text)
    {
        Color resultColor = GetModeColor();

        displayedProgress01 = 1f;
        targetProgress01 = 1f;
        activeProgress01 = 1f;

        SetRingProgress(1f);

        pendingResultText = text;
        pendingResultColor = resultColor;
        isHoldingComplete = true;
        completeHoldTimer = 0f;
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

    public void SetScanProgress(float progress01)
    {
        targetProgress01 = Mathf.Clamp01(progress01);
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
        float angleRad = (90f - (angle01 * 360f)) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angleRad) * ringRadius, Mathf.Sin(angleRad) * ringRadius, -0.1f);
    }

    private void HideRing()
    {
        scanVisible = false;
        activeMode = PacketScanVisualMode.None;
        isHoldingComplete = false;
        completeHoldTimer = 0f;

        displayedProgress01 = 0f;
        targetProgress01 = 0f;
        activeProgress01 = 0f;

        if (ringRenderer != null)
        {
            ringRenderer.widthMultiplier = 1f;
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