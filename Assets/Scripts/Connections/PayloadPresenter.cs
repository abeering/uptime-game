using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Transient geometry only; all timing, route choices, and outcomes live in PayloadDirector.
    public sealed class PayloadPresenter : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform pathLayer;
        [SerializeField] private RectTransform markerLayer;
        [SerializeField] private TerminalMarkerStyle socketPrefab;
        [SerializeField] private PayloadBodyView bodyPrefab;
        [SerializeField] private PathGraphic lineStyle;
        [SerializeField, Min(1f)] private float lineWidth = 3f;

        private struct LaneLayout
        {
            public float StartOffset;
            public float EndOffset;
            public float Width;
        }

        private sealed class Visual
        {
            public PathGraphic Line;
            public TerminalMarkerStyle Socket;
            public PayloadBodyView Traveler;
            public int Hop;
            public bool HasPose;
            public Vector2 Start;
            public Vector2 End;
            public Vector2 SocketPoint;
        }
        private readonly Dictionary<PayloadState, Visual> visuals = new();
        private readonly Dictionary<PayloadState, LaneLayout> layouts = new();

        public void SetLayout(PayloadState state, float startOffset, float endOffset,
            float width)
        {
            layouts[state] = new LaneLayout { StartOffset = startOffset,
                EndOffset = endOffset, Width = width };
        }

        public bool TryGetVisualSegment(PayloadState state, out Vector2 start,
            out Vector2 end)
        {
            if (visuals.TryGetValue(state, out Visual visual) && visual.HasPose)
            {
                start = visual.Start;
                end = visual.End;
                return true;
            }
            start = end = Vector2.zero;
            return false;
        }

        private void LateUpdate() { if (Application.isPlaying) RenderNow(); }

        public void RenderNow()
        {
            if (host?.Payloads == null || networkArea == null || pathLayer == null ||
                markerLayer == null || socketPrefab == null || bodyPrefab == null ||
                lineStyle == null) return;
            var active = new HashSet<PayloadState>();
            foreach (PayloadState state in host.Payloads.Payloads)
            {
                if (state.Phase == PayloadPhase.Resolved) continue;
                active.Add(state);
                if (!visuals.TryGetValue(state, out Visual visual))
                {
                    visual = CreateVisual(state);
                    visuals.Add(state, visual);
                }
                if (!layouts.TryGetValue(state, out LaneLayout layout)) continue;
                Vector2 a = networkArea.InverseTransformPoint(state.CurrentNode.transform.position);
                Vector2 b = networkArea.InverseTransformPoint(state.NextNode.transform.position);
                Vector2 direction = (b - a).normalized;
                float thinWidth = Mathf.Min(Mathf.Max(1f, lineWidth), layout.Width);
                Vector2 start = EdgeGeometry.CardUnderlap(state.CurrentNode, networkArea,
                    direction, layout.StartOffset, thinWidth);
                Vector2 end = EdgeGeometry.CardUnderlap(state.NextNode, networkArea,
                    -direction, -layout.EndOffset, thinWidth);
                Vector2 socket = LogicalPathView.TerminalSocketCenter(
                    EdgeGeometry.CardRect(state.NextNode, networkArea), -direction,
                    -layout.EndOffset, visual.Socket.BorderOutset);
                if (!visual.HasPose || visual.Hop != state.Hop)
                {
                    visual.Start = start;
                    visual.End = end;
                    visual.SocketPoint = socket;
                    visual.Hop = state.Hop;
                    visual.HasPose = true;
                }
                else
                {
                    float blend = 1f - Mathf.Exp(-Mathf.Max(0f, Time.unscaledDeltaTime) / .22f);
                    visual.Start = Vector2.Lerp(visual.Start, start, blend);
                    visual.End = Vector2.Lerp(visual.End, end, blend);
                    visual.SocketPoint = Vector2.Lerp(visual.SocketPoint, socket, blend);
                }
                start = visual.Start;
                end = visual.End;
                float reveal = state.Phase == PayloadPhase.Establishing ? state.Progress : 1f;
                visual.Line.color = host.Payloads.PayloadColor;
                visual.Line.SetPath(new[] { start }, new[] { end },
                    new[] { thinWidth }, reveal);
                visual.Socket.gameObject.SetActive(true);
                visual.Socket.rectTransform.anchoredPosition = visual.SocketPoint;
                bool traversing = state.Phase == PayloadPhase.Traversing;
                visual.Traveler.gameObject.SetActive(traversing);
                Vector2 position = traversing ? Vector2.Lerp(start, visual.SocketPoint,
                    state.Progress) : Vector2.Lerp(start, visual.SocketPoint, reveal);
                visual.Traveler.rectTransform.anchoredPosition = position;
            }
            var removed = new List<PayloadState>();
            foreach (var pair in visuals)
                if (!active.Contains(pair.Key))
                {
                    DestroyVisual(pair.Value);
                    removed.Add(pair.Key);
                }
            foreach (PayloadState state in removed)
            {
                visuals.Remove(state);
                layouts.Remove(state);
            }
        }

        private Visual CreateVisual(PayloadState state)
        {
            var lineObject = new GameObject("Payload " + state.Id + " hop", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(PathGraphic));
            var lineRect = (RectTransform)lineObject.transform;
            lineRect.SetParent(pathLayer, false);
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.one;
            lineRect.offsetMin = lineRect.offsetMax = Vector2.zero;
            PathGraphic line = lineObject.GetComponent<PathGraphic>();
            line.material = lineStyle.material;
            line.raycastTarget = false;
            line.SetActivity(0f, 0f, 0f);
            Color accent = host.Payloads.PayloadColor;
            TerminalMarkerStyle socket = Instantiate(socketPrefab, markerLayer, false);
            socket.name = "Payload " + state.Id + " next-hop socket";
            socket.SetMaximumDiameter(17f, .01f);
            socket.SetAccent(accent);
            PayloadBodyView traveler = Instantiate(bodyPrefab, markerLayer, false);
            traveler.name = "Payload " + state.Id + " traveler";
            traveler.Bind(state.Id, accent);
            return new Visual { Line = line, Socket = socket, Traveler = traveler };
        }

        private static void DestroyVisual(Visual visual)
        {
            Remove(visual.Line);
            Remove(visual.Socket);
            Remove(visual.Traveler);
        }

        private static void Remove(Component component)
        {
            if (component == null) return;
            if (Application.isPlaying) Destroy(component.gameObject);
            else DestroyImmediate(component.gameObject);
        }

        private void OnDisable()
        {
            foreach (Visual visual in visuals.Values) DestroyVisual(visual);
            visuals.Clear();
            layouts.Clear();
        }
    }
}
