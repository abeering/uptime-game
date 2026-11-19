# Connections — Living Shift V0

Connections implementation experiment on Advanced, based on `main` at `d43dab4` (Surprise V0 and PR #113 recorder fix). Packet and the four-node Connections scene are unchanged. This is a bounded experiment in context, cumulative legitimate WORK, changing pressure and an ending, not a Level Director or campaign contract.

## Play and configuration

Open `Assets/Scenes/ConnectionsAdvanced.unity` and press Play. The existing randomized traffic seed also identifies the shift, but planning and activity recipes use two dedicated seed-derived RNGs. The same seed, configuration, simulation step schedule and player actions reproduce decisions. The existing ambient, identity, threat, Worm, Payload and Duel RNGs remain separate. Extra ordinary connections necessarily consume globally unique IDs and influence shared resources.

The serialized `LivingShiftDirector.livingShiftEnabled` switch disables this experiment on reset/next Play and restores Advanced's endless sandbox. Disabling the component also removes its subscriptions and restores its owned hostile mean. Use a reset after changing modes, rather than switching a finished run back into live play. No experiment component/reference is added to `Connections.unity`.

The shift lasts **360 simulation seconds**, with a fixed, Inspector-authored **2,800 confirmed WORK** target. All completed benign operations count, including ambient and developer-spawned benign work. Health is diagnostic and never an independent loss condition. Reaching the target early does not end the shift or add requirements. The compact objective lives in unused network titlebar space, with confirmed WORK, a bar, remaining simulation time and `BEHIND PACE`. That warning compares confirmed WORK to 90% of linear pace after the first minute; it is not a failure forecast. Integer confirmed totals display rounded down, so 2,799.9 cannot appear to have met 2,800.

The fictional workstation clock maps the authored 09:00–17:00 interval to the six-minute shift. Sandbox mapping is unchanged. Result presentation gives keyboard selection to `RESTART / NEXT SEED`; click or use the UI submit key. Restart increments the seed and resets the existing host, rather than creating another simulation.

## Company activity and pressure

A small catalog selects one main premise, a distinct secondary, and two later activities. Sales promotion mixes checkout, pricing, login and product requests. Inventory reconciliation starts genuine inventory-sync work against DB and mixes supporting lookups. Reporting starts sales-export work on RPT with supporting logins. Each batch guarantees a premise-specific first connection, then samples its supporting mix. Existing ambient benign traffic remains enabled.

Four selected batches have 4–6 jobs each and 7–13-second job spacing. Announcements occur near 2, 65–90, 145–165 and 230–245 seconds; corresponding work starts near 8–16, 95–115, 175–200 and 255–275 seconds. These windows and counts are provisional authoring bounds. Batches can overlap ordinary operations and each other; they are not invisible jobs or special allocation rules. Plans pass through the ordinary purpose recipe, concrete plan queue, host registration, target-node allocator and operation lifecycle. Only reconciliation's explicit DB target differs from that purpose's ordinary ambient JOBS target.

The existing single hostile opportunity clock retains its sampled wait and selection mechanism. Future waits use the sandbox mean × 1.25 in the first two minutes, × 1 in the middle, and × 0.75 in the final two minutes (Advanced's 30-second mean gives 37.5/30/22.5 seconds). No hostile outcome is forced by a business premise. Threats, infections and Health are never cleared to manufacture an arc.

## Accounting and narrative truth

`SimulationHost.OperationCompleted` publishes the authoritative connection, completed operation index and actual completed WORK captured by the existing allocator. The director credits only benign completions, once per `(connection ID, operation index)`. A final resolution callback adds no credit. Partial interrupted operations count zero; successful earlier stages retain credit. No accounting reads the recorder or UI.

Eligible opportunity WORK is the sum of authored operation requirements when each benign connection registers. Lost WORK is the uncredited remainder of that opportunity on a non-completed resolution. Both are diagnostic measures of offered work, not extra scoring rules; changing effective requirements can make them differ from eventual actual completed WORK. The final receipt also records unfinished opportunity WORK separately. Reset clears totals, credit identities, ownership, queues, timers and RNG state.

Opening content and traffic derive from the same selected activity. Existing Northstar Operations and Support speakers provide short authored variations. Main context is sent as IM and, once, as an optional call through the normal presentation APIs. Failed delivery is recorded; calls are not retried. An opened shift call ends after 16 simulation seconds, only if it still owns that call. It never takes terminal focus.

An owned connection interruption reports its actual ID and resolution; only the first per batch complains. A batch result reports actual completed/interrupted/unavailable counts after every job resolves. The existing generic `CompanyKillFeedback` suppresses owned connections, avoiding duplicate kill reactions. Other benign kills retain their existing generic feedback. Service deterioration requires Health below 25 with real degradation. Recovery requires Health at least 40 with degradation cleared. Generic service messages are checked again at delivery and dropped if their premise has become stale. They do not name hidden malicious causes. Messages have a 14-simulation-second minimum interval, while the existing passive surface retains its real-time reading duration.

## Terminal state and evidence

The host clamps advancement to 360 seconds and refuses subsequent simulation steps; traffic dispatch also rejects a finished shift. The final step's legitimate completions count before evaluation. The ending cancels pending/active Duels, cancels CLEAN without penalty and unfinished scan sessions, closes CLEAN and calls, cancels delayed terminal samples, disables command input, clears pending shift messages and gives the result restart control focus. Remaining entities stay frozen as evidence; no gameplay progresses behind the result. The recorder takes a final snapshot and ends the run. Restart uses the normal host lifecycle and restores terminal entry.

Recorder events include `shift_started` (seed, premise, all batch timings/counts/spacing/variation), `shift_phase`, `shift_activity_connection` (ownership and concrete plan ID), `shift_eligible`, `shift_work_credit`, `shift_work_lost`, `shift_cue_queued`, `shift_communication`, `shift_call`, `shift_scan_canceled`, and `shift_finished`. Delivery receipts distinguish success, unavailable presentation, stale conditions and pending cues discarded at the ending. Existing `plan_queued`, hostile opportunity/activity, operation, effect and consequence records retain concrete traffic and independently selected incidents. Shift payloads are additive schema-2 event data.

Use `Tools/Connections/Run Living Shift Witness`, `Measure Living Shift Baselines`, and `Capture Living Shift Play Mode Review`. The [review evidence](../Review/ConnectionsLivingShiftV0/README.md) records actual checks, measurements, captures and remaining physical-keyboard review. The small experiment does not establish that benign WORK management is sufficiently interesting or that the target is balanced for human play.
