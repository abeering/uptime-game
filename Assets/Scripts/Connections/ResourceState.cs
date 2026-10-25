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
        [NonSerialized] private float requestedByInfections;
        [NonSerialized] private float allocatedToInfections;

        public NodeView View => view;
        public float LoadCapacity => Mathf.Max(0f, loadCapacity);
        public float BaselineLoad => Mathf.Clamp(baselineLoad, 0f, LoadCapacity);
        public float ConnectionRequestedLoad => requestedByConnections;
        public float ConnectionAllocatedLoad => allocatedToConnections;
        public float InfectionRequestedLoad => requestedByInfections;
        public float InfectionAllocatedLoad => allocatedToInfections;
        public float RequestedLoad => BaselineLoad + requestedByInfections + requestedByConnections;
        public float AllocatedLoad => BaselineLoad + allocatedToInfections + allocatedToConnections;

        public NodeResources(NodeView node, float capacity, float baseline = 0f)
        {
            view = node;
            loadCapacity = capacity;
            baselineLoad = baseline;
            ClampToCapacity();
        }

        internal void ClearConnectionUsage()
        {
            requestedByConnections = 0f;
            allocatedToConnections = 0f;
        }

        internal void SetInfectionUsage(float requested, float allocated)
        {
            requestedByInfections = requested;
            allocatedToInfections = allocated;
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
        public PhysicalLinkReference(PhysicalLinkView link) { view = link; }
    }
}
