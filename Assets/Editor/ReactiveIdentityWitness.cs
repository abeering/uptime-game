using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class ReactiveIdentityWitness
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string CaptureOutput = "Docs/Review/ConnectionsReactiveIdentityV0";
        private static Canvas captureCanvas;
        private static RenderMode previousMode;
        private static Camera previousCamera;
        private static Camera camera;

        [MenuItem("Tools/Connections/Run Reactive Identity Witness")]
        public static void Run()
        {
            foreach (string scene in new[] { "Connections", "ConnectionsAdvanced" })
                CheckScene(scene);
            Debug.Log("ReactiveIdentityWitness PASS: commands, collision, Probe, UI, " +
                "recorder, reset and replay in both Connections scenes.");
        }

        private static void CheckScene(string scene)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
            var identities = UnityEngine.Object.FindFirstObjectByType<IdentityPresenter>();
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && traffic != null && presenter != null && identities != null &&
                terminal != null && recorder != null, scene + " services");
            Check(Terminal.TryParseSpawn(new[] { "spawn", "basic-threat", "idreact" },
                out string profile, out _, out bool hopping, out bool reactive, out _) &&
                profile == "basic-threat" && reactive && !hopping,
                scene + " reactive spawn syntax");
            Check(!Terminal.TryParseSpawn(new[] { "spawn", "basic-threat", "idhop", "idreact" },
                out _, out _, out _, out _, out string conflict) &&
                conflict.Contains("cannot be combined"), scene + " conflicting syntax");

            var exhausted = new ConnectionIdentityRuntime("r1", 0f, 91,
                ConnectionIdentityMode.ReactiveMisdirection);
            Check(!exhausted.Start(0, _ => false) && exhausted.KnownIdentity == "r1" &&
                !exhausted.ReactToFailedAddress("r1", _ => false),
                scene + " exhausted alias pool exposes no collision");

            bool oldAuto = traffic.AutoGenerationEnabled;
            bool oldHostile = traffic.HostileGenerationEnabled;
            bool oldRecording = recorder.RecordingEnabled;
            GameObject panels = null;
            try
            {
                traffic.AutoGenerationEnabled = false;
                traffic.HostileGenerationEnabled = false;
                recorder.RecordingEnabled = true;
                host.SetRunMode(ConnectionRunMode.Generated);
                recorder.BeginRun();

                var seedProbe = new ConnectionIdentityRuntime("r1", 0f, 91,
                    ConnectionIdentityMode.ReactiveMisdirection);
                seedProbe.Start(0, _ => true);
                string occupied = seedProbe.AliasIdentity;
                Queue(host, traffic, occupied, ConnectionIdentityMode.Normal);
                Queue(host, traffic, "r1", ConnectionIdentityMode.ReactiveMisdirection);
                Queue(host, traffic, "r2", ConnectionIdentityMode.ReactiveMisdirection);
                Queue(host, traffic, "h1", ConnectionIdentityMode.ContinuousHopping);
                host.StepSimulation(.01f);
                LogicalConnectionState first = host.FindConnection("r1");
                LogicalConnectionState second = host.FindConnection("r2");
                LogicalConnectionState hopper = host.FindConnection("h1");
                string initial = first.KnownIdentity;
                string secondInitial = second.KnownIdentity;
                Check(initial != "r1" && initial != occupied && initial != secondInitial &&
                    secondInitial != "r2" && hopper.Identity.IsHopping &&
                    host.FindConnection(initial) == null && !host.IsLiveAlias(occupied),
                    scene + " immediate false aliases avoid canonical and live IDs");

                host.StepSimulation(2.5f);
                Check(first.KnownIdentity == initial && second.KnownIdentity == secondInitial &&
                    hopper.Identity.HopCount >= 2, scene + " reactive labels stay stable; hopping continues");
                presenter.Bind(host.RuntimeConnections.ToArray());
                foreach (LogicalPathView view in presenter.PathLayer
                    .GetComponentsInChildren<LogicalPathView>(true))
                    view.AdvanceVisual(.4f);
                presenter.RefreshLayouts();
                BeginCapture(presenter);
                Capture(scene, "stable", presenter);
                IdentityPillView[] pills = Pills(identities, "r1");
                Check(pills.Length >= 2 && pills.All(p => p.DisplayedId == initial),
                    scene + " alias appears on route pills");

                panels = new GameObject("Reactive identity witness panels", typeof(RectTransform));
                ((RectTransform)panels.transform).sizeDelta = new Vector2(500f, 900f);
                var load = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<LoadOutput>(
                    "Assets/Prefabs/Connections/LoadOutput.prefab"), panels.transform);
                var work = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WorkOutput>(
                    "Assets/Prefabs/Connections/WorkOutput.prefab"), panels.transform);
                load.Bind("DB", host);
                work.Bind("DB", host);
                LoadRequestRow loadRow = load.GetComponentsInChildren<LoadRequestRow>(true)
                    .Single(row => row.DisplayedId == initial);
                WorkRequestRow workRow = work.GetComponentsInChildren<WorkRequestRow>(true)
                    .Single(row => row.DisplayedId == initial);
                int loadIndex = loadRow.transform.GetSiblingIndex();
                int workIndex = workRow.transform.GetSiblingIndex();
                float workBefore = first.CompletedWork;

                Submit(terminal, "kill " + initial.ToUpperInvariant());
                string replacement = first.KnownIdentity;
                presenter.RefreshLayouts(); Refresh(load); Refresh(work);
                Check(!first.IsComplete && replacement != initial &&
                    second.KnownIdentity == secondInitial && first.CompletedWork == workBefore &&
                    pills.All(p => p.DisplayedId == replacement) &&
                    Pills(identities, "r1").SequenceEqual(pills) &&
                    loadRow.DisplayedId == replacement && workRow.DisplayedId == replacement &&
                    loadRow.transform.GetSiblingIndex() == loadIndex &&
                    workRow.transform.GetSiblingIndex() == workIndex,
                    scene + " failed kill reacts immediately without rebuilding UI or changing WORK");
                Capture(scene, "reacted", presenter);
                Submit(terminal, "inspect " + initial);
                Submit(terminal, "kill zzzz");
                Check(first.KnownIdentity == replacement, scene + " stale and unknown IDs have no effect");
                int inspected = terminal.GetComponentsInChildren<InspectOutput>(true).Length;
                Submit(terminal, "inspect " + replacement);
                string third = first.KnownIdentity;
                Check(third != replacement && !first.IsComplete &&
                    terminal.GetComponentsInChildren<InspectOutput>(true).Length == inspected,
                    scene + " failed inspect reacts without producing a card");
                Submit(terminal, "kill " + secondInitial);
                string secondReplacement = second.KnownIdentity;
                Check(secondReplacement != secondInitial && first.KnownIdentity == third &&
                    secondReplacement != third,
                    scene + " each reactive connection changes independently");
                Submit(terminal, "inspect r1");
                Check(terminal.GetComponentsInChildren<InspectOutput>(true).Length == inspected + 1,
                    scene + " canonical inspect succeeds");

                var edge = host.PhysicalLinks.Select(link => link.View).Single(link =>
                    link.NodeA.NodeId == "DB" && link.NodeB.NodeId == "API" ||
                    link.NodeA.NodeId == "API" && link.NodeB.NodeId == "DB");
                Check(host.TryActivateProbe(edge.LinkId, "probe " + edge.LinkId, out _) &&
                    first.KnownIdentity == "r1", scene + " Probe reveals canonical ID");
                Capture(scene, "revealed", presenter);
                Submit(terminal, "kill " + third);
                Check(first.KnownIdentity == "r1" && first.Identity.AliasIdentity == third,
                    scene + " concealed alias does not react");
                host.StepSimulation(host.ProbeActiveRemaining);
                Check(first.KnownIdentity == third && !first.Identity.IsRevealed,
                    scene + " same hidden alias returns at Probe expiry");
                Submit(terminal, "kill " + initial + " r1");
                Check(first.IsComplete && second.KnownIdentity == secondReplacement,
                    scene + " multi-kill still processes true ID after a stale failure");

                var changes = recorder.CurrentLog.events.Where(e =>
                    e.type == "identity_alias_changed" && e.connectionId == "r1" &&
                    e.attemptedIdentity != null).ToArray();
                Check(changes.Length == 2 && changes[0].previousIdentity == initial &&
                    changes[0].advertisedIdentity == replacement &&
                    changes[0].command == "kill" && changes[0].actionSequence > 0 &&
                    changes[1].previousIdentity == replacement &&
                    changes[1].advertisedIdentity == third && changes[1].command == "inspect" &&
                    changes[1].actionSequence > 0 &&
                    recorder.CurrentLog.events.Any(e => e.type == "plan_queued" &&
                        e.connectionId == "r1" &&
                        e.definition.identityMode == "ReactiveMisdirection" &&
                        e.definition.identitySeed == 91),
                    scene + " recorder attributes both failed actions and concrete identity mode");

                host.ResetSimulation();
                Queue(host, traffic, occupied, ConnectionIdentityMode.Normal);
                Queue(host, traffic, "r1", ConnectionIdentityMode.ReactiveMisdirection);
                Queue(host, traffic, "r2", ConnectionIdentityMode.ReactiveMisdirection);
                Queue(host, traffic, "h1", ConnectionIdentityMode.ContinuousHopping);
                host.StepSimulation(.01f);
                Check(host.FindConnection("r1").KnownIdentity == initial &&
                    host.FindConnection("r2").KnownIdentity == secondInitial,
                    scene + " reset and seeded replay");
                ConnectionSpawnPlan manual = traffic.QueueProfile("basic-threat", idReactive: true);
                Check(manual.IsHostile && manual.IdentityMode ==
                    ConnectionIdentityMode.ReactiveMisdirection && manual.IdentitySeed != 0 &&
                    traffic.EffectiveAmbientWeight("basic-threat") == 0f,
                    scene + " manual fixed hostile profile only");
                ConnectionSpawnPlan ambient = traffic.GenerateHostilePlan(host.SimulationTimeSeconds);
                Check(ambient == null || ambient.IdentityMode == ConnectionIdentityMode.Normal,
                    scene + " ambient identity behavior remains normal");
                Debug.Log("ReactiveIdentityWitness PASS: " + scene);
            }
            finally
            {
                EndCapture(presenter);
                if (panels != null) UnityEngine.Object.DestroyImmediate(panels);
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                traffic.AutoGenerationEnabled = oldAuto;
                traffic.HostileGenerationEnabled = oldHostile;
            }
        }

        private static void Queue(SimulationHost host, ConnectionTrafficDirector traffic,
            string id, ConnectionIdentityMode mode)
        {
            NodeView Node(string name) => host.Nodes.Single(n => n.View.NodeId == name).View;
            NodeView[] route = host.Nodes.Any(n => n.View.NodeId == "WEB") ?
                new[] { Node("EXT1"), Node("WEB"), Node("API"), Node("DB") } :
                new[] { Node("EXT1"), Node("API"), Node("DB") };
            traffic.QueueSpawnPlan(new ConnectionSpawnPlan(host.SimulationTimeSeconds, id,
                route, "reactive-identity-witness", "Database Maintenance",
                new[] { new OperationStage(OperationKind.Write, 1000f, 1f, "Long-running witness") },
                mode != ConnectionIdentityMode.Normal,
                identityHopSeconds: mode == ConnectionIdentityMode.ContinuousHopping ? 2f : 0f,
                identitySeed: 91, identityMode: mode));
        }

        private static IdentityPillView[] Pills(IdentityPresenter presenter, string trueId)
        {
            RectTransform layer = (RectTransform)typeof(IdentityPresenter)
                .GetField("pillLayer", Private).GetValue(presenter);
            return layer.GetComponentsInChildren<IdentityPillView>(true)
                .Where(p => p.name.EndsWith(" identity " + trueId, StringComparison.Ordinal))
                .OrderBy(p => p.name).ToArray();
        }

        private static void Submit(Terminal terminal, string command) =>
            typeof(Terminal).GetMethod("HandleSubmit", Private)
                .Invoke(terminal, new object[] { command });

        private static void Refresh(object panel) => panel.GetType()
            .GetMethod("Refresh", Private).Invoke(panel, null);

        private static void BeginCapture(LogicalPathPresenter presenter)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            captureCanvas = presenter.GetComponentInParent<Canvas>();
            previousMode = captureCanvas.renderMode;
            previousCamera = captureCanvas.worldCamera;
            camera = new GameObject("Reactive identity capture camera", typeof(Camera))
                .GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .065f, .08f);
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -100);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000f;
            captureCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            captureCanvas.worldCamera = camera;
            captureCanvas.planeDistance = 10f;
            RefreshCaptureGeometry(presenter);
        }

        private static void EndCapture(LogicalPathPresenter presenter)
        {
            if (captureCanvas != null)
            {
                captureCanvas.renderMode = previousMode;
                captureCanvas.worldCamera = previousCamera;
                RefreshCaptureGeometry(presenter);
            }
            if (camera != null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
            captureCanvas = null;
            camera = null;
        }

        private static void Capture(string scene, string phase,
            LogicalPathPresenter presenter)
        {
            if (camera == null) return;
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720),
                new Vector2Int(1920, 1080) })
            {
                var target = new RenderTexture(size.x, size.y, 24);
                camera.targetTexture = target;
                try
                {
                    RefreshCaptureGeometry(presenter);
                    camera.Render();
                    RenderTexture previous = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = target;
                        var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                        try
                        {
                            image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                            image.Apply();
                            Directory.CreateDirectory(CaptureOutput);
                            File.WriteAllBytes(Path.Combine(CaptureOutput,
                                scene + "-" + phase + "-" + size.x + "x" + size.y + ".png"),
                                image.EncodeToPNG());
                        }
                        finally { UnityEngine.Object.DestroyImmediate(image); }
                    }
                    finally { RenderTexture.active = previous; }
                }
                finally
                {
                    camera.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }

        private static void RefreshCaptureGeometry(LogicalPathPresenter presenter)
        {
            foreach (PhysicalLinkView link in UnityEngine.Object.FindObjectsByType<PhysicalLinkView>(
                FindObjectsSortMode.None)) link.AdvanceVisual(0f);
            presenter.RefreshLayouts();
            Canvas.ForceUpdateCanvases();
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Reactive Identity Witness: " + message);
        }
    }
}
