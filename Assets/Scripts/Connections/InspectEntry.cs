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
        [SerializeField] private InspectOperationToken loadBadge;
        [SerializeField, Min(60f)] private float loadBadgeWidth = 126f;
        [SerializeField, Min(0f)] private float loadBadgeGap = 12f;
        [SerializeField, Min(0f)] private float contentInset = 16f;
        [SerializeField, Min(0f)] private float identityPillTop = 12f;
        [SerializeField, Min(0f)] private float identityPillGap = 12f;
        [SerializeField] private TMP_Text work;
        [SerializeField] private TMP_Text throughput;
        [SerializeField] private TMP_Text bottleneck;
        [SerializeField] private TMP_Text eta;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image stateTag;
        [SerializeField] private Image panelBackground;
        [SerializeField] private Color activeColor = new Color(0.35f, 0.92f, 0.72f);
        [SerializeField] private Color atSpeedColor = new Color(0.56f, 0.89f, 0.40f);
        [SerializeField] private Color limitedColor = new Color(1f, 0.69f, 0.34f);
        [SerializeField] private Color stalledColor = new Color(0.96f, 0.29f, 0.26f);
        [SerializeField] private Color quietColor = new Color(0.52f, 0.67f, 0.66f);
        [SerializeField] private Color activeBackground = new Color(0.035f, 0.105f, 0.13f);
        [SerializeField] private Color completeBackground = new Color(0.026f, 0.078f, 0.095f);

        private readonly List<InspectOperationToken> operationTokens = new List<InspectOperationToken>();
        private LogicalConnectionState renderedState;
        private int renderedStage = -1;
        private int renderedOperationCount = -1;
        private bool renderedComplete;
        private float renderedTimelineWidth = -1f;
        private InspectOperationToken activeToken;
        private bool stackedUpper;

        public float ExtraHeight => stackedUpper ? 52f : 0f;

        public void ClearRuntimeTokens()
        {
            if (operationRow == null) return;
            foreach (InspectOperationToken token in
                operationRow.GetComponentsInChildren<InspectOperationToken>(true))
            {
                if (Application.isPlaying) Destroy(token.gameObject);
                else DestroyImmediate(token.gameObject);
            }
            operationTokens.Clear();
            activeToken = null;
            renderedState = null;
            renderedStage = -1;
            renderedOperationCount = -1;
            renderedTimelineWidth = -1f;
        }

        public void SetHeight(float height)
        {
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (stateTag != null)
                stateTag.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        public void SetCardWidth(float width)
        {
            RectTransform card = (RectTransform)transform;
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            float contentWidth = Mathf.Max(0f, width - contentInset * 2f);
            float upperLeft = contentInset;
            if (headerIdentityPill != null)
            {
                RectTransform pill = (RectTransform)headerIdentityPill.transform;
                pill.anchorMin = pill.anchorMax = new Vector2(0f, 1f);
                pill.pivot = new Vector2(0f, 1f);
                pill.anchoredPosition = new Vector2(contentInset, -identityPillTop);
                upperLeft += headerIdentityPill.Size.x + identityPillGap;
            }
            float upperWidth = Mathf.Max(0f, width - upperLeft - contentInset);
            stackedUpper = upperWidth - loadBadgeWidth - loadBadgeGap < 100f;
            float detailOffset = stackedUpper ? 52f : 0f;
            if (purpose != null)
                purpose.rectTransform.anchoredPosition = new Vector2(upperLeft, -9f);
            if (operationRow != null)
            {
                operationRow.anchoredPosition = new Vector2(
                    stackedUpper ? contentInset : upperLeft,
                    stackedUpper ? -90f : -39f);
                operationRow.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    Mathf.Max(0f, (stackedUpper ? contentWidth : upperWidth) -
                        loadBadgeWidth - loadBadgeGap));
            }
            if (loadBadge != null)
            {
                RectTransform badge = (RectTransform)loadBadge.transform;
                badge.anchorMin = badge.anchorMax = new Vector2(1f, 1f);
                badge.pivot = new Vector2(1f, 1f);
                badge.anchoredPosition = new Vector2(-contentInset,
                    stackedUpper ? -90f : -39f);
                badge.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, loadBadgeWidth);
                badge.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40f);
            }
            SetTextWidth(throughput, stackedUpper ? contentWidth : upperWidth);
            SetTextWidth(work, contentWidth);
            SetTextWidth(purpose, upperWidth);
            SetTextWidth(bottleneck, Mathf.Max(0f, contentWidth - 120f));
            if (throughput != null)
                throughput.rectTransform.anchoredPosition = new Vector2(
                    stackedUpper ? contentInset : upperLeft, -84f - detailOffset);
            SetLeftInset(work, contentInset);
            SetLeftInset(bottleneck, contentInset);
            if (work != null)
                work.rectTransform.anchoredPosition = new Vector2(contentInset,
                    -120f - detailOffset);
            if (bottleneck != null)
                bottleneck.rectTransform.anchoredPosition = new Vector2(contentInset,
                    -172f - detailOffset);
            if (eta != null)
            {
                RectTransform rect = eta.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-contentInset, -172f - detailOffset);
            }
            if (progressFill != null)
            {
                RectTransform progress = progressFill.transform.parent.GetComponent<RectTransform>();
                progress.anchoredPosition = new Vector2(contentInset, -148f - detailOffset);
                progressFill.transform.parent.GetComponent<RectTransform>()
                    .SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
            }
        }

        public void Render(LogicalConnectionState state, Color? identityAccent = null,
            InspectPresentationMode mode = InspectPresentationMode.Compact)
        {
            bool verbose = mode == InspectPresentationMode.Verbose;
            Color accent = identityAccent ?? activeColor;
            Set(identity, string.Empty);
            Set(purpose, string.IsNullOrWhiteSpace(state.Definition.ClaimedPurpose) ?
                string.Empty : state.Definition.ClaimedPurpose);
            if (identity != null) identity.color = state.IsComplete ? quietColor : accent;
            if (purpose != null) purpose.color = state.IsComplete ? quietColor : accent;
            if (headerIdentityPill != null)
            {
                Color identityColor = state.IsComplete ? quietColor : accent;
                if (!ReferenceEquals(renderedState, state))
                    headerIdentityPill.Bind(state.ConnectionId, identityColor);
                else
                    headerIdentityPill.SetAccentColor(identityColor);
            }
            if (panelBackground != null)
                panelBackground.color = state.IsComplete ? completeBackground : activeBackground;
            Color tint = state.IsComplete || !state.IsActive || state.IsTransitioning ? quietColor :
                state.Performance == LoadPerformance.AtSpeed ? atSpeedColor :
                state.Performance == LoadPerformance.Slowed ? limitedColor : stalledColor;
            if (!ReferenceEquals(renderedState, state) || renderedStage != state.CurrentOperationIndex ||
                renderedOperationCount != state.OperationCount || renderedComplete != state.IsComplete ||
                (operationRow != null &&
                    Mathf.Abs(renderedTimelineWidth - operationRow.rect.width) > 0.5f))
                RebuildOperations(state, tint);
            else if (!state.IsComplete && activeToken != null)
                activeToken.SetActiveColor(tint);
            Color badgeColor = state.IsComplete || !state.IsActive || state.IsTransitioning ?
                quietColor : state.Performance == LoadPerformance.Slowed ? limitedColor :
                state.Performance == LoadPerformance.Stalled ? stalledColor : quietColor;
            if (loadBadge != null)
                loadBadge.ShowLoad(state.IsDemanding ? state.DesiredLoad : 0f, badgeColor);
            NodeResources target = state.TargetResources;
            string status = state.Resolution == ConnectionResolution.Canceled ? "CANCELED" :
                state.Resolution == ConnectionResolution.Killed ? "KILLED" :
                state.IsComplete ? "COMPLETE" : state.IsPending ? "PENDING" :
                state.IsConnecting ? "CONNECTING" :
                state.IsTransitioning ? "NEXT OPERATION" : state.Performance == LoadPerformance.AtSpeed ?
                "AT SPEED" : state.Performance == LoadPerformance.Slowed ? "SLOWED" : "STALLED";
            string reason = string.Empty;
            if (state.IsDemanding && state.Performance != LoadPerformance.AtSpeed)
                reason = target.LoadCapacity - target.BaselineLoad <= 0.0001f ?
                    " · NO LOAD AVAILABLE" : target.RequestedLoad > target.LoadCapacity + 0.0001f ?
                    " · " + state.TargetNode.NodeId + " OVERLOADED" : " · TARGET LIMITED";
            bool workPaused = state.IsDemanding && state.AllocatedLoad > 0f &&
                state.EffectiveWorkRateMultiplier == 0f;
            string rateNote = workPaused ? " · WORK PAUSED" :
                state.IsDemanding && state.EffectiveWorkRateMultiplier != 1f ?
                    $" · WORK ×{state.EffectiveWorkRateMultiplier:G3}" : string.Empty;
            Set(throughput, status + reason + rateNote);
            if (throughput != null) throughput.color = tint;
            float fraction = state.CurrentTotalWork > 0f ? state.CompletedWork / state.CurrentTotalWork : 0f;
            Set(work, $"WORK {fraction * 100f:0}%   {state.CompletedWork:0.#}/{state.CurrentTotalWork:0.#}");
            Set(bottleneck, state.ActivityDescription);
            string remaining = state.IsComplete ? "00:00" : !state.IsActive ? "--:--" :
                workPaused ? "PAUSED" : state.ActualWorkRate <= 0f ? "--:--" :
                FormatSeconds(state.RemainingWork / state.ActualWorkRate);
            Set(eta, "ETA  " + remaining);
            if (throughput != null) throughput.gameObject.SetActive(verbose);
            if (work != null) work.gameObject.SetActive(verbose && !state.IsComplete);
            if (bottleneck != null) bottleneck.gameObject.SetActive(verbose && !state.IsComplete);
            if (eta != null) eta.gameObject.SetActive(verbose && !state.IsComplete);
            if (progressFill != null)
                progressFill.transform.parent.gameObject.SetActive(verbose && !state.IsComplete);

            if (progressFill != null)
            {
                Vector2 anchor = progressFill.rectTransform.anchorMax;
                if (Mathf.Abs(anchor.x - fraction) > 0.0005f)
                    progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), anchor.y);
            }
            Color chrome = state.IsComplete ? quietColor : accent;
            if (progressFill != null && progressFill.color != chrome) progressFill.color = chrome;
            if (stateTag != null && stateTag.color != chrome) stateTag.color = chrome;
        }

        private void RebuildOperations(LogicalConnectionState state, Color tint)
        {
            bool completedNormally = state.Resolution == ConnectionResolution.Completed;
            foreach (InspectOperationToken token in operationTokens)
            {
                token.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(token.gameObject);
                else DestroyImmediate(token.gameObject);
            }
            operationTokens.Clear();
            activeToken = null;
            if (operationRow == null || operationTokenPrefab == null) return;
            string target = state.TargetNode.NodeId;
            int current = state.CurrentOperationIndex;
            int completedCount = completedNormally ? state.OperationCount - 1 : current;
            int futureCount = completedNormally ? 0 : state.OperationCount - current - 1;
            int olderCount = Mathf.Max(0, completedCount - 1);
            bool currentCompleted = completedNormally;
            string currentLabel = InspectOperationToken.OperationLabel(
                state.Definition.OperationAt(current), target);
            float available = Mathf.Max(0f, operationRow.rect.width);
            float spacing = operationRow.GetComponent<HorizontalLayoutGroup>()?.spacing ?? 0f;
            float currentWidth = operationTokenPrefab.Measure(currentLabel,
                currentCompleted ? 32f : 24f);
            float historyWidth = completedCount > 0 ?
                operationTokenPrefab.Measure("+" + completedCount, 32f) : 0f;
            float futureWidth = futureCount > 0 ?
                operationTokenPrefab.Measure("+" + futureCount, 20f) : 0f;
            float shortWidth = operationTokenPrefab.Measure(InspectOperationToken.OperationLabel(
                state.Definition.OperationAt(current), target, true),
                currentCompleted ? 32f : 24f);
            int fullGroups = VisibleGroups(currentWidth, historyWidth, futureWidth,
                available, spacing);
            int shortGroups = VisibleGroups(shortWidth, historyWidth, futureWidth,
                available, spacing);
            bool shortCurrent = currentWidth > available || shortGroups > fullGroups;
            if (shortCurrent) currentWidth = shortWidth;
            float widths = currentWidth;
            int items = 1;
            bool showHistory = completedCount > 0 &&
                Fits(widths + historyWidth, items + 1, available, spacing);
            if (showHistory) { widths += historyWidth; items++; }
            bool showFuture = futureCount > 0 &&
                Fits(widths + futureWidth, items + 1, available, spacing);
            if (showFuture) { widths += futureWidth; items++; }
            bool showPrevious = false;
            bool shortPrevious = false;
            bool showOlder = false;
            if (completedCount > 0)
            {
                float previousWidth = operationTokenPrefab.Measure(
                    InspectOperationToken.OperationLabel(state.Definition.OperationAt(current - 1),
                        target), 32f);
                float olderWidth = olderCount > 0 ?
                    operationTokenPrefab.Measure("+" + olderCount, 32f) : 0f;
                float replacement = previousWidth + (olderCount > 0 ? olderWidth : 0f);
                int replacementItems = 1 + (olderCount > 0 ? 1 : 0);
                float candidateWidths = widths - (showHistory ? historyWidth : 0f) + replacement;
                int candidateItems = items - (showHistory ? 1 : 0) + replacementItems;
                if (!Fits(candidateWidths, candidateItems, available, spacing))
                {
                    float shortPreviousWidth = operationTokenPrefab.Measure(
                        InspectOperationToken.OperationLabel(
                            state.Definition.OperationAt(current - 1), target, true), 32f);
                    candidateWidths = candidateWidths - previousWidth + shortPreviousWidth;
                    shortPrevious = true;
                }
                if (Fits(candidateWidths, candidateItems, available, spacing))
                {
                    showPrevious = true;
                    showOlder = olderCount > 0;
                    showHistory = false;
                }
            }
            if (showOlder) AddToken().ShowCompletedCount(olderCount);
            else if (showHistory) AddToken().ShowCompletedCount(completedCount);
            if (showPrevious)
                AddToken().ShowOperation(state.Definition.OperationAt(current - 1), target, true,
                    quietColor, shortPrevious);
            activeToken = AddToken();
            activeToken.ShowOperation(state.Definition.OperationAt(current), target,
                currentCompleted, tint, shortCurrent);
            if (currentWidth > available) activeToken.ConstrainWidth(available);
            if (showFuture) AddToken().ShowFuture(futureCount);
            renderedState = state;
            renderedStage = state.CurrentOperationIndex;
            renderedOperationCount = state.OperationCount;
            renderedComplete = state.IsComplete;
            renderedTimelineWidth = available;
        }

        private InspectOperationToken AddToken()
        {
            InspectOperationToken token = Instantiate(operationTokenPrefab, operationRow);
            operationTokens.Add(token);
            return token;
        }

        private static bool Fits(float widths, int count, float available, float spacing) =>
            widths + Mathf.Max(0, count - 1) * spacing <= available + 0.5f;

        private static int VisibleGroups(float currentWidth, float historyWidth, float futureWidth,
            float available, float spacing)
        {
            float widths = currentWidth;
            int count = 1;
            if (historyWidth > 0f && Fits(widths + historyWidth, count + 1, available, spacing))
            {
                widths += historyWidth;
                count++;
            }
            if (futureWidth > 0f && Fits(widths + futureWidth, count + 1, available, spacing))
                count++;
            return count;
        }

        private static void SetTextWidth(TMP_Text label, float width)
        {
            if (label != null)
                label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private static void SetLeftInset(TMP_Text label, float inset)
        {
            if (label != null)
                label.rectTransform.anchoredPosition =
                    new Vector2(inset, label.rectTransform.anchoredPosition.y);
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
