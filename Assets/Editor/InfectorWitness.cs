using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class InfectorWitness
    {
        [MenuItem("Tools/Connections/Run INFECTOR Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && director != null && recorder != null, "scene bindings");
            bool oldAuto = director.AutoGenerationEnabled;
            bool oldRandomize = director.RandomizeSeed;
            bool oldRecording = recorder.RecordingEnabled;
            int oldSeed = director.Seed;
            FieldInfo travel = Field(typeof(SimulationHost), "connectionTravelSecondsPerLink");
            float oldTravel = (float)travel.GetValue(host);
            FieldInfo overrides = Field(typeof(ConnectionTrafficDirector), "ambientWeightOverrides");
            var oldOverrides = (AmbientWeightOverride[])overrides.GetValue(director);
            try
            {
                director.AutoGenerationEnabled = false;
                director.RandomizeSeed = false;
                director.Seed = 1701;
                recorder.RecordingEnabled = true;
                travel.SetValue(host, 0.000001f);
                host.SetRunMode(ConnectionRunMode.Generated);
                Check(director.InfectorDelayedChance == 0.33f &&
                    director.InfectorMinDeliverySeconds == 25f &&
                    director.InfectorMinFinalEvidenceSeconds == 18f,
                    "authored provisional INFECTOR tuning");
                foreach (string purpose in new[] { "product-lookup", "order-submit" })
                {
                    SelectPurpose(host, director, overrides, purpose);
                    foreach (InfectorStructure structure in Enum.GetValues(typeof(InfectorStructure)))
                        foreach (InfectorSignature signature in new[]
                        {
                            InfectorSignature.HighLoad, InfectorSignature.LowLoad,
                            InfectorSignature.OperationDeviation, InfectorSignature.WorkLong,
                            InfectorSignature.Waving
                        })
                            CheckRecipe(host, director, recorder, purpose, structure, signature);
                }
                SelectPurpose(host, director, overrides, "product-lookup");
                CheckSubtleAndTiming(host, director);
                CheckSecondaryAndWeights(host, director);
                CheckWavingSharedEvidence(host, director, overrides);
                CheckWavingInterruption(host, director, recorder);
                CheckWavingCorrection(host, director, recorder, overrides);
                CheckLifecycle(host, director, recorder);
                CheckManualAndSpawner(host, director, recorder);
                CheckAmbiguousOutlier(host, director, overrides);
                Debug.Log("InfectorWitness PASS: structures, signatures, weighted decisions, " +
                    "timing, subtle delivery, ambiguity, kill/Blackout interruption, " +
                    "one-shot installation, manual parameters, Spawner lineage, replay, JSON.");
            }
            finally
            {
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

        private static void CheckRecipe(SimulationHost host, ConnectionTrafficDirector director,
            ConnectionsRunRecorder recorder, string purpose, InfectorStructure structure,
            InfectorSignature signature)
        {
            host.ResetSimulation(); recorder.BeginRun();
            ConnectionSpawnPlan plan = director.GenerateInfectorPlanForWitness(0f, structure,
                signature, InfectorSignature.None, InfectionKind.PhantomLoad);
            var trace = plan.GenerationTrace;
            var infector = trace.infector;
            int baseline = trace.normalOperations.Length;
            Check(plan.ProfileId == purpose && plan.IsHostile && plan.LoaderAttack == null &&
                trace.hasInfector &&
                trace.objective == "DELIVERY" && infector != null &&
                infector.structure == structure.ToString().ToUpperInvariant() &&
                infector.primarySignature != "none" &&
                infector.secondarySignature == "none" &&
                trace.anomalies.Length == 1 &&
                infector.deliveryOperationIndex == plan.Operations.Length - 1 &&
                infector.finalNominalDeliverySeconds >= 24.999f &&
                plan.Operations.Last().EffectOnCompletion == CompletionEffect.InfectionDelivery &&
                plan.Operations.Last().InfectionPayload.Kind == InfectionKind.PhantomLoad &&
                plan.Operations.All(o => o.Behaviors.All(b => b.WorkRateMultiplier == 1f)),
                "valid PURPOSE and ordinary productive DELIVERY plan: " + purpose + "/" +
                structure + "/" + signature);
            if (structure == InfectorStructure.Delayed)
            {
                Check(plan.Operations.Length == baseline + 1 && baseline ==
                    (purpose == "order-submit" ? 2 : 1), "DELAYED appends exactly one stage");
                for (int i = 0; i < baseline; i++)
                    Check(plan.Operations[i].Operation.ToString() == trace.normalOperations[i].kind &&
                        Approx(plan.Operations[i].DesiredLoad,
                            trace.normalOperations[i].desiredLoad) &&
                        Approx(plan.Operations[i].TotalWork,
                            trace.normalOperations[i].totalWork) &&
                        plan.Operations[i].EffectOnCompletion == CompletionEffect.None,
                        "DELAYED retains the complete sampled PURPOSE arc " + i);
                Check(infector.finalOperationNominalSeconds >= 17.999f,
                    "appended evidence has an investigation window");
            }
            else Check(plan.Operations.Length == baseline ||
                signature == InfectorSignature.OperationDeviation &&
                plan.Operations.Length == baseline + 1,
                "STANDARD derives the existing PURPOSE arc");
            if (signature == InfectorSignature.HighLoad)
            {
                float normalLoad = structure == InfectorStructure.Delayed ?
                    trace.normalOperations.Last().desiredLoad : trace.normalOperations[0].desiredLoad;
                float elevated = structure == InfectorStructure.Delayed ?
                    plan.Operations.Last().DesiredLoad : plan.Operations[0].DesiredLoad;
                Check(elevated >= normalLoad * 2.5f - 0.001f &&
                    elevated <= normalLoad * 4f + 0.001f &&
                    infector.finalNominalDeliverySeconds >= infector.normalNominalDeliverySeconds - 0.01f,
                    "high LOAD raises demand without compressing delivery time");
            }
            if (signature == InfectorSignature.WorkLong)
            {
                Check((structure == InfectorStructure.Delayed ?
                    plan.Operations.Last().DesiredLoad ==
                        trace.normalOperations.Last().desiredLoad :
                    plan.Operations.Select(o => o.DesiredLoad).SequenceEqual(
                        trace.normalOperations.Select(o => o.desiredLoad))) &&
                    (structure == InfectorStructure.Delayed ?
                    plan.Operations.Last().TotalWork >=
                        trace.normalOperations.Last().totalWork * 2f - 0.001f :
                    plan.Operations.Where((o, i) => o.TotalWork >=
                        trace.normalOperations[i].totalWork * 2f - 0.001f).Any()),
                    "WORK-only long duration retains normal LOAD");
            if (signature == InfectorSignature.Waving)
            {
                var selections = trace.selectedKeywords.Where(k => k.behaviorKind == "Waving").ToArray();
                Check(selections.Length == 1 && trace.anomalies.Single() == "waving" &&
                    selections[0].operationIndex >= 0 &&
                    selections[0].operationIndex < plan.Operations.Length &&
                    plan.NaturalLoadNoise &&
                    infector.finalConservativeDeliverySeconds >= 24.999f &&
                    infector.finalOperationConservativeSeconds >=
                        (structure == InfectorStructure.Delayed ? 20f : 0f),
                    "WAVING signature, natural noise, and conservative delivery evidence");
                var wave = plan.Operations[selections[0].operationIndex].Behaviors.Single(b =>
                    b.Kind == ConnectionBehaviorKind.Waving);
                Check(wave.MinimumLoadOffset < 0f && wave.MaximumLoadOffset >= 0.4f &&
                    wave.StartAfterSeconds >= 2f && wave.StartAfterSeconds <= 5f &&
                    wave.WavePeriodSeconds >= 15f && wave.WavePeriodSeconds <= 30f &&
                    wave.WorkRateMultiplier == 1f,
                    "WAVING sampled bounds, period, onset, and productive WORK");
            }
            }
            director.QueueSpawnPlan(plan, "infector-witness");
            RunEvent queued = recorder.CurrentLog.events.Single(e =>
                e.type == "plan_queued" && e.connectionId == plan.ConnectionId);
            Check(queued.generation.infector != null &&
                queued.definition.operations.Length == plan.Operations.Length &&
                queued.definition.operations.Last().infectionKind == "PhantomLoad" &&
                queued.generation.infector.finalNominalDeliverySeconds ==
                    infector.finalNominalDeliverySeconds,
                "queue records concrete plan and complete INFECTOR decision");
            if (signature == InfectorSignature.Waving)
            {
                int selectedIndex = trace.selectedKeywords.Single(k =>
                    k.behaviorKind == "Waving").operationIndex;
                var sampled = plan.Operations[selectedIndex].Behaviors.Single(b =>
                    b.Kind == ConnectionBehaviorKind.Waving);
                BehaviorRecord recorded = queued.definition.operations[selectedIndex].behaviors
                    .Single(b => b.kind == "Waving");
                Check(queued.generation.infector.primarySignature == "waving" &&
                    queued.generation.selectedKeywords.Single().operationIndex == selectedIndex &&
                    recorded.minimumLoadOffset == sampled.MinimumLoadOffset &&
                    recorded.maximumLoadOffset == sampled.MaximumLoadOffset &&
                    recorded.wavePeriodSeconds == sampled.WavePeriodSeconds &&
                    recorded.startAfterSeconds == sampled.StartAfterSeconds &&
                    queued.generation.infector.finalConservativeDeliverySeconds >= 25f - 0.001f,
                    "queued WAVING plan preserves operation, parameters, and corrected timing");
            }
            Check(recorder.Save(false) && File.ReadAllText(recorder.OutputPath)
                .Contains("\"hasInfector\": true") &&
                File.ReadAllText(recorder.OutputPath).Contains("\"primarySignature\""),
                "INFECTOR decisions persist in JSON");
            if (signature == InfectorSignature.Waving)
            {
                var wave = plan.Operations.SelectMany(o => o.Behaviors)
                    .Single(b => b.Kind == ConnectionBehaviorKind.Waving);
                int waveIndex = trace.selectedKeywords.Single(k =>
                    k.behaviorKind == "Waving").operationIndex;
                LogicalConnectionState active = null;
                for (int i = 0; i < 1200; i++)
                {
                    host.StepSimulation(0.2f);
                    active = host.FindConnection(plan.ConnectionId);
                    if (active?.IsActive == true && active.CurrentOperationIndex == waveIndex)
                        break;
                }
                Check(active?.IsActive == true && active.CurrentOperationIndex == waveIndex,
                    "uncontended INFECTOR reaches its WAVING operation");
                float beforeWork = active.CompletedWork;
                for (int i = 0; i < 35; i++) host.StepSimulation(0.2f);
                Check(active.CurrentOperationIndex == waveIndex &&
                    active.CompletedWork > beforeWork && active.AllocatedLoad > 0f &&
                    active.DesiredLoad > active.BaseLoad + active.NaturalLoadDelta + 0.1f &&
                    active.Behaviors.ActiveModifiers.Any(m => m.SourceId == "WAVING"),
                    "INFECTOR WAVING composes with natural noise while productive WORK advances");
                LogicalConnectionState completed = WaitForResolution(host, plan.ConnectionId);
                RunEvent resolved = recorder.CurrentLog.events.Single(e =>
                    e.type == "connection_resolved" && e.connectionId == plan.ConnectionId);
                Check(completed.Resolution == ConnectionResolution.Completed &&
                    resolved.resolvedAtSeconds - resolved.establishedAtSeconds >= 24.8f &&
                    host.Infections.Count == 1 &&
                    recorder.CurrentLog.events.Count(e => e.type == "infection_applied" &&
                        e.connectionId == plan.ConnectionId) == 1,
                    "uncontended WAVING INFECTOR retains actual delivery window and installs once");
                if (structure == InfectorStructure.Delayed)
                {
                    float actualFinal = recorder.CurrentLog.events.Single(e =>
                        e.type == "operation_completed" && e.connectionId == plan.ConnectionId &&
                        e.operationIndex == plan.Operations.Length - 1).actualDurationSeconds;
                    Check(actualFinal >= wave.StartAfterSeconds + 17.8f,
                        "DELAYED WAVING remains observable for 18 seconds after onset");
                    Debug.Log($"InfectorWitness WAVING {purpose}/{structure}: actual " +
                        $"delivery={resolved.resolvedAtSeconds - resolved.establishedAtSeconds:F1}s, " +
                        $"final={actualFinal:F1}s, onset={wave.StartAfterSeconds:F1}s, " +
                        $"WORK added={infector.deliveryWindowWorkAdded:F1}");
                }
                else Debug.Log($"InfectorWitness WAVING {purpose}/{structure}: actual " +
                    $"delivery={resolved.resolvedAtSeconds - resolved.establishedAtSeconds:F1}s, " +
                    $"WORK added={infector.deliveryWindowWorkAdded:F1}");
            }
            host.ResetSimulation();
            ConnectionSpawnPlan replay = director.GenerateInfectorPlanForWitness(0f, structure,
                signature, InfectorSignature.None, InfectionKind.PhantomLoad);
            Check(plan.ConnectionId == replay.ConnectionId &&
                JsonUtility.ToJson(trace) == JsonUtility.ToJson(replay.GenerationTrace),
                "seed/reset replay preserves INFECTOR plan");
        }

        private static void CheckSubtleAndTiming(SimulationHost host,
            ConnectionTrafficDirector director)
        {
            host.ResetSimulation();
            var subtle = director.GenerateInfectorPlanForWitness(0f,
                InfectorStructure.Standard, InfectorSignature.None,
                InfectorSignature.None, InfectionKind.PhantomLoad);
            Check(subtle.GenerationTrace.generationDecision == "DELIVERY_SUBTLE" &&
                subtle.GenerationTrace.anomalies.Length == 0 &&
                subtle.GenerationTrace.infector.deliveryWindowWorkAdded == 0f &&
                subtle.GenerationTrace.normalOperations.Length == subtle.Operations.Length &&
                subtle.Operations.Select(o => o.DesiredLoad).SequenceEqual(
                    subtle.GenerationTrace.normalOperations.Select(o => o.desiredLoad)) &&
                subtle.Operations.Select(o => o.TotalWork).SequenceEqual(
                    subtle.GenerationTrace.normalOperations.Select(o => o.totalWork)),
                "subtle carrier has no structural or numerical deviation");
            bool correctedFastHighLoad = false;
            for (int seed = 100; seed < 130 && !correctedFastHighLoad; seed++)
            {
                director.Seed = seed; host.ResetSimulation();
                var high = director.GenerateInfectorPlanForWitness(0f,
                    InfectorStructure.Standard, InfectorSignature.HighLoad,
                    InfectorSignature.None, InfectionKind.PhantomLoad);
                float oldFastWindow = high.GenerationTrace.normalOperations[0].totalWork /
                    high.Operations[0].DesiredLoad;
                correctedFastHighLoad = oldFastWindow < 25f &&
                    high.GenerationTrace.infector.finalNominalDeliverySeconds >= 25f &&
                    high.Operations[0].TotalWork > high.GenerationTrace.normalOperations[0].totalWork;
            }
            Check(correctedFastHighLoad,
                "a previously too-fast high-LOAD carrier retains an intentional delivery window");
        }

        private static void CheckSecondaryAndWeights(SimulationHost host,
            ConnectionTrafficDirector director)
        {
            director.Seed = 1701; host.ResetSimulation();
            var doubleSignature = director.GenerateInfectorPlanForWitness(0f,
                InfectorStructure.Standard, InfectorSignature.OperationDeviation,
                InfectorSignature.WorkLong);
            Check(doubleSignature.GenerationTrace.anomalies.SequenceEqual(new[]
                { "operation-deviation", "work-deviation" }) &&
                doubleSignature.GenerationTrace.infector.secondarySignature == "work-deviation" &&
                doubleSignature.GenerationTrace.infector.finalNominalDeliverySeconds >= 25f,
                "compatible primary/secondary signatures record in order");
            var secondaryWave = director.GenerateInfectorPlanForWitness(0f,
                InfectorStructure.Standard, InfectorSignature.OperationDeviation,
                InfectorSignature.Waving);
            Check(secondaryWave.GenerationTrace.anomalies.SequenceEqual(new[]
                { "operation-deviation", "waving" }) &&
                secondaryWave.GenerationTrace.infector.secondarySignature == "waving" &&
                secondaryWave.GenerationTrace.selectedKeywords.Single(k =>
                    k.behaviorKind == "Waving").operationIndex < secondaryWave.Operations.Length,
                "secondary WAVING records its compatible operation and ordered signature");
            bool conflictRejected = false;
            try { director.GenerateInfectorPlanForWitness(1f,
                InfectorStructure.Standard, InfectorSignature.HighLoad,
                InfectorSignature.LowLoad); }
            catch (ArgumentException) { conflictRejected = true; }
            Check(conflictRejected, "opposing LOAD signatures cannot cancel each other");
            foreach (InfectorSignature conflict in new[] { InfectorSignature.HighLoad,
                InfectorSignature.LowLoad, InfectorSignature.Waving })
            {
                conflictRejected = false;
                try { director.GenerateInfectorPlanForWitness(1f,
                    InfectorStructure.Standard, InfectorSignature.Waving, conflict); }
                catch (ArgumentException) { conflictRejected = true; }
                Check(conflictRejected, "WAVING excludes conflicting or duplicate LOAD signatures");
            }
            int subtle = 0, delayed = 0, anomalous = 0, secondaryCount = 0;
            int[] count = new int[5];
            for (int seed = 200; seed < 220; seed++)
            {
                director.Seed = seed; host.ResetSimulation();
                for (int i = 0; i < 120; i++)
                {
                    var plan = director.GenerateInfectorPlanForWitness(i);
                    var decision = plan.GenerationTrace.infector;
                    if (decision.primarySignature == "none") { subtle++; continue; }
                    anomalous++;
                    if (decision.structure == "DELAYED") delayed++;
                    if (decision.secondarySignature != "none") secondaryCount++;
                    switch (decision.primarySignature)
                    {
                        case "high-load": count[0]++; break;
                        case "low-load": count[1]++; break;
                        case "operation-deviation": count[2]++; break;
                        case "work-deviation": count[3]++; break;
                        case "waving": count[4]++; break;
                        default: throw new InvalidOperationException("Unknown signature");
                    }
                }
            }
            Check(subtle > 160 && subtle < 320 && delayed > anomalous * 0.28f &&
                delayed < anomalous * 0.38f &&
                secondaryCount > anomalous * 0.11f &&
                secondaryCount < anomalous * 0.19f &&
                count.Sum() == anomalous &&
                count[0] > anomalous * 0.46f && count[0] < anomalous * 0.54f &&
                count[1] > anomalous * 0.07f && count[1] < anomalous * 0.13f &&
                count[2] > anomalous * 0.12f && count[2] < anomalous * 0.18f &&
                count[3] > anomalous * 0.07f && count[3] < anomalous * 0.13f &&
                count[4] > anomalous * 0.12f && count[4] < anomalous * 0.18f,
                "seeded 2400-plan sample follows 50/10/15/10/15 weighted primary signatures");
            Debug.Log($"InfectorWitness weighting: subtle={subtle}, delayed={delayed}/{anomalous}, " +
                $"secondary={secondaryCount}, " +
                $"high={count[0]}, low={count[1]}, operation={count[2]}, work={count[3]}, wave={count[4]}");
        }

        private static void CheckWavingSharedEvidence(SimulationHost host,
            ConnectionTrafficDirector director, FieldInfo overrides)
        {
            SelectPurpose(host, director, overrides, "product-lookup");
            int outliers = 0, waves = 0, ordinary = 0;
            var benignMaximums = new System.Collections.Generic.List<float>();
            var hostileMaximums = new System.Collections.Generic.List<float>();
            for (int seed = 300; seed < 325; seed++)
            {
                director.Seed = seed; host.ResetSimulation();
                for (int i = 0; i < 100; i++)
                {
                    ConnectionSpawnPlan plan = director.GenerateBenignPlan(i);
                    var trace = plan.GenerationTrace;
                    if (trace.generationDecision == "BENIGN_NORMAL")
                    {
                        ordinary++;
                        Check(trace.selectedKeywords.Length == 0 &&
                            plan.Operations.All(o => o.Behaviors.All(b =>
                                b.Kind != ConnectionBehaviorKind.Waving)),
                            "ordinary benign PURPOSE has only natural noise");
                        continue;
                    }
                    outliers++;
                    if (!trace.selectedKeywords.Any(k => k.behaviorKind == "Waving")) continue;
                    waves++;
                    Check(!plan.IsHostile && plan.NaturalLoadNoise &&
                        trace.anomalies.Length == 0 && trace.selectedKeywords.Length == 1 &&
                        plan.Operations.All(o => o.EffectOnCompletion == CompletionEffect.None),
                        "benign WAVING is one outlier keyword without infection");
                    RecordWave(plan, benignMaximums);
                }
            }
            for (int seed = 400; seed < 405; seed++)
            {
                director.Seed = seed; host.ResetSimulation();
                for (int i = 0; i < 20; i++)
                {
                    ConnectionSpawnPlan plan = director.GenerateInfectorPlanForWitness(i,
                        InfectorStructure.Standard, InfectorSignature.Waving,
                        InfectorSignature.None, InfectionKind.PhantomLoad);
                    Check(plan.NaturalLoadNoise && plan.GenerationTrace.selectedKeywords
                        .Any(k => k.behaviorKind == "Waving"),
                        "INFECTOR WAVING keeps natural noise and keyword provenance");
                    RecordWave(plan, hostileMaximums);
                }
            }
            Check(ordinary + outliers == 2500 && outliers > 160 && outliers < 240 &&
                waves > outliers * 0.23f && waves < outliers * 0.37f &&
                Mathf.Abs(benignMaximums.Average() - hostileMaximums.Average()) < 0.25f,
                "8% total benign outliers; about 30% WAVING, sampled like INFECTOR WAVING");
            Debug.Log($"InfectorWitness benign sample: outliers={outliers}/2500, " +
                $"waves={waves}/{outliers}, benignWaveMaxMean={benignMaximums.Average():F2}, " +
                $"infectorWaveMaxMean={hostileMaximums.Average():F2}");
        }

        private static void RecordWave(ConnectionSpawnPlan plan,
            System.Collections.Generic.List<float> maximums)
        {
            GeneratedKeywordSelection selection = plan.GenerationTrace.selectedKeywords
                .Single(k => k.behaviorKind == "Waving");
            OperationStage stage = plan.Operations[selection.operationIndex];
            TimedModifierBehaviorDefinition wave = stage.Behaviors.Single(b =>
                b.Kind == ConnectionBehaviorKind.Waving);
            float negativeBase = Mathf.Min(stage.DesiredLoad, 2f);
            Check(wave.MinimumLoadOffset <= -negativeBase * 0.4f + 0.001f &&
                wave.MinimumLoadOffset >= -negativeBase * 0.75f - 0.001f &&
                wave.MaximumLoadOffset >= 0.4f - 0.001f &&
                wave.MaximumLoadOffset <= Mathf.Max(0.4f,
                    Mathf.Min(1.5f, stage.DesiredLoad * 0.75f)) + 0.001f &&
                wave.WavePeriodSeconds >= 15f && wave.WavePeriodSeconds <= 30f &&
                wave.StartAfterSeconds >= 2f && wave.StartAfterSeconds <= 5f,
                "shared waveform sampler respects base-relative bounds, period, and onset");
            maximums.Add(wave.MaximumLoadOffset);
        }

        private static void CheckWavingInterruption(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            director.Seed = 1701; host.ResetSimulation(); recorder.BeginRun();
            ConnectionSpawnPlan killed = director.GenerateInfectorPlanForWitness(0f,
                InfectorStructure.Standard, InfectorSignature.Waving,
                InfectorSignature.None, InfectionKind.PhantomLoad);
            director.QueueSpawnPlan(killed);
            for (int i = 0; i < 35; i++) host.StepSimulation(0.2f);
            Check(host.FindConnection(killed.ConnectionId).Behaviors.ActiveModifiers
                .Any(m => m.SourceId == "WAVING") && host.KillConnection(killed.ConnectionId),
                "active WAVING INFECTOR can be killed");
            host.StepSimulation(200f);
            Check(host.Infections.Count == 0 &&
                !recorder.CurrentLog.events.Any(e => e.connectionId == killed.ConnectionId &&
                    e.type == "infection_applied"),
                "killing WAVING INFECTOR prevents infection");

            host.ResetSimulation(); recorder.BeginRun();
            ConnectionSpawnPlan canceled = director.GenerateInfectorPlanForWitness(0f,
                InfectorStructure.Standard, InfectorSignature.Waving,
                InfectorSignature.None, InfectionKind.PhantomLoad);
            director.QueueSpawnPlan(canceled);
            for (int i = 0; i < 35; i++) host.StepSimulation(0.2f);
            Check(host.TryInjectInfection(canceled.Target.NodeId, InfectionKind.Blackout, 0f,
                out _), "WAVING cancellation setup");
            host.StepSimulation(0.2f);
            Check(host.FindConnection(canceled.ConnectionId).Resolution ==
                ConnectionResolution.Canceled &&
                !recorder.CurrentLog.events.Any(e => e.connectionId == canceled.ConnectionId &&
                    e.type == "infection_applied"),
                "Blackout cancellation prevents WAVING delivery");
        }

        private static void CheckWavingCorrection(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder,
            FieldInfo overrides)
        {
            SelectPurpose(host, director, overrides, "order-submit");
            ConnectionSpawnPlan corrected = null;
            for (int seed = 500; seed < 540 && corrected == null; seed++)
            {
                director.Seed = seed; host.ResetSimulation();
                for (int i = 0; i < 100; i++)
                {
                    ConnectionSpawnPlan plan = director.GenerateInfectorPlanForWitness(0f,
                        InfectorStructure.Standard, InfectorSignature.Waving,
                        InfectorSignature.None, InfectionKind.PhantomLoad);
                    if (plan.GenerationTrace.infector.deliveryWindowWorkAdded > 0f &&
                        plan.GenerationTrace.selectedKeywords.Single().operationIndex ==
                            plan.Operations.Length - 1)
                    { corrected = plan; break; }
                }
            }
            Check(corrected != null, "seeded WAVING carrier needs a final-stage WORK correction");
            var decision = corrected.GenerationTrace.infector;
            int last = corrected.Operations.Length - 1;
            float onset = corrected.Operations[last].Behaviors.Single(b =>
                b.Kind == ConnectionBehaviorKind.Waving).StartAfterSeconds;
            Check(decision.deliveryWindowCorrectionReason ==
                    "waving_conservative_delivery_window" &&
                decision.finalConservativeDeliverySeconds >= 25f - 0.001f &&
                decision.finalOperationConservativeSeconds >= onset + 18f - 0.001f &&
                corrected.Operations[0].TotalWork ==
                    corrected.GenerationTrace.normalOperations[0].totalWork &&
                corrected.Operations[last].TotalWork >
                    corrected.GenerationTrace.normalOperations[last].totalWork,
                "minimal correction changes only final WORK and records conservative window");
            recorder.BeginRun(); director.QueueSpawnPlan(corrected);
            LogicalConnectionState completed = WaitForResolution(host, corrected.ConnectionId);
            RunEvent resolved = recorder.CurrentLog.events.Single(e =>
                e.type == "connection_resolved" && e.connectionId == corrected.ConnectionId);
            float actualFinal = recorder.CurrentLog.events.Single(e =>
                e.type == "operation_completed" && e.connectionId == corrected.ConnectionId &&
                e.operationIndex == last).actualDurationSeconds;
            Check(completed.Resolution == ConnectionResolution.Completed &&
                resolved.resolvedAtSeconds - resolved.establishedAtSeconds >= 24.8f &&
                actualFinal >= onset + 17.8f && host.Infections.Count == 1,
                "corrected WAVING carrier completes with actual post-onset evidence time");
            Debug.Log($"InfectorWitness corrected WAVING: added={decision.deliveryWindowWorkAdded:F1} " +
                $"WORK, actual delivery={resolved.resolvedAtSeconds - resolved.establishedAtSeconds:F1}s, " +
                $"final={actualFinal:F1}s, onset={onset:F1}s");
        }

        private static void CheckLifecycle(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            director.Seed = 1701;
            host.ResetSimulation(); recorder.BeginRun();
            ConnectionSpawnPlan killed = director.GenerateInfectorPlanForWitness(0f,
                InfectorStructure.Standard, InfectorSignature.HighLoad,
                InfectorSignature.None, InfectionKind.PhantomLoad);
            director.QueueSpawnPlan(killed);
            host.StepSimulation(0.1f);
            Check(host.KillConnection(killed.ConnectionId), "pre-delivery kill succeeds");
            host.StepSimulation(200f);
            Check(host.Infections.Count == 0 &&
                !recorder.CurrentLog.events.Any(e => e.connectionId == killed.ConnectionId &&
                    e.type == "infection_applied"), "kill prevents payload");

            host.ResetSimulation(); recorder.BeginRun();
            ConnectionSpawnPlan canceled = director.GenerateInfectorPlanForWitness(0f,
                InfectorStructure.Standard, InfectorSignature.LowLoad,
                InfectorSignature.None, InfectionKind.PhantomLoad);
            director.QueueSpawnPlan(canceled);
            host.StepSimulation(0.1f);
            Check(host.TryInjectInfection(canceled.Target.NodeId, InfectionKind.Blackout, 0f,
                out _), "Blackout interruption setup");
            host.StepSimulation(200f);
            Check(host.FindConnection(canceled.ConnectionId).Resolution ==
                ConnectionResolution.Canceled &&
                !recorder.CurrentLog.events.Any(e => e.connectionId == canceled.ConnectionId &&
                    e.type == "effect"), "Blackout cancels pending delivery");

            host.ResetSimulation(); recorder.BeginRun();
            ConnectionSpawnPlan delivered = director.GenerateInfectorPlanForWitness(0f,
                InfectorStructure.Delayed, InfectorSignature.WorkLong,
                InfectorSignature.None, InfectionKind.PhantomLoad);
            director.QueueSpawnPlan(delivered);
            LogicalConnectionState state = WaitForResolution(host, delivered.ConnectionId);
            Check(state.Resolution == ConnectionResolution.Completed &&
                host.Infections.Count == 1 &&
                host.Infections.Single().SourceConnectionId == delivered.ConnectionId &&
                host.Occurrences.Count(o => o.ConnectionId == delivered.ConnectionId) == 1 &&
                recorder.CurrentLog.events.Count(e => e.connectionId == delivered.ConnectionId &&
                    e.type == "infection_applied") == 1 &&
                !host.KillConnection(delivered.ConnectionId),
                "normal completion installs exactly once and survives carrier retirement");
            Check(recorder.CurrentLog.events.Single(e => e.type == "operation_completed" &&
                e.connectionId == delivered.ConnectionId &&
                e.operationIndex == delivered.Operations.Length - 1).actualDurationSeconds > 0f,
                "recorder retains actual delivery-operation duration");
        }

        private static void CheckManualAndSpawner(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            host.ResetSimulation(); recorder.BeginRun();
            var manual = director.QueueProfile("product-lookup", new ConnectionSpawnOverrides(
                ManualObjective.Delivery, InfectionKind.PhantomLoad, 8f));
            Check(manual.GenerationTrace.forcedOptions.hasLoadParameter &&
                manual.GenerationTrace.infector != null &&
                manual.Operations.Last().InfectionPayload.RequestedLoad == 8f,
                "manual forced parameter survives composition");
            WaitForResolution(host, manual.ConnectionId);
            Check(host.Infections.Single().Payload.RequestedLoad == 8f,
                "manual forced parameter installs through completion");

            host.ResetSimulation(); recorder.BeginRun();
            Check(host.TryInjectInfection("DB", InfectionKind.Spawner, 2f, out _),
                "Spawner setup");
            host.StepSimulation(2f);
            RunEvent child = recorder.CurrentLog.events.Single(e =>
                e.type == "plan_queued" && e.source == "spawner");
            Check(child.parentNodeId == "DB" && child.generation.infector != null &&
                child.definition.operations.Last().completionEffect == "InfectionDelivery" &&
                child.definition.operations.Last().infectionKind != null &&
                child.generation.infector.finalNominalDeliverySeconds >= 25f - 0.001f,
                "Spawner child uses timed INFECTOR carrier with parent provenance");
        }

        private static void CheckAmbiguousOutlier(SimulationHost host,
            ConnectionTrafficDirector director, FieldInfo overrides)
        {
            SelectPurpose(host, director, overrides, "order-submit");
            ConnectionSpawnPlan benign = null;
            for (int seed = 10; seed < 80 && benign == null; seed++)
            {
                director.Seed = seed; host.ResetSimulation();
                for (int i = 0; i < 150; i++)
                {
                    var plan = director.GenerateBenignPlan(i);
                    if (plan.GenerationTrace.generationDecision == "BENIGN_OUTLIER" &&
                        plan.Operations.Length == 3)
                    { benign = plan; break; }
                }
            }
            Check(benign != null && !benign.IsHostile, "constructed benign three-operation outlier");
            string visible = string.Join(",", benign.Operations.Select(o => o.Operation.ToString()));
            ConnectionSpawnPlan hostile = null;
            for (int seed = 10; seed < 80 && hostile == null; seed++)
            {
                director.Seed = seed; host.ResetSimulation();
                for (int i = 0; i < 100; i++)
                {
                    var plan = director.GenerateInfectorPlanForWitness(i,
                        InfectorStructure.Standard, InfectorSignature.OperationDeviation,
                        InfectorSignature.None);
                    if (plan.Operations.Length == 3 &&
                        string.Join(",", plan.Operations.Select(o => o.Operation.ToString())) == visible)
                    { hostile = plan; break; }
                }
            }
            Check(hostile != null && hostile.IsHostile &&
                benign.ClaimedPurpose == hostile.ClaimedPurpose &&
                benign.Target == hostile.Target &&
                benign.Operations.Length == hostile.Operations.Length &&
                benign.Operations.Select(o => o.ActivityDescription).SequenceEqual(
                    hostile.Operations.Select(o => o.ActivityDescription)) &&
                benign.Operations.All(o => o.EffectOnCompletion == CompletionEffect.None) &&
                hostile.Operations.Last().EffectOnCompletion == CompletionEffect.InfectionDelivery,
                "benign outlier and INFECTOR share PURPOSE and visible three-operation kinds");
        }

        private static LogicalConnectionState WaitForResolution(SimulationHost host, string id)
        {
            for (int i = 0; i < 2400; i++)
            {
                host.StepSimulation(0.5f);
                LogicalConnectionState state = host.FindConnection(id);
                if (state?.Resolution != null) return state;
            }
            throw new InvalidOperationException("INFECTOR did not resolve: " + id);
        }

        private static void SelectPurpose(SimulationHost host,
            ConnectionTrafficDirector director, FieldInfo field, string purpose)
        {
            field.SetValue(director, director.Profiles.Where(p => p.purposeTemplate)
                .Select(p => new AmbientWeightOverride
                { profileId = p.id, weight = p.id == purpose ? 1f : 0f }).ToArray());
            host.ResetSimulation();
        }

        private static FieldInfo Field(Type type, string name) => type.GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Missing field " + name);
        private static bool Approx(float a, float b) => Mathf.Abs(a - b) < 0.001f;
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("InfectorWitness: " + message);
        }
    }
}
