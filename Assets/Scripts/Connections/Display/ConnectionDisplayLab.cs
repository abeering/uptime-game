using TMPro;
using UnityEngine;

namespace Uptime.Connections.Display
{
    [ExecuteAlways]
    public sealed class ConnectionDisplayLab : MonoBehaviour
    {
        public ConnectionHeartbeat heartbeat;
        public ConnectionDisplayLabSample[] samples;
        public TMP_Text clockLabel;
        public ConnectionDisplayStyle style;
        public ConnectionProgressPillGraphic[] progressComparisons;
        public int SampleCount => samples?.Length ?? 0;
        private void OnEnable() => RenderNow();
        private void LateUpdate() => RenderNow();
        public void RenderNow()
        {
            if (heartbeat == null || samples == null) return;
            foreach (var sample in samples) if (sample != null) sample.Render(heartbeat.ElapsedSeconds, heartbeat);
            if (progressComparisons != null && style != null)
                foreach (var pill in progressComparisons) if (pill != null)
                    pill.Render(pill.progress, pill.treatment, style.ordinaryRail);
            if (clockLabel != null) clockLabel.SetText("{0:0} BPM   /   {1:0.00}s   /   {2:0.00} BEATS",
                heartbeat.bpm, (float)heartbeat.ElapsedSeconds, (float)heartbeat.Beats);
        }
        public void Replay() { heartbeat.Replay(); RenderNow(); }
        public void TogglePause() { heartbeat.TogglePause(); RenderNow(); }
    }
}
