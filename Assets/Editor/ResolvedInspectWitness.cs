using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class ResolvedInspectWitness
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Tools/Connections/Run Resolved Inspect Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>("Assets/Prefabs/Connections/InspectOutput.prefab");
            var travel = typeof(SimulationHost).GetField("connectionTravelSecondsPerLink", Private);
            float oldTravel = (float)travel.GetValue(host);
            bool oldAuto = director.AutoGenerationEnabled;
            float oldScale = Time.timeScale;
            try
            {
                director.AutoGenerationEnabled = false;
                travel.SetValue(host, 0.000001f);
                host.SetRunMode(ConnectionRunMode.Generated);
                var route = new[] { Node(host, "EXT1"), Node(host, "API"), Node(host, "DB") };
                LogicalConnectionState Add(string id, OperationKind kind, float work) =>
                    host.RegisterConnection(LogicalConnectionDefinition.FromPlan(new ConnectionSpawnPlan(
                        host.SimulationTimeSeconds, id, route, "resolved-inspect-witness",
                        "Customer Authentication", new[] { new OperationStage(kind, work, 2f, "Service") })));
                var done = Add("rc", OperationKind.Execute, 0.1f);
                host.StepSimulation(1f);
                var killed = Add("rk", OperationKind.Write, 100f);
                host.StepSimulation(0.1f); host.KillConnection(killed.ConnectionId);
                var canceled = Add("rx", OperationKind.Read, 100f);
                host.StepSimulation(0.1f);
                Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _), "cancel setup");
                host.TryRemoveInfection("API", InfectionKind.Blackout, out _);
                host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _);
                var canceledWhileConnecting = Add("rp", OperationKind.Read, 100f);
                host.StepSimulation(0.1f);
                Check(!canceledWhileConnecting.HasEstablished, "cancel before establishment setup");
                host.TryRemoveInfection("API", InfectionKind.Blackout, out _);
                var loader = director.GenerateLoaderPlanForWitness(host.SimulationTimeSeconds,
                    LoaderStructure.Single, LoaderPattern.Immediate);
                director.QueueSpawnPlan(loader);
                host.StepSimulation(loader.LoaderAttack.durationSeconds + 3f);
                var withdrawn = host.FindConnection(loader.ConnectionId);
                var states = new[] { done, killed, canceled, withdrawn, canceledWhileConnecting };
                var expected = new[] { "COMPLETE", "KILLED", "CANCELED", "WITHDRAWN", "CANCELED" };
                Check(states.All(s => s.IsComplete) && states.Select(s => s.Resolution).SequenceEqual(
                    new ConnectionResolution?[] { ConnectionResolution.Completed, ConnectionResolution.Killed,
                        ConnectionResolution.Canceled, ConnectionResolution.Withdrawn,
                        ConnectionResolution.Canceled }), "four authoritative outcomes and pre-establishment cancellation");
                var facts = states.Select(Facts).ToArray();
                float simulationTime = host.SimulationTimeSeconds;
                var output = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    output.Bind(states, presentationMode: InspectPresentationMode.Verbose);
                    Check(new SerializedObject(output).FindProperty("resolvedHoldSeconds").floatValue == 4f,
                        "authored hold is four UI seconds");
                    float deadline = Deadlines(output)[0];
                    Advance(output, deadline - 0.001f);
                    for (int i = 0; i < states.Length; i++)
                    {
                        InspectEntry entry = Entries(output)[i];
                        Check(!entry.IsResolvedSummary && Rect(entry).rect.height == 128f &&
                            Field<TMP_Text>(entry, "throughput").text == expected[i] &&
                            Field<InspectOperationToken>(entry, "loadBadge").Text == "LOAD 0.0",
                            "full resolved card held before boundary: " + expected[i]);
                    }
                    Time.timeScale = 0f;
                    Advance(output, deadline);
                    for (int i = 0; i < states.Length; i++) CheckSummary(Entries(output)[i], states[i], expected[i]);
                    Check(!output.enabled && host.SimulationTimeSeconds == simulationTime &&
                        states.Select(Facts).SequenceEqual(facts), "UI collapse freezes without changing simulation facts");
                    foreach (float width in new[] { 552f, 362f, 272f })
                        foreach (InspectEntry entry in Entries(output).Take(states.Length))
                        {
                            entry.SetCardWidth(width, 0f);
                            CheckRowFits(Field<ResolvedInspectRow>(entry, "resolvedRow"));
                        }
                    output.Bind(new[] { done });
                    Check(!Entries(output)[0].IsResolvedSummary && Rect(Entries(output)[0]).rect.height == 98f,
                        "historical reinspection restores the existing chosen mode and a fresh hold");
                    Advance(output, Deadlines(output)[0]);
                    CheckSummary(Entries(output)[0], done, "COMPLETE");
                }
                finally { UnityEngine.Object.DestroyImmediate(output.gameObject); }

                Time.timeScale = oldScale;
                var active = Add("ra", OperationKind.Read, 1000f);
                host.StepSimulation(0.1f);
                var mixed = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    mixed.Bind(new[] { done, active }, presentationMode: InspectPresentationMode.Verbose);
                    float firstDeadline = Deadlines(mixed)[0];
                    Advance(mixed, firstDeadline);
                    Check(Entries(mixed)[0].IsResolvedSummary && !Entries(mixed)[1].IsResolvedSummary &&
                        Rect(Entries(mixed)[1]).rect.height == 210f, "resolved and active cards are independent");
                    host.KillConnection(active.ConnectionId);
                    Advance(mixed, firstDeadline + 1f);
                    Check(Deadlines(mixed)[1] == firstDeadline + 5f && !Entries(mixed)[1].IsResolvedSummary,
                        "later resolution starts its own four-second hold");
                    Advance(mixed, firstDeadline + 4.999f);
                    Check(!Entries(mixed)[1].IsResolvedSummary, "later card remains full until its boundary");
                    Advance(mixed, firstDeadline + 5f);
                    CheckSummary(Entries(mixed)[1], active, "KILLED");
                }
                finally { UnityEngine.Object.DestroyImmediate(mixed.gameObject); }

                var reading = Add("rr", OperationKind.Read, 1000f);
                host.StepSimulation(0.1f);
                CheckScroll(prefab, done, reading);
                Debug.Log("ResolvedInspectWitness PASS: four outcomes, exact four-second UI hold, " +
                    "paused time, independent cards, fresh historical inspection, narrow layout, " +
                    "simulation invariance, scroll anchoring, bottom following, and input preservation.");
            }
            finally
            {
                Time.timeScale = oldScale;
                travel.SetValue(host, oldTravel);
                director.AutoGenerationEnabled = oldAuto;
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static void CheckSummary(InspectEntry entry, LogicalConnectionState state, string expected)
        {
            var row = Field<ResolvedInspectRow>(entry, "resolvedRow");
            var operation = Field<InspectOperationToken>(row, "operation");
            var id = Field<IdentityPillView>(row, "identityPill");
            Check(entry.IsResolvedSummary && Rect(entry).rect.height == 48f &&
                !Field<GameObject>(entry, "fullContent").activeSelf && row.gameObject.activeSelf &&
                Field<TMP_Text>(row, "resolution").text == expected &&
                operation.HasCheckmark == (state.Resolution == ConnectionResolution.Completed) &&
                id.DisplayedId == state.ConnectionId.ToLowerInvariant() && id.Size == new Vector2(54f, 28f) &&
                row.GetComponentsInChildren<TMP_Text>().All(t => !t.text.StartsWith("LOAD ") && !t.text.StartsWith("WORK ")),
                "compact row retains exact outcome and appropriate completion mark: " + expected);
            CheckRowFits(row);
        }

        private static void CheckRowFits(ResolvedInspectRow row)
        {
            Canvas.ForceUpdateCanvases(); row.FitWidth();
            var name = Field<TMP_Text>(row, "connectionName");
            var status = Field<TMP_Text>(row, "resolution");
            var op = Field<InspectOperationToken>(row, "operation");
            float opLeft = Rect(op).anchoredPosition.x - Rect(op).rect.width;
            float statusLeft = status.rectTransform.anchoredPosition.x - status.rectTransform.rect.width;
            Check(name.rectTransform.anchoredPosition.x + name.rectTransform.rect.width <= opLeft - 9.9f &&
                Rect(op).anchoredPosition.x <= statusLeft - 9.9f &&
                status.rectTransform.anchoredPosition.x <= Rect(row).rect.width - 11.9f,
                "summary name, operation, and status do not overlap");
        }

        private static void CheckScroll(InspectOutput prefab, LogicalConnectionState done, LogicalConnectionState active)
        {
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            var content = Field<RectTransform>(terminal, "historyContent");
            var scroll = Field<ScrollRect>(terminal, "historyScroll");
            var input = Field<TMP_InputField>(terminal, "commandInput");
            GameObject Spacer(string name)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
                go.transform.SetParent(content, false); go.GetComponent<LayoutElement>().preferredHeight = 1000f;
                return go;
            }
            var before = Spacer("Resolved inspect witness before");
            var output = UnityEngine.Object.Instantiate(prefab, content);
            var after = Spacer("Resolved inspect witness after");
            try
            {
                output.Bind(new[] { done, active }, presentationMode: InspectPresentationMode.Verbose);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content); Canvas.ForceUpdateCanvases();
                var anchor = Rect(Entries(output)[1]);
                float Top()
                {
                    var corners = new Vector3[4]; anchor.GetWorldCorners(corners);
                    return scroll.viewport.InverseTransformPoint(corners[1]).y;
                }
                content.anchoredPosition += new Vector2(0f, scroll.viewport.rect.yMax - Top());
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition);
                Canvas.ForceUpdateCanvases();
                float top = Top();
                input.SetTextWithoutNotify("inspect unfinished");
                input.selectionStringAnchorPosition = 2; input.selectionStringFocusPosition = 5;
                bool focused = input.isFocused;
                var events = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
                var selected = events.currentSelectedGameObject;
                Advance(output, Deadlines(output)[0]);
                Check(Mathf.Abs(Top() - top) < 0.5f, "later visible card stays anchored when earlier card shrinks");
                Check(input.text == "inspect unfinished" && input.selectionStringAnchorPosition == 2 &&
                    input.selectionStringFocusPosition == 5 && input.isFocused == focused &&
                    events.currentSelectedGameObject == selected,
                    "collapse leaves draft, selection, focus, and selected object untouched");
                output.Bind(new[] { done, active }, presentationMode: InspectPresentationMode.Verbose);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content); Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 0f;
                Advance(output, Deadlines(output)[0]);
                Check(Mathf.Abs(scroll.verticalNormalizedPosition) < 0.001f, "bottom stays followed during collapse");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(before);
                UnityEngine.Object.DestroyImmediate(output.gameObject);
                UnityEngine.Object.DestroyImmediate(after);
            }
        }

        private static string Facts(LogicalConnectionState s) =>
            $"{s.Resolution}/{s.Lifecycle}/{s.CurrentOperationIndex}/{s.CompletedWork:R}/{s.DesiredLoad:R}/{s.AllocatedLoad:R}/{s.ResolvedAtSeconds:R}";
        private static NodeView Node(SimulationHost host, string id) => host.Nodes.Single(n => n.View.NodeId == id).View;
        private static RectTransform Rect(Component c) => (RectTransform)c.transform;
        private static InspectEntry[] Entries(InspectOutput output) => output.GetComponentsInChildren<InspectEntry>(true);
        private static T Field<T>(UnityEngine.Object obj, string name) where T : UnityEngine.Object =>
            (T)new SerializedObject(obj).FindProperty(name).objectReferenceValue;
        private static float[] Deadlines(InspectOutput output) => (float[])typeof(InspectOutput).GetField("collapseAt", Private).GetValue(output);
        private static void Advance(InspectOutput output, float now) =>
            typeof(InspectOutput).GetMethod("RefreshPresentation", Private).Invoke(output, new object[] { now });
        private static void Check(bool valid, string message)
        { if (!valid) throw new InvalidOperationException("ResolvedInspectWitness: " + message); }
    }
}
