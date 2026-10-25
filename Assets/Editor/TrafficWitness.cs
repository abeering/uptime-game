using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class TrafficWitness
    {
        [MenuItem("Tools/Connections/Run Traffic Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            Check(host != null && director != null, "scene traffic bindings");
            bool originalAuto = director.AutoGenerationEnabled;
            int originalSeed = director.Seed;
            FieldInfo travelField = typeof(SimulationHost).GetField(
                "connectionTravelSecondsPerLink", BindingFlags.Instance | BindingFlags.NonPublic);
            float originalTravel = (float)travelField.GetValue(host);
            // Preserve this generator/operation witness's old service-time examples;
            // LifecycleWitness checks ordinary Connecting travel separately.
            travelField.SetValue(host, 0.000001f);
            FieldInfo overridesField = typeof(ConnectionTrafficDirector).GetField(
                "ambientWeightOverrides", BindingFlags.Instance | BindingFlags.NonPublic);
            var originalOverrides = (AmbientWeightOverride[])overridesField.GetValue(director);
            try
            {
                director.AutoGenerationEnabled = false;
                director.Seed = 456;
                host.SetRunMode(ConnectionRunMode.Generated);
                Check(host.RuntimeConnections.Count == 0, "generated mode excludes authored witness");
                var first = Enumerable.Range(0, 200).Select(i => director.GeneratePlan(i * 13f)).ToArray();
                director.ResetDirector();
                var replay = Enumerable.Range(0, 200).Select(i => director.GeneratePlan(i * 13f)).ToArray();
                for (int i = 0; i < first.Length; i++)
                    Check(Signature(first[i]) == Signature(replay[i]), "seeded plan replay " + i);
                CheckGeneratedIdentifiers(host, director);
                foreach (ConnectionSpawnPlan plan in replay)
                {
                    Check(plan.Route.Length >= 2 && plan.Origin.NodeId == "EXT1" &&
                        plan.Target == plan.Route[plan.Route.Length - 1], "generated valid route");
                    ConnectionTrafficProfile profile = director.Profiles.First(p => p.id == plan.ProfileId);
                    Check(profile.purposeTemplate && plan.GenerationTrace != null &&
                        plan.GenerationTrace.purposeId == profile.id &&
                        plan.Target.NodeId == profile.eligibleTargetIds[0],
                        "ambient plan uses purpose target and records generation decision");
                    Check(plan.GenerationTrace.normalOperations.Length == profile.operations.Length,
                        "sampled normal operation count");
                    for (int stageIndex = 0; stageIndex < profile.operations.Length; stageIndex++)
                    {
                        GeneratedOperationValue stage = plan.GenerationTrace.normalOperations[stageIndex];
                        ConnectionOperationProfile spec = profile.operations[stageIndex];
                        Check(stage.kind == spec.operation.ToString() &&
                            stage.desiredLoad >= spec.minimumLoad && stage.desiredLoad <= spec.maximumLoad &&
                            stage.totalWork >= spec.meanWork * 0.75f - 0.001f &&
                            stage.totalWork <= spec.meanWork * 1.25f + 0.001f,
                            "normal LOAD and independent WORK within purpose ranges");
                    }
                    Check(plan.GenerationTrace.finalOperations.Length == plan.Operations.Length &&
                        plan.GenerationTrace.finalOperations.Select(o => o.desiredLoad)
                            .SequenceEqual(plan.Operations.Select(o => o.DesiredLoad)) &&
                        (plan.IsHostile == (plan.GenerationTrace.objective != "NONE")),
                        "trace matches concrete plan and internal objective");
                    if (plan.GenerationTrace.objective == "LOADER")
                        Check(plan.GenerationTrace.anomalies.Contains("high-load") &&
                            plan.Operations.All(o => o.EffectOnCompletion == CompletionEffect.None),
                            "loader raises demand without delivery");
                    if (plan.GenerationTrace.objective == "DELIVERY")
                        Check(plan.Operations.Last().EffectOnCompletion == CompletionEffect.InfectionDelivery &&
                            plan.Operations.Take(plan.Operations.Length - 1)
                                .All(o => o.EffectOnCompletion == CompletionEffect.None),
                            "delivery effect belongs to final operation");
                    if (!plan.IsHostile)
                        Check(plan.Operations.All(o => o.EffectOnCompletion == CompletionEffect.None),
                            "benign anomalies and keywords have no delivery");
                }
                Check(director.Profiles.Count == 16 &&
                    replay.Select(p => p.ProfileId).Distinct().Count() == 6 &&
                    replay.Any(p => p.GenerationTrace.objective == "LOADER") &&
                    replay.Any(p => p.GenerationTrace.objective == "DELIVERY") &&
                    replay.Any(p => !p.IsHostile && p.GenerationTrace.anomalies.Length > 0) &&
                    replay.Any(p => p.IsHostile && p.GenerationTrace.anomalies.Length == 0) &&
                    replay.Any(p => p.ProfileId == "sales-export" && !p.IsHostile),
                    "six purpose mix includes both objectives, benign outliers, subtle delivery, and sales export");
                CheckAnomalyIndependence(replay);
                CheckGenerationDecisions(replay);
                CheckQueuedPlanRecording(host, director, replay);
                float total = director.Profiles.Sum(p => director.EffectiveAmbientWeight(p.id));
                Near(total, 5.25f, "default total purpose weight");
                foreach (var expected in new[] { ("product-lookup", 1.5f),
                    ("customer-login", 1f), ("cart-pricing", 1f), ("order-submit", 0.9f),
                    ("inventory-sync", 0.6f), ("sales-export", 0.25f) })
                    Near(director.EffectiveAmbientWeight(expected.Item1), expected.Item2,
                        "default weight " + expected.Item1);
                Check(director.Profiles.Where(p => !p.purposeTemplate)
                    .All(p => director.EffectiveAmbientWeight(p.id) == 0f),
                    "legacy witness profiles are manual by default");
                ConnectionTrafficProfile spikingThreat = director.Profiles.Single(p =>
                    p.id == "spiking-threat");
                ConnectionTrafficProfile spikingBenign = director.Profiles.Single(p =>
                    p.id == "spiking-benign");
                Check(spikingThreat.isHostile && !spikingBenign.isHostile &&
                    spikingThreat.claimedPurpose == spikingBenign.claimedPurpose &&
                    spikingThreat.operations[0].operation == spikingBenign.operations[0].operation &&
                    spikingThreat.operations[0].minimumLoad == spikingBenign.operations[0].minimumLoad &&
                    spikingThreat.operations[0].maximumLoad == spikingBenign.operations[0].maximumLoad &&
                    spikingThreat.operations[0].minimumDuration == spikingBenign.operations[0].minimumDuration &&
                    spikingThreat.operations[0].behaviors[0].LoadDelta ==
                        spikingBenign.operations[0].behaviors[0].LoadDelta,
                    "hostile and benign SPIKING have equivalent visible demand definitions");
                CheckWeightOverrides(host, director, overridesField);
                CheckCustomProfiles(host, director, overridesField);
                CheckAmbientScheduleReplay(host, director);
                ConnectionTrafficProfile quick = director.Profiles.First(p => p.id == "quick-lookup");
                Check(quick.operations.Length == 1 && quick.operations[0].operation == OperationKind.Read &&
                    quick.operations[0].minimumDuration == 4f && quick.operations[0].maximumDuration == 8f,
                    "background profile duration and operation");
                ConnectionTrafficProfile indexing = director.Profiles.First(p => p.id == "indexing");
                Check(indexing.operations.Length == 2 &&
                    indexing.operations.Sum(o => o.maximumDuration) <= 75f,
                    "multi-stage lifetime is a total, not per-stage range");
                Debug.Log("TrafficWitness seeded 200-plan replay: expected identical; observed identical. " +
                    "Six purpose templates generate independent WORK and LOAD; legacy profiles remain manual.");

                host.ResetSimulation();
                NodeView ext = Node(host, "EXT1");
                NodeView api = Node(host, "API");
                NodeView db = Node(host, "DB");
                var route = new[] { ext, api, db };
                var fast = new ConnectionSpawnPlan(0f, "w1", route, "quick-lookup", "Quick Record Lookup",
                    new[] { new OperationStage(OperationKind.Read, 1f, 1f, "Lookup") });
                var arc = new ConnectionSpawnPlan(0f, "w2", route, "indexing", "Record Indexing",
                    new[] { new OperationStage(OperationKind.Write, 1f, 1f, "Write"),
                        new OperationStage(OperationKind.Read, 2f, 2f, "Read") });
                director.QueueSpawnPlan(fast);
                director.QueueSpawnPlan(arc);
                bool duplicateRejected = false;
                try { director.QueueSpawnPlan(fast); }
                catch (InvalidOperationException) { duplicateRejected = true; }
                Check(duplicateRejected, "queued duplicate ID rejected");
                bool routeRejected = false;
                try { director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "bad", new[] { ext, db },
                    "test", "Bad route", fast.Operations)); }
                catch (InvalidOperationException) { routeRejected = true; }
                Check(routeRejected, "invalid route rejected");
                host.StepSimulation(0.5f);
                var w1 = host.FindConnection("w1");
                var w2 = host.FindConnection("w2");
                Check(host.RuntimeConnections.Count == 2 && w1.IsActive && w2.IsActive,
                    "queued plans arrive without simulation reset");
                Near(host.SimulationTimeSeconds, 0.5f, "queued arrival step time");
                Near(w1.AllocatedLoad, 1f, "uncontended quick LOAD");
                Near(w2.AllocatedLoad, 1f, "uncontended arc LOAD");
                host.StepSimulation(0.5f);
                Check(w1.IsComplete && w1.Resolution == ConnectionResolution.Completed &&
                    host.RuntimeConnections.Count == 1, "quick READ completes and retires");
                Check(w2.CurrentOperationIndex == 1 && w2.IsTransitioning && w2.IsActive,
                    "arc transitions without identity reset");
                host.StepSimulation(0.5f);
                Near(w2.AllocatedLoad, 2f, "second-stage LOAD");
                host.StepSimulation(0.5f);
                Check(w2.IsComplete && w2.Resolution == ConnectionResolution.Completed &&
                    host.RuntimeConnections.Count == 0 && host.FindConnection("w2") == w2,
                    "arc completes and remains inspectable in history");
                Debug.Log("TrafficWitness operations: expected quick completion at 1s, arc transition at 1s, " +
                    "final completion at 2s; observed matching states and retired history.");

                var longStage = new[] { new OperationStage(OperationKind.Read, 100f, 1f, "Scheduled") };
                director.QueueSpawnPlan(new ConnectionSpawnPlan(3f, "later1", route,
                    "test", "Scheduled", longStage));
                director.QueueSpawnPlan(new ConnectionSpawnPlan(3f, "later2", route,
                    "test", "Scheduled", longStage));
                host.StepSimulation(0.9f);
                host.StepSimulation(0.1f);
                Check(host.RuntimeConnections.Count == 0, "future plans stay queued");
                host.StepSimulation(0.1f);
                Check(host.RuntimeConnections.Count == 2 &&
                    host.RuntimeConnections[0].ConnectionId == "later1" &&
                    host.RuntimeConnections[1].ConnectionId == "later2",
                    "same-time plans dispatch in insertion order");
                Check(host.KillConnection("later1") && host.KillConnection("later2"),
                    "scheduled plans can be killed");

                // Event-style burst with ambient disabled. There is no population cap.
                for (int i = 0; i < 12; i++)
                    director.QueueSpawnPlan(new ConnectionSpawnPlan(host.SimulationTimeSeconds,
                        "burst" + i, route, "high-demand-witness", "High Demand Test",
                        new[] { new OperationStage(OperationKind.Read, 500f, 10f, "Test") }));
                host.StepSimulation(0.1f);
                Check(host.RuntimeConnections.Count == 12, "all simultaneous event plans dispatched");
                NodeResources dbResources = host.Nodes.First(n => n.View == db);
                Near(dbResources.RequestedLoad, 120f, "burst requested LOAD");
                Near(dbResources.AllocatedLoad, 10f, "burst allocated LOAD");
                foreach (LogicalConnectionState state in host.RuntimeConnections.ToArray())
                    Check(host.KillConnection(state.ConnectionId), "kill active connection");
                Check(host.RuntimeConnections.Count == 0 &&
                    host.FindConnection("burst0").Resolution == ConnectionResolution.Killed,
                    "killed connections retire with reason");
                Near(dbResources.RequestedLoad, dbResources.BaselineLoad, "killed LOAD released");
                Debug.Log("TrafficWitness burst: expected 12 arrivals, DB 120 requested/10 allocated; " +
                    $"observed 12 and {dbResources.RequestedLoad:0.#} after kill release.");
                CheckManualProfilesAndThreats(host, director);
                CheckDelayedThreat(host, director);
                CheckGeneratedPurposeRuntime(host, director);
                Debug.Log("TrafficWitness PASS");
            }
            finally
            {
                director.Seed = originalSeed;
                director.AutoGenerationEnabled = originalAuto;
                travelField.SetValue(host, originalTravel);
                overridesField.SetValue(director, originalOverrides);
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static void CheckManualProfilesAndThreats(SimulationHost host,
            ConnectionTrafficDirector director)
        {
            var observed = new List<ConnectionEffectOccurrence>();
            Action<ConnectionEffectOccurrence> onEffect = observed.Add;
            host.EffectOccurred += onEffect;
            try
            {
                director.AutoGenerationEnabled = false;
                host.ResetSimulation();
                bool unknownRejected = false;
                try { director.QueueProfile("missing-profile"); }
                catch (InvalidOperationException error)
                { unknownRejected = error.Message.Contains("Unknown traffic profile"); }
                Check(unknownRejected, "unknown manual profile reports a clear error");
                ConnectionSpawnPlan benign = director.QueueProfile("quick-lookup");
                ConnectionSpawnPlan threat = director.QueueProfile("basic-threat");
                Check(host.FindConnection(benign.ConnectionId) == null &&
                    host.FindConnection(threat.ConnectionId) == null,
                    "manual plans queue before simulation dispatch");
                Check(!benign.IsHostile && threat.IsHostile &&
                    threat.Operations.Length == 1 &&
                    threat.Operations[0].Operation == OperationKind.Write &&
                    threat.Operations[0].DesiredLoad == 12f &&
                    threat.Operations[0].TotalWork == 240f &&
                    threat.Operations[0].EffectOnCompletion == CompletionEffect.InfectionDelivery,
                    "manual profiles build concrete benign and fixed high-LOAD threat plans");
                host.StepSimulation(0.1f);
                LogicalConnectionState benignState = host.FindConnection(benign.ConnectionId);
                LogicalConnectionState threatState = host.FindConnection(threat.ConnectionId);
                Check(benignState.IsActive && threatState.IsActive &&
                    benignState.AllocatedLoad < benignState.DesiredLoad &&
                    threatState.AllocatedLoad < threatState.DesiredLoad,
                    "manual threat contends with benign traffic through proportional LOAD");
                NodeResources db = host.Nodes.First(n => n.View.NodeId == "DB");
                Near(db.RequestedLoad, benignState.DesiredLoad + threatState.DesiredLoad,
                    "manual demand appears in target LOAD");
                Check(host.KillConnection(threat.ConnectionId) &&
                    threatState.Resolution == ConnectionResolution.Killed &&
                    host.Occurrences.Count == 0 && observed.Count == 0,
                    "kill before WORK completion prevents delivery");
                Near(db.RequestedLoad, benignState.DesiredLoad,
                    "kill releases requested LOAD immediately");
                host.StepSimulation(0.1f);
                Near(benignState.AllocatedLoad, benignState.DesiredLoad,
                    "benign allocation recovers on next step");
                int steps = 0;
                while (!benignState.IsComplete && steps++ < 100)
                    host.StepSimulation(0.1f);
                Check(benignState.Resolution == ConnectionResolution.Completed &&
                    host.Occurrences.Count == 0,
                    "manually spawned benign operation completes without an effect");

                host.ResetSimulation();
                Check(host.Occurrences.Count == 0, "reset clears effect history");
                ConnectionSpawnPlan first = director.QueueProfile("basic-threat");
                ConnectionSpawnPlan second = director.QueueProfile("basic-threat");
                host.StepSimulation(0.1f);
                LogicalConnectionState firstState = host.FindConnection(first.ConnectionId);
                LogicalConnectionState secondState = host.FindConnection(second.ConnectionId);
                Check(firstState != secondState && firstState.IsHostile && secondState.IsHostile &&
                    firstState.CompletedWork > 0f && secondState.CompletedWork > 0f,
                    "concurrent instances have separate runtime state");
                steps = 0;
                while ((!firstState.IsComplete || !secondState.IsComplete) && steps++ < 600)
                    host.StepSimulation(0.1f);
                Check(firstState.Resolution == ConnectionResolution.Completed &&
                    secondState.Resolution == ConnectionResolution.Completed &&
                    host.Occurrences.Count == 2 && observed.Count == 2 &&
                    host.Occurrences.Select(e => e.ConnectionId).Distinct().Count() == 2 &&
                    host.Occurrences.All(e => e.Type == CompletionEffect.InfectionDelivery &&
                        e.OperationIndex == 0 && e.TargetNodeId == "DB" &&
                        e.TimeSeconds > 0f),
                    "each completed threat delivers exactly one independent occurrence");
                host.StepSimulation(0.1f);
                Check(!host.KillConnection(first.ConnectionId) && host.Occurrences.Count == 2,
                    "killing after completion cannot erase delivered effects");

                host.ResetSimulation();
                Check(host.Occurrences.Count == 0, "replay starts without stale effects");
                ConnectionSpawnPlan replay = director.QueueProfile("basic-threat");
                Check(replay.ConnectionId == first.ConnectionId,
                    "manual first spawn ID replays from seed");
                host.StepSimulation(24f);
                Check(host.Occurrences.Count == 1 &&
                    host.Occurrences[0].ConnectionId == replay.ConnectionId,
                    "WORK completed inside a step emits once at the completed step time");
                Near(host.Occurrences[0].TimeSeconds, 24f, "completed step time");

                host.ResetSimulation();
                ConnectionSpawnPlan arc = director.QueueProfile("indexing");
                host.StepSimulation(0.5f);
                LogicalConnectionState arcState = host.FindConnection(arc.ConnectionId);
                steps = 0;
                while (arcState.CurrentOperationIndex == 0 && steps++ < 100)
                    host.StepSimulation(0.5f);
                Check(arcState.IsActive && arcState.IsTransitioning &&
                    arcState.CurrentOperationIndex == 1 && arcState.ConnectionId == arc.ConnectionId &&
                    host.Occurrences.Count == 0,
                    "manual benign arc uses normal next-step operation transition");
                host.StepSimulation(0.5f);
                Check(arcState.CompletedWork > 0f && !arcState.IsTransitioning,
                    "manual benign second operation starts through normal allocator");
                steps = 0;
                while (!arcState.IsComplete && steps++ < 100)
                    host.StepSimulation(0.5f);
                Check(arcState.Resolution == ConnectionResolution.Completed &&
                    host.Occurrences.Count == 0,
                    "manual multi-operation benign profile completes without delivery");

                Debug.Log("TrafficWitness manual profiles: expected queued dispatch, contention, " +
                    "kill prevention and release, two independent deliveries, replay, and benign arc; " +
                    "observed matching states and occurrences.");
            }
            finally
            {
                host.EffectOccurred -= onEffect;
                host.ResetSimulation();
            }
        }

        private static void CheckDelayedThreat(SimulationHost host,
            ConnectionTrafficDirector director)
        {
            host.ResetSimulation();
            ConnectionTrafficProfile profile = director.Profiles.Single(p => p.id == "delayed-threat");
            ConnectionTrafficProfile basic = director.Profiles.Single(p => p.id == "basic-threat");
            ConnectionSpawnPlan plan = director.QueueProfile("delayed-threat");
            Check(profile.isHostile && director.EffectiveAmbientWeight(profile.id) == 0f &&
                plan.IsHostile && plan.Operations.Length == 2 &&
                plan.Operations[0].Operation == OperationKind.Read &&
                plan.Operations[0].DesiredLoad == 2f && plan.Operations[0].TotalWork == 16f &&
                plan.Operations[0].EffectOnCompletion == CompletionEffect.None &&
                plan.Operations[1].Operation == OperationKind.Write &&
                plan.Operations[1].DesiredLoad == 12f && plan.Operations[1].TotalWork == 240f &&
                plan.Operations[1].EffectOnCompletion == CompletionEffect.InfectionDelivery &&
                basic.operations[0].minimumLoad == 12f &&
                basic.operations[0].completionEffect == CompletionEffect.InfectionDelivery &&
                plan.Route.Select(n => n.NodeId).SequenceEqual(new[] { "EXT1", "API", "DB" }),
                "manual delayed threat has ordinary READ and unchanged high-LOAD WRITE");

            host.StepSimulation(0.1f);
            LogicalConnectionState state = host.FindConnection(plan.ConnectionId);
            Near(state.AllocatedLoad, 2f, "READ receives ordinary LOAD");
            Near(state.CompletedWork, 0.2f, "READ advances ordinary WORK");
            host.StepSimulation(7.9f);
            Check(state.IsActive && state.IsTransitioning &&
                state.CurrentOperationIndex == 1 && state.CurrentOperation == OperationKind.Write &&
                state.CompletedWork == 0f && state.DesiredLoad == 12f &&
                host.FindConnection(plan.ConnectionId) == state &&
                state.Definition.Route.Select(n => n.Node.NodeId)
                    .SequenceEqual(plan.Route.Select(n => n.NodeId)) &&
                host.Occurrences.Count == 0,
                "READ completes without delivery and keeps identity and route for WRITE");
            NodeResources db = host.Nodes.Single(n => n.View.NodeId == "DB");
            Near(db.RequestedLoad, 0f, "next operation waits until the following step");
            host.StepSimulation(0.1f);
            Check(!state.IsTransitioning && state.CurrentOperationIndex == 1 &&
                state.CompletedWork > 0f && state.ActualWorkRate == 10f,
                "WRITE begins on next step with normal capacity allocation");
            Near(db.RequestedLoad, 12f, "WRITE requests high LOAD");
            host.StepSimulation(24f);
            Check(state.Resolution == ConnectionResolution.Completed &&
                host.Occurrences.Count == 1 &&
                host.Occurrences[0].ConnectionId == plan.ConnectionId &&
                host.Occurrences[0].OperationIndex == 1 &&
                host.Occurrences[0].Type == CompletionEffect.InfectionDelivery &&
                host.Occurrences[0].TargetNodeId == "DB" &&
                host.FindConnection(plan.ConnectionId) == state,
                "normal WRITE completion delivers once and retains inspection history");
            Near(host.Occurrences[0].TimeSeconds, host.SimulationTimeSeconds,
                "delivery uses WRITE completion time");
            host.StepSimulation(0.1f);
            Check(host.Occurrences.Count == 1, "completed WRITE does not deliver twice");

            host.ResetSimulation();
            ConnectionSpawnPlan readKill = director.QueueProfile("delayed-threat");
            host.StepSimulation(0.1f);
            LogicalConnectionState readState = host.FindConnection(readKill.ConnectionId);
            float readWork = readState.CompletedWork;
            Check(host.KillConnection(readKill.ConnectionId) &&
                readState.Resolution == ConnectionResolution.Killed &&
                readState.CurrentOperationIndex == 0 && readState.CompletedWork == readWork &&
                host.Occurrences.Count == 0,
                "kill during READ retains partial READ and prevents later operations");
            host.StepSimulation(40f);
            Check(host.Occurrences.Count == 0 && readState.CurrentOperationIndex == 0,
                "killed READ never starts WRITE");

            host.ResetSimulation();
            ConnectionSpawnPlan writeKill = director.QueueProfile("delayed-threat");
            host.StepSimulation(8f);
            LogicalConnectionState writeState = host.FindConnection(writeKill.ConnectionId);
            host.StepSimulation(0.1f);
            float writeWork = writeState.CompletedWork;
            Check(writeState.CurrentOperationIndex == 1 && writeWork > 0f &&
                host.KillConnection(writeKill.ConnectionId) &&
                writeState.Resolution == ConnectionResolution.Killed &&
                writeState.CompletedWork == writeWork && host.Occurrences.Count == 0,
                "kill during WRITE retains completed READ and partial WRITE without delivery");
            Near(db.RequestedLoad, 0f, "kill during WRITE releases requested LOAD");
            host.StepSimulation(40f);
            Check(host.Occurrences.Count == 0, "killed WRITE never delivers");

            host.ResetSimulation();
            ConnectionSpawnPlan first = director.QueueProfile("delayed-threat");
            ConnectionSpawnPlan second = director.QueueProfile("delayed-threat");
            host.StepSimulation(8f);
            Check(host.Occurrences.Count == 0 &&
                host.FindConnection(first.ConnectionId).CurrentOperationIndex == 1 &&
                host.FindConnection(second.ConnectionId).CurrentOperationIndex == 1,
                "concurrent READ completions do not share effects");
            host.StepSimulation(0.1f);
            Near(db.RequestedLoad, 24f, "concurrent WRITEs share high demand");
            host.StepSimulation(48f);
            Check(host.Occurrences.Count == 2 &&
                host.Occurrences.Select(o => o.ConnectionId).Distinct().Count() == 2 &&
                host.Occurrences.All(o => o.OperationIndex == 1 && o.TargetNodeId == "DB"),
                "concurrent instances each deliver once from their own WRITE");
            host.ResetSimulation();
            ConnectionSpawnPlan replay = director.QueueProfile("delayed-threat");
            Check(replay.ConnectionId == first.ConnectionId &&
                host.Occurrences.Count == 0,
                "reset replays ID and clears prior deliveries");
            host.StepSimulation(8f);
            host.StepSimulation(24f);
            Check(host.Occurrences.Count == 1 &&
                host.Occurrences[0].ConnectionId == replay.ConnectionId &&
                host.Occurrences[0].OperationIndex == 1,
                "replayed arc delivers once from WRITE");
            host.ResetSimulation();
            Debug.Log("TrafficWitness delayed threat: expected READ 2 LOAD/16 WORK with no " +
                "delivery, next-step WRITE 12 LOAD/240 WORK, one delivery at index 1, " +
                "kill prevention in either operation, independent concurrent replay; observed matching states.");
        }

        private static void CheckAnomalyIndependence(ConnectionSpawnPlan[] plans)
        {
            foreach (string id in new[] { "high-load", "low-load", "work-deviation",
                "operation-deviation" })
                Check(plans.Any(p => p.GenerationTrace.anomalies.Contains(id)),
                    "sample includes " + id);
            foreach (ConnectionSpawnPlan plan in plans.Where(p =>
                p.GenerationTrace.anomalies.Length == 1 &&
                (p.GenerationTrace.anomalies[0] == "high-load" ||
                 p.GenerationTrace.anomalies[0] == "low-load")))
            {
                GeneratedOperationValue[] before = plan.GenerationTrace.normalOperations;
                GeneratedOperationValue[] after = plan.GenerationTrace.finalOperations;
                Check(before.Length == after.Length && before.Select(o => o.totalWork)
                    .SequenceEqual(after.Select(o => o.totalWork)),
                    "LOAD anomaly leaves WORK unchanged");
                float[] ratios = before.Select((o, i) => after[i].desiredLoad / o.desiredLoad).ToArray();
                bool high = plan.GenerationTrace.anomalies[0] == "high-load";
                Check(ratios.Count(r => Mathf.Abs(r - 1f) > 0.001f) == 1 &&
                    ratios.All(r => Mathf.Abs(r - 1f) <= 0.001f ||
                        (high ? r >= 2.5f - 0.001f && r <= 4f + 0.001f :
                            r >= 0.2f - 0.001f && r <= 0.5f + 0.001f)),
                    "LOAD multiplier applies to exactly one operation");
            }
            Check(plans.Any(p => p.GenerationTrace.anomalies.Length == 1 &&
                p.GenerationTrace.anomalies[0] == "high-load") &&
                plans.Any(p => p.GenerationTrace.anomalies.Length == 1 &&
                    p.GenerationTrace.anomalies[0] == "low-load"),
                "both LOAD directions sampled");
            foreach (ConnectionSpawnPlan plan in plans.Where(p =>
                p.GenerationTrace.anomalies.Length == 1 &&
                p.GenerationTrace.anomalies[0] == "work-deviation"))
            {
                GeneratedOperationValue[] before = plan.GenerationTrace.normalOperations;
                GeneratedOperationValue[] after = plan.GenerationTrace.finalOperations;
                Check(before.Length == after.Length && before.Select(o => o.desiredLoad)
                    .SequenceEqual(after.Select(o => o.desiredLoad)),
                    "WORK anomaly leaves LOAD unchanged");
                float[] ratios = before.Select((o, i) => after[i].totalWork / o.totalWork).ToArray();
                Check(ratios.Count(r => Mathf.Abs(r - 1f) > 0.001f) == 1 &&
                    ratios.All(r => Mathf.Abs(r - 1f) <= 0.001f ||
                        (r >= 2f - 0.001f && r <= 4f + 0.001f) ||
                        (r >= 0.25f - 0.001f && r <= 0.5f + 0.001f)),
                    "WORK multiplier applies to exactly one operation");
            }
            Check(plans.Any(p => p.GenerationTrace.anomalies.Length == 1 &&
                p.GenerationTrace.anomalies[0] == "work-deviation"),
                "WORK deviation sampled");
            Check(plans.Where(p => p.GenerationTrace.anomalies.Length == 1 &&
                p.GenerationTrace.anomalies[0] == "work-deviation")
                .Any(p => p.GenerationTrace.finalOperations.Zip(
                    p.GenerationTrace.normalOperations, (a, b) => a.totalWork / b.totalWork)
                    .Any(r => r < 0.5f)), "short WORK deviation sampled");
            foreach (ConnectionSpawnPlan plan in plans.Where(p =>
                p.GenerationTrace.anomalies.Length == 1 &&
                p.GenerationTrace.anomalies[0] == "operation-deviation"))
            {
                GeneratedOperationValue[] before = plan.GenerationTrace.normalOperations;
                GeneratedOperationValue[] after = plan.GenerationTrace.finalOperations;
                Check(after.Length == before.Length || after.Length == before.Length + 1,
                    "operation deviation changes one kind or appends one stage");
            }
        }

        private static void CheckGenerationDecisions(ConnectionSpawnPlan[] plans)
        {
            foreach (ConnectionSpawnPlan plan in plans)
            {
                ConnectionGenerationTrace trace = plan.GenerationTrace;
                Check(trace.selectedKeywords != null, "keyword decisions are explicit, including none");
                switch (trace.generationDecision)
                {
                    case "BENIGN_NORMAL":
                        Check(!plan.IsHostile && trace.objective == "NONE" &&
                            trace.anomalies.Length == 0 && trace.selectedKeywords.Length == 0,
                            "ordinary benign decision is explicit");
                        break;
                    case "BENIGN_OUTLIER":
                        Check(!plan.IsHostile && trace.objective == "NONE" &&
                            trace.anomalies.Length + trace.selectedKeywords.Length == 1,
                            "benign outlier records its anomaly or keyword choice");
                        break;
                    case "LOADER":
                        Check(plan.IsHostile && trace.objective == "LOADER" &&
                            trace.anomalies[0] == "high-load",
                            "loader decision records high-demand selection");
                        break;
                    case "DELIVERY_SUBTLE":
                        Check(plan.IsHostile && trace.objective == "DELIVERY" &&
                            trace.anomalies.Length == 0 && trace.selectedKeywords.Length == 0,
                            "intentional subtle delivery is distinguished from omitted evidence");
                        break;
                    case "DELIVERY_ANOMALOUS":
                        Check(plan.IsHostile && trace.objective == "DELIVERY" &&
                            trace.anomalies.Length >= 1,
                            "anomalous delivery records its mutation choice");
                        break;
                    default:
                        throw new InvalidOperationException("Unknown generation decision: " +
                            trace.generationDecision);
                }
                foreach (GeneratedKeywordSelection selection in trace.selectedKeywords)
                {
                    Check(selection.operationIndex >= 0 &&
                        selection.operationIndex < plan.Operations.Length &&
                        plan.Operations[selection.operationIndex].Behaviors.Any(behavior =>
                            behavior.Id == selection.behaviorId &&
                            behavior.Kind.ToString() == selection.behaviorKind) &&
                        trace.finalOperations[selection.operationIndex].behaviorIds
                            .Contains(selection.behaviorId),
                        "selected keyword records kind and operation index of concrete behavior");
                }
            }
            foreach (string decision in new[] { "BENIGN_NORMAL", "BENIGN_OUTLIER", "LOADER",
                "DELIVERY_SUBTLE", "DELIVERY_ANOMALOUS" })
                Check(plans.Any(p => p.GenerationTrace.generationDecision == decision),
                    "seeded sample includes decision " + decision);
            Check(plans.Any(p => p.GenerationTrace.generationDecision == "BENIGN_OUTLIER" &&
                    p.GenerationTrace.selectedKeywords.Length == 1) &&
                plans.Any(p => p.GenerationTrace.generationDecision == "LOADER" &&
                    p.GenerationTrace.selectedKeywords.Length == 1),
                "seeded sample includes selected keyword for benign and loader intent");
        }

        private static void CheckQueuedPlanRecording(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionSpawnPlan[] plans)
        {
            ConnectionsRunRecorder recorder = host.Recorder;
            bool originalRecording = recorder.RecordingEnabled;
            try
            {
                host.ResetSimulation();
                recorder.RecordingEnabled = true;
                recorder.BeginRun();
                var representatives = new[]
                {
                    plans.First(p => p.GenerationTrace.generationDecision == "DELIVERY_SUBTLE"),
                    plans.First(p => p.GenerationTrace.generationDecision == "BENIGN_OUTLIER" &&
                        p.GenerationTrace.selectedKeywords.Length == 1),
                    plans.First(p => p.GenerationTrace.generationDecision == "LOADER" &&
                        p.GenerationTrace.selectedKeywords.Length == 1)
                };
                foreach (ConnectionSpawnPlan plan in representatives)
                {
                    director.QueueSpawnPlan(plan, "ambient");
                    RunEvent entry = recorder.CurrentLog.events.Last();
                    Check(entry.type == "plan_queued" && entry.source == "ambient" &&
                        host.FindConnection(plan.ConnectionId) == null &&
                        entry.generation != null &&
                        JsonUtility.ToJson(entry.generation) == JsonUtility.ToJson(plan.GenerationTrace),
                        "queued decision is recorded before connection registration");
                    CheckQueuedDefinition(entry, plan);
                }
                ConnectionSpawnPlan manualPurpose = director.QueueProfile("product-lookup");
                RunEvent manualEntry = recorder.CurrentLog.events.Last();
                Check(manualEntry.source == "manual" &&
                    manualEntry.generation.generationDecision == "MANUAL_BENIGN" &&
                    manualEntry.generation.anomalies.Length == 0 &&
                    manualEntry.generation.selectedKeywords.Length == 0 &&
                    host.FindConnection(manualPurpose.ConnectionId) == null,
                    "manual purpose records an explicit ordinary decision before spawn");
                CheckQueuedDefinition(manualEntry, manualPurpose);
                NodeView[] route = { Node(host, "EXT1"), Node(host, "API"), Node(host, "DB") };
                var explicitPlan = new ConnectionSpawnPlan(3000f, "proof1", route,
                    "manual-proof", "Manual Probe", new[]
                    {
                        new OperationStage(OperationKind.Read, 23f, 1.2f, "Read probe"),
                        new OperationStage(OperationKind.Write, 47f, 2.4f, "Write probe",
                            CompletionEffect.InfectionDelivery, new[]
                            {
                                TimedModifierBehaviorDefinition.Spiking("SPIKING", 3f, 2f, 4f)
                            })
                    }, true, new[] { TimedModifierBehaviorDefinition.Overload("OVERLOAD", 5f, 7f) });
                director.QueueSpawnPlan(explicitPlan);
                RunEvent explicitEntry = recorder.CurrentLog.events.Last();
                Check(explicitEntry.source == "explicit" && explicitEntry.generation == null &&
                    host.FindConnection(explicitPlan.ConnectionId) == null,
                    "manual queue has full plan without fabricated generation choices");
                CheckQueuedDefinition(explicitEntry, explicitPlan);
                ConnectionsRunLog roundTrip = JsonUtility.FromJson<ConnectionsRunLog>(
                    JsonUtility.ToJson(recorder.CurrentLog));
                Check(roundTrip.events.Count(e => e.type == "plan_queued") == 5 &&
                    roundTrip.events.Last().definition.operations[1].behaviors[0].kind == "Spiking" &&
                    roundTrip.events.Any(e => e.generation != null &&
                        e.generation.generationDecision == "DELIVERY_SUBTLE"),
                    "full queued plans and explicit decisions survive JSON serialization");
            }
            finally
            {
                recorder.EndRun();
                recorder.RecordingEnabled = originalRecording;
                host.ResetSimulation();
            }
        }

        private static void CheckQueuedDefinition(RunEvent entry, ConnectionSpawnPlan plan)
        {
            ConnectionDefinitionRecord definition = entry.definition;
            Check(entry.connectionId == plan.ConnectionId &&
                entry.scheduledSpawnTime == plan.ScheduledSpawnTime &&
                entry.profileId == plan.ProfileId && definition != null &&
                definition.profileId == plan.ProfileId &&
                definition.isHostile == plan.IsHostile &&
                definition.claimedPurpose == plan.ClaimedPurpose &&
                definition.origin == plan.Origin.NodeId &&
                definition.target == plan.Target.NodeId &&
                definition.route.SequenceEqual(plan.Route.Select(node => node.NodeId)) &&
                definition.operations.Length == plan.Operations.Length,
                "plan_queued includes identity, schedule, route, claim, intent, and all stages");
            for (int i = 0; i < plan.Operations.Length; i++)
            {
                OperationRecord recorded = definition.operations[i];
                OperationStage stage = plan.Operations[i];
                Check(recorded.kind == stage.Operation.ToString() &&
                    recorded.activityDescription == stage.ActivityDescription &&
                    recorded.totalWork == stage.TotalWork &&
                    recorded.desiredLoad == stage.DesiredLoad &&
                    recorded.completionEffect == stage.EffectOnCompletion.ToString() &&
                    recorded.behaviors.Length == stage.Behaviors.Length,
                    "queued stage records exact WORK, LOAD, activity, effect, and behaviors");
                for (int j = 0; j < stage.Behaviors.Length; j++)
                    CheckBehavior(recorded.behaviors[j], stage.Behaviors[j]);
            }
            Check(definition.connectionBehaviors.Length == plan.ConnectionBehaviors.Length,
                "queued plan records connection-scope behavior count");
            for (int i = 0; i < plan.ConnectionBehaviors.Length; i++)
                CheckBehavior(definition.connectionBehaviors[i], plan.ConnectionBehaviors[i]);
        }

        private static void CheckBehavior(BehaviorRecord recorded,
            TimedModifierBehaviorDefinition behavior) => Check(
            recorded.id == behavior.Id && recorded.kind == behavior.Kind.ToString() &&
            recorded.startAfterSeconds == behavior.StartAfterSeconds &&
            recorded.stopAfterSeconds == behavior.StopAfterSeconds &&
            recorded.normalIntervalSeconds == behavior.NormalIntervalSeconds &&
            recorded.elevatedIntervalSeconds == behavior.ElevatedIntervalSeconds &&
            recorded.workDelta == behavior.WorkDelta && recorded.loadDelta == behavior.LoadDelta,
            "queued behavior records all concrete parameters");

        private static void CheckGeneratedPurposeRuntime(SimulationHost host,
            ConnectionTrafficDirector director)
        {
            host.ResetSimulation();
            var plans = Enumerable.Range(0, 100).Select(i => director.GeneratePlan(0f)).ToArray();
            ConnectionSpawnPlan delivery = plans.First(p => p.GenerationTrace.objective == "DELIVERY");
            ConnectionSpawnPlan benignSamePurpose = plans.First(p => !p.IsHostile &&
                p.ProfileId == delivery.ProfileId);
            Check(delivery.IsHostile && !benignSamePurpose.IsHostile &&
                delivery.ClaimedPurpose == benignSamePurpose.ClaimedPurpose,
                "one claimed purpose can be benign or hostile");
            director.QueueSpawnPlan(delivery);
            host.StepSimulation(0.1f);
            Check(host.KillConnection(delivery.ConnectionId) &&
                host.Occurrences.All(e => e.ConnectionId != delivery.ConnectionId),
                "killing generated DELIVERY before completion prevents effect");

            host.ResetSimulation();
            delivery = Enumerable.Range(0, 100).Select(i => director.GeneratePlan(0f))
                .First(p => p.GenerationTrace.objective == "DELIVERY" &&
                    p.Operations.Last().InfectionPayload.Kind == InfectionKind.PhantomLoad);
            director.QueueSpawnPlan(delivery);
            for (int i = 0; i < delivery.Operations.Length + 1; i++)
                host.StepSimulation(1000f);
            Check(host.FindConnection(delivery.ConnectionId).Resolution ==
                ConnectionResolution.Completed && host.Occurrences.Count(e =>
                    e.ConnectionId == delivery.ConnectionId &&
                    e.Type == CompletionEffect.InfectionDelivery) == 1,
                "generated DELIVERY emits exactly one effect at WORK completion");
            host.ResetSimulation();
            Debug.Log("TrafficWitness generated purposes: same claim can be benign or hostile; " +
                "generated DELIVERY kill prevents effect and normal completion emits once.");
        }

        private static string Signature(ConnectionSpawnPlan p) => p.ScheduledSpawnTime + "|" +
            p.ConnectionId + "|" + p.ProfileId + "|" + string.Join(">", p.Route.Select(n => n.NodeId)) +
            "|" + string.Join(";", p.Operations.Select(o => o.Operation + ":" +
                o.DesiredLoad.ToString("R") + ":" + o.TotalWork.ToString("R") + ":" +
                o.EffectOnCompletion)) + "|" + JsonUtility.ToJson(p.GenerationTrace);

        private static void CheckWeightOverrides(SimulationHost host,
            ConnectionTrafficDirector director, FieldInfo field)
        {
            var original = (AmbientWeightOverride[])field.GetValue(director);
            var excluded = new[] { "product-lookup", "customer-login", "cart-pricing",
                "order-submit", "inventory-sync", "sales-export" };
            try
            {
                host.ResetSimulation();
                string[] originalIds = Enumerable.Range(0, 20)
                    .Select(i => director.GeneratePlan(i * 13f).ConnectionId).ToArray();
                field.SetValue(director, excluded.Select(id =>
                    new AmbientWeightOverride { profileId = id, weight = 0f }).ToArray());
                host.ResetSimulation();
                Check(excluded.All(id => director.EffectiveAmbientWeight(id) == 0f),
                    "Inspector overrides exclude all purpose templates");
                Check(director.GeneratePlan(0f) == null,
                    "zero purpose weights stop ambient plans without enabling legacy profiles");
                field.SetValue(director, excluded.Select(id => new AmbientWeightOverride
                    { profileId = id, weight = id == "sales-export" ? 1f : 0f }).ToArray());
                host.ResetSimulation();
                var plans = Enumerable.Range(0, 20).Select(i => director.GeneratePlan(i * 13f)).ToArray();
                Check(plans.All(p => p.ProfileId == "sales-export") &&
                    plans.Select(p => p.ConnectionId).SequenceEqual(originalIds),
                    "weight override selects a purpose without shifting ID stream");
                host.ResetSimulation();
                ConnectionSpawnPlan explicitThreat = director.QueueProfile("basic-threat");
                Check(explicitThreat.IsHostile, "zero-weight profile still spawns explicitly");
                field.SetValue(director, new[] { new AmbientWeightOverride
                    { profileId = "missing", weight = 1f } });
                CheckResetRejected(director, "Unknown ambient weight profile");
                field.SetValue(director, new[] { new AmbientWeightOverride
                    { profileId = "product-lookup", weight = 0f }, new AmbientWeightOverride
                    { profileId = "PRODUCT-LOOKUP", weight = 1f } });
                CheckResetRejected(director, "Duplicate ambient weight override");
                field.SetValue(director, new[] { new AmbientWeightOverride
                    { profileId = "product-lookup", weight = float.NaN } });
                CheckResetRejected(director, "finite nonnegative weight");
            }
            finally
            {
                field.SetValue(director, original);
                host.ResetSimulation();
            }
        }

        private static void CheckResetRejected(ConnectionTrafficDirector director, string message)
        {
            bool rejected = false;
            try { director.ResetDirector(); }
            catch (InvalidOperationException error) { rejected = error.Message.Contains(message); }
            Check(rejected, "invalid override reports " + message);
        }

        private static void CheckCustomProfiles(SimulationHost host,
            ConnectionTrafficDirector director, FieldInfo overridesField)
        {
            FieldInfo profilesField = typeof(ConnectionTrafficDirector).GetField(
                "profiles", BindingFlags.Instance | BindingFlags.NonPublic);
            var originalProfiles = (ConnectionTrafficProfile[])profilesField.GetValue(director);
            var originalOverrides = (AmbientWeightOverride[])overridesField.GetValue(director);
            try
            {
                profilesField.SetValue(director, new[]
                {
                    originalProfiles.Single(p => p.id == "retrieval"),
                    originalProfiles.Single(p => p.id == "spiking-benign")
                });
                overridesField.SetValue(director, new[] { new AmbientWeightOverride
                    { profileId = "spiking-benign", weight = 1f } });
                host.ResetSimulation();
                Check(Enumerable.Range(0, 10).All(i =>
                    director.GeneratePlan(i * 13f).ProfileId == "spiking-benign"),
                    "custom Inspector profile array and keyed override drive ambient selection");
                Check(director.QueueProfile("retrieval").ProfileId == "retrieval",
                    "zero-weight custom profile remains explicitly spawnable");
            }
            finally
            {
                profilesField.SetValue(director, originalProfiles);
                overridesField.SetValue(director, originalOverrides);
                host.ResetSimulation();
            }
        }

        private static void CheckAmbientScheduleReplay(SimulationHost host,
            ConnectionTrafficDirector director)
        {
            var recorder = host.Recorder;
            bool oldRecording = recorder.RecordingEnabled;
            bool oldAuto = director.AutoGenerationEnabled;
            int oldSeed = director.Seed;
            try
            {
                recorder.RecordingEnabled = true;
                director.AutoGenerationEnabled = true;
                string first = AmbientRunSignature(host, recorder, out string firstMix);
                string replay = AmbientRunSignature(host, recorder, out string replayMix);
                Check(first == replay, "same seed and configuration replay ambient schedule and concrete plans");
                Check(firstMix == replayMix, "same seed replays ambient profile mix");
                director.Seed = oldSeed + 1;
                string different = AmbientRunSignature(host, recorder, out string differentMix);
                Check(first != different && firstMix != differentMix,
                    "a different seed changes generated plans and mix");
            }
            finally
            {
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                director.AutoGenerationEnabled = oldAuto;
                director.Seed = oldSeed;
                host.ResetSimulation();
            }
        }

        private static string AmbientRunSignature(SimulationHost host,
            ConnectionsRunRecorder recorder, out string mix)
        {
            host.ResetSimulation();
            recorder.BeginRun();
            for (int i = 0; i < 80; i++) host.StepSimulation(1f);
            RunEvent[] arrivals = recorder.CurrentLog.events.Where(e =>
                e.type == "plan_queued" && e.source == "ambient").ToArray();
            Check(arrivals.Length >= 5 && arrivals[0].scheduledSpawnTime == 3f,
                "single ambient scheduler keeps scene grace and interval");
            Check(arrivals.All(e => e.generation != null &&
                e.generation.purposeId == e.profileId &&
                e.generation.normalOperations.Length > 0 &&
                e.generation.finalOperations.Length > 0 &&
                JsonUtility.ToJson(e).Contains("normalOperations")),
                "ambient recorder events serialize purpose and before/after operations");
            mix = string.Join("|", arrivals.Select(e => e.profileId));
            string signature = string.Join("|", recorder.CurrentLog.events.Where(e =>
                e.type == "plan_queued" || e.type == "connection_spawned")
                .Select(e => JsonUtility.ToJson(e)));
            recorder.EndRun();
            return signature;
        }

        private static string TrafficSignature(ConnectionSpawnPlan p) => p.ScheduledSpawnTime + "|" +
            p.ProfileId + "|" + string.Join(">", p.Route.Select(n => n.NodeId)) +
            "|" + string.Join(";", p.Operations.Select(o => o.Operation + ":" +
                o.DesiredLoad.ToString("R") + ":" + o.TotalWork.ToString("R") + ":" +
                o.EffectOnCompletion)) + "|" + JsonUtility.ToJson(p.GenerationTrace);

        private static void CheckGeneratedIdentifiers(SimulationHost host,
            ConnectionTrafficDirector director)
        {
            director.ResetDirector();
            ConnectionSpawnPlan referenceFirst = director.GeneratePlan(0f);
            ConnectionSpawnPlan referenceSecond = director.GeneratePlan(13f);
            ConnectionSpawnPlan referenceThird = director.GeneratePlan(26f);
            host.ResetSimulation();
            var explicitPlan = new ConnectionSpawnPlan(0f, referenceSecond.ConnectionId,
                referenceFirst.Route, "test", "Explicit two-character reservation",
                new[] { new OperationStage(OperationKind.Read, 100f, 1f, "Read") });
            director.QueueSpawnPlan(explicitPlan);
            ConnectionSpawnPlan generatedFirst = director.GeneratePlan(0f);
            ConnectionSpawnPlan generatedSecond = director.GeneratePlan(13f);
            Check(generatedFirst.ConnectionId == referenceFirst.ConnectionId &&
                generatedSecond.ConnectionId == referenceThird.ConnectionId,
                "explicit queued ID is skipped in deterministic pool order");
            Check(TrafficSignature(referenceFirst) == TrafficSignature(generatedFirst) &&
                TrafficSignature(referenceSecond) == TrafficSignature(generatedSecond),
                "ID reservation does not shift profile, route, LOAD, or WORK draws");
            director.QueueSpawnPlan(generatedFirst);
            host.StepSimulation(0.1f);
            Check(host.KillConnection(explicitPlan.ConnectionId) &&
                host.KillConnection(generatedFirst.ConnectionId.ToUpperInvariant()) &&
                host.FindConnection(generatedFirst.ConnectionId.ToUpperInvariant()) != null,
                "generated and explicit IDs remain case-insensitive and inspectable after resolution");

            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { explicitPlan.ConnectionId };
            foreach (ConnectionSpawnPlan plan in new[] { generatedFirst, generatedSecond })
                Check(seen.Add(plan.ConnectionId), "early generated IDs are unique");
            ConnectionSpawnPlan next;
            while ((next = director.GeneratePlan(26f)) != null)
            {
                Check(next.ConnectionId.Length == 2 &&
                    next.ConnectionId[0] >= 'a' && next.ConnectionId[0] <= 'z' &&
                    next.ConnectionId[1] >= '0' && next.ConnectionId[1] <= '9',
                    "generated ID is a lowercase letter followed by a digit");
                Check(seen.Add(next.ConnectionId), "all generated IDs are unique across run history");
            }
            Check(seen.Count == 260 && director.GeneratePlan(39f) == null,
                "all 260 IDs exhaust safely without recycling");
            Check(seen.All(id => id.Length == 2 &&
                id[0] >= 'a' && id[0] <= 'z' && id[1] >= '0' && id[1] <= '9'),
                "every generated or reserved pool ID is letter then digit");
            bool demandRejected = false;
            try { director.QueueHighDemandWitness(); }
            catch (InvalidOperationException) { demandRejected = true; }
            Check(demandRejected, "high-demand request reports exhausted IDs");
            Debug.Log("TrafficWitness IDs: 260 distinct letter-digit IDs, deterministic replay, " +
                "explicit reservation, retained history, independent traffic RNG, safe exhaustion.");
        }
        private static NodeView Node(SimulationHost host, string id) =>
            host.Nodes.First(n => n.View.NodeId == id).View;
        private static void Near(float observed, float expected, string name) =>
            Check(Mathf.Abs(observed - expected) < 0.001f,
                name + $": expected {expected:0.###}, observed {observed:0.###}");
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("TrafficWitness failed: " + name);
        }
    }
}
