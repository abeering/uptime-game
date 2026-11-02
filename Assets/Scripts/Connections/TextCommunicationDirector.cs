using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Connections-owned adaptation of Packet's newest-first stack, dynamic heights,
    // expiry, eviction and reflow. No simulation, typing focus or Escape authority.
    public sealed class TextCommunicationDirector : MonoBehaviour
    {
        [SerializeField] private RectTransform entriesRoot;
        [SerializeField] private TextCommunicationView entryPrefab;
        [SerializeField] private NetworkHealthPanel workstationClock;
        [SerializeField] private SimulationHost simulationHost;
        [SerializeField, Min(1)] private int maxVisibleMessages = 4;
        [SerializeField, Min(0)] private float gap = 8;
        [SerializeField, Min(1)] private float preferredWidth = 440;
        [SerializeField, Min(.1f)] private float minimumLifetime = 6;
        [SerializeField, Min(.1f)] private float maximumLifetime = 16;
        [SerializeField, Min(1)] private float charactersPerReadingSecond = 24;
        [SerializeField] private Color instantMessageAccent = new(.22f, .79f, .76f);
        [SerializeField] private Color systemNotificationAccent = new(.91f, .68f, .30f);
        private readonly List<TextCommunicationView> active = new();
        private readonly List<TextCommunicationView> exiting = new();
        private Vector2 lastSize;
        public IReadOnlyList<TextCommunicationView> ActiveEntries => active;
        public int ExitingCount => exiting.Count;

        public bool PushMessage(TextCommunication message)
        {
            if (message == null || !isActiveAndEnabled || entriesRoot == null || entryPrefab == null ||
                (message.Type != TextCommunicationType.InstantMessage && message.Type != TextCommunicationType.SystemNotification))
                return false;
            Canvas.ForceUpdateCanvases();
            var entry = Instantiate(entryPrefab, entriesRoot);
            entry.gameObject.SetActive(true);
            double now = Time.unscaledTimeAsDouble;
            entry.Bind(message, message.Type == TextCommunicationType.InstantMessage ? instantMessageAccent : systemNotificationAccent,
                message.Timestamp ?? workstationClock?.ClockText, Mathf.Min(preferredWidth, entriesRoot.rect.width),
                entriesRoot.rect.height, LifetimeFor(message), now);
            active.Insert(0, entry);
            FitAndReflow(now);
            return true;
        }

        public float LifetimeFor(TextCommunication message)
        {
            if (message.LifetimeSeconds.HasValue && !float.IsNaN(message.LifetimeSeconds.Value) &&
                !float.IsInfinity(message.LifetimeSeconds.Value) && message.LifetimeSeconds.Value > 0)
                return message.LifetimeSeconds.Value;
            return Mathf.Clamp(minimumLifetime + (message.Body?.Length ?? 0) / Mathf.Max(1, charactersPerReadingSecond),
                minimumLifetime, Mathf.Max(minimumLifetime, maximumLifetime));
        }

        private void OnEnable()
        {
            if (simulationHost != null) simulationHost.SimulationReset += Clear;
        }
        private void OnDisable()
        {
            if (simulationHost != null) simulationHost.SimulationReset -= Clear;
            Clear();
        }
        private void OnDestroy() => Clear();

        public void Clear()
        {
            foreach (var entry in active) Remove(entry);
            foreach (var entry in exiting) Remove(entry);
            active.Clear(); exiting.Clear();
        }
        private static void Remove(TextCommunicationView entry)
        {
            if (entry == null) return;
            entry.gameObject.SetActive(false);
            Destroy(entry.gameObject);
        }
        private void Retire(int index, double now)
        {
            var entry = active[index]; active.RemoveAt(index);
            entry.Retire(now); exiting.Add(entry);
            // Bound transient departures during synchronous/rapid bursts too.
            while (exiting.Count > Mathf.Max(1, maxVisibleMessages))
            { Remove(exiting[0]); exiting.RemoveAt(0); }
        }
        private void Update()
        {
            if (entriesRoot == null) return;
            double now = Time.unscaledTimeAsDouble;
            bool changed = lastSize != entriesRoot.rect.size;
            for (int i = active.Count - 1; i >= 0; i--)
                if (now >= active[i].ExpiresAt) { Retire(i, now); changed = true; }
            for (int i = exiting.Count - 1; i >= 0; i--)
                if (exiting[i].ExitFinished(now)) { Remove(exiting[i]); exiting.RemoveAt(i); }
            if (changed) FitAndReflow(now);
            foreach (var entry in active) entry.Animate(now);
            foreach (var entry in exiting) entry.Animate(now);
        }
        private void FitAndReflow(double now)
        {
            lastSize = entriesRoot.rect.size;
            foreach (var entry in active)
                entry.Resize(Mathf.Min(preferredWidth, entriesRoot.rect.width), entriesRoot.rect.height);
            float total = 0;
            for (int i = 0; i < active.Count; i++) total += active[i].Height + (i > 0 ? gap : 0);
            while (active.Count > 1 && (active.Count > Mathf.Max(1, maxVisibleMessages) || total > entriesRoot.rect.height))
            { total -= active[^1].Height + gap; Retire(active.Count - 1, now); }
            float y = 0;
            foreach (var entry in active)
            { entry.MoveTo(new Vector2(0, -y), now); y += entry.Height + gap; }
        }

        // Soft placement considers actual readable parts of live entries, never a
        // permanent exclusion zone. Include departures only while they remain readable.
        public IEnumerable<RectTransform> ReadableContent()
        {
            foreach (var entry in active)
                foreach (var part in entry.ReadableContent) yield return part;
            foreach (var entry in exiting)
                foreach (var part in entry.ReadableContent) yield return part;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("Push Developer Instant Message")]
        public void PushSampleInstantMessage() => PushSample("im");
        [ContextMenu("Push Developer System Notification")]
        public void PushSampleSystemNotification() => PushSample("system");
        public void PushSample(string kind)
        {
            if (kind == "burst")
            {
                PushSample("im"); PushSample("system");
                PushMessage(new TextCommunication { Sender = "Operations / demo", Body = "Demonstration only.\nThe handoff notes are ready when you are." });
                PushMessage(new TextCommunication { Sender = "CEO / demo", Body = "Demo message: thanks for covering the late shift. I'll check back before the sale rehearsal." });
                PushSample("im"); return;
            }
            PushMessage(new TextCommunication {
                Type = kind == "system" ? TextCommunicationType.SystemNotification : TextCommunicationType.InstantMessage,
                Sender = kind == "system" ? "Workstation / demo" : "Sarah Mitchell / demo",
                Body = kind == "system" ? "Developer sample: service notification delivery preview. This is not a report about the live network." :
                    "Demo message: we're rehearsing the sale in five minutes. Ready? This is authored test content, not a live event."
            });
        }
#endif
    }
}
