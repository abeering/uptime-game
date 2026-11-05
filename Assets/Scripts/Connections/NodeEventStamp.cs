using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // A single visual announcement. Status state remains with its existing owner.
    public sealed class NodeEventStamp : MonoBehaviour
    {
        public enum EventKind { Offline, Infected, ScanJammed, PortScanned, Clean, Isolated, Intercept, Caught, Backdoor }

        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Sprite[] symbols;
        [SerializeField] private Color pain = new Color(1f, .37f, .39f);
        [SerializeField] private Color awareness = new Color(.68f, .52f, 1f);
        [SerializeField] private Color beneficial = new Color(.40f, .91f, .76f);
        [SerializeField] private Vector2 size = new Vector2(250f, 300f);
        [SerializeField, Range(1f, 60f)] private float framesPerSecond = 24f;
        [SerializeField, Min(.1f)] private float totalSeconds = 1f;
        [SerializeField, Range(0f, 1f)] private float fadeStart = .57f;
        [SerializeField, Range(0f, 1f)] private float backgroundOpacity = .56f;
        [SerializeField, Range(0f, 1f)] private float iconOpacity = .88f;
        [SerializeField, Range(0f, 1f)] private float iconDelay = .04f;
        [SerializeField, Range(.1f, 1.5f)] private float iconScale = .82f;

        private float startedAt;
        private bool playing;
        private Color tint;

        public bool IsPlaying => playing;
        public EventKind CurrentKind { get; private set; }
        public RectTransform Rect => (RectTransform)transform;

        private void Awake() => Cancel();
        private void Update() => RenderNow(Time.unscaledTime);

        public void Play(EventKind kind)
        {
            if (background == null || icon == null || frames == null || frames.Length == 0 ||
                symbols == null || (int)kind >= symbols.Length || symbols[(int)kind] == null)
                return;
            CurrentKind = kind;
            tint = kind == EventKind.Offline || kind == EventKind.Infected ||
                kind == EventKind.ScanJammed || kind == EventKind.Backdoor ? pain :
                kind == EventKind.PortScanned ? awareness : beneficial;
            startedAt = Time.unscaledTime;
            playing = true;
            Rect.sizeDelta = size;
            icon.sprite = symbols[(int)kind];
            icon.rectTransform.localScale = Vector3.one * iconScale;
            background.gameObject.SetActive(true);
            icon.gameObject.SetActive(true);
            RenderNow(startedAt);
        }

        public void RenderNow(float realTime)
        {
            if (!playing) return;
            float elapsed = Mathf.Max(0f, realTime - startedAt);
            if (elapsed >= totalSeconds) { Cancel(); return; }
            int index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(elapsed * framesPerSecond));
            background.sprite = frames[index];
            float normalized = elapsed / totalSeconds;
            float ghost = normalized <= fadeStart ? 1f :
                1f - (normalized - fadeStart) / Mathf.Max(.001f, 1f - fadeStart);
            // Fast appearance, a brief hold, then a transparent ghost.
            float impact = Mathf.Clamp01(elapsed / .075f);
            background.color = new Color(tint.r, tint.g, tint.b,
                backgroundOpacity * ghost * impact);
            icon.color = new Color(tint.r, tint.g, tint.b,
                iconOpacity * ghost * Mathf.Clamp01((elapsed - iconDelay) / .07f));
        }

        public void Cancel()
        {
            playing = false;
            if (background != null) background.gameObject.SetActive(false);
            if (icon != null) icon.gameObject.SetActive(false);
        }
    }
}
