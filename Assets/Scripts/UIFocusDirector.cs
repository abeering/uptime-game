using UnityEngine;

public class UIFocusDirector : MonoBehaviour
{
    public static UIFocusDirector Instance { get; private set; }

    [Header("Focus Targets")]
    public ConsoleController consoleController;
    public CleanMinigameOverlayView cleanOverlay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RefreshFocus()
    {
        if (cleanOverlay != null && cleanOverlay.WantsFocus())
        {
            cleanOverlay.ForceFocus();
            return;
        }

        consoleController?.ForceFocus();
    }
}