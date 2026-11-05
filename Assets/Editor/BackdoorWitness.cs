using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class BackdoorWitness
    {
        [MenuItem("Tools/Connections/Run Backdoor Witness")]
        public static void Run()
        {
            CheckScene("Assets/Scenes/Connections.unity", 0f, 0f);
            CheckScene("Assets/Scenes/ConnectionsAdvanced.unity", 1f, .25f);
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var presenter = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            traffic.AutoGenerationEnabled = false;
            traffic.RandomizeSeed = false;
            host.SetRunMode(ConnectionRunMode.Generated);
            typeof(SimulationHost).GetField("connectionTravelSecondsPerLink",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(host, .000001f);
            var db = host.FindNodeResources("DB");
            var edge = new[] { host.FindNodeResources("API").View, db.View };

            RunRace(host, traffic, db, edge, false);
            RunRace(host, traffic, db, edge, true);
            RunDynamicRates(host, traffic, db, edge);

            host.ResetSimulation();
            recorder.BeginRun();
            Queue(traffic, edge, "arc", true, 6f, 100f, true);
            host.StepSimulation(.01f);
            var arc = host.FindConnection("arc");
            host.ApplyCondition(db, new ConditionPayload(ConditionKind.Backdoor, 45f),
                sourceWormBranchId: "witness.1");
            Check(arc.EffectiveWorkRateMultiplier == 2f && arc.ActualWorkRate == 12f,
                "first operation accelerates");
            float initialArcWork = arc.CompletedWork;
            host.StepSimulation(4f);
            Near(arc.CompletedWork - initialArcWork, 48f, "accelerated first operation");
            Check(host.TryInjectInfection("DB", InfectionKind.Backdoor, 0f, out _),
                "persistent installation");
            Check(!host.TryInjectInfection("DB", InfectionKind.Backdoor, 0f, out _),
                "same-kind duplicate rejected");
            Check(arc.EffectiveWorkRateMultiplier == 2f, "two sources do not stack");
            host.StepSimulation(5f);
            Check(arc.CurrentOperationIndex == 1 && arc.EffectiveWorkRateMultiplier == 2f,
                "second operation accelerates");
            presenter.RenderNow(1f, 1f);
            var status = presenter.StateFor(db.View);
            Check(status.Backdoor && !status.KnownInfected &&
                presenter.ViewFor(db.View).PainVisible, "overt condition and covert infection");
            Check(host.TryRemoveInfection("DB", InfectionKind.Backdoor, out _) &&
                arc.EffectiveWorkRateMultiplier == 2f, "condition survives infection removal");
            host.StepSimulation(36f);
            Check(host.ConditionFor(db.View, ConditionKind.Backdoor) == null &&
                arc.EffectiveWorkRateMultiplier == 1f, "expiry restores rate");
            Check(recorder.CurrentLog.events.Any(e => e.type == "condition_applied" &&
                e.sourceWormBranchId == "witness.1") &&
                recorder.CurrentLog.events.Any(e => e.type == "condition_expired") &&
                recorder.CurrentLog.events.Any(e => e.type == "infection_rejected"),
                "condition and infection receipts");
            Check(Terminal.TryParseWormEffect("backdoor", out WormEffect effect) &&
                effect == WormEffect.Backdoor &&
                Terminal.TryParseInfectionKind("backdoor", out InfectionKind kind) &&
                kind == InfectionKind.Backdoor, "developer commands parse");
            Debug.Log("BackdoorWitness PASS: 10 capacity / 6+6 LOAD race, 5 vs 10 WORK/s, " +
                "kill at 12s, multi-operation rate, coexistence, expiry, presentation, receipts.");
        }

        private static void RunRace(SimulationHost host, ConnectionTrafficDirector traffic,
            NodeResources db, NodeView[] edge, bool backdoor)
        {
            host.ResetSimulation();
            if (backdoor)
                host.ApplyCondition(db, new ConditionPayload(ConditionKind.Backdoor, 45f));
            Queue(traffic, edge, "benign", false, 6f, 200f, false);
            Queue(traffic, edge, "infector", true, 6f, 100f, false);
            host.StepSimulation(.01f);
            var benign = host.FindConnection("benign");
            var infector = host.FindConnection("infector");
            Near(benign.AllocatedLoad, 5f, "benign allocated LOAD");
            Near(infector.AllocatedLoad, 5f, "infector allocated LOAD");
            Near(infector.DesiredLoad, 6f, "infector requested LOAD");
            Near(infector.CurrentTotalWork, 100f, "required WORK");
            Near(infector.ActualWorkRate, backdoor ? 10f : 5f, "actual WORK rate");
            Near(benign.ActualWorkRate, 5f, "benign WORK rate stays unchanged");
            host.StepSimulation(12f);
            Check(host.KillConnection("infector") == !backdoor,
                "kill before delivery only without BACKDOOR");
            Check(host.Infections.Any(i => i.Payload.Kind == InfectionKind.Backdoor) == backdoor,
                "installation outcome persists after kill window");
        }

        private static void RunDynamicRates(SimulationHost host,
            ConnectionTrafficDirector traffic, NodeResources db, NodeView[] route)
        {
            host.ResetSimulation();
            Queue(traffic, route, "dynamic", true, 6f, 1000f, false);
            host.StepSimulation(.01f);
            var carrier = host.FindConnection("dynamic");
            host.StepSimulation(2f);
            float before = carrier.CompletedWork;
            host.ApplyCondition(db, new ConditionPayload(ConditionKind.Backdoor, 5f));
            Near(carrier.ActualWorkRate, 12f, "mid-operation activation rate");
            host.StepSimulation(10f);
            Near(carrier.CompletedWork - before, 90f,
                "coarse step splits 5 seconds accelerated and 5 seconds normal");
            Near(carrier.ActualWorkRate, 6f, "expiry restores active carrier rate");

            host.ResetSimulation();
            host.ApplyCondition(db, new ConditionPayload(ConditionKind.Backdoor, 45f));
            traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "other-hostile", route,
                "backdoor-witness", "Service", new[] {
                    new OperationStage(OperationKind.Read, 1000f, 6f, "Reconnaissance")
                }, true));
            host.StepSimulation(.01f);
            Near(host.FindConnection("other-hostile").EffectiveWorkRateMultiplier, 1f,
                "hostile without delivery is ineligible");

            host.ResetSimulation();
            FieldInfo baseline = typeof(NodeResources).GetField("baselineLoad",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float previousBaseline = (float)baseline.GetValue(db);
            try
            {
                baseline.SetValue(db, db.LoadCapacity);
                host.ApplyCondition(db, new ConditionPayload(ConditionKind.Backdoor, 45f));
                Queue(traffic, route, "blocked", true, 6f, 1000f, false);
                host.StepSimulation(1f);
                var blocked = host.FindConnection("blocked");
                Near(blocked.AllocatedLoad, 0f, "zero allocation");
                Near(blocked.CompletedWork, 0f, "zero allocation prevents WORK");
            }
            finally { baseline.SetValue(db, previousBaseline); }

            host.ResetSimulation();
            host.ApplyCondition(db, new ConditionPayload(ConditionKind.Backdoor, 45f));
            traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "paused", route,
                "backdoor-witness", "Service", new[] {
                    new OperationStage(OperationKind.Write, 1000f, 6f, "Paused delivery",
                        CompletionEffect.InfectionDelivery,
                        new[] { TimedModifierBehaviorDefinition.Rate("PAUSE", 0f, 0f, 0f) },
                        new InfectionPayload(InfectionKind.Backdoor, 0f, 0f))
                }, true));
            host.StepSimulation(1f);
            var paused = host.FindConnection("paused");
            Near(paused.AllocatedLoad, 6f, "paused carrier still receives LOAD");
            Near(paused.ActualWorkRate, 0f, "zero WORK factor survives BACKDOOR");
            Near(paused.CompletedWork, 0f, "zero WORK factor prevents progress");
        }

        private static void Queue(ConnectionTrafficDirector traffic, NodeView[] route,
            string id, bool hostile, float load, float work, bool arc)
        {
            var delivery = new OperationStage(OperationKind.Write, arc ? 1000f : work, load,
                "Delivery", hostile ? CompletionEffect.InfectionDelivery : CompletionEffect.None,
                payload: hostile ? new InfectionPayload(InfectionKind.Backdoor, 0f, 0f) : null);
            var operations = arc ? new[] {
                new OperationStage(OperationKind.Read, 100f, load, "Preparation"), delivery
            } : new[] { delivery };
            traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, id, route, "backdoor-witness",
                "Service", operations, hostile));
        }

        private static void CheckScene(string path, float infectionWeight, float wormChance)
        {
            EditorSceneManager.OpenScene(path);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var activity = UnityEngine.Object.FindFirstObjectByType<HostileActivityDirector>();
            Check(host.BackdoorWorkMultiplier == 2f &&
                traffic.BackdoorWeight == infectionWeight &&
                activity.BackdoorWormChance == wormChance &&
                host.Worms.DefaultTuning.backdoorConditionSeconds == 45f,
                path + " BACKDOOR tuning");
        }

        private static void Near(float observed, float expected, string label) =>
            Check(Mathf.Abs(observed - expected) < .01f,
                label + ": expected " + expected + ", observed " + observed);
        private static void Check(bool okay, string label)
        {
            if (!okay) throw new InvalidOperationException("BackdoorWitness: " + label);
        }
    }

    public static class BackdoorAmbientWitness
    {
        [MenuItem("Tools/Connections/Run Backdoor Ambient Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            traffic.RandomizeSeed = false;
            foreach (int seed in new[] { 42, 101, 1701, 9001 })
            {
                traffic.Seed = seed;
                host.SetRunMode(ConnectionRunMode.Generated);
                recorder.BeginRun();
                var accelerated = new System.Collections.Generic.HashSet<string>();
                for (int second = 0; second < 600; second++)
                {
                    host.StepSimulation(1f);
                    foreach (LogicalConnectionState connection in host.RuntimeConnections)
                        if (connection.IsActive &&
                            connection.EffectiveWorkRateMultiplier > 1.01f &&
                            Enumerable.Range(0, connection.OperationCount).Any(index =>
                                connection.Definition.EffectAt(index) ==
                                CompletionEffect.InfectionDelivery))
                            accelerated.Add(connection.ConnectionId);
                }
                var events = recorder.CurrentLog.events;
                int worms = events.Count(e => e.type == "hostile_activity_selected" &&
                    e.wormEffect == "Backdoor");
                int conditions = events.Count(e => e.type == "condition_applied" &&
                    e.conditionKind == "Backdoor");
                int plans = events.Count(e => e.type == "plan_queued" &&
                    e.definition?.operations != null && e.definition.operations.Any(o =>
                        o.infectionKind == "Backdoor"));
                int installed = events.Count(e => e.type == "infection_applied" &&
                    e.infectionKind == "Backdoor");
                int rejected = events.Count(e => e.type == "infection_rejected" &&
                    e.infectionKind == "Backdoor");
                Debug.Log($"BACKDOOR AMBIENT seed={seed} worms={worms} conditions={conditions} " +
                    $"plans={plans} installed={installed} rejected={rejected} " +
                    $"acceleratedConnections={accelerated.Count}");
                recorder.EndRun();
            }
        }
    }
}
