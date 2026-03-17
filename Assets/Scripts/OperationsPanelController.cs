using TMPro;
using UnityEngine;

public class OperationsPanelController : MonoBehaviour
{
    public TMP_Text operationsText;
    public CommandDirector commandDirector;

    private void Update()
    {
        if (operationsText == null || commandDirector == null)
            return;

        operationsText.text = commandDirector.GetOperationsDisplayText();
    }
}