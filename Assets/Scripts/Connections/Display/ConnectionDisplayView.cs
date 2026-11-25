using TMPro;
using UnityEngine;

namespace Uptime.Connections.Display
{
    // Production-facing binding seam: caller supplies presentation state and an existing path.
    public sealed class ConnectionDisplayView : MonoBehaviour
    {
        public ConnectionDisplayStyle style;
        public ConnectionDisplayGraphic path;
        public ConnectionProgressPillGraphic pill;
        public TMP_Text identity;
        public TerminalMarkerStyle terminalMarker;
        public CanvasGroup displayGroup;
        public PillProgressTreatment progressTreatment;
        [Range(0, 1)] public float pillPosition = .5f;
        [Range(0, 1)] public float phaseBeats;
        [Range(1, 4)] public int subdivision = 1;
        public void Render(ConnectionDisplayState state, ConnectionHeartbeat clock)
        {
            if (style == null || path == null || clock == null) return;
            path.Render(state, style, clock, phaseBeats, subdivision);
            pill.Render(state.workProgress, progressTreatment, style.ClassRail(state.trafficClass));
            if (identity.text != state.identity) identity.SetText(state.identity);
            ((RectTransform)pill.transform).anchoredPosition = path.PointAt(pillPosition);
            if (terminalMarker != null)
            {
                terminalMarker.rectTransform.anchoredPosition = path.PointAt(1);
                terminalMarker.SetAccent(style.Rail(state, clock.Beats));
            }
            if (displayGroup != null) displayGroup.alpha = style.Opacity(state);
        }
    }
}
