using System;
using System.Linq;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class NodeStatusWitness
    {
        [MenuItem("Tools/Connections/Run Node Status Witness")]
        public static void Run()
        {
            CheckScene("Assets/Scenes/Connections.unity", 4);
            CheckScene("Assets/Scenes/ConnectionsAdvanced.unity", 8);
            CheckPlacement();
            CheckModel();
            CheckHostLifecycle();
            Debug.Log("NodeStatusWitness PASS: both scenes, authored layers, placement, " +
                "diagnostic knowledge, grouping, real-time expiry, clean and reset.");
        }

        private static void CheckScene(string path, int nodeCount)
        {
            EditorSceneManager.OpenScene(path);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            Check(host != null && presenter != null && host.Nodes.Length == nodeCount,
                path + " services and node count");
            Check(presenter.Host == host && presenter.NetworkArea != null &&
                presenter.StatusLayer != null && presenter.StatusPrefab != null &&
                presenter.StatusLayer.parent == presenter.NetworkArea &&
                presenter.StatusLayer.GetSiblingIndex() ==
                    presenter.StatusLayer.parent.childCount - 1,
                path + " authored presenter/layer bindings");
            var prefab = presenter.StatusPrefab;
            var bands = prefab.GetComponentsInChildren<NodeStatusBandGraphic>(true);
            var panels = prefab.GetComponentsInChildren<NodeStatusPanel>(true);
            Check(bands.Length == 3 && panels.Length == 3 &&
                bands.All(b => !b.raycastTarget), path + " three family layers");
            foreach (NodeStatusPanel panel in panels)
            {
                var serialized = new SerializedObject(panel);
                Check(serialized.FindProperty("plate").objectReferenceValue != null &&
                    serialized.FindProperty("firstRow").objectReferenceValue != null &&
                    serialized.FindProperty("secondRow").objectReferenceValue != null &&
                    serialized.FindProperty("firstRadialTimer").objectReferenceValue != null &&
                    serialized.FindProperty("secondRadialTimer").objectReferenceValue != null,
                    path + " authored panel bindings and optional per-row timers");
                var plate = (PillGraphic)serialized.FindProperty("plate").objectReferenceValue;
                Check(plate.material != null && plate.material.shader.name ==
                    "Uptime/Connections/AntialiasedPill" && !plate.raycastTarget,
                    path + " shared antialiased panel material");
            }
            var preview = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var panel = preview.GetComponentsInChildren<NodeStatusPanel>(true)[0];
                panel.SetContent("OFFLINE", "INFECTED", Color.red, .75f, .25f);
                var serialized = new SerializedObject(panel);
                var first = (Image)serialized.FindProperty("firstRadialTimer").objectReferenceValue;
                var second = (Image)serialized.FindProperty("secondRadialTimer").objectReferenceValue;
                Check(first.gameObject.activeSelf && second.gameObject.activeSelf &&
                    Mathf.Approximately(first.fillAmount, .75f) &&
                    Mathf.Approximately(second.fillAmount, .25f),
                    path + " independent optional row timers");
                panel.SetContent("CLEAN", null, Color.green);
                Check(!first.gameObject.activeSelf && !second.gameObject.activeSelf,
                    path + " untimed statuses hide both radial timers");
            }
            finally { UnityEngine.Object.DestroyImmediate(preview.gameObject); }
        }

        private static void CheckPlacement()
        {
            Rect node = new Rect(-120, -40, 240, 80);
            Rect viewport = new Rect(-500, -300, 1000, 600);
            var topPill = new[] { new Rect(-110, 50, 220, 90) };
            NodeStatusAnchor choice = NodeStatusPlacement.Choose(node,
                new Vector2(184, 63), viewport, Array.Empty<Rect>(), topPill,
                Array.Empty<Rect>());
            Check(choice != NodeStatusAnchor.N &&
                !NodeStatusPlacement.At(choice, node, new Vector2(184, 63))
                    .Overlaps(topPill[0]), "identity pill is protected at preferred N anchor");
            var blocked = new[] { new Rect(130, -100, 220, 180) };
            choice = NodeStatusPlacement.Choose(node, new Vector2(184, 63), viewport,
                blocked, Array.Empty<Rect>(), Array.Empty<Rect>());
            Check(choice != NodeStatusAnchor.E, "neighbor node card is avoided");
            Rect topNode = new Rect(-120, 230, 240, 60);
            choice = NodeStatusPlacement.Choose(topNode, new Vector2(184, 63), viewport,
                Array.Empty<Rect>(), Array.Empty<Rect>(), Array.Empty<Rect>());
            Check(NodeStatusPlacement.At(choice, topNode, new Vector2(184, 63)).yMax <=
                viewport.yMax, "viewport edge is respected");
            for (int i = 0; i < 8; i++)
            {
                Rect position = NodeStatusPlacement.At((NodeStatusAnchor)i, node,
                    new Vector2(184, 63));
                Check(!position.Overlaps(node), "all eight anchors outside card " + i);
            }
        }

        private static void CheckModel()
        {
            var state = new NodeStatusState();
            Check(!state.HasAny(10f), "normal node has no status");
            state.SetOffline(true);
            state.ScanResult(NodeScanResult.Infected, 10f, 2.5f);
            Check(state.Offline && state.KnownInfected && !state.CleanVisible(10f),
                "OFFLINE and known INFECTED coexist in Pain");
            state.CleanSucceeded();
            Check(state.Offline && !state.KnownInfected && !state.CleanVisible(10f),
                "success clears knowledge but does not imply CLEAN");
            state.SetOffline(false);
            state.ScanResult(NodeScanResult.Clean, 20f, 2.5f);
            Check(state.CleanVisible(22.49f) && !state.CleanVisible(22.5f),
                "CLEAN uses short real-time lifetime");
            state.SetAwarenessPreview(true);
            Check(state.AwarenessPreview && state.HasAny(30f),
                "awareness family can preview independently");
            state.Reset();
            Check(!state.HasAny(30f), "reset clears knowledge and timers");
        }

        private static void CheckHostLifecycle()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            director.AutoGenerationEnabled = false;
            director.HostileGenerationEnabled = false;
            host.SetRunMode(ConnectionRunMode.Generated);
            NodeView db = host.Nodes.First(n => n.View.NodeId == "DB").View;
            float now = Time.unscaledTime;
            presenter.RenderNow(now, 1f);
            Check(presenter.ViewFor(db) != null && !presenter.ViewFor(db).HasVisibleFamily,
                "normal node is unadorned");
            Check(host.TryInjectInfection("DB", InfectionKind.Blackout, 0f, out _),
                "blackout injects for witness");
            presenter.RenderNow(now, .16f);
            var enteringPanel = presenter.ViewFor(db).GetComponentsInChildren<NodeStatusPanel>(true)
                .First(p => p.name == "PainPanel");
            Check(enteringPanel.transform.localScale.x > 1f,
                "activation gives the status panel a brief visual pop");
            presenter.RenderNow(now, 1f);
            Check(presenter.StateFor(db).Offline && presenter.ViewFor(db).PainVisible &&
                db.transform.Find("NodeHeading/NodeRole").GetComponent<TMP_Text>().text ==
                    "· DATABASE", "offline status replaces only the old subtitle symptom");
            Check(host.StartScan("DB", "scan DB", out _) == ScanStartResult.Started,
                "scan starts");
            host.StepSimulation(host.ScanDurationSeconds);
            presenter.RenderNow(now, 1f);
            var text = presenter.ViewFor(db).GetComponentsInChildren<TMP_Text>(true);
            Check(presenter.StateFor(db).KnownInfected &&
                text.Any(t => t.gameObject.activeInHierarchy && t.text == "OFFLINE") &&
                text.Any(t => t.gameObject.activeInHierarchy && t.text == "INFECTED"),
                "scan-confirmed infected shares one Pain panel with offline");

            Check(host.StartClean("DB", "clean DB", out CleanSession clean) ==
                CleanStartResult.Started, "clean session starts");
            Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 8f, out _),
                "new hidden infection arrives after clean begins");
            int malicious = clean.Processes.First(p => p.CpuPercent >= 82).Pid;
            Check(host.SelectCleanPid(malicious, out int removed) ==
                CleanSelectionResult.Success && removed == 1,
                "clean removes captured infection only");
            presenter.RenderNow(now, 1f);
            Check(!presenter.StateFor(db).KnownInfected &&
                !presenter.StateFor(db).CleanVisible(now) &&
                !presenter.StateFor(db).Offline &&
                host.Infections.Any(i => i.Target.View == db),
                "success clears visible knowledge without exposing surviving infection");

            Check(host.TryRemoveInfection("DB", InfectionKind.PhantomLoad, out _),
                "remove surviving infection for clean scan");
            Check(host.StartScan("DB", "scan DB", out _) == ScanStartResult.Started,
                "second scan starts");
            host.StepSimulation(host.ScanDurationSeconds);
            presenter.RenderNow(Time.unscaledTime, 1f);
            Check(presenter.ViewFor(db).BenefitVisible &&
                !presenter.StateFor(db).KnownInfected,
                "clean scan reveals short beneficial acknowledgement");
            presenter.SetAwarenessPreview("DB", true);
            presenter.RenderNow(Time.unscaledTime, 1f);
            Check(presenter.ViewFor(db).AwarenessVisible &&
                presenter.ViewFor(db).BenefitVisible,
                "two families coexist in Editor preview");
            host.ResetSimulation();
            presenter.RenderNow(Time.unscaledTime, 1f);
            Check(!presenter.StateFor(db).HasAny(Time.unscaledTime) &&
                !presenter.ViewFor(db).HasVisibleFamily,
                "reset clears all visual state and timers");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("NodeStatusWitness: " + message);
        }
    }
}
