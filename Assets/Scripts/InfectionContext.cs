public class InfectionContext
{
    public int currentTick;
    public NetworkRuntime networkRuntime;
    public TrafficDirector trafficDirector;
    public CommandDirector commandDirector;

    public InfectionContext(
        int currentTick,
        NetworkRuntime networkRuntime,
        TrafficDirector trafficDirector,
        CommandDirector commandDirector)
    {
        this.currentTick = currentTick;
        this.networkRuntime = networkRuntime;
        this.trafficDirector = trafficDirector;
        this.commandDirector = commandDirector;
    }
}