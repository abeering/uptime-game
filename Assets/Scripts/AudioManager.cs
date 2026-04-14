using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource sfxSourcePrefab;
    [SerializeField] private int pooledSourceCount = 8;

    [Header("Master Volume")]
    [Range(0f, 1f)] [SerializeField] private float sfxMasterVolume = 1f;

    [Header("UI / Command")]
    [SerializeField] private AudioCue commandAccepted;
    [SerializeField] private AudioCue commandRejected;
    [SerializeField] private AudioCue click;

    [Header("Operations")]
    [SerializeField] private AudioCue scanStarted;
    [SerializeField] private AudioCue operationComplete;
    [SerializeField] private AudioCue operationFailed;

    [Header("Blocks")]
    [SerializeField] private AudioCue blockPlaced;
    [SerializeField] private AudioCue blockTriggered;

    [Header("Threat / Network")]
    [SerializeField] private AudioCue threatIdentified;
    [SerializeField] private AudioCue infectionStarted;
    [SerializeField] private AudioCue nodeRecovered;
    [SerializeField] private AudioCue alert;

    [Header("Notifications")]
    [SerializeField] private AudioCue newNotification;

    private AudioSource[] pooledSources;
    private int nextSourceIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildPool();
    }

    private void BuildPool()
    {
        pooledSources = new AudioSource[pooledSourceCount];

        for (int i = 0; i < pooledSourceCount; i++)
        {
            AudioSource source = Instantiate(sfxSourcePrefab, transform);
            source.playOnAwake = false;
            pooledSources[i] = source;
        }
    }

    private AudioSource GetNextSource()
    {
        if (pooledSources == null || pooledSources.Length == 0)
            return null;

        AudioSource source = pooledSources[nextSourceIndex];
        nextSourceIndex = (nextSourceIndex + 1) % pooledSources.Length;
        return source;
    }

    public void Play(AudioCue cue)
    {
        if (cue == null)
            return;

        AudioClip clip = cue.GetRandomClip();
        if (clip == null)
            return;

        AudioSource source = GetNextSource();
        if (source == null)
            return;

        source.clip = clip;
        source.volume = cue.volume * sfxMasterVolume;
        source.pitch = cue.GetRandomPitch();
        source.Play();
    }

    public void SetSfxMasterVolume(float value)
    {
        sfxMasterVolume = Mathf.Clamp01(value);
    }

    // commands 
    public void PlayCommandAccepted() => Play(commandAccepted);
    public void PlayCommandRejected() => Play(commandRejected);
    public void PlayClick() => Play(click);

    // scanning 
    public void PlayScanStarted() => Play(scanStarted);
    public void PlayThreatIdentified() => Play(threatIdentified);

    // operatiions
    public void PlayOperationComplete() => Play(operationComplete);
    public void PlayOperationFailed() => Play(operationFailed);

    // threat / network
    public void PlayInfectionStarted() => Play(infectionStarted);
    public void PlayNodeRecovered() => Play(nodeRecovered);

    // blocks and blocking 
    public void PlayBlockPlaced() => Play(blockPlaced);
    public void PlayBlockTriggered() => Play(blockTriggered);

    // notifications 
    public void PlayNewNotification() => Play(newNotification);
    
    public void PlayAlert() => Play(alert);
}