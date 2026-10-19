using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.ConnectionsToy
{
    public sealed class ConnectionsToyLogicalPathView : MonoBehaviour
    {
        [SerializeField] private ConnectionsToyPathGraphic path;
        [SerializeField] private TMP_Text[] segmentTags;
        [SerializeField, Range(0.1f, 2f)] private float establishmentSeconds = 0.6f;
        [SerializeField, Range(0.1f, 2f)] private float departureSeconds = 0.45f;
        [SerializeField] private Color healthyColor = new Color(0.36f, 0.95f, 0.74f);
        [SerializeField] private Color constrainedColor = new Color(1f, 0.69f, 0.34f);

        private ConnectionsToyLogicalConnectionState state;
        private RectTransform networkArea;
        private float[] laneOffsets;
        private Vector2[] starts;
        private Vector2[] ends;
        private float establishmentElapsed;
        private float departureElapsed;
        private bool establishedVisual;

        public void Bind(ConnectionsToyLogicalConnectionState connection, RectTransform area, float[] offsets)
        {
            state = connection;
            networkArea = area;
            laneOffsets = offsets;
            starts = new Vector2[offsets.Length];
            ends = new Vector2[offsets.Length];
            for (int i = 0; i < segmentTags.Length; i++)
            {
                segmentTags[i].text = connection.ConnectionId;
                segmentTags[i].transform.parent.gameObject.SetActive(false);
            }
            path.raycastTarget = false;
            path.SetPath(starts, ends, 0f);
        }

        private void LateUpdate()
        {
            if (state == null || networkArea == null || path == null || state.IsPending)
                return;

            if (!establishedVisual)
            {
                establishedVisual = true;
                establishmentElapsed = 0f;
            }

            if (state.IsComplete)
            {
                departureElapsed += Time.deltaTime;
                if (departureElapsed >= departureSeconds)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            else
                establishmentElapsed += Time.deltaTime;

            float reveal = Mathf.Clamp01(establishmentElapsed / establishmentSeconds);
            float fade = state.IsComplete ? 1f - Mathf.Clamp01(departureElapsed / departureSeconds) : 1f;
            Color tint = state.Satisfaction >= 0.98f ? healthyColor : constrainedColor;
            tint.a *= fade;
            if (path.color != tint)
                path.color = tint;

            float totalLength = 0f;
            for (int i = 0; i < starts.Length; i++)
            {
                Vector2 a = networkArea.InverseTransformPoint(state.Definition.Route[i].Node.transform.position);
                Vector2 b = networkArea.InverseTransformPoint(state.Definition.Route[i + 1].Node.transform.position);
                Vector2 direction = (b - a).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                starts[i] = CardEdge(state.Definition.Route[i].Node, a, direction) + normal * laneOffsets[i];
                ends[i] = CardEdge(state.Definition.Route[i + 1].Node, b, -direction) + normal * laneOffsets[i];
                totalLength += Vector2.Distance(starts[i], ends[i]);
            }
            path.SetPath(starts, ends, reveal);

            float visibleLength = totalLength * reveal;
            float before = 0f;
            for (int i = 0; i < segmentTags.Length; i++)
            {
                if (i >= starts.Length)
                {
                    segmentTags[i].transform.parent.gameObject.SetActive(false);
                    continue;
                }
                float length = Vector2.Distance(starts[i], ends[i]);
                float tagFraction = i == 0 ? (laneOffsets[i] > 0f ? 0.04f : 0.4f) : 0.28f;
                bool visible = visibleLength >= before + length * tagFraction;
                RectTransform tag = (RectTransform)segmentTags[i].transform.parent;
                if (tag.gameObject.activeSelf != visible)
                    tag.gameObject.SetActive(visible);
                if (visible)
                {
                    Vector2 direction = (ends[i] - starts[i]).normalized;
                    Vector2 normal = new Vector2(-direction.y, direction.x);
                    float side = laneOffsets[i] < 0f ? -1f : 1f;
                    if (i > 0 && direction.x > 0f)
                        side = -1f;
                    float tagOffset = i == 0 ? 18f : 30f;
                    tag.anchoredPosition = Vector2.Lerp(starts[i], ends[i], tagFraction) + normal * (side * tagOffset);
                    Color labelColor = tint;
                    labelColor.a = fade;
                    segmentTags[i].color = labelColor;
                    Image background = tag.GetComponent<Image>();
                    if (background != null)
                        background.color = new Color(0.035f, 0.10f, 0.13f, 0.94f * fade);
                }
                before += length;
            }
        }

        private Vector2 CardEdge(ConnectionsToyNodeView node, Vector2 center, Vector2 direction)
        {
            RectTransform card = (RectTransform)node.transform;
            Vector2 size = card.rect.size;
            float x = Mathf.Abs(direction.x) > 0.001f ? size.x * 0.5f / Mathf.Abs(direction.x) : float.PositiveInfinity;
            float y = Mathf.Abs(direction.y) > 0.001f ? size.y * 0.5f / Mathf.Abs(direction.y) : float.PositiveInfinity;
            return center + direction * (Mathf.Min(x, y) + 5f);
        }
    }
}
