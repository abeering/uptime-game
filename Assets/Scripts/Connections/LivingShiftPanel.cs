using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    public sealed class LivingShiftPanel : MonoBehaviour
    {
        [SerializeField] private LivingShiftDirector director;
        [SerializeField] private GameObject objectiveRoot;
        [SerializeField] private TMP_Text objectiveText, paceText;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private TMP_Text resultText, summaryText;
        [SerializeField] private Button restartButton;
        private bool showingResult;
        private void OnEnable() { if (restartButton != null) restartButton.onClick.AddListener(Restart); Render(); }
        private void OnDisable() { if (restartButton != null) restartButton.onClick.RemoveListener(Restart); }
        private void Restart() => director.Restart();
        private void Update() => Render();
        public void Render()
        {
            if (objectiveRoot == null || resultRoot == null) return;
            bool visible = director != null && director.IsEnabled && (director.IsRunning || director.IsFinished);
            objectiveRoot.SetActive(visible);
            bool finished = visible && director.IsFinished;
            resultRoot.SetActive(finished);
            if (finished && !showingResult && Application.isPlaying) restartButton.Select();
            showingResult = finished;
            if (!visible) return;
            int seconds = Mathf.CeilToInt(director.TimeRemaining);
            objectiveText.text = $"SHIFT WORK  {Mathf.FloorToInt(director.CreditedWork)} / {director.Target:0}    {seconds / 60:00}:{seconds % 60:00}";
            paceText.text = director.IsFinished ? "HANDOFF" : director.BehindPace ? "BEHIND PACE" : "CONFIRMED WORK";
            paceText.color = director.BehindPace ? new Color(1f, .65f, .38f) : new Color(.5f, .83f, .76f);
            progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(director.CreditedWork / director.Target), 1f);
            if (!director.IsFinished) return;
            resultText.text = director.Succeeded ? "SHIFT COMPLETE" : "SHIFT FAILED";
            resultText.color = director.Succeeded ? new Color(.5f, .9f, .76f) : new Color(1f, .5f, .4f);
            summaryText.text = director.FinalSummary;
        }
    }
}
