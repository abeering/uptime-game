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
    public ScoreDirector scoreDirector;

    private void Update()
    {
        if (operationsText == null || commandDirector == null || networkRuntime == null || scanDirector == null)
            return;

        StringBuilder sb = new StringBuilder();

        scanDirector.AppendKnownThreatsSection(sb, networkRuntime);
        sb.AppendLine();

        commandDirector.AppendOperationsPanel(sb);
        sb.AppendLine();

        scoreDirector.AppendOperationsPanel(sb);
        
        operationsText.text = sb.ToString();
    }

}