using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One-shot story beat used to connect authored traffic to the company's day.
/// </summary>
public sealed class LevelNotificationEvent : ILevelEvent
{
    private const int MaxBodyCharacters = 48;

    public int StartTick { get; }
    public int EndTick => StartTick;

    private readonly string speaker;
    private readonly string body;

    public LevelNotificationEvent(int startTick, string speaker, string body)
    {
        StartTick = startTick;
        this.speaker = speaker;
        this.body = string.IsNullOrEmpty(body) || body.Length <= MaxBodyCharacters
            ? body
            : body.Substring(0, MaxBodyCharacters);
    }

    public bool IsActive(int globalTick) => globalTick == StartTick;

    public void OnTick(int globalTick, int localTick, LevelEventContext context)
    {
        context.Notifications?.PushMessage(new Notification
        {
            SpeakerName = speaker,
            Body = body
        });
    }
}

/// <summary>
/// One infectious packet is enough to turn the sale into an incident without
/// adding another wave to an already busy ambient traffic layer.
/// </summary>
public sealed class InventoryInfectionEvent : ILevelEvent
{
    public int StartTick { get; }
    public int EndTick => StartTick;

    public InventoryInfectionEvent(int startTick)
    {
        StartTick = startTick;
    }

    public bool IsActive(int globalTick) => globalTick == StartTick;

    public void OnTick(int globalTick, int localTick, LevelEventContext context)
    {
        TrafficDirector traffic = context.Traffic;
        if (traffic == null || traffic.routeToDb == null || traffic.routeToDb.Length == 0)
            return;

        List<InfectionPayload> infections = new()
        {
            InfectionFactory.CreateDefaultPayload(InfectionType.Spawner)
        };

        SpawnPlan plan = traffic.CreateEventSpawnPlan(
            globalTick,
            PacketClass.Threat,
            PacketKind.Virus,
            traffic.routeToDb,
            scanDifficultyOverride: 50,
            baseSpeedOverride: 2,
            infections: infections,
            sourceAddressOverride: "inventory.sync"
        );

        if (plan != null)
            traffic.QueueSpawnPlan(plan);
    }
}

/// <summary>
/// A temporary change to ambient traffic. This keeps the baseline layer separate
/// from authored packet bursts while allowing the level to breathe.
/// </summary>
public sealed class LevelTrafficModifierEvent : ILevelEvent
{
    public int StartTick { get; }
    public int EndTick => StartTick;

    private readonly int durationTicks;
    private readonly int spawnIntervalDelta;
    private readonly float malwareChanceDelta;
    private readonly float priorityChanceDelta;

    public LevelTrafficModifierEvent(
        int startTick,
        int durationTicks,
        int spawnIntervalDelta = 0,
        float malwareChanceDelta = 0f,
        float priorityChanceDelta = 0f)
    {
        StartTick = startTick;
        this.durationTicks = durationTicks;
        this.spawnIntervalDelta = spawnIntervalDelta;
        this.malwareChanceDelta = malwareChanceDelta;
        this.priorityChanceDelta = priorityChanceDelta;
    }

    public bool IsActive(int globalTick) => globalTick == StartTick;

    public void OnTick(int globalTick, int localTick, LevelEventContext context)
    {
        context.ApplyTrafficModifier(new TrafficModifier(durationTicks)
        {
            spawnIntervalDelta = spawnIntervalDelta,
            malwareChanceDelta = malwareChanceDelta,
            priorityChanceDelta = priorityChanceDelta
        });
    }
}

/// <summary>
/// The company's flash-sale traffic: mostly customer requests with enough
/// priority checkout/auth work mixed in that indiscriminate blocking is costly.
/// </summary>
public sealed class FlashSaleRushEvent : ILevelEvent
{
    public int StartTick { get; }
    public int EndTick { get; }

    private readonly int packetCount;
    private readonly int cadenceTicks;

    public FlashSaleRushEvent(int startTick, int packetCount = 14, int cadenceTicks = 2)
    {
        StartTick = startTick;
        this.packetCount = Mathf.Max(1, packetCount);
        this.cadenceTicks = Mathf.Max(1, cadenceTicks);
        EndTick = startTick + this.packetCount * this.cadenceTicks;
    }

    public bool IsActive(int globalTick) => globalTick >= StartTick && globalTick <= EndTick;

    public void OnTick(int globalTick, int localTick, LevelEventContext context)
    {
        if (localTick != 0 || context.Traffic == null)
            return;

        TrafficDirector traffic = context.Traffic;

        for (int i = 0; i < packetCount; i++)
        {
            bool isCheckout = i % 4 == 1;
            PacketClass packetClass = isCheckout ? PacketClass.Priority : PacketClass.Benign;
            PacketKind packetKind = isCheckout ? PacketKind.Auth : PacketKind.None;
            RouteStep[] route = i % 3 == 0 ? traffic.routeToDb : traffic.routeToCache;

            if (route == null || route.Length == 0)
                route = traffic.routeToDb;

            SpawnPlan plan = traffic.CreateEventSpawnPlan(
                StartTick + i * cadenceTicks,
                packetClass,
                packetKind,
                route,
                scanDifficultyOverride: isCheckout ? 24 : 12,
                baseSpeedOverride: isCheckout ? 1 : 2,
                startsQuickScanned: isCheckout,
                sourceAddressOverride: isCheckout ? "checkout.app" : "campaign.social"
            );

            if (plan != null)
                traffic.QueueSpawnPlan(plan);
        }
    }
}

/// <summary>
/// A small group of disguised account-takeover probes hidden behind the sale.
/// They share a source so a careful player can identify and block the pattern.
/// </summary>
public sealed class CheckoutProbeEvent : ILevelEvent
{
    public int StartTick { get; }
    public int EndTick { get; }

    private readonly int packetCount;

    public CheckoutProbeEvent(int startTick, int packetCount = 4)
    {
        StartTick = startTick;
        this.packetCount = Mathf.Max(1, packetCount);
        EndTick = startTick + this.packetCount * 5;
    }

    public bool IsActive(int globalTick) => globalTick >= StartTick && globalTick <= EndTick;

    public void OnTick(int globalTick, int localTick, LevelEventContext context)
    {
        if (localTick != 0 || context.Traffic == null)
            return;

        TrafficDirector traffic = context.Traffic;

        for (int i = 0; i < packetCount; i++)
        {
            List<IPacketKeyword> keywords = new()
            {
                i % 2 == 0 ? new MutatingKeyword(4) : new JitteryKeyword(1)
            };

            SpawnPlan plan = traffic.CreateEventSpawnPlan(
                StartTick + i * 5,
                PacketClass.Threat,
                PacketKind.Spyware,
                traffic.routeToDb,
                scanDifficultyOverride: 38 + i * 3,
                baseSpeedOverride: 2,
                keywords: keywords,
                sourceAddressOverride: "4.4.2.1"
            );

            if (plan != null)
                traffic.QueueSpawnPlan(plan);
        }
    }
}
