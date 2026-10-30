using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    // Authored circular marker. Its label travels with the Payload rather than
    // occupying a separate connection identity pill.
    public sealed class PayloadBodyView : MonoBehaviour
    {
        [SerializeField] private TerminalMarkerStyle marker;
        [SerializeField] private TextMeshProUGUI idText;

        public RectTransform rectTransform => (RectTransform)transform;
        public TerminalMarkerStyle Marker => marker;
        public TextMeshProUGUI IdText => idText;

        public void Bind(string id, Color accent)
        {
            marker.SetAccent(accent);
            if (marker.InnerDot != null) marker.InnerDot.gameObject.SetActive(false);
            idText.text = id;
            idText.color = accent;
        }
    }
}
