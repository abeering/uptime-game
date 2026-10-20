using Uptime.Connections;
using UnityEditor;

[CustomEditor(typeof(ConnectionsIdentityPresenter))]
public sealed class ConnectionsIdentityPresenterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.TextField("Readability stage",
                ((ConnectionsIdentityPresenter)target).StageDiagnostic);
    }
}
