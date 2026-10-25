using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public sealed class Terminal : MonoBehaviour
    {
        [SerializeField] private TMP_InputField commandInput;
        [SerializeField] private ScrollRect historyScroll;
        [SerializeField] private RectTransform historyContent;
        [SerializeField] private TextRow textRowPrefab;
        [SerializeField] private PlaceholderOutput placeholderPrefab;
        [SerializeField] private InspectOutput inspectPrefab;
        [SerializeField] private SimulationHost simulationHost;
        [SerializeField] private ConnectionTrafficDirector trafficDirector;
        [SerializeField] private ConnectionsRunRecorder recorder;
        [SerializeField] private IdentityPresenter identityPresenter;
        [SerializeField, Min(1)] private int maxEntries = 100;
        [SerializeField] private Color normalColor = new(0.73f, 0.89f, 0.84f);
        [SerializeField] private Color mutedColor = new(0.47f, 0.63f, 0.62f);
        [SerializeField] private Color errorColor = new(1f, 0.63f, 0.39f);

        private int entryCount;
        private bool scrollPending;
        private TMP_InputField.OnValidateInput previousInputValidator;

        private void Awake()
        {
            if (commandInput != null)
            {
                commandInput.onSubmit.AddListener(HandleSubmit);
                previousInputValidator = commandInput.onValidateInput;
                commandInput.onValidateInput = ValidateCommandCharacter;
            }
        }

        private void Start()
        {
            AppendText("CONNECTIONS // TERMINAL READY", normalColor);
            AppendText("Type help for commands.", mutedColor);
            StartCoroutine(FocusInputNextFrame());
        }

        private void OnEnable()
        {
            if (Application.isPlaying && simulationHost != null)
                simulationHost.EffectOccurred += OnEffectOccurred;
        }

        private void OnDisable()
        {
            if (simulationHost != null)
                simulationHost.EffectOccurred -= OnEffectOccurred;
        }

        private void OnEffectOccurred(ConnectionEffectOccurrence occurrence)
        {
            if (occurrence.Type == CompletionEffect.InfectionDelivery)
                AppendText("[THREAT] " + occurrence.ConnectionId.ToLowerInvariant() +
                    " delivered infection payload to " + occurrence.TargetNodeId, errorColor);
        }

        private void OnDestroy()
        {
            if (commandInput != null)
            {
                commandInput.onSubmit.RemoveListener(HandleSubmit);
                commandInput.onValidateInput = previousInputValidator;
            }
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.backquoteKey.wasPressedThisFrame)
                return;
            identityPresenter?.CycleCongestedEdges();
        }

        private char ValidateCommandCharacter(string text, int position, char character)
        {
            if (character == '\t') return '\0';
            if ((character == '`' || character == '~') && Keyboard.current != null &&
                Keyboard.current.backquoteKey.wasPressedThisFrame)
                return '\0';
            return previousInputValidator != null ? previousInputValidator(text, position, character) : character;
        }

        private void HandleSubmit(string raw)
        {
            if (commandInput == null || string.IsNullOrWhiteSpace(raw))
            {
                StartCoroutine(FocusInputNextFrame());
                return;
            }

            string command = raw.Trim();
            commandInput.SetTextWithoutNotify(string.Empty);
            AppendText($"> {command}", normalColor);

            string[] tokens = command.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            switch (tokens[0].ToLowerInvariant())
            {
                case "test":
                    AppendText("TEST // structured output follows", mutedColor);
                    PlaceholderOutput block = AppendOutputBlock(placeholderPrefab);
                    if (block != null)
                        block.Bind("PRESENTATION CHECK", "Authored UI block inside terminal history.", "PHASE 0", "STATIC", 0.42f);
                    break;
                case "inspect":
                case "i":
                    Inspect(tokens);
                    break;
                case "kill":
                    if (tokens.Length != 2)
                        AppendText("USAGE  kill a7", errorColor);
                    else
                    {
                        bool killed = simulationHost != null && simulationHost.KillConnection(tokens[1]);
                        recorder?.RecordPlayerAction(command, tokens[1], killed);
                        string displayId = tokens[1].ToLowerInvariant();
                        AppendText(killed ? "KILLED  " + displayId :
                            "KILL  no active connection " + displayId,
                            killed ? normalColor : errorColor);
                    }
                    break;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                case "spawn":
                    if (tokens.Length != 2)
                        AppendText("USAGE  spawn <profileId>", errorColor);
                    else if (trafficDirector == null || simulationHost == null)
                        AppendText("SPAWN  simulation unavailable", errorColor);
                    else
                    {
                        try
                        {
                            ConnectionSpawnPlan plan = trafficDirector.QueueProfile(tokens[1]);
                            recorder?.RecordPlayerAction(command, plan.ConnectionId, true);
                            AppendText("QUEUED  " + plan.ConnectionId.ToLowerInvariant() +
                                " from profile " + plan.ProfileId, normalColor);
                        }
                        catch (System.InvalidOperationException error)
                        {
                            recorder?.RecordPlayerAction(command, null, false);
                            AppendText("SPAWN  " + error.Message, errorColor);
                        }
                    }
                    break;
#endif
                case "demandtest":
                    if (trafficDirector == null || simulationHost == null ||
                        simulationHost.RunMode != ConnectionRunMode.Generated)
                        AppendText("DEMANDTEST  generated mode required", errorColor);
                    else
                    {
                        try
                        {
                            ConnectionSpawnPlan plan = trafficDirector.QueueHighDemandWitness();
                            recorder?.RecordPlayerAction(command, plan.ConnectionId, true);
                            AppendText("QUEUED  " + plan.ConnectionId.ToLowerInvariant() +
                                " high demand to DB", normalColor);
                        }
                        catch (System.InvalidOperationException error)
                        {
                            AppendText("DEMANDTEST  " + error.Message, errorColor);
                        }
                    }
                    break;
                case "mark":
                    string note = raw.TrimStart().Substring(tokens[0].Length).TrimStart(' ', '\t');
                    if (recorder != null && recorder.Mark(note))
                        AppendText("MARK SAVED  " + System.IO.Path.GetFileName(recorder.OutputPath), normalColor);
                    else AppendText("MARK  " + (recorder?.LastError ?? "recording unavailable"), errorColor);
                    break;
                case "log":
                    if (tokens.Length != 2)
                        AppendText("USAGE  log save | dir | folder", errorColor);
                    else if (tokens[1].Equals("dir", System.StringComparison.OrdinalIgnoreCase))
                        AppendText("LOG DIR  " + ConnectionsRunRecorder.LogDirectory, normalColor);
                    else if (tokens[1].Equals("folder", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (ConnectionsRunRecorder.RevealDirectory(out string error))
                            AppendText("LOG FOLDER OPENED", normalColor);
                        else AppendText("LOG FOLDER  " + error, errorColor);
                    }
                    else if (tokens[1].Equals("save", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (recorder != null && recorder.Save(true))
                            AppendText("LOG SAVED  " + System.IO.Path.GetFileName(recorder.OutputPath), normalColor);
                        else AppendText("LOG SAVE  " + (recorder?.LastError ?? "recording unavailable"), errorColor);
                    }
                    else AppendText("USAGE  log save | dir | folder", errorColor);
                    break;
                case "help":
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    AppendText("COMMANDS  inspect|i [-v] <id> [id ...]  kill  spawn <profileId> (dev)  demandtest  mark  log save|dir|folder  test  help", mutedColor);
#else
                    AppendText("COMMANDS  inspect|i [-v] <id> [id ...]  kill  demandtest  mark  log save|dir|folder  test  help", mutedColor);
#endif
                    break;
                default:
                    AppendText($"UNKNOWN COMMAND  {command}  // type help", errorColor);
                    break;
            }

            StartCoroutine(FocusInputNextFrame());
        }

        private void Inspect(string[] tokens)
        {
            if (!TryParseInspect(tokens,
                out InspectPresentationMode mode, out string[] ids, out string error))
            {
                AppendText(error, errorColor);
                return;
            }
            if (simulationHost == null || simulationHost.RuntimeConnections == null)
            {
                AppendText("INSPECT  simulation unavailable", errorColor);
                return;
            }

            var selected = new List<LogicalConnectionState>();
            foreach (string id in ids)
            {
                LogicalConnectionState match = simulationHost.FindConnection(id);
                if (match == null)
                {
                    AppendText("INSPECT  unknown connection " + id.ToLowerInvariant(), errorColor);
                    return;
                }
                if (selected.Contains(match))
                {
                    AppendText("INSPECT  duplicate connection " + id.ToLowerInvariant(), errorColor);
                    return;
                }
                selected.Add(match);
            }
            InspectOutput block = AppendOutputBlock(inspectPrefab);
            if (block == null)
                AppendText("INSPECT  output unavailable", errorColor);
            else
                block.Bind(selected.ToArray(), simulationHost.PresentationPalette, mode);
        }

        public static bool TryParseInspect(string[] tokens,
            out InspectPresentationMode mode, out string[] ids, out string error)
        {
            const string usage = "USAGE  inspect [-v|--verbose] <id> [id ...]";
            mode = InspectPresentationMode.Compact;
            ids = System.Array.Empty<string>();
            error = usage;
            if (tokens == null || tokens.Length == 0)
                return false;
            int firstId = 1;
            if (tokens.Length > 1 &&
                (tokens[1].Equals("-v", System.StringComparison.OrdinalIgnoreCase) ||
                 tokens[1].Equals("--verbose", System.StringComparison.OrdinalIgnoreCase)))
            {
                mode = InspectPresentationMode.Verbose;
                firstId++;
            }
            for (int i = firstId; i < tokens.Length; i++)
                if (tokens[i].StartsWith("-", System.StringComparison.Ordinal))
                {
                    error = "INSPECT  invalid flag " + tokens[i] + " // " + usage;
                    return false;
                }
            int count = tokens.Length - firstId;
            if (count < 1)
                return false;
            ids = new string[count];
            System.Array.Copy(tokens, firstId, ids, 0, count);
            error = null;
            return true;
        }

        public TextRow AppendText(string message, Color color)
        {
            TextRow row = AppendOutputBlock(textRowPrefab);
            if (row != null)
                row.Bind(message, color);
            return row;
        }

        public T AppendOutputBlock<T>(T prefab) where T : Component
        {
            if (prefab == null || historyContent == null)
                return null;

            T instance = Instantiate(prefab, historyContent);
            entryCount++;
            while (entryCount > maxEntries && historyContent.childCount > 0)
            {
                Transform oldest = historyContent.GetChild(0);
                oldest.SetParent(null, false);
                Destroy(oldest.gameObject);
                entryCount--;
            }

            if (!scrollPending && (historyScroll == null ||
                historyScroll.verticalNormalizedPosition <= 0.01f))
                StartCoroutine(ScrollToBottomNextFrame());
            return instance;
        }

        // Keep the top visible history location when the workstation changes height.
        public void SetTopInset(float inset)
        {
            var terminalRect = (RectTransform)transform;
            bool atBottom = historyScroll == null ||
                historyScroll.verticalNormalizedPosition <= 0.01f;
            float offsetFromTop = 0f;
            if (historyScroll != null && !atBottom)
            {
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4];
                historyContent.GetWorldCorners(corners);
                offsetFromTop = historyScroll.viewport.InverseTransformPoint(corners[1]).y -
                    historyScroll.viewport.rect.yMax;
            }
            terminalRect.offsetMax = new Vector2(terminalRect.offsetMax.x, -inset);
            if (historyScroll == null) return;
            Canvas.ForceUpdateCanvases();
            if (atBottom)
                historyScroll.verticalNormalizedPosition = 0f;
            else
            {
                float travel = Mathf.Max(0f,
                    historyContent.rect.height - historyScroll.viewport.rect.height);
                historyScroll.verticalNormalizedPosition = travel > 0f ?
                    1f - Mathf.Clamp01(offsetFromTop / travel) : 0f;
            }
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            scrollPending = true;
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (historyContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(historyContent);
            if (historyScroll != null)
                historyScroll.verticalNormalizedPosition = 0f;
            scrollPending = false;
        }

        private IEnumerator FocusInputNextFrame()
        {
            yield return null;
            if (commandInput == null)
                yield break;
            commandInput.ActivateInputField();
            commandInput.Select();
        }
    }
}
