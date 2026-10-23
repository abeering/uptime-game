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
                    Inspect(tokens);
                    break;
                case "block":
                    if (tokens.Length != 2)
                        AppendText("USAGE  block ab", errorColor);
                    else
                    {
                        bool blocked = simulationHost != null && simulationHost.BlockConnection(tokens[1]);
                        recorder?.RecordPlayerAction(command, tokens[1], blocked);
                        AppendText(blocked ? "BLOCKED  " + tokens[1].ToUpperInvariant() :
                            "BLOCK  no active connection " + tokens[1], blocked ? normalColor : errorColor);
                    }
                    break;
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
                            AppendText("QUEUED  " + plan.ConnectionId.ToUpperInvariant() +
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
                    AppendText("COMMANDS  inspect  block  demandtest  mark  log save|dir|folder  test  help", mutedColor);
                    break;
                default:
                    AppendText($"UNKNOWN COMMAND  {command}  // type help", errorColor);
                    break;
            }

            StartCoroutine(FocusInputNextFrame());
        }

        private void Inspect(string[] tokens)
        {
            if (tokens.Length < 2 || tokens.Length > 3)
            {
                AppendText("USAGE  inspect ab [cd]", errorColor);
                return;
            }
            if (simulationHost == null || simulationHost.RuntimeConnections == null)
            {
                AppendText("INSPECT  simulation unavailable", errorColor);
                return;
            }

            var selected = new List<LogicalConnectionState>();
            for (int i = 1; i < tokens.Length; i++)
            {
                LogicalConnectionState match = null;
                match = simulationHost.FindConnection(tokens[i]);
                if (match == null)
                {
                    AppendText("INSPECT  unknown connection " + tokens[i], errorColor);
                    return;
                }
                if (selected.Contains(match))
                {
                    AppendText("INSPECT  duplicate connection " + tokens[i], errorColor);
                    return;
                }
                selected.Add(match);
            }
            InspectOutput block = AppendOutputBlock(inspectPrefab);
            if (block == null)
                AppendText("INSPECT  output unavailable", errorColor);
            else
                block.Bind(selected.ToArray(), simulationHost.PresentationPalette);
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

            if (!scrollPending)
                StartCoroutine(ScrollToBottomNextFrame());
            return instance;
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
