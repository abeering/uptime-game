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
            var payloadPresenter = UnityEngine.Object.FindFirstObjectByType<PayloadPresenter>();
            PayloadState first = director.Spawn("DB", PayloadMotionProfile.Variable,
                "EXT1", new[] { 7f, 8f, 9f });
            Check(first.Id == "x1" && first.Route.Select(n => n.NodeId)
                .SequenceEqual(new[] { "EXT1", "WEB", "API", "DB" }) &&
                first.Phase == PayloadPhase.Establishing &&
                first.TravelSeconds.SequenceEqual(new[] { 7f, 8f, 9f }),
                "stable ID, seeded physical route and witness timing");
            Check(host.Nodes.All(n => n.DefensiveRequestedLoad == 0f) &&
                host.Infections.Count == 0, "Payload motion has no LOAD or immediate effect");
            payloadPresenter.RenderNow();
            Check(UnityEngine.Object.FindObjectsByType<PathGraphic>(FindObjectsSortMode.None)
                .Count(g => g.name == "Payload x1 hop") == 1 &&
                UnityEngine.Object.FindObjectsByType<TerminalMarkerStyle>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(m => m.name == "Payload x1 traveler" && !m.gameObject.activeSelf),
                "one visible hop and no traveler during establishment");
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
                    .GetComponentsInChildren<NodeStatusPanel>(true)[2].Height == 63f,
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
            payloadPresenter.RenderNow();
            var sockets = UnityEngine.Object.FindObjectsByType<TerminalMarkerStyle>(
                FindObjectsSortMode.None);
            var firstSocket = sockets.Single(s => s.name == "Payload x1 next-hop socket");
            var secondSocket = sockets.Single(s => s.name == "Payload x2 next-hop socket");
            Check(Vector2.Distance(firstSocket.rectTransform.anchoredPosition,
                secondSocket.rectTransform.anchoredPosition) >= 20f,
                "same-link Payload sockets occupy distinct transient lanes");
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
            Debug.Log("PayloadWitness PASS: both scenes, identity, hop lifecycle, defense, " +
                "expiry, cooldown, delivery, wrong matches, congestion, WORK/Health, recorder and reset.");
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
                "socketPrefab", "idPrefab", "lineStyle" })
                Check(serialized.FindProperty(field).objectReferenceValue != null,
                    scene + " presenter binding " + field);
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("PayloadWitness: " + message); }
    }
}
