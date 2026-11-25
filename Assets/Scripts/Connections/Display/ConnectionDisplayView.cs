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
        private IdentityPillView pillBinding;
        private bool pillBound;
        public void Render(ConnectionDisplayState state, ConnectionHeartbeat clock)
        {
            if (style == null || path == null || clock == null) return;
            path.Render(state, style, clock, phaseBeats, subdivision);
            if (pill != null)
            {
                if (pillBinding == null) pillBinding = pill.GetComponent<IdentityPillView>();
                if (pillBinding != null)
                {
                    if (!pillBound) { pillBinding.Bind(state.identity, style.ClassRail(state.trafficClass)); pillBound = true; }
                    else pillBinding.SetDisplayedId(state.identity);
                    pillBinding.SetDisplayState(state, progressTreatment, displayGroup == null);
                }
                else pill.Render(state.workProgress, progressTreatment, style.ClassRail(state.trafficClass));
                ((RectTransform)pill.transform).anchoredPosition = path.PointAt(pillPosition);
            }
            if (identity != null && identity.text != state.identity) identity.SetText(state.identity);
            if (terminalMarker != null)
            {
                terminalMarker.rectTransform.anchoredPosition = path.PointAt(1);
                terminalMarker.SetAccent(style.Rail(state, clock.Beats));
            }
            if (displayGroup != null) displayGroup.alpha = style.Opacity(state);
        }
    }
}
