using System;
using System.Linq;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class GeometryWitness
    {
        [MenuItem("Tools/Connections/Run Geometry Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            Canvas.ForceUpdateCanvases();
            var presenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
            var preview = UnityEngine.Object.FindFirstObjectByType<GeometryPreview>();
            var identity = UnityEngine.Object.FindFirstObjectByType<IdentityPresenter>();
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var nodes = UnityEngine.Object.FindObjectsByType<NodeView>(FindObjectsSortMode.None);
            var links = UnityEngine.Object.FindObjectsByType<PhysicalLinkView>(FindObjectsSortMode.None);
            Check(presenter != null && preview != null && host != null && identity != null, "scene components");
            host.SetRunMode(ConnectionRunMode.AuthoredWitness);
            Check(presenter.NetworkArea != null && presenter.PathLayer != null && presenter.PathGraphicPrefab != null,
                "path prefab and area references");
            Check(nodes.Length == 4 && links.Length == 3, "four nodes and three links");
            CheckRouteLaneOrdering(presenter, nodes, links);
            foreach (NodeView node in nodes)
                foreach (Vector2 direction in new[] { Vector2.up, Vector2.up + Vector2.right, Vector2.right,
                    Vector2.down + Vector2.right, Vector2.down, Vector2.down + Vector2.left,
                    Vector2.left, Vector2.up + Vector2.left })
                    Check(node.BundleWidthToward(direction) > 0f, node.NodeId + " attachment width");

            foreach (PhysicalLinkView link in links)
            {
                float budget = link.BundleWidth(presenter.NetworkArea);
                Rect sourceCard = EdgeGeometry.CardRect(link.NodeA, presenter.NetworkArea);
                Rect destinationCard = EdgeGeometry.CardRect(link.NodeB, presenter.NetworkArea);
                Vector2 direction = (destinationCard.center - sourceCard.center).normalized;
                Check(budget > 0f, link.LinkId + " bundle width");
                foreach (int count in new[] { 1, 2, 3, 4, 8, 16 })
                {
                    float width = EdgeGeometry.LineWidth(
                        budget, count, presenter.MinLineGap, presenter.MaxLineWidth);
                    Check(width <= presenter.MaxLineWidth + 0.001f, "max line width");
                    float first = EdgeGeometry.Offset(0, count, width, presenter.MinLineGap);
                    float last = EdgeGeometry.Offset(count - 1, count, width, presenter.MinLineGap);
                    Check(Mathf.Abs(first + last) < 0.001f, "centered bundle");
                    bool gapFits = (count - 1) * presenter.MinLineGap <= budget + 0.001f;
                    if (gapFits)
                        Check(last + width * 0.5f <= budget * 0.5f + 0.001f,
                            "bundle within budget");
                    else
                        Check(width == 0f && EdgeGeometry.BelowMinimum(budget, count,
                            presenter.MinLineGap, presenter.MaxLineWidth, presenter.MinLineWidth),
                            "dense bundle reports saturated gap");
                    for (int i = 1; i < count; i++)
                    {
                        float previous = EdgeGeometry.Offset(i - 1, count, width, presenter.MinLineGap);
                        float current = EdgeGeometry.Offset(i, count, width, presenter.MinLineGap);
                        Check(current - previous - width >= presenter.MinLineGap - 0.001f, "minimum gap");
                    }
                    if (count % 2 == 1)
                        Check(Mathf.Abs(EdgeGeometry.Offset(count / 2, count, width,
                            presenter.MinLineGap)) < 0.001f, "odd center lane");
                    for (int i = 0; gapFits && i < count; i++)
                    {
                        float offset = EdgeGeometry.Offset(i, count, width, presenter.MinLineGap);
                        Check(EndCovered(sourceCard, EdgeGeometry.CardUnderlap(sourceCard,
                            direction, offset, width), direction, width),
                            link.LinkId + " source path end covered");
                        Check(EndCovered(destinationCard, EdgeGeometry.CardUnderlap(destinationCard,
                            -direction, -offset, width), direction, width),
                            link.LinkId + " destination path end covered");
                    }
                }
            }
            Check(EdgeGeometry.LineWidth(96f, 1, 3f, 12f) == 12f, "low density max");
            Check(EdgeGeometry.LineWidth(96f, 8, 3f, 12f) < 12f, "density shrink");
            Check(EdgeGeometry.LineWidth(72f, 8, 3f, 12f) <
                EdgeGeometry.LineWidth(96f, 8, 3f, 12f), "independent edge widths");
            Check(EdgeGeometry.BelowMinimum(72f, 16, 3f, 12f, 2.5f), "overflow threshold");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Connections/LogicalPath.prefab");
            Check(prefab != null && prefab.GetComponent<LogicalPathView>() != null &&
                prefab.GetComponent<PathGraphic>() != null, "authored path prefab");
            LogicalPathView activity = prefab.GetComponent<LogicalPathView>();
            Check(LogicalPathView.DirectionFor(OperationKind.Write) == 1 &&
                LogicalPathView.DirectionFor(OperationKind.Read) == -1 &&
                LogicalPathView.DirectionFor(OperationKind.Execute) == 0,
                "operation pulse direction");
            Check(activity.PulseSpacingFor(8f) < activity.PulseSpacingFor(2f) * 0.7f,
                "high requested LOAD has denser activity");
            Check(prefab.GetComponentsInChildren<TMPro.TMP_Text>(true).Length == 0, "old path tags removed");
            var pillPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Connections/IdentityPill.prefab");
            Check(pillPrefab != null && pillPrefab.GetComponent<IdentityPillView>() != null &&
                pillPrefab.GetComponent<PillGraphic>() != null &&
                pillPrefab.GetComponentsInChildren<TMPro.TMP_Text>(true).Length == 1, "authored identity pill");
            Check(new SerializedObject(presenter).FindProperty("identityPresenter").objectReferenceValue == identity &&
                new SerializedObject(preview).FindProperty("identityPresenter").objectReferenceValue == identity &&
                new SerializedObject(UnityEngine.Object.FindFirstObjectByType<Terminal>())
                    .FindProperty("identityPresenter").objectReferenceValue == identity, "identity scene bindings");
            var observedStages = new System.Collections.Generic.HashSet<IdentityPacking.ReadabilityStage>();
            foreach (PhysicalLinkView link in links)
                foreach (int count in new[] { 1, 2, 3, 4, 6, 8, 10, 12 })
                {
                    var layout = identity.ResolveLayout(link, count, presenter.MaxLineWidth,
                        presenter.MinLineWidth, presenter.MinLineGap);
                    CheckLayout(layout, link.BundleWidth(presenter.NetworkArea), count);
                    Rect sourceCard = EdgeGeometry.CardRect(link.NodeA,
                        presenter.NetworkArea);
                    Rect destinationCard = EdgeGeometry.CardRect(link.NodeB,
                        presenter.NetworkArea);
                    foreach (IdentityPacking.Lane lane in layout.Lanes)
                    {
                        Vector2 direction = (destinationCard.center - sourceCard.center).normalized;
                        Check(EndCovered(sourceCard, lane.Start, direction, lane.Width),
                            link.LinkId + " source lane is covered by its card");
                        Check(EndCovered(destinationCard, lane.End, direction, lane.Width),
                            link.LinkId + " destination lane is covered by its card");
                    }
                    observedStages.Add(layout.Stage);
                    Debug.Log($"{link.LinkId} {count}: {layout.Stage}, {layout.LineWidth:0.##} px line, " +
                        $"{layout.LineGap:0.##} px gap, {layout.Schedule[0].Count} visible");
                }
            foreach (PhysicalLinkView link in links)
            {
                float threeWidth = identity.ResolveLayout(link, 3, presenter.MaxLineWidth,
                    presenter.MinLineWidth, presenter.MinLineGap).LineWidth;
                float fourWidth = identity.ResolveLayout(link, 4, presenter.MaxLineWidth,
                    presenter.MinLineWidth, presenter.MinLineGap).LineWidth;
                Check(threeWidth + 0.01f >= fourWidth,
                    link.LinkId + " three paths stay at least as wide as four");
            }
            CheckRenderedPillsAtReferenceSize(presenter, identity, links);
            foreach (IdentityPacking.ReadabilityStage stage in new[] {
                IdentityPacking.ReadabilityStage.Centered,
                IdentityPacking.ReadabilityStage.Staggered,
                IdentityPacking.ReadabilityStage.Overlaid,
                IdentityPacking.ReadabilityStage.Rotating })
                Check(observedStages.Contains(stage), "all four readability stages are exercised");
            foreach (Vector2 direction in new[] { Vector2.up, Vector2.right,
                new Vector2(1f, 1f).normalized })
                foreach (int count in new[] { 2, 4, 8, 12 })
                    CheckLayout(IdentityPacking.Solve(Vector2.zero,
                        direction * 280f, 96f, count, 12f, 2.5f, 3f, new Vector2(54f, 28f),
                        6f, 20f, 12f, 1f,
                        Array.Empty<IdentityPacking.PillBounds>()), 96f, count);
            var stableLink = links.First(l => l.LinkId == "E1");
            var stableLayout = identity.ResolveLayout(stableLink, 12,
                presenter.MaxLineWidth, presenter.MinLineWidth, presenter.MinLineGap);
            identity.CycleCongestedEdges();
            Check(ReferenceEquals(stableLayout, identity.ResolveLayout(stableLink, 12,
                presenter.MaxLineWidth, presenter.MinLineWidth, presenter.MinLineGap)),
                "manual cycle never replans line geometry");
            Check(IdentityPacking.Solve(Vector2.zero, Vector2.up * 70f,
                96f, 12, 12f, 2.5f, 3f, new Vector2(54f, 28f), 6f, 20f, 12f, 1f,
                Array.Empty<IdentityPacking.PillBounds>()).Stage ==
                    IdentityPacking.ReadabilityStage.Unavailable,
                "short edge cannot fit an invalid pill");
            CheckLayout(IdentityPacking.Solve(Vector2.zero, Vector2.up * 110f,
                96f, 12, 12f, 2.5f, 3f, new Vector2(54f, 28f), 6f, 20f, 12f, 1f,
                Array.Empty<IdentityPacking.PillBounds>()), 96f, 12);

            host.ResetSimulation();
            var c1 = host.RuntimeConnections.First(c => c.ConnectionId == "c1");
            var c2 = host.RuntimeConnections.First(c => c.ConnectionId == "c2");
            var c3 = host.RuntimeConnections.First(c => c.ConnectionId == "c3");
            Check(c1.IsActive && c2.IsPending && c3.IsPending, "pending excludes delayed lanes");
            int startSteps = 0;
            while (c3.IsPending && startSteps++ < 50) host.StepSimulation(0.1f);
            Check(c1.IsActive && c2.IsActive && c3.IsActive, "delayed establishment includes c2/c3");
            float sharedRate = c1.ActualWorkRate;
            int steps = 0;
            while (!c3.IsComplete && steps++ < 1000) host.StepSimulation(0.1f);
            Check(c3.IsComplete && c1.IsActive, "c3 completion removes active lane");
            host.StepSimulation(0.1f);
            Check(c1.ActualWorkRate > sharedRate, "released capacity accelerates c1");
            host.ResetSimulation();
            CheckIdentityColors(presenter, host, nodes);
            CheckTerminalMarkers(presenter, host, nodes);
            Debug.Log("GeometryWitness PASS: scene references, edge budgets and stages, " +
                "rendered pill visibility at reference size and 6 px gap, rotated pill clearance " +
                "and cycle coverage, short edges, terminal markers, establishment and completion.");
            host.SetRunMode(ConnectionRunMode.Generated);
        }

        private static void CheckLayout(IdentityPacking.Layout layout, float budget, int count)
        {
            Check(layout != null && layout.Lanes.Length == count, "edge layout exists");
            Check(layout.Stage != IdentityPacking.ReadabilityStage.Unavailable,
                "edge supports at least one pill");
            Check(count * layout.LineWidth + (count - 1) * layout.LineGap <= budget + 0.02f,
                "pill-aware lines remain within attachment budget");
            Check(layout.Stage == IdentityPacking.ReadabilityStage.Rotating ||
                layout.Schedule.Count == 1 && layout.Schedule[0].Count == count,
                "all labels remain visible until rotation is necessary");
            if (layout.Stage == IdentityPacking.ReadabilityStage.Centered)
                Check(layout.Schedule[0].All(p => Mathf.Abs(p.Fraction - 0.5f) < 0.001f),
                    "centered stage has aligned pills");
            var covered = new System.Collections.Generic.HashSet<int>();
            foreach (var cycle in layout.Schedule)
            {
                for (int i = 0; i < cycle.Count; i++)
                {
                    var placement = cycle[i];
                    covered.Add(placement.LaneIndex);
                    var lane = layout.Lanes[placement.LaneIndex];
                    Vector2 center = Vector2.Lerp(lane.Start, lane.End, placement.Fraction);
                    Check((center - placement.Bounds.Center).sqrMagnitude < 0.001f,
                        "rotated pill attached to line");
                    Check((placement.Bounds.Along - (lane.End - lane.Start).normalized).sqrMagnitude < 0.001f,
                        "pill follows line direction");
                    float margin = (54f * 0.5f + 12f) / Vector2.Distance(lane.Start, lane.End);
                    Check(placement.Fraction >= margin - 0.001f &&
                        placement.Fraction <= 1f - margin + 0.001f, "pill stays inside edge");
                    for (int j = 0; j < i; j++)
                        Check(!IdentityPacking.Intersects(placement.Bounds,
                            cycle[j].Bounds, 6f), "no pill overlap");
                    if (layout.Stage == IdentityPacking.ReadabilityStage.Centered ||
                        layout.Stage == IdentityPacking.ReadabilityStage.Staggered)
                        for (int other = 0; other < count; other++)
                            if (other != placement.LaneIndex)
                                Check(!IdentityPacking.SegmentIntersects(placement.Bounds,
                                    layout.Lanes[other].Start, layout.Lanes[other].End,
                                    layout.Lanes[other].Width * 0.5f + 1f),
                                    "no unrelated line occlusion in stage A/B");
                }
            }
            Check(covered.Count == count, "cycling covers every connection");
            if (layout.Stage == IdentityPacking.ReadabilityStage.Rotating &&
                layout.Schedule[0].Count > 1)
                Check(layout.Schedule[0].Last().LaneIndex - layout.Schedule[0].First().LaneIndex >= count / 2,
                    "distributed first cycle");
        }

        private static void Check(bool condition, string name)
        {
            if (!condition)
                throw new InvalidOperationException("GeometryWitness failed: " + name);
        }

        private static void CheckIdentityColors(LogicalPathPresenter presenter,
            SimulationHost host, NodeView[] nodes)
        {
            host.SetRunMode(ConnectionRunMode.Generated);
            ConnectionIdentityPalette palette = presenter.IdentityPalette;
            Check(palette != null && ConnectionIdentityPalette.SlotCount == 6,
                "authored six-color identity palette");
            palette.Reset();
            var colors = Enumerable.Range(0, 6).Select(palette.ColorAt).ToArray();
            Check(colors.Distinct().Count() == 6, "six distinct authored accents");
            NodeView ext = nodes.First(n => n.NodeId == "EXT1");
            NodeView api = nodes.First(n => n.NodeId == "API");
            var states = new System.Collections.Generic.List<LogicalConnectionState>();
            for (int i = 0; i < 8; i++)
            {
                string id = "palette" + i;
                var plan = new ConnectionSpawnPlan(0f, id, new[] { ext, api },
                    "test", "Palette", new[] { new OperationStage(OperationKind.Read,
                        100f, 1f, "Read") });
                LogicalConnectionState state = host.RegisterConnection(
                    LogicalConnectionDefinition.FromPlan(plan));
                states.Add(state);
                Color assigned = palette.Assign(state);
                Check(palette.Assign(state) == assigned,
                    "identity accent is stable for the same state");
                if (i < 6) Check(assigned == colors[i], "first six arrivals rotate through palette");
                else Check(assigned == colors[i - 6], "overflow reuses slots deterministically");
            }
            palette.Release(states[1]);
            palette.Release(states[7]);
            Check(!palette.TryGet(states[1], out _) && states[1].ConnectionId == "palette1",
                "slot release keeps historical connection identity");
            var ninth = new ConnectionSpawnPlan(0f, "palette8", new[] { ext, api },
                "test", "Palette", new[] { new OperationStage(OperationKind.Read, 100f, 1f, "Read") });
            LogicalConnectionState reused = host.RegisterConnection(
                LogicalConnectionDefinition.FromPlan(ninth));
            Check(palette.Assign(reused) == colors[1],
                "released color is reused from rotating cursor");

            var pathPrefab = AssetDatabase.LoadAssetAtPath<LogicalPathView>(
                "Assets/Prefabs/Connections/LogicalPath.prefab");
            LogicalPathView path = UnityEngine.Object.Instantiate(pathPrefab);
            try
            {
                path.SetIdentityColor(colors[0]);
                Color slowed = new SerializedObject(path).FindProperty("constrainedColor").colorValue;
                Color stalled = new SerializedObject(path).FindProperty("stalledColor").colorValue;
                Check(path.BeamColorFor(LoadPerformance.AtSpeed) == colors[0] &&
                    path.BeamColorFor(LoadPerformance.Slowed) == slowed &&
                    path.BeamColorFor(LoadPerformance.Stalled) == stalled &&
                    slowed != colors[0] && stalled != colors[0],
                    "identity beam leaves warning treatments distinct");
            }
            finally { UnityEngine.Object.DestroyImmediate(path.gameObject); }

            var pillPrefab = AssetDatabase.LoadAssetAtPath<IdentityPillView>(
                "Assets/Prefabs/Connections/IdentityPill.prefab");
            IdentityPillView pill = UnityEngine.Object.Instantiate(pillPrefab);
            try
            {
                pill.Bind("A7", colors[0]);
                Check(pill.DisplayedId == "a7" && pill.AccentColor == colors[0],
                    "pill displays lowercase ID and keeps identity accent");
            }
            finally { UnityEngine.Object.DestroyImmediate(pill.gameObject); }

            palette.Reset();
            host.ResetSimulation();
        }

        private static void CheckTerminalMarkers(LogicalPathPresenter presenter,
            SimulationHost host, NodeView[] nodes)
        {
            var pathPrefab = AssetDatabase.LoadAssetAtPath<LogicalPathView>(
                "Assets/Prefabs/Connections/LogicalPath.prefab");
            var markerPrefab = AssetDatabase.LoadAssetAtPath<Image>(
                "Assets/Prefabs/Connections/TerminalMarker.prefab");
            Check(markerPrefab != null && !markerPrefab.raycastTarget &&
                markerPrefab.GetComponent<TerminalMarkerStyle>() != null &&
                markerPrefab.GetComponent<TerminalMarkerStyle>().Padding == new Vector2(4f, 4f) &&
                new SerializedObject(pathPrefab).FindProperty("terminalMarkerPrefab")
                    .objectReferenceValue == markerPrefab &&
                presenter.MarkerLayer != null && presenter.MarkerLayer.GetSiblingIndex() >
                    nodes[0].transform.parent.GetSiblingIndex(),
                "authored marker and overlay above node cards");

            Rect synthetic = new Rect(-120f, -40f, 240f, 80f);
            Vector2 side = LogicalPathView.TerminalBorderPoint(synthetic, Vector2.left, 0f,
                out bool vertical);
            Check(vertical && Mathf.Abs(side.x - synthetic.xMin) < 0.001f,
                "horizontal approach uses a vertical border marker");
            Vector2 diagonal = LogicalPathView.TerminalBorderPoint(synthetic,
                new Vector2(1f, 1f).normalized, 8f, out vertical);
            Check(!vertical && Mathf.Abs(diagonal.y - synthetic.yMax) < 0.001f,
                "diagonal approach uses the visible horizontal border");
            Vector2 padding = markerPrefab.GetComponent<TerminalMarkerStyle>().Padding;
            Rect straightMark = LogicalPathView.TerminalMarkerRect(synthetic, Vector2.left,
                0f, 8f, padding);
            Rect diagonalMark = LogicalPathView.TerminalMarkerRect(synthetic,
                new Vector2(1f, 1f), 0f, 8f, padding);
            Check(Mathf.Abs(straightMark.width - 4f) < 0.001f &&
                Mathf.Abs(straightMark.height - 12f) < 0.001f,
                "straight arrival uses beam width plus prefab padding");
            Check(Mathf.Abs(diagonalMark.width - (8f * Mathf.Sqrt(2f) + 4f)) < 0.01f &&
                Mathf.Abs(diagonalMark.height - 4f) < 0.001f,
                "diagonal arrival covers its wider border footprint");
            Rect cornerMark = LogicalPathView.TerminalMarkerRect(new Rect(-20f, -20f, 40f, 40f),
                new Vector2(1f, 1f), 0f, 8f, Vector2.zero);
            Check(cornerMark.xMax <= 20.001f && cornerMark.yMax <= 20.001f,
                "border footprint clips at card corner before padding");

            var area = presenter.NetworkArea;
            NodeView ext = nodes.First(n => n.NodeId == "EXT1");
            NodeView api = nodes.First(n => n.NodeId == "API");
            NodeView db = nodes.First(n => n.NodeId == "DB");
            var views = new System.Collections.Generic.List<LogicalPathView>();
            try
            {
                host.SetRunMode(ConnectionRunMode.AuthoredWitness);
                LogicalConnectionState pending = host.RuntimeConnections.First(c => c.ConnectionId == "c2");
                LogicalPathView pendingView = UnityEngine.Object.Instantiate(pathPrefab,
                    presenter.PathLayer, false);
                views.Add(pendingView);
                pendingView.Bind(pending, area, presenter.MarkerLayer);
                pendingView.SetLayout(new float[pending.Definition.Route.Length - 1],
                    Enumerable.Repeat(8f, pending.Definition.Route.Length - 1).ToArray());
                pendingView.AdvanceVisual(1f);
                Check(!pendingView.TerminalMarker.gameObject.activeSelf,
                    "pending connection has no terminal marker");

                host.SetRunMode(ConnectionRunMode.Generated);
                LogicalConnectionState apiState = host.RegisterConnection(
                    LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0f, "marker_api",
                        new[] { ext, api }, "test", "API target", new[] {
                            new OperationStage(OperationKind.Read, 0.1f, 1f, "Read"),
                            new OperationStage(OperationKind.Execute, 500f, 1f, "Execute") })));
                LogicalConnectionState dbState = host.RegisterConnection(
                    LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0f, "marker_db",
                        new[] { ext, api, db }, "test", "DB target", new[] {
                            new OperationStage(OperationKind.Read, 500f, 1f, "Read") })));
                LogicalConnectionState secondDb = host.RegisterConnection(
                    LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0f, "marker_db2",
                        new[] { ext, api, db }, "test", "Second DB target", new[] {
                            new OperationStage(OperationKind.Write, 500f, 1f, "Write") })));
                Color apiColor = presenter.IdentityPalette.ColorAt(0);
                Color dbColor = presenter.IdentityPalette.ColorAt(1);
                LogicalPathView apiView = UnityEngine.Object.Instantiate(pathPrefab,
                    presenter.PathLayer, false);
                LogicalPathView dbView = UnityEngine.Object.Instantiate(pathPrefab,
                    presenter.PathLayer, false);
                LogicalPathView secondView = UnityEngine.Object.Instantiate(pathPrefab,
                    presenter.PathLayer, false);
                views.Add(apiView);
                views.Add(dbView);
                views.Add(secondView);
                apiView.Bind(apiState, area, presenter.MarkerLayer);
                dbView.Bind(dbState, area, presenter.MarkerLayer);
                secondView.Bind(secondDb, area, presenter.MarkerLayer);
                apiView.SetIdentityColor(apiColor);
                dbView.SetIdentityColor(dbColor);
                secondView.SetIdentityColor(presenter.IdentityPalette.ColorAt(2));
                apiView.SetLayout(new[] { 0f }, new[] { 8f });
                dbView.SetLayout(new[] { 0f, -8f }, new[] { 8f, 8f });
                secondView.SetLayout(new[] { 0f, 8f }, new[] { 8f, 8f });
                apiView.AdvanceVisual(0.3f);
                dbView.AdvanceVisual(0.3f);
                Check(!apiView.TerminalMarker.gameObject.activeSelf &&
                    !dbView.TerminalMarker.gameObject.activeSelf,
                    "markers wait until establishment reaches target");
                apiView.AdvanceVisual(0.3f);
                dbView.AdvanceVisual(0.3f);
                secondView.AdvanceVisual(0.6f);
                Check(apiView.TerminalMarker.gameObject.activeSelf &&
                    dbView.TerminalMarker.gameObject.activeSelf &&
                    secondView.TerminalMarker.gameObject.activeSelf,
                    "each established route has one terminal marker");
                Rect apiCard = EdgeGeometry.CardRect(api, area);
                Rect dbCard = EdgeGeometry.CardRect(db, area);
                Vector2 apiPoint = area.InverseTransformPoint(apiView.TerminalMarker.rectTransform.position);
                Vector2 dbPoint = area.InverseTransformPoint(dbView.TerminalMarker.rectTransform.position);
                Vector2 secondPoint = area.InverseTransformPoint(secondView.TerminalMarker.rectTransform.position);
                Vector2 towardApi = (area.InverseTransformPoint(ext.transform.position) -
                    area.InverseTransformPoint(api.transform.position)).normalized;
                Vector2 towardDb = (area.InverseTransformPoint(api.transform.position) -
                    area.InverseTransformPoint(db.transform.position)).normalized;
                Vector2 expectedApi = LogicalPathView.TerminalBorderPoint(apiCard, towardApi, 0f,
                    out _);
                Vector2 expectedDb = LogicalPathView.TerminalBorderPoint(dbCard, towardDb, 8f,
                    out _);
                Check((apiPoint - expectedApi).sqrMagnitude < 0.01f &&
                    (dbPoint - expectedDb).sqrMagnitude < 0.01f &&
                    !apiCard.Contains(dbPoint),
                    "API target marks API; DB route passes API and marks DB");
                Check((dbPoint - secondPoint).sqrMagnitude > 1f &&
                    dbView.TerminalMarker != secondView.TerminalMarker,
                    "two DB targets retain distinct border markers");
                Rect expectedDbMarker = LogicalPathView.TerminalMarkerRect(dbCard, towardDb,
                    8f, 8f, padding);
                Check((dbView.TerminalMarker.rectTransform.sizeDelta -
                    expectedDbMarker.size).sqrMagnitude < 0.01f,
                    "live diagonal marker uses final segment width");
                Check(dbView.TerminalMarker.color == dbColor &&
                    dbView.BeamColorFor(LoadPerformance.Stalled) != dbColor,
                    "terminal marker keeps identity accent during beam warning");
                Vector2 hiddenEnd = EdgeGeometry.CardUnderlap(dbCard, towardDb, 8f, 8f);
                Check((dbPoint - hiddenEnd).sqrMagnitude > 0.1f,
                    "marker uses visible border instead of hidden underlap");

                dbView.SetLayout(new[] { 0f, 8f }, new[] { 8f, 4f });
                dbView.AdvanceVisual(0.15f);
                dbPoint = area.InverseTransformPoint(dbView.TerminalMarker.rectTransform.position);
                expectedDb = LogicalPathView.TerminalBorderPoint(dbCard, towardDb, 0f, out _);
                Vector2 finalPoint = LogicalPathView.TerminalBorderPoint(dbCard, towardDb, -8f,
                    out _);
                Rect reflowMarker = LogicalPathView.TerminalMarkerRect(dbCard, towardDb,
                    0f, 6f, padding);
                Check((dbPoint - expectedDb).sqrMagnitude < 0.01f &&
                    (dbPoint - finalPoint).sqrMagnitude > 1f,
                    "marker follows interpolated lane instead of snapping to final offset");
                Check((dbView.TerminalMarker.rectTransform.sizeDelta -
                    reflowMarker.size).sqrMagnitude < 0.01f &&
                    dbView.TerminalMarker.rectTransform.sizeDelta.x < expectedDbMarker.width,
                    "marker width follows interpolated beam width during reflow");
                dbView.SetPresentationActive(false);
                Check(!dbView.TerminalMarker.gameObject.activeSelf,
                    "preview hides marker with its owning path");
                dbView.SetPresentationActive(true);
                dbView.AdvanceVisual(0f);
                Check(dbView.TerminalMarker.gameObject.activeSelf,
                    "marker returns with its owning path");

                LogicalConnectionState shortState = host.RegisterConnection(
                    LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0f, "marker_done",
                        new[] { ext, api }, "test", "Short API target", new[] {
                            new OperationStage(OperationKind.Write, 0.1f, 1f, "Write") })));
                LogicalPathView shortView = UnityEngine.Object.Instantiate(pathPrefab,
                    presenter.PathLayer, false);
                views.Add(shortView);
                shortView.Bind(shortState, area, presenter.MarkerLayer);
                shortView.SetIdentityColor(presenter.IdentityPalette.ColorAt(3));
                shortView.SetLayout(new[] { 8f }, new[] { 8f });
                shortView.AdvanceVisual(0.6f);
                host.StepSimulation(0.2f);
                Check(shortState.IsComplete && apiState.IsTransitioning &&
                    apiView.TerminalMarker.gameObject.activeSelf,
                    "completion fades its marker while operation transition keeps its own");
                shortView.AdvanceVisual(0.2f);
                Check(shortView.TerminalMarker.color.a < 0.6f &&
                    shortView.TerminalMarker.color.a > 0f,
                    "completed marker fades with path departure");
                host.StepSimulation(0.1f);
                apiView.AdvanceVisual(0f);
                Check(apiState.CurrentOperation == OperationKind.Execute &&
                    apiView.TerminalMarker.gameObject.activeSelf,
                    "local EXECUTE retains target marker");
                Check(host.BlockConnection(dbState.ConnectionId), "block marker test connection");
                dbView.AdvanceVisual(0.2f);
                Check(dbView.TerminalMarker.color.a < 0.6f &&
                    dbView.TerminalMarker.color.a > 0f,
                    "blocked marker fades with path departure");
                Image retired = dbView.TerminalMarker;
                dbView.ReleaseTerminalMarker();
                UnityEngine.Object.DestroyImmediate(dbView.gameObject);
                Check(retired == null || !retired.gameObject.activeSelf,
                    "marker disappears with its owning path");
                views.Remove(dbView);
            }
            finally
            {
                foreach (LogicalPathView view in views)
                    if (view != null)
                    {
                        view.ReleaseTerminalMarker();
                        UnityEngine.Object.DestroyImmediate(view.gameObject);
                    }
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static void CheckRouteLaneOrdering(LogicalPathPresenter presenter,
            NodeView[] nodes, PhysicalLinkView[] links)
        {
            NodeView ext = nodes.First(n => n.NodeId == "EXT1");
            NodeView api = nodes.First(n => n.NodeId == "API");
            NodeView db = nodes.First(n => n.NodeId == "DB");
            NodeView auth = nodes.First(n => n.NodeId == "AUTH");
            PhysicalLinkView e1 = links.First(l => l.LinkId == "E1");
            PhysicalLinkView e2 = links.First(l => l.LinkId == "E2");
            PhysicalLinkView e3 = links.First(l => l.LinkId == "E3");
            var area = presenter.NetworkArea;
            var solver = new RouteLaneOrder();
            var routes = new System.Collections.Generic.List<RouteLaneOrder.Route>
            {
                new RouteLaneOrder.Route("C1", new[] { ext, api, db }, new[] { e1, e2 }),
                new RouteLaneOrder.Route("C2", new[] { ext, api }, new[] { e1 }),
                new RouteLaneOrder.Route("C3", new[] { ext, api, auth }, new[] { e1, e3 }),
                new RouteLaneOrder.Route("C4", new[] { ext, api, db }, new[] { e1, e2 })
            };
            solver.Update(routes, area);
            var incoming = solver.Orders[e1];
            Check(Mathf.Abs(incoming.IndexOf("C1") - incoming.IndexOf("C4")) == 1,
                "DB routes stay adjacent before API");
            Vector2 from = area.InverseTransformPoint(e1.NodeA.transform.position);
            Vector2 to = area.InverseTransformPoint(e1.NodeB.transform.position);
            Vector2 normal = new Vector2(-(to - from).normalized.y, (to - from).normalized.x);
            float dbSide = Vector2.Dot((area.InverseTransformPoint(db.transform.position) -
                area.InverseTransformPoint(api.transform.position)).normalized, normal);
            float authSide = Vector2.Dot((area.InverseTransformPoint(auth.transform.position) -
                area.InverseTransformPoint(api.transform.position)).normalized, normal);
            int dbRank = incoming.IndexOf("C1");
            int authRank = incoming.IndexOf("C3");
            Check((dbRank - authRank) * (dbSide - authSide) > 0,
                "DB and AUTH branches take their geometric sides");
            Check(incoming.IndexOf("C2") > Mathf.Min(dbRank, incoming.IndexOf("C4")) &&
                incoming.IndexOf("C2") < authRank,
                "terminal API route stays between branches");
            string initial = string.Join(",", incoming);
            int solveCount = solver.SolveCount;
            solver.Update(routes, area);
            Check(solver.SolveCount == solveCount && string.Join(",", solver.Orders[e1]) == initial,
                "unchanged route input does not re-solve or move lanes");
            Vector3 apiPosition = api.transform.localPosition;
            try
            {
                api.transform.localPosition = apiPosition + Vector3.right * 3f;
                Canvas.ForceUpdateCanvases();
                solver.Update(routes, area);
                Check(solver.SolveCount == solveCount + 1,
                    "node geometry change triggers route ordering");
            }
            finally
            {
                api.transform.localPosition = apiPosition;
                Canvas.ForceUpdateCanvases();
                solver.Update(routes, area);
            }

            // A connection using the same two ports in reverse must retain physical lane order.
            routes.Add(new RouteLaneOrder.Route("C5", new[] { auth, api, ext },
                new[] { e3, e1 }));
            solver.Update(routes, area);
            Check(solver.Orders[e1].IndexOf("C3") < solver.Orders[e1].IndexOf("C5") ==
                (solver.Orders[e3].IndexOf("C3") < solver.Orders[e3].IndexOf("C5")),
                "reverse traversal preserves canonical side across API");
            Check(solver.Orders[e1].IndexOf("C1") < solver.Orders[e1].IndexOf("C4"),
                "arrival preserves the existing DB pair order");
            routes.RemoveAt(routes.Count - 1);
            solver.Update(routes, area);
            Check(string.Join(",", solver.Orders[e1]) == initial,
                "departure restores stable four-route order");

            var dense = new System.Collections.Generic.List<RouteLaneOrder.Route>(routes);
            for (int i = 6; i <= 14; i++)
                dense.Add(new RouteLaneOrder.Route("C" + i,
                    i % 2 == 0 ? new[] { ext, api, db } : new[] { ext, api, auth },
                    i % 2 == 0 ? new[] { e1, e2 } : new[] { e1, e3 }));
            solver.Update(dense, area);
            Check(solver.Orders[e1].Count == dense.Count &&
                solver.Orders[e1].Distinct().Count() == dense.Count,
                "dense routes retain one unique lane each");
            string denseOrder = string.Join(",", solver.Orders[e1]);
            solveCount = solver.SolveCount;
            solver.Update(dense, area);
            Check(solver.SolveCount == solveCount && string.Join(",", solver.Orders[e1]) == denseOrder,
                "dense unchanged layout remains stable");
        }

        private static void CheckRenderedPillsAtReferenceSize(LogicalPathPresenter presenter,
            IdentityPresenter identity, PhysicalLinkView[] links)
        {
            RectTransform area = presenter.NetworkArea;
            RectTransform panel = (RectTransform)area.parent;
            Vector2 reference = area.GetComponentInParent<CanvasScaler>().referenceResolution;
            Vector2 originalSize = area.sizeDelta;
            float width = reference.x * (panel.anchorMax.x - panel.anchorMin.x) +
                panel.sizeDelta.x + originalSize.x;
            float height = reference.y * (panel.anchorMax.y - panel.anchorMin.y) +
                panel.sizeDelta.y + originalSize.y;
            try
            {
                area.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                area.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                Canvas.ForceUpdateCanvases();
                var pillLayer = (RectTransform)new SerializedObject(identity)
                    .FindProperty("pillLayer").objectReferenceValue;
                Debug.Log($"Geometry witness at reference layout: {area.rect.size}");
                foreach (float lineGap in new[] { presenter.MinLineGap, 6f })
                    foreach (PhysicalLinkView link in links)
                        foreach (int count in new[] { 3, 4, 5 })
                        {
                            var layout = identity.ResolveLayout(link, count, presenter.MaxLineWidth,
                                presenter.MinLineWidth, lineGap);
                            Check(layout.Schedule.Count > 0, link.LinkId + " has a pill schedule");
                            Vector2 a = area.InverseTransformPoint(link.NodeA.transform.position);
                            Vector2 b = area.InverseTransformPoint(link.NodeB.transform.position);
                            Vector2 direction = (b - a).normalized;
                            var liveLines = new System.Collections.Generic.List<IdentityPresenter.LiveLine>();
                            for (int i = 0; i < count; i++)
                            {
                                float offset = EdgeGeometry.Offset(i, count, layout.LineWidth, layout.LineGap);
                                Vector2 start = EdgeGeometry.CardUnderlap(link.NodeA, area,
                                    direction, offset, layout.LineWidth);
                                Vector2 end = EdgeGeometry.CardUnderlap(link.NodeB, area,
                                    -direction, -offset, layout.LineWidth);
                                liveLines.Add(new IdentityPresenter.LiveLine("c" + (i + 1), start, end,
                                    start, end, Color.white, 1f, layout.LineWidth));
                            }
                            identity.BeginFrame();
                            identity.PresentEdge(link, liveLines);
                            identity.EndFrame();
                            int shown = pillLayer.GetComponentsInChildren<IdentityPillView>(true)
                                .Count(pill => pill.gameObject.activeSelf);
                            Check(shown == layout.Schedule[0].Count,
                                $"{link.LinkId} {count} at {lineGap:0.#} px gap: {shown} live pills " +
                                $"versus {layout.Schedule[0].Count} planned");
                            if (Mathf.Approximately(lineGap, 6f) && count <= 4)
                                Check(shown == count,
                                    $"{link.LinkId} {count} at 6 px gap shows every identity");
                            Debug.Log($"{link.LinkId} {count} at {lineGap:0.#} px gap: " +
                                $"{layout.Stage}, {shown}/{count} pills shown");
                        }
            }
            finally
            {
                identity.BeginFrame();
                identity.EndFrame();
                area.sizeDelta = originalSize;
                Canvas.ForceUpdateCanvases();
            }
        }

        private static bool EndCovered(Rect card, Vector2 center, Vector2 direction, float width)
        {
            Vector2 normal = new Vector2(-direction.y, direction.x) * (width * 0.5f);
            return InsideCard(card, center - normal) && InsideCard(card, center + normal);
        }

        private static bool InsideCard(Rect card, Vector2 point)
        {
            const float inset = 1.9f;
            return point.x >= card.xMin + inset && point.x <= card.xMax - inset &&
                point.y >= card.yMin + inset && point.y <= card.yMax - inset;
        }
    }
}
