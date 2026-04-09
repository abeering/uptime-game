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
}

public class CleanDirector : MonoBehaviour
{
    [Header("References")]
    public NetworkRuntime networkRuntime;
    public CleanMinigameOverlayView overlayView;

    [Header("Timing")]
    public int splashTicks = 2;

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
                // real minigame loop later
                break;

            case CleanState.Result:
                activeSession.ticksRemainingInState--;

                if (activeSession.ticksRemainingInState <= 0)
                    EndSession();

                break;
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
            overlayView.ShowMinigameStub(session);
    }

    private void EndSession()
    {
        if (overlayView != null)
            overlayView.Hide();

        activeSession = null;
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