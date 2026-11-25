using System;
using TMPro;
using UnityEngine;

namespace Uptime.Connections.Display
{
    [Serializable]
    public sealed class DisplayLabPhase
    {
        public string label;
        [Min(.1f)] public float duration = 4;
        public ConnectionDisplayState state;
        public bool advanceWork = true;
        [Min(.1f)] public float nominalWorkSeconds = 24;
        [Range(-1, 1)] public float endSeverity = -1;
        public bool showServiceEstimates;
        public float serviceRemaining = 30, projectedCurrent = 20, projectedNormal = 20;
    }

    // Only the lab owns synthetic recipes/timing/labels. Renderer knows none of these.
    public sealed class ConnectionDisplayLabSample : MonoBehaviour
    {
        public TMP_Text title, diagnostic, phaseLabel;
        public ConnectionDisplayView view;
        public DisplayLabPhase[] phases;
        public ConnectionDisplayState Current { get; private set; }
        public int PhaseIndex { get; private set; }
        private int lastPhase = -1;
        private string lastLabel;
        private bool lastShowService;
        public void Render(double time, ConnectionHeartbeat clock)
        {
            if (phases == null || phases.Length == 0) return;
            float duration = 0; foreach (var phase in phases) duration += phase.duration;
            float local = (float)(time % duration);
            int index = 0;
            while (index < phases.Length - 1 && local >= phases[index].duration)
            { local -= phases[index].duration; index++; }
            var p = phases[index]; var state = p.state;
            if (p.endSeverity >= 0) state.dangerSeverity = Mathf.Lerp(state.dangerSeverity, p.endSeverity, local / p.duration);
            if (p.advanceWork && state.resolution == DisplayResolution.Active)
                state.workProgress = Mathf.Repeat(state.workProgress + local * state.productiveSpeed / p.nominalWorkSeconds, 1);
            if (state.resolution != DisplayResolution.Active) state.resolutionAge = local;
            Current = state; PhaseIndex = index;
            view.Render(state, clock);
            diagnostic.SetText("LOAD {0:0.0}   RATE {1:0.00}x   WORK {2:0}%   SEV {3:0.00}", state.requestedLoad,
                state.productiveSpeed, state.workProgress * 100, state.dangerSeverity);
            if (phaseLabel != null)
            {
                if (p.showServiceEstimates)
                {
                    if (p.projectedCurrent < 0)
                        phaseLabel.SetText("BUDGET {0:0.0}s / EST stalled / NORMAL {1:0.0}s", p.serviceRemaining, p.projectedNormal);
                    else
                        phaseLabel.SetText("BUDGET {0:0.0}s / EST {1:0.0}s / NORMAL {2:0.0}s", p.serviceRemaining, p.projectedCurrent, p.projectedNormal);
                }
                else if (lastPhase != index || lastLabel != p.label || lastShowService)
                    phaseLabel.SetText(p.label);
            }
            lastPhase = index; lastLabel = p.label; lastShowService = p.showServiceEstimates;

        }
    }

}
