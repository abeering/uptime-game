using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public sealed class ConnectionsInspectEntry : MonoBehaviour
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

        public void Render(ConnectionsLogicalConnectionState state)
        {
            string lifecycle = state.IsComplete ? "COMPLETE" : state.IsPending ? "PENDING" : "ACTIVE";
            Set(identity, state.ConnectionId.ToUpperInvariant() + "  /  " + lifecycle);
            string routeText = string.Empty;
            foreach (ConnectionsNodeRequirement requirement in state.Definition.Route)
                routeText += (routeText.Length == 0 ? string.Empty : "  >  ") + requirement.Node.NodeId;
            Set(route, routeText);

            float fraction = state.Definition.TotalWorkMB > 0f ? state.CompletedWorkMB / state.Definition.TotalWorkMB : 0f;
            Set(work, $"WORK  {state.CompletedWorkMB:0.#} / {state.Definition.TotalWorkMB:0.#} MB   {fraction * 100f:0}%");
            Set(throughput, $"RATE  {state.ActualRateMBps:0.#} / {state.Definition.DesiredRateMBps:0.#} MB/s   SAT {state.Satisfaction * 100f:0}%");

            string limit = state.IsPending ? "Waiting to establish" : state.IsComplete ? "Released" :
                state.LimitKind == ConnectionsLimitKind.None ? "None" :
                state.LimitingResourceId + " " + LimitName(state.LimitKind);
            Set(bottleneck, "LIMIT  " + limit);
            string remaining = state.IsComplete ? "00:00" : state.IsPending ? "--:--" :
                state.ActualRateMBps <= 0f ? "--:--" : FormatSeconds(state.RemainingWorkMB / state.ActualRateMBps);
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

        private static string LimitName(ConnectionsLimitKind kind)
        {
            switch (kind)
            {
                case ConnectionsLimitKind.PhysicalLinkBandwidth: return "bandwidth";
                case ConnectionsLimitKind.NodeCpu: return "CPU";
                case ConnectionsLimitKind.NodeDiskIo: return "disk I/O";
                case ConnectionsLimitKind.NodeMemory: return "memory";
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
