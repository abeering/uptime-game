using System;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    [Serializable]
    public sealed class ConnectionsToyNodeResources
    {
        [SerializeField] private ConnectionsToyNodeView view;
        [SerializeField, Min(0f), Tooltip("Capacity in abstract CPU units.")] private float cpuCapacityUnits = 100f;
        [SerializeField, Min(0f), Tooltip("Idle CPU usage in the same units as capacity.")] private float cpuBaselineUnits;
        [SerializeField, Min(0f)] private float memoryCapacityGB = 8f;
        [SerializeField, Min(0f)] private float memoryBaselineGB;
        [SerializeField, Min(0f)] private float diskIoCapacityMBps = 80f;
        [SerializeField, Min(0f)] private float diskIoBaselineMBps;

        public ConnectionsToyNodeView View => view;
        public float CpuCapacityUnits => Mathf.Max(0f, cpuCapacityUnits);
        public float CpuBaselineUnits => Mathf.Clamp(cpuBaselineUnits, 0f, CpuCapacityUnits);
        public float CpuUsageUnits => CpuBaselineUnits;
        public float MemoryCapacityGB => Mathf.Max(0f, memoryCapacityGB);
        public float MemoryBaselineGB => Mathf.Clamp(memoryBaselineGB, 0f, MemoryCapacityGB);
        public float MemoryUsageGB => MemoryBaselineGB;
        public float DiskIoCapacityMBps => Mathf.Max(0f, diskIoCapacityMBps);
        public float DiskIoBaselineMBps => Mathf.Clamp(diskIoBaselineMBps, 0f, DiskIoCapacityMBps);
        public float DiskIoUsageMBps => DiskIoBaselineMBps;

        internal void ClampToCapacity()
        {
            cpuCapacityUnits = CpuCapacityUnits;
            cpuBaselineUnits = CpuBaselineUnits;
            memoryCapacityGB = MemoryCapacityGB;
            memoryBaselineGB = MemoryBaselineGB;
            diskIoCapacityMBps = DiskIoCapacityMBps;
            diskIoBaselineMBps = DiskIoBaselineMBps;
        }
    }

    [Serializable]
    public sealed class ConnectionsToyPhysicalLinkResources
    {
        [SerializeField] private ConnectionsToyPhysicalLinkView view;
        [SerializeField, Min(0f), Tooltip("Physical link capacity in megabytes per second.")] private float bandwidthCapacityMBps = 50f;

        public ConnectionsToyPhysicalLinkView View => view;
        public float BandwidthCapacityMBps => Mathf.Max(0f, bandwidthCapacityMBps);
        public float BandwidthUsageMBps => 0f;

        internal void ClampToCapacity() => bandwidthCapacityMBps = BandwidthCapacityMBps;
    }
}
