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
    public static class LogsOutputWitness
    {
        [MenuItem("Tools/Connections/Run LOGS Output Witness")]
        public static void Run()
        {
            Parse();
            Scene("Assets/Scenes/Connections.unity", false);
            Scene("Assets/Scenes/ConnectionsAdvanced.unity", true);
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            var prefab = AssetDatabase.LoadAssetAtPath<LogsOutput>(
                "Assets/Prefabs/Connections/LogsOutput.prefab");
            var parent = new GameObject("LOGS witness width", typeof(RectTransform));
            var parentRect = (RectTransform)parent.transform;
            parentRect.sizeDelta = new Vector2(320f, 800f);
            bool auto = traffic.AutoGenerationEnabled;
            bool hostile = traffic.HostileGenerationEnabled;
            var travelField = typeof(SimulationHost).GetField("connectionTravelSecondsPerLink",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float travel = (float)travelField.GetValue(host);
            LogsOutput five = null, one = null, twenty = null, historical = null;
            try
            {
                traffic.AutoGenerationEnabled = false;
                traffic.HostileGenerationEnabled = false;
                travelField.SetValue(host, 1f);
                host.SetRunMode(ConnectionRunMode.Generated);
                host.ResetSimulation();
                recorder.BeginRun();
                NodeView[] route = { host.FindNodeResources("EXT1").View,
                    host.FindNodeResources("API").View, host.FindNodeResources("DB").View };
                // A pending plan and its established state share the same card binding.
                var plan = new ConnectionSpawnPlan(2f, "p1", route,
                    "logs-witness", "Diagnostic", new[] { Stage(OperationKind.Read, 20f,
                        Log(.02f, "read-a"), Log(.03f, "read-b"),
                        Log(.04f, "read-c"), Log(.05f, "read-d"),
                        Log(.06f, "read-e"), Log(.07f, "read-f"),
                        Log(.08f, "read-g")) , Stage(OperationKind.Write, 20f,
                        Log(.02f, "write-a"), Log(.03f, "write-b")) }, false);
                var definition = LogicalConnectionDefinition.FromPlan(plan);
                typeof(LogicalConnectionDefinition).GetField("startDelaySeconds",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(definition, 2f);
                host.RegisterConnection(definition);
                var state = host.FindConnection("p1");
                Check(state != null && state.IsPending, "pending connection available by canonical ID");
                five = UnityEngine.Object.Instantiate(prefab, parent.transform);
                one = UnityEngine.Object.Instantiate(prefab, parent.transform);
                twenty = UnityEngine.Object.Instantiate(prefab, parent.transform);
                five.Bind(state, host, 5);
                one.Bind(state, host, 1);
                twenty.Bind(state, host, 20);
                Check(Empty(five).text == "WAITING FOR CONNECTION" &&
                    five.DisplayedRowCount == 0 && Height(five) == 70f &&
                    Width(five) <= 320f, "pending compact state and narrow width");
                host.StepSimulation(2f);
                five.RefreshNow(0f);
                Check(state.IsConnecting && Empty(five).text == "CONNECTING",
                    "connecting empty state");
                host.StepSimulation(2f);
                five.RefreshNow(0f);
                Check(state.IsActive && Empty(five).text == "NO LOGS EMITTED",
                    "established no-log state");
                int recorderBefore = recorder.CurrentLog.events.Count;
                float workBefore = state.CompletedWork;
                five.RefreshNow(0f); one.RefreshNow(0f); twenty.RefreshNow(0f);
                Check(recorder.CurrentLog.events.Count == recorderBefore &&
                    state.CompletedWork == workBefore,
                    "UI refresh has no recorder or simulation side effects");
                host.StepSimulation(1f);
                five.RefreshNow(0f); one.RefreshNow(0f); twenty.RefreshNow(0f);
                Check(state.EmittedLogs.Count == 7 &&
                    Activities(five).SequenceEqual(new[] { "read-c", "read-d", "read-e",
                        "read-f", "read-g" }) &&
                    Activities(one).SequenceEqual(new[] { "read-g" }) &&
                    Activities(twenty).SequenceEqual(new[] { "read-a", "read-b", "read-c",
                        "read-d", "read-e", "read-f", "read-g" }) &&
                    Height(five) == 185f && Height(one) == 93f &&
                    Height(twenty) == 231f,
                    "burst, independent tails, retained history, and content height");
                Check(Rows(five).All(r => r.OperationText.text == "READ") &&
                    Rows(five).All(r => r.TimeText.text.Contains(":")) &&
                    Rows(five).All(r => r.ResultText.text == "OK"),
                    "row shows timestamp, originating operation, and result");
                // A second invocation must read older earned history, not replay the recorder.
                CheckCommand(terminal, new[] { "logs", "-n", "20", "P1" }, 20, 7);
                CheckError(terminal, new[] { "logs", "missing" }, "unknown connection");
                CheckError(terminal, new[] { "logs", "-x", "p1" }, "invalid flag");
                CheckError(terminal, new[] { "logs", "-n", "0", "p1" }, "USAGE");
                host.StepSimulation(12f);
                host.StepSimulation(1f);
                five.RefreshNow(0f); twenty.RefreshNow(0f);
                Check(state.CurrentOperationIndex == 1 &&
                    Activities(twenty).Take(7).All(a => a.StartsWith("read-")) &&
                    Activities(twenty).Skip(7).All(a => a.StartsWith("write-")) &&
                    Rows(twenty).Last().OperationText.text == "WRITE" &&
                    state.EmittedLogs.Count == 9,
                    "operation transition preserves previous-operation entries: op=" +
                    state.CurrentOperationIndex + " emitted=" + state.EmittedLogs.Count +
                    " rows=" + string.Join(",", Activities(twenty)));
                host.StepSimulation(20f);
                five.RefreshNow(0f);
                Check(state.IsComplete && state.Resolution == ConnectionResolution.Completed &&
                    !five.IsSummary && Activities(five).Last() == "write-b",
                    "completion retains earned tail during hold");
                five.RefreshNow(4.3f);
                Check(five.IsSummary && Height(five) == 48f,
                    "live card collapses after short resolved hold");
                historical = UnityEngine.Object.Instantiate(prefab, parent.transform);
                historical.Bind(state, host, 20);
                historical.RefreshNow(100f);
                Check(historical.IsHistoricalSnapshot && !historical.IsSummary &&
                    historical.DisplayedRowCount == 9,
                    "historical request remains a readable static snapshot");
                Check(CheckCommand(terminal, new[] { "logs", "P1" }, 5, 5)
                    .IsHistoricalSnapshot, "historical canonical ID works through terminal");
                host.ResetSimulation();
                historical.RefreshNow(200f);
                Check(historical.IsSummary && Height(historical) == 48f &&
                    Summary(historical).text.Contains("RUN RESET") &&
                    historical.DisplayedRowCount == 0,
                    "reset invalidates old binding without following reused ID");
                ResolutionCases(host, recorder, route, prefab, parent.transform, travelField);
                Check(terminal != null, "terminal remains present");
                Debug.Log("LogsOutputWitness PASS: parsing, scene bindings, pending/connecting/empty, " +
                    "burst and retained tails, command addressability, operation transition, " +
                    "completion hold, kill/cancel/withdraw, historical snapshot, reset, " +
                    "narrow width, recorder isolation.");
            }
            catch (Exception failure)
            {
                Debug.LogException(failure);
                throw;
            }
            finally
            {
                foreach (var panel in new[] { five, one, twenty, historical })
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel.gameObject);
                UnityEngine.Object.DestroyImmediate(parent);
                travelField.SetValue(host, travel);
                traffic.AutoGenerationEnabled = auto;
                traffic.HostileGenerationEnabled = hostile;
                host.ResetSimulation();
            }
        }

        private static void Parse()
        {
            Check(Terminal.TryParseLogs(new[] { "LoGs", "X1" }, out string id,
                out int n, out _) && id == "X1" && n == 5, "default parse");
            foreach (int count in new[] { 1, 20, 30 })
                Check(Terminal.TryParseLogs(new[] { "logs", "-n", count.ToString(), "x1" },
                    out id, out n, out _) && id == "x1" && n == count,
                    "explicit tail " + count);
            foreach (string[] bad in new[] { new[] { "logs" },
                new[] { "logs", "x1", "x2" }, new[] { "logs", "-v", "x1" },
                new[] { "logs", "-n", "0", "x1" },
                new[] { "logs", "-n", "31", "x1" },
                new[] { "logs", "-n", "1.5", "x1" },
                new[] { "logs", "-n", "-1", "x1" },
                new[] { "logs", "-n", "1" } })
                Check(!Terminal.TryParseLogs(bad, out _, out _, out _),
                    "reject malformed command: " + string.Join(" ", bad));
        }

        private static void Scene(string path, bool advanced)
        {
            EditorSceneManager.OpenScene(path);
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var output = AssetDatabase.LoadAssetAtPath<LogsOutput>(
                "Assets/Prefabs/Connections/LogsOutput.prefab");
            var row = AssetDatabase.LoadAssetAtPath<LogsEntryRow>(
                "Assets/Prefabs/Connections/LogsEntryRow.prefab");
            Check(output != null && row != null &&
                new SerializedObject(terminal).FindProperty("logsPrefab")
                    .objectReferenceValue == output &&
                new SerializedObject(output).FindProperty("rowPrefab")
                    .objectReferenceValue == row,
                "authored LOGS card and row in " + path);
            if (advanced)
            {
                var field = typeof(ConnectionTrafficDirector).GetField("profiles",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var profiles = (ConnectionTrafficProfile[])field.GetValue(traffic);
                var demo = profiles.Single(p => p.id == "logs-demo");
                Check(demo.ambientWeight == 0f && demo.operations.Length == 2 &&
                    demo.operations.Sum(p => p.logs.Length) > 5,
                    "advanced scene has non-ambient two-operation demo");
            }
            else Check(traffic.EffectiveAmbientWeight("logs-demo") == 0f,
                "default-scene demo has zero ambient weight");
        }

        private static OperationStage Stage(OperationKind kind, float work,
            params PlannedOperationLog[] logs) =>
            new OperationStage(kind, work, 2f, "logs witness", logs: logs);

        private static void ResolutionCases(SimulationHost host, ConnectionsRunRecorder recorder,
            NodeView[] route, LogsOutput prefab, Transform parent, FieldInfo travelField)
        {
            travelField.SetValue(host, .000001f);
            foreach (ConnectionResolution expected in new[] { ConnectionResolution.Killed,
                ConnectionResolution.Canceled, ConnectionResolution.Withdrawn })
            {
                host.ResetSimulation();
                recorder.BeginRun();
                var operation = expected == ConnectionResolution.Withdrawn ?
                    new OperationStage(OperationKind.Read, 20f, 2f, "logs witness",
                        behaviors: new[] {
                            TimedModifierBehaviorDefinition.Rate("LOADER_ACTIVE", 1f, 0f, 0f) },
                        logs: new[] { Log(.1f, "earned"), Log(.9f, "unearned") }) :
                    Stage(OperationKind.Read, 20f, Log(.1f, "earned"),
                        Log(.9f, "unearned"));
                var loader = expected == ConnectionResolution.Withdrawn ? new LoaderAttackPlan
                {
                    structure = LoaderStructure.Single, pattern = LoaderPattern.Immediate,
                    attackOperationIndex = 0, targetCapacity = 10f,
                    normalLoad = 2f, attackLoad = 8f,
                    onsetSeconds = 1f, durationSeconds = 1f
                } : null;
                var plan = new ConnectionSpawnPlan(0f, "case", route, "logs-witness",
                    "Diagnostic", new[] { operation }, expected == ConnectionResolution.Withdrawn,
                    loaderAttack: loader);
                var state = host.RegisterConnection(LogicalConnectionDefinition.FromPlan(plan));
                host.StepSimulation(.01f);
                host.StepSimulation(1f);
                Check(state.EmittedLogs.Count == 1, expected + " earned one entry");
                LogsOutput live = UnityEngine.Object.Instantiate(prefab, parent);
                LogsOutput snapshot = null;
                try
                {
                    live.Bind(state, host, 5);
                    if (expected == ConnectionResolution.Killed)
                        Check(host.KillConnection("case"), "kill resolution command");
                    else if (expected == ConnectionResolution.Canceled)
                        Check(host.InterruptForIsolation(host.FindNodeResources("DB")) == 1,
                            "isolation cancellation");
                    else host.StepSimulation(2f);
                    live.RefreshNow(0f);
                    Check(state.Resolution == expected &&
                        Status(live).text == expected.ToString().ToUpperInvariant() &&
                        Activities(live).SequenceEqual(new[] { "earned" }),
                        expected + " live card retains earned entry and outcome");
                    snapshot = UnityEngine.Object.Instantiate(prefab, parent);
                    snapshot.Bind(state, host, 20);
                    snapshot.RefreshNow(100f);
                    Check(snapshot.IsHistoricalSnapshot && !snapshot.IsSummary &&
                        Activities(snapshot).SequenceEqual(new[] { "earned" }),
                        expected + " historical request stays expanded");
                    host.StepSimulation(20f);
                    live.RefreshNow(4.3f);
                    Check(live.IsSummary && state.EmittedLogs.Count == 1 &&
                        Summary(live).text.Contains(expected.ToString().ToUpperInvariant()),
                        expected + " summary and no fabricated late entries");
                }
                finally
                {
                    if (snapshot != null) UnityEngine.Object.DestroyImmediate(snapshot.gameObject);
                    UnityEngine.Object.DestroyImmediate(live.gameObject);
                }
            }
        }

        private static PlannedOperationLog Log(float fraction, string activity) =>
            new PlannedOperationLog(fraction, activity, "OK");

        private static LogsEntryRow[] Rows(LogsOutput panel) =>
            panel.GetComponentsInChildren<LogsEntryRow>(true)
                .Where(r => r.gameObject.activeSelf).OrderBy(r => r.transform.GetSiblingIndex())
                .ToArray();

        private static string[] Activities(LogsOutput panel) =>
            Rows(panel).Select(r => r.ActivityText.text).ToArray();

        private static TMP_Text Empty(LogsOutput panel) =>
            (TMP_Text)new SerializedObject(panel).FindProperty("emptyText").objectReferenceValue;

        private static TMP_Text Summary(LogsOutput panel) =>
            (TMP_Text)new SerializedObject(panel).FindProperty("summaryText").objectReferenceValue;

        private static TMP_Text Status(LogsOutput panel) =>
            (TMP_Text)new SerializedObject(panel).FindProperty("statusText").objectReferenceValue;

        private static float Height(LogsOutput panel) =>
            ((RectTransform)panel.transform).rect.height;

        private static float Width(LogsOutput panel) =>
            ((RectTransform)panel.transform).rect.width;

        private static LogsOutput CheckCommand(Terminal terminal, string[] tokens,
            int tail, int expectedRows)
        {
            var content = (RectTransform)new SerializedObject(terminal)
                .FindProperty("historyContent").objectReferenceValue;
            int before = content.childCount;
            typeof(Terminal).GetMethod("Logs", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(terminal, new object[] { tokens, string.Join(" ", tokens) });
            Check(content.childCount == before + 1 &&
                content.GetChild(before).GetComponent<LogsOutput>() is LogsOutput panel &&
                panel.TailCount == tail && panel.DisplayedRowCount == expectedRows,
                "command appends correctly bound tail");
            return content.GetChild(before).GetComponent<LogsOutput>();
        }

        private static void CheckError(Terminal terminal, string[] tokens, string expected)
        {
            var content = (RectTransform)new SerializedObject(terminal)
                .FindProperty("historyContent").objectReferenceValue;
            int before = content.childCount;
            typeof(Terminal).GetMethod("Logs", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(terminal, new object[] { tokens, string.Join(" ", tokens) });
            Check(content.childCount == before + 1 &&
                content.GetChild(before).GetComponentInChildren<TMP_Text>().text.Contains(expected),
                "terminal error: " + expected);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("LogsOutputWitness: " + message);
        }
    }
}
