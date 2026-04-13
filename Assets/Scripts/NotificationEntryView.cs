using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationEntryView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image avatarFallbackImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private LayoutElement layoutElement;

    [Header("Sizing")]
    [SerializeField] private float fallbackHeight = 92f;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.18f;
    [SerializeField] private float enterFadeDuration = 0.16f;
    [SerializeField] private float exitDuration = 0.22f;

    private Coroutine moveRoutine;
    private Coroutine exitRoutine;

    public RectTransform RectTransform => rectTransform != null ? rectTransform : (RectTransform)transform;
    public bool IsExiting { get; private set; }

    private void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();
    }

    public void Bind(Notification message, Sprite placeholderAvatar = null)
    {
        if (speakerNameText != null)
            speakerNameText.text = string.IsNullOrWhiteSpace(message?.SpeakerName) ? "Unknown" : message.SpeakerName;

        if (bodyText != null)
            bodyText.text = string.IsNullOrWhiteSpace(message?.Body) ? "" : message.Body;

        Sprite resolvedAvatar = message != null && message.Avatar != null ? message.Avatar : placeholderAvatar;

        if (avatarImage != null)
        {
            bool hasAvatar = resolvedAvatar != null;
            avatarImage.enabled = hasAvatar;
            avatarImage.sprite = resolvedAvatar;
        }

        if (avatarFallbackImage != null)
        {
            bool showFallback = resolvedAvatar == null;
            avatarFallbackImage.enabled = showFallback;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
    }

    public float GetHeight()
    {
        if (layoutElement != null && layoutElement.preferredHeight > 0f)
            return layoutElement.preferredHeight;

        if (RectTransform != null && RectTransform.rect.height > 0f)
            return RectTransform.rect.height;

        return fallbackHeight;
    }

    public void SetInstantPosition(Vector2 anchoredPosition, float alpha = 1f)
    {
        RectTransform.anchoredPosition = anchoredPosition;

        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }

    public void PlayEnter(Vector2 finalPosition, float spawnOffsetY)
    {
        StopMoveRoutine();

        Vector2 start = finalPosition + new Vector2(0f, spawnOffsetY);
        RectTransform.anchoredPosition = start;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        moveRoutine = StartCoroutine(EnterRoutine(finalPosition));
    }

    public void MoveTo(Vector2 targetPosition)
    {
        if (IsExiting)
            return;

        StopMoveRoutine();
        moveRoutine = StartCoroutine(MoveRoutine(targetPosition, moveDuration));
    }

    public void PlayExit(System.Action onComplete, float slideX = -28f, float slideY = 12f)
    {
        if (IsExiting)
            return;

        IsExiting = true;
        StopMoveRoutine();

        if (exitRoutine != null)
            StopCoroutine(exitRoutine);

        exitRoutine = StartCoroutine(ExitRoutine(onComplete, slideX, slideY));
    }

    private IEnumerator EnterRoutine(Vector2 finalPosition)
    {
        float elapsed = 0f;
        Vector2 start = RectTransform.anchoredPosition;
        float duration = Mathf.Max(0.01f, moveDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);

            RectTransform.anchoredPosition = Vector2.LerpUnclamped(start, finalPosition, eased);

            if (canvasGroup != null)
            {
                float fadeT = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, enterFadeDuration));
                canvasGroup.alpha = fadeT;
            }

            yield return null;
        }

        RectTransform.anchoredPosition = finalPosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        moveRoutine = null;
    }

    private IEnumerator MoveRoutine(Vector2 targetPosition, float duration)
    {
        duration = Mathf.Max(0.01f, duration);

        Vector2 start = RectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);

            RectTransform.anchoredPosition = Vector2.LerpUnclamped(start, targetPosition, eased);
            yield return null;
        }

        RectTransform.anchoredPosition = targetPosition;
        moveRoutine = null;
    }

    private IEnumerator ExitRoutine(System.Action onComplete, float slideX, float slideY)
    {
        Vector2 startPos = RectTransform.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(slideX, slideY);
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, exitDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseInCubic(t);

            RectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        RectTransform.anchoredPosition = endPos;

        onComplete?.Invoke();
    }

    private void StopMoveRoutine()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }
}