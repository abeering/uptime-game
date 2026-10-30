using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // One procedural path: the luminous body is a thick interval of the same curve.
    public sealed class WormThreadGraphic : AntialiasedLineGraphic
    {
        private Vector2 start;
        private Vector2 end;
        private float reveal;
        private float tail;
        private float head;
        private float thinWidth;
        private float bodyWidth;
        private float amplitude;
        private float phase;

        public Vector2 PathStart => start;
        public Vector2 PathEnd => end;
        public float Tail => tail;
        public float Head => head;
        public float Reveal => reveal;

        public void SetWorm(Vector2 from, Vector2 to, float visibleFraction,
            float tailFraction, float headFraction, float threadWidth,
            float thickWidth, float waveAmplitude, float wavePhase, Color accent)
        {
            start = from;
            end = to;
            reveal = Mathf.Clamp01(visibleFraction);
            tail = Mathf.Clamp01(tailFraction);
            head = Mathf.Clamp01(headFraction);
            thinWidth = Mathf.Max(.5f, threadWidth);
            bodyWidth = Mathf.Max(thinWidth, thickWidth);
            amplitude = Mathf.Max(0f, waveAmplitude);
            phase = wavePhase;
            color = accent;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float length = Vector2.Distance(start, end);
            if (length < 1f || reveal <= 0f) return;
            int steps = Mathf.Clamp(Mathf.CeilToInt(length / 8f), 8, 96);
            Vector2 previous = Point(0f, length);
            for (int i = 1; i <= steps; i++)
            {
                float t = Mathf.Min(reveal, i / (float)steps);
                if (t <= (i - 1f) / steps) break;
                Vector2 next = Point(t, length);
                AddLine(mesh, previous, next, thinWidth, color * new Color(1f, 1f, 1f, .78f));
                previous = next;
                if (t >= reveal - .0001f) break;
            }
            if (head <= tail + .0001f) return;
            int bodySteps = Mathf.Max(2, Mathf.CeilToInt((head - tail) * steps));
            previous = Point(tail, length);
            for (int i = 1; i <= bodySteps; i++)
            {
                float t = Mathf.Lerp(tail, head, i / (float)bodySteps);
                Vector2 next = Point(t, length);
                float front = i / (float)bodySteps;
                Color glow = Color.Lerp(color, Color.white, .18f + .42f * front);
                glow.a = color.a;
                AddLine(mesh, previous, next,
                    bodyWidth * (.78f + .22f * Mathf.Sin(front * Mathf.PI)), glow);
                previous = next;
            }
        }

        private Vector2 Point(float t, float length)
        {
            Vector2 direction = (end - start) / length;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            float cycles = Mathf.Max(1f, length / 72f);
            float envelope = Mathf.Sin(Mathf.PI * t);
            float wave = Mathf.Sin(t * cycles * Mathf.PI * 2f + phase) *
                amplitude * envelope;
            return Vector2.Lerp(start, end, t) + normal * wave;
        }
    }
}
