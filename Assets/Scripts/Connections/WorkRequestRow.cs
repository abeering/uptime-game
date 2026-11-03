using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Authored two-line row in the live node WORK panel.
    public sealed class WorkRequestRow : MonoBehaviour
    {
        [SerializeField] private IdentityPillView identityPill;
        [SerializeField] private TMP_Text purpose;
        [SerializeField] private RectTransform operationRow;
        [SerializeField] private InspectOperationToken operationTokenPrefab;
        [SerializeField] private RectTransform progressTrack;
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text eta;
        [SerializeField] private Color quietColor = new(0.52f, 0.67f, 0.66f);
        [SerializeField] private Color atSpeedColor = new(0.56f, 0.89f, 0.40f);
        [SerializeField] private Color limitedColor = new(1f, 0.69f, 0.34f);
        [SerializeField] private Color stalledColor = new(0.96f, 0.29f, 0.26f);

        private readonly List<InspectOperationToken> tokens = new();
        private LogicalConnectionState renderedState;
        private InspectOperationToken activeToken;
        private Color identityAccent;
        private int renderedOperation = -1;
        private float renderedWidth = -1f;

        public string DisplayedId => identityPill != null ? identityPill.DisplayedId : string.Empty;
        public string DisplayedEta => eta != null ? eta.text : string.Empty;
        public float FillFraction => progressFill != null ? progressFill.rectTransform.anchorMax.x : 0f;

        public void Bind(string id, Color accent)
        {
            identityAccent = accent;
            identityPill?.Bind(id, accent);
        }

        public void SetDisplayedId(string id) => identityPill?.SetDisplayedId(id);

        public void FitWidth(float width)
        {
            RectTransform root = (RectTransform)transform;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            float available = Mathf.Max(0f, width - 76f);
            float purposeWidth = Mathf.Min(150f, Mathf.Max(48f, available - 160f));
            purposeWidth = Mathf.Min(purposeWidth, available);
            if (purpose != null)
                purpose.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    purposeWidth);
            if (operationRow != null)
            {
                operationRow.anchoredPosition = new Vector2(0f,
                    operationRow.anchoredPosition.y);
                operationRow.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    Mathf.Max(0f, available - purposeWidth));
            }
            if (progressTrack != null)
                progressTrack.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    Mathf.Max(0f, width - 98f));
        }

        public void Render(LogicalConnectionState state)
        {
            if (purpose != null)
            {
                string label = state.Definition.ClaimedPurpose ?? string.Empty;
                if (purpose.text != label) purpose.text = label;
                purpose.color = identityAccent;
            }
            Color tint = !state.IsDemanding ? quietColor :
                state.Performance == LoadPerformance.AtSpeed ? atSpeedColor :
                state.Performance == LoadPerformance.Slowed ? limitedColor : stalledColor;
            float width = operationRow != null ? operationRow.rect.width : 0f;
            if (!ReferenceEquals(renderedState, state) ||
                renderedOperation != state.CurrentOperationIndex ||
                Mathf.Abs(renderedWidth - width) > 0.5f)
            {
                activeToken = InspectEntry.RenderOperationTokens(state, tint, quietColor,
                    operationRow, operationTokenPrefab, tokens);
                renderedState = state;
                renderedOperation = state.CurrentOperationIndex;
                renderedWidth = width;
            }
            else activeToken?.SetActiveColor(tint);
            if (eta != null)
            {
                string value = InspectEntry.CurrentOperationEta(state);
                if (eta.text != value) eta.text = value;
            }
            if (progressFill != null)
            {
                float fraction = InspectEntry.CurrentWorkFraction(state);
                RectTransform fill = progressFill.rectTransform;
                if (Mathf.Abs(fill.anchorMax.x - fraction) > 0.0005f)
                    fill.anchorMax = new Vector2(fraction, fill.anchorMax.y);
                if (progressFill.color != identityAccent) progressFill.color = identityAccent;
            }
        }
    }
}
