using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    // Checks the serialized playable scene, then exercises its real director and host.
    public static class AdvancedSceneWitness
    {
        private const string Scene = "Assets/Scenes/ConnectionsAdvanced.unity";
        private static readonly string[] NodeIds =
            { "EXT1", "EXT2", "WEB", "API", "AUTH", "DB", "JOBS", "RPT" };
        private static readonly string[] Edges =
            { "EXT1-WEB", "API-WEB", "AUTH-WEB", "API-DB", "DB-RPT", "API-JOBS", "EXT2-JOBS" };
        private static readonly Dictionary<string, string> Roles = new Dictionary<string, string>
        {
            { "EXT1", "CUSTOMER INGRESS" }, { "EXT2", "PARTNER INGRESS" },
            { "WEB", "CUSTOMER GATEWAY" }, { "API", "SHARED APP SERVICE" },
            { "AUTH", "AUTH SERVICE" }, { "DB", "DATABASE" },
            { "JOBS", "INTEGRATION" }, { "RPT", "REPORTING" }
        };
        private static readonly Dictionary<string, string> Targets = new Dictionary<string, string>
        {
            { "product-lookup", "DB" }, { "customer-login", "AUTH" },
            { "cart-pricing", "API" }, { "order-submit", "API" },
            { "inventory-sync", "JOBS" }, { "sales-export", "RPT" }
        };

        [MenuItem("Tools/Connections/Run Advanced Scene Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(Scene);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
            Check(host != null && director != null && recorder != null && presenter != null,
                "host, director, recorder and path presenter exist");
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>(FindObjectsInactive.Include);
            var identity = UnityEngine.Object.FindFirstObjectByType<IdentityPresenter>(FindObjectsInactive.Include);
            var clean = UnityEngine.Object.FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
            Check(terminal != null && identity != null && clean != null &&
                UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>(FindObjectsInactive.Include) != null,
                "workstation UI bindings exist");
            foreach (string field in new[] { "commandInput", "historyScroll", "historyContent",
                "textRowPrefab", "inspectPrefab", "scanPrefab", "simulationHost",
                "trafficDirector", "recorder", "identityPresenter", "cleanOverlay" })
                Check(Bound(terminal, field), "terminal binding " + field);
            foreach (string field in new[] { "networkArea", "pathLayer", "markerLayer",
                "pathPrefab", "identityPresenter" })
                Check(Bound(presenter, field), "path presenter binding " + field);
            foreach (string field in new[] { "title", "body", "pidInput" })
                Check(Bound(clean, field), "clean overlay binding " + field);
            Check(host.RunMode == ConnectionRunMode.Generated && host.Definitions.Length == 0 &&
                director.AutoGenerationEnabled && director.HostileGenerationEnabled &&
                director.RandomizeSeed && recorder.RecordingEnabled,
                "playable generated traffic and recording defaults");
            Check(host.Nodes.Length == 8 && host.PhysicalLinks.Length == 7,
                "eight resource nodes and seven physical links");
            var nodes = host.Nodes.ToDictionary(n => n.View.NodeId);
            Check(nodes.Keys.OrderBy(x => x).SequenceEqual(NodeIds.OrderBy(x => x)),
                "exact node IDs");
            foreach (NodeResources node in host.Nodes)
            {
                bool external = node.View.NodeId.StartsWith("EXT", StringComparison.Ordinal);
                Check(node.LoadCapacity == 10f && node.BaselineLoad == (external ? 0f : 1f),
                    "node LOAD defaults " + node.View.NodeId);
                Check(node.View.GetComponent<RectTransform>() != null,
                    "authored card " + node.View.NodeId);
                Transform heading = node.View.transform.Find("NodeHeading");
                Check(node.View.LoadBar != null && heading != null &&
                    heading.Find("NodeId")?.GetComponent<TMP_Text>()?.text == node.View.NodeId &&
                    heading.Find("NodeRole")?.GetComponent<TMP_Text>()?.text ==
                        Roles[node.View.NodeId],
                    "visible node ID, role and LOAD bar " + node.View.NodeId);
            }
            var links = host.PhysicalLinks.Select(p => p.View).ToArray();
            Check(links.All(l => l != null && l.ResourceLabel != null &&
                nodes.ContainsKey(l.NodeA.NodeId) && nodes.ContainsKey(l.NodeB.NodeId)) &&
                links.Select(l => l.LinkId).Distinct().Count() == 7,
                "link labels, endpoints and unique IDs");
            Check(links.Select(l => Edge(l.NodeA.NodeId, l.NodeB.NodeId)).OrderBy(x => x)
                .SequenceEqual(Edges.OrderBy(x => x)), "exact tree edges");
            Check(director.AmbientOriginId == "EXT1" &&
                director.EffectiveAmbientOrigins.Count == 2 &&
                director.EffectiveAmbientOrigins[0].nodeId == "EXT1" &&
                Mathf.Approximately(director.EffectiveAmbientOrigins[0].weight, 0.6f) &&
                director.EffectiveAmbientOrigins[1].nodeId == "EXT2" &&
                Mathf.Approximately(director.EffectiveAmbientOrigins[1].weight, 0.4f),
                "weighted dual ingress");
            Check(director.BenignPopulationGoal == 4f && director.CoverageStartupGoal == 1f &&
                director.CoverageRampSeconds == 75f &&
                director.HostileMeanIntervalSeconds == 54f,
                "population and hostile scheduling");
            director.ResetDirector();
            foreach (var target in Targets)
                Check(director.EffectiveTargetIds(director.Profiles.First(p => p.id == target.Key))
                    .SequenceEqual(new[] { target.Value }), "PURPOSE target " + target.Key);

            // The geometry check uses the authored 1920x1080 anchors, independent of Game View size.
            var area = presenter.NetworkArea;
            CanvasScaler scaler = area.GetComponentInParent<CanvasScaler>();
            var panel = area.parent as RectTransform;
            Vector2 reference = scaler.referenceResolution;
            Vector2 panelSize = Vector2.Scale(reference, panel.anchorMax - panel.anchorMin) + panel.sizeDelta;
            Vector2 areaSize = Vector2.Scale(panelSize, area.anchorMax - area.anchorMin) + area.sizeDelta;
            var graph = new StressTopologyGraph();
            var positions = new Vector2[host.Nodes.Length];
            for (int i = 0; i < host.Nodes.Length; i++)
            {
                graph.Nodes.Add(new StressTopologyGraph.Node
                {
                    Id = host.Nodes[i].View.NodeId,
                    External = host.Nodes[i].View.NodeId.StartsWith("EXT", StringComparison.Ordinal)
                });
                positions[i] = host.Nodes[i].View.GetComponent<RectTransform>().anchoredPosition;
            }
            foreach (var link in links)
                graph.Edges.Add(new StressTopologyGraph.Edge(
                    Array.FindIndex(host.Nodes, n => n.View == link.NodeA),
                    Array.FindIndex(host.Nodes, n => n.View == link.NodeB)));
            Vector2 cardSize = host.Nodes[0].View.GetComponent<RectTransform>().sizeDelta;
            var geometry = TopologyStressGeometry.Measure(graph, positions, areaSize, cardSize);
            Check(geometry.NodeOverlaps == 0 && geometry.NodeOverflow == 0 &&
                geometry.LinksThroughNodes == 0 && geometry.LinkCrossings == 0 &&
                geometry.Below120 == 0, "16:9 topology geometry");
            Debug.Log($"ADVANCED GEOMETRY: area={areaSize}, card={cardSize}, " +
                $"shortest={geometry.ShortestVisible:0.#}px, overlaps={geometry.NodeOverlaps}, " +
                $"overflow={geometry.NodeOverflow}, crossings={geometry.LinkCrossings}");

            foreach (int seed in new[] { 1701, 3141, 9001 })
            {
                director.RandomizeSeed = false;
                director.Seed = seed;
                host.SetRunMode(ConnectionRunMode.Generated);
                recorder.BeginRun();
                Check(recorder.CurrentLog.run.nodes.Length == 8 &&
                    recorder.CurrentLog.run.traffic.ambientOrigins.Length == 2,
                    "recorder topology and ingress metadata");
                for (int second = 0; second < 600; second++) host.StepSimulation(1f);
                var events = recorder.CurrentLog.events;
                var ambient = events.Where(e => e.type == "plan_queued" &&
                    (e.source == "ambient_benign" || e.source == "ambient_hostile")).ToArray();
                Check(ambient.Length > 0 && ambient.Any(e => e.source == "ambient_benign") &&
                    ambient.Any(e => e.source == "ambient_hostile"), "benign and hostile arrivals");
                Check(ambient.All(e => e.definition != null &&
                    Targets[e.profileId] == e.definition.target &&
                    (e.definition.origin == "EXT1" || e.definition.origin == "EXT2") &&
                    e.definition.route.First() == e.definition.origin &&
                    e.definition.route.Last() == e.definition.target &&
                    e.definition.route.Length == e.definition.route.Distinct().Count() &&
                    e.definition.route.Zip(e.definition.route.Skip(1), Edge)
                        .All(edge => Edges.Contains(edge))),
                    "all ambient plans have correct endpoints and targets");
                Check(ambient.Any(e => e.definition.origin == "EXT1") &&
                    ambient.Any(e => e.definition.origin == "EXT2"),
                    "both ingress nodes used");
                Check(events.Any(e => e.type == "infection_applied"),
                    "generated infection delivered");
                Check(recorder.Save(false), "run log saved");
                Debug.Log($"ADVANCED SEED {seed}: benign={ambient.Count(e => e.source == "ambient_benign")}, " +
                    $"hostile={ambient.Count(e => e.source == "ambient_hostile")}, " +
                    $"EXT1={ambient.Count(e => e.definition.origin == "EXT1")}, " +
                    $"EXT2={ambient.Count(e => e.definition.origin == "EXT2")}, " +
                    $"infections={events.Count(e => e.type == "infection_applied")}, " +
                    $"log={recorder.OutputPath}");
                recorder.EndRun();
            }

            director.AutoGenerationEnabled = false;
            director.Seed = 1701;
            host.SetRunMode(ConnectionRunMode.Generated);
            recorder.BeginRun();
            ConnectionSpawnPlan crossing = null;
            for (int i = 0; i < 20 && crossing == null; i++)
            {
                ConnectionSpawnPlan candidate = director.GenerateBenignPlan(0f);
                if (candidate.Origin.NodeId == "EXT1") crossing = candidate;
            }
            Check(crossing != null, "generated benign EXT1 crossing plan");
            director.QueueSpawnPlan(crossing, "advanced_scene_witness");
            host.StepSimulation(0.1f);
            Check(host.TryInjectInfection("WEB", InfectionKind.Blackout, 0f, out _),
                "Blackout at customer gateway");
            host.StepSimulation(2f);
            RunEvent canceled = recorder.CurrentLog.events.LastOrDefault(e =>
                e.type == "connection_resolved" && e.connectionId == crossing.ConnectionId);
            Debug.Log($"ADVANCED BLACKOUT: id={crossing.ConnectionId}, event={canceled?.resolutionReason}, " +
                $"cause={canceled?.resolutionCause}, node={canceled?.failureNodeId}, " +
                $"penalty={canceled?.healthPenaltyAmount}");
            Check(canceled != null && canceled.resolutionReason == "Canceled" &&
                canceled.resolutionCause == "Blackout" && canceled.failureNodeId == "WEB" &&
                Mathf.Approximately(canceled.healthPenaltyAmount, -5f),
                "Blackout cancels routed legitimate attempt once");
            recorder.EndRun();

            host.SetRunMode(ConnectionRunMode.Generated);
            recorder.BeginRun();
            Check(host.TryInjectInfection("DB", InfectionKind.Spawner, 1f, out _),
                "Spawner at internal DB");
            host.StepSimulation(2f);
            var children = recorder.CurrentLog.events.Where(e =>
                e.type == "plan_queued" && e.source == "spawner").ToArray();
            Check(children.Length > 0 && children.All(e => e.definition.origin == "DB" &&
                Targets.Values.Contains(e.definition.target) &&
                e.definition.target != "DB" && e.definition.target != "EXT1" &&
                e.definition.target != "EXT2"),
                "Spawner routes children to eligible internal destinations");
            recorder.EndRun();
            Debug.Log("AdvancedSceneWitness PASS: authored scene, three deterministic " +
                "real-traffic runs, Blackout cancellation and Spawner routing.");
        }

        private static string Edge(string a, string b) =>
            string.CompareOrdinal(a, b) < 0 ? a + "-" + b : b + "-" + a;

        private static bool Bound(UnityEngine.Object target, string field) =>
            new SerializedObject(target).FindProperty(field)?.objectReferenceValue != null;

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("AdvancedSceneWitness failed: " + label);
        }
    }
}
