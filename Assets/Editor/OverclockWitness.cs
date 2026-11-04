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
    public static class OverclockWitness
    {
        [MenuItem("Tools/Connections/Run Overclock Witness")]
        public static void Run()
        {
            CheckScene("Connections");
            CheckScene("ConnectionsAdvanced");
            Debug.Log("OverclockWitness PASS: both scenes, 10-to-15 LOAD allocation, " +
                "unchanged requests and WORK, expiry, cooldown, reset, UI and recorder.");
        }

        private static void CheckScene(string scene)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var panel = UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            Check(host != null && panel != null && presenter != null && traffic != null,
                scene + " services");
            var strip = (RectTransform)panel.transform;
            var row = strip.Find("ToolReadinessRow") as RectTransform;
            var fields = new SerializedObject(panel);
            var fill = (Image)fields.FindProperty("overclockStatusFill").objectReferenceValue;
            var text = (TMP_Text)fields.FindProperty("overclockStatusText").objectReferenceValue;
            Check(row.Find("OverclockGroup/Slots") != null && fill != null && text != null &&
                !fill.raycastTarget && !text.raycastTarget, scene + " authored status binding");
            Canvas.ForceUpdateCanvases();
            Check(row.rect.width < strip.rect.width - 680f &&
                row.rect.width > 500f, scene + " compact three-tool row");

            bool auto = traffic.AutoGenerationEnabled;
            bool hostileGeneration = traffic.HostileGenerationEnabled;
            var db = host.FindNodeResources("DB");
            var api = host.FindNodeResources("API");
            float originalCapacity = Float(db, "loadCapacity");
            float originalTravel = Float(host, "connectionTravelSecondsPerLink");
            bool originalNoise = (bool)Field(host, "naturalLoadNoiseEnabled").GetValue(host);
            try
            {
                traffic.AutoGenerationEnabled = false;
                traffic.HostileGenerationEnabled = false;
                Set(host, "naturalLoadNoiseEnabled", false);
                Set(host, "connectionTravelSecondsPerLink", .000001f);
                Set(db, "loadCapacity", 10f);
                host.SetRunMode(ConnectionRunMode.Generated);
                panel.Render();
                Check(text.text == "READY", scene + " initial READY");
                Check(!host.TryActivateOverclock("missing", "overclock missing", out _) &&
                    host.OverclockCooldownRemaining == 0f, scene + " invalid node leaves timer untouched");

                if (scene == "ConnectionsAdvanced")
                {
                    Check(host.TryActivateOverclock("DB", "overclock DB", out _),
                        scene + " generated-mode activation");
                    Near(db.LoadCapacity, 15f, scene + " effective capacity");
                    panel.Render();
                    presenter.RenderNow(0f, 1f);
                    Check(text.text == "ACTIVE 15s" && presenter.StateFor(db.View).Overclocked,
                        scene + " readiness and node presentation");
                    host.StepSimulation(15f);
                    panel.Render();
                    presenter.RenderNow(15f, 1f);
                    Check(text.text == "45s" && !presenter.StateFor(db.View).Overclocked,
                        scene + " expiry and cooldown");
                    host.ResetSimulation();
                    panel.Render();
                    Check(text.text == "READY", scene + " reset readiness");
                    return;
                }

                host.SetRunMode(ConnectionRunMode.AuthoredWitness);
                // The original witness is benign; turn c3 hostile and extend WORK only
                // within this Editor witness so neither connection finishes in 15 seconds.
                var c1Def = host.Definitions.First(c => c.ConnectionId == "c1");
                var c3Def = host.Definitions.First(c => c.ConnectionId == "c3");
                float c1Work = Float(c1Def, "totalWork");
                float c3Work = Float(c3Def, "totalWork");
                float c3Load = Float(c3Def, "desiredLoad");
                float c3Delay = Float(c3Def, "startDelaySeconds");
                var c3Ops = (OperationStage[])Field(c3Def, "operations").GetValue(c3Def);
                bool c3Hostile = (bool)Field(c3Def, "isHostile").GetValue(c3Def);
                try
                {
                    Set(c1Def, "totalWork", 1000f);
                    Set(c3Def, "totalWork", 1000f);
                    Set(c3Def, "desiredLoad", 11f);
                    Set(c3Def, "startDelaySeconds", 0f);
                    Set(c3Def, "operations", Array.Empty<OperationStage>());
                    Set(c3Def, "isHostile", true);
                    host.ResetSimulation();
                    host.StepSimulation(.1f);
                    var c1 = host.FindConnection("c1");
                    var c3 = host.FindConnection("c3");
                    Check(c1.IsActive && c3.IsActive && !c1.IsHostile && c3.IsHostile,
                        scene + " legitimate and hostile active");
                    Near(db.RequestedLoad, 16f, "preboost request");
                    Near(db.AllocatedLoad, 10f, "preboost allocation");
                    Near(c1.AllocatedLoad, 5f * 10f / 16f, "preboost legitimate share");
                    Near(c3.AllocatedLoad, 11f * 10f / 16f, "preboost hostile share");
                    Check(host.TryActivateOverclock("db", "overclock db", out _),
                        scene + " activation");
                    Near(db.BaseLoadCapacity, 10f, "authored base retained");
                    Near(db.LoadCapacity, 15f, "boosted capacity");
                    Near(db.RequestedLoad, 16f, "request unchanged");
                    Near(db.AllocatedLoad, 15f, "immediate reallocation");
                    Near(c1.AllocatedLoad, 5f * 15f / 16f, "boosted legitimate share");
                    Near(c3.AllocatedLoad, 11f * 15f / 16f, "boosted hostile share");
                    Near(c1.DesiredLoad, 5f, "legitimate request unchanged");
                    Near(c3.DesiredLoad, 11f, "hostile request unchanged");
                    Near(c1.CurrentTotalWork, 1000f, "required WORK unchanged");
                    Check(c1.ActualWorkRate <= c1.DesiredLoad + .001f &&
                        c3.ActualWorkRate <= c3.DesiredLoad + .001f,
                        scene + " normal WORK-rate ceiling");
                    Check(db.View.LoadBar.LabelText == "LOAD - 16 / 15",
                        scene + " node LOAD shows effective capacity");
                    panel.Render();
                    Check(text.text == "ACTIVE 15s", scene + " ACTIVE readiness");
                    presenter.RenderNow(0f, 1f);
                    Check(presenter.StateFor(db.View).Overclocked &&
                        !presenter.StateFor(api.View).Overclocked &&
                        presenter.ViewFor(db.View).GetComponentsInChildren<TMP_Text>(true)
                            .Any(t => t.gameObject.activeInHierarchy && t.text == "OVERCLOCKED"),
                        scene + " affected-node status only");
                    host.Recorder?.BeginRun();
                    Near(host.Recorder.CurrentLog.run.nodes.First(n => n.id == "DB")
                        .loadCapacity, 10f, "recorder metadata keeps authored capacity");
                    int index = host.Recorder.CaptureSnapshot(host.SimulationTimeSeconds);
                    Near(host.Recorder.CurrentLog.snapshots[index].nodes
                        .First(n => n.id == "DB").loadCapacity, 15f,
                        "recorder snapshot effective capacity");
                    float frozen = host.SimulationTimeSeconds;
                    panel.Render();
                    presenter.RenderNow(1f, 1f);
                    Near(host.SimulationTimeSeconds, frozen, "render does not advance simulation");
                    Check(!host.TryActivateOverclock("API", "overclock API", out _) &&
                        !host.TryActivateOverclock("missing", "overclock missing", out _) &&
                        host.OverclockedNode == db, scene + " busy and invalid attempts preserve target");
                    host.StepSimulation(3f);
                    presenter.RenderNow(3f, 1f);
                    Check(presenter.ViewFor(db.View).GetComponentsInChildren<TMP_Text>(true)
                        .Any(t => t.gameObject.activeInHierarchy && t.text == "12s"),
                        scene + " node countdown uses simulation time");
                    host.StepSimulation(12f);
                    Near(db.LoadCapacity, 10f, "expiry restores capacity");
                    Check(host.OverclockedNode == null, scene + " active state expires");
                    presenter.RenderNow(16f, 1f);
                    Check(!presenter.StateFor(db.View).Overclocked,
                        scene + " node status expires");
                    panel.Render();
                    Check(text.text == "45s", scene + " activation-origin cooldown");
                    Check(!host.TryActivateOverclock("API", "overclock API", out _) &&
                        host.OverclockCooldownRemaining > 44.9f,
                        scene + " cooldown attempt does not restart deadline");
                    host.StepSimulation(45f);
                    panel.Render();
                    Check(text.text == "READY" && host.TryActivateOverclock("API",
                        "overclock API", out _), scene + " ready at 60 seconds");
                    Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _),
                        scene + " Blackout test condition");
                    presenter.RenderNow(60f, 1f);
                    var apiLabels = presenter.ViewFor(api.View).GetComponentsInChildren<TMP_Text>(true);
                    Check(presenter.StateFor(api.View).Offline &&
                        presenter.StateFor(api.View).Overclocked &&
                        apiLabels.Any(t => t.gameObject.activeInHierarchy && t.text == "OFFLINE") &&
                        apiLabels.Any(t => t.gameObject.activeInHierarchy && t.text == "OVERCLOCKED"),
                        scene + " Blackout warning coexists with overclock");
                    Check(host.StartScan("API", "scan API", out _) == ScanStartResult.Started,
                        scene + " infection diagnosis starts during overclock");
                    host.StepSimulation(host.ScanDurationSeconds);
                    host.ApplyCondition(api, new ConditionPayload(ConditionKind.PortScanned, 8f));
                    host.ApplyCondition(api, new ConditionPayload(ConditionKind.ScanJammed, 8f));
                    presenter.RenderNow(68f, 1f);
                    Check(presenter.StateFor(api.View).KnownInfected &&
                        presenter.StateFor(api.View).PortScanned &&
                        presenter.StateFor(api.View).ScanJammed &&
                        presenter.StateFor(api.View).Overclocked,
                        scene + " infection, awareness and condition coexist");
                    host.ResetSimulation();
                    panel.Render();
                    presenter.RenderNow(61f, 1f);
                    Check(text.text == "READY" && host.OverclockedNode == null &&
                        !presenter.StateFor(api.View).Overclocked &&
                        Mathf.Approximately(api.LoadCapacity, api.BaseLoadCapacity),
                        scene + " reset clears ability and presentation");
                }
                finally
                {
                    Set(c1Def, "totalWork", c1Work);
                    Set(c3Def, "totalWork", c3Work);
                    Set(c3Def, "desiredLoad", c3Load);
                    Set(c3Def, "startDelaySeconds", c3Delay);
                    Set(c3Def, "operations", c3Ops);
                    Set(c3Def, "isHostile", c3Hostile);
                }
            }
            finally
            {
                Set(db, "loadCapacity", originalCapacity);
                Set(host, "connectionTravelSecondsPerLink", originalTravel);
                Set(host, "naturalLoadNoiseEnabled", originalNoise);
                traffic.AutoGenerationEnabled = auto;
                traffic.HostileGenerationEnabled = hostileGeneration;
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static FieldInfo Field(object owner, string name) => owner.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        private static float Float(object owner, string name) => (float)Field(owner, name).GetValue(owner);
        private static void Set(object owner, string name, object value) => Field(owner, name).SetValue(owner, value);
        private static void Near(float actual, float expected, string label) =>
            Check(Mathf.Abs(actual - expected) < .01f,
                label + $": expected {expected:0.###}, observed {actual:0.###}");
        private static void Check(bool okay, string label)
        {
            if (!okay) throw new Exception("OverclockWitness FAILED: " + label);
        }
    }
}
