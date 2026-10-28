using System;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public sealed class LogicalPathView : MonoBehaviour
    {
        [SerializeField] private PathGraphic path;
        [SerializeField] private Image terminalMarkerPrefab;
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
        private Image terminalMarker;
        private Vector2 markerPadding;

        public event Action<LogicalConnectionState> Destroyed;
        public Image TerminalMarker => terminalMarker;
        public Color DisplayAccent => state != null && state.IsComplete &&
            !state.IsNormalDeparture &&
            Mathf.FloorToInt(failureElapsed / failureFlashHalfCycleSeconds) % 2 == 0 ?
            failureColor : identityColor;

        public float PulseSpacingFor(float requestedLoad) =>
            Mathf.Max(18f, pulseSpacingAtOneLoad /
                (1f + spacingReductionPerLoad * Mathf.Max(0f, requestedLoad - 1f)));
        public static int DirectionFor(OperationKind operation) =>
            operation == OperationKind.Read ? -1 : operation == OperationKind.Write ? 1 : 0;

        public void SetIdentityColor(Color color)
        {
            identityColor = color;
            hasIdentityColor = true;
        }

        public Color BeamColorFor(LoadPerformance performance) =>
            performance == LoadPerformance.AtSpeed ?
                (hasIdentityColor ? identityColor : healthyColor) :
            performance == LoadPerformance.Slowed ? constrainedColor : stalledColor;

        public void Bind(LogicalConnectionState connection, RectTransform area, RectTransform markerLayer)
        {
            state = connection;
            networkArea = area;
            int count = connection.RouteLinks.Length;
            starts = new Vector2[count];
            ends = new Vector2[count];
            path.raycastTarget = false;
            path.SetPath(starts, ends, new float[count], 0f);
            if (terminalMarkerPrefab != null && markerLayer != null && connection.TargetNode != null)
            {
                terminalMarker = Instantiate(terminalMarkerPrefab, markerLayer, false);
                terminalMarker.name = "Terminal marker " + connection.ConnectionId;
                terminalMarker.raycastTarget = false;
                TerminalMarkerStyle style = terminalMarker.GetComponent<TerminalMarkerStyle>();
                markerPadding = style != null ? style.Padding : Vector2.zero;
                terminalMarker.gameObject.SetActive(false);
            }
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

        // The beam is a strip perpendicular to its direction. Its intersection with
        // a card edge is wider at oblique angles; clip that span at the card corners.
        // Prefab padding is then added in screen X and Y, independently of edge side.
        public static Rect TerminalMarkerRect(Rect card, Vector2 towardSource,
            float laneOffset, float lineWidth, Vector2 padding)
        {
            Vector2 direction = towardSource.normalized;
            Vector2 point = TerminalBorderPoint(card, direction, laneOffset,
                out bool verticalSide);
            float normalComponent = Mathf.Abs(verticalSide ? direction.x : direction.y);
            float halfSpan = Mathf.Max(0f, lineWidth) * 0.5f /
                Mathf.Max(0.001f, normalComponent);
            float coordinate = verticalSide ? point.y : point.x;
            float low = verticalSide ? card.yMin : card.xMin;
            float high = verticalSide ? card.yMax : card.xMax;
            float start = Mathf.Clamp(coordinate - halfSpan, low, high);
            float end = Mathf.Clamp(coordinate + halfSpan, low, high);
            float center = (start + end) * 0.5f;
            float span = end - start;
            Vector2 size = verticalSide ?
                new Vector2(Mathf.Max(0f, padding.x), span + Mathf.Max(0f, padding.y)) :
                new Vector2(span + Mathf.Max(0f, padding.x), Mathf.Max(0f, padding.y));
            Vector2 position = verticalSide ? new Vector2(point.x, center) :
                new Vector2(center, point.y);
            return new Rect(position - size * 0.5f, size);
        }

        public bool TryGetVisualSegment(int segment, out Vector2 start, out Vector2 end,
            out float visibleFraction, out Color lineColor)
        {
            start = end = Vector2.zero;
            visibleFraction = 0f;
            lineColor = path != null ? path.color : Color.white;
            if (!establishedVisual || segment < 0 || segment >= starts.Length)
                return false;
            start = starts[segment];
            end = ends[segment];
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

        public void SetLayout(float[] nextStarts, float[] nextEnds, float[] widths)
        {
            if (targetStartOffsets == null)
            {
                startOffsets = (float[])nextStarts.Clone();
                endOffsets = (float[])nextEnds.Clone();
                lineWidths = (float[])widths.Clone();
                fromStartOffsets = (float[])nextStarts.Clone();
                fromEndOffsets = (float[])nextEnds.Clone();
                fromWidths = (float[])widths.Clone();
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

        private void LateUpdate() => AdvanceVisual(Time.deltaTime);

        // Shared by Play Mode and the deterministic geometry witness.
        public void AdvanceVisual(float deltaSeconds)
        {
            if (state == null || networkArea == null || path == null || state.IsPending || startOffsets == null)
                return;

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
                departureElapsed += deltaSeconds;
                bool failure = !state.IsNormalDeparture;
                if (failure) failureElapsed += deltaSeconds;
                if (failure ? failureElapsed >= failureFlashHalfCycleSeconds * 4f :
                    departureElapsed >= departureSeconds)
                {
                    if (terminalMarker != null)
                        terminalMarker.gameObject.SetActive(false);
                    Destroy(gameObject);
                    return;
                }
            }
            float reveal = CurrentReveal();
            float fade = state.IsComplete && state.IsNormalDeparture ?
                1f - Mathf.Clamp01(departureElapsed / departureSeconds) : 1f;
            LoadPerformance performance = state.Performance;
            Color tint = state.IsComplete && !state.IsNormalDeparture ?
                DisplayAccent : state.IsConnecting ? identityColor : BeamColorFor(performance);
            tint.a *= fade;
            if (path.color != tint)
                path.color = tint;

            for (int i = 0; i < starts.Length; i++)
            {
                Vector2 a = networkArea.InverseTransformPoint(state.Definition.Route[i].Node.transform.position);
                Vector2 b = networkArea.InverseTransformPoint(state.Definition.Route[i + 1].Node.transform.position);
                Vector2 direction = (b - a).normalized;
                starts[i] = EdgeGeometry.CardUnderlap(state.Definition.Route[i].Node,
                    networkArea, direction, startOffsets[i], lineWidths[i]);
                ends[i] = EdgeGeometry.CardUnderlap(state.Definition.Route[i + 1].Node,
                    networkArea, -direction, -endOffsets[i], lineWidths[i]);
            }
            path.SetPath(starts, ends, lineWidths, reveal);
            if (terminalMarker != null)
            {
                bool reachedTarget = reveal >= 1f;
                terminalMarker.gameObject.SetActive(reachedTarget);
                if (reachedTarget)
                {
                    int last = ends.Length - 1;
                    NodeView target = state.TargetNode;
                    Vector2 from = networkArea.InverseTransformPoint(
                        state.Definition.Route[last].Node.transform.position);
                    Vector2 to = networkArea.InverseTransformPoint(target.transform.position);
                    Vector2 towardSource = (from - to).normalized;
                    Rect marker = TerminalMarkerRect(EdgeGeometry.CardRect(target, networkArea),
                        towardSource, -endOffsets[last], lineWidths[last], markerPadding);
                    terminalMarker.rectTransform.position = networkArea.TransformPoint(marker.center);
                    terminalMarker.rectTransform.sizeDelta = marker.size;
                    Color markerColor = state.IsComplete && !state.IsNormalDeparture ?
                        DisplayAccent : hasIdentityColor ? identityColor : healthyColor;
                    markerColor.a *= fade;
                    terminalMarker.color = markerColor;
                }
            }
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
            Image marker = terminalMarker;
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
