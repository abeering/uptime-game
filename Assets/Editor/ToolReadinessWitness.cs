using System;
using System.IO;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class ToolReadinessWitness
    {
        private const string Output = "Docs/Review/ConnectionsToolReadinessV0";

        [MenuItem("Tools/Connections/Run Tool Readiness Witness")]
        public static void Run()
        {
            foreach (string name in new[] { "Connections", "ConnectionsAdvanced" })
            {
                EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                var panel = UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>();
                var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
                Check(host != null && panel != null && director != null, name + " services");
                Check(new SerializedObject(panel).FindProperty("host").objectReferenceValue == host,
                    name + " panel host binding");
                var root = (RectTransform)panel.transform;
                var row = root.Find("ToolReadinessRow") as RectTransform;
                Check(row != null && Mathf.Abs(root.rect.height - 52f) < .01f &&
                    Mathf.Abs(row.anchoredPosition.x + 24f) < .01f,
                    name + " permanent 52-unit right-aligned strip");
                Check(row.GetComponent<HorizontalLayoutGroup>() != null &&
                    row.Find("ProbeGroup/Slots")?.GetComponent<HorizontalLayoutGroup>() != null &&
                    row.Find("ScanGroup/Slots")?.GetComponent<HorizontalLayoutGroup>() != null,
                    name + " expandable horizontal slot containers");
                Check(row.GetComponent<ContentSizeFitter>()?.horizontalFit ==
                    ContentSizeFitter.FitMode.PreferredSize &&
                    row.Find("ProbeGroup")?.GetComponent<ContentSizeFitter>()?.horizontalFit ==
                    ContentSizeFitter.FitMode.PreferredSize &&
                    row.Find("ScanGroup")?.GetComponent<ContentSizeFitter>()?.horizontalFit ==
                    ContentSizeFitter.FitMode.PreferredSize &&
                    row.Find("ProbeGroup/Slots")?.GetComponent<ContentSizeFitter>()?.horizontalFit ==
                    ContentSizeFitter.FitMode.PreferredSize &&
                    row.Find("ScanGroup/Slots")?.GetComponent<ContentSizeFitter>()?.horizontalFit ==
                    ContentSizeFitter.FitMode.PreferredSize,
                    name + " groups grow left as future slots are authored");
                Check(row.Find("ProbeGroup/Slots").childCount == 1 &&
                    row.Find("ScanGroup/Slots").childCount == 1,
                    name + " exactly one slot per tool");
                CheckExpandableSlots(row);
                Check(root.Find("Clock") != null && root.Find("HealthValue") != null &&
                    root.Find("HealthBar") != null && root.Find("SignedRate") != null,
                    name + " original Health elements retained");
                var fields = new SerializedObject(panel);
                var probeFill = (Image)fields.FindProperty("probeStatusFill").objectReferenceValue;
                var scanFill = (Image)fields.FindProperty("scanStatusFill").objectReferenceValue;
                var probeText = (TMP_Text)fields.FindProperty("probeStatusText").objectReferenceValue;
                var scanText = (TMP_Text)fields.FindProperty("scanStatusText").objectReferenceValue;
                Check(probeFill != null && scanFill != null && probeText != null &&
                    scanText != null && !probeFill.raycastTarget && !scanFill.raycastTarget &&
                    !probeText.raycastTarget && !scanText.raycastTarget &&
                    Mathf.Abs(probeFill.rectTransform.rect.width - 130f) < .01f &&
                    Mathf.Abs(scanFill.rectTransform.rect.width - 130f) < .01f,
                    name + " passive fixed-width authored indicators");

                bool previousAuto = director.AutoGenerationEnabled;
                bool previousHostiles = director.HostileGenerationEnabled;
                try
                {
                    director.AutoGenerationEnabled = false;
                    director.HostileGenerationEnabled = false;
                    host.SetRunMode(ConnectionRunMode.Generated);
                    panel.Render();
                    Status(probeText, probeFill, "READY", new Color(.14f, .33f, .27f),
                        "initial Probe");
                    Status(scanText, scanFill, "READY", new Color(.14f, .33f, .27f),
                        "initial Scan");
                    Capture(name, "ready", panel);

                    string linkId = host.PhysicalLinks[0].View.LinkId;
                    string nodeId = host.Nodes[0].View.NodeId;
                    Check(host.TryActivateProbe(linkId, "probe " + linkId, out _),
                        "Probe activation");
                    Check(host.StartScan(nodeId, "scan " + nodeId, out _) ==
                        ScanStartResult.Started, "Scan activation");
                    panel.Render();
                    Status(probeText, probeFill, "ACTIVE 10s", new Color(.13f, .31f, .36f),
                        "simultaneous Probe");
                    Status(scanText, scanFill, "ACTIVE 8s", new Color(.13f, .31f, .36f),
                        "simultaneous Scan");
                    Capture(name, "active", panel);

                    host.StepSimulation(.2f);
                    panel.Render();
                    Check(probeText.text == "ACTIVE 10s" && scanText.text == "ACTIVE 8s",
                        "fractional remaining seconds round up");
                    panel.Render();
                    Check(probeText.text == "ACTIVE 10s" && scanText.text == "ACTIVE 8s",
                        "render without simulation progress keeps timers still");
                    host.StepSimulation(1f);
                    panel.Render();
                    Check(probeText.text == "ACTIVE 9s" && scanText.text == "ACTIVE 7s",
                        "countdowns advance only with simulation time");
                    host.StepSimulation(6.8f);
                    panel.Render();
                    Check(host.ActiveScan == null && scanText.text == "READY" &&
                        probeText.text == "ACTIVE 2s", "Scan completes immediately to ready");
                    host.StepSimulation(2f);
                    panel.Render();
                    Status(probeText, probeFill, "25s", new Color(.34f, .28f, .13f),
                        "Probe expires into activation-origin cooldown");
                    Check(scanText.text == "READY", "Scan has no cooldown");
                    Capture(name, "cooldown", panel);
                    host.StepSimulation(24.1f);
                    panel.Render();
                    Check(probeText.text == "1s", "fractional cooldown stays cooling");
                    host.StepSimulation(.9f);
                    panel.Render();
                    Check(probeText.text == "READY" &&
                        host.TryActivateProbe(linkId, "probe " + linkId, out _),
                        "Probe ready at actual cooldown deadline");
                    Check(host.StartScan(nodeId, "scan " + nodeId, out _) ==
                        ScanStartResult.Started, "Scan slot reusable");
                    host.ResetSimulation();
                    panel.Render();
                    Check(probeText.text == "READY" && scanText.text == "READY",
                        "reset immediately clears both displayed states");
                    Debug.Log("ToolReadinessWitness PASS: " + name +
                        " bindings, layout, simulation countdowns, concurrent use and reset");
                }
                finally
                {
                    director.AutoGenerationEnabled = previousAuto;
                    director.HostileGenerationEnabled = previousHostiles;
                    host.SetRunMode(ConnectionRunMode.Generated);
                }
            }
        }

        private static void Status(TMP_Text text, Image fill, string expected,
            Color expectedColor, string label)
        {
            Check(text.text == expected && (fill.color - expectedColor).maxColorComponent < .001f &&
                (expectedColor - fill.color).maxColorComponent < .001f, label);
        }

        private static void CheckExpandableSlots(RectTransform row)
        {
            Canvas.ForceUpdateCanvases();
            float originalWidth = row.rect.width;
            var slots = (RectTransform)row.Find("ProbeGroup/Slots");
            GameObject second = UnityEngine.Object.Instantiate(slots.GetChild(0).gameObject, slots);
            try
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(slots);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)slots.parent);
                LayoutRebuilder.ForceRebuildLayoutImmediate(row);
                Canvas.ForceUpdateCanvases();
                Check(row.rect.width >= originalWidth + 130f,
                    "authoring another slot grows the row instead of overlapping Scan: " +
                    originalWidth + " -> " + row.rect.width + ", slots " + slots.rect.width);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(second);
                Canvas.ForceUpdateCanvases();
            }
        }

        private static void Capture(string scene, string phase, NetworkHealthPanel panel)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                var canvas = panel.GetComponentInParent<Canvas>();
                var priorMode = canvas.renderMode;
                var priorCamera = canvas.worldCamera;
                Camera camera = null;
                RenderTexture target = null;
                try
                {
                    camera = new GameObject("Tool readiness capture camera", typeof(Camera))
                        .GetComponent<Camera>();
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(.025f, .065f, .08f);
                    camera.orthographic = true;
                    camera.transform.position = new Vector3(0, 0, -100);
                    camera.nearClipPlane = .1f;
                    camera.farClipPlane = 1000f;
                    target = new RenderTexture(size.x, size.y, 24);
                    camera.targetTexture = target;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 10f;
                    Canvas.ForceUpdateCanvases();
                    panel.Render();
                    camera.Render();
                    var previous = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = target;
                        var png = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                        try
                        {
                            png.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                            png.Apply();
                            Directory.CreateDirectory(Output);
                            File.WriteAllBytes(Path.Combine(Output,
                                scene + "-" + phase + "-" + size.x + "x" + size.y + ".png"),
                                png.EncodeToPNG());
                        }
                        finally { UnityEngine.Object.DestroyImmediate(png); }
                    }
                    finally { RenderTexture.active = previous; }
                }
                finally
                {
                    canvas.renderMode = priorMode;
                    canvas.worldCamera = priorCamera;
                    if (camera != null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
                    if (target != null) UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("ToolReadinessWitness FAILED: " + message);
        }
    }
}
