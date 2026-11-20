using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class HackerDirectivePressureWitness
    {
        [Serializable] public sealed class RunResult
        {
            public string directive;
            public IntegrityPressureWitness.Result result;
        }
        [Serializable] public sealed class Results { public RunResult[] runs; }
        [MenuItem("Tools/Connections/Run Hacker Directive Pressure Evidence")]
        public static void Run()
        {
            var results = new List<RunResult>();
            var checks = new List<string>();
            foreach (HackerDirective mode in new[] { HackerDirective.SPREAD, HackerDirective.BREACH })
                foreach (int seed in new[] { 1701, 1702, 3141, 9001 })
                    foreach (string policy in new[] { "unattended", "visible-defense", "kill-all" })
                    {
                        string Observe(bool save, out IntegrityPressureWitness.Result result) =>
                            IntegrityPressureWitness.Observe(seed, policy, save, out result,
                                host => host.GetComponent<HackerDirectives>().ConfigureForDevelopment(mode),
                                HackerDirectiveWitness.Evidence + "/" + mode);
                        string signature = Observe(true, out var result);
                        results.Add(new RunResult { directive = mode.ToString(), result = result });
                        if (seed == 1701)
                        {
                            if (signature != Observe(false, out _))
                                throw new InvalidOperationException("Directive seeded replay differs: " + mode + "/" + policy);
                            checks.Add("PASS exact events/snapshots/topology replay: " + mode + "/" + seed + "/" + policy);
                        }
                    }
            Directory.CreateDirectory(HackerDirectiveWitness.Evidence);
            File.WriteAllText(HackerDirectiveWitness.Evidence + "/pressure-summary.json",
                JsonUtility.ToJson(new Results { runs = results.ToArray() }, true));
            File.WriteAllLines(HackerDirectiveWitness.Evidence + "/replay-checks.txt", checks);
            Debug.Log("HackerDirectivePressureWitness PASS: 24 natural runs; six exact seeded replays.");
        }
        public static void Regressions()
        {
            IntegrityWitness.Run();
            File.Copy(IntegrityWitness.Evidence + "/deterministic-checks.txt",
                HackerDirectiveWitness.Evidence + "/integrity-checks.txt", true);
            try { IntegrityWitness.RunRegressions(); }
            finally
            {
                File.Copy(IntegrityWitness.Evidence + "/regression-checks.txt",
                    HackerDirectiveWitness.Evidence + "/regression-checks.txt", true);
            }
        }
    }
}
