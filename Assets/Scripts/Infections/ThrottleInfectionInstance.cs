using System.Collections.Generic;
using UnityEngine;

public class ThrottleInfectionInstance : NodeInfectionInstance
{
    private readonly List<ConnectionView> affectedConnections = new();
    private string modifierSourceId;

    public override InfectionType Type => InfectionType.Throttle;
    public override Color? GetNodeTintColor() => new Color(0.25f, 0.45f, 0.85f);
    public override float GetNodeTintStrength() => 0.5f;
    public override int GetVisualPriority() => 0;

    public override void OnApplied()
    {
        if (node == null)
            return;

        NetworkRuntime runtime = Object.FindObjectOfType<NetworkRuntime>();
        if (runtime == null)
        {
            Debug.LogWarning("[ThrottleInfection] No NetworkRuntime found.");
            return;
        }

        modifierSourceId = BuildModifierSourceId();
        int latencyPenalty = GetLatencyPenalty();

        affectedConnections.Clear();

        List<ConnectionView> adjacentConnections = runtime.GetConnectionsForNode(node);
        for (int i = 0; i < adjacentConnections.Count; i++)
        {
            ConnectionView connection = adjacentConnections[i];
            if (connection == null)
                continue;

            connection.AddLatencyModifier(modifierSourceId, latencyPenalty);
            affectedConnections.Add(connection);
        }
    }

    public override void OnRemoved()
    {
        for (int i = 0; i < affectedConnections.Count; i++)
        {
            ConnectionView connection = affectedConnections[i];
            if (connection == null)
                continue;

            connection.RemoveLatencyModifier(modifierSourceId);
        }

        affectedConnections.Clear();
    }

    private int GetLatencyPenalty()
    {
        if (payload?.parameters?.throttle == null)
            return 1;

        return Mathf.Max(0, payload.parameters.throttle.latencyPenalty);
    }

    private string BuildModifierSourceId()
    {
        string nodeId = node != null ? node.nodeId : "unknown";
        return $"infection:throttle:{nodeId}:{GetHashCode()}";
    }
}