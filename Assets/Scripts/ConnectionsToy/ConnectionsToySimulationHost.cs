using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // Phase 0 seam: authored topology only. Simulation steps and resource state come later.
    public sealed class ConnectionsToySimulationHost : MonoBehaviour
    {
        [SerializeField] private ConnectionsToyNodeView[] nodes;
        [SerializeField] private ConnectionsToyPhysicalLinkView[] physicalLinks;

        public ConnectionsToyNodeView[] Nodes => nodes;
        public ConnectionsToyPhysicalLinkView[] PhysicalLinks => physicalLinks;
    }
}
