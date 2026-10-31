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
    public static class LoadOutputWitness
    {
        private static readonly MethodInfo RefreshMethod = typeof(LoadOutput).GetMethod(
            "Refresh", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo RefreshAtMethod = typeof(LoadOutput).GetMethod(
            "RefreshAt", BindingFlags.Instance | BindingFlags.NonPublic);

        [MenuItem("Tools/Connections/Run LOAD Output Witness")]
        public static void Run()
        {
            Check(Terminal.TryParseLoad(new[] { "load", "db" }, out string id,
                out _) && id == "db" &&
                Terminal.TryParseLoad(new[] { "LOAD", "DB" }, out id, out _) && id == "DB" &&
                !Terminal.TryParseLoad(new[] { "load" }, out _, out string usage) &&
                usage == "USAGE  load <nodeId>" &&
                !Terminal.TryParseLoad(new[] { "load", "db", "api" }, out _, out _) &&
                !Terminal.TryParseLoad(new[] { "inspect", "db" }, out _, out _),
                "exactly one case-insensitive node argument and clear usage");
            CheckScene("Assets/Scenes/Connections.unity", "DB");
            CheckScene("Assets/Scenes/ConnectionsAdvanced.unity", "JOBS");
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            bool oldAuto = traffic.AutoGenerationEnabled;
            bool oldHostile = traffic.HostileGenerationEnabled;
            float oldTravel = (float)typeof(SimulationHost).GetField(
                "connectionTravelSecondsPerLink", BindingFlags.Instance |
                BindingFlags.NonPublic).GetValue(host);
            var travelField = typeof(SimulationHost).GetField("connectionTravelSecondsPerLink",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var prefab = AssetDatabase.LoadAssetAtPath<LoadOutput>(
                "Assets/Prefabs/Connections/LoadOutput.prefab");
            try
            {
                traffic.AutoGenerationEnabled = false;
                traffic.HostileGenerationEnabled = false;
                travelField.SetValue(host, 0.000001f);
                host.SetRunMode(ConnectionRunMode.Generated);
                CheckCommand(terminal, "db");
                CheckCommand(terminal, "DB");
                CheckError(terminal, new[] { "load" }, "USAGE");
                CheckError(terminal, new[] { "load", "db", "api" }, "USAGE");
                CheckError(terminal, new[] { "load", "missing" }, "unknown node");

                var parent = new GameObject("LOAD witness width", typeof(RectTransform));
                ((RectTransform)parent.transform).sizeDelta = new Vector2(320f, 800f);
                var scroll = parent.AddComponent<ScrollRect>();
                scroll.enabled = false;
                scroll.viewport = (RectTransform)parent.transform;
                LoadOutput panel = UnityEngine.Object.Instantiate(prefab, parent.transform);
                LoadOutput second = UnityEngine.Object.Instantiate(prefab, parent.transform);
                try
                {
                    panel.Bind("api", host);
                    Check(panel.ConnectionRowCount == 0 && !Rows(panel).Any() &&
                        Empty(panel).gameObject.activeSelf &&
                        Empty(panel).text == "NO ACTIVE CONNECTION REQUESTS" &&
                        !panel.GetComponentsInChildren<TMP_Text>(true)
                            .Any(t => t.text.Contains("REQUESTED") || t.text.Contains("CAPACITY")),
                        "empty node states only the absence of connection requests");
                    panel.Bind("db", host);
                    second.Bind("DB", host);
                    Check(panel.ConnectionRowCount == 0 && second.ConnectionRowCount == 0 &&
                        !Rows(panel).Any() && panel.CurrentScale == 3f,
                        "zero-demand node has no obsolete rows");
                    Check(((RectTransform)panel.transform).rect.width == 320f &&
                        ((RectTransform)panel.transform).rect.height == 122f &&
                        Empty(panel).preferredWidth <= Empty(panel).rectTransform.rect.width,
                        "narrow terminal width and empty layout");

                    NodeView[] route = { Node(host, "EXT1"), Node(host, "API"),
                        Node(host, "DB") };
                    NodeView[] authRoute = { Node(host, "EXT1"), Node(host, "API"),
                        Node(host, "AUTH") };
                    Queue(traffic, authRoute, "l5", new OperationStage(OperationKind.Read,
                        1000f, 2.2f, "small service request"));
                    host.StepSimulation(.1f);
                    panel.Bind("AUTH", host);
                    LoadRequestRow small = Row(panel, "l5");
                    Check(panel.CurrentScale == 3f && small.DisplayedValue == "2.2" &&
                        small.FillFraction > .7f && small.FillFraction < .8f &&
                        RowFill(small).color == PanelColor(panel, "modestColor") &&
                        host.FindConnection("l5").AllocatedLoad == 2.2f,
                        "small request gets a useful relative teal bar at full allocation");
                    panel.Bind("DB", host);
                    Queue(traffic, route, "l1", new OperationStage(OperationKind.Read,
                        1000f, 4f, "spiking", behaviors: new[] {
                            TimedModifierBehaviorDefinition.Spiking("SPIKING", 1f, 1f,
                                14f, 1f) }));
                    Queue(traffic, route, "l2", new OperationStage(OperationKind.Write,
                        1000f, 8f, "ordinary"));
                    Queue(traffic, route, "l3", new OperationStage(OperationKind.Read,
                        1000f, 2f, "waving", behaviors: new[] {
                            TimedModifierBehaviorDefinition.Waving("WAVING", 0f,
                                -1f, 1f, 4f) }));
                    host.StepSimulation(0.1f);
                    Refresh(panel); Refresh(second);
                    var spike = host.FindConnection("l1");
                    var wave = host.FindConnection("l3");
                    Check(panel.ConnectionRowCount == 3 && second.ConnectionRowCount == 3 &&
                        spike.IsDemanding && wave.IsDemanding &&
                        panel.CurrentScale == second.CurrentScale &&
                        RowIds(panel).SequenceEqual(new[] { "l2", "l1", "l3" }),
                        "multiple panels share authoritative requests independently");
                    float low = spike.DesiredLoad;
                    float lowBar = Row(panel, "l1").FillFraction;
                    Check(low >= 3.9f && low <= 4.1f &&
                        Row(panel, "l1").DisplayedValue == "4.0" &&
                        Row(panel, "l2").DisplayedValue == "8.0" &&
                        panel.CurrentScale == 9.5f &&
                        RowFill(Row(panel, "l2")).color == PanelColor(panel, "elevatedColor") &&
                        Mathf.Abs(NodeResources(host, "DB").RequestedLoad -
                            (spike.DesiredLoad + host.FindConnection("l2").DesiredLoad +
                             wave.DesiredLoad)) < 0.01f,
                        "initial rows use effective requests and capacity-relative colors");

                    for (int step = 0; step < 8 && spike.DesiredLoad < 17.9f; step++)
                        host.StepSimulation(.25f);
                    Refresh(panel); Refresh(second);
                    Check(spike.DesiredLoad > 17.9f &&
                        spike.AllocatedLoad < spike.DesiredLoad &&
                        Row(panel, "l1").DisplayedValue == "18.0" &&
                        Row(panel, "l1").FillFraction > lowBar &&
                        panel.CurrentScale == 21f &&
                        RowFill(Row(panel, "l1")).color == PanelColor(panel, "extremeColor") &&
                        RowIds(panel).SequenceEqual(new[] { "l2", "l1", "l3" }) &&
                        Row(second, "l1").DisplayedValue == "18.0",
                        "SPIKING row follows changing DesiredLoad despite constrained allocation");
                    Check(wave.DesiredLoad != wave.BaseLoad &&
                        Row(panel, "l3").DisplayedValue ==
                            wave.DesiredLoad.ToString("0.0",
                                System.Globalization.CultureInfo.InvariantCulture),
                        "WAVING row follows effective rather than base LOAD");
                    float highScale = panel.CurrentScale;
                    for (int step = 0; step < 8 && spike.DesiredLoad > 4.1f; step++)
                        host.StepSimulation(.25f);
                    float clock = Time.unscaledTime;
                    RefreshAt(panel, clock + .1f);
                    Check(spike.DesiredLoad < 4.1f &&
                        Row(panel, "l1").DisplayedValue == "4.0" &&
                        panel.CurrentScale == highScale &&
                        Row(panel, "l1").FillFraction <
                            Row(panel, "l2").FillFraction &&
                        RowIds(panel).SequenceEqual(new[] { "l2", "l1", "l3" }),
                        "SPIKING bar falls promptly while scale holds its short grace period");
                    RefreshAt(panel, clock + 1.6f);
                    Check(panel.CurrentScale < highScale && panel.CurrentScale > 9.5f,
                        "scale contracts gradually rather than jumping to the low target");
                    RefreshAt(panel, clock + 4.1f);
                    Check(panel.CurrentScale <= 9.75f &&
                        panel.CurrentScale >= 9.5f &&
                        RowIds(panel).SequenceEqual(new[] { "l2", "l1", "l3" }),
                        "scale contracts smoothly toward live demand after the spike");

                    Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 3f,
                        out _), "inject non-connection demand");
                    RefreshAt(panel, clock + 4.2f);
                    NodeResources db = NodeResources(host, "DB");
                    db.View.LoadBar.Render(db);
                    Check(Mathf.Abs(db.RequestedLoad -
                            (spike.DesiredLoad + host.FindConnection("l2").DesiredLoad +
                             wave.DesiredLoad + 3f)) < .01f &&
                        db.View.LoadBar.LabelText ==
                            $"LOAD - {db.RequestedLoad:0.#} / {db.LoadCapacity:0.#}" &&
                        panel.ConnectionRowCount == 3 &&
                        !Rows(panel).Any(r => r.DisplayedId == "other") &&
                        !panel.GetComponentsInChildren<TMP_Text>(true)
                            .Any(t => t.text.Contains("INFECTED") ||
                                t.text.Contains("REQUESTED") || t.text.Contains("CAPACITY")),
                        "Phantom LOAD stays in node accounting but outside connection panel");
                    Check(host.KillConnection("l2"), "resolve a connection");
                    RefreshAt(panel, clock + 4.3f);
                    Check(panel.ConnectionRowCount == 2 &&
                        panel.GetComponentsInChildren<LoadRequestRow>(true)
                            .Count(r => r.gameObject.activeSelf) == 2,
                        "resolved demand row disappears and layout contracts");
                    RefreshAt(panel, clock + 8.3f);
                    Check(panel.CurrentScale <= 5f &&
                        RowIds(panel).SequenceEqual(new[] { "l1", "l3" }),
                        "scale also contracts after the largest requester disappears");
                    traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "l4", route,
                        "load-witness", "Service", new[] {
                            new OperationStage(OperationKind.Read, 1f, 3f, "first operation"),
                            new OperationStage(OperationKind.Write, 1000f, 9f,
                                "second operation") }, false));
                    host.StepSimulation(.1f);
                    RefreshAt(panel, clock + 8.4f);
                    var transition = host.FindConnection("l4");
                    Check(transition != null && transition.IsDemanding &&
                        transition.CurrentOperationIndex == 0 &&
                        panel.ConnectionRowCount == 3 &&
                        Row(panel, "l4").DisplayedValue == "3.0",
                        "new requesting connection appears without a new command");
                    for (int step = 0; step < 40 && transition.CurrentOperationIndex == 0; step++)
                        host.StepSimulation(.25f);
                    host.StepSimulation(.1f); // Publish the next operation's new request.
                    RefreshAt(panel, clock + 8.5f);
                    Check(transition.CurrentOperationIndex == 1 && transition.IsDemanding &&
                        Row(panel, "l4").DisplayedValue == "9.0" &&
                        panel.ConnectionRowCount == 3,
                        "operation transition updates the same identity row");
                    host.ResetSimulation();
                    Refresh(panel); Refresh(second);
                    Check(panel.ConnectionRowCount == 0 && second.ConnectionRowCount == 0 &&
                        !Rows(panel).Any() && Empty(panel).gameObject.activeSelf,
                        "reset clears old connection references in both panels");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(panel.gameObject);
                    UnityEngine.Object.DestroyImmediate(second.gameObject);
                    UnityEngine.Object.DestroyImmediate(parent);
                }
                host.ResetSimulation(); // Destroyed panels must no longer receive updates.
                Debug.Log("LoadOutputWitness PASS: commands, both scenes, dynamic requests, " +
                    "contention, relative scale and contraction, infection boundary, " +
                    "multi-panel, responsive layout, resolution, reset.");
            }
            finally
            {
                travelField.SetValue(host, oldTravel);
                traffic.AutoGenerationEnabled = oldAuto;
                traffic.HostileGenerationEnabled = oldHostile;
                host.ResetSimulation();
            }
        }

        private static void CheckScene(string scene, string nodeId)
        {
            EditorSceneManager.OpenScene(scene);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            var prefab = new SerializedObject(terminal).FindProperty("loadPrefab")
                .objectReferenceValue as LoadOutput;
            var source = AssetDatabase.LoadAssetAtPath<LoadOutput>(
                "Assets/Prefabs/Connections/LoadOutput.prefab");
            var row = AssetDatabase.LoadAssetAtPath<LoadRequestRow>(
                "Assets/Prefabs/Connections/LoadRequestRow.prefab");
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Shaders/ConnectionsPill.mat");
            Check(host.FindNodeResources(nodeId) != null &&
                host.Nodes.All(n => n.BaselineLoad == 0f) &&
                prefab == source && row != null &&
                new SerializedObject(prefab).FindProperty("rowPrefab")
                    .objectReferenceValue == row &&
                prefab.transform.Find("LoadCard/RequestedTotal") == null &&
                new SerializedObject(prefab).FindProperty("requestedText") == null &&
                new SerializedObject(prefab).FindProperty("nodePill")
                    .objectReferenceValue is PillGraphic nodePill &&
                nodePill.material == material &&
                row.GetComponentInChildren<PillGraphic>(true).material == material,
                "zero scene baseline, minimal authored panel, pill material, and binding in " + scene);
        }

        private static void CheckCommand(Terminal terminal, string id)
        {
            var content = (RectTransform)new SerializedObject(terminal)
                .FindProperty("historyContent").objectReferenceValue;
            int before = content.childCount;
            typeof(Terminal).GetMethod("Load", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(terminal, new object[] { new[] { "load", id } });
            var panel = content.GetChild(before).GetComponent<LoadOutput>();
            Check(content.childCount == before + 1 && panel != null &&
                Mathf.Abs(((RectTransform)panel.transform).rect.width -
                    Mathf.Min(580f, content.rect.width)) < 0.01f &&
                Mathf.Abs(((RectTransform)panel.transform.Find("LoadCard")).rect.width -
                    (Mathf.Min(580f, content.rect.width) - 28f)) < 0.01f,
                "valid command appends a live panel with the inspect/scan gutter");
        }

        private static void CheckError(Terminal terminal, string[] tokens, string expected)
        {
            var content = (RectTransform)new SerializedObject(terminal)
                .FindProperty("historyContent").objectReferenceValue;
            int before = content.childCount;
            typeof(Terminal).GetMethod("Load", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(terminal, new object[] { tokens });
            TMP_Text label = content.GetChild(before).GetComponentInChildren<TMP_Text>();
            Check(content.childCount == before + 1 && label.text.Contains(expected),
                "invalid command reports " + expected);
        }

        private static void Queue(ConnectionTrafficDirector traffic, NodeView[] route,
            string id, OperationStage stage) => traffic.QueueSpawnPlan(new ConnectionSpawnPlan(
                0f, id, route, "load-witness", "Service", new[] { stage }, false));

        private static NodeView Node(SimulationHost host, string id) =>
            host.FindNodeResources(id).View;

        private static NodeResources NodeResources(SimulationHost host, string id) =>
            host.FindNodeResources(id);

        private static void Refresh(LoadOutput panel) => RefreshMethod.Invoke(panel, null);

        private static void RefreshAt(LoadOutput panel, float now) =>
            RefreshAtMethod.Invoke(panel, new object[] { now });

        private static LoadRequestRow[] Rows(LoadOutput panel) =>
            panel.GetComponentsInChildren<LoadRequestRow>(true)
                .Where(r => r.gameObject.activeSelf).ToArray();

        private static string[] RowIds(LoadOutput panel) =>
            Rows(panel).Select(r => r.DisplayedId).ToArray();

        private static TMP_Text Empty(LoadOutput panel) =>
            (TMP_Text)new SerializedObject(panel).FindProperty("emptyText").objectReferenceValue;

        private static Image RowFill(LoadRequestRow row) =>
            (Image)new SerializedObject(row).FindProperty("fill").objectReferenceValue;

        private static Color PanelColor(LoadOutput panel, string field) =>
            new SerializedObject(panel).FindProperty(field).colorValue;

        private static LoadRequestRow Row(LoadOutput panel, string id) =>
            Rows(panel).Single(r => r.DisplayedId.Equals(id,
                    StringComparison.OrdinalIgnoreCase));

        private static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException("LoadOutputWitness: " + message);
        }
    }
}
