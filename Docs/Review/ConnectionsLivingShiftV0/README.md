# Living Shift V0 — implementation review

Connections implementation on `feat/connections-living-shift-v0`, based on `d43dab4` (`main` including PR #113). This review accompanies an unmerged PR. Packet and `Connections.unity` are unchanged.

## Implementation

Advanced runs a 360-simulation-second company shift. A compact objective in the network titlebar shows confirmed benign operation WORK against a fixed 2,800 target, remaining time, progress and `BEHIND PACE`. A private seeded plan selects a main premise, a distinct secondary and two later batches, with varied timing/count/spacing and ordinary generated purpose recipes. Sales, reconciliation and reporting produce real connections. Reconciliation uses DB traffic; ordinary inventory ambient targeting remains JOBS. The existing hostile opportunity mechanism continues, with mean waits of 37.5/30/22.5 seconds across the three two-minute windows.

The host exposes its existing authoritative operation completion boundary. A deduplicated ledger credits all benign completed stages, retains earlier credit after interruption and excludes hostile/partial stages. Operations messages, an optional opening call, batch outcomes and generic service/recovery cues use actual owned IDs and observed facts. Owned kills suppress duplicate generic company complaints. Failed/stale/discarded delivery and full activity plans are recorded.

At the deadline, the host stops progression, cancels Duels/CLEAN/calls, unfinished scans and delayed samples, freezes remaining entities, closes the run recording and shows a complete/failed result. The restart button receives UI selection and starts the next seed. The serialized Living Shift switch, on reset/next Play, restores endless Advanced sandbox behavior. No new commands, allocator changes or threat mechanics are introduced. See the [runtime contract](../../Connections/connections-living-shift-v0.md).

Changed runtime seams: `SimulationHost`, `ConnectionTrafficDirector`, `ConnectionsRunRecorder`, `CompanyKillFeedback`, `NetworkHealthPanel`, `Terminal`, CLEAN/Duel/call lifecycle, plus the new `LivingShiftDirector` and `LivingShiftPanel`. Visible UI is an authored `LivingShiftPanel.prefab` instance in Advanced; ordinary runtime hierarchies are not built in code. Focused Editor witnesses/captures, three existing sandbox witnesses and supporting/current-state/generated docs are updated.

## Deterministic measurements and target selection

Unity 6000.3.11f1 ran in an isolated project copy. The main project was left available for the user's Editor. Measurements use the real Advanced host, allocator, purpose recipes, hostile opportunities, infections, Worms, Payloads and operation outcomes. They step at 0.1 simulation seconds (the host subdivides further where its existing mechanics require). Unattended Duels advance their actual CommandRaceSession by 0.1 per simulation step with no player input; the harness does not wait for the real-time splash. These are deterministic simulation witnesses, not real-time human runs.

The initial throughput probe used a placeholder 5,000 target, which exceeded even every protected witness outcome. It was replaced by a fixed **2,800**, about 86% of the protected median (~3,268), then all measurements below were run with that unchanged target. Credits were not scaled, fabricated or altered. This is a provisional experiment setting, not a human balance claim.

Three policies, using the same eight seeds:

- **No input:** no player actions; unattended Duels can lose normally through the existing race/resolution APIs.
- **Diagnostic intervention:** every two simulation seconds, kill internally known hostile connections and remove internally known infections through host APIs. This deliberately privileged upper bound is not representative gameplay, and does not stop independently moving Worms/Payloads or automatically win Duels.
- **Scan/CLEAN/overclock:** rotate through API, AUTH, DB, JOBS, RPT and WEB with the existing eight-second scan; only an `Infected` result requests CLEAN, then wait four simulation seconds and select the visible highest-CPU process. Use overclock on the visibly most oversubscribed internal node when ready. Wait during active Duels and accept unattended losses. This policy reads no `IsHostile`, infection list or hidden malicious PID; it is a reproducible proxy for existing tool use, not physical keyboard play. It uses no connection kills or Probe.

| Policy | Final WORK min / median / max | Complete | Target first reached |
| --- | --- | --- | --- |
| No input | 895 / 1,972 / 2,934 | 1/8 | 335s in its sole success |
| Diagnostic intervention | 2,437 / 3,268 / 3,793 | 7/8 | 258–347s in successes |
| Scan/CLEAN/overclock | 2,025 / 2,986 / 3,683 | 6/8 | 274–346s in successes |

| Seed | No-input WORK | Final Health | Result | Scan/CLEAN/overclock WORK | Final Health | Result |
| --- | ---: | ---: | --- | ---: | ---: | --- |
| 1701 | 1,724 | 68.80 | Failed | 2,025 | 25.77 | Failed |
| 23 | 2,220 | 0.00 | Failed | 3,683 | 19.77 | Complete |
| 42 | 895 | 0.00 | Failed | 3,244 | 0.13 | Complete |
| 99 | 959 | 5.35 | Failed | 3,671 | 5.35 | Complete |
| 4109 | 2,229 | 64.11 | Failed | 2,312 | 99.58 | Failed |
| 8102 | 1,429 | 0.00 | Failed | 2,887 | 96.87 | Complete |
| 777 | 2,464 | 0.00 | Failed | 2,937 | 94.88 | Complete |
| 2026 | 2,934 | 79.79 | Complete | 3,036 | 95.73 | Complete |

Exact totals, eligible and interrupted opportunity WORK, and target-crossing times are in [baseline/diagnostic CSV](seeded-measurements.csv) and [operator-policy CSV](operator-measurements.csv). Seed 99 provides a controlled result change with the same seed/target: normal scan/CLEAN/overclock policy improves 959 → 3,671 WORK, reaching the objective around 274 seconds. Final Health stays 5.35; Health deliberately does not override the WORK result.

**Design evidence:** this sample does not passively win early, and existing tools can materially change WORK delivery. It does not prove that managing benign WORK is interesting or fair. Seed 4109 still fails under both policies despite recovering high Health; seed 1701 remains hard for the slower scan policy. Offering, timing, purpose mix, unattended Duel losses and seed effects remain substantial. Actual play may find the target too demanding, the scanner sweep repetitive, or limited choices about benign work. Do not interpret these figures as evidence for a finished game, a guaranteed winning policy, or seed-independent agency.

## Checks actually run

[Selected actual witness output](witness-results.txt) preserves the pass receipts and measurement lines.

Unity 6000.3.11f1 compiled the implementation. `LivingShiftWitness` passed seeded activity replay, main/distinct secondary/later scheduling, real benign plans, matching opening content, actual ID interruption, lost opportunities, benign-only operation credit, hostile exclusion, retained earlier stages, duplicate-credit rejection, reset, batch outcomes, deadline resolution, frozen simulation, one result receipt, coarse-step schedule/deadline handling, switch-to-sandbox behavior and no experiment in the four-node scene. The expanded witness also checks serious service degradation and genuine recovery before their narrative cues, plus all authored panel references and the fixed target.

Regression witnesses passed: **Traffic, Recorder, Network Health, Consequence, Ambient Hostile Activity and Advanced Scene**. Three Advanced-focused witnesses explicitly disable Living Shift while testing the existing sandbox mechanics, rather than allowing the new experiment to contaminate those baselines. The Network Health witness covers both scenes. Existing allocator/runtime resource logic is unchanged.

**Outstanding baseline failure:** Resource Witness fails `inspect header pill keeps authored large geometry and identity color` in its presentation checks. An independent isolated checkout of unchanged `main` at `d43dab4` fails the identical assertion and stack location. Its earlier uncontended/contention model assertions ran before that failure; the remainder did not run. This PR does not claim a passing Resource Witness or repair unrelated inspect styling. One initial capture launch also logged a Unity Editor QuickSearch indexing exception; the Play Mode assertions completed. Later capture runs and the implementation checks should be assessed by their explicit result logs, not that unrelated Editor indexer.

`python3 scripts/build_uptime_project_core.py` regenerated the Core after the current-state change; `--check` and `git diff --check` passed. Unity run JSON remains in the isolated project's ignored `Logs/UptimeRuns`; committed CSVs and captures preserve the concise review evidence. No physical keyboard/IME playtesting or Packet Play Mode run was performed.

## Scripted Play Mode captures

The capture runner fixes Canvas size before placement, pauses automatic simulation, advances actual gameplay through the host, and settles presentation geometry before rendering the real authored Canvas. Early context is at 22s, overlap at 130s, jeopardy at 290s, and endings at 360s. Ephemeral message lifetime/subtitle animation still uses real time, so fast-forwarded frames can retain messages longer relative to simulation time than natural play. These are scripted evidence, not a substitute for a six-minute keyboard playthrough.

The failed run uses seed 99 with no input. The successful run uses the same seed and **the visible-information scan/CLEAN/overclock policy**, without changing the target. The failure boundary deliberately opens real CLEAN, call and active Duel surfaces near 359s, then verifies their safe closure. Checks also cover terminal draft/focus during opening communications, actual delivery, result selection, frozen time, new seed and restored input after restart.

| State | 1920×1080 | 1280×720 |
| --- | --- | --- |
| Company context and real traffic | [capture](Captures/early-context-1920x1080.png) | [capture](Captures/early-context-1280x720.png) |
| Overlapping activity | [capture](Captures/overlap-1920x1080.png) | [capture](Captures/overlap-1280x720.png) |
| Objective and jeopardy | [capture](Captures/objective-jeopardy-1920x1080.png) | [capture](Captures/objective-jeopardy-1280x720.png) |
| Failed shift | [capture](Captures/failed-ending-1920x1080.png) | [capture](Captures/failed-ending-1280x720.png) |
| Successful shift | [capture](Captures/successful-ending-1920x1080.png) | [capture](Captures/successful-ending-1280x720.png) |

Reproduce with Unity's `-projectPath <isolated-project> -batchmode -executeMethod Uptime.Connections.EditorTools.LivingShiftWitness.Run -quit` (add `-nographics` for numerical witnesses), `LivingShiftWitness.Measure`, or `LivingShiftWitness.MeasureOperator`. The measurement commands write CSVs under `/private/tmp`. Capture with `-executeMethod Uptime.Connections.EditorTools.LivingShiftCapture.Run -witnessWidth 1920 -witnessHeight 1080 -capturePath <output-directory>`, then repeat at 1280×720. The Play Mode capture command exits when done; do not add `-quit` before it enters Play Mode.

## Manual Unity review

1. Open Advanced from disk at 1920×1080, then 1280×720. For exact seeds, uncheck traffic **Randomize Seed** and set seed 99 (or another CSV seed) before Play. Confirm the objective is 2,800, six-minute countdown, and clock 09:00 → 17:00. Use the normal keyboard for the entire shift.
2. Read the opening premise and inspect the corresponding live purposes/targets. Check that the one optional call never steals a partial terminal draft or CLEAN PID entry. Hang up mid-subtitle; traffic and typing must continue. Watch secondary/later batches and natural hostile overlap without developer spawns.
3. Use `inspect`, `work`, `load`, scan, CLEAN, Probe, kill, interception and overclock normally. Confirm stage completions raise confirmed WORK, a killed partial stage does not, and earlier completed stages remain credited. Kill one owned benign connection to review one specific interruption rather than duplicate generic complaints. Compare successful and interrupted batch reactions with recorder IDs.
4. Review label crowding under the real overlapping batches, node statuses, titlebar objective readability, terminal history scrolling, `inspect` output and command focus. Confirm `BEHIND PACE` reads as current pace rather than certain failure. Observe genuine service deterioration/recovery; narration must not reveal covert causes or celebrate ongoing degradation.
5. At the deadline, review complete and failed endings. Test a pending countdown, active Duel, CLEAN splash, PID entry and lingering result near the boundary. No threat, WORK, clock or command progression should continue behind the result. Click restart and use keyboard submit on the selected restart control; confirm another seed, zero credit, cleared old cues and normal terminal input.
6. Disable **Living Shift Enabled**, reset/re-enter Play and compare endless Advanced: no objective/result/shift batches or cadence ramp. Reopen the four-node Connections scene and smoke-test its existing commands. Packet scenes were not edited; a separate smoke test remains manual.

After a future merge, manually refresh the uploaded ChatGPT Project Source for the changed generated Core.
