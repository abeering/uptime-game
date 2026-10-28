using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Uptime.Connections
{
    // Authored overlay for the Connections PID-Select session. Simulation stays live.
    public sealed class ConnectionsCleanOverlay : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TMP_InputField pidInput;
        [SerializeField, Min(0f)] private float characterSeconds = 0.012f;
        [SerializeField, Min(0f)] private float resultSeconds = 1.3f;

        private CleanSession session;
        private SimulationHost host;
        private Terminal terminal;
        private Coroutine sequence;
        private bool acceptingPid;
        private bool showingResult;

        public bool IsOpen => gameObject.activeInHierarchy && session != null;

        private void Awake()
        {
            if (pidInput != null) pidInput.onSubmit.AddListener(Submit);
        }

        private void OnDestroy()
        {
            if (pidInput != null) pidInput.onSubmit.RemoveListener(Submit);
        }

        public void Open(CleanSession cleanSession, SimulationHost simulationHost,
            Terminal sourceTerminal)
        {
            session = cleanSession;
            host = simulationHost;
            terminal = sourceTerminal;
            acceptingPid = false;
            showingResult = false;
            gameObject.SetActive(true);
            title.text = "PROCESS KILLER  /  " + session.NodeId;
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
            if (!showingResult && Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                host.CancelClean();
                Close("CLEAN CANCELED  " + session.NodeId, true);
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
            listing.Append("\nENTER PID TO TERMINATE   /   ESC TO CANCEL");
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
            pidInput.gameObject.SetActive(false);
            bool success = result == CleanSelectionResult.Success;
            string feedback = success ? "PROCESS TERMINATED  /  CLEAN COMPLETE" :
                "INNOCENT PROCESS TERMINATED  /  CLEAN FAILED";
            body.text = feedback;
            string terminalMessage = success ?
                "CLEAN COMPLETE  " + session.NodeId + "  /  " + removed + " infection(s) removed" :
                "CLEAN FAILED  " + session.NodeId + "  /  innocent process terminated";
            sequence = StartCoroutine(ShowResult(terminalMessage, success));
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

        private IEnumerator ShowResult(string message, bool success)
        {
            yield return new WaitForSecondsRealtime(resultSeconds);
            Close(message, success);
        }

        private void Close(string message, bool success)
        {
            if (sequence != null) StopCoroutine(sequence);
            sequence = null;
            session = null;
            gameObject.SetActive(false);
            terminal?.FinishClean(message, success);
        }
    }
}
