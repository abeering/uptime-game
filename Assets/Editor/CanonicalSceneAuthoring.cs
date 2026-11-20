using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    // Explicit one-time promotion of the existing assets, never a runtime scene builder.
    public static class CanonicalSceneAuthoring
    {
        public static readonly string[] Scenes = { "Assets/Scenes/Connections.unity", "Assets/Scenes/ConnectionsAdvanced.unity" };
        [Serializable] private sealed class Recipe { public ConnectionTrafficProfile profile; }
        public static void Promote()
        {
            EditorSceneManager.OpenScene(IntegritySceneAuthoring.Scene);
            var source = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            string storefront = JsonUtility.ToJson(new Recipe { profile = source.Profiles.Single(p => p.id == "storefront-page") });
            foreach (string scene in Scenes)
            {
                EditorSceneManager.OpenScene(scene);
                var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                if (host.GetComponent<IntegrityPresenter>() != null)
                    throw new InvalidOperationException("Canonical scene already promoted: " + scene);
                var traffic = host.GetComponent<ConnectionTrafficDirector>();
                // Materialize Small's existing legal default catalog instead of copying Advanced overrides.
                traffic.ResetDirector();
                if (scene == Scenes[1])
                    IntegrityWitness.Set(traffic, "profiles", traffic.Profiles.Concat(new[] {
                        JsonUtility.FromJson<Recipe>(storefront).profile }).ToArray());
                var trafficFields = new SerializedObject(traffic);
                trafficFields.FindProperty("payloadSpawnerWeight").floatValue = 1;
                trafficFields.FindProperty("backdoorWeight").floatValue = 1;
                trafficFields.ApplyModifiedPropertiesWithoutUndo();
                var activity = host.GetComponent<HostileActivityDirector>();
                activity.OrdinaryWeight = 4; activity.ReconnaissanceWeight = 2; activity.PayloadWeight = 2;
                activity.WormWeight = 1; activity.PayloadLauncherWeight = 1;
                activity.DirectDuelWeight = 1; activity.ConnectionDuelWeight = 1;
                IntegritySceneAuthoring.ApplyToLoadedScene();
                var status = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<NodeStatusPresenter>());
                status.FindProperty("duelDirector").objectReferenceValue = host.GetComponent<ConnectionsDuelDirector>();
                status.ApplyModifiedPropertiesWithoutUndo();
                var directives = host.gameObject.AddComponent<HackerDirectives>();
                var fields = new SerializedObject(directives);
                fields.FindProperty("host").objectReferenceValue = host;
                fields.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
                EditorSceneManager.SaveScene(host.gameObject.scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CanonicalSceneAuthoring PASS: promoted existing Small and Advanced assets; default timing/layout preserved.");
        }
    }
}
