using System;
using System.Linq;
using System.Reflection;
using TMPro;
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
            var c2Definition = host.Definitions.First(c => c.ConnectionId == "c2");
            var c3Definition = host.Definitions.First(c => c.ConnectionId == "c3");
            var db = Node(host, "DB");
            var api = Node(host, "API");
            NodeView originalC2Target = c2Definition.TargetNode;
            float originalC2Load = GetFloat(c2Definition, "desiredLoad");
            float originalC3Load = GetFloat(c3Definition, "desiredLoad");
            float originalC3Work = GetFloat(c3Definition, "totalWork");
            float originalC2Delay = GetFloat(c2Definition, "startDelaySeconds");
            float originalC3Delay = GetFloat(c3Definition, "startDelaySeconds");
            float originalDbBaseline = GetFloat(db, "baselineLoad");
            try
            {
                host.ResetSimulation();
                var c1 = State(host, "c1");
                var c2 = State(host, "c2");
                var c3 = State(host, "c3");
                host.StepSimulation(0.1f);
                Near(c1.AllocatedLoad, 5f, "uncontended c1 LOAD");
                Near(db.RequestedLoad, 5f, "uncontended DB request");
                Near(db.AllocatedLoad, 5f, "uncontended DB allocation");
                Near(api.RequestedLoad, 1f, "transit API retains only baseline LOAD");
                Color elevated = new SerializedObject(db.View.LoadBar).FindProperty("elevatedColor").colorValue;
                Check(db.View.LoadBar.FillColor == elevated, "5/10 LOAD uses elevated color");
                Check(c2.IsPending && c3.IsPending, "delayed connections pending");
                CheckCapacityAndAccounting(host);
                Debug.Log($"ResourceWitness uncontended: expected c1 5, DB 5/10, API baseline 1; observed {c1.AllocatedLoad:0.###}, {db.RequestedLoad:0.###}/{db.LoadCapacity:0.###}, {api.RequestedLoad:0.###}");

                int steps = 0;
                while (c3.IsPending && steps++ < 50)
                    host.StepSimulation(0.1f);
                Check(c2.IsActive && c3.IsActive, "authored delayed establishment");
                Near(db.RequestedLoad, 13f, "authored DB requested LOAD");
                Near(db.AllocatedLoad, 10f, "authored DB allocated LOAD");
                Near(c1.AllocatedLoad, 50f / 13f, "authored c1 proportional LOAD");
                Near(c3.AllocatedLoad, 80f / 13f, "authored c3 proportional LOAD");
                Near(c2.AllocatedLoad, 5f, "AUTH target does not compete with DB");
                Near(api.RequestedLoad, 1f, "transit API remains at baseline");
                CheckCapacityAndAccounting(host);
                CheckPresentation(host, c1, db);
                Debug.Log($"ResourceWitness authored 13/10: expected c1 3.846, c3 6.154, c2 5; observed {c1.AllocatedLoad:0.###}, {c3.AllocatedLoad:0.###}, {c2.AllocatedLoad:0.###}");

                steps = 0;
                while (!c3.IsComplete && steps++ < 1000)
                    host.StepSimulation(0.1f);
                Check(c3.IsComplete && c1.IsActive, "c3 completes while c1 remains active");
                host.StepSimulation(0.1f);
                Near(c1.AllocatedLoad, 5f, "c1 recovers after c3 completion");
                Near(db.RequestedLoad, 5f, "completed c3 releases requested LOAD");
                Near(db.AllocatedLoad, 5f, "completed c3 releases allocated LOAD");
                CheckCapacityAndAccounting(host);
                Debug.Log($"ResourceWitness completion: expected c1 5, DB request/allocation 5/5; observed {c1.AllocatedLoad:0.###}, {db.RequestedLoad:0.###}/{db.AllocatedLoad:0.###}");

                SetField(c2Definition.Route.Last(), "node", db.View);
                SetField(c2Definition, "desiredLoad", 3f);
                SetField(c3Definition, "desiredLoad", 5f);
                SetField(c3Definition, "totalWork", 5f);
                SetField(c2Definition, "startDelaySeconds", 0f);
                SetField(c3Definition, "startDelaySeconds", 0f);
                host.ResetSimulation();
                host.StepSimulation(1f);
                c1 = State(host, "c1");
                c2 = State(host, "c2");
                c3 = State(host, "c3");
                Near(db.RequestedLoad, 13f, "three-way requested LOAD");
                Near(db.AllocatedLoad, 10f, "three-way allocated LOAD");
                Near(c1.AllocatedLoad, 50f / 13f, "three-way c1 allocation");
                Near(c2.AllocatedLoad, 30f / 13f, "three-way c2 allocation");
                Near(c3.AllocatedLoad, 50f / 13f, "three-way c3 allocation");
                Near(c2.CompletedWork, 30f / 13f, "allocated LOAD advances WORK");
                CheckCapacityAndAccounting(host);
                Debug.Log($"ResourceWitness 5+5+3/10: expected 3.846/3.846/2.308; observed {c1.AllocatedLoad:0.###}/{c3.AllocatedLoad:0.###}/{c2.AllocatedLoad:0.###}");

                host.StepSimulation(1f);
                Check(c3.IsComplete, "short c3 work completes");
                host.StepSimulation(0.1f);
                Near(c1.AllocatedLoad, 5f, "c1 full LOAD after release");
                Near(c2.AllocatedLoad, 3f, "c2 full LOAD after release");
                Near(db.RequestedLoad, 8f, "remaining requests after release");
                CheckCapacityAndAccounting(host);

                SetField(db, "baselineLoad", 2f);
                host.ResetSimulation();
                host.StepSimulation(0.1f);
                Near(db.RequestedLoad, 15f, "baseline plus requested LOAD");
                Near(db.AllocatedLoad, 10f, "baseline reduces available LOAD");
                Near(State(host, "c1").AllocatedLoad, 40f / 13f, "baseline c1 fraction");
                CheckCapacityAndAccounting(host);
                Debug.Log($"ResourceWitness baseline: expected request 15, allocation 10, c1 3.077; observed {db.RequestedLoad:0.###}, {db.AllocatedLoad:0.###}, {State(host, "c1").AllocatedLoad:0.###}");
                Debug.Log("ResourceWitness PASS");
            }
            finally
            {
                SetField(c2Definition.Route.Last(), "node", originalC2Target);
                SetField(c2Definition, "desiredLoad", originalC2Load);
                SetField(c3Definition, "desiredLoad", originalC3Load);
                SetField(c3Definition, "totalWork", originalC3Work);
                SetField(c2Definition, "startDelaySeconds", originalC2Delay);
                SetField(c3Definition, "startDelaySeconds", originalC3Delay);
                SetField(db, "baselineLoad", originalDbBaseline);
                host.ResetSimulation();
            }
        }

        private static void CheckPresentation(SimulationHost host, LogicalConnectionState c1, NodeResources db)
        {
            NodeLoadBar bar = db.View.LoadBar;
            Check(bar != null, "authored DB LOAD bar reference");
            Check(bar.LabelText == "LOAD - 13 / 10", "DB bar shows uncapped requested LOAD");
            Near(bar.FillFraction, 1f, "DB bar fill caps at 100 percent");
            Color overloadColor = new SerializedObject(bar).FindProperty("overloadedColor").colorValue;
            Check(bar.FillColor == overloadColor, "DB bar overload color");
            var prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>("Assets/Prefabs/Connections/InspectOutput.prefab");
            Check(prefab != null, "inspect prefab");
            InspectOutput output = UnityEngine.Object.Instantiate(prefab);
            try
            {
                output.Bind(new[] { c1 });
                var entry = (InspectEntry)new SerializedObject(output).FindProperty("entries")
                    .GetArrayElementAtIndex(0).objectReferenceValue;
                SerializedObject fields = new SerializedObject(entry);
                string load = ((TMP_Text)fields.FindProperty("throughput").objectReferenceValue).text;
                string target = ((TMP_Text)fields.FindProperty("bottleneck").objectReferenceValue).text;
                Check(load.Contains("3.8 / 5") && load.Contains("LOAD"), "inspect allocated/desired LOAD");
                Check(target.Contains("13 / 10 requested") && target.Contains("10 allocated"),
                    "inspect agrees with node LOAD");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output.gameObject);
            }
            foreach (PhysicalLinkReference link in host.PhysicalLinks)
            {
                TMP_Text label = (TMP_Text)new SerializedObject(link.View)
                    .FindProperty("resourceReadout").objectReferenceValue;
                Check(label != null && label.text == link.View.LinkId,
                    link.View.LinkId + " shows no resource LOAD");
            }
        }

        private static void CheckCapacityAndAccounting(SimulationHost host)
        {
            foreach (NodeResources node in host.Nodes)
            {
                float requested = node.BaselineLoad;
                float allocated = node.BaselineLoad;
                foreach (LogicalConnectionState connection in host.RuntimeConnections)
                    if (connection.IsActive && connection.TargetNode == node.View)
                    {
                        requested += connection.Definition.DesiredLoad;
                        allocated += connection.AllocatedLoad;
                    }
                Near(node.RequestedLoad, requested, node.View.NodeId + " requested accounting");
                Near(node.AllocatedLoad, allocated, node.View.NodeId + " allocated accounting");
                Check(node.AllocatedLoad <= node.LoadCapacity + Tolerance,
                    node.View.NodeId + " capacity");
                NodeLoadBar bar = node.View.LoadBar;
                Check(bar != null && bar.LabelText == $"LOAD - {requested:0.#} / {node.LoadCapacity:0.#}",
                    node.View.NodeId + " bar matches requested LOAD");
                float fraction = node.LoadCapacity > 0f ? Mathf.Clamp01(requested / node.LoadCapacity) : 0f;
                Near(bar.FillFraction, fraction, node.View.NodeId + " capped bar fill");
            }
        }

        private static LogicalConnectionState State(SimulationHost host, string id) =>
            host.RuntimeConnections.First(c => c.ConnectionId == id);
        private static NodeResources Node(SimulationHost host, string id) =>
            host.Nodes.First(n => n.View.NodeId == id);
        private static float GetFloat(object owner, string name) => (float)Field(owner, name).GetValue(owner);
        private static void SetField(object owner, string name, object value) => Field(owner, name).SetValue(owner, value);
        private static FieldInfo Field(object owner, string name) => owner.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException("Missing field " + name);
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
