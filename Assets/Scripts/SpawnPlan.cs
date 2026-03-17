using System;
using UnityEngine;

[Serializable]
public class SpawnPlan
{
    public int spawnTick;
    public string packetId;
    public PacketKind kind;
    public int baseSpeed = 1;
    public RouteStep[] route;
}