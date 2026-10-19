using UnityEngine;

namespace Uptime.ConnectionsToy
{
    // A physical edge in the static topology. Logical connections will use a separate type.
    public sealed class ConnectionsToyPhysicalLinkView : MonoBehaviour
    {
        [SerializeField] private string linkId;
        [SerializeField] private ConnectionsToyNodeView nodeA;
        [SerializeField] private ConnectionsToyNodeView nodeB;
        [SerializeField] private RectTransform networkArea;
        [SerializeField] private RectTransform label;

        public string LinkId => linkId;
        public ConnectionsToyNodeView NodeA => nodeA;
        public ConnectionsToyNodeView NodeB => nodeB;

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
                Vector2 normal = new(-direction.y, direction.x);
                label.anchoredPosition = middle + normal.normalized * 22f;
            }
        }
    }
}
