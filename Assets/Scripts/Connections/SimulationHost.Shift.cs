using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // ShiftComplete advances the future shift sequence; Fired/CoreBreached end a run.
    // All three stop this single-shift prototype through the legacy RunEnded seam.
    public enum RunEnding { None, ShiftComplete, Fired, CoreBreached }

    public sealed class BusinessWorkCredit
    {
        internal BusinessWorkCredit(LogicalConnectionState state, float time, double total)
        { ConnectionId = state.ConnectionId; Work = state.BusinessWorkValue; TimeSeconds = time; CumulativeWork = total; }
        public string ConnectionId { get; }
        public double Work { get; }
        public float TimeSeconds { get; }
        public double CumulativeWork { get; }
    }

    [Serializable] public sealed class ShiftSnapshot
    {
        public bool enabled;
        public float target, timeSeconds;
        public int failureLimit, failures, completedJobs;
        public double credited, eligible, lost, unfinished;
        public string ending;
        public bool shiftSucceeded, runFailed;
    }

    public sealed partial class SimulationHost
    {
        [Header("Business reliability / single shift (opt out for historical fixtures)")]
        [SerializeField] private bool shiftEnabled;
        [SerializeField, Min(.01f)] private float shiftWorkTarget = 1000f;
        [SerializeField, Min(1)] private int businessFailureLimit = 10;
        private readonly Dictionary<string, BusinessWorkCredit> workCredits =
            new Dictionary<string, BusinessWorkCredit>(StringComparer.OrdinalIgnoreCase);
        private double creditedWork;
        private RunEnding runEnding;
        public bool ShiftEnabled => shiftEnabled;
        public float ShiftWorkTarget => shiftWorkTarget;
        public int BusinessFailureLimit => businessFailureLimit;
        public double CreditedWork => creditedWork;
        public int WorkCreditCount => workCredits.Count;
        public int WorkCreditRevision { get; private set; }
        public double LastWorkCredit { get; private set; }
        public RunEnding Ending => runEnding;
        public bool RunEnded => runEnding != RunEnding.None;
        public bool ShiftSucceeded => runEnding == RunEnding.ShiftComplete;
        public bool RunFailed => runEnding == RunEnding.Fired || runEnding == RunEnding.CoreBreached;
        public string EndingTitle => runEnding == RunEnding.ShiftComplete ? "SHIFT COMPLETE" :
            runEnding == RunEnding.Fired ? "FIRED" : runEnding == RunEnding.CoreBreached ? "CORE BREACHED" : "SHIFT IN PROGRESS";
        public string RunEndedMessage => EndingTitle + "  /  type restart";
        public string RunEndedError => runEnding == RunEnding.CoreBreached ? "core_breached" : "shift_ended";
        public event Action RunFinished;
        public event Action<BusinessWorkCredit> BusinessWorkCredited;

        private void CreditSuccessfulConnection(LogicalConnectionState state)
        {
            if (RunEnded || state.IsHostile || state.Resolution != ConnectionResolution.Completed ||
                workCredits.ContainsKey(state.ConnectionId)) return;
            creditedWork += state.BusinessWorkValue;
            var receipt = new BusinessWorkCredit(state, simulationTimeSeconds, creditedWork);
            workCredits.Add(state.ConnectionId, receipt);
            LastWorkCredit = receipt.Work; WorkCreditRevision++;
            recorder?.RecordWorkCredit(receipt);
            BusinessWorkCredited?.Invoke(receipt);
        }

        public ShiftSnapshot Shift
        {
            get
            {
                var snapshot = new ShiftSnapshot { enabled = shiftEnabled, target = shiftWorkTarget,
                    failureLimit = businessFailureLimit, failures = LegitimateFailureCount,
                    completedJobs = workCredits.Count, timeSeconds = simulationTimeSeconds,
                    credited = creditedWork, ending = runEnding.ToString(), shiftSucceeded = ShiftSucceeded, runFailed = RunFailed };
                foreach (var state in history.Values)
                {
                    if (state.IsHostile || workCredits.ContainsKey(state.ConnectionId)) continue;
                    if (state.Resolution.HasValue) snapshot.lost += state.BusinessWorkValue;
                    else snapshot.unfinished += state.BusinessWorkValue;
                }
                snapshot.eligible = snapshot.credited + snapshot.lost + snapshot.unfinished;
                return snapshot;
            }
        }

        // Accounting reset is separate from topology/traffic/Integrity resets. A future
        // shift coordinator can reuse it with explicit new-shift job/history scoping.
        // Only ResetSimulation invokes it in this single-shift implementation.
        private void ResetShiftAccounting()
        {
            runEnding = RunEnding.None; creditedWork = 0; workCredits.Clear();
            businessFailures.Clear(); LastWorkCredit = 0; WorkCreditRevision = 0;
            if (float.IsNaN(shiftWorkTarget) || float.IsInfinity(shiftWorkTarget) || shiftWorkTarget <= 0)
                throw new InvalidOperationException("WORK target must be finite and positive.");
            if (businessFailureLimit < 1) throw new InvalidOperationException("Failure limit must be positive.");
        }

        private void CompleteShiftBoundary()
        {
            if (!shiftEnabled || RunEnded) return;
            if (LegitimateFailureCount >= businessFailureLimit) FinishRun(RunEnding.Fired);
            else if (creditedWork >= shiftWorkTarget) FinishRun(RunEnding.ShiftComplete);
        }

        // Nested existing gameplay transactions settle their effects/receipts before
        // observers may evaluate an ending. No scope objects or alternate clocks.
        internal bool BeginBoundaryMutation()
        { bool previous = steppingIntegrity; steppingIntegrity = true; return previous; }
        internal void EndBoundaryMutation(bool previous)
        { steppingIntegrity = previous; CompleteMutationBoundary(); }

        internal void CompleteMutationBoundary()
        { if (!steppingIntegrity) CompleteIntegrityBoundary(); }

        private void FinishRun(RunEnding cause)
        {
            if (RunEnded) return;
            runEnding = cause; // Claim before observer/overlay cleanup can reenter.
            if (duelDirector == null) duelDirector = GetComponent<ConnectionsDuelDirector>();
            if (duelDirector?.Current != null && !duelDirector.Current.Outcome.HasValue)
                recorder?.RecordDuel("duel_aborted", duelDirector.Current, simulationTimeSeconds, reason: "shift_ended");
            recorder?.RecordShiftEnded(Shift);
            try { RunFinished?.Invoke(); }
            finally { recorder?.CaptureSnapshot(simulationTimeSeconds); recorder?.EndRun(); }
        }

        public static string FormatUptime(float seconds)
        {
            long whole = (long)Math.Floor(Math.Max(0, seconds));
            return $"{whole / 60:00}:{whole % 60:00}";
        }
    }
}
