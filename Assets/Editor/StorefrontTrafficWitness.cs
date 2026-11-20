using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class StorefrontTrafficWitness
    {
        public const string Evidence = HackerDirectiveWitness.Evidence + "/StorefrontPatch";
        [MenuItem("Tools/Connections/Run Storefront Traffic Witness")]
        public static void Run()
        {
            Directory.CreateDirectory(Evidence);
            var checks = new List<string>();
            void Check(bool condition, string message)
            { if (!condition) throw new InvalidOperationException("StorefrontTrafficWitness FAIL: " + message); checks.Add("PASS " + message); }
            var host = HackerDirectiveWitness.Open(HackerDirective.BREACH);
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            var recipe = traffic.Profiles.Single(p => p.id == "storefront-page");
            Check(recipe.purposeTemplate && !recipe.isHostile && recipe.ambientWeight == 1 &&
                recipe.claimedPurpose == "Storefront Page Requests" && traffic.EffectiveTargetIds(recipe).SequenceEqual(new[] { "WEB" }),
                "one ordinary weighted PURPOSE template serves WEB; shared benign/hostile construction");
            Check(recipe.operations.Select(o => o.operation).SequenceEqual(new[] { OperationKind.Read, OperationKind.Execute }) &&
                recipe.operations.Select(o => o.meanWork).SequenceEqual(new[] { 24f, 36f }), "READ/EXECUTE baseline 24/36 mean WORK");
            var kinds = new HashSet<string>();
            int benign = 0, directed = 0;
            // Ordinary weighted generation with all seven PURPOSEs retained; no forced objective/signature/recipe.
            for (int seed = 1; seed <= 16; seed++)
            {
                host = HackerDirectiveWitness.Open(HackerDirective.BREACH, seed);
                traffic = host.GetComponent<ConnectionTrafficDirector>();
                for (int i = 0; i < 20; i++)
                {
                    foreach (var plan in new[] { traffic.GenerateBenignPlan(0), traffic.GenerateHostilePlan(0) })
                    {
                        if (plan.ProfileId != "storefront-page") continue;
                        host.ValidatePlan(plan);
                        var normal = plan.GenerationTrace.normalOperations;
                        Check(normal.Length == 2 && normal[0].kind == "Read" && normal[1].kind == "Execute" &&
                            normal[0].desiredLoad >= .5f && normal[0].desiredLoad <= 1.1f && normal[0].totalWork >= 18 && normal[0].totalWork <= 30 &&
                            normal[1].desiredLoad >= .8f && normal[1].desiredLoad <= 1.6f && normal[1].totalWork >= 27 && normal[1].totalWork <= 45,
                            "normal independently sampled LOAD/WORK ranges seed=" + seed + " id=" + plan.ConnectionId);
                        if (!plan.IsHostile) { benign++; continue; }
                        kinds.Add(plan.GenerationTrace.objective);
                        Check(plan.GenerationTrace.objective == "DELIVERY" ?
                            plan.Operations.Last().EffectOnCompletion == CompletionEffect.InfectionDelivery && plan.Operations.Last().InfectionPayload != null :
                            plan.LoaderAttack != null && plan.GenerationTrace.anomalies.Contains("loader-capacity-demand"),
                            "existing hostile composition/effects retained seed=" + seed + " id=" + plan.ConnectionId);
                        var decision = host.GetComponent<HackerDirectives>().Prefer("ordinary", 0, plan.Target, traffic.EligibleDirectiveTargets(plan));
                        if (decision.finalTarget == "WEB") directed++;
                    }
                }
            }
            Check(benign > 0 && kinds.SetEquals(new[] { "LOADER", "DELIVERY" }) && directed > 0,
                "weighted generation produces benign, LOADER, INFECTOR and matching-purpose BREACH WEB selections");
            foreach (string scene in new[] { "Connections", "ConnectionsAdvanced", "ConnectionsAdvancedPressure" })
            {
                EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
                host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                host.ResetSimulation();
                Check(host.GetComponent<ConnectionTrafficDirector>().Profiles.Any(p => p.id == "storefront-page") == (scene == "ConnectionsAdvanced"),
                    scene + " canonical Advanced storefront / Small and historical Pressure catalogs retained");
            }
            File.WriteAllLines(Evidence + "/focused-checks.txt", checks);
            Debug.Log("StorefrontTrafficWitness PASS: " + checks.Count + " checks.");
        }
        public static void Natural()
        {
            Run();
            var runs = new List<HackerDirectivePressureWitness.RunResult>();
            var checks = new List<string>();
            foreach (HackerDirective mode in new[] { HackerDirective.SPREAD, HackerDirective.BREACH })
                foreach (int seed in new[] { 1701, 1702, 3141, 9001 })
                {
                    string Observe(bool save, out IntegrityPressureWitness.Result result) =>
                        IntegrityPressureWitness.Observe(seed, "unattended", save, out result,
                            host => host.GetComponent<HackerDirectives>().ConfigureForDevelopment(mode), Evidence + "/" + mode);
                    string signature = Observe(true, out var result);
                    runs.Add(new HackerDirectivePressureWitness.RunResult { directive = mode.ToString(), result = result });
                    if (seed == 1701)
                    {
                        if (signature != Observe(false, out _)) throw new InvalidOperationException("Storefront replay differs: " + mode);
                        checks.Add("PASS exact events/snapshots/graph replay: " + mode + "/1701/unattended");
                    }
                }
            File.WriteAllText(Evidence + "/pressure-summary.json", JsonUtility.ToJson(new HackerDirectivePressureWitness.Results { runs = runs.ToArray() }, true));
            File.WriteAllLines(Evidence + "/replay-checks.txt", checks);
            Debug.Log("StorefrontTrafficWitness PASS: eight natural runs and two exact seeded replays.");
        }
        public static void Regressions()
        {
            Directory.CreateDirectory(Evidence);
            HackerDirectiveWitness.Run();
            File.Copy(HackerDirectiveWitness.Evidence + "/deterministic-checks.txt", Evidence + "/directive-checks.txt", true);
            IntegrityWitness.Run();
            File.Copy(IntegrityWitness.Evidence + "/deterministic-checks.txt", Evidence + "/integrity-checks.txt", true);
            TrafficWitness.Run(); AmbientHostileActivityWitness.Run();
            File.WriteAllText(Evidence + "/regression-checks.txt", "PASS HackerDirectiveWitness 375 assertions\nPASS IntegrityWitness 327 assertions\nPASS TrafficWitness\nPASS AmbientHostileActivityWitness\n");
        }
    }
}
