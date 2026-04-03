using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum ConsoleLogPrefix
{
    Intel,
    Block,
    Flow,
    Error
}

public class CommandDirector : MonoBehaviour
{
    public NetworkRuntime networkRuntime;
    public TrafficDirector trafficDirector;
    public ScanDirector scanDirector;

    public event Action<string> OnLogMessage;

    private readonly List<Operation> operations = new();

    private int nextScanId = 1;
    private int nextBlockId = 1;

    private AudioManager audioManager;
    public AudioManager AudioManager => audioManager;

    void Awake() {
        audioManager = AudioManager.Instance;
        if (scanDirector != null)
            scanDirector.SetCommandDirector(this);
    }

    private ScanLogTheme GetConsoleTheme()
    {
        if (scanDirector != null && scanDirector.GetLogTheme() != null)
            return scanDirector.GetLogTheme();

        return null;
    }

    private Color GetStageColor(ScanStage stage)
    {
        ScanLogTheme theme = GetConsoleTheme();
        if (theme == null)
        {
            return stage switch
            {
                ScanStage.Unknown => new Color(0.53f, 0.53f, 0.53f, 1f),
                ScanStage.Probable => new Color(1.00f, 0.82f, 0.40f, 1f),
                ScanStage.Likely => new Color(0.49f, 1.00f, 0.42f, 1f),
                ScanStage.Confirmed => new Color(0.40f, 0.80f, 1.00f, 1f),
                _ => Color.white
            };
        }

        return stage switch
        {
            ScanStage.Unknown => theme.stageUnknown,
            ScanStage.Probable => theme.stageProbable,
            ScanStage.Likely => theme.stageLikely,
            ScanStage.Confirmed => theme.stageConfirmed,
            _ => Color.white
        };
    }

    private Color GetVisibleClassColor(VisibleClass visibleClass)
    {
        ScanLogTheme theme = GetConsoleTheme();
        if (theme == null)
        {
            return visibleClass switch
            {
                VisibleClass.Unknown => new Color(0.53f, 0.53f, 0.53f, 1f),
                VisibleClass.Benign => new Color(0.72f, 1.00f, 0.72f, 1f),
                VisibleClass.Threat => new Color(1.00f, 0.42f, 0.42f, 1f),
                VisibleClass.Priority => new Color(0.40f, 0.80f, 1.00f, 1f),
                _ => Color.white
            };
        }

        return visibleClass switch
        {
            VisibleClass.Unknown => theme.classUnknown,
            VisibleClass.Benign => theme.classBenign,
            VisibleClass.Threat => theme.classThreat,
            VisibleClass.Priority => theme.classPriority,
            _ => Color.white
        };
    }

    private Color GetPacketClassColor(PacketClass packetClass)
    {
        ScanLogTheme theme = GetConsoleTheme();
        if (theme == null)
        {
            return packetClass switch
            {
                PacketClass.Benign => new Color(0.72f, 1.00f, 0.72f, 1f),
                PacketClass.Threat => new Color(1.00f, 0.42f, 0.42f, 1f),
                PacketClass.Priority => new Color(0.40f, 0.80f, 1.00f, 1f),
                _ => Color.white
            };
        }

        return packetClass switch
        {
            PacketClass.Benign => theme.classBenign,
            PacketClass.Threat => theme.classThreat,
            PacketClass.Priority => theme.classPriority,
            _ => Color.white
        };
    }

    private Color GetMutedColor()
    {
        ScanLogTheme theme = GetConsoleTheme();
        if (theme != null)
            return theme.muted;

        return new Color(0.67f, 0.67f, 0.67f, 0.53f);
    }

    private Color GetFailureColor()
    {
        ScanLogTheme theme = GetConsoleTheme();
        if (theme != null)
            return theme.classThreat;

        return new Color(1.00f, 0.42f, 0.42f, 1f);
    }

    private Color GetSuccessColor()
    {
        ScanLogTheme theme = GetConsoleTheme();
        if (theme != null)
            return theme.classBenign;

        return new Color(0.72f, 1.00f, 0.72f, 1f);
    }

    public string FormatPrefix(ConsoleLogPrefix prefix)
    {
        string text = prefix switch
        {
            ConsoleLogPrefix.Intel => "INTEL",
            ConsoleLogPrefix.Block => "BLOCK",
            ConsoleLogPrefix.Flow => "FLOW",
            ConsoleLogPrefix.Error => "ERROR",
            _ => "LOG"
        };

        return RichTextUtil.Colorize(text, GetMutedColor(), true);
    }

    public string FormatPacketId(string packetId)
    {
        if (string.IsNullOrWhiteSpace(packetId))
            return "----";

        return $"<b>{packetId}</b>";
    }

    public string FormatPacketId(PacketView packet)
    {
        if (packet == null)
            return "----";

        return FormatPacketId(packet.packetId);
    }

    public string FormatStage(ScanStage stage)
    {
        string label = scanDirector != null
            ? scanDirector.GetStageShortLabelPublic(stage)
            : stage.ToString().ToUpperInvariant();

        bool bold = stage != ScanStage.Unknown;
        return RichTextUtil.Colorize(label, GetStageColor(stage), bold);
    }

    public string FormatVisibleClass(PacketView packet)
    {
        if (packet == null)
            return "----";

        string label = scanDirector != null
            ? scanDirector.GetVisibleClassShortLabelPublic(packet)
            : packet.GetVisibleClass().ToString().ToUpperInvariant();

        bool bold = packet.GetVisibleClass() != VisibleClass.Unknown;
        return RichTextUtil.Colorize(label, GetVisibleClassColor(packet.GetVisibleClass()), bold);
    }

    public string FormatPacketClass(PacketClass packetClass)
    {
        string label = scanDirector != null
            ? scanDirector.GetPacketClassShortLabelPublic(packetClass)
            : packetClass.ToString().ToUpperInvariant();

        return RichTextUtil.Colorize(label, GetPacketClassColor(packetClass), true);
    }

    public string FormatMuted(string text, bool bold = false)
    {
        return RichTextUtil.Colorize(text, GetMutedColor(), bold);
    }

    public string FormatFailure(string text)
    {
        return RichTextUtil.Colorize(text.ToUpperInvariant(), GetFailureColor(), true);
    }

    public string FormatSuccess(string text)
    {
        return RichTextUtil.Colorize(text.ToUpperInvariant(), GetSuccessColor(), true);
    }

    public void LogIntelStageChange(PacketView packet, ScanStage newStage)
    {
        if (packet == null || newStage == ScanStage.Unknown)
            return;

        string prefix = FormatPrefix(ConsoleLogPrefix.Intel);
        string packetId = FormatPacketId(packet);
        string stageLabel = FormatStage(newStage);
        string classLabel = FormatVisibleClass(packet);

        if (newStage == ScanStage.Confirmed)
            Log($"{prefix}  {packetId}  {stageLabel}  {classLabel}");
        else
            Log($"{prefix}  {packetId}  {stageLabel}  ({classLabel})");
    }

    public void LogIntelReveal(PacketView packet, IntelRevealType revealType, string revealedValue)
    {
        if (packet == null || string.IsNullOrWhiteSpace(revealedValue))
            return;

        string prefix = FormatPrefix(ConsoleLogPrefix.Intel);
        string packetId = FormatPacketId(packet);

        switch (revealType)
        {
            case IntelRevealType.Kind:
                Log($"{prefix}  {packetId}  <b>KIND</b>  {revealedValue}");
                break;

            case IntelRevealType.InfectionType:
                Log($"{prefix}  {packetId}  <b>INFECTION</b>  {revealedValue}");
                break;

            case IntelRevealType.Keyword:
                Log($"{prefix}  {packetId}  <b>KEYWORD</b>  {revealedValue}");
                break;
        }
    }

    public void LogTraceStarted(string packetId, int durationTicks)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Intel);
        Log($"{prefix}  {FormatPacketId(packetId)}  <b>TRACE</b>  {FormatMuted($"started ({durationTicks}s)")}");
    }

    public void LogTraceReveal(PacketView packet)
    {
        if (packet == null)
            return;

        string prefix = FormatPrefix(ConsoleLogPrefix.Intel);
        string packetId = FormatPacketId(packet);
        Log($"{prefix}  {packetId}  <b>TRACE</b>  source={packet.sourceAddress}  destination={packet.GetDestinationName()}");
    }

    public void LogTraceFailed(string displayId, string packetId, string reason)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Intel);
        Log($"{prefix}  <b>{displayId}</b>  {FormatFailure("trace failed")}  {FormatPacketId(packetId)}  {reason}");
    }

    public void LogTraceCancelled(string displayId)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Intel);
        Log($"{prefix}  <b>{displayId}</b>  {FormatMuted("TRACE CANCELLED", true)}");
    }

    public void LogBlockArmed(string displayId, string packetId, string nodeId)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Block);
        Log($"{prefix}  <b>{displayId}</b>  {FormatMuted("ARMED", true)}  {FormatPacketId(packetId)}  @ {nodeId}");
    }

    public void LogBlockTriggered(string displayId, string packetId, string nodeId, string verb)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Block);
        string action = string.IsNullOrWhiteSpace(verb) ? "BLOCKED" : verb.ToUpperInvariant();
        Log($"{prefix}  <b>{displayId}</b>  {FormatFailure(action)}  {FormatPacketId(packetId)}  @ {nodeId}");
    }

    public void LogBlockFailed(string displayId, string packetId, string reason)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Block);
        Log($"{prefix}  <b>{displayId}</b>  {FormatFailure("failed")}  {FormatPacketId(packetId)}  {reason}");
    }

    public void LogBlockCancelled(string displayId)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Block);
        Log($"{prefix}  <b>{displayId}</b>  {FormatMuted("CANCELLED", true)}");
    }

    public void LogScanStarted(string packetId, int durationTicks)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Intel);
        Log($"{prefix}  {FormatPacketId(packetId)}  <b>SCAN</b>  {FormatMuted($"started ({durationTicks}s)")}");
    }

    public void LogBoostApplied(string packetId)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Flow);
        Log($"{prefix}  {FormatPacketId(packetId)}  <b>BOOST</b>");
    }

    public void LogCommandError(string message)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Error);
        Log($"{prefix}  {FormatFailure("FAILED")}  {message}");
    }

    public void LogThrottleApplied(ConnectionView connection, int amount)
    {
        string prefix = FormatPrefix(ConsoleLogPrefix.Flow);
        string edgeId = connection != null ? connection.connectionId : "----";
        string value = connection != null ? $"L{connection.latency}+{amount}" : amount.ToString();

        Log($"{prefix}  <b>{edgeId}</b>  <b>THROTTLE</b>  {FormatMuted(value)}");
    }

    public void Execute(ParsedCommand command)
    {
        if (command == null)
        {
            Log("ERROR null command");
            audioManager?.PlayCommandRejected();
            return;
        }

        switch (command.type)
        {
            case CommandType.Scan:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("ERROR usage: scan <packet>");
                    audioManager?.PlayCommandRejected();
                    return;
                }

                StartScan(command.packetId);
                return;

            case CommandType.Trace:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("ERROR usage: trace <packet>");
                    audioManager?.PlayCommandRejected();
                    return;
                }

                StartTrace(command.packetId);
                return;

            case CommandType.Block:
                if (string.IsNullOrWhiteSpace(command.packetId) || string.IsNullOrWhiteSpace(command.nodeId))
                {
                    Log("ERROR usage: block <packet> @ <node>");
                    audioManager?.PlayCommandRejected();
                    return;
                }

                StartBlock(command.packetId, command.nodeId);
                return;

            case CommandType.Cancel:
                if (string.IsNullOrWhiteSpace(command.operationId))
                {
                    Log("ERROR usage: cancel <operationId>");
                    audioManager?.PlayCommandRejected();
                    return;
                }

                CancelOperation(command.operationId);
                return;

            case CommandType.Boost:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("ERROR usage: boost <packet>");
                    audioManager?.PlayCommandRejected();
                    return;
                }


                StartBoost(command.packetId);
                return;

            case CommandType.Throttle:
                if (string.IsNullOrWhiteSpace(command.connectionId))
                {
                    Log("ERROR usage: throttle <connection> <amount>");
                    audioManager?.PlayCommandRejected();
                    return;
                }

                StartThrottle(command.connectionId, command.throttleAmount);
                return;

            case CommandType.Spawn:
                if (command.routeNodeIds == null || command.routeNodeIds.Length < 2)
                {
                    Log("ERROR usage: spawn <class> <kind> <node1> <node2> [node3...] [kw:name] [inf:type] [infrule:first|nth:N|any|destination] [infallowreinfect:true|false]");
                    audioManager?.PlayCommandRejected();
                    return;
                }

                StartSpawn(
                    command.packetClass,
                    command.packetKind,
                    command.routeNodeIds,
                    command.spawnKeywordSpecs,
                    command.spawnInfectionType,
                    command.spawnInfectionTargetRule,
                    command.spawnInfectionNthNode,
                    command.spawnAllowAlreadyInfectedNode
                );
                return;

            case CommandType.AutoSpawn:
                SetAutoSpawn(command.autoSpawnMode);
                return;

            default:
                Log("ERROR unknown command");
                audioManager?.PlayCommandRejected();
                return;
        }
    }

    public void ProcessTick()
    {
        if (scanDirector != null)
            scanDirector.Tick();

        for (int i = 0; i < operations.Count; i++)
        {
            operations[i].Tick(this);
        }

        for (int i = operations.Count - 1; i >= 0; i--)
        {
            operations[i].UpdateLinger(1);

            if (operations[i].ShouldRemove())
            {
                Debug.Log($"[CommandDirector] removing operation {operations[i].displayId}");
                operations.RemoveAt(i);
            }
        }
    }

    public bool ResolvePacketArrivalIntercepts(PacketView packet, NodeView node)
    {
        if (packet == null || node == null)
            return false;

        for (int i = 0; i < operations.Count; i++)
        {
            var op = operations[i];
            if (op == null || op.isFinished)
                continue;

            if (op is BlockOperation block)
            {
                block.TryTrigger(packet, node, this);

                if (packet.isRemoved)
                    return true;
            }
        }

        return false;
    }

    public void NotifyPacketRemoved(string packetId, string reason)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);
        
        if (scanDirector != null)
            scanDirector.RemovePacket(packet);

        for (int i = 0; i < operations.Count; i++)
        {
            operations[i].OnPacketRemoved(packetId, reason, this);
        }
    }

    private void StartSpawn(
        PacketClass packetClass,
        PacketKind packetKind,
        string[] routeNodeIds,
        List<string> keywordSpecs,
        InfectionType? infectionType,
        InfectionTargetRule? infectionTargetRule,
        int infectionNthNode,
        bool allowAlreadyInfectedNode)
    {
        if (trafficDirector == null)
        {
            Log("SPAWN failed: no traffic director");
            audioManager?.PlayCommandRejected();
            return;
        }

        bool success = trafficDirector.DebugSpawnPacket(
            packetClass,
            packetKind,
            routeNodeIds,
            keywordSpecs,
            infectionType,
            infectionTargetRule,
            infectionNthNode,
            allowAlreadyInfectedNode,
            out string message
        );

        Log(message);

        if (!success)
        {
            audioManager?.PlayCommandRejected();
            return;
        }

        audioManager?.PlayCommandAccepted();
    }

    private void StartScan(string packetId, int durationTicks = 4)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            LogCommandError($"scan failed: {packetId} not found");
            AudioManager.Instance?.PlayCommandRejected();
            return;
        }

        scanDirector.StartScan(packet);

        AudioManager.Instance?.PlayCommandAccepted();
        LogScanStarted(packetId, durationTicks);
    }

    private void StartTrace(string packetId, int durationTicks = 4)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            Log($"TRACE failed: packet {packetId} not found");
            AudioManager.Instance?.PlayCommandRejected();
            return;
        }

        TraceOperation trace = new TraceOperation
        {
            id = nextScanId,
            displayId = $"trace{nextScanId}",
            packetId = packetId,
            remainingTicks = durationTicks,
            totalTicks = durationTicks
        };

        nextScanId++;
        operations.Add(trace);

        AudioManager.Instance?.PlayCommandAccepted();
        LogTraceStarted(packetId, durationTicks);
    }

    private void StartBlock(string packetId, string nodeId)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);
        NodeView node = networkRuntime.GetNode(nodeId);

        if (packet == null)
        {
            Log($"BLOCK failed: packet {packetId} not found");
            audioManager?.PlayCommandRejected();
            return;
        }

        if (node == null)
        {
            Log($"BLOCK failed: node {nodeId} not found");
            audioManager?.PlayCommandRejected();
            return;
        }

        BlockOperation block = new BlockOperation
        {
            id = nextBlockId,
            displayId = $"block{nextBlockId}",
            packetId = packetId,
            nodeId = nodeId
        };

        nextBlockId++;
        operations.Add(block);

        audioManager?.PlayCommandAccepted();
        LogBlockArmed(block.displayId, packetId, nodeId);
    }

    private void StartBoost(string packetId)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            LogCommandError($"boost failed: {packetId} not found");
            audioManager?.PlayCommandRejected();
            return;
        }

        if (!packet.IsVisiblePriority())
        {
            Log($"BOOST failed: {packetId} is not identified as priority");
            audioManager?.PlayCommandRejected();
            return;
        }

        int previousSpeed = packet.baseSpeed;

        if (!packet.TryBoost())
        {
            Log($"BOOST failed: {packetId} cannot move faster");
            audioManager?.PlayCommandRejected();
            return;
        }

        audioManager?.PlayCommandAccepted();
        LogBoostApplied(packetId);
    }

    private void StartThrottle(string connectionId, int amount)
    {
        ConnectionView connection = networkRuntime != null
            ? networkRuntime.GetConnection(connectionId)
            : null;

        if (connection == null)
        {
            LogCommandError($"throttle failed: {connectionId} not found");
            audioManager?.PlayCommandRejected();
            return;
        }

        if (amount < 0)
        {
            LogCommandError($"throttle failed: amount must be >= 0");
            audioManager?.PlayCommandRejected();
            return;
        }

        connection.SetThrottle(amount);

        audioManager?.PlayCommandAccepted();
        LogThrottleApplied(connection, amount);
    }

    private void CancelOperation(string operationId)
    {
        for (int i = 0; i < operations.Count; i++)
        {
            Operation op = operations[i];

            if (!string.Equals(op.displayId, operationId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!op.CanCancel())
            {
                Log($"CANCEL failed: {op.displayId} cannot be cancelled");
                audioManager?.PlayCommandRejected();
                return;
            }

            audioManager?.PlayCommandAccepted();
            op.Cancel(this);
            return;
        }

        Log($"CANCEL failed: {operationId} not found");
        audioManager?.PlayCommandRejected();
    }

    public void AppendOperationsPanel(StringBuilder sb)
    {
        ScanLogTheme theme = GetConsoleTheme();
        Color mutedColor = theme != null
            ? theme.muted
            : new Color(0.67f, 0.67f, 0.67f, 0.53f);

        Color armedColor = theme != null
            ? theme.classBenign
            : new Color(0.72f, 1.00f, 0.72f, 1f);

        sb.AppendLine("<b>BLOCKS</b>");

        bool hasBlocks = false;
        int blockIndex = 1;

        for (int i = 0; i < operations.Count; i++)
        {
            if (operations[i] is not BlockOperation block)
                continue;

            hasBlocks = true;

            sb.AppendLine($"B{blockIndex}  <b>{block.displayId}</b>  target=<b>{block.packetId}</b>  @ {block.nodeId}");

            if (block.isFinished)
                sb.AppendLine($"    {RichTextUtil.Colorize("finished", mutedColor)}");
            else
                sb.AppendLine($"    {RichTextUtil.Colorize("armed", armedColor, true)}");

            sb.AppendLine();
            blockIndex++;
        }

        if (!hasBlocks)
            sb.AppendLine(RichTextUtil.Colorize("none", mutedColor));
    }

    private void SetAutoSpawn(string mode)
    {
        if (trafficDirector == null)
        {
            Log("AUTOSPAWN failed: no traffic director");
            audioManager?.PlayCommandRejected();
            return;
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            Log($"AUTOSPAWN is {(trafficDirector.autoSpawnEnabled ? "ON" : "OFF")}");
            return;
        }

        mode = mode.ToLowerInvariant();

        if (mode == "on")
        {
            trafficDirector.autoSpawnEnabled = true;
        }
        else if (mode == "off")
        {
            trafficDirector.autoSpawnEnabled = false;
        }
        else if (mode == "toggle")
        {
            trafficDirector.autoSpawnEnabled = !trafficDirector.autoSpawnEnabled;
        }
        else
        {
            Log("ERROR usage: autospawn [on|off|toggle]");
            audioManager?.PlayCommandRejected();
            return;
        }

        Log($"AUTOSPAWN {(trafficDirector.autoSpawnEnabled ? "ENABLED" : "DISABLED")}");
        audioManager?.PlayCommandAccepted();
    }

    public void Log(string message)
    {
        Debug.Log($"[Command] {message}");
        OnLogMessage?.Invoke(message);
    }
}