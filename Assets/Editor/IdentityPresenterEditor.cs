using Uptime.Connections;
using UnityEditor;

namespace Uptime.Connections.EditorTools
{
    [CustomEditor(typeof(IdentityPresenter))]
    public sealed class IdentityPresenterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Readability stage",
                    ((IdentityPresenter)target).StageDiagnostic);
        }
    }
}
