using System;
using UnityEngine;

namespace Uptime.Connections.Display
{
    public enum DisplayTrafficClass { Ordinary, Priority }
    public enum DisplayResolution { Active, Completed, Killed, TimedOut }
    public enum PillProgressTreatment { Interior, Perimeter }

    // Presentation inputs only. A future adapter may copy gameplay facts into this value.
    // No severity thresholds or priority decisions feed back into simulation.
    [Serializable]
    public struct ConnectionDisplayState
    {
        public string identity;
        public DisplayTrafficClass trafficClass;
        public OperationKind operation;
        [Min(0)] public float requestedLoad;
        [Range(0, 2)] public float productiveSpeed;
        [Range(0, 1)] public float workProgress;
        [Range(0, 1)] public float dangerSeverity;
        public bool exceedsNormalBudget;
        public DisplayResolution resolution;
        [Min(0)] public float resolutionAge;
    }

}
