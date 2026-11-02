using System;
using UnityEngine;

namespace Uptime.Connections
{
    public enum TextCommunicationType { InstantMessage, SystemNotification }

    // Delivery content only. Future gameplay/narrative callers own identity and meaning.
    [Serializable]
    public sealed class TextCommunication
    {
        public TextCommunicationType Type;
        public string Sender;
        [TextArea(2, 8)] public string Body;
        public Sprite Avatar;
        public string Timestamp;
        public float? LifetimeSeconds;
    }
}
