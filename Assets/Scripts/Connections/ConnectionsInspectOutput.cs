using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Each block owns its bindings. Destroying a history entry also stops its updates.
    public sealed class ConnectionsInspectOutput : MonoBehaviour
    {
        [SerializeField] private ConnectionsInspectEntry[] entries;
        [SerializeField] private LayoutElement layout;
        [SerializeField, Min(0.05f)] private float refreshSeconds = 0.2f;
        [SerializeField] private float oneEntryHeight = 156f;
        [SerializeField] private float twoEntryHeight = 282f;

        private ConnectionsLogicalConnectionState[] states;
        private bool[] frozen;
        private float nextRefresh;

        public void Bind(ConnectionsLogicalConnectionState[] selected)
        {
            if (selected == null || selected.Length == 0 || selected.Length > entries.Length)
                return;
            states = selected;
            frozen = new bool[selected.Length];
            for (int i = 0; i < entries.Length; i++)
                entries[i].gameObject.SetActive(i < states.Length);
            if (layout != null)
                layout.preferredHeight = states.Length == 1 ? oneEntryHeight : twoEntryHeight;
            Refresh();
            nextRefresh = Time.unscaledTime + refreshSeconds;
        }

        private void Update()
        {
            if (states == null || Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + refreshSeconds;
            Refresh();
        }

        private void Refresh()
        {
            bool allFrozen = true;
            for (int i = 0; i < states.Length; i++)
            {
                if (frozen[i])
                    continue;
                entries[i].Render(states[i]);
                if (states[i].IsComplete)
                    frozen[i] = true;
                else
                    allFrozen = false;
            }
            if (allFrozen)
                enabled = false;
        }

        private void OnDestroy()
        {
            states = null;
            frozen = null;
        }
    }
}
