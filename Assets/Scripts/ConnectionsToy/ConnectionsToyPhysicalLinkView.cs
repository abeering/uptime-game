using TMPro;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // A physical edge in the static topology. Logical connections will use a separate type.
    [ExecuteAlways]
    public sealed class ConnectionsToyPhysicalLinkView : MonoBehaviour
    {
        [SerializeField] private string linkId;
        [SerializeField] private ConnectionsToyNodeView nodeA;
        [SerializeField] private ConnectionsToyNodeView nodeB;
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform label;
        [SerializeField] private TMP_Text resourceReadout;
        [SerializeField, Min(0f)] private float labelOffset = 26f;

        public string LinkId => linkId;
        public ConnectionsToyNodeView NodeA => nodeA;
        public ConnectionsToyNodeView NodeB => nodeB;

        public float BundleWidth(RectTransform area)
        {
            Vector2 a = area.InverseTransformPoint(nodeA.transform.position);
            Vector2 b = area.InverseTransformPoint(nodeB.transform.position);
            return Mathf.Min(nodeA.BundleWidthToward(b - a), nodeB.BundleWidthToward(a - b));
        }

        public void RenderResources(ConnectionsToyPhysicalLinkResources resources)
        {
            if (resourceReadout == null || resources == null)
                return;

            string value = $"{linkId} {resources.BandwidthUsageMBps:0.#}/{resources.BandwidthCapacityMBps:0.#} MB/s";
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

                label.anchoredPosition = middle + normal * labelOffset;
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
