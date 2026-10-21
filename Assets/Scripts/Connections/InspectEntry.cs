using TMPro;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public sealed class InspectEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text identity;
        [SerializeField] private TMP_Text route;
        [SerializeField] private TMP_Text work;
        [SerializeField] private TMP_Text throughput;
        [SerializeField] private TMP_Text bottleneck;
        [SerializeField] private TMP_Text eta;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image stateTag;
        [SerializeField] private Image panelBackground;
        [SerializeField] private Image titleBar;
        [SerializeField] private Color activeColor = new Color(0.35f, 0.92f, 0.72f);
        [SerializeField] private Color limitedColor = new Color(1f, 0.69f, 0.34f);
        [SerializeField] private Color stalledColor = new Color(0.96f, 0.29f, 0.26f);
        [SerializeField] private Color quietColor = new Color(0.52f, 0.67f, 0.66f);
        [SerializeField] private Color activeBackground = new Color(0.035f, 0.105f, 0.13f);
        [SerializeField] private Color completeBackground = new Color(0.026f, 0.078f, 0.095f);
        [SerializeField] private Color activeTitleBar = new Color(0.055f, 0.16f, 0.19f);
        [SerializeField] private Color completeTitleBar = new Color(0.04f, 0.11f, 0.13f);

        public void SetHeight(float height)
        {
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (stateTag != null)
                stateTag.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        public void Render(LogicalConnectionState state)
        {
            string claim = string.IsNullOrWhiteSpace(state.Definition.ClaimedPurpose) ?
                string.Empty : " - " + state.Definition.ClaimedPurpose;
            Set(identity, "INSPECT / " + state.ConnectionId.ToUpperInvariant() + claim);
            if (identity != null) identity.color = state.IsComplete ? quietColor : activeColor;
            if (panelBackground != null)
                panelBackground.color = state.IsComplete ? completeBackground : activeBackground;
            if (titleBar != null)
                titleBar.color = state.IsComplete ? completeTitleBar : activeTitleBar;
            Color tint = state.IsComplete || state.IsPending || state.IsTransitioning ? quietColor :
                state.Performance == LoadPerformance.AtSpeed ? activeColor :
                state.Performance == LoadPerformance.Slowed ? limitedColor : stalledColor;
            Set(route, Timeline(state, tint));
            NodeResources target = state.TargetResources;
            string status = state.IsComplete ? "COMPLETE" : state.IsPending ? "PENDING" :
                state.IsTransitioning ? "NEXT OPERATION" : state.Performance == LoadPerformance.AtSpeed ?
                "AT SPEED" : state.Performance == LoadPerformance.Slowed ? "SLOWED" : "STALLED";
            string reason = string.Empty;
            if (state.IsDemanding && state.Performance != LoadPerformance.AtSpeed)
                reason = target.LoadCapacity - target.BaselineLoad <= 0.0001f ?
                    " · NO LOAD AVAILABLE" : target.RequestedLoad > target.LoadCapacity + 0.0001f ?
                    " · " + state.TargetNode.NodeId + " OVERLOADED" : " · TARGET LIMITED";
            Set(throughput, state.IsComplete ? "COMPLETE" :
                $"{status} · LOAD {state.AllocatedLoad:0.#}/{state.DesiredLoad:0.#}{reason}");
            if (throughput != null) throughput.color = tint;
            float fraction = state.CurrentTotalWork > 0f ? state.CompletedWork / state.CurrentTotalWork : 0f;
            Set(work, $"WORK {fraction * 100f:0}%   {state.CompletedWork:0.#}/{state.CurrentTotalWork:0.#}");
            Set(bottleneck, state.ActivityDescription);
            string remaining = state.IsComplete ? "00:00" : state.IsPending ? "--:--" :
                state.ActualWorkRate <= 0f ? "--:--" : FormatSeconds(state.RemainingWork / state.ActualWorkRate);
            Set(eta, "ETA  " + remaining);
            if (work != null) work.gameObject.SetActive(!state.IsComplete);
            if (bottleneck != null) bottleneck.gameObject.SetActive(!state.IsComplete);
            if (eta != null) eta.gameObject.SetActive(!state.IsComplete);
            if (progressFill != null)
                progressFill.transform.parent.gameObject.SetActive(!state.IsComplete);

            if (progressFill != null)
            {
                Vector2 anchor = progressFill.rectTransform.anchorMax;
                if (Mathf.Abs(anchor.x - fraction) > 0.0005f)
                    progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), anchor.y);
            }
            if (progressFill != null && progressFill.color != tint) progressFill.color = tint;
            if (stateTag != null && stateTag.color != tint) stateTag.color = tint;
        }

        private static string Timeline(LogicalConnectionState state, Color activeColor)
        {
            var line = new StringBuilder();
            string target = state.TargetNode.NodeId;
            for (int i = 0; i < state.OperationCount; i++)
            {
                if (i > state.CurrentOperationIndex && !state.IsComplete)
                {
                    line.Append("  +").Append(state.OperationCount - i);
                    break;
                }
                if (line.Length > 0) line.Append("  ");
                OperationKind operation = state.Definition.OperationAt(i);
                string arrow = operation == OperationKind.Read ? "←" :
                    operation == OperationKind.Write ? "→" : "·";
                string pill = " " + operation.ToString().ToUpperInvariant() +
                    " " + arrow + " " + target + " ";
                if (i < state.CurrentOperationIndex || state.IsComplete)
                    line.Append("<mark=#4B5D5D44><color=#839696>")
                        .Append(pill).Append("</color></mark>");
                else
                {
                    string color = ColorUtility.ToHtmlStringRGB(activeColor);
                    line.Append("<mark=#").Append(color).Append("66><color=#")
                        .Append(color).Append('>').Append(pill).Append("</color></mark>");
                }
            }
            return line.ToString();
        }

        private static string FormatSeconds(float seconds)
        {
            int whole = Mathf.CeilToInt(seconds);
            return (whole / 60).ToString("00") + ":" + (whole % 60).ToString("00");
        }

        private static void Set(TMP_Text label, string value)
        {
            if (label != null && label.text != value)
                label.text = value;
        }
    }
}
