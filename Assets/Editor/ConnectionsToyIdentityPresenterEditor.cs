using Uptime.ConnectionsToy;
using UnityEditor;

[CustomEditor(typeof(ConnectionsToyIdentityPresenter))]
public sealed class ConnectionsToyIdentityPresenterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.TextField("Readability stage",
                ((ConnectionsToyIdentityPresenter)target).StageDiagnostic);
    }
}
