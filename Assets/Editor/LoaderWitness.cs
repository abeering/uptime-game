using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class LoaderWitness
    {
        [MenuItem("Tools/Connections/Run LOADER Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && director != null && recorder != null, "scene bindings");
            int oldSeed = director.Seed;
            bool oldAuto = director.AutoGenerationEnabled;
            bool oldRandomize = director.RandomizeSeed;
            bool oldRecording = recorder.RecordingEnabled;
            FieldInfo travel = Field(typeof(SimulationHost), "connectionTravelSecondsPerLink");
            float oldTravel = (float)travel.GetValue(host);
            FieldInfo overrides = Field(typeof(ConnectionTrafficDirector), "ambientWeightOverrides");
            var oldOverrides = (AmbientWeightOverride[])overrides.GetValue(director);
            NodeResources db = host.Nodes.Single(n => n.View.NodeId == "DB");
            FieldInfo capacity = Field(typeof(NodeResources), "loadCapacity");
            float oldCapacity = (float)capacity.GetValue(db);
            try
            {
                director.Seed = 1701;
                director.RandomizeSeed = false;
                director.AutoGenerationEnabled = false;
                recorder.RecordingEnabled = true;
                travel.SetValue(host, 0.000001f);
                host.SetRunMode(ConnectionRunMode.Generated);
                overrides.SetValue(director, director.Profiles.Where(p => p.purposeTemplate)
                    .Select(p => new AmbientWeightOverride
                    { profileId = p.id, weight = p.id == "inventory-sync" ? 1f : 0f })
                    .ToArray());
                host.ResetSimulation();
                foreach (LoaderStructure structure in Enum.GetValues(typeof(LoaderStructure)))
                    foreach (LoaderPattern pattern in Enum.GetValues(typeof(LoaderPattern)))
                        CheckVariant(host, director, recorder, structure, pattern);

                capacity.SetValue(db, 17f);
                host.ResetSimulation();
                var alternate = director.GenerateLoaderPlanForWitness(0f,
                    LoaderStructure.Single, LoaderPattern.Immediate);
                Check(alternate.LoaderAttack.targetCapacity == 17f &&
                    alternate.LoaderAttack.attackLoad >= 13.6f &&
                    alternate.LoaderAttack.attackLoad <= 20.4f &&
                    alternate.LoaderAttack.attackLoad > alternate.LoaderAttack.normalLoad,
                    "alternate capacity changes attack LOAD without scaling normal PURPOSE LOAD");
                capacity.SetValue(db, oldCapacity);

                host.ResetSimulation(); recorder.BeginRun();
                var interrupted = director.GenerateLoaderPlanForWitness(0f,
                    LoaderStructure.Single, LoaderPattern.Overload);
                director.QueueSpawnPlan(interrupted);
                LogicalConnectionState blocked = StepUntilAttack(host, interrupted.ConnectionId);
                Check(host.TryInjectInfection("DB", InfectionKind.Blackout, 0f, out _) &&
                    blocked.Resolution == ConnectionResolution.Canceled &&
                    blocked.TerminationCause == ConnectionTerminationCause.Blackout &&
                    blocked.Behaviors.ActiveModifiers.Count == 0,
                    "Blackout cancels attack and cleans operation modifiers");
                host.StepSimulation(interrupted.LoaderAttack.durationSeconds + 1f);
                Check(recorder.CurrentLog.events.Count(e => e.connectionId == interrupted.ConnectionId &&
                    e.type == "connection_resolved") == 1 &&
                    recorder.CurrentLog.events.All(e => e.connectionId != interrupted.ConnectionId ||
                        e.type != "loader_withdrawal"),
                    "Blackout interruption cannot withdraw or resolve twice");
                Debug.Log("LoaderWitness PASS: all six structure/pattern combinations, " +
                    "capacity scaling, onset, contention, withdrawal, kill, Blackout, and replay.");
            }
            finally
            {
                capacity.SetValue(db, oldCapacity);
                overrides.SetValue(director, oldOverrides);
                travel.SetValue(host, oldTravel);
                director.Seed = oldSeed;
                director.RandomizeSeed = oldRandomize;
                director.AutoGenerationEnabled = oldAuto;
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                host.ResetSimulation();
            }
        }

        private static void CheckVariant(SimulationHost host, ConnectionTrafficDirector director,
            ConnectionsRunRecorder recorder, LoaderStructure structure, LoaderPattern pattern)
        {
            host.ResetSimulation(); recorder.BeginRun();
            ConnectionSpawnPlan plan = director.GenerateLoaderPlanForWitness(0f, structure, pattern);
            LoaderAttackPlan attack = plan.LoaderAttack;
            Check(plan.ProfileId == "inventory-sync" && plan.IsHostile &&
                plan.Route.Last().NodeId == "DB" && attack != null &&
                attack.structure == structure && attack.pattern == pattern &&
                attack.attackOperationIndex == plan.Operations.Length - 1 &&
                attack.targetCapacity == 10f && attack.attackLoad >= 8f &&
                attack.attackLoad <= 12f && attack.attackLoad > attack.normalLoad &&
                plan.GenerationTrace.objective == "LOADER" &&
                plan.GenerationTrace.normalOperations.Length == 2 &&
                plan.GenerationTrace.loaderAttack != null,
                structure + "/" + pattern + " selects valid canonical plan and capacity-relative attack: " +
                plan.ProfileId + " target=" + plan.Route.Last().NodeId +
                " capacity=" + attack?.targetCapacity + " normal=" + attack?.normalLoad +
                " attack=" + attack?.attackLoad + " baselineStages=" +
                plan.GenerationTrace.normalOperations.Length);
            Check(plan.Operations.Length == (structure == LoaderStructure.Delayed ? 3 : 1) &&
                plan.Operations.All(o => o.EffectOnCompletion == CompletionEffect.None),
                "structure and no-delivery effect " + structure + "/" + pattern);
            if (structure == LoaderStructure.Delayed)
                for (int i = 0; i < 2; i++)
                    Check(plan.Operations[i].Operation.ToString() ==
                        plan.GenerationTrace.normalOperations[i].kind &&
                        Approx(plan.Operations[i].DesiredLoad,
                            plan.GenerationTrace.normalOperations[i].desiredLoad) &&
                        Approx(plan.Operations[i].TotalWork,
                            plan.GenerationTrace.normalOperations[i].totalWork),
                        "DELAYED retains original multi-operation PURPOSE arc " + i);
            if (pattern == LoaderPattern.Spiking)
                Check(attack.onsetSeconds >= 5f && attack.onsetSeconds <= 15f &&
                    attack.spikeQuietSeconds == 5f && attack.spikeElevatedSeconds == 2f &&
                    attack.onsetSeconds != attack.spikeQuietSeconds,
                    "first SPIKING onset is independent of repeating quiet interval");
            if (pattern != LoaderPattern.Immediate)
                Check(plan.Operations.Last().DesiredLoad == attack.normalLoad &&
                    plan.Operations.Last().TotalWork > attack.normalLoad * attack.onsetSeconds,
                    "normal pre-attack demand and sufficient WORK survive sampled onset");
            director.QueueSpawnPlan(plan, "loader-witness");
            var peer = new ConnectionSpawnPlan(0f, "peer", plan.Route,
                "loader-witness", "Legitimate service",
                new[] { new OperationStage(OperationKind.Read, 10000f, 4f, "Service") });
            director.QueueSpawnPlan(peer, "loader-witness");
            RunEvent queued = recorder.CurrentLog.events.First(e =>
                e.connectionId == plan.ConnectionId && e.type == "plan_queued");
            Check(queued.definition.loaderAttack != null &&
                queued.generation.loaderAttack != null &&
                queued.definition.operations.Length == plan.Operations.Length,
                "complete attack plan is recorded before spawn");
            LogicalConnectionState state = StepUntilAttack(host, plan.ConnectionId);
            LogicalConnectionState benign = host.FindConnection("peer");
            Check(state.IsActive && benign.IsActive &&
                state.EffectiveWorkRateMultiplier == 0f && state.ActualWorkRate == 0f &&
                Approx(state.LoaderWithdrawAtSeconds - state.LoaderActivatedAtSeconds,
                    attack.durationSeconds) &&
                attack.durationSeconds >= 30f && attack.durationSeconds <= 60f,
                "attack activation pauses WORK and schedules duration from actual onset");
            if (pattern == LoaderPattern.Immediate || pattern == LoaderPattern.Overload ||
                pattern == LoaderPattern.Spiking)
                Check(Approx(state.DesiredLoad - state.NaturalLoadDelta, attack.attackLoad) &&
                    benign.AllocatedLoad < benign.DesiredLoad &&
                    state.AllocatedLoad > 0f,
                    "attack consumes capacity and causes real peer contention");
            if (pattern == LoaderPattern.Spiking)
            {
                float accomplished = state.CompletedWork;
                host.StepSimulation(2.5f);
                Check(state.IsActive && state.EffectiveWorkRateMultiplier == 0f &&
                    Approx(state.DesiredLoad - state.NaturalLoadDelta, attack.normalLoad) &&
                    Approx(state.CompletedWork, accomplished),
                    "SPIKING quiet phase keeps attack-active WORK paused");
            }
            float untilWithdrawal = state.LoaderWithdrawAtSeconds - host.SimulationTimeSeconds;
            host.StepSimulation(untilWithdrawal + 0.1f);
            Check(state.Resolution == ConnectionResolution.Withdrawn &&
                state.TerminationCause == ConnectionTerminationCause.AttackerWithdrawal &&
                state.HealthPenaltyAmount == 0f &&
                state.Behaviors.ActiveModifiers.Count == 0 &&
                !host.RuntimeConnections.Contains(state) &&
                host.FindConnection(plan.ConnectionId) == state &&
                Approx(state.ResolvedAtSeconds, state.LoaderWithdrawAtSeconds) &&
                host.Occurrences.All(o => o.ConnectionId != plan.ConnectionId) &&
                Approx(benign.AllocatedLoad, benign.DesiredLoad),
                "withdrawal releases LOAD, retains inspect history, and avoids effects/penalty");
            Check(recorder.CurrentLog.events.Count(e => e.connectionId == plan.ConnectionId &&
                e.type == "loader_activated") == 1 &&
                recorder.CurrentLog.events.Count(e => e.connectionId == plan.ConnectionId &&
                    e.type == "loader_withdrawal") == 1 &&
                recorder.CurrentLog.events.Count(e => e.connectionId == plan.ConnectionId &&
                    e.type == "connection_resolved" && e.resolutionReason == "Withdrawn") == 1,
                "activation, withdrawal, and distinct resolution each record once");
            host.StepSimulation(2f);
            Check(!host.KillConnection(plan.ConnectionId) &&
                recorder.CurrentLog.events.Count(e => e.connectionId == plan.ConnectionId &&
                    e.type == "connection_resolved") == 1,
                "withdrawn LOADER cannot resolve again");

            host.ResetSimulation(); recorder.BeginRun();
            ConnectionSpawnPlan replay = director.GenerateLoaderPlanForWitness(0f, structure, pattern);
            Check(replay.ConnectionId == plan.ConnectionId &&
                JsonUtility.ToJson(replay.GenerationTrace) ==
                JsonUtility.ToJson(plan.GenerationTrace),
                "seed/reset replay reproduces concrete LOADER decisions");
            director.QueueSpawnPlan(replay);
            LogicalConnectionState killed = StepUntilAttack(host, replay.ConnectionId);
            Check(host.KillConnection(replay.ConnectionId) &&
                killed.Resolution == ConnectionResolution.Killed &&
                killed.Behaviors.ActiveModifiers.Count == 0 &&
                killed.AllocatedLoad == 0f,
                "player kill interrupts attack and releases LOAD");
            host.StepSimulation(attack.durationSeconds + 1f);
            Check(recorder.CurrentLog.events.All(e => e.connectionId != replay.ConnectionId ||
                e.type != "loader_withdrawal"),
                "killed LOADER does not later withdraw");
        }

        private static LogicalConnectionState StepUntilAttack(SimulationHost host, string id)
        {
            LogicalConnectionState state = null;
            for (int i = 0; i < 900; i++)
            {
                host.StepSimulation(0.5f);
                state = host.FindConnection(id);
                if (state != null && state.LoaderActivatedAtSeconds >= 0f) return state;
                Check(state == null || !state.Resolution.HasValue,
                    "LOADER cannot finish before first attack activation");
            }
            throw new InvalidOperationException("LoaderWitness: attack did not activate " + id);
        }

        private static FieldInfo Field(Type type, string name) => type.GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Missing field " + name);
        private static bool Approx(float a, float b) => Mathf.Abs(a - b) < 0.01f;
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("LoaderWitness: " + message);
        }
    }
}
