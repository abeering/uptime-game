using System;
using UnityEngine;

namespace Uptime.Connections.Display
{
    public enum DisplayTrafficClass { Ordinary, Important }
    public enum DisplayResolution { Active, Completed, Killed, TimedOut, Canceled, Withdrawn }
    public enum PillProgressTreatment { Interior, Perimeter }

    // Presentation inputs only. Synthetic fixtures and gameplay adapters supply the same value.
    // No severity thresholds or importance presentation feed back into simulation.
    [Serializable]
    public struct ConnectionDisplayState
    {
        public string identity;
        public DisplayTrafficClass trafficClass;
        [Min(0)] public int identitySlot;
        public OperationKind operation;
        [Min(0)] public float requestedLoad;
        [Range(0, 2)] public float productiveSpeed;
        [Range(0, 1)] public float workProgress;
        [Range(0, 1)] public float dangerSeverity;
        public bool exceedsNormalBudget;
        // Zero keeps authored lab flags fully colored; gameplay supplies a positive soft blend.
        [Range(0, 1)] public float normalBudgetWarningMix;
        public DisplayResolution resolution;
        [Min(0)] public float resolutionAge;
    }

}
