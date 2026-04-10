using System.Collections.Generic;
using UnityEngine;

public enum CleanState
{
    None,
    Splash,
    Minigame,
    Result
}

public enum CleanMinigameType
{
    None,
    ProcessKiller,
    PacketSniffer,
    HexScrubber
}

public enum CleanModifierType
{
    None
}

public class CleanSession
{
    public NodeView node;
    public InfectionType infectionType;
    public CleanMinigameType minigameType;
    public int difficultyTier;
    public List<CleanModifierType> modifiers = new();

    public CleanState state = CleanState.None;
    public int ticksRemainingInState = 0;

    public List<ProcessKillerEntry> processEntries = new();
    public string targetPid;
    public bool wasSuccessful;
    public string resultMessage;
}

public class ProcessKillerEntry
{
    public string pid;
    public int cpuPercent;
    public string processName;
}

public class CleanDirector : MonoBehaviour
{
    [Header("References")]
    public NetworkRuntime networkRuntime;
    public CleanMinigameOverlayView overlayView;
    public ScoreDirector scoreDirector;

    [Header("Timing")]
    public int splashTicks = 4;
    public int resultTicks = 2;

    private CommandDirector commandDirector;
    private CleanSession activeSession;

    public void SetCommandDirector(CommandDirector director)
    {
        commandDirector = director;
    }

    public bool HasActiveClean()
    {
        return activeSession != null;
    }

    public void Tick()
    {
        if (activeSession == null)
            return;

        switch (activeSession.state)
        {
            case CleanState.Splash:
                activeSession.ticksRemainingInState--;

                if (activeSession.ticksRemainingInState <= 0)
                    EnterMinigame(activeSession);

                break;

            case CleanState.Minigame:
                // input-driven for now
                break;

            case CleanState.Result:
                activeSession.ticksRemainingInState--;

                if (activeSession.ticksRemainingInState <= 0)
                    EndSession();

                break;
        }
    }

    public void SubmitInput(string rawInput)
    {
        if (activeSession == null || activeSession.state != CleanState.Minigame)
            return;

        string trimmed = string.IsNullOrWhiteSpace(rawInput)
            ? string.Empty
            : rawInput.Trim();

        switch (activeSession.minigameType)
        {
            case CleanMinigameType.ProcessKiller:
                ResolveProcessKiller(trimmed);
                return;
        }
    }

    public bool TryStartClean(NodeView node, out string failureReason)
    {
        failureReason = null;

        if (node == null)
        {
            failureReason = "clean failed: node not found";
            return false;
        }

        if (activeSession != null)
        {
            failureReason = "clean failed: another clean is already active";
            return false;
        }

        if (!node.IsInfected)
        {
            failureReason = $"clean failed: {node.nodeId} is not infected";
            return false;
        }

        InfectionType infectionType = node.ActiveInfectionType;
        CleanMinigameType minigameType = ResolveMinigameType(infectionType);

        if (minigameType == CleanMinigameType.None)
        {
            failureReason = $"clean failed: no minigame mapped for {infectionType}";
            return false;
        }

        activeSession = new CleanSession
        {
            node = node,
            infectionType = infectionType,
            minigameType = minigameType,
            difficultyTier = 1,
            state = CleanState.Splash,
            ticksRemainingInState = Mathf.Max(1, splashTicks)
        };

        BuildMinigameData(activeSession);

        if (overlayView != null)
        {
            overlayView.Show();
            overlayView.ShowSplash(activeSession);
            overlayView.PositionNearNode(node);
        }

        return true;
    }

    private void EnterMinigame(CleanSession session)
    {
        session.state = CleanState.Minigame;

        if (overlayView != null)
            overlayView.ShowMinigame(session);

        UIFocusDirector.Instance?.RefreshFocus();
    }

    private void BuildMinigameData(CleanSession session)
    {
        if (session == null)
            return;

        switch (session.minigameType)
        {
            case CleanMinigameType.ProcessKiller:
                BuildProcessKillerData(session);
                return;
        }
    }

    private void BuildProcessKillerData(CleanSession session)
    {
        session.processEntries.Clear();

        string[] names =
        {
            "syncd",
            "minerd",
            "auth",
            "cache",
            "worker",
            "kernel",
            "logger",
            "proxy"
        };

        int rowCount = GetProcessKillerRowCount(session.difficultyTier);
        int targetIndex = Random.Range(0, rowCount);

        for (int i = 0; i < rowCount; i++)
        {
            ProcessKillerEntry entry = new ProcessKillerEntry
            {
                pid = Random.Range(1000, 9999).ToString(),
                processName = names[Random.Range(0, names.Length)],
                cpuPercent = Random.Range(8, 45)
            };

            session.processEntries.Add(entry);
        }

        session.processEntries[targetIndex].cpuPercent = GetTargetCpu(session.difficultyTier);
        session.targetPid = session.processEntries[targetIndex].pid;

        // ensure unique highest CPU
        for (int i = 0; i < session.processEntries.Count; i++)
        {
            if (i == targetIndex)
                continue;

            if (session.processEntries[i].cpuPercent >= session.processEntries[targetIndex].cpuPercent)
                session.processEntries[i].cpuPercent = session.processEntries[targetIndex].cpuPercent - Random.Range(6, 18);
        }
    }

    private int GetProcessKillerRowCount(int difficultyTier)
    {
        return difficultyTier switch
        {
            <= 1 => 5,
            2 => 6,
            _ => 7
        };
    }

    private int GetTargetCpu(int difficultyTier)
    {
        return difficultyTier switch
        {
            <= 1 => Random.Range(82, 95),
            2 => Random.Range(72, 90),
            _ => Random.Range(62, 84)
        };
    }

    private void ResolveProcessKiller(string submittedPid)
    {
        if (activeSession == null)
            return;

        bool success = string.Equals(
            submittedPid,
            activeSession.targetPid,
            System.StringComparison.OrdinalIgnoreCase
        );

        activeSession.wasSuccessful = success;

        int currentTick = GameController.Instance != null
            ? GameController.Instance.CurrentTick
            : 0;

        if (success)
        {
            bool removed = activeSession.node.RemoveInfection(activeSession.infectionType);
            activeSession.resultMessage = removed
                ? "NODE RESTORED"
                : "CLEAN COMPLETE";

            scoreDirector?.RecordCleanResult(
                activeSession.node,
                activeSession.infectionType,
                true,
                currentTick
            );

            commandDirector?.Log(
                $"{commandDirector.FormatPrefix(ConsoleLogPrefix.Flow)}  <b>CLEAN</b>  {activeSession.node.nodeId}  {commandDirector.FormatSuccess("restored")}"
            );
        }
        else
        {
            activeSession.resultMessage = "CLEAN FAILED";

            scoreDirector?.RecordCleanResult(
                activeSession.node,
                activeSession.infectionType,
                false,
                currentTick
            );

            commandDirector?.Log(
                $"{commandDirector.FormatPrefix(ConsoleLogPrefix.Error)}  <b>CLEAN</b>  {activeSession.node.nodeId}  {commandDirector.FormatFailure("failed")}"
            );
        }

        EnterResult(activeSession);
    }

    private void EnterResult(CleanSession session)
    {
        session.state = CleanState.Result;
        session.ticksRemainingInState = Mathf.Max(1, resultTicks);

        if (overlayView != null)
            overlayView.ShowResult(session);

        UIFocusDirector.Instance?.RefreshFocus();
    }

    private void EndSession()
    {
        if (overlayView != null)
            overlayView.Hide();

        activeSession = null;

        UIFocusDirector.Instance?.RefreshFocus();
    }

    private CleanMinigameType ResolveMinigameType(InfectionType infectionType)
    {
        return infectionType switch
        {
            InfectionType.Blackout => CleanMinigameType.ProcessKiller,
            InfectionType.Spawner => CleanMinigameType.ProcessKiller,
            InfectionType.Throttle => CleanMinigameType.ProcessKiller,
            _ => CleanMinigameType.None
        };
    }
}