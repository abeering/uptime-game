using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public enum ConsequenceMode { Defaults, DefaultsWithAdditions, Explicit, CustomWeights }
    public enum ConsequenceKind { Infection, Condition, WormOutbreak, Payload }
    public enum ConsequenceTactic { BreachLockdown, ConditionWormOutbreak, PayloadBurst }

    [Serializable]
    public sealed class ConsequenceEffectSpec
    {
        public ConsequenceKind kind;
        public string targetNodeId;
        public InfectionKind infectionKind = InfectionKind.PhantomLoad;
        public float parameter = 3f;
        public ConditionKind conditionKind = ConditionKind.CleanLocked;
        public float durationSeconds = 15f;
        public WormEffect wormEffect = WormEffect.ScanJammed;
        public PayloadMotionProfile payloadProfile = PayloadMotionProfile.Steady;
        [Min(1)] public int payloadCount = 1;

        public ConsequenceEffectSpec Copy() => (ConsequenceEffectSpec)MemberwiseClone();
    }

    // One entry is one weighted alternative, or one part of an explicit together-list.
    [Serializable]
    public sealed class ConsequenceCandidate
    {
        public string id;
        [Min(0f)] public float weight = 1f;
        public ConsequenceTactic[] tactics;
        public ConsequenceEffectSpec[] effects;
        public InfectionKind breachInfectionKind = InfectionKind.PhantomLoad;
        public float breachInfectionParameter = 3f;
        public float cleanLockSeconds = 15f;

        public ConsequenceCandidate Copy() => new ConsequenceCandidate
        {
            id = id, weight = weight,
            breachInfectionKind = breachInfectionKind,
            breachInfectionParameter = breachInfectionParameter,
            cleanLockSeconds = cleanLockSeconds,
            tactics = tactics == null ? Array.Empty<ConsequenceTactic>() :
                (ConsequenceTactic[])tactics.Clone(),
            effects = CopyEffects(effects)
        };

        private static ConsequenceEffectSpec[] CopyEffects(ConsequenceEffectSpec[] source)
        {
            if (source == null) return Array.Empty<ConsequenceEffectSpec>();
            var copy = new ConsequenceEffectSpec[source.Length];
            for (int i = 0; i < copy.Length; i++) copy[i] = source[i]?.Copy();
            return copy;
        }

        public static ConsequenceCandidate Tactic(string id, ConsequenceTactic tactic,
            float weight = 1f) => new ConsequenceCandidate
            { id = id, weight = weight, tactics = new[] { tactic } };
        public static ConsequenceCandidate Atomic(string id, ConsequenceEffectSpec effect,
            float weight = 1f) => new ConsequenceCandidate
            { id = id, weight = weight, effects = new[] { effect } };
    }

    public sealed class ConsequenceConfiguration
    {
        public ConsequenceMode Mode { get; }
        public bool ReplaceDefaults { get; }
        public IReadOnlyList<ConsequenceCandidate> Entries { get; }

        private ConsequenceConfiguration(ConsequenceMode mode,
            IEnumerable<ConsequenceCandidate> entries, bool replaceDefaults)
        {
            Mode = mode;
            ReplaceDefaults = replaceDefaults;
            var copy = new List<ConsequenceCandidate>();
            if (entries != null)
                foreach (ConsequenceCandidate entry in entries) copy.Add(entry?.Copy());
            Entries = copy.AsReadOnly();
        }

        public static ConsequenceConfiguration Defaults() =>
            new ConsequenceConfiguration(ConsequenceMode.Defaults, null, false);
        public static ConsequenceConfiguration Additions(params ConsequenceCandidate[] entries) =>
            new ConsequenceConfiguration(ConsequenceMode.DefaultsWithAdditions, entries, false);
        public static ConsequenceConfiguration Explicit(params ConsequenceCandidate[] together) =>
            new ConsequenceConfiguration(ConsequenceMode.Explicit, together, true);
        public static ConsequenceConfiguration Custom(bool replaceDefaults,
            params ConsequenceCandidate[] entries) =>
            new ConsequenceConfiguration(ConsequenceMode.CustomWeights, entries, replaceDefaults);
    }

    // Values chosen at scheduling are retained here. The executor never selects again.
    public sealed class ConsequenceEffect
    {
        private readonly WormTuning wormTuning;
        public ConsequenceKind Kind { get; }
        public string TargetNodeId { get; }
        public string OriginNodeId { get; }
        public InfectionKind InfectionKind { get; }
        public float Parameter { get; }
        public ConditionKind ConditionKind { get; }
        public float DurationSeconds { get; }
        public WormEffect WormEffect { get; }
        public WormTuning WormTuning => wormTuning?.Copy();
        public PayloadMotionProfile PayloadProfile { get; }

        internal ConsequenceEffect(ConsequenceKind kind, string target, string origin,
            InfectionKind infection, float parameter, ConditionKind condition,
            float duration, WormEffect worm, WormTuning tuning, PayloadMotionProfile profile)
        {
            Kind = kind; TargetNodeId = target; OriginNodeId = origin;
            InfectionKind = infection; Parameter = parameter;
            ConditionKind = condition; DurationSeconds = duration;
            WormEffect = worm; wormTuning = tuning?.Copy(); PayloadProfile = profile;
        }
    }

    public sealed class ConsequencePlan
    {
        private readonly ConsequenceEffect[] effects;
        private readonly ConsequenceCandidate[] candidates;
        private readonly string[] candidateReasons;
        internal bool Executed;
        internal SimulationHost Host { get; }
        internal int SimulationGeneration { get; }
        public string Id { get; }
        public string Source { get; }
        public string TargetNodeId { get; }
        public float ScheduledAtSeconds { get; }
        public ConsequenceMode Mode { get; }
        public int Seed { get; }
        public float SelectionRoll { get; }
        public string SelectedCandidateId { get; }
        public IReadOnlyList<ConsequenceEffect> Effects => Array.AsReadOnly(effects);
        public IReadOnlyList<ConsequenceCandidate> Candidates =>
            Array.AsReadOnly(Array.ConvertAll(candidates, c => c.Copy()));
        // Null means eligible for the roll (or explicit expansion).
        public IReadOnlyList<string> CandidateReasons => Array.AsReadOnly(candidateReasons);

        internal ConsequencePlan(SimulationHost host, string id, string source, string target, float scheduledAt,
            ConsequenceMode mode, int seed, float roll, string selected,
            List<ConsequenceCandidate> effective, List<string> reasons,
            List<ConsequenceEffect> resolved)
        {
            Host = host; SimulationGeneration = host.SimulationGeneration;
            Id = id; Source = source; TargetNodeId = target;
            ScheduledAtSeconds = scheduledAt; Mode = mode; Seed = seed;
            SelectionRoll = roll; SelectedCandidateId = selected;
            candidates = effective.ConvertAll(c => c.Copy()).ToArray();
            candidateReasons = reasons.ToArray();
            effects = resolved.ToArray();
        }
    }

    public sealed class ConsequenceComponentResult
    {
        public int Index { get; internal set; }
        public ConsequenceEffect Effect { get; internal set; }
        public bool Succeeded { get; internal set; }
        public string Reason { get; internal set; }
        public string CreatedId { get; internal set; }
    }

    public sealed class ConsequenceExecution
    {
        public string PlanId { get; internal set; }
        public float ExecutedAtSeconds { get; internal set; }
        public IReadOnlyList<ConsequenceComponentResult> Components { get; internal set; }
        public bool AnySucceeded
        {
            get
            {
                foreach (var item in Components) if (item.Succeeded) return true;
                return false;
            }
        }
    }

    public static class ConsequencePlanner
    {
        public static ConsequenceCandidate[] StandardPool(InfectionKind infectionKind =
            InfectionKind.PhantomLoad, float infectionParameter = 3f) => new[]
        {
            ConsequenceCandidate.Atomic("phantom-load", new ConsequenceEffectSpec
                { kind = ConsequenceKind.Infection, infectionKind = infectionKind,
                    parameter = infectionParameter }, 2f),
            new ConsequenceCandidate { id = "breach-lockdown", weight = 2f,
                tactics = new[] { ConsequenceTactic.BreachLockdown },
                breachInfectionKind = infectionKind,
                breachInfectionParameter = infectionParameter, cleanLockSeconds = 15f },
            ConsequenceCandidate.Tactic("condition-worm", ConsequenceTactic.ConditionWormOutbreak, 2f),
            ConsequenceCandidate.Tactic("payload-burst", ConsequenceTactic.PayloadBurst, 2f)
        };

        public static bool TryPlan(SimulationHost host, string planId, string source,
            string targetNodeId, IEnumerable<ConsequenceCandidate> defaults,
            ConsequenceConfiguration config, int seed, out ConsequencePlan plan,
            out string reason)
        {
            plan = null; reason = null;
            if (host == null || host.FindNodeResources(targetNodeId) == null)
            { reason = "unknown_target"; return false; }
            if (string.IsNullOrWhiteSpace(planId))
            { reason = "missing_plan_id"; return false; }
            config ??= ConsequenceConfiguration.Defaults();
            var effective = new List<ConsequenceCandidate>();
            if (config.Mode != ConsequenceMode.Explicit && !config.ReplaceDefaults && defaults != null)
                foreach (ConsequenceCandidate entry in defaults) Upsert(effective, entry);
            if (config.Mode != ConsequenceMode.Defaults)
                foreach (ConsequenceCandidate entry in config.Entries) Upsert(effective, entry);
            if (config.Mode == ConsequenceMode.Explicit && effective.Count == 0)
            { reason = "empty_explicit_plan"; return false; }
            var eligible = new List<ConsequenceCandidate>();
            var candidateReasons = new List<string>();
            foreach (ConsequenceCandidate entry in effective)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) ||
                    float.IsNaN(entry.weight) || float.IsInfinity(entry.weight) || entry.weight < 0f)
                { reason = "invalid_candidate_or_weight"; return false; }
                if (config.Mode != ConsequenceMode.Explicit && entry.weight == 0f)
                { candidateReasons.Add("zero_weight"); continue; }
                if (CanExpand(host, targetNodeId, entry, out string why))
                { eligible.Add(entry); candidateReasons.Add(null); }
                else if (config.Mode == ConsequenceMode.Explicit)
                { reason = why; return false; }
                else candidateReasons.Add(why);
            }
            if (eligible.Count == 0)
            { reason = "no_eligible_consequences"; return false; }
            var random = new System.Random(seed);
            float roll = -1f;
            var resolved = new List<ConsequenceEffect>();
            string selected;
            if (config.Mode == ConsequenceMode.Explicit)
            {
                selected = string.Join("+", eligible.ConvertAll(e => e.id));
                foreach (ConsequenceCandidate entry in eligible)
                    Expand(host, targetNodeId, entry, random, resolved);
            }
            else
            {
                float total = 0f;
                foreach (ConsequenceCandidate entry in eligible) total += entry.weight;
                if (float.IsInfinity(total) || total <= 0f)
                { reason = "invalid_total_weight"; return false; }
                roll = (float)random.NextDouble();
                float cursor = roll * total;
                ConsequenceCandidate choice = eligible[eligible.Count - 1];
                foreach (ConsequenceCandidate entry in eligible)
                {
                    cursor -= entry.weight;
                    if (cursor < 0f) { choice = entry; break; }
                }
                selected = choice.id;
                Expand(host, targetNodeId, choice, random, resolved);
            }
            plan = new ConsequencePlan(host, planId, source, targetNodeId,
                host.SimulationTimeSeconds, config.Mode, seed, roll, selected,
                effective, candidateReasons, resolved);
            return true;
        }

        private static void Upsert(List<ConsequenceCandidate> entries, ConsequenceCandidate entry)
        {
            if (entry == null) { entries.Add(null); return; }
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && string.Equals(entries[i].id, entry.id,
                    StringComparison.OrdinalIgnoreCase))
                { entries[i] = entry.Copy(); return; }
            entries.Add(entry.Copy());
        }

        private static bool CanExpand(SimulationHost host, string targetId,
            ConsequenceCandidate entry, out string reason)
        {
            reason = null;
            int count = (entry.effects?.Length ?? 0) + (entry.tactics?.Length ?? 0);
            if (count == 0) { reason = "empty_candidate"; return false; }
            foreach (ConsequenceTactic tactic in entry.tactics ?? Array.Empty<ConsequenceTactic>())
            {
                if (tactic == ConsequenceTactic.BreachLockdown &&
                    (!Enum.IsDefined(typeof(InfectionKind), entry.breachInfectionKind) ||
                     float.IsNaN(entry.breachInfectionParameter) ||
                     float.IsInfinity(entry.breachInfectionParameter) ||
                     ((entry.breachInfectionKind == InfectionKind.PhantomLoad ||
                       entry.breachInfectionKind == InfectionKind.Spawner ||
                       entry.breachInfectionKind == InfectionKind.PayloadSpawner) ?
                        entry.breachInfectionParameter <= 0f :
                        entry.breachInfectionParameter != 0f) ||
                     entry.cleanLockSeconds <= 0f || float.IsNaN(entry.cleanLockSeconds) ||
                     float.IsInfinity(entry.cleanLockSeconds)))
                { reason = "invalid_breach_tuning"; return false; }
                if (tactic == ConsequenceTactic.ConditionWormOutbreak &&
                    (host.Worms == null || host.Worms.IsNodeIsolated(
                        host.FindNodeResources(targetId)?.View)))
                { reason = "worm_origin_unavailable"; return false; }
                if (tactic == ConsequenceTactic.PayloadBurst &&
                    (host.Payloads == null || PayloadTargets(host, targetId).Count == 0))
                { reason = "no_payload_destination"; return false; }
                if (!Enum.IsDefined(typeof(ConsequenceTactic), tactic))
                { reason = "unknown_tactic"; return false; }
            }
            foreach (ConsequenceEffectSpec spec in entry.effects ?? Array.Empty<ConsequenceEffectSpec>())
            {
                if (spec == null || !Enum.IsDefined(typeof(ConsequenceKind), spec.kind))
                { reason = "invalid_effect"; return false; }
                string target = spec.targetNodeId ?? targetId;
                if (host.FindNodeResources(target) == null)
                { reason = "unknown_effect_target"; return false; }
                if (spec.kind == ConsequenceKind.Infection &&
                    (!Enum.IsDefined(typeof(InfectionKind), spec.infectionKind) ||
                     float.IsNaN(spec.parameter) || float.IsInfinity(spec.parameter) ||
                     ((spec.infectionKind == InfectionKind.PhantomLoad ||
                       spec.infectionKind == InfectionKind.Spawner ||
                       spec.infectionKind == InfectionKind.PayloadSpawner) ?
                        spec.parameter <= 0f : spec.parameter != 0f)))
                { reason = "invalid_infection"; return false; }
                if (spec.kind == ConsequenceKind.Condition &&
                    (spec.durationSeconds <= 0f || float.IsNaN(spec.durationSeconds) ||
                     float.IsInfinity(spec.durationSeconds) ||
                     !Enum.IsDefined(typeof(ConditionKind), spec.conditionKind)))
                { reason = "invalid_condition"; return false; }
                if (spec.kind == ConsequenceKind.WormOutbreak &&
                    (host.Worms == null || !Enum.IsDefined(typeof(WormEffect), spec.wormEffect) ||
                     host.Worms.IsNodeIsolated(host.FindNodeResources(target).View)))
                { reason = "worm_origin_unavailable"; return false; }
                if (spec.kind == ConsequenceKind.Payload &&
                    (host.Payloads == null || spec.payloadCount < 1 ||
                     !Enum.IsDefined(typeof(PayloadMotionProfile), spec.payloadProfile) ||
                     PayloadTargets(host, target).Count == 0))
                { reason = "no_payload_destination"; return false; }
            }
            return true;
        }

        private static void Expand(SimulationHost host, string targetId,
            ConsequenceCandidate entry, System.Random random, List<ConsequenceEffect> output)
        {
            foreach (ConsequenceTactic tactic in entry.tactics ?? Array.Empty<ConsequenceTactic>())
            {
                if (tactic == ConsequenceTactic.BreachLockdown)
                {
                    AddSpec(host, targetId, new ConsequenceEffectSpec
                        { kind = ConsequenceKind.Infection,
                            infectionKind = entry.breachInfectionKind,
                            parameter = entry.breachInfectionParameter }, random, output);
                    AddSpec(host, targetId, new ConsequenceEffectSpec
                        { kind = ConsequenceKind.Condition,
                            conditionKind = ConditionKind.CleanLocked,
                            durationSeconds = entry.cleanLockSeconds }, random, output);
                }
                else if (tactic == ConsequenceTactic.ConditionWormOutbreak)
                    AddSpec(host, targetId, new ConsequenceEffectSpec
                        { kind = ConsequenceKind.WormOutbreak,
                            wormEffect = random.Next(2) == 0 ? WormEffect.ScanJammed :
                                WormEffect.Backdoor }, random, output);
                else if (tactic == ConsequenceTactic.PayloadBurst)
                    AddSpec(host, targetId, new ConsequenceEffectSpec
                        { kind = ConsequenceKind.Payload, payloadCount = 2 + random.Next(2),
                            payloadProfile = PayloadMotionProfile.Variable }, random, output);
            }
            foreach (ConsequenceEffectSpec spec in entry.effects ?? Array.Empty<ConsequenceEffectSpec>())
                AddSpec(host, targetId, spec, random, output);
        }

        private static void AddSpec(SimulationHost host, string fallback,
            ConsequenceEffectSpec spec, System.Random random, List<ConsequenceEffect> output)
        {
            string target = spec.targetNodeId ?? fallback;
            if (spec.kind != ConsequenceKind.Payload)
            {
                output.Add(new ConsequenceEffect(spec.kind, target, null,
                    spec.infectionKind, spec.parameter, spec.conditionKind,
                    spec.durationSeconds, spec.wormEffect,
                    spec.kind == ConsequenceKind.WormOutbreak ? host.Worms.DefaultTuning : null,
                    spec.payloadProfile));
                return;
            }
            List<string> destinations = PayloadTargets(host, target);
            int count = Mathf.Min(spec.payloadCount, destinations.Count);
            for (int i = 0; i < count; i++)
            {
                int selected = random.Next(destinations.Count);
                string destination = destinations[selected];
                destinations.RemoveAt(selected);
                output.Add(new ConsequenceEffect(ConsequenceKind.Payload, destination, target,
                    spec.infectionKind, 0f, spec.conditionKind, 0f, spec.wormEffect, null,
                    spec.payloadProfile));
            }
        }

        private static List<string> PayloadTargets(SimulationHost host, string originId)
        {
            var result = new List<string>();
            NodeView origin = host.FindNodeResources(originId)?.View;
            if (origin == null) return result;
            var visited = new HashSet<NodeView> { origin };
            var queue = new Queue<NodeView>(); queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                NodeView node = queue.Dequeue();
                foreach (PhysicalLinkReference link in host.PhysicalLinks)
                {
                    NodeView next = link?.View?.NodeA == node ? link.View.NodeB :
                        link?.View?.NodeB == node ? link.View.NodeA : null;
                    if (next != null && visited.Add(next)) queue.Enqueue(next);
                }
            }
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            foreach (NodeView node in visited)
            {
                if (node == origin) continue;
                bool ingress = false;
                if (traffic != null)
                    foreach (AmbientOriginWeight entry in traffic.EffectiveAmbientOrigins)
                        if (entry != null && string.Equals(entry.nodeId, node.NodeId,
                            StringComparison.OrdinalIgnoreCase)) { ingress = true; break; }
                if (!ingress) result.Add(node.NodeId);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }
    }

    public static class ConsequenceExecutor
    {
        public static ConsequenceExecution Execute(SimulationHost host, ConsequencePlan plan)
        {
            if (host == null || plan == null || plan.Executed ||
                plan.Host != host || plan.SimulationGeneration != host.SimulationGeneration)
                throw new InvalidOperationException("Consequence plan unavailable, stale, or already executed.");
            plan.Executed = true;
            var results = new List<ConsequenceComponentResult>();
            for (int i = 0; i < plan.Effects.Count; i++)
            {
                ConsequenceEffect effect = plan.Effects[i];
                var result = new ConsequenceComponentResult { Index = i, Effect = effect };
                string reason = null;
                try
                {
                    if (effect.Kind == ConsequenceKind.Infection)
                        result.Succeeded = host.TryApplyConsequenceInfection(
                            effect.TargetNodeId, effect.InfectionKind, effect.Parameter,
                            "consequence:" + plan.Id, out reason);
                    else if (effect.Kind == ConsequenceKind.Condition)
                    {
                        host.ApplyCondition(host.FindNodeResources(effect.TargetNodeId),
                            new ConditionPayload(effect.ConditionKind, effect.DurationSeconds),
                            "consequence:" + plan.Id);
                        result.Succeeded = true;
                    }
                    else if (effect.Kind == ConsequenceKind.WormOutbreak)
                    {
                        result.CreatedId = host.Worms.Spawn(effect.TargetNodeId,
                            effect.WormEffect, effect.WormTuning).Id;
                        result.Succeeded = true;
                    }
                    else if (effect.Kind == ConsequenceKind.Payload)
                    {
                        result.CreatedId = host.Payloads.Spawn(effect.TargetNodeId,
                            effect.PayloadProfile, effect.OriginNodeId).Id;
                        result.Succeeded = true;
                    }
                    else result.Reason = "unknown_effect";
                    if (!result.Succeeded && result.Reason == null)
                        result.Reason = effect.Kind == ConsequenceKind.Infection ?
                            reason : "effect_rejected";
                }
                catch (InvalidOperationException error) { result.Reason = error.Message; }
                results.Add(result);
                host.Recorder?.RecordConsequenceComponent(plan, result,
                    host.SimulationTimeSeconds);
            }
            var execution = new ConsequenceExecution { PlanId = plan.Id,
                ExecutedAtSeconds = host.SimulationTimeSeconds,
                Components = results.AsReadOnly() };
            host.Recorder?.RecordConsequenceExecution(plan, execution);
            return execution;
        }
    }
}
