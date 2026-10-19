using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // Owns the toy's resource state. Views only render these values; no load is simulated yet.
    [ExecuteAlways]
    public sealed class ConnectionsToySimulationHost : MonoBehaviour
    {
        [SerializeField] private ConnectionsToyNodeResources[] nodes;
        [SerializeField] private ConnectionsToyPhysicalLinkResources[] physicalLinks;

        public ConnectionsToyNodeResources[] Nodes => nodes;
        public ConnectionsToyPhysicalLinkResources[] PhysicalLinks => physicalLinks;

        private void OnEnable() => RefreshDisplays();

        private void OnValidate()
        {
            if (nodes != null)
            {
                foreach (ConnectionsToyNodeResources node in nodes)
                    node?.ClampToCapacity();
            }

            if (physicalLinks != null)
            {
                foreach (ConnectionsToyPhysicalLinkResources link in physicalLinks)
                    link?.ClampToCapacity();
            }
        }

        private void Update() => RefreshDisplays();

        public void RefreshDisplays()
        {
            if (nodes != null)
            {
                foreach (ConnectionsToyNodeResources node in nodes)
                {
                    if (node?.View != null)
                        node.View.RenderResources(node);
                }
            }

            if (physicalLinks != null)
            {
                foreach (ConnectionsToyPhysicalLinkResources link in physicalLinks)
                {
                    if (link?.View != null)
                        link.View.RenderResources(link);
                }
            }
        }
    }
}
