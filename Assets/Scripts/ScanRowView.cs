using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScanRowView : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text warningText;
    public TMP_Text slotText;
    public TMP_Text packetIdText;
    public TMP_Text difficultyText;
    public TMP_Text percentText;
    public TMP_Text etaText;
    public TMP_Text stageText;
    public TMP_Text readoutText;

    [Header("Packet Icon")]
    public Image packetBody;
    public Image packetBorder;

    [Header("Fallback Colors")]
    public Color unknownColor = new(0.53f, 0.53f, 0.53f, 1f);
    public Color benignColor = new(0.72f, 1.00f, 0.72f, 1f);
    public Color threatColor = new(1.00f, 0.42f, 0.42f, 1f);
    public Color priorityColor = new(0.40f, 0.80f, 1.00f, 1f);
    public Color traceColor = new(0.95f, 0.45f, 1.00f, 1f);
    public Color mutedColor = new(0.67f, 0.67f, 0.67f, 0.53f);

    [Header("Dim")]
    [Range(0f, 1f)] public float emptyAlpha = 0.35f;
    [Range(0f, 1f)] public float bodyAlpha = 0.45f;

    public void Render(ScanDirector.ScanPanelRowData row)
    {
        if (row == null)
        {
            RenderEmpty("?");
            return;
        }

        bool isEmpty =
            row.state == ScanDirector.ScanPanelRowState.EmptyScan ||
            row.state == ScanDirector.ScanPanelRowState.EmptyTrace;

        Color rowColor = row.slotColor;
        Color packetColor = GetPacketColor(row);

        SetText(warningText, row.willBeDropped ? "!" : "");
        SetText(slotText, row.slotLabel);
        SetText(packetIdText, row.packetId);
        SetText(difficultyText, row.difficultyText);
        SetText(percentText, row.percentText);
        SetText(etaText, row.showDone ? "DONE" : row.etaText);
        SetText(stageText, row.stageText);
        SetText(readoutText, row.readoutText);

        SetTextColor(slotText, rowColor);
        SetTextColor(warningText, threatColor);

        float alpha = isEmpty ? emptyAlpha : 1f;
        ApplyAlpha(alpha);

        if (packetBody != null)
        {
            Color c = packetColor;
            c.a = isEmpty ? emptyAlpha : bodyAlpha;
            packetBody.color = c;
        }

        if (packetBorder != null)
        {
            Color c = packetColor;
            c.a = isEmpty ? emptyAlpha : 1f;
            packetBorder.color = c;
        }
    }

    public void RenderEmpty(string slotLabel)
    {
        Render(new ScanDirector.ScanPanelRowData
        {
            state = ScanDirector.ScanPanelRowState.EmptyScan,
            slotLabel = slotLabel,
            packetId = "--",
            difficultyText = "--",
            percentText = "--%",
            etaText = "--",
            stageText = "empty",
            readoutText = "",
            visibleClass = VisibleClass.Unknown,
            slotColor = mutedColor
        });
    }

    private Color GetPacketColor(ScanDirector.ScanPanelRowData row)
    {
        bool isTrace =
            row.state == ScanDirector.ScanPanelRowState.ActiveTrace ||
            row.state == ScanDirector.ScanPanelRowState.CompletedTraceLinger ||
            row.state == ScanDirector.ScanPanelRowState.EmptyTrace;

        if (isTrace)
            return traceColor;

        return row.visibleClass switch
        {
            VisibleClass.Benign => benignColor,
            VisibleClass.Threat => threatColor,
            VisibleClass.Priority => priorityColor,
            _ => unknownColor
        };
    }

    private void ApplyAlpha(float alpha)
    {
        ApplyTextAlpha(warningText, alpha);
        ApplyTextAlpha(slotText, alpha);
        ApplyTextAlpha(packetIdText, alpha);
        ApplyTextAlpha(difficultyText, alpha);
        ApplyTextAlpha(percentText, alpha);
        ApplyTextAlpha(etaText, alpha);
        ApplyTextAlpha(stageText, alpha);
        ApplyTextAlpha(readoutText, alpha);
    }

    private void ApplyTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
            return;

        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? "";
    }

    private void SetTextColor(TMP_Text text, Color color)
    {
        if (text != null)
            text.color = color;
    }
}