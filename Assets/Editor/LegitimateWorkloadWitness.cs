using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class LegitimateWorkloadWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsLegitimateWorkloadV0";
        private static readonly List<string> checks = new();
        private static readonly List<string> samples = new();
        private static void Check(bool ok, string label)
        { checks.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); }
        private static bool Near(double a, double b) => Math.Abs(a - b) <= .0001 * Math.Max(1, Math.Abs(b));
        private static SimulationHost Open(string scene, float multiplier)
        {
            var h = BusinessShiftWitness.Open(scene);
            FragilityWitness.Set(h.GetComponent<ConnectionTrafficDirector>(), "legitimateWorkloadMultiplier", multiplier);
            h.ResetSimulation(); h.Recorder.BeginRun(); return h;
        }
        private static ConnectionSpawnPlan[] Plans(string scene, float multiplier, float outliers, float waving)
        {
            var h = Open(scene, multiplier); var t = h.GetComponent<ConnectionTrafficDirector>();
            FragilityWitness.Set(t, "benignOutlierChance", outliers);
            FragilityWitness.Set(t, "benignWavingOutlierChance", waving); h.ResetSimulation();
            var plans = new List<ConnectionSpawnPlan>();
            // Bounded recipe comparisons only, not an ambient calibration or simulation matrix.
            for (int i = 0; i < 12; i++) { plans.Add(t.GenerateBenignPlan(i)); plans.Add(t.GenerateHostilePlan(i)); }
            return plans.ToArray();
        }
        [MenuItem("Tools/Connections/Run Legitimate Workload Witness")]
        public static void Run()
        {
            checks.Clear(); samples.Clear(); Directory.CreateDirectory(Evidence);
            samples.Add("scene,case,id,hostile,operation,base_load,scaled_load,base_work,scaled_work,base_nominal,scaled_nominal,base_business,scaled_business");
            try
            {
                foreach (string scene in CanonicalSceneAuthoring.Scenes)
                {
                    EditorSceneManager.OpenScene(scene); var authored = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                    Check(authored.GetComponent<ConnectionTrafficDirector>().LegitimateWorkloadMultiplier == 1.5f && authored.ShiftEnabled &&
                        authored.BusinessFailureLimit == 10 && authored.ShiftWorkTarget == (scene == CanonicalSceneAuthoring.Scenes[0] ? 1000 : 1500), scene + " authored 1.5x and accepted objectives");
                    foreach (var mode in new[] { ("normal", 0f, 0f), ("static-keyword-outliers", 1f, 0f), ("waving-outliers", 1f, 1f) })
                    {
                        var baseline = Plans(scene, 1, mode.Item2, mode.Item3); var scaled = Plans(scene, 1.5f, mode.Item2, mode.Item3);
                        for (int n = 0; n < baseline.Length; n++)
                        {
                            var a = baseline[n]; var b = scaled[n]; double factor = a.IsHostile ? 1 : 1.5;
                            Check(a.ConnectionId == b.ConnectionId && a.ProfileId == b.ProfileId && a.IsHostile == b.IsHostile &&
                                a.ScheduledSpawnTime == b.ScheduledSpawnTime && a.Route.Select(x => x.NodeId).SequenceEqual(b.Route.Select(x => x.NodeId)) &&
                                a.IdentityMode == b.IdentityMode && a.IdentitySeed == b.IdentitySeed && a.NaturalLoadSeed == b.NaturalLoadSeed &&
                                a.Operations.Length == b.Operations.Length && a.BusinessWorkValue == b.BusinessWorkValue,
                                scene + " " + mode.Item1 + " " + n + " same recipe/route/identity/nominal arrival/business value; hostile=" + a.IsHostile);
                            bool operationsMatch = true;
                            for (int i = 0; i < a.Operations.Length; i++)
                            {
                                var x = a.Operations[i]; var y = b.Operations[i];
                                var withoutScaling = new OperationStage(y.Operation, x.TotalWork, x.DesiredLoad, y.ActivityDescription,
                                    y.EffectOnCompletion, y.Behaviors, y.InfectionPayload, y.ConditionPayload, y.LaunchSpec, y.PlannedLogs);
                                operationsMatch &= Near(y.TotalWork, x.TotalWork * factor) && Near(y.DesiredLoad, x.DesiredLoad * factor) &&
                                    Near(x.TotalWork / x.DesiredLoad, y.TotalWork / y.DesiredLoad) && JsonUtility.ToJson(x) == JsonUtility.ToJson(withoutScaling);
                                samples.Add(FormattableString.Invariant($"{Path.GetFileNameWithoutExtension(scene)},{mode.Item1},{a.ConnectionId},{a.IsHostile},{i},{x.DesiredLoad},{y.DesiredLoad},{x.TotalWork},{y.TotalWork},{x.TotalWork / x.DesiredLoad},{y.TotalWork / y.DesiredLoad},{a.BusinessWorkValue},{b.BusinessWorkValue}"));
                            }
                            var trace = b.GenerationTrace.Copy(); trace.finalOperations = a.GenerationTrace.finalOperations;
                            Check(operationsMatch && JsonUtility.ToJson(a.GenerationTrace) == JsonUtility.ToJson(trace) &&
                                b.GenerationTrace.finalOperations.Select(x => x.totalWork).SequenceEqual(b.Operations.Select(x => x.TotalWork)),
                                scene + " " + mode.Item1 + " " + n + " proportional processing, unchanged nominal duration and exact anomaly/behavior/log/effect parameters");
                            Check(b.ForEvent("review").BusinessWorkValue == a.BusinessWorkValue &&
                                b.WithAmbientIdentity(b.IdentityMode, b.IdentityHopSeconds, b.IdentitySeed, 0).BusinessWorkValue == a.BusinessWorkValue,
                                scene + " " + mode.Item1 + " " + n + " plan copies retain business value");
                        }
                    }
                    double? baselineLifetime = null;
                    foreach (float multiplier in new[] { 1f, 1.5f })
                    {
                        var h = Open(scene, multiplier); ConfigureQueue(h, multiplier);
                        var t = h.GetComponent<ConnectionTrafficDirector>(); var plan = t.QueueProfile("product-lookup");
                        float travel = (plan.Route.Length - 1) * h.ConnectionTravelSecondsPerLink;
                        h.StepSimulation(travel + .02f); var job = h.FindConnection(plan.ConnectionId);
                        Check(Near(job.DesiredLoad, 5 * multiplier) && Near(job.CurrentTotalWork, 100 * multiplier) &&
                            Near(job.ServiceNominalDurationSeconds, 20) && Near(job.ServiceBudgetSeconds, 30) && job.BusinessWorkValue == 100,
                            scene + " actual queue " + multiplier + "x: LOAD/processing scaled, nominal20/budget30/business100");
                        Check(h.Recorder.CurrentLog.run.legitimateWorkloadMultiplier == multiplier &&
                            h.Recorder.CurrentLog.events.Single(x => x.type == "plan_queued").definition.businessWorkValue == 100,
                            scene + " metadata and queued definition retain effective factor and unscaled job value");
                        h.StepSimulation(20.1f);
                        Check(job.Resolution == ConnectionResolution.Completed && h.CreditedWork == 100 && h.WorkCreditCount == 1 && h.LegitimateFailureCount == 0 &&
                            Math.Abs(job.ResolvedAtSeconds - job.EstablishedAtSeconds - 20) <= Time.fixedDeltaTime + .005 &&
                            (!baselineLifetime.HasValue || Math.Abs(job.ResolvedAtSeconds - job.EstablishedAtSeconds - baselineLifetime.Value) <= Time.fixedDeltaTime + .001), scene + " " + multiplier + "x same uncontended lifetime and actual business credit: resolution=" + job.Resolution + ", credit=" + h.CreditedWork + ", receipts=" + h.WorkCreditCount + ", failures=" + h.LegitimateFailureCount + ", service=" + (job.ResolvedAtSeconds - job.EstablishedAtSeconds));
                        baselineLifetime ??= job.ResolvedAtSeconds - job.EstablishedAtSeconds;
                        ConfigureQueue(h, multiplier, 2); FragilityWitness.Set(h, "shiftWorkTarget", 200f); FragilityWitness.Set(h, "businessFailureLimit", 2); h.ResetSimulation(); h.Recorder.BeginRun();
                        t.QueueProfile("product-lookup"); t.QueueProfile("product-lookup"); h.StepSimulation(100);
                        Check(multiplier == 1 ? h.Ending == RunEnding.ShiftComplete && h.CreditedWork == 200 && h.LegitimateFailureCount == 0 :
                            h.Ending == RunEnding.Fired && h.CreditedWork == 0 && h.LegitimateFailureCount == 2,
                            scene + " same two jobs and capacity, " + multiplier + "x: expected contention changes real completion/failure, not quota value");
                    }
                    var empty = Open(scene, 1.5f); empty.Recorder.RecordingEnabled = false;
                    ConfigureQueue(empty, 1.5f); empty.Recorder.RecordingEnabled = false;
                    empty.GetComponent<ConnectionTrafficDirector>().QueueProfile("product-lookup"); empty.StepSimulation(24);
                    Check(empty.CreditedWork == 100 && empty.WorkCreditCount == 1, scene + " scaled job accounting independent of recording");
                }
            }
            finally { File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks); File.WriteAllLines(Evidence + "/paired-recipes.csv", samples); }
            Debug.Log("LegitimateWorkloadWitness PASS: " + checks.Count + " focused assertions.");
        }
        public static void ConfigureQueue(SimulationHost h, float multiplier, float baseline = 0)
        {
            FragilityWitness.ConfigureFixture(h, baseline, 0);
            FragilityWitness.Set(h.GetComponent<ConnectionTrafficDirector>(), "legitimateWorkloadMultiplier", multiplier);
            h.ResetSimulation(); h.Recorder.BeginRun();
        }
        public static void Regressions()
        {
            Directory.CreateDirectory(Evidence); var report = new List<string>();
            foreach (var test in new (string name, Action run)[] { ("Workload", Run), ("Shift", BusinessShiftWitness.Run),
                ("Fragility", FragilityWitness.Run), ("Traffic", TrafficWitness.Run), ("Recorder", RecorderWitness.Run) })
            { try { test.run(); report.Add("PASS " + test.name); } catch (Exception e) { report.Add("FAIL " + test.name + ": " + e.Message); Debug.LogException(e); } }
            File.WriteAllLines(Evidence + "/regression-checks.txt", report);
            if (report.Any(x => x.StartsWith("FAIL"))) throw new Exception("Workload regressions failed; see report.");
        }
    }
}
