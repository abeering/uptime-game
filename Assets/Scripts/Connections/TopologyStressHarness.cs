using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public enum StressTrafficDensity { Light, Typical, Heavy }
    public enum StressPlacementMode { LegacyV0, OptimizedV1 }

    // Opt-in scene component. It builds copies of authored UI, then queues ordinary plans.
    [DefaultExecutionOrder(-100)]
    public sealed class TopologyStressHarness : MonoBehaviour
    {
        [Header("Experiment configuration")]
        [SerializeField] private int nodeCount = 8;
        [SerializeField] private int topologySeed = 1701;
        [SerializeField] private int trafficSeed = 2401;
        [SerializeField] private StressTrafficDensity density = StressTrafficDensity.Typical;
        [SerializeField] private StressPlacementMode placementMode = StressPlacementMode.OptimizedV1;
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
        public StressPlacementMode PlacementMode => placementMode;
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

        public void SetPlacementMode(StressPlacementMode mode) => placementMode = mode;

        private void Awake()
        {
            if (!Application.isPlaying) return;
            if (topologyArea == null || nodeTemplate == null || linkTemplate == null ||
                host == null || director == null || statusLabel == null ||
                linkTemplate.ResourceLabel == null)
                throw new InvalidOperationException("Topology stress scene has missing authored references.");
            Canvas.ForceUpdateCanvases();
            graph = StressTopologyGraph.Generate(nodeCount, topologySeed);
            string placementTag = placementMode == StressPlacementMode.LegacyV0 ||
                nodeCount == 4 ? "V0" : "V1";
            statusLabel.text = $"{nodeCount} NODES  //  {graph.Edges.Count} LINKS  //  " +
                $"{density.ToString().ToUpperInvariant()} TRAFFIC  //  SEED {topologySeed}  //  " +
                placementTag;
            Vector2 areaSize = ReferenceTopologySize();
            Vector2[] positions = placementMode == StressPlacementMode.LegacyV0 ?
                StressTopologyPlacement.Place(graph, areaSize, topologySeed) :
                TopologyStressPlacementV1.Place(graph, areaSize,
                    ((RectTransform)nodeTemplate.transform).rect.size, topologySeed);
            views = new NodeView[graph.Nodes.Count];
            for (int i = 0; i < views.Length; i++)
            {
                NodeView view = Instantiate(nodeTemplate, nodeTemplate.transform.parent, false);
                view.gameObject.name = "Stress " + graph.Nodes[i].Id;
                view.gameObject.SetActive(true);
                view.ConfigureStressNode(graph.Nodes[i].Id, graph.Nodes[i].Role);
                RectTransform card = (RectTransform)view.transform;
                if (placementMode == StressPlacementMode.OptimizedV1 && nodeCount != 4)
                {
                    // The authored template is anchored at y=0.51. V1 slots are
                    // measured from the center of the topology rectangle.
                    card.anchorMin = new Vector2(0.5f, 0.5f);
                    card.anchorMax = card.anchorMin;
                }
                card.anchoredPosition = positions[i];
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

    // Geometry of the experimental full-size cards. No presentation solver runs in the search.
    public static class TopologyStressGeometry
    {
        public sealed class Measurements
        {
            public int NodeOverlaps;
            public int NodeOverflow;
            public int LinksThroughNodes;
            public int LinkCrossings;
            public int Below120;
            public int Below180;
            public int Below60;
            public int Deficit180;
            public int CrowdedApproaches;
            public float ShortestVisible = float.PositiveInfinity;
            public float[] VisibleLengths;
        }

        public static Rect Card(Vector2 center, Vector2 size)
            => new Rect(center - size * 0.5f, size);

        public static float VisibleLength(Vector2 a, Vector2 b, Vector2 cardSize)
        {
            Vector2 direction = (b - a).normalized;
            return Vector2.Distance(
                EdgeGeometry.CardEdge(Card(a, cardSize), direction, 0f),
                EdgeGeometry.CardEdge(Card(b, cardSize), -direction, 0f));
        }

        public static Measurements Measure(StressTopologyGraph graph, Vector2[] positions,
            Vector2 areaSize, Vector2 cardSize)
        {
            if (graph == null || positions == null || positions.Length != graph.Nodes.Count ||
                areaSize.x <= 0f || areaSize.y <= 0f || cardSize.x <= 0f || cardSize.y <= 0f)
                throw new ArgumentException("Invalid stress topology geometry.");
            int count = positions.Length;
            var result = new Measurements { VisibleLengths = new float[graph.Edges.Count] };
            var cards = new Rect[count];
            Rect area = Card(Vector2.zero, areaSize);
            for (int i = 0; i < count; i++)
            {
                Vector2 point = positions[i];
                if (float.IsNaN(point.x) || float.IsNaN(point.y) ||
                    float.IsInfinity(point.x) || float.IsInfinity(point.y))
                    throw new ArgumentException("Nonfinite stress topology position.");
                cards[i] = Card(point, cardSize);
                if (!area.Contains(cards[i].min) || !area.Contains(cards[i].max))
                    result.NodeOverflow++;
                for (int j = 0; j < i; j++)
                    if (cards[i].Overlaps(cards[j])) result.NodeOverlaps++;
            }
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                StressTopologyGraph.Edge edge = graph.Edges[i];
                Vector2 a = positions[edge.A], b = positions[edge.B];
                float visible = VisibleLength(a, b, cardSize);
                result.VisibleLengths[i] = visible;
                result.ShortestVisible = Mathf.Min(result.ShortestVisible, visible);
                if (visible < 60f) result.Below60++;
                if (visible < 120f) result.Below120++;
                if (visible < 180f) result.Below180++;
                result.Deficit180 += Mathf.RoundToInt(Mathf.Max(0f, 180f - visible));
                for (int node = 0; node < count; node++)
                    if (node != edge.A && node != edge.B &&
                        SegmentIntersectsRect(a, b, cards[node]))
                        result.LinksThroughNodes++;
                for (int j = 0; j < i; j++)
                {
                    StressTopologyGraph.Edge other = graph.Edges[j];
                    if (edge.A == other.A || edge.A == other.B ||
                        edge.B == other.A || edge.B == other.B) continue;
                    if (SegmentsCross(a, b, positions[other.A], positions[other.B]))
                        result.LinkCrossings++;
                }
            }
            for (int node = 0; node < count; node++)
            {
                var directions = new List<Vector2>();
                foreach (StressTopologyGraph.Edge edge in graph.Edges)
                {
                    int other = edge.A == node ? edge.B : edge.B == node ? edge.A : -1;
                    if (other >= 0) directions.Add((positions[other] - positions[node]).normalized);
                }
                for (int i = 0; i < directions.Count; i++)
                    for (int j = 0; j < i; j++)
                        if (Vector2.Dot(directions[i], directions[j]) > 0.82f)
                            result.CrowdedApproaches++;
            }
            return result;
        }

        public static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float first = Cross(b - a, c - a), second = Cross(b - a, d - a);
            float third = Cross(d - c, a - c), fourth = Cross(d - c, b - c);
            return first * second < -0.001f && third * fourth < -0.001f;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        public static bool SegmentIntersectsRect(Vector2 a, Vector2 b, Rect rect)
        {
            float low = 0f, high = 1f;
            Vector2 delta = b - a;
            return Clip(-delta.x, a.x - rect.xMin, ref low, ref high) &&
                   Clip(delta.x, rect.xMax - a.x, ref low, ref high) &&
                   Clip(-delta.y, a.y - rect.yMin, ref low, ref high) &&
                   Clip(delta.y, rect.yMax - a.y, ref low, ref high) && low <= high;
        }

        private static bool Clip(float p, float q, ref float low, ref float high)
        {
            if (Mathf.Abs(p) < 0.00001f) return q >= 0f;
            float t = q / p;
            if (p < 0f) low = Mathf.Max(low, t);
            else high = Mathf.Min(high, t);
            return low <= high;
        }
    }

    // Bounded multistart assignment search for the fixed experimental trees.
    public static class TopologyStressPlacementV1
    {
        public const int StartsPerGrid = 3;
        public const int SwapPasses = 3;

        private readonly struct Score : IComparable<Score>
        {
            private readonly int overlaps, overflow, obstructions, severe, short120,
                crossings, short180, deficit, crowding, ingress, totalLength, balance;

            public Score(TopologyStressGeometry.Measurements geometry,
                StressTopologyGraph graph, Vector2[] positions, Vector2 areaSize)
            {
                overlaps = geometry.NodeOverlaps;
                overflow = geometry.NodeOverflow;
                obstructions = geometry.LinksThroughNodes;
                severe = geometry.Below60;
                short120 = geometry.Below120;
                crossings = geometry.LinkCrossings;
                short180 = geometry.Below180;
                deficit = geometry.Deficit180;
                crowding = geometry.CrowdedApproaches;
                float ingressCost = 0f;
                for (int i = 0; i < graph.Nodes.Count; i++)
                    if (graph.Nodes[i].External)
                        for (int j = 0; j < i; j++)
                            if (graph.Nodes[j].External)
                                ingressCost += Mathf.Max(0f, areaSize.x * 0.7f -
                                    Vector2.Distance(positions[i], positions[j]));
                ingress = Mathf.RoundToInt(ingressCost);
                float length = 0f;
                foreach (StressTopologyGraph.Edge edge in graph.Edges)
                    length += Vector2.Distance(positions[edge.A], positions[edge.B]);
                totalLength = Mathf.RoundToInt(length);
                Vector2 middle = Vector2.zero;
                foreach (Vector2 point in positions) middle += point;
                balance = Mathf.RoundToInt((middle / positions.Length).magnitude);
            }

            public int CompareTo(Score other)
            {
                int result = overlaps.CompareTo(other.overlaps);
                if (result != 0) return result;
                result = overflow.CompareTo(other.overflow);
                if (result != 0) return result;
                result = obstructions.CompareTo(other.obstructions);
                if (result != 0) return result;
                result = severe.CompareTo(other.severe);
                if (result != 0) return result;
                result = short120.CompareTo(other.short120);
                if (result != 0) return result;
                result = crossings.CompareTo(other.crossings);
                if (result != 0) return result;
                result = short180.CompareTo(other.short180);
                if (result != 0) return result;
                result = deficit.CompareTo(other.deficit);
                if (result != 0) return result;
                result = crowding.CompareTo(other.crowding);
                if (result != 0) return result;
                result = ingress.CompareTo(other.ingress);
                if (result != 0) return result;
                result = totalLength.CompareTo(other.totalLength);
                return result != 0 ? result : balance.CompareTo(other.balance);
            }
        }

        public static Vector2[] Place(StressTopologyGraph graph, Vector2 areaSize,
            Vector2 cardSize, int seed)
        {
            if (graph == null || graph.Nodes.Count < 2 || areaSize.x <= cardSize.x ||
                areaSize.y <= cardSize.y || cardSize.x <= 0f || cardSize.y <= 0f)
                throw new ArgumentException("Invalid V1 placement input.");
            // At four nodes the geometry-only search turns a second busy link
            // toward the narrower diagonal attachment budget. Keep that control.
            if (graph.Nodes.Count == 4)
                return StressTopologyPlacement.Place(graph, areaSize, seed);
            Vector2[] best = null;
            Score bestScore = default;
            int startsPerGrid = graph.Nodes.Count <= 12 ? StartsPerGrid : 2;
            int swapPasses = graph.Nodes.Count <= 12 ? SwapPasses : 1;
            int maxColumns = Mathf.Min(4, Mathf.FloorToInt((areaSize.x - 16f) / cardSize.x));
            int maxRows = Mathf.FloorToInt((areaSize.y - 16f) / cardSize.y);
            for (int columns = 2; columns <= maxColumns; columns++)
            {
                int rows = Mathf.CeilToInt((float)graph.Nodes.Count / columns);
                if (rows > maxRows) continue;
                for (int start = 0; start < startsPerGrid; start++)
                {
                    Vector2[] slots = Slots(graph.Nodes.Count, columns, rows,
                        areaSize, cardSize);
                    int mixedSeed = unchecked(seed * 1103515245 + columns * 7919 + start * 104729);
                    var random = new System.Random(mixedSeed);
                    Vector2[] positions = InitialAssignment(graph, slots, areaSize,
                        cardSize, start, random);
                    Score score = Evaluate(graph, positions, areaSize, cardSize);
                    for (int pass = 0; pass < swapPasses; pass++)
                    {
                        int chosenA = -1, chosenB = -1;
                        Score improved = score;
                        for (int a = 0; a < positions.Length; a++)
                            for (int b = a + 1; b < positions.Length; b++)
                            {
                                Swap(positions, a, b);
                                Score candidate = Evaluate(graph, positions, areaSize, cardSize);
                                Swap(positions, a, b);
                                if (candidate.CompareTo(improved) >= 0) continue;
                                improved = candidate;
                                chosenA = a;
                                chosenB = b;
                            }
                        if (chosenA < 0) break;
                        Swap(positions, chosenA, chosenB);
                        score = improved;
                    }
                    if (best == null || score.CompareTo(bestScore) < 0)
                    {
                        best = positions;
                        bestScore = score;
                    }
                }
            }
            if (best == null) throw new InvalidOperationException("No V1 grid fits the node cards.");
            return best;
        }

        private static Vector2[] Slots(int count, int columns, int rows,
            Vector2 areaSize, Vector2 cardSize)
        {
            var slots = new Vector2[count];
            float extentX = areaSize.x * 0.5f - cardSize.x * 0.5f - 8f;
            float extentY = areaSize.y * 0.5f - cardSize.y * 0.5f - 8f;
            for (int i = 0; i < count; i++)
            {
                int row = i / columns, column = i % columns;
                int inRow = Mathf.Min(columns, count - row * columns);
                float x = inRow == 1 ? 0f : Mathf.Lerp(-extentX, extentX,
                    (float)column / (inRow - 1));
                float y = rows == 1 ? 0f : Mathf.Lerp(extentY, -extentY,
                    (float)row / (rows - 1));
                slots[i] = new Vector2(x, y);
            }
            return slots;
        }

        private static Vector2[] InitialAssignment(StressTopologyGraph graph,
            Vector2[] slots, Vector2 areaSize, Vector2 cardSize, int start,
            System.Random random)
        {
            var positions = new Vector2[graph.Nodes.Count];
            var taken = new bool[slots.Length];
            Vector2[] ingress = { new Vector2(-1f, 1f), new Vector2(1f, -1f),
                new Vector2(-1f, -1f), new Vector2(1f, 1f) };
            int external = 0;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (!graph.Nodes[i].External) continue;
                Vector2 target = ingress[(external + start) % ingress.Length];
                int slot = NearestFree(slots, taken,
                    Vector2.Scale(target, areaSize * 0.5f));
                positions[i] = slots[slot];
                taken[slot] = true;
                external++;
            }
            int root = NearestFree(slots, taken, Vector2.zero);
            positions[0] = slots[root];
            taken[root] = true;
            for (int i = 1; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i].External) continue;
                int parent = graph.Nodes[i].Parent;
                int chosen = -1;
                float best = float.PositiveInfinity;
                for (int slot = 0; slot < slots.Length; slot++)
                {
                    if (taken[slot]) continue;
                    float visible = TopologyStressGeometry.VisibleLength(
                        positions[parent], slots[slot], cardSize);
                    float cost = Mathf.Abs(visible - 240f) +
                        5f * Mathf.Max(0f, 120f - visible) +
                        (float)random.NextDouble() * 110f;
                    if (cost >= best) continue;
                    best = cost;
                    chosen = slot;
                }
                positions[i] = slots[chosen];
                taken[chosen] = true;
            }
            return positions;
        }

        private static int NearestFree(Vector2[] slots, bool[] taken, Vector2 target)
        {
            int chosen = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < slots.Length; i++)
            {
                float distance = (slots[i] - target).sqrMagnitude;
                if (taken[i] || distance >= best) continue;
                chosen = i;
                best = distance;
            }
            return chosen;
        }

        private static Score Evaluate(StressTopologyGraph graph, Vector2[] positions,
            Vector2 areaSize, Vector2 cardSize)
            => new Score(TopologyStressGeometry.Measure(graph, positions, areaSize,
                cardSize), graph, positions, areaSize);

        private static void Swap(Vector2[] positions, int a, int b)
            => (positions[a], positions[b]) = (positions[b], positions[a]);
    }
}
