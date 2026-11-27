using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections.EditorTools
{
    public static class OperationRoutingCacheAuthoring
    {
        private static void Ref(UnityEngine.Object owner, string field, UnityEngine.Object value)
        { var s = new SerializedObject(owner); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
        public static void Apply()
        {
            var prefab = PrefabUtility.LoadPrefabContents(ApiConfigurationAuthoring.Prefab);
            Ref(prefab.GetComponent<ConnectionsSshOverlay>(), "description", prefab.transform.Find("Tradeoff").GetComponent<TMP_Text>());
            PrefabUtility.SaveAsPrefabAsset(prefab, ApiConfigurationAuthoring.Prefab); PrefabUtility.UnloadPrefabContents(prefab);
            foreach (string scene in CanonicalSceneAuthoring.Scenes)
            {
                EditorSceneManager.OpenScene(scene);
                var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                var api = host.FindNodeResources("API").View;
                if (host.FindNodeResources("CACHE") == null)
                {
                    var node = UnityEngine.Object.Instantiate(api, api.transform.parent, false); node.gameObject.name = "Node CACHE";
                    var nodeFields = new SerializedObject(node); nodeFields.FindProperty("nodeId").stringValue = "CACHE"; nodeFields.ApplyModifiedPropertiesWithoutUndo();
                    node.transform.Find("NodeHeading/NodeId").GetComponent<TMP_Text>().text = "CACHE";
                    node.transform.Find("NodeHeading/NodeRole").GetComponent<TMP_Text>().text = "READ CACHE";
                    var rect = (RectTransform)node.transform; var apiRect = (RectTransform)api.transform;
                    rect.anchoredPosition = scene == CanonicalSceneAuthoring.Scenes[0] ? apiRect.anchoredPosition + new Vector2(330, 250) :
                        new Vector2(((RectTransform)host.FindNodeResources("JOBS").View.transform).anchoredPosition.x, apiRect.anchoredPosition.y);
                    var source = host.PhysicalLinks[0].View;
                    var link = UnityEngine.Object.Instantiate(source, source.transform.parent, false);
                    var label = UnityEngine.Object.Instantiate(source.ResourceLabel, source.ResourceLabel.parent, false);
                    string id = "E" + (host.PhysicalLinks.Length + 1); link.name = "Physical link " + id; label.name = "Link label " + id;
                    link.transform.SetAsFirstSibling();
                    var linkFields = new SerializedObject(link); linkFields.FindProperty("linkId").stringValue = id;
                    linkFields.FindProperty("nodeA").objectReferenceValue = api; linkFields.FindProperty("nodeB").objectReferenceValue = node;
                    linkFields.FindProperty("label").objectReferenceValue = label;
                    var readout = label.GetComponentInChildren<TMP_Text>(true); readout.text = id;
                    linkFields.FindProperty("resourceReadout").objectReferenceValue = readout; linkFields.ApplyModifiedPropertiesWithoutUndo();
                    var fields = new SerializedObject(host); var nodes = fields.FindProperty("nodes"); int last = nodes.arraySize++;
                    var entry = nodes.GetArrayElementAtIndex(last); entry.FindPropertyRelative("view").objectReferenceValue = node;
                    entry.FindPropertyRelative("loadCapacity").floatValue = 10; entry.FindPropertyRelative("baselineLoad").floatValue = 0;
                    var links = fields.FindProperty("physicalLinks"); last = links.arraySize++; links.GetArrayElementAtIndex(last).FindPropertyRelative("view").objectReferenceValue = link;
                    fields.ApplyModifiedPropertiesWithoutUndo();
                    var presentation = UnityEngine.Object.FindFirstObjectByType<IntegrityPresenter>(); var bindings = new SerializedObject(presentation);
                    var cards = bindings.FindProperty("nodes"); last = cards.arraySize++; var card = cards.GetArrayElementAtIndex(last);
                    card.FindPropertyRelative("node").objectReferenceValue = node;
                    card.FindPropertyRelative("background").objectReferenceValue = node.GetComponent<Image>();
                    card.FindPropertyRelative("border").objectReferenceValue = node.GetComponent<Outline>();
                    card.FindPropertyRelative("accent").objectReferenceValue = node.transform.Find("Accent").GetComponent<Image>();
                    card.FindPropertyRelative("marker").objectReferenceValue = node.transform.Find("Infrastructure identity").GetComponent<TMP_Text>();
                    var edges = bindings.FindProperty("links"); last = edges.arraySize++; var edge = edges.GetArrayElementAtIndex(last);
                    edge.FindPropertyRelative("link").objectReferenceValue = link; edge.FindPropertyRelative("line").objectReferenceValue = link.GetComponent<Graphic>();
                    edge.FindPropertyRelative("label").objectReferenceValue = readout; bindings.ApplyModifiedPropertiesWithoutUndo();
                }
                Ref(host, "cacheConfigurationNode", host.FindNodeResources("CACHE").View);
                var tuning = new SerializedObject(host); tuning.FindProperty("cachedWorkMultiplier").floatValue = .25f; tuning.FindProperty("cachedLoadMultiplier").floatValue = .5f;
                tuning.ApplyModifiedPropertiesWithoutUndo();
                host.ResetSimulation(); EditorSceneManager.MarkSceneDirty(host.gameObject.scene); EditorSceneManager.SaveScene(host.gameObject.scene);
            }
            Debug.Log("OperationRoutingCacheAuthoring PASS: shared SSH bound; CACHE capacity10 and one API leaf link authored in both canonical scenes.");
        }
    }
}
