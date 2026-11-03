using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Presents authoritative Health in the permanently compact, authored status strip.
    public sealed class NetworkHealthPanel : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private TMP_Text clockText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text rateText;
        [SerializeField] private Image healthFill;
        [Header("Global tool readiness")]
        [SerializeField] private Image probeStatusFill;
        [SerializeField] private TMP_Text probeStatusText;
        [SerializeField] private Image scanStatusFill;
        [SerializeField] private TMP_Text scanStatusText;
        [SerializeField] private Color readyFillColor = new Color(0.14f, 0.33f, 0.27f);
        [SerializeField] private Color readyTextColor = new Color(0.70f, 0.91f, 0.78f);
        [SerializeField] private Color activeFillColor = new Color(0.13f, 0.31f, 0.36f);
        [SerializeField] private Color activeTextColor = new Color(0.70f, 0.87f, 0.90f);
        [SerializeField] private Color coolingFillColor = new Color(0.34f, 0.28f, 0.13f);
        [SerializeField] private Color coolingTextColor = new Color(0.96f, 0.81f, 0.53f);
        [SerializeField] private Color healthyBarColor = new Color(0.32f, 0.77f, 0.66f);
        [SerializeField] private Color drainingBarColor = new Color(0.85f, 0.53f, 0.32f);
        [SerializeField] private Color criticalBarColor = new Color(0.94f, 0.32f, 0.30f);
        [SerializeField, Range(0f, 100f)] private float criticalHealthThreshold = 25f;
        [SerializeField, Min(0f)] private float healthBarLerpSpeed = 10f;
        [SerializeField] private Color degradingColor = new Color(1f, 0.63f, 0.39f);
        [SerializeField] private Color recoveringColor = new Color(0.52f, 0.84f, 0.72f);
        [SerializeField] private Color stableColor = new Color(0.47f, 0.63f, 0.62f);
        [SerializeField, Range(0, 23)] private int shiftStartHour = 9;
        [SerializeField, Range(0, 59)] private int shiftStartMinute;
        [SerializeField, Range(0, 23)] private int shiftEndHour = 17;
        [SerializeField, Range(0, 59)] private int shiftEndMinute;
        [SerializeField, Min(0.001f)] private float clockMinutesPerSimulationSecond = 1f;
        private float displayedHealth = float.NaN;
        private float lastSimulationTime;

        public string ClockText => clockText != null ? clockText.text : null;
        public int ShiftEndMinuteOfDay => shiftEndHour * 60 + shiftEndMinute;

        private void Update() => Render();

        private void OnEnable()
        {
            displayedHealth = float.NaN;
            Render();
        }

        public void Render()
        {
            if (host == null) return;
            float clockMinutes = shiftStartHour * 60f + shiftStartMinute +
                host.SimulationTimeSeconds * Mathf.Max(0.001f, clockMinutesPerSimulationSecond);
            int minuteOfDay = Mathf.FloorToInt(clockMinutes) % 1440;
            if (clockText != null) clockText.text = $"{minuteOfDay / 60:00}:{minuteOfDay % 60:00}";
            RenderTools();
            NetworkHealthSnapshot health = host.Health;
            if (health == null) return;
            if (valueText != null) valueText.text = $"HEALTH {health.Value:0.#}";
            RenderHealthBar(health, Time.unscaledDeltaTime,
                !Application.isPlaying || host.SimulationTimeSeconds < lastSimulationTime);
            lastSimulationTime = host.SimulationTimeSeconds;
            float displayRate = Mathf.Abs(health.NetRate) < 0.005f ? 0f : health.NetRate;
            Color rateColor = displayRate < 0f ? degradingColor :
                displayRate > 0f ? recoveringColor : stableColor;
            if (rateText != null)
            {
                rateText.text = $"{displayRate:+0.00;-0.00;0.00}/s";
                rateText.color = rateColor;
            }
        }

        private void RenderTools()
        {
            if (host.ProbedLink != null)
                SetToolStatus(probeStatusFill, probeStatusText,
                    $"ACTIVE {Seconds(host.ProbeActiveRemaining)}s", activeFillColor,
                    activeTextColor);
            else if (host.ProbeCooldownRemaining > 0.00001f)
                SetToolStatus(probeStatusFill, probeStatusText,
                    $"{Seconds(host.ProbeCooldownRemaining)}s", coolingFillColor,
                    coolingTextColor);
            else
                SetToolStatus(probeStatusFill, probeStatusText, "READY", readyFillColor,
                    readyTextColor);

            NodeScanSession scan = host.ActiveScan;
            if (scan != null)
                SetToolStatus(scanStatusFill, scanStatusText,
                    $"ACTIVE {Seconds(scan.ScheduledCompletionAtSeconds - host.SimulationTimeSeconds)}s",
                    activeFillColor, activeTextColor);
            else
                SetToolStatus(scanStatusFill, scanStatusText, "READY", readyFillColor,
                    readyTextColor);
        }

        private static int Seconds(float remaining) => Mathf.Max(1, Mathf.CeilToInt(remaining));

        private static void SetToolStatus(Image fill, TMP_Text text, string value,
            Color fillColor, Color textColor)
        {
            if (fill != null) fill.color = fillColor;
            if (text != null)
            {
                text.text = value;
                text.color = textColor;
            }
        }

        private void RenderHealthBar(NetworkHealthSnapshot health, float deltaSeconds, bool snap)
        {
            if (healthFill == null) return;
            float target = Mathf.Clamp(health.Value, 0f, 100f);
            if (snap || float.IsNaN(displayedHealth) || healthBarLerpSpeed <= 0f)
                displayedHealth = target;
            else
            {
                // Exponential lerp keeps the response consistent across frame rates.
                float blend = 1f - Mathf.Exp(-healthBarLerpSpeed * Mathf.Max(0f, deltaSeconds));
                displayedHealth = Mathf.Lerp(displayedHealth, target, blend);
                if (Mathf.Abs(displayedHealth - target) < 0.01f) displayedHealth = target;
            }

            // A one-off penalty stays orange while its visible drop settles, even if
            // the simulation has already resumed recovery. Critical Health wins immediately.
            bool draining = health.DegradationRate > 0f || health.NetRate < 0f ||
                displayedHealth > target + 0.01f;
            healthFill.color = target < criticalHealthThreshold ? criticalBarColor :
                draining ? drainingBarColor : healthyBarColor;
            // Sprite-less Images ignore fillAmount; size the authored rectangle instead.
            RectTransform fill = healthFill.rectTransform;
            fill.anchorMax = new Vector2(displayedHealth / 100f, fill.anchorMax.y);
        }
    }
}
