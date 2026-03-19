using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CommandDirector : MonoBehaviour
{
    public NetworkRuntime networkRuntime;
    public TrafficDirector trafficDirector;

    public event Action<string> OnLogMessage;

    private readonly List<Operation> operations = new();

    private int nextScanId = 1;
    private int nextBlockId = 1;

    public void Execute(ParsedCommand command)
    {
        if (command == null)
        {
            Log("ERROR null command");
            return;
        }

        switch (command.type)
        {
            case CommandType.Scan:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("ERROR usage: scan <packet>");
                    return;
                }

                StartScan(command.packetId);
                return;

            case CommandType.DeepScan:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("ERROR usage: deepscan <packet>");
                    return;
                }

                StartDeepScan(command.packetId);
                return;

            case CommandType.Trace:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("ERROR usage: trace <packet>");
                    return;
                }

                StartTrace(command.packetId);
                return;

            case CommandType.Block:
                if (string.IsNullOrWhiteSpace(command.packetId) || string.IsNullOrWhiteSpace(command.nodeId))
                {
                    Log("ERROR usage: block <packet> @ <node>");
                    return;
                }

                StartBlock(command.packetId, command.nodeId);
                return;

            case CommandType.Cancel:
                if (string.IsNullOrWhiteSpace(command.operationId))
                {
                    Log("ERROR usage: cancel <operationId>");
                    return;
                }

                CancelOperation(command.operationId);
                return;

            case CommandType.Boost:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("ERROR usage: boost <packet>");
                    return;
                }

                StartBoost(command.packetId);
                return;

            case CommandType.Spawn:
                if (command.routeNodeIds == null || command.routeNodeIds.Length < 2)
                {
                    Log("ERROR usage: spawn <class> <kind> <node1> <node2> [node3...]");
                    return;
                }

                StartSpawn(command.packetClass, command.packetKind, command.routeNodeIds);
                return;

            default:
                Log("ERROR unknown command");
                return;
        }
    }

    public void Tick()
    {
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
            return;
        }

        ScanOperation scan = new ScanOperation
        {
            id = nextScanId,
            displayId = $"scan{nextScanId}",
            packetId = packetId,
            remainingTicks = durationTicks,
            totalTicks = durationTicks
        };

        nextScanId++;
        operations.Add(scan);

        packet.BeginQuickScanVisual();
        packet.UpdateScanVisual(0f);

        Log($"SCAN started: {packetId} ({durationTicks}s)");
    }

    private void StartTrace(string packetId, int durationTicks = 4)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            Log($"TRACE failed: packet {packetId} not found");
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

        Log($"TRACE started: {packetId} ({durationTicks}s)");
    }

    private void StartDeepScan(string packetId, int durationTicks = 10)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            Log($"DEEPSCAN failed: packet {packetId} not found");
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

        Log($"DEEPSCAN started: {packetId} ({durationTicks}s)");
    }

    private void StartBlock(string packetId, string nodeId)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);
        NodeView node = networkRuntime.GetNode(nodeId);

        if (packet == null)
        {
            Log($"BLOCK failed: packet {packetId} not found");
            return;
        }

        if (node == null)
        {
            Log($"BLOCK failed: node {nodeId} not found");
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

        Log($"BLOCK {block.displayId} armed: {packetId} @ {nodeId}");
    }

    private void StartBoost(string packetId)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            Log($"BOOST failed: packet {packetId} not found");
            return;
        }

        if (!packet.IsVisiblePriority())
        {
            Log($"BOOST failed: {packetId} is not identified as priority");
            return;
        }

        int previousSpeed = packet.baseSpeed;

        if (!packet.TryBoost())
        {
            Log($"BOOST failed: {packetId} cannot move faster");
            return;
        }

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
                return;
            }

            op.Cancel(this);
            return;
        }

        Log($"CANCEL failed: {operationId} not found");
    }

    public void AppendOperationsPanel(StringBuilder sb)
    {
        sb.AppendLine("SCANS");
        bool hasScans = false;

        for (int i = 0; i < operations.Count; i++)
        {
            if (operations[i] is ScanOperation scan)
            {
                hasScans = true;
                sb.AppendLine(scan.GetOperationsLine());
            }
            if (operations[i] is DeepScanOperation deepScan)
            {
                hasScans = true;
                sb.AppendLine(deepScan.GetOperationsLine());
            }
            if (operations[i] is TraceOperation trace)
            {
                hasScans = true;
                sb.AppendLine(trace.GetOperationsLine());
            }
        }

        if (!hasScans)
            sb.AppendLine("none");

        sb.AppendLine();
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