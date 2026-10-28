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
        [SerializeField] private ScanOutput scanPrefab;
        [SerializeField] private SimulationHost simulationHost;
        [SerializeField] private ConnectionTrafficDirector trafficDirector;
        [SerializeField] private ConnectionsRunRecorder recorder;
        [SerializeField] private IdentityPresenter identityPresenter;
        [SerializeField] private ConnectionsCleanOverlay cleanOverlay;
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
            if (cleanOverlay == null || !cleanOverlay.IsOpen)
                StartCoroutine(FocusInputNextFrame());
        }

        private void OnEnable()
        {
            if (Application.isPlaying && simulationHost != null)
                simulationHost.ScanCompleted += OnScanCompleted;
        }

        private void OnDisable()
        {
            if (simulationHost != null)
                simulationHost.ScanCompleted -= OnScanCompleted;
        }

        private void OnScanCompleted(NodeScanSession session)
        {
            AppendText("SCAN COMPLETE  " + session.NodeId + "  /  " +
                (session.Result == NodeScanResult.Infected ? "INFECTED" : "CLEAN"),
                session.Result == NodeScanResult.Infected ? errorColor : normalColor);
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
                case "k":
                    Kill(tokens, command);
                    break;
                case "clean":
                    Clean(tokens, command);
                    break;
                case "scan":
                    Scan(tokens, command);
                    break;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                case "spawn":
                    if (!TryParseSpawn(tokens, out string profileId,
                        out ConnectionSpawnOverrides spawnOverrides, out string spawnError))
                        AppendText(spawnError, errorColor);
                    else if (trafficDirector == null || simulationHost == null)
                        AppendText("SPAWN  simulation unavailable", errorColor);
                    else
                    {
                        try
                        {
                            ConnectionSpawnPlan plan = trafficDirector.QueueProfile(profileId,
                                spawnOverrides);
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
                case "infect":
                    if ((tokens.Length != 3 && tokens.Length != 4) ||
                        !TryParseInfectionKind(tokens[2], out InfectionKind injectKind) ||
                        injectKind == InfectionKind.Blackout && tokens.Length == 4)
                        AppendText("USAGE  infect <nodeId> phantom-load|blackout|spawner [load|cadence]", errorColor);
                    else if (simulationHost == null)
                        AppendText("INFECT  simulation unavailable", errorColor);
                    else if (tokens.Length == 4 &&
                        (!float.TryParse(tokens[3], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float parsedParameter) ||
                         parsedParameter <= 0f || float.IsNaN(parsedParameter) ||
                         float.IsInfinity(parsedParameter)))
                        AppendText("INFECT  parameter must be a finite positive number", errorColor);
                    else
                    {
                        float parameter = tokens.Length == 4 ?
                            float.Parse(tokens[3], System.Globalization.CultureInfo.InvariantCulture) :
                            injectKind == InfectionKind.PhantomLoad ? simulationHost.DefaultPhantomLoad :
                            injectKind == InfectionKind.Spawner ? simulationHost.DefaultSpawnerCadence : 0f;
                        bool applied = simulationHost.TryInjectInfection(tokens[1], injectKind, parameter,
                            out string infectionError);
                        recorder?.RecordPlayerAction(command, null, applied);
                        AppendText(applied ? "INJECTED  " + injectKind + " on " +
                            tokens[1].ToUpperInvariant() : "INFECT  " + infectionError,
                            applied ? normalColor : errorColor);
                    }
                    break;
                case "uninfect":
                    if ((tokens.Length != 2 && tokens.Length != 3) ||
                        tokens.Length == 3 && !TryParseInfectionKind(tokens[2], out _))
                        AppendText("USAGE  uninfect <nodeId> [phantom-load|blackout|spawner]", errorColor);
                    else if (simulationHost == null)
                        AppendText("UNINFECT  simulation unavailable", errorColor);
                    else
                    {
                        InfectionKind removeKind = InfectionKind.PhantomLoad;
                        if (tokens.Length == 3) TryParseInfectionKind(tokens[2], out removeKind);
                        bool removed = simulationHost.TryRemoveInfection(tokens[1], removeKind,
                            out string removalError);
                        recorder?.RecordPlayerAction(command, null, removed);
                        AppendText(removed ? "REMOVED  " + removeKind + " from " +
                            tokens[1].ToUpperInvariant() : "UNINFECT  " + removalError,
                            removed ? normalColor : errorColor);
                    }
                    break;
                case "infections":
                    if (tokens.Length != 1 || simulationHost == null)
                        AppendText("USAGE  infections", errorColor);
                    else if (simulationHost.Infections.Count == 0)
                        AppendText("INFECTIONS  none", mutedColor);
                    else
                        foreach (NodeInfection infection in simulationHost.Infections)
                            AppendText("INFECTION  " + infection.Target.View.NodeId + " " +
                                infection.Payload.Kind + " load=" +
                                infection.Payload.RequestedLoad.ToString("0.##") + " cadence=" +
                                infection.Payload.CadenceSeconds.ToString("0.##") + " next=" +
                                infection.NextSpawnAtSeconds.ToString("0.##") + " at=" +
                                infection.AppliedAtSeconds.ToString("0.##") + "s source=" +
                                (infection.SourceConnectionId ?? "debug") + " op=" +
                                infection.SourceOperationIndex, mutedColor);
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
                    AppendText("COMMANDS  inspect|i [-v] <id> [id ...]  kill|k <id> [id ...]  scan <nodeId>  clean <nodeId>", mutedColor);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    AppendText("MORE  spawn <profileId> [obj:delivery|loader] [inf:phantom-load|blackout|spawner] [infp:load=N|cadence=N]", mutedColor);
                    AppendText("DEV  infect <nodeId> <kind> [load|cadence]  uninfect <nodeId> [kind]  infections", mutedColor);
                    AppendText("MORE  demandtest  mark  log save|dir|folder  test  help", mutedColor);
#else
                    AppendText("MORE  demandtest  mark  log save|dir|folder  test  help", mutedColor);
#endif
                    break;
                default:
                    AppendText($"UNKNOWN COMMAND  {command}  // type help", errorColor);
                    break;
            }

            if (cleanOverlay == null || !cleanOverlay.IsOpen)
                StartCoroutine(FocusInputNextFrame());
        }

        public static bool TryParseSpawn(string[] tokens, out string profileId,
            out ConnectionSpawnOverrides overrides, out string error)
        {
            const string usage = "USAGE  spawn <profileId> [obj:delivery|loader] " +
                "[inf:phantom-load|blackout|spawner] [infp:load=N|cadence=N]";
            profileId = null;
            overrides = null;
            error = usage;
            if (tokens == null || tokens.Length < 2) return false;
            profileId = tokens[1];
            ManualObjective? objective = null;
            InfectionKind? kind = null;
            float? load = null;
            float? cadence = null;
            for (int i = 2; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (token.StartsWith("obj:", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (objective.HasValue) { error = "SPAWN  duplicate obj option"; return false; }
                    string value = token.Substring(4);
                    if (value.Equals("delivery", System.StringComparison.OrdinalIgnoreCase))
                        objective = ManualObjective.Delivery;
                    else if (value.Equals("loader", System.StringComparison.OrdinalIgnoreCase))
                        objective = ManualObjective.Loader;
                    else { error = "SPAWN  unknown objective: " + value; return false; }
                }
                else if (token.StartsWith("inf:", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (kind.HasValue) { error = "SPAWN  duplicate inf option"; return false; }
                    string value = token.Substring(4);
                    if (!TryParseInfectionKind(value, out InfectionKind parsedKind))
                    { error = "SPAWN  unknown infection type: " + value; return false; }
                    kind = parsedKind;
                }
                else if (token.StartsWith("infp:", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (load.HasValue || cadence.HasValue) { error = "SPAWN  duplicate infp option"; return false; }
                    string value = token.Substring(5);
                    bool isLoad = value.StartsWith("load=", System.StringComparison.OrdinalIgnoreCase);
                    bool isCadence = value.StartsWith("cadence=", System.StringComparison.OrdinalIgnoreCase);
                    if ((!isLoad && !isCadence) ||
                        !float.TryParse(value.Substring(isLoad ? 5 : 8),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float amount) ||
                        amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
                    { error = "SPAWN  infp requires finite positive load=N or cadence=N"; return false; }
                    if (isLoad) load = amount;
                    else cadence = amount;
                }
                else { error = "SPAWN  unknown option: " + token; return false; }
            }
            if (!objective.HasValue && (kind.HasValue || load.HasValue || cadence.HasValue))
            { error = "SPAWN  infection options require obj:delivery"; return false; }
            if (objective == ManualObjective.Loader && (kind.HasValue || load.HasValue || cadence.HasValue))
            { error = "SPAWN  obj:loader cannot carry an infection"; return false; }
            if (load.HasValue && kind != InfectionKind.PhantomLoad)
            { error = "SPAWN  infp:load requires inf:phantom-load"; return false; }
            if (cadence.HasValue && kind != InfectionKind.Spawner)
            { error = "SPAWN  infp:cadence requires inf:spawner"; return false; }
            if (objective.HasValue)
                overrides = new ConnectionSpawnOverrides(objective.Value, kind, load, cadence);
            error = null;
            return true;
        }

        public static bool TryParseInfectionKind(string text, out InfectionKind kind)
        {
            kind = InfectionKind.PhantomLoad;
            if (text == null) return false;
            if (text.Equals("phantom-load", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (text.Equals("blackout", System.StringComparison.OrdinalIgnoreCase))
            { kind = InfectionKind.Blackout; return true; }
            if (text.Equals("spawner", System.StringComparison.OrdinalIgnoreCase))
            { kind = InfectionKind.Spawner; return true; }
            return false;
        }

        private void Kill(string[] tokens, string command)
        {
            if (tokens.Length < 2)
            {
                AppendText("USAGE  kill|k <id> [id ...]", errorColor);
                return;
            }
            var attempted = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < tokens.Length; i++)
            {
                string id = tokens[i];
                string displayId = id.ToLowerInvariant();
                if (!attempted.Add(id))
                {
                    AppendText("KILL  duplicate connection " + displayId + " (skipped)", errorColor);
                    continue;
                }
                bool killed = simulationHost != null && simulationHost.KillConnection(id);
                recorder?.RecordPlayerAction(command, id, killed);
                AppendText(killed ? "KILLED  " + displayId :
                    "KILL  no active connection " + displayId,
                    killed ? normalColor : errorColor);
            }
        }

        private void Clean(string[] tokens, string command)
        {
            if (tokens.Length != 2)
                AppendText("USAGE  clean <nodeId>", errorColor);
            else if (simulationHost == null)
                AppendText("CLEAN FAILED  simulation unavailable", errorColor);
            else if (cleanOverlay == null)
                AppendText("CLEAN FAILED  interface unavailable", errorColor);
            else
            {
                CleanStartResult result = simulationHost.StartClean(tokens[1], command,
                    out CleanSession session);
                string nodeId = tokens[1].ToUpperInvariant();
                if (result == CleanStartResult.Started)
                {
                    AppendText("CLEAN SESSION  " + nodeId, normalColor);
                    commandInput.DeactivateInputField();
                    cleanOverlay.Open(session, simulationHost, this);
                }
                else AppendText(result == CleanStartResult.AlreadyActive ?
                    "CLEAN FAILED  SESSION ALREADY ACTIVE" :
                    "CLEAN FAILED  UNKNOWN NODE " + nodeId, errorColor);
            }
        }

        private void Scan(string[] tokens, string command)
        {
            if (tokens.Length != 2)
            {
                AppendText("USAGE  scan <nodeId>", errorColor);
                return;
            }
            if (simulationHost == null)
            {
                AppendText("SCAN FAILED  simulation unavailable", errorColor);
                return;
            }
            if (scanPrefab == null || historyContent == null)
            {
                AppendText("SCAN FAILED  output unavailable", errorColor);
                return;
            }
            ScanStartResult result = simulationHost.StartScan(tokens[1], command,
                out NodeScanSession session);
            switch (result)
            {
                case ScanStartResult.Started:
                    ScanOutput block = AppendOutputBlock(scanPrefab);
                    block.Bind(session, simulationHost);
                    break;
                case ScanStartResult.AlreadyScanning:
                    AppendText("ALREADY SCANNING  " + tokens[1].ToUpperInvariant(), errorColor);
                    break;
                case ScanStartResult.Busy:
                    AppendText("SCAN BUSY  " + simulationHost.ActiveScan.NodeId, errorColor);
                    break;
                default:
                    AppendText("SCAN FAILED  UNKNOWN NODE " + tokens[1].ToUpperInvariant(),
                        errorColor);
                    break;
            }
        }

        public void FinishClean(string message, bool success)
        {
            AppendText(message, success ? normalColor : errorColor);
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
            if (commandInput == null || cleanOverlay != null && cleanOverlay.IsOpen)
                yield break;
            commandInput.ActivateInputField();
            commandInput.Select();
        }
    }
}
