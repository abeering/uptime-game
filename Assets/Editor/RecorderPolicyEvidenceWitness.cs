using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class RecorderPolicyEvidenceWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsOperationRoutingCacheV0/RecorderCompleteness";
        private static readonly List<string> checks = new();
        private static void Check(bool ok, string label) { checks.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); }
        private static void Near(float a, float b, string label) => Check(Math.Abs(a - b) < .0002f, label + $": expected {b}, observed {a}");
        private static float Raw(ConnectionBehaviorRuntime runtime, string method, int index) => (float)typeof(ConnectionBehaviorRuntime)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(runtime, new object[] { index });
        private static LogicalConnectionState Add(SimulationHost h, string id, string purpose, OperationStage[] stages, bool noise = false) =>
            h.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0, id, ApiConfigurationWitness.Route(h), purpose, purpose, stages,
                naturalLoadNoise: noise, naturalLoadSeed: 99)));
        private static ConnectionsRunLog Read(SimulationHost h)
        {
            Check(h.Recorder.Save(false), "actual run JSON saved");
            return JsonUtility.FromJson<ConnectionsRunLog>(File.ReadAllText(h.Recorder.OutputPath));
        }
        [MenuItem("Tools/Connections/Run Recorder Policy Evidence Witness")]
        public static void Run()
        {
            checks.Clear(); Directory.CreateDirectory(Evidence);
            try
            {
                foreach (string scene in CanonicalSceneAuthoring.Scenes)
                {
                    foreach (float factor in new[] { .5f, .3f })
                    {
                        var h = OperationRoutingCacheWitness.Open(scene);
                        var cache = h.FindNodeResources("CACHE"); OperationRoutingCacheWitness.Set(cache, "loadCapacity", 5f); OperationRoutingCacheWitness.Set(cache, "baselineLoad", 1f);
                        OperationRoutingCacheWitness.Set(h, "cachedLoadMultiplier", factor); OperationRoutingCacheWitness.Set(h, "naturalLoadNoiseEnabled", true);
                        h.ResetSimulation(); h.Recorder.BeginRun();
                        Check(h.TryActivateOverclock("CACHE", "recording witness", out _), "existing overclock supplies effective capacity");
                        h.TrySetCachePurpose("product-lookup", out _); h.TrySetApiPurpose("product-lookup", out _);
                        var a = Add(h, "a0", "product-lookup", new[] { new OperationStage(OperationKind.Read, 1000, 4, "Modifiers and noise", behaviors: new[] {
                            new TimedModifierBehaviorDefinition("requirements", 0, 0, 50, 2), TimedModifierBehaviorDefinition.Overload("overload", 0, 3),
                            TimedModifierBehaviorDefinition.Waving("wave", 0, -2, 2, 4), TimedModifierBehaviorDefinition.Rate("paused", 0, 0, 0) }) }, true);
                        h.TrySetCachePurpose("sales-export", out _);
                        Add(h, "b0", "sales-export", new[] { new OperationStage(OperationKind.Read, 1000, 8, "Other cached class") });
                        h.StepSimulation(4); h.TryInjectInfection("CACHE", InfectionKind.PhantomLoad, 2, out _);
                        Check(h.Payloads.TryIntercept(null, "CACHE", out _), "existing defense reserves LOAD"); h.StepSimulation(.02f);
                        h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds); var saved = Read(h); var snapshot = saved.snapshots.Last();
                        var node = snapshot.nodes.Single(n => n.id == "CACHE"); var op = snapshot.connections.Single(c => c.id == "a0");
                        Near(node.loadCapacity, 7.5f, "sampled effective capacity includes authored 1.5x Overclock rather than base 5"); Near(node.baselineLoad, 1, "baseline reservation");
                        Near(node.infectionAllocatedLoad, 2, "infection reservation"); Near(node.defensiveAllocatedLoad, 4, "defense reservation");
                        Check(op.isDemanding && op.lifecycle == "Active" && op.allocatedLoad > 0 && op.actualWorkRate == 0 && op.effectiveWorkRateMultiplier == 0, "zero productive rate is still actual allocation demand");
                        Check(op.target == "CACHE" && op.operationRouting.actualDestination == "CACHE" && op.operationRouting.intendedDestination == "DB" && op.operationRouting.selectedCachePurpose == "product-lookup" && snapshot.cachePurposeId == "sales-export", "ordinary/actual destinations and committed policy remain distinct from current selection");
                        Near(op.desiredLoad / op.operationRouting.loadMultiplier, Raw(a.Behaviors, "EffectiveLoad", 0), "saved values reconstruct uncached sampled demand including all active modifiers/noise");
                        Near(op.totalWork / op.operationRouting.workMultiplier, 1050, "saved values reconstruct unoptimized WORK including requirement modifier");
                        Near(op.baseLoad / op.operationRouting.loadMultiplier, 4, "original base LOAD distinguished from composed LOAD");
                        Check(Math.Abs(op.naturalLoadDelta) > .00001 && a.Behaviors.ActiveModifiers.Count == 4 && Math.Abs(op.desiredLoad / factor - op.operationRouting.originalLoad) > 1, "test exercises genuine fixed/keyword/wave/rate/noise composition, not base-only demand");
                        Check(op.allocationWeight == 2 && op.allocationPolicyNodeId == "API" && op.operationRouting.workMultiplier == .25f && op.operationRouting.loadMultiplier == factor, "applied API weight and locked CACHE factors serialized");
                        Check(saved.events.Any(e => e.type == "operation_started" && e.connectionId == "a0") && saved.events.Count(e => e.type == "cache_policy_changed") == 2 && saved.events.Any(e => e.type == "api_policy_changed"), "existing operation/configuration timestamps present");
                        Check(File.ReadAllText(h.Recorder.OutputPath).Contains("\"isDemanding\": true") && h.Recorder.SnapshotIntervalSeconds == 1, "new raw flag survives disk serialization without increasing sample frequency");
                        if (factor == .5f) File.WriteAllText(Evidence + "/" + h.gameObject.scene.name + "-sample.json", JsonUtility.ToJson(snapshot, true));
                    }
                    {
                        var h = OperationRoutingCacheWitness.Open(scene); var a = Add(h, "h0", "product-lookup", new[] {
                            new OperationStage(OperationKind.Read, .08f, 4, "First"), new OperationStage(OperationKind.Write, 100, 4, "Next") });
                        RouteTransitionWitness.Until(h, () => a.CurrentOperationIndex == 1);
                        Check(a.IsActive && a.IsTransitioning, "real held transition boundary reached");
                        h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds); var saved = Read(h); var held = saved.snapshots.Last().connections.Single();
                        Check(held.lifecycle == "Active" && !held.isDemanding && held.allocatedLoad == 0 && saved.events.Any(e => e.type == "operation_completed" && e.operationIndex == 0), "held Active operation is excluded from offline allocation demand; preceding end time recorded");
                        h.StepSimulation(.02f); h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds);
                        Check(Read(h).snapshots.Last().connections.Single().isDemanding, "next started operation resumes recorded demand");
                    }
                }
            }
            finally { File.WriteAllLines(Evidence + "/checks.txt", checks); }
            Debug.Log("RecorderPolicyEvidenceWitness PASS: " + checks.Count + " checks.");
        }
        public static void Regressions() { Run(); RouteTransitionWitness.Run(); RecorderWitness.Run(); }
    }
}
