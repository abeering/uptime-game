using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class ConnectionOperationProfile
    {
        public OperationKind operation;
        public string activityDescription;
        [Min(0.01f)] public float minimumLoad = 1f;
        [Min(0.01f)] public float maximumLoad = 3f;
        [Min(0.01f)] public float minimumDuration = 40f;
        [Min(0.01f)] public float maximumDuration = 75f;
    }

    [Serializable]
    public sealed class ConnectionTrafficProfile
    {
        public string id;
        public string claimedPurpose;
        public string[] eligibleTargetIds;
        [Min(0f)] public float ambientWeight = 1f;
        public ConnectionOperationProfile[] operations;
    }

    // Owns only generation and scheduling. SimulationHost owns every live connection.
    public sealed class ConnectionTrafficDirector : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private bool autoGenerationEnabled = true;
        [SerializeField] private int seed = 1701;
        [SerializeField, Min(0f)] private float initialGraceSeconds = 3f;
        [SerializeField, Min(0.01f)] private float spawnIntervalSeconds = 13.5f;
        [SerializeField, Min(0f)] private float spawnJitterSeconds = 1.5f;
        [SerializeField] private string ambientOriginId = "EXT1";
        [SerializeField] private ConnectionTrafficProfile[] profiles;

        private sealed class Queued
        {
            public ConnectionSpawnPlan Plan;
            public long Order;
        }
        private readonly List<Queued> queue = new List<Queued>();
        private readonly HashSet<string> reservedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private System.Random random;
        private float nextAmbientAt;
        private int nextId;
        private long nextOrder;

        public bool AutoGenerationEnabled { get => autoGenerationEnabled; set => autoGenerationEnabled = value; }
        public int Seed { get => seed; set => seed = value; }
        public IReadOnlyList<ConnectionTrafficProfile> Profiles => profiles;

        public void ResetDirector()
        {
            random = new System.Random(seed);
            queue.Clear();
            reservedIds.Clear();
            nextId = 1;
            nextOrder = 0;
            nextAmbientAt = initialGraceSeconds;
            if (profiles == null || profiles.Length == 0)
                profiles = DefaultProfiles();
        }

        public void QueueSpawnPlan(ConnectionSpawnPlan plan)
        {
            if (plan == null || string.IsNullOrWhiteSpace(plan.ConnectionId) ||
                !reservedIds.Add(plan.ConnectionId))
                throw new InvalidOperationException("Spawn plans require a unique, nonempty connection ID.");
            try { host.ValidatePlan(plan); }
            catch { reservedIds.Remove(plan.ConnectionId); throw; }
            queue.Add(new Queued { Plan = plan, Order = nextOrder++ });
        }

        public void Tick(float simulationTime)
        {
            if (random == null) ResetDirector();
            if (autoGenerationEnabled)
            {
                while (nextAmbientAt <= simulationTime + 0.00001f)
                {
                    ConnectionSpawnPlan plan = GeneratePlan(nextAmbientAt);
                    if (plan != null) QueueSpawnPlan(plan);
                    nextAmbientAt += Mathf.Max(0.01f, spawnIntervalSeconds +
                        Range(-spawnJitterSeconds, spawnJitterSeconds));
                }
            }
            queue.Sort((a, b) =>
            {
                int time = a.Plan.ScheduledSpawnTime.CompareTo(b.Plan.ScheduledSpawnTime);
                return time != 0 ? time : a.Order.CompareTo(b.Order);
            });
            while (queue.Count > 0 && queue[0].Plan.ScheduledSpawnTime <= simulationTime + 0.00001f)
            {
                ConnectionSpawnPlan plan = queue[0].Plan;
                queue.RemoveAt(0);
                host.RegisterConnection(LogicalConnectionDefinition.FromPlan(plan));
            }
        }

        // Opt-in mechanical witness. Normal ambient selection excludes profiles with weight zero.
        public ConnectionSpawnPlan QueueHighDemandWitness(string targetId = "DB", bool obvious = true)
        {
            if (random == null) ResetDirector();
            NodeView origin = FindNode(ambientOriginId);
            NodeView target = FindNode(targetId);
            NodeView[] route = FindRoute(origin, target);
            float load = obvious ? Range(10f, 12f) : Range(7f, 9f);
            var plan = new ConnectionSpawnPlan(host.SimulationTimeSeconds, NewId(), route,
                "high-demand-witness", "High Demand Test",
                new[] { new OperationStage(OperationKind.Read, load * 50f, load, "High demand test") });
            QueueSpawnPlan(plan);
            return plan;
        }

        public ConnectionSpawnPlan GeneratePlan(float atTime)
        {
            if (random == null) ResetDirector();
            float total = 0f;
            foreach (ConnectionTrafficProfile profile in profiles)
                if (profile != null) total += Mathf.Max(0f, profile.ambientWeight);
            if (total <= 0f) return null;
            float choice = (float)random.NextDouble() * total;
            ConnectionTrafficProfile selected = null;
            foreach (ConnectionTrafficProfile profile in profiles)
            {
                if (profile == null || profile.ambientWeight <= 0f) continue;
                selected = profile;
                choice -= profile.ambientWeight;
                if (choice < 0f) break;
            }
            NodeView origin = FindNode(ambientOriginId);
            string targetId = selected.eligibleTargetIds[random.Next(selected.eligibleTargetIds.Length)];
            NodeView[] route = FindRoute(origin, FindNode(targetId));
            var stages = new OperationStage[selected.operations.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                ConnectionOperationProfile spec = selected.operations[i];
                float load = Range(spec.minimumLoad, spec.maximumLoad);
                float duration = Range(spec.minimumDuration, spec.maximumDuration);
                stages[i] = new OperationStage(spec.operation, load * duration, load,
                    spec.activityDescription);
            }
            return new ConnectionSpawnPlan(atTime, NewId(), route, selected.id,
                selected.claimedPurpose, stages);
        }

        private string NewId()
        {
            string id;
            do { id = "c" + nextId++; } while (reservedIds.Contains(id) || host.HasEverUsedId(id));
            return id;
        }
        private float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
        private NodeView FindNode(string id)
        {
            foreach (NodeResources node in host.Nodes)
                if (node.View != null && string.Equals(node.View.NodeId, id, StringComparison.OrdinalIgnoreCase))
                    return node.View;
            throw new InvalidOperationException("Unknown traffic node: " + id);
        }
        private NodeView[] FindRoute(NodeView origin, NodeView target)
        {
            if (origin == null || target == null || origin == target)
                throw new InvalidOperationException("Traffic requires distinct configured origin and target nodes.");
            var paths = new Queue<List<NodeView>>();
            paths.Enqueue(new List<NodeView> { origin });
            var visited = new HashSet<NodeView> { origin };
            while (paths.Count > 0)
            {
                List<NodeView> path = paths.Dequeue();
                NodeView tail = path[path.Count - 1];
                foreach (PhysicalLinkReference link in host.PhysicalLinks)
                {
                    NodeView adjacent = link.View.NodeA == tail ? link.View.NodeB :
                        link.View.NodeB == tail ? link.View.NodeA : null;
                    if (adjacent == null || !visited.Add(adjacent)) continue;
                    var extended = new List<NodeView>(path) { adjacent };
                    if (adjacent == target) return extended.ToArray();
                    paths.Enqueue(extended);
                }
            }
            throw new InvalidOperationException("No route to traffic target " + target.NodeId);
        }

        private static ConnectionTrafficProfile[] DefaultProfiles() => new[]
        {
            Profile("retrieval", "Record Retrieval", "DB", 1f,
                Stage(OperationKind.Read, "Reading records", 1f, 2.5f, 40f, 70f)),
            Profile("indexing", "Record Indexing", "DB", 0.8f,
                Stage(OperationKind.Write, "Updating the DB index", 1.5f, 3f, 20f, 35f),
                Stage(OperationKind.Read, "Reading the updated index", 1f, 2.5f, 20f, 35f)),
            Profile("auth", "Customer Authentications", "AUTH", 1f,
                Stage(OperationKind.Execute, "Processing authentications", 1f, 2.5f, 40f, 70f)),
            Profile("updates", "Application Updates", "API", 0.8f,
                Stage(OperationKind.Write, "Applying updates", 1.5f, 3f, 20f, 35f),
                Stage(OperationKind.Read, "Verifying updates", 1f, 2.5f, 20f, 35f)),
            Profile("quick-lookup", "Quick Record Lookup", "DB", 0.35f,
                Stage(OperationKind.Read, "Looking up a record", 0.5f, 1.5f, 4f, 8f))
        };
        private static ConnectionTrafficProfile Profile(string id, string purpose, string target,
            float weight, params ConnectionOperationProfile[] stages) => new ConnectionTrafficProfile
            { id = id, claimedPurpose = purpose, eligibleTargetIds = new[] { target },
                ambientWeight = weight, operations = stages };
        private static ConnectionOperationProfile Stage(OperationKind kind, string description,
            float lowLoad, float highLoad, float lowSeconds, float highSeconds) => new ConnectionOperationProfile
            { operation = kind, activityDescription = description, minimumLoad = lowLoad,
                maximumLoad = highLoad, minimumDuration = lowSeconds, maximumDuration = highSeconds };
    }
}
