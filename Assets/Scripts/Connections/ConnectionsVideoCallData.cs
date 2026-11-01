using System;
using UnityEngine;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class ConnectionsVideoCallData
    {
        public string CallerName;
        [TextArea(2, 8)] public string Body;
        public Sprite Portrait;
        public RuntimeAnimatorController PortraitAnimation;
    }
}
