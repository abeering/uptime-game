using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public sealed partial class SimulationHost
    {
        [Header("API configuration: one PURPOSE priority slot")]
        [SerializeField] private NodeView apiConfigurationNode;
        [SerializeField, Min(1f)] private float apiAllocationWeight = 2f;
        private string apiFavoredPurposeId;
        public NodeView ApiConfigurationNode => apiConfigurationNode;
        public float ApiAllocationWeight => apiAllocationWeight;
        public string ApiFavoredPurposeId => apiFavoredPurposeId ?? string.Empty;
        public event Action ApiPolicyChanged;

        public IReadOnlyList<ConnectionTrafficProfile> ApiPurposeChoices
        {
            get
            {
                var choices = new List<ConnectionTrafficProfile>();
                var traffic = GetComponent<ConnectionTrafficDirector>();
                if (traffic != null) foreach (var profile in traffic.Profiles)
                    if (profile != null && profile.purposeTemplate) choices.Add(profile);
                choices.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
                return choices;
            }
        }

        public bool TrySetApiPurpose(string purposeId, out string result)
        {
            if (RunEnded) { result = RunEndedMessage; return false; }
            if (apiConfigurationNode == null) { result = "API CONFIGURATION UNAVAILABLE"; return false; }
            string selected = null;
            if (!string.IsNullOrEmpty(purposeId))
            {
                foreach (var choice in ApiPurposeChoices)
                    if (string.Equals(choice.id, purposeId, StringComparison.OrdinalIgnoreCase)) { selected = choice.id; break; }
                if (selected == null) { result = "UNKNOWN PURPOSE " + purposeId; return false; }
            }
            string previous = apiFavoredPurposeId;
            apiFavoredPurposeId = selected;
            result = selected == null ? "API PRIORITY CLEARED" : "API FAVORS " + selected + $"  ×{apiAllocationWeight:G3}";
            if (!string.Equals(previous, selected, StringComparison.Ordinal))
            {
                recorder?.RecordApiPolicyChange(apiConfigurationNode.NodeId, previous, selected,
                    apiAllocationWeight, simulationTimeSeconds);
                ApiPolicyChanged?.Invoke();
            }
            // Authoritative allocation changes on the next ordinary allocation update.
            return true;
        }

        private void ResetApiConfiguration()
        {
            if (!float.IsFinite(apiAllocationWeight) || apiAllocationWeight < 1f)
                throw new InvalidOperationException("API allocation weight must be finite and at least1.");
            apiFavoredPurposeId = null;
        }

        private void RefreshApiAllocationWeights()
        {
            if (runtimeConnections == null) return;
            foreach (var c in runtimeConnections)
            {
                if (c.Resolution.HasValue) continue; // Preserve historical applied weight on resolved jobs.
                bool matches = apiConfigurationNode != null && !string.IsNullOrEmpty(apiFavoredPurposeId) &&
                    string.Equals(c.Definition.ProfileId, apiFavoredPurposeId, StringComparison.OrdinalIgnoreCase);
                bool traverses = false;
                if (matches) foreach (var stop in c.Route)
                    if (stop.Node == apiConfigurationNode) { traverses = true; break; }
                c.AllocationWeight = traverses ? apiAllocationWeight : 1f;
                c.AllocationPolicyNodeId = traverses ? apiConfigurationNode.NodeId : null;
            }
        }
    }
}
