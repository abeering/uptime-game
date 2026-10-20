using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // The edge solver settles line spacing once; cycling then changes only the separate pill overlay.
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
            public float Width;

            public LiveLine(string id, Vector2 targetStart, Vector2 targetEnd,
                Vector2 visualStart, Vector2 visualEnd, Color color, float visibleFraction,
                float width)
            {
                Id = id;
                TargetStart = targetStart;
                TargetEnd = targetEnd;
                VisualStart = visualStart;
                VisualEnd = visualEnd;
                Color = color;
                VisibleFraction = visibleFraction;
                Width = width;
            }
        }

        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform pillLayer;
        [SerializeField] private ConnectionsToyIdentityPillView pillPrefab;
        [Header("Pill packing (UI pixels)")]
        [SerializeField, Min(0f)] private float minimumPillGap = 6f;
        [SerializeField, Min(4f)] private float staggerStep = 20f;
        [SerializeField, Min(0f)] private float endInset = 12f;
        [SerializeField, Min(0f)] private float pillToLineClearance = 1f;
        [Header("Identity rotation")]
        [SerializeField] private bool automaticCycling = true;
        [SerializeField, Min(0.5f)] private float automaticCycleSeconds = 2.6f;
        [SerializeField, Min(0.5f)] private float minimumDwellSeconds = 1.2f;

        private sealed class EdgeState
        {
            public ConnectionsToyPhysicalLinkView Link;
            public readonly List<LiveLine> Lines = new List<LiveLine>();
            public readonly List<ConnectionsToyIdentityPillView> Pills = new List<ConnectionsToyIdentityPillView>();
            public readonly List<string> PillIds = new List<string>();
            public ConnectionsToyIdentityPacking.Layout Layout;
            public ConnectionsToyIdentityPacking.PillBounds[] Obstacles;
            public Vector2 Start;
            public Vector2 End;
            public Vector2 PillSize;
            public float Gap;
            public float Step;
            public float Inset;
            public float Clearance;
            public float Budget;
            public float MaximumWidth;
            public float MinimumReadableWidth;
            public float MinimumGap;
            public int Count;
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
        public string StageDiagnostic
        {
            get
            {
                var lines = new List<string>();
                foreach (EdgeState state in edges.Values)
                    if (state.Layout != null)
                        lines.Add($"{state.Link.LinkId}: {StageLetter(state.Layout.Stage)} " +
                            $"{state.Layout.Stage} " +
                            $"({state.Count} lines, {VisibleCount(state)} pills)");
                lines.Sort(StringComparer.Ordinal);
                return lines.Count == 0 ? "No active edge" : string.Join(" | ", lines);
            }
        }

        private static string StageLetter(ConnectionsToyIdentityPacking.ReadabilityStage stage)
        {
            switch (stage)
            {
                case ConnectionsToyIdentityPacking.ReadabilityStage.Centered: return "A";
                case ConnectionsToyIdentityPacking.ReadabilityStage.Staggered: return "B";
                case ConnectionsToyIdentityPacking.ReadabilityStage.Overlaid: return "C";
                case ConnectionsToyIdentityPacking.ReadabilityStage.Rotating: return "D";
                default: return "—";
            }
        }

        public void BeginFrame()
        {
            foreach (EdgeState state in edges.Values)
                state.Seen = false;
        }

        public ConnectionsToyIdentityPacking.Layout ResolveLayout(ConnectionsToyPhysicalLinkView link,
            int count, float maximumLineWidth, float minimumReadableLineWidth,
            float minimumLineGap)
        {
            if (link == null || networkArea == null || pillPrefab == null)
                return null;
            if (!edges.TryGetValue(link, out EdgeState state))
            {
                state = new EdgeState { Link = link };
                edges.Add(link, state);
            }
            Vector2 a = networkArea.InverseTransformPoint(link.NodeA.transform.position);
            Vector2 b = networkArea.InverseTransformPoint(link.NodeB.transform.position);
            Vector2 direction = (b - a).normalized;
            Vector2 start = ConnectionsToyEdgeGeometry.CardEdge(link.NodeA, networkArea, direction);
            Vector2 end = ConnectionsToyEdgeGeometry.CardEdge(link.NodeB, networkArea, -direction);
            float budget = link.BundleWidth(networkArea);
            ConnectionsToyIdentityPacking.PillBounds[] obstacles = Obstacles(link);
            if (NeedsPlan(state, start, end, budget, count, maximumLineWidth,
                minimumReadableLineWidth, minimumLineGap, obstacles))
            {
                state.Start = start;
                state.End = end;
                state.Budget = budget;
                state.Count = count;
                state.MaximumWidth = maximumLineWidth;
                state.MinimumReadableWidth = minimumReadableLineWidth;
                state.MinimumGap = minimumLineGap;
                state.Obstacles = obstacles;
                state.PillSize = PillSize;
                state.Gap = MinimumPillGap;
                state.Step = StaggerStep;
                state.Inset = EndInset;
                state.Clearance = Mathf.Max(0f, pillToLineClearance);
                state.Layout = ConnectionsToyIdentityPacking.Solve(start, end, budget, count,
                    maximumLineWidth, minimumReadableLineWidth, minimumLineGap,
                    PillSize, MinimumPillGap,
                    StaggerStep, EndInset, state.Clearance, obstacles);
                state.Cycle = 0;
                state.NextCycleAt = Clock() + Dwell(state);
                if (state.Layout.Stage == ConnectionsToyIdentityPacking.ReadabilityStage.Unavailable &&
                    !state.WarnedNoRoom)
                {
                    Debug.LogWarning($"No full-size identity pill fits on {link.LinkId}.", link);
                    state.WarnedNoRoom = true;
                }
                else if (state.Layout.Stage != ConnectionsToyIdentityPacking.ReadabilityStage.Unavailable)
                    state.WarnedNoRoom = false;
            }
            return state.Layout;
        }

        public void PresentEdge(ConnectionsToyPhysicalLinkView link, List<LiveLine> liveLines)
        {
            if (link == null || liveLines == null || liveLines.Count == 0 ||
                networkArea == null || pillLayer == null || pillPrefab == null)
                return;
            if (!edges.TryGetValue(link, out EdgeState state) || state.Layout == null)
                return;
            state.Seen = true;
            state.Lines.Clear();
            state.Lines.AddRange(liveLines);
            state.Lines.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
            bool idsChanged = state.PillIds.Count != state.Lines.Count;
            if (!idsChanged)
                for (int i = 0; i < state.Lines.Count; i++)
                    if (state.PillIds[i] != state.Lines[i].Id)
                    {
                        idsChanged = true;
                        break;
                    }
            if (idsChanged)
            {
                var old = new Dictionary<string, ConnectionsToyIdentityPillView>();
                for (int i = 0; i < state.PillIds.Count; i++)
                    if (state.Pills[i] != null)
                        old.Add(state.PillIds[i], state.Pills[i]);
                state.Pills.Clear();
                state.PillIds.Clear();
                foreach (LiveLine line in state.Lines)
                {
                    if (!old.TryGetValue(line.Id, out ConnectionsToyIdentityPillView pill))
                    {
                        pill = Instantiate(pillPrefab, pillLayer, false);
                        if (!Application.isPlaying)
                            pill.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                    }
                    else
                        old.Remove(line.Id);
                    pill.name = $"{link.LinkId} identity {line.Id}";
                    pill.Bind(line.Id, line.Color);
                    state.Pills.Add(pill);
                    state.PillIds.Add(line.Id);
                }
                foreach (ConnectionsToyIdentityPillView pill in old.Values)
                    DestroyView(pill.gameObject);
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
                if (state.Layout == null || state.Layout.Schedule.Count <= 1 || now < state.NextCycleAt)
                    continue;
                Advance(state, now);
            }
        }

        public void CycleCongestedEdges()
        {
            float now = Clock();
            foreach (EdgeState state in edges.Values)
                if (state.Layout != null && state.Layout.Schedule.Count > 1)
                    Advance(state, now);
        }

        private void Advance(EdgeState state, float now)
        {
            state.Cycle = (state.Cycle + 1) % state.Layout.Schedule.Count;
            state.NextCycleAt = now + Dwell(state);
            Apply(state);
        }

        private float Dwell(EdgeState state)
        {
            int visible = Mathf.Max(1, VisibleCount(state));
            float density = Mathf.Max(1f, (float)state.Count / visible);
            return Mathf.Max(minimumDwellSeconds, automaticCycleSeconds / Mathf.Sqrt(density));
        }

        private static int VisibleCount(EdgeState state) =>
            state.Layout != null && state.Layout.Schedule.Count > 0 ?
                state.Layout.Schedule[state.Cycle].Count : 0;

        private void Apply(EdgeState state)
        {
            if (state.Layout == null || state.Layout.Schedule.Count == 0)
            {
                foreach (ConnectionsToyIdentityPillView pill in state.Pills)
                    if (pill != null)
                        pill.gameObject.SetActive(false);
                return;
            }
            List<ConnectionsToyIdentityPacking.Placement> visible = state.Layout.Schedule[state.Cycle];
            var displayed = new List<ConnectionsToyIdentityPacking.Placement>();
            var visualLanes = new List<ConnectionsToyIdentityPacking.Lane>();
            foreach (LiveLine line in state.Lines)
                visualLanes.Add(new ConnectionsToyIdentityPacking.Lane(line.Id,
                    line.VisualStart, line.VisualEnd, line.Width));
            bool avoidLines = state.Layout.Stage == ConnectionsToyIdentityPacking.ReadabilityStage.Centered ||
                state.Layout.Stage == ConnectionsToyIdentityPacking.ReadabilityStage.Staggered;
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
                float length = Vector2.Distance(line.VisualStart, line.VisualEnd);
                bool show = selected && length > 0.01f &&
                    line.VisibleFraction + 0.001f >= fraction + state.PillSize.x * 0.5f / length;
                ConnectionsToyIdentityPillView pill = state.Pills[i];
                ConnectionsToyIdentityPacking.PillBounds bounds =
                    ConnectionsToyIdentityPacking.Bounds(visualLanes[i], fraction, state.PillSize);
                if (show)
                    show = ConnectionsToyIdentityPacking.CanShow(bounds, i, visualLanes,
                        displayed, state.Obstacles, state.Gap, avoidLines, state.Clearance);
                if (pill.gameObject.activeSelf != show)
                    pill.gameObject.SetActive(show);
                if (show)
                {
                    displayed.Add(new ConnectionsToyIdentityPacking.Placement(i, fraction, bounds));
                    pill.SetPose(bounds.Center, bounds.Along);
                    pill.SetLineColor(line.Color);
                }
            }
        }

        private bool NeedsPlan(EdgeState state, Vector2 start, Vector2 end, float budget,
            int count, float maximumLineWidth, float minimumReadableLineWidth,
            float minimumLineGap, ConnectionsToyIdentityPacking.PillBounds[] obstacles)
        {
            if (state.Layout == null || state.Count != count || state.PillSize != PillSize ||
                (state.Start - start).sqrMagnitude > 0.25f ||
                (state.End - end).sqrMagnitude > 0.25f ||
                !Mathf.Approximately(state.Budget, budget) ||
                !Mathf.Approximately(state.MaximumWidth, maximumLineWidth) ||
                !Mathf.Approximately(state.MinimumReadableWidth, minimumReadableLineWidth) ||
                !Mathf.Approximately(state.MinimumGap, minimumLineGap) ||
                !Mathf.Approximately(state.Gap, MinimumPillGap) ||
                !Mathf.Approximately(state.Step, StaggerStep) || !Mathf.Approximately(state.Inset, EndInset) ||
                !Mathf.Approximately(state.Clearance, Mathf.Max(0f, pillToLineClearance)) ||
                state.Obstacles == null || state.Obstacles.Length != obstacles.Length)
                return true;
            for (int i = 0; i < obstacles.Length; i++)
                if ((state.Obstacles[i].Center - obstacles[i].Center).sqrMagnitude > 0.25f ||
                    (state.Obstacles[i].HalfSize - obstacles[i].HalfSize).sqrMagnitude > 0.25f ||
                    (state.Obstacles[i].Along - obstacles[i].Along).sqrMagnitude > 0.0001f)
                    return true;
            return false;
        }

        private ConnectionsToyIdentityPacking.PillBounds[] Obstacles(ConnectionsToyPhysicalLinkView link)
        {
            return link.ResourceLabel != null ? new[] { Bounds(link.NodeA.transform),
                Bounds(link.NodeB.transform), Bounds(link.ResourceLabel) } :
                new[] { Bounds(link.NodeA.transform), Bounds(link.NodeB.transform) };
        }

        private ConnectionsToyIdentityPacking.PillBounds Bounds(Transform target)
        {
            var corners = new Vector3[4];
            ((RectTransform)target).GetWorldCorners(corners);
            Vector2 bottomLeft = networkArea.InverseTransformPoint(corners[0]);
            Vector2 topLeft = networkArea.InverseTransformPoint(corners[1]);
            Vector2 topRight = networkArea.InverseTransformPoint(corners[2]);
            Vector2 bottomRight = networkArea.InverseTransformPoint(corners[3]);
            return new ConnectionsToyIdentityPacking.PillBounds(
                (bottomLeft + topRight) * 0.5f, bottomRight - bottomLeft,
                new Vector2(Vector2.Distance(bottomLeft, bottomRight),
                    Vector2.Distance(bottomLeft, topLeft)));
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
