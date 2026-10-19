using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.ConnectionsToy
{
    public sealed class ConnectionsToyTerminal : MonoBehaviour
    {
        [SerializeField] private TMP_InputField commandInput;
        [SerializeField] private ScrollRect historyScroll;
        [SerializeField] private RectTransform historyContent;
        [SerializeField] private ConnectionsToyTextRow textRowPrefab;
        [SerializeField] private ConnectionsToyPlaceholderOutput placeholderPrefab;
        [SerializeField, Min(1)] private int maxEntries = 100;
        [SerializeField] private Color normalColor = new(0.73f, 0.89f, 0.84f);
        [SerializeField] private Color mutedColor = new(0.47f, 0.63f, 0.62f);
        [SerializeField] private Color errorColor = new(1f, 0.63f, 0.39f);

        private int entryCount;
        private bool scrollPending;

        private void Awake()
        {
            if (commandInput != null)
                commandInput.onSubmit.AddListener(HandleSubmit);
        }

        private void Start()
        {
            AppendText("CONNECTIONS TOY // TERMINAL READY", normalColor);
            AppendText("Type test to render the output block.", mutedColor);
            StartCoroutine(FocusInputNextFrame());
        }

        private void OnDestroy()
        {
            if (commandInput != null)
                commandInput.onSubmit.RemoveListener(HandleSubmit);
        }

        private void HandleSubmit(string raw)
        {
            if (commandInput == null || string.IsNullOrWhiteSpace(raw))
            {
                StartCoroutine(FocusInputNextFrame());
                return;
            }

            string command = raw.Trim();
            commandInput.SetTextWithoutNotify(string.Empty);
            AppendText($"> {command}", normalColor);

            switch (command.ToLowerInvariant())
            {
                case "test":
                    AppendText("TEST // structured output follows", mutedColor);
                    ConnectionsToyPlaceholderOutput block = AppendOutputBlock(placeholderPrefab);
                    if (block != null)
                        block.Bind("PRESENTATION CHECK", "Authored UI block inside terminal history.", "PHASE 0", "STATIC", 0.42f);
                    break;
                case "help":
                    AppendText("COMMANDS  test  help", mutedColor);
                    break;
                default:
                    AppendText($"UNKNOWN COMMAND  {command}  // type help", errorColor);
                    break;
            }

            StartCoroutine(FocusInputNextFrame());
        }

        public ConnectionsToyTextRow AppendText(string message, Color color)
        {
            ConnectionsToyTextRow row = AppendOutputBlock(textRowPrefab);
            if (row != null)
                row.Bind(message, color);
            return row;
        }

        public T AppendOutputBlock<T>(T prefab) where T : Component
        {
            if (prefab == null || historyContent == null)
                return null;

            T instance = Instantiate(prefab, historyContent);
            entryCount++;
            while (entryCount > maxEntries && historyContent.childCount > 0)
            {
                Transform oldest = historyContent.GetChild(0);
                oldest.SetParent(null, false);
                Destroy(oldest.gameObject);
                entryCount--;
            }

            if (!scrollPending)
                StartCoroutine(ScrollToBottomNextFrame());
            return instance;
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            scrollPending = true;
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (historyContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(historyContent);
            if (historyScroll != null)
                historyScroll.verticalNormalizedPosition = 0f;
            scrollPending = false;
        }

        private IEnumerator FocusInputNextFrame()
        {
            yield return null;
            if (commandInput == null)
                yield break;
            commandInput.ActivateInputField();
            commandInput.Select();
        }
    }
}
