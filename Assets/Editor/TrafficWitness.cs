using System;
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
                Check(host.BlockConnection("later1") && host.BlockConnection("later2"),
                    "scheduled plans can be blocked");

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
                    Check(host.BlockConnection(state.ConnectionId), "block active connection");
                Check(host.RuntimeConnections.Count == 0 &&
                    host.FindConnection("burst0").Resolution == ConnectionResolution.Blocked,
                    "blocked connections retire with reason");
                Near(dbResources.RequestedLoad, dbResources.BaselineLoad, "blocked LOAD released");
                Debug.Log("TrafficWitness burst: expected 12 arrivals, DB 120 requested/10 allocated; " +
                    $"observed 12 and {dbResources.RequestedLoad:0.#} after block release.");
                Debug.Log("TrafficWitness PASS");
            }
            finally
            {
                director.Seed = originalSeed;
                director.AutoGenerationEnabled = originalAuto;
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static string Signature(ConnectionSpawnPlan p) => p.ScheduledSpawnTime + "|" +
            p.ConnectionId + "|" + p.ProfileId + "|" + string.Join(">", p.Route.Select(n => n.NodeId)) +
            "|" + string.Join(";", p.Operations.Select(o => o.Operation + ":" +
                o.DesiredLoad.ToString("R") + ":" + o.TotalWork.ToString("R")));
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
