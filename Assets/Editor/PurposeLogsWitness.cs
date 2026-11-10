using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class PurposeLogsWitness
    {
        private static readonly (string id, string name, string target, float weight,
            (OperationKind kind, float low, float high, float work)[] stages, int logs)[] Specs =
        {
            ("product-lookup", "Product Detail Lookups", "DB", 1.5f,
                new[] { (OperationKind.Read, .5f, 1.1f, 48f) }, 20),
            ("customer-login", "Customer Authentications", "AUTH", 1f,
                new[] { (OperationKind.Execute, .8f, 1.6f, 48f) }, 20),
            ("cart-pricing", "Cart Price Calculations", "API", 1f,
                new[] { (OperationKind.Execute, 1f, 2f, 70f) }, 20),
            ("order-submit", "Customer Checkouts", "API", .9f,
                new[] { (OperationKind.Write, 1.5f, 3f, 120f),
                    (OperationKind.Read, .6f, 1.3f, 36f) }, 23),
            ("inventory-sync", "Inventory Reconciliations", "DB", .6f,
                new[] { (OperationKind.Read, 1f, 1.8f, 80f),
                    (OperationKind.Write, 1.7f, 3.2f, 130f) }, 21),
            ("sales-export", "Sales Report Export", "DB", .25f,
                new[] { (OperationKind.Read, 2.5f, 4.5f, 290f) }, 19)
        };

        private const string Clue = "checkout.session.export";

        [MenuItem("Tools/Connections/Run PURPOSE Logs Witness")]
        public static void Run()
        {
            Dictionary<string, string> defaultContent =
                Catalog("Assets/Scenes/Connections.unity", false);
            Dictionary<string, string> advancedContent =
                Catalog("Assets/Scenes/ConnectionsAdvanced.unity", true);
            foreach (var spec in Specs)
                Check(defaultContent[spec.id] == advancedContent[spec.id],
                    "both scenes use the same " + spec.id + " log schedule");
            Check(defaultContent["logs-checkout-evidence"] ==
                advancedContent["logs-checkout-evidence"],
                "manual evidence is identical in both scenes");
            TimingAndEvidence();
            Debug.Log("PurposeLogsWitness PASS: six catalog baselines in both scenes, " +
                "benign and hostile inherited logs, seeded replay, WORK timing and contention, " +
                "manual evidence before delivery, recorder provenance, logs-demo.");
        }

        private static Dictionary<string, string> Catalog(string path, bool advanced)
        {
            EditorSceneManager.OpenScene(path);
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            Check(host != null && traffic != null, path + " bindings");
            bool auto = traffic.AutoGenerationEnabled;
            bool hostile = traffic.HostileGenerationEnabled;
            bool randomize = traffic.RandomizeSeed;
            int seed = traffic.Seed;
            try
            {
                traffic.AutoGenerationEnabled = false;
                traffic.HostileGenerationEnabled = false;
                traffic.RandomizeSeed = false;
                traffic.Seed = 1701;
                host.SetRunMode(ConnectionRunMode.Generated);
                Check(traffic.Profiles.Count == 21, path + " complete catalog");
                var content = new Dictionary<string, string>();
                foreach (var spec in Specs)
                {
                    ConnectionTrafficProfile profile = traffic.Profiles.Single(p => p.id == spec.id);
                    Check(profile.purposeTemplate && !profile.isHostile &&
                        profile.claimedPurpose == spec.name &&
                        profile.eligibleTargetIds.SequenceEqual(new[] { spec.target }) &&
                        traffic.EffectiveAmbientWeight(spec.id) == spec.weight &&
                        profile.operations.Length == spec.stages.Length &&
                        traffic.EffectiveTargetIds(profile).SequenceEqual(new[] {
                            advanced && spec.id == "inventory-sync" ? "JOBS" :
                            advanced && spec.id == "sales-export" ? "RPT" : spec.target }),
                        path + " " + spec.id + " name, target, and weight");
                    for (int i = 0; i < spec.stages.Length; i++)
                    {
                        var expected = spec.stages[i];
                        ConnectionOperationProfile stage = profile.operations[i];
                        Check(stage.operation == expected.kind &&
                            stage.minimumLoad == expected.low &&
                            stage.maximumLoad == expected.high &&
                            stage.meanWork == expected.work &&
                            stage.minimumDuration == 40f && stage.maximumDuration == 75f &&
                            stage.completionEffect == CompletionEffect.None &&
                            stage.logs != null && stage.logs.Length > 0,
                            path + " " + spec.id + " unchanged operation baseline " + i);
                        float previous = -1f;
                        foreach (PlannedOperationLog log in stage.logs)
                        {
                            Check(log != null && log.WorkFraction >= previous &&
                                log.WorkFraction <= 1f && !string.IsNullOrWhiteSpace(log.Activity) &&
                                log.Activity != Clue, path + " " + spec.id + " normal milestone");
                            previous = log.WorkFraction;
                        }
                    }
                    Check(profile.operations.Sum(s => s.logs.Length) == spec.logs,
                        path + " " + spec.id + " authored total");
                    content.Add(spec.id, ProfileLogs(profile));
                    ConnectionSpawnPlan manual = traffic.QueueProfile(spec.id);
                    Check(!manual.IsHostile && manual.Operations.Length == profile.operations.Length &&
                        manual.ClaimedPurpose == spec.name &&
                        manual.Target.NodeId == traffic.EffectiveTargetIds(profile)[0] &&
                        manual.Operations.Select(s => Logs(s.PlannedLogs))
                            .SequenceEqual(profile.operations.Select(s => Logs(s.logs))),
                        path + " " + spec.id + " normal plan keeps stage-specific logs");
                }
                ConnectionTrafficProfile evidence = traffic.Profiles.Single(p =>
                    p.id == "logs-checkout-evidence");
                Check(!evidence.purposeTemplate && evidence.isHostile &&
                    evidence.claimedPurpose == "Customer Checkouts" &&
                    evidence.eligibleTargetIds.SequenceEqual(new[] { "API" }) &&
                    traffic.EffectiveAmbientWeight(evidence.id) == 0f &&
                    evidence.operations.Length == 2 &&
                    evidence.operations[0].operation == OperationKind.Write &&
                    evidence.operations[1].operation == OperationKind.Read &&
                    evidence.operations[1].completionEffect == CompletionEffect.InfectionDelivery &&
                    evidence.operations[0].minimumDuration == 50f &&
                    evidence.operations[1].minimumDuration == 35f &&
                    evidence.operations[0].logs.Length == 15 &&
                    evidence.operations[1].logs.Length == 9 &&
                    evidence.operations[0].logs[10].Activity == Clue &&
                    evidence.operations[0].logs[10].Result == "START" &&
                    evidence.operations[0].logs[10].WorkFraction == .54f,
                    path + " manual zero-ambient delivery example");
                content.Add(evidence.id, ProfileLogs(evidence));
                ConnectionTrafficProfile demo = traffic.Profiles.Single(p => p.id == "logs-demo");
                Check(traffic.EffectiveAmbientWeight(demo.id) == 0f &&
                    demo.operations.Sum(s => s.logs.Length) == 10 &&
                    traffic.QueueProfile(demo.id).Operations.Sum(s => s.PlannedLogs.Length) == 10,
                    path + " synthetic logs-demo remains available");
                traffic.ResetDirector();
                var normalSchedules = traffic.Profiles.Where(p => p.purposeTemplate)
                    .ToDictionary(p => p.id, p => p.operations.Select(s => Logs(s.logs)).ToArray());
                for (int i = 0; i < 80; i++)
                {
                    ConnectionSpawnPlan benign = traffic.GenerateBenignPlan(i);
                    ConnectionSpawnPlan threat = traffic.GenerateHostilePlan(i);
                    Check(benign != null && !benign.IsHostile &&
                        benign.Operations.All(s => normalSchedules[benign.ProfileId]
                            .Contains(Logs(s.PlannedLogs))) &&
                        (benign.GenerationTrace.generationDecision != "BENIGN_NORMAL" ||
                            benign.Operations.Select(s => Logs(s.PlannedLogs))
                                .SequenceEqual(normalSchedules[benign.ProfileId])),
                        path + " generated benign operation logs " + i);
                    Check(threat != null && threat.IsHostile &&
                        threat.Operations.All(s => normalSchedules[threat.ProfileId]
                            .Contains(Logs(s.PlannedLogs))) &&
                        threat.Operations.All(s => s.PlannedLogs.All(l => l.Activity != Clue)),
                        path + " generated hostile inherits ordinary logs " + i);
                }
                foreach (ManualObjective objective in new[] {
                    ManualObjective.Loader, ManualObjective.Delivery })
                {
                    ConnectionSpawnPlan forced = traffic.QueueProfile("order-submit",
                        new ConnectionSpawnOverrides(objective));
                    Check(forced.IsHostile && forced.Operations.All(s =>
                        normalSchedules["order-submit"].Contains(Logs(s.PlannedLogs))) &&
                        forced.Operations.All(s => s.PlannedLogs.All(l => l.Activity != Clue)),
                        path + " forced " + objective + " retains ordinary checkout logs");
                }
                traffic.ResetDirector();
                string[] first = Enumerable.Range(0, 24).Select(i =>
                    PlanSignature(traffic.GenerateHostilePlan(i))).ToArray();
                traffic.ResetDirector();
                string[] replay = Enumerable.Range(0, 24).Select(i =>
                    PlanSignature(traffic.GenerateHostilePlan(i))).ToArray();
                Check(first.SequenceEqual(replay), path + " seeded hostile replay");
                return content;
            }
            finally
            {
                traffic.AutoGenerationEnabled = auto;
                traffic.HostileGenerationEnabled = hostile;
                traffic.RandomizeSeed = randomize;
                traffic.Seed = seed;
                host.ResetSimulation();
            }
        }

        private static void TimingAndEvidence()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            bool auto = traffic.AutoGenerationEnabled;
            bool hostile = traffic.HostileGenerationEnabled;
            var travelField = typeof(SimulationHost).GetField(
                "connectionTravelSecondsPerLink", BindingFlags.Instance | BindingFlags.NonPublic);
            float travel = (float)travelField.GetValue(host);
            try
            {
                traffic.AutoGenerationEnabled = false;
                traffic.HostileGenerationEnabled = false;
                travelField.SetValue(host, .000001f);
                host.SetRunMode(ConnectionRunMode.Generated);
                recorder.BeginRun();
                ConnectionSpawnPlan evidence = traffic.QueueProfile("logs-checkout-evidence");
                Check(evidence.IsHostile && evidence.Target.NodeId == "API" &&
                    evidence.Operations[1].EffectOnCompletion == CompletionEffect.InfectionDelivery &&
                    evidence.Operations[1].InfectionPayload?.Kind == InfectionKind.PhantomLoad,
                    "fixed plan carries actual Phantom LOAD delivery");
                for (int i = 0; i < 100 && host.FindConnection(evidence.ConnectionId) == null; i++)
                    host.StepSimulation(1f);
                LogicalConnectionState state = host.FindConnection(evidence.ConnectionId);
                Check(state != null, "manual example spawned");
                for (int i = 0; i < 150 && !state.EmittedLogs.Any(e => e.Activity == Clue); i++)
                    host.StepSimulation(1f);
                EmittedConnectionLog clue = state.EmittedLogs.Single(e => e.Activity == Clue);
                Check(state.IsActive && state.CurrentOperationIndex == 0 &&
                    clue.Operation == OperationKind.Write && clue.Result == "START" &&
                    host.Infections.Count == 0 && host.Occurrences.Count == 0,
                    "fictional clue emits before any infection or completion effect");
                Check(recorder.CurrentLog.events.Any(e => e.type == "plan_queued" &&
                    e.connectionId == evidence.ConnectionId &&
                    e.definition.operations[0].plannedLogs.Length == 15 &&
                    e.definition.operations[1].plannedLogs.Length == 9) &&
                    recorder.CurrentLog.events.Any(e => e.type == "connection_log_emitted" &&
                        e.connectionId == evidence.ConnectionId &&
                        e.logAuthoredIndex == 10 && e.timeSeconds == clue.TimeSeconds),
                    "recorder retains concrete plan and clue emission");
                for (int i = 0; i < 200 && !state.IsComplete; i++)
                    host.StepSimulation(1f);
                Check(state.IsComplete && state.EmittedLogs.Count == 24 &&
                    !state.EmittedLogs.Skip(19).Any(e => e.Activity == Clue) &&
                    host.Infections.Count == 1 &&
                    host.Infections[0].Target.View.NodeId == "API" &&
                    host.Infections[0].SourceConnectionId == evidence.ConnectionId &&
                    host.Infections[0].AppliedAtSeconds > clue.TimeSeconds &&
                    recorder.CurrentLog.events.Count(e => e.type == "connection_log_emitted" &&
                        e.connectionId == evidence.ConnectionId) == 24,
                    "later ordinary logs displace clue; final READ delivers once");
                Check(recorder.Save(false), "evidence run recording saved");
                ConnectionsRunLog saved = JsonUtility.FromJson<ConnectionsRunLog>(
                    File.ReadAllText(recorder.OutputPath));
                Check(saved.events.Any(e => e.type == "plan_queued" &&
                    e.connectionId == evidence.ConnectionId &&
                    e.definition.operations[0].plannedLogs[10].activity == Clue) &&
                    saved.events.Count(e => e.type == "connection_log_emitted" &&
                        e.connectionId == evidence.ConnectionId) == 24,
                    "saved JSON retains authored clue and earned entries");

                PlannedOperationLog[] logs = traffic.Profiles.Single(p =>
                    p.id == "product-lookup").operations[0].logs;
                NodeView[] route = { host.FindNodeResources("EXT1").View,
                    host.FindNodeResources("API").View, host.FindNodeResources("DB").View };
                int fast = Fixture(host, traffic, route, logs, "fast", 10f, 2f, false);
                int slow = Fixture(host, traffic, route, logs, "slow", 20f, 2f, false);
                int alone = Fixture(host, traffic, route, logs, "alone", 20f, 8f, false);
                int contested = Fixture(host, traffic, route, logs, "contested", 20f, 8f, true);
                Check(fast > slow && alone > contested,
                    "WORK duration and shared LOAD contention change earned log timing");
                Debug.Log("PurposeLogsWitness timing: fast/slow " + fast + "/" + slow +
                    ", alone/contended " + alone + "/" + contested + " after one second.");
            }
            finally
            {
                travelField.SetValue(host, travel);
                traffic.AutoGenerationEnabled = auto;
                traffic.HostileGenerationEnabled = hostile;
                host.ResetSimulation();
            }
        }

        private static int Fixture(SimulationHost host, ConnectionTrafficDirector traffic,
            NodeView[] route, PlannedOperationLog[] logs, string id,
            float work, float load, bool competitor)
        {
            host.ResetSimulation();
            var stage = new OperationStage(OperationKind.Read, work, load,
                "Reading product details", logs: logs);
            traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, id, route,
                "product-lookup", "Product Detail Lookups", new[] { stage }));
            if (competitor)
                traffic.QueueSpawnPlan(new ConnectionSpawnPlan(0f, id + "-peer", route,
                    "contention-witness", "Other work", new[] {
                        new OperationStage(OperationKind.Read, 100f, load, "Competing read") }));
            host.StepSimulation(.01f);
            host.StepSimulation(1f);
            return host.FindConnection(id).EmittedLogs.Count;
        }

        private static string ProfileLogs(ConnectionTrafficProfile profile) =>
            string.Join("|", profile.operations.Select(s => Logs(s.logs)));

        private static string Logs(IEnumerable<PlannedOperationLog> logs) =>
            string.Join(";", logs.Select(l => l.WorkFraction.ToString("R",
                CultureInfo.InvariantCulture) + ":" + l.Activity + ":" + l.Result));

        private static string PlanSignature(ConnectionSpawnPlan plan) =>
            plan.ProfileId + "|" + plan.ConnectionId + "|" + plan.IsHostile + "|" +
            string.Join("|", plan.Operations.Select(s =>
                s.Operation + ":" + s.TotalWork.ToString("R", CultureInfo.InvariantCulture) +
                ":" + s.DesiredLoad.ToString("R", CultureInfo.InvariantCulture) +
                ":" + Logs(s.PlannedLogs)));

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("PurposeLogsWitness: " + label);
        }
    }
}
