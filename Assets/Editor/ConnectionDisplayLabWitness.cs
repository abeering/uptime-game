using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Uptime.Connections.Display;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class ConnectionDisplayLabWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsDisplayLabV0";
        private static readonly List<string> checks = new();
        private static void Check(bool value, string label)
        { checks.Add((value ? "PASS " : "FAIL ") + label); if (!value) throw new Exception("DisplayLab: " + label); }
        private static void Near(double value, double expected, string label) => Check(Math.Abs(value - expected) < .0001,
            label + ": expected " + expected + ", observed " + value);
        public static void BuildAndCheck() { ConnectionDisplayLabAuthoring.Build(); Run(); }
        [MenuItem("Tools/Connections/Run Connection Display Lab Witness")]
        public static void Run()
        {
            checks.Clear(); Directory.CreateDirectory(Evidence);
            try
            {
                EditorSceneManager.OpenScene(ConnectionDisplayLabAuthoring.ScenePath);
                var lab = UnityEngine.Object.FindFirstObjectByType<ConnectionDisplayLab>();
                Check(lab != null && lab.heartbeat != null && lab.style != null, "authored lab, heartbeat and style references");
                Check(lab.SampleCount == 26 && lab.progressComparisons.Length == 10, "26 path examples plus ten static pill comparisons");
                Check(UnityEngine.Object.FindFirstObjectByType<SimulationHost>() == null &&
                    UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>() == null &&
                    UnityEngine.Object.FindFirstObjectByType<HostileActivityDirector>() == null,
                    "no simulation, traffic director or hacker components");
                foreach (var sample in lab.samples)
                {
                    Check(sample.phases.Length > 0 && sample.view.path != null && sample.view.pill != null && sample.view.terminalMarker != null,
                        "authored bindings: " + sample.name);
                    Check(sample.view.path.GetComponent<CanvasRenderer>() != null && sample.view.pill.GetComponent<CanvasRenderer>() != null,
                        "authored CanvasRenderer components: " + sample.name);
                    Check(PrefabUtility.GetCorrespondingObjectFromSource(sample.view) != null, "linked reusable display prefab: " + sample.name);
                    Check(sample.view.subdivision == 1 && sample.view.phaseBeats == 0, "coherent default phase/subdivision: " + sample.name);
                    Check(sample.view.progressTreatment == PillProgressTreatment.Interior, "primary animated interior fill: " + sample.name);
                }
                Check(lab.style.ordinaryIdentityColors.Length == 6 && lab.style.importantIdentityColors.Length == 6,
                    "two shared six-slot identity families");
                // Literal historical values from pre-redesign palette, not a second runtime palette.
                Color[] oldPalette = { new(.36f,.87f,.91f), new(.35f,.91f,.76f), new(.43f,.82f,.92f),
                    new(.43f,.89f,.79f), new(.31f,.85f,.84f), new(.49f,.87f,.75f) };
                for (int slot = 0; slot < 6; slot++)
                {
                    Check(lab.style.IdentityColor(DisplayTrafficClass.Important, slot) == oldPalette[slot],
                        "IMPORTANT slot " + slot + " restores previous blue/teal accent");
                    Check(lab.style.ordinaryIdentityColors.Distinct().Count() == 6 &&
                        lab.style.IdentityColor(DisplayTrafficClass.Ordinary, slot).grayscale > .6f,
                        "ordinary slot " + slot + " is differentiated and readable");
                }
                Check(lab.samples.SelectMany(s => s.phases).Select(p => p.state.identitySlot).Distinct().Count() == 6,
                    "authored lab demonstrates all ordinary identity slots without new hierarchy");
                Check(lab.style.bodyFillOpacity >= .3f && lab.style.bodyFillOpacity < .5f &&
                    lab.style.Activity(DisplayTrafficClass.Ordinary).a < .7f,
                    "meaningful translucent body and quieter activity defaults");
                var clock = lab.heartbeat; Near(clock.bpm, 120, "default BPM");
                Near(clock.Travel(0, 1), 0, "beat begins at prior settled position");
                Near(clock.Travel(.29, 1), 9, "smooth halfway movement");
                Near(clock.Travel(.58, 1), 18, "moving fraction reaches next discrete position");
                Near(clock.Travel(.99, 1), 18, "clear hold before next beat");
                Near(clock.Travel(1, 1), 18, "beat boundary has no teleport");
                Near(clock.Travel(1.29, 1), 27, "next eased advance");
                Near(clock.Travel(2, .65f), 23.4, "moderate slowdown skips a beat with honest average travel");
                Near(clock.Travel(4, .22f), 15.84, "heavy slowdown skips three beats");
                Near(clock.Travel(100, 0), 0, "stall remains still");
                clock.Replay(); clock.Advance(1); Near(clock.Beats, 2, "shared unscaled clock");
                clock.bpm = 60; clock.Advance(1); Near(clock.Beats, 3, "tempo change preserves accumulated phase");
                clock.paused = true; clock.Advance(100); Near(clock.Beats, 3, "pause freezes visual time");
                clock.Replay(); Near(clock.Beats, 0, "replay resets phase"); Near(clock.ElapsedSeconds, 0, "replay resets transitions");
                clock.paused = false; clock.bpm = 120;
                Check(lab.style.Spacing(8) < lab.style.Spacing(1), "LOAD controls density independently of speed");
                Check(LogicalPathView.DirectionFor(OperationKind.Read) == -1 && LogicalPathView.DirectionFor(OperationKind.Write) == 1 &&
                    LogicalPathView.DirectionFor(OperationKind.Execute) == 0, "existing operation directions reused");
                clock.SetReviewTime(3.25); lab.RenderNow();
                var safe = lab.samples.Single(s => s.Current.identity == "c1");
                var danger = lab.samples.Single(s => s.Current.identity == "c3");
                Near(safe.view.path.ActivityTravel, danger.view.path.ActivityTravel, "full-speed danger does not slow activity");
                Check(lab.style.Rail(safe.Current, clock.Beats) != lab.style.Rail(danger.Current, clock.Beats), "same speed, independent warning rail");
                float originalBudget = safe.phases[0].serviceRemaining;
                safe.phases[0].serviceRemaining = 41.5f; safe.Render(clock.ElapsedSeconds, clock);
                Check(safe.phaseLabel.text.Contains("41.5"), "synthetic timing diagnostics reflect live Inspector input");
                safe.phases[0].serviceRemaining = originalBudget;
                var slowSafe = lab.samples.Single(s => s.Current.identity == "b3");
                Check(slowSafe.Current.dangerSeverity == 0 && slowSafe.Current.productiveSpeed < .4f, "heavy slowdown can remain safe");
                var denseHealthy = lab.samples.Single(s => s.Current.identity == "b1");
                Near(denseHealthy.view.path.ActivitySpacing, slowSafe.view.path.ActivitySpacing, "dense slowed traffic retains requested LOAD density");
                clock.SetReviewTime(2); lab.RenderNow();
                var changed = denseHealthy.Current; double continuous = denseHealthy.view.path.ActivityTravel;
                changed.productiveSpeed = .22f; changed.operation = OperationKind.Read;
                denseHealthy.view.Render(changed, clock);
                Near(denseHealthy.view.path.ActivityTravel, continuous, "speed/direction change preserves activity position");
                changed.productiveSpeed = 0; denseHealthy.view.Render(changed, clock);
                clock.Advance(2); denseHealthy.view.Render(changed, clock);
                Near(denseHealthy.view.path.ActivityTravel, continuous, "stall holds accumulated activity position");
                changed.productiveSpeed = 1; denseHealthy.view.Render(changed, clock);
                Near(denseHealthy.view.path.ActivityTravel, continuous, "recovery resumes without teleporting activity");
                var normalLate = lab.samples.Single(s => s.Current.identity == "c7");
                Check(normalLate.Current.exceedsNormalBudget && normalLate.Current.productiveSpeed == 1, "normal-rate budget failure independent of current speed");
                float[] work = { 0, .25f, .5f, .75f, .99f };
                foreach (PillProgressTreatment mode in Enum.GetValues(typeof(PillProgressTreatment)))
                {
                    var pills = lab.progressComparisons.Where(p => p.treatment == mode).OrderBy(p => p.progress).ToArray();
                    Check(pills.Length == 5 && pills.Select(p => p.progress).SequenceEqual(work), mode + " exact 0/25/50/75/99 WORK samples");
                    Check(pills.All(p => p.GetComponentInChildren<TMP_Text>().color.r > .9f), mode + " independent high-contrast identity text");
                }
                var bend = lab.samples.Single(s => s.Current.identity == "b6");
                Check(bend.view.path.SegmentCount == 3 && bend.view.path.PathLength > 380, "joined multi-segment variable-width route");
                var begin = bend.view.path.PointAt(0); var end = bend.view.path.PointAt(1);
                Check(begin == new Vector2(-190, -15) && end == new Vector2(185, 22), "existing path endpoints preserved");
                CheckContinuousRails(lab, denseHealthy.view.path, bend.view.path);
                lab.RenderNow();
                using (var helper = new VertexHelper())
                {
                    typeof(ConnectionDisplayGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)
                        .Invoke(bend.view.path, new object[] { helper });
                    Check(helper.currentVertCount > 40, "bent route emits rails and activity into a shared mesh");
                    int activityVertices = 0;
                    var vertex = new UIVertex(); bool inside = true;
                    for (int i = 0; i < helper.currentVertCount; i++)
                    {
                        helper.PopulateUIVertex(ref vertex, i);
                        if (!vertex.color.Equals((Color32)lab.style.Activity(bend.Current.trafficClass))) continue;
                        activityVertices++;
                        inside &= vertex.position.x >= begin.x - .001f && vertex.position.x <= end.x + .001f;
                    }
                    Check(inside, "all " + activityVertices + " activity vertices clipped inside route endpoint extent");
                    Check(activityVertices > 8 && lab.style.blipWidthFraction + .12f < 1, "activity width and feather remain inside rail body");
                }
                clock.SetReviewTime(11.99); lab.RenderNow(); float beforeReset = bend.Current.workProgress;
                clock.SetReviewTime(12.01); lab.RenderNow();
                Check(beforeReset > .99f && bend.Current.workProgress < .01f && bend.Current.operation == OperationKind.Write,
                    "new operation resets WORK and reverses direction without rerouting");
                var recover = lab.samples.Single(s => s.phases[0].state.identity == "d3");
                clock.SetReviewTime(2); lab.RenderNow(); Check(recover.Current.productiveSpeed < 1 && recover.Current.dangerSeverity > .5f, "recovery sequence starts slow and endangered");
                clock.SetReviewTime(8); lab.RenderNow(); Check(recover.Current.productiveSpeed == 1 && recover.Current.dangerSeverity > .5f, "full speed remains endangered");
                clock.SetReviewTime(14); lab.RenderNow(); Check(recover.Current.productiveSpeed == 1 && recover.Current.dangerSeverity == 0, "scripted danger recovery clears rail warning");
                var seen = new HashSet<DisplayResolution>();
                for (float t = 0; t < 25; t += .1f)
                { clock.SetReviewTime(t); lab.RenderNow(); foreach (var s in lab.samples) seen.Add(s.Current.resolution); }
                Check(seen.Count == 4, "active/completed/killed/timed-out outcomes replay without destroying views");
                lab.Replay(); Check(lab.samples.All(s => s.PhaseIndex == 0), "global replay restarts every transition");
                int objects = lab.GetComponentsInChildren<Transform>(true).Length;
                Check(objects < 450, "compact authored hierarchy: " + objects + " objects for 26 paths and ten comparisons");
                int revision = bend.view.path.GeometryRevision;
                // Warm every phase and TMP numeric buffers before measuring binding allocations.
                for (int i = 0; i < 1000; i++) { clock.SetReviewTime(i * .037); lab.RenderNow(); }
                long allocated = PresentationAllocationProbe.Measure(() => {
                    for (int i = 0; i < 1000; i++) { clock.SetReviewTime(i * .021); lab.RenderNow(); }
                });
                if (allocated >= 0) Check(allocated == 0, "1000 warmed presentation binding updates allocate " + allocated + " managed bytes (calibrated GC.Alloc)");
                else checks.Add("UNAVAILABLE allocation recorder failed sentinel calibration; no zero-allocation claim");
                Check(lab.GetComponentsInChildren<Transform>(true).Length == objects && bend.view.path.GeometryRevision == revision,
                    "animation creates no GameObjects and does not rebuild cached route arrays");
                Check(!ShaderUtil.ShaderHasError(lab.samples[0].view.pill.material.shader), "progress shader imported without reported errors");
                lab.Replay();
                Debug.Log("ConnectionDisplayLabWitness PASS: " + checks.Count(s => s.StartsWith("PASS")) + " assertions; hierarchy=" + objects + "; binding GC=" + allocated);
            }
            finally { File.WriteAllLines(Evidence + "/deterministic-checks.txt", checks); }
        }
        public static void Regressions()
        {
            Directory.CreateDirectory(Evidence); var report = new List<string>();
            foreach (var test in new (string name, Action run)[] { ("Geometry", GeometryWitness.Run), ("CanonicalScenes", CanonicalSceneWitness.Run) })
            { try { test.run(); report.Add("PASS " + test.name); } catch (Exception e) { report.Add("FAIL " + test.name + ": " + e.Message); } }
            File.WriteAllLines(Evidence + "/regression-checks.txt", report);
            if (report.Any(r => r.StartsWith("FAIL"))) throw new Exception("Display-lab regression failures; see report.");
        }

        private static void CheckContinuousRails(ConnectionDisplayLab lab, params ConnectionDisplayGraphic[] paths)
        {
            var populate = typeof(ConnectionDisplayGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null);
            foreach (var path in paths)
            {
                // Probe both opaque border interiors, including shared join endpoints.
                // Filter by rail color so the continuous body cannot conceal a broken outline.
                Vector2[] Vectors(string field) => (Vector2[])typeof(ConnectionDisplayGraphic)
                    .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(path);
                var starts = Vectors("segmentStarts"); var ends = Vectors("segmentEnds");
                var startOffsets = Vectors("startOffsets"); var endOffsets = Vectors("endOffsets");
                foreach (float speed in new[] { 1f, .65f, .22f, 0f })
                foreach (float severity in new[] { 0f, .3f, .8f, .98f, 1f })
                foreach (DisplayTrafficClass traffic in Enum.GetValues(typeof(DisplayTrafficClass)))
                foreach (bool deficit in new[] { false, true })
                foreach (double seconds in new[] { .125, .375 })
                {
                    var state = path.State; state.productiveSpeed = speed; state.dangerSeverity = severity;
                    state.trafficClass = traffic; state.exceedsNormalBudget = deficit; state.resolution = DisplayResolution.Active;
                    lab.heartbeat.SetReviewTime(seconds); path.Render(state, lab.style, lab.heartbeat);
                    using var mesh = new VertexHelper(); populate.Invoke(path, new object[] { mesh });
                    var vertices = new List<UIVertex>(); mesh.GetUIVertexStream(vertices);
                    Color32 rail = lab.style.Rail(state, lab.heartbeat.Beats);
                    bool covered = true;
                    for (int segment = 0; segment < starts.Length && covered; segment++)
                    for (int step = 0; step <= 100 && covered; step++)
                    foreach (float side in new[] { -.9f, .9f })
                    {
                        float t = step / 100f;
                        Vector2 probe = Vector2.Lerp(starts[segment], ends[segment], t) +
                            Vector2.Lerp(startOffsets[segment], endOffsets[segment], t) * side;
                        bool found = false;
                        for (int i = 0; i < vertices.Count && !found; i += 3)
                            if (vertices[i].color.Equals(rail) && vertices[i + 1].color.Equals(rail) && vertices[i + 2].color.Equals(rail))
                                found = InTriangle(probe, vertices[i].position, vertices[i + 1].position, vertices[i + 2].position);
                        covered &= found;
                    }
                    Color32 body = lab.style.Body(state, lab.heartbeat.Beats);
                    bool ribbon = true;
                    for (int segment = 0; segment < starts.Length && ribbon; segment++)
                    for (int step = 0; step <= 20 && ribbon; step++)
                    foreach (float side in new[] { -.5f, 0f, .5f })
                    {
                        float t = step / 20f;
                        Vector2 probe = Vector2.Lerp(starts[segment], ends[segment], t) +
                            Vector2.Lerp(startOffsets[segment], endOffsets[segment], t) * side;
                        bool found = false;
                        for (int i = 0; i < vertices.Count && !found; i += 3)
                            if (vertices[i].color.Equals(body) && vertices[i + 1].color.Equals(body) && vertices[i + 2].color.Equals(body))
                                found = InTriangle(probe, vertices[i].position, vertices[i + 1].position, vertices[i + 2].position);
                        ribbon &= found;
                    }
                    Check(covered && ribbon && body.a > 0 && body.a < 255 && body.r == rail.r && body.g == rail.g && body.b == rail.b,
                        $"continuous opaque rails and status-colored translucent body: segments={starts.Length}, speed={speed}, danger={severity}, class={traffic}, normal deficit={deficit}, time={seconds}");
                }
            }
        }
        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
            float x = Cross(b - a, p - a), y = Cross(c - b, p - b), z = Cross(a - c, p - c);
            return (x >= -.001f && y >= -.001f && z >= -.001f) || (x <= .001f && y <= .001f && z <= .001f);
        }
    }
}
