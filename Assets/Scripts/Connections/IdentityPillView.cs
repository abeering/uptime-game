using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    public sealed class IdentityPillView : MonoBehaviour
    {
        [SerializeField] private PillGraphic plate;
        [SerializeField] private TMP_Text idText;
        [SerializeField] private Vector2 dimensions = new Vector2(54f, 28f);
        [SerializeField, Min(8f)] private float fontSize = 16f;

        public Vector2 Size => new Vector2(Mathf.Max(32f, dimensions.x), Mathf.Max(23f, dimensions.y));
        public Color AccentColor => plate != null ? plate.color : Color.clear;
        public string DisplayedId => idText != null ? idText.text : string.Empty;

        public void Bind(string connectionId, Color accent)
        {
            idText.text = connectionId.ToLowerInvariant();
            plate.SetOutline(accent);
            plate.raycastTarget = false;
            idText.raycastTarget = false;
            ApplySize();
        }

        public void SetPose(Vector2 position, Vector2 direction)
        {
            RectTransform rect = (RectTransform)transform;
            rect.anchoredPosition = position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle > 90f)
                angle -= 180f;
            else if (angle < -90f)
                angle += 180f;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetAccentColor(Color accent) => plate.SetOutline(accent);

        private void OnValidate() => ApplySize();

        private void ApplySize()
        {
            ((RectTransform)transform).sizeDelta = Size;
            if (idText != null)
                idText.fontSize = Mathf.Max(8f, fontSize);
        }
    }
}
