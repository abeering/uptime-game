using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    public enum RunEnding { None, WorkComplete, InsufficientWork, CoreBreached }

    [Serializable] public sealed class ShiftSnapshot
    {
        public bool enabled;
        public float durationSeconds, timeSeconds, target;
        public double credited, eligible, lost, unfinished;
        public string ending;
    }

    public sealed partial class SimulationHost
    {
        [Header("WORK objective (opt-in for historical witnesses)")]
        [SerializeField] private bool shiftEnabled;
        [SerializeField, Min(.01f)] private float shiftDurationSeconds = 360f;
        [SerializeField, Min(.01f)] private float shiftWorkTarget = 1000f;
        private readonly Dictionary<string, double> workCredits = new Dictionary<string, double>();
        private double creditedWork;
        private RunEnding runEnding;
        public bool ShiftEnabled => shiftEnabled;
        public float ShiftDurationSeconds => shiftDurationSeconds;
        public float ShiftWorkTarget => shiftWorkTarget;
        public double CreditedWork => creditedWork;
        public RunEnding Ending => runEnding;
        public bool RunEnded => runEnding != RunEnding.None;
        public string EndingTitle => runEnding == RunEnding.WorkComplete ? "SHIFT COMPLETE" :
            runEnding == RunEnding.InsufficientWork ? "SHIFT FAILED — INSUFFICIENT WORK" :
            runEnding == RunEnding.CoreBreached ? "CORE BREACHED" : "SHIFT IN PROGRESS";
        public string RunEndedMessage => EndingTitle + "  /  type restart";
        public event Action<LogicalConnectionState, int, float> OperationCompleted;
        public event Action RunFinished;
        public int WorkCreditCount => workCredits.Count;
        public float LastWorkCredit { get; private set; }
        public int WorkCreditRevision { get; private set; }

        private static string WorkKey(LogicalConnectionState state, int index) => state.ConnectionId + "/" + index;

        // Called only from the allocator's successful completion path, before any effects
        // or resolution. Neither subscribers nor recorder/render refreshes grant credit.
        private void CreditCompletedOperation(LogicalConnectionState state, int index, float work)
        {
            if (state.IsHostile || RunEnded || workCredits.ContainsKey(WorkKey(state, index))) return;
            if (float.IsNaN(work) || float.IsInfinity(work) || work < 0f)
                throw new InvalidOperationException("Completed WORK must be finite and nonnegative.");
            workCredits.Add(WorkKey(state, index), work);
            creditedWork += work;
            LastWorkCredit = work;
            WorkCreditRevision++;
            recorder?.RecordWorkCredit(state, index, work, creditedWork, simulationTimeSeconds);
        }

        public ShiftSnapshot Shift
        {
            get
            {
                var snapshot = new ShiftSnapshot { enabled = shiftEnabled, durationSeconds = shiftDurationSeconds,
                    target = shiftWorkTarget, timeSeconds = simulationTimeSeconds, credited = creditedWork,
                    ending = runEnding.ToString() };
                foreach (LogicalConnectionState state in history.Values)
                {
                    if (state.IsHostile) continue;
                    for (int index = 0; index < state.OperationCount; index++)
                    {
                        if (workCredits.ContainsKey(WorkKey(state, index))) continue;
                        double uncredited = index == state.CurrentOperationIndex ?
                            Math.Max(state.CurrentTotalWork, state.CompletedWork) : state.Definition.WorkAt(index);
                        if (state.Resolution.HasValue) snapshot.lost += uncredited;
                        else snapshot.unfinished += uncredited;
                    }
                }
                snapshot.eligible = snapshot.credited + snapshot.lost + snapshot.unfinished;
                return snapshot;
            }
        }

        private void ResetShift()
        {
            runEnding = RunEnding.None;
            creditedWork = 0;
            workCredits.Clear();
            LastWorkCredit = 0;
            WorkCreditRevision = 0;
            if (float.IsNaN(shiftDurationSeconds) || float.IsInfinity(shiftDurationSeconds) || shiftDurationSeconds <= 0)
                throw new InvalidOperationException("Shift duration must be finite and positive.");
            if (float.IsNaN(shiftWorkTarget) || float.IsInfinity(shiftWorkTarget) || shiftWorkTarget <= 0)
                throw new InvalidOperationException("WORK target must be finite and positive.");
        }

        private void CompleteShiftBoundary()
        {
            if (!shiftEnabled || RunEnded) return;
            // CORE has already been evaluated, after all effects on this boundary.
            if (creditedWork >= shiftWorkTarget) FinishRun(RunEnding.WorkComplete);
            else if (simulationTimeSeconds >= shiftDurationSeconds) FinishRun(RunEnding.InsufficientWork);
        }

        private void FinishRun(RunEnding cause)
        {
            if (RunEnded) return;
            runEnding = cause; // Claim once before observers/overlay cleanup can reenter.
            if (duelDirector == null) duelDirector = GetComponent<ConnectionsDuelDirector>();
            if (duelDirector?.Current != null)
                recorder?.RecordDuel("duel_aborted", duelDirector.Current, simulationTimeSeconds, reason: "run_ended");
            recorder?.RecordShiftEnded(Shift);
            // Preserve model sessions for diagnostics; presentation aborts without penalties.
            try { RunFinished?.Invoke(); }
            finally
            {
                recorder?.CaptureSnapshot(simulationTimeSeconds);
                recorder?.EndRun();
            }
        }

        public static string FormatUptime(float seconds)
        {
            int whole = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{whole / 60:00}:{whole % 60:00}";
        }
    }
}
