# Independent Business Demand V0 — review

Connections implementation on `feature/connections-independent-business-demand-v0`, from verified main `98908528099cdde7eb29ed8aa88515fe7e6bfdfc`. Preparation updated #134 to the human-accepted 2.5× workload in both canonical scenes, passed 456 focused workload assertions plus Shift/Fragility/Traffic/Recorder, and merged normally through GitHub. [Current demand contract](../../Connections/connections-independent-business-demand-v0.md).

## Changes and tuning

One independent seeded exponential clock replaces population-responsive legitimate scheduling inside `ConnectionTrafficDirector`. It feeds the existing PURPOSE builder, concrete queue, registration and lifecycle. No new director, policy selector, nominal-occupancy model, backlog framework, visual or gameplay command is added. The former population/cancellation/outage feedback path is removed. Historical recorder fields and experimental evidence remain.

Inspector: **SimulationHost → Connection Traffic Director → Independent business demand (restart after edits) → Legitimate Mean Arrival Seconds**. Smaller means more arrivals. Start at **Small 23.225807s / Advanced 18.947369s**; these are #125's measured comparable-volume reference means, not its faster stress settings and not a new rate match at 2.5×. The next adjustment should follow human playtesting. Restart after edits; selected seed and concrete-plan determinism are retained.

The accepted 2.5× processing workload, equivalent unscaled business credit, nominal service budget, node capacities, objectives, proportional allocator, PURPOSE weights/destinations/anomalies and independent hostile clock remain. Better throughput does not automatically produce replacement requests. Outages can cancel real Connecting attempts, while company demand continues.

## Actual focused evidence

Unity **6000.3.11f1** ran in an isolated validation copy; the original open Editor was left open. The checks are mechanical comparisons, not a calibration matrix or final balance study.

- [47 passing deterministic assertions](deterministic-checks.txt) compare complete schedules and concrete recipes at one seed across healthy, congested, kill and outage conditions in both scenes. Each uses 6,000 ordinary .02s steps, about 120s; [outcomes.csv](outcomes.csv) retains exact observed simulation duration, rather than implying exact decimal timing.
- [Paired requests](paired-demand.csv) retain ideal double-precision due time, next wait, ID, PURPOSE, target, business value and full recipe/trace SHA256. All 4 Small and 5 Advanced requests match across their four conditions. Every request is admitted through the existing queue, including outage attempts. Healthy cases complete one job and earn 36.210/63.507 business WORK; the congested Small case times out one job, Advanced remains slowed without timeout within this window; kill/outage cases resolve their real interruptions. Their schedules stay identical.
- Diagnostic healthy fixtures use capacity 1,000; congestion fixtures use capacity 10/baseline 9.9; Integrity is disabled and objectives are unreachable to preserve a common observation duration. These are temporary mechanical conditions, not saved gameplay tuning. Actual failure/WORK receipts remain authoritative.
- Additional checks cover reset/replay of previously active hosts and their counters/first due time, recording disabled, empty PURPOSE weighting, finite-ID exhaustion with continuing demand, rejected invalid concrete plans, ambient disable/resume, unchanged ordinary hostile schedule/full recipes under matched .02s boundaries and capacities, and actual independent-demand SHIFT COMPLETE/FIRED freeze/finalization.
- [Seven passing regression groups](regression-checks.txt) retain the focused demand, accepted workload, Shift, Fragility, Traffic, Recorder and canonical-scene witnesses. Obsolete population-feedback assertions are replaced with queue/demand/recorder checks. The known inherited Advanced Overclock row, Resource inspect-header and stale Generated Identity failures are unchanged; the entire broad suite is not rerun for this phase.

## Actual gameplay sample

All 10 graphical assertions pass, with four actual viewport captures. The graphical runner uses the **saved canonical scenes**, fixed seed 1701, normal capacities, actual 2.5× workload, mixed hostile traffic, Integrity and shift objectives. It samples up to 45s and 90s at 1280×720. It respects real Duel pauses and terminal outcomes; a stop before the next target is retained rather than inventing later gameplay. [Graphical checks](graphical-checks.txt) report actual observation times and counters.

[Small gameplay receipt/configuration extract](Connections-gameplay.json) and [Advanced extract](ConnectionsAdvanced-gameplay.json) preserve effective policy/mean/workload, the objective and demand snapshot, and ideal/observed request, queue-definition and admission receipts. They are deliberately small extracts, not complete run logs. The ordinary full recorder remains available in `Logs/UptimeRuns` when reproduced.

| Scene | Earlier sample | Later sample, if no real stop/pause |
| --- | --- | --- |
| Small | [45s target](Captures/Connections-45.png) | [90s target](Captures/Connections-90.png) |
| Advanced | [45s target](Captures/ConnectionsAdvanced-45.png) | [90s target](Captures/ConnectionsAdvanced-90.png) |

These are scripted actual Editor Play Mode frames. They do not establish human keyboard comfort, sustained high-density readability or final shift fairness. Natural bursts, lulls and seed variation need human review; no target was adjusted to manufacture success. The graphical launch logged a nonfatal Unity QuickSearch startup exception; all 10 capture-runner assertions completed successfully.

## Manual review

1. Open `Assets/Scenes/Connections.unity`, confirm 2.5× workload and mean 23.225807s, and enter Play Mode. Repeat in `ConnectionsAdvanced.unity` at mean 18.947369s. Randomize Seed may stay enabled for natural play; disable it and choose 1701 for a repeatable sequence.
2. Use ordinary `inspect -v`, Overclock, kill, CLEAN and interception as needed. Assess whether improving throughput buys real breathing room instead of replacement traffic. Later arrivals continue during slowed/failed jobs and outages, with stochastic bursts and lulls.
3. For a comparison, keep the same seed/interval, restart, and change how quickly you complete or kill jobs. Inspect full recorder `business_demand_arrived` ideal due times through the common simulation prefix; do not compare invented arrivals after a real ending or periods when the player explicitly disables ambient generation. Hostile state-dependent outcomes can legitimately diverge after interventions.
4. Tune only **Legitimate Mean Arrival Seconds**, restart, and play again. Do not compensate with PURPOSE weights, capacity, hostile frequency, timeout or workload changes in this phase. Use `log save` / `log folder` to inspect effective configuration, queue definitions, unadmitted reasons and actual business credits/failures.
5. Verify restart resets the same seed/clock and both objectives. Stop Play Mode and keep/discard Inspector edits deliberately; incidental Unity reserialization is normal and does not require a separate cleanup session.

Reproduce batch checks with `Uptime.Connections.EditorTools.IndependentBusinessDemandWitness.Regressions`. Reproduce the small gameplay sample with `Uptime.Connections.EditorTools.IndependentBusinessDemandCapture.Run` without `-nographics` or `-quit`. No human playtest of this new scheduler is claimed.

## Limits and future direction

The existing 260 generated-ID pool can exhaust in long diagnostic/endless sessions. Demand continues with explicit unadmitted receipts; no unseen waiting queue, retry promise or fabricated business failure is added. There is no new policy for never-materialized business loss. Historical #125/#126 data remains on their existing branches; #115/#124/#125/#126 are untouched and unmerged.

Company circumstances may later change frequency, PURPOSE mix, intensity, batches and individual-request importance. Importance belongs to particular business requests; operator-configured processing priority is separate. Those future systems are not implemented. The Project Core is regenerated from source; refreshing the uploaded ChatGPT Project Source after the accepted merge is manual.
