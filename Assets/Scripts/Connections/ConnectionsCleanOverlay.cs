using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace Uptime.Connections
{
    // Authored overlay for the Connections PID-Select session. Simulation stays live.
    public sealed class ConnectionsCleanOverlay : MonoBehaviour, IEscapeTarget
    {
        [SerializeField] private EscapeCoordinator escapeCoordinator;
        [SerializeField] private ConnectionsWindowPlacement placement;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private GameObject cancelHint;
        [SerializeField] private TMP_InputField pidInput;
        [SerializeField, Min(0f)] private float characterSeconds = 0.012f;
        [SerializeField, Min(0f), Tooltip("Result visibility in real seconds; terminal typing resumes on submission.")]
        private float resultSeconds = 1.1f;

        private CleanSession session;
        private SimulationHost host;
        private Terminal terminal;
        private Coroutine sequence;
        private bool acceptingPid;
        private bool showingResult;

        public bool IsOpen => gameObject.activeInHierarchy && session != null;

        // The completed result can remain visible after typing returns to the terminal.
        public bool WantsKeyboardFocus => IsOpen && !showingResult;

        public bool CanDismissWithEscape => IsOpen && !showingResult;

        public void SetEscapeTarget(bool targeted) => cancelHint.SetActive(targeted && CanDismissWithEscape);

        public void DismissFromEscape()
        {
            if (!CanDismissWithEscape) return;
            host.CancelClean();
            Close("CLEAN CANCELED  " + session.NodeId, true);
        }

        private void OnDisable()
        {
            if (escapeCoordinator != null) escapeCoordinator.Unregister(this);
            if (session == null) return;
            if (!showingResult && host != null && host.ActiveClean == session) host.CancelClean();
            string message = "CLEAN SESSION ENDED  " + session.NodeId;
            session = null;
            if (sequence != null) StopCoroutine(sequence);
            sequence = null;
            if (!showingResult && terminal != null && terminal.isActiveAndEnabled)
                terminal.FinishClean(message, false);
        }

        private void Awake()
        {
            if (pidInput != null) pidInput.onSubmit.AddListener(Submit);
        }

        private void OnDestroy()
        {
            if (escapeCoordinator != null) escapeCoordinator.Unregister(this);
            if (pidInput != null) pidInput.onSubmit.RemoveListener(Submit);
        }

        public void Open(CleanSession cleanSession, SimulationHost simulationHost,
            Terminal sourceTerminal)
        {
            // A new command may replace a lingering result. Retire its close timer first.
            if (sequence != null) StopCoroutine(sequence);
            sequence = null;
            session = cleanSession;
            host = simulationHost;
            terminal = sourceTerminal;
            acceptingPid = false;
            showingResult = false;
            gameObject.SetActive(true);
            title.text = "PROCESS KILLER  /  " + session.NodeId;
            placement.PlaceClean(session.Target.View);
            transform.SetAsLastSibling();
            escapeCoordinator.Register(this);
            pidInput.gameObject.SetActive(false);
            sequence = StartCoroutine(Splash());
        }

        private void Update()
        {
            if (session == null) return;
            if (!showingResult && (host == null || host.ActiveClean != session))
            {
                Close("CLEAN SESSION ENDED  " + session.NodeId, false);
                return;
            }
        }

        private IEnumerator Splash()
        {
            string text = "ssh operator@" + session.NodeId.ToLowerInvariant() +
                "\nAUTHENTICATING... OK\nLOADING PROCESS KILLER...";
            body.text = string.Empty;
            foreach (char character in text)
            {
                body.text += character;
                yield return new WaitForSecondsRealtime(characterSeconds);
            }
            yield return new WaitForSecondsRealtime(0.25f);
            var listing = new StringBuilder("PID       CPU       PROCESS\n\n");
            foreach (CleanProcess process in session.Processes)
                listing.AppendFormat("{0,-9} {1,2}%       {2}\n", process.Pid,
                    process.CpuPercent, process.Name);
            listing.Append("\nENTER PID TO TERMINATE");
            body.text = listing.ToString();
            pidInput.SetTextWithoutNotify(string.Empty);
            pidInput.gameObject.SetActive(true);
            acceptingPid = true;
            yield return null;
            pidInput.ActivateInputField();
            pidInput.Select();
        }

        private void Submit(string raw)
        {
            if (!acceptingPid || session == null) return;
            pidInput.SetTextWithoutNotify(string.Empty);
            if (!int.TryParse(raw.Trim(), out int pid))
            {
                body.text += "\nINVALID PID  /  SELECT A LISTED PROCESS";
                RefocusPid();
                return;
            }
            CleanSelectionResult result = host.SelectCleanPid(pid, out int removed);
            if (result == CleanSelectionResult.NoSession)
            {
                Close("CLEAN SESSION ENDED  " + session.NodeId, false);
                return;
            }
            if (result == CleanSelectionResult.InvalidPid)
            {
                body.text += "\nINVALID PID  /  SELECT A LISTED PROCESS";
                RefocusPid();
                return;
            }
            acceptingPid = false;
            showingResult = true;
            escapeCoordinator.Unregister(this);
            cancelHint.SetActive(false);
            pidInput.gameObject.SetActive(false);
            bool success = result == CleanSelectionResult.Success;
            string feedback = success ? "PROCESS TERMINATED  /  CLEAN COMPLETE" :
                "INNOCENT PROCESS TERMINATED  /  CLEAN FAILED";
            body.text = feedback;
            string terminalMessage = success ?
                "CLEAN COMPLETE  " + session.NodeId + "  /  " + removed + " infection(s) removed" :
                "CLEAN FAILED  " + session.NodeId + "  /  innocent process terminated";
            if (terminal != null && terminal.isActiveAndEnabled)
                terminal.FinishClean(terminalMessage, success);
            sequence = StartCoroutine(ShowResult());
        }

        private void RefocusPid() => StartCoroutine(RefocusNextFrame());

        private IEnumerator RefocusNextFrame()
        {
            yield return null;
            if (acceptingPid && pidInput != null)
            {
                pidInput.ActivateInputField();
                pidInput.Select();
            }
        }

        private IEnumerator ShowResult()
        {
            yield return new WaitForSecondsRealtime(resultSeconds);
            Close();
        }

        private void Close(string message = null, bool success = false)
        {
            escapeCoordinator.Unregister(this);
            if (sequence != null) StopCoroutine(sequence);
            sequence = null;
            session = null;
            gameObject.SetActive(false);
            if (message != null && terminal != null && terminal.isActiveAndEnabled)
                terminal.FinishClean(message, success);
        }
    }
}
