using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public enum StressTrafficDensity { Light, Typical, Heavy }

    // Opt-in scene component. It builds copies of authored UI, then queues ordinary plans.
    [DefaultExecutionOrder(-100)]
    public sealed class TopologyStressHarness : MonoBehaviour
    {
        [Header("Experiment configuration")]
        [SerializeField] private int nodeCount = 8;
        [SerializeField] private int topologySeed = 1701;
        [SerializeField] private int trafficSeed = 2401;
        [SerializeField] private StressTrafficDensity density = StressTrafficDensity.Typical;
        [Header("Authored scene references")]
        [SerializeField] private RectTransform topologyArea;
        [SerializeField] private NodeView nodeTemplate;
        [SerializeField] private PhysicalLinkView linkTemplate;
        [SerializeField] private SimulationHost host;
        [SerializeField] private ConnectionTrafficDirector director;
        [SerializeField] private TMP_Text statusLabel;

        private StressTopologyGraph graph;
        private NodeView[] views;
        private PhysicalLinkView[] links;
        private System.Random trafficRandom;
        private string[] ids;
        private int idIndex;
        private float nextArrival;

        public int NodeCount => nodeCount;
        public int TopologySeed => topologySeed;
        public int TrafficSeed => trafficSeed;
        public StressTrafficDensity Density => density;
        public StressTopologyGraph Graph => graph;
        public IReadOnlyList<NodeView> GeneratedNodes => views;
        public IReadOnlyList<PhysicalLinkView> GeneratedLinks => links;

        public void Configure(int count, int graphSeed, int arrivalsSeed, StressTrafficDensity traffic)
        {
            nodeCount = count;
            topologySeed = graphSeed;
            trafficSeed = arrivalsSeed;
            density = traffic;
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;
            if (topologyArea == null || nodeTemplate == null || linkTemplate == null ||
                host == null || director == null || statusLabel == null ||
                linkTemplate.ResourceLabel == null)
                throw new InvalidOperationException("Topology stress scene has missing authored references.");
            Canvas.ForceUpdateCanvases();
            graph = StressTopologyGraph.Generate(nodeCount, topologySeed);
            statusLabel.text = $"{nodeCount} NODES  //  {graph.Edges.Count} LINKS  //  " +
                $"{density.ToString().ToUpperInvariant()} TRAFFIC  //  SEED {topologySeed}";
            Vector2[] positions = StressTopologyPlacement.Place(graph, ReferenceTopologySize(),
                topologySeed);
            views = new NodeView[graph.Nodes.Count];
            for (int i = 0; i < views.Length; i++)
            {
                NodeView view = Instantiate(nodeTemplate, nodeTemplate.transform.parent, false);
                view.gameObject.name = "Stress " + graph.Nodes[i].Id;
                view.gameObject.SetActive(true);
                view.ConfigureStressNode(graph.Nodes[i].Id, graph.Nodes[i].Role);
                ((RectTransform)view.transform).anchoredPosition = positions[i];
                views[i] = view;
            }
            links = new PhysicalLinkView[graph.Edges.Count];
            for (int i = 0; i < links.Length; i++)
            {
                StressTopologyGraph.Edge edge = graph.Edges[i];
                PhysicalLinkView link = Instantiate(linkTemplate,
                    linkTemplate.transform.parent, false);
                RectTransform label = Instantiate(linkTemplate.ResourceLabel,
                    linkTemplate.ResourceLabel.parent, false);
                link.gameObject.name = "Stress physical link " + (i + 1);
                label.gameObject.name = "Stress link label " + (i + 1);
                link.gameObject.SetActive(true);
                label.gameObject.SetActive(true);
                link.transform.SetAsFirstSibling();
                link.ConfigureStressLink("E" + (i + 1), views[edge.A], views[edge.B],
                    topologyArea, label, label.GetComponentInChildren<TMP_Text>(true));
                links[i] = link;
            }
            var resources = new NodeResources[views.Length];
            for (int i = 0; i < views.Length; i++)
                resources[i] = new NodeResources(views[i], 10f, graph.Nodes[i].External ? 0f : 1f);
            var references = new PhysicalLinkReference[links.Length];
            for (int i = 0; i < links.Length; i++)
                references[i] = new PhysicalLinkReference(links[i]);
            host.ConfigureStressTopology(resources, references);
            director.AutoGenerationEnabled = false;
            trafficRandom = new System.Random(trafficSeed);
            ids = new string[260];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = ((char)('a' + i / 10)).ToString() + (i % 10);
            for (int i = ids.Length - 1; i > 0; i--)
            {
                int swap = trafficRandom.Next(i + 1);
                (ids[i], ids[swap]) = (ids[swap], ids[i]);
            }
            nextArrival = 2f;
        }

        private Vector2 ReferenceTopologySize()
        {
            // In batch Play Mode the Game View can report a zero canvas in Awake.
            // Resolve the authored 16:9 anchors from the CanvasScaler reference size.
            CanvasScaler scaler = topologyArea.GetComponentInParent<CanvasScaler>();
            RectTransform panel = topologyArea.parent as RectTransform;
            if (scaler == null || panel == null)
                throw new InvalidOperationException("Stress topology needs the authored scaled canvas.");
            RectTransform canvas = scaler.GetComponent<RectTransform>();
            Vector2 reference = scaler.referenceResolution;
            Vector2 panelSize = new Vector2(
                reference.x * (panel.anchorMax.x - panel.anchorMin.x) + panel.sizeDelta.x,
                reference.y * (panel.anchorMax.y - panel.anchorMin.y) + panel.sizeDelta.y);
            Vector2 areaSize = new Vector2(
                panelSize.x * (topologyArea.anchorMax.x - topologyArea.anchorMin.x) +
                    topologyArea.sizeDelta.x,
                panelSize.y * (topologyArea.anchorMax.y - topologyArea.anchorMin.y) +
                    topologyArea.sizeDelta.y);
            if (canvas == null || reference.x < 1800f || reference.y < 1000f ||
                areaSize.x < 1000f || areaSize.y < 700f)
                throw new InvalidOperationException("Stress scene expects the authored 1920x1080 layout.");
            return areaSize;
        }

        private void FixedUpdate() => StepTraffic();

        // Batch comparison advances this before each explicit host step.
        public void StepTraffic()
        {
            if (graph == null || host == null || director == null) return;
            float time = host.SimulationTimeSeconds;
            float interval = density == StressTrafficDensity.Light ? 12f :
                density == StressTrafficDensity.Typical ? 5f : 2.5f;
            while (time + 0.0001f >= nextArrival && idIndex < ids.Length)
            {
                QueueArrival(nextArrival);
                nextArrival += interval * (0.8f + (float)trafficRandom.NextDouble() * 0.4f);
            }
        }

        private void QueueArrival(float scheduledTime)
        {
            var origins = new List<int>();
            var targets = new List<int>();
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i].External) origins.Add(i);
                else if (graph.Nodes[i].Role != "TRANSIT") targets.Add(i);
            }
            int origin = origins[trafficRandom.Next(origins.Count)];
            int target = targets[trafficRandom.Next(targets.Count)];
            NodeView[] route = FindRoute(origin, target);
            OperationKind operation = (OperationKind)trafficRandom.Next(3);
            float load = 1f + (float)trafficRandom.NextDouble() * 2f;
            float duration = 45f + (float)trafficRandom.NextDouble() * 45f;
            var stages = new List<OperationStage>
            {
                new OperationStage(operation, load * duration, load,
                    "Stress " + operation.ToString().ToLowerInvariant())
            };
            if (trafficRandom.Next(5) == 0)
                stages.Add(new OperationStage(OperationKind.Write, load * 25f, load,
                    "Stress follow-up write"));
            var plan = new ConnectionSpawnPlan(scheduledTime, ids[idIndex++], route,
                "topology-stress", "Routine service activity", stages.ToArray());
            director.QueueSpawnPlan(plan, "topology-stress");
        }

        private NodeView[] FindRoute(int origin, int target)
        {
            var queue = new Queue<int>();
            var parent = new int[graph.Nodes.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = -1;
            queue.Enqueue(origin);
            parent[origin] = origin;
            while (queue.Count > 0 && parent[target] < 0)
            {
                int current = queue.Dequeue();
                foreach (StressTopologyGraph.Edge edge in graph.Edges)
                {
                    int next = edge.A == current ? edge.B : edge.B == current ? edge.A : -1;
                    if (next < 0 || parent[next] >= 0) continue;
                    parent[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (parent[target] < 0) throw new InvalidOperationException("Stress graph disconnected.");
            var result = new List<NodeView>();
            for (int at = target; at != origin; at = parent[at]) result.Add(views[at]);
            result.Add(views[origin]);
            result.Reverse();
            return result.ToArray();
        }
    }

    // Experimental tree only. This is not the shared Uptime topology grammar.
    public sealed class StressTopologyGraph
    {
        public sealed class Node
        {
            public string Id;
            public string Role;
            public bool External;
            public int Parent;
            public int Depth;
        }

        public readonly struct Edge
        {
            public readonly int A;
            public readonly int B;
            public Edge(int a, int b) { A = a; B = b; }
        }

        public readonly List<Node> Nodes = new List<Node>();
        public readonly List<Edge> Edges = new List<Edge>();

        public static StressTopologyGraph Generate(int count, int seed)
        {
            if (count != 4 && count != 8 && count != 12 && count != 16 &&
                count != 20 && count != 24 && count != 28)
                throw new ArgumentOutOfRangeException(nameof(count), "Use a requested 4-node tier.");
            var result = new StressTopologyGraph();
            var random = new System.Random(seed);
            int externalCount = count >= 24 ? 4 : count >= 16 ? 3 : count >= 8 ? 2 : 1;
            int internalCount = count - externalCount;
            var childCounts = new int[internalCount];
            for (int i = 0; i < internalCount; i++)
            {
                int parent = i == 0 ? -1 : (i - 1) / 2;
                if (i > 3 && random.Next(4) == 0)
                {
                    int alternate = Math.Max(0, parent - 1);
                    if (childCounts[alternate] < 3) parent = alternate;
                }
                int depth = parent < 0 ? 0 : result.Nodes[parent].Depth + 1;
                result.Nodes.Add(new Node
                {
                    Id = "N" + (i + 1).ToString("00"),
                    Role = i == 0 ? "TRANSIT" : i % 5 == 4 ? "CORE" : "SERVICE",
                    Parent = parent, Depth = depth
                });
                if (parent >= 0)
                {
                    childCounts[parent]++;
                    result.Edges.Add(new Edge(parent, i));
                }
            }
            for (int i = 0; i < externalCount; i++)
            {
                int parent = Math.Min(internalCount - 1, i == 0 ? 0 :
                    i == 1 ? Math.Min(2, internalCount - 1) :
                    i == 2 ? Math.Min(4, internalCount - 1) : internalCount - 1);
                int index = result.Nodes.Count;
                result.Nodes.Add(new Node
                {
                    Id = "X" + (i + 1), Role = "ENTRY", External = true,
                    Parent = parent, Depth = result.Nodes[parent].Depth + 1
                });
                result.Edges.Add(new Edge(parent, index));
            }
            return result;
        }
    }

    // Intentionally simple depth columns and perimeter entries. Diagnostics expose its failures.
    public static class StressTopologyPlacement
    {
        public static Vector2[] Place(StressTopologyGraph graph, Vector2 areaSize, int seed)
        {
            var positions = new Vector2[graph.Nodes.Count];
            var levels = new SortedDictionary<int, List<int>>();
            int maxDepth = 0;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i].External) continue;
                int depth = graph.Nodes[i].Depth;
                if (!levels.TryGetValue(depth, out List<int> level))
                    levels.Add(depth, level = new List<int>());
                level.Add(i);
                maxDepth = Math.Max(maxDepth, depth);
            }
            var random = new System.Random(unchecked(seed ^ 0x3F519));
            foreach (KeyValuePair<int, List<int>> level in levels)
            {
                float x = Mathf.Lerp(-areaSize.x * 0.5f + 155f,
                    areaSize.x * 0.5f - 155f,
                    maxDepth == 0 ? 0.5f : (float)level.Key / maxDepth);
                for (int row = 0; row < level.Value.Count; row++)
                {
                    float y = (0.5f - (row + 0.5f) / level.Value.Count) *
                        (areaSize.y - 110f);
                    float jitter = level.Value.Count == 1 ? 0f :
                        ((float)random.NextDouble() - 0.5f) * 20f;
                    positions[level.Value[row]] = new Vector2(x, y + jitter);
                }
            }
            int externalIndex = 0;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (!graph.Nodes[i].External) continue;
                float halfW = areaSize.x * 0.5f - 148f;
                float halfH = areaSize.y * 0.5f - 52f;
                positions[i] = externalIndex == 0 ? new Vector2(-halfW, halfH * 0.75f) :
                    externalIndex == 1 ? new Vector2(-halfW * 0.45f, halfH) :
                    externalIndex == 2 ? new Vector2(halfW * 0.45f, -halfH) :
                    new Vector2(halfW, halfH * 0.75f);
                externalIndex++;
            }
            return positions;
        }
    }
}
