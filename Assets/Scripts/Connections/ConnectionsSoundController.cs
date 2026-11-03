using System;
using UnityEngine;

namespace Uptime.Connections
{
    // Scene-owned presentation of simulation and UI events. Never drives gameplay.
    public sealed class ConnectionsSoundController : MonoBehaviour
    {
        [Header("Event sources")]
        [SerializeField] private SimulationHost simulationHost;
        [SerializeField] private Terminal terminal;
        [SerializeField] private TextCommunicationDirector textCommunications;
        [SerializeField] private ConnectionsVideoCall videoCall;
        [Header("Playback")]
        [SerializeField] private AudioSource sourcePrefab;
        [SerializeField, Min(1)] private int pooledSourceCount = 8;
        [SerializeField, Range(0f, 1f)] private float masterSfxVolume = 0.8f;
        [SerializeField, Min(0f)] private float connectionArrivalCooldown = 0.18f;
        [SerializeField, Min(0f)] private float messageCooldown = 0.12f;
        [Header("Network")]
        [SerializeField] private AudioCue wormStatus;
        [SerializeField] private AudioCue statusPortscan;
        [SerializeField] private AudioCue packetSpawn;
        [SerializeField] private AudioCue probeSweepingLoop;
        [Header("Outcomes")]
        [SerializeField] private AudioCue interceptSuccess;
        [SerializeField] private AudioCue nodeInfected;
        [SerializeField] private AudioCue scanCompleteBenign;
        [Header("Workstation")]
        [SerializeField] private AudioCue chatNotification;
        [SerializeField] private AudioCue commandError;
        [SerializeField] private AudioCue videoCallOpen;

        private AudioSource[] sources;
        private AudioSource probeSource;
        private int nextSource;
        private float nextArrivalAt;
        private float nextMessageAt;
        // Audio variation is independent of UnityEngine.Random, which traffic uses.
        private readonly System.Random audioRandom = new();

        private void Awake()
        {
            if (!Application.isPlaying || sourcePrefab == null) return;
            sources = new AudioSource[Mathf.Max(1, pooledSourceCount)];
            for (int i = 0; i < sources.Length; i++)
                sources[i] = CreateSource("One Shot " + (i + 1), false);
            probeSource = CreateSource("Probe Loop", true);
        }

        private AudioSource CreateSource(string label, bool loop)
        {
            AudioSource source = Instantiate(sourcePrefab, transform);
            source.name = label;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = loop;
            source.Stop();
            return source;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (simulationHost != null)
            {
                simulationHost.ConnectionRegistered += OnConnectionRegistered;
                simulationHost.ConditionApplied += OnConditionApplied;
                simulationHost.ScanCompleted += OnScanCompleted;
                simulationHost.SimulationReset += OnSimulationReset;
                if (simulationHost.Payloads != null)
                    simulationHost.Payloads.InterceptSucceeded += OnInterceptSucceeded;
            }
            if (terminal != null) terminal.CommandFailed += OnCommandFailed;
            if (textCommunications != null)
                textCommunications.MessageDelivered += OnMessageDelivered;
            if (videoCall != null) videoCall.CallOpened += OnCallOpened;
        }

        private void OnDisable()
        {
            if (simulationHost != null)
            {
                simulationHost.ConnectionRegistered -= OnConnectionRegistered;
                simulationHost.ConditionApplied -= OnConditionApplied;
                simulationHost.ScanCompleted -= OnScanCompleted;
                simulationHost.SimulationReset -= OnSimulationReset;
                if (simulationHost.Payloads != null)
                    simulationHost.Payloads.InterceptSucceeded -= OnInterceptSucceeded;
            }
            if (terminal != null) terminal.CommandFailed -= OnCommandFailed;
            if (textCommunications != null)
                textCommunications.MessageDelivered -= OnMessageDelivered;
            if (videoCall != null) videoCall.CallOpened -= OnCallOpened;
            StopAllSources();
        }

        private void Update() => ReconcileProbe();

        private void ReconcileProbe()
        {
            if (probeSource == null) return;
            bool active = simulationHost != null && simulationHost.isActiveAndEnabled &&
                simulationHost.ProbedLink != null && simulationHost.ProbeActiveRemaining > 0f;
            AudioClip clip = FirstClip(probeSweepingLoop);
            if (!active || clip == null)
            {
                if (probeSource.isPlaying) probeSource.Stop();
                return;
            }
            if (probeSource.clip != clip || !probeSource.isPlaying)
            {
                probeSource.clip = clip;
                probeSource.pitch = 1f;
                probeSource.Play();
            }
            probeSource.volume = Mathf.Clamp01(masterSfxVolume) * Mathf.Clamp01(probeSweepingLoop.volume);
        }

        private void OnSimulationReset()
        {
            StopAllSources();
            nextArrivalAt = nextMessageAt = 0f;
        }

        private void StopAllSources()
        {
            if (sources != null)
                foreach (AudioSource source in sources)
                    if (source != null) source.Stop();
            if (probeSource != null) probeSource.Stop();
        }

        private void OnConditionApplied(NodeCondition condition)
        {
            if (condition == null) return;
            if (!string.IsNullOrEmpty(condition.SourceWormBranchId)) Play(wormStatus);
            else if (condition.Kind == ConditionKind.PortScanned &&
                !string.IsNullOrEmpty(condition.SourceConnectionId)) Play(statusPortscan);
        }

        private void OnConnectionRegistered(LogicalConnectionState _) =>
            PlayAmbient(packetSpawn, connectionArrivalCooldown, ref nextArrivalAt);
        private void OnInterceptSucceeded(PayloadState _) => Play(interceptSuccess);
        private void OnScanCompleted(NodeScanSession session)
        {
            if (session == null) return;
            Play(session.Result == NodeScanResult.Infected ? nodeInfected : scanCompleteBenign);
        }
        private void OnMessageDelivered(TextCommunication _) =>
            PlayAmbient(chatNotification, messageCooldown, ref nextMessageAt);
        private void OnCommandFailed() => Play(commandError);
        private void OnCallOpened() => Play(videoCallOpen);

        private void PlayAmbient(AudioCue cue, float cooldown, ref float nextAt)
        {
            float now = Time.unscaledTime;
            if (now < nextAt) return;
            if (Play(cue, ambient: true)) nextAt = now + Mathf.Max(0f, cooldown);
        }

        private bool Play(AudioCue cue, bool ambient = false)
        {
            if (cue?.clips == null || cue.clips.Length == 0 || sources == null) return false;
            AudioClip clip = cue.clips[audioRandom.Next(cue.clips.Length)];
            if (clip == null) return false;
            AudioSource source = null;
            for (int i = 0; i < sources.Length; i++)
            {
                int index = (nextSource + i) % sources.Length;
                if (sources[index] == null || sources[index].isPlaying) continue;
                source = sources[index];
                nextSource = (index + 1) % sources.Length;
                break;
            }
            if (source == null)
            {
                if (ambient) return false;
                source = sources[nextSource];
                nextSource = (nextSource + 1) % sources.Length;
            }
            if (source == null) return false;
            source.clip = clip;
            source.volume = Mathf.Clamp01(masterSfxVolume) * Mathf.Clamp01(cue.volume);
            source.pitch = Mathf.Lerp(cue.pitchMin, cue.pitchMax, (float)audioRandom.NextDouble());
            source.Play();
            return true;
        }

        private static AudioClip FirstClip(AudioCue cue) =>
            cue?.clips != null && cue.clips.Length > 0 ? cue.clips[0] : null;
    }
}
