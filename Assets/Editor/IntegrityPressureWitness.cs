using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class IntegrityPressureWitness
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] public sealed class Result
        {
            public int seed;
            public string policy;
            public float observedSeconds, lossTime = -1;
            public int commands, closestAdditionalCompromises;
            public IntegrityEvent[] damage;
            public IntegritySnapshot finalGraph;
        }
        [Serializable] public sealed class Results { public Result[] runs; }
        [MenuItem("Tools/Connections/Run Integrity Pressure Evidence")]
        public static void Run()
        {
            IntegrityWitness.Run();
            Directory.CreateDirectory(IntegrityWitness.Evidence + "/Runs");
            var results = new List<Result>();
            foreach (int seed in new[] { 1701, 1702, 3141, 9001 })
                foreach (string policy in new[] { "unattended", "visible-defense", "kill-all" })
                {
                    string signature = Observe(seed, policy, true, out var result);
                    results.Add(result);
                    if (seed == 1701 && signature != Observe(seed, policy, false, out _))
                        throw new InvalidOperationException("Integrity Pressure seeded replay differs: " + policy);
                }
            File.WriteAllText(IntegrityWitness.Evidence + "/pressure-summary.json", JsonUtility.ToJson(new Results { runs = results.ToArray() }, true));
            Debug.Log("IntegrityPressureWitness PASS: twelve natural 600s-or-loss runs and three exact seeded replays.");
        }
        public static string Observe(int seed, string policy, bool save, out Result result,
            Action<SimulationHost> configure = null, string evidenceDirectory = null, string scene = null)
        {
            var host = IntegrityWitness.Open(true, seed, configure, scene);
            var duels = host.GetComponent<ConnectionsDuelDirector>();
            var overlay = UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            // Edit Mode cannot retire history rows with delayed Destroy; keep this finite witness below its cap.
            IntegrityWitness.Set(terminal, "maxEntries", 10000);
            float nextDecision = 6, blockedUntil = 0, splashRemaining = 0;
            int encounter = -1, commands = 0, closest = Closest(host);
            var samples = new StringBuilder("second,remaining_compromises,compromised,connected_nodes,connected_links\n");
            for (int tick = 1; tick <= 30000 && !host.RunEnded; tick++)
            {
                host.StepSimulation(.02f);
                float now = host.SimulationTimeSeconds;
                if (duels.Current?.Lifecycle == DuelLifecycle.Active && !host.RunEnded)
                {
                    if (encounter != duels.Current.Id)
                    {
                        encounter = duels.Current.Id;
                        string node = duels.Current.Target.View.NodeId;
                        string splash = "INTRUSION DETECTED / " + node + "\n\nssh operator@" +
                            node.ToLowerInvariant() + "\nAUTHENTICATING... OK\nINITIALIZING COUNTERMEASURES...";
                        splashRemaining = splash.Length * Mathf.Max(.02f,
                            (float)IntegrityWitness.Field(overlay, "splashCharacterSeconds")) + .25f;
                    }
                    if (splashRemaining > 0) splashRemaining -= .02f;
                    else
                    {
                        var race = (CommandRaceSession)IntegrityWitness.Field(overlay, "race");
                        race.Advance(.02f);
                        if (race.PlayerWon.HasValue)
                        {
                            duels.Resolve(race);
                            blockedUntil = now + (float)IntegrityWitness.Field(overlay, "resultSeconds");
                            overlay.Abort();
                        }
                    }
                }
                if (!host.RunEnded && policy != "unattended" && now >= nextDecision &&
                    now >= blockedUntil && duels.Current?.Lifecycle != DuelLifecycle.Active)
                {
                    nextDecision = now + (policy == "kill-all" ? 1 : 6);
                    string command;
                    if (policy == "kill-all")
                    {
                        string[] ids = host.RuntimeConnections.Where(c => c.IsActive || c.IsConnecting).Select(c => c.ConnectionId).ToArray();
                        command = ids.Length == 0 ? null : "kill " + string.Join(" ", ids);
                    }
                    else
                    {
                        var red = host.Payloads.Payloads.FirstOrDefault(p => p.Phase != PayloadPhase.Resolved && p.NextNode == p.FinalNode);
                        var busy = host.Nodes.OrderByDescending(n => n.RequestedLoad / n.LoadCapacity).First();
                        command = red != null ? "intercept @ " + red.NextNode.NodeId :
                            busy.RequestedLoad > busy.LoadCapacity && host.OverclockCooldownRemaining <= 0 ? "overclock " + busy.View.NodeId : null;
                    }
                    if (command != null)
                    {
                        commands++;
                        typeof(Terminal).GetMethod("HandleSubmit", Private).Invoke(terminal, new object[] { command });
                    }
                }
                if (tick % 50 == 0 || host.RunEnded)
                {
                    closest = Mathf.Min(closest, Closest(host));
                    var graph = host.Integrity.Snapshot();
                    samples.AppendLine(FormattableString.Invariant($"{now:R},{Closest(host)},{string.Join("|", graph.nodes.Where(n => !n.external && n.integrity == 0).Select(n => n.id))},{string.Join("|", graph.nodes.Where(n => !n.external && n.connected).Select(n => n.id))},{string.Join("|", graph.connectedLinks)}"));
                }
            }
            host.Recorder.CaptureSnapshot(host.SimulationTimeSeconds);
            result = new Result { seed = seed, policy = policy, observedSeconds = host.SimulationTimeSeconds,
                lossTime = host.RunEnded ? host.Integrity.Breach.time : -1, commands = commands,
                closestAdditionalCompromises = closest,
                damage = host.Recorder.CurrentLog.events.Where(e => e.type == "integrity_damage").Select(e => e.integrity).ToArray(),
                finalGraph = host.Integrity.Snapshot() };
            string signature = string.Join("\n", host.Recorder.CurrentLog.events.Select(e => JsonUtility.ToJson(e))) +
                string.Join("\n", host.Recorder.CurrentLog.snapshots.Select(e => JsonUtility.ToJson(e))) + samples;
            host.Recorder.EndRun();
            if (save)
            {
                string path = (evidenceDirectory ?? IntegrityWitness.Evidence) + "/Runs/" + seed + "-" + policy;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".csv", samples.ToString());
                using (var file = File.Create(path + ".json.gz"))
                using (var gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(host.Recorder.CurrentLog));
                    gzip.Write(bytes, 0, bytes.Length);
                }
            }
            typeof(ConnectionsDuelDirector).GetMethod("OnDisable", Private).Invoke(duels, null);
            Debug.Log("INTEGRITY PRESSURE seed=" + seed + " policy=" + policy + " loss=" + result.lossTime + " observed=" + result.observedSeconds);
            return signature;
        }
        // Minimum count of not-yet-compromised internal nodes on any ingress-to-critical physical path.
        public static int Closest(SimulationHost host)
        {
            var states = host.Integrity.Nodes.ToArray();
            var distance = states.ToDictionary(n => n.Resources.View, n => n.External ? 0 : 999);
            for (int pass = 0; pass < states.Length; pass++)
                foreach (var edge in host.PhysicalLinks)
                    foreach (var pair in new[] { (edge.View.NodeA, edge.View.NodeB), (edge.View.NodeB, edge.View.NodeA) })
                    {
                        var destination = host.Integrity.For(host.FindNodeResources(pair.Item2.NodeId));
                        distance[pair.Item2] = Mathf.Min(distance[pair.Item2], distance[pair.Item1] +
                            (destination.External || destination.Compromised ? 0 : 1));
                    }
            return states.Where(n => n.Critical).Min(n => distance[n.Resources.View]);
        }
    }
}
