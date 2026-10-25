using System;
using System.Linq;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class InspectV2Witness
    {
        [MenuItem("Tools/Connections/Run Inspect V2 Witness")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Connections.unity");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var prefab = AssetDatabase.LoadAssetAtPath<InspectOutput>(
                "Assets/Prefabs/Connections/InspectOutput.prefab");
            Check(host != null && director != null && prefab != null,
                "scene and inspect prefab bindings");
            Material pillMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Shaders/ConnectionsPill.mat");
            var identityPrefab = AssetDatabase.LoadAssetAtPath<IdentityPillView>(
                "Assets/Prefabs/Connections/IdentityPill.prefab");
            var operationPrefab = AssetDatabase.LoadAssetAtPath<InspectOperationToken>(
                "Assets/Prefabs/Connections/InspectOperationToken.prefab");
            Check(pillMaterial != null && pillMaterial.shader != null &&
                pillMaterial.shader.isSupported && identityPrefab != null &&
                operationPrefab != null &&
                identityPrefab.GetComponent<PillGraphic>().material == pillMaterial &&
                operationPrefab.GetComponentInChildren<PillGraphic>().material == pillMaterial &&
                UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                    .Any(c => (c.additionalShaderChannels &
                        AdditionalCanvasShaderChannels.TexCoord1) != 0),
                "identity and operation pills use supported antialiased material and UV channel");
            bool oldAuto = director.AutoGenerationEnabled;
            try
            {
                CheckCommand("inspect a1", InspectPresentationMode.Compact, "a1");
                CheckCommand("i a1", InspectPresentationMode.Compact, "a1");
                CheckCommand("inspect -v a1", InspectPresentationMode.Verbose, "a1");
                CheckCommand("inspect --verbose a1", InspectPresentationMode.Verbose, "a1");
                CheckCommand("i -v a1", InspectPresentationMode.Verbose, "a1");
                CheckCommand("i --verbose a1", InspectPresentationMode.Verbose, "a1");
                CheckCommand("inspect a1 b2", InspectPresentationMode.Compact, "a1", "b2");
                CheckCommand("inspect -v a1 b2", InspectPresentationMode.Verbose, "a1", "b2");
                CheckCommand("inspect a1 b2 c3 d4 e5", InspectPresentationMode.Compact,
                    "a1", "b2", "c3", "d4", "e5");
                CheckCommand("i --verbose a1 b2 c3 d4 e5", InspectPresentationMode.Verbose,
                    "a1", "b2", "c3", "d4", "e5");
                CheckInvalid("inspect", "USAGE");
                CheckInvalid("i --verbose", "USAGE");
                CheckInvalid("inspect --other a1", "invalid flag");
                CheckInvalid("inspect a1 --verbose", "invalid flag");

                director.AutoGenerationEnabled = false;
                host.SetRunMode(ConnectionRunMode.Generated);
                NodeView[] route = { Node(host, "EXT1"), Node(host, "API"), Node(host, "DB") };
                director.QueueSpawnPlan(Plan("iv1", route,
                    new[] { Stage(OperationKind.Read, 100f, 2f) }));
                director.QueueSpawnPlan(Plan("iv2", route,
                    new[] { Stage(OperationKind.Write, 100f, 2f) }));
                for (int i = 3; i <= 5; i++)
                    director.QueueSpawnPlan(Plan("iv" + i, route,
                        new[] { Stage(OperationKind.Read, 100f, 1f) }));
                host.StepSimulation(1f);
                LogicalConnectionState first = host.FindConnection("iv1");
                LogicalConnectionState second = host.FindConnection("iv2");
                InspectOutput compact = UnityEngine.Object.Instantiate(prefab);
                InspectOutput verbose = UnityEngine.Object.Instantiate(prefab);
                InspectOutput many = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    compact.Bind(new[] { first, second });
                    Check(Height(compact) == 232f && Entry(compact, 0).rect.height == 98f &&
                        Entry(compact, 1).rect.height == 98f,
                        "two consecutive compact results use two-row cards");
                    CheckCompact(compact, 0, "LOAD 2", 1);
                    CheckCompact(compact, 1, "LOAD 2", 1);
                    verbose.Bind(new[] { first }, presentationMode: InspectPresentationMode.Verbose);
                    Check(Height(verbose) == 234f && Entry(verbose, 0).rect.height == 210f,
                        "verbose active result retains its detailed height");
                    LogicalConnectionState[] five = Enumerable.Range(1, 5)
                        .Select(i => host.FindConnection("iv" + i)).ToArray();
                    many.Bind(five);
                    Check(Height(many) == 562f &&
                        many.GetComponentsInChildren<InspectEntry>().Length == 5 &&
                        Enumerable.Range(0, 5).All(i =>
                            EntryComponent(many, i).GetComponent<RectTransform>().rect.height == 98f),
                        "one inspect output grows to five authored entry instances");
                    for (int i = 0; i < 5; i++)
                        CheckCompact(many, i, i < 2 ? "LOAD 2" : "LOAD 1", 1);
                    many.Bind(five, presentationMode: InspectPresentationMode.Verbose);
                    Check(Height(many) == 1122f && Enumerable.Range(0, 5).All(i =>
                        ((TMP_Text)new SerializedObject(EntryComponent(many, i))
                            .FindProperty("throughput").objectReferenceValue).gameObject.activeSelf),
                        "five-ID verbose output keeps each detailed card");
                    var detail = new SerializedObject(EntryComponent(verbose, 0));
                    Check(((TMP_Text)detail.FindProperty("throughput").objectReferenceValue)
                            .text == "AT SPEED" &&
                        ((TMP_Text)detail.FindProperty("work").objectReferenceValue)
                            .text.Contains("WORK") &&
                        ((TMP_Text)detail.FindProperty("bottleneck").objectReferenceValue)
                            .gameObject.activeSelf &&
                        ((TMP_Text)detail.FindProperty("eta").objectReferenceValue)
                            .gameObject.activeSelf &&
                        ((Image)detail.FindProperty("progressFill").objectReferenceValue)
                            .transform.parent.gameObject.activeSelf,
                        "verbose retains allocation, WORK, activity, ETA, and progress bar");
                    Check(((TMP_Text)detail.FindProperty("throughput").objectReferenceValue)
                        .gameObject.activeSelf, "verbose retains performance explanation");
                    CheckCardGeometry(EntryComponent(compact, 0), false, pillMaterial);
                    CheckCardGeometry(EntryComponent(verbose, 0), true, pillMaterial);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(compact.gameObject);
                    UnityEngine.Object.DestroyImmediate(verbose.gameObject);
                    UnityEngine.Object.DestroyImmediate(many.gameObject);
                }

                host.ResetSimulation();
                var stages = new[]
                {
                    Stage(OperationKind.Read, 2f, 2f),
                    Stage(OperationKind.Write, 2f, 2f),
                    Stage(OperationKind.Execute, 2f, 2f),
                    Stage(OperationKind.Read, 20f, 2f),
                    Stage(OperationKind.Write, 2f, 2f),
                    Stage(OperationKind.Execute, 2f, 2f)
                };
                director.QueueSpawnPlan(Plan("long", route, stages));
                for (int i = 0; i < 4; i++) host.StepSimulation(1f);
                LogicalConnectionState longState = host.FindConnection("long");
                Check(longState.CurrentOperationIndex == 3,
                    "long plan reached middle operation without changing plan");
                InspectOutput wide = UnityEngine.Object.Instantiate(prefab);
                var narrowHost = new GameObject("Narrow terminal", typeof(RectTransform));
                var narrowRect = (RectTransform)narrowHost.transform;
                narrowRect.sizeDelta = new Vector2(390f, 900f);
                InspectOutput narrow = UnityEngine.Object.Instantiate(prefab, narrowRect, false);
                try
                {
                    wide.Bind(new[] { longState });
                    InspectOperationToken[] tokens = Tokens(EntryComponent(wide, 0));
                    Check(tokens.Length >= 2 && tokens.Any(t => t.Text == "+2") &&
                        tokens.Any(t => t.Text.Contains("READ") && !t.HasCheckmark) &&
                        tokens.Any(t => t.HasCheckmark) &&
                        tokens.All(t => t.HasOutline),
                        "wide middle timeline retains completed/current/future groups: " +
                        string.Join(",", tokens.Select(t => t.Text)));
                    narrow.Bind(new[] { longState });
                    InspectEntry narrowEntry = EntryComponent(narrow, 0);
                    InspectOperationToken[] trimmed = Tokens(narrowEntry);
                    Check(trimmed.Any(t => t.Text.Contains("READ")) &&
                        trimmed.All(t => t.HasOutline),
                        "narrow timeline retains current operation: " +
                        string.Join(",", trimmed.Select(t => t.Text)));
                    var fields = new SerializedObject(narrowEntry);
                    RectTransform row = (RectTransform)fields.FindProperty("operationRow")
                        .objectReferenceValue;
                    var badge = (InspectOperationToken)fields.FindProperty("loadBadge")
                        .objectReferenceValue;
                    Check(badge != null && badge.Text == "LOAD 2" &&
                        Mathf.Abs(((RectTransform)badge.transform).anchoredPosition.x + 16f) < 0.1f &&
                        row.anchoredPosition.x + row.rect.width + 12f <=
                            narrowEntry.GetComponent<RectTransform>().rect.width - 16f -
                            ((RectTransform)badge.transform).rect.width + 0.5f &&
                        ((RectTransform)narrow.transform).rect.width == 390f,
                        "narrow card reserves a stable right-aligned LOAD badge without overflow");
                    Check(longState.Definition.OperationCount == 6,
                        "visual trimming does not mutate the Connection Plan");
                    narrowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 330f);
                    narrow.Bind(new[] { longState });
                    InspectOperationToken[] tight = Tokens(EntryComponent(narrow, 0));
                    Check(tight.Any(t => t.Text == "READ") &&
                        ((RectTransform)narrow.transform).rect.width == 330f,
                        "tighter card keeps current operation without reducing pill size: " +
                        string.Join(",", tight.Select(t => t.Text)));
                    narrowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300f);
                    narrow.Bind(new[] { longState });
                    var extremeFields = new SerializedObject(EntryComponent(narrow, 0));
                    var extremeRow = (RectTransform)extremeFields.FindProperty("operationRow")
                        .objectReferenceValue;
                    var extremeBadge = (InspectOperationToken)extremeFields
                        .FindProperty("loadBadge").objectReferenceValue;
                    Check(Tokens(EntryComponent(narrow, 0)).Any(t => t.Text.Contains("READ")) &&
                        extremeRow.anchoredPosition.x + extremeRow.rect.width + 12f <=
                            EntryComponent(narrow, 0).GetComponent<RectTransform>().rect.width -
                            16f - ((RectTransform)extremeBadge.transform).rect.width + 0.5f,
                        "extreme width keeps current operation and reserved badge space");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(wide.gameObject);
                    UnityEngine.Object.DestroyImmediate(narrowHost);
                }
                host.KillConnection("long");
                InspectOutput killed = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    killed.Bind(new[] { longState });
                    CheckCompact(killed, 0, "LOAD 0", -1);
                    Check(Tokens(EntryComponent(killed, 0)).Any(t => t.HasCheckmark),
                        "killed history retains completed operations");
                    var killedFields = new SerializedObject(EntryComponent(killed, 0));
                    var killedPill = (IdentityPillView)killedFields
                        .FindProperty("headerIdentityPill").objectReferenceValue;
                    Check(killedPill.AccentColor == killedFields.FindProperty("quietColor").colorValue,
                        "resolved identity pill uses muted outline");
                }
                finally { UnityEngine.Object.DestroyImmediate(killed.gameObject); }

                host.ResetSimulation();
                director.QueueSpawnPlan(Plan("dynamic", route, new[]
                {
                    new OperationStage(OperationKind.Write, 100f, 2f, "Writing records",
                        CompletionEffect.None, new[]
                        { TimedModifierBehaviorDefinition.Spiking("spike", 1f, 1f, 10f) })
                }, TimedModifierBehaviorDefinition.Overload("overload", 2f, 3f)));
                host.StepSimulation(1f);
                LogicalConnectionState dynamic = host.FindConnection("dynamic");
                InspectOutput live = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    live.Bind(new[] { dynamic });
                    CheckCompact(live, 0, "LOAD 2", 1);
                    host.StepSimulation(1f);
                    EntryComponent(live, 0).Render(dynamic);
                    CheckCompact(live, 0, "LOAD 12", 1);
                    host.StepSimulation(1f);
                    EntryComponent(live, 0).Render(dynamic);
                    CheckCompact(live, 0, "LOAD 5", 1);
                    Check(Tokens(EntryComponent(live, 0))[0].Text == "WRITE → DB",
                        "effective LOAD updates without changing current operation identity");
                }
                finally { UnityEngine.Object.DestroyImmediate(live.gameObject); }

                host.ResetSimulation();
                director.QueueSpawnPlan(Plan("done", route,
                    new[] { Stage(OperationKind.Read, 2f, 2f) }));
                host.StepSimulation(1f);
                InspectOutput done = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    done.Bind(new[] { host.FindConnection("done") });
                    CheckCompact(done, 0, "LOAD 0", 1);
                    Check(Tokens(EntryComponent(done, 0))[0].HasCheckmark,
                        "completed history remains inspectable in compact mode");
                }
                finally { UnityEngine.Object.DestroyImmediate(done.gameObject); }
                host.ResetSimulation();
                director.QueueSpawnPlan(Plan("offline", route,
                    new[] { Stage(OperationKind.Read, 100f, 2f) }));
                host.StepSimulation(0.1f);
                Check(host.TryInjectInfection("API", InfectionKind.Blackout, 0f, out _),
                    "Blackout inspection setup");
                InspectOutput interrupted = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    interrupted.Bind(new[] { host.FindConnection("offline") },
                        presentationMode: InspectPresentationMode.Verbose);
                    var status = new SerializedObject(EntryComponent(interrupted, 0));
                    Check(((TMP_Text)status.FindProperty("throughput").objectReferenceValue)
                            .text == "BLACKOUT" &&
                        !Tokens(EntryComponent(interrupted, 0))[0].HasCheckmark,
                        "inspect shows Blackout outcome without marking unfinished operation complete");
                }
                finally { UnityEngine.Object.DestroyImmediate(interrupted.gameObject); }
                host.ResetSimulation();
                ConnectionSpawnPlan hiddenThreat = director.QueueProfile("spiking-threat");
                ConnectionSpawnPlan matchingBenign = director.QueueProfile("spiking-benign");
                host.StepSimulation(0.1f);
                InspectOutput hidden = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    var threatState = host.FindConnection(hiddenThreat.ConnectionId);
                    var benignState = host.FindConnection(matchingBenign.ConnectionId);
                    Check(threatState.IsHostile && !benignState.IsHostile &&
                        threatState.Definition.ProfileId != benignState.Definition.ProfileId,
                        "test pair differs in internal intent and profile ID");
                    hidden.Bind(new[] { threatState, benignState },
                        presentationMode: InspectPresentationMode.Verbose);
                    string visible = string.Join(" ", hidden.GetComponentsInChildren<TMP_Text>(true)
                        .Select(label => label.text));
                    Check(!visible.Contains("spiking-threat") &&
                        !visible.Contains("spiking-benign") &&
                        !visible.Contains("InfectionDelivery") &&
                        !visible.Contains("HOSTILE") &&
                        Tokens(EntryComponent(hidden, 0))[0].Text ==
                            Tokens(EntryComponent(hidden, 1))[0].Text,
                        "multi-ID verbose inspect keeps internal intent, profile, and future effect hidden");
                }
                finally { UnityEngine.Object.DestroyImmediate(hidden.gameObject); }
                Debug.Log("InspectV2Witness PASS: command modes, multi-ID, compact/verbose, " +
                    "width-aware six-stage timeline, fixed badge, live effective LOAD, " +
                    "Blackout outcome, and history.");
            }
            finally
            {
                director.AutoGenerationEnabled = oldAuto;
                host.SetRunMode(ConnectionRunMode.Generated);
            }
        }

        private static ConnectionSpawnPlan Plan(string id, NodeView[] route,
            OperationStage[] stages, params TimedModifierBehaviorDefinition[] connectionBehaviors) =>
            new ConnectionSpawnPlan(0f, id, route, "inspect-v2-witness", "Records",
                stages, false, connectionBehaviors);
        private static OperationStage Stage(OperationKind kind, float work, float load) =>
            new OperationStage(kind, work, load, kind.ToString());
        private static NodeView Node(SimulationHost host, string id) =>
            host.Nodes.Single(n => n.View.NodeId == id).View;
        private static RectTransform Entry(InspectOutput output, int index) =>
            (RectTransform)EntryComponent(output, index).transform;
        private static InspectEntry EntryComponent(InspectOutput output, int index) =>
            output.GetComponentsInChildren<InspectEntry>(true)[index];
        private static float Height(InspectOutput output) =>
            ((LayoutElement)new SerializedObject(output).FindProperty("layout")
                .objectReferenceValue).preferredHeight;
        private static InspectOperationToken[] Tokens(InspectEntry entry)
        {
            var row = (RectTransform)new SerializedObject(entry).FindProperty("operationRow")
                .objectReferenceValue;
            return row.GetComponentsInChildren<InspectOperationToken>();
        }
        private static void CheckCompact(InspectOutput output, int index, string badge, int tokenCount)
        {
            InspectEntry entry = EntryComponent(output, index);
            var fields = new SerializedObject(entry);
            var load = (InspectOperationToken)fields.FindProperty("loadBadge").objectReferenceValue;
            Check(load != null && load.Text == badge && load.HasOutline &&
                (tokenCount < 0 || Tokens(entry).Length == tokenCount) &&
                !((TMP_Text)fields.FindProperty("throughput").objectReferenceValue)
                    .gameObject.activeSelf &&
                !((TMP_Text)fields.FindProperty("work").objectReferenceValue)
                    .gameObject.activeSelf &&
                !((TMP_Text)fields.FindProperty("bottleneck").objectReferenceValue)
                    .gameObject.activeSelf &&
                !((TMP_Text)fields.FindProperty("eta").objectReferenceValue)
                    .gameObject.activeSelf &&
                !((Image)fields.FindProperty("progressFill").objectReferenceValue)
                    .transform.parent.gameObject.activeSelf,
                "compact content and badge " + badge);
        }
        private static void CheckCardGeometry(InspectEntry entry, bool verbose, Material material)
        {
            var fields = new SerializedObject(entry);
            var pill = (IdentityPillView)fields.FindProperty("headerIdentityPill")
                .objectReferenceValue;
            var rail = (Image)fields.FindProperty("stateTag").objectReferenceValue;
            var purpose = (TMP_Text)fields.FindProperty("purpose").objectReferenceValue;
            var row = (RectTransform)fields.FindProperty("operationRow").objectReferenceValue;
            var load = (InspectOperationToken)fields.FindProperty("loadBadge").objectReferenceValue;
            var status = (TMP_Text)fields.FindProperty("throughput").objectReferenceValue;
            var work = (TMP_Text)fields.FindProperty("work").objectReferenceValue;
            RectTransform card = (RectTransform)entry.transform;
            RectTransform pillRect = (RectTransform)pill.transform;
            var pillFields = new SerializedObject(pill);
            var plate = (PillGraphic)pillFields.FindProperty("plate").objectReferenceValue;
            var label = (TMP_Text)pillFields.FindProperty("idText").objectReferenceValue;
            Check(pill != null && plate.material == material && pill.Size == new Vector2(104f, 70f) &&
                pillFields.FindProperty("fontSize").floatValue == 51f &&
                pillFields.FindProperty("tintTextWithAccent").boolValue &&
                label.alignment == TextAlignmentOptions.Center &&
                label.rectTransform.sizeDelta == new Vector2(-16f, -8f) &&
                new SerializedObject(plate).FindProperty("cornerRadius").floatValue == 13f &&
                new SerializedObject(plate).FindProperty("borderThickness").floatValue == 3f,
                "authored large ID pill typography, border, padding, and antialiasing");
            Check(rail.rectTransform.rect.height == card.rect.height &&
                entry.transform.Find("TitleBar") == null &&
                rail.rectTransform.anchoredPosition.x == 0f &&
                pillRect.anchoredPosition == new Vector2(16f, -12f) &&
                purpose.rectTransform.anchoredPosition.x >= pillRect.anchoredPosition.x + pill.Size.x + 12f &&
                row.anchoredPosition.x == purpose.rectTransform.anchoredPosition.x &&
                row.rect.height == 40f &&
                ((RectTransform)load.transform).rect.width == 126f &&
                row.anchoredPosition.x + row.rect.width + 12f <=
                    card.rect.width - 16f - 126f + 0.5f &&
                status.rectTransform.anchoredPosition.x == purpose.rectTransform.anchoredPosition.x &&
                status.gameObject.activeSelf == verbose &&
                work.rectTransform.anchoredPosition.x == 16f &&
                purpose.text != "INSPECT /",
                "compact and verbose upper geometry preserves rail, timeline, and LOAD sizes");
        }
        private static void CheckCommand(string command, InspectPresentationMode expected,
            params string[] expectedIds)
        {
            Check(Terminal.TryParseInspect(command.Split(' '), out var mode,
                    out string[] ids, out string error) &&
                mode == expected && ids.SequenceEqual(expectedIds) && error == null,
                "inspect command " + command);
        }
        private static void CheckInvalid(string command, string message)
        {
            Check(!Terminal.TryParseInspect(command.Split(' '), out _,
                    out _, out string error) && error.Contains(message),
                "invalid inspect command " + command);
        }
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("InspectV2Witness failed: " + name);
        }
    }
}
