using System;
using System.Linq;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class PayloadWitness
    {
        [MenuItem("Tools/Connections/Run Payload Witness")]
        public static void Run()
        {
            CheckScene("Assets/Scenes/Connections.unity", 4);
            CheckScene("Assets/Scenes/ConnectionsAdvanced.unity", 8);
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            var status = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            traffic.AutoGenerationEnabled = false;
            traffic.RandomizeSeed = false;
            host.SetRunMode(ConnectionRunMode.Generated);
            recorder.BeginRun();
            PayloadDirector director = host.Payloads;
            CheckAllPayloadInterception(host, director, recorder, status);
            host.ResetSimulation();
            recorder.BeginRun();
            var payloadPresenter = UnityEngine.Object.FindFirstObjectByType<PayloadPresenter>();
            var pathPresenter = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
            pathPresenter.Bind(host.RuntimeConnections.ToArray());
            PayloadState first = director.Spawn("DB", PayloadMotionProfile.Variable,
                "EXT1", new[] { 7f, 8f, 9f });
            Check(first.Id == "x1" && first.Route.Select(n => n.NodeId)
                .SequenceEqual(new[] { "EXT1", "WEB", "API", "DB" }) &&
                first.Phase == PayloadPhase.Establishing &&
                first.TravelSeconds.SequenceEqual(new[] { 7f, 8f, 9f }),
                "stable ID, seeded physical route and witness timing");
            Check(host.Nodes.All(n => n.DefensiveRequestedLoad == 0f) &&
                host.Infections.Count == 0, "Payload motion has no LOAD or immediate effect");
            pathPresenter.RefreshLayouts();
            payloadPresenter.RenderNow();
            Check(UnityEngine.Object.FindObjectsByType<PathGraphic>(FindObjectsSortMode.None)
                .Count(g => g.name == "Payload x1 hop") == 1 &&
                UnityEngine.Object.FindObjectsByType<PayloadBodyView>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(m => m.name == "Payload x1 traveler" && !m.gameObject.activeSelf &&
                    m.IdText.text == "x1" && m.Marker.InnerDot != null &&
                    !m.Marker.InnerDot.gameObject.activeSelf) &&
                !UnityEngine.Object.FindObjectsByType<IdentityPillView>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Any(p => p.name.Contains("Payload x1")),
                "one visible hop, circle contains ID, and no separate Payload pill");
            host.StepSimulation(.6f);
            Check(first.Phase == PayloadPhase.Establishing &&
                Mathf.Abs(first.Progress - .5f) < .001f,
                "establishment grows before circle movement");
            host.StepSimulation(.6f);
            Check(first.Phase == PayloadPhase.Traversing && first.Progress == 0f,
                "traversal begins at source after establishment");
            host.StepSimulation(7f);
            Check(first.Phase == PayloadPhase.Establishing && first.Hop == 1 &&
                host.Infections.Count == 0, "intermediate arrival starts only next hop");
            Check(director.TryIntercept(first.Id, "WEB", out _),
                "wrong prediction is accepted without hidden-route validation");
            Check(!director.TryIntercept(first.Id, "WEB", out _),
                "one active interception slot per node");
            Check(director.TryIntercept(first.Id, "DB", out _) &&
                director.InterceptorAt(host.FindNodeResources("DB").View) != null,
                "separate nodes can arm independently");
            Check(host.FindNodeResources("WEB").DefensiveRequestedLoad == 4f &&
                host.FindNodeResources("WEB").DefensiveAllocatedLoad == 4f,
                "defensive demand appears in authoritative allocation");
            status.RenderNow(1f, 1f);
            Check(status.StateFor(host.FindNodeResources("WEB").View).InterceptPayloadId == "x1" &&
                status.ViewFor(host.FindNodeResources("WEB").View).BenefitVisible,
                "beneficial interception status reads host state");
            NodeStatusState webStatus = status.StateFor(host.FindNodeResources("WEB").View);
            webStatus.ScanResult(NodeScanResult.Clean, 1f, 2.5f);
            status.RenderNow(1.1f, 1f);
            Check(webStatus.CleanVisible(1.1f) && webStatus.InterceptPayloadId == "x1" &&
                status.ViewFor(host.FindNodeResources("WEB").View).BenefitVisible &&
                status.ViewFor(host.FindNodeResources("WEB").View)
                    .GetComponentsInChildren<TMPro.TMP_Text>(true)
                    .Any(t => t.text == "CLEAN") &&
                status.ViewFor(host.FindNodeResources("WEB").View)
                    .GetComponentsInChildren<TMPro.TMP_Text>(true)
                    .Any(t => t.text == "INTERCEPT x1"),
                "CLEAN and timed interception share the beneficial panel");
            host.StepSimulation(5f);
            Check(director.InterceptorAt(host.FindNodeResources("WEB").View) == null &&
                host.FindNodeResources("WEB").DefensiveRequestedLoad == 0f &&
                !director.TryIntercept(first.Id, "WEB", out _),
                "expiry releases LOAD but node cooldown remains");
            host.StepSimulation(10f);
            Check(director.TryIntercept(first.Id, "WEB", out _),
                "cooldown starts at activation, not expiration");
            host.ResetSimulation();
            recorder.BeginRun();
            Check(director.Payloads.Count == 0 && director.Interceptors.Count == 0 &&
                director.FindActive("x1") == null, "reset clears active Payload and defense");

            PayloadState sampled = director.Spawn("DB", PayloadMotionProfile.Variable, "EXT1");
            float[] timings = (float[])sampled.TravelSeconds.Clone();
            Check(timings.All(t => t >= 7f && t <= 25f),
                "Variable samples within configured movement range");
            host.ResetSimulation();
            PayloadState replay = director.Spawn("DB", PayloadMotionProfile.Variable, "EXT1");
            Check(replay.Id == sampled.Id && replay.TravelSeconds.SequenceEqual(timings),
                "reset replays Payload timing without ambient RNG dependence");
            host.ResetSimulation();
            PayloadState steadySample = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1");
            Check(steadySample.TravelSeconds.All(t => t >= 15f && t <= 20f),
                "Steady samples within consistent movement range");
            host.ResetSimulation();
            bool invalidProfile = false;
            try { director.Spawn("DB", (PayloadMotionProfile)99, "EXT1"); }
            catch (InvalidOperationException) { invalidProfile = true; }
            Check(invalidProfile && director.Payloads.Count == 0,
                "invalid movement profile cannot spawn");
            var reserved = new ConnectionSpawnPlan(0f, "x1",
                new[] { host.FindNodeResources("EXT1").View,
                    host.FindNodeResources("WEB").View,
                    host.FindNodeResources("API").View,
                    host.FindNodeResources("DB").View },
                "id-witness", "ID witness",
                new[] { new OperationStage(OperationKind.Read, 100f, 1f,
                    "ID witness") });
            traffic.QueueSpawnPlan(reserved);
            Check(director.Spawn("DB", PayloadMotionProfile.Steady, "EXT1",
                new[] { 1f, 1f, 1f }).Id == "x2",
                "Payload ID avoids queued ordinary connection ID");
            host.ResetSimulation();
            recorder.BeginRun();

            PayloadState delivery = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", new[] { 1f, 1f, 1f });
            host.StepSimulation(6.6f); // Three 1.2s establishment periods plus three traversals.
            Check(delivery.Phase == PayloadPhase.Resolved && delivery.EffectApplied &&
                host.Infections.Any(i => i.Target.View.NodeId == "DB" &&
                    i.SourceConnectionId == delivery.Id),
                "final action delivers existing Phantom LOAD with Payload provenance");
            Check(!director.TryIntercept(delivery.Id, "DB", out _),
                "resolved ID rejected without target disclosure");
            Check(recorder.CurrentLog.events.Any(e => e.type == "payload_created" &&
                e.payloadId == delivery.Id && e.payloadRoute.SequenceEqual(
                    new[] { "EXT1", "WEB", "API", "DB" })) &&
                recorder.CurrentLog.events.Any(e => e.type == "payload_effect_delivered" &&
                    e.payloadId == delivery.Id) &&
                recorder.CurrentLog.events.Any(e => e.type == "infection_applied" &&
                    e.sourceConnectionId == delivery.Id),
                "recorder captures full internal plan and delivery lineage");

            host.ResetSimulation();
            recorder.BeginRun();
            PayloadState threat = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", new[] { 1f, 1f, 1f });
            PayloadState other = director.Spawn("DB", PayloadMotionProfile.Variable,
                "EXT1", new[] { 1f, 1f, 1f });
            Check(threat.Id == "x1" && other.Id == "x2", "simultaneous IDs do not collide");
            pathPresenter.RefreshLayouts();
            payloadPresenter.RenderNow();
            var sockets = UnityEngine.Object.FindObjectsByType<TerminalMarkerStyle>(
                FindObjectsSortMode.None);
            var firstSocket = sockets.Single(s => s.name == "Payload x1 next-hop socket");
            var secondSocket = sockets.Single(s => s.name == "Payload x2 next-hop socket");
            Check(Vector2.Distance(firstSocket.rectTransform.anchoredPosition,
                secondSocket.rectTransform.anchoredPosition) >= 10f,
                "same-link Payload sockets occupy distinct shared lanes");
            Check(director.TryIntercept(threat.Id, "DB", out _) &&
                !director.TryIntercept(other.Id, "DB", out _),
                "one target selected from simultaneous Payloads");
            Check(host.FindNodeResources("DB").DefensiveRequestedLoad == 4f,
                "one slot reserves one defense demand");
            host.StepSimulation(5f);
            Check(director.InterceptorAt(host.FindNodeResources("DB").View) == null,
                "window expires before late arrivals");
            host.StepSimulation(1.6f);
            Check(!threat.Intercepted && !other.Intercepted &&
                host.Infections.Count == 1, "expired window cannot block final action");

            host.ResetSimulation();
            recorder.BeginRun();
            PayloadState defended = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", new[] { 1f, 1f, 1f });
            host.StepSimulation(3.6f); // One hop remains; final arrival is at 6.6s.
            Check(director.TryIntercept(defended.Id, "DB", out _,
                "intercept x1 @ db"),
                "interception can arm just before final hop");
            host.StepSimulation(3f);
            Check(defended.Intercepted && !defended.EffectApplied &&
                host.Infections.Count == 0 &&
                director.InterceptorAt(host.FindNodeResources("DB").View)?.Succeeded == true &&
                host.FindNodeResources("DB").DefensiveRequestedLoad == 4f,
                "correct ID/node/window intercepts; slot and cost persist");
            Check(recorder.CurrentLog.events.Any(e => e.type == "intercept_succeeded" &&
                e.payloadId == defended.Id && e.targetNodeId == "DB") &&
                recorder.CurrentLog.events.Any(e => e.type == "intercept_activated" &&
                    e.actionSequence > 0 && recorder.CurrentLog.events.Any(action =>
                        action.type == "player_action" && action.sequence == e.actionSequence)) &&
                recorder.CurrentLog.events.Any(e => e.type == "payload_intercepted" &&
                    e.payloadId == defended.Id) &&
                !recorder.CurrentLog.events.Any(e => e.type == "infection_applied" &&
                    e.sourceConnectionId == defended.Id), "intercept recorder outcome and suppression");

            host.ResetSimulation();
            recorder.BeginRun();
            PayloadState wrongNode = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", new[] { 1f, 1f, 1f });
            host.StepSimulation(2.2f);
            Check(director.TryIntercept(wrongNode.Id, "WEB", out _),
                "wrong node on known physical route is accepted");
            host.StepSimulation(4.4f);
            Check(!wrongNode.Intercepted && wrongNode.EffectApplied,
                "pass-through does not trigger wrong-node interception");

            host.ResetSimulation();
            recorder.BeginRun();
            PayloadState chosen = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", new[] { 1f, 1f, 1f });
            PayloadState unchosen = director.Spawn("DB", PayloadMotionProfile.Variable,
                "EXT1", new[] { 1f, 1f, 1f });
            host.StepSimulation(3.6f);
            Check(director.TryIntercept(chosen.Id, "DB", out _) &&
                !director.TryIntercept(unchosen.Id, "DB", out _),
                "cannot arm another ID in occupied node slot");
            host.StepSimulation(3f);
            Check(chosen.Intercepted && !unchosen.Intercepted &&
                unchosen.EffectApplied, "same-time arrival matches only selected ID");

            host.ResetSimulation();
            recorder.BeginRun();
            PayloadState congested = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", new[] { 1f, 1f, 1f });
            Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 10f, out _),
                "congested DB setup");
            host.StepSimulation(3.6f);
            Check(director.TryIntercept(congested.Id, "DB", out _) &&
                host.FindNodeResources("DB").DefensiveRequestedLoad == 4f &&
                host.FindNodeResources("DB").DefensiveAllocatedLoad == 0f,
                "defense demand recorded even when infection consumes capacity");
            host.StepSimulation(3f);
            Check(congested.Intercepted && !congested.EffectApplied &&
                recorder.CurrentLog.events.Any(e => e.type == "intercept_succeeded" &&
                    e.defensiveRequestedLoad == 4f && e.defensiveAllocatedLoad == 0f),
                "protection reliable with zero allocated defensive LOAD");

            host.ResetSimulation();
            recorder.BeginRun();
            PayloadState pressure = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", new[] { 20f, 20f, 20f });
            ConnectionSpawnPlan plan = traffic.QueueHighDemandWitness("DB");
            host.StepSimulation(3f);
            LogicalConnectionState work = host.FindConnection(plan.ConnectionId);
            Check(work != null && work.IsActive, "legitimate high-demand WORK established");
            float before = work.AllocatedLoad;
            Check(director.TryIntercept(pressure.Id, "DB", out _),
                "defense can activate under productive LOAD contention");
            float after = work.AllocatedLoad;
            float healthBefore = host.Health.Value;
            host.StepSimulation(1f);
            Check(after < before - 3.9f && work.CompletedWork > 0f &&
                host.Health.Value < healthBefore &&
                recorder.CurrentLog.snapshots.Any(s => s.nodes.Any(n => n.id == "DB" &&
                    n.defensiveRequestedLoad == 4f && n.defensiveAllocatedLoad == 4f)),
                "defense takes capacity from WORK; ordinary Health slowdown and snapshots follow");

            host.ResetSimulation();
            recorder.BeginRun();
            NodeView ext = host.Nodes.Single(n => n.View.NodeId == "EXT1").View;
            NodeView web = host.Nodes.Single(n => n.View.NodeId == "WEB").View;
            var sharedPlan = new ConnectionSpawnPlan(0f, "shared1", new[] { ext, web },
                "test", "Shared lane", new[] {
                    new OperationStage(OperationKind.Read, 500f, 1f, "Read") });
            host.RegisterConnection(LogicalConnectionDefinition.FromPlan(sharedPlan));
            PayloadState shared = director.Spawn("WEB", PayloadMotionProfile.Steady,
                "EXT1", new[] { 20f });
            pathPresenter.Bind(host.RuntimeConnections.ToArray());
            pathPresenter.RefreshLayouts();
            payloadPresenter.RenderNow();
            LogicalPathView normalView = UnityEngine.Object.FindObjectsByType<LogicalPathView>(
                FindObjectsSortMode.None).Single(v => v.name == "Logical path shared1");
            normalView.AdvanceVisual(1f);
            Check(normalView.TryGetVisualSegment(0, out Vector2 normalStart,
                    out Vector2 normalEnd, out _, out _) &&
                payloadPresenter.TryGetVisualSegment(shared, out Vector2 payloadStart,
                    out Vector2 payloadEnd) &&
                Vector2.Distance((normalStart + normalEnd) * .5f,
                    (payloadStart + payloadEnd) * .5f) >= 6f,
                "Payload and ordinary connection occupy separate shared-solver lanes");
            Check(Resources.FindObjectsOfTypeAll<IdentityPillView>()
                    .Count(p => p.name.Contains("identity shared1")) == 1 &&
                !Resources.FindObjectsOfTypeAll<IdentityPillView>()
                    .Any(p => p.name.Contains("identity x1")),
                "ordinary lane retains its pill while Payload lane has none");
            Debug.Log("PayloadWitness PASS: both scenes, identity, hop lifecycle, defense, " +
                "all-Payload and targeted commands, repeated catches, shared lanes, expiry, cooldown, " +
                "delivery, wrong matches, congestion, WORK/Health, recorder and reset.");
        }

        private static void CheckAllPayloadInterception(SimulationHost host, PayloadDirector director,
            ConnectionsRunRecorder recorder, NodeStatusPresenter status)
        {
            Check(Terminal.TryParseIntercept(new[] { "intercept", "@", "api" }, out string filter,
                out string nodeId) && filter == null && nodeId == "api", "node-only command");
            Check(Terminal.TryParseIntercept(new[] { "intercept", "X1", "@", "API" }, out filter,
                out nodeId) && filter == "X1" && nodeId == "API", "optional targeted command");
            foreach (string[] invalid in new[] {
                new[] { "intercept" }, new[] { "intercept", "@" },
                new[] { "intercept", "api" }, new[] { "intercept", "x1", "api" },
                new[] { "intercept", "@", "@" }, new[] { "intercept", "@", "@", "api" },
                new[] { "intercept", "@", "api", "extra" },
                new[] { "intercept", "x1", "@", "api", "extra" } })
                Check(!Terminal.TryParseIntercept(invalid, out _, out _), "malformed intercept command");

            host.ResetSimulation();
            recorder.BeginRun();
            NodeResources db = host.FindNodeResources("DB");
            Check(!director.TryIntercept(null, "missing", out _) &&
                !director.TryIntercept("missing", "DB", out _) &&
                director.Interceptors.Count == 0, "invalid nodes and explicit IDs cannot arm defense");
            Check(director.TryIntercept(null, "db", out _, "intercept @ db"),
                "node-only defense can arm before any Payload exists");
            InterceptionState armed = director.InterceptorAt(db.View);
            float expiry = armed.ExpiresAt;
            Check(armed.AllPayloads && armed.PayloadId == null && armed.CaughtCount == 0 &&
                db.DefensiveRequestedLoad == 4f && db.DefensiveAllocatedLoad == 4f,
                "one broad interceptor has one four-LOAD cost");
            PayloadState first = director.Spawn("DB", PayloadMotionProfile.Steady, "API", new[] { .8f });
            PayloadState simultaneous = director.Spawn("DB", PayloadMotionProfile.Steady, "API", new[] { .8f });
            PayloadState later = director.Spawn("DB", PayloadMotionProfile.Steady, "API", new[] { 2.8f });
            PayloadState boundary = director.Spawn("DB", PayloadMotionProfile.Steady, "API", new[] { 3.8f });
            PayloadState late = director.Spawn("DB", PayloadMotionProfile.Steady, "API", new[] { 4f });
            Check(!director.TryIntercept(first.Id, "DB", out _) &&
                !director.TryIntercept(null, "DB", out _), "both modes share one occupied node slot");
            host.StepSimulation(2f);
            Check(first.Intercepted && simultaneous.Intercepted && armed.CaughtCount == 2 &&
                director.InterceptorAt(db.View) == armed && armed.ExpiresAt == expiry &&
                db.DefensiveRequestedLoad == 4f && host.Infections.Count == 0,
                "simultaneous catches retain the original window and single LOAD cost");
            status.RenderNow(1f, 1f);
            Check(status.StateFor(db.View).InterceptActive && status.StateFor(db.View).InterceptAllPayloads &&
                status.StateFor(db.View).HasAny(1f) && status.ViewFor(db.View).BenefitVisible &&
                status.ViewFor(db.View).GetComponentsInChildren<TMPro.TMP_Text>()
                    .Any(t => t.text == "INTERCEPT ALL"),
                "broad defense remains visibly active after catches with its timer");
            int snapshotIndex = recorder.CaptureSnapshot(host.SimulationTimeSeconds);
            InterceptionSnapshot snapshot = recorder.CurrentLog.snapshots[snapshotIndex].interceptors.Single();
            Check(snapshot.allPayloads && snapshot.payloadId == null && snapshot.caughtCount == 2 &&
                snapshot.expiresAtSeconds == expiry, "snapshot preserves broad scope and repeated success");
            host.StepSimulation(2f);
            Check(later.Intercepted && armed.CaughtCount == 3 && db.DefensiveRequestedLoad == 4f,
                "later arrival is caught after the earlier successes");
            host.StepSimulation(1f);
            Check(boundary.Intercepted && armed.CaughtCount == 4 && director.InterceptorAt(db.View) == null &&
                db.DefensiveRequestedLoad == 0f && !director.TryIntercept(null, "DB", out _),
                "arrival wins at expiry; LOAD releases and cooldown persists");
            host.StepSimulation(.2f);
            Check(!late.Intercepted && late.EffectApplied, "arrival after broad expiry delivers its effect");
            var catches = recorder.CurrentLog.events.Where(e => e.type == "intercept_succeeded").ToArray();
            Check(catches.Length == 4 && catches.All(e => e.interceptAllPayloads &&
                e.actionSequence == armed.ActionSequence && e.actionSequence > 0 && e.targetNodeId == "DB") &&
                catches.Select(e => e.payloadId).SequenceEqual(new[] {
                    first.Id, simultaneous.Id, later.Id, boundary.Id }) &&
                catches.Select(e => e.interceptCaughtCount).SequenceEqual(new[] { 1, 2, 3, 4 }) &&
                recorder.CurrentLog.events.Any(e => e.type == "intercept_activated" &&
                    e.interceptAllPayloads && e.payloadId == null) &&
                recorder.CurrentLog.events.Any(e => e.type == "intercept_expired" &&
                    e.interceptAllPayloads && e.interceptCaughtCount == 4),
                "recorder links each actual catch to the broad activation and cumulative count");
            ConnectionsRunLog roundTrip = JsonUtility.FromJson<ConnectionsRunLog>(JsonUtility.ToJson(recorder.CurrentLog));
            Check(roundTrip.events.Count(e => e.type == "intercept_succeeded" && e.interceptAllPayloads) == 4 &&
                roundTrip.snapshots[snapshotIndex].interceptors.Single().caughtCount == 2,
                "broad scope and catch history survive recorder JSON");
            host.StepSimulation(9.8f);
            Check(director.TryIntercept(null, "DB", out _), "broad cooldown ends fifteen seconds from activation");

            host.ResetSimulation();
            recorder.BeginRun();
            status.RenderNow(2f, 1f);
            Check(director.Interceptors.Count == 0 && !status.StateFor(db.View).InterceptActive,
                "reset clears broad defense and status");
            PayloadState through = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", new[] { 1f, 1f, 1f });
            Check(director.TryIntercept(null, "API", out _) && director.TryIntercept(null, "WEB", out _),
                "nodes can arm independently without target or route prediction validation");
            host.StepSimulation(4.4f);
            Check(!through.Intercepted && through.Phase != PayloadPhase.Resolved &&
                director.InterceptorAt(host.FindNodeResources("API").View).CaughtCount == 0,
                "broad defense does not catch a Payload passing through an intermediate node");
            host.StepSimulation(2.2f);
            Check(through.EffectApplied, "broad defense preserves final-destination interception rule");

            host.ResetSimulation();
            recorder.BeginRun();
            Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad, 10f, out _) &&
                director.TryIntercept(null, "DB", out _), "broad defense under full infection demand");
            PayloadState congested = director.Spawn("DB", PayloadMotionProfile.Steady, "API", new[] { .8f });
            Check(db.DefensiveRequestedLoad == 4f && db.DefensiveAllocatedLoad == 0f,
                "broad defense records its cost with no spare allocation");
            host.StepSimulation(2f);
            Check(congested.Intercepted && !congested.EffectApplied &&
                recorder.CurrentLog.events.Any(e => e.type == "intercept_succeeded" &&
                    e.interceptAllPayloads && e.defensiveAllocatedLoad == 0f),
                "broad interception remains reliable at zero defensive allocation");
        }

        private static void CheckScene(string scene, int nodeCount)
        {
            EditorSceneManager.OpenScene(scene);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<PayloadPresenter>();
            Check(host != null && host.Nodes.Length == nodeCount && host.Payloads != null &&
                presenter != null, scene + " Payload services");
            var serialized = new SerializedObject(presenter);
            foreach (string field in new[] { "host", "networkArea", "pathLayer", "markerLayer",
                "socketPrefab", "bodyPrefab", "lineStyle" })
                Check(serialized.FindProperty(field).objectReferenceValue != null,
                    scene + " presenter binding " + field);
            var path = UnityEngine.Object.FindFirstObjectByType<LogicalPathPresenter>();
            var pathBindings = new SerializedObject(path);
            foreach (string field in new[] { "payloadDirector", "payloadPresenter" })
                Check(pathBindings.FindProperty(field).objectReferenceValue != null,
                    scene + " shared lane binding " + field);
            CheckTelegraph(host, presenter, path, scene);
        }

        private static void CheckTelegraph(SimulationHost host, PayloadPresenter presenter,
            LogicalPathPresenter path, string scene)
        {
            host.SetRunMode(ConnectionRunMode.Generated);
            host.ResetSimulation();
            PayloadDirector director = host.Payloads;
            path.Bind(host.RuntimeConnections.ToArray());
            float[] hopTimes = scene.EndsWith("Connections.unity", StringComparison.Ordinal) ?
                new[] { 1f, 1f } : new[] { 1f, 1f, 1f };
            PayloadState multi = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", hopTimes);
            Check(multi.Route.Length == hopTimes.Length + 1 &&
                multi.CurrentNode.NodeId == "EXT1" && multi.FinalNode.NodeId == "DB",
                scene + " telegraph route");
            for (int hop = 0; hop < hopTimes.Length - 1; hop++)
            {
                path.RefreshLayouts();
                presenter.RenderNow();
                CheckVisualColor(multi, director.PayloadColor, scene + " orange establishment");
                host.StepSimulation(1.2f);
                presenter.RenderNow();
                CheckVisualColor(multi, director.PayloadColor, scene + " orange traversal");
                host.StepSimulation(1f);
                Check(multi.Hop == hop + 1 && multi.Phase == PayloadPhase.Establishing &&
                    !multi.EffectApplied && host.Infections.Count == 0,
                    scene + " intermediate arrival has no infection attempt");
            }
            path.RefreshLayouts();
            presenter.RenderNow();
            CheckVisualColor(multi, director.StrikeColor, scene + " red at final establishment start");
            host.StepSimulation(.6f);
            presenter.RenderNow();
            CheckVisualColor(multi, director.StrikeColor, scene + " red during establishment");
            host.StepSimulation(.6f);
            presenter.RenderNow();
            CheckVisualColor(multi, director.StrikeColor, scene + " red at traversal start");
            host.StepSimulation(.5f);
            presenter.RenderNow();
            CheckVisualColor(multi, director.StrikeColor, scene + " red during traversal");

            PayloadState oneHop = director.Spawn("DB", PayloadMotionProfile.Variable,
                "API", new[] { 20f });
            PayloadState orange = director.Spawn("DB", PayloadMotionProfile.Steady,
                "EXT1", scene.EndsWith("Connections.unity", StringComparison.Ordinal) ?
                    new[] { 20f, 20f } : new[] { 20f, 20f, 20f });
            path.RefreshLayouts(); // Occupancy changes drive the normal animated lane reflow.
            presenter.RenderNow();
            CheckVisualColor(multi, director.StrikeColor, scene + " red during reflow");
            CheckVisualColor(oneHop, director.StrikeColor, scene + " one-hop red on first appearance");
            CheckVisualColor(orange, director.PayloadColor, scene + " concurrent orange during reflow");

            host.StepSimulation(.5f);
            presenter.RenderNow();
            Check(multi.Phase == PayloadPhase.Resolved &&
                !HasVisual(multi), scene + " resolved visual removed");
            host.ResetSimulation();
            presenter.RenderNow();
            Check(!HasVisual(oneHop) && !HasVisual(orange),
                scene + " reset removes remaining visuals");
        }

        private static void CheckVisualColor(PayloadState payload, Color expected, string label)
        {
            PathGraphic line = UnityEngine.Object.FindObjectsByType<PathGraphic>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(v => v.name == "Payload " + payload.Id + " hop");
            TerminalMarkerStyle socket = UnityEngine.Object.FindObjectsByType<TerminalMarkerStyle>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(v => v.name == "Payload " + payload.Id + " next-hop socket");
            PayloadBodyView body = UnityEngine.Object.FindObjectsByType<PayloadBodyView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(v => v.name == "Payload " + payload.Id + " traveler");
            Check(line.color == expected && socket.color == expected &&
                body.Marker.color == expected && body.IdText.color == expected,
                label + " line, socket, body, and ID");
        }

        private static bool HasVisual(PayloadState payload) =>
            UnityEngine.Object.FindObjectsByType<PathGraphic>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(v => v.name == "Payload " + payload.Id + " hop");

        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("PayloadWitness: " + message); }
    }
}
