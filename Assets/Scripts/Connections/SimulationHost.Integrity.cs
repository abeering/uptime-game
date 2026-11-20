using System;
using UnityEngine;

namespace Uptime.Connections
{
    public sealed partial class SimulationHost
    {
        [Header("Integrity experiment (opt-in)")]
        [SerializeField] private IntegrityTuning integrityTuning = new IntegrityTuning();
        private IntegrityState integrity;
        private bool steppingIntegrity;
        public IntegrityState Integrity => integrity;
        public bool RunEnded => integrity?.Breach != null;
        public bool IntegrityEnabled => integrityTuning != null && integrityTuning.enabled;
        public event Action<IntegrityEvent> IntegrityOccurred;
        public event Action CoreBreached;

        private void ResetIntegrity()
        {
            integrity = IntegrityEnabled ? new IntegrityState(nodes, physicalLinks,
                n => trafficDirector != null && !trafficDirector.IsInternalNode(n.View), integrityTuning) : null;
            if (integrity != null) integrity.Occurred += entry =>
            {
                recorder?.RecordIntegrity(entry);
                IntegrityOccurred?.Invoke(entry);
            };
        }

        public bool DamageIntegrity(NodeResources target, string source, string cause,
            bool overt = true, bool completeBoundary = true)
        {
            bool changed = integrity?.Damage(target, source, cause, simulationTimeSeconds, overt) == true;
            if (completeBoundary && !steppingIntegrity) CompleteIntegrityBoundary();
            return changed;
        }

        public void CompleteIntegrityBoundary()
        {
            if (integrity?.LatchBreach(simulationTimeSeconds) != true) return;
            recorder?.RecordIntegrity(new IntegrityEvent { type = "core_breached", time = simulationTimeSeconds,
                target = integrity.Breach.criticalNode, source = integrity.Breach.trigger.source,
                cause = integrity.Breach.trigger.cause, visible = true, revealed = true });
            // Preserve frozen sessions/entities for telemetry. All mutating entry points reject the terminal state.
            recorder?.CaptureSnapshot(simulationTimeSeconds);
            recorder?.Save(false);
            CoreBreached?.Invoke();
        }

        public string IntegrityReceipt(NodeResources target)
        {
            IntegrityNode node = integrity?.For(target);
            return node == null || node.External ? string.Empty :
                "  /  INTEGRITY " + node.Value + "/2" + (node.Compromised ? "  COMPROMISED" : string.Empty);
        }
    }
}
