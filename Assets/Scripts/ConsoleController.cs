using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ConsoleController : MonoBehaviour
{
    [Header("References")]
    public TMP_InputField inputField;
    public TMP_Text historyText;
    public CommandDirector commandDirector;
    public ScrollRect historyScrollRect;

    [Header("Settings")]
    public int maxHistoryLines = 30;

    private readonly List<string> historyLines = new();

    private void Start()
    {
        if (commandDirector != null)
            commandDirector.OnLogMessage += HandleCommandLog;

        AppendLine("console ready");
        FocusInput();
    }

    private void OnDestroy()
    {
        if (commandDirector != null)
            commandDirector.OnLogMessage -= HandleCommandLog;
    }

    public void SubmitCurrentInput()
    {
        if (inputField == null || commandDirector == null)
            return;

        string raw = inputField.text;

        if (string.IsNullOrWhiteSpace(raw))
        {
            FocusInput();
            return;
        }

        AppendLine($"> {raw}");

        ParsedCommand parsed = CommandParser.Parse(raw);
        commandDirector.Execute(parsed);

        inputField.text = "";
        FocusInput();
    }

    private void HandleCommandLog(string message)
    {
        AppendLine(message);
    }

    private void AppendLine(string line)
    {
        historyLines.Add(line);

        while (historyLines.Count > maxHistoryLines)
            historyLines.RemoveAt(0);

        if (historyText != null)
            historyText.text = string.Join("\n", historyLines);

        if (historyScrollRect != null && historyText != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyText.rectTransform);
            Canvas.ForceUpdateCanvases();
            historyScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void FocusInput()
    {
        if (inputField == null)
            return;

        inputField.ActivateInputField();
        inputField.Select();
    }

    public void HandleEndEdit(string _)
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.enterKey.isPressed || Keyboard.current.numpadEnterKey.isPressed)
        {
            SubmitCurrentInput();
        }
    }
}