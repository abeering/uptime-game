using System;
using System.IO;
using System.Linq;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    // Deterministic Editor previews using the scene simulation and authored inspect prefab.
    public static class InspectV2Capture
    {
        private const string Output = "Docs/Review/ConnectionsTerminalInspectCleanup";

        [MenuItem("Tools/Connections/Capture Terminal Inspect Cleanup Review")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            SimulationHost host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            ConnectionTrafficDirector director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            InspectOutput prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>(
                "Assets/Prefabs/Connections/InspectOutput.prefab");
            if (host == null || director == null || prefab == null)
                throw new InvalidOperationException("Inspect V2 capture scene or prefab unavailable");

            bool oldAuto = director.AutoGenerationEnabled;
            Camera camera = null;
            Canvas canvas = null;
            RenderTexture target = null;
            try
            {
                director.AutoGenerationEnabled = false;
                host.SetRunMode(ConnectionRunMode.Generated);
                NodeView[] route = { Node(host, "EXT1"), Node(host, "API"), Node(host, "DB") };
                director.QueueSpawnPlan(Plan("a1", route, new[]
                {
                    Stage(OperationKind.Read, 120f, 2f)
                }));
                director.QueueSpawnPlan(Plan("b2", route, new[]
                {
                    Stage(OperationKind.Write, 120f, 3f)
                }));
                director.QueueSpawnPlan(Plan("c3", route, new[]
                {
                    Stage(OperationKind.Execute, 120f, 2f)
                }));
                director.QueueSpawnPlan(Plan("d4", route, new[]
                {
                    Stage(OperationKind.Read, 2f, 2f),
                    Stage(OperationKind.Write, 2f, 2f),
                    Stage(OperationKind.Execute, 2f, 2f),
                    Stage(OperationKind.Read, 120f, 2f),
                    Stage(OperationKind.Write, 2f, 2f),
                    Stage(OperationKind.Execute, 2f, 2f)
                }));
                for (int i = 0; i < 4; i++) host.StepSimulation(1f);
                director.QueueSpawnPlan(Plan("e5", route, new[]
                {
                    Stage(OperationKind.Read, 2f, 2f)
                }));
                host.StepSimulation(1f);
                host.StepSimulation(1f);
                if (!host.FindConnection("e5").IsComplete)
                    throw new InvalidOperationException("Editor preview completed connection did not resolve");

                GameObject cameraObject = new GameObject("Inspect V2 capture camera", typeof(Camera));
                camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.028f, 0.075f, 0.09f);
                camera.cullingMask = 1 << 5;
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                canvas = new GameObject("Inspect V2 capture canvas", typeof(RectTransform),
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
                canvas.gameObject.layer = 5;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
                canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                RectTransform root = (RectTransform)canvas.transform;
                root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1920f);
                root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1080f);

                RectTransform terminal = Panel(root, "Terminal column", 650f, 1080f,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero,
                    new Color(0.035f, 0.09f, 0.105f));
                Panel(terminal, "Terminal heading", 650f, 62f,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero,
                    new Color(0.06f, 0.15f, 0.18f));
                Heading(terminal, "CONNECTIONS // TERMINAL", new Vector2(25f, -16f), 24);
                Heading(terminal, "TERMINAL INSPECT  /  EDITOR PREVIEW", new Vector2(25f, -86f), 17);

                Directory.CreateDirectory(Output);
                Capture(camera, target, terminal, prefab, "inspect a1",
                    new[] { host.FindConnection("a1") }, InspectPresentationMode.Compact,
                    "compact-single-1920x1080.png");
                Capture(camera, target, terminal, prefab, "inspect -v a1",
                    new[] { host.FindConnection("a1") }, InspectPresentationMode.Verbose,
                    "verbose-single-1920x1080.png");
                Capture(camera, target, terminal, prefab, "inspect d4",
                    new[] { host.FindConnection("d4") }, InspectPresentationMode.Compact,
                    "compact-long-1920x1080.png");
                Capture(camera, target, terminal, prefab, "inspect -v d4",
                    new[] { host.FindConnection("d4") }, InspectPresentationMode.Verbose,
                    "verbose-long-1920x1080.png");
                Capture(camera, target, terminal, prefab, "inspect a1 b2 c3 d4 e5",
                    new[] { host.FindConnection("a1"), host.FindConnection("b2"),
                        host.FindConnection("c3"), host.FindConnection("d4"),
                        host.FindConnection("e5") },
                    InspectPresentationMode.Compact, "compact-multiple-completed-1920x1080.png");
                Capture(camera, target, terminal, prefab, "inspect -v a1 e5",
                    new[] { host.FindConnection("a1"), host.FindConnection("e5") },
                    InspectPresentationMode.Verbose, "verbose-multiple-completed-1920x1080.png");
                Debug.Log("InspectV2Capture PASS: six 1920x1080 Editor previews in " + Output);
            }
            finally
            {
                director.AutoGenerationEnabled = oldAuto;
                host.SetRunMode(ConnectionRunMode.Generated);
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                if (camera != null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
                if (target != null) UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void Capture(Camera camera, RenderTexture target, RectTransform terminal,
            InspectOutput prefab, string command, LogicalConnectionState[] states,
            InspectPresentationMode mode, string filename)
        {
            GameObject group = new GameObject("Review content", typeof(RectTransform));
            group.layer = 5;
            RectTransform content = (RectTransform)group.transform;
            content.SetParent(terminal, false);
            content.anchorMin = content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = new Vector2(24f, -132f);
            content.sizeDelta = new Vector2(602f, 800f);
            try
            {
                Heading(content, "> " + command, new Vector2(0f, 0f), 18);
                InspectOutput output = PrefabUtility.InstantiatePrefab(prefab, content) as InspectOutput;
                if (output == null) throw new InvalidOperationException("Could not instantiate inspect prefab");
                RectTransform rect = (RectTransform)output.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0f, -40f);
                output.Bind(states, presentationMode: mode);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(Output, filename), image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
                RenderTexture.active = previous;
            }
            finally { UnityEngine.Object.DestroyImmediate(group); }
        }

        private static RectTransform Panel(RectTransform parent, string name, float width, float height,
            Vector2 anchor, Vector2 pivot, Vector2 position, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.layer = 5;
            RectTransform rect = (RectTransform)panel.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, height);
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private static void Heading(RectTransform parent, string content, Vector2 position, float size)
        {
            GameObject gameObject = new GameObject("Caption", typeof(RectTransform),
                typeof(TextMeshProUGUI));
            gameObject.layer = 5;
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(570f, 34f);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = new Color(0.7f, 0.9f, 0.83f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static ConnectionSpawnPlan Plan(string id, NodeView[] route, OperationStage[] stages) =>
            new ConnectionSpawnPlan(0f, id, route, "inspect-v2-review", "Records", stages);
        private static OperationStage Stage(OperationKind kind, float work, float load) =>
            new OperationStage(kind, work, load, kind.ToString());
        private static NodeView Node(SimulationHost host, string id) =>
            host.Nodes.Single(n => n.View.NodeId == id).View;
    }
}
