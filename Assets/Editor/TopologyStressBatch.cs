using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class TopologyStressBatch
    {
        private const string Active = "Uptime.Stress.Active";
        private const string Phase = "Uptime.Stress.Phase";
        private const string Index = "Uptime.Stress.Index";
        private const string Mode = "Uptime.Stress.Mode";
        private const string OutputV0 = "Docs/research/topology-stress-v0";
        private const string OutputV1 = "Docs/research/topology-placement-v1";
        private const string BaseColumns =
            "nodes,topology_seed,traffic_seed,density,links,origins,active_connections,node_overlaps,node_overflow,links_through_other_nodes,min_visible_link_px,below_min_width_links,rotating_links,overlaid_links,unavailable_links,visible_ids_first_phase,total_active_link_uses";
        private const string PlacementColumns =
            ",link_crossings,links_below_120_px,links_below_180_px,occupied_links_below_120_px,occupied_links_below_180_px,legacy_link_crossings,legacy_links_below_120_px,legacy_links_below_180_px,legacy_occupied_links_below_120_px,legacy_occupied_links_below_180_px";
        private static readonly int[] Counts = { 4, 8, 12, 16, 20, 24, 28 };
        private static readonly int[] Seeds = { 1701, 3141, 9001 };
        private static double captureAt;

        static TopologyStressBatch() => EditorApplication.update += Tick;

        // Run in an isolated project with -batchmode, without -quit. The runner exits Unity.
        public static void Run() => Start(StressPlacementMode.LegacyV0);

        public static void RunPlacementV1() => Start(StressPlacementMode.OptimizedV1);

        private static void Start(StressPlacementMode mode)
        {
            if (!Application.isBatchMode || SessionState.GetBool(Active, false))
                throw new InvalidOperationException("Run once in an isolated batch Editor.");
            string output = mode == StressPlacementMode.LegacyV0 ? OutputV0 : OutputV1;
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "measurements.csv"), BaseColumns +
                (mode == StressPlacementMode.OptimizedV1 ? PlacementColumns : "") + "\n");
            SessionState.SetBool(Active, true);
            SessionState.SetInt(Index, 0);
            SessionState.SetInt(Phase, 0);
            SessionState.SetInt(Mode, (int)mode);
            Debug.Log("TopologyStressBatch: starting 21 " + mode + " real-traffic configurations.");
        }

        private static StressPlacementMode CurrentMode =>
            (StressPlacementMode)SessionState.GetInt(Mode, 0);

        private static string CurrentOutput =>
            CurrentMode == StressPlacementMode.LegacyV0 ? OutputV0 : OutputV1;

        private static void Tick()
        {
            if (!SessionState.GetBool(Active, false)) return;
            try
            {
                int index = SessionState.GetInt(Index, 0);
                if (index >= Counts.Length * Seeds.Length)
                {
                    SessionState.SetBool(Active, false);
                    Debug.Log("TopologyStressBatch PASS: 21 " + CurrentMode +
                        " captures and measurements.");
                    EditorApplication.Exit(0);
                    return;
                }
                int phase = SessionState.GetInt(Phase, 0);
                if (phase == 0 && !EditorApplication.isPlaying)
                {
                    EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsTopologyStress.unity");
                    var harness = UnityEngine.Object.FindFirstObjectByType<TopologyStressHarness>();
                    if (harness == null) throw new InvalidOperationException("Stress scene lacks harness.");
                    int count = Counts[index % Counts.Length];
                    int seed = Seeds[index / Counts.Length];
                    harness.Configure(count, seed, 2401, StressTrafficDensity.Typical);
                    harness.SetPlacementMode(CurrentMode);
                    SessionState.SetInt(Phase, 1);
                    EditorApplication.isPlaying = true;
                }
                else if (phase == 1 && EditorApplication.isPlaying)
                {
                    var harness = UnityEngine.Object.FindFirstObjectByType<TopologyStressHarness>();
                    var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                    if (harness == null || harness.Graph == null || host == null ||
                        host.RuntimeConnections == null) return;
                    harness.enabled = false;
                    host.enabled = false;
                    var presenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
                    presenter.SetPreviewMode(true);
                    for (int second = 0; second < 60; second++)
                    {
                        harness.StepTraffic();
                        host.StepSimulation(1f);
                    }
                    presenter.SetPreviewMode(false);
                    captureAt = EditorApplication.timeSinceStartup + 2.5;
                    SessionState.SetInt(Phase, 2);
                }
                else if (phase == 2 && EditorApplication.isPlaying &&
                    EditorApplication.timeSinceStartup >= captureAt)
                {
                    Capture(index);
                    SessionState.SetInt(Phase, 3);
                    EditorApplication.isPlaying = false;
                }
                else if (phase == 3 && !EditorApplication.isPlaying)
                {
                    SessionState.SetInt(Index, index + 1);
                    SessionState.SetInt(Phase, 0);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool(Active, false);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Capture(int index)
        {
            var harness = UnityEngine.Object.FindFirstObjectByType<TopologyStressHarness>();
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
            var identity = UnityEngine.Object.FindFirstObjectByType<IdentityPresenter>();
            string file = $"n{harness.NodeCount:00}-s{harness.TopologySeed}.png";
            string measurement = null;
            Render(Path.Combine(CurrentOutput, file), presenter, () =>
            {
                foreach (PhysicalLinkView link in harness.GeneratedLinks)
                    link.SendMessage("LateUpdate");
                presenter.SendMessage("UpdateLayouts");
                foreach (LogicalPathView view in UnityEngine.Object.FindObjectsByType<LogicalPathView>(
                    FindObjectsSortMode.None))
                    view.AdvanceVisual(1f);
                presenter.SendMessage("UpdateLayouts");
                identity.Tick();
                Canvas.ForceUpdateCanvases();
                measurement = Measure(harness, host, presenter, identity);
            });
            File.AppendAllText(Path.Combine(CurrentOutput, "measurements.csv"), measurement);
            Debug.Log("TopologyStressBatch captured " + file);
        }

        private static string Measure(TopologyStressHarness harness, SimulationHost host,
            LogicalPathPresenter presenter, IdentityPresenter identity)
        {
            RectTransform area = presenter.NetworkArea;
            var nodes = harness.GeneratedNodes;
            var links = harness.GeneratedLinks;
            Rect[] cards = nodes.Select(n => EdgeGeometry.CardRect(n, area)).ToArray();
            int overlaps = 0, overflow = 0, crossings = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                if (!area.rect.Contains(cards[i].min) || !area.rect.Contains(cards[i].max))
                    overflow++;
                for (int j = i + 1; j < cards.Length; j++)
                    if (cards[i].Overlaps(cards[j])) overlaps++;
            }
            float shortest = float.PositiveInfinity;
            int narrow = 0, rotating = 0, overlaid = 0, unavailable = 0;
            int visibleIds = 0, linkUses = 0;
            var usersByLink = new int[links.Count];
            int linkIndex = 0;
            foreach (PhysicalLinkView link in links)
            {
                int aIndex = IndexOf(nodes, link.NodeA);
                int bIndex = IndexOf(nodes, link.NodeB);
                Vector2 a = cards[aIndex].center;
                Vector2 b = cards[bIndex].center;
                Vector2 direction = (b - a).normalized;
                float visible = Vector2.Distance(EdgeGeometry.CardEdge(cards[aIndex], direction, 0f),
                    EdgeGeometry.CardEdge(cards[bIndex], -direction, 0f));
                shortest = Mathf.Min(shortest, visible);
                for (int i = 0; i < cards.Length; i++)
                    if (i != aIndex && i != bIndex && PassesThrough(a, b, cards[i])) crossings++;
                int users = 0;
                foreach (LogicalConnectionState state in host.RuntimeConnections)
                {
                    if (!state.IsActive) continue;
                    RouteNode[] route = state.Definition.Route;
                    for (int step = 0; step + 1 < route.Length; step++)
                        if ((route[step].Node == link.NodeA && route[step + 1].Node == link.NodeB) ||
                            (route[step].Node == link.NodeB && route[step + 1].Node == link.NodeA))
                            users++;
                }
                linkUses += users;
                usersByLink[linkIndex++] = users;
                if (users == 0) continue;
                IdentityPacking.Layout layout = identity.ResolveLayout(link, users,
                    presenter.MaxLineWidth, presenter.MinLineWidth, presenter.MinLineGap);
                if (layout.LineWidth < presenter.MinLineWidth) narrow++;
                if (layout.Stage == IdentityPacking.ReadabilityStage.Rotating) rotating++;
                if (layout.Stage == IdentityPacking.ReadabilityStage.Overlaid) overlaid++;
                if (layout.Stage == IdentityPacking.ReadabilityStage.Unavailable) unavailable++;
                if (layout.Schedule.Count > 0) visibleIds += layout.Schedule[0].Count;
            }
            string row =
                $"{harness.NodeCount},{harness.TopologySeed},{harness.TrafficSeed},{harness.Density}," +
                $"{links.Count},{harness.Graph.Nodes.Count(n => n.External)}," +
                $"{host.RuntimeConnections.Count(c => c.IsActive)},{overlaps},{overflow},{crossings}," +
                $"{shortest:0.##},{narrow},{rotating},{overlaid},{unavailable},{visibleIds},{linkUses}";
            if (CurrentMode == StressPlacementMode.LegacyV0) return row + "\n";
            var centers = cards.Select(card => card.center).ToArray();
            Vector2 cardSize = cards[0].size;
            TopologyStressGeometry.Measurements current = TopologyStressGeometry.Measure(
                harness.Graph, centers, area.rect.size, cardSize);
            Vector2[] legacyPositions = StressTopologyPlacement.Place(harness.Graph,
                area.rect.size, harness.TopologySeed);
            TopologyStressGeometry.Measurements legacy = TopologyStressGeometry.Measure(
                harness.Graph, legacyPositions, area.rect.size, cardSize);
            int currentOccupied120 = 0, currentOccupied180 = 0;
            int legacyOccupied120 = 0, legacyOccupied180 = 0;
            for (int i = 0; i < usersByLink.Length; i++)
            {
                if (usersByLink[i] == 0) continue;
                if (current.VisibleLengths[i] < 120f) currentOccupied120++;
                if (current.VisibleLengths[i] < 180f) currentOccupied180++;
                if (legacy.VisibleLengths[i] < 120f) legacyOccupied120++;
                if (legacy.VisibleLengths[i] < 180f) legacyOccupied180++;
            }
            return row + $",{current.LinkCrossings},{current.Below120},{current.Below180}," +
                $"{currentOccupied120},{currentOccupied180},{legacy.LinkCrossings}," +
                $"{legacy.Below120},{legacy.Below180},{legacyOccupied120},{legacyOccupied180}\n";
        }

        private static int IndexOf(IReadOnlyList<NodeView> nodes, NodeView node)
        {
            for (int i = 0; i < nodes.Count; i++) if (nodes[i] == node) return i;
            throw new InvalidOperationException("Link endpoint missing from generated graph.");
        }

        private static bool PassesThrough(Vector2 a, Vector2 b, Rect card)
        {
            // Sampled centerline diagnostic; the report does not claim exact collision geometry.
            for (int step = 1; step < 40; step++)
                if (card.Contains(Vector2.Lerp(a, b, step / 40f))) return true;
            return false;
        }

        private static void Render(string path, LogicalPathPresenter presenter,
            Action beforeRender)
        {
            Canvas canvas = presenter.GetComponentInParent<Canvas>();
            var cameraObject = new GameObject("Stress capture camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(1920, 1080, 24);
            RenderMode oldMode = canvas.renderMode;
            Camera oldCamera = canvas.worldCamera;
            float oldDistance = canvas.planeDistance;
            try
            {
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.028f, 0.075f, 0.09f);
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                Canvas.ForceUpdateCanvases();
                beforeRender();
                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
