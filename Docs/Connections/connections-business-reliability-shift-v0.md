# Connections — Business Reliability & Shift Objectives V0

Implementation branch `feature/connections-business-reliability-shift-v0`, based on merged main `4c099d76328c12bc64240f0607b0066b3be3e90b`. Scope: canonical Small and Advanced Connections. Packet and historical scenes remain separate. This implements one shift, without multi-shift progression or new demand scheduling.

## The playable rule

**Complete the required successful business WORK before the company’s legitimate connection failure limit is reached.** There is no shift deadline, required WORK-per-second pace, elapsed-time penalty or insufficient-WORK ending. A slower successful shift exposes the player to the existing simulation for longer. Individual connection service deadlines from Fragility still apply independently.

Both canonical scenes enable `shiftEnabled`. Provisional independent values are **Small: 1,000 WORK / 10 failures; Advanced: 1,500 WORK / 10 failures**. The tenth failure causes dismissal; it does not mean ten failures plus one are tolerated. Completed business does not replenish the allowance. These values are initial review tuning, not a calibrated fairness/fun claim. [Concrete-plan sampling](../Review/ConnectionsBusinessReliabilityShiftV0/plan-value-sample.csv) uses the current scene recipes and seeded generator, rather than transferring #124’s fixed-duration calibration. Natural demand, congestion, hostile frequency and timeout tolerance are unchanged. Business Demand & Congestion is the next phase.

The host’s default objective switch is off for historical fixtures/scenes. That opts out of WORK/reliability endings and the canonical objective HUD; successful business and failure diagnostics still remain authoritative. CORE can end either mode.

## Business success is a whole connection

`LogicalConnectionState.BusinessWorkValue` is frozen from the concrete job’s operation WORK after sampling/anomalies. The [Legitimate Workload Increase V0](connections-legitimate-workload-v0.md) follow-up captures that sum in the generated plan before proportional processing LOAD/WORK scaling and carries it into state; authored definitions retain the sum-of-authored-WORK fallback. It is not recomputed as processing evolves.

- Recipe sampling (`normalWorkSpread`) and generation-time `work-deviation` affect those concrete operation requirements and therefore the job value. A generation-time `operation-deviation` that adds a stage contributes that stage’s concrete WORK.
- LOAD-only changes, waving/keyword demand, allocation, natural LOAD noise, productivity multipliers and runtime required-WORK modifiers do not change the frozen business value. A 40-WORK job whose runtime processing requirement rises to 50 or falls to 20 still delivers 40 business WORK if it succeeds.
- This is a provisional value definition, not an economy. Future CACHE can reduce processing after the business value is established; any plan-time CACHE rewrite will need to preserve that value explicitly.

`SimulationHost.Resolve` first claims the canonical state’s resolution. Only a legitimate `Completed` resolution grants the entire fixed job value, exactly once, keyed by canonical connection ID with the host’s case-insensitive identity semantics. Aliases never become keys. A READ40 / EXECUTE60 / WRITE100 job grants **200 only after its final successful resolution**. Its first two operation completions grant zero shift WORK. If the final operation times out, the job grants zero and the existing failure ledger records one failure.

Hostile completion, timeout, kill, cancellation, isolation and withdrawal do not grant business WORK. No UI, recorder callback or operation-completion observer grants credit. Accounting works with recording disabled. Existing operation progress, processing logs/effects and connection WORK fill retain their meanings and owners.

The existing immutable `BusinessFailure` receipts remain the only reliability ledger. Every legitimate non-Completed resolution contributes one unit through the same seam. There is no second failure counter, priority weighting or forgiveness on success.

## One terminal outcome owner

`SimulationHost.Shift.cs` owns `RunEnding`: `None`, `ShiftComplete`, `Fired`, `CoreBreached`. The host settles each authoritative boundary, then evaluates **CORE breach → failure limit reached → WORK target reached**. All genuinely earned successful job credit, failure receipts and relevant effects on that boundary remain included. A batch can end with a count above its failure limit or WORK above its target; counters are not clipped to disguise same-boundary results.

The existing `CompleteIntegrityBoundary` API delegates to the common terminal owner. During simulation segments or nested gameplay transactions, observer callbacks cannot finish a partially settled boundary. Kill, Blackout, isolation and Duel consequence transactions defer evaluation until their effects, released resources and receipts have settled. The witness exercises completion plus a hostile Blackout cancellation on the same allocator boundary in both ID orders, and a CORE strike on that boundary.

Quota-enabled coarse calls subdivide into the ordinary fixed simulation steps and stop at the first reached terminal boundary. They do not continue through the unused outer interval. The allocator, its existing numeric tolerance, operation transition ownership and connection timeout ordering are unchanged. Declared final completion at a connection’s exact service deadline precedes timeout.

`FinishRun` claims the outcome once before callbacks. It freezes subsequent simulation/traffic/attack progression and rejects gameplay mutations, while preserving unresolved jobs and model sessions for diagnostics. Frozen unfinished jobs do not become failures. CLEAN/Duel presentation aborts without PID penalties, invented concession or attack consequences; scan/model state remains frozen. Recording finalizes exactly once. Existing inspect/history, diagnostics and same-scene/seed `restart` remain usable. The shared #131 presentation clock freezes through the existing `RunEnded` seam for every outcome.

`ShiftComplete` is a successful **shift**, while `Fired` and `CoreBreached` are run losses (`ShiftSucceeded` / `RunFailed` and recorded flags distinguish them). The legacy `RunEnded` name stops all three in this single-shift prototype. No next-shift UI or progression is implemented.

## Reset policy and future shifts

Restart performs the existing full simulation reset and clears credited WORK, canonical credit keys, business failures, outcome and receipt state. It retains the scene and chosen seed. The intended future between-shift policy is a **full WORK objective and failure-allowance reset**, not a partially depleted allowance carried into the next shift.

`ResetShiftAccounting` groups those accounting resets separately from topology, traffic, Integrity, infection and session reset code. A future shift coordinator can reuse it without resetting an entire run, but must also define new-shift job/history scoping and transition policy. It is private and currently called only by full restart. No public next-shift operation or persistent run state is introduced.

## Authored compact presentation

The existing scene strip now shows unbounded elapsed `UPTIME`, successful `WORK credited / target`, a compact WORK bar, and `FAILURES count / limit`. Existing tool-readiness elements and network/terminal geometry stay in place. Failure text highlights the final remaining allowance. A two-real-second `+N WORK` receipt reports successful job credit; multiple credits observed together aggregate their delta. Intermediate processing does not show a business receipt.

The existing authored CORE panel also presents `SHIFT COMPLETE` (green), `FIRED` and `CORE BREACHED` (red), final elapsed/WORK/failure values, and the CORE path where applicable. Result binding/cleanup is adapted in the existing `IntegrityPresenter`; ordinary UI is not built at runtime.

Deferred visual ideas remain untouched: a moving deadline checkpoint on the inspect WORK bar, replacing/reducing the two SERVICE lines, narrowing ordinary identities to tinted slate greys, and further connection polish. Accepted ribbon/socket/heartbeat assets and inspect presentation are unchanged.

## Recorder schema 4

New metadata records objective enablement, WORK target, failure limit and `businessWorkPolicy = completed_legitimate_connection_authored_work`. There is no shift-duration configuration.

- `connection_resolved` retains existing resolution/operation evidence. A successful legitimate resolution is followed by one **`business_work_credit`** event: canonical ID, `businessWork` amount, cumulative `creditedWork`, time and `operationIndex = -1` to denote a whole job.
- Existing `business_failure` events and cumulative counts are unchanged and are not duplicated.
- Snapshots include the objective state and each materialized job’s fixed business value. `eligible = credited + lost + unfinished` covers registered legitimate jobs only; lost and unfinished are whole uncredited business values, not partial processing credit or ungenerated future demand.
- One `shift_ended` includes the final outcome, objective values, elapsed time, successful job count and the shift-success/run-failure distinction. Metadata retains `finalShift`; the final diagnostic snapshot is saved and recording ends once.

Existing operation-completion events continue to describe processing WORK and are never interpreted as business credits. Prior schema 1–3 evidence remains historical: added fields are optional/defaulted when reading older JSON. #124’s `shift_work_credit` is a different operation-credit event from an unmerged experiment; its quota/deadline results are not current rules. No per-frame event type is added.

## Donor provenance and validation

Primary donor: unmerged [#124](https://github.com/abeering/uptime-game/pull/124), head `7cec0ae7e2c0b363fa07613c99cc4b3e2898dcf3`. Selectively adapted its host-owned outcome/recorder-finalization structure, mutation gates, strip/result binding, held-key cleanup, authored UI workflow and graphical witness approach. Its fixed 360-second deadline, insufficient-WORK loss, operation-level credits and time-based calibration were deliberately superseded. No merge or wholesale cherry-pick. #115/#124/#125/#126 remain unchanged and unmerged; their full historical evidence stays on their existing branches/PRs.

[Review evidence, actual checks, recorder examples and manual fixtures](../Review/ConnectionsBusinessReliabilityShiftV0/README.md) distinguish automated, scripted Play Mode and remaining human review. Historical mechanics fixtures explicitly opt out of objective endings. The retained Health witness uses its original prefab geometry, rather than asserting a Health bar against the new canonical WORK bar. Known unrelated inherited assertions are not changed to force green.
