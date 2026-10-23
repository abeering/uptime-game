using System;
using System.Collections.Generic;
using System.Linq;
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
                    Check(plan.Operations.Length == profile.operations.Length, "generated operation count");
                    for (int stageIndex = 0; stageIndex < plan.Operations.Length; stageIndex++)
                    {
                        OperationStage stage = plan.Operations[stageIndex];
                        ConnectionOperationProfile spec = profile.operations[stageIndex];
                        float nominal = stage.TotalWork / stage.DesiredLoad;
                        Check(stage.Operation == spec.operation &&
                            stage.DesiredLoad >= spec.minimumLoad && stage.DesiredLoad <= spec.maximumLoad &&
                            nominal >= spec.minimumDuration - 0.001f &&
                            nominal <= spec.maximumDuration + 0.001f,
                            "generated LOAD and duration within configured ranges");
                    }
                }
                Check(replay.Select(p => p.ProfileId).Distinct().Count() == 5,
                    "weighted catalog includes ordinary and incidental profiles");
                Check(replay.All(p => p.ProfileId != "basic-threat"),
                    "manual threat is absent from ambient traffic");
                ConnectionTrafficProfile quick = director.Profiles.First(p => p.id == "quick-lookup");
                Check(quick.operations.Length == 1 && quick.operations[0].operation == OperationKind.Read &&
                    quick.operations[0].minimumDuration == 4f && quick.operations[0].maximumDuration == 8f,
                    "background profile duration and operation");
                ConnectionTrafficProfile indexing = director.Profiles.First(p => p.id == "indexing");
                Check(indexing.operations.Length == 2 &&
                    indexing.operations.Sum(o => o.maximumDuration) <= 75f,
                    "multi-stage lifetime is a total, not per-stage range");
                Debug.Log("TrafficWitness seeded 200-plan replay: expected identical; observed identical. " +
                    "Catalog: five weighted profiles, quick READ 4-8s, multi-stage total <=75s.");

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
                Check(host.RuntimeConnections.Count == 2 && w1.IsActive && w2.IsActive &&
                    host.SimulationTimeSeconds == 0.5f, "queued plans arrive without simulation reset");
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
                Debug.Log("TrafficWitness PASS");
            }
            finally
            {
                director.Seed = originalSeed;
                director.AutoGenerationEnabled = originalAuto;
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
                    host.Occurrences[0].ConnectionId == replay.ConnectionId &&
                    host.Occurrences[0].TimeSeconds == 24f,
                    "WORK completed inside a step emits once at the completed step time");

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

        private static string Signature(ConnectionSpawnPlan p) => p.ScheduledSpawnTime + "|" +
            p.ConnectionId + "|" + p.ProfileId + "|" + string.Join(">", p.Route.Select(n => n.NodeId)) +
            "|" + string.Join(";", p.Operations.Select(o => o.Operation + ":" +
                o.DesiredLoad.ToString("R") + ":" + o.TotalWork.ToString("R")));

        private static string TrafficSignature(ConnectionSpawnPlan p) => p.ScheduledSpawnTime + "|" +
            p.ProfileId + "|" + string.Join(">", p.Route.Select(n => n.NodeId)) +
            "|" + string.Join(";", p.Operations.Select(o => o.Operation + ":" +
                o.DesiredLoad.ToString("R") + ":" + o.TotalWork.ToString("R")));

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
