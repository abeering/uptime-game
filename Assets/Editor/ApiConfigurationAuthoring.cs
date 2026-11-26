using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class ApiConfigurationAuthoring
    {
        public const string Prefab = "Assets/Prefabs/Connections/ConnectionsSshOverlay.prefab";
        private static void Set(UnityEngine.Object obj, string field, UnityEngine.Object value)
        { var s = new SerializedObject(obj); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
        private static TMP_Text Label(Transform parent, string name, TMP_Text source, float y, float height, float fontSize)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>(); label.transform.SetParent(parent, false);
            label.font = source.font; label.fontSharedMaterial = source.fontSharedMaterial; label.fontSize = fontSize;
            label.color = new Color(.70f, .89f, .82f); label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis;
            var r = label.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(22, y); r.sizeDelta = new Vector2(616, height); return label;
        }
        private static void BuildPrefab(TMP_Text source)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefab) != null) return;
            var root = new GameObject("Connections SSH", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(ConnectionsSshOverlay));
            var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(660, 550); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            root.GetComponent<Image>().color = new Color(.035f, .105f, .13f, .98f);
            root.GetComponent<Outline>().effectColor = new Color(.35f, .75f, .68f); root.GetComponent<Outline>().effectDistance = Vector2.one;
            var overlay = root.GetComponent<ConnectionsSshOverlay>();
            var title = Label(root.transform, "Title", source, -18, 34, 28); title.text = "SSH / API";
            var policy = Label(root.transform, "Current policy", source, -66, 30, 22); policy.text = "FAVORED: NONE    WEIGHT ×2";
            var note = Label(root.transform, "Tradeoff", source, -106, 26, 19); note.text = "FAVOR ONE PURPOSE · TOTAL CAPACITY UNCHANGED";
            var labels = new TMP_Text[8]; var buttons = new Button[8];
            for (int i = 0; i < labels.Length; i++)
            {
                var row = new GameObject("Purpose choice " + i, typeof(RectTransform), typeof(Image), typeof(Button)); row.transform.SetParent(root.transform, false);
                var r = (RectTransform)row.transform; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
                r.anchoredPosition = new Vector2(16, -148 - i * 40); r.sizeDelta = new Vector2(628, 38);
                var image = row.GetComponent<Image>(); image.color = new Color(.12f, .23f, .24f, .45f);
                buttons[i] = row.GetComponent<Button>(); buttons[i].targetGraphic = image;
                buttons[i].navigation = new Navigation { mode = Navigation.Mode.None };
                labels[i] = Label(row.transform, "Label", source, -3, 32, 19); labels[i].rectTransform.anchoredPosition = new Vector2(8, -3); labels[i].rectTransform.sizeDelta = new Vector2(612, 32);
                labels[i].text = i == 0 ? "> [X] NONE / CLEAR PRIORITY" : "  [ ] PURPOSE";
            }
            var hint = Label(root.transform, "Keyboard hint", source, -506, 28, 20); hint.text = "↑↓ CHOOSE   ENTER APPLY   ESC CLOSE";
            Set(overlay, "title", title); Set(overlay, "currentPolicy", policy); Set(overlay, "hint", hint);
            var fields = new SerializedObject(overlay); var entries = fields.FindProperty("choices"); var actions = fields.FindProperty("choiceButtons");
            entries.arraySize = labels.Length; actions.arraySize = buttons.Length;
            for (int i = 0; i < labels.Length; i++) { entries.GetArrayElementAtIndex(i).objectReferenceValue = labels[i]; actions.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i]; }
            fields.ApplyModifiedPropertiesWithoutUndo(); root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, Prefab); UnityEngine.Object.DestroyImmediate(root);
        }
        public static void FitText()
        {
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                float height = label.name == "Title" ? 45 : label.name == "Tradeoff" ? 32 : label.name == "Keyboard hint" ? 34 : 36;
                var size = label.rectTransform.sizeDelta; size.y = height; label.rectTransform.sizeDelta = size;
                if (label.name == "Label") { label.rectTransform.anchoredPosition = new Vector2(8, -1); label.fontSize = 19; }
            }
            PrefabUtility.SaveAsPrefabAsset(root, Prefab); PrefabUtility.UnloadPrefabContents(root);
        }
        public static void Apply()
        {
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                EditorSceneManager.OpenScene(scene);
                var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
                var clean = UnityEngine.Object.FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include);
                var source = (TMP_Text)new SerializedObject(clean).FindProperty("body").objectReferenceValue;
                BuildPrefab(source); FitText();
                var overlay = UnityEngine.Object.FindFirstObjectByType<ConnectionsSshOverlay>(FindObjectsInactive.Include);
                if (overlay == null) overlay = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), clean.transform.parent)).GetComponent<ConnectionsSshOverlay>();
                var duel = UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include);
                var placement = UnityEngine.Object.FindFirstObjectByType<ConnectionsWindowPlacement>();
                Set(overlay, "escapeCoordinator", UnityEngine.Object.FindFirstObjectByType<EscapeCoordinator>());
                Set(overlay, "placement", placement); Set(overlay, "duelOverlay", duel);
                Set(terminal, "sshOverlay", overlay); Set(duel, "sshOverlay", overlay); Set(placement, "sshOverlay", overlay);
                var fields = new SerializedObject(placement); var content = fields.FindProperty("sshContent");
                var labels = overlay.GetComponentsInChildren<TMP_Text>(true); content.arraySize = labels.Length;
                for (int i = 0; i < labels.Length; i++) content.GetArrayElementAtIndex(i).objectReferenceValue = labels[i].rectTransform;
                fields.ApplyModifiedPropertiesWithoutUndo(); Set(host, "apiConfigurationNode", host.FindNodeResources("API").View);
                var hostFields = new SerializedObject(host); hostFields.FindProperty("apiAllocationWeight").floatValue = 2; hostFields.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(host.gameObject.scene); EditorSceneManager.SaveScene(host.gameObject.scene);
            }
        }
    }
}
