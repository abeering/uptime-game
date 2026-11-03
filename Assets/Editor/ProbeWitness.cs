using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class ProbeWitness
    {
        [MenuItem("Tools/Connections/Run Probe Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            Check(host != null && director != null && presenter != null &&
                recorder != null && terminal != null,
                "scene services");
            Check(new SerializedObject(presenter).FindProperty("probeRegionPrefab")
                .objectReferenceValue != null, "authored scanner binding");
            ScannerShader();
            bool previousAuto = director.AutoGenerationEnabled;
            bool previousHostiles = director.HostileGenerationEnabled;
            bool previousRecording = recorder.RecordingEnabled;
            try
            {
                director.AutoGenerationEnabled = false;
                director.HostileGenerationEnabled = false;
                recorder.RecordingEnabled = true;
                host.SetRunMode(ConnectionRunMode.Generated);
                recorder.BeginRun();
                Check(host.SimulationTimeSeconds == 0f && host.ProbedLink == null &&
                    host.ProbeCooldownRemaining == 0f, "fresh state");
                Check(!host.TryActivateProbe("no-such-edge", "probe no-such-edge", out _) &&
                    host.ProbedLink == null, "invalid link has no cost");

                PhysicalLinkView first = Link(host, "EXT1", "API");
                PhysicalLinkView second = Link(host, "API", "DB");
                Check(host.TryActivateProbe(first.LinkId.ToUpperInvariant(),
                    "probe " + first.LinkId, out _) && host.ProbedLink.View == first &&
                    host.ProbeCooldownRemaining == 35f, "case-insensitive immediate activation");
                Check(!host.TryActivateProbe(second.LinkId, "probe " + second.LinkId,
                    out _) && host.ProbedLink.View == first, "busy does not replace Probe");

                Queue(host, director, "probe-hostile", true, "DB", 800f);
                Queue(host, director, "probe-benign", false, "DB", 800f);
                Queue(host, director, "probe-other", true, "AUTH", 800f);
                host.StepSimulation(0.01f);
                Check(host.FindConnection("probe-hostile").IsConnecting &&
                    host.FindConnection("probe-hostile").IsHostile &&
                    !host.FindConnection("probe-benign").IsHostile,
                    "new arrivals retain authoritative classification while Connecting");
                presenter.Bind(host.RuntimeConnections.ToArray());
                presenter.RefreshLayouts();
                Check(ProbeSegment(presenter, "probe-hostile") == 0 &&
                    ProbeSegment(presenter, "probe-benign") == 0 &&
                    ProbeSegment(presenter, "probe-other") == 0,
                    "all visible first-edge lanes classified");
                Check(ProbeColor(presenter, "probe-hostile").r > 0.95f &&
                    ProbeColor(presenter, "probe-benign").r < 0.7f,
                    "hostile red and benign gray derive from simulation identity");

                host.StepSimulation(1.3f);
                Check(host.FindConnection("probe-hostile").IsActive,
                    "classification persists after establishment");
                CheckScannerPadding(host, presenter, first);
                Check(host.KillConnection("probe-hostile"), "ordinary kill remains available");
                presenter.RefreshLayouts();
                Check(ProbeSegment(presenter, "probe-hostile") == -1,
                    "resolved connection loses classification");
                int expiries = 0;
                void Expired(string _) => expiries++;
                host.ProbeExpired += Expired;
                try
                {
                    host.StepSimulation(8.69f); // 0.01 + 1.3 + 8.69 = 10
                    Check(host.ProbedLink == null && expiries == 1 &&
                        Mathf.Abs(host.ProbeCooldownRemaining - 25f) < .001f,
                        "exact expiration and activation-origin cooldown");
                    Check(!host.TryActivateProbe(second.LinkId, "probe " + second.LinkId,
                        out string cooldown) && cooldown.Contains("25.0"),
                        "cooldown reports remaining simulation seconds");
                    host.StepSimulation(25f);
                    Check(host.TryActivateProbe(second.LinkId, "probe " + second.LinkId,
                        out _) && host.ProbedLink.View == second,
                        "reusable at cooldown deadline");
                    presenter.RefreshLayouts();
                    Check(ProbeSegment(presenter, "probe-benign") == 1 &&
                        ProbeSegment(presenter, "probe-other") == -1,
                        "selected edge only, including multi-link route and independent target");
                    CheckScannerPadding(host, presenter, second);
                    typeof(Terminal).GetMethod("HandleSubmit",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(terminal, new object[] { "probe reset" });
                    Check(host.ProbedLink.View == second &&
                        host.ProbeCooldownRemaining == 0f &&
                        !host.TryActivateProbe(first.LinkId, "probe " + first.LinkId,
                            out string activeResult) && activeResult.Contains("BUSY"),
                        "development reset clears cooldown but keeps active Probe");
                    Check(recorder.CurrentLog.events.Any(e => e.type == "player_action" &&
                        e.command == "probe reset" && e.success &&
                        e.reason == "probe_cooldown_reset" &&
                        e.probeLinkId == second.LinkId),
                        "development reset is recorded");
                    host.StepSimulation(10f);
                    Check(host.ProbedLink == null && host.ProbeCooldownRemaining == 0f &&
                        host.TryActivateProbe(first.LinkId, "probe " + first.LinkId, out _),
                        "cooldown reset allows deployment after active expiry");
                    Check(recorder.CurrentLog.events.Any(e => e.type == "probe_expired" &&
                        e.probeLinkId == first.LinkId && Mathf.Abs(e.timeSeconds - 10f) < .001f &&
                        Mathf.Abs(e.probeExpiresAtSeconds - 10f) < .001f),
                        "recorder expiration timestamp and edge");
                    Check(recorder.CurrentLog.events.Any(e => e.type == "player_action" &&
                        e.probeLinkId == "no-such-edge" && !e.success &&
                        e.reason == "unknown_edge") &&
                        recorder.CurrentLog.events.Any(e => e.type == "player_action" &&
                        e.probeLinkId == second.LinkId && !e.success && e.reason == "busy") &&
                        recorder.CurrentLog.events.Any(e => e.type == "player_action" &&
                        e.probeLinkId == second.LinkId && !e.success && e.reason == "cooldown"),
                        "recorder retains failed attempts with selected edge and reason");
                    host.ResetSimulation();
                    Check(host.ProbedLink == null && host.ProbeCooldownRemaining == 0f &&
                        host.TryActivateProbe(first.LinkId, "probe " + first.LinkId, out _),
                        "reset clears deployment and cooldown");
                    host.enabled = false;
                    Check(host.ProbedLink == null && host.ProbeCooldownRemaining == 0f,
                        "disable clears deployment and cooldown");
                    host.enabled = true;
                }
                finally { host.ProbeExpired -= Expired; }

                var events = recorder.CurrentLog.events;
                Check(events.Any(e => e.type == "player_action" &&
                    e.probeLinkId == first.LinkId && e.success) &&
                    events.Any(e => e.type == "probe_activated" &&
                    e.probeLinkId == first.LinkId) , "recorder activation and action");
                MeshClipping();
                EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
                var advanced = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
                Check(advanced != null && new SerializedObject(advanced)
                    .FindProperty("probeRegionPrefab").objectReferenceValue != null,
                    "advanced scene scanner binding");
                Debug.Log("ProbeWitness PASS: command, clock, cooldown, reset, arrivals, " +
                    "classification, departure, recorder, mesh clipping, both scenes.");
            }
            finally
            {
                recorder.EndRun();
                recorder.RecordingEnabled = previousRecording;
                director.AutoGenerationEnabled = previousAuto;
                director.HostileGenerationEnabled = previousHostiles;
            }
        }

        private static void Queue(SimulationHost host, ConnectionTrafficDirector director,
            string id, bool hostile, string target, float work)
        {
            NodeView ext = host.Nodes.Single(n => n.View.NodeId == "EXT1").View;
            NodeView api = host.Nodes.Single(n => n.View.NodeId == "API").View;
            NodeView destination = host.Nodes.Single(n => n.View.NodeId == target).View;
            var stage = new OperationStage(OperationKind.Write, work, 1f, "Probe witness");
            director.QueueSpawnPlan(new ConnectionSpawnPlan(host.SimulationTimeSeconds,
                id, new[] { ext, api, destination }, "probe-witness", "Probe witness",
                new[] { stage }, hostile));
        }

        private static PhysicalLinkView Link(SimulationHost host, string a, string b) =>
            host.PhysicalLinks.Select(l => l.View).Single(l =>
                l.NodeA.NodeId == a && l.NodeB.NodeId == b ||
                l.NodeA.NodeId == b && l.NodeB.NodeId == a);

        private static LogicalPathView View(LogicalPathPresenter presenter, string id) =>
            presenter.PathLayer.GetComponentsInChildren<LogicalPathView>(true)
                .Single(v => v.name == "Logical path " + id);

        private static int ProbeSegment(LogicalPathPresenter presenter, string id) =>
            (int)typeof(LogicalPathView).GetField("probeSegment",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(View(presenter, id));

        private static Color ProbeColor(LogicalPathPresenter presenter, string id) =>
            (Color)typeof(LogicalPathView).GetField("probeTint",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(View(presenter, id));

        private static void CheckScannerPadding(SimulationHost host,
            LogicalPathPresenter presenter, PhysicalLinkView link)
        {
            foreach (LogicalConnectionState state in host.RuntimeConnections)
                View(presenter, state.ConnectionId).AdvanceVisual(.3f);
            presenter.RefreshLayouts();
            link.GetRenderedSpine(out Vector2 a, out Vector2 b);
            Vector2 center = (a + b) * .5f;
            Vector2 along = (b - a).normalized;
            Vector2 across = new Vector2(-along.y, along.x);
            float expectedHalfWidth = 10f;
            foreach (LogicalConnectionState state in host.RuntimeConnections)
            {
                if (!state.IsActive && !state.IsConnecting) continue;
                LogicalPathView view = View(presenter, state.ConnectionId);
                for (int segment = 0; segment < state.Definition.Route.Length - 1; segment++)
                {
                    NodeView routeA = state.Definition.Route[segment].Node;
                    NodeView routeB = state.Definition.Route[segment + 1].Node;
                    if (!(routeA == link.NodeA && routeB == link.NodeB ||
                          routeA == link.NodeB && routeB == link.NodeA)) continue;
                    if (!view.TryGetVisualSegment(segment, out Vector2 start, out Vector2 end,
                        out float visible, out _, out float width) || visible <= 0f) continue;
                    end = Vector2.Lerp(start, end, visible);
                    Vector2 direction = (end - start).normalized;
                    Vector2 normal = new Vector2(-direction.y, direction.x);
                    float centerlineExtent = Mathf.Max(
                        Mathf.Abs(Vector2.Dot(start - center, across)),
                        Mathf.Abs(Vector2.Dot(end - center, across)));
                    float outerEdge = centerlineExtent + width * .5f *
                        Mathf.Abs(Vector2.Dot(normal, across));
                    expectedHalfWidth = Mathf.Max(expectedHalfWidth, outerEdge + 14f);
                }
            }
            var scanner = (RectTransform)typeof(LogicalPathPresenter)
                .GetField("probeRegion", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(presenter);
            Check(scanner != null &&
                Mathf.Abs(scanner.rect.height * .5f - expectedHalfWidth) < .5f,
                "scanner includes rendered line thickness and 14 px side padding");
            Check(Mathf.Abs(scanner.rect.width - Vector2.Distance(a, b) * .8f) < .5f,
                "scanner retains 80 percent link coverage");
        }

        private static void MeshClipping()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Connections/LogicalPath.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var graphic = instance.GetComponent<PathGraphic>();
                graphic.color = Color.green;
                foreach (Vector2 along in new[] { Vector2.right, Vector2.up,
                    new Vector2(1f, 1f).normalized })
                {
                    graphic.SetPath(new[] { Vector2.zero, along * 100f },
                        new[] { along * 100f, along * 200f },
                        new[] { 4f, 4f }, 1f);
                    graphic.SetProbe(0, along * 50f, along,
                        new Vector2(40, 12), Color.red);
                    using (var mesh = new VertexHelper())
                    {
                        typeof(PathGraphic).GetMethod("OnPopulateMesh",
                            BindingFlags.Instance | BindingFlags.NonPublic, null,
                            new[] { typeof(VertexHelper) }, null)
                            .Invoke(graphic, new object[] { mesh });
                        bool red = false, ordinaryBefore = false, ordinaryAfter = false;
                        for (int i = 0; i < mesh.currentVertCount; i++)
                        {
                            UIVertex vertex = default;
                            mesh.PopulateUIVertex(ref vertex, i);
                            float distance = Vector2.Dot(new Vector2(vertex.position.x,
                                vertex.position.y), along);
                            if (vertex.color.r > 240 && vertex.color.g < 10)
                            {
                                red = true;
                                Check(distance >= 8f && distance <= 92f,
                                    "red mesh stays inside rotated scanner span");
                            }
                            else if (distance < 5f) ordinaryBefore = true;
                            else if (distance > 105f) ordinaryAfter = true;
                        }
                        Check(red && ordinaryBefore && ordinaryAfter,
                            "only selected physical segment is recolored");
                    }
                }
                graphic.SetPath(new[] { Vector2.zero, new Vector2(100, 0) },
                    new[] { new Vector2(100, 0), new Vector2(200, 0) },
                    new[] { 4f, 4f }, .25f);
                graphic.SetProbe(1, new Vector2(150, 0), Vector2.right,
                    new Vector2(40, 12), Color.red);
                using (var hidden = new VertexHelper())
                {
                    typeof(PathGraphic).GetMethod("OnPopulateMesh",
                        BindingFlags.Instance | BindingFlags.NonPublic, null,
                        new[] { typeof(VertexHelper) }, null)
                        .Invoke(graphic, new object[] { hidden });
                    for (int i = 0; i < hidden.currentVertCount; i++)
                    {
                        UIVertex vertex = default;
                        hidden.PopulateUIVertex(ref vertex, i);
                        Check(vertex.position.x < 55f && vertex.color.g > 240,
                            "future segment stays hidden while Connecting");
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static void ScannerShader()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Connections/ProbeRegion.prefab");
            Check(prefab != null && prefab.GetComponentsInChildren<Image>(true).Length == 0,
                "scanner uses a custom graphic instead of unshaded Images");
            var graphic = prefab.GetComponent<ProbeRegionGraphic>();
            Check(graphic != null && !graphic.raycastTarget &&
                AssetDatabase.GetAssetPath(graphic.material) ==
                "Assets/Shaders/ConnectionsLine.mat", "scanner uses the shared line shader");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                using (var mesh = new VertexHelper())
                {
                    typeof(ProbeRegionGraphic).GetMethod("OnPopulateMesh",
                        BindingFlags.Instance | BindingFlags.NonPublic, null,
                        new[] { typeof(VertexHelper) }, null)
                        .Invoke(instance.GetComponent<ProbeRegionGraphic>(), new object[] { mesh });
                    Check(mesh.currentVertCount == 20,
                        "scanner fill and all four border lines use shader geometry");
                    for (int i = 0; i < mesh.currentVertCount; i++)
                    {
                        UIVertex vertex = default;
                        mesh.PopulateUIVertex(ref vertex, i);
                        Check(vertex.uv0.z > 0f && vertex.uv0.w > 0f,
                            "scanner vertices carry line coverage coordinates");
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("ProbeWitness FAILED: " + message);
        }
    }
}
