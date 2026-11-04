using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
        [SerializeField] private LoadOutput loadPrefab;
        [SerializeField] private WorkOutput workPrefab;
        [SerializeField] private SimulationHost simulationHost;
        [SerializeField] private ConnectionTrafficDirector trafficDirector;
        [SerializeField] private ConnectionsRunRecorder recorder;
        [SerializeField] private IdentityPresenter identityPresenter;
        [SerializeField] private ConnectionsCleanOverlay cleanOverlay;
        [SerializeField] private ConnectionsVideoCall videoCall;
        [SerializeField] private TextCommunicationDirector textCommunications;
        [SerializeField, Min(1)] private int maxEntries = 100;
        [SerializeField, Min(1)] private int maxCommandHistory = 100;
        [SerializeField, Min(1f)] private float keyboardScrollStep = 48f;
        [SerializeField] private Color normalColor = new(0.73f, 0.89f, 0.84f);
        [SerializeField] private Color mutedColor = new(0.47f, 0.63f, 0.62f);
        [SerializeField] private Color errorColor = new(1f, 0.63f, 0.39f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private Coroutine delayedCall;
        private Coroutine delayedNotification;
#endif
        private int entryCount;
        private bool scrollPending;
        private Coroutine bottomScrollRoutine;
        private Key heldScrollKey;
        private float nextScrollRepeatAt;
        private TMP_InputField.OnValidateInput previousInputValidator;
        private readonly List<string> commandHistory = new();
        private int commandHistoryIndex = -1;
        private string commandDraft = string.Empty;
        private bool commandFailed;
        public event Action CommandFailed;

        // Command handlers mark rejection explicitly; asynchronous rows use AppendText.
        private Color CommandFailureColor()
        {
            commandFailed = true;
            return errorColor;
        }

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
            if (Application.isPlaying && simulationHost != null)
                simulationHost.ProbeExpired += OnProbeExpired;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Application.isPlaying && simulationHost != null)
                simulationHost.SimulationReset += CancelNotificationTest;
#endif
            if (Application.isPlaying && simulationHost?.Payloads != null)
                simulationHost.Payloads.InterceptSucceeded += OnInterceptSucceeded;
        }

        private void OnDisable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (delayedCall != null) StopCoroutine(delayedCall);
            delayedCall = null;
            CancelNotificationTest();
            if (simulationHost != null) simulationHost.SimulationReset -= CancelNotificationTest;
#endif
            if (simulationHost != null)
                simulationHost.ScanCompleted -= OnScanCompleted;
            if (simulationHost != null)
                simulationHost.ProbeExpired -= OnProbeExpired;
            if (simulationHost?.Payloads != null)
                simulationHost.Payloads.InterceptSucceeded -= OnInterceptSucceeded;
        }

        private void OnProbeExpired(string linkId)
        {
            AppendText("PROBE EXPIRED  " + linkId, mutedColor);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void CancelNotificationTest()
        {
            if (delayedNotification != null) StopCoroutine(delayedNotification);
            delayedNotification = null;
        }

        private void DeveloperNotificationTest(string[] tokens)
        {
            float delay = 0;
            if (tokens.Length < 2 || tokens.Length > 3 ||
                (tokens[1] != "im" && tokens[1] != "system" && tokens[1] != "burst") ||
                (tokens.Length == 3 && (!tokens[2].StartsWith("delay=") ||
                    !float.TryParse(tokens[2].Substring(6), NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out delay) || float.IsNaN(delay) ||
                    float.IsInfinity(delay) || delay < 1 || delay > 30)))
            {
                AppendText("USAGE  notifytest im|system|burst [delay=1..30]", CommandFailureColor());
                return;
            }
            if (textCommunications == null || !textCommunications.isActiveAndEnabled || delayedNotification != null)
            {
                AppendText("DEVELOPER MESSAGE TEST UNAVAILABLE OR ALREADY SCHEDULED", CommandFailureColor());
                return;
            }
            if (delay == 0) textCommunications.PushSample(tokens[1]);
            else delayedNotification = StartCoroutine(OpenDelayedNotification(tokens[1], delay));
            AppendText(delay == 0 ? "DEVELOPER MESSAGE SAMPLE DELIVERED" : "DEVELOPER MESSAGE SAMPLE SCHEDULED", mutedColor);
        }

        private IEnumerator OpenDelayedNotification(string kind, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            delayedNotification = null;
            // Delivery performs no terminal history write or input/focus operation.
            if (textCommunications != null) textCommunications.PushSample(kind);
        }

        private void DeveloperCallTest(string[] tokens)
        {
            float delay = 0f;
            if (tokens.Length != 1 && (tokens.Length != 2 || !tokens[1].StartsWith("delay=") ||
                !float.TryParse(tokens[1].Substring(6), NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out delay) || float.IsNaN(delay) ||
                float.IsInfinity(delay) || delay < 1f || delay > 30f))
            {
                AppendText("USAGE  calltest [delay=1..30]", CommandFailureColor());
                return;
            }
            if (delayedCall != null || videoCall == null || videoCall.IsActive)
            {
                AppendText("TEST CALL UNAVAILABLE, ACTIVE OR ALREADY SCHEDULED", CommandFailureColor());
                return;
            }
            if (delay == 0f)
            {
                bool opened = videoCall.TryStartSampleCall();
                AppendText(opened ? "DEVELOPER TEST CALL OPENED" :
                    "TEST CALL UNAVAILABLE OR ALREADY ACTIVE",
                    opened ? mutedColor : CommandFailureColor());
            }
            else
            {
                delayedCall = StartCoroutine(OpenDelayedCall(delay));
                AppendText("DEVELOPER TEST CALL SCHEDULED  /  " +
                    delay.ToString("0.##", CultureInfo.InvariantCulture) + "s", mutedColor);
            }
        }

        private IEnumerator OpenDelayedCall(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            delayedCall = null;
            // No history write or input/focus transfer when an unsolicited test arrives.
            if (videoCall != null) videoCall.TryStartSampleCall();
        }
#endif

        private void OnInterceptSucceeded(PayloadState payload)
        {
            AppendText("INTERCEPTED  " + payload.Id.ToLowerInvariant(), normalColor);
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
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.backquoteKey.wasPressedThisFrame)
                identityPresenter?.CycleCongestedEdges();
            UpdateHistoryScrolling(keyboard);
        }

        // Run after the EventSystem has processed TMP's normal arrow-key navigation.
        private void LateUpdate()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || commandInput == null || !commandInput.isFocused ||
                keyboard.ctrlKey.isPressed || keyboard.leftMetaKey.isPressed ||
                keyboard.rightMetaKey.isPressed || keyboard.altKey.isPressed ||
                keyboard.shiftKey.isPressed)
                return;
            if (keyboard.upArrowKey.wasPressedThisFrame) RecallCommand(-1);
            else if (keyboard.downArrowKey.wasPressedThisFrame) RecallCommand(1);
        }

        private void RememberCommand(string command)
        {
            if (commandHistory.Count == 0 || commandHistory[commandHistory.Count - 1] != command)
                commandHistory.Add(command);
            while (commandHistory.Count > Mathf.Max(1, maxCommandHistory))
                commandHistory.RemoveAt(0);
            commandHistoryIndex = -1;
            commandDraft = string.Empty;
        }

        private void RecallCommand(int direction)
        {
            if (commandInput == null || commandHistory.Count == 0) return;
            if (commandHistoryIndex < 0)
            {
                if (direction > 0) return;
                commandDraft = commandInput.text;
                commandHistoryIndex = commandHistory.Count;
            }
            commandHistoryIndex = Mathf.Clamp(commandHistoryIndex + direction,
                0, commandHistory.Count);
            bool atDraft = commandHistoryIndex == commandHistory.Count;
            commandInput.SetTextWithoutNotify(atDraft ? commandDraft :
                commandHistory[commandHistoryIndex]);
            commandInput.textComponent.ForceMeshUpdate();
            commandInput.MoveTextEnd(false);
            if (atDraft) commandHistoryIndex = -1;
        }

        private void UpdateHistoryScrolling(Keyboard keyboard)
        {
            Key key = Key.None;
            if (commandInput != null && commandInput.isFocused &&
                !keyboard.altKey.isPressed && !keyboard.shiftKey.isPressed)
            {
                if (keyboard.pageUpKey.isPressed) key = Key.PageUp;
                else if (keyboard.pageDownKey.isPressed) key = Key.PageDown;
                else if (keyboard.ctrlKey.isPressed || keyboard.leftMetaKey.isPressed ||
                    keyboard.rightMetaKey.isPressed)
                {
                    if (keyboard.upArrowKey.isPressed) key = Key.UpArrow;
                    else if (keyboard.downArrowKey.isPressed) key = Key.DownArrow;
                }
            }

            if (key == Key.None)
            {
                heldScrollKey = Key.None;
                return;
            }
            bool firstPress = key != heldScrollKey || keyboard[key].wasPressedThisFrame;
            if (!firstPress && Time.unscaledTime < nextScrollRepeatAt) return;
            heldScrollKey = key;
            nextScrollRepeatAt = Time.unscaledTime + (firstPress ? 0.35f : 0.06f);

            bool page = key == Key.PageUp || key == Key.PageDown;
            float direction = key == Key.UpArrow || key == Key.PageUp ? 1f : -1f;
            ScrollHistory(direction, page);
        }

        private void ScrollHistory(float direction, bool page)
        {
            if (historyScroll == null || historyContent == null || historyScroll.viewport == null)
                return;
            if (bottomScrollRoutine != null)
            {
                StopCoroutine(bottomScrollRoutine);
                bottomScrollRoutine = null;
                scrollPending = false;
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyContent);
            float viewportHeight = historyScroll.viewport.rect.height;
            float travel = Mathf.Max(0f, historyContent.rect.height - viewportHeight);
            historyScroll.StopMovement();
            if (travel <= 0f) return;
            float distance = page ? viewportHeight * 0.9f : keyboardScrollStep;
            historyScroll.verticalNormalizedPosition = Mathf.Clamp01(
                historyScroll.verticalNormalizedPosition + direction * distance / travel);
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
            commandFailed = false;
            RememberCommand(command);
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
                    Inspect(tokens, command);
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
                case "probe":
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (tokens.Length == 2 && tokens[1].Equals("reset",
                        System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (simulationHost == null)
                        {
                            recorder?.RecordPlayerAction(command, null, false, null,
                                "simulation_unavailable");
                            AppendText("PROBE  simulation unavailable", CommandFailureColor());
                        }
                        else
                        {
                            simulationHost.ResetProbeCooldown(command);
                            AppendText(simulationHost.ProbedLink == null ?
                                "PROBE COOLDOWN RESET  ready" :
                                "PROBE COOLDOWN RESET  active on " +
                                simulationHost.ProbedLink.View.LinkId, normalColor);
                        }
                        break;
                    }
#endif
                    if (tokens.Length != 2)
                    {
                        recorder?.RecordPlayerAction(command, null, false, null,
                            "usage", tokens.Length > 1 ? tokens[1] : null);
                        AppendText("USAGE  probe <linkId>", CommandFailureColor());
                    }
                    else if (simulationHost == null)
                    {
                        recorder?.RecordPlayerAction(command, null, false, null,
                            "simulation_unavailable", tokens[1]);
                        AppendText("PROBE  simulation unavailable", CommandFailureColor());
                    }
                    else
                    {
                        bool deployed = simulationHost.TryActivateProbe(tokens[1], command,
                            out string probeResult);
                        AppendText(probeResult, deployed ? normalColor : CommandFailureColor());
                    }
                    break;
                case "load":
                    Load(tokens);
                    break;
                case "work":
                    Work(tokens);
                    break;
                case "intercept":
                    if (!TryParseIntercept(tokens, out string interceptPayloadId, out string interceptNodeId))
                        AppendText("USAGE  intercept [payloadId] @ <nodeId>", CommandFailureColor());
                    else if (simulationHost?.Payloads == null)
                        AppendText("INTERCEPT  simulation unavailable", CommandFailureColor());
                    else
                    {
                        bool armed = simulationHost.Payloads.TryIntercept(interceptPayloadId, interceptNodeId,
                            out string interceptError, command);
                        if (!armed) recorder?.RecordPlayerAction(command, interceptPayloadId, false,
                            interceptNodeId, interceptError);
                        AppendText(armed ? "INTERCEPT ACTIVE  " + (interceptPayloadId?.ToLowerInvariant() ?? "ALL") +
                            " @ " + interceptNodeId.ToUpperInvariant() : "INTERCEPT  " + interceptError,
                            armed ? normalColor : CommandFailureColor());
                    }
                    break;
                case "isolate":
                    if (tokens.Length != 2)
                        AppendText("USAGE  isolate <nodeId>", CommandFailureColor());
                    else if (simulationHost?.Worms == null)
                        AppendText("ISOLATE  simulation unavailable", CommandFailureColor());
                    else
                    {
                        bool isolated = simulationHost.Worms.TryIsolate(tokens[1], command,
                            out string isolateError);
                        if (!isolated) recorder?.RecordPlayerAction(command, null, false,
                            tokens[1], isolateError);
                        AppendText(isolated ? "ISOLATED  " + tokens[1].ToUpperInvariant() :
                            "ISOLATE  " + isolateError, isolated ? normalColor : CommandFailureColor());
                    }
                    break;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                case "notifytest":
                    DeveloperNotificationTest(tokens);
                    break;
                case "calltest":
                    DeveloperCallTest(tokens);
                    break;
                case "worm":
                    if (tokens.Length != 3 || !TryParseWormEffect(tokens[2], out WormEffect effect))
                        AppendText("USAGE  worm <originNodeId> scan-jammed|port-scanned", CommandFailureColor());
                    else if (simulationHost?.Worms == null)
                        AppendText("WORM  simulation unavailable", CommandFailureColor());
                    else try
                    {
                        WormOutbreak outbreak = simulationHost.Worms.Spawn(tokens[1], effect);
                        recorder?.RecordPlayerAction(command, outbreak.Id, true,
                            tokens[1], "worm_spawned");
                        AppendText("WORM  " + outbreak.Id + " from " +
                            tokens[1].ToUpperInvariant(), normalColor);
                    }
                    catch (System.InvalidOperationException error)
                    {
                        recorder?.RecordPlayerAction(command, null, false,
                            tokens[1], error.Message);
                        AppendText("WORM  " + error.Message, CommandFailureColor());
                    }
                    break;
                case "payload":
                    if (tokens.Length < 2 || tokens.Length > 3 || tokens.Length == 3 &&
                        (!System.Enum.TryParse(tokens[2], true, out PayloadMotionProfile parsedProfile) ||
                         !System.Enum.IsDefined(typeof(PayloadMotionProfile), parsedProfile)))
                        AppendText("USAGE  payload <targetNodeId> [steady|variable]", CommandFailureColor());
                    else if (simulationHost?.Payloads == null)
                        AppendText("PAYLOAD  simulation unavailable", CommandFailureColor());
                    else try
                    {
                        PayloadMotionProfile profile = tokens.Length == 3 ?
                            (PayloadMotionProfile)System.Enum.Parse(typeof(PayloadMotionProfile),
                                tokens[2], true) : PayloadMotionProfile.Steady;
                        PayloadState payload = simulationHost.Payloads.Spawn(tokens[1], profile);
                        recorder?.RecordPlayerAction(command, payload.Id, true,
                            tokens[1], "payload_spawned");
                        AppendText("PAYLOAD  " + payload.Id.ToLowerInvariant() +
                            " moving (" + profile.ToString().ToLowerInvariant() + ")", normalColor);
                    }
                    catch (System.InvalidOperationException error)
                    {
                        recorder?.RecordPlayerAction(command, null, false,
                            tokens[1], error.Message);
                        AppendText("PAYLOAD  " + error.Message, CommandFailureColor());
                    }
                    break;
                case "portscan":
                case "recon":
                    if (tokens.Length != 2)
                        AppendText("USAGE  " + tokens[0].ToLowerInvariant() + " <nodeId>", CommandFailureColor());
                    else
                    {
                        HostileActivityDirector activities = simulationHost != null ?
                            simulationHost.GetComponent<HostileActivityDirector>() : null;
                        if (activities == null)
                            AppendText("RECON  activity director unavailable", CommandFailureColor());
                        else
                            try
                            {
                                ConnectionSpawnPlan plan = tokens[0].Equals("recon",
                                    System.StringComparison.OrdinalIgnoreCase) ?
                                    activities.QueueReconnaissance(tokens[1]) :
                                    activities.QueuePortScan(tokens[1]);
                                bool success = plan != null;
                                recorder?.RecordPlayerAction(command, plan?.ConnectionId, success);
                                AppendText(success ? "QUEUED  " + plan.ConnectionId.ToLowerInvariant() +
                                    " to " + plan.Target.NodeId : "RECON  connection IDs exhausted",
                                    success ? normalColor : CommandFailureColor());
                            }
                            catch (System.InvalidOperationException error)
                            {
                                recorder?.RecordPlayerAction(command, null, false);
                                AppendText("RECON  " + error.Message, CommandFailureColor());
                            }
                    }
                    break;
                case "spawn":
                    if (!TryParseSpawn(tokens, out string profileId,
                        out ConnectionSpawnOverrides spawnOverrides, out bool idHopping,
                        out bool idReactive,
                        out string spawnError))
                        AppendText(spawnError, CommandFailureColor());
                    else if (trafficDirector == null || simulationHost == null)
                        AppendText("SPAWN  simulation unavailable", CommandFailureColor());
                    else
                    {
                        try
                        {
                            ConnectionSpawnPlan plan = trafficDirector.QueueProfile(profileId,
                                spawnOverrides, idHopping, idReactive);
                            recorder?.RecordPlayerAction(command, plan.ConnectionId, true);
                            AppendText("QUEUED  " + plan.ConnectionId.ToLowerInvariant() +
                                " from profile " + plan.ProfileId, normalColor);
                        }
                        catch (System.InvalidOperationException error)
                        {
                            recorder?.RecordPlayerAction(command, null, false);
                            AppendText("SPAWN  " + error.Message, CommandFailureColor());
                        }
                    }
                    break;
                case "infect":
                    if ((tokens.Length != 3 && tokens.Length != 4) ||
                        !TryParseInfectionKind(tokens[2], out InfectionKind injectKind) ||
                        injectKind == InfectionKind.Blackout && tokens.Length == 4)
                        AppendText("USAGE  infect <nodeId> phantom-load|blackout|spawner [load|cadence]", CommandFailureColor());
                    else if (simulationHost == null)
                        AppendText("INFECT  simulation unavailable", CommandFailureColor());
                    else if (tokens.Length == 4 &&
                        (!float.TryParse(tokens[3], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float parsedParameter) ||
                         parsedParameter <= 0f || float.IsNaN(parsedParameter) ||
                         float.IsInfinity(parsedParameter)))
                        AppendText("INFECT  parameter must be a finite positive number", CommandFailureColor());
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
                            applied ? normalColor : CommandFailureColor());
                    }
                    break;
                case "uninfect":
                    if ((tokens.Length != 2 && tokens.Length != 3) ||
                        tokens.Length == 3 && !TryParseInfectionKind(tokens[2], out _))
                        AppendText("USAGE  uninfect <nodeId> [phantom-load|blackout|spawner]", CommandFailureColor());
                    else if (simulationHost == null)
                        AppendText("UNINFECT  simulation unavailable", CommandFailureColor());
                    else
                    {
                        InfectionKind removeKind = InfectionKind.PhantomLoad;
                        if (tokens.Length == 3) TryParseInfectionKind(tokens[2], out removeKind);
                        bool removed = simulationHost.TryRemoveInfection(tokens[1], removeKind,
                            out string removalError);
                        recorder?.RecordPlayerAction(command, null, removed);
                        AppendText(removed ? "REMOVED  " + removeKind + " from " +
                            tokens[1].ToUpperInvariant() : "UNINFECT  " + removalError,
                            removed ? normalColor : CommandFailureColor());
                    }
                    break;
                case "infections":
                    if (tokens.Length != 1 || simulationHost == null)
                        AppendText("USAGE  infections", CommandFailureColor());
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
                        AppendText("DEMANDTEST  generated mode required", CommandFailureColor());
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
                            AppendText("DEMANDTEST  " + error.Message, CommandFailureColor());
                        }
                    }
                    break;
                case "mark":
                    string note = raw.TrimStart().Substring(tokens[0].Length).TrimStart(' ', '\t');
                    if (recorder != null && recorder.Mark(note))
                        AppendText("MARK SAVED  " + System.IO.Path.GetFileName(recorder.OutputPath), normalColor);
                    else AppendText("MARK  " + (recorder?.LastError ?? "recording unavailable"), CommandFailureColor());
                    break;
                case "log":
                    if (tokens.Length != 2)
                        AppendText("USAGE  log save | dir | folder", CommandFailureColor());
                    else if (tokens[1].Equals("dir", System.StringComparison.OrdinalIgnoreCase))
                        AppendText("LOG DIR  " + ConnectionsRunRecorder.LogDirectory, normalColor);
                    else if (tokens[1].Equals("folder", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (ConnectionsRunRecorder.RevealDirectory(out string error))
                            AppendText("LOG FOLDER OPENED", normalColor);
                        else AppendText("LOG FOLDER  " + error, CommandFailureColor());
                    }
                    else if (tokens[1].Equals("save", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (recorder != null && recorder.Save(true))
                            AppendText("LOG SAVED  " + System.IO.Path.GetFileName(recorder.OutputPath), normalColor);
                        else AppendText("LOG SAVE  " + (recorder?.LastError ?? "recording unavailable"), CommandFailureColor());
                    }
                    else AppendText("USAGE  log save | dir | folder", CommandFailureColor());
                    break;
                case "help":
                    AppendText("COMMANDS  inspect|i [-v] <id> [id ...]  load <nodeId>  work <nodeId>", mutedColor);
                    AppendText("ACTION  kill|k <id> [id ...]", mutedColor);
                    AppendText("NODES  scan <nodeId>  clean <nodeId>", mutedColor);
                    AppendText("DEFENSE  probe <linkId>  intercept [payloadId] @ <nodeId>  isolate <nodeId>", mutedColor);
                    AppendText("RECALL  Up/Down  // Enter to run", mutedColor);
                    AppendText("SCROLL  Ctrl/Cmd + Up/Down  Page Up/Down", mutedColor);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    AppendText("DEV  probe reset  // clear Probe cooldown", mutedColor);
                    AppendText("MORE  spawn <profileId> [obj:delivery|loader] [inf:phantom-load|blackout|spawner] [infp:load=N|cadence=N]", mutedColor);
                    AppendText("DEV  spawn basic-threat idhop  // manual hostile ID hopping", mutedColor);
                    AppendText("DEV  spawn basic-threat idreact  // reactive false identity", mutedColor);
                    AppendText("DEV  notifytest im|system|burst [delay=1..30]  // authored samples", mutedColor);
                    AppendText("DEV  calltest [delay=1..30]  // sample call; not a network event", mutedColor);
                    AppendText("DEV  portscan <nodeId> | recon <nodeId>", mutedColor);
                    AppendText("DEV  payload <targetNodeId> [steady|variable]", mutedColor);
                    AppendText("DEV  worm <originNodeId> scan-jammed|port-scanned", mutedColor);
                    AppendText("DEV  infect <nodeId> <kind> [load|cadence]  uninfect <nodeId> [kind]  infections", mutedColor);
                    AppendText("MORE  demandtest  mark  log save|dir|folder  test  help", mutedColor);
#else
                    AppendText("MORE  demandtest  mark  log save|dir|folder  test  help", mutedColor);
#endif
                    break;
                default:
                    AppendText($"UNKNOWN COMMAND  {command}  // type help", CommandFailureColor());
                    break;
            }

            if (commandFailed) CommandFailed?.Invoke();
            if (cleanOverlay == null || !cleanOverlay.IsOpen)
                StartCoroutine(FocusInputNextFrame());
        }

        public static bool TryParseSpawn(string[] tokens, out string profileId,
            out ConnectionSpawnOverrides overrides, out string error)
            => TryParseSpawn(tokens, out profileId, out overrides, out _, out error);

        public static bool TryParseSpawn(string[] tokens, out string profileId,
            out ConnectionSpawnOverrides overrides, out bool idHopping, out string error)
            => TryParseSpawn(tokens, out profileId, out overrides, out idHopping,
                out _, out error);

        public static bool TryParseSpawn(string[] tokens, out string profileId,
            out ConnectionSpawnOverrides overrides, out bool idHopping,
            out bool idReactive, out string error)
        {
            const string usage = "USAGE  spawn <profileId> [obj:delivery|loader] " +
                "[inf:phantom-load|blackout|spawner] [infp:load=N|cadence=N] [idhop|idreact]";
            profileId = null;
            overrides = null;
            idHopping = false;
            idReactive = false;
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
                if (token.Equals("idhop", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (idHopping) { error = "SPAWN  duplicate idhop option"; return false; }
                    idHopping = true;
                }
                else if (token.Equals("idreact", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (idReactive) { error = "SPAWN  duplicate idreact option"; return false; }
                    idReactive = true;
                }
                else if (token.StartsWith("obj:", System.StringComparison.OrdinalIgnoreCase))
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
            if (idHopping && idReactive)
            { error = "SPAWN  idhop and idreact cannot be combined"; return false; }
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

        public static bool TryParseWormEffect(string text, out WormEffect effect)
        {
            if (string.Equals(text, "scan-jammed", System.StringComparison.OrdinalIgnoreCase))
            { effect = WormEffect.ScanJammed; return true; }
            if (string.Equals(text, "port-scanned", System.StringComparison.OrdinalIgnoreCase))
            { effect = WormEffect.PortScanned; return true; }
            effect = default;
            return false;
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
                AppendText("USAGE  kill|k <id> [id ...]", CommandFailureColor());
                return;
            }
            var attempted = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < tokens.Length; i++)
            {
                string id = tokens[i];
                string displayId = id.ToLowerInvariant();
                if (!attempted.Add(id))
                {
                    AppendText("KILL  duplicate connection " + displayId + " (skipped)", CommandFailureColor());
                    continue;
                }
                bool killed = simulationHost != null && simulationHost.KillConnection(id);
                int action = recorder != null ? recorder.RecordPlayerAction(command, id, killed) : 0;
                if (!killed) simulationHost?.NotifyFailedIdentityAddress(id, "kill", action);
                AppendText(killed ? "KILLED  " + displayId :
                    "KILL  no active connection " + displayId,
                    killed ? normalColor : CommandFailureColor());
            }
        }

        private void Clean(string[] tokens, string command)
        {
            if (tokens.Length != 2)
                AppendText("USAGE  clean <nodeId>", CommandFailureColor());
            else if (simulationHost == null)
                AppendText("CLEAN FAILED  simulation unavailable", CommandFailureColor());
            else if (cleanOverlay == null)
                AppendText("CLEAN FAILED  interface unavailable", CommandFailureColor());
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
                    "CLEAN FAILED  UNKNOWN NODE " + nodeId, CommandFailureColor());
            }
        }

        private void Scan(string[] tokens, string command)
        {
            if (tokens.Length != 2)
            {
                AppendText("USAGE  scan <nodeId>", CommandFailureColor());
                return;
            }
            if (simulationHost == null)
            {
                AppendText("SCAN FAILED  simulation unavailable", CommandFailureColor());
                return;
            }
            if (scanPrefab == null || historyContent == null)
            {
                AppendText("SCAN FAILED  output unavailable", CommandFailureColor());
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
                    AppendText("ALREADY SCANNING  " + tokens[1].ToUpperInvariant(), CommandFailureColor());
                    break;
                case ScanStartResult.Busy:
                    AppendText("SCAN BUSY  " + simulationHost.ActiveScan.NodeId, CommandFailureColor());
                    break;
                case ScanStartResult.Jammed:
                    AppendText("SCAN JAMMED  " + tokens[1].ToUpperInvariant(), CommandFailureColor());
                    break;
                default:
                    AppendText("SCAN FAILED  UNKNOWN NODE " + tokens[1].ToUpperInvariant(),
                        CommandFailureColor());
                    break;
            }
        }

        private void Load(string[] tokens)
        {
            if (!TryParseLoad(tokens, out string nodeId, out string error))
            {
                AppendText(error, CommandFailureColor());
                return;
            }
            if (simulationHost == null)
            {
                AppendText("LOAD  simulation unavailable", CommandFailureColor());
                return;
            }
            NodeResources node = simulationHost.FindNodeResources(nodeId);
            if (node == null)
            {
                AppendText("LOAD  unknown node " + nodeId.ToUpperInvariant(), CommandFailureColor());
                return;
            }
            LoadOutput block = AppendOutputBlock(loadPrefab);
            if (block == null)
                AppendText("LOAD  output unavailable", CommandFailureColor());
            else
                block.Bind(node.View.NodeId, simulationHost);
        }

        private void Work(string[] tokens)
        {
            if (!TryParseWork(tokens, out string nodeId, out string error))
            {
                AppendText(error, CommandFailureColor());
                return;
            }
            if (simulationHost == null)
            {
                AppendText("WORK  simulation unavailable", CommandFailureColor());
                return;
            }
            NodeResources node = simulationHost.FindNodeResources(nodeId);
            if (node == null)
            {
                AppendText("WORK  unknown node " + nodeId.ToUpperInvariant(), CommandFailureColor());
                return;
            }
            WorkOutput block = AppendOutputBlock(workPrefab);
            if (block == null)
                AppendText("WORK  output unavailable", CommandFailureColor());
            else
                block.Bind(node.View.NodeId, simulationHost);
        }

        public static bool TryParseWork(string[] tokens, out string nodeId, out string error)
        {
            nodeId = null;
            error = "USAGE  work <nodeId>";
            if (tokens == null || tokens.Length != 2 ||
                !tokens[0].Equals("work", System.StringComparison.OrdinalIgnoreCase))
                return false;
            nodeId = tokens[1];
            error = null;
            return true;
        }

        public static bool TryParseLoad(string[] tokens, out string nodeId, out string error)
        {
            nodeId = null;
            error = "USAGE  load <nodeId>";
            if (tokens == null || tokens.Length != 2 ||
                !tokens[0].Equals("load", System.StringComparison.OrdinalIgnoreCase))
                return false;
            nodeId = tokens[1];
            error = null;
            return true;
        }

        public void FinishClean(string message, bool success)
        {
            AppendText(message, success ? normalColor : errorColor);
            StartCoroutine(FocusInputNextFrame());
        }

        private void Inspect(string[] tokens, string command)
        {
            if (!TryParseInspect(tokens,
                out InspectPresentationMode mode, out string[] ids, out string error))
            {
                AppendText(error, CommandFailureColor());
                return;
            }
            if (simulationHost == null || simulationHost.RuntimeConnections == null)
            {
                AppendText("INSPECT  simulation unavailable", CommandFailureColor());
                return;
            }

            var selected = new List<LogicalConnectionState>();
            foreach (string id in ids)
            {
                LogicalConnectionState match = simulationHost.FindConnection(id);
                if (match == null)
                {
                    int action = recorder != null ? recorder.RecordPlayerAction(command,
                        id, false, reason: "unknown_connection") : 0;
                    simulationHost.NotifyFailedIdentityAddress(id, "inspect", action);
                    AppendText("INSPECT  unknown connection " + id.ToLowerInvariant(), CommandFailureColor());
                    return;
                }
                if (selected.Contains(match))
                {
                    AppendText("INSPECT  duplicate connection " + id.ToLowerInvariant(), CommandFailureColor());
                    return;
                }
                selected.Add(match);
            }
            InspectOutput block = AppendOutputBlock(inspectPrefab);
            if (block == null)
                AppendText("INSPECT  output unavailable", CommandFailureColor());
            else
                block.Bind(selected.ToArray(), simulationHost.PresentationPalette, mode);
        }

        public static bool TryParseIntercept(string[] tokens, out string payloadId, out string nodeId)
        {
            payloadId = null;
            nodeId = null;
            if (tokens == null) return false;
            if (tokens.Length == 3 && tokens[1] == "@")
                nodeId = tokens[2];
            else if (tokens.Length == 4 && tokens[2] == "@" &&
                !string.IsNullOrWhiteSpace(tokens[1]) && tokens[1] != "@")
            {
                payloadId = tokens[1];
                nodeId = tokens[3];
            }
            return !string.IsNullOrWhiteSpace(nodeId) && nodeId != "@";
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
                bottomScrollRoutine = StartCoroutine(ScrollToBottomNextFrame());
            return instance;
        }

        // Anchor the first visible card/entry while resolved inspection rows shrink.
        internal void UpdateHistoryLayout(System.Action updateLayout)
        {
            if (historyScroll == null || historyContent == null || historyScroll.viewport == null)
            {
                updateLayout();
                return;
            }
            Canvas.ForceUpdateCanvases();
            bool atBottom = historyScroll.verticalNormalizedPosition <= 0.01f;
            RectTransform viewport = historyScroll.viewport;
            RectTransform anchor = null;
            float topBefore = 0f;
            var corners = new Vector3[4];
            bool Visible(RectTransform rect)
            {
                rect.GetWorldCorners(corners);
                float top = viewport.InverseTransformPoint(corners[1]).y;
                float bottom = viewport.InverseTransformPoint(corners[0]).y;
                if (top <= viewport.rect.yMin || bottom >= viewport.rect.yMax) return false;
                anchor = rect;
                topBefore = top;
                return true;
            }
            if (!atBottom)
                foreach (RectTransform child in historyContent)
                {
                    if (!child.gameObject.activeInHierarchy) continue;
                    InspectEntry[] entries = child.GetComponentsInChildren<InspectEntry>();
                    if (entries.Length > 0)
                    {
                        foreach (InspectEntry entry in entries)
                            if (Visible((RectTransform)entry.transform)) break;
                    }
                    else Visible(child);
                    if (anchor != null) break;
                }
            updateLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyContent);
            Canvas.ForceUpdateCanvases();
            if (atBottom)
                historyScroll.verticalNormalizedPosition = 0f;
            else if (anchor != null)
            {
                anchor.GetWorldCorners(corners);
                float topAfter = viewport.InverseTransformPoint(corners[1]).y;
                // If the text being read disappeared, reveal its retained summary row.
                if (topBefore - viewport.rect.yMax >= anchor.rect.height)
                    topBefore = viewport.rect.yMax;
                Vector3 correction = historyContent.parent.InverseTransformVector(
                    viewport.TransformVector(new Vector3(0f, topBefore - topAfter, 0f)));
                historyContent.anchoredPosition += new Vector2(0f, correction.y);
                historyScroll.verticalNormalizedPosition = Mathf.Clamp01(
                    historyScroll.verticalNormalizedPosition);
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
            bottomScrollRoutine = null;
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
