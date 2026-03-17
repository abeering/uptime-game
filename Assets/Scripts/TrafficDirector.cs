using System.Collections.Generic;
using UnityEngine;

public class TrafficDirector : MonoBehaviour
{
    [Header("References")]
    public PacketView packetPrefab;
    public Transform packetsRoot;
    public NetworkRuntime networkRuntime;
    public CommandDirector commandDirector;

    [Header("Routes")]
    public RouteStep[] routeToDb;
    public RouteStep[] routeToCache;

    [Header("Spawn Cadence")]
    // Average ticks between spawns at the start of the run.
    // Larger = calmer opening.
    [Min(1)] public int startingSpawnIntervalTicks = 10;

    // Minimum allowed interval between spawns later in the run.
    // Lower = more intense late game.
    [Min(1)] public int minSpawnIntervalTicks = 3;

    // How many ticks it takes to reduce interval by 1.
    // Controls how quickly difficulty ramps.
    [Min(1)] public int ticksPerSpawnIntervalStep = 25;

    // Random variation applied to each scheduled spawn interval.
    // Prevents perfectly predictable rhythm.
    [Min(0)] public int spawnIntervalJitter = 2;

    // Optional grace period before spawning begins.
    [Min(0)] public int openingGraceTicks = 5;


    [Header("Threat Ramp")]

    // Chance that a newly spawned packet is malware at the start of the run.
    // Keep this low so early game teaches the player before punishing them.
    [Range(0f, 1f)] public float startingMalwareChance = 0.05f;

    // Hard cap on malware ratio later in the run.
    // High values make the game feel hopeless unless the player has strong tools.
    [Range(0f, 1f)] public float maxMalwareChance = 0.25f;

    // Amount added to malware chance every tick.
    // This controls how quickly the run shifts from "traffic management"
    // to "incident response."
    public float malwareChanceRampPerTick = 0.001f;


    [Header("Packet Move Interval")]

    // Fastest possible packet movement interval.
    // Lower = faster packet.
    // 1 means the packet can advance every tick before latency is applied.
    [Min(1)] public int minBaseMoveInterval = 1;

    // Slowest possible packet movement interval.
    // Higher = slower packet.
    // Wider ranges create more variety, but too much variety can feel noisy.
    [Min(1)] public int maxBaseMoveInterval = 3;


    [Header("Burst Control")]

    // Maximum number of new packets that may be generated on a single tick.
    // Keep this at 1 for a calmer, more readable prototype.
    // Increase later for panic/burst scenarios.
    [Min(1)] public int maxSpawnsPerTick = 1;

    [Header("Debug")]
    public bool logSpawns = true;
    public bool logRampValues = false;

    private readonly List<SpawnPlan> queuedPlans = new();
    private readonly List<PacketView> activePackets = new();

    private readonly Queue<string> availablePacketIds = new();
    private int ticksUntilNextSpawn = 0;

    private void Awake()
    {
        InitializePacketIdPool();
    }

    private void Start()
    {
        ScheduleNextSpawn(0);
    }

    private void InitializePacketIdPool()
    {
        availablePacketIds.Clear();

        // for (int num = 0; num <= 9; num++)
        // {
            for (char letter = 'a'; letter <= 'z'; letter++)            
            {
                // availablePacketIds.Enqueue($"{letter}{num}");
                availablePacketIds.Enqueue($"{letter}");

            }
        // }
    }

    public void QueueSpawnPlan(SpawnPlan plan)
    {
        if (plan == null) return;
        queuedPlans.Add(plan);
    }

    public void ProcessTick(int currentTick)
    {
        UpdateScheduledSpawns(currentTick);
        SpawnDuePlans(currentTick);
        TickActivePackets();
    }

    private void UpdateScheduledSpawns(int currentTick)
    {
        if (currentTick < openingGraceTicks)
            return;

        ticksUntilNextSpawn--;

        if (ticksUntilNextSpawn > 0)
            return;

        float malwareChance = GetCurrentMalwareChance(currentTick);

        QueueSpawnPlan(BuildProceduralPlan(currentTick, malwareChance));

        ScheduleNextSpawn(currentTick);
    }

    private float GetCurrentMalwareChance(int currentTick)
    {
        return Mathf.Min(
            maxMalwareChance,
            startingMalwareChance + (malwareChanceRampPerTick * currentTick)
        );
    }

    private int GetCurrentSpawnInterval(int currentTick)
    {
        int reduction = currentTick / ticksPerSpawnIntervalStep;
        int interval = startingSpawnIntervalTicks - reduction;

        return Mathf.Max(minSpawnIntervalTicks, interval);
    }

    private void ScheduleNextSpawn(int currentTick)
    {
        int baseInterval = GetCurrentSpawnInterval(currentTick);
        int jitter = Random.Range(-spawnIntervalJitter, spawnIntervalJitter + 1);

        ticksUntilNextSpawn = Mathf.Max(1, baseInterval + jitter);

        if (logSpawns)
        {
            Debug.Log($"Next spawn in {ticksUntilNextSpawn} ticks (base {baseInterval}, jitter {jitter})");
        }
    }

    private SpawnPlan BuildProceduralPlan(int currentTick, float malwareChance)
    {
        bool isMalware = Random.value < malwareChance;
        PacketKind kind = isMalware ? PacketKind.Malware : PacketKind.Normal;

        int baseMoveInterval = Random.Range(minBaseMoveInterval, maxBaseMoveInterval + 1);

        RouteStep[] route = ChooseRoute();

        return new SpawnPlan
        {
            spawnTick = currentTick,
            packetId = GetNextPacketId(),
            kind = kind,
            baseSpeed = baseMoveInterval,
            route = route
        };
    }

    private RouteStep[] ChooseRoute()
    {
        List<RouteStep[]> availableRoutes = new();

        if (routeToDb != null && routeToDb.Length > 0)
            availableRoutes.Add(routeToDb);

        if (routeToCache != null && routeToCache.Length > 0)
            availableRoutes.Add(routeToCache);

        if (availableRoutes.Count == 0)
            return null;

        int index = Random.Range(0, availableRoutes.Count);
        return CloneRoute(availableRoutes[index]);
    }

    private RouteStep[] CloneRoute(RouteStep[] source)
    {
        RouteStep[] copy = new RouteStep[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            copy[i] = new RouteStep
            {
                connection = source[i].connection,
                aToB = source[i].aToB
            };
        }

        return copy;
    }

    private void SpawnDuePlans(int currentTick)
    {
        for (int i = queuedPlans.Count - 1; i >= 0; i--)
        {
            SpawnPlan plan = queuedPlans[i];

            if (plan == null)
            {
                queuedPlans.RemoveAt(i);
                continue;
            }

            if (plan.spawnTick > currentTick)
                continue;

            SpawnPacket(plan);
            queuedPlans.RemoveAt(i);
        }
    }

    private void SpawnPacket(SpawnPlan plan)
    {
        if (packetPrefab == null || packetsRoot == null)
        {
            Debug.LogWarning("TrafficDirector missing packetPrefab or packetsRoot.");
            return;
        }

        if (plan.route == null || plan.route.Length == 0)
        {
            Debug.LogWarning($"SpawnPlan {plan.packetId} has no route.");
            return;
        }

        PacketView packet = Instantiate(packetPrefab, packetsRoot);
        packet.Initialize(plan.packetId, plan.kind, plan.baseSpeed, plan.route);
        activePackets.Add(packet);
        networkRuntime.RegisterPacket(packet);
        packet.OnReachedNode += (p, node) =>
        {
            commandDirector.NotifyPacketReachedNode(p, node);
        };

        if (logSpawns)
        {
            Debug.Log(
                $"Spawned {plan.packetId} kind={plan.kind} moveInterval={plan.baseSpeed} at tick {plan.spawnTick}"
            );
        }
    }

    private void TickActivePackets()
    {
        for (int i = activePackets.Count - 1; i >= 0; i--)
        {
            PacketView packet = activePackets[i];

            if (packet == null)
            {
                activePackets.RemoveAt(i);
                continue;
            }

            packet.Tick();

            if (packet.hasArrived)
            {
                if (logSpawns)
                    Debug.Log($"Packet {packet.packetId} arrived.");

                RemovePacket(packet, "arrived");
            }
        }
    }

    public void RemovePacket(PacketView packet, string reason)
    {
        if (packet == null)
            return;

        if (logSpawns)
            Debug.Log($"[TrafficDirector] removing {packet.packetId} ({reason})");

        if(commandDirector != null)
            commandDirector.NotifyPacketRemoved(packet.packetId, reason);
        
        packet.NotifyRemoved(reason);
        networkRuntime.UnregisterPacket(packet);
        activePackets.Remove(packet);
        availablePacketIds.Enqueue(packet.packetId);

        Destroy(packet.gameObject);
    }

    private string GetNextPacketId()
    {
        if (availablePacketIds.Count == 0)
        {
            Debug.LogWarning("Ran out of packet IDs!");
            return "??";
        }

        return availablePacketIds.Dequeue();
    }
}