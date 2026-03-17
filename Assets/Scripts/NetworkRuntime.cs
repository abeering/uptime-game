using System.Collections.Generic;
using UnityEngine;

public class NetworkRuntime : MonoBehaviour
{
    private Dictionary<string, PacketView> packets = new();
    private Dictionary<string, NodeView> nodes = new();

    // ---- PACKETS ----

    public void RegisterPacket(PacketView packet)
    {
        if (packet == null || string.IsNullOrEmpty(packet.PacketId))
            return;

        packets[packet.PacketId] = packet;
        Debug.Log($"[Registry] Registered packet {packet.PacketId}");
    }

    public void UnregisterPacket(PacketView packet)
    {
        if (packet == null) return;

        if (packets.TryGetValue(packet.PacketId, out var existing) && existing == packet)
        {
            packets.Remove(packet.PacketId);
            Debug.Log($"[Registry] Unregistered packet {packet.PacketId}");
        }
        
    }

    public PacketView GetPacket(string id)
    {
        packets.TryGetValue(id, out var packet);
        return packet;
    }

    // ---- NODES ----

    public void RegisterNode(NodeView node)
    {
        if (node == null || string.IsNullOrEmpty(node.nodeId))
            return;

        nodes[node.nodeId] = node;
    }

    public NodeView GetNode(string id)
    {
        nodes.TryGetValue(id, out var node);
        return node;
    }
}