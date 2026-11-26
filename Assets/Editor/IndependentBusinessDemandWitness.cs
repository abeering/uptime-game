using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class IndependentBusinessDemandWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsIndependentBusinessDemandV0";
        private static readonly List<string> checks = new(), rows = new(), outcomes = new();
        private static void Check(bool ok, string label)
        { checks.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); }
        private static void Set(object target, string field, object value) => FragilityWitness.Set(target, field, value);
        public static SimulationHost Open(string scene)
        {
            var h = BusinessShiftWitness.Open(scene, 1000000, 10000);
            var t = h.GetComponent<ConnectionTrafficDirector>();
            t.AutoGenerationEnabled = true; t.HostileGenerationEnabled = false;
            h.ResetSimulation(); h.Recorder.BeginRun(); return h;
        }
        private static string Digest(string value) => BitConverter.ToString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        private static string[] Schedule(SimulationHost h) => h.Recorder.CurrentLog.events.Where(e => e.type == "business_demand_arrived")
            .Select(e => e.demandSequence + ":" + e.demandDueAtSeconds.ToString("R", CultureInfo.InvariantCulture) + ":" + e.demandIntervalSeconds.ToString("R", CultureInfo.InvariantCulture)).ToArray();
        private static string[] Recipes(SimulationHost h) => h.Recorder.CurrentLog.events.Where(e => e.type == "plan_queued" && e.source == "independent_business")
            .Select(e => e.connectionId + ":" + JsonUtility.ToJson(e.definition) + ":" + JsonUtility.ToJson(e.generation)).ToArray();
        private static SimulationHost Scenario(string scene, string condition, bool hostile = false)
        {
            var h = Open(scene); var t = h.GetComponent<ConnectionTrafficDirector>();
            if (hostile)
            {
                t.HostileGenerationEnabled = true; Set(t, "deliveryChance", 0f);
                var activity = h.GetComponent<HostileActivityDirector>();
                activity.OrdinaryWeight = 1; activity.ReconnaissanceWeight = activity.PayloadWeight = activity.WormWeight =
                    activity.PayloadLauncherWeight = activity.DirectDuelWeight = activity.ConnectionDuelWeight = 0;
            }
            foreach (var n in h.Nodes)
            {
                Set(n, "baselineLoad", condition == "congested" ? 9.9f : 0f);
                Set(n, "loadCapacity", hostile ? 10f : condition == "healthy" ? 1000f : 10f);
            }
            h.ResetSimulation(); h.Recorder.BeginRun();
            if (condition == "outage")
                foreach (var n in h.Nodes.Where(n => t.IsInternalNode(n.View))) h.TryInjectInfection(n.View.NodeId, InfectionKind.Blackout, 0, out _);
            for (int i = 0; i < 6000 && !h.RunEnded; i++)
            {
                h.StepSimulation(.02f);
                if (condition == "kill") foreach (var c in h.RuntimeConnections.Where(c => !c.IsHostile && (c.IsConnecting || c.IsActive)).ToArray()) h.KillConnection(c.ConnectionId);
            }
            return h;
        }
        private static void Retain(SimulationHost h, string condition)
        {
            var log = h.Recorder.CurrentLog; string scene = h.gameObject.scene.name;
            foreach (var arrival in log.events.Where(e => e.type == "business_demand_arrived"))
            {
                var plan = log.events.FirstOrDefault(e => e.type == "plan_queued" && e.connectionId == arrival.connectionId);
                rows.Add(FormattableString.Invariant($"{scene},{condition},{arrival.demandSequence},{arrival.demandDueAtSeconds:R},{arrival.demandIntervalSeconds:R},{arrival.connectionId},{plan?.profileId},{plan?.definition.target},{plan?.definition.businessWorkValue},{(plan == null ? "" : Digest(JsonUtility.ToJson(plan.definition) + JsonUtility.ToJson(plan.generation)))}"));
            }
            var resolutions = log.events.Where(e => e.type == "connection_resolved").ToArray();
            outcomes.Add(FormattableString.Invariant($"{scene},{condition},{h.SimulationTimeSeconds},{h.Recorder.CurrentLog.events.Count(e => e.type == "connection_spawned")},{h.GetComponent<ConnectionTrafficDirector>().BusinessDemand.arrived},{h.GetComponent<ConnectionTrafficDirector>().BusinessDemand.admitted},{h.LegitimateFailureCount},{h.CreditedWork},{resolutions.Count(e => e.resolutionReason == "Completed")},{resolutions.Count(e => e.resolutionReason == "TimedOut")},{resolutions.Count(e => e.resolutionReason == "Killed")},{resolutions.Count(e => e.resolutionReason == "Canceled")}"));
        }
        [MenuItem("Tools/Connections/Run Independent Business Demand Witness")]
        public static void Run()
        {
            checks.Clear(); rows.Clear(); outcomes.Clear(); Directory.CreateDirectory(Evidence);
            rows.Add("scene,scenario,sequence,ideal_due_seconds,next_interval_seconds,id,purpose,target,business_work,complete_recipe_sha256");
            outcomes.Add("scene,scenario,observed_seconds,materialized_jobs,arrived,admitted,business_failures,credited_work,completed,timed_out,killed,canceled");
            try
            {
                foreach (string scene in CanonicalSceneAuthoring.Scenes)
                {
                    EditorSceneManager.OpenScene(scene); var authored = UnityEngine.Object.FindFirstObjectByType<SimulationHost>(); var traffic = authored.GetComponent<ConnectionTrafficDirector>();
                    Check(traffic.LegitimateWorkloadMultiplier == 2.5f && traffic.LegitimateMeanArrivalSeconds == (scene == CanonicalSceneAuthoring.Scenes[0] ? 23.225807f : 18.947369f) &&
                        authored.ShiftEnabled && authored.BusinessFailureLimit == 10 && authored.ShiftWorkTarget == (scene == CanonicalSceneAuthoring.Scenes[0] ? 1000 : 1500), scene + " accepted authored rates, workload and shift objectives");
                    var baseline = Scenario(scene, "healthy"); var schedule = Schedule(baseline); var recipes = Recipes(baseline); Retain(baseline, "healthy");
                    Check(schedule.Length > 1 && baseline.WorkCreditCount > 0, scene + " healthy real lifecycle completes jobs and earns business WORK");
                    foreach (string condition in new[] { "congested", "kill", "outage" })
                    {
                        var h = Scenario(scene, condition); Retain(h, condition);
                        Check(schedule.SequenceEqual(Schedule(h)) && recipes.SequenceEqual(Recipes(h)), scene + " identical ideal schedules and complete recipes through 120s: healthy vs " + condition);
                        Check((condition == "congested" ? h.RuntimeConnections.Any(c => c.IsDemanding && c.AllocatedLoad < c.DesiredLoad) : h.LegitimateFailureCount > 0) && h.BusinessFailures.Count == h.LegitimateFailureCount, scene + " actual congestion/interruption with existing resolution ledger: " + condition);
                        var d = h.GetComponent<ConnectionTrafficDirector>().BusinessDemand;
                        Check(d.arrived == d.admitted && d.waiting == 0 && d.unadmitted == 0 &&
                            h.Recorder.CurrentLog.events.Count(e => e.type == "business_demand_admitted") == d.admitted,
                            scene + " concrete queued registrations admitted even under " + condition);
                    }
                    var replay = Scenario(scene, "healthy");
                    Check(schedule.SequenceEqual(Schedule(replay)) && recipes.SequenceEqual(Recipes(replay)), scene + " reset/same seed exact replay");
                    Check(replay.Recorder.CurrentLog.run.traffic.legitimateArrivalPolicy == "IndependentBusinessDemand" &&
                        replay.Recorder.CurrentLog.run.legitimateWorkloadMultiplier == 2.5f, scene + " effective recorder policy/mean/workload");
                    var due = replay.Recorder.CurrentLog.events.Where(e => e.type == "business_demand_arrived").Select(e => e.demandIntervalSeconds).ToArray();
                    Check(due.Distinct().Count() > 1, scene + " stochastic waits retain bursts/lulls rather than fixed cadence");
                    double firstDue = replay.Recorder.CurrentLog.events.First(e => e.type == "business_demand_arrived").demandDueAtSeconds;
                    var replayTraffic = replay.GetComponent<ConnectionTrafficDirector>(); replay.ResetSimulation();
                    Check(replayTraffic.BusinessDemand.arrived == 0 && replayTraffic.BusinessDemand.admitted == 0 &&
                        replayTraffic.BusinessDemand.waiting == 0 && replayTraffic.BusinessDemand.unadmitted == 0 &&
                        replayTraffic.BusinessDemand.nextArrivalAtSeconds == firstDue && replayTraffic.Seed == 1701,
                        scene + " reset of previously active host clears counters and restores the same first due time/seed");
                    FailureCases(scene);
                }
                var normal = Scenario(CanonicalSceneAuthoring.Scenes[0], "normal", true);
                string[] hostileTimes = normal.Recorder.CurrentLog.events.Where(e => e.type == "hostile_opportunity").Select(e => e.scheduledSpawnTime + ":" + e.nextHostileIntervalSeconds).ToArray();
                string[] hostileRecipes = normal.Recorder.CurrentLog.events.Where(e => e.type == "plan_queued" && e.source == "ambient_hostile").Select(e => JsonUtility.ToJson(e.definition) + JsonUtility.ToJson(e.generation)).ToArray();
                var killed = Scenario(CanonicalSceneAuthoring.Scenes[0], "kill", true);
                Check(hostileTimes.Length > 0 && hostileTimes.SequenceEqual(killed.Recorder.CurrentLog.events.Where(e => e.type == "hostile_opportunity").Select(e => e.scheduledSpawnTime + ":" + e.nextHostileIntervalSeconds)) &&
                    hostileRecipes.SequenceEqual(killed.Recorder.CurrentLog.events.Where(e => e.type == "plan_queued" && e.source == "ambient_hostile").Select(e => JsonUtility.ToJson(e.definition) + JsonUtility.ToJson(e.generation))),
                    "ordinary hostile clock and complete recipes unchanged at equivalent .02s boundaries/capacities when legitimate jobs are killed");
            }
            finally { File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks); File.WriteAllLines(Evidence + "/paired-demand.csv", rows); File.WriteAllLines(Evidence + "/outcomes.csv", outcomes); }
            Debug.Log("IndependentBusinessDemandWitness PASS: " + checks.Count + " assertions; bounded 120s comparisons at one seed, not balance calibration.");
        }
        private static void FailureCases(string scene)
        {
            var h = Open(scene); var t = h.GetComponent<ConnectionTrafficDirector>();
            Set(t, "ambientWeightOverrides", t.Profiles.Where(p => p.purposeTemplate).Select(p => new AmbientWeightOverride { profileId = p.id, weight = 0 }).ToArray());
            h.ResetSimulation(); h.Recorder.BeginRun(); Set(t, "nextBusinessDemandAt", 0d); h.StepSimulation(.02f);
            Check(t.BusinessDemand.arrived == 1 && t.BusinessDemand.unadmitted == 1 && t.BusinessDemand.admitted == 0 && h.LegitimateFailureCount == 0 &&
                h.Recorder.CurrentLog.events.Any(e => e.type == "business_demand_unadmitted" && e.reason == "no_weighted_purpose"), scene + " no-purpose generation failure recorded honestly, not a fabricated connection failure");
            h = Open(scene); t = h.GetComponent<ConnectionTrafficDirector>(); Set(t, "nextIdIndex", 260); Set(t, "nextBusinessDemandAt", 0d); h.StepSimulation(.02f);
            Set(t, "nextBusinessDemandAt", (double)h.SimulationTimeSeconds); h.StepSimulation(.02f);
            Check(t.BusinessDemand.arrived == 2 && t.BusinessDemand.unadmitted == 2 && t.BusinessDemand.waiting == 0 &&
                h.Recorder.CurrentLog.events.Count(e => e.type == "business_demand_unadmitted" && e.reason == "ids_exhausted") == 2,
                scene + " finite IDs do not stop demand clock or invent a waiting queue");
            h = Open(scene); t = h.GetComponent<ConnectionTrafficDirector>();
            Set(t, "ambientWeightOverrides", t.Profiles.Where(p => p.purposeTemplate).Select(p => new AmbientWeightOverride { profileId = p.id, weight = p.id == "product-lookup" ? 1 : 0 }).ToArray());
            h.ResetSimulation(); h.Recorder.BeginRun();
            t.Profiles.Single(p => p.id == "product-lookup").operations[0].meanWork = 0;
            Set(t, "nextBusinessDemandAt", 0d); h.StepSimulation(.02f);
            Check(t.BusinessDemand.arrived == 1 && t.BusinessDemand.admitted == 0 && t.BusinessDemand.unadmitted == 1 &&
                h.Recorder.CurrentLog.events.Any(e => e.type == "business_demand_unadmitted" && e.reason.StartsWith("queue_failed:")),
                scene + " invalid concrete plan records queue rejection without phantom admission");
            h = Open(scene); t = h.GetComponent<ConnectionTrafficDirector>(); t.AutoGenerationEnabled = false; h.StepSimulation(120);
            Check(t.BusinessDemand.arrived == 0, scene + " explicit ambient disable creates no demand");
            t.AutoGenerationEnabled = true; h.StepSimulation(.02f); Check(t.BusinessDemand.arrived == 0 && t.BusinessDemand.nextArrivalAtSeconds > h.SimulationTimeSeconds,
                scene + " re-enable samples fresh wait, no disabled-period catch-up");
            h = Open(scene); t = h.GetComponent<ConnectionTrafficDirector>(); h.Recorder.RecordingEnabled = false; h.StepSimulation(60);
            Check(t.BusinessDemand.arrived > 0 && t.BusinessDemand.arrived == t.BusinessDemand.admitted, scene + " authoritative demand works without recording");
            foreach (bool fail in new[] { false, true })
            {
                h = Open(scene); t = h.GetComponent<ConnectionTrafficDirector>();
                Set(h, "shiftWorkTarget", .01f); Set(h, "businessFailureLimit", 1);
                foreach (var n in h.Nodes) Set(n, "loadCapacity", 1000f);
                h.ResetSimulation(); h.Recorder.BeginRun(); Set(t, "nextBusinessDemandAt", 0d);
                if (fail) foreach (var n in h.Nodes.Where(n => t.IsInternalNode(n.View))) h.TryInjectInfection(n.View.NodeId, InfectionKind.Blackout, 0, out _);
                h.StepSimulation(400); int count = t.BusinessDemand.arrived; float time = h.SimulationTimeSeconds; h.StepSimulation(400);
                Check((fail ? h.Ending == RunEnding.Fired && h.CreditedWork == 0 && h.LegitimateFailureCount == 1 : h.Ending == RunEnding.ShiftComplete && h.CreditedWork > 0 && h.LegitimateFailureCount == 0) &&
                    h.SimulationTimeSeconds == time && t.BusinessDemand.arrived == count && !h.Recorder.IsRecording,
                    scene + " real independent demand reaches " + (fail ? "FIRED" : "SHIFT COMPLETE") + " and freezes clock/recorder exactly once");
            }
        }
        public static void Regressions()
        {
            Directory.CreateDirectory(Evidence); var report = new List<string>();
            foreach (var test in new (string name, Action run)[] { ("IndependentDemand", Run), ("Workload", LegitimateWorkloadWitness.Run),
                ("Shift", BusinessShiftWitness.Run), ("Fragility", FragilityWitness.Run), ("Traffic", TrafficWitness.Run), ("Recorder", RecorderWitness.Run), ("Canonical", CanonicalSceneWitness.Run) })
            { try { test.run(); report.Add("PASS " + test.name); } catch (Exception e) { report.Add("FAIL " + test.name + ": " + e.Message); Debug.LogException(e); } }
            File.WriteAllLines(Evidence + "/regression-checks.txt", report);
            if (report.Any(x => x.StartsWith("FAIL"))) throw new Exception("Demand regression failures; see report.");
        }
    }
}
