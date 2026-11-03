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
    public static class IdHoppingWitness
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string CaptureOutput = "Docs/Review/ConnectionsIdHoppingV0";
        private static Canvas captureCanvas;
        private static RenderMode previousMode;
        private static Camera previousCamera;
        private static Camera camera;
        private static RenderTexture target;

        [MenuItem("Tools/Connections/Run ID Hopping Witness")]
        public static void Run()
        {
            foreach (string scene in new[] { "Connections", "ConnectionsAdvanced" })
                CheckScene(scene);
            Debug.Log("IdHoppingWitness PASS: aliases, collisions, commands, Probe, " +
                "pills, live panels, reset and replay in both Connections scenes.");
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
            Check(Mathf.Approximately(traffic.IdentityHopIntervalSeconds, 2f),
                scene + " serialized two-second interval");
            Check(Terminal.TryParseSpawn(new[] { "spawn", "basic-threat", "idhop" },
                out string profile, out _, out bool hopping, out _) &&
                profile == "basic-threat" && hopping, scene + " development syntax");
            var exhausted = new ConnectionIdentityRuntime("h1", 2f, 91);
            Check(!exhausted.Start(0d, _ => false) && exhausted.KnownIdentity == "h1" &&
                exhausted.HopCount == 0, scene + " exhausted alias pool exposes no collision");
            bool oldAuto = traffic.AutoGenerationEnabled;
            bool oldHostile = traffic.HostileGenerationEnabled;
            bool oldRecording = recorder.RecordingEnabled;
            GameObject panelRoot = null;
            try
            {
                traffic.AutoGenerationEnabled = false;
                traffic.HostileGenerationEnabled = false;
                recorder.RecordingEnabled = true;
                host.SetRunMode(ConnectionRunMode.Generated);
                recorder.BeginRun();

                var probe = new ConnectionIdentityRuntime("h1", 2f, 91);
                probe.Start(0d, _ => true);
                string occupiedCanonical = probe.AliasIdentity;
                Queue(host, traffic, occupiedCanonical, false, 0f);
                Queue(host, traffic, "h1", true, 2f);
                Queue(host, traffic, "h2", true, 2f);
                host.StepSimulation(.01f);
                var first = host.FindConnection("h1");
                var second = host.FindConnection("h2");
                var ordinary = host.FindConnection(occupiedCanonical);
                string initial = first.KnownIdentity;
                Check(ordinary.KnownIdentity == ordinary.ConnectionId &&
                    first.Identity.Started && first.IsConnecting &&
                    initial != first.ConnectionId && initial != occupiedCanonical &&
                    second.KnownIdentity != second.ConnectionId &&
                    second.KnownIdentity != initial &&
                    ShortId(initial) && ShortId(second.KnownIdentity),
                    scene + " immediate aliases and canonical/alias collision avoidance");
                Check(first.ConnectionId == "h1" && host.FindConnection(initial) == null &&
                    host.FindConnection("h1") == first,
                    scene + " canonical lookup never resolves false alias");
                bool rejectedAliasPlan = false;
                try { Queue(host, traffic, initial, false, 0f); }
                catch (InvalidOperationException) { rejectedAliasPlan = true; }
                Check(rejectedAliasPlan && !traffic.IsIdReserved(initial),
                    scene + " new canonical plan cannot take a live false alias");

                float fullTravel = host.ConnectionTravelSecondsPerLink *
                    (host.Nodes.Any(n => n.View.NodeId == "WEB") ? 3f : 2f);
                host.StepSimulation(fullTravel - .02f);
                Check(first.KnownIdentity == initial && first.IsConnecting,
                    scene + " no early hop while Connecting");
                host.StepSimulation(.02f);
                Check(first.IsActive && first.KnownIdentity == initial,
                    scene + " identity survives establishment");
                presenter.Bind(host.RuntimeConnections.ToArray());
                foreach (LogicalPathView view in presenter.PathLayer
                    .GetComponentsInChildren<LogicalPathView>(true))
                    view.AdvanceVisual(.4f);
                BeginCapture(presenter);
                presenter.RefreshLayouts();
                IdentityPillView[] pills = Pills(identities, "h1");
                Check(pills.Length >= 2 && pills.All(p => p.DisplayedId == initial),
                    scene + " all route pills show one alias");

                panelRoot = new GameObject("ID Hopping witness panels", typeof(RectTransform));
                ((RectTransform)panelRoot.transform).sizeDelta = new Vector2(500f, 900f);
                var load = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<LoadOutput>(
                    "Assets/Prefabs/Connections/LoadOutput.prefab"), panelRoot.transform);
                var work = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WorkOutput>(
                    "Assets/Prefabs/Connections/WorkOutput.prefab"), panelRoot.transform);
                load.Bind("DB", host);
                work.Bind("DB", host);
                LoadRequestRow loadRow = load.GetComponentsInChildren<LoadRequestRow>(true)
                    .Single(row => row.DisplayedId == initial);
                WorkRequestRow workRow = work.GetComponentsInChildren<WorkRequestRow>(true)
                    .Single(row => row.DisplayedId == initial);
                int loadIndex = loadRow.transform.GetSiblingIndex();
                int workIndex = workRow.transform.GetSiblingIndex();
                float workBefore = first.CompletedWork;

                host.StepSimulation(1.99f - host.SimulationTimeSeconds);
                Check(first.KnownIdentity == initial, scene + " two-second boundary not early");
                host.StepSimulation(.01f);
                string changed = first.KnownIdentity;
                presenter.RefreshLayouts();
                Refresh(load);
                Refresh(work);
                Check(changed != initial && first.ConnectionId == "h1" &&
                    first.Identity.HopCount == 2 && first.CompletedWork > workBefore &&
                    first.AllocatedLoad > 0f && host.Infections.Count == 0,
                    scene + " fixed hop leaves canonical identity and resources intact");
                Check(Pills(identities, "h1").SequenceEqual(pills) &&
                    pills.All(p => p.DisplayedId == changed),
                    scene + " existing route pills update together without replacement");
                Check(loadRow.DisplayedId == changed && workRow.DisplayedId == changed &&
                    load.GetComponentsInChildren<LoadRequestRow>(true).Contains(loadRow) &&
                    work.GetComponentsInChildren<WorkRequestRow>(true).Contains(workRow) &&
                    loadRow.transform.GetSiblingIndex() == loadIndex &&
                    workRow.transform.GetSiblingIndex() == workIndex,
                    scene + " live LOAD/WORK labels update in stable rows");
                Capture(scene, "alias", presenter);

                PhysicalLinkView edge = Link(host, "API", "DB");
                Check(host.TryActivateProbe(edge.LinkId, "probe " + edge.LinkId, out _) &&
                    first.KnownIdentity == "h1" && second.KnownIdentity == "h2" &&
                    first.Identity.IsRevealed, scene + " Probe reveals true IDs immediately");
                presenter.RefreshLayouts(); Refresh(load); Refresh(work);
                Check(pills.All(p => p.DisplayedId == "h1") &&
                    loadRow.DisplayedId == "h1" && workRow.DisplayedId == "h1",
                    scene + " revelation reaches every pill and live panel");
                Capture(scene, "revealed", presenter);
                Queue(host, traffic, "h3", true, 2f);
                host.StepSimulation(.01f);
                var newcomer = host.FindConnection("h3");
                Check(newcomer.KnownIdentity != "h3", scene + " later arrival hidden off selected edge");
                host.StepSimulation(fullTravel - host.ConnectionTravelSecondsPerLink + .02f);
                Check(newcomer.KnownIdentity == "h3", scene + " later arrival revealed on selected edge");
                host.StepSimulation(host.ProbeActiveRemaining);
                var scanner = (RectTransform)typeof(LogicalPathPresenter)
                    .GetField("probeRegion", Private).GetValue(presenter);
                Check(host.ProbedLink == null && scanner.GetComponent<ProbeRegionGraphic>()
                    .IsShuttingDown && !first.Identity.IsRevealed &&
                    first.KnownIdentity == first.Identity.AliasIdentity &&
                    first.KnownIdentity != "h1" && second.CurrentOperationIndex == 1 &&
                    second.ConnectionId == "h2",
                    scene + " reveal ends at expiry and aliases survive operation transitions");
                presenter.RefreshLayouts(); Refresh(load); Refresh(work);
                Check(pills.All(p => p.DisplayedId == first.KnownIdentity) &&
                    loadRow.DisplayedId == first.KnownIdentity &&
                    workRow.DisplayedId == first.KnownIdentity,
                    scene + " alias returns on all surfaces after expiry");
                Capture(scene, "expired", presenter);

                int inspections = terminal.GetComponentsInChildren<InspectOutput>(true).Length;
                Submit(terminal, "inspect " + first.KnownIdentity);
                Check(terminal.GetComponentsInChildren<InspectOutput>(true).Length == inspections,
                    scene + " false alias gives ordinary unknown inspection");
                Submit(terminal, "kill " + first.KnownIdentity);
                Check(!first.IsComplete, scene + " false alias cannot kill");
                Submit(terminal, "inspect h1");
                var output = terminal.GetComponentsInChildren<InspectOutput>(true).Last();
                Check(output.GetComponentsInChildren<IdentityPillView>(true)
                    .Any(p => p.DisplayedId == "h1"),
                    scene + " true-ID inspection remains verified after Probe");
                Submit(terminal, "kill h1");
                Check(first.IsComplete && host.FindConnection("h1") == first,
                    scene + " true-ID kill works after Probe and history remains inspectable");
                Submit(terminal, "inspect h1");
                Check(terminal.GetComponentsInChildren<InspectOutput>(true).Length == inspections + 2,
                    scene + " resolved true-ID inspection still works");

                Check(recorder.CurrentLog.events.Any(e => e.type == "identity_alias_changed" &&
                    e.connectionId == "h1" && e.advertisedIdentity == initial &&
                    Mathf.Abs(e.timeSeconds) < .001f) &&
                    recorder.CurrentLog.events.Any(e => e.type == "identity_alias_changed" &&
                    e.connectionId == "h1" && e.identityHopIndex == 2 &&
                    Mathf.Abs(e.timeSeconds - 2f) < .001f) &&
                    recorder.CurrentLog.events.Any(e => e.type == "plan_queued" &&
                    e.connectionId == "h1" && e.definition.identityHopSeconds == 2f &&
                    e.definition.identitySeed == 91),
                    scene + " recorder keeps canonical ID, behavior seed, and hop events");

                host.ResetSimulation();
                Check(host.ProbedLink == null && host.RuntimeConnections.Count == 0 &&
                    host.FindConnection("h1") == null, scene + " reset clears aliases and revelation");
                Queue(host, traffic, occupiedCanonical, false, 0f);
                Queue(host, traffic, "h1", true, 2f);
                Queue(host, traffic, "h2", true, 2f);
                host.StepSimulation(.01f);
                Check(host.FindConnection("h1").KnownIdentity == initial,
                    scene + " same seed and reservation replay initial alias");
                ConnectionSpawnPlan manual = traffic.QueueProfile("basic-threat", idHopping: true);
                Check(manual.IsHostile && manual.IdentityHopSeconds == 2f &&
                    manual.IdentitySeed != 0 && traffic.EffectiveAmbientWeight("basic-threat") == 0f,
                    scene + " manual hostile profile only; ambient weight stays zero");
                ConnectionSpawnPlan ambient = traffic.GenerateHostilePlan(host.SimulationTimeSeconds);
                Check(ambient == null || ambient.IdentityHopSeconds == 0f,
                    scene + " ambient generation does not select hopping");
                Debug.Log("IdHoppingWitness PASS: " + scene);
            }
            finally
            {
                EndCapture(presenter);
                if (panelRoot != null) UnityEngine.Object.DestroyImmediate(panelRoot);
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                traffic.AutoGenerationEnabled = oldAuto;
                traffic.HostileGenerationEnabled = oldHostile;
            }
        }

        private static void Queue(SimulationHost host, ConnectionTrafficDirector traffic,
            string id, bool hostile, float hopSeconds)
        {
            NodeView[] route = host.Nodes.Any(n => n.View.NodeId == "WEB") ?
                new[] { Node(host, "EXT1"), Node(host, "WEB"), Node(host, "API"), Node(host, "DB") } :
                new[] { Node(host, "EXT1"), Node(host, "API"), Node(host, "DB") };
            OperationStage[] stages = id == "h2" ? new[]
            {
                new OperationStage(OperationKind.Read, 4f, 1f, "Initial read"),
                new OperationStage(OperationKind.Write, 1000f, 1f, "Long-running write")
            } : new[] { new OperationStage(OperationKind.Write, 1000f, 1f,
                "Long-running identity witness") };
            traffic.QueueSpawnPlan(new ConnectionSpawnPlan(host.SimulationTimeSeconds, id,
                route, "id-hopping-witness", "Database Maintenance", stages, hostile,
                identityHopSeconds: hopSeconds, identitySeed: 91));
        }

        private static NodeView Node(SimulationHost host, string id) =>
            host.Nodes.Single(n => n.View.NodeId == id).View;

        private static PhysicalLinkView Link(SimulationHost host, string a, string b) =>
            host.PhysicalLinks.Select(link => link.View).Single(link =>
                link.NodeA.NodeId == a && link.NodeB.NodeId == b ||
                link.NodeA.NodeId == b && link.NodeB.NodeId == a);

        private static IdentityPillView[] Pills(IdentityPresenter presenter, string trueId)
        {
            RectTransform layer = (RectTransform)typeof(IdentityPresenter)
                .GetField("pillLayer", Private).GetValue(presenter);
            return layer.GetComponentsInChildren<IdentityPillView>(true)
                .Where(p => p.name.EndsWith(" identity " + trueId,
                    StringComparison.Ordinal)).OrderBy(p => p.name).ToArray();
        }

        private static bool ShortId(string id) => id.Length == 2 &&
            id[0] >= 'a' && id[0] <= 'z' && id[1] >= '0' && id[1] <= '9';

        private static void Refresh(object panel) => panel.GetType()
            .GetMethod("Refresh", Private).Invoke(panel, null);

        private static void Submit(Terminal terminal, string command) =>
            typeof(Terminal).GetMethod("HandleSubmit", Private)
                .Invoke(terminal, new object[] { command });

        private static void BeginCapture(LogicalPathPresenter presenter)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            captureCanvas = presenter.GetComponentInParent<Canvas>();
            previousMode = captureCanvas.renderMode;
            previousCamera = captureCanvas.worldCamera;
            camera = new GameObject("ID Hopping capture camera", typeof(Camera))
                .GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .065f, .08f);
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -100);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000f;
            target = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = target;
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
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            captureCanvas = null;
            camera = null;
            target = null;
        }

        private static void Capture(string scene, string phase,
            LogicalPathPresenter presenter)
        {
            if (camera == null) return;
            RefreshCaptureGeometry(presenter);
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                try
                {
                    image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                    image.Apply();
                    Directory.CreateDirectory(CaptureOutput);
                    File.WriteAllBytes(Path.Combine(CaptureOutput,
                        scene + "-" + phase + "-1920x1080.png"), image.EncodeToPNG());
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
            if (!condition) throw new Exception("ID Hopping Witness: " + message);
        }
    }
}
