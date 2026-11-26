# Legitimate Workload Increase V0 — review

Connections-only implementation on `feature/connections-legitimate-workload-v0`, from verified main `9ceff51e5e1b4a6a4dd7b6ae61c25a94b54a82fc`, the normal GitHub merge of human-accepted #132. [Implementation contract](../../Connections/connections-legitimate-workload-v0.md).

## Exact change and Inspector

Both canonical scenes author **1.5×** on **SimulationHost → Connection Traffic Director → Legitimate generated workload → Legitimate Workload Multiplier**. The component default is 1×. Restart after edits. Recipe generation and anomalies finish first; only non-hostile generated operation processing WORK and base requested LOAD are scaled. A concrete 100-WORK/5-LOAD job becomes 150/7.5, retaining nominal 20s, service budget 30s and successful business credit 100.

Business value now travels with the plan before processing scaling, including event/identity/route copies, and is preserved in runtime state. Authored plans retain their existing sum fallback. Recording adds the effective multiplier and queued/resolved definition business value to schema4. Arrival selection/timing rules, PURPOSE weights, hostile recipes, capacity, allocator, tolerance and visuals are untouched. #126's proportional benign scaling informed this implementation; no scheduler or experiment framework was imported. #115/#124/#125/#126 remain unchanged and unmerged.

## Focused validation

Actual Unity **6000.3.11f1** compilation and checks use an isolated project copy; the original open Editor stays open. No ambient calibration or large simulation matrix is performed.

- [452 deterministic assertions](deterministic-checks.txt) compare 1×/1.5× recipes at one fixed seed, including normal, forced static/keyword outliers, forced WAVING and interleaved hostile generation. They verify same IDs/routes/purposes/operation counts, proportional processing requirements, unchanged nominal duration/business value, exact anomaly/behavior/log/effect parameters, unchanged hostile plans and retained value through plan copies. [Paired concrete values](paired-recipes.csv) are generation comparisons, not completed-gameplay measurements.
- A few actual queue/lifecycle/allocator cases in each scene verify metadata, unchanged 20s nominal/30s budget, successful credit 100 at both factors, accounting with recording disabled, and existing successful/failure endings. Actual uncontended elapsed service agrees within the existing 0.02s fixed-step precision (observed about 20.00–20.02s); no allocator tolerance or lifecycle change was made.
- Two simultaneous 100-WORK/5-LOAD jobs with DB capacity 10/baseline 2 complete at 1× and earn 200 business WORK. At 1.5× the same two jobs request 7.5 each, receive 4 each, and time out with zero credit and two failures. The fixture disables Integrity to isolate this mechanical comparison. It does not imply that ordinary gameplay uses that baseline or lacks Integrity consequences.
- [Regression report](regression-checks.txt) runs the existing Business Shift, Fragility, Traffic and Recorder witnesses alongside the new witness. Historical fixed-example fixtures explicitly select1×; authored canonical 1.5× is asserted separately. These fixture changes are not scene tuning.
- [Graphical checks](graphical-checks.txt), [actual Play Mode values](play-mode-comparison.csv) and four 1280×720 captures compare two identical queued jobs at each factor in both scenes, at the same service age. DB capacity 10/baseline 0: requested total 10→15, allocation remains 5 per job; the existing renderer changes from at-speed to slowed. Required processing 100→150, business value 100 and service budget 30 remain. No partial business credit is shown. These are scripted Play Mode captures, not human keyboard playtesting or a balance verdict.

| Scene | 1× | 1.5× |
| --- | --- | --- |
| Small | [Control](Captures/Connections-1x.png) | [Increased workload](Captures/Connections-1.5x.png) |
| Advanced | [Control](Captures/ConnectionsAdvanced-1x.png) | [Increased workload](Captures/ConnectionsAdvanced-1.5x.png) |

Known inherited broad-suite failures remain documented on accepted #132: Advanced Overclock compact tool row, Resource inspect-header styling and stale Generated Identity tuning. This small phase does not rerun that entire broad suite or change those assertions. Human playtesting of #132 was accepted; the feel of this workload increase still needs human review.

Reproduce focused checks using `Uptime.Connections.EditorTools.LegitimateWorkloadWitness.Regressions` with batch `-nographics -quit`. Reproduce the four frames using `Uptime.Connections.EditorTools.LegitimateWorkloadCapture.Run` without `-nographics` or `-quit`; it enters real Play Mode and exits on completion.

## Manual gameplay review

1. Open `Assets/Scenes/Connections.unity`, confirm the Inspector multiplier 1.5 and enter Play Mode with ordinary generated traffic. Repeat in `ConnectionsAdvanced.unity`.
2. Inspect legitimate jobs and node LOAD. Assess whether familiar connection counts now compete meaningfully for capacity. Use existing Overclock/kill/clean/interception tools; verify prompt focus, normal resolution, business credits and restart.
3. For a repeatable comparison, stop Play Mode, disable Randomize Seed, keep the same Seed, and run once at 1× and once at 1.5×. Compare early equivalent jobs' LOAD/processing requirements and service budgets. Later population/outcomes may diverge because real contention changes lifetimes; the scheduling algorithm itself is unchanged.
4. Use `log save`/`log folder` and inspect `run.legitimateWorkloadMultiplier`, `plan_queued.definition.businessWorkValue`, operation requirements and successful `business_work_credit` receipts. Return the scene to 1.5× and discard unsaved review tuning.

Do not infer final difficulty from the mechanical fixtures. The next change should follow gameplay feel. The generated Project Core is refreshed from source and verified; refreshing the uploaded ChatGPT Project Source after the accepted source merge remains a manual step.

## Inspector-save serialization follow-up

After the implementation commit `1135e6c`, a human changed only Advanced's multiplier from 1.5 to 2.5 and saved. Unity reordered existing scene-object blocks and the newly inserted serialized field, emitted its usual whitespace, refreshed TMP style/color caches and rounded a few transform floats. No objects were added or removed. A separate fallback-font diff was generated atlas data for U+2212 (minus), not an authored font change; this package already clears such dynamic data when the Editor closes.

The separate serialization commit adopts native Unity ordering for both canonical scenes. Four consecutive native reserialization passes per scene converge after the first pass and remain byte-stable on passes 2–4. File-ID comparisons confirm no changed values during normalization; the Advanced scene's already-saved TMP/float caches are retained. `AGENTS.md` now prohibits reconstructing scenes into an older Git layout to reduce implementation diffs. The intended gameplay configuration in the PR remains 1.5× in both scenes; the human's 2.5× Advanced tuning remains a separate, uncommitted local edit. Earlier byte-comparison/scope reports describe the original implementation checkpoint, before this serialization-only follow-up. No gameplay code or visible layout was changed by the follow-up.

Generated TMP atlas data may still appear during an open Editor session. Keep it separate from authored changes; do not mark tracked assets assume-unchanged. If Unity asks to reload the externally updated scene, reload it: the local 2.5× value is preserved on disk. Backups of the original human save were made before cleanup.

`.gitattributes` allows Unity's native end-of-line scalar spacing in scenes/prefabs/assets while retaining other whitespace checks. This is a whitespace-check setting, not an ignore rule or content filter.
