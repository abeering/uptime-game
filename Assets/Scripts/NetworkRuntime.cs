using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkRuntime : MonoBehaviour
{
    private Dictionary<string, PacketView> packets = new();
    private Dictionary<string, NodeView> nodes = new();
    private Dictionary<string, ConnectionView> connections = new();

    private void Awake()
    {
        // register all nodes in the scene
        NodeView[] sceneNodes = FindObjectsOfType<NodeView>();

        foreach (var node in sceneNodes)
        {
            node.Initialize(this);
        }

        // register all connections in the scene
        ConnectionView[] sceneConnections = FindObjectsOfType<ConnectionView>();

        foreach (var connection in sceneConnections)
        {
            RegisterConnection(connection);
        }
    }

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

    public System.Collections.Generic.List<NodeView> GetAllNodes()
    {
        return new System.Collections.Generic.List<NodeView>(nodes.Values);
    }

    public List<PacketView> GetKnownThreatPackets()
    {
        List<PacketView> knownThreats = new();

        foreach (var packet in packets.Values)
        {
            if (packet != null && packet.IsKnownThreat())
                knownThreats.Add(packet);
        }

        knownThreats.Sort((a, b) => string.CompareOrdinal(a.packetId, b.packetId));
        return knownThreats;
    }

    public void RegisterConnection(ConnectionView connection)
    {
        if (connection == null || string.IsNullOrEmpty(connection.connectionId))
            return;

        connections[connection.connectionId] = connection;
    }

    public void UnregisterConnection(ConnectionView connection)
    {
        if (connection == null || string.IsNullOrEmpty(connection.connectionId))
            return;

        if (connections.TryGetValue(connection.connectionId, out var existing) && existing == connection)
            connections.Remove(connection.connectionId);
    }

    public ConnectionView GetConnection(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        connections.TryGetValue(id, out var connection);
        return connection;
    }

    public ConnectionView FindConnectionBetween(NodeView fromNode, NodeView toNode)
    {
        if (fromNode == null || toNode == null)
            return null;

        foreach (var connection in connections.Values)
        {
            if (connection == null)
                continue;

            bool matchesForward = connection.nodeA == fromNode && connection.nodeB == toNode;
            bool matchesReverse = connection.nodeB == fromNode && connection.nodeA == toNode;

            if (matchesForward || matchesReverse)
                return connection;
        }

        return null;
    }

    public List<ConnectionView> GetConnectionsForNode(NodeView node)
    {
        List<ConnectionView> results = new();

        if (node == null)
            return results;

        foreach (var connection in connections.Values)
        {
            if (connection == null)
                continue;

            if (connection.nodeA == node || connection.nodeB == node)
                results.Add(connection);
        }

        return results;
    }

}