using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    [Serializable] public sealed class LivingShiftActivity
    {
        public string id, premise;
        public float announceAt, startAt, spacing;
        public int jobs, variation;
        [NonSerialized] public int queued, resolved, succeeded, interrupted;
        [NonSerialized] public bool announced, reported;
    }

    [Serializable] public sealed class LivingShiftRecord
    {
        public int seed, phase, operationIndex = -1;
        public string premise, activityId, connectionId, purpose, cue, body, outcome, reason;
        public bool delivered;
        public float work, credited, eligible, lost, unfinished, target, hostileMean, durationSeconds;
        public LivingShiftActivity[] activities;
    }

    // Bounded six-minute experiment. The host still owns all gameplay and operation truth.
    public sealed class LivingShiftDirector : MonoBehaviour
    {
        public const float Duration = 360f;
        [SerializeField] private bool livingShiftEnabled = true;
        [SerializeField] private SimulationHost host;
        [SerializeField] private ConnectionTrafficDirector traffic;
        [SerializeField] private TextCommunicationDirector communications;
        [SerializeField] private ConnectionsVideoCall videoCall;
        [SerializeField] private CompanyDefinition company;
        [SerializeField] private ConnectionsCleanOverlay cleanOverlay;
        [SerializeField] private Terminal terminal;
        [SerializeField] private LivingShiftPanel panel;
        [SerializeField, Min(1)] private float workTarget = 2800f;
        private System.Random planRandom, trafficRandom;
        private ConnectionsVideoCallData shiftCall;
        private readonly Dictionary<string, LivingShiftActivity> ownership = new();
        private readonly HashSet<string> credits = new();
        private readonly Dictionary<string, float> eligibleByConnection = new();
        private readonly Dictionary<string, float> creditedByConnection = new();
        private readonly Queue<(string cue, string body, bool support)> messages = new();
        private float nextMessageAt, callEndsAt, sandboxMean;
        private bool cadenceOwned, serviceTrouble, objectiveAnnounced;
        private int phase = -1;
        public bool IsEnabled => enabled && livingShiftEnabled;
        public bool IsRunning { get; private set; }
        public bool IsFinished { get; private set; }
        public bool Succeeded => IsFinished && CreditedWork >= workTarget;
        public int Seed { get; private set; }
        public string Premise { get; private set; }
        public IReadOnlyList<LivingShiftActivity> Activities => activities;
        private LivingShiftActivity[] activities = Array.Empty<LivingShiftActivity>();
        public float CreditedWork { get; private set; }
        public float EligibleWork { get; private set; }
        public float LostWork { get; private set; }
        public float Target => workTarget;
        public float TimeRemaining => Mathf.Max(0f, Duration - host.SimulationTimeSeconds);
        public bool BehindPace => IsRunning && host.SimulationTimeSeconds >= 60f &&
            CreditedWork < workTarget * (host.SimulationTimeSeconds / Duration) * .9f;
        public string FinalSummary { get; private set; }
        public bool Owns(string id) => IsEnabled && ownership.ContainsKey(id);

        private void Unsubscribe()
        {
            if (host == null) return;
            host.OperationCompleted -= Credit;
            host.ConnectionRegistered -= Registered;
            host.ConnectionResolved -= Resolved;
        }
        private void OnDisable()
        {
            Unsubscribe();
            if (cadenceOwned && traffic != null) traffic.HostileMeanIntervalSeconds = sandboxMean;
            cadenceOwned = false;
            IsRunning = false;
        }

        public void PrepareReset()
        {
            if (cadenceOwned) traffic.HostileMeanIntervalSeconds = sandboxMean;
            cadenceOwned = false;
        }

        // Called after the host, traffic and recorder reset, in that order.
        public void BeginShift()
        {
            bool restorePresentation = IsEnabled || IsRunning || IsFinished;
            Unsubscribe();
            if (cadenceOwned) traffic.HostileMeanIntervalSeconds = sandboxMean;
            cadenceOwned = false;
            IsRunning = IsFinished = false;
            CreditedWork = EligibleWork = LostWork = 0f;
            credits.Clear(); ownership.Clear(); eligibleByConnection.Clear(); creditedByConnection.Clear();
            messages.Clear(); activities = Array.Empty<LivingShiftActivity>();
            phase = -1; nextMessageAt = callEndsAt = 0f;
            shiftCall = null;
            serviceTrouble = objectiveAnnounced = false; FinalSummary = null;
            if (restorePresentation)
            {
                videoCall?.EndCall(); communications?.Clear();
                if (Application.isPlaying) { terminal?.SuspendKeyboard(false); terminal?.SetShiftFinished(false); }
            }
            panel?.Render();
            if (!IsEnabled || host.RunMode != ConnectionRunMode.Generated) return;
            sandboxMean = traffic.HostileMeanIntervalSeconds;
            cadenceOwned = true;
            Seed = traffic.Seed;
            planRandom = new System.Random(unchecked(Seed ^ 0x51F7A11));
            trafficRandom = new System.Random(unchecked(Seed ^ 0x51B36E1));
            int main = planRandom.Next(3);
            int secondary = (main + 1 + planRandom.Next(2)) % 3;
            Premise = PremiseId(main);
            activities = new[] {
                Activity("main", main, 2f, Range(8, 16)),
                Activity("overlap", secondary, Range(65, 90), Range(95, 115)),
                Activity("followup", planRandom.Next(3), Range(145, 165), Range(175, 200)),
                Activity("late", planRandom.Next(3), Range(230, 245), Range(255, 275)) };
            IsRunning = true;
            host.OperationCompleted += Credit;
            host.ConnectionRegistered += Registered;
            host.ConnectionResolved += Resolved;
            Record("shift_started", new LivingShiftRecord { seed = Seed, premise = Premise,
                target = workTarget, durationSeconds = Duration, hostileMean = sandboxMean, activities = activities });
            Advance();
        }

        private LivingShiftActivity Activity(string id, int premise, float announce, float start) =>
            new LivingShiftActivity { id = id, premise = PremiseId(premise), announceAt = announce,
                startAt = start, jobs = planRandom.Next(4, 7), spacing = Range(7, 13),
                variation = planRandom.Next(2) };
        private float Range(float min, float max) => min + (float)planRandom.NextDouble() * (max - min);
        private static string PremiseId(int i) => new[] { "sales-promotion", "inventory-reconciliation", "reporting-deadline" }[i];
        private static string Name(string p) => p == "sales-promotion" ? "Sales promotion" :
            p == "inventory-reconciliation" ? "Inventory reconciliation" : "Reporting deadline";

        public void Advance()
        {
            if (!IsRunning || !IsEnabled) return;
            float now = host.SimulationTimeSeconds;
            if (now >= Duration - .00001f) { Finish(); return; }
            int nextPhase = now < 120f ? 0 : now < 240f ? 1 : 2;
            if (phase != nextPhase)
            {
                phase = nextPhase;
                // Retains the one opportunity clock and its current sampled wait.
                traffic.HostileMeanIntervalSeconds = sandboxMean * (phase == 0 ? 1.25f : phase == 1 ? 1f : .75f);
                Record("shift_phase", new LivingShiftRecord { phase = phase,
                    hostileMean = traffic.HostileMeanIntervalSeconds });
            }
            foreach (var a in activities)
            {
                if (!a.announced && now >= a.announceAt)
                {
                    a.announced = true;
                    Queue("activity_open:" + a.id, Opening(a), false);
                }
                while (a.queued < a.jobs && now + .00001f >= a.startAt + a.queued * a.spacing)
                {
                    string purpose = Purpose(a);
                    // Reconciliation exercises actual database work, not a display-only job.
                    string target = a.premise == "inventory-reconciliation" && purpose == "inventory-sync" ? "DB" : null;
                    var plan = traffic.GenerateActivityPlan(purpose,
                        a.startAt + a.queued * a.spacing, trafficRandom, target);
                    a.queued++;
                    if (plan == null) { a.resolved++; a.interrupted++; continue; }
                    ownership.Add(plan.ConnectionId, a);
                    traffic.QueueSpawnPlan(plan, "living_shift:" + a.id);
                    Record("shift_activity_connection", new LivingShiftRecord { activityId = a.id,
                        premise = a.premise, connectionId = plan.ConnectionId, purpose = purpose });
                }
                if (!a.reported && a.queued == a.jobs && a.resolved == a.jobs)
                {
                    a.reported = true;
                    Queue("activity_result:" + a.id, $"{Name(a.premise)} batch: {a.succeeded}/{a.jobs} connections completed; {a.interrupted} interrupted or unavailable.", false);
                }
            }
            if (!serviceTrouble && host.Health != null && host.Health.Value < 25f && host.Health.DegradationRate > 0f)
            {
                serviceTrouble = true;
                Queue("service_deterioration", "Service is deteriorating. We're seeing delays on active work. Please check the network.", true);
            }
            else if (serviceTrouble && host.Health.Value >= 40f && host.Health.DegradationRate <= 0f)
            {
                serviceTrouble = false;
                Queue("service_recovery", "Service is recovering and the active slowdown has cleared. Thank you for staying on it.", true);
            }
            if (!objectiveAnnounced && CreditedWork >= workTarget)
            {
                objectiveAnnounced = true;
                Queue("objective_met", "Confirmed WORK has reached the shift target. Keep services running until handoff.", false);
            }
            if (callEndsAt > 0f && now >= callEndsAt)
            {
                if (videoCall != null && ReferenceEquals(videoCall.CurrentCall, shiftCall)) videoCall.EndCall();
                shiftCall = null; callEndsAt = 0f;
            }
            if (messages.Count > 0 && now >= nextMessageAt)
            {
                var cue = messages.Dequeue();
                bool stale = cue.cue == "service_recovery" &&
                    (host.Health.Value < 40f || host.Health.DegradationRate > 0f) ||
                    cue.cue == "service_deterioration" && host.Health.DegradationRate <= 0f;
                if (stale) Record("shift_communication", new LivingShiftRecord { cue = cue.cue,
                    body = cue.body, delivered = false, reason = "condition_changed_before_delivery" });
                else Deliver(cue.cue, cue.body, cue.support);
                nextMessageAt = now + 14f;
            }
            panel?.Render();
        }

        private string Purpose(LivingShiftActivity a)
        {
            string[] purposes = a.premise == "sales-promotion" ?
                new[] { "order-submit", "cart-pricing", "customer-login", "product-lookup" } :
                a.premise == "inventory-reconciliation" ?
                new[] { "inventory-sync", "inventory-sync", "product-lookup" } :
                new[] { "sales-export", "sales-export", "customer-login" };
            // Guarantee actual premise work, then vary the supporting mix.
            return a.queued == 0 ? purposes[0] : purposes[trafficRandom.Next(purposes.Length)];
        }
        private static string Opening(LivingShiftActivity a)
        {
            if (a.premise == "sales-promotion") return a.variation == 0 ?
                "The promotion is going live. Expect checkouts, pricing requests and customer logins shortly." :
                "Marketing has opened the sale. Customer traffic is coming through; please keep checkout moving.";
            if (a.premise == "inventory-reconciliation") return a.variation == 0 ?
                "Operations is starting inventory reconciliation. Database sync work will arrive shortly, alongside regular requests." :
                "We're reconciling warehouse counts. Expect an inventory batch against the database; other teams are still working.";
            return a.variation == 0 ?
                "Finance needs sales exports for the reporting deadline. Reporting work and supporting requests will start shortly." :
                "The reporting handoff is due this shift. We're sending sales exports through RPT while normal service continues.";
        }
        private void Registered(LogicalConnectionState state)
        {
            if (!IsRunning || state.IsHostile) return;
            float work = 0f;
            for (int i = 0; i < state.Definition.OperationCount; i++) work += state.Definition.WorkAt(i);
            eligibleByConnection[state.ConnectionId] = work;
            EligibleWork += work;
            Record("shift_eligible", new LivingShiftRecord { connectionId = state.ConnectionId, work = work });
        }
        private void Credit(LogicalConnectionState state, int index, float work)
        {
            if (!IsRunning || state.IsHostile || host.SimulationTimeSeconds > Duration + .00001f ||
                !credits.Add(state.ConnectionId + "/" + index)) return;
            CreditedWork += work;
            creditedByConnection.TryGetValue(state.ConnectionId, out float prior);
            creditedByConnection[state.ConnectionId] = prior + work;
            Record("shift_work_credit", new LivingShiftRecord { connectionId = state.ConnectionId,
                operationIndex = index, work = work, credited = CreditedWork });
        }
        private void Resolved(LogicalConnectionState state, ConnectionResolution result)
        {
            if (!IsRunning || state.IsHostile) return;
            if (result != ConnectionResolution.Completed)
            {
                eligibleByConnection.TryGetValue(state.ConnectionId, out float eligible);
                creditedByConnection.TryGetValue(state.ConnectionId, out float credited);
                float lost = Mathf.Max(0f, eligible - credited);
                LostWork += lost;
                Record("shift_work_lost", new LivingShiftRecord { connectionId = state.ConnectionId,
                    work = lost, reason = result + "/" + state.TerminationCause });
            }
            if (!ownership.TryGetValue(state.ConnectionId, out var a)) return;
            a.resolved++;
            if (result == ConnectionResolution.Completed) a.succeeded++;
            else
            {
                a.interrupted++;
                if (a.interrupted == 1)
                    Queue("activity_interruption:" + a.id, $"{Name(a.premise)} connection {state.ConnectionId} was interrupted ({result}). Earlier completed WORK remains confirmed.", false);
            }
        }
        private void Queue(string cue, string body, bool support)
        {
            messages.Enqueue((cue, body, support));
            Record("shift_cue_queued", new LivingShiftRecord { cue = cue, body = body });
        }
        private string Speaker(bool support)
        {
            var speaker = support ? company?.CustomerSupport : company?.Operations;
            return speaker == null ? "Operations" : speaker.displayName + " / " + speaker.role;
        }
        private void Deliver(string cue, string body, bool support)
        {
            bool delivered = communications != null && communications.PushMessage(new TextCommunication {
                Sender = Speaker(support), Body = body });
            Record("shift_communication", new LivingShiftRecord { cue = cue, body = body, delivered = delivered, reason = delivered ? null : "presentation_unavailable" });
            // One optional opening call; the same IM always carries critical context.
            if (cue == "activity_open:main" && videoCall != null && Application.isPlaying)
            {
                shiftCall = new ConnectionsVideoCallData { CallerName = Speaker(false), Body = body };
                bool opened = videoCall.TryStartCall(shiftCall);
                if (opened) callEndsAt = host.SimulationTimeSeconds + 16f;
                Record("shift_call", new LivingShiftRecord { cue = cue, body = body, delivered = opened });
            }
        }
        private void Finish()
        {
            IsRunning = false; IsFinished = true;
            while (messages.Count > 0)
            {
                var pending = messages.Dequeue();
                Record("shift_communication", new LivingShiftRecord { cue = pending.cue,
                    body = pending.body, delivered = false, reason = "shift_finished" });
            }
            FinalSummary = $"{Mathf.FloorToInt(CreditedWork)} / {workTarget:0} confirmed WORK. " +
                (Succeeded ? "Required operational WORK completed before handoff." : "Deadline reached before required WORK was completed.") +
                $"\nInterrupted opportunities: {LostWork:0} WORK. Final Health: {host.Health.Value:0}.";
            var duel = host.GetComponent<ConnectionsDuelDirector>();
            if (duel?.Current != null)
                host.Recorder?.RecordDuel("duel_aborted", duel.Current, host.SimulationTimeSeconds,
                    reason: "shift_finished");
            duel?.ResetDuel();
            if (host.ActiveScan != null)
                Record("shift_scan_canceled", new LivingShiftRecord { reason = "shift_finished",
                    cue = "scan:" + host.ActiveScan.Id });
            host.EndShiftSessions();
            cleanOverlay?.AbortForShiftEnd(); videoCall?.EndCall();
            if (Application.isPlaying) terminal?.SetShiftFinished(true);
            communications?.Clear();
            Deliver("shift_result", Succeeded ? "Shift complete. The required WORK is confirmed. Thanks for covering the handoff." :
                "Shift failed. We reached handoff short of the required confirmed WORK.", false);
            Record("shift_finished", new LivingShiftRecord { outcome = Succeeded ? "complete" : "failed",
                reason = Succeeded ? "work_target_met" : "deadline_work_shortfall", credited = CreditedWork,
                eligible = EligibleWork, lost = LostWork, unfinished = Mathf.Max(0f, EligibleWork - CreditedWork - LostWork), target = workTarget });
            host.Recorder?.CaptureSnapshot(host.SimulationTimeSeconds);
            host.Recorder?.EndRun();
            panel?.Render();
        }
        public void Restart()
        {
            traffic.Seed = traffic.Seed == int.MaxValue ? 0 : traffic.Seed + 1;
            host.ResetSimulation();
        }
        private void Record(string type, LivingShiftRecord data)
        {
            data.seed = Seed;
            host.Recorder?.RecordShift(type, host.SimulationTimeSeconds, data);
        }
    }
}
