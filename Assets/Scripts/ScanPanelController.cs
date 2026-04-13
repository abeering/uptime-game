using System.Text;
using TMPro;
using UnityEngine;

public class ScanPanelController : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text scanText;
    public ScanDirector scanDirector;

    private void Update()
    {
        if (scanText == null || scanDirector == null)
            return;

        int scans = scanDirector.GetActiveScanCount();
        int maxScans = scanDirector.maxActiveScans;

        int traces = scanDirector.GetActiveTraceCount();
        int maxTraces = scanDirector.maxActiveTraces;

        string scanColor = scans >= maxScans ? "#FF7373" : "#66FF66";
        string traceColor = traces >= maxTraces ? "#FF7373" : "#66CCFF";

        titleText.text =
            $"INTEL  S <color={scanColor}>{scans} / {maxScans}</color>   T <color={traceColor}>{traces} / {maxTraces}</color>";

        StringBuilder sb = new StringBuilder();
        scanDirector.AppendScanPanel(sb);
        scanText.text = sb.ToString();
    }

}