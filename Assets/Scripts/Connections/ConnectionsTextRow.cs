using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    public sealed class ConnectionsTextRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        public void Bind(string message, Color color)
        {
            if (text == null)
                return;

            text.text = message;
            text.color = color;
        }
    }
}
