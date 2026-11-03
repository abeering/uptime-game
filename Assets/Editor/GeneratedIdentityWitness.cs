using System;
using System.Linq;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class GeneratedIdentityWitness
    {
        private const int PlansPerSeed = 120;
        private static readonly int[] Seeds = { 1701, 2203, 3307, 4409, 5501, 6607 };

        [MenuItem("Tools/Connections/Run Generated Identity Witness")]
        public static void Run()
        {
            foreach (string scene in new[] { "Connections", "ConnectionsAdvanced" })
                CheckScene(scene);
            Debug.Log("GeneratedIdentityWitness PASS: categories, seeded recipe parity, " +
                "metadata, Probe, and long sessions in both Connections scenes.");
        }

        private static void CheckScene(string scene)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var activities = UnityEngine.Object.FindFirstObjectByType<HostileActivityDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && traffic != null && activities != null && recorder != null,
                scene + " services");
            Check(Mathf.Approximately(traffic.AmbientIdentityHopChance, .1f) &&
                Mathf.Approximately(traffic.AmbientIdentityReactiveChance, .1f) &&
                Mathf.Approximately(traffic.IdentityHopIntervalSeconds, 2f),
                scene + " serialized 80/10/10 identity mix and two-second hop");
            Check(scene == "Connections" ?
                activities.OrdinaryWeight == 1f && activities.ReconnaissanceWeight == 0f &&
                activities.PayloadWeight == 0f && activities.WormWeight == 0f &&
                traffic.HostileMeanIntervalSeconds == 54f :
                activities.OrdinaryWeight == 4f && activities.ReconnaissanceWeight == 2f &&
                activities.PayloadWeight == 2f && activities.WormWeight == 1f &&
                traffic.HostileMeanIntervalSeconds == 30f,
                scene + " hostile activity weights and arrival interval unchanged");

            bool oldAuto = traffic.AutoGenerationEnabled;
            bool oldHostile = traffic.HostileGenerationEnabled;
            bool oldRandomize = traffic.RandomizeSeed;
            bool oldRecording = recorder.RecordingEnabled;
            int oldSeed = traffic.Seed;
            float oldHop = traffic.AmbientIdentityHopChance;
            float oldReactive = traffic.AmbientIdentityReactiveChance;
            try
            {
                traffic.AutoGenerationEnabled = false;
                traffic.RandomizeSeed = false;
                recorder.RecordingEnabled = true;
                host.SetRunMode(ConnectionRunMode.Generated);
                int[] direct = new int[3];
                foreach (int seed in Seeds)
                {
                    traffic.Seed = seed;
                    traffic.AmbientIdentityHopChance = 0f;
                    traffic.AmbientIdentityReactiveChance = 0f;
                    host.ResetSimulation();
                    ConnectionSpawnPlan[] baseline = SamplePlans(traffic);
                    Check(baseline.All(p => p != null &&
                        p.IdentityMode == ConnectionIdentityMode.Normal &&
                        p.IdentityHopSeconds == 0f && p.IdentitySeed == 0 &&
                        p.GenerationTrace.identitySelectionEligible &&
                        p.GenerationTrace.identitySelectionMode == "Normal"),
                        scene + " zero-probability compatibility");

                    traffic.Seed = seed;
                    traffic.AmbientIdentityHopChance = .1f;
                    traffic.AmbientIdentityReactiveChance = .1f;
                    host.ResetSimulation();
                    ConnectionSpawnPlan[] varied = SamplePlans(traffic);
                    for (int i = 0; i < varied.Length; i++)
                    {
                        ConnectionSpawnPlan plan = varied[i];
                        Check(RecipeSignature(baseline[i]) == RecipeSignature(plan),
                            scene + " identity stream must not perturb recipe or canonical ID " + i);
                        Check(plan.GenerationTrace.identitySelectionEligible &&
                            plan.GenerationTrace.identitySelectionMode == plan.IdentityMode.ToString() &&
                            plan.GenerationTrace.identitySelectionRoll >= 0f &&
                            plan.GenerationTrace.identitySelectionRoll < 1f,
                            scene + " each ordinary hostile plan records one categorical roll");
                        Check(plan.IdentityMode == ConnectionIdentityMode.Normal ?
                            plan.IdentityHopSeconds == 0f && plan.IdentitySeed == 0 :
                            plan.IdentitySeed != 0 &&
                            plan.IdentityHopSeconds ==
                                (plan.IdentityMode == ConnectionIdentityMode.ContinuousHopping ? 2f : 0f),
                            scene + " concrete mode, seed and timer");
                        direct[(int)plan.IdentityMode]++;
                    }
                    traffic.Seed = seed;
                    host.ResetSimulation();
                    ConnectionSpawnPlan[] replay = SamplePlans(traffic);
                    Check(varied.Zip(replay, (a, b) =>
                        a.IdentityMode == b.IdentityMode && a.IdentitySeed == b.IdentitySeed &&
                        a.GenerationTrace.identitySelectionRoll ==
                            b.GenerationTrace.identitySelectionRoll).All(equal => equal),
                        scene + " seeded identity selection replay");
                    ConnectionSpawnPlan benign = traffic.GenerateBenignPlan(2000f);
                    Check(benign != null && benign.IdentityMode == ConnectionIdentityMode.Normal &&
                        !benign.GenerationTrace.identitySelectionEligible,
                        scene + " benign plans never select identity behavior");
                }
                Check(direct[0] > PlansPerSeed * Seeds.Length * .65f &&
                    direct[1] > 0 && direct[2] > 0 &&
                    direct[0] + direct[1] + direct[2] == PlansPerSeed * Seeds.Length,
                    scene + " normal identity remains the majority; both behaviors selectable");
                Debug.Log($"GeneratedIdentityWitness direct {scene}: ordinary={direct.Sum()}, " +
                    $"normal={direct[0]}, hopping={direct[1]}, reactive={direct[2]}");

                CheckExcludedPlanSources(host, traffic, scene);
                CheckGeneratedProbe(host, traffic, recorder, scene,
                    ConnectionIdentityMode.ContinuousHopping);
                CheckGeneratedProbe(host, traffic, recorder, scene,
                    ConnectionIdentityMode.ReactiveMisdirection);

                int opportunities = 0, ordinary = 0;
                var observed = new int[3];
                traffic.AmbientIdentityHopChance = .1f;
                traffic.AmbientIdentityReactiveChance = .1f;
                traffic.AutoGenerationEnabled = true;
                traffic.HostileGenerationEnabled = true;
                foreach (int seed in Seeds)
                {
                    traffic.Seed = seed;
                    host.ResetSimulation();
                    recorder.BeginRun();
                    host.StepSimulation(900f);
                    RunEvent[] events = recorder.CurrentLog.events.ToArray();
                    opportunities += events.Count(e => e.type == "hostile_opportunity");
                    RunEvent[] generated = events.Where(e => e.type == "plan_queued" &&
                        e.source == "ambient_hostile" && e.generation != null &&
                        e.generation.identitySelectionEligible).ToArray();
                    ordinary += generated.Length;
                    foreach (RunEvent entry in generated)
                    {
                        var mode = (ConnectionIdentityMode)Enum.Parse(
                            typeof(ConnectionIdentityMode), entry.definition.identityMode);
                        observed[(int)mode]++;
                    }
                    Check(recorder.CurrentLog.run.traffic.ambientIdentityHopChance == .1f &&
                        recorder.CurrentLog.run.traffic.ambientIdentityReactiveChance == .1f,
                        scene + " effective probabilities in run metadata");
                    recorder.EndRun();
                }
                Check(opportunities > ordinary && scene == "ConnectionsAdvanced" ||
                    scene == "Connections" && opportunities == ordinary,
                    scene + " ordinary connection category is distinct from all opportunities");
                Check(observed.Sum() == ordinary && ordinary > 0,
                    scene + " long-session mode counts match ordinary plans");
                Debug.Log($"GeneratedIdentityWitness sessions {scene}: seeds={Seeds.Length}, " +
                    $"secondsPerSeed=900, opportunities={opportunities}, ordinary={ordinary}, " +
                    $"normal={observed[0]}, hopping={observed[1]}, reactive={observed[2]}, " +
                    $"hoppingAll={100f * observed[1] / opportunities:F1}%, " +
                    $"reactiveAll={100f * observed[2] / opportunities:F1}%");
                Debug.Log("GeneratedIdentityWitness PASS: " + scene);
            }
            finally
            {
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                traffic.Seed = oldSeed;
                traffic.AmbientIdentityHopChance = oldHop;
                traffic.AmbientIdentityReactiveChance = oldReactive;
                traffic.AutoGenerationEnabled = oldAuto;
                traffic.HostileGenerationEnabled = oldHostile;
                traffic.RandomizeSeed = oldRandomize;
            }
        }

        private static ConnectionSpawnPlan[] SamplePlans(ConnectionTrafficDirector traffic) =>
            Enumerable.Range(0, PlansPerSeed)
                .Select(i => traffic.GenerateHostilePlan(i * 9f)).ToArray();

        private static string RecipeSignature(ConnectionSpawnPlan plan)
        {
            ConnectionGenerationTrace trace = plan.GenerationTrace.Copy();
            trace.identitySelectionEligible = false;
            trace.identitySelectionRoll = 0f;
            trace.identitySelectionMode = null;
            return plan.ConnectionId + "|" + plan.ProfileId + "|" +
                string.Join(">", plan.Route.Select(node => node.NodeId)) + "|" +
                string.Join(";", plan.Operations.Select(stage => stage.Operation + ":" +
                    stage.DesiredLoad.ToString("R") + ":" +
                    stage.TotalWork.ToString("R") + ":" +
                    stage.EffectOnCompletion + ":" +
                    stage.InfectionPayload?.Kind)) + "|" + JsonUtility.ToJson(trace);
        }

        private static void CheckExcludedPlanSources(SimulationHost host,
            ConnectionTrafficDirector traffic, string scene)
        {
            traffic.Seed = 1701;
            traffic.AmbientIdentityHopChance = 1f;
            traffic.AmbientIdentityReactiveChance = 0f;
            host.ResetSimulation();
            ConnectionSpawnPlan scan = traffic.GeneratePortScanPlan(0f, 1f, 20f, 20f);
            ConnectionSpawnPlan followup = traffic.GenerateTargetedInfectorPlan(0f, "DB");
            ConnectionSpawnPlan witness = traffic.GenerateLoaderPlanForWitness(0f,
                LoaderStructure.Single, LoaderPattern.Immediate);
            ConnectionSpawnPlan manual = traffic.QueueProfile("basic-threat");
            Check(scan != null && followup != null && witness != null && manual != null &&
                new[] { scan, followup, witness, manual }.All(plan =>
                    plan.IdentityMode == ConnectionIdentityMode.Normal &&
                    (plan.GenerationTrace == null ||
                     !plan.GenerationTrace.identitySelectionEligible)),
                scene + " recon, follow-up, witness, and manual paths excluded");
            var infection = new NodeInfection(host.Nodes.Single(n => n.View.NodeId == "DB"),
                new InfectionPayload(InfectionKind.Spawner, 0f, 20f), 0f, "source", 0);
            ConnectionSpawnPlan child = traffic.QueueSpawnerChild(infection, 0f, out _);
            Check(child != null && child.IdentityMode == ConnectionIdentityMode.Normal &&
                !child.GenerationTrace.identitySelectionEligible,
                scene + " Spawner children excluded");
        }

        private static void CheckGeneratedProbe(SimulationHost host,
            ConnectionTrafficDirector traffic, ConnectionsRunRecorder recorder,
            string scene, ConnectionIdentityMode mode)
        {
            traffic.Seed = 3307;
            traffic.AmbientIdentityHopChance = mode == ConnectionIdentityMode.ContinuousHopping ?
                1f : 0f;
            traffic.AmbientIdentityReactiveChance = mode ==
                ConnectionIdentityMode.ReactiveMisdirection ? 1f : 0f;
            host.ResetSimulation();
            recorder.BeginRun();
            ConnectionSpawnPlan plan = traffic.GenerateHostilePlan(0f);
            traffic.QueueSpawnPlan(plan, "ambient_hostile");
            Check(plan.IdentityMode == mode && plan.IsHostile &&
                recorder.CurrentLog.events.Any(e => e.type == "plan_queued" &&
                    e.connectionId == plan.ConnectionId &&
                    e.generation.identitySelectionMode == mode.ToString() &&
                    e.definition.identityMode == mode.ToString()),
                scene + " generated mode in queued plan and recorder");
            host.StepSimulation(.01f);
            LogicalConnectionState state = host.FindConnection(plan.ConnectionId);
            string alias = state.KnownIdentity;
            Check(state.IsConnecting && alias != plan.ConnectionId &&
                host.IsLiveAlias(alias), scene + " generated alias begins when Connecting");
            bool collisionRejected = false;
            try
            {
                traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, alias, plan.Route,
                    "collision-test", "Collision", new[] {
                        new OperationStage(OperationKind.Read, 20f, 1f, "Read") }));
            }
            catch (InvalidOperationException) { collisionRejected = true; }
            Check(collisionRejected, scene + " generated alias reserves its live label");
            NodeView a = plan.Route[plan.Route.Length - 2];
            NodeView b = plan.Route[plan.Route.Length - 1];
            PhysicalLinkView edge = host.PhysicalLinks.Select(link => link.View).Single(link =>
                link.NodeA == a && link.NodeB == b || link.NodeA == b && link.NodeB == a);
            host.StepSimulation(host.ConnectionTravelSecondsPerLink *
                (plan.Route.Length - 1) + .02f);
            Check(state.IsActive && host.TryActivateProbe(edge.LinkId,
                "probe " + edge.LinkId, out _) && state.KnownIdentity == plan.ConnectionId,
                scene + " Probe reveals naturally generated " + mode);
            if (mode == ConnectionIdentityMode.ReactiveMisdirection)
            {
                host.NotifyFailedIdentityAddress(alias, "kill");
                Check(state.Identity.AliasIdentity == alias,
                    scene + " hidden reactive alias cannot change during Probe");
            }
            host.StepSimulation(host.ProbeActiveRemaining);
            Check(!state.IsComplete && state.KnownIdentity != plan.ConnectionId &&
                state.KnownIdentity == state.Identity.AliasIdentity,
                scene + " generated alias returns on Probe expiry");
            recorder.EndRun();
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Generated Identity Witness: " + message);
        }
    }
}
