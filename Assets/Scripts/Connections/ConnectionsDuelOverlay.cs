using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // The authored Command Race window owns typing while its encounter is active.
    public sealed class ConnectionsDuelOverlay : MonoBehaviour, IEscapeTarget,
        IExclusiveEscapeTarget
    {
        [SerializeField] private EscapeCoordinator escapeCoordinator;
        [SerializeField] private ConnectionsWindowPlacement placement;
        [SerializeField] private ConnectionsCleanOverlay cleanOverlay;
        [SerializeField] private Terminal terminal;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text playerCommands;
        [SerializeField] private TMP_Text hackerCommands;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Image playerProgress;
        [SerializeField] private Image hackerProgress;
        [SerializeField, Min(0f)] private float resultSeconds = 1.1f;

        private DuelEncounter encounter;
        private CommandRaceSession race;
        private ConnectionsDuelDirector director;
        private Coroutine resultRoutine;
        private Keyboard subscribedKeyboard;
        private float nextBackspaceAt;
        private bool backspaceHeld;
        private bool waitingForRelease;

        public bool IsOpen => encounter != null && gameObject.activeInHierarchy;
        public bool CanDismissWithEscape => IsOpen;
        public void SetEscapeTarget(bool targeted) { }
        public void DismissFromEscape() { } // Consumed; a Duel has no dismissal action.

        public void Open(DuelEncounter encounter, CommandRaceSession race,
            ConnectionsDuelDirector director)
        {
            Abort();
            this.encounter = encounter;
            this.race = race;
            this.director = director;
            gameObject.SetActive(true);
            title.text = "INTRUSION DUEL  /  " + encounter.Target.View.NodeId;
            resultText.gameObject.SetActive(false);
            terminal?.SuspendKeyboard(true);
            cleanOverlay?.SuspendKeyboard(true);
            placement.PlaceDuel(encounter.Target.View);
            transform.SetAsLastSibling();
            escapeCoordinator.Register(this);
            EventSystem.current?.SetSelectedGameObject(gameObject);
            subscribedKeyboard = Keyboard.current;
            waitingForRelease = subscribedKeyboard != null &&
                subscribedKeyboard.anyKey.isPressed;
            if (subscribedKeyboard != null) subscribedKeyboard.onTextInput += OnTextInput;
            Render();
        }

        private void OnTextInput(char character)
        {
            if (encounter == null || encounter.Lifecycle != DuelLifecycle.Active ||
                waitingForRelease) return;
            if (character == '\b') return;
            race.TypePlayer(character);
            Render();
            if (race.PlayerWon.HasValue) Complete();
        }

        private void Update()
        {
            if (encounter == null || encounter.Lifecycle != DuelLifecycle.Active) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != subscribedKeyboard)
            {
                if (subscribedKeyboard != null) subscribedKeyboard.onTextInput -= OnTextInput;
                subscribedKeyboard = keyboard;
                if (keyboard != null) keyboard.onTextInput += OnTextInput;
            }
            if (waitingForRelease)
            {
                if (keyboard != null && keyboard.anyKey.isPressed) return;
                waitingForRelease = false;
            }
            bool pressed = keyboard != null && keyboard.backspaceKey.isPressed;
            if (!pressed) backspaceHeld = false;
            else if (!backspaceHeld || Time.unscaledTime >= nextBackspaceAt)
            {
                backspaceHeld = true;
                nextBackspaceAt = Time.unscaledTime + .075f;
                race.BackspacePlayer();
                Render();
            }
            race.Advance(Time.unscaledDeltaTime);
            Render();
            if (race.PlayerWon.HasValue) Complete();
        }

        private void LateUpdate()
        {
            if (IsOpen && transform.GetSiblingIndex() != transform.parent.childCount - 1)
                transform.SetAsLastSibling();
        }

        private void Complete()
        {
            if (encounter == null || encounter.Lifecycle != DuelLifecycle.Active) return;
            director.Resolve(race);
            if (subscribedKeyboard != null) subscribedKeyboard.onTextInput -= OnTextInput;
            subscribedKeyboard = null;
            resultText.text = encounter.PlayerWon == true ? "DUEL WON  /  INTRUSION STOPPED" :
                encounter.InfectionApplied == true ? "DUEL LOST  /  INTRUSION SUCCEEDED" :
                "DUEL LOST  /  INTRUSION BLOCKED";
            resultText.gameObject.SetActive(true);
            terminal?.DuelReceipt(encounter);
            resultRoutine = StartCoroutine(ShowResult());
        }

        private IEnumerator ShowResult()
        {
            yield return new WaitForSecondsRealtime(resultSeconds);
            // Do not transfer a held key to CLEAN or the terminal.
            while (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
                yield return null;
            ConnectionsDuelDirector owner = director;
            CloseWindow();
            owner?.FinishResult();
        }

        public void Abort()
        {
            if (encounter == null) return;
            CloseWindow();
        }

        private void CloseWindow()
        {
            if (resultRoutine != null) StopCoroutine(resultRoutine);
            resultRoutine = null;
            if (subscribedKeyboard != null) subscribedKeyboard.onTextInput -= OnTextInput;
            subscribedKeyboard = null;
            escapeCoordinator?.Unregister(this);
            encounter = null;
            race = null;
            director = null;
            gameObject.SetActive(false);
            cleanOverlay?.SuspendKeyboard(false);
            terminal?.SuspendKeyboard(false);
            if (cleanOverlay != null && cleanOverlay.WantsKeyboardFocus)
                cleanOverlay.RestoreKeyboardFocus();
            else terminal?.RestoreKeyboardFocus();
        }

        private void OnDisable()
        {
            if (encounter != null) CloseWindow();
        }

        private void Render()
        {
            if (race == null) return;
            playerCommands.text = Format(race.Player, true);
            hackerCommands.text = Format(race.Hacker, false);
            SetBar(playerProgress, race.Player.Progress);
            SetBar(hackerProgress, race.Hacker.Progress);
        }

        private static void SetBar(Image image, float progress)
        {
            RectTransform fill = image.rectTransform;
            RectTransform track = (RectTransform)fill.parent;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, .5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(track.rect.width * Mathf.Clamp01(progress), 0f);
            image.fillAmount = 1f;
        }

        private static string Format(CommandRaceSession.Competitor competitor, bool player)
        {
            var result = new StringBuilder();
            for (int line = 0; line < competitor.Commands.Count; line++)
            {
                if (line > 0) result.Append('\n');
                string command = competitor.Commands[line];
                bool current = line == competitor.Line;
                if (current && player) result.Append("<mark=#255D4C70>");
                result.Append(current ? player ? "<color=#71E2C1>" :
                    "<color=#F27A7C>" : line < competitor.Line ?
                    "<color=#A5C6BA>" : "<color=#829B97>");
                result.Append("> ");
                result.Append("</color>");
                if (line < competitor.Line)
                    result.Append("<color=#A5C6BA>").Append(Escape(command)).Append("</color>");
                else if (current)
                {
                    bool error = competitor.ErrorStartIndex >= 0;
                    int position = error && !player ? competitor.ErrorStartIndex :
                        competitor.Draft.Length;
                    TokenBounds(command, position, out int tokenStart, out int tokenEnd);
                    for (int i = 0; i < command.Length; i++)
                    {
                        if (i == tokenStart) result.Append("<u>");
                        char ch = command[i];
                        bool errorTail = player && error &&
                            i >= competitor.ErrorStartIndex && i < competitor.Draft.Length;
                        bool correct = player ? i < (error ? competitor.ErrorStartIndex :
                            competitor.Draft.Length) : i < competitor.Draft.Length &&
                            competitor.Draft[i] == ch;
                        bool activeToken = i >= tokenStart && i < tokenEnd;
                        string color = errorTail ? "#FFE8EA" :
                            correct ? player ? "#55E9C1" : "#F27A7C" :
                            i < competitor.Draft.Length && !player ? "#FFBA52" :
                            player ? activeToken ? "#DEF8EC" : "#A9C7BB" :
                            activeToken ? "#F1CECE" : "#B69B9B";
                        if (errorTail) result.Append("<mark=#B4404E80>");
                        else if (player && i == position)
                        {
                            result.Append(error ? "<mark=#E94D5988>" :
                                "<mark=#55E9C140>");
                            color = error ? "#FFF0ED" : "#F5FFFB";
                        }
                        result.Append("<color=").Append(color).Append(">");
                        // The player's reference never changes, even while a wrong key blocks progress.
                        result.Append(Escape(!player && i < competitor.Draft.Length ?
                            competitor.Draft[i].ToString() : ch.ToString()));
                        result.Append("</color>");
                        if (errorTail || (player && i == position))
                            result.Append("</mark>");
                        if (i == tokenEnd - 1) result.Append("</u>");
                    }
                }
                else result.Append("<color=#829B97>").Append(Escape(command)).Append("</color>");
                if (current && player) result.Append("</mark>");
            }
            return result.ToString();
        }

        private static void TokenBounds(string command, int position, out int start, out int end)
        {
            int at = Mathf.Min(position, command.Length - 1);
            while (at < command.Length && char.IsWhiteSpace(command[at])) at++;
            if (at == command.Length)
            {
                at = Mathf.Min(position, command.Length - 1);
                while (at >= 0 && char.IsWhiteSpace(command[at])) at--;
            }
            if (at < 0)
            {
                start = end = -1;
                return;
            }
            start = at;
            while (start > 0 && !char.IsWhiteSpace(command[start - 1])) start--;
            end = at;
            while (end < command.Length && !char.IsWhiteSpace(command[end])) end++;
        }

        private static string Escape(string source) => source.Replace("&", "&amp;")
            .Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
