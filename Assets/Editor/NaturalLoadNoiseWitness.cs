using System;
using System.Linq;
using System.Reflection;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class NaturalLoadNoiseWitness
    {
        [MenuItem("Tools/Connections/Run Natural LOAD Noise Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var recorder = UnityEngine.Object.FindFirstObjectByType<ConnectionsRunRecorder>();
            Check(host != null && director != null && recorder != null, "scene bindings");
            bool oldAuto = director.AutoGenerationEnabled;
            bool oldRecording = recorder.RecordingEnabled;
            int oldSeed = director.Seed;
            bool oldRandomize = director.RandomizeSeed;
            FieldInfo enabled = typeof(SimulationHost).GetField("naturalLoadNoiseEnabled",
                BindingFlags.Instance | BindingFlags.NonPublic);
            bool oldEnabled = (bool)enabled.GetValue(host);
            try
            {
                director.AutoGenerationEnabled = false;
                director.RandomizeSeed = false;
                director.Seed = 1701;
                recorder.RecordingEnabled = true;
                float[] first = Exercise(host, director, recorder, enabled, true);
                float[] replay = Exercise(host, director, recorder, enabled, true);
                Check(first.SequenceEqual(replay), "same seed, IDs, and steps replay exactly");
                float[] quiet = Exercise(host, director, recorder, enabled, false);
                Check(quiet.All(value => value == 0f), "global off switch preserves exact LOAD");

                host.ResetSimulation();
                Check(director.GenerateLoaderPlanForWitness(0f, LoaderStructure.Single,
                    LoaderPattern.Immediate).NaturalLoadNoise,
                    "canonical LOADER plan opts in");
                host.ResetSimulation();
                Check(director.GenerateInfectorPlanForWitness(0f, InfectorStructure.Standard,
                    InfectorSignature.None).NaturalLoadNoise,
                    "canonical INFECTOR plan opts in");
                CheckLifecycle(host, director, recorder, enabled);
                Debug.Log("NaturalLoadNoiseWitness PASS: deterministic independent smooth LOAD, " +
                    "additive modifiers, allocation/WORK/node/inspect/recorder agreement, " +
                    "global disable, canonical hostile opt-in, transition, completion, kill, " +
                    "cancellation, and one-decimal low-LOAD readability.");
            }
            finally
            {
                enabled.SetValue(host, oldEnabled);
                director.AutoGenerationEnabled = oldAuto;
                director.Seed = oldSeed;
                director.RandomizeSeed = oldRandomize;
                recorder.EndRun();
                recorder.RecordingEnabled = oldRecording;
                host.ResetSimulation();
            }
        }

        private static void CheckLifecycle(SimulationHost host,
            ConnectionTrafficDirector director, ConnectionsRunRecorder recorder,
            FieldInfo enabled)
        {
            enabled.SetValue(host, true);
            host.SetRunMode(ConnectionRunMode.Generated);
            recorder.BeginRun();
            NodeView[] route = { host.Nodes.Single(n => n.View.NodeId == "EXT1").View,
                host.Nodes.Single(n => n.View.NodeId == "API").View,
                host.Nodes.Single(n => n.View.NodeId == "DB").View };
            director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "noise-transition", route,
                "noise-witness", "Noise witness", new[] {
                    new OperationStage(OperationKind.Read, 1f, 8f, "First"),
                    new OperationStage(OperationKind.Write, 1000f, 8f, "Second") },
                naturalLoadNoise: true, naturalLoadSeed: 1701));
            host.StepSimulation(1.2f);
            host.StepSimulation(0.2f);
            var transitioning = host.FindConnection("noise-transition");
            Check(transitioning.IsTransitioning && transitioning.CurrentOperationIndex == 1 &&
                transitioning.NaturalLoadDelta == 0f && transitioning.DesiredLoad == 8f,
                "completed operation clears contribution before next allocation");
            for (int i = 0; i < 18; i++) host.StepSimulation(0.2f);
            Check(transitioning.IsActive && !transitioning.IsTransitioning &&
                Mathf.Abs(transitioning.NaturalLoadDelta) > 0.01f,
                "next operation establishes its own pattern");
            Check(host.KillConnection("noise-transition") &&
                transitioning.Resolution == ConnectionResolution.Killed &&
                transitioning.NaturalLoadDelta == 0f &&
                host.Nodes.Single(n => n.View.NodeId == "DB").ConnectionRequestedLoad == 0f,
                "kill releases dynamic contribution and resource demand");

            host.ResetSimulation();
            recorder.BeginRun();
            director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "noise-cancel", route,
                "noise-witness", "Noise witness", new[] {
                    new OperationStage(OperationKind.Read, 1000f, 8f, "Cancel") },
                naturalLoadNoise: true, naturalLoadSeed: 1701));
            host.StepSimulation(1.2f);
            for (int i = 0; i < 18; i++) host.StepSimulation(0.2f);
            var canceled = host.FindConnection("noise-cancel");
            Check(Mathf.Abs(canceled.NaturalLoadDelta) > 0.01f,
                "cancellation setup has active noise");
            Check(host.TryInjectInfection("DB", InfectionKind.Blackout, 0f, out _) &&
                canceled.Resolution == ConnectionResolution.Canceled &&
                canceled.NaturalLoadDelta == 0f &&
                host.Nodes.Single(n => n.View.NodeId == "DB").ConnectionRequestedLoad == 0f,
                "Blackout cancellation releases dynamic contribution and demand");

            host.ResetSimulation();
            recorder.BeginRun();
            director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "noise-low", route,
                "noise-witness", "Noise witness", new[] {
                    new OperationStage(OperationKind.Read, 1000f, 0.8f, "Low LOAD") },
                naturalLoadNoise: true, naturalLoadSeed: 1701));
            host.StepSimulation(1.2f);
            var low = host.FindConnection("noise-low");
            var displayedTenths = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < 200; i++)
            {
                host.StepSimulation(0.2f);
                displayedTenths.Add(Mathf.RoundToInt(low.DesiredLoad * 10f));
            }
            Check(displayedTenths.Count >= 2,
                "ordinary low LOAD changes at existing one-decimal precision");
            Check(host.KillConnection("noise-low"), "low-LOAD witness cleanup");

            host.ResetSimulation();
            recorder.BeginRun();
            director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "noise-complete", route,
                "noise-witness", "Noise witness", new[] {
                    new OperationStage(OperationKind.Read, 1f, 8f, "Complete") },
                naturalLoadNoise: true, naturalLoadSeed: 1701));
            host.StepSimulation(1.4f);
            var complete = host.FindConnection("noise-complete");
            Check(complete.Resolution == ConnectionResolution.Completed &&
                complete.NaturalLoadDelta == 0f &&
                host.Nodes.Single(n => n.View.NodeId == "DB").ConnectionRequestedLoad == 0f,
                "normal completion releases dynamic contribution and demand");
        }

        private static float[] Exercise(SimulationHost host, ConnectionTrafficDirector director,
            ConnectionsRunRecorder recorder, FieldInfo enabled, bool noise)
        {
            enabled.SetValue(host, noise);
            host.SetRunMode(ConnectionRunMode.Generated);
            recorder.BeginRun();
            NodeView[] route = { host.Nodes.Single(n => n.View.NodeId == "EXT1").View,
                host.Nodes.Single(n => n.View.NodeId == "API").View,
                host.Nodes.Single(n => n.View.NodeId == "DB").View };
            var fixedLoad = TimedModifierBehaviorDefinition.Overload("fixed", 0f, 3f);
            var stage = new OperationStage(OperationKind.Read, 1000f, 8f, "Noise witness");
            director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "noise-a", route,
                "noise-witness", "Noise witness", new[] { stage },
                connectionBehaviors: new[] { fixedLoad },
                naturalLoadNoise: true, naturalLoadSeed: 1701));
            director.QueueSpawnPlan(new ConnectionSpawnPlan(0f, "noise-b", route,
                "noise-witness", "Noise witness", new[] { stage },
                naturalLoadNoise: true, naturalLoadSeed: 1701));
            host.StepSimulation(1.2f);
            var a = host.FindConnection("noise-a");
            var b = host.FindConnection("noise-b");
            Check(a.IsActive && b.IsActive, "both connections establish");
            var samples = new float[50];
            float largestA = 0f, largestB = 0f;
            bool independent = false;
            for (int i = 0; i < samples.Length; i++)
            {
                float beforeA = a.CompletedWork;
                host.StepSimulation(0.2f);
                samples[i] = a.NaturalLoadDelta;
                largestA = Mathf.Max(largestA, Mathf.Abs(a.NaturalLoadDelta));
                largestB = Mathf.Max(largestB, Mathf.Abs(b.NaturalLoadDelta));
                independent |= Mathf.Abs(a.NaturalLoadDelta - b.NaturalLoadDelta) > 0.01f;
                Near(a.DesiredLoad, 11f + a.NaturalLoadDelta, "base + fixed + noise");
                Near(b.DesiredLoad, 8f + b.NaturalLoadDelta, "independent base + noise");
                Near(host.Nodes.Single(n => n.View.NodeId == "DB").ConnectionRequestedLoad,
                    a.DesiredLoad + b.DesiredLoad, "node requested LOAD");
                Near(a.CompletedWork - beforeA, a.AllocatedLoad * 0.2f,
                    "WORK uses authoritative allocated LOAD");
                Check(Mathf.Abs(a.NaturalLoadDelta) <= 0.801f &&
                    Mathf.Abs(b.NaturalLoadDelta) <= 0.801f,
                    "variation stays within ten percent of base LOAD");
                if (i > 0) Check(Mathf.Abs(samples[i] - samples[i - 1]) < 0.25f,
                    "variation changes smoothly between steps");
            }
            if (noise) Check(largestA > 0.05f && largestB > 0.05f && independent,
                "two IDs have independent visible variation");
            else Check(largestA == 0f && largestB == 0f && a.DesiredLoad == 11f &&
                b.DesiredLoad == 8f, "disabled noise preserves exact modifier math");
            recorder.CaptureSnapshot(host.SimulationTimeSeconds);
            var snapshot = recorder.CurrentLog.snapshots.Last().connections.Single(c => c.id == "noise-a");
            Near(snapshot.desiredLoad, a.DesiredLoad, "recorder authoritative requested LOAD");
            Near(snapshot.naturalLoadDelta, a.NaturalLoadDelta, "recorder noise contribution");
            Check(recorder.CurrentLog.run.naturalLoadNoiseEnabled == noise &&
                recorder.CurrentLog.events.Any(e => e.type == "plan_queued" &&
                    e.connectionId == "noise-a" && e.definition.naturalLoadNoise &&
                    e.definition.naturalLoadSeed == 1701), "configuration and seed recorded");
            Check(!recorder.CurrentLog.events.Any(e => e.type.StartsWith("modifier_") &&
                e.behaviorId == "natural"), "noise does not flood modifier events");
            var prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>(
                "Assets/Prefabs/Connections/InspectOutput.prefab");
            var output = UnityEngine.Object.Instantiate(prefab);
            try
            {
                output.Bind(new[] { a, b });
                var card = (InspectEntry)new SerializedObject(output).FindProperty("entries")
                    .GetArrayElementAtIndex(0).objectReferenceValue;
                var badge = (InspectOperationToken)new SerializedObject(card)
                    .FindProperty("loadBadge").objectReferenceValue;
                Check(badge.Text == $"LOAD {a.DesiredLoad:0.#}",
                    "inspect LOAD pill displays authoritative requested LOAD");
            }
            finally { UnityEngine.Object.DestroyImmediate(output.gameObject); }
            return samples;
        }

        private static void Near(float actual, float expected, string name) =>
            Check(Mathf.Abs(actual - expected) < 0.002f,
                name + $": expected {expected:0.###}, observed {actual:0.###}");

        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("NaturalLoadNoiseWitness failed: " + name);
        }
    }
}
