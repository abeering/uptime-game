using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    // Explicit, one-time asset authoring. Runtime only binds these serialized labels.
    public static class FragilityAuthoring
    {
        public static void Apply()
        {
            const string pillPath = "Assets/Prefabs/Connections/IdentityPill.prefab";
            var pillRoot = PrefabUtility.LoadPrefabContents(pillPath);
            var pill = pillRoot.GetComponent<IdentityPillView>();
            var pillFields = new SerializedObject(pill);
            if (pillFields.FindProperty("serviceDangerText").objectReferenceValue == null)
            {
                var source = (TMP_Text)pillFields.FindProperty("idText").objectReferenceValue;
                var label = Object.Instantiate(source, source.transform.parent);
                label.name = "ServiceDanger"; label.text = "AT RISK"; label.fontSize = 11;
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
                label.rectTransform.pivot = new Vector2(.5f, .5f);
                label.rectTransform.anchoredPosition = new Vector2(0, -8);
                label.rectTransform.sizeDelta = new Vector2(48, 12);
                label.gameObject.SetActive(false);
                pillFields.FindProperty("serviceDangerText").objectReferenceValue = label;
                pillFields.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(pillRoot, pillPath);
            PrefabUtility.UnloadPrefabContents(pillRoot);
            const string inspectPath = "Assets/Prefabs/Connections/InspectOutput.prefab";
            var root = PrefabUtility.LoadPrefabContents(inspectPath);
            foreach (var entry in root.GetComponentsInChildren<InspectEntry>(true))
            {
                var fields = new SerializedObject(entry);
                if (fields.FindProperty("service").objectReferenceValue != null) continue;
                var source = (TMP_Text)fields.FindProperty("bottleneck").objectReferenceValue;
                var label = Object.Instantiate(source, source.transform.parent);
                label.name = "ServiceBudget"; label.text = "SERVICE";
                label.rectTransform.sizeDelta = new Vector2(500, 64);
                label.fontSize = 18; label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
                fields.FindProperty("service").objectReferenceValue = label;
                fields.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, inspectPath);
            PrefabUtility.UnloadPrefabContents(root);
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                EditorSceneManager.OpenScene(scene);
                var host = Object.FindFirstObjectByType<SimulationHost>();
                var fields = new SerializedObject(host);
                fields.FindProperty("serviceTimeoutsEnabled").boolValue = true;
                fields.FindProperty("serviceTimeoutTolerance").floatValue = 1.5f;
                fields.ApplyModifiedPropertiesWithoutUndo();
                var panel = Object.FindFirstObjectByType<NetworkHealthPanel>();
                fields = new SerializedObject(panel);
                if (fields.FindProperty("businessFailureText").objectReferenceValue == null)
                {
                    var clock = (TMP_Text)fields.FindProperty("clockText").objectReferenceValue;
                    var label = Object.Instantiate(clock, panel.transform);
                    label.name = "BusinessFailures"; label.text = "BUSINESS FAILURES 0";
                    label.fontSize = 22; label.raycastTarget = false;
                    var rect = label.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
                    rect.pivot = new Vector2(0, .5f);
                    rect.anchoredPosition = new Vector2(225, 0);
                    rect.sizeDelta = new Vector2(310, 40);
                    fields.FindProperty("businessFailureText").objectReferenceValue = label;
                    fields.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
                EditorSceneManager.SaveScene(host.gameObject.scene);
            }
        }
    }
}
