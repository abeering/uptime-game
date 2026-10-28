using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // A terminal history card; the host owns timing and the one active scan slot.
    public sealed class ScanOutput : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private LayoutElement layout;
        [SerializeField] private PillGraphic nodePill;
        [SerializeField] private TMP_Text nodeIdText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image accentRail;
        [SerializeField, Min(0.05f)] private float refreshSeconds = 0.1f;
        [SerializeField] private Color scanningColor = new Color(0.35f, 0.92f, 0.72f);
        [SerializeField] private Color cleanColor = new Color(0.56f, 0.89f, 0.40f);
        [SerializeField] private Color infectedColor = new Color(1f, 0.63f, 0.39f);

        private NodeScanSession session;
        private SimulationHost host;
        private float nextRefresh;

        public void Bind(NodeScanSession scan, SimulationHost owner)
        {
            session = scan;
            host = owner;
            if (nodeIdText != null) nodeIdText.text = scan.NodeId;
            Refresh();
            nextRefresh = Time.unscaledTime + refreshSeconds;
        }

        private void Update()
        {
            if (session == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshSeconds;
            Refresh();
        }

        private void Refresh()
        {
            if (session == null) return;
            RectTransform root = (RectTransform)transform;
            RectTransform parent = transform.parent as RectTransform;
            float width = parent != null && parent.rect.width > 0f ?
                Mathf.Min(580f, parent.rect.width) : root.rect.width;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (card != null) card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Max(0f, width - 28f));
            if (layout != null) layout.preferredHeight = root.rect.height;

            bool complete = session.IsComplete;
            bool reset = session.WasReset;
            Color color = complete ? session.Result == NodeScanResult.Infected ?
                infectedColor : cleanColor : reset ? infectedColor : scanningColor;
            if (nodePill != null) nodePill.SetOutline(color);
            if (nodeIdText != null) nodeIdText.color = color;
            if (statusText != null)
            {
                statusText.color = color;
                statusText.text = complete ? session.Result == NodeScanResult.Infected ?
                    "INFECTED" : "CLEAN" : reset ? "RESET" : "SCANNING";
            }
            if (progressFill != null)
            {
                progressFill.color = color;
                float progress = session.ProgressAt(host != null ?
                    host.SimulationTimeSeconds : session.StartedAtSeconds);
                var track = (RectTransform)progressFill.transform.parent;
                progressFill.rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, track.rect.width * progress);
            }
            if (accentRail != null) accentRail.color = color;
            if (timeText != null)
            {
                float remaining = Mathf.Max(0f, session.ScheduledCompletionAtSeconds -
                    (host != null ? host.SimulationTimeSeconds : session.StartedAtSeconds));
                timeText.text = complete ? "100%  /  RESULT AT " +
                    session.CompletedAtSeconds.ToString("0.0") + "s" : reset ? "SESSION RESET" :
                    Mathf.RoundToInt(session.ProgressAt(host != null ?
                        host.SimulationTimeSeconds : session.StartedAtSeconds) * 100f) +
                    "%  /  " + remaining.ToString("0.0") + "s REMAINING";
            }
            if (complete || reset) enabled = false;
        }
    }
}
