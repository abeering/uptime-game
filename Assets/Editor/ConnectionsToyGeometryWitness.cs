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
        var host = UnityEngine.Object.FindFirstObjectByType<ConnectionsToySimulationHost>();
        var nodes = UnityEngine.Object.FindObjectsByType<ConnectionsToyNodeView>(FindObjectsSortMode.None);
        var links = UnityEngine.Object.FindObjectsByType<ConnectionsToyPhysicalLinkView>(FindObjectsSortMode.None);
        Check(presenter != null && preview != null && host != null, "scene components");
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
        Check(prefab.GetComponentsInChildren<TMPro.TMP_Text>(true).Length == 2, "independent ID tags");

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
        Debug.Log("ConnectionsToyGeometryWitness PASS: scene references, 1/2/3/4/8/16 lanes on three edges, " +
            "centering, gap, budget, density, threshold, prefab IDs, establishment and completion.");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException("ConnectionsToyGeometryWitness failed: " + name);
    }
}
