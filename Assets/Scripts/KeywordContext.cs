using System.Collections.Generic;

public class KeywordContext
{
    public NetworkRuntime runtime;
    public CommandDirector commandDirector;
    public float deltaTime;

    public Dictionary<PacketView, int> speedModifiers = new();

    public KeywordContext(NetworkRuntime runtime, CommandDirector commandDirector, float deltaTime)
    {
        this.runtime = runtime;
        this.commandDirector = commandDirector;
        this.deltaTime = deltaTime;
    }

    public void AddSpeedModifier(PacketView packet, int delta)
    {
        if (packet == null) return;

        if (!speedModifiers.ContainsKey(packet))
            speedModifiers[packet] = 0;

        speedModifiers[packet] += delta;
    }
}