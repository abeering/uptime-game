using UnityEngine;
using System.Collections.Generic;

public class LevelDirector : MonoBehaviour
{
    public TrafficDirector trafficDirector;

    private List<ILevelEvent> events = new();
    private int currentTick;

    public void Initialize()
    {
        // hardcode one event for now
        events.Add(new InfectionBurstEvent(startTick: 500, duration: 40));
        events.Add(new DdosSwarmEvent(startTick: 120, secondBurstTick: 300));
    }

    public void ProcessTick(int tick)
    {
        currentTick = tick;

        foreach (var e in events)
        {
            if (!e.IsActive(tick))
                continue;

            int localTick = tick - e.StartTick;
            e.OnTick(tick, localTick, trafficDirector);
        }
    }
}