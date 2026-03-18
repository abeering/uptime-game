using System;
using TMPro;
using UnityEngine;
using System.Text;


public class OperationsPanelController : MonoBehaviour
{
    public TMP_Text operationsText;
    public CommandDirector commandDirector;
    public NetworkRuntime networkRuntime;

    private void Update()
    {
        if (operationsText == null || commandDirector == null || networkRuntime == null)
            return;

        StringBuilder sb = new StringBuilder();
        commandDirector.AppendOperationsPanel(sb);
        sb.AppendLine();
        networkRuntime.AppendOperationsPanel(sb);

        operationsText.text = sb.ToString();
    }
}