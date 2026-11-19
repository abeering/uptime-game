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
    public static class LivingShiftWitness
    {
        static SimulationHost host;
        static ConnectionTrafficDirector traffic;
        static LivingShiftDirector shift;
        static ConnectionsRunRecorder recorder;
        static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("LivingShiftWitness: " + message); Debug.Log("SHIFT CHECK: " + message); }
        static void Field(object o, string name, object value) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(o, value);
        static void Reset(int seed, bool ambient = false)
        {
            traffic.RandomizeSeed = false; traffic.Seed = seed;
            traffic.AutoGenerationEnabled = ambient; traffic.HostileGenerationEnabled = ambient;
            // Ordinary MonoBehaviour lifecycle does not run in an Edit Mode witness.
            var duel = host.GetComponent<ConnectionsDuelDirector>();
            typeof(ConnectionsDuelDirector).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(duel, null);
            typeof(ConnectionsDuelDirector).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(duel, null);
            host.ResetSimulation();
            recorder.BeginRun(); shift.BeginShift();
        }
        static void Step(float seconds)
        {
            float until = host.SimulationTimeSeconds + seconds;
            while (host.SimulationTimeSeconds < until && !shift.IsFinished)
                host.StepSimulation(Mathf.Min(.1f, until - host.SimulationTimeSeconds));
        }
        [MenuItem("Tools/Connections/Run Living Shift Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            traffic = host.GetComponent<ConnectionTrafficDirector>(); shift = host.LivingShift;
            recorder = host.Recorder;
            Check(shift != null && shift.IsEnabled, "Advanced enables authored Living Shift");
            UnityEngine.Object.FindFirstObjectByType<TextCommunicationDirector>().enabled = false;
            var panel = UnityEngine.Object.FindFirstObjectByType<LivingShiftPanel>();
            var bindings = new SerializedObject(panel);
            foreach (string field in new[] { "director", "objectiveRoot", "objectiveText", "paceText", "progressFill", "resultRoot", "resultText", "summaryText", "restartButton" })
                Check(bindings.FindProperty(field).objectReferenceValue != null, "authored panel binding " + field);
            Check(shift.Target == 2800f, "fixed measured target is 2800 WORK");
            Reset(1701);
            string plan = JsonUtility.ToJson(recorder.CurrentLog.events.First(e => e.type == "shift_started").shift);
            Reset(1701);
            Check(plan == JsonUtility.ToJson(recorder.CurrentLog.events.First(e => e.type == "shift_started").shift), "same seed repeats selected activities/timing/count/intensity");
            Check(shift.Activities.Count == 4 && shift.Activities[0].premise != shift.Activities[1].premise,
                "main, distinct secondary and two later activities planned");
            Step(20f);
            var plans = recorder.CurrentLog.events.Where(e => e.type == "plan_queued" && e.source.StartsWith("living_shift:")).ToArray();
            Check(plans.Length > 0 && plans.All(p => !p.definition.isHostile), "real legitimate activity plans queued through existing pipeline");
            Check(recorder.CurrentLog.events.Any(e => e.type == "shift_communication" && e.shift.cue == "activity_open:main" && e.shift.body.Length > 0), "opening content comes from main activity");
            var owned = host.RuntimeConnections.First(c => shift.Owns(c.ConnectionId));
            host.KillConnection(owned.ConnectionId); Step(20f);
            Check(recorder.CurrentLog.events.Any(e => e.type == "shift_communication" && e.shift.cue.StartsWith("activity_interruption:") && e.shift.body.Contains(owned.ConnectionId)), "interruption cites real owned ID");
            Check(shift.LostWork > 0f, "lost opportunity recorded");
            Reset(23);
            // Keep scheduled company work away from this tiny accounting case.
            foreach (var a in shift.Activities) { a.startAt = 300; a.announceAt = 300; }
            var route = new[] { host.FindNodeResources("EXT1").View, host.FindNodeResources("WEB").View, host.FindNodeResources("API").View };
            var benign = host.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0f, "credit", route, "witness", "Accounting witness",
                new[] { new OperationStage(OperationKind.Read, 1f, 5f, "first"), new OperationStage(OperationKind.Write, 1000f, 1f, "later") })));
            var hostile = host.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0f, "hostile", route, "witness", "Accounting witness",
                new[] { new OperationStage(OperationKind.Read, 1f, 5f, "hostile") }, true)));
            Step(4f);
            Check(Mathf.Abs(shift.CreditedWork - 1f) < .001f, "only completed benign operation credited; hostile excluded");
            host.KillConnection(benign.ConnectionId);
            Check(Mathf.Abs(shift.CreditedWork - 1f) < .001f && shift.LostWork == 1000f, "earlier credit retained, partial killed operation receives zero");
            typeof(LivingShiftDirector).GetMethod("Credit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shift, new object[] { benign, 0, 1f });
            Check(shift.CreditedWork == 1f, "duplicate completion identity cannot double credit");
            Reset(23);
            foreach (var a in shift.Activities) { a.startAt = 500; a.announceAt = 500; }
            foreach (string id in new[] { "service-a", "service-b" })
                host.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0, id, route,
                    "service-witness", "Service witness", new[] { new OperationStage(OperationKind.Read, 1000, 12, "service work") })));
            Step(170f);
            Check(host.Health.DegradationRate > 0f && recorder.CurrentLog.events.Any(e => e.type == "shift_communication" &&
                e.shift.cue == "service_deterioration"), "serious service cue requires actual deteriorating work");
            host.KillConnection("service-a"); host.KillConnection("service-b"); Step(120f);
            Check(host.Health.Value >= 40f && host.Health.DegradationRate == 0f && recorder.CurrentLog.events.Any(e =>
                e.type == "shift_communication" && e.shift.cue == "service_recovery"), "recovery cue follows genuine cleared degradation and Health recovery");
            Reset(1701); Step(360f);
            Check(shift.Activities[0].succeeded == shift.Activities[0].jobs &&
                recorder.CurrentLog.events.Any(e => e.type == "shift_communication" && e.shift.cue == "activity_result:main" &&
                e.shift.body.Contains($"{shift.Activities[0].jobs}/{shift.Activities[0].jobs}")),
                "successful batch acknowledgment matches every actual owned resolution");
            Reset(23); Check(shift.CreditedWork == 0f && shift.LostWork == 0f && shift.EligibleWork == 0f, "reset clears totals and identities");
            while (!shift.IsFinished)
            {
                foreach (var c in host.RuntimeConnections.ToArray())
                    if (c.IsActive || c.IsConnecting) host.KillConnection(c.ConnectionId);
                host.StepSimulation(.1f);
            }
            Check(shift.IsFinished && !shift.Succeeded && Mathf.Abs(host.SimulationTimeSeconds - 360f) < .001f, "fixed target produces actual deadline failure at 360");
            float time = host.SimulationTimeSeconds, work = shift.CreditedWork;
            host.StepSimulation(50f);
            Check(host.SimulationTimeSeconds == time && shift.CreditedWork == work, "terminal result freezes simulation and new traffic");
            Check(recorder.CurrentLog.events.Count(e => e.type == "shift_finished") == 1, "one final result receipt");
            Check(recorder.CurrentLog.events.Any(e => e.type == "shift_communication" && e.shift.cue.StartsWith("activity_result:") && e.shift.body.Contains("completed")), "batch result based on authoritative resolutions");
            Reset(1701);
            host.StepSimulation(400f);
            Check(shift.IsFinished && host.SimulationTimeSeconds == 360f &&
                recorder.CurrentLog.events.Count(e => e.type == "shift_activity_connection") >= 16,
                "coarse steps visit activity schedules and clamp at the deadline with ambient off");
            Field(shift, "livingShiftEnabled", false); Reset(23);
            host.StepSimulation(400f);
            Check(!shift.IsRunning && !shift.IsFinished && host.SimulationTimeSeconds >= 400f && traffic.HostileMeanIntervalSeconds == 30f, "switch restores endless sandbox cadence and progression");
            Field(shift, "livingShiftEnabled", true);
            Reset(1701);
            Check(shift.CreditedWork == 0f && shift.IsRunning, "fresh reset restarts shift");
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            Check(UnityEngine.Object.FindFirstObjectByType<SimulationHost>().LivingShift == null, "four-node scene has no experiment enabled");
            Debug.Log("LivingShiftWitness PASS");
        }

        public static void AdvanceUnattendedDuel(float seconds)
        {
            var duel = host.GetComponent<ConnectionsDuelDirector>();
            if (duel.Current?.Lifecycle != DuelLifecycle.Active) return;
            var overlay = UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
            var race = (CommandRaceSession)typeof(ConnectionsDuelOverlay).GetField("race", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(overlay);
            if (race == null) return;
            race.Advance(seconds);
            if (race.PlayerWon.HasValue) duel.Resolve(race);
        }

        [MenuItem("Tools/Connections/Measure Living Shift Operator Policy")]
        public static void MeasureOperator()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            traffic = host.GetComponent<ConnectionTrafficDirector>(); shift = host.LivingShift; recorder = host.Recorder;
            UnityEngine.Object.FindFirstObjectByType<TextCommunicationDirector>().enabled = false;
            using var writer = new StreamWriter("/private/tmp/living-shift-operator.csv");
            writer.WriteLine("seed,policy,credited,eligible,lost,health,result,targetMetAt");
            foreach (int seed in new[] { 1701, 23, 42, 99, 4109, 8102, 777, 2026 })
            {
                Reset(seed, true);
                var policy = new LivingShiftOperatorPolicy();
                float metAt = -1f;
                while (!shift.IsFinished)
                {
                    AdvanceUnattendedDuel(.1f);
                    policy.Act(host);
                    host.StepSimulation(.1f);
                    if (metAt < 0 && shift.CreditedWork >= shift.Target) metAt = host.SimulationTimeSeconds;
                }
                string row = $"{seed},scan-clean-overclock,{shift.CreditedWork:F2},{shift.EligibleWork:F2},{shift.LostWork:F2},{host.Health.Value:F2},{(shift.Succeeded ? "complete" : "failed")},{metAt:F2}";
                writer.WriteLine(row); writer.Flush(); Debug.Log("SHIFT OPERATOR: " + row);
            }
            Debug.Log("LivingShift Operator Measure PASS");
        }

        [MenuItem("Tools/Connections/Measure Living Shift Baselines")]
        public static void Measure()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ConnectionsAdvanced.unity");
            host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            traffic = host.GetComponent<ConnectionTrafficDirector>(); shift = host.LivingShift; recorder = host.Recorder;
            // Presentation is separately validated in Play Mode; avoid Edit Mode UI lifecycle.
            UnityEngine.Object.FindFirstObjectByType<TextCommunicationDirector>().enabled = false;
            string path = "/private/tmp/living-shift-measurements.csv";
            using var writer = new StreamWriter(path);
            writer.WriteLine("seed,policy,credited,eligible,lost,health,result,targetMetAt");
            foreach (int seed in new[] { 1701, 23, 42, 99, 4109, 8102, 777, 2026 })
            foreach (string policy in new[] { "no-input", "diagnostic-intervention" })
            {
                Reset(seed, true);
                float metAt = -1f;
                int ticks = 0;
                while (!shift.IsFinished && ticks++ < 3610)
                {
                    if (policy != "no-input" && ticks % 20 == 0)
                    {
                        // Deliberately privileged causal upper bound, not a human-play skill claim.
                        foreach (var c in host.RuntimeConnections.ToArray())
                            if (c.IsHostile && (c.IsActive || c.IsConnecting)) host.KillConnection(c.ConnectionId);
                        foreach (var infection in host.Infections.ToArray()) host.TryRemoveInfection(infection.Target.View.NodeId, infection.Payload.Kind, out _);
                    }
                    AdvanceUnattendedDuel(.1f);
                    host.StepSimulation(.1f);
                    if (metAt < 0 && shift.CreditedWork >= shift.Target) metAt = host.SimulationTimeSeconds;
                }
                string row = $"{seed},{policy},{shift.CreditedWork:F2},{shift.EligibleWork:F2},{shift.LostWork:F2},{host.Health.Value:F2},{(shift.Succeeded ? "complete" : "failed")},{metAt:F2}";
                writer.WriteLine(row); writer.Flush(); Debug.Log("SHIFT MEASURE: " + row);
            }
            Debug.Log("LivingShift Measure PASS " + path);
        }
    }

    public sealed class LivingShiftOperatorPolicy
    {
        private readonly string[] nodes = { "API", "AUTH", "DB", "JOBS", "RPT", "WEB" };
        private int cursor;
        private NodeScanSession scan;
        private CleanSession clean;
        private float cleanDue, nextScan;
        public void Act(SimulationHost host)
        {
            float now = host.SimulationTimeSeconds;
            if (host.GetComponent<ConnectionsDuelDirector>().Current?.Lifecycle == DuelLifecycle.Active) return;
            if (clean != null && now >= cleanDue)
            {
                // This is the visible process list's conspicuous CPU row, not MaliciousPid.
                var process = clean.Processes.OrderByDescending(p => p.CpuPercent).First();
                host.SelectCleanPid(process.Pid, out _); clean = null;
            }
            if (scan != null && scan.IsComplete)
            {
                if (scan.Result == NodeScanResult.Infected &&
                    host.StartClean(scan.NodeId, "clean " + scan.NodeId, out var session) == CleanStartResult.Started)
                { clean = session; cleanDue = now + 4f; }
                scan = null;
            }
            if (scan == null && clean == null && host.ActiveScan == null && now >= nextScan)
            {
                string node = nodes[cursor++ % nodes.Length];
                host.StartScan(node, "scan " + node, out scan); nextScan = now + 2f;
            }
            if (host.OverclockCooldownRemaining <= 0f)
            {
                var node = host.Nodes.Where(n => nodes.Contains(n.View.NodeId))
                    .OrderByDescending(n => n.RequestedLoad / n.LoadCapacity).First();
                if (node.RequestedLoad > node.LoadCapacity)
                    host.TryActivateOverclock(node.View.NodeId, "overclock " + node.View.NodeId, out _);
            }
        }
    }
}
