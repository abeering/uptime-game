using System;
using TMPro;
using UnityEngine;
using System.Text;

public class OperationsPanelController : MonoBehaviour
{
    public TMP_Text operationsText;
    public CommandDirector commandDirector;
    public NetworkRuntime networkRuntime;
    public ScanDirector scanDirector;

    private void Update()
    {
        if (operationsText == null || commandDirector == null || networkRuntime == null || scanDirector == null)
            return;

        StringBuilder sb = new StringBuilder();
        commandDirector.AppendOperationsPanel(sb);
        sb.AppendLine();
        scanDirector.AppendKnownThreatsSection(sb, networkRuntime);
        operationsText.text = sb.ToString();
    }
}