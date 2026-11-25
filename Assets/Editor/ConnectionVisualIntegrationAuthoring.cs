using TMPro;
using Uptime.Connections.Display;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    // Explicit, one-time composition/binding change. Lab fixture authoring never calls this.
    public static class ConnectionVisualIntegrationAuthoring
    {
        public const string GameplayPath = "Assets/Prefabs/Connections/Display/GameplayConnection.prefab";
        private const string Folder = ConnectionDisplayLabAuthoring.PrefabFolder;
        public static void Apply()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPath) != null)
                throw new System.InvalidOperationException("Visual integration migration is already applied. Edit shared assets directly; this one-time command must not regenerate production prefabs.");
            var style = AssetDatabase.LoadAssetAtPath<ConnectionDisplayStyle>(ConnectionDisplayLabAuthoring.StylePath);
            var shared = PrefabUtility.LoadPrefabContents(Folder + "ConnectionDisplay.prefab");
            var pathRect = shared.GetComponent<ConnectionDisplayView>().path.rectTransform;
            pathRect.anchorMin = Vector2.zero; pathRect.anchorMax = Vector2.one;
            pathRect.sizeDelta = pathRect.anchoredPosition = Vector2.zero;
            PrefabUtility.SaveAsPrefabAsset(shared, Folder + "ConnectionDisplay.prefab");
            PrefabUtility.UnloadPrefabContents(shared);

            var pill = PrefabUtility.LoadPrefabContents(Folder + "DisplayIdentityPill.prefab");
            var identity = pill.GetComponent<IdentityPillView>() ?? pill.AddComponent<IdentityPillView>();
            var fields = new SerializedObject(identity);
            fields.FindProperty("progressPlate").objectReferenceValue = pill.GetComponent<ConnectionProgressPillGraphic>();
            fields.FindProperty("displayStyle").objectReferenceValue = style;
            fields.FindProperty("idText").objectReferenceValue = pill.GetComponentInChildren<TMP_Text>();
            fields.FindProperty("dimensions").vector2Value = new Vector2(62, 32);
            fields.FindProperty("fontSize").floatValue = 20;
            fields.ApplyModifiedPropertiesWithoutUndo();
            ((RectTransform)pill.transform).sizeDelta = new Vector2(62, 32);
            pill.GetComponentInChildren<TMP_Text>().fontSize = 20;
            PrefabUtility.SaveAsPrefabAsset(pill, Folder + "DisplayIdentityPill.prefab");
            PrefabUtility.UnloadPrefabContents(pill);

            var display = AssetDatabase.LoadAssetAtPath<ConnectionDisplayView>(Folder + "ConnectionDisplay.prefab");
            var root = new GameObject("GameplayConnection", typeof(RectTransform), typeof(CanvasGroup), typeof(ConnectionDisplayView), typeof(LogicalPathView));
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.sizeDelta = rect.anchoredPosition = Vector2.zero;
            var view = root.GetComponent<ConnectionDisplayView>(); view.style = style; view.displayGroup = root.GetComponent<CanvasGroup>();
            var path = root.GetComponent<LogicalPathView>(); fields = new SerializedObject(path);
            fields.FindProperty("sharedDisplay").objectReferenceValue = view;
            fields.FindProperty("sharedPathPrefab").objectReferenceValue = display.path;
            fields.FindProperty("terminalMarkerPrefab").objectReferenceValue = display.terminalMarker;
            fields.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, GameplayPath); Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var pathPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPath).GetComponent<LogicalPathView>();
            var pillPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "DisplayIdentityPill.prefab").GetComponent<IdentityPillView>();
            var legacy = AssetDatabase.LoadAssetAtPath<PathGraphic>("Assets/Prefabs/Connections/LogicalPath.prefab");
            if (pathPrefab == null || pillPrefab == null) throw new System.InvalidOperationException("Shared component import/binding failed.");
            foreach (string scene in new[] { "Connections", "ConnectionsAdvanced" })
            {
                var opened = EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
                pathPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPath).GetComponent<LogicalPathView>();
                pillPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "DisplayIdentityPill.prefab").GetComponent<IdentityPillView>();
                Debug.Log("Shared bindings " + scene + " path=" + AssetDatabase.GetAssetPath(pathPrefab) + " pill=" + AssetDatabase.GetAssetPath(pillPrefab));
                var host = Object.FindFirstObjectByType<SimulationHost>();
                var clock = host.GetComponent<ConnectionHeartbeat>() ?? host.gameObject.AddComponent<ConnectionHeartbeat>();
                clock.externallyDriven = true;
                var adapter = host.GetComponent<ConnectionGameplayDisplay>() ?? host.gameObject.AddComponent<ConnectionGameplayDisplay>();
                adapter.host = host; adapter.heartbeat = clock; adapter.style = style;
                adapter.duel = host.GetComponent<ConnectionsDuelDirector>();
                var presenter = Object.FindFirstObjectByType<LogicalPathPresenter>(); fields = new SerializedObject(presenter);
                fields.FindProperty("pathPrefab").objectReferenceValue = pathPrefab;
                fields.FindProperty("connectionDisplay").objectReferenceValue = adapter;
                fields.FindProperty("geometryPreviewPathPrefab").objectReferenceValue = legacy;
                fields.ApplyModifiedPropertiesWithoutUndo();
                var packing = Object.FindFirstObjectByType<IdentityPresenter>(); fields = new SerializedObject(packing);
                fields.FindProperty("pillPrefab").objectReferenceValue = pillPrefab; fields.ApplyModifiedPropertiesWithoutUndo();
                if (new SerializedObject(presenter).FindProperty("pathPrefab").objectReferenceValue == null ||
                    new SerializedObject(packing).FindProperty("pillPrefab").objectReferenceValue == null)
                    throw new System.InvalidOperationException("Canonical shared prefab binding was lost.");
                Object.FindFirstObjectByType<Canvas>().rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                EditorSceneManager.SaveScene(opened);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
