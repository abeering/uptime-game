using System.Collections.Generic;
using TMPro;
using Uptime.Connections.Display;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    // Explicit one-time authoring. No ordinary UI hierarchy is built at runtime.
    public static class ConnectionDisplayLabAuthoring
    {
        public const string ScenePath = "Assets/Scenes/ConnectionsDisplayLab.unity";
        public const string PrefabFolder = "Assets/Prefabs/Connections/Display/";
        public const string StylePath = PrefabFolder + "DisplayStyle.asset";
        private static TMP_FontAsset font;
        private static ConnectionDisplayStyle style;
        private static ConnectionDisplayView displayPrefab;
        private static ConnectionDisplayLabSample rowPrefab;
        private static readonly Color Text = new(.78f, .84f, .88f);
        private static readonly Color Muted = new(.44f, .55f, .62f);

        [MenuItem("Tools/Connections/Author Connection Display Lab")]
        public static void Build()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Author outside Play Mode.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/JetBrainsMono-Regular SDF.asset");
            style = AssetDatabase.LoadAssetAtPath<ConnectionDisplayStyle>(StylePath);
            displayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "ConnectionDisplay.prefab")?.GetComponent<ConnectionDisplayView>();
            rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "DisplayLabSample.prefab")?.GetComponent<ConnectionDisplayLabSample>();
            if (style == null || displayPrefab == null || rowPrefab == null)
                throw new System.InvalidOperationException("Shared display assets must already exist. Lab authoring only rebuilds fixtures; it never creates or overwrites production assets.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvasObject = new GameObject("Connection Display Lab", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var root = canvasObject.GetComponent<RectTransform>();
            var background = UI("Background", root, new Vector2(1920, 1080), Vector2.zero).gameObject.AddComponent<Image>();
            background.color = new Color(.025f, .043f, .06f); background.raycastTarget = false;
            background.rectTransform.anchorMin = Vector2.zero; background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
            var lab = canvasObject.AddComponent<ConnectionDisplayLab>();
            lab.heartbeat = canvasObject.AddComponent<ConnectionHeartbeat>(); lab.style = style;
            Label("Title", root, "CONNECTION DISPLAY LAB / V0", 30, new Vector2(1400, 42), new Vector2(42, -25), Text);
            Label("Subtitle", root, "SYNTHETIC STATES  /  shared heartbeat  /  tinted ordinary + blue/teal important  /  shared gameplay assets", 17,
                new Vector2(1750, 24), new Vector2(42, -72), Muted);
            lab.clockLabel = Label("Clock", root, "120 BPM", 16, new Vector2(550, 26), new Vector2(1085, -32), Muted);
            Button(root, "PauseResume", "PAUSE / RESUME", new Vector2(1560, -30), lab.TogglePause);
            Button(root, "Replay", "REPLAY", new Vector2(1755, -30), lab.Replay, 125);
            string[] headings = { "A  IDENTITY / OPERATION", "B  PERFORMANCE", "C  DEADLINE RISK", "D  TRANSITIONS / OUTCOMES" };
            for (int col = 0; col < 4; col++) Label("Group " + col, root, headings[col], 22, new Vector2(444, 30), new Vector2(42 + col * 464, -111), Text);
            var samples = new List<ConnectionDisplayLabSample>();
            ConnectionDisplayLabSample Row(int col, int row, string title, params DisplayLabPhase[] phases)
            {
                var sample = (ConnectionDisplayLabSample)PrefabUtility.InstantiatePrefab(rowPrefab, root);
                sample.name = title; sample.title.text = title; sample.phases = phases;
                sample.GetComponent<RectTransform>().anchoredPosition = new Vector2(42 + col * 464, -151 - row * 116);
                samples.Add(sample); return sample;
            }
            Row(0, 0, "READ / LOW LOAD", P("a1", OperationKind.Read, 1, 1));
            Row(0, 1, "READ / HIGH LOAD", P("a2", OperationKind.Read, 8, 1));
            Row(0, 2, "WRITE / MEDIUM LOAD", P("a3", OperationKind.Write, 4, 1));
            Row(0, 3, "EXECUTE / LOCAL ACTIVITY", P("a4", OperationKind.Execute, 4, 1));
            var establish = P("a5", OperationKind.Read, 4, 0, important: true, duration: 4, label: "CONNECTING / no productive activity");
            establish.traversing = true; establish.advanceWork = false;
            var reroute = P("a5", OperationKind.Read, 4, 0, important: true, duration: 4, label: "REROUTING / established prefix + new suffix");
            reroute.traversing = true; reroute.establishedPrefix = .55f; reroute.advanceWork = false;
            Row(0, 4, "IMPORTANT / READ + TRAVERSAL", establish,
                P("a5", OperationKind.Read, 4, 1, important: true, duration: 8), reroute,
                P("a5", OperationKind.Read, 4, 1, important: true, duration: 8));
            Row(0, 5, "IMPORTANT / WRITE", P("a6", OperationKind.Write, 4, 1, important: true));
            Row(1, 0, "AT SPEED / SAFE", P("b1", OperationKind.Write, 6, 1));
            Row(1, 1, "MODERATE / SAFE", P("b2", OperationKind.Write, 6, .65f, label: "EVERY SECOND BEAT / long service budget"));
            Row(1, 2, "HEAVY SLOWDOWN / SAFE", P("b3", OperationKind.Write, 6, .22f, label: "EVERY FOURTH BEAT / density stays high"));
            Row(1, 3, "STALLED / SAFE FOR NOW", P("b4", OperationKind.Write, 6, 0, label: "NO MOVEMENT / synthetic long budget"));
            Row(1, 4, "IMPORTANT / SLOWED", P("b5", OperationKind.Write, 6, .65f, important: true));
            var bend = Row(1, 5, "BENT ROUTE / WIDTH 10 > 16 > 10", P("b6", OperationKind.Read, 6, 1, label: "3 joined segments / one arc-length activity phase"));
            bend.GetComponent<RectTransform>().sizeDelta = new Vector2(444, 170);
            bend.view.GetComponent<RectTransform>().anchoredPosition = new Vector2(222, -75);
            bend.diagnostic.rectTransform.anchoredPosition = new Vector2(10, -135);
            bend.phaseLabel.rectTransform.anchoredPosition = new Vector2(10, -153);
            bend.view.path.SetGeometry(new[] { new Vector2(-190, -15), new Vector2(-75, -15), new Vector2(-20, 22) },
                new[] { new Vector2(-75, -15), new Vector2(-20, 22), new Vector2(185, 22) }, new[] { 10f, 16f, 10f });
            bend.view.pillPosition = .22f;
            bend.phases = new[] {
                P("b6", OperationKind.Read, 6, 1, duration: 12, progress: 0, label: "READ / WORK resets at next operation"),
                P("b6", OperationKind.Write, 6, 1, duration: 12, progress: 0, label: "WRITE / same geometry, new operation") };
            foreach (var phase in bend.phases) phase.nominalWorkSeconds = 12;
            Label("Progress treatment note", root, "ANIMATED: INTERIOR / RACK: BOTH", 13,
                new Vector2(444, 22), new Vector2(1434, -976), Muted);
            Label("Performance note", root, "SPEED != DANGER\nDense + slow stays dense.\nRails remain visible during stall.", 16, new Vector2(420, 70), new Vector2(516, -919), Muted);
            Row(2, 0, "AT SPEED / SAFE", Risk("c1", 1, 0, 40, 20, 20));
            Row(2, 1, "AT SPEED / BEHIND, RECOVERABLE", Risk("c2", 1, .3f, 24, 20, 20));
            Row(2, 2, "AT SPEED / SEVERELY ENDANGERED", Risk("c3", 1, .8f, 20.5f, 20, 20));
            Row(2, 3, "SLOWED / BEHIND, RECOVERABLE", Risk("c4", .65f, .35f, 28, 31, 20));
            Row(2, 4, "IMPORTANT / ENDANGERED", Risk("c5", 1, .8f, 20.5f, 20, 20, important: true));
            Row(2, 5, "STALLED / IMMINENT TIMEOUT", Risk("c6", 0, .98f, 2, -1, 20, true));
            Row(2, 6, "EXCEEDS NORMAL-RATE BUDGET", Risk("c7", 1, 1, 12, 20, 20, true));
            var healthy = P("d1", OperationKind.Write, 5, 1, duration: 6, progress: .1f, label: "HEALTHY");
            var slowed = P("d1", OperationKind.Write, 5, .4f, duration: 8, progress: .35f, label: "SLOWED / SAFE");
            Row(3, 0, "HEALTHY > SLOWED > HEALTHY", healthy, slowed,
                P("d1", OperationKind.Write, 5, 1, duration: 6, progress: .4833f, label: "HEALTHY AGAIN"));
            var behind = P("d2", OperationKind.Read, 5, .55f, .3f, duration: 6, progress: .267f, label: "BEHIND / severity increases"); behind.endSeverity = .6f;
            var near = P("d2", OperationKind.Read, 5, .55f, .6f, duration: 6, progress: .404f, label: "ENDANGERED > CRITICAL"); near.endSeverity = 1;
            Row(3, 1, "SAFE > BEHIND > TIMEOUT", P("d2", OperationKind.Read, 5, 1, duration: 4, progress: .1f, label: "SAFE"), behind, near,
                Outcome("d2", DisplayResolution.TimedOut, .542f));
            Row(3, 2, "SLOWED > FULL SPEED > SAFE",
                P("d3", OperationKind.Write, 5, .4f, .8f, duration: 6, progress: .1f, label: "SLOWED + ENDANGERED"),
                P("d3", OperationKind.Write, 5, 1, .8f, duration: 6, progress: .2f, label: "FULL SPEED / STILL ENDANGERED"),
                P("d3", OperationKind.Write, 5, 1, 0, duration: 6, progress: .45f, label: "SAFE / scripted severity recovery"));
            var late = P("d4", OperationKind.Read, 5, 1, 1, duration: 6, progress: .2f, label: "FULL SPEED / NORMAL ETA > BUDGET"); late.state.exceedsNormalBudget = true;
            Row(3, 3, "FULL SPEED / CANNOT RECOVER NORMALLY",
                P("d4", OperationKind.Read, 5, .4f, .8f, duration: 6, progress: .1f, label: "SLOWED + ENDANGERED"), late,
                Outcome("d4", DisplayResolution.TimedOut, .45f));
            Row(3, 4, "KILLED / TWO FLASHES + CUT", P("d5", OperationKind.Write, 5, 1, duration: 5, progress: .4f, label: "ACTIVE / then killed"), Outcome("d5", DisplayResolution.Killed, .608f));
            Row(3, 5, "TIMED OUT / HEAT + FADE", P("d6", OperationKind.Read, 5, .5f, .75f, duration: 6, progress: .4f, label: "ENDANGERED / then timed out"), Outcome("d6", DisplayResolution.TimedOut, .525f));
            var completing = P("d7", OperationKind.Execute, 5, 1, duration: 4.2f, progress: .65f, label: "WORK ADVANCES TO 100%"); completing.nominalWorkSeconds = 12;
            Row(3, 6, "NATURAL COMPLETION / FADE", completing, Outcome("d7", DisplayResolution.Completed, 1));
            ProgressRack(root, "INTERIOR FILL / WORK", -866, PillProgressTreatment.Interior);
            ProgressRack(root, "PERIMETER FILL / WORK", -946, PillProgressTreatment.Perimeter);
            Label("Footer", root, "SEVERITY IS SYNTHETIC, NOT A GAMEPLAY FORECAST.  Normal-rate budget failure does not rule out future intervention.\nBPM / step / move-hold / easing: Heartbeat Inspector. Palette / density / rail / departure: DisplayStyle asset. Transitions loop; REPLAY resets all.",
                15, new Vector2(1830, 48), new Vector2(42, -1018), Muted);
            lab.samples = samples.ToArray();
            var comparisonPills = new List<ConnectionProgressPillGraphic>();
            foreach (var pill in root.GetComponentsInChildren<ConnectionProgressPillGraphic>(true))
                if (pill.transform.parent == root) comparisonPills.Add(pill);
            lab.progressComparisons = comparisonPills.ToArray(); lab.RenderNow();
            var camera = new GameObject("Lab Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -100); camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = background.color;
            camera.cullingMask = 0;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.SaveScene(canvasObject.scene, ScenePath);
            Debug.Log("Display lab authored: " + samples.Count + " path samples; ten pill comparisons; isolated synthetic driver.");
        }
        private static DisplayLabPhase P(string id, OperationKind op, float load, float speed, float severity = 0,
            bool important = false, float duration = 24, float progress = .25f, string label = "SHARED BEAT / current-operation WORK") => new()
        { label = label, duration = duration, state = new ConnectionDisplayState { identity = id, identitySlot = (id[id.Length - 1] - '1') % ConnectionIdentityPalette.SlotCount, operation = op,
            requestedLoad = load, productiveSpeed = speed, workProgress = progress, dangerSeverity = severity,
            trafficClass = important ? DisplayTrafficClass.Important : DisplayTrafficClass.Ordinary } };
        private static DisplayLabPhase Risk(string id, float speed, float severity, float remaining, float current, float normal, bool exceeds = false, bool important = false)
        {
            var p = P(id, OperationKind.Read, 5, speed, severity, important: important);
            p.advanceWork = false; p.state.workProgress = .45f; p.state.exceedsNormalBudget = exceeds;
            p.showServiceEstimates = true;
            p.serviceRemaining = remaining; p.projectedCurrent = current; p.projectedNormal = normal;
            p.label = current < 0 ? $"BUDGET {remaining:0.#}s / EST stalled / NORMAL {normal:0.#}s" :
                $"BUDGET {remaining:0.#}s / EST {current:0.#}s / NORMAL {normal:0.#}s";
            return p;
        }
        private static DisplayLabPhase Outcome(string id, DisplayResolution outcome, float progress)
        { var p = P(id, OperationKind.Read, 5, 0, duration: 1.5f, progress: progress, label: outcome.ToString().ToUpperInvariant());
            p.state.resolution = outcome; p.advanceWork = false; return p; }

        private static void ProgressRack(RectTransform parent, string title, float y, PillProgressTreatment treatment)
        {
            Label(title, parent, title, 16, new Vector2(444, 22), new Vector2(52, y + 18), Text);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "DisplayIdentityPill.prefab");
            float[] values = { 0, .25f, .5f, .75f, .99f };
            for (int i = 0; i < values.Length; i++)
            {
                var item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); item.name = treatment + " " + values[i];
                var rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.anchoredPosition = new Vector2(88 + i * 82, y - 24);
                item.GetComponent<ConnectionProgressPillGraphic>().Render(values[i], treatment, style.IdentityColor(DisplayTrafficClass.Ordinary, i));
                item.GetComponentInChildren<TMP_Text>().text = "p" + i;
                var caption = Label("Progress " + i, parent, (values[i] * 100).ToString("0") + "%", 12, new Vector2(62, 18), new Vector2(57 + i * 82, y - 42), Muted); caption.alignment = TextAlignmentOptions.Center;
            }
        }
        private static RectTransform UI(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>();
            if (parent != null) r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.sizeDelta = size; r.anchoredPosition = position; return r;
        }
        private static TMP_Text Label(string name, Transform parent, string value, float size, Vector2 bounds, Vector2 position, Color tint)
        {
            var r = UI(name, parent, bounds, position); var text = r.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.color = tint; text.text = value; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Overflow; return text;
        }
        private static void Button(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action, float width = 180)
        {
            var r = UI(name, parent, new Vector2(width, 34), position); var image = r.gameObject.AddComponent<Image>(); image.color = new Color(.12f, .23f, .31f);
            var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image; UnityEventTools.AddPersistentListener(button.onClick, action);
            var text = Label("Label", r, label, 15, new Vector2(width, 34), Vector2.zero, Text); text.alignment = TextAlignmentOptions.Center;
        }
    }
}
