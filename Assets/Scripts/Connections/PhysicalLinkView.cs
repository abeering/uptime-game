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

        public string LinkId => linkId;
        public NodeView NodeA => nodeA;
        public NodeView NodeB => nodeB;
        public RectTransform ResourceLabel => label;

        // Keep the label at the link midpoint while active paths need more side clearance.
        public void SetOccupiedLabelOffset(float minimumOffset)
        {
            float next = Mathf.Max(labelOffset, minimumOffset);
            if (occupiedLabelOffset.HasValue && Mathf.Abs(occupiedLabelOffset.Value - next) < 0.01f)
                return;
            occupiedLabelOffset = next;
            LateUpdate();
        }

        public void ClearOccupiedLabelOffset()
        {
            if (!occupiedLabelOffset.HasValue) return;
            occupiedLabelOffset = null;
            LateUpdate();
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

        private void LateUpdate()
        {
            if (nodeA == null || nodeB == null || networkArea == null)
                return;

            RectTransform line = (RectTransform)transform;
            Vector2 a = networkArea.InverseTransformPoint(nodeA.transform.position);
            Vector2 b = networkArea.InverseTransformPoint(nodeB.transform.position);
            Vector2 direction = b - a;
            Vector2 middle = (a + b) * 0.5f;

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
