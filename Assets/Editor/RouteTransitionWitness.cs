using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class RouteTransitionWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsOperationRoutingCacheV0/TransitionRefinement";
        private static readonly List<string> checks = new(), timings = new();
        private static void Check(bool ok, string label) { checks.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); }
        private static void Near(double a, double b, string label) => Check(Math.Abs(a - b) < .002, label + $": expected {b}, actual {a}");
        public static LogicalConnectionState Arc(SimulationHost h, string first, string next, string origin = "EXT1", float firstWork = .4f, float nextWork = 100)
        {
            var route = (NodeView[])typeof(ConnectionTrafficDirector).GetMethod("FindRoute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(h.GetComponent<ConnectionTrafficDirector>(), new object[] { h.FindNodeResources(origin).View, h.FindNodeResources(first).View });
            return h.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0, "r0", route, "product-lookup", "Route continuity", new[] {
                new OperationStage(OperationKind.Read, firstWork, 4, "First operation"),
                new OperationStage(OperationKind.Write, nextWork, 4, "Next operation", intendedDestination: h.FindNodeResources(next).View) })));
        }
        private static SimulationHost Open(string scene) => OperationRoutingCacheWitness.Open(scene);
        public static void Until(SimulationHost h, Func<bool> done) => OperationRoutingCacheWitness.Until(h, done);
        [MenuItem("Tools/Connections/Run Route Transition Witness")]
        public static void Run()
        {
            checks.Clear(); timings.Clear(); Directory.CreateDirectory(Evidence); timings.Add("scene,case,shared_nodes,new_links,started,finished,charged");
            try
            {
                foreach (string scene in CanonicalSceneAuthoring.Scenes)
                {
                    foreach (var sample in new[] { ("identical", "DB", "DB", "EXT1"), ("shared-prefix", "CACHE", "DB", "EXT1"),
                        ("shorter", "CACHE", "API", "EXT1"), ("longer", "API", "CACHE", "EXT1"), ("origin", "DB", "CACHE", "API") })
                    {
                        var h = Open(scene); var a = Arc(h, sample.Item2, sample.Item3, sample.Item4); Until(h, () => a.IsActive);
                        float established = a.EstablishedAtSeconds, deadline = a.ServiceDeadlineSeconds; double value = a.BusinessWorkValue;
                        Until(h, () => a.CurrentOperationIndex == 1 && a.Routing != null);
                        var r = a.RouteTransition;
                        if (sample.Item1 == "identical")
                        {
                            Check(r == null && a.IsActive && !a.IsRerouting && h.Recorder.CurrentLog.events.All(e => e.type != "connection_rerouting_started"), scene + " identical route directly starts next operation"); continue;
                        }
                        int shared = sample.Item4 == "API" ? 1 : scene == CanonicalSceneAuthoring.Scenes[0] ? 2 : 3;
                        Check(r != null && r.SharedNodeCount == shared, scene + "/" + sample.Item1 + " longest common prefix");
                        int newLinks = sample.Item1 == "shorter" ? 0 : 1;
                        Check(r.NewLinkCount == newLinks && a.HasEstablished && !a.IsConnecting && a.EstablishedAtSeconds == established && a.ServiceDeadlineSeconds == deadline && a.BusinessWorkValue == value && a.KnownIdentity == "r0", "persistent established identity/value/deadline across rerouting");
                        Near(r.TravelSeconds, newLinks * a.TravelSecondsPerLink, "only suffix links charged");
                        if (newLinks > 0)
                        {
                            Check(a.IsRerouting && !a.IsDemanding && a.AllocatedLoad == 0 && a.CompletedWork == 0, "real REROUTING state releases processing and performs no WORK");
                            Check(a.ReachedRouteNodeIndex == shared - 1, "already established prefix remains reached");
                            h.Recorder.CaptureSnapshot(h.SimulationTimeSeconds); var snapshot = h.Recorder.CurrentLog.snapshots.Last();
                            Check(snapshot.activeConnections == 1 && snapshot.establishedBenign == 1 && snapshot.anticipatedBenign == 0 && snapshot.connections.Single().lifecycle == "Rerouting" && snapshot.connections.Single().rerouting.sharedNodeCount == shared, "recorder retains established classification and explicit rerouting evidence");
                            h.StepSimulation(.1f); Check(a.IsRerouting && a.CompletedWork == 0 && a.AllocatedLoad == 0, "elapsed traversal never executes processing WORK");
                            Until(h, () => a.IsActive);
                        }
                        else Check(a.IsActive && r.StartedAtSeconds == r.FinishedAtSeconds, "strictly shorter route charges zero time; cosmetic retraction never gates WORK");
                        Near(r.FinishedAtSeconds.Value - r.StartedAtSeconds, r.TravelSeconds, "recorded reroute duration equals new physical traversal");
                        Check(h.Recorder.CurrentLog.events.Count(e => e.type == "connection_established") == 1 && h.Recorder.CurrentLog.events.Count(e => e.type == "operation_started") == 2 && h.Recorder.CurrentLog.events.Count(e => e.type == "connection_rerouting_started") == 1 && h.Recorder.CurrentLog.events.Count(e => e.type == "connection_rerouting_finished") == 1, "exact-once connection/operation/rerouting boundaries");
                        timings.Add(FormattableString.Invariant($"{Path.GetFileNameWithoutExtension(scene)},{sample.Item1},{shared},{newLinks},{r.StartedAtSeconds},{r.FinishedAtSeconds},{r.TravelSeconds}"));
                        Until(h, () => a.IsComplete); Check(h.CreditedWork == value && h.WorkCreditCount == 1 && h.LegitimateFailureCount == 0, "whole arc earns original business credit once");
                    }
                    {
                        var h = Open(scene); var a = Arc(h, "CACHE", "DB", firstWork: 12);
                        var peer = h.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(0, "p0", ApiConfigurationWitness.Route(h, "CACHE"), "sales-export", "Peer", new[] { new OperationStage(OperationKind.Read, 1000, 8, "Peer") })));
                        Until(h, () => a.IsActive && peer.IsActive); Near(a.AllocatedLoad, 10f / 3, "oversubscribed old target allocates proportionally");
                        Until(h, () => a.IsRerouting); Near(peer.AllocatedLoad, 8, "old-target peer immediately receives released processing capacity");
                        Near(h.FindNodeResources("DB").ConnectionAllocatedLoad, 0, "new target reserves no processing during reroute");
                        Until(h, () => a.IsActive); h.StepSimulation(.02f); Near(a.AllocatedLoad, 4, "new target resumes operation allocation after suffix establishment");
                    }
                    {
                        var h = Open(scene); var route = ApiConfigurationWitness.Route(h, "CACHE");
                        var plan = new ConnectionSpawnPlan(0, "r0", route, "product-lookup", "Identity/timer witness", new[] {
                            new OperationStage(OperationKind.Read, .4f, 4, "First"),
                            new OperationStage(OperationKind.Write, 100, 4, "Next", behaviors: new[] { TimedModifierBehaviorDefinition.Overload("next-op", 0, 2) }, intendedDestination: h.FindNodeResources("DB").View) },
                            isHostile: true, connectionBehaviors: new[] { TimedModifierBehaviorDefinition.Overload("connection", .15f, 1) }, identityHopSeconds: .1f, identitySeed: 99);
                        var a = h.RegisterConnection(LogicalConnectionDefinition.FromPlan(plan)); Until(h, () => a.IsRerouting); string alias = a.Identity.AliasIdentity;
                        h.StepSimulation(.22f);
                        Check(a.IsRerouting && a.Behaviors.ActiveModifiers.Count == 1 && a.Behaviors.ActiveModifiers.Single().SourceId == "connection" && a.CompletedWork == 0, "connection timers advance while upcoming operation timers wait");
                        Check(a.ConnectionId == "r0" && a.Identity.AliasIdentity != alias && h.IsLiveAlias(a.Identity.AliasIdentity), "identity hopping and live alias reservation persist during rerouting");
                        Until(h, () => a.IsActive); h.StepSimulation(.02f); Check(a.Behaviors.ActiveModifiers.Count == 2 && a.DesiredLoad == 7, "operation behavior starts only after new route establishment");
                    }
                    foreach (bool reached in new[] { false, true })
                    {
                        var h = Open(scene); var a = Arc(h, "CACHE", "DB"); Until(h, () => a.IsRerouting);
                        string node = reached ? "API" : "DB"; h.TryInjectInfection(node, InfectionKind.Blackout, 0, out _);
                        Check(reached ? a.Resolution == ConnectionResolution.Canceled : a.IsRerouting, reached ? "BLACKOUT on retained prefix interrupts immediately" : "offline future node keeps locked route until arrival");
                        h.StepSimulation(1); Check(a.Resolution == ConnectionResolution.Canceled && a.TerminationPhase == Lifecycle.Rerouting && a.TerminationCause == ConnectionTerminationCause.Blackout && h.LegitimateFailureCount == 1 && h.CreditedWork == 0, "BLACKOUT during rerouting cancels exactly once without failover/partial credit");
                    }
                    {
                        var h = Open(scene); var a = Arc(h, "CACHE", "DB"); Until(h, () => a.IsRerouting); h.TryInjectInfection("CACHE", InfectionKind.Blackout, 0, out _);
                        Check(a.IsRerouting && !a.Resolution.HasValue, "obsolete tail is presentational; its BLACKOUT cannot cancel the newly committed path");
                        h.StepSimulation(1); Check(a.IsActive && a.TargetNode.NodeId == "DB", "reroute resumes at new target after obsolete-node outage");
                    }
                    {
                        var h = Open(scene); var a = Arc(h, "CACHE", "DB", firstWork: .1f, nextWork: .4f); h.StepSimulation(6);
                        Check(a.Resolution == ConnectionResolution.TimedOut && a.TerminationPhase == Lifecycle.Rerouting && h.LegitimateFailureCount == 1 && h.CreditedWork == 0 && h.WorkCreditCount == 0, "original service timeout continues during REROUTING");
                    }
                    {
                        var h = Open(scene); var a = Arc(h, "CACHE", "DB"); Until(h, () => a.IsRerouting); Check(h.KillConnection("r0"), "rerouting connection remains addressable/killable");
                        Check(!h.KillConnection("r0") && h.LegitimateFailureCount == 1 && h.Recorder.CurrentLog.events.Count(e => e.type == "business_failure") == 1, "repeated termination cannot duplicate accounting");
                    }
                }
            }
            finally { File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks); File.WriteAllLines(Evidence + "/timings.csv", timings); }
            Debug.Log("RouteTransitionWitness PASS: " + checks.Count + " assertions.");
        }
        public static void Regressions()
        {
            Run(); OperationRoutingCacheWitness.Regressions();
        }
    }
}
