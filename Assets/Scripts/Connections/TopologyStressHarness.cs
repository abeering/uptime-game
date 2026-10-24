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
}
