using System.Collections.Generic;
using UnityEngine;

public class DdosSwarmEvent : ILevelEvent
{
    public int StartTick { get; private set; }
    public int EndTick { get; private set; }

    private readonly int secondBurstLocalTick;

    private readonly int minPacketCount;
    private readonly int maxPacketCount;

    private readonly int minCadenceTicks;
    private readonly int maxCadenceTicks;

    public DdosSwarmEvent(
        int startTick = 120,
        int secondBurstTick = 300,
        int minPacketCount = 10,
        int maxPacketCount = 12,
        int minCadenceTicks = 4,
        int maxCadenceTicks = 6)
    {
        StartTick = startTick;
        secondBurstLocalTick = secondBurstTick - startTick;

        this.minPacketCount = Mathf.Max(1, minPacketCount);
        this.maxPacketCount = Mathf.Max(this.minPacketCount, maxPacketCount);

        this.minCadenceTicks = Mathf.Max(1, minCadenceTicks);
        this.maxCadenceTicks = Mathf.Max(this.minCadenceTicks, maxCadenceTicks);

        int maxBurstDuration = this.maxPacketCount * this.maxCadenceTicks;
        EndTick = secondBurstTick + maxBurstDuration;
    }

    public bool IsActive(int globalTick)
    {
        return globalTick >= StartTick && globalTick <= EndTick;
    }

    public void OnTick(int globalTick, int localTick, TrafficDirector traffic)
    {
        if (traffic == null)
            return;

        if (localTick == 0)
            QueueBurst(globalTick, traffic);

        if (localTick == secondBurstLocalTick)
            QueueBurst(globalTick, traffic);
    }

    private void QueueBurst(int burstStartTick, TrafficDirector traffic)
    {
        RouteStep[] route = ChooseRoute(traffic);
        if (route == null || route.Length == 0)
            return;

        int packetCount = Random.Range(minPacketCount, maxPacketCount + 1);
        int scheduledTick = burstStartTick;

        string batchSourceAddress = GenerateBatchSourceAddress();

        for (int i = 0; i < packetCount; i++)
        {
            List<IPacketKeyword> keywords = new()
            {
                new DraggingKeyword(6, 2)
            };

            SpawnPlan plan = traffic.CreateEventSpawnPlan(
                scheduledTick,
                PacketClass.Threat,
                PacketKind.Ddos,
                route,
                scanDifficultyOverride: 35,
                baseSpeedOverride: 1,
                infections: null,
                keywords: keywords,
                sourceAddressOverride: batchSourceAddress
            );

            if (plan != null)
                traffic.QueueSpawnPlan(plan);

            int cadence = Random.Range(minCadenceTicks, maxCadenceTicks + 1);
            scheduledTick += cadence;
        }
    }

    private string GenerateBatchSourceAddress()
    {
        return $"{Random.Range(1, 6)}.{Random.Range(1, 6)}.{Random.Range(1, 6)}.{Random.Range(1, 6)}";
    }

    private RouteStep[] ChooseRoute(TrafficDirector traffic)
    {
        bool hasDb = traffic.routeToDb != null && traffic.routeToDb.Length > 0;
        bool hasCache = traffic.routeToCache != null && traffic.routeToCache.Length > 0;

        if (hasDb && hasCache)
            return Random.value < 0.5f ? traffic.routeToDb : traffic.routeToCache;

        if (hasDb)
            return traffic.routeToDb;

        if (hasCache)
            return traffic.routeToCache;

        return null;
    }
}