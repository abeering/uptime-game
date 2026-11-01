using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Connections-owned adaptation of Packet's subtitle typewriter and IsTalking hook.
    // A call is presentation only: no focus, simulation, or incident ownership.
    public sealed class ConnectionsVideoCall : MonoBehaviour, IEscapeTarget
    {
        [SerializeField] private EscapeCoordinator escapeCoordinator;
        [SerializeField] private TMP_Text callerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private GameObject portraitFallback;
        [SerializeField] private GameObject hangUpHint;
        [SerializeField] private Animator portraitAnimator;
        [SerializeField] private string talkingBoolName = "IsTalking";
        [SerializeField, Min(1f)] private float charactersPerSecond = 45f;
        [SerializeField] private ConnectionsVideoCallData sampleCall;
        private Coroutine typeRoutine;
        private bool active;

        public bool IsActive => active && isActiveAndEnabled;
        public bool IsTyping { get; private set; }
        public bool CanDismissWithEscape => IsActive;

        // Duplicate or unavailable requests leave the current call untouched.
        // Future directors supply content; placement and typing remain scene-owned.
        public bool TryStartCall(ConnectionsVideoCallData data)
        {
            if (data == null || active || !enabled || (transform.parent != null && !transform.parent.gameObject.activeInHierarchy) ||
                escapeCoordinator == null || !escapeCoordinator.isActiveAndEnabled)
                return false;
            active = true;
            callerText.text = "CALL / " + (string.IsNullOrWhiteSpace(data.CallerName) ?
                "UNKNOWN CALLER" : data.CallerName);
            portraitImage.sprite = data.Portrait;
            portraitImage.enabled = data.Portrait != null;
            portraitFallback.SetActive(data.Portrait == null);
            portraitAnimator.runtimeAnimatorController = data.PortraitAnimation;
            bodyText.text = data.Body ?? string.Empty;
            bodyText.maxVisibleCharacters = 0;
            bodyText.pageToDisplay = 1;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            escapeCoordinator.Register(this);
            bodyText.ForceMeshUpdate();
            typeRoutine = StartCoroutine(TypeBody());
            return true;
        }

        // Also used by a future director when a conversation ends automatically.
        public void EndCall()
        {
            StopCall();
            gameObject.SetActive(false);
        }

        public void SetEscapeTarget(bool targeted) => hangUpHint.SetActive(targeted && CanDismissWithEscape);
        public void DismissFromEscape() => EndCall(); // Never skip subtitles before hanging up.

        private void OnEnable()
        {
            if (Application.isPlaying && !active) gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            StopCall();
            if (Application.isPlaying && gameObject.activeSelf) gameObject.SetActive(false);
        }
        private void OnDestroy() => StopCall();

        private void StopCall()
        {
            active = false;
            if (escapeCoordinator != null) escapeCoordinator.Unregister(this);
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = null;
            IsTyping = false;
            SetTalking(false);
            if (hangUpHint != null) hangUpHint.SetActive(false);
        }

        private IEnumerator TypeBody()
        {
            int count = bodyText.textInfo.characterCount;
            IsTyping = count > 0;
            SetTalking(IsTyping);
            float revealed = 0f;
            while (bodyText.maxVisibleCharacters < count)
            {
                revealed += Mathf.Max(1f, charactersPerSecond) * Time.unscaledDeltaTime;
                bodyText.maxVisibleCharacters = Mathf.Min(count, Mathf.FloorToInt(revealed));
                if (bodyText.maxVisibleCharacters > 0)
                    bodyText.pageToDisplay = bodyText.textInfo.characterInfo[
                        bodyText.maxVisibleCharacters - 1].pageNumber + 1;
                yield return null;
            }
            IsTyping = false;
            SetTalking(false);
            typeRoutine = null;
        }

        private void SetTalking(bool value)
        {
            if (portraitAnimator == null || portraitAnimator.runtimeAnimatorController == null ||
                string.IsNullOrWhiteSpace(talkingBoolName)) return;
            foreach (AnimatorControllerParameter parameter in portraitAnimator.parameters)
                if (parameter.name == talkingBoolName && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    portraitAnimator.SetBool(talkingBoolName, value);
                    break;
                }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("Start Developer Sample Call")]
        public void StartSampleCall() => TryStartSampleCall();
        public bool TryStartSampleCall() => TryStartCall(sampleCall);
#endif
    }
}
