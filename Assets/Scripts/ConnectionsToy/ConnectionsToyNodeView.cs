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

            string value = $"USED / CAP\nCPU   {resources.CpuUsageUnits:0.#}/{resources.CpuCapacityUnits:0.#} u\nMEM   {resources.MemoryUsageGB:0.#}/{resources.MemoryCapacityGB:0.#} GB\nDISK  {resources.DiskIoUsageMBps:0.#}/{resources.DiskIoCapacityMBps:0.#} MB/s";
            if (resourceReadout.text != value)
                resourceReadout.text = value;
        }
    }
}
