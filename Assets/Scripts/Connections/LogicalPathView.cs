using System;
using UnityEngine;
using Uptime.Connections.Display;

namespace Uptime.Connections
{
    public sealed class LogicalPathView : MonoBehaviour
    {
        [SerializeField] private PathGraphic path;
        [SerializeField] private ConnectionDisplayView sharedDisplay;
        [SerializeField] private ConnectionDisplayGraphic sharedPathPrefab;
        public ConnectionDisplayGraphic SharedPathPrefab => sharedPathPrefab;
        private ConnectionGameplayDisplay gameplayDisplay;
        public bool UsesSharedDisplay => sharedDisplay != null;
        public ConnectionDisplayView SharedDisplay => sharedDisplay;
        private int identitySlot;
        public void SetIdentitySlot(int slot) => identitySlot = slot;
        private double resolutionBegan = double.NaN;
        public ConnectionDisplayState DisplayState
        {
            get
            {
                if (state.IsComplete && gameplayDisplay != null)
                {
                    double now = gameplayDisplay.heartbeat.ElapsedSeconds;
                    if (double.IsNaN(resolutionBegan)) resolutionBegan = now;
                    departureElapsed = (float)Math.Max(0, now - resolutionBegan);
                }
                var display = ConnectionGameplayDisplay.Map(state, sharedDisplay.style, departureElapsed);
                display.identitySlot = identitySlot; return display;
            }
        }
        [SerializeField] private TerminalMarkerStyle terminalMarkerPrefab;
        [SerializeField, Range(0.1f, 2f)] private float departureSeconds = 0.45f;
        [SerializeField, Range(0.05f, 1f)] private float failureFlashHalfCycleSeconds = 0.12f;
        [SerializeField] private Color failureColor = new Color(1f, 0.12f, 0.12f);
        [SerializeField, Range(0.05f, 1f)] private float relayoutSeconds = 0.3f;
        [SerializeField] private Color healthyColor = new Color(0.36f, 0.95f, 0.74f);
        [SerializeField] private Color constrainedColor = new Color(1f, 0.69f, 0.34f);
        [SerializeField] private Color stalledColor = new Color(0.96f, 0.29f, 0.26f);
        [Header("Operation activity")]
        [SerializeField, Min(16f)] private float pulseSpacingAtOneLoad = 90f;
        [SerializeField, Min(0f)] private float spacingReductionPerLoad = 0.32f;
        [SerializeField, Min(1f)] private float pulseSpeedPixelsPerSecond = 90f;

        private LogicalConnectionState state;
        private RectTransform networkArea;
        private float[] startOffsets;
        private float[] endOffsets;
        private float[] lineWidths;
        private float[] fromStartOffsets;
        private float[] fromEndOffsets;
        private float[] fromWidths;
        private float[] targetStartOffsets;
        private float[] targetEndOffsets;
        private float[] targetWidths;
        private float relayoutElapsed;
        private Vector2[] starts;
        private Vector2[] ends;
        private float departureElapsed;
        private float failureElapsed;
        private bool establishedVisual;
        private float pulseTravel;
        private Color identityColor;
        private bool hasIdentityColor;
        private TerminalMarkerStyle terminalMarker;
        private int probeSegment = -1;
        private Vector2 probeCenter;
        private Vector2 probeAlong;
        private Vector2 probeHalfSize;
        private Color probeTint;

        public event Action<LogicalConnectionState> Destroyed;
        public TerminalMarkerStyle TerminalMarker => terminalMarker;
        public int VisualSegmentCount => starts?.Length ?? 0;
        public float RelayoutSeconds => relayoutSeconds;
        public Color DisplayAccent => UsesSharedDisplay && state != null ?
            sharedDisplay.style.Rail(DisplayState, gameplayDisplay != null ? gameplayDisplay.heartbeat.Beats : 0) : state != null && state.IsComplete &&
            !state.IsNormalDeparture &&
            Mathf.FloorToInt(failureElapsed / failureFlashHalfCycleSeconds) % 2 == 0 ?
            failureColor : identityColor;

        public float PulseSpacingFor(float requestedLoad) => UsesSharedDisplay ? sharedDisplay.style.Spacing(requestedLoad) :
            Mathf.Max(18f, pulseSpacingAtOneLoad /
                (1f + spacingReductionPerLoad * Mathf.Max(0f, requestedLoad - 1f)));
        public static int DirectionFor(OperationKind operation) =>
            operation == OperationKind.Read ? -1 : operation == OperationKind.Write ? 1 : 0;

        public void SetIdentityColor(Color color)
        {
            identityColor = color;
            hasIdentityColor = true;
        }

        public void SetProbe(int segment, Vector2 center, Vector2 along,
            Vector2 halfSize, Color tint)
        {
            probeSegment = segment;
            probeCenter = center;
            probeAlong = along;
            probeHalfSize = halfSize;
            probeTint = tint;
            if (UsesSharedDisplay) sharedDisplay.path.SetProbe(segment, center, along, halfSize, tint);
            else if (path != null)
                path.SetProbe(segment, center, along, halfSize, tint);
        }

        public Color BeamColorFor(LoadPerformance performance) => UsesSharedDisplay ? DisplayAccent :
            performance == LoadPerformance.AtSpeed ?
                (hasIdentityColor ? identityColor : healthyColor) :
            performance == LoadPerformance.Slowed ? constrainedColor : stalledColor;

        public void Bind(LogicalConnectionState connection, RectTransform area, RectTransform markerLayer, ConnectionGameplayDisplay display = null)
        {
            state = connection;
            gameplayDisplay = display;
            resolutionBegan = double.NaN; departureElapsed = failureElapsed = 0;
            networkArea = area;
            int count = connection.RouteLinks.Length;
            starts = new Vector2[count];
            ends = new Vector2[count];
            if (UsesSharedDisplay)
            {
                if (sharedDisplay.path == null)
                    sharedDisplay.path = Instantiate(sharedPathPrefab, transform, false);
                sharedDisplay.path.SetGeometry(starts, ends, new float[count]);
                sharedDisplay.path.SetVisibility(0, false);
            }
            else
            {
                path.raycastTarget = false;
                path.SetPath(starts, ends, new float[count], 0f);
            }
            if (terminalMarkerPrefab != null && markerLayer != null && connection.TargetNode != null)
            {
                terminalMarker = Instantiate(terminalMarkerPrefab, markerLayer, false);
                terminalMarker.name = "Terminal marker " + connection.ConnectionId;
                terminalMarker.gameObject.SetActive(false);
            }
        }

        private RouteNode[] presentedRoute;
        private PhysicalLinkReference[] presentedLinks;
        private int presentedRevision = -1;
        private bool routeAnimation, extendingRoute;
        private float routeAnimationElapsed, retractionSeconds, routeTravelSeconds, retractionStartReveal = 1, lastReveal;
        private int visualSharedLinks;
        private float[] retainedStarts, retainedEnds, retainedWidths;
        public RouteNode[] PresentationRoute => presentedRoute ?? state.Route;
        internal PhysicalLinkReference[] PresentationLinks => presentedLinks ?? state.RouteLinks;
        public bool IsRetractingRoute => routeAnimation && !extendingRoute;
        public float VisibleRouteFraction => CurrentReveal();
        public void RefreshOperationRoute()
        {
            if (state == null) return;
            if (presentedRoute == null) { SwitchPresentedRoute(state.Route, state.RouteLinks); presentedRevision = state.RouteRevision; return; }
            if (state.IsComplete) return; // Freeze the actually visible route for the existing departure.
            if (presentedRevision != state.RouteRevision)
            {
                presentedRevision = state.RouteRevision;
                int common = OperationRouteTransition.CommonPrefix(presentedRoute, state.Route);
                if (common == presentedRoute.Length && common == state.Route.Length) return;
                var transition = state.RouteTransition;
                if (transition == null || transition.RouteRevision != state.RouteRevision)
                { SwitchPresentedRoute(state.Route, state.RouteLinks); return; }
                routeAnimation = true; extendingRoute = false; routeAnimationElapsed = 0;
                visualSharedLinks = Mathf.Max(0, common - 1); retractionStartReveal = lastReveal;
                routeTravelSeconds = transition.TravelSeconds;
                retractionSeconds = visualSharedLinks >= presentedLinks.Length ? 0 : routeTravelSeconds > 0 ? Mathf.Min(relayoutSeconds, routeTravelSeconds * .5f) : relayoutSeconds;
            }
            if (routeAnimation && gameplayDisplay?.host != null)
                routeAnimationElapsed = Mathf.Max(routeAnimationElapsed, gameplayDisplay.host.SimulationTimeSeconds - state.RouteTransition.StartedAtSeconds);
            if (routeAnimation && !extendingRoute && routeAnimationElapsed >= retractionSeconds)
            { SwitchPresentedRoute(state.Route, state.RouteLinks); extendingRoute = true; }
        }
        private void SwitchPresentedRoute(RouteNode[] route, PhysicalLinkReference[] links)
        {
            int retained = presentedRoute == null ? 0 : Mathf.Max(0, OperationRouteTransition.CommonPrefix(presentedRoute, route) - 1);
            retainedStarts = new float[retained]; retainedEnds = new float[retained]; retainedWidths = new float[retained];
            for (int i = 0; i < retained && startOffsets != null; i++)
            { retainedStarts[i] = startOffsets[i]; retainedEnds[i] = endOffsets[i]; retainedWidths[i] = lineWidths[i]; }
            presentedRoute = route; presentedLinks = links;
            starts = new Vector2[links.Length]; ends = new Vector2[links.Length];
            startOffsets = endOffsets = lineWidths = targetStartOffsets = targetEndOffsets = targetWidths = null;
            fromStartOffsets = fromEndOffsets = fromWidths = null; establishedVisual = false;
            if (terminalMarker != null) terminalMarker.gameObject.SetActive(false);
        }

        public void SetPresentationActive(bool active)
        {
            gameObject.SetActive(active);
            if (!active && terminalMarker != null)
                terminalMarker.gameObject.SetActive(false);
        }

        public static Vector2 TerminalBorderPoint(Rect card, Vector2 towardSource,
            float laneOffset, out bool verticalSide)
        {
            Vector2 point = EdgeGeometry.CardEdge(card, towardSource, laneOffset);
            float xGap = Mathf.Min(Mathf.Abs(point.x - card.xMin), Mathf.Abs(point.x - card.xMax));
            float yGap = Mathf.Min(Mathf.Abs(point.y - card.yMin), Mathf.Abs(point.y - card.yMax));
            verticalSide = xGap < yGap || Mathf.Abs(xGap - yGap) < 0.001f &&
                Mathf.Abs(towardSource.x) >= Mathf.Abs(towardSource.y);
            return point;
        }

        public static Vector2 TerminalSocketCenter(Rect card, Vector2 towardSource,
            float laneOffset, float borderOutset)
        {
            Vector2 direction = towardSource.normalized;
            return TerminalBorderPoint(card, direction, laneOffset, out _) +
                direction * Mathf.Max(0f, borderOutset);
        }

        public static Rect TerminalSocketRect(Rect card, Vector2 towardSource,
            float laneOffset, float diameter, float borderOutset)
        {
            Vector2 center = TerminalSocketCenter(card, towardSource,
                laneOffset, borderOutset);
            Vector2 size = Vector2.one * Mathf.Max(0f, diameter);
            return new Rect(center - size * 0.5f, size);
        }

        public bool TryGetVisualSegment(int segment, out Vector2 start, out Vector2 end,
            out float visibleFraction, out Color lineColor)
            => TryGetVisualSegment(segment, out start, out end, out visibleFraction,
                out lineColor, out _);

        public bool TryGetVisualSegment(int segment, out Vector2 start, out Vector2 end,
            out float visibleFraction, out Color lineColor, out float lineWidth)
        {
            start = end = Vector2.zero;
            visibleFraction = 0f;
            lineColor = UsesSharedDisplay ? DisplayAccent : path != null ? path.color : Color.white;
            lineWidth = 0f;
            if (!establishedVisual || segment < 0 || segment >= starts.Length)
                return false;
            start = starts[segment];
            end = ends[segment];
            lineWidth = lineWidths[segment];
            float total = 0f;
            float before = 0f;
            for (int i = 0; i < starts.Length; i++)
            {
                float length = Vector2.Distance(starts[i], ends[i]);
                if (i < segment)
                    before += length;
                total += length;
            }
            float segmentLength = Vector2.Distance(start, end);
            if (segmentLength < 0.01f)
                return false;
            visibleFraction = Mathf.Clamp01((total * CurrentReveal() - before) /
                segmentLength);
            return true;
        }

        private float CurrentReveal()
        {
            if (state.IsComplete && routeAnimation) return lastReveal;
            if (routeAnimation)
            {
                float totalLength = 0, prefixLength = 0;
                for (int i = 0; i < starts.Length; i++)
                { float length = Vector2.Distance(starts[i], ends[i]); totalLength += length; if (i < visualSharedLinks) prefixLength += length; }
                float prefix = totalLength > .0001f ? prefixLength / totalLength : 0;
                if (!extendingRoute) return Mathf.Lerp(retractionStartReveal, Mathf.Min(retractionStartReveal, prefix),
                    Mathf.SmoothStep(0, 1, Mathf.Clamp01(routeAnimationElapsed / Mathf.Max(.0001f, retractionSeconds))));
                float extension = routeTravelSeconds <= retractionSeconds ? 1 : Mathf.Clamp01((routeAnimationElapsed - retractionSeconds) / (routeTravelSeconds - retractionSeconds));
                return Mathf.Lerp(prefix, 1, extension);
            }
            if (state.IsActive || state.IsComplete &&
                (state.IsNormalDeparture ||
                 state.TerminationPhase == Lifecycle.Active)) return 1f;
            float total = 0f, visible = 0f;
            for (int i = 0; i < starts.Length; i++)
            {
                float length = Vector2.Distance(starts[i], ends[i]);
                total += length;
                if (i < state.ReachedRouteNodeIndex) visible += length;
                else if (i == state.ReachedRouteNodeIndex)
                    visible += length * Mathf.Clamp01(state.LinkTravelElapsedSeconds /
                        state.TravelSecondsPerLink);
            }
            return total > 0f ? visible / total : 0f;
        }

        public void SetLayout(float[] offsets, float[] widths)
            => SetLayout(offsets, offsets, widths);

        public void SetLayout(float[] nextStarts, float[] nextEnds, float[] widths,
            float maximumSocketDiameter = float.PositiveInfinity)
        {
            RefreshOperationRoute();
            if (terminalMarker != null && !terminalMarker.FitsSocketNeighbors)
                terminalMarker.SetMaximumDiameter(maximumSocketDiameter, relayoutSeconds);
            if (targetStartOffsets == null)
            {
                startOffsets = (float[])nextStarts.Clone();
                endOffsets = (float[])nextEnds.Clone();
                lineWidths = (float[])widths.Clone();
                if (retainedStarts != null)
                    for (int i = 0; i < retainedStarts.Length; i++)
                    { startOffsets[i] = retainedStarts[i]; endOffsets[i] = retainedEnds[i]; lineWidths[i] = retainedWidths[i]; }
                retainedStarts = retainedEnds = retainedWidths = null;
                fromStartOffsets = (float[])startOffsets.Clone();
                fromEndOffsets = (float[])endOffsets.Clone();
                fromWidths = (float[])lineWidths.Clone();
            }
            else
            {
                bool changed = false;
                for (int i = 0; i < nextStarts.Length; i++)
                    changed |= Mathf.Abs(targetStartOffsets[i] - nextStarts[i]) > 0.001f ||
                        Mathf.Abs(targetEndOffsets[i] - nextEnds[i]) > 0.001f ||
                        Mathf.Abs(targetWidths[i] - widths[i]) > 0.001f;
                if (!changed)
                    return;
                fromStartOffsets = (float[])startOffsets.Clone();
                fromEndOffsets = (float[])endOffsets.Clone();
                fromWidths = (float[])lineWidths.Clone();
            }
            targetStartOffsets = (float[])nextStarts.Clone();
            targetEndOffsets = (float[])nextEnds.Clone();
            targetWidths = (float[])widths.Clone();
            relayoutElapsed = 0f;
        }

        private void LateUpdate() => AdvanceVisual(UsesSharedDisplay && gameplayDisplay != null ? gameplayDisplay.FrameDelta : Time.deltaTime);

        // Shared by Play Mode and the deterministic geometry witness.
        public void AdvanceVisual(float deltaSeconds)
        {
            if (state == null || networkArea == null || (!UsesSharedDisplay && path == null) || state.IsPending || startOffsets == null)
                return;

            if (routeAnimation && !state.IsComplete) routeAnimationElapsed += deltaSeconds;
            if (terminalMarker != null)
                terminalMarker.AdvanceVisual(deltaSeconds);

            if (!state.IsComplete)
            {
                relayoutElapsed += deltaSeconds;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(relayoutElapsed / relayoutSeconds));
                for (int i = 0; i < startOffsets.Length; i++)
                {
                    startOffsets[i] = Mathf.Lerp(fromStartOffsets[i], targetStartOffsets[i], t);
                    endOffsets[i] = Mathf.Lerp(fromEndOffsets[i], targetEndOffsets[i], t);
                    lineWidths[i] = Mathf.Lerp(fromWidths[i], targetWidths[i], t);
                }
            }

            if (!establishedVisual)
            {
                establishedVisual = true;
            }

            if (state.IsComplete)
            {
                if (UsesSharedDisplay) _ = DisplayState;
                else departureElapsed += deltaSeconds;
                bool failure = !state.IsNormalDeparture;
                if (failure) failureElapsed += deltaSeconds;
                if (UsesSharedDisplay ? departureElapsed >= sharedDisplay.style.DepartureDuration(DisplayState.resolution) :
                    failure ? failureElapsed >= failureFlashHalfCycleSeconds * 4f : departureElapsed >= departureSeconds)
                {
                    if (terminalMarker != null)
                        terminalMarker.gameObject.SetActive(false);
                    Destroy(gameObject);
                    return;
                }
            }
            for (int i = 0; i < starts.Length; i++)
            {
                Vector2 a = networkArea.InverseTransformPoint(PresentationRoute[i].Node.transform.position);
                Vector2 b = networkArea.InverseTransformPoint(PresentationRoute[i + 1].Node.transform.position);
                Vector2 direction = (b - a).normalized;
                starts[i] = EdgeGeometry.CardUnderlap(PresentationRoute[i].Node,
                    networkArea, direction, startOffsets[i], lineWidths[i]);
                ends[i] = EdgeGeometry.CardUnderlap(PresentationRoute[i + 1].Node,
                    networkArea, -direction, -endOffsets[i], lineWidths[i]);
            }
            float reveal = CurrentReveal(); lastReveal = reveal;
            float fade = state.IsComplete && state.IsNormalDeparture ?
                1f - Mathf.Clamp01(departureElapsed / departureSeconds) : 1f;
            LoadPerformance performance = state.Performance;
            if (UsesSharedDisplay)
            {
                if (gameplayDisplay == null) return;
                sharedDisplay.path.SetGeometry(starts, ends, lineWidths);
                sharedDisplay.path.SetVisibility(reveal, !IsRetractingRoute && (state.IsDemanding || state.IsComplete && state.TerminationPhase == Lifecycle.Active));
                sharedDisplay.path.SetProbe(probeSegment, probeCenter, probeAlong, probeHalfSize, probeTint);
                sharedDisplay.Render(DisplayState, gameplayDisplay.heartbeat);
                fade = sharedDisplay.style.Opacity(DisplayState);
            }
            else
            {
                Color tint = state.IsComplete && !state.IsNormalDeparture ?
                    DisplayAccent : state.IsTraversing ? identityColor : BeamColorFor(performance);
                tint.a *= fade;
                if (path.color != tint) path.color = tint;
                path.SetPath(starts, ends, lineWidths, reveal);
                path.SetProbe(probeSegment, probeCenter, probeAlong, probeHalfSize, probeTint);
            }
            if (terminalMarker != null)
            {
                bool reachedTarget = reveal >= 1f && (!extendingRoute || !state.IsRerouting);
                terminalMarker.gameObject.SetActive(reachedTarget);
                if (reachedTarget)
                {
                    int last = ends.Length - 1;
                    NodeView target = PresentationRoute[PresentationRoute.Length - 1].Node;
                    Vector2 from = networkArea.InverseTransformPoint(
                        PresentationRoute[last].Node.transform.position);
                    Vector2 to = networkArea.InverseTransformPoint(target.transform.position);
                    Vector2 towardSource = (from - to).normalized;
                    Vector2 socket = TerminalSocketCenter(EdgeGeometry.CardRect(target,
                        networkArea), towardSource, -endOffsets[last],
                        terminalMarker.BorderOutset);
                    terminalMarker.rectTransform.position = networkArea.TransformPoint(socket);
                    Color markerColor = UsesSharedDisplay ? DisplayAccent : state.IsComplete && !state.IsNormalDeparture ?
                        DisplayAccent : hasIdentityColor ? identityColor : healthyColor;
                    markerColor.a *= fade;
                    terminalMarker.SetAccent(markerColor);
                }
            }
            if (UsesSharedDisplay) return;
            float spacing = state.IsActive && !state.IsTransitioning ?
                PulseSpacingFor(state.DesiredLoad) : 0f;
            bool interrupted = performance == LoadPerformance.Slowed &&
                Mathf.Repeat(Time.unscaledTime, 0.55f) >= 0.32f;
            int pulseDirection = DirectionFor(state.CurrentOperation);
            if (spacing > 0f && pulseDirection != 0 && performance != LoadPerformance.Stalled && !interrupted)
                pulseTravel += deltaSeconds * pulseSpeedPixelsPerSecond;
            float phase = spacing > 0f && pulseDirection != 0 ?
                Mathf.Repeat(pulseTravel * pulseDirection, spacing) : 0f;
            float opacity = performance == LoadPerformance.Stalled ? 0.2f :
                interrupted ? 0f : 0.9f;
            path.SetActivity(spacing, phase, opacity);
        }

        public void ReleaseTerminalMarker()
        {
            if (terminalMarker == null) return;
            TerminalMarkerStyle marker = terminalMarker;
            terminalMarker = null;
            marker.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(marker.gameObject);
            else DestroyImmediate(marker.gameObject);
        }

        private void OnDestroy()
        {
            ReleaseTerminalMarker();
            Destroyed?.Invoke(state);
        }

    }
}
