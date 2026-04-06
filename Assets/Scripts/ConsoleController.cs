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

    [Header("Expand / Collapse")]
    public RectTransform consoleCanvas;
    public float collapsedHeight = 200f;
    public float expandedHeight = 420f;

    [Header("Scan Panel Docking")]
    public RectTransform scanPanelCanvas;
    public float scanPanelGap = 0f;

    [Header("Title Bar")]
    public TMP_Text titleText;

    private bool isExpanded = false;

    private readonly List<string> historyLines = new();

    private void Start()
    {
        if (commandDirector != null)
            commandDirector.OnLogMessage += HandleCommandLog;

        ApplyConsoleHeight();
        UpdateTitle();
        AppendLine("CONSOLE ready");
        FocusInput();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleConsoleExpanded();
        }
    }

    private void OnDestroy()
    {
        if (commandDirector != null)
            commandDirector.OnLogMessage -= HandleCommandLog;
    }

    private void ToggleConsoleExpanded()
    {
        isExpanded = !isExpanded;
        ApplyConsoleHeight();
        UpdateTitle();
        RefreshHistoryScrollToBottom();
        FocusInput();
    }

    private void ApplyConsoleHeight()
    {
        if (consoleCanvas == null)
            return;

        float targetHeight = isExpanded ? expandedHeight : collapsedHeight;
        consoleCanvas.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

        UpdateScanPanelDock(targetHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(consoleCanvas);

        if (scanPanelCanvas != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scanPanelCanvas);

        Canvas.ForceUpdateCanvases();
    }

    private void UpdateScanPanelDock(float consoleHeight)
    {
        if (scanPanelCanvas == null || consoleCanvas == null)
            return;

        float scaledConsoleHeight = consoleHeight * consoleCanvas.localScale.y;
        float scaledGap = scanPanelGap * consoleCanvas.localScale.y;

        Vector3 pos = scanPanelCanvas.localPosition;
        pos.x = consoleCanvas.localPosition.x;
        pos.y = consoleCanvas.localPosition.y + scaledConsoleHeight + scaledGap;
        scanPanelCanvas.localPosition = pos;
    }

    private void UpdateTitle()
    {
        if (titleText == null)
            return;

        string arrow = isExpanded ? "▲" : "▼";
        titleText.text = $"{arrow} console";
        
    }

    private void RefreshHistoryScrollToBottom()
    {
        if (historyScrollRect == null || historyText == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(historyText.rectTransform);
        Canvas.ForceUpdateCanvases();
        historyScrollRect.verticalNormalizedPosition = 0f;
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

        RefreshHistoryScrollToBottom();
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