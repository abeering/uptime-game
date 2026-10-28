using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    // Authored terminal-only summary; the inspected state remains owned by the simulation.
    public sealed class ResolvedInspectRow : MonoBehaviour
    {
        [SerializeField] private IdentityPillView identityPill;
        [SerializeField] private TMP_Text connectionName;
        [SerializeField] private InspectOperationToken operation;
        [SerializeField] private TMP_Text resolution;
        [SerializeField, Min(0f)] private float inset = 12f;
        [SerializeField, Min(0f)] private float gap = 10f;

        private LogicalConnectionState state;
        private Color color;

        public void Bind(LogicalConnectionState inspected, Color tint)
        {
            state = inspected;
            color = tint;
            identityPill.Bind(state.ConnectionId, tint);
            connectionName.text = state.Definition.ClaimedPurpose ?? string.Empty;
            connectionName.color = tint;
            resolution.text = state.Resolution == ConnectionResolution.Completed ? "COMPLETE" :
                state.Resolution == ConnectionResolution.Killed ? "KILLED" :
                state.Resolution == ConnectionResolution.Canceled ? "CANCELED" :
                state.Resolution == ConnectionResolution.Withdrawn ? "WITHDRAWN" : "RESOLVED";
            resolution.color = tint;
            FitWidth();
        }

        public void FitWidth()
        {
            if (state == null) return;
            float width = ((RectTransform)transform).rect.width;
            float statusWidth = Mathf.Ceil(resolution.GetPreferredValues(resolution.text).x);
            Place(resolution.rectTransform, width - inset, statusWidth, true);
            Place((RectTransform)identityPill.transform, inset, identityPill.Size.x);

            bool completed = state.Resolution == ConnectionResolution.Completed;
            OperationKind kind = state.Definition.OperationAt(state.CurrentOperationIndex);
            string target = state.TargetNode.NodeId;
            float remaining = Mathf.Max(0f, width - inset * 2f - identityPill.Size.x -
                gap * 3f - statusWidth);
            float fullWidth = operation.Measure(InspectOperationToken.OperationLabel(kind, target), completed);
            bool shortLabel = fullWidth > remaining - 80f;
            operation.ShowOperation(kind, target, completed, color, shortLabel);
            float operationWidth = Mathf.Min(remaining, operation.Measure(operation.Text, completed));
            if (operationWidth < operation.Measure(operation.Text, completed))
                operation.ConstrainWidth(operationWidth);
            Place((RectTransform)operation.transform,
                width - inset - statusWidth - gap, operationWidth, true);
            Place(connectionName.rectTransform, inset + identityPill.Size.x + gap,
                Mathf.Max(0f, remaining - operationWidth));
        }

        private static void Place(RectTransform rect, float x, float width, bool rightAligned = false)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(rightAligned ? 1f : 0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
    }
}
