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
            LogsOutput five = null, one = null, fifteen = null, twenty = null,
                historical = null;
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
                        new PlannedOperationLog(.06f, "read-e", "START"), Log(.07f, "read-f"),
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
                fifteen = UnityEngine.Object.Instantiate(prefab, parent.transform);
                twenty = UnityEngine.Object.Instantiate(prefab, parent.transform);
                five.Bind(state, host, 5);
                one.Bind(state, host, 1);
                fifteen.Bind(state, host, 15);
                twenty.Bind(state, host, 20);
                Check(Empty(five).text == "WAITING FOR CONNECTION" &&
                    five.DisplayedRowCount == 0 && Height(five) == 80f &&
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
                five.RefreshNow(0f); one.RefreshNow(0f);
                fifteen.RefreshNow(0f); twenty.RefreshNow(0f);
                Check(recorder.CurrentLog.events.Count == recorderBefore &&
                    state.CompletedWork == workBefore,
                    "UI refresh has no recorder or simulation side effects");
                host.StepSimulation(1f);
                five.RefreshNow(0f); one.RefreshNow(0f);
                fifteen.RefreshNow(0f); twenty.RefreshNow(0f);
                Check(state.EmittedLogs.Count == 7 &&
                    Activities(five).SequenceEqual(new[] { "read-c", "read-d", "read-e",
                        "read-f", "read-g" }) &&
                    Activities(one).SequenceEqual(new[] { "read-g" }) &&
                    Activities(twenty).SequenceEqual(new[] { "read-a", "read-b", "read-c",
                        "read-d", "read-e", "read-f", "read-g" }) &&
                    Activities(fifteen).SequenceEqual(Activities(twenty)) &&
                    Height(five) == 320f && Height(one) == 128f &&
                    Height(twenty) == 416f,
                    "burst, independent tails, retained history, and content height");
                Check(Rows(five).All(r => r.OperationText.text == "READ") &&
                    Rows(five).All(r => r.TimeText.text.Contains(":")) &&
                    Rows(five).Any(r => r.ResultText.text == "START") &&
                    Rows(five).All(r => r.ActivityText.fontSize >= 15f) &&
                    !Rows(five).First().ResultText.gameObject.activeSelf &&
                    Rows(five).First().ActivityText.rectTransform.rect.width >= 190f &&
                    Rows(five).First().ActivityText.rectTransform.anchoredPosition.y == -24f &&
                    five.ClipBounds != null && five.ClipBounds.GetComponent<RectMask2D>() != null &&
                    Mathf.Abs(five.ClipBounds.rect.height - Height(five) + 12f) < .01f,
                    "row shows timestamp, originating operation, and result");
                parentRect.sizeDelta = new Vector2(580f, 800f);
                five.RefreshNow(0f); fifteen.RefreshNow(0f);
                Check(Width(five) <= 580f &&
                    Rows(five).First().ResultText.gameObject.activeSelf &&
                    Rows(five).First().ActivityText.rectTransform.rect.width > 200f &&
                    Rows(five).All(r => r.TimeText.GetPreferredValues(r.TimeText.text).x <=
                        r.TimeText.rectTransform.rect.width) &&
                    Rows(five).Where(r => r.ResultText.text == "START").All(r =>
                        r.ResultText.GetPreferredValues(r.ResultText.text).x <=
                        r.ResultText.rectTransform.rect.width) &&
                    Rows(five).First().ActivityText.overflowMode == TextOverflowModes.Ellipsis &&
                    !five.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "LIVE"),
                    "wide row readability, ellipsis, and no LIVE header");
                parentRect.sizeDelta = new Vector2(320f, 800f);
                five.RefreshNow(0f);
                // A second invocation must read older earned history, not replay the recorder.
                CheckCommand(terminal, new[] { "logs", "-n", "20", "P1" }, 20, 7);
                CheckError(terminal, new[] { "logs", "missing" }, "unknown connection");
                CheckError(terminal, new[] { "logs", "-x", "p1" }, "invalid flag");
                CheckError(terminal, new[] { "logs", "-n", "0", "p1" }, "USAGE");
                host.StepSimulation(12f);
                host.StepSimulation(1f);
                five.RefreshNow(0f); fifteen.RefreshNow(0f); twenty.RefreshNow(0f);
                Check(state.CurrentOperationIndex == 1 &&
                    Activities(twenty).Take(7).All(a => a.StartsWith("read-")) &&
                    Activities(twenty).Skip(7).All(a => a.StartsWith("write-")) &&
                    Rows(twenty).Last().OperationText.text == "WRITE" &&
                    fifteen.DisplayedRowCount == 9 && Height(fifteen) == 512f &&
                    state.EmittedLogs.Count == 9,
                    "operation transition preserves previous-operation entries: op=" +
                    state.CurrentOperationIndex + " emitted=" + state.EmittedLogs.Count +
                    " rows=" + string.Join(",", Activities(twenty)));
                host.StepSimulation(20f);
                five.RefreshNow(0f);
                Check(state.IsComplete && state.Resolution == ConnectionResolution.Completed &&
                    !five.IsSummary && Activities(five).Last() == "write-b" &&
                    Purpose(five).color == Quiet(five) &&
                    Pill(five).AccentColor == Quiet(five) &&
                    Rows(five).All(r => r.ActivityText.color == Quiet(five)),
                    "completion immediately mutes the expanded earned tail");
                five.RefreshNow(4.1f);
                Check(!five.IsSummary && Height(five) < 320f && Height(five) > 48f &&
                    Mathf.Abs(five.ClipBounds.rect.height - Height(five) + 12f) < .01f,
                    "clipping boundary follows the shrinking card");
                five.RefreshNow(4.3f);
                Check(five.IsSummary && Height(five) == 48f,
                    "live card collapses after short resolved hold");
                historical = UnityEngine.Object.Instantiate(prefab, parent.transform);
                historical.Bind(state, host, 20);
                float historicalOpenedAt = Time.unscaledTime;
                historical.RefreshNow(historicalOpenedAt + 3.9f);
                Check(!historical.IsSummary &&
                    historical.DisplayedRowCount == 9,
                    "historical request is briefly readable and muted");
                historical.RefreshNow(historicalOpenedAt + 4.3f);
                Check(historical.IsSummary && Height(historical) == 48f,
                    "historical request collapses on the same hold");
                LogsOutput historicalCommand = CheckCommand(terminal,
                    new[] { "logs", "P1" }, 5, 5);
                historicalCommand.RefreshNow(Time.unscaledTime + 4.3f);
                Check(historicalCommand.IsSummary,
                    "historical canonical command also collapses");
                host.ResetSimulation();
                historical.RefreshNow(200f);
                Check(historical.IsSummary && Height(historical) == 48f &&
                    Summary(historical).text.Contains("RUN RESET") &&
                    historical.DisplayedRowCount == 0,
                    "reset invalidates old binding without following reused ID");
                ResolutionCases(host, recorder, route, prefab, parent.transform, travelField);
                IdentityCases(host, traffic, recorder, terminal, route, travelField);
                Check(terminal != null, "terminal remains present");
                Debug.Log("LogsOutputWitness PASS: parsing, scene bindings, pending/connecting/empty, " +
                    "burst and retained tails, command addressability, operation transition, " +
                    "completion hold, kill/cancel/withdraw, historical collapse, reset, " +
                    "canonical hopping identity, clipping, narrow and wide widths, recorder isolation.");
            }
            catch (Exception failure)
            {
                Debug.LogException(failure);
                throw;
            }
            finally
            {
                foreach (var panel in new[] { five, one, fifteen, twenty, historical })
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
            foreach (int count in new[] { 1, 15, 20, 30 })
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
                    .objectReferenceValue == row &&
                new SerializedObject(output).FindProperty("identityPill")
                    .objectReferenceValue is IdentityPillView &&
                output.ClipBounds != null &&
                output.ClipBounds.GetComponent<RectMask2D>() != null,
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
                        Purpose(live).color == Quiet(live) &&
                        Pill(live).AccentColor == Quiet(live) &&
                        Rows(live).All(r => r.ActivityText.color == Quiet(live)) &&
                        Activities(live).SequenceEqual(new[] { "earned" }),
                        expected + " live card immediately mutes earned entry");
                    snapshot = UnityEngine.Object.Instantiate(prefab, parent);
                    snapshot.Bind(state, host, 20);
                    float openedAt = Time.unscaledTime;
                    snapshot.RefreshNow(openedAt + 3.9f);
                    Check(!snapshot.IsSummary &&
                        Activities(snapshot).SequenceEqual(new[] { "earned" }),
                        expected + " historical request has a short readable hold");
                    snapshot.RefreshNow(openedAt + 4.3f);
                    Check(snapshot.IsSummary,
                        expected + " historical request also collapses");
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

        private static void IdentityCases(SimulationHost host, ConnectionTrafficDirector traffic,
            ConnectionsRunRecorder recorder, Terminal terminal, NodeView[] route,
            FieldInfo travelField)
        {
            host.ResetSimulation();
            recorder.BeginRun();
            travelField.SetValue(host, .000001f);
            traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "h1", route,
                "logs-identity-witness", "Diagnostic", new[] {
                    Stage(OperationKind.Read, 1000f) }, true,
                identityHopSeconds: 2f, identitySeed: 91));
            host.StepSimulation(.01f);
            LogicalConnectionState state = host.FindConnection("h1");
            string alias = state.KnownIdentity;
            Check(alias != "h1" && host.FindConnection(alias) == null,
                "disguised alias is not canonical");
            CheckError(terminal, new[] { "logs", alias }, "unknown connection");
            LogsOutput panel = CheckCommand(terminal, new[] { "logs", "H1" }, 5, 0);
            Check(Pill(panel).DisplayedId == "h1", "LOGS pill shows verified canonical ID");
            int recorderBefore = recorder.CurrentLog.events.Count;
            float workBefore = state.CompletedWork;
            panel.RefreshNow(Time.unscaledTime);
            Check(recorder.CurrentLog.events.Count == recorderBefore &&
                state.CompletedWork == workBefore, "identity refresh has no simulation side effects");
            host.StepSimulation(2f);
            Check(state.KnownIdentity != alias, "disguised identity hopped");
            panel.RefreshNow(Time.unscaledTime);
            Check(Pill(panel).DisplayedId == "h1", "verified pill is stable across hops");
            PhysicalLinkView edge = host.PhysicalLinks.Select(link => link.View).Single(link =>
                link.NodeA.NodeId == "API" && link.NodeB.NodeId == "DB" ||
                link.NodeA.NodeId == "DB" && link.NodeB.NodeId == "API");
            Check(host.TryActivateProbe(edge.LinkId, "probe " + edge.LinkId, out _) &&
                state.KnownIdentity == "h1", "Probe temporarily reveals canonical ID");
            host.StepSimulation(host.ProbeActiveRemaining);
            panel.RefreshNow(Time.unscaledTime);
            Check(state.KnownIdentity != "h1" && Pill(panel).DisplayedId == "h1",
                "Probe expiration cannot change verified LOGS identity");
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

        private static TMP_Text Purpose(LogsOutput panel) =>
            (TMP_Text)new SerializedObject(panel).FindProperty("purposeText").objectReferenceValue;

        private static IdentityPillView Pill(LogsOutput panel) =>
            (IdentityPillView)new SerializedObject(panel).FindProperty("identityPill")
                .objectReferenceValue;

        private static Color Quiet(LogsOutput panel) =>
            new SerializedObject(panel).FindProperty("quietColor").colorValue;

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
