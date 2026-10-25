using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            CheckPanelBindings(host);
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
                Near(host.Health.DegradationRate, 0.05f, "three shortfall impacts");
                Near(host.Health.Value, 99.95f, "one second drain");
                Check(host.Health.SlowedCount == 3 && host.Health.OverdueCount == 0 &&
                    host.Health.Impacts.Count == 3 && a.HealthWasSubstantiallySlowed,
                    "slowed facts and aggregate");
                for (int i = 0; i < 14; i++) host.StepSimulation(1f);
                Check(host.Health.OverdueCount == 0, "deadline is not late at exact boundary");
                host.StepSimulation(1f);
                float firstOverdue = host.Health.DegradationRate;
                Check(host.Health.OverdueCount == 3 && a.HealthBecameLate && firstOverdue > 0.05f,
                    "overdue replaces slowdown drain");
                host.StepSimulation(1f);
                Check(host.Health.DegradationRate > firstOverdue, "overdue escalates");
                float beforeKill = host.Health.Value;
                Check(host.KillConnection("a"), "legitimate kill");
                Near(host.Health.Value, beforeKill - 5f, "one immediate kill penalty");
                Check(a.Resolution == ConnectionResolution.Killed &&
                    a.ResolvedAtSeconds == host.SimulationTimeSeconds &&
                    host.Health.RecentEvents.Count == 1 &&
                    host.Health.Impacts.All(x => x.ConnectionId != "a"),
                    "resolution facts and released impact");
                Check(host.KillConnection("b") && host.KillConnection("c"),
                    "remaining legitimate kills");
                float beforeRecovery = host.Health.Value;
                host.StepSimulation(5f);
                Near(host.Health.Value, beforeRecovery + 0.1f,
                    "recovery independent of drain");
                Debug.Log($"NetworkHealthWitness contention: expected 3.333 LOAD, 0.05/s drain, escalating overdue, -5 kill, +0.1 recovery; observed {observedSharedGrant:0.###} initial LOAD, {firstOverdue:0.###}/s first late, {host.Health.Value:0.###} recovered");

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
                    host.StepSimulation(1f);
                    Check(host.Health.Failed, "recovery does not clear latched failure");
                }
                finally
                {
                    tuning.floatValue = originalPenalty;
                    tuning.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    host.ResetSimulation();
                }
                Debug.Log("NetworkHealthWitness PASS: ordinary, contention, independent target, overdue escalation, kills, recovery, hostile isolation, reset, and operation arc.");
            }
            finally { director.AutoGenerationEnabled = auto; }
        }

        private static LogicalConnectionState Add(SimulationHost host, string id, string target,
            bool hostile, params OperationStage[] operations)
        {
            NodeView ext = host.Nodes.First(x => x.View.NodeId == "EXT1").View;
            NodeView api = host.Nodes.First(x => x.View.NodeId == "API").View;
            NodeView destination = host.Nodes.First(x => x.View.NodeId == target).View;
            return host.RegisterConnection(LogicalConnectionDefinition.FromPlan(
                new ConnectionSpawnPlan(host.SimulationTimeSeconds, id,
                    new[] { ext, api, destination }, "health-witness", "Service",
                    operations, hostile)));
        }
        private static void CheckPanelBindings(SimulationHost host)
        {
            var panel = UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>();
            Check(panel != null && PrefabUtility.GetCorrespondingObjectFromSource(panel) != null,
                "authored panel prefab instance");
            var fields = new SerializedObject(panel);
            Check(fields.FindProperty("host").objectReferenceValue == host &&
                fields.FindProperty("commandInput").objectReferenceValue != null &&
                fields.FindProperty("terminalPanel").objectReferenceValue != null,
                "panel simulation, command, and terminal bindings");
            var rect = (RectTransform)panel.transform;
            var terminal = (RectTransform)fields.FindProperty("terminalPanel").objectReferenceValue;
            Check(rect.anchorMax.x == terminal.anchorMax.x &&
                rect.anchorMin.x == terminal.anchorMin.x &&
                Mathf.Abs(rect.rect.width - terminal.rect.width) < 0.01f,
                "matching panel widths");
            Check(panel.ShiftEndMinuteOfDay == 17 * 60, "configurable end marker");
            panel.Render();
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
        private static void Near(float actual, float expected, string label) =>
            Check(Mathf.Abs(actual - expected) < 0.001f,
                $"{label}: expected {expected:0.###}, observed {actual:0.###}");
        private static void Check(bool value, string label)
        { if (!value) throw new InvalidOperationException("NetworkHealthWitness: " + label); }
    }
}
