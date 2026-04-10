using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class CleanMinigameOverlayView : MonoBehaviour
{
    [Header("References")]
    public RectTransform rootTransform;
    public TMP_Text titleText;
    public TMP_Text minigameText;
    public TMP_InputField inputField;

    [Header("Splash Animation")]
    public float splashLineDelay = 0.18f;
    public float splashTypedCharDelay = 0.025f;
    public bool animateSplash = true;

    private Coroutine splashRoutine;

    [Header("Placement")]
    public Vector3 worldOffset = new Vector3(1.5f, 0.5f, 0f);

    [Header("Runtime")]
    public CleanDirector cleanDirector;

    private string currentPromptPrefix = string.Empty;
    private bool suppressInputChange = false;

    private void Awake()
    {
        if (rootTransform == null)
            rootTransform = transform as RectTransform;

        if (inputField != null)
            inputField.onValueChanged.AddListener(HandleInputValueChanged);
    }

    private void OnDestroy()
    {
        if (inputField != null)
            inputField.onValueChanged.RemoveListener(HandleInputValueChanged);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (splashRoutine != null)
        {
            StopCoroutine(splashRoutine);
            splashRoutine = null;
        }

        if (inputField != null)
            inputField.DeactivateInputField();

        gameObject.SetActive(false);
        currentPromptPrefix = string.Empty;
    }

    public bool WantsFocus()
    {
        return gameObject.activeInHierarchy
            && inputField != null
            && inputField.gameObject.activeInHierarchy;
    }

    public void ForceFocus()
    {
        if (inputField == null || !inputField.gameObject.activeInHierarchy)
            return;

        inputField.ActivateInputField();
        inputField.Select();

        int end = inputField.text != null ? inputField.text.Length : 0;
        inputField.caretPosition = end;
        inputField.stringPosition = end;
        inputField.selectionAnchorPosition = end;
        inputField.selectionFocusPosition = end;

        ClampCaretToEditableRegion();
    }

    public void ShowSplash(CleanSession session)
    {
        if (session == null)
            return;

        if (titleText != null)
            titleText.text = $"clean {session.node.nodeId}";

        if (inputField != null)
        {
            inputField.gameObject.SetActive(false);
            inputField.text = string.Empty;
        }

        if (minigameText != null)
        {
            if (splashRoutine != null)
                StopCoroutine(splashRoutine);

            minigameText.text = string.Empty;

            if (animateSplash)
                splashRoutine = StartCoroutine(AnimateSplashText(session));
            else
                minigameText.text = BuildSplashText(session);
        }

        currentPromptPrefix = string.Empty;
    }

    public void ShowMinigame(CleanSession session)
    {
        if (session == null)
            return;

        if (splashRoutine != null)
        {
            StopCoroutine(splashRoutine);
            splashRoutine = null;
        }

        if (titleText != null)
            titleText.text = session.minigameType.ToString();

        if (minigameText != null)
            minigameText.text = BuildMinigameText(session);

        currentPromptPrefix = "terminate > ";

        if (inputField != null)
        {
            inputField.gameObject.SetActive(true);
            SetInputTextPreservingPrompt(string.Empty);
        }

        ForceFocus();
    }

    public void ShowResult(CleanSession session)
    {
        if (session == null)
            return;

        if (splashRoutine != null)
        {
            StopCoroutine(splashRoutine);
            splashRoutine = null;
        }

        if (titleText != null)
            titleText.text = "clean result";

        if (minigameText != null)
            minigameText.text = session.resultMessage;

        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.gameObject.SetActive(false);
        }

        currentPromptPrefix = string.Empty;
    }

    public void SubmitCurrentInput()
    {
        if (inputField == null || cleanDirector == null)
            return;

        string raw = GetSubmittedInput();

        cleanDirector.SubmitInput(raw);

        if (inputField.gameObject.activeInHierarchy)
            SetInputTextPreservingPrompt(string.Empty);
    }

    public void HandleEndEdit(string _)
    {
        SubmitCurrentInput();
    }

    private void HandleInputValueChanged(string value)
    {
        if (inputField == null || suppressInputChange)
            return;

        if (string.IsNullOrEmpty(currentPromptPrefix))
            return;

        if (!value.StartsWith(currentPromptPrefix))
        {
            string repairedUserText = string.Empty;

            if (value.Length > currentPromptPrefix.Length)
                repairedUserText = value.Substring(currentPromptPrefix.Length);

            SetInputTextPreservingPrompt(repairedUserText);
            return;
        }

        ClampCaretToEditableRegion();
    }

    private void ClampCaretToEditableRegion()
    {
        if (inputField == null || string.IsNullOrEmpty(currentPromptPrefix))
            return;

        int min = currentPromptPrefix.Length;

        if (inputField.caretPosition < min)
            inputField.caretPosition = min;

        if (inputField.stringPosition < min)
            inputField.stringPosition = min;

        if (inputField.selectionAnchorPosition < min)
            inputField.selectionAnchorPosition = min;

        if (inputField.selectionFocusPosition < min)
            inputField.selectionFocusPosition = min;
    }

    private void SetInputTextPreservingPrompt(string userText)
    {
        if (inputField == null)
            return;

        suppressInputChange = true;
        inputField.text = currentPromptPrefix + userText;
        inputField.caretPosition = inputField.text.Length;
        inputField.stringPosition = inputField.text.Length;
        inputField.selectionAnchorPosition = inputField.text.Length;
        inputField.selectionFocusPosition = inputField.text.Length;
        inputField.ForceLabelUpdate();
        suppressInputChange = false;
    }

    private string GetSubmittedInput()
    {
        if (inputField == null)
            return string.Empty;

        string value = inputField.text ?? string.Empty;

        if (string.IsNullOrEmpty(currentPromptPrefix))
            return value.Trim();

        if (!value.StartsWith(currentPromptPrefix))
            return value.Trim();

        return value.Substring(currentPromptPrefix.Length).Trim();
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

    private struct SplashLine
    {
        public string text;
        public bool typewriter;

        public SplashLine(string text, bool typewriter = false)
        {
            this.text = text;
            this.typewriter = typewriter;
        }
    }

    private IEnumerator AnimateSplashText(CleanSession session)
    {
        if (minigameText == null || session == null)
            yield break;

        List<SplashLine> lines = BuildSplashLines(session);
        StringBuilder committed = new StringBuilder();

        for (int i = 0; i < lines.Count; i++)
        {
            SplashLine line = lines[i];

            if (line.typewriter)
            {
                for (int c = 0; c < line.text.Length; c++)
                {
                    minigameText.text = committed.ToString() + line.text.Substring(0, c + 1);
                    yield return new WaitForSeconds(splashTypedCharDelay);
                }
            }
            else
            {
                minigameText.text = committed.ToString() + line.text;
            }

            committed.AppendLine(line.text);
            minigameText.text = committed.ToString();

            if (i < lines.Count - 1)
                yield return new WaitForSeconds(splashLineDelay);
        }

        splashRoutine = null;
    }

    private List<SplashLine> BuildSplashLines(CleanSession session)
    {
        return new List<SplashLine>
        {
            new SplashLine($"> ssh {session.node.nodeId}.local", true),
            new SplashLine("connecting..."),
            new SplashLine("auth ok"),
            new SplashLine("mounting /proc..."),
            new SplashLine("scanning anomalies..."),
            new SplashLine(""),
            new SplashLine($"detected infection: {session.infectionType.ToString().ToUpperInvariant()}"),
            new SplashLine($"cleanup module: {session.minigameType.ToString().ToUpperInvariant()}"),
            new SplashLine($"difficulty: {GetDifficultyLabel(session.difficultyTier)}"),
            new SplashLine("modifiers: NONE"),
            new SplashLine(""),
            new SplashLine("stand by...")
        };
    }

    private string BuildSplashText(CleanSession session)
    {
        StringBuilder sb = new StringBuilder();
        List<SplashLine> lines = BuildSplashLines(session);

        for (int i = 0; i < lines.Count; i++)
            sb.AppendLine(lines[i].text);

        return sb.ToString().TrimEnd();
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