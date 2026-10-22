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
        [SerializeField] private Color activeColor = new Color(0.35f, 0.92f, 0.72f);
        [SerializeField] private Color limitedColor = new Color(1f, 0.69f, 0.34f);
        [SerializeField] private Color stalledColor = new Color(0.96f, 0.29f, 0.26f);
        [SerializeField] private Color quietColor = new Color(0.52f, 0.67f, 0.66f);

        public void Render(LogicalConnectionState state)
        {
            string claim = string.IsNullOrWhiteSpace(state.Definition.ClaimedPurpose) ?
                string.Empty : " — " + state.Definition.ClaimedPurpose;
            Set(identity, state.ConnectionId.ToUpperInvariant() + claim);
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
            Set(throughput, $"{status} · LOAD {state.AllocatedLoad:0.#}/{state.DesiredLoad:0.#}{reason}");
            if (throughput != null) throughput.color = tint;
            float fraction = state.CurrentTotalWork > 0f ? state.CompletedWork / state.CurrentTotalWork : 0f;
            Set(work, $"WORK {fraction * 100f:0}%   {state.CompletedWork:0.#}/{state.CurrentTotalWork:0.#}");
            Set(bottleneck, state.ActivityDescription);
            string remaining = state.IsComplete ? "00:00" : state.IsPending ? "--:--" :
                state.ActualWorkRate <= 0f ? "--:--" : FormatSeconds(state.RemainingWork / state.ActualWorkRate);
            Set(eta, "ETA  " + remaining);

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
                string pill = "[" + operation.ToString().ToUpperInvariant() + " " + arrow + " " + target + "]";
                if (i < state.CurrentOperationIndex || state.IsComplete)
                    line.Append("<mark=#4B5D5D66><color=#839696>")
                        .Append(pill).Append("</color></mark>");
                else
                {
                    string color = ColorUtility.ToHtmlStringRGB(activeColor);
                    line.Append("<mark=#").Append(color).Append("44><color=#")
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
