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
            Require(harness != null && harness.NodeCount == 8, "authored stress scene");
            var serialized = new SerializedObject(harness);
            foreach (string field in new[] { "topologyArea", "nodeTemplate", "linkTemplate",
                "host", "director", "statusLabel" })
                Require(serialized.FindProperty(field).objectReferenceValue != null,
                    "stress scene reference " + field);
            Debug.Log("TopologyStressWitness PASS: separate scene loads with authored bindings.");
        }

        public static void Run()
        {
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
                }
            Debug.Log("TopologyStressWitness PASS: 21 connected unique trees, origins, services, deterministic placement.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Stress witness: " + message);
        }
    }
}
