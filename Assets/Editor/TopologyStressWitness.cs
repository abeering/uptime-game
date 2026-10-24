using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class TopologyStressWitness
    {
        public static void RunWithRegressions()
        {
            Run();
            GeometryWitness.Run();
            ResourceWitness.Run();
            TrafficWitness.Run();
            ValidateStressScene();
            Debug.Log("TopologyStressWitness regression suite PASS.");
        }

        private static void ValidateStressScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsTopologyStress.unity");
            var harness = UnityEngine.Object.FindFirstObjectByType<TopologyStressHarness>();
            Require(harness != null && harness.NodeCount == 8 &&
                harness.PlacementMode == StressPlacementMode.OptimizedV1,
                "authored stress scene");
            var serialized = new SerializedObject(harness);
            foreach (string field in new[] { "topologyArea", "nodeTemplate", "linkTemplate",
                "host", "director", "statusLabel" })
                Require(serialized.FindProperty(field).objectReferenceValue != null,
                    "stress scene reference " + field);
            Debug.Log("TopologyStressWitness PASS: separate scene loads with authored bindings.");
        }

        public static void Run()
        {
            Vector2 area = new Vector2(1203.2f, 911f);
            Vector2 card = new Vector2(290f, 96f);
            foreach (int count in new[] { 4, 8, 12, 16, 20, 24, 28 })
                foreach (int seed in new[] { 1701, 3141, 9001 })
                {
                    StressTopologyGraph graph = StressTopologyGraph.Generate(count, seed);
                    Require(graph.Nodes.Count == count && graph.Edges.Count == count - 1,
                        "tree size");
                    var pairs = new HashSet<string>();
                    var reached = new HashSet<int> { 0 };
                    foreach (StressTopologyGraph.Edge edge in graph.Edges)
                    {
                        Require(edge.A >= 0 && edge.B < count && edge.A != edge.B,
                            "link endpoints");
                        Require(pairs.Add(Math.Min(edge.A, edge.B) + ":" +
                            Math.Max(edge.A, edge.B)), "duplicate link");
                        Require(reached.Contains(edge.A), "unconnected insertion");
                        reached.Add(edge.B);
                    }
                    Require(reached.Count == count, "connected graph");
                    int external = 0, internalServices = 0;
                    foreach (StressTopologyGraph.Node node in graph.Nodes)
                        if (node.External) external++;
                        else if (node.Role == "SERVICE" || node.Role == "CORE")
                            internalServices++;
                    Require(external >= (count >= 8 ? 2 : 1) && internalServices >= 2,
                        "origins and services");
                    Vector2[] first = StressTopologyPlacement.Place(graph,
                        new Vector2(1203.2f, 911f), seed);
                    Vector2[] second = StressTopologyPlacement.Place(
                        StressTopologyGraph.Generate(count, seed),
                        new Vector2(1203.2f, 911f), seed);
                    for (int i = 0; i < count; i++)
                        Require(first[i] == second[i], "deterministic placement");
                    Vector2[] optimized = TopologyStressPlacementV1.Place(graph,
                        area, card, seed);
                    Vector2[] repeated = TopologyStressPlacementV1.Place(graph,
                        area, card, seed);
                    var reordered = new StressTopologyGraph();
                    reordered.Nodes.AddRange(graph.Nodes);
                    for (int edge = graph.Edges.Count - 1; edge >= 0; edge--)
                        reordered.Edges.Add(graph.Edges[edge]);
                    Vector2[] reversedEdges = TopologyStressPlacementV1.Place(reordered,
                        area, card, seed);
                    Require(optimized.Length == count, "optimized node count");
                    for (int i = 0; i < count; i++)
                        Require(optimized[i] == repeated[i] &&
                            optimized[i] == reversedEdges[i],
                            "optimized placement replay and edge order");
                    TopologyStressGeometry.Measurements geometry =
                        TopologyStressGeometry.Measure(graph, optimized, area, card);
                    Require(geometry.NodeOverlaps == 0 && geometry.NodeOverflow == 0,
                        "optimized finite card placement");
                }
            CheckGeometryDiagnostics();
            Debug.Log("TopologyStressWitness PASS: 21 trees, deterministic V0/V1 placement, finite cards, geometric diagnostics.");
        }

        private static void CheckGeometryDiagnostics()
        {
            Vector2 card = new Vector2(40f, 40f);
            Require(TopologyStressGeometry.SegmentsCross(
                new Vector2(-150f, 0f), new Vector2(150f, 0f),
                new Vector2(0f, -150f), new Vector2(0f, 150f)),
                "unrelated edge crossing");
            Require(!TopologyStressGeometry.SegmentsCross(
                Vector2.zero, new Vector2(150f, 0f),
                Vector2.zero, new Vector2(0f, 150f)),
                "shared endpoint is not crossing");
            Require(TopologyStressGeometry.SegmentIntersectsRect(
                new Vector2(-150f, 0f), new Vector2(150f, 0f),
                TopologyStressGeometry.Card(Vector2.zero, card)),
                "link through unrelated card");
            Require(!TopologyStressGeometry.SegmentIntersectsRect(
                new Vector2(-150f, 100f), new Vector2(150f, 100f),
                TopologyStressGeometry.Card(Vector2.zero, card)),
                "clear link does not obstruct card");
            Require(Mathf.Abs(TopologyStressGeometry.VisibleLength(Vector2.zero,
                new Vector2(200f, 0f), new Vector2(100f, 50f)) - 100f) < 0.01f,
                "card-boundary visible length");
            var crossingGraph = new StressTopologyGraph();
            for (int i = 0; i < 4; i++) crossingGraph.Nodes.Add(new StressTopologyGraph.Node());
            crossingGraph.Edges.Add(new StressTopologyGraph.Edge(0, 1));
            crossingGraph.Edges.Add(new StressTopologyGraph.Edge(2, 3));
            TopologyStressGeometry.Measurements measured = TopologyStressGeometry.Measure(
                crossingGraph, new[] { new Vector2(-150f, 0f), new Vector2(150f, 0f),
                    new Vector2(0f, -150f), new Vector2(0f, 150f) },
                new Vector2(800f, 800f), card);
            Require(measured.LinkCrossings == 1 && measured.LinksThroughNodes == 0 &&
                measured.Below120 == 0, "crossing diagnostic excludes shared nodes");
            // A blocked subset of lanes must not keep rotating already scheduled IDs.
            var obstacle = new IdentityPacking.PillBounds(new Vector2(150f, -50f),
                Vector2.right, new Vector2(500f, 65f));
            IdentityPacking.Layout impossible = IdentityPacking.Solve(
                Vector2.zero, new Vector2(300f, 0f), 96f, 4, 12f, 2.5f, 6f,
                new Vector2(54f, 28f), 6f, 20f, 12f, 1f,
                new[] { obstacle });
            Require(impossible.Stage == IdentityPacking.ReadabilityStage.Unavailable &&
                impossible.Schedule.Count == 0, "rotation with unreachable IDs terminates");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Stress witness: " + message);
        }
    }
}
