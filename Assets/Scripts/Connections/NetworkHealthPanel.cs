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
        [SerializeField] private GameObject expandedContent;
        [SerializeField] private TMP_Text clockText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text trendText;
        [SerializeField] private TMP_Text serviceText;
        [SerializeField] private TMP_Text causesText;
        [SerializeField] private TMP_Text eventsText;
        [SerializeField] private Image healthFill;
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
            if (Application.isPlaying && Keyboard.current != null &&
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
            commandInput.ActivateInputField();
            commandInput.Select();
            commandInput.selectionAnchorPosition = anchor;
            commandInput.selectionFocusPosition = focus;
            StartCoroutine(RestoreFocusNextFrame());
        }

        private IEnumerator RestoreFocusNextFrame()
        {
            yield return null;
            if (commandInput == null) yield break;
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
            terminalPanel.offsetMax = new Vector2(terminalPanel.offsetMax.x, -height - 6f);
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
            if (trendText != null) trendText.text =
                $"TREND {health.NetRate:+0.000;-0.000;0.000}/s   REC +{health.RecoveryRate:0.000}   DRAIN -{health.DegradationRate:0.000}";
            if (serviceText != null) serviceText.text =
                $"SERVICE  {health.SlowedCount} SLOWED   {health.OverdueCount} OVERDUE";
            if (causesText != null) causesText.text = health.Impacts.Count == 0 ?
                "CAUSES  NONE" : $"CAUSES  {health.OverdueCount} LATE / {health.SlowedCount} LOAD LIMITED";
            if (eventsText != null) eventsText.text = health.RecentEvents.Count == 0 ?
                (health.Failed ? "NETWORK FAILURE" : "RECENT IMPACTS  NONE") :
                $"RECENT IMPACT  SERVICE INTERRUPTED {health.RecentEvents[0].Amount:0.#}" +
                (health.Failed ? "   NETWORK FAILURE" : "");
        }
    }
}
