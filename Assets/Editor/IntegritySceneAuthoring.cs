using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    // One-scene authoring recipe. All generated objects are serialized; runtime only binds them.
    public static class IntegritySceneAuthoring
    {
        public const string Scene = "Assets/Scenes/ConnectionsAdvancedIntegrity.unity";
        public const string StatusPrefab = "Assets/Prefabs/Connections/NodeStatusIntegrity.prefab";
        public static void Build()
        {
            EditorSceneManager.OpenScene(MaximumPressureWitness.Pressure);
            ApplyToLoadedScene(true);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Integrity authored scene and seven-row status prefab saved.");
        }

        // Shared Editor authoring path for both canonical topologies; runtime UI stays serialized.
        public static void ApplyToLoadedScene(bool rebuildStatusPrefab = false)
        {
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            if (host.GetComponent<IntegrityPresenter>() != null)
                throw new InvalidOperationException("Loaded scene already has Integrity presentation.");
            var settings = new SerializedObject(host);
            settings.FindProperty("integrityTuning.enabled").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var presenter = host.gameObject.AddComponent<IntegrityPresenter>();
            var fields = new SerializedObject(presenter);
            Set(fields, "host", host);
            var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            Set(fields, "terminal", terminal);
            Set(fields, "cleanOverlay", UnityEngine.Object.FindFirstObjectByType<ConnectionsCleanOverlay>(FindObjectsInactive.Include));
            Set(fields, "duelOverlay", UnityEngine.Object.FindFirstObjectByType<ConnectionsDuelOverlay>(FindObjectsInactive.Include));
            var template = host.Nodes[0].View.transform.Find("NodeHeading/NodeRole").GetComponent<TMP_Text>();
            var bindings = fields.FindProperty("nodes");
            bindings.arraySize = host.Nodes.Length;
            for (int i = 0; i < host.Nodes.Length; i++)
            {
                NodeView node = host.Nodes[i].View;
                var binding = bindings.GetArrayElementAtIndex(i);
                binding.FindPropertyRelative("node").objectReferenceValue = node;
                binding.FindPropertyRelative("background").objectReferenceValue = node.GetComponent<Image>();
                binding.FindPropertyRelative("border").objectReferenceValue = node.GetComponent<Outline>();
                binding.FindPropertyRelative("accent").objectReferenceValue = node.transform.Find("Accent").GetComponent<Image>();
                TMP_Text marker = Text("Infrastructure identity", node.transform, template, 12f);
                marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = Vector2.one;
                marker.rectTransform.pivot = Vector2.one;
                marker.rectTransform.anchoredPosition = new Vector2(-10f, -8f);
                marker.rectTransform.sizeDelta = new Vector2(77f, 20f);
                marker.alignment = TextAlignmentOptions.TopRight;
                marker.text = node.NodeId.StartsWith("EXT", StringComparison.Ordinal) ? "UNTRUSTED" :
                    node.NodeId == "DB" || node.NodeId == "AUTH" ? "CRITICAL" : "";
                binding.FindPropertyRelative("marker").objectReferenceValue = marker;
                if (node.NodeId.StartsWith("EXT", StringComparison.Ordinal))
                    node.transform.Find("NodeHeading/NodeRole").GetComponent<TMP_Text>().text = node.NodeId == "EXT1" ? "CUSTOMER" : "PARTNER";
            }
            var edges = fields.FindProperty("links");
            edges.arraySize = host.PhysicalLinks.Length;
            for (int i = 0; i < host.PhysicalLinks.Length; i++)
            {
                PhysicalLinkView edge = host.PhysicalLinks[i].View;
                var binding = edges.GetArrayElementAtIndex(i);
                binding.FindPropertyRelative("link").objectReferenceValue = edge;
                binding.FindPropertyRelative("line").objectReferenceValue = edge.GetComponent<Graphic>();
                binding.FindPropertyRelative("label").objectReferenceValue = new SerializedObject(edge).FindProperty("resourceReadout").objectReferenceValue;
            }
            var panel = new GameObject("Core breach result", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(terminal.GetComponentInParent<Canvas>().transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(2f / 3f, .5f);
            rect.sizeDelta = new Vector2(700f, 210f);
            rect.anchoredPosition = new Vector2(0f, -190f);
            panel.GetComponent<Image>().color = new Color(.10f, .025f, .04f, .97f);
            panel.GetComponent<Image>().raycastTarget = false;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, .25f, .3f);
            TMP_Text title = Text("Title", panel.transform, template, 30f);
            title.text = "CORE BREACHED";
            title.color = new Color(1f, .35f, .4f);
            title.rectTransform.anchorMin = new Vector2(0, 1);
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.pivot = new Vector2(.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0, -16);
            title.rectTransform.sizeDelta = new Vector2(-32, 40);
            title.alignment = TextAlignmentOptions.Center;
            TMP_Text path = Text("Winning path", panel.transform, template, 18f);
            path.rectTransform.anchorMin = Vector2.zero;
            path.rectTransform.anchorMax = Vector2.one;
            path.rectTransform.offsetMin = new Vector2(20, 16);
            path.rectTransform.offsetMax = new Vector2(-20, -66);
            path.alignment = TextAlignmentOptions.Center;
            Set(fields, "breachPanel", panel);
            Set(fields, "breachPath", path);
            panel.SetActive(false);
            fields.ApplyModifiedPropertiesWithoutUndo();
            var strip = UnityEngine.Object.FindFirstObjectByType<NetworkHealthPanel>();
            var stripFields = new SerializedObject(strip);
            foreach (string name in new[] { "valueText", "rateText" })
                ((Component)stripFields.FindProperty(name).objectReferenceValue).gameObject.SetActive(false);
            ((Component)stripFields.FindProperty("healthFill").objectReferenceValue).transform.parent.gameObject.SetActive(false);
            if (rebuildStatusPrefab) BuildStatusPrefab();
            var status = UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>();
            var statusFields = new SerializedObject(status);
            statusFields.FindProperty("statusPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NodeStatusView>(StatusPrefab);
            statusFields.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
        }

        private static void BuildStatusPrefab()
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Connections/NodeStatus.prefab"));
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (NodeStatusPanel panel in instance.GetComponentsInChildren<NodeStatusPanel>(true))
            {
                var fields = new SerializedObject(panel);
                foreach (string name in new[] { "labels", "timers" })
                {
                    SerializedProperty array = fields.FindProperty(name);
                    var last = (TMP_Text)array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue;
                    int old = array.arraySize;
                    array.arraySize = 7;
                    for (int i = old; i < 7; i++)
                    {
                        TMP_Text extra = UnityEngine.Object.Instantiate(last, last.transform.parent, false);
                        extra.name = name + " " + i;
                        array.GetArrayElementAtIndex(i).objectReferenceValue = extra;
                    }
                }
                fields.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(instance, StatusPrefab);
            UnityEngine.Object.DestroyImmediate(instance);
        }
        private static TMP_Text Text(string name, Transform parent, TMP_Text template, float size)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            text.transform.SetParent(parent, false);
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
            text.fontSize = size;
            text.color = template.color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }
        private static void Set(SerializedObject fields, string name, UnityEngine.Object value) =>
            fields.FindProperty(name).objectReferenceValue = value;
    }
}
