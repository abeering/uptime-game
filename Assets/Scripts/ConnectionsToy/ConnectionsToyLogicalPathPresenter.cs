using UnityEngine;

namespace Uptime.ConnectionsToy
{
    public sealed class ConnectionsToyLogicalPathPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform pathLayer;
        [SerializeField] private ConnectionsToyLogicalPathView pathPrefab;
        [SerializeField, Min(2f)] private float laneSpacing = 18f;

        public void Bind(ConnectionsToyLogicalConnectionState[] connections)
        {
            if (networkArea == null || pathLayer == null || pathPrefab == null)
                return;
            for (int i = pathLayer.childCount - 1; i >= 0; i--)
                Destroy(pathLayer.GetChild(i).gameObject);

            foreach (ConnectionsToyLogicalConnectionState connection in connections)
            {
                float[] offsets = new float[connection.RouteLinks.Length];
                for (int edge = 0; edge < offsets.Length; edge++)
                {
                    int index = 0, count = 0;
                    foreach (ConnectionsToyLogicalConnectionState peer in connections)
                    {
                        if (!peer.Uses(connection.RouteLinks[edge]))
                            continue;
                        if (peer == connection)
                            index = count;
                        count++;
                    }
                    offsets[edge] = (index - (count - 1) * 0.5f) * laneSpacing;
                }
                ConnectionsToyLogicalPathView view = Instantiate(pathPrefab, pathLayer, false);
                view.name = "Logical path " + connection.ConnectionId;
                view.Bind(connection, networkArea, offsets);
            }
        }
    }
}
