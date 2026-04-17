using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VideoCallView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("UI")]
    [SerializeField] private TMP_Text callerNameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image portraitFallbackImage;
    [SerializeField] private Button closeButton;

    [Header("Typewriter")]
    [SerializeField] private float charactersPerSecond = 45f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Talking")]
    [SerializeField] private Animator portraitAnimator;
    [SerializeField] private string talkingBoolName = "IsTalking";

    private Coroutine typeRoutine;
    private string currentFullText = string.Empty;
    private bool isTyping;

    public bool IsTyping => isTyping;
    public System.Action OnClosePressed;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(HandleClosePressed);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (bodyText != null)
            bodyText.text = string.Empty;
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleClosePressed);
    }

    public void Bind(VideoCallData data)
    {
        string caller = string.IsNullOrWhiteSpace(data?.CallerName) ? "Unknown" : data.CallerName;
        currentFullText = data?.Body ?? string.Empty;

        if (callerNameText != null)
            callerNameText.text = caller;

        if (bodyText != null)
            bodyText.text = string.Empty;

        Sprite portrait = data != null ? data.Portrait : null;

        if (portraitImage != null)
        {
            bool hasPortrait = portrait != null;
            portraitImage.enabled = hasPortrait;
            portraitImage.sprite = portrait;
        }

        if (portraitFallbackImage != null)
            portraitFallbackImage.enabled = portrait == null;

        SetTalking(false);
    }

    public void PlayBody(System.Action onComplete = null)
    {
        if (typeRoutine != null)
            StopCoroutine(typeRoutine);

        typeRoutine = StartCoroutine(TypeRoutine(currentFullText, onComplete));
    }

    public void SetBodyImmediate()
    {
        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        isTyping = false;
        SetTalking(false);

        if (bodyText != null)
            bodyText.text = currentFullText;
    }

    private IEnumerator TypeRoutine(string fullText, System.Action onComplete)
    {
        isTyping = true;
        SetTalking(true);

        if (bodyText != null)
            bodyText.text = string.Empty;

        if (string.IsNullOrEmpty(fullText))
        {
            isTyping = false;
            SetTalking(false);
            typeRoutine = null;
            onComplete?.Invoke();
            yield break;
        }

        float delayPerChar = charactersPerSecond <= 0f ? 0f : 1f / charactersPerSecond;

        for (int i = 0; i < fullText.Length; i++)
        {
            if (bodyText != null)
                bodyText.text = fullText.Substring(0, i + 1);

            if (delayPerChar > 0f)
            {
                if (useUnscaledTime)
                {
                    float elapsed = 0f;
                    while (elapsed < delayPerChar)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSeconds(delayPerChar);
                }
            }
            else
            {
                yield return null;
            }
        }

        isTyping = false;
        SetTalking(false);
        typeRoutine = null;
        onComplete?.Invoke();
    }

    private void SetTalking(bool value)
    {
        if (portraitAnimator != null && !string.IsNullOrWhiteSpace(talkingBoolName))
            portraitAnimator.SetBool(talkingBoolName, value);
    }

    private void HandleClosePressed()
    {
        OnClosePressed?.Invoke();
    }
}