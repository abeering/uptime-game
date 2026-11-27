using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class OperationRoutingCacheWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsOperationRoutingCacheV0";
        private static readonly List<string> checks = new(), outcomes = new();
        public static void Set(object o, string field, object value) => FragilityWitness.Set(o, field, value);
        private static void Check(bool ok, string message) { checks.Add((ok ? "PASS " : "FAIL ") + message); if (!ok) throw new Exception(message); }
        private static void Near(double value, double expected, string label) => Check(Math.Abs(value - expected) < .003, label + $": expected {expected}, observed {value}");
        public static SimulationHost Open(string scene)
        {
            var h = BusinessShiftWitness.Open(scene, 100000, 100);
            Check(h.CacheConfigurationNode != null && h.CacheConfigurationNode.NodeId == "CACHE" && h.CachedWorkMultiplier == .25f && h.CachedLoadMultiplier == .5f && h.CachePurposeId == "", scene + " authored CACHE/quarter WORK/half LOAD/empty slot");
            Check(h.FindNodeResources("CACHE").LoadCapacity == 10 && h.PhysicalLinks.Count(l => l.View.NodeA == h.CacheConfigurationNode || l.View.NodeB == h.CacheConfigurationNode) == 1, scene + " ordinary capacity10 leaf node");
            foreach (var n in h.Nodes) { Set(n, "loadCapacity", 10f); Set(n, "baselineLoad", 0f); }
            h.ResetSimulation(); h.Recorder.BeginRun(); return h;
        }
        public static LogicalConnectionState Add(SimulationHost h, string id, OperationStage[] stages = null, string target = "DB", string purpose = "product-lookup", bool hostile = false, string label = null)
        {
            var p = new ConnectionSpawnPlan(h.SimulationTimeSeconds, id, ApiConfigurationWitness.Route(h, target), purpose, label ?? "Product Detail Lookups",
                stages ?? new[] { new OperationStage(OperationKind.Read, 100, 5, "Read") }, hostile);
            return h.RegisterConnection(LogicalConnectionDefinition.FromPlan(p));
        }
        public static void Until(SimulationHost h, Func<bool> done, float limit = 60)
        {
            float end = h.SimulationTimeSeconds + limit;
            while (!done() && !h.RunEnded && h.SimulationTimeSeconds < end) h.StepSimulation(.02f);
            Check(done(), "bounded simulation reaches expected boundary");
        }
        [MenuItem("Tools/Connections/Run Operation Routing CACHE Witness")]
        public static void Run()
        {
            checks.Clear(); outcomes.Clear(); Directory.CreateDirectory(Evidence); outcomes.Add("scene,cache,product_resolution,sales_resolution,business_work,failures,ending");
            try
            {
                foreach (string scene in CanonicalSceneAuthoring.Scenes)
                {
                    var h = Open(scene); var a = Add(h, "empty"); h.StepSimulation(4);
                    Check(!a.IsCached && a.TargetNode.NodeId == "DB" && a.Route.SequenceEqual(a.Definition.Route), scene + " empty policy retains original authored physical route");
                    Near(a.DesiredLoad, 5, "empty requested LOAD"); Near(a.CurrentTotalWork, 100, "empty processing WORK"); Near(a.BusinessWorkValue, 100, "empty business value"); Near(a.ServiceBudgetSeconds, 30, "empty service budget");
                    h = Open(scene); Check(h.TrySetCachePurpose("PRODUCT-LOOKUP", out _), "known stable read PURPOSE canonicalized");
                    a = Add(h, "cached", new[] { new OperationStage(OperationKind.Read, 100, 5, "Read with evidence", logs: new[] {
                        new PlannedOperationLog(.1f, "first"), new PlannedOperationLog(.5f, "middle"), new PlannedOperationLog(1, "last") }) });
                    Check(a.IsCached && a.TargetNode == h.CacheConfigurationNode && a.Routing.IntendedDestination.NodeId == "DB", scene + " READ redirects before traversal");
                    Near(a.CurrentTotalWork, 25, "cached WORK25"); Near(a.DesiredLoad, 2.5, "cached LOAD2.5"); Near(a.BusinessWorkValue, 100, "cached original business100");
                    Check(a.Definition.WorkAt(0) == 100 && a.Definition.LoadAt(0) == 5, "concrete source definition remains unmodified");
                    var locked = a.Routing; var lockedRoute = a.Route; h.StepSimulation(4); Near(a.ServiceBudgetSeconds, 30, "cached retains original30 service budget");
                    h.TrySetCachePurpose(null, out _); h.TrySetCachePurpose("sales-export", out _); h.StepSimulation(.02f);
                    Check(ReferenceEquals(a.Routing, locked) && ReferenceEquals(a.Route, lockedRoute) && a.IsCached && a.CurrentTotalWork == 25, "policy changes never reroute/reprice committed READ");
                    Until(h, () => a.IsComplete); Near(h.CreditedWork, 100, "whole cached job credits original business value");
                    Check(h.WorkCreditCount == 1 && h.LegitimateFailureCount == 0 && a.EmittedLogs.Select(l => l.Activity).SequenceEqual(new[] { "first", "middle", "last" }) && a.EmittedLogs.Last().WorkThreshold == 25, "cached completion preserves every earned evidence milestone and exact-once credit");
                    var routing = h.Recorder.CurrentLog.events.Single(e => e.type == "operation_route_committed").operationRouting;
                    Check(routing.cached && routing.selectedCachePurpose == "product-lookup" && routing.actualDestination == "CACHE" && routing.intendedDestination == "DB" && routing.processingWork == 25 && routing.processingLoad == 2.5 && routing.originalWork == 100 && routing.route.Last() == "CACHE" && h.Recorder.CurrentLog.run.cachedWorkMultiplier == .25f, "recorder joins original requirements, committed path, locked efficiency and metadata");
                    File.WriteAllText(Evidence + "/" + Path.GetFileNameWithoutExtension(scene) + "-route-record.json", JsonUtility.ToJson(routing, true));
                    foreach (var kind in new[] { OperationKind.Write, OperationKind.Execute })
                    {
                        h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); a = Add(h, "not-read", new[] { new OperationStage(kind, 100, 5, "Ordinary") });
                        Check(!a.IsCached && a.TargetNode.NodeId == "DB" && a.CurrentTotalWork == 100 && a.DesiredLoad == 5, kind + " keeps ordinary destination/requirements");
                    }
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); a = Add(h, "nonmatch", purpose: "sales-export", label: "Product Detail Lookups");
                    Check(!a.IsCached && a.TargetNode.NodeId == "DB", "equal display string is not a PURPOSE match");
                    Check(!h.TrySetCachePurpose("Product Detail Lookups", out _) && !h.TrySetCachePurpose("cart-pricing", out _), "reject display strings and catalog classes without READ");
                    h = Open(scene); a = Add(h, "intended", new[] { new OperationStage(OperationKind.Read, 20, 4, "AUTH destination", intendedDestination: h.FindNodeResources("AUTH").View) });
                    Check(a.TargetNode.NodeId == "AUTH" && a.Definition.TargetNode.NodeId == "DB" && !a.IsCached, "operation ordinary intended destination is independent of logical plan default");
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); a = Add(h, "launch-read", new[] { new OperationStage(OperationKind.Read, 20, 4, "Launch", CompletionEffect.PayloadLaunch, launch: new PayloadLaunchSpec("DB", PayloadMotionProfile.Steady)) }, target: "API", hostile: true);
                    Check(!a.IsCached && a.TargetNode.NodeId == "API" && a.Routing.Reason == "cache_effect_route_incompatible", "READ requiring an adjacent launch destination falls back without sanitizing its effect");
                    h.StepSimulation(8); Check(h.Occurrences.Any(e => e.Type == CompletionEffect.PayloadLaunch && e.TargetNodeId == "API" && e.LaunchedPayloadId != null), "fallback READ retains actual Payload launch");
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _);
                    a = Add(h, "mixed", new[] { new OperationStage(OperationKind.Read, 8, 4, "Cached read"), new OperationStage(OperationKind.Write, 20, 4, "Ordinary write"), new OperationStage(OperationKind.Read, 8, 4, "Later read") });
                    Until(h, () => a.IsActive); float deadline = a.ServiceDeadlineSeconds; double business = a.BusinessWorkValue;
                    h.TrySetCachePurpose(null, out _); Check(a.IsCached, "clear leaves current cached operation locked");
                    Until(h, () => a.CurrentOperationIndex == 1 && a.Routing != null);
                    Check(a.IsRerouting && a.TargetNode.NodeId == "DB" && a.OperationDestinationAt(0).NodeId == "CACHE" && a.ConnectionId == "mixed" && !a.IsCached, "mixed next WRITE changes route but retains identity and previous destination history");
                    Check(a.HasEstablished && a.ServiceDeadlineSeconds == deadline && a.BusinessWorkValue == business && h.CreditedWork == 0, "route traversal retains original deadline/value and no partial credit");
                    h.TrySetCachePurpose("product-lookup", out _); Until(h, () => a.CurrentOperationIndex == 2 && a.Routing != null);
                    Check(a.IsCached && a.TargetNode.NodeId == "CACHE" && a.IsRerouting && a.ServiceDeadlineSeconds == deadline, "next READ independently uses then-current policy");
                    Until(h, () => a.IsComplete); Check(a.Resolution == ConnectionResolution.Completed && h.CreditedWork == 36 && h.WorkCreditCount == 1, "mixed arc credits once only after final operation");
                    Check(h.Recorder.CurrentLog.events.Count(e => e.type == "operation_route_committed") == 3 && h.Recorder.CurrentLog.events.Count(e => e.type == "operation_started") == 3 && h.Recorder.CurrentLog.events.Count(e => e.type == "connection_established") == 1, "three operation decisions/starts, one persistent connection establishment");
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); var peers = Enumerable.Range(0, 3).Select(i => Add(h, "cache" + i, new[] { new OperationStage(OperationKind.Read, 1000, 8, "Contended") })).ToArray();
                    var ordinary = Add(h, "ordinary", new[] { new OperationStage(OperationKind.Read, 1000, 8, "DB") }, purpose: "sales-export"); h.StepSimulation(4);
                    foreach (var peer in peers) Near(peer.AllocatedLoad, 10f / 3, "CACHE ordinary proportional competition"); Near(ordinary.AllocatedLoad, 8, "DB independently relieved"); Near(h.FindNodeResources("CACHE").ConnectionRequestedLoad, 12, "cached effective node demand");
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); a = Add(h, "priority-a", new[] { new OperationStage(OperationKind.Read, 1000, 8, "Cache A") });
                    h.TrySetCachePurpose("sales-export", out _); var cachedSales = Add(h, "priority-b", new[] { new OperationStage(OperationKind.Read, 1000, 8, "Cache B") }, purpose: "sales-export");
                    Set(h.FindNodeResources("CACHE"), "loadCapacity", 5f); h.TrySetApiPurpose("product-lookup", out _); h.StepSimulation(4);
                    Near(a.AllocatedLoad, 10f / 3, "live API priority favors first committed cached class"); Near(cachedSales.AllocatedLoad, 5f / 3, "second committed cached class retains weighted share");
                    h.TrySetApiPurpose("sales-export", out _); h.StepSimulation(.02f); Near(cachedSales.AllocatedLoad, 10f / 3, "API can change live shares without rerouting either cached class");
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); h.TryInjectInfection("CACHE", InfectionKind.Blackout, 0, out _); a = Add(h, "unavailable");
                    Check(!a.IsCached && a.TargetNode.NodeId == "DB" && a.Routing.Reason == "cache_unavailable_or_unreachable", "offline CACHE falls back only during resolution");
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); Set(h, "physicalLinks", h.PhysicalLinks.Where(l => l.View.NodeA != h.CacheConfigurationNode && l.View.NodeB != h.CacheConfigurationNode).ToArray()); a = Add(h, "unreachable");
                    Check(!a.IsCached && a.TargetNode.NodeId == "DB", "unreachable CACHE falls back to unchanged ordinary route");
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _);
                    a = h.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0, "cache-source", new[] { h.CacheConfigurationNode, h.ApiConfigurationNode, h.FindNodeResources("DB").View }, "product-lookup", "Read", new[] { new OperationStage(OperationKind.Read, 100, 5, "Read from cache source") })));
                    Check(!a.IsCached && a.TargetNode.NodeId == "DB" && a.Routing.Reason == "cache_local_source_unsupported", "CACHE-origin requests retain ordinary route; zero-link local execution is not invented");
                    foreach (bool afterEstablishment in new[] { false, true })
                    {
                        h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); a = Add(h, "locked-blackout"); if (afterEstablishment) h.StepSimulation(4);
                        locked = a.Routing; h.TryInjectInfection("CACHE", InfectionKind.Blackout, 0, out _); h.TrySetCachePurpose(null, out _); h.StepSimulation(4);
                        Check(a.Resolution == ConnectionResolution.Canceled && a.TerminationCause == ConnectionTerminationCause.Blackout && ReferenceEquals(a.Routing, locked) && a.TargetNode.NodeId == "CACHE" && h.LegitimateFailureCount == 1 && h.CreditedWork == 0, "locked CACHE BLACKOUT " + (afterEstablishment ? "processing" : "traversal") + " uses existing failure semantics without failover");
                    }
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); a = Add(h, "hostile", new[] { new OperationStage(OperationKind.Read, 20, 4, "Read", CompletionEffect.InfectionDelivery, payload: new InfectionPayload(InfectionKind.PhantomLoad, 2)) }, hostile: true); h.StepSimulation(8);
                    Check(a.IsCached && a.Resolution == ConnectionResolution.Completed && h.Infections.Single().Target.View.NodeId == "CACHE" && h.Infections.Single().Payload.RequestedLoad == 2 && h.Occurrences.Count == 1 && h.CreditedWork == 0 && h.LegitimateFailureCount == 0, "matching hostile READ retains one real infection effect at actual CACHE destination and zero business credit");
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); a = Add(h, "behavior", new[] { new OperationStage(OperationKind.Read, 100, 4, "Keywords", behaviors: new[] { TimedModifierBehaviorDefinition.Overload("demand", 0, 8), TimedModifierBehaviorDefinition.Rate("paused", 0, 0, 0) }) }, hostile: true); h.StepSimulation(4);
                    Check(a.IsCached && a.Behaviors.ActiveModifiers.Count == 2 && a.EffectiveWorkRateMultiplier == 0 && a.CompletedWork == 0 && a.DesiredLoad == 6, "CACHE preserves hostile behavior activation, cheaper composed demand and paused WORK");
                    Check(h.KillConnection("behavior") && a.Behaviors.ActiveModifiers.Count == 0 && h.Infections.Count == 0, "kill cleans cached behaviors without effects");
                    if (scene == CanonicalSceneAuthoring.Scenes[1])
                    {
                        h = Open(scene); h.TrySetApiPurpose("product-lookup", out _); a = Add(h, "web-old", new[] { new OperationStage(OperationKind.Read, 100, 4, "WEB") }, target: "WEB"); h.TrySetCachePurpose("product-lookup", out _);
                        var b = Add(h, "web-cache", new[] { new OperationStage(OperationKind.Read, 20, 4, "CACHE"), new OperationStage(OperationKind.Write, 100, 4, "WEB") }, target: "WEB"); h.StepSimulation(2);
                        Check(a.AllocationWeight == 1 && b.AllocationWeight == 2 && b.Route.Length == 4 && b.Definition.Route.Length == 2, "API eligibility uses actual cached path, not immutable WEB plan route");
                        Until(h, () => b.CurrentOperationIndex == 1 && b.Routing != null); h.StepSimulation(.02f);
                        Check(b.Route.Length == 2 && b.TargetNode.NodeId == "WEB" && b.AllocationWeight == 1, "next WEB WRITE loses API benefit and changes segment count");
                    }
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); a = Add(h, "travel-timeout", new[] { new OperationStage(OperationKind.Read, .5f, 4, "Short cached read"), new OperationStage(OperationKind.Write, 1, 4, "Short write") }); h.StepSimulation(8);
                    Check(a.Resolution == ConnectionResolution.TimedOut && a.CurrentOperationIndex == 1 && a.TerminationPhase == Lifecycle.Rerouting && a.HasEstablished && h.LegitimateFailureCount == 1 && h.CreditedWork == 0, "original service deadline continues through subsequent route traversal with no partial credit");
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); Set(h, "naturalLoadNoiseEnabled", true);
                    var noisy = new ConnectionSpawnPlan(0, "noise", ApiConfigurationWitness.Route(h), "product-lookup", "Read with noise", new[] { new OperationStage(OperationKind.Read, 1000, 5, "Noisy read") }, naturalLoadNoise: true, naturalLoadSeed: 99);
                    a = h.RegisterConnection(LogicalConnectionDefinition.FromPlan(noisy)); h.StepSimulation(4);
                    Near(a.DesiredLoad - a.BaseLoad, a.NaturalLoadDelta, "cached natural noise recorded in effective LOAD units exactly once");
                    Check(Math.Abs(a.NaturalLoadDelta) > .00001 && a.Definition.LoadAt(0) == 5 && a.BaseLoad == 2.5f, "cached noise remains seeded and productive without mutating source requirements");
                    h = Open(scene); h.TrySetCachePurpose("product-lookup", out _); a = Add(h, "reserve-a", new[] { new OperationStage(OperationKind.Read, 1000, 8, "Cache") }); var reservedPeer = Add(h, "reserve-b", new[] { new OperationStage(OperationKind.Read, 1000, 8, "Cache") });
                    Set(h.FindNodeResources("CACHE"), "baselineLoad", 1f); h.TryInjectInfection("CACHE", InfectionKind.PhantomLoad, 2, out _); h.StepSimulation(4); Check(h.Payloads.TryIntercept(null, "CACHE", out _), "CACHE accepts ordinary defense"); h.StepSimulation(.02f);
                    Near(a.AllocatedLoad, 1.5, "CACHE reserves baseline/infection/defense before productive A"); Near(reservedPeer.AllocatedLoad, 1.5, "CACHE reserves before productive B"); Near(h.FindNodeResources("CACHE").AllocatedLoad, 10, "CACHE capacity remains authoritative");
                    h = IntegrityWitness.Open(scene: scene); var cache = h.FindNodeResources("CACHE");
                    h.ApplyCondition(cache, new ConditionPayload(ConditionKind.ScanJammed, 10));
                    Check(h.ConditionFor(cache.View, ConditionKind.ScanJammed) != null && h.TryInjectInfection("CACHE", InfectionKind.PhantomLoad, 2, out _) && h.TryInjectInfection("CACHE", InfectionKind.Backdoor, 0, out _) && h.Integrity.For(cache).Value == 0 && !h.Integrity.For(cache).External && !h.RunEnded, "CACHE accepts ordinary conditions/infections and loses both Integrity points without special immunity");
                    foreach (bool enabled in new[] { false, true })
                    {
                        h = Open(scene); LoadBenefit(h); if (enabled) h.TrySetCachePurpose("product-lookup", out _); h.StepSimulation(40);
                        a = h.FindConnection(FixtureProduct); var b = h.FindConnection(FixtureSales);
                        Check(enabled ? h.Ending == RunEnding.ShiftComplete && h.CreditedWork == 64 && h.WorkCreditCount == 2 && h.LegitimateFailureCount == 0 : h.Ending == RunEnding.Fired && h.CreditedWork == 0 && h.LegitimateFailureCount == 2, scene + " generated controlled CACHE " + enabled + " saves identical64 business value or loses both");
                        outcomes.Add(FormattableString.Invariant($"{Path.GetFileNameWithoutExtension(scene)},{enabled},{a.Resolution},{b.Resolution},{h.CreditedWork},{h.LegitimateFailureCount},{h.Ending}"));
                        Check(!h.TrySetCachePurpose("sales-export", out _), "ending gates configuration"); h.ResetSimulation(); Check(h.CachePurposeId == "" && h.ApiFavoredPurposeId == "" && h.CreditedWork == 0 && h.LegitimateFailureCount == 0, "restart clears both configurations and accounting");
                    }
                }
            }
            finally { File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks); File.WriteAllLines(Evidence + "/benefit-outcomes.csv", outcomes); }
            Debug.Log("OperationRoutingCacheWitness PASS: " + checks.Count + " assertions.");
        }
        public static string FixtureProduct { get; private set; }
        public static string FixtureSales { get; private set; }
        public static void LoadBenefit(SimulationHost h)
        {
            var t = h.GetComponent<ConnectionTrafficDirector>(); t.AutoGenerationEnabled = t.HostileGenerationEnabled = false; t.RandomizeSeed = false; t.Seed = 1701;
            Set(t, "ambientOrigins", Array.Empty<AmbientOriginWeight>()); Set(t, "purposeTargetOverrides", Array.Empty<PurposeTargetOverride>()); Set(t, "normalWorkSpread", 0f); Set(t, "benignOutlierChance", 0f); Set(t, "legitimateWorkloadMultiplier", 2.5f); Set(h, "naturalLoadNoiseEnabled", false);
            Set(h, "shiftEnabled", true); Set(h, "shiftWorkTarget", 64f); Set(h, "businessFailureLimit", 2);
            foreach (var n in h.Nodes) { Set(n, "loadCapacity", 10f); Set(n, "baselineLoad", 0f); }
            foreach (string id in new[] { "product-lookup", "sales-export" })
            {
                var profile = t.Profiles.Single(p => p.id == id); profile.eligibleTargetIds = new[] { "DB" };
                foreach (var op in profile.operations) { op.minimumLoad = op.maximumLoad = 3.2f; op.meanWork = 32; }
            }
            h.ResetSimulation(); h.Recorder.BeginRun();
            ConnectionSpawnPlan Queue(string id)
            {
                var method = typeof(ConnectionTrafficDirector).GetMethod("BuildPlan", BindingFlags.NonPublic | BindingFlags.Instance);
                object[] args = method.GetParameters().Select(p => p.DefaultValue).ToArray(); args[0] = t.Profiles.Single(p => p.id == id); args[1] = 8f;
                var plan = (ConnectionSpawnPlan)method.Invoke(t, args); t.QueueSpawnPlan(plan, "cache_benefit_fixture"); return plan;
            }
            FixtureProduct = Queue("product-lookup").ConnectionId; FixtureSales = Queue("sales-export").ConnectionId;
        }
        public static LogicalConnectionState LoadMixed(SimulationHost h, float readWork = 20, string ordinaryDestination = null)
        {
            var t = h.GetComponent<ConnectionTrafficDirector>(); t.AutoGenerationEnabled = t.HostileGenerationEnabled = false;
            t.RandomizeSeed = false; t.Seed = 1701; Set(h, "naturalLoadNoiseEnabled", false);
            Set(h, "shiftEnabled", true); Set(h, "shiftWorkTarget", 10000f); Set(h, "businessFailureLimit", 100);
            h.ResetSimulation(); h.Recorder.BeginRun(); h.TrySetCachePurpose("product-lookup", out _);
            return Add(h, "r0", new[] { new OperationStage(OperationKind.Read, readWork, 4, "Read catalog"),
                new OperationStage(OperationKind.Write, 100, 4, "Write ordinary destination") }, ordinaryDestination ?? (h.FindNodeResources("WEB") == null ? "DB" : "WEB"));
        }
        [MenuItem("Tools/Connections/Load CACHE Mixed Route (Play Mode only)")]
        public static void LoadPlayMixed()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter canonical Play Mode first.");
            var state = LoadMixed(UnityEngine.Object.FindFirstObjectByType<SimulationHost>(), 80);
            Debug.Log("CACHE route fixture: inspect -v r0. READ at CACHE, then WRITE at " + state.Definition.TargetNode.NodeId +
                ". Same identity, original budget/value. ssh cache can clear/change the slot without rerouting the current READ. Stop/reload to discard fixture tuning.");
        }
        [MenuItem("Tools/Connections/Load CACHE Benefit (Play Mode only)")]
        public static void LoadPlayBenefit()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter canonical Play Mode first.");
            LoadBenefit(UnityEngine.Object.FindFirstObjectByType<SimulationHost>());
            Debug.Log("CACHE fixture: eight seconds to ssh cache / select product-lookup / Enter / Escape. Empty policy loses both jobs; configured CACHE saves both for64 business WORK. Product=" + FixtureProduct + " Sales=" + FixtureSales + ". Stop/reload to discard temporary settings.");
        }
        public static void Regressions()
        {
            var results = new List<string>(); Directory.CreateDirectory(Evidence);
            foreach (var test in new (string name, Action run)[] { ("CACHE", Run), ("API", ApiConfigurationWitness.Run), ("Shift", BusinessShiftWitness.Run), ("Fragility", FragilityWitness.Run),
                ("Demand", IndependentBusinessDemandWitness.Run), ("Workload", LegitimateWorkloadWitness.Run), ("Integrity", IntegrityWitness.Run), ("Behavior", BehaviorWitness.Run),
                ("Traffic", TrafficWitness.Run), ("Recorder", RecorderWitness.Run), ("Canonical", CanonicalSceneWitness.Run), ("DuelConcession", DuelConcessionWitness.Run),
                ("Resource", ResourceWitness.Run), ("Overclock", OverclockWitness.Run), ("GeneratedIdentity", GeneratedIdentityWitness.Run) })
            { try { test.run(); results.Add("PASS " + test.name); } catch (Exception e) { results.Add("FAIL " + test.name + ": " + e.Message); Debug.LogException(e); } }
            File.WriteAllLines(Evidence + "/regression-checks.txt", results);
            if (results.Any(x => x.StartsWith("FAIL"))) throw new Exception("CACHE regression failures; compare known inherited assertions in report.");
        }
    }
}
