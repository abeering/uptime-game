using UnityEngine;

public class VideoCallDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoCallView callPrefab;
    [SerializeField] private Transform callParent;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;
    [SerializeField] private VideoCallData debugCallData;

    private VideoCallData currentCallData;
    private VideoCallView activeCallView;

    public bool IsActive => activeCallView != null;

    public void StartCall(VideoCallData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[VideoCallDirector] StartCall called with null data.");
            return;
        }

        if (callPrefab == null)
        {
            Debug.LogWarning("[VideoCallDirector] Missing callPrefab reference.");
            return;
        }

        DismissCurrentCall();

        currentCallData = data;
        activeCallView = Instantiate(callPrefab, callParent != null ? callParent : transform);

        activeCallView.OnClosePressed += HandleActiveCallClosed;
        activeCallView.Bind(data);
        activeCallView.PlayBody();

        if (logStateChanges)
            Debug.Log($"[VideoCallDirector] Spawned call from {data.CallerName}");
    }

    public void SkipOrDismiss()
    {
        if (activeCallView == null)
            return;

        if (activeCallView.IsTyping)
        {
            activeCallView.SetBodyImmediate();
            return;
        }

        DismissCurrentCall();
    }

    public void DismissCurrentCall()
    {
        if (activeCallView != null)
        {
            activeCallView.OnClosePressed -= HandleActiveCallClosed;
            Destroy(activeCallView.gameObject);
            activeCallView = null;
        }

        currentCallData = null;

        if (logStateChanges)
            Debug.Log("[VideoCallDirector] Dismissed call.");
    }

    [ContextMenu("Debug Start Call")]
    public void DebugStartCall()
    {
        if (debugCallData == null)
        {
            Debug.LogWarning("[VideoCallDirector] No debugCallData assigned.");
            return;
        }

        StartCall(debugCallData);
    }

    private void HandleActiveCallClosed()
    {
        DismissCurrentCall();
    }
}