using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    // Experiment-only evidence. No runtime tuning, spawning or difficulty logic lives here.
    public static class MaximumPressureWitness
    {
        public const string Baseline = "Assets/Scenes/ConnectionsAdvanced.unity";
        public const string Pressure = "Assets/Scenes/ConnectionsAdvancedPressure.unity";
        public const string Evidence = "Docs/Review/ConnectionsMaximumPressureV0";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float Step = .02f;

        public static void RunRegressions()
        {
            TrafficWitness.Run();
            AmbientHostileActivityWitness.Run();
            NetworkHealthWitness.Run();
            DuelWitness.Run();
            ConsequenceWitness.Run();
            AdvancedSceneWitness.Run();
            RecorderWitness.Run();
            PayloadWitness.Run();
            WormWitness.Run();
            AmbientPayloadThreatWitness.Run();
            Debug.Log("Maximum Pressure regression suite PASS (10 existing witnesses).");
        }

        [MenuItem("Tools/Connections/Run Maximum Pressure Witness")]
        public static void Run()
        {
            // Byte-for-byte equivalence except the four intentional serialized settings
            // protects every authored binding, prefab override and ordinary game system.
            Check(File.ReadAllText(Pressure) == File.ReadAllText(Baseline)
                .Replace("  hostileMeanIntervalSeconds: 30\n", "  hostileMeanIntervalSeconds: 8\n")
                .Replace("  ordinaryWeight: 4\n", "  ordinaryWeight: 6\n")
                .Replace("  wormWeight: 1\n", "  wormWeight: 2\n")
                .Replace("  payloadLauncherWeight: 1\n", "  payloadLauncherWeight: 2\n"),
                "only intended tuning differs from Advanced");
            Check(AssetDatabase.AssetPathToGUID(Pressure) != AssetDatabase.AssetPathToGUID(Baseline),
                "scene has its own GUID");
            EditorSceneManager.OpenScene(Pressure);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            var activity = host.GetComponent<HostileActivityDirector>();
            Check(host.Nodes.Length == 8 && host.PhysicalLinks.Length == 7 &&
                host.RunMode == ConnectionRunMode.Generated && traffic.AutoGenerationEnabled &&
                traffic.HostileGenerationEnabled && traffic.RandomizeSeed &&
                traffic.HostileMeanIntervalSeconds == 8 && traffic.InitialGraceSeconds == 3 &&
                traffic.BenignPopulationGoal == 4 && activity.OrdinaryWeight == 6 &&
                activity.ReconnaissanceWeight == 2 && activity.PayloadWeight == 2 &&
                activity.WormWeight == 2 && activity.PayloadLauncherWeight == 2 &&
                activity.DirectDuelWeight == 1 && activity.ConnectionDuelWeight == 1 &&
                host.GetComponent<ConnectionsRunRecorder>().RecordingEnabled,
                "authored Pressure configuration");
            foreach (GameObject root in host.gameObject.scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                        "no missing scripts: " + child.name);
            InfectionWitness.CheckCleanOverlayBindings();
            EscapeVideoCallWitness.CheckBindings();

            Directory.CreateDirectory(Evidence + "/Runs");
            foreach (string scene in new[] { Baseline, Pressure })
                foreach (int seed in new[] { 1701, 1702, 3141, 9001 })
                    foreach (bool intervention in new[] { false, true })
                    {
                        string signature = Observe(scene, seed, intervention, true);
                        if (seed == 1701)
                            Check(signature == Observe(scene, seed, intervention, false),
                                "seeded events and snapshots replay: " + scene + "/" + intervention);
                    }
            Debug.Log("MaximumPressureWitness PASS: isolated scene bindings/tuning, " +
                "sixteen 180-second comparisons and four seeded replays.");
        }

        private static string Observe(string scene, int seed, bool intervention, bool save)
        {
            EditorSceneManager.OpenScene(scene);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            var recorder = host.GetComponent<ConnectionsRunRecorder>();
            var duels = host.GetComponent<ConnectionsDuelDirector>();
            var overlay = UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            traffic.RandomizeSeed = false;
            traffic.Seed = seed;
            // Edit Mode does not invoke the carrier completion / reset subscriptions.
            typeof(ConnectionsDuelDirector).GetMethod("OnEnable", Private).Invoke(duels, null);
            host.SetRunMode(ConnectionRunMode.Generated);
            recorder.BeginRun();
            float minimum = 100, nextDecision = 6, blockedUntil = 0;
            int encounterId = -1;
            float splashRemaining = 0;
            var samples = new StringBuilder("second,hostileConnecting,hostileEstablished,payloads,wormBranches,infections,conditions,duel,benignEstablished,benignSlowed,health,netRate,overloadedNodes,minimumHealth\n");
            try
            {
                for (int tick = 1; tick <= 9000; tick++)
                {
                    host.StepSimulation(Step);
                    float now = host.SimulationTimeSeconds;
                    // The real race uses unscaled wall time. For deterministic evidence only,
                    // drive that SAME session at 50 Hz with wall time = simulation time (1x).
                    // No perfect player or forced resolution: both policies lose unattended races.
                    if (duels.Current?.Lifecycle == DuelLifecycle.Active)
                    {
                        if (encounterId != duels.Current.Id)
                        {
                            encounterId = duels.Current.Id;
                            string node = duels.Current.Target.View.NodeId;
                            string splash = "INTRUSION DETECTED / " + node + "\n\nssh operator@" +
                                node.ToLowerInvariant() + "\nAUTHENTICATING... OK\nINITIALIZING COUNTERMEASURES...";
                            splashRemaining = splash.Length * Mathf.Max(Step,
                                (float)Field(overlay, "splashCharacterSeconds")) + .25f;
                        }
                        if (splashRemaining > 0) splashRemaining -= Step;
                        else
                        {
                            var race = (CommandRaceSession)Field(overlay, "race");
                            race.Advance(Step);
                            if (race.PlayerWon.HasValue)
                            {
                                duels.Resolve(race);
                                blockedUntil = now + (float)Field(overlay, "resultSeconds");
                                overlay.Abort();
                            }
                        }
                    }
                    if (intervention && now >= nextDecision && duels.Current?.Lifecycle != DuelLifecycle.Active &&
                        now >= blockedUntil)
                    {
                        nextDecision = now + 6;
                        // Only current visible red-hop information and node LOAD, never hidden
                        // connection intent, infection state, future route or a perfect CLEAN PID.
                        var red = host.Payloads.Payloads.FirstOrDefault(p => p.Phase != PayloadPhase.Resolved &&
                            p.NextNode == p.FinalNode);
                        NodeResources busy = host.Nodes.OrderByDescending(n => n.RequestedLoad / n.LoadCapacity).First();
                        string command = red != null ? "intercept @ " + red.NextNode.NodeId :
                            busy.RequestedLoad > busy.LoadCapacity && host.OverclockCooldownRemaining <= 0 ?
                            "overclock " + busy.View.NodeId : null;
                        if (command != null)
                            typeof(Terminal).GetMethod("HandleSubmit", Private).Invoke(terminal, new object[] { command });
                    }
                    minimum = Mathf.Min(minimum, host.Health.Value);
                    if (tick % 50 != 0) continue;
                    var live = host.RuntimeConnections;
                    int payloads = host.Payloads.Payloads.Count(p => p.Phase != PayloadPhase.Resolved);
                    int branches = host.Worms.Outbreaks.Sum(w => w.Branches.Count(b => b.Phase != WormPhase.Resolved));
                    int slowed = live.Count(c => !c.IsHostile && c.IsActive && c.Satisfaction < .98f);
                    samples.AppendLine(FormattableString.Invariant($"{tick / 50},{live.Count(c => c.IsHostile && c.IsConnecting)},{live.Count(c => c.IsHostile && c.IsActive)},{payloads},{branches},{host.Infections.Count},{host.Conditions.Count},{(duels.Current == null ? 0 : 1)},{live.Count(c => !c.IsHostile && c.IsActive)},{slowed},{host.Health.Value:R},{host.Health.NetRate:R},{host.Nodes.Count(n => n.RequestedLoad > n.LoadCapacity)},{minimum:R}"));
                }
                Check(Mathf.Abs(host.SimulationTimeSeconds - 180) < .05f, "180-simulation-second window");
                Check(recorder.CurrentLog.events.Any(e => e.type == "hostile_opportunity") &&
                    recorder.CurrentLog.events.Any(e => e.type == "plan_queued" && e.source == "ambient_benign"),
                    "ordinary seeded clock and legitimate traffic remain active");
                recorder.CaptureSnapshot(host.SimulationTimeSeconds);
                // Exclude run IDs and UTC save metadata when checking exact replay.
                string signature = string.Join("\n", recorder.CurrentLog.events.Select(e => JsonUtility.ToJson(e))) +
                    string.Join("\n", recorder.CurrentLog.snapshots.Select(s => JsonUtility.ToJson(s))) + samples;
                recorder.EndRun();
                if (save)
                {
                    string id = (scene == Pressure ? "pressure" : "baseline") + "-" + seed +
                        (intervention ? "-limited" : "-unattended");
                    File.WriteAllText(Evidence + "/Runs/" + id + ".csv", samples.ToString());
                    using (var file = File.Create(Evidence + "/Runs/" + id + ".json.gz"))
                    using (var gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(recorder.CurrentLog));
                        gzip.Write(bytes, 0, bytes.Length);
                    }
                    Debug.Log("PRESSURE EVIDENCE " + id + " min=" + minimum + " final=" + host.Health.Value);
                }
                return signature;
            }
            finally
            {
                recorder.EndRun();
                typeof(ConnectionsDuelDirector).GetMethod("OnDisable", Private).Invoke(duels, null);
            }
        }

        private static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("MaximumPressureWitness: " + message); }
    }
}
