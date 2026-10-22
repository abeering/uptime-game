using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public sealed class InspectEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text identity;
        [SerializeField] private IdentityPillView headerIdentityPill;
        [SerializeField] private TMP_Text purpose;
        [SerializeField] private RectTransform operationRow;
        [SerializeField] private InspectOperationToken operationTokenPrefab;
        [SerializeField] private TMP_Text work;
        [SerializeField] private TMP_Text throughput;
        [SerializeField] private TMP_Text bottleneck;
        [SerializeField] private TMP_Text eta;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image stateTag;
        [SerializeField] private Image panelBackground;
        [SerializeField] private Image titleBar;
        [SerializeField] private Color activeColor = new Color(0.35f, 0.92f, 0.72f);
        [SerializeField] private Color atSpeedColor = new Color(0.56f, 0.89f, 0.40f);
        [SerializeField] private Color limitedColor = new Color(1f, 0.69f, 0.34f);
        [SerializeField] private Color stalledColor = new Color(0.96f, 0.29f, 0.26f);
        [SerializeField] private Color quietColor = new Color(0.52f, 0.67f, 0.66f);
        [SerializeField] private Color activeBackground = new Color(0.035f, 0.105f, 0.13f);
        [SerializeField] private Color completeBackground = new Color(0.026f, 0.078f, 0.095f);
        [SerializeField] private Color activeTitleBar = new Color(0.055f, 0.16f, 0.19f);
        [SerializeField] private Color completeTitleBar = new Color(0.04f, 0.11f, 0.13f);

        private readonly List<InspectOperationToken> operationTokens = new List<InspectOperationToken>();
        private LogicalConnectionState renderedState;
        private int renderedStage = -1;
        private int renderedOperationCount = -1;
        private bool renderedComplete;

        public void SetHeight(float height)
        {
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (stateTag != null)
                stateTag.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        public void Render(LogicalConnectionState state)
        {
            Set(identity, "INSPECT /");
            Set(purpose, string.IsNullOrWhiteSpace(state.Definition.ClaimedPurpose) ?
                string.Empty : "- " + state.Definition.ClaimedPurpose);
            if (identity != null) identity.color = state.IsComplete ? quietColor : activeColor;
            if (purpose != null) purpose.color = state.IsComplete ? quietColor : activeColor;
            if (headerIdentityPill != null)
            {
                Color identityColor = state.IsComplete ? quietColor : activeColor;
                if (!ReferenceEquals(renderedState, state))
                    headerIdentityPill.Bind(state.ConnectionId, identityColor);
                else
                    headerIdentityPill.SetLineColor(identityColor);
            }
            if (panelBackground != null)
                panelBackground.color = state.IsComplete ? completeBackground : activeBackground;
            if (titleBar != null)
                titleBar.color = state.IsComplete ? completeTitleBar : activeTitleBar;
            Color tint = state.IsComplete || state.IsPending || state.IsTransitioning ? quietColor :
                state.Performance == LoadPerformance.AtSpeed ? atSpeedColor :
                state.Performance == LoadPerformance.Slowed ? limitedColor : stalledColor;
            if (!ReferenceEquals(renderedState, state) || renderedStage != state.CurrentOperationIndex ||
                renderedOperationCount != state.OperationCount || renderedComplete != state.IsComplete)
                RebuildOperations(state, tint);
            else if (!state.IsComplete && renderedStage < operationTokens.Count)
                operationTokens[renderedStage].SetActiveColor(tint);
            NodeResources target = state.TargetResources;
            string status = state.Resolution == ConnectionResolution.Blocked ? "BLOCKED" :
                state.IsComplete ? "COMPLETE" : state.IsPending ? "PENDING" :
                state.IsTransitioning ? "NEXT OPERATION" : state.Performance == LoadPerformance.AtSpeed ?
                "AT SPEED" : state.Performance == LoadPerformance.Slowed ? "SLOWED" : "STALLED";
            string reason = string.Empty;
            if (state.IsDemanding && state.Performance != LoadPerformance.AtSpeed)
                reason = target.LoadCapacity - target.BaselineLoad <= 0.0001f ?
                    " · NO LOAD AVAILABLE" : target.RequestedLoad > target.LoadCapacity + 0.0001f ?
                    " · " + state.TargetNode.NodeId + " OVERLOADED" : " · TARGET LIMITED";
            Set(throughput, state.IsComplete ? status :
                $"{status} · LOAD {state.AllocatedLoad:0.#}/{state.DesiredLoad:0.#}{reason}");
            if (throughput != null) throughput.color = tint;
            float fraction = state.CurrentTotalWork > 0f ? state.CompletedWork / state.CurrentTotalWork : 0f;
            Set(work, $"WORK {fraction * 100f:0}%   {state.CompletedWork:0.#}/{state.CurrentTotalWork:0.#}");
            Set(bottleneck, state.ActivityDescription);
            string remaining = state.IsComplete ? "00:00" : state.IsPending ? "--:--" :
                state.ActualWorkRate <= 0f ? "--:--" : FormatSeconds(state.RemainingWork / state.ActualWorkRate);
            Set(eta, "ETA  " + remaining);
            if (work != null) work.gameObject.SetActive(!state.IsComplete);
            if (bottleneck != null) bottleneck.gameObject.SetActive(!state.IsComplete);
            if (eta != null) eta.gameObject.SetActive(!state.IsComplete);
            if (progressFill != null)
                progressFill.transform.parent.gameObject.SetActive(!state.IsComplete);

            if (progressFill != null)
            {
                Vector2 anchor = progressFill.rectTransform.anchorMax;
                if (Mathf.Abs(anchor.x - fraction) > 0.0005f)
                    progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), anchor.y);
            }
            Color chrome = state.IsComplete ? quietColor : activeColor;
            if (progressFill != null && progressFill.color != chrome) progressFill.color = chrome;
            if (stateTag != null && stateTag.color != chrome) stateTag.color = chrome;
        }

        private void RebuildOperations(LogicalConnectionState state, Color tint)
        {
            bool completedNormally = state.IsComplete && state.Resolution != ConnectionResolution.Blocked;
            foreach (InspectOperationToken token in operationTokens)
            {
                token.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(token.gameObject);
                else DestroyImmediate(token.gameObject);
            }
            operationTokens.Clear();
            if (operationRow == null || operationTokenPrefab == null) return;
            string target = state.TargetNode.NodeId;
            for (int i = 0; i < state.OperationCount; i++)
            {
                if (i > state.CurrentOperationIndex && !completedNormally)
                {
                    InspectOperationToken future = Instantiate(operationTokenPrefab, operationRow);
                    future.ShowFuture(state.OperationCount - i);
                    operationTokens.Add(future);
                    break;
                }
                InspectOperationToken token = Instantiate(operationTokenPrefab, operationRow);
                token.ShowOperation(state.Definition.OperationAt(i), target,
                    i < state.CurrentOperationIndex || completedNormally, tint);
                operationTokens.Add(token);
            }
            renderedState = state;
            renderedStage = state.CurrentOperationIndex;
            renderedOperationCount = state.OperationCount;
            renderedComplete = state.IsComplete;
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
