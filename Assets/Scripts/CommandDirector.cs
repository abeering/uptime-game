using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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

            case CommandType.DeepScan:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("ERROR usage: deepscan <packet>");
                    audioManager?.PlayCommandRejected();
                    return;
                }

                StartDeepScan(command.packetId);
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

            case CommandType.Spawn:
                if (command.routeNodeIds == null || command.routeNodeIds.Length < 2)
                {
                    Log("ERROR usage: spawn <class> <kind> <node1> <node2> [node3...]");
                    audioManager?.PlayCommandRejected();
                    return;
                }

                StartSpawn(command.packetClass, command.packetKind, command.routeNodeIds);
                return;

            default:
                Log("ERROR unknown command");
                audioManager?.PlayCommandRejected();
                return;
        }
    }

    public void Tick()
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

    public void NotifyPacketReachedNode(PacketView packet, NodeView node)
    {
        for (int i = 0; i < operations.Count; i++)
        {
            if (operations[i] is BlockOperation block)
            {
                block.TryTrigger(packet, node, this);
            }
        }
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

    private void StartSpawn(PacketClass packetClass, PacketKind packetKind, string[] routeNodeIds)
    {
        if (trafficDirector == null)
        {
            Log("SPAWN failed: no traffic director");
            audioManager?.PlayCommandRejected();
            return;
        }

        bool success = trafficDirector.DebugSpawnPacket(packetClass, packetKind, routeNodeIds, out string message);
        Log(message);

        if (!success)
            return;
    }

    private void StartScan(string packetId, int durationTicks = 4)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            Log($"SCAN failed: packet {packetId} not found");
            AudioManager.Instance?.PlayCommandRejected();
            return;
        }

        scanDirector.StartScan(packet);

        // ScanOperation scan = new ScanOperation
        // {
        //     id = nextScanId,
        //     displayId = $"scan{nextScanId}",
        //     packetId = packetId,
        //     remainingTicks = durationTicks,
        //     totalTicks = durationTicks
        // };

        // nextScanId++;
        // operations.Add(scan);

        // packet.BeginQuickScanVisual();
        // packet.UpdateScanVisual(0f);

        AudioManager.Instance?.PlayCommandAccepted();
        Log($"SCAN started: {packetId} ({durationTicks}s)");
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
        Log($"TRACE started: {packetId} ({durationTicks}s)");
    }

    private void StartDeepScan(string packetId, int durationTicks = 10)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            Log($"DEEPSCAN failed: packet {packetId} not found");
            audioManager?.PlayCommandRejected();
            return;
        }

        DeepScanOperation scan = new DeepScanOperation
        {
            id = nextScanId,
            displayId = $"deepscan{nextScanId}",
            packetId = packetId,
            remainingTicks = durationTicks,
            totalTicks = durationTicks
        };

        nextScanId++;
        operations.Add(scan);

        packet.BeginDeepScanVisual();
        packet.UpdateScanVisual(0f);

        audioManager?.PlayCommandAccepted();
        Log($"DEEPSCAN started: {packetId} ({durationTicks}s)");
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
        Log($"BLOCK {block.displayId} armed: {packetId} @ {nodeId}");
    }

    private void StartBoost(string packetId)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            Log($"BOOST failed: packet {packetId} not found");
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
        Log($"BOOST complete: {packetId} speed {previousSpeed} -> {packet.baseSpeed}");
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
        sb.AppendLine("BLOCKS");
        bool hasBlocks = false;

        for (int i = 0; i < operations.Count; i++)
        {
            if (operations[i] is BlockOperation block)
            {
                hasBlocks = true;
                sb.AppendLine(block.GetOperationsLine());
            }
        }

        if (!hasBlocks)
            sb.AppendLine("none");
    }

    public void Log(string message)
    {
        Debug.Log($"[Command] {message}");
        OnLogMessage?.Invoke(message);
    }
}