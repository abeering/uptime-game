using System;
using System.Linq;
using System.Reflection;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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
            host.SetRunMode(ConnectionRunMode.AuthoredWitness);
            var c1Definition = host.Definitions.First(c => c.ConnectionId == "c1");
            var c2Definition = host.Definitions.First(c => c.ConnectionId == "c2");
            var c3Definition = host.Definitions.First(c => c.ConnectionId == "c3");
            var db = Node(host, "DB");
            var api = Node(host, "API");
            NodeView originalC2Target = c2Definition.TargetNode;
            float originalC2Load = GetFloat(c2Definition, "desiredLoad");
            float originalC3Load = GetFloat(c3Definition, "desiredLoad");
            float originalC3Work = GetFloat(c3Definition, "totalWork");
            var originalC3Operations = (OperationStage[])Field(c3Definition, "operations").GetValue(c3Definition);
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
                Check(c1.OperationCount == 1 && c2.OperationCount == 1 && c3.OperationCount == 2,
                    "legacy single operations and authored arc");
                CheckCapacityAndAccounting(host);
                Debug.Log($"ResourceWitness uncontended: expected c1 5, DB 5/10, API baseline 1; observed {c1.AllocatedLoad:0.###}, {db.RequestedLoad:0.###}/{db.LoadCapacity:0.###}, {api.RequestedLoad:0.###}");

                int steps = 0;
                while (c3.IsPending && steps++ < 50)
                    host.StepSimulation(0.1f);
                Check(c2.IsActive && c3.IsActive, "authored delayed establishment");
                Check(c3.CurrentOperation == OperationKind.Write && c3.CurrentOperationIndex == 0,
                    "arc begins with WRITE");
                Near(db.RequestedLoad, 13f, "authored DB requested LOAD");
                Near(db.AllocatedLoad, 10f, "authored DB allocated LOAD");
                Near(c1.AllocatedLoad, 50f / 13f, "authored c1 proportional LOAD");
                Near(c3.AllocatedLoad, 80f / 13f, "authored c3 proportional LOAD");
                Near(c2.AllocatedLoad, 5f, "AUTH target does not compete with DB");
                Near(api.RequestedLoad, 1f, "transit API remains at baseline");
                CheckCapacityAndAccounting(host);
                CheckPresentation(host, c1, db);
                CheckArcPresentation(c3, true);
                Debug.Log($"ResourceWitness authored 13/10: expected c1 3.846, c3 6.154, c2 5; observed {c1.AllocatedLoad:0.###}, {c3.AllocatedLoad:0.###}, {c2.AllocatedLoad:0.###}");

                steps = 0;
                while (c3.CurrentOperationIndex == 0 && steps++ < 1000)
                    host.StepSimulation(0.1f);
                Check(c3.IsActive && c3.CurrentOperation == OperationKind.Read &&
                    c3.CurrentOperationIndex == 1 && c3.IsTransitioning &&
                    c3.CompletedWork == 0f && c3.AllocatedLoad == 0f,
                    "WRITE completes into READ without completing connection");
                Check(ReferenceEquals(c3, State(host, "c3")) && c3.ConnectionId == "c3",
                    "arc retains connection identity and runtime state");
                Near(db.RequestedLoad, 5f, "transition defers next operation LOAD");
                host.StepSimulation(0.1f);
                Near(c3.AllocatedLoad, 4f, "READ uses its independent LOAD");
                Near(c1.AllocatedLoad, 5f, "c1 recovers when READ reduces demand");
                Near(db.RequestedLoad, 9f, "READ and c1 request nine LOAD");
                Check(c3.CompletedWork > 0f && c3.CompletedWork < c3.CurrentTotalWork,
                    "READ begins its own WORK");
                CheckArcPresentation(c3, false);
                Debug.Log($"ResourceWitness arc: WRITE 30/8 -> READ 60/4, DB request {db.RequestedLoad:0.###}, c3 allocation {c3.AllocatedLoad:0.###}");
                steps = 0;
                while (!c3.IsComplete && steps++ < 1000)
                    host.StepSimulation(0.1f);
                Check(c3.IsComplete && c1.IsActive, "c3 completes while c1 remains active");
                host.StepSimulation(0.1f);
                Near(c1.AllocatedLoad, 5f, "c1 recovers after c3 completion");
                Near(db.RequestedLoad, 5f, "completed c3 releases requested LOAD");
                Near(db.AllocatedLoad, 5f, "completed c3 releases allocated LOAD");
                CheckCapacityAndAccounting(host);
                CheckCompletedPresentation(c1, c3);
                Debug.Log($"ResourceWitness completion: expected c1 5, DB request/allocation 5/5; observed {c1.AllocatedLoad:0.###}, {db.RequestedLoad:0.###}/{db.AllocatedLoad:0.###}");
                CheckThreeStagePresentation(host, c3Definition, originalC3Operations,
                    originalC3Delay);

                SetField(c2Definition.Route.Last(), "node", db.View);
                SetField(c2Definition, "desiredLoad", 3f);
                SetField(c3Definition, "desiredLoad", 5f);
                SetField(c3Definition, "totalWork", 5f);
                SetField(c3Definition, "operations", Array.Empty<OperationStage>());
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
                SetField(db, "baselineLoad", 10f);
                host.ResetSimulation();
                host.StepSimulation(0.1f);
                Near(State(host, "c1").CompletedWork, 0f, "stalled connection makes no WORK");
                Near(State(host, "c1").AllocatedLoad, 0f, "stalled connection has no LOAD");
                CheckCapacityAndAccounting(host);
                CheckStalledPresentation(State(host, "c1"));
                Debug.Log("ResourceWitness PASS");
            }
            finally
            {
                SetField(c2Definition.Route.Last(), "node", originalC2Target);
                SetField(c2Definition, "desiredLoad", originalC2Load);
                SetField(c3Definition, "desiredLoad", originalC3Load);
                SetField(c3Definition, "totalWork", originalC3Work);
                SetField(c3Definition, "operations", originalC3Operations);
                SetField(c2Definition, "startDelaySeconds", originalC2Delay);
                SetField(c3Definition, "startDelaySeconds", originalC3Delay);
                SetField(db, "baselineLoad", originalDbBaseline);
                host.ResetSimulation();
                host.SetRunMode(ConnectionRunMode.Generated);
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
                var tokens = entry.GetComponentsInChildren<InspectOperationToken>();
                CheckHeader(fields, "C1", "Record Retrieval", false);
                Check(load.Contains("SLOWED") && load.Contains("LOAD 3.8/5") &&
                    load.Contains("DB OVERLOADED"), "inspect agrees with node LOAD and performance");
                Check(tokens.Length == 1 && tokens[0].Text == "READ → DB" &&
                    tokens[0].HasOutline && !tokens[0].HasCheckmark,
                    "single active operation has authored outlined pill");
                Check(tokens[0].OutlineColor == new SerializedObject(entry)
                    .FindProperty("limitedColor").colorValue,
                    "slowed operation outline uses orange performance color");
                Check(tokens[0].GetComponent<LayoutElement>().preferredHeight == 40f,
                    "operation pill has full authored height");
                entry.Render(c1);
                Check(ReferenceEquals(tokens[0], entry.GetComponentInChildren<InspectOperationToken>()),
                    "live LOAD refresh keeps authored operation token");
                Check(((LayoutElement)new SerializedObject(output).FindProperty("layout")
                    .objectReferenceValue).preferredHeight == 202f, "single active entry height");
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

        private static void CheckArcPresentation(LogicalConnectionState state, bool writeStage)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>("Assets/Prefabs/Connections/InspectOutput.prefab");
            InspectOutput output = UnityEngine.Object.Instantiate(prefab);
            try
            {
                output.Bind(new[] { state });
                var entry = (InspectEntry)new SerializedObject(output).FindProperty("entries")
                    .GetArrayElementAtIndex(0).objectReferenceValue;
                SerializedObject fields = new SerializedObject(entry);
                var tokens = entry.GetComponentsInChildren<InspectOperationToken>();
                string work = ((TMP_Text)fields.FindProperty("work").objectReferenceValue).text;
                string status = ((TMP_Text)fields.FindProperty("throughput").objectReferenceValue).text;
                if (writeStage)
                {
                    Check(tokens.Length == 2 && tokens[0].Text == "WRITE → DB" &&
                        tokens[0].HasOutline && tokens[1].Text == "+1" &&
                        !tokens[1].HasOutline, "future operation hides details");
                    Check(work.Contains("/30"), "inspection WORK uses WRITE stage");
                    Check(status.Contains("SLOWED") && status.Contains("DB OVERLOADED"),
                        "inspection WRITE slowdown");
                }
                else
                {
                    Check(tokens.Length == 2 && tokens[0].Text == "WRITE → DB" &&
                        tokens[0].HasCheckmark && !tokens[0].HasOutline &&
                        tokens[1].Text == "READ → DB" && tokens[1].HasOutline,
                        "completed and active authored tokens update");
                    Color nominal = fields.FindProperty("atSpeedColor").colorValue;
                    Color chrome = fields.FindProperty("activeColor").colorValue;
                    Check(tokens[1].OutlineColor == nominal &&
                        ((TMP_Text)fields.FindProperty("throughput").objectReferenceValue).color == nominal,
                        "at-speed pill and status use green performance color");
                    Check(((TMP_Text)fields.FindProperty("identity").objectReferenceValue).color == chrome &&
                        ((Image)fields.FindProperty("stateTag").objectReferenceValue).color == chrome &&
                        ((Image)fields.FindProperty("progressFill").objectReferenceValue).color == chrome,
                        "title and panel accents keep blue-green chrome color");
                    Check(work.Contains("/60"), "inspection WORK uses READ stage");
                    Check(status.Contains("AT SPEED") && status.Contains("LOAD 4/4"),
                        "inspection uses READ allocation");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output.gameObject);
            }
        }

        private static void CheckCompletedPresentation(LogicalConnectionState active, LogicalConnectionState complete)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>("Assets/Prefabs/Connections/InspectOutput.prefab");
            InspectOutput output = UnityEngine.Object.Instantiate(prefab);
            try
            {
                output.Bind(new[] { active, complete });
                var serialized = new SerializedObject(output);
                var entries = serialized.FindProperty("entries");
                var activeEntry = (InspectEntry)entries.GetArrayElementAtIndex(0).objectReferenceValue;
                var completedEntry = (InspectEntry)entries.GetArrayElementAtIndex(1).objectReferenceValue;
                var fields = new SerializedObject(completedEntry);
                var tokens = completedEntry.GetComponentsInChildren<InspectOperationToken>();
                var status = (TMP_Text)fields.FindProperty("throughput").objectReferenceValue;
                var work = (TMP_Text)fields.FindProperty("work").objectReferenceValue;
                var activity = (TMP_Text)fields.FindProperty("bottleneck").objectReferenceValue;
                var eta = (TMP_Text)fields.FindProperty("eta").objectReferenceValue;
                var progress = (Image)fields.FindProperty("progressFill").objectReferenceValue;
                CheckHeader(fields, "C3", "Record Indexing", true);
                Check(tokens.Length == 2 && tokens[0].Text == "WRITE → DB" &&
                    tokens[1].Text == "READ → DB" && tokens.All(t => t.HasCheckmark && !t.HasOutline),
                    "completed operations remain subdued and visible");
                Check(status.text == "COMPLETE", "completed status has no live LOAD");
                Check(!work.gameObject.activeSelf && !activity.gameObject.activeSelf &&
                    !eta.gameObject.activeSelf && !progress.transform.parent.gameObject.activeSelf,
                    "completed progress and supporting info hidden");
                Check(((RectTransform)activeEntry.transform).rect.height == 186f &&
                    ((RectTransform)completedEntry.transform).rect.height == 124f,
                    "completed entry is shorter than active entry");
                Check(((LayoutElement)serialized.FindProperty("layout").objectReferenceValue)
                    .preferredHeight == 336f, "mixed entries reflow within terminal history");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output.gameObject);
            }
        }

        private static void CheckThreeStagePresentation(SimulationHost host,
            LogicalConnectionDefinition definition, OperationStage[] originalOperations, float originalDelay)
        {
            try
            {
                SetField(definition, "operations", new[]
                {
                    Stage(OperationKind.Read, 3f, 5f),
                    Stage(OperationKind.Write, 30f, 8f),
                    Stage(OperationKind.Execute, 5f, 4f)
                });
                SetField(definition, "startDelaySeconds", 0f);
                host.ResetSimulation();
                var state = State(host, "c3");
                int steps = 0;
                while (state.CurrentOperationIndex == 0 && steps++ < 1000)
                    host.StepSimulation(0.1f);
                host.StepSimulation(0.1f);
                Check(state.CurrentOperationIndex == 1 && state.Performance == LoadPerformance.Slowed,
                    "three-stage middle operation is slowed");
                CheckTokens(state, middle => middle.Length == 3 &&
                    middle[0].Text == "READ → DB" && middle[0].HasCheckmark &&
                    !middle[0].HasOutline && middle[1].Text == "WRITE → DB" &&
                    middle[1].HasOutline && middle[2].Text == "+1" &&
                    !middle[2].HasOutline,
                    "middle-stage timeline is completed, active, future");
                steps = 0;
                while (state.CurrentOperationIndex == 1 && steps++ < 1000)
                    host.StepSimulation(0.1f);
                host.StepSimulation(0.1f);
                CheckTokens(state, final => final.Length == 3 && final[0].HasCheckmark &&
                    final[1].HasCheckmark && final[2].Text == "EXECUTE → DB" &&
                    final[2].HasOutline,
                    "next stage reveals final operation and preserves history");
                steps = 0;
                while (!state.IsComplete && steps++ < 1000) host.StepSimulation(0.1f);
                CheckTokens(state, complete => complete.Length == 3 &&
                    complete.All(t => t.HasCheckmark && !t.HasOutline),
                    "all completed operations are subdued");
            }
            finally
            {
                SetField(definition, "operations", originalOperations);
                SetField(definition, "startDelaySeconds", originalDelay);
                host.ResetSimulation();
            }
        }

        private static void CheckHeader(SerializedObject fields, string id,
            string purpose, bool complete)
        {
            var prefix = (TMP_Text)fields.FindProperty("identity").objectReferenceValue;
            var purposeText = (TMP_Text)fields.FindProperty("purpose").objectReferenceValue;
            var pill = (IdentityPillView)fields.FindProperty("headerIdentityPill").objectReferenceValue;
            Check(prefix != null && prefix.text == "INSPECT /" &&
                purposeText != null && purposeText.text == "- " + purpose,
                "inspect header separates prefix and purpose");
            Check(pill != null && pill.GetComponentInChildren<TMP_Text>().text == id,
                "inspect header identity is in its own pill");
            var inspectPrefab = AssetDatabase.LoadAssetAtPath<InspectOutput>(
                "Assets/Prefabs/Connections/InspectOutput.prefab");
            var nestedPill = inspectPrefab.GetComponentInChildren<IdentityPillView>();
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(nestedPill);
            Check(AssetDatabase.GetAssetPath(source) == "Assets/Prefabs/Connections/IdentityPill.prefab",
                "inspect header reuses network identity pill prefab");
            Color expected = fields.FindProperty(complete ? "quietColor" : "activeColor").colorValue;
            Check(pill.GetComponent<PillGraphic>().color == expected &&
                pill.Size == new Vector2(54f, 28f),
                "inspect header pill keeps authored geometry and chrome color");
        }

        private static OperationStage Stage(OperationKind kind, float work, float load)
        {
            var stage = new OperationStage();
            SetField(stage, "operation", kind);
            SetField(stage, "totalWork", work);
            SetField(stage, "desiredLoad", load);
            return stage;
        }

        private static void CheckTokens(LogicalConnectionState state,
            Func<InspectOperationToken[], bool> condition, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>("Assets/Prefabs/Connections/InspectOutput.prefab");
            InspectOutput output = UnityEngine.Object.Instantiate(prefab);
            try
            {
                output.Bind(new[] { state });
                var entry = (InspectEntry)new SerializedObject(output).FindProperty("entries")
                    .GetArrayElementAtIndex(0).objectReferenceValue;
                var tokens = entry.GetComponentsInChildren<InspectOperationToken>();
                Check(condition(tokens), name);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output.gameObject);
            }
        }

        private static void CheckStalledPresentation(LogicalConnectionState state)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>("Assets/Prefabs/Connections/InspectOutput.prefab");
            InspectOutput output = UnityEngine.Object.Instantiate(prefab);
            try
            {
                output.Bind(new[] { state });
                var entry = (InspectEntry)new SerializedObject(output).FindProperty("entries")
                    .GetArrayElementAtIndex(0).objectReferenceValue;
                string status = ((TMP_Text)new SerializedObject(entry).FindProperty("throughput")
                    .objectReferenceValue).text;
                Check(status.Contains("STALLED") && status.Contains("NO LOAD AVAILABLE"),
                    "inspection stalled status");
                var token = entry.GetComponentsInChildren<InspectOperationToken>().Single();
                Check(token.HasOutline && token.OutlineColor ==
                    new SerializedObject(entry).FindProperty("stalledColor").colorValue,
                    "stalled operation outline uses red performance color");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output.gameObject);
            }
        }

        private static void CheckCapacityAndAccounting(SimulationHost host)
        {
            foreach (NodeResources node in host.Nodes)
            {
                float requested = node.BaselineLoad;
                float allocated = node.BaselineLoad;
                foreach (LogicalConnectionState connection in host.RuntimeConnections)
                    if (connection.IsDemanding && connection.TargetNode == node.View)
                    {
                        requested += connection.DesiredLoad;
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
