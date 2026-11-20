# Connections — Maximum Pressure V0 review

**Historical fixture:** Canonical gameplay now lives in Small/Advanced under [Canonical Scenes V0](../../Connections/connections-canonical-scenes-v0.md). Pressure and the evidence below remain regression material, not canonical difficulty/presentation authority.

Connections implementation experiment, November 19, 2026. Independent branch from `main` at `d43dab45eba5a848011d3039d3e29f9b46942aee`; Living Shift PR #115 is excluded and remains unmerged.

Open **`Assets/Scenes/ConnectionsAdvancedPressure.unity`**. This is a full authored copy of Advanced with a unique scene GUID and exactly four serialized changes:

| Setting | Advanced | Maximum Pressure |
|---|---:|---:|
| Mean hostile opportunity interval | 30 s | 8 s |
| Ordinary hostile weight | 4 | 6 |
| Worm weight | 1 | 2 |
| Payload Launcher weight | 1 | 2 |

Reconnaissance and direct Payload remain at 2 each; direct and carrier Duel remain at 1 each. Opening grace stays 3 s. Benign coverage stays at its original 1→4 goal over 75 s, with all purpose definitions, routing and LOAD/WORK values intact. No benign adjustment was needed to demonstrate consequential interference in these runs. The exponential clock and seeded selector, including failed selections and global Duel slot rejection, are unchanged.

There are **no gameplay runtime, existing-scene, prefab, scoring, command, effect, or ending-system changes**. The new Editor witness records comparisons; the capture helper observes natural Play Mode. Health zero retains its existing failure latch and does not stop this sandbox.

## Findings to take into human playtesting

The requested tuning creates much more overlapping activity and materially worse legitimate service in this four-seed sample. Pressure makes 12–32 selections versus baseline's 3–10. This is not itself proof of urgency: the report separately preserves actual establishment, resolution, infections, entity creation, slot rejections, slowdown and Health.

| Seed | Baseline unattended min / final Health | Pressure unattended min / final | Pressure limited intervention min / final |
|---:|---:|---:|---:|
| 1701 | 99.74 / 100.00 | 39.75 / 49.23 | 67.96 / 83.16 |
| 1702 | 97.86 / 97.86 | 96.12 / 96.12 | 99.69 / 100.00 |
| 3141 | 100.00 / 100.00 | 38.44 / 38.44 | 99.56 / 99.56 |
| 9001 | 76.09 / 76.09 | 39.46 / 39.46 | 69.20 / 69.20 |

For example, unattended seed 9001 at 120 s has eight established hostile connections, four moving Payloads, one live Worm branch, three infections and three Conditions. At 180 s it has fourteen established hostiles and five infections. Seed 1701 falls to about 40 Health, then recovers to 49 as interference clears; limited intervention ends at 83. No seed hits zero in this observation window and there is no immediate deterministic collapse to tune away.

The counterexample matters: Pressure seed 1702 ends near 96 Health. Covert infections, harmlessly placed attacks, long routes, delayed operations, Worm Conditions and slot restrictions do not become urgent just because a selection succeeded. Pressure 1701's first established hostile is around 9.5 s, but its first sampled overt symptom is around 34 s; seed 3141's is around 37 s. The retained exponential process can still produce quiet openings. Keep the proposed tuning for this experiment; human play should decide whether these quieter seeds are valuable variation or insufficient pressure.

The limited policy improves final Health in all four Pressure seeds with 5, 5, 5 and 11 command submissions respectively, including real rejected interceptions. It does not establish that command spam is satisfying, that a human can diagnose/execute at the same moments, that CLEAN recovery is enjoyable, or that every overlapping entity demands attention. High-density hidden intent is not the same as recognizable trouble.

## Evidence and measurement method

[Measured tables](RESULTS.md) include every run, all activity types, first activity times, sampled quiet intervals and concurrency at 30/60/90/120/150/180 s. [summary.json](summary.json) retains per-kind infections, resolution outcomes, actual Duel scheduling/rejection reasons, benign slowdown/cancellation, Payload outcomes and other concrete counters. [activity-counts.csv](activity-counts.csv) and [concurrency.csv](concurrency.csv) provide flat comparisons. `Runs/` contains the full existing recorder JSON compressed with gzip and 180 one-second samples for each comparison.

- Four fixed seeds: **1701, 1702, 3141, 9001**. Each actual scene is opened afresh for unattended and limited-policy runs. The host takes 9,000 ordinary 0.02-s steps (180 simulation seconds, with normal float accumulation). Authoritative Health minimum is observed every step; per-second samples and recorder snapshots retain concurrency and service state. Four seed-1701 replays compare every event and snapshot, excluding nondeterministic run IDs/UTC metadata, and all per-second samples.
- Selected/produced means the selector attempted an activity and successfully queued a plan, created an entity, or accepted a direct Duel. A queued carrier is not yet an established connection or accepted Duel. Actual connection counts include ambient ordinary/recon/launcher/carrier plans, recon follow-ups and infection Spawner children. Resolved counts include connecting cancellations, so they can exceed established counts. Worm and Payload creation counts include Duel consequences and launchers. Scheduling attempts are `duel_scheduled + duel_rejected`; selecting a carrier does not immediately schedule a Duel.
- Both policies let every Duel race run without player typing and accept its real resulting consequence. Edit Mode has no unscaled frame loop: the helper advances the **same race session created by the real overlay**, using 0.02 s of modeled wall time per simulation step, with an approximate 50-Hz authored splash and 1.1-s result interruption. It never fabricates a winner, releases a busy slot to permit a selection, substitutes effects, or removes a threat. Actual interactive Duel pacing depends on unscaled frame time; this is deterministic simulation evidence at modeled 1x time, not a recording of human play.
- Limited policy: at most one command every six simulation seconds, postponed during active Duel/splash/result interruption. It selects the first currently visible red-hop Payload and submits `intercept @ <visible next node>`; otherwise it submits `overclock` for the node with the highest visible requested LOAD/capacity when that ratio exceeds one and cooldown is ready. Commands go through the existing terminal submission handler and ordinary validation. Cooldown failures remain failures. It neither types a Duel nor reads hidden connection intent, future Payload routes, covert infections, scan results or a perfect CLEAN PID. It does not kill or clean anything. Overclock/intercept costs and unintended service slowdown remain real.
- Quiet intervals have two views: no mechanical hostile presence (including connecting connections, infections, Conditions, entities and Duels), and no sampled **concrete overt symptom** (Worm, red-hop Payload, Duel, Condition, Blackout, node overload or legitimate degradation). These are explicit observations, not an actionability score. Ordinary-looking hostile paths and covert infections can be active without either being recognizable as malicious. Conversely, an overt Condition/overload need not justify immediate action.
- Minimum/final Health is not a new score. Slowed sample counts are seconds with at least one observed benign satisfaction below 0.98, not integrated slowdown duration. Sampled quiet boundaries can miss brief events. This small fixed sample does not estimate population-level probabilities or prove fun, fairness, recovery under full player play, or a generally balanced mode.

To reproduce, run `Tools/Connections/Run Maximum Pressure Witness`. It overwrites only this experiment's measurement evidence and ignored normal run logs; it does not save scene changes. Gzip records can be read with `gzip -dc Runs/<run>.json.gz` or Python's `gzip.open`. Summaries count the named recorder events and use the adjacent samples as described above.

## Unity validation

Unity **6000.3.11f1** compiled the independent project and passed:

- Maximum Pressure Witness: scene GUID, exact four-setting copy equivalence, loaded topology/tuning, no missing scripts, CLEAN/call bindings, normal generated benign/hostile traffic and recording, sixteen comparisons, four exact seeded event/snapshot/sample replays.
- Existing **Traffic, Ambient Hostile Activity, Network Health, Duel, Consequence, Advanced Scene, Recorder, Payload, Worm, and Ambient Payload Threat** witnesses. The latter exercises INFECTOR/launcher/CLEAN and propagation lineage. Existing witnesses retain their original coverage; they are not extended claims about physical keyboard delivery in the new scene.
- Source/derived-doc check, scoped code/docs whitespace check and original-scene/runtime/prefab equality against the base are recorded in [validation.txt](validation.txt). The full whitespace check flags the copied Unity scene’s existing blank-value spacing; the exact serialized copy is intentionally retained.

The batch commands are:

```sh
Unity -batchmode -nographics -projectPath <checkout> -executeMethod Uptime.Connections.EditorTools.MaximumPressureWitness.Run -quit -logFile <measurements.log>
Unity -batchmode -nographics -projectPath <checkout> -executeMethod Uptime.Connections.EditorTools.MaximumPressureWitness.RunRegressions -quit -logFile <regressions.log>
Unity -batchmode -projectPath <checkout> -executeMethod Uptime.Connections.EditorTools.MaximumPressureCapture.Run -logFile <captures.log>
```

## Play Mode captures and manual comparison

The capture helper runs **natural seed 9001 at 1x in Play Mode**, with no forced entities/opportunities, overrides, time skipping or gameplay interventions. It submits only `inspect -v` / `work db` information requests through the input field event. Each scene frame is rendered through an offscreen camera at both requested resolutions. This is actual running UI, not a static Editor mockup or a claim that a human played it. The adjacent [live recorder evidence](Captures/play-mode-run.json.gz) is retained with the frames. The two resolutions are captured consecutively while the simulation continues, so their Health and progress can differ slightly. The late images retain 14–16 live connections; the Health strip and persistent prompt remain visible. At 150 s the two orange Payload bodies near API are close together; moving-body crowding remains a human readability review point. A UnityEditor.Search startup indexing `ArgumentOutOfRangeException` appeared in the capture log; Play Mode continued and all six captures completed. This is recorded as a tooling caveat, not hidden as a clean Console claim.

| Moment | 1920×1080 | 1280×720 |
|---|---|---|
| 90 s: Worm activity, crowded paths, inspect | [frame](Captures/seed9001-90s-1920x1080.png) | [frame](Captures/seed9001-90s-1280x720.png) |
| 150 s: accumulated threats, service WORK | [frame](Captures/seed9001-150s-1920x1080.png) | [frame](Captures/seed9001-150s-1280x720.png) |
| 175 s: late density and Health jeopardy | [frame](Captures/seed9001-175s-1920x1080.png) | [frame](Captures/seed9001-175s-1280x720.png) |

1. In Unity 6000.3.11f1, open normal `ConnectionsAdvanced.unity` and play about three minutes. Use existing commands normally and observe the Health strip. Save/mark observations with the existing recorder.
2. Open `ConnectionsAdvancedPressure.unity` and play about three minutes. Compare urgent decisions, attention conflicts, missed threats, actual service deterioration and recovery. Health reaching zero is evidence; it does not end this sandbox.
3. For reproducible comparisons, stop Play, select the Simulation Host / `ConnectionTrafficDirector`, uncheck **Randomize Seed**, set **Seed** to 1701, 1702, 3141 or 9001, and enter Play again. Use the same seed in both scenes; repeats are comparisons, not an identical attack sequence across different weighted configurations. Default randomization remains enabled in both authored scenes.
4. At **1920×1080 and 1280×720**, inspect multiple IDs, watch dense pills at WEB/API and stacked node Conditions, scroll history, and keep typing a partial command. Check legibility and persistent prompt focus. Let a natural Duel interrupt CLEAN and verify restoration after the race/result. Confirm calls and other existing communication surfaces remain usable. Use `kill`, `scan`/CLEAN, Probe, isolate, intercept and overclock as appropriate; do not use developer spawning to manufacture the primary playtest pressure.
5. Note the time and state when a prioritization decision becomes interesting, when a threat stays hidden, when the scene becomes chores, or when recovery feels earned. Include whether several problems competed for attention and whether actual action ordering mattered. Human keyboard playtesting remains the decisive test.

No physical-keyboard/IME or human urgency verdict is claimed by this PR. The tuning preserves Advanced's visible bindings and focus systems exactly; only gameplay demand changes.
