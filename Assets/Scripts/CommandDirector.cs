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
            Log("error: null command");
            return;
        }

        switch (command.type)
        {
            case CommandType.Scan:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("usage: scan <packet>");
                    return;
                }

                StartScan(command.packetId);
                return;

            case CommandType.DeepScan:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("usage: deepscan <packet>");
                    return;
                }

                StartDeepScan(command.packetId);
                return;

            case CommandType.Trace:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("usage: trace <packet>");
                    return;
                }

                StartTrace(command.packetId);
                return;

            case CommandType.Block:
                if (string.IsNullOrWhiteSpace(command.packetId) || string.IsNullOrWhiteSpace(command.nodeId))
                {
                    Log("usage: block <packet> @ <node>");
                    return;
                }

                StartBlock(command.packetId, command.nodeId);
                return;

            case CommandType.Cancel:
                if (string.IsNullOrWhiteSpace(command.operationId))
                {
                    Log("usage: cancel <operationId>");
                    return;
                }

                CancelOperation(command.operationId);
                return;

            case CommandType.Boost:
                if (string.IsNullOrWhiteSpace(command.packetId))
                {
                    Log("usage: boost <packet>");
                    return;
                }

                StartBoost(command.packetId);
                return;

            case CommandType.Spawn:
                if (command.routeNodeIds == null || command.routeNodeIds.Length < 2)
                {
                    Log("usage: spawn <class> <kind> <node1> <node2> [node3...]");
                    return;
                }

                StartSpawn(command.packetClass, command.packetKind, command.routeNodeIds);
                return;

            default:
                Log("unknown command");
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
            Log("spawn failed: no traffic director");
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
            Log($"scan failed: packet {packetId} not found");
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

        Log($"{scan.displayId} started: {packetId} ({durationTicks}s)");
    }

    private void StartTrace(string packetId, int durationTicks = 4)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            Log($"trace failed: packet {packetId} not found");
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

        Log($"{trace.displayId} started: {packetId} ({durationTicks}s)");
    }

    private void StartDeepScan(string packetId, int durationTicks = 10)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            Log($"deepscan failed: packet {packetId} not found");
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

        Log($"{scan.displayId} started: {packetId} ({durationTicks}s)");
    }

    private void StartBlock(string packetId, string nodeId)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);
        NodeView node = networkRuntime.GetNode(nodeId);

        if (packet == null)
        {
            Log($"block failed: packet {packetId} not found");
            return;
        }

        if (node == null)
        {
            Log($"block failed: node {nodeId} not found");
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

        Log($"{block.displayId} armed: {packetId} @ {nodeId}");
    }

    private void StartBoost(string packetId)
    {
        PacketView packet = networkRuntime.GetPacket(packetId);

        if (packet == null)
        {
            Log($"boost failed: packet {packetId} not found");
            return;
        }

        if (!packet.IsVisiblePriority())
        {
            Log($"boost failed: {packetId} is not identified as priority");
            return;
        }

        int previousSpeed = packet.baseSpeed;

        if (!packet.TryBoost())
        {
            Log($"boost failed: {packetId} cannot move faster");
            return;
        }

        Log($"boost complete: {packetId} speed {previousSpeed} -> {packet.baseSpeed}");
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
                Log($"cancel failed: {op.displayId} cannot be cancelled");
                return;
            }

            op.Cancel(this);
            return;
        }

        Log($"cancel failed: {operationId} not found");
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