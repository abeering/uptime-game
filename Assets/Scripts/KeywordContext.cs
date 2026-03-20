public class KeywordContext
{
    public NetworkRuntime runtime;
    public CommandDirector commandDirector;
    public float deltaTime;

    public KeywordContext(NetworkRuntime runtime, CommandDirector commandDirector, float deltaTime)
    {
        this.runtime = runtime;
        this.commandDirector = commandDirector;
        this.deltaTime = deltaTime;
    }
}