using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Tick")]
    [Min(0.1f)]
    public float tickIntervalSeconds = 0.5f;
    public bool autoStart = true;
    public bool logTicks = true;

    [Header("References")]
    public TrafficDirector trafficDirector;
    public NodeDirector nodeDirector;
    public CommandDirector commandDirector;
    public LevelDirector levelDirector;
    public NetworkRuntime networkRuntime;

    [Header("Systems")]
    public bool levelEnabled = false;

    private int tickCount = 0;
    public int CurrentTick => tickCount;

    private bool ticksPaused = false;
    public bool TicksPaused => ticksPaused;

    public static GameController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameController] Duplicate instance found, destroying newest.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshAllConnections();

        if (levelEnabled && levelDirector != null)
            levelDirector.Initialize();

        if (autoStart)
            StartCoroutine(TickLoop());
    }

    private IEnumerator TickLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(tickIntervalSeconds);
            RunTick();
        }
    }

    public void PauseTicks()
    {
        ticksPaused = true;
    }

    public void ResumeTicks()
    {
        ticksPaused = false;
    }

    private void RunTick()
    {
        if (ticksPaused)
            return;

        tickCount++;

        if (logTicks)
            Debug.Log($"--- TICK {tickCount} ---");

        if (levelEnabled && levelDirector != null)
            levelDirector.ProcessTick(tickCount);

        if (trafficDirector != null)
            trafficDirector.ProcessTick(tickCount);

        if (nodeDirector != null)
            nodeDirector.ProcessTick(tickCount);

        if (commandDirector != null)
            commandDirector.ProcessTick();

        if (networkRuntime != null)
            TickConnections();
    }

    private void TickConnections()
    {
        var connections = networkRuntime.GetAllConnections();

        foreach (var connection in connections)
        {
            if (connection != null)
                connection.ProcessTick();
        }
    }

    [ContextMenu("Refresh All Connections")]
    public void RefreshAllConnections()
    {
        ConnectionView[] all = FindObjectsByType<ConnectionView>(FindObjectsSortMode.None);
        foreach (var c in all)
            c.RefreshLine();
    }
}