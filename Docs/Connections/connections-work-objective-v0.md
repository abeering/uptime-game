# Connections — WORK Objective & Shift V0

Implementation contract, November 20, 2026. Based on `main` at `bfbb19d5cd3b5f6682845ff838f4f3bca7b19a6a`; tracks [#119](https://github.com/abeering/uptime-game/issues/119). Applies to Small `Connections.unity` and `ConnectionsAdvanced.unity`. Living Shift PR #115 remains separate and unmerged; only its authoritative operation-completion/exact-once accounting approach informs this implementation.

Both canonical scenes have an Inspector-configurable bounded shift, initially **360 simulation seconds**, and a provisional WORK quota of **900 in Small / 1,200 in Advanced**. Preserve CORE and complete sufficient legitimate WORK. Meeting the quota ends successfully immediately. CORE breach or reaching the deadline short of quota ends unsuccessfully. There is no next shift, progression, company traffic batch, narrative scheduling, additional hostile clock, or pressure retuning.

## Authoritative accounting and ending order

`SimulationHost.Shift.cs` owns the ledger and single run-ending state. Only the allocator's successful benign operation-completion path grants credit. Its key is canonical connection ID plus operation index, independent of ID presentation aliases. Hostile operations grant zero. Incomplete operations grant zero, and killing or canceling an arc preserves its earlier completed credits. Resets clear every key and total. Recorder events, connection resolution, UI refreshes, and `OperationCompleted` observers do not grant credit independently.

Confirmed WORK is the operation's actual completed amount, preserving accomplished WORK when a requirement decreases. The existing allocator's completion tolerance and `FinishOperation` normalization are retained: a successfully completed operation is credited at least its final effective requirement. This avoids an operation being marked complete while a tiny floating-point tolerance shortfall prevents its corresponding quota credit. The ledger accumulates these values as doubles; quota comparison uses the precise configured value, never displayed integers. Allocation, WORK rate, and operation transitions remain unchanged.

Each authoritative segment settles connection completions and their effects, arrivals, moving attacks, scan completion, infection spawners, timed effects and Duel boundaries before evaluating the ending. **CORE breach takes precedence, then quota success, then deadline shortfall.** Completed operations on the final deadline boundary earn credit before that evaluation. Coarse shift steps subdivide using Unity's fixed step and clip to the configured deadline; split final segments retain their intended clock boundary. Reaching quota can finish at an earlier segment boundary rather than waiting for the outer witness call or deadline.

The existing `RunEnded` guards now read the shared terminal state. Gameplay time, traffic generation, WORK, attacks and mutating commands stop. Integrity's existing CORE result uses the same owner. CLEAN, scan and Duel model state remains frozen for diagnostics; active presentation aborts without PID selection, cancellation penalties, or an invented concession/loss. A pending/active Duel receives a `duel_aborted` recording. The result restores guarded terminal input for `restart` and existing read-only diagnostics. Restart uses the current scene and seed.

## Presentation and evidence

The existing serialized compact clock/Health strip is adapted in both canonical scenes:

`UPTIME 02:32 / 06:00    WORK 720 / <quota>    [progress]    +48 WORK`

UPTIME starts at `00:00`. Its limit is always visible. The bar is confirmed WORK / quota, capped at full. A brief two-real-second receipt shows the latest operation credit; simultaneous completions still all count in the authoritative total. Credited text floors to whole WORK to avoid appearing to reach quota early. The target can show fractional configuration values. The existing tool-readiness indicators retain their bindings and behavior. No Health value, rate, or Health-specific warning colors are reintroduced. The retained Health prefab/model still supports historical mechanics witnesses and diagnostics.

The existing authored CORE panel is reused for `SHIFT COMPLETE`, `SHIFT FAILED — INSUFFICIENT WORK`, and `CORE BREACHED`. Every result includes final UPTIME, credited/required WORK, and cause; CORE additionally shows its winning physical path. Success is green, failure red. UI construction does not occur at runtime.

Recorder schema 2 gains initial shift configuration, `shift_work_credit` events with canonical ID/index/amount/cumulative credit, `shift_ended` with cause and totals, final metadata, and per-snapshot objective state. The model owns accounting even with recording disabled. A terminal result captures final diagnostics and finalizes/saves the recording once. Wall-clock metadata remains nondeterministic; gameplay event/snapshot replay can be compared exactly.

Diagnostic eligible WORK is credited + lost + unfinished. Uncredited operations count their whole requirement, including operations with partial progress. Resolved uncredited operations are lost; unresolved ones are unfinished. These diagnostics cover materialized benign connections, not ungenerated future traffic. Current effective requirements are used for the current operation; authored requirements are used for future operations. They are opportunity estimates, not additional win credit.

Calibration, actual validation outcomes, graphical captures and the manual keyboard guide are in [the review evidence](../Review/ConnectionsWorkObjectiveV0/README.md). Quotas and balance remain provisional. Historical Pressure/Integrity fixtures, their original evidence, and Packet scenes remain unchanged. Older witnesses explicitly opt out of shifts where they exercise pre-shift mechanics.
