using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    public sealed class NodeView : MonoBehaviour
    {
        [SerializeField] private string nodeId;
        [SerializeField] private NodeLoadBar loadBar;
        [Header("Logical path attachment widths (UI pixels)")]
        [SerializeField, Min(0f)] private float north = 96f;
        [SerializeField, Min(0f)] private float northEast = 72f;
        [SerializeField, Min(0f)] private float east = 72f;
        [SerializeField, Min(0f)] private float southEast = 72f;
        [SerializeField, Min(0f)] private float south = 96f;
        [SerializeField, Min(0f)] private float southWest = 72f;
        [SerializeField, Min(0f)] private float west = 72f;
        [SerializeField, Min(0f)] private float northWest = 72f;

        public string NodeId => nodeId;
        public NodeLoadBar LoadBar => loadBar;
        private TMP_Text roleLabel;
        private string normalRole;
        private Color normalRoleColor;

        public void RenderAvailability(bool offline)
        {
            if (roleLabel == null)
            {
                roleLabel = transform.Find("NodeHeading/NodeRole")?.GetComponent<TMP_Text>();
                if (roleLabel == null) return;
                normalRole = roleLabel.text;
                normalRoleColor = roleLabel.color;
            }
            string desired = offline ? "OFFLINE" : normalRole;
            if (roleLabel.text != desired) roleLabel.text = desired;
            roleLabel.color = offline ? new Color(1f, 0.42f, 0.34f, 1f) : normalRoleColor;
        }

        // Used by the opt-in topology stress scene on copies of its authored node.
        public void ConfigureStressNode(string id, string role)
        {
            nodeId = id;
            Transform heading = transform.Find("NodeHeading");
            TMP_Text idLabel = heading != null ? heading.Find("NodeId")?.GetComponent<TMP_Text>() : null;
            TMP_Text roleLabel = heading != null ? heading.Find("NodeRole")?.GetComponent<TMP_Text>() : null;
            if (idLabel == null || roleLabel == null)
                throw new System.InvalidOperationException("Stress node template needs NodeHeading/NodeId and NodeRole.");
            idLabel.text = id;
            roleLabel.text = role;
        }

        public float BundleWidthToward(Vector2 direction)
        {
            // Round the edge heading to the nearest of eight compass regions.
            int region = Mathf.RoundToInt(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg / 45f);
            switch ((region + 8) % 8)
            {
                case 0: return Mathf.Max(0f, east);
                case 1: return Mathf.Max(0f, northEast);
                case 2: return Mathf.Max(0f, north);
                case 3: return Mathf.Max(0f, northWest);
                case 4: return Mathf.Max(0f, west);
                case 5: return Mathf.Max(0f, southWest);
                case 6: return Mathf.Max(0f, south);
                default: return Mathf.Max(0f, southEast);
            }
        }

        public void RenderResources(NodeResources resources)
        {
            if (loadBar != null && resources != null)
                loadBar.Render(resources);
        }
    }
}
