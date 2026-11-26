# Business Reliability & Shift Objectives V0 — review

Connections implementation based on merged main `4c099d76328c12bc64240f0607b0066b3be3e90b` (#131). The [implementation contract](../../Connections/connections-business-reliability-shift-v0.md) defines the rules and accounting. This evidence concerns the canonical Small and Advanced scenes; it does not establish natural congestion or final shift balance.

## Review the playable loop

Open `Assets/Scenes/Connections.unity` or `Assets/Scenes/ConnectionsAdvanced.unity`, enter Play Mode, and use **Tools → Connections → Business Shift**. Each menu resets an in-memory fixture using real plans, queues, allocation, lifecycle and resolution. No test gameplay commands or saved fixture tuning are added. Stop Play Mode and reload the scene to return to ordinary authored traffic and objectives.

| Menu | Expected result / intervention |
| --- | --- |
| Successful whole-connection WORK | READ40 / EXECUTE60 / WRITE100: intermediate operations leave WORK at zero; final success credits 200 and shows SHIFT COMPLETE. |
| Reliability defeat or rescue | Two existing `product-lookup` jobs share DB with baseline LOAD4. No action reaches two timeouts and FIRED. Overclock can rescue them. |
| Just-in-time success versus dismissal | Begins at genuinely earned WORK200/300 and one real killed job, FAILURES1/2. Inspect queued jobs; `overclock db` after roughly ten service seconds saves both. Final WORK400/300, failures1/2. No intervention reaches FIRED at WORK200, failures3: both failures on the final boundary remain counted. |
| Failed multi-operation job earns zero | READ40 and EXECUTE60 process successfully; final WRITE stalls and times out. WORK0, failures1, FIRED. |
| CORE breach with unfinished business | Actual Integrity damage creates a breached physical CORE path while a 1,000-WORK job is unfinished. CORE BREACHED, WORK0, failures0; unfinished work remains diagnostic state. |
| Beyond six minutes without a deadline | Empty controlled shift advanced beyond 360 seconds: UPTIME continues, WORK0, no ending. |

For every outcome, try `inspect -v <id>`, `scan db`, `logs`, and an attempted gameplay mutation, then `restart`. Diagnostics remain available, mutations are rejected, and restart clears WORK, failures and outcome with the same scene/seed. The result panel is reused for all outcomes. Also review command history/input focus, the tool row, identity readability and crowded multi-ID inspect output at **1920×1080 and 1280×720**. Keyboard timing and live readability still require human playtesting.

## Actual automated and scripted evidence

Executed with **Unity 6000.3.11f1**, using an isolated project copy with the working branch's Assets. The original open Editor was left open. Batch compilation and deterministic witnesses use `-batchmode -nographics`; graphical evidence uses an actual Editor Play Mode viewport and renderer. Captures are scripted, not a human keyboard playtest.

- [Deterministic checks](deterministic-checks.txt): **88 passing assertions** covering whole-job accounting, modifier invariance, exact-once canonical receipts, hostile exclusion, failed arcs, failure/WORK boundaries, simultaneous success/failure and CORE precedence, reentrant observers, exact-deadline completion, coarse-step stopping, recording disabled, frozen diagnostics, mutation gates, full reset and deterministic replay.
- [Graphical checks](graphical-checks.txt): **672 passing scripted assertions**, **56 native viewport captures** (14 situations × two scenes × two resolutions), including objective text bounds, column/tool separation, all outcomes, CLEAN/Duel cleanup without consequences, frozen scan/model state, restored command draft/focus, shared heartbeat freeze and same-seed restart.
- [Existing canonical regression report](existing-canonical-regression-checks.txt), [mechanics report](existing-fragility-regression-checks.txt), [visual integration report](existing-regression-checks.txt) and [lab checks](lab-checks.txt) retain the actual results. The three inherited failures below remain visible; the aggregate regression command consequently exits unsuccessfully.

The integrated run passes Fragility (126 assertions), canonical parity (883), Lifecycle, Behavior, Natural LOAD noise, Backdoor, Loader, Infector, Inspect V2/resolved inspect, Duel concession, LOAD/WORK/logs output, Traffic, Ambient Hostile Activity, Network Health, Duel, Consequence, Advanced Scene, Recorder, Payload, Worm, Ambient Payload Threat, Scan, Infection, Node Status, Geometry, Integrity, Hacker Directive, Storefront, Maximum Pressure, ID Hopping, Reactive Identity, Probe and Probe Sweep. Visual integration passes 81 assertions with two unavailable allocation measurements; the Display Lab passes 512 assertions with one unavailable measurement.

The inherited failures are **Advanced Overclock compact tool row**, **Resource inspect-header styling**, and **stale Generated Identity traffic tuning assertion**. Their assertions remain unchanged. Historical mechanics fixtures opt out of objective endings. The retained Health fixture tests its original prefab geometry rather than expecting the new canonical WORK bar to retain the old Health bar's height. Allocation instrumentation reports unavailable sentinel measurements honestly; these are not zero-allocation claims. A nonfatal Unity QuickSearch startup exception appears in the graphical log; the scripted capture runner completed all checks.

Useful screenshot sequences (all images have both resolution variants in their scene folders):

| Situation | Small, 1280×720 | Advanced, 1280×720 |
| --- | --- | --- |
| Authored objective strip | [Small](Captures/Connections/01-authored-objectives-1280x720.png) | [Advanced](Captures/ConnectionsAdvanced/01-authored-objectives-1280x720.png) |
| Whole-job receipt + multi-ID inspect | [Small](Captures/Connections/02-credit-and-inspect-1280x720.png) | [Advanced](Captures/ConnectionsAdvanced/02-credit-and-inspect-1280x720.png) |
| Intermediate processing, zero credit | [Small](Captures/Connections/03-intermediate-zero-credit-1280x720.png) | [Advanced](Captures/ConnectionsAdvanced/03-intermediate-zero-credit-1280x720.png) |
| SHIFT COMPLETE | [Small](Captures/Connections/04-shift-complete-1280x720.png) | [Advanced](Captures/ConnectionsAdvanced/04-shift-complete-1280x720.png) |
| FIRED | [Small](Captures/Connections/08-fired-reliability-1280x720.png) | [Advanced](Captures/ConnectionsAdvanced/08-fired-reliability-1280x720.png) |
| Just-in-time rescue | [Small](Captures/Connections/11-just-in-time-saved-1280x720.png) | [Advanced](Captures/ConnectionsAdvanced/11-just-in-time-saved-1280x720.png) |
| CORE BREACHED | [Small](Captures/Connections/12-core-breached-1280x720.png) | [Advanced](Captures/ConnectionsAdvanced/12-core-breached-1280x720.png) |
| Restart after CORE | [Small](Captures/Connections/13-restart-after-core-1280x720.png) | [Advanced](Captures/ConnectionsAdvanced/13-restart-after-core-1280x720.png) |
| No six-minute deadline | [Small](Captures/Connections/14-unbounded-uptime-1280x720.png) | [Advanced](Captures/ConnectionsAdvanced/14-unbounded-uptime-1280x720.png) |

Restart after success and dismissal, plus the failed arc before/after timeout, are retained in the same capture folders. Prior terminal history is deliberately preserved between fixtures. Fixture IDs can be longer than ordinary short IDs; their inspect layouts are not a redesign of accepted #131 assets.

Reproduce deterministic/regression evidence with `Uptime.Connections.EditorTools.BusinessShiftWitness.Regressions`; reproduce sampled values with `.SamplePlans`. Reproduce screenshots with `Uptime.Connections.EditorTools.BusinessShiftCapture.Run`, **without `-nographics` or `-quit`**; it enters Play Mode and exits after completion. The Author method is a development authoring helper, not a prerequisite for review: the scenes already contain the authored objectives and bindings.

## Recorder and provisional tuning

[Recorder examples](Recorder/) retain schema4 evidence extracts from both scenes for success, failed arc, reliability dismissal, just-in-time rescue and CORE breach. Every event and the final diagnostic snapshot are retained; repetitive periodic snapshots and empty/default fields are omitted for review. Each file declares that extraction. The capture command writes full unabridged logs when reproduced. `operation_completed` remains processing evidence. A legitimate `connection_resolved` Completed is followed by a single `business_work_credit` with canonical ID, fixed business value, cumulative credit and `operationIndex=-1`. Existing failure events are not duplicated. `shift_ended`, final snapshot and metadata retain the complete final boundary, including genuine quota/count overshoot and unfinished business. These are controlled short runs, not ambient calibration.

The [200-plan sample per scene](plan-value-sample.csv) uses concrete generated benign operation requirements, including generation-time anomalies. Small mean101.422 (range36.210–347.350), Advanced mean87.926 (range19.216–328.889). The provisional **1,000 / 1,500 WORK** targets are about ten / seventeen mean sampled job values; **ten failures** is independently configured in each scene. This sample is not the distribution of successfully completed jobs, a throughput measurement, or proof that ambient demand reliably creates pressure. No hostile, allocator, timeout, ambient demand or accepted connection visual tuning changed.

## Selective donor use

Primary donor [#124](https://github.com/abeering/uptime-game/pull/124), head `7cec0ae7e2c0b363fa07613c99cc4b3e2898dcf3`, remains unmerged and unchanged.

| Adapted | Superseded / deferred |
| --- | --- |
| Host-owned outcome, mutation gating and recorder finalization structure | Fixed 360-second deadline and insufficient-WORK loss |
| Authored strip/result bindings, overlay cleanup and focus restoration | Per-operation business credit; current credit requires whole-job success |
| Same-seed reset and actual Play Mode capture approach | Old 900/1,200 targets and fixed-duration calibration |
| Retained-prefab legacy Health fixture | Full multi-shift progression, upgrades, persistent run state |

Full historical donor evidence remains on its own PR/branch. #115/#124/#125/#126 are untouched. The next intended phase is Business Demand & Congestion. Moving deadline checkpoints, SERVICE-block reduction and identity palette narrowing remain deferred.
