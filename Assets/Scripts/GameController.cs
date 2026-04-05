using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Tick")]
    [Min(0.1f)]
    public float tickIntervalSeconds = 1f;
    public bool autoStart = true;
    public bool logTicks = true;

    [Header("References")]
    public TrafficDirector trafficDirector;
    public NodeDirector nodeDirector;
    public CommandDirector commandDirector;

    private int tickCount = 0;

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

    private void RunTick()
    {
        tickCount++;

        if (logTicks)
            Debug.Log($"--- TICK {tickCount} ---");

        if (trafficDirector != null)
            trafficDirector.ProcessTick(tickCount);

        if (nodeDirector != null)
            nodeDirector.ProcessTick(tickCount);

        if (commandDirector != null)
            commandDirector.ProcessTick();
    }

    [ContextMenu("Refresh All Connections")]
    public void RefreshAllConnections()
    {
        ConnectionView[] all = FindObjectsByType<ConnectionView>(FindObjectsSortMode.None);
        foreach (var c in all)
            c.RefreshLine();
    }
}