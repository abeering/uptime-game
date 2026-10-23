using UnityEngine;

namespace Uptime.Connections
{
    // Padding around the beam's actual intersection with the target card border.
    public sealed class TerminalMarkerStyle : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float horizontalPadding = 4f;
        [SerializeField, Min(0f)] private float verticalPadding = 4f;

        public Vector2 Padding => new Vector2(Mathf.Max(0f, horizontalPadding),
            Mathf.Max(0f, verticalPadding));
    }
}
