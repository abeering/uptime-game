using TMPro;
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
        [SerializeField] private Color quietColor = new Color(0.52f, 0.67f, 0.66f);

        public void Render(LogicalConnectionState state)
        {
            string lifecycle = state.IsComplete ? "COMPLETE" : state.IsPending ? "PENDING" : "ACTIVE";
            string claim = string.IsNullOrWhiteSpace(state.Definition.ClaimedPurpose) ? string.Empty :
                "  /  " + state.Definition.ClaimedPurpose;
            Set(identity, state.ConnectionId.ToUpperInvariant() + "  /  " +
                state.Definition.CurrentOperation.ToString().ToUpperInvariant() + claim + "  /  " + lifecycle);
            string routeText = string.Empty;
            foreach (NodeRequirement requirement in state.Definition.Route)
                routeText += (routeText.Length == 0 ? string.Empty : "  >  ") + requirement.Node.NodeId;
            Set(route, string.IsNullOrWhiteSpace(state.Definition.ActivityDescription) ? routeText :
                state.Definition.ActivityDescription + "  •  " + routeText);

            float fraction = state.Definition.TotalWork > 0f ? state.CompletedWork / state.Definition.TotalWork : 0f;
            Set(work, $"WORK  {state.CompletedWork:0.#} / {state.Definition.TotalWork:0.#} work units   {fraction * 100f:0}%");
            Set(throughput, $"RATE  {state.ActualWorkRate:0.#} / {state.Definition.DesiredWorkRate:0.#} work units/s  BW {state.ActualBandwidthMBps:0.#} / {state.DesiredBandwidthMBps:0.#} MB/s  SAT {state.Satisfaction * 100f:0}%");

            string limit = state.IsPending ? "Waiting to establish" : state.IsComplete ? "Released" :
                state.LimitKind == LimitKind.None ? "None" :
                state.LimitingResourceId + " " + LimitName(state.LimitKind);
            Set(bottleneck, "LIMIT  " + limit);
            string remaining = state.IsComplete ? "00:00" : state.IsPending ? "--:--" :
                state.ActualWorkRate <= 0f ? "--:--" : FormatSeconds(state.RemainingWork / state.ActualWorkRate);
            Set(eta, "ETA  " + remaining);

            if (progressFill != null)
            {
                Vector2 anchor = progressFill.rectTransform.anchorMax;
                if (Mathf.Abs(anchor.x - fraction) > 0.0005f)
                    progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), anchor.y);
            }
            Color tint = state.IsComplete || state.IsPending ? quietColor : state.Satisfaction >= 0.98f ? activeColor : limitedColor;
            if (progressFill != null && progressFill.color != tint) progressFill.color = tint;
            if (stateTag != null && stateTag.color != tint) stateTag.color = tint;
        }

        private static string LimitName(LimitKind kind)
        {
            switch (kind)
            {
                case LimitKind.PhysicalLinkBandwidth: return "bandwidth";
                case LimitKind.NodeCpu: return "CPU";
                case LimitKind.NodeDiskIo: return "disk I/O";
                case LimitKind.NodeMemory: return "memory";
                default: return "none";
            }
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
