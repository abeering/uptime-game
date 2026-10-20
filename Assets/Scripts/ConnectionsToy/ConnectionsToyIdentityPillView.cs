using TMPro;
using UnityEngine;

namespace Uptime.ConnectionsToy
{
    public sealed class ConnectionsToyIdentityPillView : MonoBehaviour
    {
        [SerializeField] private ConnectionsToyPillGraphic plate;
        [SerializeField] private TMP_Text idText;
        [SerializeField] private Vector2 dimensions = new Vector2(56f, 29f);
        [SerializeField, Min(8f)] private float fontSize = 16f;

        public Vector2 Size => new Vector2(Mathf.Max(32f, dimensions.x), Mathf.Max(23f, dimensions.y));

        public void Bind(string connectionId, Color lineColor)
        {
            idText.text = connectionId.ToUpperInvariant();
            plate.SetOutline(lineColor);
            plate.raycastTarget = false;
            idText.raycastTarget = false;
            ApplySize();
        }

        public void SetPosition(Vector2 position)
        {
            ((RectTransform)transform).anchoredPosition = position;
        }

        public void SetLineColor(Color lineColor) => plate.SetOutline(lineColor);

        private void OnValidate() => ApplySize();

        private void ApplySize()
        {
            ((RectTransform)transform).sizeDelta = Size;
            if (idText != null)
                idText.fontSize = Mathf.Max(8f, fontSize);
        }
    }
}
