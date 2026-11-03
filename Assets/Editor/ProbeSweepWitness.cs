using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class ProbeSweepWitness
    {
        private const string Output = "Docs/Review/ConnectionsProbeSweepV0";
        private static Canvas captureCanvas;
        private static RenderMode previousRenderMode;
        private static Camera previousWorldCamera;
        private static Camera captureCamera;
        private static RenderTexture captureTarget;

        [MenuItem("Tools/Connections/Run Probe Sweep Witness")]
        public static void Run()
        {
            foreach (string scene in new[] { "Connections", "ConnectionsAdvanced" })
            {
                EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
                var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                var presenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
                var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
                Check(host != null && presenter != null && director != null,
                    scene + " Probe services");
                var prefab = new SerializedObject(presenter).FindProperty("probeRegionPrefab")
                    .objectReferenceValue as RectTransform;
                Check(prefab != null && prefab.GetComponent<ProbeRegionGraphic>() != null &&
                    AssetDatabase.GetAssetPath(prefab.GetComponent<ProbeRegionGraphic>().material) ==
                    "Assets/Shaders/ConnectionsLine.mat", scene + " shared authored graphic");
                AxisCases(prefab);
                bool auto = director.AutoGenerationEnabled;
                bool hostiles = director.HostileGenerationEnabled;
                try
                {
                    director.AutoGenerationEnabled = false;
                    director.HostileGenerationEnabled = false;
                    host.SetRunMode(ConnectionRunMode.Generated);
                    presenter.Bind(host.RuntimeConnections.ToArray());
                    BeginCapture(presenter);
                    Capture(scene, "inactive", presenter);
                    Check(Region(presenter) == null || !Region(presenter).gameObject.activeSelf,
                        "no idle scanner");

                    // Local Y is the cross-edge direction at every link angle.
                    bool capturedDiagonal = false;
                    foreach (PhysicalLinkReference physical in host.PhysicalLinks)
                    {
                        PhysicalLinkView link = physical.View;
                        presenter.SetProbe(link);
                        presenter.RefreshLayouts();
                        var region = Region(presenter);
                        var graphic = region.GetComponent<ProbeRegionGraphic>();
                        link.GetRenderedSpine(out Vector2 a, out Vector2 b);
                        Vector2 along = (b - a).normalized;
                        Vector2 across = new Vector2(-along.y, along.x);
                        Vector2 localUp = region.TransformVector(Vector3.up).normalized;
                        Check(region.gameObject.activeSelf && graphic.IsActiveVisual &&
                            Vector2.Dot(localUp, across) > .999f &&
                            Mathf.Abs(region.rect.width - Vector2.Distance(a, b) * .8f) < .5f,
                            scene + " sweep axis and scanner length on " + link.LinkId);
                        float start = graphic.SweepLocalY;
                        graphic.AdvanceVisual(.75f);
                        float moved = graphic.SweepLocalY;
                        Check(moved > start && moved > region.rect.yMin &&
                            moved < region.rect.yMax && MeshCount(graphic, out _) >= 28,
                            scene + " visible cross-edge sweep on " + link.LinkId);
                        if (!capturedDiagonal && Mathf.Abs(along.x) > .2f &&
                            Mathf.Abs(along.y) > .2f)
                        {
                            Capture(scene, "diagonal-" + link.LinkId, presenter);
                            capturedDiagonal = true;
                        }
                        presenter.SetProbe(null);
                        Check(!region.gameObject.activeSelf,
                            "manual visual clear leaves no stale scanner");
                    }

                    string id = host.PhysicalLinks[0].View.LinkId;
                    Check(host.TryActivateProbe(id, "probe " + id, out _),
                        scene + " Probe activation");
                    presenter.RefreshLayouts();
                    var activeRegion = Region(presenter);
                    var visual = activeRegion.GetComponent<ProbeRegionGraphic>();
                    Check(visual.IsActiveVisual && activeRegion.gameObject.activeSelf &&
                        MeshCount(visual, out _) == 24,
                        scene + " power-on flash and initially off-edge sweep");
                    Capture(scene, "activation", presenter);
                    visual.AdvanceVisual(.75f);
                    Check(MeshCount(visual, out _) >= 28,
                        scene + " glow and leading edge during active sweep");
                    Capture(scene, "active", presenter);
                    float firstSweepY = visual.SweepLocalY;
                    visual.AdvanceVisual(.55f);
                    Check(visual.SweepLocalY > firstSweepY,
                        scene + " sweep advances toward the opposite side");
                    Capture(scene, "active-later", presenter);
                    if (scene == "Connections")
                    {
                        QueueCrowd(host, director);
                        host.StepSimulation(1.3f);
                        presenter.Bind(host.RuntimeConnections.ToArray());
                        foreach (LogicalPathView path in presenter.PathLayer
                            .GetComponentsInChildren<LogicalPathView>(true))
                            path.AdvanceVisual(.4f);
                        presenter.RefreshLayouts();
                        Check(activeRegion.rect.height > 24f,
                            "crowded edge expands scanner without changing sweep alignment: " +
                            activeRegion.rect.height);
                        Capture(scene, "crowded", presenter);
                    }
                    float untilExpiry = 10f - host.SimulationTimeSeconds;
                    host.StepSimulation(untilExpiry);
                    int shutdownMesh = MeshCount(visual, out byte fullAlpha);
                    Check(host.ProbedLink == null && visual.IsShuttingDown &&
                        activeRegion.gameObject.activeSelf && shutdownMesh == 20,
                        scene + " gameplay expires while only the rectangle fades");
                    visual.AdvanceVisual(.11f);
                    int fadedMesh = MeshCount(visual, out byte fadedAlpha);
                    Check(visual.IsShuttingDown && fadedMesh == 20 &&
                        fadedAlpha < fullAlpha, scene + " shutdown opacity falls");
                    Capture(scene, "expiration", presenter);
                    visual.AdvanceVisual(.12f);
                    Check(!activeRegion.gameObject.activeSelf,
                        scene + " shutdown completes without stale graphic");
                    host.StepSimulation(25f);
                    Check(host.TryActivateProbe(id, "probe " + id, out _),
                        scene + " Probe remains reusable after cooldown");
                    host.ResetSimulation();
                    Check(host.ProbedLink == null && !activeRegion.gameObject.activeSelf,
                        scene + " reset hides active scanner immediately");
                    Debug.Log("ProbeSweepWitness PASS: " + scene +
                        " orientation, activation, cross-edge motion, expiry fade, reset");
                }
                finally
                {
                    EndCapture(presenter);
                    director.AutoGenerationEnabled = auto;
                    director.HostileGenerationEnabled = hostiles;
                    host.SetRunMode(ConnectionRunMode.Generated);
                }
            }
        }

        private static void QueueCrowd(SimulationHost host, ConnectionTrafficDirector director)
        {
            NodeView ext = host.Nodes.Single(n => n.View.NodeId == "EXT1").View;
            NodeView api = host.Nodes.Single(n => n.View.NodeId == "API").View;
            NodeView db = host.Nodes.Single(n => n.View.NodeId == "DB").View;
            for (int i = 0; i < 5; i++)
                director.QueueSpawnPlan(new ConnectionSpawnPlan(host.SimulationTimeSeconds,
                    "sweep-" + i, new[] { ext, api, db }, "sweep-witness", "Sweep witness",
                    new[] { new OperationStage(OperationKind.Write, 800f, 1f,
                        "Sweep witness") }, i == 0));
        }

        private static void AxisCases(RectTransform prefab)
        {
            RectTransform instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                instance.sizeDelta = new Vector2(180f, 40f);
                ProbeRegionGraphic graphic = instance.GetComponent<ProbeRegionGraphic>();
                foreach (float angle in new[] { 0f, 45f, 90f })
                {
                    instance.localRotation = Quaternion.Euler(0f, 0f, angle);
                    graphic.BeginActivation();
                    Vector2 along = instance.TransformVector(Vector3.right).normalized;
                    Vector2 across = new Vector2(-along.y, along.x);
                    Vector2 before = instance.TransformPoint(new Vector2(0f, graphic.SweepLocalY));
                    graphic.AdvanceVisual(.5f);
                    Vector2 after = instance.TransformPoint(new Vector2(0f, graphic.SweepLocalY));
                    Vector2 motion = after - before;
                    Check(Vector2.Dot(motion.normalized, across) > .999f &&
                        Mathf.Abs(Vector2.Dot(motion, along)) < .001f &&
                        MeshCount(graphic, out _) >= 28,
                        "synthetic " + angle + " degree sweep stays perpendicular");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(instance.gameObject); }
        }

        private static RectTransform Region(LogicalPathPresenter presenter) =>
            (RectTransform)typeof(LogicalPathPresenter).GetField("probeRegion",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(presenter);

        private static int MeshCount(ProbeRegionGraphic graphic, out byte fillAlpha)
        {
            using (var mesh = new VertexHelper())
            {
                typeof(ProbeRegionGraphic).GetMethod("OnPopulateMesh",
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(VertexHelper) }, null)
                    .Invoke(graphic, new object[] { mesh });
                UIVertex first = default;
                if (mesh.currentVertCount > 0) mesh.PopulateUIVertex(ref first, 0);
                fillAlpha = first.color.a;
                return mesh.currentVertCount;
            }
        }

        private static void BeginCapture(LogicalPathPresenter presenter)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            captureCanvas = presenter.GetComponentInParent<Canvas>();
            previousRenderMode = captureCanvas.renderMode;
            previousWorldCamera = captureCanvas.worldCamera;
            captureCamera = new GameObject("Probe sweep capture camera", typeof(Camera))
                .GetComponent<Camera>();
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = new Color(.025f, .065f, .08f);
            captureCamera.orthographic = true;
            captureCamera.transform.position = new Vector3(0, 0, -100);
            captureCamera.nearClipPlane = .1f;
            captureCamera.farClipPlane = 1000f;
            captureTarget = new RenderTexture(1920, 1080, 24);
            captureCamera.targetTexture = captureTarget;
            captureCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            captureCanvas.worldCamera = captureCamera;
            captureCanvas.planeDistance = 10f;
            Canvas.ForceUpdateCanvases();
            RefreshCaptureGeometry(presenter);
        }

        private static void EndCapture(LogicalPathPresenter presenter)
        {
            if (captureCanvas != null)
            {
                captureCanvas.renderMode = previousRenderMode;
                captureCanvas.worldCamera = previousWorldCamera;
                Canvas.ForceUpdateCanvases();
                RefreshCaptureGeometry(presenter);
            }
            if (captureCamera != null) UnityEngine.Object.DestroyImmediate(captureCamera.gameObject);
            if (captureTarget != null) UnityEngine.Object.DestroyImmediate(captureTarget);
            captureCanvas = null;
            captureCamera = null;
            captureTarget = null;
        }

        private static void Capture(string scene, string phase, LogicalPathPresenter presenter)
        {
            if (captureCamera == null) return;
            RefreshCaptureGeometry(presenter);
            captureCamera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = captureTarget;
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                try
                {
                    image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                    image.Apply();
                    Directory.CreateDirectory(Output);
                    File.WriteAllBytes(Path.Combine(Output, scene + "-" + phase +
                        "-1920x1080.png"), image.EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(image); }
            }
            finally { RenderTexture.active = previous; }
        }

        private static void RefreshCaptureGeometry(LogicalPathPresenter presenter)
        {
            foreach (PhysicalLinkView link in UnityEngine.Object.FindObjectsByType<PhysicalLinkView>(
                FindObjectsSortMode.None))
                link.AdvanceVisual(0f);
            presenter.RefreshLayouts();
            Canvas.ForceUpdateCanvases();
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("ProbeSweepWitness FAILED: " + message);
        }
    }
}
