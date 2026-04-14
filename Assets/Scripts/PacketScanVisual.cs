using UnityEngine;

public class PacketScanVisual : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private SpriteRenderer sweepRenderer;

    [Header("Rotation")]
    [SerializeField] private float scanSweepMinDegreesPerSecond = 130f;
    [SerializeField] private float scanSweepMidDegreesPerSecond = 180f;   // speed at midpoint confidence
    [SerializeField] private float scanSweepMaxDegreesPerSecond = 300f;
    [SerializeField, Range(0f, 1f)] private float scanSweepMidConfidence = 0.5f;
    [SerializeField] private float scanSweepLowCurveExponent = 1.0f;      // 0 -> midpoint
    [SerializeField] private float scanSweepHighCurveExponent = 1.4f;     // midpoint -> 100%
    [SerializeField] private float traceSweepDegreesPerSecond = 110f;
    [SerializeField] private float startAngleDegrees = 0f;

    [Header("Opacity")]
    [SerializeField] private float probableAlpha = 0.28f;
    [SerializeField] private float likelyAlpha = 0.40f;
    [SerializeField] private float confirmedAlpha = 0.55f;
    [SerializeField] private float traceAlpha = 0.30f;

    [Header("Scale")]
    [SerializeField] private Vector3 scanScale = Vector3.one;
    [SerializeField] private Vector3 traceScale = Vector3.one;

    private bool isVisible = false;
    private bool isTrace = false;
    private float sweepDegreesPerSecond = 180f;
    private float currentConfidence01 = 0f;
    private Color activeColor = Color.white;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (sweepRenderer != null)
        {
            baseScale = sweepRenderer.transform.localScale;
            sweepRenderer.enabled = false;
            sweepRenderer.color = Color.clear;
            sweepRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, startAngleDegrees);
        }
    }

    private void Update()
    {
        if (!isVisible || sweepRenderer == null || !sweepRenderer.enabled)
            return;

        float delta = sweepDegreesPerSecond * Time.deltaTime;
        sweepRenderer.transform.Rotate(0f, 0f, -delta, Space.Self);
    }

    public void ShowScan(Color color, ScanStage stage)
    {
        ShowScan(color, stage, 0f);
    }

    public void ShowScan(Color color, ScanStage stage, float confidence01)
    {
        if (sweepRenderer == null)
            return;

        isVisible = true;
        isTrace = false;
        activeColor = color;
        currentConfidence01 = Mathf.Clamp01(confidence01);
        sweepDegreesPerSecond = GetScanSweepDegreesPerSecond(currentConfidence01);

        sweepRenderer.enabled = true;
        sweepRenderer.transform.localScale = Vector3.Scale(baseScale, scanScale);

        Color c = activeColor;
        c.a = GetAlphaForStage(stage);
        sweepRenderer.color = c;
    }

    public void UpdateScanConfidence(float confidence01)
    {
        if (!isVisible || isTrace)
            return;

        currentConfidence01 = Mathf.Clamp01(confidence01);
        sweepDegreesPerSecond = GetScanSweepDegreesPerSecond(currentConfidence01);
    }

    public void ShowTrace(Color color)
    {
        if (sweepRenderer == null)
            return;

        isVisible = true;
        isTrace = true;
        sweepDegreesPerSecond = traceSweepDegreesPerSecond;
        activeColor = color;

        sweepRenderer.enabled = true;
        sweepRenderer.transform.localScale = Vector3.Scale(baseScale, traceScale);

        Color c = activeColor;
        c.a = traceAlpha;
        sweepRenderer.color = c;
    }

    public void Hide()
    {
        isVisible = false;
        isTrace = false;
        currentConfidence01 = 0f;

        if (sweepRenderer == null)
            return;

        sweepRenderer.enabled = false;
        sweepRenderer.color = Color.clear;
        sweepRenderer.transform.localScale = baseScale;
        sweepRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, startAngleDegrees);
    }

    private float GetScanSweepDegreesPerSecond(float confidence01)
    {
        confidence01 = Mathf.Clamp01(confidence01);

        float mid = Mathf.Clamp(scanSweepMidConfidence, 0.001f, 0.999f);

        if (confidence01 <= mid)
        {
            float t = confidence01 / mid;
            t = Mathf.Pow(t, Mathf.Max(0.01f, scanSweepLowCurveExponent));
            return Mathf.Lerp(scanSweepMinDegreesPerSecond, scanSweepMidDegreesPerSecond, t);
        }
        else
        {
            float t = (confidence01 - mid) / (1f - mid);
            t = Mathf.Pow(t, Mathf.Max(0.01f, scanSweepHighCurveExponent));
            return Mathf.Lerp(scanSweepMidDegreesPerSecond, scanSweepMaxDegreesPerSecond, t);
        }
    }

    private float GetAlphaForStage(ScanStage stage)
    {
        return stage switch
        {
            ScanStage.Probable => probableAlpha,
            ScanStage.Likely => likelyAlpha,
            ScanStage.Confirmed => confirmedAlpha,
            _ => probableAlpha
        };
    }

    public void SetSorting(int sortingLayerID, int sortingOrder)
    {
        if (sweepRenderer == null)
            return;

        sweepRenderer.sortingLayerID = sortingLayerID;
        sweepRenderer.sortingOrder = sortingOrder;
    }
}