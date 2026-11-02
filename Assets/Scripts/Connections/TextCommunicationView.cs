using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Packet's short unscaled container fade/slide and smooth reflow, adapted without
    // per-entry coroutines so reflow cannot cancel an in-progress arrival fade.
    public sealed class TextCommunicationView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image accentRail;
        [SerializeField] private Image avatarImage;
        [SerializeField] private GameObject avatarFallback;
        [SerializeField] private GameObject systemFallback;
        [SerializeField] private TMP_Text senderText;
        [SerializeField] private TMP_Text timestampText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private RectTransform[] readableContent;
        public RectTransform[] ReadableContent => readableContent;
        [SerializeField, Min(.01f)] private float arrivalSeconds = .16f;
        [SerializeField, Min(.01f)] private float exitSeconds = .22f;
        [SerializeField, Min(.01f)] private float reflowSeconds = .18f;
        private Vector2 target, moveStart;
        private double arrivedAt, movedAt, retiredAt;
        private float exitAlpha;
        private Vector2 exitStart;
        public RectTransform Rect => (RectTransform)transform;
        public float Height => Rect.rect.height;
        public double ExpiresAt { get; private set; }
        public bool IsExiting { get; private set; }
        public bool ExitFinished(double now) => IsExiting && now >= retiredAt + exitSeconds;

        public void Bind(TextCommunication message, Color accent, string timestamp,
            float width, float maxHeight, float lifetime, double now)
        {
            accentRail.color = accent;
            senderText.text = string.IsNullOrWhiteSpace(message.Sender) ? "Unknown" : message.Sender;
            timestampText.text = timestamp ?? string.Empty;
            bodyText.text = message.Body ?? string.Empty;
            bodyText.maxVisibleCharacters = int.MaxValue;
            avatarImage.sprite = message.Avatar;
            avatarImage.enabled = message.Avatar != null;
            avatarFallback.SetActive(message.Avatar == null && message.Type == TextCommunicationType.InstantMessage);
            systemFallback.SetActive(message.Avatar == null && message.Type == TextCommunicationType.SystemNotification);
            Resize(width, maxHeight);
            arrivedAt = movedAt = now;
            ExpiresAt = now + lifetime + arrivalSeconds;
            target = Vector2.zero;
            moveStart = new Vector2(-12, 0);
            Rect.anchoredPosition = moveStart;
            canvasGroup.alpha = 0;
        }

        public void Resize(float width, float maxHeight)
        {
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            // The prefab authors layout; runtime only measures text-driven height.
            float height = Mathf.Max(88, 55 + bodyText.GetPreferredValues(bodyText.text, width - 96, 0).y);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(height, maxHeight));
        }

        public void MoveTo(Vector2 position, double now)
        {
            if (IsExiting || target == position) return;
            moveStart = Rect.anchoredPosition;
            target = position;
            movedAt = now;
        }

        public void Retire(double now)
        {
            if (IsExiting) return;
            IsExiting = true;
            retiredAt = now;
            exitStart = Rect.anchoredPosition;
            exitAlpha = canvasGroup.alpha;
        }

        public void Animate(double now)
        {
            if (IsExiting)
            {
                float t = Mathf.Clamp01((float)(now - retiredAt) / exitSeconds);
                canvasGroup.alpha = exitAlpha * (1 - t);
                Rect.anchoredPosition = exitStart + new Vector2(-12 * t, 0);
                return;
            }
            float move = Mathf.Clamp01((float)(now - movedAt) / reflowSeconds);
            Rect.anchoredPosition = Vector2.LerpUnclamped(moveStart, target, 1 - Mathf.Pow(1 - move, 3));
            canvasGroup.alpha = Mathf.Clamp01((float)(now - arrivedAt) / arrivalSeconds);
        }
    }
}
