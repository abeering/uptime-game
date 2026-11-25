# Connection Fragility V0 — review and evidence

Connections implementation, based on main `802a98e25209ef4c81e6ac90ef329a5382f1f6bc`, November 25, 2026. Both canonical scenes are supported. [Exact mechanical contract](../../Connections/connections-fragility-v0.md); [next Connection Visual Language phase](../../Design/Uptime_Design.md#connections-reliability-milestone--next-visual-phase-2026-11-25).

## Controlled benign result

The same legitimate Product Lookup PURPOSE/queue/lifecycle/allocator fixture runs in Small and Advanced with seed 1701 and no hostile or automatic arrivals. DB remains capacity 10. READ is fixed to 100 WORK / 5 LOAD, with spread/outliers/natural noise disabled for exact repeatability. Baseline 4 reserves existing node LOAD without producing a severe-overload Integrity crisis. This is deliberately forced mechanical evidence, not a calibrated ambient scenario.

| Case (both scenes) | Expected | Observed |
|---|---|---|
| One request, baseline 4 | At speed;20s service; succeeds inside 30s budget | Completed;0 failures |
| Two requests, baseline 2 |4 LOAD each; slowed but safe;25s service | Completed;0 failures |
| Two requests, baseline 4, no action |3 LOAD each; need 33⅓s; timeout at 30s | Both TimedOut; approximately 90/100 WORK each;2 failures |
| Same pair, Overclock after roughly 10–12 service seconds |5 LOAD each during rescue; danger clears | Both complete;0 failures |
| Same pair, kill one after roughly 10–12 service seconds | One sacrificed business connection; survivor gets5 LOAD | Survivor completes;1 failure |

All ten cases retain full 2/2 internal Integrity and avoid CORE loss. Small establishes at approximately 1.2 seconds, Advanced at 1.8 seconds because of ingress length; each gets the same 30-second service budget. The first danger projection is available on establishment, giving roughly 30 simulation seconds of notice; the demonstrated interventions happen well before expiry. [Observed rows](benign-trials.csv) retain actual floating-point timestamps and progress. Small observed 89.99801 WORK and Advanced 89.99797 at timeout; ordinary fixed-step float accumulation explains the difference from analytical 90.

The late-recovery witness separately shows an at-speed connection still at risk after earlier delay. The zero-rate multi-operation witness preserves its completed first operation, times out the unfinished second operation, removes modifiers, and prevents its pending infection-delivery effect. Completing the final operation exactly on the deadline succeeds. Coarse 100-second calls stop unfinished service on its actual deadline, rather than after 100 seconds.

[Small compressed recorder evidence](Connections-benign-timeouts.json.gz) and [Advanced evidence](ConnectionsAdvanced-benign-timeouts.json.gz) contain full recipes, queue/arrival/establishment events, original budgets/deadlines, progress, causes, business-failure receipts and cumulative totals. Read with `gzip -dc <file>`. They contain controlled runs rather than natural demand samples.

## Automated validation

Actual Unity 6000.3.11f1 batch Editor compilation and simulation witnesses ran. Results are retained in:

- [Deterministic checks](deterministic-checks.txt): 126 focused assertions, including nominal sums, independent Health tuning, establishment/Pending/Connecting exclusion, exact-boundary success, coarse timeout, transitions, cleanup/effects, release/reallocation, safe slowdown, at-speed danger, Overclock, sacrifice, historical/collapsed inspect, recorder on/off parity, reset/replay and CORE freezing unresolved work.
- [Canonical regression report](canonical-regression-checks.txt) and [additional regression report](regression-checks.txt): actual pass/fail details. The two unrelated UI assertions are retained as failures: Advanced Overclock compact tool row, and Resource inspect-header pill geometry/color. No claim that those witnesses pass.
- [Pristine-main baseline](main-baseline-regressions.txt) reproduces both UI failures on unchanged main. [Additional pristine-main checks](main-baseline-extra.txt) also demonstrate old Lifecycle/Behavior/Natural LOAD Noise coarse-fixture assumptions and BACKDOOR Small-scene tuning assertions were already stale. This branch explicitly uses the existing legacy fixture configuration for those old mechanical tests and updates BACKDOOR's canonical Small expectation. These fixture changes do not disable canonical fragility checks or alter scene behavior.

The existing Inspect V2/Resolved Inspect/Geometry/Health binding assertions were updated only for the added authored labels and 66-unit verbose service block. The existing recorder witness expects schema 3. Resource/Overclock presentation was not repaired to force a green suite.

Reproduce with `-batchmode -nographics -projectPath <repo> -executeMethod Uptime.Connections.EditorTools.FragilityWitness.Validate -quit -logFile <path>`. The aggregate exits nonzero while the inherited UI assertions fail; inspect individual reports. `Tools → Connections → Run Fragility Witness` runs the focused checks alone. Older regression runners rewrite their historical evidence paths; the committed new reports are copied here, and unrelated generated artifacts are excluded from this PR.

## Scripted graphical Play Mode

[54 graphical assertions](graphical-checks.txt) and 32 captured frames cover Small/Advanced at 1920×1080 and 1280×720. The runner enters actual Play Mode, then disables automatic host stepping and advances the authoritative host explicitly so each frame is reproducible. It submits commands through the terminal's input submission event and checks prompt focus/draft preservation. It uses a temporary capture camera and render texture; this is scripted graphical Play Mode, not human keyboard/IME or a native Game-view session.

| Situation | Small720p | Advanced720p |
|---|---|---|
| Healthy | [Frame](Captures/Connections/01-healthy-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/01-healthy-1280x720.png) |
| Slowed but safe | [Frame](Captures/Connections/02-slowed-safe-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/02-slowed-safe-1280x720.png) |
| At risk | [Frame](Captures/Connections/03-at-risk-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/03-at-risk-1280x720.png) |
| Overclock rescue | [Frame](Captures/Connections/04-rescue-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/04-rescue-1280x720.png) |
| Timeout departure | [Frame](Captures/Connections/05-timeout-departure-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/05-timeout-departure-1280x720.png) |
| Historical timeout | [Frame](Captures/Connections/06-timeout-history-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/06-timeout-history-1280x720.png) |
| Collapsed summary/prompt | [Frame](Captures/Connections/07-resolved-summary-and-prompt-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/07-resolved-summary-and-prompt-1280x720.png) |
| Sacrifice | [Frame](Captures/Connections/08-sacrifice-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/08-sacrifice-1280x720.png) |

The graphical run emitted two nonfatal “No full-size identity pill fits” warnings on E1/E2 during initial layout; settled review frames show the packed pills. Crowding remains a human-review concern.

Every linked frame has a 1920×1080 companion in the same directory. Run `Uptime.Connections.EditorTools.FragilityCapture.Run` with graphical Unity batch mode (omit `-nographics` and `-quit`; the runner exits itself) to regenerate.

## Human Play Mode review still required

Repeat these steps in **Assets/Scenes/Connections.unity** and **Assets/Scenes/ConnectionsAdvanced.unity**, first1920×1080 then1280×720. Enter ordinary Play Mode; the fixture menus do not disable the host, so simulation continues while typing. Do not save Play Mode fixture changes to the scene. Stop Play Mode/reload to discard fixture tuning; `restart` alone resets state while retaining in-memory fixture settings.

1. Choose **Tools → Connections → Load Healthy Fragility Fixture (Play Mode only)**. Inspect the shown ID with `inspect -v <id>`. Confirm current-operation ETA and connection SERVICE budget are distinct; it stays safe and completes with BUSINESS FAILURES 0.
2. Choose **Load Slowed Safe Fragility Fixture**. Inspect both IDs. Orange slowdown alone must not show AT RISK. Both should complete without a business failure.
3. Choose **Load Benign Fragility Fixture**. Both IDs should show small AT RISK text inside the original pill bounds; verbose inspect explains insufficient projected time. With no action, watch both time out near30 service seconds, flash twice in the existing red departure, and leave BUSINESS FAILURES 2.
4. `inspect -v <timed-out-id>` must explicitly say TIMED OUT. Wait over 4 real seconds for the collapsed summary; it must retain TIMED OUT. Test multiple historical/active cards, scrolling, prompt focus, draft/caret preservation and command recall.
5. Reload the benign fixture. Within approximately 10 service seconds, type `overclock db`. Confirm danger clears, grants/rates rise, fixed service deadline stays put and both complete with zero failures. The fixture resets the ability cooldown each time.
6. Reload it again and `kill <one-id>` within approximately 10 service seconds. Confirm one recorded business failure, peer recovery and successful peer completion. The sacrificed connection remains historically inspectable.
7. Inspect simultaneous IDs and multi-operation natural traffic. Check label cycling/crowding, stable colors, separate physical/logical paths, terminal history and input focus. Confirm the smaller temporary danger text is readable at native 720p; sustained maximum-density human review remains open.
8. Stop/re-enter Play Mode without a fixture. Confirm normal canonical generation, existing threats/defenses, Integrity/CORE ending and restart behavior. The fixture does not establish ambient balance or a failure allowance/firing rule.

Human physical-keyboard/IME, sustained high-density readability and natural ambient balance have not been validated by this work. The target/enforcement asymmetry for hostiles is documented in the mechanical contract: uniform service information does not expose classification, but a hostile may persist beyond the service target to preserve existing attacks. That residual clue and 1.5× tolerance are playtest/design concerns. Packet and historical experimental scenes were not changed or human-playtested.
