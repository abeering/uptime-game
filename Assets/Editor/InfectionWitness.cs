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
    public static class InfectionWitness
    {
        private const float Tolerance = 0.001f;

        [MenuItem("Tools/Connections/Run Infection Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && director != null && recorder != null &&
                host.Recorder == recorder, "scene infection bindings");
            bool oldAuto = director.AutoGenerationEnabled;
            bool oldRecording = recorder.RecordingEnabled;
            int oldSeed = director.Seed;
            try
            {
                director.AutoGenerationEnabled = false;
                director.Seed = 1701;
                recorder.RecordingEnabled = true;
                host.SetRunMode(ConnectionRunMode.Generated);
                Check(host.DefaultPhantomLoad == 8f && host.Infections.Count == 0,
                    "authored default and clean initial state");
                CheckParserAndDeterminism(director);
                CheckKilledCarrier(host, director, recorder);
                CheckDeliveredCarrier(host, director, recorder);
                CheckDirectInjectionAndAllocation(host, director, recorder);
                CheckBaselinePriority(host, director);
                CheckLegacyProfile(host, director);
                Debug.Log("InfectionWitness PASS: queued payload, interruption, completion, " +
                    "persistence, duplicate rejection, direct controls, capacity, Health, " +
                    "recorder JSON, deterministic generation, and legacy profile.");
            }
            finally
            {
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                director.AutoGenerationEnabled = oldAuto;
                director.Seed = oldSeed;
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static void CheckParserAndDeterminism(ConnectionTrafficDirector director)
        {
            foreach (string text in new[]
            {
                "spawn product-lookup obj:bogus",
                "spawn product-lookup obj:delivery inf:blackout",
                "spawn product-lookup obj:delivery inf:phantom-load infp:load=-1",
                "spawn product-lookup obj:delivery inf:phantom-load infp:load=NaN",
                "spawn product-lookup obj:delivery infp:load=8",
                "spawn product-lookup obj:loader inf:phantom-load",
                "spawn product-lookup obj:delivery route:DB"
            })
                Check(!Parse(text, out _, out _, out string error) &&
                    !string.IsNullOrWhiteSpace(error), "invalid spawn option: " + text);
            Check(Parse("spawn product-lookup obj:delivery inf:phantom-load",
                out _, out ConnectionSpawnOverrides standard, out _) &&
                standard.Objective == ManualObjective.Delivery &&
                standard.InfectionKind == InfectionKind.PhantomLoad &&
                !standard.InfectionLoad.HasValue, "default delivery command parses");
            Check(Parse("spawn product-lookup obj:loader", out _,
                out ConnectionSpawnOverrides loader, out _) &&
                loader.Objective == ManualObjective.Loader, "loader command parses");
            Check(Parse("spawn basic-threat", out _, out ConnectionSpawnOverrides legacy,
                out _) && legacy == null, "legacy command parses unchanged");
            bool invalidProfileOverride = false;
            try { director.QueueProfile("basic-threat", standard); }
            catch (InvalidOperationException) { invalidProfileOverride = true; }
            Check(invalidProfileOverride, "legacy profile rejects objective override");

            FieldInfo hostileRate = typeof(ConnectionTrafficDirector).GetField("hostileChance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo deliveryRate = typeof(ConnectionTrafficDirector).GetField("deliveryChance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float oldHostileRate = (float)hostileRate.GetValue(director);
            float oldDeliveryRate = (float)deliveryRate.GetValue(director);
            try
            {
                hostileRate.SetValue(director, 0f);
                deliveryRate.SetValue(director, 0f);
                ConnectionSpawnPlan forced = director.QueueProfile("product-lookup", standard);
                Check(forced.IsHostile && forced.GenerationTrace.objective == "DELIVERY" &&
                    forced.Operations.Last().EffectOnCompletion ==
                        CompletionEffect.InfectionDelivery,
                    "manual objective bypasses zero hostile and delivery rates");
            }
            finally
            {
                hostileRate.SetValue(director, oldHostileRate);
                deliveryRate.SetValue(director, oldDeliveryRate);
            }

            director.ResetDirector();
            ConnectionSpawnPlan[] generated = Enumerable.Range(0, 120)
                .Select(i => director.GeneratePlan(i * 13f)).ToArray();
            Check(generated.Any(plan => plan.GenerationTrace.objective == "DELIVERY") &&
                generated.Where(plan => plan.GenerationTrace.objective == "DELIVERY")
                    .All(plan => plan.Operations.Last().InfectionPayload?.Kind ==
                        InfectionKind.PhantomLoad &&
                        plan.Operations.Last().InfectionPayload.RequestedLoad == 8f) &&
                generated.Where(plan => plan.GenerationTrace.objective != "DELIVERY")
                    .All(plan => plan.Operations.All(stage =>
                        stage.InfectionPayload == null)),
                "all natural DELIVERY plans carry Phantom LOAD and other plans do not");
            var first = generated.Select(PayloadSignature).ToArray();
            director.ResetDirector();
            var second = Enumerable.Range(0, 120).Select(i => director.GeneratePlan(i * 13f))
                .Select(PayloadSignature).ToArray();
            Check(first.SequenceEqual(second), "seeded payload generation replay");
            director.ResetDirector();
        }

        private static void CheckKilledCarrier(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            host.ResetSimulation();
            recorder.BeginRun();
            Check(Parse("spawn product-lookup obj:delivery inf:phantom-load",
                out string purpose, out ConnectionSpawnOverrides options, out _),
                "default delivery parse");
            ConnectionSpawnPlan plan = director.QueueProfile(purpose, options);
            Check(plan.GenerationTrace.forcedOptions.objective == "DELIVERY" &&
                plan.Operations.Last().InfectionPayload.RequestedLoad == 8f &&
                host.FindConnection(plan.ConnectionId) == null && host.Infections.Count == 0,
                "forced plan is queued without immediate infection");
            host.StepSimulation(0.1f);
            Check(host.FindConnection(plan.ConnectionId).IsActive &&
                host.Infections.Count == 0, "real carrier exists before delivery");
            Check(host.KillConnection(plan.ConnectionId), "carrier can be interrupted");
            host.StepSimulation(100f);
            Check(host.Infections.Count == 0 &&
                recorder.CurrentLog.events.All(e => e.type != "infection_applied"),
                "kill before completion prevents application");
        }

        private static void CheckDeliveredCarrier(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            host.ResetSimulation();
            recorder.BeginRun();
            Check(Parse("spawn product-lookup obj:delivery inf:phantom-load infp:load=8",
                out string purpose, out ConnectionSpawnOverrides options, out _),
                "parameterized delivery parse");
            ConnectionSpawnPlan plan = director.QueueProfile(purpose, options);
            RunEvent queued = recorder.CurrentLog.events.Last();
            Check(queued.type == "plan_queued" &&
                queued.generation.forcedOptions.objective == "DELIVERY" &&
                queued.generation.forcedOptions.infectionType == "PhantomLoad" &&
                queued.generation.forcedOptions.hasLoadParameter &&
                queued.generation.forcedOptions.loadParameter == 8f &&
                queued.definition.operations.Last().infectionKind == "PhantomLoad" &&
                queued.definition.operations.Last().infectionLoad == 8f,
                "queued recorder plan contains forced decision and typed payload");
            host.StepSimulation(0.1f);
            Check(host.Infections.Count == 0, "command and arrival do not deliver");
            StepUntilComplete(host, plan.ConnectionId);
            NodeInfection applied = host.Infections.Single();
            NodeResources db = Node(host, "DB");
            Check(applied.Target == db && applied.Payload.Kind == InfectionKind.PhantomLoad &&
                applied.Payload.RequestedLoad == 8f &&
                applied.SourceConnectionId == plan.ConnectionId &&
                applied.SourceOperationIndex == plan.Operations.Length - 1 &&
                applied.AppliedAtSeconds == host.SimulationTimeSeconds,
                "completed operation applies typed infection to authored target");
            Check(host.RuntimeConnections.Count == 0 &&
                !host.KillConnection(plan.ConnectionId) && host.Infections.Count == 1,
                "infection persists after carrier resolves");
            Near(db.RequestedLoad, 8f, "infection LOAD without connections");
            Near(db.AllocatedLoad, 8f, "infection allocation without connections");
            Check(db.View.LoadBar.LabelText.Contains("8 / 10"),
                "existing node LOAD bar shows unexplained demand");
            Check(recorder.CurrentLog.events.Count(e => e.type == "infection_applied") == 1 &&
                recorder.CurrentLog.events.Count(e => e.type == "effect") == 1,
                "completion delivers and applies once");

            var duplicate = new ConnectionSpawnPlan(host.SimulationTimeSeconds, "dupe1",
                new[] { Node(host, "EXT1").View, Node(host, "API").View, db.View },
                "witness", "Maintenance", new[]
                {
                    new OperationStage(OperationKind.Write, 1f, 1f, "Duplicate delivery",
                        CompletionEffect.InfectionDelivery, null,
                        new InfectionPayload(InfectionKind.PhantomLoad, 3f))
                }, true);
            director.QueueSpawnPlan(duplicate);
            host.StepSimulation(1f);
            Check(host.Infections.Count == 1 && ReferenceEquals(host.Infections[0], applied) &&
                recorder.CurrentLog.events.Count(e => e.type == "infection_rejected") == 1 &&
                recorder.CurrentLog.events.Last(e => e.type == "infection_rejected")
                    .reason == "already_active", "duplicate delivery does not stack or reset");
            recorder.CaptureSnapshot(host.SimulationTimeSeconds);
            Check(recorder.Save(false), "infection JSON saved");
            ConnectionsRunLog saved = JsonUtility.FromJson<ConnectionsRunLog>(
                File.ReadAllText(recorder.OutputPath));
            RunEvent savedQueue = saved.events.Single(e => e.type == "plan_queued" &&
                e.connectionId == plan.ConnectionId);
            NodeSnapshot savedDb = saved.snapshots.Last().nodes.Single(n => n.id == "DB");
            Check(savedQueue.generation.forcedOptions.hasLoadParameter &&
                savedQueue.definition.operations.Last().infectionLoad == 8f &&
                saved.events.Any(e => e.type == "infection_applied" &&
                    e.sourceConnectionId == plan.ConnectionId && e.infectionLoad == 8f) &&
                saved.snapshots.Last().infections.Single().sourceConnectionId ==
                    plan.ConnectionId && savedDb.infectionRequestedLoad == 8f &&
                savedDb.connectionRequestedLoad == 0f,
                "saved JSON retains forced plan, provenance, and resource breakdown");
            Check(host.TryRemovePhantomLoad("DB", out _) &&
                host.Infections.Count == 0 && db.RequestedLoad == db.BaselineLoad &&
                recorder.CurrentLog.events.Last(e => e.type == "infection_removed")
                    .sourceConnectionId == plan.ConnectionId,
                "debug removal restores capacity and retains source provenance");
            Check(Parse("spawn product-lookup obj:delivery inf:phantom-load infp:load=6",
                out purpose, out options, out _), "nondefault parameter parse");
            ConnectionSpawnPlan tuned = director.QueueProfile(purpose, options);
            Check(tuned.Operations.Last().InfectionPayload.RequestedLoad == 6f &&
                recorder.CurrentLog.events.Last().definition.operations.Last().infectionLoad == 6f,
                "nondefault parameter propagates into queued plan and recorder");
            host.StepSimulation(0.1f);
            Check(host.Infections.Count == 0, "nondefault payload waits for operation completion");
            StepUntilComplete(host, tuned.ConnectionId);
            Check(host.Infections.Single().Payload.RequestedLoad == 6f &&
                host.Infections.Single().SourceConnectionId == tuned.ConnectionId,
                "nondefault parameter reaches persistent infection through delivery");
            host.ResetSimulation();
            Check(host.Infections.Count == 0 && db.InfectionRequestedLoad == 0f,
                "reset clears persistent state");
        }

        private static void CheckDirectInjectionAndAllocation(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder)
        {
            host.ResetSimulation();
            recorder.BeginRun();
            NodeResources db = Node(host, "DB");
            Check(host.TryInjectPhantomLoad("DB", 8f, out _) &&
                host.RuntimeConnections.Count == 0 && host.Infections.Count == 1,
                "direct debug injection works with zero connections");
            Check(!host.TryInjectPhantomLoad("DB", 3f, out _) &&
                host.Infections.Count == 1 &&
                recorder.CurrentLog.events.Any(e => e.type == "infection_rejected" &&
                    e.source == "debug"), "direct duplicate is recorded and ignored");
            var route = new[] { Node(host, "EXT1").View, Node(host, "API").View,
                db.View };
            foreach (string id in new[] { "ben1", "ben2" })
                director.QueueSpawnPlan(new ConnectionSpawnPlan(host.SimulationTimeSeconds,
                    id, route, "witness", "Service",
                    new[] { new OperationStage(OperationKind.Read, 100f, 5f, "Service") }));
            host.StepSimulation(1f);
            LogicalConnectionState a = host.FindConnection("ben1");
            LogicalConnectionState b = host.FindConnection("ben2");
            Near(a.AllocatedLoad, 1f, "first 5-LOAD connection gets 1");
            Near(b.AllocatedLoad, 1f, "second 5-LOAD connection gets 1");
            Near(a.CompletedWork, 1f, "first WORK follows allocation");
            Near(b.CompletedWork, 1f, "second WORK follows allocation");
            Near(db.RequestedLoad, 18f, "baseline plus infection plus connections");
            Near(db.AllocatedLoad, 10f, "node allocation respects capacity");
            Check(host.Health.Value < 100f && host.Health.DegradationRate > 0f,
                "legitimate slowdown indirectly drains Health");
            recorder.CaptureSnapshot(host.SimulationTimeSeconds);
            NodeSnapshot snapshot = recorder.CurrentLog.snapshots.Last().nodes.Single(n => n.id == "DB");
            Check(snapshot.baselineLoad == 0f && snapshot.infectionRequestedLoad == 8f &&
                snapshot.infectionAllocatedLoad == 8f &&
                snapshot.connectionRequestedLoad == 10f &&
                snapshot.connectionAllocatedLoad == 2f &&
                snapshot.allocatedLoad == 10f,
                "snapshot separates all LOAD contributors");
            Debug.Log($"InfectionWitness 10-capacity example: expected request 18, " +
                $"allocated 10, peers 1/1, WORK 1/1; observed request " +
                $"{db.RequestedLoad:0.###}, allocated {db.AllocatedLoad:0.###}, " +
                $"peers {a.AllocatedLoad:0.###}/{b.AllocatedLoad:0.###}, " +
                $"WORK {a.CompletedWork:0.###}/{b.CompletedWork:0.###}.");
            Check(host.TryRemovePhantomLoad("DB", out _), "debug removal succeeds");
            Near(a.AllocatedLoad, 5f, "first peer recovers after removal");
            Near(b.AllocatedLoad, 5f, "second peer recovers after removal");
            host.StepSimulation(1f);
            Near(a.CompletedWork, 6f, "first WORK recovers after removal");
            Near(b.CompletedWork, 6f, "second WORK recovers after removal");
            Check(!host.TryInjectPhantomLoad("UNKNOWN", 8f, out _) &&
                !host.TryInjectPhantomLoad("DB", 0f, out _),
                "direct injection rejects unknown node and invalid amount");
        }

        private static void CheckBaselinePriority(SimulationHost host,
            ConnectionTrafficDirector director)
        {
            NodeResources db = Node(host, "DB");
            FieldInfo baseline = typeof(NodeResources).GetField("baselineLoad",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float original = (float)baseline.GetValue(db);
            try
            {
                baseline.SetValue(db, 2f);
                host.ResetSimulation();
                Check(host.TryInjectPhantomLoad("DB", 8f, out _),
                    "baseline priority injection");
                director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "base1",
                    new[] { Node(host, "EXT1").View, Node(host, "API").View, db.View },
                    "witness", "Service",
                    new[] { new OperationStage(OperationKind.Read, 100f, 5f, "Service") }));
                host.StepSimulation(1f);
                Near(db.BaselineLoad, 2f, "authored baseline retained");
                Near(db.InfectionAllocatedLoad, 8f, "infection follows baseline");
                Near(host.FindConnection("base1").AllocatedLoad, 0f,
                    "connection receives remaining capacity only");
                Near(db.AllocatedLoad, 10f, "baseline priority capacity bound");
                Debug.Log($"InfectionWitness baseline example: expected baseline 2, " +
                    $"infection 8, peer 0; observed {db.BaselineLoad:0.###}, " +
                    $"{db.InfectionAllocatedLoad:0.###}, " +
                    $"{host.FindConnection("base1").AllocatedLoad:0.###}.");
            }
            finally
            {
                baseline.SetValue(db, original);
                host.ResetSimulation();
            }
        }

        private static void CheckLegacyProfile(SimulationHost host,
            ConnectionTrafficDirector director)
        {
            host.ResetSimulation();
            ConnectionSpawnPlan plan = director.QueueProfile("basic-threat");
            Check(plan.GenerationTrace == null && plan.Operations.Single().InfectionPayload != null &&
                plan.Operations.Single().InfectionPayload.RequestedLoad == 8f,
                "manual legacy threat retains profile semantics with default payload");
            host.StepSimulation(0.1f);
            StepUntilComplete(host, plan.ConnectionId);
            Check(host.Infections.Count == 1 &&
                host.Infections[0].SourceConnectionId == plan.ConnectionId,
                "legacy manual threat delivers through same completion path");
            host.ResetSimulation();
            ConnectionSpawnPlan loader = director.QueueProfile("product-lookup",
                new ConnectionSpawnOverrides(ManualObjective.Loader));
            Check(loader.IsHostile && loader.GenerationTrace.objective == "LOADER" &&
                loader.GenerationTrace.forcedOptions.objective == "LOADER" &&
                loader.GenerationTrace.anomalies.Contains("high-load") &&
                loader.Operations.All(o => o.EffectOnCompletion == CompletionEffect.None),
                "forced loader uses anomaly machinery without delivery");
        }

        private static void StepUntilComplete(SimulationHost host, string id)
        {
            for (int i = 0; i < 1000 && !host.FindConnection(id).IsComplete; i++)
                host.StepSimulation(1f);
            Check(host.FindConnection(id).IsComplete, "carrier completes within witness bound");
        }

        private static string PayloadSignature(ConnectionSpawnPlan plan) =>
            plan.ProfileId + ":" + plan.GenerationTrace.generationDecision + ":" +
            string.Join(",", plan.Operations.Select(o => o.Operation + "/" +
                o.TotalWork.ToString("R") + "/" + o.DesiredLoad.ToString("R") + "/" +
                o.EffectOnCompletion + "/" + o.InfectionPayload?.Kind + "/" +
                o.InfectionPayload?.RequestedLoad.ToString("R")));

        private static bool Parse(string command, out string profile,
            out ConnectionSpawnOverrides options, out string error) =>
            Terminal.TryParseSpawn(command.Split(' '), out profile, out options, out error);

        private static NodeResources Node(SimulationHost host, string id) =>
            host.Nodes.Single(n => n.View.NodeId == id);

        private static void Near(float actual, float expected, string description) =>
            Check(Mathf.Abs(actual - expected) < Tolerance,
                description + $": expected {expected}, observed {actual}");

        private static void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("InfectionWitness: " + description);
        }
    }
}
