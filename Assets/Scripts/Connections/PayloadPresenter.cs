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
        [SerializeField] private IdentityPillView idPrefab;
        [SerializeField] private PathGraphic lineStyle;
        [SerializeField, Min(1f)] private float lineWidth = 3f;
        [SerializeField] private float laneOffset = 9f;

        private sealed class Visual
        {
            public PathGraphic Line;
            public TerminalMarkerStyle Socket;
            public TerminalMarkerStyle Traveler;
            public IdentityPillView Id;
            public int Hop;
            public bool HasPose;
            public Vector2 Start;
            public Vector2 End;
        }
        private readonly Dictionary<PayloadState, Visual> visuals = new();

        private void LateUpdate() { if (Application.isPlaying) RenderNow(); }

        public void RenderNow()
        {
            if (host?.Payloads == null || networkArea == null || pathLayer == null ||
                markerLayer == null || socketPrefab == null || idPrefab == null ||
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
                if (!TrySegment(state, out Vector2 start, out Vector2 end)) continue;
                if (!visual.HasPose || visual.Hop != state.Hop)
                {
                    visual.Start = start;
                    visual.End = end;
                    visual.Hop = state.Hop;
                    visual.HasPose = true;
                }
                else
                {
                    float blend = 1f - Mathf.Exp(-Mathf.Max(0f, Time.unscaledDeltaTime) / .22f);
                    visual.Start = Vector2.Lerp(visual.Start, start, blend);
                    visual.End = Vector2.Lerp(visual.End, end, blend);
                }
                start = visual.Start;
                end = visual.End;
                float reveal = state.Phase == PayloadPhase.Establishing ? state.Progress : 1f;
                visual.Line.color = host.Payloads.PayloadColor;
                visual.Line.SetPath(new[] { start }, new[] { end },
                    new[] { Mathf.Max(1f, lineWidth) }, reveal);
                visual.Socket.gameObject.SetActive(true);
                visual.Socket.rectTransform.anchoredPosition = end;
                bool traversing = state.Phase == PayloadPhase.Traversing;
                visual.Traveler.gameObject.SetActive(traversing);
                Vector2 position = traversing ? Vector2.Lerp(start, end, state.Progress) :
                    Vector2.Lerp(start, end, reveal);
                visual.Traveler.rectTransform.anchoredPosition = position;
                Vector2 direction = (end - start).normalized;
                Vector2 side = new Vector2(-direction.y, direction.x);
                visual.Id.SetPose(Vector2.Lerp(start, end,
                    state.Phase == PayloadPhase.Establishing ?
                        Mathf.Max(.08f, reveal * .5f) : .5f) + side * 22f, direction);
            }
            var removed = new List<PayloadState>();
            foreach (var pair in visuals)
                if (!active.Contains(pair.Key))
                {
                    DestroyVisual(pair.Value);
                    removed.Add(pair.Key);
                }
            foreach (PayloadState state in removed) visuals.Remove(state);
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
            TerminalMarkerStyle traveler = Instantiate(socketPrefab, markerLayer, false);
            traveler.name = "Payload " + state.Id + " traveler";
            traveler.SetMaximumDiameter(21f, .01f);
            traveler.SetAccent(accent);
            IdentityPillView id = Instantiate(idPrefab, markerLayer, false);
            id.name = "Payload " + state.Id + " identity";
            id.Bind(state.Id, accent);
            return new Visual { Line = line, Socket = socket, Traveler = traveler, Id = id };
        }

        private bool TrySegment(PayloadState state, out Vector2 start, out Vector2 end)
        {
            foreach (PhysicalLinkReference reference in host.PhysicalLinks)
            {
                PhysicalLinkView link = reference?.View;
                if (link == null) continue;
                bool forward = link.NodeA == state.CurrentNode && link.NodeB == state.NextNode;
                if (!forward && !(link.NodeB == state.CurrentNode &&
                    link.NodeA == state.NextNode)) continue;
                link.GetRenderedSpine(out start, out end);
                if (!forward) (start, end) = (end, start);
                Vector2 direction = (end - start).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                // Payloads sharing a physical hop need distinct, stable visible lanes.
                // This does not change logical-connection port ownership or simulation.
                int count = 0;
                int rank = 0;
                foreach (PayloadState other in host.Payloads.Payloads)
                {
                    if (other.Phase == PayloadPhase.Resolved ||
                        !((other.CurrentNode == link.NodeA && other.NextNode == link.NodeB) ||
                          (other.CurrentNode == link.NodeB && other.NextNode == link.NodeA)))
                        continue;
                    if (string.CompareOrdinal(other.Id, state.Id) < 0) rank++;
                    count++;
                }
                float offset = (laneOffset + (rank - (count - 1) * .5f) * 24f) *
                    (forward ? 1f : -1f);
                start += normal * offset;
                end += normal * offset;
                return true;
            }
            start = end = Vector2.zero;
            return false;
        }

        private static void DestroyVisual(Visual visual)
        {
            Remove(visual.Line);
            Remove(visual.Socket);
            Remove(visual.Traveler);
            Remove(visual.Id);
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
        }
    }
}
