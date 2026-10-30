using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public sealed class WormPresenter : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform pathLayer;
        [SerializeField] private PathGraphic lineStyle;
        [SerializeField, Min(.5f)] private float threadWidth = 2f;
        [SerializeField, Min(2f)] private float bodyWidth = 8f;
        [SerializeField, Min(0f)] private float waveAmplitude = 2.5f;

        private struct LaneLayout
        {
            public float StartOffset;
            public float EndOffset;
            public float Width;
            public float Gap;
        }

        private sealed class Visual
        {
            public WormThreadGraphic Graphic;
            public Vector2 Start;
            public Vector2 End;
            public bool HasPose;
        }

        private readonly Dictionary<WormBranch, LaneLayout> layouts = new();
        private readonly Dictionary<WormBranch, Visual> visuals = new();

        public void SetLayout(WormBranch branch, float startOffset, float endOffset,
            float width, float gap)
        {
            layouts[branch] = new LaneLayout { StartOffset = startOffset,
                EndOffset = endOffset, Width = width, Gap = gap };
        }

        private void LateUpdate() { if (Application.isPlaying) RenderNow(); }

        public void RenderNow()
        {
            if (host?.Worms == null || networkArea == null || pathLayer == null ||
                lineStyle == null) return;
            var active = new HashSet<WormBranch>();
            foreach (WormOutbreak outbreak in host.Worms.Outbreaks)
                foreach (WormBranch branch in outbreak.Branches)
                {
                    if (!branch.HasHop) continue;
                    active.Add(branch);
                    if (!layouts.TryGetValue(branch, out LaneLayout layout)) continue;
                    if (!visuals.TryGetValue(branch, out Visual visual))
                    {
                        visual = Create(branch);
                        visuals.Add(branch, visual);
                    }
                    Vector2 a = networkArea.InverseTransformPoint(branch.CurrentNode.transform.position);
                    Vector2 b = networkArea.InverseTransformPoint(branch.NextNode.transform.position);
                    Vector2 direction = (b - a).normalized;
                    float thin = Mathf.Min(threadWidth, layout.Width);
                    Vector2 start = EdgeGeometry.CardUnderlap(branch.CurrentNode, networkArea,
                        direction, layout.StartOffset, thin);
                    Vector2 end = EdgeGeometry.CardUnderlap(branch.NextNode, networkArea,
                        -direction, -layout.EndOffset, thin);
                    if (!visual.HasPose)
                    {
                        visual.Start = start;
                        visual.End = end;
                        visual.HasPose = true;
                    }
                    else
                    {
                        float blend = 1f - Mathf.Exp(-Mathf.Max(0f, Time.unscaledDeltaTime) / .22f);
                        visual.Start = Vector2.Lerp(visual.Start, start, blend);
                        visual.End = Vector2.Lerp(visual.End, end, blend);
                    }
                    float length = Vector2.Distance(visual.Start, visual.End);
                    float bodyFraction = Mathf.Min(.125f, 52f / Mathf.Max(1f, length));
                    float head = branch.Phase == WormPhase.Establishing ? 0f :
                        branch.Phase == WormPhase.Entering ? 1f : branch.Progress;
                    float tail = branch.Phase == WormPhase.Entering ?
                        1f - bodyFraction * (1f - branch.Progress) :
                        Mathf.Max(0f, head - bodyFraction);
                    float reveal = branch.Phase == WormPhase.Establishing ? branch.Progress : 1f;
                    float thick = Mathf.Min(bodyWidth,
                        Mathf.Max(thin, layout.Width + layout.Gap - 2f));
                    float amplitude = Mathf.Min(waveAmplitude,
                        Mathf.Max(0f, (layout.Width + layout.Gap - thick) * .5f - .5f));
                    visual.Graphic.SetWorm(visual.Start, visual.End, reveal, tail, head,
                        thin, thick, amplitude, host.SimulationTimeSeconds * 2.1f,
                        host.Worms.WormColor);
                }
            var removed = new List<WormBranch>();
            foreach (var pair in visuals)
                if (!active.Contains(pair.Key))
                {
                    Remove(pair.Value.Graphic);
                    removed.Add(pair.Key);
                }
            foreach (WormBranch branch in removed)
            {
                visuals.Remove(branch);
                layouts.Remove(branch);
            }
        }

        public bool TryGetVisualSegment(WormBranch branch, out Vector2 start,
            out Vector2 end, out float reveal)
        {
            if (visuals.TryGetValue(branch, out Visual visual) && visual.HasPose)
            {
                start = visual.Start;
                end = visual.End;
                reveal = visual.Graphic.Reveal;
                return true;
            }
            start = end = Vector2.zero;
            reveal = 0f;
            return false;
        }

        private Visual Create(WormBranch branch)
        {
            var go = new GameObject("Worm " + branch.Id + " thread", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(WormThreadGraphic));
            var rect = (RectTransform)go.transform;
            rect.SetParent(pathLayer, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var graphic = go.GetComponent<WormThreadGraphic>();
            graphic.material = lineStyle.material;
            graphic.raycastTarget = false;
            return new Visual { Graphic = graphic };
        }

        private static void Remove(Component component)
        {
            if (component == null) return;
            if (Application.isPlaying) Destroy(component.gameObject);
            else DestroyImmediate(component.gameObject);
        }

        private void OnDisable()
        {
            foreach (Visual visual in visuals.Values) Remove(visual.Graphic);
            visuals.Clear();
            layouts.Clear();
        }
    }
}
