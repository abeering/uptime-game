using TMPro;
using Uptime.Connections.Display;
using UnityEngine;

namespace Uptime.Connections
{
    public sealed class IdentityPillView : MonoBehaviour
    {
        [SerializeField] private PillGraphic plate;
        [SerializeField] private ConnectionProgressPillGraphic progressPlate;
        [SerializeField] private ConnectionDisplayStyle displayStyle;
        private float displayOpacity = 1;
        private Color displayIdentityColor;
        public bool UsesSharedDisplay => progressPlate != null;
        public ConnectionProgressPillGraphic ProgressPlate => progressPlate;
        [SerializeField] private TMP_Text idText;
        [SerializeField] private TMP_Text serviceDangerText;
        [SerializeField] private Vector2 dimensions = new Vector2(54f, 28f);
        [SerializeField, Min(8f)] private float fontSize = 16f;
        [SerializeField] private Color identifierTextColor = new(.85f, .97f, .95f);
        [SerializeField, Min(0f)] private float identityChangeEmphasisSeconds = 0.22f;
        [SerializeField, Range(0f, 1f)] private float identityChangeWhiteMix = 0.24f;

        private LogicalConnectionState identityState;
        private ConnectionIdentityPalette identityPalette;
        public void FollowIdentity(LogicalConnectionState state, ConnectionIdentityPalette palette)
        { identityState = state; identityPalette = palette; RefreshIdentity(); }
        private void RefreshIdentity()
        {
            if (identityState != null) SetImportant(identityState.IsImportant);
            if (identityState != null && identityPalette != null && identityPalette.TryGet(identityState, out Color color) && color != baseAccent)
                SetAccentColor(color);
        }
        private string displayedId;
        private bool important;
        public bool IsImportant => important;
        public string RenderedLabel => idText != null ? idText.text : string.Empty;
        public void SetImportant(bool value)
        { if (important == value) return; important = value; RefreshLabel(); }
        private void RefreshLabel()
        { if (idText != null && displayedId != null) idText.SetText(important ? displayedId + "<size=75%>!</size>" : displayedId); }
        private Color baseAccent;
        private float changedAt = float.NegativeInfinity;

        public Vector2 Size => UsesSharedDisplay ? ((RectTransform)transform).rect.size :
            new Vector2(Mathf.Max(32f, dimensions.x), Mathf.Max(23f, dimensions.y));
        public Color AccentColor => progressPlate != null ? progressPlate.color : plate != null ? plate.color : Color.clear;
        public string DisplayedId => displayedId ?? (idText != null ? idText.text : string.Empty);

        public void Bind(string connectionId, Color accent)
        {
            identityState = null; identityPalette = null;
            displayedId = connectionId.ToLowerInvariant(); important = false; RefreshLabel();
            changedAt = float.NegativeInfinity;
            SetAccentColor(accent);
            if (plate != null) plate.raycastTarget = false;
            if (progressPlate != null) progressPlate.raycastTarget = false;
            idText.raycastTarget = false;
            ApplySize();
        }

        // Temporary danger text stays inside the original packing bounds.
        public void SetServiceDanger(bool danger)
        {
            if (serviceDangerText == null || UsesSharedDisplay) return;
            serviceDangerText.gameObject.SetActive(danger);
            idText.rectTransform.anchoredPosition = new Vector2(0, danger ? 5 : 0);
            idText.fontSize = danger ? 18 : Mathf.Max(8f, fontSize);
        }

        public void SetDisplayState(ConnectionDisplayState state, PillProgressTreatment treatment = PillProgressTreatment.Interior, bool applyOpacity = true)
        {
            if (!UsesSharedDisplay || displayStyle == null) return;
            SetImportant(state.trafficClass == DisplayTrafficClass.Important);
            displayIdentityColor = displayStyle.IdentityColor(state);
            displayOpacity = applyOpacity ? displayStyle.Opacity(state) : 1;
            Color tint = displayIdentityColor; tint.a *= displayOpacity;
            progressPlate.Render(state.workProgress, treatment, tint);
            if (idText != null) idText.alpha = displayOpacity;
            RefreshAccent();
        }

        public void SetPose(Vector2 position, Vector2 direction)
        {
            RectTransform rect = (RectTransform)transform;
            // Packed poses are measured from the overlay center. The shared pill's
            // authored lab-rack anchor is intentionally independent of this owner.
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle > 90f)
                angle -= 180f;
            else if (angle < -90f)
                angle += 180f;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetAccentColor(Color accent)
        {
            baseAccent = accent;
            RefreshAccent();
        }

        public void SetDisplayedId(string identity)
        {
            if (idText == null || string.IsNullOrEmpty(identity)) return;
            string text = identity.ToLowerInvariant();
            if (DisplayedId == text) return;
            displayedId = text; RefreshLabel();
            changedAt = Time.unscaledTime;
            RefreshAccent();
        }

        private void Update()
        {
            RefreshIdentity();
            if (Time.unscaledTime - changedAt < identityChangeEmphasisSeconds)
                RefreshAccent();
        }

        private void RefreshAccent()
        {
            float remaining = identityChangeEmphasisSeconds > 0f ?
                Mathf.Clamp01(1f - (Time.unscaledTime - changedAt) /
                    identityChangeEmphasisSeconds) : 0f;
            Color accent = Color.Lerp(baseAccent, Color.white,
                identityChangeWhiteMix * remaining);
            if (plate != null) plate.SetOutline(accent);
            // Base identity remains recognizable during rail danger; aliases retain brief emphasis.
            if (progressPlate != null && displayStyle != null)
            {
                Color neutral = Color.Lerp(displayIdentityColor, Color.white, identityChangeWhiteMix * remaining);
                neutral.a = displayOpacity; progressPlate.color = neutral;
            }
            if (idText != null) { idText.color = identifierTextColor; idText.alpha = UsesSharedDisplay ? displayOpacity : 1; }
        }

        private void OnValidate() => ApplySize();

        private void ApplySize()
        {
            // Shared pills take size and typography from the authored asset itself.
            if (UsesSharedDisplay) return;
            ((RectTransform)transform).sizeDelta = Size;
            if (idText != null)
                idText.fontSize = Mathf.Max(8f, fontSize);
        }
    }
}
