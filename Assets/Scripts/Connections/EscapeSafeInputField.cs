using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Uptime.Connections
{
    // TMP 3's native event loop adapted to reserve Escape for the coordinator.
    // Editing, selection and submission still use TMP's own protected handlers.
    public sealed class EscapeSafeInputField : TMP_InputField
    {
        private readonly Event inputEvent = new();
        private bool wasComposing;

        // InputSystemUIInputModule also dispatches ICancelHandler independently
        // of the native event queue; that path must not cancel editing either.
        public override void OnCancel(BaseEventData eventData) => eventData.Use();

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (!isFocused) return;
            bool consumed = false;
            BaseInput input = EventSystem.current?.currentInputModule?.input;
            bool composing = !readOnly && !string.IsNullOrEmpty(input != null ?
                input.compositionString : Input.compositionString);
            while (Event.PopEvent(inputEvent))
            {
                if (inputEvent.rawType == EventType.KeyUp) continue;
                if (inputEvent.rawType == EventType.KeyDown)
                {
                    consumed = true;
                    if (inputEvent.keyCode == KeyCode.Escape) continue;
                    // Preserve TMP's macOS suppression immediately after IME commit.
                    if (wasComposing && !composing && inputEvent.character == 0 &&
                        inputEvent.modifiers == EventModifiers.None) continue;
                    if (KeyPressed(inputEvent) == EditState.Finish)
                    {
                        if (!wasCanceled) SendOnSubmit();
                        DeactivateInputField();
                        break;
                    }
                    UpdateLabel();
                    // TMP's loop sets a private mesh-dirty flag before UpdateLabel.
                    // Force the same refresh through its public text component API.
                    textComponent.ForceMeshUpdate();
                }
                else if ((inputEvent.rawType == EventType.ValidateCommand ||
                    inputEvent.rawType == EventType.ExecuteCommand) && inputEvent.commandName == "SelectAll")
                {
                    SelectAll();
                    consumed = true;
                }
            }
            if (consumed || composing)
            {
                UpdateLabel();
                eventData.Use();
            }
            wasComposing = composing;
        }
    }
}
