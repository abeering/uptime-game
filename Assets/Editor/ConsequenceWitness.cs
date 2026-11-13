using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class ConsequenceWitness
    {
        [MenuItem("Tools/Connections/Run Consequence Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            var activities = host.GetComponent<HostileActivityDirector>();
            var duel = host.GetComponent<ConnectionsDuelDirector>();
            var status = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            traffic.AutoGenerationEnabled = false;
            traffic.RandomizeSeed = false;
            host.SetRunMode(ConnectionRunMode.Generated);
            typeof(ConnectionsDuelDirector).GetMethod("OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(duel, null);
            try
            {
                Planner(host, traffic, recorder);
                Tactics(host, traffic, recorder, status);
                Ambient(host, traffic, recorder, activities, duel);
            }
            finally
            {
                typeof(ConnectionsDuelDirector).GetMethod("OnDisable",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(duel, null);
                traffic.AutoGenerationEnabled = false;
                host.ResetSimulation();
            }
            FourNodeTopology();
            Debug.Log("ConsequenceWitness PASS: selection modes, repeatability, " +
                "independent execution, timed lock, Worm and Payload tactics, " +
                "ambient direct and carrier Duel routes, recorder evidence.");
        }

        private static void Planner(SimulationHost host, ConnectionTrafficDirector traffic,
            ConnectionsRunRecorder recorder)
        {
            Reset(host, traffic, recorder);
            var defaults = ConsequencePlanner.StandardPool();
            Check(Plan(host, "defaults", defaults,
                ConsequenceConfiguration.Defaults(), 23).Candidates.Count == 4,
                "defaults retain mixed atomic and Tactic alternatives");
            var extra = ConsequenceCandidate.Atomic("extra", new ConsequenceEffectSpec
                { kind = ConsequenceKind.Condition, conditionKind = ConditionKind.ScanJammed,
                    durationSeconds = 7f }, 5f);
            var additions = Plan(host, "additions", defaults,
                ConsequenceConfiguration.Additions(extra), 23);
            Check(additions.Candidates.Count == 5 &&
                additions.Candidates.Single(c => c.id == "extra").weight == 5f,
                "additions join one weighted table");
            var one = Plan(host, "one", defaults, ConsequenceConfiguration.Explicit(extra), 23);
            Check(one.SelectionRoll < 0f && one.Effects.Count == 1 &&
                one.Effects[0].Kind == ConsequenceKind.Condition,
                "explicit atomic has no weighted roll");
            var combined = Plan(host, "combined", defaults,
                ConsequenceConfiguration.Explicit(extra,
                    ConsequenceCandidate.Tactic("lock", ConsequenceTactic.BreachLockdown)), 23);
            Check(combined.Effects.Count == 3 && combined.SelectionRoll < 0f,
                "explicit list executes together and expands Tactic");
            var overridePlan = Plan(host, "override", defaults,
                ConsequenceConfiguration.Custom(false,
                    ConsequenceCandidate.Tactic("payload-burst",
                        ConsequenceTactic.PayloadBurst, 0f), extra), 23);
            Check(overridePlan.Candidates.Count == 5 &&
                overridePlan.Candidates.Single(c => c.id == "payload-burst").weight == 0f &&
                overridePlan.CandidateReasons[Array.FindIndex(
                    overridePlan.Candidates.ToArray(), c => c.id == "payload-burst")] ==
                    "zero_weight" &&
                overridePlan.SelectedCandidateId != "payload-burst",
                "identity override excludes candidate without duplication");
            var replacement = Plan(host, "replace", defaults,
                ConsequenceConfiguration.Custom(true, extra), 23);
            Check(replacement.Candidates.Count == 1 &&
                replacement.SelectedCandidateId == "extra", "custom replacement table");
            Check(!ConsequencePlanner.TryPlan(host, "zero", "witness", "API", defaults,
                ConsequenceConfiguration.Custom(true,
                    ConsequenceCandidate.Atomic("off", extra.effects[0], 0f)), 23,
                out _, out string zero) && zero == "no_eligible_consequences",
                "zero weights reject empty selection");
            Check(!ConsequencePlanner.TryPlan(host, "negative", "witness", "API", defaults,
                ConsequenceConfiguration.Custom(true,
                    ConsequenceCandidate.Atomic("bad", extra.effects[0], -1f)), 23,
                out _, out string negative) && negative == "invalid_candidate_or_weight",
                "negative weight rejected");
            Check(!ConsequencePlanner.TryPlan(host, "missing", "witness", "missing",
                defaults, null, 23, out _, out string missing) &&
                missing == "unknown_target", "unknown target rejected");
            var repeat = Plan(host, "repeat", defaults,
                ConsequenceConfiguration.Defaults(), 23);
            Check(repeat.SelectedCandidateId ==
                Plan(host, "repeat2", defaults, ConsequenceConfiguration.Defaults(), 23)
                    .SelectedCandidateId && repeat.SelectionRoll ==
                Plan(host, "repeat3", defaults, ConsequenceConfiguration.Defaults(), 23)
                    .SelectionRoll, "seed and equivalent state repeat selection");
            var fixedPlan = Plan(host, "fixed", defaults,
                ConsequenceConfiguration.Explicit(
                    ConsequenceCandidate.Tactic("burst", ConsequenceTactic.PayloadBurst)), 21);
            var destinations = fixedPlan.Effects.Select(e => e.TargetNodeId).ToArray();
            Check(destinations.Length >= 2 && destinations.Distinct().Count() ==
                destinations.Length && fixedPlan.Effects.All(e => e.OriginNodeId == "API"),
                "burst resolves distinct reachable internal destinations at schedule time");
            Check(host.TryInjectInfection("API", InfectionKind.PhantomLoad, 2f, out _),
                "change state after schedule");
            Check(fixedPlan.Effects.Select(e => e.TargetNodeId)
                .SequenceEqual(destinations), "scheduled destination choices stay fixed");
            recorder.RecordConsequencePlan(fixedPlan);
            var recorded = recorder.CurrentLog.events.Last(e => e.type == "consequence_planned");
            Check(recorded.consequenceMode == "Explicit" && recorded.consequenceSeed == 21 &&
                recorded.consequenceEffects.Length == destinations.Length &&
                recorded.consequenceEffects.All(e => e.originNodeId == "API"),
                "recorder retains selection, seed, and concrete routes");
            recorder.RecordConsequencePlan(overridePlan);
            Check(recorder.CurrentLog.events.Last(e => e.type == "consequence_planned")
                .consequenceCandidates.Any(c => c.id == "payload-burst" &&
                    !c.eligible && c.ineligibilityReason == "zero_weight"),
                "recorder includes exclusion reason for replay");
            host.ResetSimulation();
            bool staleRejected = false;
            try { ConsequenceExecutor.Execute(host, fixedPlan); }
            catch (InvalidOperationException) { staleRejected = true; }
            Check(staleRejected, "reset invalidates a scheduled plan");
            var linksField = typeof(SimulationHost).GetField("physicalLinks",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var authoredLinks = host.PhysicalLinks;
            try
            {
                linksField.SetValue(host, Array.Empty<PhysicalLinkReference>());
                Check(!ConsequencePlanner.TryPlan(host, "disconnected", "witness", "API",
                    null, ConsequenceConfiguration.Explicit(ConsequenceCandidate.Tactic(
                        "burst", ConsequenceTactic.PayloadBurst)), 1,
                    out _, out string noRoute) && noRoute == "no_payload_destination",
                    "disconnected topology rejects Payload burst");
            }
            finally { linksField.SetValue(host, authoredLinks); }
        }

        private static void Tactics(SimulationHost host, ConnectionTrafficDirector traffic,
            ConnectionsRunRecorder recorder, NodeStatusPresenter status)
        {
            Reset(host, traffic, recorder);
            var breach = Plan(host, "breach", null, ConsequenceConfiguration.Explicit(
                ConsequenceCandidate.Tactic("breach", ConsequenceTactic.BreachLockdown)), 1);
            var applied = ConsequenceExecutor.Execute(host, breach);
            Check(applied.Components.Count == 2 && applied.Components.All(c => c.Succeeded) &&
                host.ConditionFor(host.FindNodeResources("API").View,
                    ConditionKind.CleanLocked) != null && host.Infections.Count == 1,
                "breach installs infection and timed CLEAN restriction");
            Check(host.StartClean("API", "clean API", out _) == CleanStartResult.Locked,
                "new CLEAN rejected on locked node");
            status.RenderNow(0f, 1f);
            Check(status.StateFor(host.FindNodeResources("API").View).CleanLocked &&
                status.ViewFor(host.FindNodeResources("API").View)
                    .GetComponentsInChildren<TMPro.TMP_Text>(true)
                    .Any(t => t.text.Contains("CLEAN LOCKED")),
                "Pain stack shows remaining lock timer");
            bool duplicateRejected = false;
            try { ConsequenceExecutor.Execute(host, breach); }
            catch (InvalidOperationException) { duplicateRejected = true; }
            Check(duplicateRejected, "same plan cannot execute twice");
            host.StepSimulation(15.1f);
            Check(host.ConditionFor(host.FindNodeResources("API").View,
                ConditionKind.CleanLocked) == null &&
                host.StartClean("API", "clean API", out _) == CleanStartResult.Started,
                "lock expires in simulation time and CLEAN resumes");
            Reset(host, traffic, recorder);
            var lockOnly = Plan(host, "lock-only", null, ConsequenceConfiguration.Explicit(
                ConsequenceCandidate.Atomic("lock", new ConsequenceEffectSpec
                { kind = ConsequenceKind.Condition, conditionKind = ConditionKind.CleanLocked,
                    durationSeconds = 4f })), 1);
            ConsequenceExecutor.Execute(host, lockOnly);
            Check(host.Infections.Count == 0 && host.StartClean("API", "clean API",
                out _) == CleanStartResult.Locked, "lock on healthy node implies no infection");
            Reset(host, traffic, recorder);
            Check(host.TryInjectInfection("API", InfectionKind.PhantomLoad, 2f, out _),
                "existing CLEAN infection setup");
            Check(host.StartClean("API", "clean API", out var session) ==
                CleanStartResult.Started, "existing CLEAN session setup");
            ConsequenceExecutor.Execute(host, Plan(host, "lock-during-clean", null,
                ConsequenceConfiguration.Explicit(ConsequenceCandidate.Atomic("lock",
                    new ConsequenceEffectSpec { kind = ConsequenceKind.Condition,
                        conditionKind = ConditionKind.CleanLocked, durationSeconds = 5f })), 1));
            Check(host.SelectCleanPid(session.Processes.Single(p =>
                    p.CpuPercent >= 82).Pid, out int removed) ==
                CleanSelectionResult.Success && removed == 1,
                "existing CLEAN survives lock and uses captured infection");
            Reset(host, traffic, recorder);
            Check(host.TryInjectInfection("API", InfectionKind.PhantomLoad, 2f, out _),
                "partial rejection setup");
            var partial = ConsequenceExecutor.Execute(host, Plan(host, "partial", null,
                ConsequenceConfiguration.Explicit(ConsequenceCandidate.Tactic("breach",
                    ConsequenceTactic.BreachLockdown)), 1));
            Check(!partial.Components[0].Succeeded &&
                partial.Components[0].Reason == "infection_already_active" &&
                partial.Components[1].Succeeded && recorder.CurrentLog.events.Count(e =>
                    e.type == "consequence_component_result" &&
                    e.consequencePlanId == "partial") == 2,
                "partial failure records both actual component results");
            Reset(host, traffic, recorder);
            var worm = ConsequenceExecutor.Execute(host, Plan(host, "worm", null,
                ConsequenceConfiguration.Explicit(ConsequenceCandidate.Tactic("worm",
                    ConsequenceTactic.ConditionWormOutbreak)), 1));
            Check(worm.AnySucceeded && host.Worms.Outbreaks.Count == 1 &&
                host.ConditionFor(host.FindNodeResources("API").View,
                    host.Worms.Outbreaks[0].Effect == WormEffect.Backdoor ?
                        ConditionKind.Backdoor : ConditionKind.ScanJammed) != null,
                "real Worm starts and applies selected Condition");
            host.StepSimulation(45f);
            Check(host.Worms.Outbreaks[0].Branches.Count > 1,
                "Worm follows ordinary branching and propagation");
            Reset(host, traffic, recorder);
            var isolatedWormPlan = Plan(host, "isolate", null,
                ConsequenceConfiguration.Explicit(ConsequenceCandidate.Tactic("worm",
                    ConsequenceTactic.ConditionWormOutbreak)), 1);
            Check(host.Worms.TryIsolate("API", "isolate API", out _),
                "Worm isolation setup");
            var isolated = ConsequenceExecutor.Execute(host, isolatedWormPlan);
            Check(!isolated.AnySucceeded && isolated.Components[0].Reason.Contains("isolated"),
                "isolation before trigger rejects frozen Worm plan");
            Reset(host, traffic, recorder);
            var burst = ConsequenceExecutor.Execute(host, Plan(host, "burst", null,
                ConsequenceConfiguration.Explicit(ConsequenceCandidate.Tactic("burst",
                    ConsequenceTactic.PayloadBurst)), 7));
            Check(burst.Components.Count >= 2 && burst.Components.All(c => c.Succeeded) &&
                host.Payloads.Payloads.Count == burst.Components.Count &&
                host.Payloads.Payloads.All(p => p.Route[0].NodeId == "API"),
                "real Payload burst launches from Duel target");
            var first = host.Payloads.Payloads.First(p => p.Route.Length == 2);
            Check(host.Payloads.TryIntercept(first.Id, first.FinalNode.NodeId, out _),
                "normal interception can arm against burst member");
            host.StepSimulation(8f);
            Check(first.Phase == PayloadPhase.Resolved && first.Intercepted &&
                !first.EffectApplied, "burst member can be caught without infection");
            host.StepSimulation(90f);
            Check(host.Payloads.Payloads.Any(p => p != first && p.EffectApplied),
                "uncaught burst member delivers its normal infection effect");
            Check(recorder.CurrentLog.events.Count(e => e.type ==
                "consequence_component_result" && e.consequencePlanId == "burst" &&
                e.payloadId != null) == burst.Components.Count,
                "recorder keeps each launched Payload identity");
        }

        private static void Ambient(SimulationHost host, ConnectionTrafficDirector traffic,
            ConnectionsRunRecorder recorder, HostileActivityDirector activities,
            ConnectionsDuelDirector duel)
        {
            activities.OrdinaryWeight = activities.ReconnaissanceWeight =
                activities.PayloadWeight = activities.WormWeight =
                activities.PayloadLauncherWeight = 0f;
            activities.DirectDuelWeight = 1f;
            activities.ConnectionDuelWeight = 0f;
            Reset(host, traffic, recorder);
            var direct = activities.QueueAmbientActivity(0f);
            Check(direct.Activity == "direct_duel" && direct.DuelId > 0 &&
                duel.Current?.Source == "ambient_direct_duel" &&
                recorder.CurrentLog.events.Any(e => e.type ==
                    "hostile_activity_selected" && e.duelId == direct.DuelId),
                "direct ambient opportunity schedules Duel and records pathway");
            Check(!duel.TrySchedule("API", out string busy) && busy.Contains("pending"),
                "one global pending Duel slot");
            Check(recorder.CurrentLog.events.Any(e => e.type == "duel_rejected" &&
                e.reason.Contains("pending")),
                "occupied slot rejection is recorded");
            Reset(host, traffic, recorder);
            activities.DirectDuelWeight = 0f;
            activities.ConnectionDuelWeight = 1f;
            var carrier = activities.QueueAmbientActivity(0f);
            Check(carrier.Activity == "connection_duel" && carrier.ConnectionId != null &&
                carrier.ConnectionPlan.Operations[0].EffectOnCompletion ==
                    CompletionEffect.DuelSchedule && duel.Current == null,
                "connection ambient opportunity queues target-aware carrier");
            for (int i = 0; i < 120 && duel.Current == null; i++)
                host.StepSimulation(.5f);
            Check(duel.Current != null && duel.Current.Source.StartsWith("connection:") &&
                duel.Current.LossPlan != null,
                "carrier completion schedules stored Duel plan");
            Reset(host, traffic, recorder);
            var killed = activities.QueueAmbientActivity(0f);
            host.StepSimulation(.2f);
            Check(host.KillConnection(killed.ConnectionId),
                "carrier kill accepted before completion");
            host.StepSimulation(60f);
            Check(duel.Current == null && !recorder.CurrentLog.events.Any(e =>
                e.type == "duel_scheduled"), "killing carrier prevents Duel");
            activities.DirectDuelWeight = 1f;
            activities.ConnectionDuelWeight = 1f;
        }

        private static ConsequencePlan Plan(SimulationHost host, string id,
            ConsequenceCandidate[] defaults, ConsequenceConfiguration config, int seed)
        {
            Check(ConsequencePlanner.TryPlan(host, id, "witness", "API", defaults,
                config, seed, out var plan, out string reason), id + ": " + reason);
            return plan;
        }

        private static void FourNodeTopology()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            host.SetRunMode(ConnectionRunMode.Generated);
            Check(ConsequencePlanner.TryPlan(host, "small-burst", "witness", "API",
                null, ConsequenceConfiguration.Explicit(ConsequenceCandidate.Tactic(
                    "burst", ConsequenceTactic.PayloadBurst)), 1,
                out ConsequencePlan plan, out _) && plan.Effects.Count == 2 &&
                plan.Effects.Select(e => e.TargetNodeId).OrderBy(id => id)
                    .SequenceEqual(new[] { "AUTH", "DB" }),
                "four-node topology caps burst at two distinct internal targets");
        }

        private static void Reset(SimulationHost host, ConnectionTrafficDirector traffic,
            ConnectionsRunRecorder recorder)
        {
            traffic.Seed = 1701;
            host.ResetSimulation();
            recorder.BeginRun();
        }

        private static void Check(bool ok, string message)
        {
            if (!ok) throw new Exception("ConsequenceWitness: " + message);
        }
    }
}
