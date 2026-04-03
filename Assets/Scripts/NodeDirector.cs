using UnityEngine;

public class NodeDirector : MonoBehaviour
{
    [Header("References")]
    public NetworkRuntime networkRuntime;
    public TrafficDirector trafficDirector;
    public CommandDirector commandDirector;

    public void ProcessTick(int currentTick)
    {
        if (networkRuntime == null)
            return;

        InfectionContext context = new InfectionContext(
            currentTick,
            networkRuntime,
            trafficDirector,
            commandDirector
        );

        var nodes = networkRuntime.GetAllNodes();

        for (int i = 0; i < nodes.Count; i++)
        {
            NodeView node = nodes[i];

            if (node == null)
                continue;

            node.Tick(context);
        }
    }
}