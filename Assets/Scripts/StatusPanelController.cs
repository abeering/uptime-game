using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelDirector levelDirector;

    [Header("Flow")]
    [SerializeField] private TMP_Text flowLabelText;
    [SerializeField] private Image[] flowSegments;
    [SerializeField] private TMP_Text flowStateText;
    [SerializeField] private TMP_Text failureText;

    [Header("Core")]
    [SerializeField] private TMP_Text coreLabelText;
    [SerializeField] private TMP_Text coreStatusText;

    [Header("Colors")]
    [SerializeField] private Color stableColor = new Color32(120, 200, 90, 255);
    [SerializeField] private Color elevatedColor = new Color32(210, 210, 80, 255);
    [SerializeField] private Color dangerColor = new Color32(235, 140, 50, 255);
    [SerializeField] private Color criticalColor = new Color32(220, 70, 50, 255);
    [SerializeField] private Color failedColor = new Color32(170, 40, 40, 255);
    [SerializeField] private Color segmentOffColor = new Color32(60, 40, 40, 255);

    private void Awake()
    {
        if (flowLabelText != null)
            flowLabelText.text = "FLOW";

        if (coreLabelText != null)
            coreLabelText.text = "CORE";
    }

    private void Update()
    {
        if (levelDirector == null)
            return;

        Refresh();
    }

    private void Refresh()
    {
        float frac = levelDirector.TrafficLossFraction;
        LevelFlowState flowState = levelDirector.FlowState;

        RenderFlowSegments(frac);

        Color flowColor = GetFlowColor(flowState);

        if (flowStateText != null)
        {
            flowStateText.text = flowState == LevelFlowState.Stable
                ? ""
                : GetFlowStateLabel(flowState);

            flowStateText.color = flowColor;
        }

        bool failed = levelDirector.LevelFailed;

        if (failureText != null)
        {
            failureText.gameObject.SetActive(failed);

            if (failed)
            {
                if (levelDirector.FailureReason == LevelFailureReason.CoreNodeCompromised &&
                    levelDirector.FailedCoreNode != null)
                {
                    failureText.text = $"FAILED: CORE LOST ({levelDirector.FailedCoreNode.nodeId.ToUpper()})";
                }
                else
                {
                    failureText.text = "FAILED: FLOW COLLAPSE";
                }

                failureText.color = failedColor;
            }
        }

        RenderCoreStatus();
    }

    private void RenderFlowSegments(float frac)
    {
        if (flowSegments == null || flowSegments.Length == 0)
            return;

        int litCount = Mathf.Clamp(
            Mathf.CeilToInt(frac * flowSegments.Length),
            0,
            flowSegments.Length
        );

        for (int i = 0; i < flowSegments.Length; i++)
        {
            Image segment = flowSegments[i];
            if (segment == null)
                continue;

            if (i >= litCount)
            {
                segment.color = segmentOffColor;
                continue;
            }

            float t = flowSegments.Length <= 1
                ? 1f
                : (float)i / (flowSegments.Length - 1);

            if (t < 0.5f)
            {
                float localT = t / 0.5f;
                segment.color = Color.Lerp(stableColor, elevatedColor, localT);
            }
            else
            {
                float localT = (t - 0.5f) / 0.5f;
                segment.color = Color.Lerp(dangerColor, criticalColor, localT);
            }
        }
    }

    private void RenderCoreStatus()
    {
        if (coreStatusText == null)
            return;

        bool dbOk = true;
        bool authOk = true;
        bool rootOk = true;

        if (levelDirector.LevelFailed &&
            levelDirector.FailureReason == LevelFailureReason.CoreNodeCompromised &&
            levelDirector.FailedCoreNode != null)
        {
            string failedId = levelDirector.FailedCoreNode.nodeId.ToLower();

            if (failedId == "db")
                dbOk = false;
            else if (failedId == "auth")
                authOk = false;
            else if (failedId == "root")
                rootOk = false;
        }

        string db = dbOk ? "[DB:OK]" : "[DB:XX]";
        string auth = authOk ? "[AUTH:OK]" : "[AUTH:XX]";
        string root = rootOk ? "[ROOT:OK]" : "[ROOT:XX]";

        coreStatusText.text = $"{db}   {auth}   {root}";
    }

    private Color GetFlowColor(LevelFlowState state)
    {
        switch (state)
        {
            case LevelFlowState.Elevated:
                return elevatedColor;
            case LevelFlowState.Danger:
                return dangerColor;
            case LevelFlowState.Critical:
                return criticalColor;
            case LevelFlowState.Failed:
                return failedColor;
            default:
                return stableColor;
        }
    }

    private string GetFlowStateLabel(LevelFlowState state)
    {
        switch (state)
        {
            case LevelFlowState.Elevated:
                return "ELEVATED";
            case LevelFlowState.Danger:
                return "DANGER";
            case LevelFlowState.Critical:
                return "CRITICAL";
            case LevelFlowState.Failed:
                return "FAILED";
            default:
                return "STABLE";
        }
    }
}