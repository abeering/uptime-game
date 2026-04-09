using System.Text;
using TMPro;
using UnityEngine;

public class CleanMinigameOverlayView : MonoBehaviour
{
    [Header("References")]
    public RectTransform rootTransform;
    public TMP_Text titleText;
    public TMP_Text minigameText;
    public TMP_Text promptText;
    public TMP_InputField inputField;

    [Header("Placement")]
    public Vector3 worldOffset = new Vector3(1.5f, 0.5f, 0f);

    [Header("Runtime")]
    public CleanDirector cleanDirector;

    private void Awake()
    {
        if (rootTransform == null)
            rootTransform = transform as RectTransform;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ShowSplash(CleanSession session)
    {
        if (session == null)
            return;

        if (titleText != null)
            titleText.text = $"clean {session.node.nodeId}";

        if (minigameText != null)
            minigameText.text = BuildSplashText(session);

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        if (inputField != null)
        {
            inputField.gameObject.SetActive(false);
            inputField.text = string.Empty;
        }
    }

    public void ShowMinigame(CleanSession session)
    {
        if (session == null)
            return;

        if (titleText != null)
            titleText.text = session.minigameType.ToString();

        if (minigameText != null)
            minigameText.text = BuildMinigameText(session);

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "terminate >";
        }

        if (inputField != null)
        {
            inputField.gameObject.SetActive(true);
            inputField.text = string.Empty;
            inputField.Select();
            inputField.ActivateInputField();
        }
    }

    public void ShowResult(CleanSession session)
    {
        if (session == null)
            return;

        if (titleText != null)
            titleText.text = "clean result";

        if (minigameText != null)
            minigameText.text = session.resultMessage;

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.gameObject.SetActive(false);
        }
    }

    public void SubmitCurrentInput()
    {
        if (inputField == null || cleanDirector == null)
            return;

        string raw = inputField.text;
        cleanDirector.SubmitInput(raw);

        inputField.text = string.Empty;
    }

    public void HandleEndEdit(string _)
    {
        SubmitCurrentInput();
    }

    private string BuildMinigameText(CleanSession session)
    {
        if (session == null)
            return string.Empty;

        switch (session.minigameType)
        {
            case CleanMinigameType.ProcessKiller:
                return BuildProcessKillerText(session);

            default:
                return "unsupported minigame";
        }
    }

    private string BuildProcessKillerText(CleanSession session)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("PID   CPU   NAME");

        for (int i = 0; i < session.processEntries.Count; i++)
        {
            ProcessKillerEntry entry = session.processEntries[i];
            sb.AppendLine($"{entry.pid}  {entry.cpuPercent:00}%   {entry.processName}");
        }

        return sb.ToString().TrimEnd();
    }

    public void PositionNearNode(NodeView node)
    {
        if (node == null || rootTransform == null)
            return;

        rootTransform.position = node.transform.position + worldOffset;
    }

    private string BuildSplashText(CleanSession session)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"> ssh {session.node.nodeId}.local");
        sb.AppendLine("connecting...");
        sb.AppendLine("auth ok");
        sb.AppendLine("mounting /proc...");
        sb.AppendLine("scanning anomalies...");
        sb.AppendLine();
        sb.AppendLine($"detected infection: {session.infectionType.ToString().ToUpperInvariant()}");
        sb.AppendLine($"cleanup module: {session.minigameType.ToString().ToUpperInvariant()}");
        sb.AppendLine($"difficulty: {GetDifficultyLabel(session.difficultyTier)}");
        sb.AppendLine("modifiers: NONE");
        sb.AppendLine();
        sb.AppendLine("stand by...");

        return sb.ToString();
    }

    private string GetDifficultyLabel(int difficultyTier)
    {
        return difficultyTier switch
        {
            <= 1 => "EASY",
            2 => "MEDIUM",
            _ => "HARD"
        };
    }
}