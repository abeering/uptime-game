using UnityEngine;
using System.Collections.Generic;

public class InfectionBurstEvent : ILevelEvent
{
    public int StartTick { get; private set; }
    public int EndTick { get; private set; }

    public InfectionBurstEvent(int startTick, int duration)
    {
        StartTick = startTick;
        EndTick = startTick + duration;
    }

    public bool IsActive(int globalTick)
    {
        return globalTick >= StartTick && globalTick <= EndTick;
    }

    public void OnTick(int globalTick, int localTick, TrafficDirector traffic)
    {
        if (traffic == null)
            return;

        // Phase A — signal
        if (localTick == 1 || localTick == 3 || localTick == 5)
            QueueThreat(globalTick, traffic);

        // Phase B — breach (guaranteed infection)
        if (localTick == 10)
            QueueInfectiousVirus(globalTick, traffic);

        // Phase C — trailing pressure
        if (localTick >= 12 && localTick <= 20 && localTick % 3 == 0)
            QueueMixedTraffic(globalTick, traffic);
    }

    private void QueueThreat(int globalTick, TrafficDirector traffic)
    {
        if (traffic.routeToDb == null || traffic.routeToDb.Length == 0)
            return;

        SpawnPlan plan = traffic.CreateEventSpawnPlan(
            globalTick,
            PacketClass.Threat,
            PacketKind.Worm,
            traffic.routeToDb,
            scanDifficultyOverride: 45,
            baseSpeedOverride: 1
        );

        if (plan != null)
            traffic.QueueSpawnPlan(plan);
    }

    private void QueueInfectiousVirus(int globalTick, TrafficDirector traffic)
    {
        if (traffic.routeToDb == null || traffic.routeToDb.Length == 0)
            return;

        var infections = new List<InfectionPayload>
        {
            InfectionFactory.CreateDefaultPayload(InfectionType.Spawner)
        };

        SpawnPlan plan = traffic.CreateEventSpawnPlan(
            globalTick,
            PacketClass.Threat,
            PacketKind.Virus,
            traffic.routeToDb,
            scanDifficultyOverride: 60,
            baseSpeedOverride: 2,
            infections: infections
        );

        if (plan != null)
            traffic.QueueSpawnPlan(plan);
    }

    private void QueueMixedTraffic(int globalTick, TrafficDirector traffic)
    {
        if (traffic.routeToCache == null || traffic.routeToCache.Length == 0)
            return;

        PacketClass packetClass = Random.value < 0.5f
            ? PacketClass.Benign
            : PacketClass.Threat;

        PacketKind packetKind = packetClass == PacketClass.Threat
            ? PacketKind.Spyware
            : PacketKind.None;

        SpawnPlan plan = traffic.CreateEventSpawnPlan(
            globalTick,
            packetClass,
            packetKind,
            traffic.routeToCache,
            scanDifficultyOverride: 20,
            baseSpeedOverride: 2
        );

        if (plan != null)
            traffic.QueueSpawnPlan(plan);
    }
}