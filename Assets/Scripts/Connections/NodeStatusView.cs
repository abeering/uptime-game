using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // One authored visual stack per node; three families can animate independently.
    public sealed class NodeStatusView : MonoBehaviour
    {
        [SerializeField] private NodeStatusBandGraphic painBand;
        [SerializeField] private NodeStatusBandGraphic awarenessBand;
        [SerializeField] private NodeStatusBandGraphic benefitBand;
        [SerializeField] private RectTransform stackRoot;
        [SerializeField] private NodeStatusPanel painPanel;
        [SerializeField] private NodeStatusPanel awarenessPanel;
        [SerializeField] private NodeStatusPanel benefitPanel;
        [SerializeField] private Color painColor = new Color(1f, .37f, .39f);
        [SerializeField] private Color awarenessColor = new Color(.68f, .52f, 1f);
        [SerializeField] private Color benefitColor = new Color(.40f, .91f, .76f);
        [SerializeField, Min(.05f)] private float appearanceSeconds = .32f;
        [SerializeField, Min(.05f)] private float disappearanceSeconds = .5f;

        private readonly float[] strength = new float[3];
        private Rect nodeRect;
        private float stackHeight;

        public RectTransform Rect => (RectTransform)transform;
        public RectTransform StackRoot => stackRoot;
        public Vector2 StackSize => new Vector2(184f, stackHeight);
        public bool HasVisibleFamily => strength[0] > 0f || strength[1] > 0f || strength[2] > 0f;
        public bool PainVisible => painPanel.gameObject.activeSelf;
        public bool AwarenessVisible => awarenessPanel.gameObject.activeSelf;
        public bool BenefitVisible => benefitPanel.gameObject.activeSelf;

        public void SetNodeRect(Rect card)
        {
            nodeRect = card;
            Rect.anchoredPosition = card.center;
            Rect.sizeDelta = card.size;
        }

        public void PlaceStack(Rect target)
        {
            stackRoot.sizeDelta = target.size;
            stackRoot.anchoredPosition = target.center - nodeRect.center;
        }

        public void Render(NodeStatusState state, float realTime, float deltaSeconds)
        {
            bool pain = state.Offline || state.KnownInfected || state.ScanJammed;
            bool awareness = state.AwarenessPreview || state.PortScanned;
            bool clean = state.CleanVisible(realTime);
            bool intercept = state.InterceptPayloadId != null;
            bool benefit = clean || intercept || state.Isolated;
            bool[] desired = { pain, awareness, benefit };
            NodeStatusBandGraphic[] bands = { painBand, awarenessBand, benefitBand };
            NodeStatusPanel[] panels = { painPanel, awarenessPanel, benefitPanel };
            Color[] colors = { painColor, awarenessColor, benefitColor };
            stackHeight = 0f;
            int activeRank = 0;
            for (int i = 0; i < 3; i++)
            {
                float seconds = desired[i] ? appearanceSeconds : disappearanceSeconds;
                strength[i] = Mathf.MoveTowards(strength[i], desired[i] ? 1f : 0f,
                    Mathf.Max(0f, deltaSeconds) / seconds);
                bool visible = strength[i] > 0f;
                bands[i].gameObject.SetActive(visible);
                panels[i].gameObject.SetActive(visible);
                if (!visible) continue;

                float margin = 12f + 6f * strength[i] + activeRank * 12f;
                RectTransform frame = bands[i].rectTransform;
                frame.sizeDelta = nodeRect.size + Vector2.one * (margin * 2f);
                frame.anchoredPosition = Vector2.zero;
                float pulse = .89f + .11f * Mathf.Sin(realTime * (i == 0 ? 5.1f : 3.1f));
                bands[i].SetAccent(colors[i], strength[i] * pulse);
                panels[i].SetOpacity(strength[i]);
                float pop = .84f + .16f * strength[i] +
                    .12f * Mathf.Sin(Mathf.PI * strength[i]);
                panels[i].transform.localScale = Vector3.one * pop;
                activeRank++;
            }

            if (pain)
            {
                var rows = new List<string>();
                var timers = new List<float?>();
                if (state.Offline) { rows.Add("OFFLINE"); timers.Add(null); }
                if (state.KnownInfected) { rows.Add("INFECTED"); timers.Add(null); }
                if (state.ScanJammed)
                { rows.Add("SCAN JAMMED"); timers.Add(state.ScanJamRemainingFraction); }
                SetRows(painPanel, rows, timers, painColor);
            }
            if (awareness)
                awarenessPanel.SetContent(state.PortScanned ? "PORT SCANNED" : "PORT SCAN",
                    null, awarenessColor,
                    state.PortScanned ? state.PortScanRemainingFraction : (float?)null);
            if (benefit)
            {
                var rows = new List<string>();
                var timers = new List<float?>();
                if (clean) { rows.Add("CLEAN"); timers.Add(null); }
                if (state.Isolated)
                { rows.Add("ISOLATED"); timers.Add(state.IsolationRemainingFraction); }
                string interceptText = state.InterceptSucceeded ? "CAUGHT " :
                    "INTERCEPT ";
                if (intercept)
                { rows.Add(interceptText + state.InterceptPayloadId);
                    timers.Add(state.InterceptRemainingFraction); }
                SetRows(benefitPanel, rows, timers, benefitColor);
            }

            for (int i = 0; i < 3; i++)
                if (panels[i].gameObject.activeSelf)
                    stackHeight += panels[i].Height + (stackHeight > 0f ? 7f : 0f);
            stackRoot.sizeDelta = new Vector2(184f, stackHeight);
            float top = stackHeight * .5f;
            for (int i = 0; i < 3; i++)
            {
                if (!panels[i].gameObject.activeSelf) continue;
                panels[i].Rect.anchoredPosition = new Vector2(0f, top - panels[i].Height * .5f);
                top -= panels[i].Height + 7f;
            }
        }

        private static void SetRows(NodeStatusPanel panel, List<string> rows,
            List<float?> timers, Color color)
        {
            panel.SetRows(rows[0], rows.Count > 1 ? rows[1] : null,
                rows.Count > 2 ? rows[2] : null, color,
                timers[0], rows.Count > 1 ? timers[1] : null,
                rows.Count > 2 ? timers[2] : null);
        }

        public void ClearVisuals()
        {
            for (int i = 0; i < strength.Length; i++) strength[i] = 0f;
            painBand.gameObject.SetActive(false);
            awarenessBand.gameObject.SetActive(false);
            benefitBand.gameObject.SetActive(false);
            painPanel.gameObject.SetActive(false);
            awarenessPanel.gameObject.SetActive(false);
            benefitPanel.gameObject.SetActive(false);
            stackHeight = 0f;
        }
    }
}
