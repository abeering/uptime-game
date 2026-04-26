using UnityEngine;

public class GlitchDirector : MonoBehaviour
{
    [Header("Panel Overlays")]
    [SerializeField] private ConsoleGlitchOverlay console;
    [SerializeField] private ConsoleGlitchOverlay scan;
    [SerializeField] private ConsoleGlitchOverlay operations;
    [SerializeField] private ConsoleGlitchOverlay status;
    [SerializeField] private ConsoleGlitchOverlay background;

    [Header("Debug / Inspector Control")]
    public bool enableAll = false;

    [Tooltip("Set true to fire a spike once")]
    public bool spikeAll = false;

    [Range(0f, 1f)]
    public float spikeAmount = 0.6f;

    private bool _lastEnableAll;

    private void Awake()
    {
        SetAll(enableAll);
        _lastEnableAll = enableAll;
    }

    private void Update()
    {
        // detect inspector toggle change
        if (enableAll != _lastEnableAll)
        {
            SetAll(enableAll);
            _lastEnableAll = enableAll;
        }

        // one-shot spike from inspector
        if (spikeAll)
        {
            SpikeAll(spikeAmount);
            spikeAll = false;
        }
    }

    public void SetAll(bool enabled)
    {
        console?.SetGlitchEnabled(enabled);
        scan?.SetGlitchEnabled(enabled);
        operations?.SetGlitchEnabled(enabled);
        status?.SetGlitchEnabled(enabled);
        background?.SetGlitchEnabled(enabled);
    }

    public void SpikeAll(float amount = 0.6f)
    {
        console?.TriggerGlitch(amount);
        scan?.TriggerGlitch(amount);
        operations?.TriggerGlitch(amount);
        status?.TriggerGlitch(amount);
        background?.TriggerGlitch(amount);
    }
}