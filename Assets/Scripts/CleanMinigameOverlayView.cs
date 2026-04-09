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

    private Canvas parentCanvas;

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

    public void ShowMinigameStub(CleanSession session)
    {
        if (session == null)
            return;

        if (titleText != null)
            titleText.text = session.minigameType.ToString();

        if (minigameText != null)
        {
            minigameText.text =
                "PID   CPU   NAME\n" +
                "3812  12%   syncd\n" +
                "4421  87%   minerd\n" +
                "1022  08%   auth\n" +
                "7710  23%   cache";
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "terminate pid >";
        }

        if (inputField != null)
        {
            inputField.gameObject.SetActive(true);
            inputField.text = string.Empty;
            inputField.Select();
            inputField.ActivateInputField();
        }
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