using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScanPanelController : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text titleText;

    [Header("Legacy Text Renderer")]
    [Tooltip("Optional. Leave assigned during migration if you want; this script will disable it.")]
    public TMP_Text scanText;

    [Header("Row Rendering")]
    public Transform rowContainer;
    public ScanRowView rowPrefab;

    [Header("Data")]
    public ScanDirector scanDirector;

    private readonly List<ScanRowView> rowViews = new();

    private void Awake()
    {
        if (scanText != null)
            scanText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (scanDirector == null)
            return;

        UpdateTitle();
        UpdateRows();
    }

    private void UpdateTitle()
    {
        if (titleText == null)
            return;

        int scans = scanDirector.GetActiveScanCount();
        int maxScans = scanDirector.maxActiveScans;

        int traces = scanDirector.GetActiveTraceCount();
        int maxTraces = scanDirector.maxActiveTraces;

        string scanColor = scans >= maxScans ? "#FF7373" : "#66FF66";
        string traceColor = traces >= maxTraces ? "#FF7373" : "#66CCFF";

        titleText.text =
            $"INTEL  S <color={scanColor}>{scans} / {maxScans}</color>   T <color={traceColor}>{traces} / {maxTraces}</color>";
    }

    private void UpdateRows()
    {
        if (rowContainer == null || rowPrefab == null)
            return;

        List<ScanDirector.ScanPanelRowData> rows = scanDirector.GetScanPanelRows();

        EnsureRowCount(rows.Count);

        for (int i = 0; i < rowViews.Count; i++)
        {
            bool active = i < rows.Count;
            rowViews[i].gameObject.SetActive(active);

            if (active)
                rowViews[i].Render(rows[i]);
        }
    }

    private void EnsureRowCount(int count)
    {
        while (rowViews.Count < count)
        {
            ScanRowView row = Instantiate(rowPrefab, rowContainer);
            rowViews.Add(row);
        }
    }
}