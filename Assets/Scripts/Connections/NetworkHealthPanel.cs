using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // The same authored instrument changes its height and detail on TAB.
    public sealed class NetworkHealthPanel : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform terminalPanel;
        [SerializeField] private TMP_InputField commandInput;
        [SerializeField] private ConnectionsCleanOverlay cleanOverlay;
        [SerializeField] private GameObject expandedContent;
        [SerializeField] private TMP_Text clockText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text rateText;
        [SerializeField] private TMP_Text headingText;
        [SerializeField] private TMP_Text trendText;
        [SerializeField] private TMP_Text serviceText;
        [SerializeField] private TMP_Text causesText;
        [SerializeField] private TMP_Text eventsText;
        [SerializeField] private Image healthFill;
        [SerializeField] private Color degradingColor = new Color(1f, 0.63f, 0.39f);
        [SerializeField] private Color recoveringColor = new Color(0.52f, 0.84f, 0.72f);
        [SerializeField] private Color stableColor = new Color(0.47f, 0.63f, 0.62f);
        [SerializeField, Range(0, 23)] private int shiftStartHour = 9;
        [SerializeField, Range(0, 59)] private int shiftStartMinute;
        [SerializeField, Range(0, 23)] private int shiftEndHour = 17;
        [SerializeField, Range(0, 59)] private int shiftEndMinute;
        [SerializeField, Min(0.001f)] private float clockMinutesPerSimulationSecond = 1f;
        [SerializeField, Min(32f)] private float compactHeight = 52f;
        [SerializeField, Range(0.1f, 0.35f)] private float expandedHeightFraction = 0.2f;
        private bool expanded;
        private bool restoreFocus;
        private int anchor, focus;

        public bool Expanded => expanded;
        public string ClockText => clockText != null ? clockText.text : null;
        public int ShiftEndMinuteOfDay => shiftEndHour * 60 + shiftEndMinute;

        private void Update()
        {
            if (Application.isPlaying && (cleanOverlay == null || !cleanOverlay.IsOpen) &&
                Keyboard.current != null &&
                Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (commandInput != null)
                {
                    anchor = commandInput.selectionAnchorPosition;
                    focus = commandInput.selectionFocusPosition;
                    restoreFocus = true;
                }
                expanded = !expanded;
                ApplyLayout();
            }
            Render();
        }

        private void LateUpdate()
        {
            if (!restoreFocus || commandInput == null) return;
            restoreFocus = false;
            if (cleanOverlay != null && cleanOverlay.IsOpen) return;
            commandInput.ActivateInputField();
            commandInput.Select();
            commandInput.selectionAnchorPosition = anchor;
            commandInput.selectionFocusPosition = focus;
            StartCoroutine(RestoreFocusNextFrame());
        }

        private IEnumerator RestoreFocusNextFrame()
        {
            yield return null;
            if (commandInput == null || cleanOverlay != null && cleanOverlay.IsOpen)
                yield break;
            commandInput.ActivateInputField();
            commandInput.Select();
            commandInput.selectionAnchorPosition = anchor;
            commandInput.selectionFocusPosition = focus;
        }

        private void OnEnable() { ApplyLayout(); Render(); }

        public void ApplyLayout()
        {
            if (panel == null || terminalPanel == null) return;
            float parentHeight = ((RectTransform)panel.parent).rect.height;
            float height = expanded ? parentHeight * expandedHeightFraction : compactHeight;
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            var terminal = terminalPanel.GetComponent<Terminal>();
            if (terminal != null) terminal.SetTopInset(height + 6f);
            else terminalPanel.offsetMax = new Vector2(terminalPanel.offsetMax.x, -height - 6f);
            if (expandedContent != null) expandedContent.SetActive(expanded);
        }

        public void Render()
        {
            if (host == null) return;
            float clockMinutes = shiftStartHour * 60f + shiftStartMinute +
                host.SimulationTimeSeconds * Mathf.Max(0.001f, clockMinutesPerSimulationSecond);
            int minuteOfDay = Mathf.FloorToInt(clockMinutes) % 1440;
            if (clockText != null) clockText.text = $"{minuteOfDay / 60:00}:{minuteOfDay % 60:00}";
            NetworkHealthSnapshot health = host.Health;
            if (health == null) return;
            if (valueText != null) valueText.text = $"HEALTH {health.Value:0.#}";
            if (healthFill != null) healthFill.fillAmount = health.Value / 100f;
            float displayRate = Mathf.Abs(health.NetRate) < 0.005f ? 0f : health.NetRate;
            Color rateColor = displayRate < 0f ? degradingColor :
                displayRate > 0f ? recoveringColor : stableColor;
            string state = displayRate < 0f ? "DEGRADING" :
                displayRate > 0f ? "RECOVERING" : "STABLE";
            if (rateText != null)
            {
                rateText.text = $"{displayRate:+0.00;-0.00;0.00}/s";
                rateText.color = rateColor;
            }
            if (trendText != null) trendText.text =
                $"{state}   {displayRate:+0.00;-0.00;0.00}/s";
            if (trendText != null) trendText.color = rateColor;
            if (serviceText != null) serviceText.text =
                $"SLOWED CONNECTIONS   {health.SlowedCount}";
            if (causesText != null) causesText.text =
                $"OVERDUE CONNECTIONS  {health.OverdueCount}";
            if (headingText != null)
            {
                NodeResources peak = null;
                float peakFraction = -1f;
                foreach (NodeResources node in host.Nodes)
                {
                    float fraction = node.LoadCapacity > 0f ?
                        node.RequestedLoad / node.LoadCapacity : 0f;
                    if (fraction <= peakFraction) continue;
                    peak = node;
                    peakFraction = fraction;
                }
                headingText.text = peak == null ? "NETWORK HEALTH" :
                    $"NETWORK HEALTH  /  PEAK LOAD {peak.View.NodeId} {peak.RequestedLoad:0.#}/{peak.LoadCapacity:0.#}";
            }
            if (eventsText != null) eventsText.text = health.RecentEvents.Count == 0 ?
                (health.Failed ? "NETWORK FAILURE" : "RECENT IMPACTS  NONE") :
                $"RECENT IMPACT  " +
                (health.RecentEvents[0].Reason == "innocent_process" ?
                    "INNOCENT PROCESS TERMINATED " : "SERVICE INTERRUPTED ") +
                $"{health.RecentEvents[0].Amount:0.#}" +
                (health.Failed ? "   NETWORK FAILURE" : "");
        }
    }
}
