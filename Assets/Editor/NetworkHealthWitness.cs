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
    public static class NetworkHealthWitness
    {
        [MenuItem("Tools/Connections/Run Network Health Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            Check(host != null && director != null, "host and traffic director");
            var panel = UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>();
            CheckPanelBindings(host);
            CheckHistoryResize(panel);
            var settings = new SerializedObject(host).FindProperty("healthTuning");
            Near(settings.FindPropertyRelative("deadlineTolerance").floatValue, 1.5f,
                "scene deadline tolerance");
            Near(settings.FindPropertyRelative("slowdownDrainPerSecond").floatValue, 0.25f,
                "scene slowdown tuning");
            Near(settings.FindPropertyRelative("overdueDrainPerSecond").floatValue, 0.5f,
                "scene overdue tuning");
            Near(settings.FindPropertyRelative("overdueEscalationPerSecond").floatValue, 0.02f,
                "scene escalation tuning");
            Near(settings.FindPropertyRelative("recoveryPerSecond").floatValue, 0.35f,
                "scene recovery tuning");
            Near(settings.FindPropertyRelative("incorrectKillPenalty").floatValue, 10f,
                "scene kill tuning");
            Near(settings.FindPropertyRelative("benignCancellationPenalty").floatValue, 5f,
                "scene cancellation tuning");
            Near(settings.FindPropertyRelative("substantialSlowdownThreshold").floatValue, 0.98f,
                "scene substantial slowdown threshold");
            bool auto = director.AutoGenerationEnabled;
            director.AutoGenerationEnabled = false;
            try
            {
                host.SetRunMode(ConnectionRunMode.Generated);
                Check(host.Health.Value == 100f && !host.Health.Failed, "reset to 100");
                var ordinary = Add(host, "ordinary", "DB", false,
                    new OperationStage(OperationKind.Read, 10f, 5f, "ordinary"));
                Near(ordinary.HealthDeadlineSeconds, 3f, "nominal 2s with 1.5 tolerance");
                host.StepSimulation(1f);
                Near(host.Health.Value, 100f, "ordinary service does not drain");
                host.StepSimulation(1f);
                Check(ordinary.Resolution == ConnectionResolution.Completed &&
                    !ordinary.HealthWasSubstantiallySlowed && !ordinary.HealthBecameLate,
                    "ordinary completion facts");
                Near(host.Health.Value, 100f, "completion adds no duration damage");
                Debug.Log("NetworkHealthWitness ordinary: expected 100, observed " + host.Health.Value);

                host.ResetSimulation();
                var a = Add(host, "a", "DB", false,
                    new OperationStage(OperationKind.Write, 100f, 10f, "a"));
                Add(host, "b", "DB", false,
                    new OperationStage(OperationKind.Write, 100f, 10f, "b"));
                Add(host, "c", "DB", false,
                    new OperationStage(OperationKind.Write, 100f, 10f, "c"));
                var independent = Add(host, "independent", "AUTH", false,
                    new OperationStage(OperationKind.Read, 100f, 5f, "independent"));
                host.StepSimulation(1f);
                Near(a.AllocatedLoad, 10f / 3f, "shared target allocation");
                float observedSharedGrant = a.AllocatedLoad;
                Near(independent.AllocatedLoad, 5f, "independent target allocation");
                Near(host.Health.DegradationRate, 0.5f, "three shortfall impacts");
                Near(host.Health.RecoveryRate, 0f, "no recovery during damage");
                Near(host.Health.NetRate, -0.5f, "actual signed net rate");
                Near(host.Health.Value, 99.5f, "one second drain");
                Check(host.Health.SlowedCount == 3 && host.Health.OverdueCount == 0 &&
                    host.Health.Impacts.Count == 3 && a.HealthWasSubstantiallySlowed,
                    "slowed facts and aggregate");
                panel.Render();
                CheckHealthBar(panel, 99.5f);
                var panelFields = new SerializedObject(panel);
                Check(((TMP_Text)panelFields.FindProperty("rateText").objectReferenceValue).text ==
                    "-0.50/s", "compact signed rate");
                for (int i = 0; i < 14; i++) host.StepSimulation(1f);
                Check(host.Health.OverdueCount == 0, "deadline is not late at exact boundary");
                host.StepSimulation(1f);
                float firstOverdue = host.Health.DegradationRate;
                Near(firstOverdue, 1.53f, "first overdue rate replaces slowdown rate");
                Check(host.Health.OverdueCount == 3 && a.HealthBecameLate &&
                    host.Health.RecoveryRate == 0f,
                    "overdue replaces slowdown drain");
                host.StepSimulation(1f);
                Near(host.Health.DegradationRate, 1.59f, "overdue escalates");
                float beforeKill = host.Health.Value;
                Check(host.KillConnection("a"), "legitimate kill");
                Near(host.Health.Value, beforeKill - 10f, "one immediate kill penalty");
                Check(a.Resolution == ConnectionResolution.Killed &&
                    a.ResolvedAtSeconds == host.SimulationTimeSeconds &&
                    host.Health.RecentEvents.Count == 1 &&
                    host.Health.SlowedCount == 2 && host.Health.OverdueCount == 2 &&
                    host.Health.Impacts.All(x => x.ConnectionId != "a"),
                    "resolution facts and released impact");
                Check(host.KillConnection("b") && host.KillConnection("c"),
                    "remaining legitimate kills");
                Check(host.Health.SlowedCount == 0 && host.Health.OverdueCount == 0 &&
                    host.Health.RecoveryRate == 0.35f,
                    "all damaging conditions released");
                float beforeRecovery = host.Health.Value;
                host.StepSimulation(5f);
                Near(host.Health.Value, beforeRecovery + 1.75f,
                    "recovery after final damage clears");
                Near(host.Health.NetRate, 0.35f, "signed recovery rate");
                CheckHealthBar(panel, host.Health.Value);
                Debug.Log($"NetworkHealthWitness contention: expected 3.333 LOAD, -0.50/s, first overdue -1.53/s, next -1.59/s, -10 kill, +1.75 over 5s recovery; observed {observedSharedGrant:0.###} LOAD, {firstOverdue:0.###}/s first late, {host.Health.Value:0.###} after recovery");

                host.ResetSimulation();
                var benignA = Add(host, "benign-a", "DB", false,
                    new OperationStage(OperationKind.Read, 100f, 10f, "benign-a"));
                var benignB = Add(host, "benign-b", "DB", false,
                    new OperationStage(OperationKind.Read, 100f, 10f, "benign-b"));
                var mixedHostile = Add(host, "mixed-hostile", "DB", true,
                    new OperationStage(OperationKind.Write, 10f, 10f, "mixed-hostile"));
                host.StepSimulation(1f);
                Near(host.Health.DegradationRate, 1f / 3f,
                    "two legitimate shortfalls only");
                Check(host.Health.SlowedCount == 3 && host.Health.Impacts.Count == 2 &&
                    mixedHostile.HealthWasSubstantiallySlowed,
                    "all visible slowdown counted without hostile Health impact");
                host.StepSimulation(1f);
                Check(host.Health.OverdueCount == 1 && mixedHostile.HealthBecameLate &&
                    !benignA.HealthBecameLate && !benignB.HealthBecameLate,
                    "hostile late history and observable count");
                host.StepSimulation(1f);
                Check(mixedHostile.Resolution == ConnectionResolution.Completed &&
                    host.Health.SlowedCount == 2 && host.Health.OverdueCount == 0,
                    "completion removes hostile observable condition");
                Check(host.KillConnection("benign-a") && host.Health.SlowedCount == 1,
                    "legitimate kill removes observable condition");
                Check(host.KillConnection("benign-b") && host.Health.SlowedCount == 0,
                    "last legitimate kill removes observable condition");
                host.StepSimulation(1f);
                Near(host.Health.RecoveryRate, 0.35f,
                    "recovery after mixed traffic clears");

                host.ResetSimulation();
                var hostileLate = Add(host, "hostile-late", "DB", true,
                    new OperationStage(OperationKind.Write, 100f, 10f, "hostile-late"));
                var legitimate = Add(host, "legitimate", "DB", false,
                    new OperationStage(OperationKind.Read, 100f, 10f, "legitimate"));
                host.StepSimulation(16f);
                Check(host.Health.SlowedCount == 2 && host.Health.OverdueCount == 2 &&
                    hostileLate.HealthWasSubstantiallySlowed && hostileLate.HealthBecameLate &&
                    host.Health.Impacts.Count == 1,
                    "hostile and legitimate observable conditions, legitimate impact only");
                float beforeHostileKill = host.Health.Value;
                Check(host.KillConnection("hostile-late") &&
                    host.Health.SlowedCount == 1 && host.Health.OverdueCount == 1,
                    "hostile kill removes both observable conditions");
                Near(host.Health.Value, beforeHostileKill, "hostile kill has no direct penalty");
                Check(host.KillConnection("legitimate") &&
                    host.Health.SlowedCount == 0 && host.Health.OverdueCount == 0,
                    "legitimate kill releases final conditions");

                host.ResetSimulation();
                var barelyShort = Add(host, "barely-short", "DB", false,
                    new OperationStage(OperationKind.Read, 100f, 10f, "barely-short"));
                host.StepSimulation(1f);
                // Give the active connection a 1% allocation shortfall without altering its demand.
                var nearFullTuning = new NetworkHealthTuning();
                var nearFullHealth = new NetworkHealth(nearFullTuning);
                nearFullHealth.Activate(barelyShort, 0f);
                typeof(LogicalConnectionState).GetProperty("AllocatedLoad")
                    .SetValue(barelyShort, 9.9f);
                nearFullHealth.Step(new[] { barelyShort }, 1f, 1f);
                Check(nearFullHealth.Snapshot.SlowedCount == 0 &&
                    nearFullHealth.Snapshot.DegradationRate == 0f,
                    "1% shortfall is neither substantial slowdown nor passive drain");

                host.ResetSimulation();
                var hostile = Add(host, "hostile", "DB", true,
                    new OperationStage(OperationKind.Write, 100f, 10f, "hostile"));
                host.StepSimulation(5f);
                Near(host.Health.Value, 100f, "hostile has no direct service drain");
                Check(host.KillConnection(hostile.ConnectionId), "hostile kill");
                Near(host.Health.Value, 100f, "hostile kill has no penalty");
                Check(host.Health.RecentEvents.Count == 0, "reset clears events");

                host.ResetSimulation();
                var arc = Add(host, "arc", "DB", false,
                    new OperationStage(OperationKind.Read, 10f, 5f, "read"),
                    new OperationStage(OperationKind.Write, 20f, 10f, "write"));
                Near(arc.HealthDeadlineSeconds, 6f, "multi-operation nominal sum");
                host.StepSimulation(2f);
                Check(arc.CurrentOperationIndex == 1 && arc.IsTransitioning,
                    "multi-operation transition");
                host.StepSimulation(0.1f);
                host.StepSimulation(2f);
                Check(arc.Resolution == ConnectionResolution.Completed &&
                    !arc.HealthBecameLate && host.Health.Value == 100f,
                    "multi-operation completed within fixed deadline");
                host.ResetSimulation();
                Check(host.Health.Value == 100f && host.Health.RecentEvents.Count == 0 &&
                    host.Health.Impacts.Count == 0 && !host.Health.Failed,
                    "reset clears health state");
                var tuning = new SerializedObject(host).FindProperty("healthTuning")
                    .FindPropertyRelative("incorrectKillPenalty");
                float originalPenalty = tuning.floatValue;
                tuning.floatValue = 100f;
                tuning.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                try
                {
                    host.ResetSimulation();
                    Add(host, "failure", "DB", false,
                        new OperationStage(OperationKind.Read, 10f, 5f, "failure"));
                    Check(host.KillConnection("failure") && host.Health.Value == 0f &&
                        host.Health.Failed, "zero latches failure");
                    CheckHealthBar(panel, 0f);
                    host.StepSimulation(1f);
                    Check(host.Health.Failed, "recovery does not clear latched failure");
                }
                finally
                {
                    tuning.floatValue = originalPenalty;
                    tuning.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    host.ResetSimulation();
                }
                CheckMultiKillCommands(host);
                Debug.Log("NetworkHealthWitness PASS: ordinary, contention, independent target, overdue escalation, exclusive recovery, observable hostile conditions, kills, reset, and operation arc.");
            }
            finally { director.AutoGenerationEnabled = auto; }
        }

        private static void CheckMultiKillCommands(SimulationHost host)
        {
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(terminal != null && recorder != null, "terminal and recorder for command kills");
            MethodInfo kill = typeof(Terminal).GetMethod("Kill",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Check(kill != null, "terminal kill command handler");
            bool oldRecording = recorder.RecordingEnabled;
            try
            {
                recorder.RecordingEnabled = true;
                host.ResetSimulation();
                recorder.BeginRun();
                var done = Add(host, "done", "DB", false,
                    new OperationStage(OperationKind.Read, 1f, 1f, "done"));
                host.StepSimulation(1f);
                Check(done.IsComplete, "inactive completed ID prepared");
                var m7 = Add(host, "m7", "DB", false,
                    new OperationStage(OperationKind.Read, 100f, 2f, "m7"));
                var f3 = Add(host, "f3", "DB", true,
                    new OperationStage(OperationKind.Write, 100f, 2f, "f3"));
                var a8 = Add(host, "a8", "AUTH", false,
                    new OperationStage(OperationKind.Read, 100f, 2f, "a8"));
                float before = host.Health.Value;
                kill.Invoke(terminal, new object[] {
                    new[] { "kill", "m7", "missing", "f3", "M7", "done" },
                    "kill m7 missing f3 M7 done" });
                Near(host.Health.Value, before - 10f,
                    "first batch penalizes one legitimate kill only");
                kill.Invoke(terminal, new object[] {
                    new[] { "k", "a8", "f3", "unknown", "A8" },
                    "k a8 f3 unknown A8" });
                Near(host.Health.Value, before - 20f,
                    "alias batch penalizes one further legitimate kill only");
                Check(m7.Resolution == ConnectionResolution.Killed &&
                    f3.Resolution == ConnectionResolution.Killed &&
                    a8.Resolution == ConnectionResolution.Killed &&
                    done.Resolution == ConnectionResolution.Completed &&
                    host.RuntimeConnections.Count == 0,
                    "mixed failures and duplicates do not block later kills");
                var actions = recorder.CurrentLog.events.Where(e => e.type == "player_action")
                    .ToArray();
                Check(actions.Select(e => e.connectionId).SequenceEqual(new[] {
                    "m7", "missing", "f3", "done", "a8", "f3", "unknown" }) &&
                    actions.Select(e => e.success).SequenceEqual(new[] {
                    true, false, true, false, true, false, false }) &&
                    actions.Take(4).All(e => e.command == "kill m7 missing f3 M7 done") &&
                    actions.Skip(4).All(e => e.command == "k a8 f3 unknown A8"),
                    "recorder attributes each unique attempt in command order");
                Check(recorder.CurrentLog.events.Where(e =>
                    e.type == "connection_resolved" && e.resolutionReason == "Killed")
                    .Select(e => e.connectionId).SequenceEqual(new[] { "m7", "f3", "a8" }),
                    "successful resolutions are recorded once in command order");
                Debug.Log("NetworkHealthWitness multi-kill: expected -20 Health, 3 kills, " +
                    "7 recorded attempts; observed " + host.Health.Value + " Health, " +
                    actions.Length + " attempts.");
            }
            finally
            {
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                host.ResetSimulation();
            }
        }

        private static LogicalConnectionState Add(SimulationHost host, string id, string target,
            bool hostile, params OperationStage[] operations)
        {
            NodeView ext = host.Nodes.First(x => x.View.NodeId == "EXT1").View;
            NodeView api = host.Nodes.First(x => x.View.NodeId == "API").View;
            NodeView destination = host.Nodes.First(x => x.View.NodeId == target).View;
            LogicalConnectionState state = host.RegisterConnection(LogicalConnectionDefinition.FromPlan(
                new ConnectionSpawnPlan(host.SimulationTimeSeconds, id,
                    new[] { ext, api, destination }, "health-witness", "Service",
                    operations, hostile)));
            // This witness isolates established-service Health math. LifecycleWitness
            // exercises the actual Connecting path and its Health penalties.
            typeof(LogicalConnectionState).GetProperty("Lifecycle").SetValue(state,
                Lifecycle.Active);
            typeof(LogicalConnectionState).GetProperty("HasEstablished").SetValue(state, true);
            typeof(LogicalConnectionState).GetProperty("EstablishedAtSeconds").SetValue(state,
                host.SimulationTimeSeconds);
            var health = (NetworkHealth)typeof(SimulationHost).GetField("health",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(host);
            health.Activate(state, host.SimulationTimeSeconds);
            return state;
        }
        private static void CheckPanelBindings(SimulationHost host)
        {
            var panel = UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>();
            Check(panel != null && PrefabUtility.GetCorrespondingObjectFromSource(panel) != null,
                "authored panel prefab instance");
            var fields = new SerializedObject(panel);
            Check(fields.FindProperty("host").objectReferenceValue == host &&
                fields.FindProperty("commandInput").objectReferenceValue != null &&
                fields.FindProperty("terminalPanel").objectReferenceValue != null &&
                fields.FindProperty("rateText").objectReferenceValue != null &&
                fields.FindProperty("headingText").objectReferenceValue != null,
                "panel simulation, command, and terminal bindings");
            var rect = (RectTransform)panel.transform;
            var terminal = (RectTransform)fields.FindProperty("terminalPanel").objectReferenceValue;
            Check(rect.anchorMax.x == terminal.anchorMax.x &&
                rect.anchorMin.x == terminal.anchorMin.x &&
                Mathf.Abs(rect.rect.width - terminal.rect.width) < 0.01f,
                "matching panel widths");
            Check(panel.ShiftEndMinuteOfDay == 17 * 60, "configurable end marker");
            panel.Render();
            CheckHealthBar(panel, 100f);
            Check(panel.ClockText == "09:00", "24-hour clock at simulation start");
            typeof(NetworkHealthPanel).GetField("expanded", BindingFlags.Instance |
                BindingFlags.NonPublic).SetValue(panel, true);
            panel.ApplyLayout();
            Near(rect.rect.height, ((RectTransform)rect.parent).rect.height * 0.2f,
                "expanded height fraction");
            Check(terminal.rect.height > 0f, "terminal remains visible when expanded");
            typeof(NetworkHealthPanel).GetField("expanded", BindingFlags.Instance |
                BindingFlags.NonPublic).SetValue(panel, false);
            panel.ApplyLayout();
            Near(rect.rect.height, 52f, "compact height");
        }
        private static void CheckHealthBar(NetworkHealthPanel panel, float expectedHealth)
        {
            panel.Render();
            Canvas.ForceUpdateCanvases();
            var fill = (Image)new SerializedObject(panel).FindProperty("healthFill")
                .objectReferenceValue;
            Check(fill != null, "health fill binding");
            var track = (RectTransform)fill.rectTransform.parent;
            Near(fill.rectTransform.rect.width / track.rect.width,
                expectedHealth / 100f, "visible bar fraction at Health " + expectedHealth);
        }

        private static void CheckHistoryResize(NetworkHealthPanel panel)
        {
            Check(panel != null, "health panel exists for resize check");
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            var fields = new SerializedObject(terminal);
            var scroll = (ScrollRect)fields.FindProperty("historyScroll").objectReferenceValue;
            var content = (RectTransform)fields.FindProperty("historyContent").objectReferenceValue;
            var input = (TMP_InputField)fields.FindProperty("commandInput").objectReferenceValue;
            Check(scroll != null && content != null && input != null &&
                content.anchorMin.y == 0f && content.anchorMax.y == 0f && content.pivot.y == 0f,
                "authored history bottom anchor and prompt binding");
            var prompt = (RectTransform)input.transform;
            Canvas.ForceUpdateCanvases();
            float promptBottom = prompt.position.y;
            var dummy = new GameObject("Health history resize witness", typeof(RectTransform),
                typeof(LayoutElement));
            try
            {
                dummy.transform.SetParent(content, false);
                dummy.GetComponent<LayoutElement>().preferredHeight = 1000f;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                scroll.verticalNormalizedPosition = 0.5f;
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4];
                content.GetWorldCorners(corners);
                float before = scroll.viewport.InverseTransformPoint(corners[1]).y -
                    scroll.viewport.rect.yMax;
                typeof(NetworkHealthPanel).GetField("expanded", BindingFlags.Instance |
                    BindingFlags.NonPublic).SetValue(panel, true);
                panel.ApplyLayout();
                Canvas.ForceUpdateCanvases();
                content.GetWorldCorners(corners);
                float after = scroll.viewport.InverseTransformPoint(corners[1]).y -
                    scroll.viewport.rect.yMax;
                Near(after, before, "browsed history top offset preserved on expand");
                Near(prompt.position.y, promptBottom, "prompt stays at bottom on expand");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dummy);
                typeof(NetworkHealthPanel).GetField("expanded", BindingFlags.Instance |
                    BindingFlags.NonPublic).SetValue(panel, false);
                panel.ApplyLayout();
            }
        }
        private static void Near(float actual, float expected, string label) =>
            Check(Mathf.Abs(actual - expected) < 0.001f,
                $"{label}: expected {expected:0.###}, observed {actual:0.###}");
        private static void Check(bool value, string label)
        { if (!value) throw new InvalidOperationException("NetworkHealthWitness: " + label); }
    }
}
