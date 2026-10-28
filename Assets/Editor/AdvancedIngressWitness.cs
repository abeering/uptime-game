using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    // Exercises the optional routing seam without changing the four-node serialized scene.
    // AUTH is a stand-in second ingress only in this Edit Mode witness.
    public static class AdvancedIngressWitness
    {
        [MenuItem("Tools/Connections/Run Multi-Ingress Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && director != null && recorder != null,
                "authored Connections scene binds host, director and recorder");

            FieldInfo originsField = Field("ambientOrigins");
            FieldInfo targetsField = Field("purposeTargetOverrides");
            var previousOrigins = (AmbientOriginWeight[])originsField.GetValue(director);
            var previousTargets = (PurposeTargetOverride[])targetsField.GetValue(director);
            bool oldAuto = director.AutoGenerationEnabled;
            bool oldRandomize = director.RandomizeSeed;
            bool oldRecording = recorder.RecordingEnabled;
            int oldSeed = director.Seed;
            try
            {
                director.AutoGenerationEnabled = false;
                director.RandomizeSeed = false;
                director.Seed = 2401;
                originsField.SetValue(director, null);
                targetsField.SetValue(director, null);
                host.SetRunMode(ConnectionRunMode.Generated);

                Check(director.EffectiveAmbientOrigins.Count == 1 &&
                    director.EffectiveAmbientOrigins[0].nodeId == "EXT1",
                    "no overrides keeps single default origin");
                ConnectionSpawnPlan baseline = director.GenerateBenignPlan(0f);
                Check(baseline.Origin.NodeId == "EXT1" &&
                    baseline.Target.NodeId == director.EffectiveTargetIds(
                        director.Profiles.First(p => p.id == baseline.ProfileId))[0],
                    "legacy route uses the authored origin and PURPOSE target");

                var weightedOrigins = new[]
                {
                    new AmbientOriginWeight { nodeId = "EXT1", weight = 6f },
                    new AmbientOriginWeight { nodeId = "AUTH", weight = 4f }
                };
                var destinations = new[]
                {
                    new PurposeTargetOverride
                    {
                        purposeId = "customer-login",
                        eligibleTargetIds = new[] { "DB" }
                    },
                    new PurposeTargetOverride
                    {
                        purposeId = "inventory-sync",
                        eligibleTargetIds = new[] { "DB", "API" }
                    }
                };
                originsField.SetValue(director, weightedOrigins);
                targetsField.SetValue(director, destinations);
                host.SetRunMode(ConnectionRunMode.Generated);
                string first = SamplePlans(host, director);
                director.ResetDirector();
                string replay = SamplePlans(host, director);
                Check(first == replay, "weighted-origin / destination plan replay is seeded");

                recorder.RecordingEnabled = true;
                recorder.BeginRun();
                TrafficConfiguration metadata = recorder.CurrentLog.run.traffic;
                Check(metadata.ambientOrigins.Length == 2 &&
                    metadata.ambientOrigins[0].nodeId == "EXT1" &&
                    metadata.ambientOrigins[0].weight == 6f &&
                    metadata.ambientOrigins[1].nodeId == "AUTH" &&
                    metadata.ambientOrigins[1].weight == 4f,
                    "recorder captures effective weighted origins");
                ProfileConfiguration inventory = metadata.profiles.First(p => p.id == "inventory-sync");
                Check(inventory.eligibleTargetIds.SequenceEqual(new[] { "DB", "API" }) &&
                    inventory.authoredEligibleTargetIds.SequenceEqual(new[] { "DB" }),
                    "recorder captures effective and canonical purpose destinations");
                recorder.EndRun();

                // A Spawner at DB must not select either configured ingress as a target.
                // Customer-login's canonical destination is AUTH; it is excluded here
                // even though this witness gives it a different ambient target.
                destinations[0].eligibleTargetIds = new[] { "AUTH" };
                targetsField.SetValue(director, destinations);
                director.ResetDirector();
                var parent = new NodeInfection(
                    host.Nodes.First(n => n.View.NodeId == "DB"),
                    new InfectionPayload(InfectionKind.Spawner, 0f, 20f),
                    0f, "a0", 0);
                for (int i = 0; i < 12; i++)
                {
                    ConnectionSpawnPlan child = director.QueueSpawnerChild(
                        parent, i * 20f, out string reason);
                    Check(child != null && reason == null &&
                        child.Origin.NodeId == "DB" && child.Target.NodeId == "API" &&
                        child.IsHostile && child.Route.Length >= 2,
                        "Spawner uses effective internal destinations, excluding ingress nodes");
                }

                ExpectInvalid(() =>
                {
                    originsField.SetValue(director, new[]
                    {
                        new AmbientOriginWeight { nodeId = "EXT1", weight = 1f },
                        new AmbientOriginWeight { nodeId = "ext1", weight = 1f }
                    });
                    director.ResetDirector();
                }, "duplicate origin");
                ExpectInvalid(() =>
                {
                    originsField.SetValue(director, new[]
                    {
                        new AmbientOriginWeight { nodeId = "EXT1", weight = 0f },
                        new AmbientOriginWeight { nodeId = "AUTH", weight = 0f }
                    });
                    director.ResetDirector();
                }, "all-zero origin weights");
                ExpectInvalid(() =>
                {
                    originsField.SetValue(director, weightedOrigins);
                    targetsField.SetValue(director, new[]
                    {
                        new PurposeTargetOverride
                        {
                            purposeId = "inventory-sync",
                            eligibleTargetIds = new[] { "NOT-A-NODE" }
                        }
                    });
                    director.ResetDirector();
                }, "unknown purpose target");
                ExpectInvalid(() =>
                {
                    targetsField.SetValue(director, new[]
                    {
                        new PurposeTargetOverride
                        {
                            purposeId = "inventory-sync",
                            eligibleTargetIds = new[] { "AUTH" }
                        }
                    });
                    director.ResetDirector();
                }, "external ingress as purpose target");

                Debug.Log("AdvancedIngressWitness PASS: legacy compatibility, weighted ingress, " +
                    "multi-target PURPOSEs, seeded replay, recorder metadata, Spawner eligibility, " +
                    "and invalid configuration rejection.");
            }
            finally
            {
                recorder.EndRun();
                originsField.SetValue(director, previousOrigins);
                targetsField.SetValue(director, previousTargets);
                director.AutoGenerationEnabled = oldAuto;
                director.RandomizeSeed = oldRandomize;
                director.Seed = oldSeed;
                recorder.RecordingEnabled = oldRecording;
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static string SamplePlans(SimulationHost host,
            ConnectionTrafficDirector director)
        {
            var signatures = new List<string>();
            bool firstOrigin = false, secondOrigin = false;
            bool inventoryDb = false, inventoryApi = false;
            bool benignFromSecond = false, hostileFromSecond = false;
            for (int i = 0; i < 160; i++)
            {
                ConnectionSpawnPlan plan = i % 2 == 0 ?
                    director.GenerateBenignPlan(i) : director.GenerateHostilePlan(i);
                Check(plan != null, "plan generated within the 260-ID pool");
                host.ValidatePlan(plan);
                Check(plan.Route[0] == plan.Origin &&
                    plan.Route[plan.Route.Length - 1] == plan.Target,
                    "valid route endpoints");
                Check(plan.Target.NodeId != "EXT1" && plan.Target.NodeId != "AUTH",
                    "ambient targets remain internal");
                firstOrigin |= plan.Origin.NodeId == "EXT1";
                secondOrigin |= plan.Origin.NodeId == "AUTH";
                if (plan.Origin.NodeId == "AUTH")
                {
                    benignFromSecond |= !plan.IsHostile;
                    hostileFromSecond |= plan.IsHostile;
                }
                if (plan.ProfileId == "inventory-sync")
                {
                    inventoryDb |= plan.Target.NodeId == "DB";
                    inventoryApi |= plan.Target.NodeId == "API";
                }
                signatures.Add(plan.ConnectionId + ":" + plan.ProfileId + ":" +
                    string.Join(">", plan.Route.Select(n => n.NodeId)) + ":" +
                    JsonUtility.ToJson(plan.GenerationTrace));
            }
            Check(firstOrigin && secondOrigin && benignFromSecond && hostileFromSecond,
                "both ambient processes use both weighted origins");
            Check(inventoryDb && inventoryApi,
                "one canonical purpose reaches multiple overridden destinations");
            return string.Join("|", signatures);
        }

        private static FieldInfo Field(string name)
        {
            FieldInfo field = typeof(ConnectionTrafficDirector).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("Missing field: " + name);
            return field;
        }

        private static void ExpectInvalid(Action action, string caseName)
        {
            bool rejected = false;
            try { action(); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "invalid configuration rejected: " + caseName);
        }

        private static void Check(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException("AdvancedIngressWitness failed: " + label);
        }
    }
}
