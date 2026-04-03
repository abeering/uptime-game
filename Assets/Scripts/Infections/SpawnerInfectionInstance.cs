using System.Collections.Generic;
using UnityEngine;

public class SpawnerInfectionInstance : NodeInfectionInstance
{
    public override InfectionType Type => InfectionType.Spawner;
    public override Color? GetNodeTintColor() => new Color(0.45f, 0.2f, 0.55f);

    private int ticksUntilNextSpawn = 0;

    public override void OnApplied()
    {
        ticksUntilNextSpawn = GetCadenceTicks();
    }

    public override void OnTick(InfectionContext context)
    {
        if (context == null || context.networkRuntime == null || context.trafficDirector == null || node == null)
            return;

        ticksUntilNextSpawn--;

        if (ticksUntilNextSpawn > 0)
            return;

        ticksUntilNextSpawn = GetCadenceTicks();

        int burstSize = GetBurstSize();
        for (int i = 0; i < burstSize; i++)
        {
            TryQueueSpawn(context);
        }
    }

    private void TryQueueSpawn(InfectionContext context)
    {
        RouteStep[] route = BuildSpawnRoute(context.networkRuntime);

        if (route == null || route.Length == 0)
            return;

        context.trafficDirector.QueueInfectionSpawnAtTick(
            context.currentTick + 1,
            node,
            route,
            PacketClass.Threat,
            GetSpawnKind(),
            null,
            GetBaseSpeedOverride(),
            GetSpawnScanDifficulty()
        );
    }

    private RouteStep[] BuildSpawnRoute(NetworkRuntime runtime)
    {
        List<RouteStep> steps = new();

        ConnectionView firstConnection = ChooseRandomConnection(
            runtime,
            node,
            excludeConnection: null,
            excludeNeighbor: null,
            out NodeView firstDestination
        );

        if (firstConnection == null || firstDestination == null)
            return null;

        steps.Add(BuildRouteStep(node, firstConnection));

        // Optional second hop for a little variety, but keep it simple.
        if (Random.value < 0.5f)
        {
            ConnectionView secondConnection = ChooseRandomConnection(
                runtime,
                firstDestination,
                excludeConnection: firstConnection,
                excludeNeighbor: node,
                out NodeView secondDestination
            );

            if (secondConnection != null && secondDestination != null)
            {
                steps.Add(BuildRouteStep(firstDestination, secondConnection));
            }
        }

        return steps.ToArray();
    }

    private ConnectionView ChooseRandomConnection(
        NetworkRuntime runtime,
        NodeView fromNode,
        ConnectionView excludeConnection,
        NodeView excludeNeighbor,
        out NodeView destination)
    {
        destination = null;

        List<ConnectionView> allConnections = runtime.GetConnectionsForNode(fromNode);
        if (allConnections == null || allConnections.Count == 0)
            return null;

        List<ConnectionView> candidates = new();

        for (int i = 0; i < allConnections.Count; i++)
        {
            ConnectionView connection = allConnections[i];
            if (connection == null)
                continue;

            if (excludeConnection != null && connection == excludeConnection)
                continue;

            NodeView otherNode = GetOtherNode(connection, fromNode);
            if (otherNode == null)
                continue;

            if (excludeNeighbor != null && otherNode == excludeNeighbor)
                continue;

            candidates.Add(connection);
        }

        if (candidates.Count == 0)
            return null;

        ConnectionView chosen = candidates[Random.Range(0, candidates.Count)];
        destination = GetOtherNode(chosen, fromNode);
        return chosen;
    }

    private RouteStep BuildRouteStep(NodeView fromNode, ConnectionView connection)
    {
        return new RouteStep
        {
            connection = connection,
            aToB = connection.nodeA == fromNode
        };
    }

    private NodeView GetOtherNode(ConnectionView connection, NodeView currentNode)
    {
        if (connection == null || currentNode == null)
            return null;

        if (connection.nodeA == currentNode)
            return connection.nodeB;

        if (connection.nodeB == currentNode)
            return connection.nodeA;

        return null;
    }

    private SpawnerInfectionParameters GetSpawnerParameters()
    {
        if (payload?.parameters?.spawner == null)
            return new SpawnerInfectionParameters();

        return payload.parameters.spawner;
    }

    private int GetCadenceTicks()
    {
        return GetSpawnerParameters().GetSafeCadenceTicks();
    }

    private int GetBurstSize()
    {
        return GetSpawnerParameters().GetSafeBurstSize();
    }

    private PacketKind GetSpawnKind()
    {
        PacketKind kind = GetSpawnerParameters().spawnKind;
        return kind == PacketKind.None ? PacketKind.Virus : kind;
    }

    private int GetSpawnScanDifficulty()
    {
        return GetSpawnerParameters().GetSafeScanDifficulty();
    }

    private int? GetBaseSpeedOverride()
    {
        return GetSpawnerParameters().baseSpeedOverride;
    }
    
}