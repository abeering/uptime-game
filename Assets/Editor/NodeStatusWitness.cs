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
            CheckProductionStamps("Assets/Scenes/Connections.unity", 4);
            CheckProductionStamps("Assets/Scenes/ConnectionsAdvanced.unity", 8);
            Debug.Log("NodeStatusWitness PASS: both scenes, authored layers, placement, " +
                "diagnostic knowledge, grouping, stamps, real-time expiry, clean and reset.");
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
                presenter.StampLayer != null && presenter.StampPrefab != null &&
                presenter.StatusLayer.parent == presenter.NetworkArea &&
                presenter.StampLayer.parent == presenter.NetworkArea &&
                presenter.StampLayer.GetSiblingIndex() ==
                    presenter.StatusLayer.parent.childCount - 1,
                path + " authored presenter/layer bindings and stamp draw order");
            Check(presenter.StatusLayer.GetSiblingIndex() <
                presenter.StampLayer.GetSiblingIndex() &&
                presenter.StampPrefab == AssetDatabase.LoadAssetAtPath<NodeEventStamp>(
                    "Assets/Prefabs/Connections/NodeEventStamp.prefab") &&
                presenter.StampPrefab.GetComponentsInChildren<Image>(true)
                    .All(i => !i.raycastTarget), path + " shared stamp prefab is click-through");
            var prefab = presenter.StatusPrefab;
            var bands = prefab.GetComponentsInChildren<NodeStatusBandGraphic>(true);
            var panels = prefab.GetComponentsInChildren<NodeStatusPanel>(true);
            Check(bands.Length == 3 && panels.Length == 3 &&
                bands.All(b => !b.raycastTarget), path + " three family layers");
            foreach (NodeStatusPanel panel in panels)
            {
                var serialized = new SerializedObject(panel);
                Check(serialized.FindProperty("plate").objectReferenceValue != null &&
                    serialized.FindProperty("labels").arraySize == 3 &&
                    serialized.FindProperty("timers").arraySize == 3 &&
                    Enumerable.Range(0, 3).All(i =>
                        serialized.FindProperty("labels").GetArrayElementAtIndex(i).objectReferenceValue != null &&
                        serialized.FindProperty("timers").GetArrayElementAtIndex(i).objectReferenceValue != null),
                    path + " authored three-row panel and seconds timers");
                var plate = (PillGraphic)serialized.FindProperty("plate").objectReferenceValue;
                Check(plate.material != null && plate.material.shader.name ==
                    "Uptime/Connections/AntialiasedPill" && !plate.raycastTarget,
                    path + " shared antialiased panel material");
            }
            var preview = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var panel = preview.GetComponentsInChildren<NodeStatusPanel>(true)[0];
                panel.SetRows("OFFLINE", "INFECTED", "SCAN JAMMED", Color.red,
                    null, null, 7.01f);
                var serialized = new SerializedObject(panel);
                var timers = serialized.FindProperty("timers");
                var first = (TMP_Text)timers.GetArrayElementAtIndex(0).objectReferenceValue;
                var third = (TMP_Text)timers.GetArrayElementAtIndex(2).objectReferenceValue;
                Check(!first.gameObject.activeSelf && third.gameObject.activeSelf &&
                    third.text == "8s" && panel.Height == 78f,
                    path + " third-row ceiling seconds timer");
                panel.SetContent("CLEAN", null, Color.green);
                Check(!first.gameObject.activeSelf && !third.gameObject.activeSelf,
                    path + " untimed statuses hide seconds timers");
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
            // DB-like case: a live connection enters from above, while adjacent
            // cards occupy both sides and the area below remains clear.
            var adjacent = new[] { new Rect(-480, -40, 240, 80),
                new Rect(240, -40, 240, 80) };
            Check(NodeStatusPlacement.Choose(node, new Vector2(184, 63), viewport,
                adjacent, Array.Empty<Rect>(), Array.Empty<Rect>()) == NodeStatusAnchor.N,
                "unoccupied DB-like node retains preferred north placement");
            var socket = new[] { NodeStatusPlacement.Expanded(
                new Rect(-13, 40, 26, 26), 12f) };
            var beam = new[] { new NodeStatusPlacement.BeamSegment(
                new Vector2(0, 240), new Vector2(0, 40)) };
            Check(NodeStatusPlacement.Choose(node, new Vector2(184, 63), viewport,
                adjacent, Array.Empty<Rect>(), Array.Empty<Rect>(), null, beam) !=
                NodeStatusAnchor.N, "live beam alone discourages a covering callout");
            Check(NodeStatusPlacement.Choose(node, new Vector2(184, 63), viewport,
                adjacent, socket, Array.Empty<Rect>(),
                new[] { Vector2.up }, beam) == NodeStatusAnchor.S,
                "DB-like top socket and live approach place callout below");
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

        private static void CheckProductionStamps(string path, int nodeCount)
        {
            EditorSceneManager.OpenScene(path);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            traffic.AutoGenerationEnabled = false;
            traffic.HostileGenerationEnabled = false;
            host.SetRunMode(ConnectionRunMode.Generated);
            float now = Time.unscaledTime;
            presenter.RenderNow(now, 1f);
            NodeResources db = host.FindNodeResources("DB");
            NodeResources api = host.FindNodeResources("API");
            NodeEventStamp dbStamp = presenter.StampFor(db.View);
            NodeEventStamp apiStamp = presenter.StampFor(api.View);
            Check(presenter.StampLayer.childCount == nodeCount && dbStamp != null &&
                apiStamp != null && dbStamp != apiStamp && !dbStamp.IsPlaying &&
                presenter.ViewFor(db.View) != null &&
                presenter.ViewFor(db.View).GetComponentsInChildren<NodeStatusBandGraphic>(true)
                    .All(b => !b.gameObject.activeSelf),
                path + " independent dormant stamps and no exterior bands");
            Rect card = EdgeGeometry.CardRect(db.View, presenter.NetworkArea);
            Check((dbStamp.Rect.anchoredPosition - card.center).sqrMagnitude < .01f &&
                dbStamp.Rect.sizeDelta.y > card.height &&
                dbStamp.GetComponentsInChildren<Image>(true).All(i => !i.raycastTarget),
                path + " centered large click-through stamp");
            CheckStampArtwork(dbStamp, path);

            Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 3f, out _) &&
                !dbStamp.IsPlaying, path + " covert infection has no discovery stamp");
            Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _) &&
                apiStamp.IsPlaying && apiStamp.CurrentKind == NodeEventStamp.EventKind.Offline &&
                !dbStamp.IsPlaying, path + " Blackout announces only its target");
            host.ResetSimulation();
            Check(!apiStamp.IsPlaying && !dbStamp.IsPlaying,
                path + " reset cancels active stamps");

            host.ApplyCondition(db, new ConditionPayload(ConditionKind.ScanJammed, 8f));
            Check(dbStamp.IsPlaying && dbStamp.CurrentKind ==
                NodeEventStamp.EventKind.ScanJammed, path + " SCAN JAMMED applied");
            dbStamp.RenderNow(Time.unscaledTime + 1.1f);
            presenter.RenderNow(now, 1f);
            Check(!dbStamp.IsPlaying && presenter.StateFor(db.View).ScanJammed,
                path + " persistent Condition does not replay each frame");
            host.ApplyCondition(db, new ConditionPayload(ConditionKind.ScanJammed, 8f));
            Check(dbStamp.IsPlaying && dbStamp.CurrentKind ==
                NodeEventStamp.EventKind.ScanJammed, path + " Condition refresh replays once");
            host.ApplyCondition(api, new ConditionPayload(ConditionKind.PortScanned, 20f));
            Check(apiStamp.IsPlaying && apiStamp.CurrentKind ==
                NodeEventStamp.EventKind.PortScanned && dbStamp.IsPlaying,
                path + " separate nodes announce concurrently");
            host.ResetSimulation();

            Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 3f, out _) &&
                host.StartScan("DB", "scan DB", out _) == ScanStartResult.Started,
                path + " infected diagnostic starts");
            host.StepSimulation(host.ScanDurationSeconds);
            Check(dbStamp.IsPlaying && dbStamp.CurrentKind == NodeEventStamp.EventKind.Infected &&
                presenter.StateFor(db.View).KnownInfected,
                path + " completed infected diagnosis announces knowledge");
            Check(host.StartClean("DB", "clean DB", out CleanSession clean) ==
                CleanStartResult.Started, path + " cleaning starts");
            int malicious = clean.Processes.Single(p => p.CpuPercent >= 82).Pid;
            Check(host.SelectCleanPid(malicious, out int removed) ==
                CleanSelectionResult.Success && removed == 1 && dbStamp.IsPlaying &&
                dbStamp.CurrentKind == NodeEventStamp.EventKind.Clean &&
                !presenter.StateFor(db.View).CleanVisible(Time.unscaledTime),
                path + " infection removal acknowledges without persistent CLEAN");
            host.ResetSimulation();
            Check(host.StartScan("DB", "scan DB", out _) == ScanStartResult.Started,
                path + " clean diagnostic starts");
            host.StepSimulation(host.ScanDurationSeconds);
            Check(dbStamp.IsPlaying && dbStamp.CurrentKind == NodeEventStamp.EventKind.Clean &&
                presenter.StateFor(db.View).CleanVisible(Time.unscaledTime),
                path + " completed clean diagnosis shows stamp and acknowledgment");
            host.ResetSimulation();

            Check(host.Worms.TryIsolate("DB", "isolate DB", out _) &&
                dbStamp.IsPlaying && dbStamp.CurrentKind ==
                    NodeEventStamp.EventKind.Isolated,
                path + " isolation announces without redundant OFFLINE");
            presenter.RenderNow(now, 1f);
            Check(presenter.StateFor(db.View).Offline && presenter.StateFor(db.View).Isolated &&
                dbStamp.CurrentKind == NodeEventStamp.EventKind.Isolated,
                path + " isolation symptom retains one stamp");
            host.ResetSimulation();

            Check(host.Payloads.TryIntercept(null, "DB", out _) && dbStamp.IsPlaying &&
                dbStamp.CurrentKind == NodeEventStamp.EventKind.Intercept,
                path + " broad interceptor activation announces once");
            PhysicalLinkReference adjacent = host.PhysicalLinks.First(link =>
                link.View.NodeA == db.View || link.View.NodeB == db.View);
            NodeView origin = adjacent.View.NodeA == db.View ?
                adjacent.View.NodeB : adjacent.View.NodeA;
            PayloadState first = host.Payloads.Spawn("DB", PayloadMotionProfile.Steady,
                origin.NodeId, new[] { .8f });
            PayloadState later = host.Payloads.Spawn("DB", PayloadMotionProfile.Steady,
                origin.NodeId, new[] { 2.8f });
            host.StepSimulation(2f);
            Check(first.Intercepted && dbStamp.IsPlaying &&
                dbStamp.CurrentKind == NodeEventStamp.EventKind.Caught,
                path + " first real broad catch announces CAUGHT");
            dbStamp.Cancel();
            host.StepSimulation(2f);
            Check(later.Intercepted && dbStamp.IsPlaying &&
                dbStamp.CurrentKind == NodeEventStamp.EventKind.Caught &&
                host.Payloads.InterceptorAt(db.View).CaughtCount == 2,
                path + " later broad catch replays CAUGHT in one window");
            host.ResetSimulation();
            PayloadState targeted = host.Payloads.Spawn("DB", PayloadMotionProfile.Steady,
                origin.NodeId, new[] { .8f });
            Check(host.Payloads.TryIntercept(targeted.Id, "DB", out _) &&
                dbStamp.CurrentKind == NodeEventStamp.EventKind.Intercept,
                path + " targeted interceptor activation");
            host.StepSimulation(2f);
            Check(targeted.Intercepted && dbStamp.CurrentKind ==
                NodeEventStamp.EventKind.Caught, path + " targeted catch announces CAUGHT");
            host.ResetSimulation();

            host.ApplyCondition(api, new ConditionPayload(ConditionKind.PortScanned, 20f));
            presenter.enabled = false;
            presenter.enabled = true;
            presenter.RenderNow(now, 1f);
            NodeEventStamp reenabled = presenter.StampFor(api.View);
            Check(reenabled != null && !reenabled.IsPlaying &&
                presenter.StateFor(api.View).PortScanned,
                path + " existing Condition is shown without a stale event replay");
            host.ResetSimulation();
            Check(!reenabled.IsPlaying && !presenter.StateFor(api.View).HasAny(now),
                path + " reset clears reenabled presentation");
        }

        private static void CheckStampArtwork(NodeEventStamp stamp, string path)
        {
            var serialized = new SerializedObject(stamp);
            var icon = (Image)serialized.FindProperty("icon").objectReferenceValue;
            var background = (Image)serialized.FindProperty("background").objectReferenceValue;
            var symbols = serialized.FindProperty("symbols");
            var frames = serialized.FindProperty("frames");
            Color pain = serialized.FindProperty("pain").colorValue;
            Color awareness = serialized.FindProperty("awareness").colorValue;
            Color beneficial = serialized.FindProperty("beneficial").colorValue;
            Check(symbols.arraySize == 8 && frames.arraySize > 4,
                path + " eight symbols and animated background bound");
            foreach (NodeEventStamp.EventKind kind in Enum.GetValues(typeof(NodeEventStamp.EventKind)))
            {
                stamp.Play(kind);
                stamp.RenderNow(Time.unscaledTime + .2f);
                Color expected = kind == NodeEventStamp.EventKind.Offline ||
                    kind == NodeEventStamp.EventKind.Infected ||
                    kind == NodeEventStamp.EventKind.ScanJammed ? pain :
                    kind == NodeEventStamp.EventKind.PortScanned ? awareness : beneficial;
                Check(stamp.IsPlaying && icon.sprite ==
                    symbols.GetArrayElementAtIndex((int)kind).objectReferenceValue &&
                    background.sprite == frames.GetArrayElementAtIndex(4).objectReferenceValue &&
                    Mathf.Abs(icon.color.r - expected.r) < .001f &&
                    Mathf.Abs(icon.color.g - expected.g) < .001f &&
                    Mathf.Abs(icon.color.b - expected.b) < .001f &&
                    icon.color.a > 0f && background.color.a > 0f,
                    path + " symbol, tint, and unscaled animation for " + kind);
            }
            stamp.Cancel();
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("NodeStatusWitness: " + message);
        }
    }
}
