using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class WorkOutputWitness
    {
        private static readonly MethodInfo RefreshMethod = typeof(WorkOutput).GetMethod(
            "Refresh", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo LoadRefreshMethod = typeof(LoadOutput).GetMethod(
            "Refresh", BindingFlags.Instance | BindingFlags.NonPublic);

        [MenuItem("Tools/Connections/Run WORK Output Witness")]
        public static void Run()
        {
            Check(Terminal.TryParseWork(new[] { "work", "db" }, out string id,
                    out _) && id == "db" &&
                Terminal.TryParseWork(new[] { "WORK", "DB" }, out id, out _) && id == "DB" &&
                !Terminal.TryParseWork(new[] { "work" }, out _, out string usage) &&
                usage == "USAGE  work <nodeId>" &&
                !Terminal.TryParseWork(new[] { "work", "db", "api" }, out _, out _) &&
                !Terminal.TryParseWork(new[] { "load", "db" }, out _, out _),
                "single case-insensitive node argument and usage errors");
            CheckScene("Assets/Scenes/Connections.unity");
            CheckScene("Assets/Scenes/ConnectionsAdvanced.unity");

            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            SimulationHost host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            ConnectionTrafficDirector traffic =
                UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            Terminal terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            WorkOutput prefab = AssetDatabase.LoadAssetAtPath<WorkOutput>(
                "Assets/Prefabs/Connections/WorkOutput.prefab");
            LoadOutput loadPrefab = AssetDatabase.LoadAssetAtPath<LoadOutput>(
                "Assets/Prefabs/Connections/LoadOutput.prefab");
            bool auto = traffic.AutoGenerationEnabled;
            bool hostile = traffic.HostileGenerationEnabled;
            FieldInfo travelField = typeof(SimulationHost).GetField(
                "connectionTravelSecondsPerLink", BindingFlags.Instance | BindingFlags.NonPublic);
            float travel = (float)travelField.GetValue(host);
            var parent = new GameObject("WORK witness width", typeof(RectTransform));
            ((RectTransform)parent.transform).sizeDelta = new Vector2(320f, 800f);
            WorkOutput db = null;
            WorkOutput secondDb = null;
            WorkOutput auth = null;
            LoadOutput load = null;
            try
            {
                traffic.AutoGenerationEnabled = false;
                traffic.HostileGenerationEnabled = false;
                travelField.SetValue(host, 0.000001f);
                host.SetRunMode(ConnectionRunMode.Generated);
                CheckCommand(terminal, "DB");
                CheckError(terminal, new[] { "work" }, "USAGE");
                CheckError(terminal, new[] { "work", "missing" }, "unknown node");

                db = UnityEngine.Object.Instantiate(prefab, parent.transform);
                secondDb = UnityEngine.Object.Instantiate(prefab, parent.transform);
                auth = UnityEngine.Object.Instantiate(prefab, parent.transform);
                load = UnityEngine.Object.Instantiate(loadPrefab, parent.transform);
                db.Bind("db", host);
                secondDb.Bind("DB", host);
                auth.Bind("auth", host);
                load.Bind("db", host);
                Check(db.ConnectionRowCount == 0 && Empty(db).gameObject.activeSelf &&
                    ((RectTransform)db.transform).rect.width == 320f &&
                    ((RectTransform)db.transform).rect.height == 146f,
                    "empty state and narrow width");
                ((RectTransform)parent.transform).sizeDelta = new Vector2(580f, 800f);
                Refresh(db); Refresh(secondDb); Refresh(auth);

                NodeView[] dbRoute = { Node(host, "EXT1"), Node(host, "API"), Node(host, "DB") };
                NodeView[] authRoute = { Node(host, "EXT1"), Node(host, "API"), Node(host, "AUTH") };
                Queue(traffic, dbRoute, "w1", "Order Synchronization Across Regional Mirrors", false,
                    new OperationStage(OperationKind.Read, 20f, 5f, "first"),
                    new OperationStage(OperationKind.Write, 100f, 4f, "second"));
                Queue(traffic, dbRoute, "w2", "Inventory", true,
                    new OperationStage(OperationKind.Read, 100f, 8f, "inventory"));
                Queue(traffic, authRoute, "w3", "Auth", false,
                    new OperationStage(OperationKind.Execute, 100f, 2f, "auth",
                        behaviors: new[] {
                            TimedModifierBehaviorDefinition.Rate("pause", 0f, 0f, 0f) }));
                host.StepSimulation(.1f);
                Refresh(db); Refresh(secondDb); Refresh(auth);
                Refresh(load);
                Check(db.ConnectionRowCount == 2 && secondDb.ConnectionRowCount == 2 &&
                    auth.ConnectionRowCount == 1 &&
                    Ids(db).SequenceEqual(new[] { "w1", "w2" }) &&
                    LoadIds(load).SequenceEqual(Ids(db)) &&
                    Ids(auth).SequenceEqual(new[] { "w3" }),
                    "new arrivals, independent panels, shared ordering, and target membership");
                LogicalConnectionState first = host.FindConnection("w1");
                WorkRequestRow row = Row(db, "w1");
                int expectedSeconds = Mathf.CeilToInt(first.RemainingWork / first.ActualWorkRate);
                string expectedEta = $"ETA  {expectedSeconds / 60:00}:{expectedSeconds % 60:00}";
                Check(row.DisplayedEta == InspectEntry.CurrentOperationEta(first) &&
                    row.DisplayedEta == expectedEta &&
                    Mathf.Abs(row.FillFraction - InspectEntry.CurrentWorkFraction(first)) < .001f &&
                    first.AllocatedLoad < first.DesiredLoad &&
                    row.GetComponentsInChildren<InspectOperationToken>()
                        .Any(t => t.Text.Contains("READ")) &&
                    !db.GetComponentsInChildren<TMP_Text>(true)
                        .Any(t => t.text.Contains("HOSTILE") || t.text.Contains("BENIGN")),
                    "contended WORK, inspect ETA parity, operation pill, and no threat label");
                CheckTimelineFits(row);
                Check(Row(auth, "w3").DisplayedEta == "ETA  PAUSED" &&
                    host.FindConnection("w3").AllocatedLoad > 0f,
                    "explicit WORK pause matches verbose inspect");
                Check(host.TryInjectInfection("AUTH", InfectionKind.PhantomLoad, 10f,
                    out _), "reserve all AUTH LOAD");
                Refresh(auth);
                Check(Row(auth, "w3").DisplayedEta == "ETA  --:--" &&
                    host.FindConnection("w3").AllocatedLoad == 0f,
                    "zero allocation shows unknown ETA even with a pause modifier");
                Check(((RectTransform)db.transform).rect.height == 270f &&
                    ((RectTransform)auth.transform).rect.height == 190f,
                    "two-row height reflow");
                ((RectTransform)parent.transform).sizeDelta = new Vector2(320f, 800f);
                Refresh(db);
                Check(((RectTransform)db.transform).rect.width == 320f &&
                    Row(db, "w1").GetComponentsInChildren<InspectOperationToken>()
                        .Any(t => t.Text.Contains("READ")) &&
                    new SerializedObject(row).FindProperty("purpose")
                        .objectReferenceValue is TMP_Text purpose &&
                    purpose.overflowMode == TextOverflowModes.Ellipsis,
                    "narrow width keeps the current operation and ellipsizes purpose");
                CheckTimelineFits(row);
                ((RectTransform)parent.transform).sizeDelta = new Vector2(580f, 800f);
                Refresh(db);

                for (int i = 0; i < 100 && first.CurrentOperationIndex == 0; i++)
                    host.StepSimulation(.1f);
                Refresh(db);
                Check(first.CurrentOperationIndex == 1 &&
                    Row(db, "w1") == row &&
                    Ids(db).SequenceEqual(new[] { "w1", "w2" }) &&
                    row.DisplayedEta == InspectEntry.CurrentOperationEta(first) &&
                    row.GetComponentsInChildren<InspectOperationToken>()
                        .Any(t => t.HasCheckmark) &&
                    row.GetComponentsInChildren<InspectOperationToken>()
                        .Any(t => t.Text.Contains("WRITE")),
                    "operation transition retains row, advances pills, and recalculates ETA");
                CheckTimelineFits(row);
                host.StepSimulation(.1f);
                Refresh(db);
                Check(row.FillFraction < .1f &&
                    Mathf.Abs(row.FillFraction - InspectEntry.CurrentWorkFraction(first)) < .001f,
                    "second operation starts with its own WORK progress");

                float slowedEta = first.RemainingWork / first.ActualWorkRate;
                Check(host.KillConnection("w2"), "resolve competing connection");
                host.StepSimulation(.1f);
                Refresh(db); Refresh(secondDb);
                Check(db.ConnectionRowCount == 1 && secondDb.ConnectionRowCount == 1 &&
                    Row(db, "w1") == row &&
                    first.RemainingWork / first.ActualWorkRate < slowedEta &&
                    row.DisplayedEta == InspectEntry.CurrentOperationEta(first),
                    "resolved row retires and recovered allocation changes ETA");

                host.ResetSimulation();
                Refresh(db); Refresh(secondDb); Refresh(auth); Refresh(load);
                Check(db.ConnectionRowCount == 0 && secondDb.ConnectionRowCount == 0 &&
                    auth.ConnectionRowCount == 0 && load.ConnectionRowCount == 0 &&
                    Empty(db).gameObject.activeSelf,
                    "reset clears each panel's old row references");
                Debug.Log("WorkOutputWitness PASS: commands, scene bindings, membership, " +
                    "contention, inspect ETA parity, operation transition, release, reset, widths.");
            }
            finally
            {
                if (db != null) UnityEngine.Object.DestroyImmediate(db.gameObject);
                if (secondDb != null) UnityEngine.Object.DestroyImmediate(secondDb.gameObject);
                if (auth != null) UnityEngine.Object.DestroyImmediate(auth.gameObject);
                if (load != null) UnityEngine.Object.DestroyImmediate(load.gameObject);
                UnityEngine.Object.DestroyImmediate(parent);
                travelField.SetValue(host, travel);
                traffic.AutoGenerationEnabled = auto;
                traffic.HostileGenerationEnabled = hostile;
                host.ResetSimulation();
            }
        }

        private static void CheckScene(string path)
        {
            EditorSceneManager.OpenScene(path);
            Terminal terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            WorkOutput bound = new SerializedObject(terminal).FindProperty("workPrefab")
                .objectReferenceValue as WorkOutput;
            WorkOutput asset = AssetDatabase.LoadAssetAtPath<WorkOutput>(
                "Assets/Prefabs/Connections/WorkOutput.prefab");
            WorkRequestRow row = AssetDatabase.LoadAssetAtPath<WorkRequestRow>(
                "Assets/Prefabs/Connections/WorkRequestRow.prefab");
            Check(bound == asset && row != null &&
                new SerializedObject(asset).FindProperty("rowPrefab")
                    .objectReferenceValue == row &&
                row.GetComponentsInChildren<IdentityPillView>(true).Length == 1 &&
                row.GetComponentsInChildren<InspectOperationToken>(true).Length == 0,
                "authored scene and prefab binding in " + path);
        }

        private static void Queue(ConnectionTrafficDirector traffic, NodeView[] route,
            string id, string purpose, bool hostile, params OperationStage[] operations) =>
            traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, id, route,
                "work-witness", purpose, operations, hostile));

        private static NodeView Node(SimulationHost host, string id) =>
            host.FindNodeResources(id).View;

        private static void Refresh(WorkOutput panel) => RefreshMethod.Invoke(panel, null);

        private static void Refresh(LoadOutput panel) => LoadRefreshMethod.Invoke(panel, null);

        private static WorkRequestRow[] Rows(WorkOutput panel) =>
            panel.GetComponentsInChildren<WorkRequestRow>(true)
                .Where(r => r.gameObject.activeSelf).ToArray();

        private static string[] Ids(WorkOutput panel) =>
            Rows(panel).Select(r => r.DisplayedId).ToArray();

        private static string[] LoadIds(LoadOutput panel) =>
            panel.GetComponentsInChildren<LoadRequestRow>(true)
                .Where(r => r.gameObject.activeSelf).Select(r => r.DisplayedId).ToArray();

        private static WorkRequestRow Row(WorkOutput panel, string id) =>
            Rows(panel).Single(r => r.DisplayedId == id);

        private static TMP_Text Empty(WorkOutput panel) =>
            (TMP_Text)new SerializedObject(panel).FindProperty("emptyText").objectReferenceValue;

        private static void CheckCommand(Terminal terminal, string id)
        {
            RectTransform content = (RectTransform)new SerializedObject(terminal)
                .FindProperty("historyContent").objectReferenceValue;
            int before = content.childCount;
            typeof(Terminal).GetMethod("Work", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(terminal, new object[] { new[] { "work", id } });
            Check(content.childCount == before + 1 &&
                content.GetChild(before).GetComponent<WorkOutput>() != null,
                "command appends live output");
        }

        private static void CheckError(Terminal terminal, string[] tokens, string expected)
        {
            RectTransform content = (RectTransform)new SerializedObject(terminal)
                .FindProperty("historyContent").objectReferenceValue;
            int before = content.childCount;
            typeof(Terminal).GetMethod("Work", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(terminal, new object[] { tokens });
            Check(content.childCount == before + 1 &&
                content.GetChild(before).GetComponentInChildren<TMP_Text>().text.Contains(expected),
                "command error " + expected);
        }

        private static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException("WorkOutputWitness: " + message);
        }

        private static void CheckTimelineFits(WorkRequestRow row)
        {
            RectTransform timeline = (RectTransform)new SerializedObject(row)
                .FindProperty("operationRow").objectReferenceValue;
            LayoutRebuilder.ForceRebuildLayoutImmediate(timeline);
            var corners = new Vector3[4];
            foreach (InspectOperationToken token in
                timeline.GetComponentsInChildren<InspectOperationToken>())
            {
                ((RectTransform)token.transform).GetWorldCorners(corners);
                float left = timeline.InverseTransformPoint(corners[0]).x;
                float right = timeline.InverseTransformPoint(corners[3]).x;
                Check(left >= timeline.rect.xMin - 1f &&
                    right <= timeline.rect.xMax + 1f,
                    "operation pills stay inside the authored timeline width");
            }
        }
    }
}
