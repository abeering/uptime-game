using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    // A physical edge in the static topology. Logical connections will use a separate type.
    [ExecuteAlways]
    public sealed class PhysicalLinkView : MonoBehaviour
    {
        [SerializeField] private string linkId;
        [SerializeField] private NodeView nodeA;
        [SerializeField] private NodeView nodeB;
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform label;
        [SerializeField] private TMP_Text resourceReadout;
        [SerializeField, Min(0f)] private float labelOffset = 26f;

        private float? occupiedLabelOffset;
        private float renderedAtA;
        private float renderedAtB;
        private float fromAtA;
        private float fromAtB;
        private float targetAtA;
        private float targetAtB;
        private float relayoutElapsed;
        private float relayoutSeconds = 0.3f;

        public string LinkId => linkId;
        public NodeView NodeA => nodeA;
        public NodeView NodeB => nodeB;
        public RectTransform ResourceLabel => label;

        public void SetAttachmentOffsets(float atA, float atB, float durationSeconds)
        {
            if (Mathf.Abs(targetAtA - atA) < 0.01f &&
                Mathf.Abs(targetAtB - atB) < 0.01f) return;
            fromAtA = renderedAtA;
            fromAtB = renderedAtB;
            targetAtA = atA;
            targetAtB = atB;
            relayoutElapsed = 0f;
            relayoutSeconds = Mathf.Max(0.01f, durationSeconds);
            if (!Application.isPlaying) AdvanceVisual(relayoutSeconds);
        }

        public void ClearAttachmentOffsets(float durationSeconds) =>
            SetAttachmentOffsets(0f, 0f, durationSeconds);

        public void GetTargetSpine(out Vector2 start, out Vector2 end) =>
            Spine(targetAtA, targetAtB, out start, out end);

        public void GetRenderedSpine(out Vector2 start, out Vector2 end) =>
            Spine(renderedAtA, renderedAtB, out start, out end);

        public void GetTargetLabelPose(float requestedOffset, out Vector2 center,
            out Vector2 along)
        {
            GetTargetSpine(out Vector2 start, out Vector2 end);
            along = (end - start).normalized;
            Vector2 normal = new Vector2(-along.y, along.x);
            if (normal.y < 0f) normal = -normal;
            center = (start + end) * 0.5f + normal *
                Mathf.Max(labelOffset, requestedOffset);
        }

        // Keep the label at the link midpoint while active paths need more side clearance.
        public void SetOccupiedLabelOffset(float minimumOffset)
        {
            float next = Mathf.Max(labelOffset, minimumOffset);
            if (occupiedLabelOffset.HasValue && Mathf.Abs(occupiedLabelOffset.Value - next) < 0.01f)
                return;
            occupiedLabelOffset = next;
            ApplyPose();
        }

        public void ClearOccupiedLabelOffset()
        {
            if (!occupiedLabelOffset.HasValue) return;
            occupiedLabelOffset = null;
            ApplyPose();
        }

        // Rebind an authored link copy in the opt-in stress scene.
        public void ConfigureStressLink(string id, NodeView a, NodeView b,
            RectTransform area, RectTransform newLabel, TMP_Text newReadout)
        {
            linkId = id;
            nodeA = a;
            nodeB = b;
            networkArea = area;
            label = newLabel;
            resourceReadout = newReadout;
            RenderLink();
        }

        public float BundleWidth(RectTransform area)
        {
            Vector2 a = area.InverseTransformPoint(nodeA.transform.position);
            Vector2 b = area.InverseTransformPoint(nodeB.transform.position);
            return Mathf.Min(nodeA.BundleWidthToward(b - a), nodeB.BundleWidthToward(a - b));
        }

        public void RenderLink()
        {
            if (resourceReadout == null)
                return;

            string value = linkId;
            if (resourceReadout.text != value)
                resourceReadout.text = value;
        }

        private void LateUpdate() => AdvanceVisual(Time.deltaTime);

        public void AdvanceVisual(float deltaSeconds)
        {
            if (Application.isPlaying)
            {
                relayoutElapsed += Mathf.Max(0f, deltaSeconds);
                float t = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(relayoutElapsed / relayoutSeconds));
                renderedAtA = Mathf.Lerp(fromAtA, targetAtA, t);
                renderedAtB = Mathf.Lerp(fromAtB, targetAtB, t);
            }
            else
            {
                renderedAtA = targetAtA;
                renderedAtB = targetAtB;
            }
            ApplyPose();
        }

        private void Spine(float atA, float atB, out Vector2 start, out Vector2 end)
        {
            if (nodeA == null || nodeB == null || networkArea == null)
            {
                start = end = Vector2.zero;
                return;
            }

            Vector2 a = networkArea.InverseTransformPoint(nodeA.transform.position);
            Vector2 b = networkArea.InverseTransformPoint(nodeB.transform.position);
            Vector2 direction = (b - a).normalized;
            float width = ((RectTransform)transform).rect.height;
            start = EdgeGeometry.CardUnderlap(nodeA, networkArea, direction, atA, width);
            end = EdgeGeometry.CardUnderlap(nodeB, networkArea, -direction, -atB, width);
        }

        private void ApplyPose()
        {
            if (nodeA == null || nodeB == null || networkArea == null)
                return;

            RectTransform line = (RectTransform)transform;
            Spine(renderedAtA, renderedAtB, out Vector2 start, out Vector2 end);
            Vector2 direction = end - start;
            Vector2 middle = (start + end) * 0.5f;

            line.anchoredPosition = middle;
            line.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, direction.magnitude);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            if (label != null)
            {
                Vector2 normal = new Vector2(-direction.y, direction.x).normalized;
                if (normal.y < 0f)
                    normal = -normal;

                label.anchoredPosition = middle + normal *
                    (occupiedLabelOffset ?? labelOffset);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                if (angle > 90f)
                    angle -= 180f;
                else if (angle < -90f)
                    angle += 180f;
                label.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}
