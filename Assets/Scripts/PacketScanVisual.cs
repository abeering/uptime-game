using UnityEngine;
using TMPro;

public class PacketScanVisual : MonoBehaviour
{
    [Header("Ring")]
    public LineRenderer ringRenderer;
    [Min(8)] public int ringSegments = 48;

    [Header("Scan Color")]
    public Color scanColor = Color.green;

    [Header("Stage Width")]
    public float probableWidth = 0.025f;
    public float likelyWidth = 0.045f;
    public float confirmedWidth = 0.065f;

    [Header("Stage Radius")]
    public float probableRadius = 0.90f;
    public float likelyRadius = 0.72f;
    public float confirmedRadius = 0.56f;

    [Header("Animation")]
    public float progressUnitsPerSecond = 2.5f;
    public float radiusUnitsPerSecond = 3.5f;
    public float widthUnitsPerSecond = 0.20f;

    [Header("Completion")]
    public float completeHoldDuration = 0.5f;
    public float completePulseSpeed = 8f;
    public float completePulseAmount = 0.12f;

    [Header("Colors")]
    public Color failedColor = Color.red;

    [Header("Result Label")]
    public TextMeshPro resultLabel;
    public Vector3 resultLocalOffset = new Vector3(0f, -0.42f, 0f);
    public float resultRiseDistance = 0.08f;
    public float resultDuration = 1.2f;

    private bool scanVisible = false;

    private float activeProgress01 = 0f;
    private float displayedProgress01 = 0f;
    private float targetProgress01 = 0f;

    private float displayedRadius = 0.90f;
    private float targetRadius = 0.90f;

    private float displayedWidth = 0.025f;
    private float targetWidth = 0.025f;

    private bool isHoldingComplete = false;
    private float completeHoldTimer = 0f;
    private string pendingResultText = "";
    private Color pendingResultColor = Color.white;

    private bool resultPlaying = false;
    private float resultTimer = 0f;
    private Vector3 resultStartLocalPos;
    private Color resultBaseColor;
    private ScanStage lastShownStage = ScanStage.Unknown;

    private void Awake()
    {
        if (ringRenderer != null)
        {
            ringRenderer.loop = false;
            ringRenderer.useWorldSpace = false;
            ringRenderer.startWidth = probableWidth;
            ringRenderer.endWidth = probableWidth;
            ringRenderer.widthMultiplier = 1f;
            ringRenderer.positionCount = 0;
            ringRenderer.enabled = false;
            displayedRadius = probableRadius;
            targetRadius = probableRadius;
            displayedWidth = probableWidth;
            targetWidth = probableWidth;
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
        UpdateRingAnimation();
        UpdateResultLabel();
    }

    public void ShowProgressiveScan(ScanStage stage, float progress01)
    {
        scanVisible = true;
        isHoldingComplete = false;
        completeHoldTimer = 0f;

        pendingResultText = "";
        pendingResultColor = Color.white;

        if (stage != lastShownStage)
        {
            displayedProgress01 = 0f;
            lastShownStage = stage;
        }

        targetRadius = GetRadiusForStage(stage);
        targetWidth = GetWidthForStage(stage);
        ApplyRingColor(scanColor);

        targetProgress01 = Mathf.Clamp01(progress01);

        if (ringRenderer != null)
        {
            ringRenderer.enabled = true;
        }

        // if (displayedProgress01 <= 0.001f)
        //     displayedProgress01 = targetProgress01;

        SetRingProgress(displayedProgress01);
    }

    public void SetScanProgress(float progress01)
    {
        targetProgress01 = Mathf.Clamp01(progress01);

        if (ringRenderer != null && !ringRenderer.enabled)
            ringRenderer.enabled = true;

        scanVisible = true;
    }

    public void CompleteScan(string text, Color color)
    {
        scanVisible = true;

        displayedProgress01 = 1f;
        targetProgress01 = 1f;
        activeProgress01 = 1f;
        displayedRadius = confirmedRadius;
        targetRadius = confirmedRadius;

        displayedWidth = confirmedWidth;
        targetWidth = confirmedWidth;

        ApplyRingColor(scanColor);
        SetRingProgress(1f);

        pendingResultText = text;
        pendingResultColor = color;
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

    public void HideScanVisual()
    {
        HideRing();
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

            SetRingProgress(1f);

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

        displayedProgress01 = Mathf.MoveTowards(
            displayedProgress01,
            targetProgress01,
            progressUnitsPerSecond * Time.deltaTime
        );

        displayedRadius = Mathf.MoveTowards(
            displayedRadius,
            targetRadius,
            radiusUnitsPerSecond * Time.deltaTime
        );

        displayedWidth = Mathf.MoveTowards(
            displayedWidth,
            targetWidth,
            widthUnitsPerSecond * Time.deltaTime
        );

        ringRenderer.startWidth = displayedWidth;
        ringRenderer.endWidth = displayedWidth;

        SetRingProgress(displayedProgress01);
    }

    private void SetRingProgress(float progress01)
    {
        activeProgress01 = Mathf.Clamp01(progress01);

        if (ringRenderer == null)
            return;

        if (!ringRenderer.enabled)
            ringRenderer.enabled = true;

        if (activeProgress01 <= 0f)
        {
            ringRenderer.positionCount = 2;
            Vector3 start = GetCirclePoint(0f, displayedRadius);
            ringRenderer.SetPosition(0, start);
            ringRenderer.SetPosition(1, start);
            return;
        }

        int pointCount = Mathf.Max(2, Mathf.CeilToInt(ringSegments * activeProgress01) + 1);
        ringRenderer.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float t = pointCount <= 1 ? 0f : (float)i / (pointCount - 1);
            float angle01 = t * activeProgress01;
            ringRenderer.SetPosition(i, GetCirclePoint(angle01, displayedRadius));
        }
    }

    private Vector3 GetCirclePoint(float angle01, float radius)
    {
        float angleRad = (90f - (angle01 * 360f)) * Mathf.Deg2Rad;
        return new Vector3(
            Mathf.Cos(angleRad) * radius,
            Mathf.Sin(angleRad) * radius,
            -0.1f
        );
    }

    private float GetRadiusForStage(ScanStage stage)
    {
        return stage switch
        {
            ScanStage.Probable => probableRadius,
            ScanStage.Likely => likelyRadius,
            ScanStage.Confirmed => confirmedRadius,
            _ => probableRadius
        };
    }

    private float GetWidthForStage(ScanStage stage)
    {
        return stage switch
        {
            ScanStage.Probable => probableWidth,
            ScanStage.Likely => likelyWidth,
            ScanStage.Confirmed => confirmedWidth,
            _ => probableWidth
        };
    }

    private void HideRing()
    {
        scanVisible = false;
        isHoldingComplete = false;
        completeHoldTimer = 0f;

        displayedProgress01 = 0f;
        targetProgress01 = 0f;
        activeProgress01 = 0f;

        displayedRadius = probableRadius;
        targetRadius = probableRadius;

        displayedWidth = probableWidth;
        targetWidth = probableWidth;

        lastShownStage = ScanStage.Unknown;

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