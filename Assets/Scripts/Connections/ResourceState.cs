using System;
using UnityEngine;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class NodeResources
    {
        [SerializeField] private NodeView view;
        [SerializeField, Min(0f)] private float loadCapacity = 10f;
        [SerializeField, Min(0f)] private float baselineLoad;

        [NonSerialized] private float requestedByConnections;
        [NonSerialized] private float allocatedToConnections;

        public NodeView View => view;
        public float LoadCapacity => Mathf.Max(0f, loadCapacity);
        public float BaselineLoad => Mathf.Clamp(baselineLoad, 0f, LoadCapacity);
        public float RequestedLoad => BaselineLoad + requestedByConnections;
        public float AllocatedLoad => BaselineLoad + allocatedToConnections;

        internal void ClearUsage()
        {
            requestedByConnections = 0f;
            allocatedToConnections = 0f;
        }

        internal void AddUsage(float requested, float allocated)
        {
            requestedByConnections += requested;
            allocatedToConnections += allocated;
        }

        internal void ClampToCapacity()
        {
            loadCapacity = LoadCapacity;
            baselineLoad = BaselineLoad;
        }
    }

    // Physical links still define routes and visual lanes, but carry no LOAD cost.
    [Serializable]
    public sealed class PhysicalLinkReference
    {
        [SerializeField] private PhysicalLinkView view;
        public PhysicalLinkView View => view;
    }
}
