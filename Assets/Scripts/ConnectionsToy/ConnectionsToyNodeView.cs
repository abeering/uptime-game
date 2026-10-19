using UnityEngine;

namespace Uptime.ConnectionsToy
{
    public sealed class ConnectionsToyNodeView : MonoBehaviour
    {
        [SerializeField] private string nodeId;

        public string NodeId => nodeId;
    }
}
