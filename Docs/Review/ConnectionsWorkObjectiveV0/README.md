# WORK Objective & Shift V0 review

Connections implementation on `feat/connections-work-objective-v0`, based on `main` at `bfbb19d5cd3b5f6682845ff838f4f3bca7b19a6a`. Tracks [#119](https://github.com/abeering/uptime-game/issues/119). Leave this PR draft and unmerged; Living Shift #115 remains separate and unmerged.

Both canonical scenes now require confirmed legitimate WORK before a six-minute simulation deadline while retaining CORE loss. Small starts at **900 WORK**, Advanced at **1,200 WORK**. Reaching quota immediately succeeds. A CORE breach on the same authoritative boundary wins over quota success, and final-boundary completions count before timeout evaluation. These quotas are provisional playtest values, not settled balance.

## Review the implementation

- `SimulationHost.Shift.cs` owns exact-once per-operation credit, diagnostics and one shared ending state. `SimulationHost.cs` feeds it only successful allocator completions and clips shift steps to the deadline. `SimulationHost.Integrity.cs` settles CORE before the other ending checks. The allocator and operation lifecycle retain ownership of actual progress and completion.
- `ConnectionsRunRecorder.cs` observes configuration, credits, snapshots and the final result; it cannot grant credit. Every ending saves final diagnostics and finalizes the file. Representative [saved recorder extracts](recorder-extracts.json) include all three causes.
- The two existing scene assets reuse the authored status strip and result panel. `NetworkHealthPanel.cs` displays UPTIME, credited/required WORK, progress and a brief latest-completion delta; its legacy Health path remains for historical fixtures. `IntegrityPresenter.cs` presents all endings, aborts Duel/CLEAN presentation without a concession or penalty, and restores guarded terminal focus. `Terminal.cs` retains diagnostics and same-scene/seed restart after any result.
- Existing pre-shift witnesses explicitly disable the shift objective. The historical Health presentation witness reads its original geometry from the retained prefab instead of testing Health geometry against WORK layout. Canonical parity checks the new authored objective/status availability. No unrelated presentation assertions are relaxed.

No Packet scene, topology, traffic catalog, hostile frequency, infection/defense balance, historical Pressure/Integrity fixture, company narrative, run progression or shared allocator change is included. The [runtime contract](../../Connections/connections-work-objective-v0.md) describes ordering and diagnostics in detail.

## Calibration

[CALIBRATION.md](CALIBRATION.md) explains the policies, candidate deadlines and limitations. [calibration-summary.csv](calibration-summary.csv) contains all 48 natural runs; [calibration.json](calibration.json) retains exact credit facts and six deterministic replay signatures. [Eighteen actual quota-enabled runs](actual-target-runs.json) and [their checks](target-checks.txt) verify measured predictions, crossing times and credit prefixes.

| Scene | Quota | Unattended | Limited visible defense | Kill-all |
|---|---:|---:|---:|---:|
| Small | 900 | 4/8 successes | 6/8 | 0/8 |
| Advanced | 1,200 | 3/8 successes | 5/8 | 0/8 |

The defense policy uses only visible Payload position and node LOAD, at most one intercept/overclock decision every six seconds; it does not scan/CLEAN or win Duels. Some interventions worsen outcomes, some seeds remain far short of quota, and no selected quota completes within 180 seconds in this sample. These findings do not establish impossibility for all strategies or general fairness/fun. Traffic was not modified to improve results.

## Actual validation

Unity **6000.3.11f1 on macOS**, November 20, 2026. Tests ran in disposable copies under `/private/tmp`, preserving historical evidence folders in the working repository. See [validation.txt](validation.txt).

- Editor C# compilation and the final [52 focused assertions](deterministic-checks.txt) pass in both canonical scenes: one/multi-operation benign credit, actual modified requirements, reduced requirement with retained accomplished WORK, hostile zero credit, partial kills, duplicate callbacks/rendering, reset/reused IDs, immediate success, exact final-boundary success, precise quota shortfall, CORE precedence, frozen state, recorder JSON/finalization and late CLEAN/Duel rejection.
- [Integrity checks](integrity-checks.txt) retain the deterministic allocation examples: shared target, independent target, baseline, no contention, operation transition, completion, blocking and released resources.
- [Canonical regressions](canonical-regressions.txt), [parity checks](parity-checks.txt), and [terminal regressions](terminal-regressions.txt) record actual outcomes. Traffic, Ambient Hostile Activity, Network Health, Duel, Surprise/Consequence, Advanced Scene, Recorder, Payload, Worm, Ambient Payload Threat, scan, infections, Node Status, Geometry, Integrity, Hacker Directive, Storefront, Maximum Pressure, canonical parity, Duel concession, Inspect V2, resolved inspect, LOAD, WORK and logs output pass. Inspect V2 was additionally rerun with only shift disabled, retaining its original Integrity configuration, and passes.
- The aggregate regression command exits 1 for **two inherited failures**: Overclock's Advanced compact three-tool row and Resource's inspect-header pill geometry/identity color. Their assertions remain unchanged. Parent reproduction is already preserved in [Integrity validation](../ConnectionsIntegrityV0/validation.txt) and [concession validation](../ConnectionsDuelConcessionV0/validation.txt). This PR does not claim an all-green aggregate suite.
- Actual graphics-enabled scripted Play Mode passes [156 assertions](graphical-checks.txt) and produces **24 captures** at 1920×1080 and 1280×720. It covers start, earned WORK/three-ID inspect and crowded logical paths, success during an active Duel over CLEAN, same-seed restart, timeout during a scan, CORE ending, restored input focus, fitting text and nonoverlapping status bounds. First visual review found overlapping delta/bar text and a clipped result receipt; the final captures/checks include their corrections. This is not a physical keyboard playtest.
- Project Core regeneration/check and `git diff --check` pass. Packet Play Mode was not run. Unity's existing Search startup `ArgumentOutOfRangeException` appeared during graphical startup, including the final run; the Play Mode witness still completed successfully.

Example final 720p captures: [Small WORK/inspect](Captures/Connections/02-work-and-inspect-1280x720.png), [Advanced success](Captures/ConnectionsAdvanced/03-complete-1280x720.png), [timeout](Captures/Connections/05-insufficient-1280x720.png), [CORE](Captures/ConnectionsAdvanced/06-core-1280x720.png). Every state has both scene/resolution variants in `Captures/`. Captures are constructed Play Mode fixtures, not natural-play or human evidence; the short timeout fixture uses a one-second deadline to prove an incomplete scan remains frozen.

Reproduce in a disposable copy (existing regression witnesses write historical review folders):

```text
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.WorkShiftWitness.Run -quit -logFile <focused.log>
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.WorkShiftWitness.Calibrate -quit -logFile <calibration.log>
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.WorkShiftWitness.VerifyTargets -quit -logFile <targets.log>
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.DuelConcessionWitness.Regressions -quit -logFile <regressions.log>
Unity -batchmode -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.WorkShiftCapture.Run -logFile <play.log>
python3 scripts/build_uptime_project_core.py --check
```

`WorkShiftCapture.Run` explicitly applies the same two-scene authoring recipe before entering Play Mode. It must run only in the disposable copy. `Author` adjusts existing serialized elements; ordinary play does not construct or reauthor UI. Calibration records an unreachable diagnostic quota; `VerifyTargets` subsequently tests the selected quotas.

## Physical keyboard review (manual)

1. Open **Connections**, then **ConnectionsAdvanced**. Review each at 1920×1080 and 1280×720. Verify `UPTIME 00:00 / 06:00`, the correct scene quota, an empty progress bar, and existing tool readiness. No Health value/rate should be visible. Let legitimate operations finish: only completed operations increase WORK, the bar and receipt agree, and no partial progress earns credit.
2. Use `inspect` and `inspect -v` for three or more active IDs, scroll long history, and continue typing commands. Check prompt focus, status readability and crowded path labels. Distinguish physical compromised links from logical identity/performance colors. Kill an arc after its first operation: earned WORK remains; its unfinished operation earns nothing. Compare a deliberate kill-all run with selective defense.
3. Play a full shift using the existing tools. Success must appear immediately when precise credit meets quota, with final UPTIME/WORK. Insufficient WORK at six minutes produces its distinct failure. CORE retains its physical winning path. After any result, wait and try a mutating command: simulation time/entities/WORK stay frozen while inspect/history remain usable.
4. For a quick overlay-ending check, temporarily shorten the Inspector shift duration to five seconds and set an unreachable WORK quota. Before the countdown opens, issue `duel auth 3 infection`, then `scan db`, then `clean db`. The ending must close the Duel/CLEAN UI, preserve diagnostic sessions without a concession/PID penalty, and return terminal focus. Repeat a quick quota-success case by temporarily lowering quota and allowing benign traffic to finish while an overlay is active. Restore 360 seconds and the scene's 900/1,200 quota afterward; do not save temporary settings.
5. Hold Escape, Enter, Backspace or a letter across the ending. No unintended concession, cleanup selection or repeated command should occur. Release and type `restart`: the same scene/seed starts at zero WORK and zero UPTIME with cleared Integrity/infections/sessions. Repeat after all three causes. Review native editing/IME if used.
6. Check the saved recording: initial quota/duration, unique connection/operation credit keys, final cause/time/totals and one `shift_ended`, with `endedUtc` present. Restart creates a separate recording. Human input timing, balance and whether the productivity tradeoff is enjoyable remain open review work.

After merge, manually refresh the uploaded ChatGPT Project Core source. This implementation does not merge either PR.
