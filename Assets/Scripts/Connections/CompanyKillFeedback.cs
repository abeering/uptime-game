using System;
using UnityEngine;

namespace Uptime.Connections
{
    // Resolves only player-caused legitimate kills into the existing IM surface.
    public sealed class CompanyKillFeedback : MonoBehaviour
    {
        [SerializeField] private SimulationHost simulationHost;
        [SerializeField] private TextCommunicationDirector textCommunications;
        [SerializeField] private CompanyDefinition company;
        [SerializeField, Min(0f)] private float reactionDelaySeconds = 1f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 12f;
        [SerializeField] private int responseSeed = 4109;

        private System.Random responseRandom;
        private bool pending;
        private bool pendingOrderSubmit;
        private float dueAt;
        private float nextAllowedAt;

        public CompanyDefinition Company => company;
        public bool HasPendingReaction => pending;

        private void OnEnable()
        {
            ResetFeedback();
            if (simulationHost == null) return;
            simulationHost.ConnectionResolved += OnConnectionResolved;
            simulationHost.SimulationReset += ResetFeedback;
        }

        private void OnDisable()
        {
            if (simulationHost != null)
            {
                simulationHost.ConnectionResolved -= OnConnectionResolved;
                simulationHost.SimulationReset -= ResetFeedback;
            }
            ResetFeedback();
        }

        private void ResetFeedback()
        {
            pending = false;
            pendingOrderSubmit = false;
            dueAt = 0f;
            nextAllowedAt = 0f;
            responseRandom = new System.Random(responseSeed);
        }

        private void OnConnectionResolved(LogicalConnectionState state, ConnectionResolution resolution)
        {
            if (state == null || state.IsHostile ||
                resolution != ConnectionResolution.Killed ||
                state.TerminationCause != ConnectionTerminationCause.Player ||
                company == null || textCommunications == null || simulationHost == null)
                return;

            float now = simulationHost.SimulationTimeSeconds;
            if (pending || now < nextAllowedAt) return;
            pendingOrderSubmit = string.Equals(state.Definition.ProfileId, "order-submit",
                StringComparison.Ordinal);
            dueAt = now + Mathf.Max(0f, reactionDelaySeconds);
            pending = true;
        }

        private void FixedUpdate()
        {
            if (Application.isPlaying && simulationHost != null)
                DeliverDue(simulationHost.SimulationTimeSeconds);
        }

        // Explicit simulation-time seam for deterministic Editor witnesses.
        public void DeliverDue(float simulationSeconds)
        {
            if (!pending || simulationSeconds < dueAt || company == null ||
                textCommunications == null) return;
            CompanySpeaker speaker = pendingOrderSubmit ? company.CustomerSupport : company.Operations;
            string[] responses = pendingOrderSubmit ? company.OrderSubmitKillResponses :
                company.GenericBenignKillResponses;
            if (speaker == null || responses == null || responses.Length == 0) return;

            // The response RNG is never shared with traffic, identity or simulation.
            string body = responses[responseRandom.Next(responses.Length)];
            if (!textCommunications.PushMessage(new TextCommunication {
                Type = TextCommunicationType.InstantMessage,
                Sender = speaker.displayName + " / " + speaker.role,
                Body = body
            })) return;

            pending = false;
            nextAllowedAt = simulationSeconds + Mathf.Max(0f, cooldownSeconds);
        }
    }
}
