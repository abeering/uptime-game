using System.Collections.Generic;
using UnityEngine;

public class TrafficDirector : MonoBehaviour
{
    [Header("References")]
    public PacketView packetPrefab;
    public Transform packetsRoot;
    public NetworkRuntime networkRuntime;
    public CommandDirector commandDirector;
    private AudioManager audioManager;

    [Header("Routes")]
    public RouteStep[] routeToDb;
    public RouteStep[] routeToCache;

    [Header("Spawn Cadence")]

    // Average ticks between spawns at the start of the run.
    // Larger = calmer opening.
    [Min(1)] public int startingSpawnIntervalTicks = 10;

    // Minimum allowed interval between spawns later in the run.
    // Lower = more intense late game.
    [Min(1)] public int minSpawnIntervalTicks = 5;

    // How many ticks it takes to reduce interval by 1.
    // Controls how quickly difficulty ramps.
    [Min(1)] public int ticksPerSpawnIntervalStep = 200;

    // Random variation applied to each scheduled spawn interval.
    // Prevents perfectly predictable rhythm.
    [Min(0)] public int spawnIntervalJitter = 5;

    // Optional grace period before spawning begins.
    [Min(0)] public int openingGraceTicks = 5;

    [Header("Spawn Intel")]

    // Chance that a newly spawned packet starts with quickscan intel
    // instead of fully unknown.
    [Range(0f, 1f)] public float startingQuickScanChance = 0.0f;

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

    [Header("Priority Ramp")]

    // Chance that a newly spawned packet is priority traffic at the start of the run.
    [Range(0f, 1f)] public float startingPriorityChance = 0.10f;

    // Hard cap on priority traffic ratio.
    [Range(0f, 1f)] public float maxPriorityChance = 0.20f;

    // Amount added to priority chance every tick.
    public float priorityChanceRampPerTick = 0.00025f;

    [Header("Packet Move Interval")]

    // Fastest possible packet movement interval.
    // Lower = faster packet.
    // 1 means the packet can advance every tick before latency is applied.
    [Min(1)] public int minBaseMoveInterval = 1;

    // Slowest possible packet movement interval.
    // Higher = slower packet.
    // Wider ranges create more variety, but too much variety can feel noisy.
    [Min(1)] public int maxBaseMoveInterval = 3;

    [Header("Scan Difficulty")]
    [Range(0, 100)] public int minScanDifficulty = 10;
    [Range(0, 100)] public int maxScanDifficulty = 40;


    [Header("Burst Control")]

    // Maximum number of new packets that may be generated on a single tick.
    // Keep this at 1 for a calmer, more readable prototype.
    // Increase later for panic/burst scenarios.
    [Min(1)] public int maxSpawnsPerTick = 1;

    [Header("Debug")]
    public bool logSpawns = true;
    public bool logRampValues = false;
    public bool autoSpawnEnabled = true;

    private readonly List<SpawnPlan> queuedPlans = new();
    private readonly List<PacketView> activePackets = new();

    private readonly List<string> availablePacketIds = new();
    private int ticksUntilNextSpawn = 0;

    private void Awake()
    {
        InitializePacketIdPool();
        audioManager = AudioManager.Instance;
    }

    private void Start()
    {
        ScheduleNextSpawn(0);
    }

    private void InitializePacketIdPool()
    {
        availablePacketIds.Clear();

        for (int num = 0; num <= 9; num++)
        {
            for (char letter = 'a'; letter <= 'z'; letter++)            
            {
                availablePacketIds.Add($"{letter}{num}");
            }
        }
    }

    public void QueueSpawnPlan(SpawnPlan plan)
    {
        if (plan == null) return;
        queuedPlans.Add(plan);
    }

    public void QueueInfectionSpawnAtTick(
        int spawnTick,
        NodeView sourceNode,
        RouteStep[] route,
        PacketClass packetClass = PacketClass.Threat,
        PacketKind packetKind = PacketKind.Virus,
        List<InfectionPayload> infections = null,
        int? baseSpeedOverride = null,
        int? scanDifficultyOverride = null)
    {
        if (sourceNode == null || route == null || route.Length == 0)
            return;

        List<InfectionPayload> resolvedInfections = infections != null
            ? new List<InfectionPayload>(infections)
            : BuildDefaultInfectionsForKind(packetKind);

        SpawnPlan plan = new SpawnPlan
        {
            spawnTick = spawnTick,
            packetId = GetNextPacketId(),
            packetClass = packetClass,
            packetKind = packetKind,
            scanDifficulty = scanDifficultyOverride ?? Mathf.RoundToInt((minScanDifficulty + maxScanDifficulty) * 0.5f),
            sourceAddress = sourceNode.nodeId,
            baseSpeed = baseSpeedOverride ?? minBaseMoveInterval,
            route = CloneRoute(route),
            startsQuickScanned = false,
            infections = resolvedInfections
        };

        QueueSpawnPlan(plan);

        if (logSpawns)
        {
            Debug.Log($"[TrafficDirector] queued infection spawn {plan.packetId} from {sourceNode.nodeId} for tick {spawnTick}");
        }
    }

    public void ProcessTick(int currentTick)
    {
        if (autoSpawnEnabled)
        {
            UpdateScheduledSpawns(currentTick);
        }

        // Always process queued plans, even when scheduled autospawn is disabled.
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
        float priorityChance = GetCurrentPriorityChance(currentTick);

        QueueSpawnPlan(BuildProceduralPlan(currentTick, malwareChance, priorityChance));

        ScheduleNextSpawn(currentTick);
    }

    private float GetCurrentMalwareChance(int currentTick)
    {
        return Mathf.Min(
            maxMalwareChance,
            startingMalwareChance + (malwareChanceRampPerTick * currentTick)
        );
    }

    private float GetCurrentPriorityChance(int currentTick)
    {
        return Mathf.Min(
            maxPriorityChance,
            startingPriorityChance + (priorityChanceRampPerTick * currentTick)
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

    private List<InfectionPayload> BuildDefaultInfectionsForKind(PacketKind kind)
    {
        InfectionType type = InfectionRules.RollDefaultInfectionType(kind);

        if (type == InfectionType.None)
            return new List<InfectionPayload>();

        InfectionPayload payload = new InfectionPayload(type);

        ApplyDefaultInfectionParametersForKind(kind, payload);

        return new List<InfectionPayload> { payload };
    }

    private List<InfectionPayload> BuildOverrideInfections(InfectionType? infectionOverride)
    {
        if (!infectionOverride.HasValue || infectionOverride.Value == InfectionType.None)
            return new List<InfectionPayload>();

        InfectionPayload payload = new InfectionPayload(infectionOverride.Value);
        return new List<InfectionPayload> { payload };
    }

    private List<InfectionPayload> BuildOverrideInfections(
        InfectionType? infectionOverride,
        InfectionTargetRule? targetRule,
        int nthNode,
        bool allowAlreadyInfectedNode,
        Dictionary<string, string> infectionParams)
    {
        if (!infectionOverride.HasValue || infectionOverride.Value == InfectionType.None)
            return new List<InfectionPayload>();

        InfectionPayload payload = new InfectionPayload(infectionOverride.Value);

        payload.rules = new InfectionApplicationRules
        {
            targetRule = targetRule ?? InfectionTargetRule.FirstReachedNode,
            nthNode = Mathf.Max(1, nthNode),
            allowAlreadyInfectedNode = allowAlreadyInfectedNode
        };

        ApplyDebugInfectionParams(payload, infectionParams);

        return new List<InfectionPayload> { payload };
    }

    private void ApplyDefaultInfectionParametersForKind(PacketKind kind, InfectionPayload payload)
    {
        if (payload == null)
            return;

        switch (payload.type)
        {
            case InfectionType.Spawner:
                payload.parameters.spawner.cadenceTicks = 8;
                payload.parameters.spawner.burstSize = 1;
                payload.parameters.spawner.spawnKind = PacketKind.Virus;
                payload.parameters.spawner.scanDifficulty = 35;
                payload.parameters.spawner.baseSpeedOverride = minBaseMoveInterval;
                break;

            case InfectionType.Throttle:
                payload.parameters.throttle.latencyPenalty = 1;
                break;
        }
    }

    private void ApplyDebugInfectionParams(InfectionPayload payload, Dictionary<string, string> rawParams)
    {
        if (payload == null || rawParams == null || rawParams.Count == 0)
            return;

        switch (payload.type)
        {
            case InfectionType.Spawner:
                ApplySpawnerDebugParams(payload.parameters.spawner, rawParams);
                break;

            case InfectionType.Throttle:
                ApplyThrottleDebugParams(payload.parameters.throttle, rawParams);
                break;

            case InfectionType.Blackout:
                // no params yet
                break;
        }
    }

    private void ApplyThrottleDebugParams(ThrottleInfectionParameters parameters, Dictionary<string, string> rawParams)
    {
        if (parameters == null || rawParams == null)
            return;

        if (TryGetInt(rawParams, "throttle.latencypenalty", out int latencyPenalty))
            parameters.latencyPenalty = Mathf.Max(0, latencyPenalty);
    }

    private void ApplySpawnerDebugParams(SpawnerInfectionParameters parameters, Dictionary<string, string> rawParams)
    {
        if (parameters == null || rawParams == null)
            return;

        if (TryGetInt(rawParams, "spawner.cadence", out int cadence))
            parameters.cadenceTicks = cadence;

        if (TryGetInt(rawParams, "spawner.burst", out int burst))
            parameters.burstSize = burst;

        if (TryGetEnum(rawParams, "spawner.spawnkind", out PacketKind spawnKind))
            parameters.spawnKind = spawnKind;

        if (TryGetInt(rawParams, "spawner.scandifficulty", out int scanDifficulty))
            parameters.scanDifficulty = scanDifficulty;

        if (TryGetInt(rawParams, "spawner.basespeed", out int baseSpeed))
            parameters.baseSpeedOverride = baseSpeed;
    }

    private bool TryGetInt(Dictionary<string, string> rawParams, string key, out int value)
    {
        value = 0;

        if (rawParams == null || string.IsNullOrWhiteSpace(key))
            return false;

        if (!rawParams.TryGetValue(key.ToLowerInvariant(), out string rawValue))
            return false;

        return int.TryParse(rawValue, out value);
    }

    private bool TryGetEnum<TEnum>(Dictionary<string, string> rawParams, string key, out TEnum value) where TEnum : struct
    {
        value = default;

        if (rawParams == null || string.IsNullOrWhiteSpace(key))
            return false;

        if (!rawParams.TryGetValue(key.ToLowerInvariant(), out string rawValue))
            return false;

        return System.Enum.TryParse(rawValue, true, out value);
    }

    private SpawnPlan BuildProceduralPlan(int currentTick, float malwareChance, float priorityChance)
    {
        float clampedMalwareChance = Mathf.Clamp01(malwareChance);
        float clampedPriorityChance = Mathf.Clamp(priorityChance, 0f, 1f - clampedMalwareChance);
        bool startsQuickScanned = Random.value < startingQuickScanChance;

        float roll = Random.value;

        PacketClass pClass;
        PacketKind pKind = PacketKind.None;

        if (roll < clampedMalwareChance)
        {
            pClass = PacketClass.Threat;

            float kindRoll = Random.value;

            if (kindRoll < 0.4f) pKind = PacketKind.Virus;
            else if (kindRoll < 0.7f) pKind = PacketKind.Worm;
            else if (kindRoll < 0.9f) pKind = PacketKind.Spyware;
            else pKind = PacketKind.Ddos;
        }
        else if (roll < clampedMalwareChance + clampedPriorityChance)
        {
            pClass = PacketClass.Priority;

            float kindRoll = Random.value;

            if (kindRoll < 0.4f) pKind = PacketKind.Auth;
            else if (kindRoll < 0.75f) pKind = PacketKind.Control;
            else pKind = PacketKind.FileTransfer;
        }
        else
        {
            pClass = PacketClass.Benign;
        }

        int baseMoveInterval = Random.Range(minBaseMoveInterval, maxBaseMoveInterval + 1);

        // Priority traffic trends slightly faster before boost.
        if (pClass == PacketClass.Priority)
            baseMoveInterval = Mathf.Max(minBaseMoveInterval, baseMoveInterval - 1);

        int scanDifficulty = Random.Range(minScanDifficulty, maxScanDifficulty + 1);
        RouteStep[] route = ChooseRoute();

        return new SpawnPlan
        {
            spawnTick = currentTick,
            packetId = GetNextPacketId(),
            packetClass = pClass,
            packetKind = pKind,
            scanDifficulty = scanDifficulty,
            sourceAddress = GenerateSourceAddress(),
            baseSpeed = baseMoveInterval,
            route = route,
            startsQuickScanned = startsQuickScanned,
            infections = pClass == PacketClass.Threat
                ? BuildDefaultInfectionsForKind(pKind)
                : new List<InfectionPayload>()
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
        packet.Initialize(
            plan.packetId,
            plan.packetClass,
            plan.packetKind,
            plan.sourceAddress,
            plan.baseSpeed,
            plan.scanDifficulty,
            plan.route,
            plan.startsQuickScanned,
            plan.infections
        );
        packet.keywords.AddRange(plan.keywords);
        packet.OnReachedNode += HandlePacketReachedNode;
        activePackets.Add(packet);
        networkRuntime.RegisterPacket(packet);
        audioManager?.PlayClick();

        if (logSpawns)
        {
            Debug.Log(
                $"Spawned {plan.packetId} class={plan.packetClass} kind={plan.packetKind} scanDifficulty={plan.scanDifficulty} moveInterval={plan.baseSpeed} at tick {plan.spawnTick}"
            );
        }
    }

    private void HandlePacketReachedNode(PacketView packet, NodeView node)
    {
        if (packet == null || node == null || packet.isRemoved)
            return;

        // 1) Player / command intercepts resolve first.
        if (commandDirector != null)
        {
            if(commandDirector.ResolvePacketArrivalIntercepts(packet, node))
                return;

            if (packet.isRemoved)
                return;
        }

        // 2) Passive node traffic denial / blackout.
        if (node.BlocksTraffic())
        {
            if (logSpawns)
                Debug.Log($"[TrafficDirector] {packet.packetId} blocked at {node.nodeId}");

            RemovePacket(packet, "blocked");
            return;
        }

        if (packet.IsTrueThreat())
        {
            InfectionPayload payload = packet.GetPrimaryInfectionPayload();

            if (payload == null)
            {
                InfectionType fallbackType = InfectionRules.RollDefaultInfectionType(packet.trueKind);
                if (fallbackType != InfectionType.None)
                {
                    payload = new InfectionPayload(fallbackType);
                    ApplyDefaultInfectionParametersForKind(packet.trueKind, payload);
                }
            }

            if (payload != null && InfectionRuleEvaluator.CanApply(packet, node, payload))
            {
                if (logSpawns)
                    Debug.Log($"[TrafficDirector] {packet.packetId} infected node {node.nodeId} with {payload.type}");

                bool applied = node.ApplyInfection(payload);
                if (applied)
                {
                    RemovePacket(packet, "infected");
                    return;
                }
            }
        }
    }

    private void TickActivePackets()
    {
        var context = new KeywordContext(networkRuntime, commandDirector, Time.deltaTime);

        for (int i = activePackets.Count - 1; i >= 0; i--)
        {
            PacketView packet = activePackets[i];

            if (packet == null)
            {
                activePackets.RemoveAt(i);
                continue;
            }

            packet.Tick(context);

            if (packet == null || packet.isRemoved)
                continue;

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
        availablePacketIds.Add(packet.packetId);

        Destroy(packet.gameObject);
    }

    private string GetNextPacketId()
    {
        if (availablePacketIds.Count == 0)
        {
            Debug.LogWarning("Ran out of packet IDs!");
            return "??";
        }

        int index = Random.Range(0, availablePacketIds.Count);
        string id = availablePacketIds[index];

        availablePacketIds.RemoveAt(index);

        return id;
    }

    private string GenerateSourceAddress()
    {
        return $"{Random.Range(1,6)}.{Random.Range(1,6)}.{Random.Range(1,6)}.{Random.Range(1,6)}";
    }

    public bool DebugSpawnPacket(
        PacketClass packetClass,
        PacketKind packetKind,
        string[] routeNodeIds,
        List<string> keywordSpecs,
        InfectionType? infectionType,
        InfectionTargetRule? infectionTargetRule,
        int infectionNthNode,
        bool allowAlreadyInfectedNode,
        Dictionary<string, string> infectionParams,
        out string message)
    {
        message = "";

        if (routeNodeIds == null || routeNodeIds.Length < 2)
        {
            message = "spawn failed: need at least 2 node ids";
            return false;
        }

        RouteStep[] route = BuildRouteFromNodeIds(routeNodeIds);

        if (route == null || route.Length == 0)
        {
            message = "spawn failed: invalid route";
            return false;
        }

        List<IPacketKeyword> builtKeywords = SpawnKeywordFactory.BuildMany(keywordSpecs, out string keywordError);
        if (builtKeywords == null)
        {
            message = $"spawn failed: {keywordError}";
            return false;
        }

        SpawnPlan plan = new SpawnPlan
        {
            spawnTick = 0,
            packetId = GetNextPacketId(),
            packetClass = packetClass,
            packetKind = packetKind,
            scanDifficulty = Random.Range(minScanDifficulty, maxScanDifficulty + 1),
            sourceAddress = routeNodeIds[0],
            baseSpeed = minBaseMoveInterval,
            route = route,
            startsQuickScanned = false,
            infections = BuildOverrideInfections(
                infectionType,
                infectionTargetRule,
                infectionNthNode,
                allowAlreadyInfectedNode,
                infectionParams
            )
        };

        if (packetClass == PacketClass.Priority)
            plan.baseSpeed = Mathf.Max(minBaseMoveInterval, plan.baseSpeed - 1);

        plan.keywords.AddRange(builtKeywords);

        SpawnPacket(plan);

        string keywordSummary = (keywordSpecs != null && keywordSpecs.Count > 0)
            ? $" kw=[{string.Join(", ", keywordSpecs)}]"
            : "";

        string infectionSummary = "";
        if (infectionType.HasValue)
        {
            string ruleSummary = "";

            if (infectionTargetRule.HasValue)
            {
                switch (infectionTargetRule.Value)
                {
                    case InfectionTargetRule.FirstReachedNode:
                        ruleSummary = " rule=first";
                        break;

                    case InfectionTargetRule.NthReachedNode:
                        ruleSummary = $" rule=nth:{Mathf.Max(1, infectionNthNode)}";
                        break;

                    case InfectionTargetRule.AnyReachedNode:
                        ruleSummary = " rule=any";
                        break;

                    case InfectionTargetRule.DestinationNode:
                        ruleSummary = " rule=destination";
                        break;
                }
            }

            string reinfectSummary = allowAlreadyInfectedNode ? " reinfect=true" : "";
            infectionSummary = $" inf={infectionType.Value}{ruleSummary}{reinfectSummary}";
        }

        string infectionParamSummary = "";
        if (infectionParams != null && infectionParams.Count > 0)
        {
            List<string> parts = new();
            foreach (var kvp in infectionParams)
            {
                parts.Add($"{kvp.Key}={kvp.Value}");
            }

            infectionParamSummary = $" infp=[{string.Join(", ", parts)}]";
        }

        message = $"spawned {plan.packetId}: {packetClass}/{packetKind}{infectionSummary}{infectionParamSummary}{keywordSummary} on {string.Join(" -> ", routeNodeIds)}";
        return true;
    }

    private RouteStep[] BuildRouteFromNodeIds(string[] routeNodeIds)
    {
        List<RouteStep> builtRoute = new();

        for (int i = 0; i < routeNodeIds.Length - 1; i++)
        {
            string fromId = routeNodeIds[i];
            string toId = routeNodeIds[i + 1];

            NodeView fromNode = networkRuntime.GetNode(fromId);
            NodeView toNode = networkRuntime.GetNode(toId);

            if (fromNode == null || toNode == null)
            {
                Debug.LogWarning($"[TrafficDirector] invalid node in debug route: {fromId} -> {toId}");
                return null;
            }

            ConnectionView connection = FindConnectionBetween(fromNode, toNode);

            if (connection == null)
            {
                Debug.LogWarning($"[TrafficDirector] no connection between {fromId} and {toId}");
                return null;
            }

            bool aToB;
            if (connection.nodeA == fromNode && connection.nodeB == toNode)
                aToB = true;
            else if (connection.nodeB == fromNode && connection.nodeA == toNode)
                aToB = false;
            else
                return null;

            builtRoute.Add(new RouteStep
            {
                connection = connection,
                aToB = aToB
            });
        }

        return builtRoute.ToArray();
    }

    private ConnectionView FindConnectionBetween(NodeView fromNode, NodeView toNode)
    {
        if (networkRuntime == null)
            return null;

        return networkRuntime.FindConnectionBetween(fromNode, toNode);
    }

}