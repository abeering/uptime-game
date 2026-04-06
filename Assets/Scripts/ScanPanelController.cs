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
            titleText.text = $"scans {scanDirector.GetActiveScanCount()} / {scanDirector.maxActiveScans}";

        StringBuilder sb = new StringBuilder();
        scanDirector.AppendScanPanel(sb);
        scanText.text = sb.ToString();
    }
    
}