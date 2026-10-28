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
                l.ResourceLabel.gameObject.activeInHierarchy &&
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
            Check(new SerializedObject(presenter).FindProperty("adaptiveNodePorts").boolValue,
                "adaptive node ports enabled in advanced scene");
            CheckSparseCenteredPorts(links, area);
            CheckCrowdedPorts(links, area, identity);
            CheckThreeAtWeb(links, area, identity);

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

        private static void CheckSparseCenteredPorts(PhysicalLinkView[] links,
            RectTransform area)
        {
            var counts = links.Where(link => link.LinkId == "E4" ||
                link.LinkId == "E6" || link.LinkId == "E7")
                .ToDictionary(link => link, _ => 1);
            Dictionary<PhysicalLinkView, NodePortPacking.Port> ports =
                NodePortPacking.Plan(counts, area, 12f, 6f);
            foreach (PhysicalLinkView link in counts.Keys)
            {
                NodePortPacking.Port port = ports[link];
                Check(port.Budget >= 12f && Mathf.Abs(port.AtA) < 0.1f &&
                    Mathf.Abs(port.AtB) < 0.1f,
                    link.LinkId + " sparse API/JOBS path stays on its authored center");
            }
        }

        private static void CheckCrowdedPorts(PhysicalLinkView[] links, RectTransform area,
            IdentityPresenter identity)
        {
            Canvas.ForceUpdateCanvases();
            var counts = new Dictionary<PhysicalLinkView, int>();
            foreach (PhysicalLinkView link in links)
            {
                int count = link.LinkId == "E1" || link.LinkId == "E2" ||
                    link.LinkId == "E3" ? 4 : link.LinkId == "E4" ? 9 :
                    link.LinkId == "E6" ? 7 : 3;
                counts.Add(link, count);
            }
            Dictionary<PhysicalLinkView, NodePortPacking.Port> ports =
                NodePortPacking.Plan(counts, area, 12f, 6f);
            foreach (PhysicalLinkView link in links)
                link.SetAttachmentOffsets(ports[link].AtA, ports[link].AtB, 0.3f);
            Dictionary<PhysicalLinkView, NodePortPacking.Port> reordered =
                NodePortPacking.Plan(counts.Reverse().ToDictionary(p => p.Key, p => p.Value),
                    area, 12f, 6f);
            foreach (PhysicalLinkView link in links)
            {
                NodePortPacking.Port first = ports[link], second = reordered[link];
                Check(Mathf.Abs(first.Budget - second.Budget) < 0.01f &&
                    Mathf.Abs(first.AtA - second.AtA) < 0.01f &&
                    Mathf.Abs(first.AtB - second.AtB) < 0.01f,
                    link.LinkId + " node entry is independent of input enumeration");
                NodePortPacking.Port single = NodePortPacking.Plan(
                    new Dictionary<PhysicalLinkView, int> { { link, 1 } }, area,
                    12f, 6f)[link];
                Check(single.Budget > 0f && Mathf.Abs(single.AtA) < 0.01f &&
                    Mathf.Abs(single.AtB) < 0.01f,
                    link.LinkId + " stays centered when uncontested");
            }
            var points = new Dictionary<NodeView, List<(PhysicalLinkView link, Vector2 point, float width)>>();
            var layouts = new Dictionary<PhysicalLinkView, IdentityPacking.Layout>();
            identity.BeginFrame();
            foreach (PhysicalLinkView link in links)
            {
                NodePortPacking.Port port = ports[link];
                Check(port.Budget > 0f && !float.IsNaN(port.Budget),
                    link.LinkId + " has usable node entry space");
                IdentityPacking.Layout layout = identity.ResolveLayout(link, counts[link],
                    12f, 2.5f, 6f, port);
                Check(layout != null && layout.Lanes.Length == counts[link] &&
                    layout.Stage != IdentityPacking.ReadabilityStage.Unavailable,
                    link.LinkId + " crowded lanes and pills fit");
                layouts.Add(link, layout);
                link.GetTargetSpine(out Vector2 spineStart, out Vector2 spineEnd);
                Vector2 along = (spineEnd - spineStart).normalized;
                Vector2 midpoint = (spineStart + spineEnd) * 0.5f;
                Vector2 labelCenter = area.InverseTransformPoint(link.ResourceLabel.position);
                Check(Mathf.Abs(Vector2.Dot(labelCenter - midpoint, along)) < 0.1f,
                    link.LinkId + " label remains at its physical spine midpoint");
                Vector2 source = area.InverseTransformPoint(link.NodeA.transform.position);
                Vector2 target = area.InverseTransformPoint(link.NodeB.transform.position);
                bool vertical = Mathf.Abs(source.x - target.x) < 0.01f;
                bool horizontal = Mathf.Abs(source.y - target.y) < 0.01f;
                if (vertical || horizontal)
                {
                    Check(Mathf.Abs(port.AtA - port.AtB) < 0.01f &&
                        Mathf.Abs(vertical ? spineStart.x - spineEnd.x :
                            spineStart.y - spineEnd.y) < 0.1f,
                        link.LinkId + " physical cardinal spine remains straight");
                    foreach (IdentityPacking.Lane lane in layout.Lanes)
                        Check(Mathf.Abs(vertical ? lane.Start.x - lane.End.x :
                            lane.Start.y - lane.End.y) < 0.1f,
                            link.LinkId + " logical cardinal lane remains straight");
                }
                if (counts[link] % 2 == 1)
                {
                    IdentityPacking.Lane middleLane = layout.Lanes[counts[link] / 2];
                    Vector2 laneMidpoint = (middleLane.Start + middleLane.End) * 0.5f;
                    Check(Mathf.Abs(Vector2.Dot(laneMidpoint - midpoint,
                        new Vector2(-along.y, along.x))) < 1f,
                        link.LinkId + " physical spine follows its center lane");
                }
                var labelBounds = new IdentityPacking.PillBounds(labelCenter, along,
                    link.ResourceLabel.rect.size);
                foreach (var cycle in layout.Schedule)
                    foreach (IdentityPacking.Placement placement in cycle)
                        Check(!IdentityPacking.Intersects(labelBounds, placement.Bounds,
                            identity.MinimumPillGap),
                            link.LinkId + " label clears adaptive connection pills");
                foreach (NodeView node in new[] { link.NodeA, link.NodeB })
                {
                    if (!points.TryGetValue(node, out List<(PhysicalLinkView link, Vector2 point, float width)> list))
                    {
                        list = new List<(PhysicalLinkView link, Vector2 point, float width)>();
                        points.Add(node, list);
                    }
                    bool isA = node == link.NodeA;
                    NodeView other = isA ? link.NodeB : link.NodeA;
                    Vector2 direction = (Vector2)(area.InverseTransformPoint(other.transform.position) -
                        area.InverseTransformPoint(node.transform.position));
                    direction.Normalize();
                    Rect card = EdgeGeometry.CardRect(node, area);
                    for (int i = 0; i < counts[link]; i++)
                    {
                        float lane = EdgeGeometry.Offset(i, counts[link],
                            layout.LineWidth, layout.LineGap);
                        float offset = isA ? lane + port.AtA : -lane - port.AtB;
                        Vector2 point = EdgeGeometry.CardEdge(card, direction, offset);
                        Check(point.x >= card.xMin - 0.1f && point.x <= card.xMax + 0.1f &&
                            point.y >= card.yMin - 0.1f && point.y <= card.yMax + 0.1f,
                            link.LinkId + " enters " + node.NodeId + " at its card: " +
                            "point=" + point + " card=" + card + " offset=" + offset +
                            " port=" + port.Budget + "/" + port.AtA + "/" + port.AtB);
                        list.Add((link, point, layout.LineWidth));
                    }
                }
            }
            foreach (string nodeId in new[] { "WEB", "API" })
            {
                var list = points.First(p => p.Key.NodeId == nodeId).Value;
                for (int i = 0; i < list.Count; i++)
                    for (int j = i + 1; j < list.Count; j++)
                        if (list[i].link != list[j].link)
                            Check(Vector2.Distance(list[i].point, list[j].point) >=
                                (list[i].width + list[j].width) * 0.5f - 0.1f,
                                nodeId + " different bundles clear at the card edge");
            }
            foreach (PhysicalLinkView first in links)
                foreach (PhysicalLinkView second in links)
                {
                    if (string.CompareOrdinal(first.LinkId, second.LinkId) >= 0) continue;
                    NodeView shared = first.NodeA == second.NodeA || first.NodeA == second.NodeB ?
                        first.NodeA : first.NodeB == second.NodeA || first.NodeB == second.NodeB ?
                        first.NodeB : null;
                    Rect sharedCard = shared != null ? EdgeGeometry.CardRect(shared, area) : new Rect();
                    foreach (IdentityPacking.Lane a in layouts[first].Lanes)
                        foreach (IdentityPacking.Lane b in layouts[second].Lanes)
                            if (TryIntersect(a.Start, a.End, b.Start, b.End, out Vector2 cross))
                                Check(shared != null && sharedCard.Contains(cross),
                                    first.LinkId + " and " + second.LinkId +
                                    " lanes do not cross outside a shared card");
                }
            CheckMixedRoutePills(links, layouts, identity);
            identity.EndFrame();
            foreach (PhysicalLinkView link in links)
                link.ClearAttachmentOffsets(0.01f);
            Debug.Log("ADVANCED PORTS: E1=" + ports[links.First(l => l.LinkId == "E1")].Budget.ToString("0.#") +
                " E2=" + ports[links.First(l => l.LinkId == "E2")].Budget.ToString("0.#") +
                " E4=" + ports[links.First(l => l.LinkId == "E4")].Budget.ToString("0.#") +
                " E6=" + ports[links.First(l => l.LinkId == "E6")].Budget.ToString("0.#"));
        }

        private static void CheckMixedRoutePills(PhysicalLinkView[] links,
            Dictionary<PhysicalLinkView, IdentityPacking.Layout> layouts,
            IdentityPresenter identity)
        {
            PhysicalLinkView link = links.First(l => l.LinkId == "E6");
            IdentityPacking.Layout layout = layouts[link];
            Check(layout.Schedule.Count == 1 && layout.Schedule[0].Count == 7,
                "E6 plans all seven identity pills in one cycle");
            var lines = new List<IdentityPresenter.LiveLine>();
            for (int i = 0; i < layout.Lanes.Length; i++)
            {
                IdentityPacking.Lane lane = layout.Lanes[i];
                bool reverse = i == 0 || i == 3 || i == 4;
                Vector2 start = reverse ? lane.End : lane.Start;
                Vector2 end = reverse ? lane.Start : lane.End;
                lines.Add(new IdentityPresenter.LiveLine("p" + i, start, end,
                    start, end, Color.cyan, 1f, lane.Width));
            }
            identity.PresentEdge(link, lines);
            RectTransform layer = (RectTransform)new SerializedObject(identity)
                .FindProperty("pillLayer").objectReferenceValue;
            int shown = layer.GetComponentsInChildren<IdentityPillView>(true)
                .Count(pill => pill.name.StartsWith("E6 identity ", StringComparison.Ordinal) &&
                    pill.gameObject.activeSelf);
            Check(shown == 7,
                "mixed forward/reverse routes render all seven scheduled E6 pills");
        }

        private static bool TryIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d,
            out Vector2 point)
        {
            Vector2 ab = b - a, cd = d - c;
            float divisor = ab.x * cd.y - ab.y * cd.x;
            point = Vector2.zero;
            if (Mathf.Abs(divisor) < 0.001f) return false;
            Vector2 ac = c - a;
            float t = (ac.x * cd.y - ac.y * cd.x) / divisor;
            float u = (ac.x * ab.y - ac.y * ab.x) / divisor;
            if (t < 0f || t > 1f || u < 0f || u > 1f) return false;
            point = a + ab * t;
            return true;
        }

        private static void CheckThreeAtWeb(PhysicalLinkView[] links, RectTransform area,
            IdentityPresenter identity)
        {
            PhysicalLinkView e1 = links.First(l => l.LinkId == "E1");
            PhysicalLinkView e2 = links.First(l => l.LinkId == "E2");
            Rect web = EdgeGeometry.CardRect(e1.NodeB, area);
            identity.BeginFrame();
            IdentityPacking.Layout fixedE1 = identity.ResolveLayout(e1, 3, 12f, 2.5f, 6f);
            IdentityPacking.Layout fixedE2 = identity.ResolveLayout(e2, 3, 12f, 2.5f, 6f);
            float fixedGap = WebTopGap(fixedE1, fixedE2, web.yMax);
            var counts = new Dictionary<PhysicalLinkView, int> { { e1, 3 }, { e2, 3 } };
            Dictionary<PhysicalLinkView, NodePortPacking.Port> ports =
                NodePortPacking.Plan(counts, area, 12f, 6f);
            IdentityPacking.Layout adaptiveE1 = identity.ResolveLayout(e1, 3, 12f, 2.5f,
                6f, ports[e1]);
            IdentityPacking.Layout adaptiveE2 = identity.ResolveLayout(e2, 3, 12f, 2.5f,
                6f, ports[e2]);
            float adaptiveGap = WebTopGap(adaptiveE1, adaptiveE2, web.yMax);
            Check(adaptiveGap > fixedGap + 20f && adaptiveGap >= identity.MinimumPillGap &&
                Mathf.Abs(adaptiveE1.LineWidth - 12f) < 0.1f &&
                Mathf.Abs(adaptiveE2.LineWidth - 12f) < 0.1f,
                "three shared WEB paths separate without shrinking their 12px beams");
            identity.EndFrame();
            Debug.Log($"WEB THREE: fixed edge gap={fixedGap:0.#}px, " +
                $"adaptive edge gap={adaptiveGap:0.#}px, both beams=12px");
        }

        private static float WebTopGap(IdentityPacking.Layout e1,
            IdentityPacking.Layout e2, float y)
        {
            float right = float.NegativeInfinity, left = float.PositiveInfinity;
            foreach (IdentityPacking.Lane lane in e1.Lanes)
                right = Mathf.Max(right, BeamXAtY(lane, y) + BeamHalfSpanAtY(lane));
            foreach (IdentityPacking.Lane lane in e2.Lanes)
                left = Mathf.Min(left, BeamXAtY(lane, y) - BeamHalfSpanAtY(lane));
            return left - right;
        }

        private static float BeamXAtY(IdentityPacking.Lane lane, float y)
        {
            float fraction = (y - lane.Start.y) / (lane.End.y - lane.Start.y);
            return Mathf.LerpUnclamped(lane.Start.x, lane.End.x, fraction);
        }

        private static float BeamHalfSpanAtY(IdentityPacking.Lane lane) =>
            lane.Width * 0.5f / Mathf.Abs((lane.End - lane.Start).normalized.y);

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
