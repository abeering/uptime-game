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

        if (titleText != null)
            titleText.text =
                $"intel  S {scanDirector.GetActiveScanCount()} / {scanDirector.maxActiveScans}   T {scanDirector.GetActiveTraceCount()} / {scanDirector.maxActiveTraces}";
            

        StringBuilder sb = new StringBuilder();
        scanDirector.AppendScanPanel(sb);
        scanText.text = sb.ToString();
    }

}