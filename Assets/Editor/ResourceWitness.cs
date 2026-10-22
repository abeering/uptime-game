using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class ResourceWitness
    {
        private const float Tolerance = 0.001f;

        [MenuItem("Tools/Connections/Run Resource Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            Check(host != null, "simulation host");
            var c1Definition = host.Definitions.First(c => c.ConnectionId == "c1");
            var dbRequirement = c1Definition.Route.Last();
            float originalCpu = GetFloat(dbRequirement, "cpuUnitsPerWork");
            float originalDisk = GetFloat(dbRequirement, "diskIoMBPerWork");
            float originalBandwidth = GetFloat(c1Definition, "bandwidthMBPerWork");
            try
            {
                host.ResetSimulation();
                var c1 = host.RuntimeConnections.First(c => c.ConnectionId == "c1");
                var c2 = host.RuntimeConnections.First(c => c.ConnectionId == "c2");
                Check(c1Definition.BandwidthMBPerWork == 1f &&
                    c2.Definition.BandwidthMBPerWork == 1f, "authored 1 MB/work migration");
                host.StepSimulation(0.1f);
                Near(c1.ActualWorkRate, 35f, "uncontended work rate");
                Near(Link(host, "E1").BandwidthUsageMBps, 35f, "uncontended link MB/s");
                CheckCapacity(host);

                int steps = 0;
                while (c2.IsPending && steps++ < 50)
                    host.StepSimulation(0.1f);
                Check(c2.IsActive, "delayed c2 establishment");
                Near(c1.ActualWorkRate, 25f, "shared c1 work units/s");
                Near(c2.ActualWorkRate, 25f, "shared c2 work units/s");
                Near(Link(host, "E1").BandwidthUsageMBps, 50f, "shared E1 MB/s");
                CheckCapacity(host);
                Debug.Log("ResourceWitness shared E1: expected c1/c2 25/25 work units/s, E1 50 MB/s; observed " +
                    $"{c1.ActualWorkRate:0.###}/{c2.ActualWorkRate:0.###}, {Link(host, "E1").BandwidthUsageMBps:0.###}");

                steps = 0;
                while (!c2.IsComplete && steps++ < 1000)
                    host.StepSimulation(0.1f);
                Check(c2.IsComplete && c1.IsActive, "c2 completes before c1");
                host.StepSimulation(0.1f);
                Near(c1.ActualWorkRate, 35f, "released bandwidth raises c1 rate");
                Near(Link(host, "E1").BandwidthUsageMBps, 35f, "released E1 usage");
                Near(Node(host, "AUTH").MemoryUsageGB, Node(host, "AUTH").MemoryBaselineGB,
                    "completed c2 releases AUTH memory");
                CheckCapacity(host);
                Debug.Log("ResourceWitness completion: expected c1 35 work units/s, E1 35 MB/s, AUTH memory baseline; observed " +
                    $"{c1.ActualWorkRate:0.###}, {Link(host, "E1").BandwidthUsageMBps:0.###}, {Node(host, "AUTH").MemoryUsageGB:0.###} GB");

                SetFloat(dbRequirement, "cpuUnitsPerWork", 4f);
                host.ResetSimulation();
                host.StepSimulation(0.1f);
                c1 = host.RuntimeConnections.First(c => c.ConnectionId == "c1");
                Near(c1.ActualWorkRate, 18f, "DB CPU bottleneck work rate");
                Near(Node(host, "DB").CpuUsageUnits, 80f, "DB CPU capacity");
                Check(c1.LimitKind == LimitKind.NodeCpu && c1.LimitingResourceId == "DB", "DB CPU limiter");
                CheckCapacity(host);
                Debug.Log($"ResourceWitness CPU: expected 18 work units/s, DB CPU 80; observed {c1.ActualWorkRate:0.###}, {Node(host, "DB").CpuUsageUnits:0.###}");

                SetFloat(dbRequirement, "cpuUnitsPerWork", originalCpu);
                SetFloat(dbRequirement, "diskIoMBPerWork", 4f);
                host.ResetSimulation();
                host.StepSimulation(0.1f);
                c1 = host.RuntimeConnections.First(c => c.ConnectionId == "c1");
                Near(c1.ActualWorkRate, 23f, "DB disk bottleneck work rate");
                Near(Node(host, "DB").DiskIoUsageMBps, 100f, "DB disk capacity");
                Check(c1.LimitKind == LimitKind.NodeDiskIo && c1.LimitingResourceId == "DB", "DB disk limiter");
                CheckCapacity(host);
                Debug.Log($"ResourceWitness disk: expected 23 work units/s, DB disk 100 MB/s; observed {c1.ActualWorkRate:0.###}, {Node(host, "DB").DiskIoUsageMBps:0.###}");

                SetFloat(dbRequirement, "diskIoMBPerWork", originalDisk);
                SetFloat(dbRequirement, "cpuUnitsPerWork", 4f);
                SetFloat(c1Definition, "bandwidthMBPerWork", 0.1f);
                host.ResetSimulation();
                host.StepSimulation(0.1f);
                c1 = host.RuntimeConnections.First(c => c.ConnectionId == "c1");
                Near(c1.ActualWorkRate, 18f, "low-bandwidth CPU-heavy work rate");
                Near(c1.ActualBandwidthMBps, 1.8f, "low-bandwidth actual MB/s");
                Near(Link(host, "E1").BandwidthUsageMBps, 1.8f, "low-bandwidth E1 usage");
                Near(Node(host, "DB").CpuUsageUnits, 80f, "low-bandwidth DB CPU usage");
                CheckCapacity(host);
                Debug.Log($"ResourceWitness low bandwidth: expected 18 work units/s, E1 1.8 MB/s, DB CPU 80; observed {c1.ActualWorkRate:0.###}, {Link(host, "E1").BandwidthUsageMBps:0.###}, {Node(host, "DB").CpuUsageUnits:0.###}");
                Debug.Log("ResourceWitness PASS");
            }
            finally
            {
                SetFloat(dbRequirement, "cpuUnitsPerWork", originalCpu);
                SetFloat(dbRequirement, "diskIoMBPerWork", originalDisk);
                SetFloat(c1Definition, "bandwidthMBPerWork", originalBandwidth);
                host.ResetSimulation();
            }
        }

        private static float GetFloat(object owner, string name) => (float)Field(owner, name).GetValue(owner);
        private static void SetFloat(object owner, string name, float value) => Field(owner, name).SetValue(owner, value);
        private static FieldInfo Field(object owner, string name) => owner.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException("Missing field " + name);
        private static NodeResources Node(SimulationHost host, string id) => host.Nodes.First(n => n.View.NodeId == id);
        private static PhysicalLinkResources Link(SimulationHost host, string id) =>
            host.PhysicalLinks.First(l => l.View.LinkId == id);

        private static void CheckCapacity(SimulationHost host)
        {
            foreach (NodeResources node in host.Nodes)
                Check(node.CpuUsageUnits <= node.CpuCapacityUnits + Tolerance &&
                    node.MemoryUsageGB <= node.MemoryCapacityGB + Tolerance &&
                    node.DiskIoUsageMBps <= node.DiskIoCapacityMBps + Tolerance,
                    node.View.NodeId + " capacity");
            foreach (PhysicalLinkResources link in host.PhysicalLinks)
                Check(link.BandwidthUsageMBps <= link.BandwidthCapacityMBps + Tolerance,
                    link.View.LinkId + " capacity");
        }

        private static void Near(float observed, float expected, string name) =>
            Check(Mathf.Abs(observed - expected) <= Tolerance,
                $"{name}: expected {expected:0.###}, observed {observed:0.###}");
        private static void Check(bool condition, string name)
        {
            if (!condition)
                throw new InvalidOperationException("ResourceWitness failed: " + name);
        }
    }
}
