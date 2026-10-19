using TMPro;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    public sealed class ConnectionsToyNodeView : MonoBehaviour
    {
        [SerializeField] private string nodeId;
        [SerializeField] private TMP_Text resourceReadout;

        public string NodeId => nodeId;

        public void RenderResources(ConnectionsToyNodeResources resources)
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
