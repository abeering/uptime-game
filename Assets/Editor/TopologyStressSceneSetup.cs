using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    public static class TopologyStressSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/ConnectionsTopologyStress.unity";

        // Run once after copying Connections.unity to ScenePath. Never saves the source scene.
        public static void ConfigureCopiedScene()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Run in a disposable batch Editor project.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            NodeView[] nodes = UnityEngine.Object.FindObjectsByType<NodeView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            PhysicalLinkView[] links = UnityEngine.Object.FindObjectsByType<PhysicalLinkView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (nodes.Length != 4 || links.Length != 3)
                throw new InvalidOperationException("Expected the authored four-node Connections baseline.");
            NodeView nodeTemplate = nodes.Single(n => n.NodeId == "API");
            PhysicalLinkView linkTemplate = links.Single(l => l.LinkId == "E1");
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var director = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            var area = GameObject.Find("PhysicalTopology").GetComponent<RectTransform>();
            GameObject.Find("NetworkSubhead").GetComponent<TMP_Text>().text =
                "GENERATED TOPOLOGY  //  LIVE STRESS TRAFFIC";
            var preview = UnityEngine.Object.FindFirstObjectByType<GeometryPreview>();
            if (host == null || director == null || preview == null ||
                linkTemplate.ResourceLabel == null)
                throw new InvalidOperationException("Copied scene lacks Connections components.");
            preview.enabled = false;
            foreach (PhysicalLinkView link in links)
            {
                link.ResourceLabel.gameObject.SetActive(false);
                link.gameObject.SetActive(false);
            }
            foreach (NodeView node in nodes) node.gameObject.SetActive(false);
            var root = new GameObject("Topology Stress Harness");
            var harness = root.AddComponent<TopologyStressHarness>();
            var serialized = new SerializedObject(harness);
            serialized.FindProperty("topologyArea").objectReferenceValue = area;
            serialized.FindProperty("nodeTemplate").objectReferenceValue = nodeTemplate;
            serialized.FindProperty("linkTemplate").objectReferenceValue = linkTemplate;
            serialized.FindProperty("host").objectReferenceValue = host;
            serialized.FindProperty("director").objectReferenceValue = director;
            serialized.FindProperty("statusLabel").objectReferenceValue =
                GameObject.Find("NetworkSubhead").GetComponent<TMP_Text>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            harness.Configure(8, 1701, 2401, StressTrafficDensity.Typical);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save stress scene.");
            Debug.Log("TopologyStressSceneSetup PASS: separate opt-in scene configured.");
        }

        public static void RefreshCopiedScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var harness = UnityEngine.Object.FindFirstObjectByType<TopologyStressHarness>();
            var label = GameObject.Find("NetworkSubhead").GetComponent<TMP_Text>();
            var serialized = new SerializedObject(harness);
            serialized.FindProperty("statusLabel").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            label.text = "GENERATED TOPOLOGY  //  LIVE STRESS TRAFFIC";
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not refresh stress scene.");
            Debug.Log("TopologyStressSceneSetup PASS: stress scene label bound.");
        }
    }
}
