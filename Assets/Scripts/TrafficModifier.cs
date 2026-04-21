using UnityEngine;

[System.Serializable]
public class TrafficModifier
{
    public int remainingTicks;

    public float malwareChanceDelta;
    public float priorityChanceDelta;

    public int spawnIntervalDelta;

    public TrafficModifier(int durationTicks)
    {
        remainingTicks = Mathf.Max(1, durationTicks);
    }

    public bool Tick()
    {
        remainingTicks--;
        return remainingTicks <= 0;
    }
}