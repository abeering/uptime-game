using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    public sealed class NodeView : MonoBehaviour
    {
        [SerializeField] private string nodeId;
        [SerializeField] private TMP_Text resourceReadout;
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
            if (resourceReadout == null || resources == null)
                return;

            string value = $"{Percent(resources.CpuUsageUnits, resources.CpuCapacityUnits)}%\n{Percent(resources.MemoryUsageGB, resources.MemoryCapacityGB)}%\n{Percent(resources.DiskIoUsageMBps, resources.DiskIoCapacityMBps)}%";
            if (resourceReadout.text != value)
                resourceReadout.text = value;
        }

        private static int Percent(float usage, float capacity)
        {
            if (capacity <= 0f)
                return 0;

            return Mathf.FloorToInt(Mathf.Clamp01(usage / capacity) * 100f + 0.5f);
        }
    }
}
