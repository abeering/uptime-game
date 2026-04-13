using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotificationDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform entriesRoot;
    [SerializeField] private NotificationEntryView entryPrefab;
    [SerializeField] private Sprite placeholderAvatar;

    [Header("Behavior")]
    [SerializeField] private int maxVisibleMessages = 4;
    [SerializeField] private float lingerSeconds = 4.0f;
    [SerializeField] private float verticalSpacing = 8f;
    [SerializeField] private float entrySpawnOffsetY = 24f;

    [Header("Top-Left Padding")]
    [SerializeField] private float leftPadding = 0f;
    [SerializeField] private float topPadding = 0f;

    [Header("Debug")]
    [SerializeField] private bool logPushes = false;

    private readonly List<NotificationEntryView> activeEntries = new();
    private readonly Dictionary<NotificationEntryView, Coroutine> lifetimeCoroutines = new();

    public void PushMessage(Notification message)
    {
        if (message == null)
            return;

        if (entriesRoot == null)
        {
            Debug.LogWarning("[NotificationDirector] Missing entriesRoot.");
            return;
        }

        if (entryPrefab == null)
        {
            Debug.LogWarning("[NotificationDirector] Missing entryPrefab.");
            return;
        }

        NotificationEntryView entry = Instantiate(entryPrefab, entriesRoot);
        entry.gameObject.SetActive(true);
        entry.Bind(message, placeholderAvatar);

        entry.transform.SetAsLastSibling();
        activeEntries.Insert(0, entry);

        Vector2 finalPosition = GetAnchoredPositionForIndex(0);
        entry.PlayEnter(finalPosition, entrySpawnOffsetY);

        RefreshStack(skipEntry: entry);

        Coroutine life = StartCoroutine(LifetimeRoutine(entry, lingerSeconds));
        lifetimeCoroutines[entry] = life;

        if (activeEntries.Count > maxVisibleMessages)
        {
            NotificationEntryView oldest = activeEntries[activeEntries.Count - 1];
            RemoveEntry(oldest, immediateFromList: true);
        }

        if (logPushes)
            Debug.Log($"[NotificationDirector] Pushed message from {message.SpeakerName}");
    }

    public void PushDebugMessage(string speakerName, string body, Sprite avatar = null)
    {
        PushMessage(new Notification
        {
            SpeakerName = speakerName,
            Body = body,
            Avatar = avatar
        });
    }

    [ContextMenu("Push Debug Message")]
    public void PushDebugMessageContextMenu()
    {
        PushMessage(new Notification
        {
            SpeakerName = "CEO",
            Body = "Launching our sale now. Please tell me this holds.",
            Avatar = null
        });
    }

    private IEnumerator LifetimeRoutine(NotificationEntryView entry, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        RemoveEntry(entry, immediateFromList: true);
    }

    private void RemoveEntry(NotificationEntryView entry, bool immediateFromList)
    {
        if (entry == null)
            return;

        if (lifetimeCoroutines.TryGetValue(entry, out Coroutine routine))
        {
            if (routine != null)
                StopCoroutine(routine);

            lifetimeCoroutines.Remove(entry);
        }

        bool removed = activeEntries.Remove(entry);

        if (immediateFromList && removed)
            RefreshStack();

        entry.PlayExit(() =>
        {
            if (entry != null)
                Destroy(entry.gameObject);
        });
    }

    private void RefreshStack(NotificationEntryView skipEntry = null)
    {
        float y = -topPadding;

        for (int i = 0; i < activeEntries.Count; i++)
        {
            NotificationEntryView entry = activeEntries[i];
            if (entry == null)
                continue;

            Vector2 target = new(leftPadding, y);

            if (entry != skipEntry)
                entry.MoveTo(target);

            y -= entry.GetHeight() + verticalSpacing;
        }
    }

    private Vector2 GetAnchoredPositionForIndex(int index)
    {
        float y = -topPadding;

        for (int i = 0; i < index; i++)
        {
            NotificationEntryView prior = activeEntries[i];
            if (prior == null)
                continue;

            y -= prior.GetHeight() + verticalSpacing;
        }

        return new Vector2(leftPadding, y);
    }
}