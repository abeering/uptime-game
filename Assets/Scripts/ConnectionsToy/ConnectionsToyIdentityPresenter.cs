using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // A separate overlay: pills may cover lines, but neither packing nor cycling changes line geometry.
    public sealed class ConnectionsToyIdentityPresenter : MonoBehaviour
    {
        public struct LiveLine
        {
            public string Id;
            public Vector2 TargetStart;
            public Vector2 TargetEnd;
            public Vector2 VisualStart;
            public Vector2 VisualEnd;
            public Color Color;
            public float VisibleFraction;

            public LiveLine(string id, Vector2 targetStart, Vector2 targetEnd,
                Vector2 visualStart, Vector2 visualEnd, Color color, float visibleFraction)
            {
                Id = id;
                TargetStart = targetStart;
                TargetEnd = targetEnd;
                VisualStart = visualStart;
                VisualEnd = visualEnd;
                Color = color;
                VisibleFraction = visibleFraction;
            }
        }

        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform pillLayer;
        [SerializeField] private ConnectionsToyIdentityPillView pillPrefab;
        [Header("Pill packing (UI pixels)")]
        [SerializeField, Min(0f)] private float minimumPillGap = 6f;
        [SerializeField, Min(4f)] private float staggerStep = 20f;
        [SerializeField, Min(0f)] private float endInset = 12f;
        [Header("Identity rotation")]
        [SerializeField] private bool automaticCycling = true;
        [SerializeField, Min(0.5f)] private float automaticCycleSeconds = 2.6f;
        [SerializeField, Min(0.5f)] private float minimumDwellSeconds = 1.2f;

        private sealed class EdgeState
        {
            public ConnectionsToyPhysicalLinkView Link;
            public readonly List<LiveLine> Lines = new List<LiveLine>();
            public readonly List<ConnectionsToyIdentityPillView> Pills = new List<ConnectionsToyIdentityPillView>();
            public readonly List<ConnectionsToyIdentityPacking.Lane> PlanLanes = new List<ConnectionsToyIdentityPacking.Lane>();
            public List<List<ConnectionsToyIdentityPacking.Placement>> Schedule;
            public Rect[] Obstacles;
            public Vector2 PillSize;
            public float Gap;
            public float Step;
            public float Inset;
            public int Cycle;
            public float NextCycleAt;
            public bool Seen;
            public bool WarnedNoRoom;
        }

        private readonly Dictionary<ConnectionsToyPhysicalLinkView, EdgeState> edges =
            new Dictionary<ConnectionsToyPhysicalLinkView, EdgeState>();

        public RectTransform NetworkArea => networkArea;
        public Vector2 PillSize => pillPrefab != null ? pillPrefab.Size : Vector2.zero;
        public float MinimumPillGap => Mathf.Max(0f, minimumPillGap);
        public float StaggerStep => Mathf.Max(4f, staggerStep);
        public float EndInset => Mathf.Max(0f, endInset);
        public bool AutomaticCycling => automaticCycling;

        public void BeginFrame()
        {
            foreach (EdgeState state in edges.Values)
                state.Seen = false;
        }

        public void PresentEdge(ConnectionsToyPhysicalLinkView link, List<LiveLine> liveLines)
        {
            if (link == null || liveLines == null || liveLines.Count == 0 ||
                networkArea == null || pillLayer == null || pillPrefab == null)
                return;
            if (!edges.TryGetValue(link, out EdgeState state))
            {
                state = new EdgeState { Link = link };
                edges.Add(link, state);
            }
            state.Seen = true;
            state.Lines.Clear();
            state.Lines.AddRange(liveLines);
            state.Lines.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));

            var lanes = new List<ConnectionsToyIdentityPacking.Lane>();
            foreach (LiveLine line in state.Lines)
                lanes.Add(new ConnectionsToyIdentityPacking.Lane(line.Id, line.TargetStart, line.TargetEnd));
            Rect[] obstacles = Obstacles(link);
            if (NeedsPlan(state, lanes, obstacles))
            {
                foreach (ConnectionsToyIdentityPillView pill in state.Pills)
                    if (pill != null)
                        DestroyView(pill.gameObject);
                state.Pills.Clear();
                state.PlanLanes.Clear();
                state.PlanLanes.AddRange(lanes);
                state.Obstacles = obstacles;
                state.PillSize = PillSize;
                state.Gap = MinimumPillGap;
                state.Step = StaggerStep;
                state.Inset = EndInset;
                state.Schedule = ConnectionsToyIdentityPacking.BuildSchedule(lanes, PillSize,
                    MinimumPillGap, StaggerStep, EndInset, obstacles);
                state.Cycle = 0;
                state.NextCycleAt = Clock() + Dwell(state);
                foreach (LiveLine line in state.Lines)
                {
                    ConnectionsToyIdentityPillView pill = Instantiate(pillPrefab, pillLayer, false);
                    pill.name = $"{link.LinkId} identity {line.Id}";
                    if (!Application.isPlaying)
                        pill.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                    pill.Bind(line.Id, line.Color);
                    pill.gameObject.SetActive(false);
                    state.Pills.Add(pill);
                }
                if (state.Schedule.Count == 0 && !state.WarnedNoRoom)
                {
                    Debug.LogWarning($"No readable identity pill fits on {link.LinkId}; increase edge length or reduce pill size.", link);
                    state.WarnedNoRoom = true;
                }
                else if (state.Schedule.Count > 0)
                    state.WarnedNoRoom = false;
            }
            Apply(state);
        }

        public void EndFrame()
        {
            var removed = new List<ConnectionsToyPhysicalLinkView>();
            foreach (KeyValuePair<ConnectionsToyPhysicalLinkView, EdgeState> pair in edges)
                if (!pair.Value.Seen)
                    removed.Add(pair.Key);
            foreach (ConnectionsToyPhysicalLinkView link in removed)
            {
                foreach (ConnectionsToyIdentityPillView pill in edges[link].Pills)
                    if (pill != null)
                        DestroyView(pill.gameObject);
                edges.Remove(link);
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
                Tick();
        }

        public void Tick()
        {
            if (!automaticCycling)
                return;
            float now = Clock();
            foreach (EdgeState state in edges.Values)
            {
                if (state.Schedule == null || state.Schedule.Count <= 1 || now < state.NextCycleAt)
                    continue;
                Advance(state, now);
            }
        }

        public void CycleCongestedEdges()
        {
            float now = Clock();
            foreach (EdgeState state in edges.Values)
                if (state.Schedule != null && state.Schedule.Count > 1)
                    Advance(state, now);
        }

        private void Advance(EdgeState state, float now)
        {
            state.Cycle = (state.Cycle + 1) % state.Schedule.Count;
            state.NextCycleAt = now + Dwell(state);
            Apply(state);
        }

        private float Dwell(EdgeState state)
        {
            int visible = state.Schedule != null && state.Schedule.Count > 0 ?
                Mathf.Max(1, state.Schedule[state.Cycle].Count) : 1;
            float density = (float)state.Lines.Count / visible;
            return Mathf.Max(minimumDwellSeconds, automaticCycleSeconds / Mathf.Sqrt(density));
        }

        private void Apply(EdgeState state)
        {
            if (state.Schedule == null || state.Schedule.Count == 0)
                return;
            List<ConnectionsToyIdentityPacking.Placement> visible = state.Schedule[state.Cycle];
            var displayedBounds = new List<Rect>();
            for (int i = 0; i < state.Pills.Count; i++)
            {
                bool selected = false;
                float fraction = 0f;
                foreach (ConnectionsToyIdentityPacking.Placement placement in visible)
                    if (placement.LaneIndex == i)
                    {
                        selected = true;
                        fraction = placement.Fraction;
                        break;
                    }
                LiveLine line = state.Lines[i];
                bool show = selected && line.VisibleFraction + 0.001f >= fraction;
                ConnectionsToyIdentityPillView pill = state.Pills[i];
                Vector2 position = Vector2.Lerp(line.VisualStart, line.VisualEnd, fraction);
                Rect bounds = new Rect(position - state.PillSize * 0.5f, state.PillSize);
                if (show)
                {
                    foreach (Rect other in displayedBounds)
                        if (OverlapsWithGap(bounds, other, state.Gap))
                        {
                            show = false;
                            break;
                        }
                }
                if (show)
                    foreach (Rect obstacle in state.Obstacles)
                        if (OverlapsWithGap(bounds, obstacle, state.Gap))
                        {
                            show = false;
                            break;
                        }
                if (pill.gameObject.activeSelf != show)
                    pill.gameObject.SetActive(show);
                if (show)
                {
                    displayedBounds.Add(bounds);
                    pill.SetPosition(position);
                    pill.SetLineColor(line.Color);
                }
            }
        }

        private static bool OverlapsWithGap(Rect a, Rect b, float gap)
        {
            return a.xMin < b.xMax + gap && a.xMax + gap > b.xMin &&
                a.yMin < b.yMax + gap && a.yMax + gap > b.yMin;
        }

        private bool NeedsPlan(EdgeState state, List<ConnectionsToyIdentityPacking.Lane> lanes, Rect[] obstacles)
        {
            if (state.Schedule == null || state.PlanLanes.Count != lanes.Count || state.PillSize != PillSize ||
                !Mathf.Approximately(state.Gap, MinimumPillGap) ||
                !Mathf.Approximately(state.Step, StaggerStep) || !Mathf.Approximately(state.Inset, EndInset) ||
                state.Obstacles == null || state.Obstacles.Length != obstacles.Length)
                return true;
            for (int i = 0; i < obstacles.Length; i++)
                if ((state.Obstacles[i].center - obstacles[i].center).sqrMagnitude > 0.25f ||
                    (state.Obstacles[i].size - obstacles[i].size).sqrMagnitude > 0.25f)
                    return true;
            for (int i = 0; i < lanes.Count; i++)
                if (state.PlanLanes[i].Id != lanes[i].Id ||
                    (state.PlanLanes[i].Start - lanes[i].Start).sqrMagnitude > 0.25f ||
                    (state.PlanLanes[i].End - lanes[i].End).sqrMagnitude > 0.25f)
                    return true;
            return false;
        }

        private Rect[] Obstacles(ConnectionsToyPhysicalLinkView link)
        {
            return link.ResourceLabel != null ? new[] { Bounds(link.NodeA.transform),
                Bounds(link.NodeB.transform), Bounds(link.ResourceLabel) } :
                new[] { Bounds(link.NodeA.transform), Bounds(link.NodeB.transform) };
        }

        private Rect Bounds(Transform target)
        {
            var corners = new Vector3[4];
            ((RectTransform)target).GetWorldCorners(corners);
            Vector2 min = networkArea.InverseTransformPoint(corners[0]);
            Vector2 max = min;
            for (int i = 1; i < 4; i++)
            {
                Vector2 point = networkArea.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static float Clock() => Time.realtimeSinceStartup;

        private static void DestroyView(GameObject view)
        {
            view.SetActive(false);
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(view);
            else
                UnityEngine.Object.DestroyImmediate(view);
        }

        private void OnDisable()
        {
            foreach (EdgeState state in edges.Values)
                foreach (ConnectionsToyIdentityPillView pill in state.Pills)
                    if (pill != null)
                        DestroyView(pill.gameObject);
            edges.Clear();
        }
    }
}
