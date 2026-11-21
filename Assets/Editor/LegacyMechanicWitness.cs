using UnityEditor;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    // Explicit in-memory fixture for older component tests, never saved to a scene.
    // These tests predate Integrity boundary subdivision/endings and the expanded status prefab.
    // CanonicalSceneWitness independently verifies the actual authored configuration and those rules.
    public static class LegacyMechanicWitness
    {
        public static void Configure(SimulationHost host, bool originalStatusPrefab = false)
        {
            var fields = new SerializedObject(host);
            fields.FindProperty("shiftEnabled").boolValue = false;
            fields.FindProperty("integrityTuning.enabled").boolValue = false;
            fields.ApplyModifiedPropertiesWithoutUndo();
            if (host.GetComponent<IntegrityPresenter>() is { } presenter) presenter.enabled = false;
            if (host.GetComponent<HackerDirectives>() is { } directives) directives.enabled = false;
            if (host.Nodes.Length == 4)
            {
                var traffic = new SerializedObject(host.GetComponent<ConnectionTrafficDirector>());
                traffic.FindProperty("payloadSpawnerWeight").floatValue = 0;
                traffic.FindProperty("backdoorWeight").floatValue = 0;
                traffic.ApplyModifiedPropertiesWithoutUndo();
                var activity = host.GetComponent<HostileActivityDirector>();
                activity.OrdinaryWeight = 1; activity.ReconnaissanceWeight = activity.PayloadWeight = activity.WormWeight =
                    activity.PayloadLauncherWeight = activity.DirectDuelWeight = activity.ConnectionDuelWeight = 0;
            }
            if (originalStatusPrefab)
            {
                var status = new SerializedObject(Object.FindFirstObjectByType<NodeStatusPresenter>());
                status.FindProperty("statusPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NodeStatusView>("Assets/Prefabs/Connections/NodeStatus.prefab");
                status.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
