using System.Collections.Generic;
using UnityEngine;

public class PacketLaneCoordinator : MonoBehaviour
{
    public static PacketLaneCoordinator Instance { get; private set; }

    [Header("Lane Settings")]
    [Min(0f)] public float laneSpacing = 0.16f;

    // Fixed Phase 1 lane set: left / center / right
    private static readonly int[] LanePreferenceOrder = { 0, -1, 1 };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PacketLaneCoordinator] Duplicate instance found, destroying newest.");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int AssignLaneForEdge(PacketView packet, ConnectionView connection)
    {
        if (packet == null || connection == null)
            return 0;

        NetworkRuntime runtime = FindFirstObjectByType<NetworkRuntime>();
        if (runtime == null)
            return 0;

        List<PacketView> packetsOnConnection = runtime.GetPacketsOnConnection(connection);

        int bestLane = 0;
        int bestOccupancy = int.MaxValue;

        for (int i = 0; i < LanePreferenceOrder.Length; i++)
        {
            int candidateLane = LanePreferenceOrder[i];
            int occupancy = CountLaneOccupancy(packetsOnConnection, candidateLane, packet);

            if (occupancy < bestOccupancy)
            {
                bestOccupancy = occupancy;
                bestLane = candidateLane;
            }
        }

        return bestLane;
    }

    public float GetLaneOffsetWorld(int laneIndex)
    {
        return laneIndex * laneSpacing;
    }

    private int CountLaneOccupancy(List<PacketView> packetsOnConnection, int laneIndex, PacketView packetToIgnore)
    {
        if (packetsOnConnection == null || packetsOnConnection.Count == 0)
            return 0;

        int count = 0;

        for (int i = 0; i < packetsOnConnection.Count; i++)
        {
            PacketView packet = packetsOnConnection[i];

            if (packet == null || packet == packetToIgnore)
                continue;

            if (packet.GetVisualLaneIndex() == laneIndex)
                count++;
        }

        return count;
    }
}