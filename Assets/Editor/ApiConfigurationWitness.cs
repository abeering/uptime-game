using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class ApiConfigurationWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsApiConfigurationV0";
        private static readonly List<string> checks = new(), outcomes = new(), policies = new();
        public static void Set(object o, string field, object value) => FragilityWitness.Set(o, field, value);
        private static void Check(bool ok, string label) { checks.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); }
        private static void Near(double actual, double expected, string label) => Check(Math.Abs(actual - expected) < .001, label + $": expected {expected}, actual {actual}");
        public static SimulationHost Open(string scene)
        {
            var h = BusinessShiftWitness.Open(scene, 100000, 100); Check(h.ApiConfigurationNode != null && h.ApiConfigurationNode.NodeId == "API" && h.ApiAllocationWeight == 2 && h.ApiFavoredPurposeId == "", scene + " authored API node/2x and empty initial slot");
            foreach (var n in h.Nodes) { Set(n, "loadCapacity", 10f); Set(n, "baselineLoad", 0f); }
            h.ResetSimulation(); h.Recorder.BeginRun(); return h;
        }
        public static NodeView[] Route(SimulationHost h, string target = "DB") => (NodeView[])typeof(ConnectionTrafficDirector).GetMethod("FindRoute", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(h.GetComponent<ConnectionTrafficDirector>(), new object[] { h.FindNodeResources("EXT1").View, h.FindNodeResources(target).View });
        public static LogicalConnectionState Add(SimulationHost h, string id, string purpose, float load = 8, float work = 1000, string target = "DB", bool hostile = false, string label = null)
        {
            var claim = label ?? h.GetComponent<ConnectionTrafficDirector>().Profiles.Single(p => p.id == purpose).claimedPurpose;
            return h.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(h.SimulationTimeSeconds, id, Route(h, target), purpose, claim,
                new[] { new OperationStage(OperationKind.Read, work, load, "API allocation witness") }, hostile)));
        }
        public static void Establish(SimulationHost h) => h.StepSimulation(4);
        [MenuItem("Tools/Connections/Run API Configuration Witness")]
        public static void Run()
        {
            checks.Clear(); outcomes.Clear(); policies.Clear(); policies.Add("scene,time,node,previous_purpose,favored_purpose,weight"); Directory.CreateDirectory(Evidence); outcomes.Add("scene,policy,product_resolution,sales_resolution,credited_business_work,failures,ending");
            try
            {
                foreach (string scene in CanonicalSceneAuthoring.Scenes)
                {
                    var h = Open(scene); var a = Add(h, "a", "product-lookup"); var b = Add(h, "b", "sales-export"); Establish(h);
                    Near(a.AllocatedLoad, 5, scene + " empty slot original allocation A"); Near(b.AllocatedLoad, 5, scene + " empty slot original allocation B");
                    float budget = a.ServiceBudgetSeconds, deadline = a.ServiceDeadlineSeconds; double value = a.BusinessWorkValue;
                    Check(h.TrySetApiPurpose("product-lookup", out _), scene + " known stable PURPOSE selected"); Near(a.AllocatedLoad, 5, scene + " selection waits for allocation update");
                    h.StepSimulation(.02f); Near(a.AllocatedLoad, 20f / 3, scene + " favored8 receives6.67"); Near(b.AllocatedLoad, 10f / 3, scene + " ordinary8 receives3.33");
                    Check(a.DesiredLoad == 8 && a.CurrentTotalWork == 1000 && a.BusinessWorkValue == value && a.ServiceBudgetSeconds == budget && a.ServiceDeadlineSeconds == deadline && a.AllocationWeight == 2,
                        scene + " weight never changes requested LOAD, processing/business WORK or service target");
                    h.TrySetApiPurpose("sales-export", out _); h.StepSimulation(.02f); Near(b.AllocatedLoad, 20f / 3, scene + " change policy favors existing B");
                    h.TrySetApiPurpose(null, out _); h.StepSimulation(.02f); Near(a.AllocatedLoad, 5, scene + " clear restores A"); Near(b.AllocatedLoad, 5, scene + " clear restores B");
                    Check(!h.TrySetApiPurpose("Product Detail Lookups", out _) && h.ApiFavoredPurposeId == "", scene + " display labels are not policy identities");
                    h.TrySetApiPurpose("product-lookup", out _); h.StepSimulation(.02f); h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds);
                    var snapshot = h.Recorder.CurrentLog.snapshots.Last();
                    Check(h.Recorder.CurrentLog.events.Count(e => e.type == "api_policy_changed") == 4 && snapshot.apiFavoredPurposeId == "product-lookup" &&
                        snapshot.connections.Any(c => c.id == "a" && c.allocationWeight == 2 && c.allocationPolicyNodeId == "API") &&
                        h.Recorder.CurrentLog.run.apiAllocationWeight == 2, scene + " recorder joins policy changes, configured weight and applied allocation evidence");
                    policies.AddRange(h.Recorder.CurrentLog.events.Where(e => e.type == "api_policy_changed").Select(e =>
                        FormattableString.Invariant($"{Path.GetFileNameWithoutExtension(scene)},{e.timeSeconds},{e.policyNodeId},{e.previousPurposeId},{e.favoredPurposeId},{e.priorityWeight}")));
                    int receipts = h.Recorder.CurrentLog.events.Count(e => e.type == "api_policy_changed"); h.TrySetApiPurpose("product-lookup", out _);
                    Check(h.Recorder.CurrentLog.events.Count(e => e.type == "api_policy_changed") == receipts, scene + " repeated same policy adds no duplicate change event");
                    var newcomer = Add(h, "new", "product-lookup"); Establish(h); Near(a.AllocatedLoad, 4, scene + " existing favored shares with new arrival"); Near(newcomer.AllocatedLoad, 4, scene + " newly established favored shares equally"); Near(b.AllocatedLoad, 2, scene + " ordinary peer retains weighted share");
                    h.ResetSimulation(); Check(h.ApiFavoredPurposeId == "" && h.CreditedWork == 0 && h.LegitimateFailureCount == 0, scene + " restart clears policy and objectives");

                    h = Open(scene); a = Add(h, "small", "product-lookup", 1); b = Add(h, "large", "sales-export", 4); Set(h.FindNodeResources("DB"), "loadCapacity", 4.5f);
                    h.TrySetApiPurpose("product-lookup", out _); Establish(h); Near(a.AllocatedLoad, 1, scene + " favored request cap"); Near(b.AllocatedLoad, 3.5, scene + " unused share redistributed");
                    h = Open(scene); a = Add(h, "u", "product-lookup", 8); b = Add(h, "v", "sales-export", 2); h.TrySetApiPurpose("product-lookup", out _); Establish(h);
                    Near(a.AllocatedLoad, 8, scene + " uncontended favored unchanged"); Near(b.AllocatedLoad, 2, scene + " uncontended ordinary unchanged");
                    h = Open(scene); a = Add(h, "m1", "product-lookup"); b = Add(h, "m2", "product-lookup"); var c = Add(h, "m3", "sales-export"); h.TrySetApiPurpose("product-lookup", out _); Establish(h);
                    Near(a.AllocatedLoad, 4, scene + " multiple favored A"); Near(b.AllocatedLoad, 4, scene + " multiple favored B"); Near(c.AllocatedLoad, 2, scene + " multiple favored ordinary peer");
                    h = Open(scene); a = Add(h, "cap1", "product-lookup", 2); b = Add(h, "cap2", "product-lookup", 2); c = Add(h, "rest", "sales-export", 8); h.TrySetApiPurpose("product-lookup", out _); Establish(h);
                    Near(a.AllocatedLoad, 2, scene + " multiple saturated favored A"); Near(b.AllocatedLoad, 2, scene + " multiple saturated favored B"); Near(c.AllocatedLoad, 6, scene + " all capped shares redistributed");
                    h = Open(scene); a = Add(h, "target", "cart-pricing", target: "API"); b = Add(h, "peer", "order-submit", target: "API"); h.TrySetApiPurpose("cart-pricing", out _); Establish(h);
                    Near(a.AllocatedLoad, 20f / 3, scene + " connections targeting API qualify"); Near(b.AllocatedLoad, 10f / 3, scene + " API target peer weighted");
                    h = Open(scene); a = Add(h, "evil", "product-lookup", hostile: true, label: "Different display label"); b = Add(h, "ordinary", "sales-export"); h.TrySetApiPurpose("product-lookup", out _); Establish(h);
                    Near(a.AllocatedLoad, 20f / 3, scene + " hostile stable claimed PURPOSE benefits equally"); Check(a.IsHostile && a.AllocationWeight == 2, scene + " policy never filters hidden hostility");
                    h = Open(scene); a = Add(h, "honest", "product-lookup"); b = Add(h, "same-label", "sales-export", label: a.Definition.ClaimedPurpose); h.TrySetApiPurpose("product-lookup", out _); Establish(h);
                    Check(a.AllocationWeight == 2 && b.AllocationWeight == 1, scene + " equal display strings do not create equal PURPOSE identity");
                    h = Open(scene); a = Add(h, "db", "product-lookup"); b = Add(h, "auth", "sales-export", target: "AUTH"); h.TrySetApiPurpose("product-lookup", out _); Establish(h);
                    Near(a.AllocatedLoad, 8, scene + " independent DB target"); Near(b.AllocatedLoad, 8, scene + " independent AUTH target");
                    if (scene == CanonicalSceneAuthoring.Scenes[1])
                    {
                        h = Open(scene); a = Add(h, "bypass", "product-lookup", target: "WEB"); b = Add(h, "bypass-peer", "sales-export", target: "WEB"); h.TrySetApiPurpose("product-lookup", out _); Establish(h);
                        Check(!a.Definition.Route.Any(n => n.Node == h.ApiConfigurationNode) && a.AllocationWeight == 1, "Advanced real EXT1→WEB bypass excludes API policy"); Near(a.AllocatedLoad, 5, "Advanced bypass retains original proportional allocation");
                    }
                    h = Open(scene); a = Add(h, "reserved-a", "product-lookup"); b = Add(h, "reserved-b", "sales-export"); Set(h.FindNodeResources("DB"), "baselineLoad", 1f);
                    h.TryInjectInfection("DB", InfectionKind.PhantomLoad, 2, out _); h.TrySetApiPurpose("product-lookup", out _); Establish(h);
                    Check(h.Payloads.TryIntercept(null, "DB", out _), scene + " existing defense reservation activated"); h.StepSimulation(.02f);
                    Near(h.FindNodeResources("DB").InfectionAllocatedLoad, 2, scene + " infection keeps reserved2"); Near(h.FindNodeResources("DB").DefensiveAllocatedLoad, 4, scene + " defense keeps reserved4"); Near(a.AllocatedLoad, 2, scene + " remaining3 favors A"); Near(b.AllocatedLoad, 1, scene + " remaining3 ordinary B");
                    Near(h.FindNodeResources("DB").AllocatedLoad, 10, scene + " no total capacity inflation");
                    h = Open(scene); Set(h, "apiAllocationWeight", 3f); h.ResetSimulation(); a = Add(h, "weight3", "product-lookup"); b = Add(h, "peer3", "sales-export");
                    h.TrySetApiPurpose("product-lookup", out _); Establish(h); Near(a.AllocatedLoad, 7.5, scene + " configurable3x weight"); Near(b.AllocatedLoad, 2.5, scene + " configurable3x ordinary peer");
                    Check(h.KillConnection("weight3") && h.LegitimateFailureCount == 1 && h.CreditedWork == 0, scene + " favored kill retains exact-once failure and zero business credit");
                    h.StepSimulation(.02f); Near(b.AllocatedLoad, 8, scene + " killed favored releases all target resources");
                    h = Open(scene); LoadFixture(h); Set(h, "shiftWorkTarget", 32f); h.TrySetApiPurpose("product-lookup", out _); h.StepSimulation(30);
                    Check(h.Ending == RunEnding.ShiftComplete && h.CreditedWork == 32 && h.WorkCreditCount == 1 && h.LegitimateFailureCount == 0 &&
                        !h.TrySetApiPurpose("sales-export", out _), scene + " favored unscaled whole-job credit ends shift and freezes unfinished peer without fabricated failure");
                    foreach (string selected in new[] { "", "product-lookup", "sales-export" })
                    {
                        h = Open(scene); LoadFixture(h); if (selected != "") h.TrySetApiPurpose(selected, out _); h.StepSimulation(30);
                        var product = h.FindConnection(FixtureProduct); var sales = h.FindConnection(FixtureSales);
                        Check(selected == "" ? h.Ending == RunEnding.Fired && h.CreditedWork == 0 && h.LegitimateFailureCount == 2 :
                            h.Ending == RunEnding.None && Math.Abs(h.CreditedWork - 32) < .001 && h.LegitimateFailureCount == 1 &&
                            (selected == "product-lookup" ? product.Resolution == ConnectionResolution.Completed && sales.Resolution == ConnectionResolution.TimedOut : sales.Resolution == ConnectionResolution.Completed && product.Resolution == ConnectionResolution.TimedOut),
                            scene + " generated 2.5x tradeoff: " + (selected == "" ? "NONE loses both/FIRED" : selected + " saved32 business WORK at expense of peer"));
                        outcomes.Add(FormattableString.Invariant($"{Path.GetFileNameWithoutExtension(scene)},{selected},{product.Resolution},{sales.Resolution},{h.CreditedWork},{h.LegitimateFailureCount},{h.Ending}"));
                        if (h.RunEnded) Check(!h.TrySetApiPurpose("product-lookup", out _), scene + " terminal outcome rejects policy mutation");
                    }
                }
            }
            finally { File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks); File.WriteAllLines(Evidence + "/tradeoff-outcomes.csv", outcomes); File.WriteAllLines(Evidence + "/policy-change-events.csv", policies); }
            Debug.Log("ApiConfigurationWitness PASS: " + checks.Count + " assertions.");
        }
        public static string FixtureProduct { get; private set; }
        public static string FixtureSales { get; private set; }
        public static void LoadFixture(SimulationHost h)
        {
            var t = h.GetComponent<ConnectionTrafficDirector>(); t.AutoGenerationEnabled = t.HostileGenerationEnabled = false; t.RandomizeSeed = false; t.Seed = 1701;
            Set(t, "ambientOrigins", Array.Empty<AmbientOriginWeight>()); Set(t, "purposeTargetOverrides", Array.Empty<PurposeTargetOverride>());
            Set(t, "legitimateWorkloadMultiplier", 2.5f); Set(t, "normalWorkSpread", 0f); Set(t, "benignOutlierChance", 0f); Set(h, "naturalLoadNoiseEnabled", false);
            Set(h, "shiftEnabled", true); Set(h, "shiftWorkTarget", 64f); Set(h, "businessFailureLimit", 2);
            foreach (var n in h.Nodes) { Set(n, "loadCapacity", 10f); Set(n, "baselineLoad", 0f); }
            foreach (string id in new[] { "product-lookup", "sales-export" })
            {
                var p = t.Profiles.Single(x => x.id == id); p.eligibleTargetIds = new[] { "DB" };
                foreach (var op in p.operations) { op.minimumLoad = op.maximumLoad = 3.2f; op.meanWork = 32; }
            }
            h.ResetSimulation(); h.Recorder.BeginRun();
            FixtureProduct = t.QueueProfile("product-lookup").ConnectionId; FixtureSales = t.QueueProfile("sales-export").ConnectionId;
        }
        [MenuItem("Tools/Connections/Load API PURPOSE Tradeoff (Play Mode only)")]
        public static void LoadPlayFixture()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter canonical Play Mode first; fixture settings are never saved.");
            LoadFixture(UnityEngine.Object.FindFirstObjectByType<SimulationHost>());
            Debug.Log("API tradeoff: " + FixtureProduct + " product-lookup / " + FixtureSales + " sales-export. ssh api, choose either before about10 active seconds. No policy loses both; favoring one saves its32 business WORK and loses the peer. Restart resets policy; stop/reload to discard fixture tuning.");
        }
        public static void Regressions()
        {
            Directory.CreateDirectory(Evidence); var report = new List<string>();
            foreach (var test in new (string name, Action run)[] { ("API", Run), ("Shift", BusinessShiftWitness.Run), ("Fragility", FragilityWitness.Run),
                ("Demand", IndependentBusinessDemandWitness.Run), ("Workload", LegitimateWorkloadWitness.Run), ("Integrity", IntegrityWitness.Run),
                ("Traffic", TrafficWitness.Run), ("Recorder", RecorderWitness.Run), ("Canonical", CanonicalSceneWitness.Run), ("DuelConcession", DuelConcessionWitness.Run),
                ("Resource", ResourceWitness.Run), ("Overclock", OverclockWitness.Run), ("GeneratedIdentity", GeneratedIdentityWitness.Run) })
            { try { test.run(); report.Add("PASS " + test.name); } catch (Exception e) { report.Add("FAIL " + test.name + ": " + e.Message); Debug.LogException(e); } }
            File.WriteAllLines(Evidence + "/regression-checks.txt", report);
            if (report.Any(x => x.StartsWith("FAIL"))) throw new Exception("API regression failures; compare inherited leaf assertions in report.");
        }
    }
}
