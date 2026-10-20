using System;
using System.Linq;
using Uptime.ConnectionsToy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConnectionsToyGeometryWitness
{
    [MenuItem("Tools/Connections Toy/Run Geometry Witness")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsToy.unity");
        Canvas.ForceUpdateCanvases();
        var presenter = UnityEngine.Object.FindFirstObjectByType<ConnectionsToyLogicalPathPresenter>();
        var preview = UnityEngine.Object.FindFirstObjectByType<ConnectionsToyGeometryPreview>();
        var identity = UnityEngine.Object.FindFirstObjectByType<ConnectionsToyIdentityPresenter>();
        var host = UnityEngine.Object.FindFirstObjectByType<ConnectionsToySimulationHost>();
        var nodes = UnityEngine.Object.FindObjectsByType<ConnectionsToyNodeView>(FindObjectsSortMode.None);
        var links = UnityEngine.Object.FindObjectsByType<ConnectionsToyPhysicalLinkView>(FindObjectsSortMode.None);
        Check(presenter != null && preview != null && host != null && identity != null, "scene components");
        Check(presenter.NetworkArea != null && presenter.PathLayer != null && presenter.PathGraphicPrefab != null,
            "path prefab and area references");
        Check(nodes.Length == 4 && links.Length == 3, "four nodes and three links");
        foreach (ConnectionsToyNodeView node in nodes)
            foreach (Vector2 direction in new[] { Vector2.up, Vector2.up + Vector2.right, Vector2.right,
                Vector2.down + Vector2.right, Vector2.down, Vector2.down + Vector2.left,
                Vector2.left, Vector2.up + Vector2.left })
                Check(node.BundleWidthToward(direction) > 0f, node.NodeId + " attachment width");

        foreach (ConnectionsToyPhysicalLinkView link in links)
        {
            float budget = link.BundleWidth(presenter.NetworkArea);
            Check(budget > 0f, link.LinkId + " bundle width");
            foreach (int count in new[] { 1, 2, 3, 4, 8, 16 })
            {
                float width = ConnectionsToyEdgeGeometry.LineWidth(
                    budget, count, presenter.MinLineGap, presenter.MaxLineWidth);
                Check(width <= presenter.MaxLineWidth + 0.001f, "max line width");
                float first = ConnectionsToyEdgeGeometry.Offset(0, count, width, presenter.MinLineGap);
                float last = ConnectionsToyEdgeGeometry.Offset(count - 1, count, width, presenter.MinLineGap);
                Check(Mathf.Abs(first + last) < 0.001f, "centered bundle");
                Check(last + width * 0.5f <= budget * 0.5f + 0.001f, "bundle within budget");
                for (int i = 1; i < count; i++)
                {
                    float previous = ConnectionsToyEdgeGeometry.Offset(i - 1, count, width, presenter.MinLineGap);
                    float current = ConnectionsToyEdgeGeometry.Offset(i, count, width, presenter.MinLineGap);
                    Check(current - previous - width >= presenter.MinLineGap - 0.001f, "minimum gap");
                }
                if (count % 2 == 1)
                    Check(Mathf.Abs(ConnectionsToyEdgeGeometry.Offset(count / 2, count, width,
                        presenter.MinLineGap)) < 0.001f, "odd center lane");
            }
        }
        Check(ConnectionsToyEdgeGeometry.LineWidth(96f, 1, 3f, 12f) == 12f, "low density max");
        Check(ConnectionsToyEdgeGeometry.LineWidth(96f, 8, 3f, 12f) < 12f, "density shrink");
        Check(ConnectionsToyEdgeGeometry.LineWidth(72f, 8, 3f, 12f) <
            ConnectionsToyEdgeGeometry.LineWidth(96f, 8, 3f, 12f), "independent edge widths");
        Check(ConnectionsToyEdgeGeometry.BelowMinimum(72f, 16, 3f, 12f, 2.5f), "overflow threshold");

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ConnectionsToy/ToyLogicalPath.prefab");
        Check(prefab != null && prefab.GetComponent<ConnectionsToyLogicalPathView>() != null &&
            prefab.GetComponent<ConnectionsToyPathGraphic>() != null, "authored path prefab");
        Check(prefab.GetComponentsInChildren<TMPro.TMP_Text>(true).Length == 0, "old path tags removed");
        var pillPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ConnectionsToy/ToyIdentityPill.prefab");
        Check(pillPrefab != null && pillPrefab.GetComponent<ConnectionsToyIdentityPillView>() != null &&
            pillPrefab.GetComponent<ConnectionsToyPillGraphic>() != null &&
            pillPrefab.GetComponentsInChildren<TMPro.TMP_Text>(true).Length == 1, "authored identity pill");
        Check(new SerializedObject(presenter).FindProperty("identityPresenter").objectReferenceValue == identity &&
            new SerializedObject(preview).FindProperty("identityPresenter").objectReferenceValue == identity &&
            new SerializedObject(UnityEngine.Object.FindFirstObjectByType<ConnectionsToyTerminal>())
                .FindProperty("identityPresenter").objectReferenceValue == identity, "identity scene bindings");
        var observedStages = new System.Collections.Generic.HashSet<ConnectionsToyIdentityPacking.ReadabilityStage>();
        foreach (ConnectionsToyPhysicalLinkView link in links)
            foreach (int count in new[] { 1, 2, 3, 4, 6, 8, 10, 12 })
            {
                var layout = identity.ResolveLayout(link, count, presenter.MaxLineWidth,
                    presenter.MinLineWidth, presenter.MinLineGap);
                CheckLayout(layout, link.BundleWidth(presenter.NetworkArea), count);
                observedStages.Add(layout.Stage);
                Debug.Log($"{link.LinkId} {count}: {layout.Stage}, {layout.LineWidth:0.##} px line, " +
                    $"{layout.LineGap:0.##} px gap, {layout.Schedule[0].Count} visible");
            }
        foreach (ConnectionsToyIdentityPacking.ReadabilityStage stage in new[] {
            ConnectionsToyIdentityPacking.ReadabilityStage.Centered,
            ConnectionsToyIdentityPacking.ReadabilityStage.Staggered,
            ConnectionsToyIdentityPacking.ReadabilityStage.Overlaid,
            ConnectionsToyIdentityPacking.ReadabilityStage.Rotating })
            Check(observedStages.Contains(stage), "all four readability stages are exercised");
        foreach (Vector2 direction in new[] { Vector2.up, Vector2.right,
            new Vector2(1f, 1f).normalized })
            foreach (int count in new[] { 2, 4, 8, 12 })
                CheckLayout(ConnectionsToyIdentityPacking.Solve(Vector2.zero,
                    direction * 280f, 96f, count, 12f, 2.5f, 3f, new Vector2(56f, 29f),
                    6f, 20f, 12f, 1f,
                    Array.Empty<ConnectionsToyIdentityPacking.PillBounds>()), 96f, count);
        var stableLink = links.First(l => l.LinkId == "E1");
        var stableLayout = identity.ResolveLayout(stableLink, 12,
            presenter.MaxLineWidth, presenter.MinLineWidth, presenter.MinLineGap);
        identity.CycleCongestedEdges();
        Check(ReferenceEquals(stableLayout, identity.ResolveLayout(stableLink, 12,
            presenter.MaxLineWidth, presenter.MinLineWidth, presenter.MinLineGap)),
            "manual cycle never replans line geometry");
        Check(ConnectionsToyIdentityPacking.Solve(Vector2.zero, Vector2.up * 70f,
            96f, 12, 12f, 2.5f, 3f, new Vector2(56f, 29f), 6f, 20f, 12f, 1f,
            Array.Empty<ConnectionsToyIdentityPacking.PillBounds>()).Stage ==
                ConnectionsToyIdentityPacking.ReadabilityStage.Unavailable,
            "short edge cannot fit an invalid pill");
        CheckLayout(ConnectionsToyIdentityPacking.Solve(Vector2.zero, Vector2.up * 110f,
            96f, 12, 12f, 2.5f, 3f, new Vector2(56f, 29f), 6f, 20f, 12f, 1f,
            Array.Empty<ConnectionsToyIdentityPacking.PillBounds>()), 96f, 12);

        host.ResetSimulation();
        var c1 = host.RuntimeConnections.First(c => c.ConnectionId == "c1");
        var c2 = host.RuntimeConnections.First(c => c.ConnectionId == "c2");
        Check(c1.IsActive && c2.IsPending, "pending excludes c2 from active lanes");
        int startSteps = 0;
        while (c2.IsPending && startSteps++ < 50) host.StepSimulation(0.1f);
        Check(c1.IsActive && c2.IsActive, "delayed establishment includes c2");
        float sharedRate = c1.ActualRateMBps;
        int steps = 0;
        while (!c2.IsComplete && steps++ < 1000) host.StepSimulation(0.1f);
        Check(c2.IsComplete && c1.IsActive, "c2 completion removes active lane");
        host.StepSimulation(0.1f);
        Check(c1.ActualRateMBps > sharedRate, "released capacity accelerates c1");
        host.ResetSimulation();
        Debug.Log("ConnectionsToyGeometryWitness PASS: scene references, edge budgets and stages, " +
            "rotated pill clearance and cycle coverage, short edges, establishment and completion.");
    }

    private static void CheckLayout(ConnectionsToyIdentityPacking.Layout layout, float budget, int count)
    {
        Check(layout != null && layout.Lanes.Length == count, "edge layout exists");
        Check(layout.Stage != ConnectionsToyIdentityPacking.ReadabilityStage.Unavailable,
            "edge supports at least one pill");
        Check(count * layout.LineWidth + (count - 1) * layout.LineGap <= budget + 0.02f,
            "pill-aware lines remain within attachment budget");
        Check(layout.Stage == ConnectionsToyIdentityPacking.ReadabilityStage.Rotating ||
            layout.Schedule.Count == 1 && layout.Schedule[0].Count == count,
            "all labels remain visible until rotation is necessary");
        if (layout.Stage == ConnectionsToyIdentityPacking.ReadabilityStage.Centered)
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
                float margin = (56f * 0.5f + 12f) / Vector2.Distance(lane.Start, lane.End);
                Check(placement.Fraction >= margin - 0.001f &&
                    placement.Fraction <= 1f - margin + 0.001f, "pill stays inside edge");
                for (int j = 0; j < i; j++)
                    Check(!ConnectionsToyIdentityPacking.Intersects(placement.Bounds,
                        cycle[j].Bounds, 6f), "no pill overlap");
                if (layout.Stage == ConnectionsToyIdentityPacking.ReadabilityStage.Centered ||
                    layout.Stage == ConnectionsToyIdentityPacking.ReadabilityStage.Staggered)
                    for (int other = 0; other < count; other++)
                        if (other != placement.LaneIndex)
                            Check(!ConnectionsToyIdentityPacking.SegmentIntersects(placement.Bounds,
                                layout.Lanes[other].Start, layout.Lanes[other].End,
                                layout.Lanes[other].Width * 0.5f + 1f),
                                "no unrelated line occlusion in stage A/B");
            }
        }
        Check(covered.Count == count, "cycling covers every connection");
        if (layout.Stage == ConnectionsToyIdentityPacking.ReadabilityStage.Rotating &&
            layout.Schedule[0].Count > 1)
            Check(layout.Schedule[0].Last().LaneIndex - layout.Schedule[0].First().LaneIndex >= count / 2,
                "distributed first cycle");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException("ConnectionsToyGeometryWitness failed: " + name);
    }
}
