# Connections Integrity & CORE Breach V0 review

Experimental Connections implementation, November 19, 2026, stacked on `feat/connections-maximum-pressure-v0` / PR #116 at `e8a2f77066f4bff08baffe4020639c536c1279c8`. Living Shift PR #115 is excluded. Open **`Assets/Scenes/ConnectionsAdvancedIntegrity.unity`**.

The [changed-file manifest](FILES_CHANGED.txt) lists the implementation and evidence files. The [implementation contract](../../Connections/connections-integrity-core-breach-v0.md) describes the two-point model, hidden knowledge, damage attribution, graph traversal and agreed presentation. The new scene alone enables Integrity and hides Health's value/bar/rate. Other scene assets, original prefabs, traffic tuning and Packet are unchanged.

## Natural Pressure findings

Twelve natural fixed-seed runs compare unattended, limited visible defense, and indiscriminate connection killing. Runs stop on real breach or after 30,000 ordinary 0.02-second simulation steps, approximately 600 seconds. Float accumulation yields about 599.96 seconds in surviving runs; those are censored observations, not inferred later losses.

| Seed | Unattended loss | Visible-defense loss | Kill-all loss |
|---|---:|---:|---:|
| 1701 | 403.52 s | 402.84 s | None observed |
| 1702 | 205.12 s | None observed | None observed |
| 3141 | 331.16 s | None observed | None observed |
| 9001 | 133.35 s | 347.93 s | None observed |

All four unattended runs lose. Earliest observed loss is **133.35 s**; the four-run median is **268.14 s**. The final causes include lost Duels, accepted infections, and sustained overload, with both external ingresses and both critical nodes represented. This small selected sample does not estimate general loss probabilities or establish fun/fairness.

Limited defense prevents observed loss in two seeds and delays seed 9001's loss by about 215 seconds. It does not rescue seed 1701 and actually ends slightly earlier on a different ingress path. Existing interventions can matter without guaranteeing improvement.

**Kill-all does not prevent all damage:** every run still receives independent infection and/or direct-Duel strikes. Nevertheless, all four kill-all runs survive the observation window. This is a substantial survivability result to human-playtest, not proof of indefinite immunity or a reason to silently increase threat production. There is no Health-based ending in this scene, so ordinary connection killing's existing Health cost cannot end the experiment.

[RESULTS.md](RESULTS.md) gives per-run damage counts, compromised/connected regions, closest approach, and winning paths. [pressure-summary.json](pressure-summary.json) retains every damage event with target/source/cause/time and final full graph. [damage.csv](damage.csv) is the flat event ledger. `Runs/` retains compressed full recorder logs and one-second topology samples.

## Evidence method and limits

- Seeds: 1701, 1702, 3141, 9001. Each scene is opened/reset afresh. No forced damage, outcomes, opportunity frequency, target selection or hidden-state intervention is used in natural runs. Normal ambient generation and the existing consequence planner remain active.
- Unattended submits no gameplay commands.
- Visible defense submits at most one command per six simulation seconds, outside active Duel/splash/result interruption. It chooses a currently visible red-hop Payload and submits `intercept @ <visible-next-node>`; otherwise it overclocks the node with the largest visible requested LOAD/capacity when demand exceeds capacity and cooldown is ready. It does not inspect hidden intent, infections, future Payload routes or perfect CLEAN PIDs. Rejected interceptions remain real failures.
- Kill-all submits one multi-ID kill command at most once per simulation second when not blocked by Duel/result presentation. It kills all currently connecting/active ordinary connections without distinguishing benign from hostile. It neither intercepts independent entities nor types Duel commands. Command counts include submitted attempts, not a claim that every effect succeeded.
- Both policies leave every real Duel race unattended. Edit Mode lacks an unscaled frame loop, so the helper drives the actual overlay-created CommandRaceSession at modeled 1x wall time, including the authored splash and result interruption. It never fabricates a winner or replaces a consequence. Physical keyboard timing is not modeled.
- The finite Edit Mode witness raises terminal history's row cap to avoid delayed-Destroy errors from long output; it changes no simulation or traffic settings. The playable scene retains its authored cap.
- Three exact seed-1701 replays compare every event, snapshot and topology sample, excluding run IDs/UTC metadata.
- Closest approach is the minimum number of **additional internal-node compromises** needed along any physical ingress-to-critical path. It counts nodes, not remaining one-point strikes. Zero means breach. This is an analysis measure, not a new player gauge.

## Provisional overload tuning

Requested LOAD must be strictly above 1.5 × effective capacity for ten continuous simulation seconds. Falling to or below it fully resets the timer. Each additional strike needs a fresh full window.

At capacity 10, requested LOAD 15 does not trigger damage, while 16 does after the full warning. Overclock raises capacity to 15 and thus raises the severe threshold to 22.5; the same 16 LOAD stops and resets the threat. Demand can remain severe even while overclocked. The threshold avoids treating ordinary moderate contention as automatic damage, but overlapping existing demand still produces overload strikes in the natural sample. No spawn or LOAD/WORK tuning was changed to obtain those results.

## Deterministic and regression validation

Unity **6000.3.11f1** compiled and ran in an isolated project copy because the working project was already open. [deterministic-checks.txt](deterministic-checks.txt) records expected/observed assertions for the model, knowledge, overload, Duel, freeze/restart, JSON/full graph and scene bindings. Numerical allocation cases cover shared oversubscription, independent targets, baseline priority, ordinary demand, operation transitions, completion, blocking and released resources.

[regression-checks.txt](regression-checks.txt) records **14 passing existing witnesses**: Traffic, Ambient Hostile Activity, Network Health, Duel, Consequence, Advanced Scene, Recorder, Payload, Worm, Ambient Payload Threat, Scan, Infection, Node Status and Geometry.

Two existing witnesses fail on both this branch and an untouched archive of parent `e8a2f77`:

- Resource: `inspect header pill keeps authored large geometry and identity color`.
- Overclock: `ConnectionsAdvanced compact three-tool row`.

The focused Integrity witness independently checks allocation and overclock mechanics. These older UI assertion failures are retained, not silently rewritten or reported as passes. See [validation.txt](validation.txt) for commands, outcomes and parent reproduction. Existing scene/prefab equality and generated-Core freshness are checked separately.

## Scripted Play Mode captures

`Captures/` contains actual rendered Play Mode UI at 1920×1080 and 1280×720. These are deliberately constructed development fixtures, separate from natural Pressure evidence. Automatic host stepping is disabled in this fixture; damage, scan, CLEAN and command submissions use the actual host/UI paths, and elapsed simulation advances through ordinary fixed steps. The captures do not establish human keyboard usability.

| Fixture | 1080p | 720p |
|---|---|---|
| Confirmed healthy DB and hidden-damaged API look neutral | [frame](Captures/01-neutral-hidden-and-healthy-1920x1080.png) | [frame](Captures/01-neutral-hidden-and-healthy-1280x720.png) |
| Orange known damage; isolated red DB persists after CLEAN | [frame](Captures/02-orange-isolated-compromise-after-clean-1920x1080.png) | [frame](Captures/02-orange-isolated-compromise-after-clean-1280x720.png) |
| External-connected red WEB; busy logical lanes retain colors | [frame](Captures/03-connected-region-crowded-links-1920x1080.png) | [frame](Captures/03-connected-region-crowded-links-1280x720.png) |
| Overload countdown and existing diagnostic status | [frame](Captures/04-overload-with-existing-statuses-1920x1080.png) | [frame](Captures/04-overload-with-existing-statuses-1280x720.png) |
| Duel over CLEAN before terminal loss | [frame](Captures/05-duel-over-clean-before-breach-1920x1080.png) | [frame](Captures/05-duel-over-clean-before-breach-1280x720.png) |
| CORE BREACHED, frozen graph and winning path | [frame](Captures/06-core-breached-frozen-1920x1080.png) | [frame](Captures/06-core-breached-frozen-1280x720.png) |
| Terminal restart returns cards, paths and clock to initial state | [frame](Captures/07-restart-1920x1080.png) | [frame](Captures/07-restart-1280x720.png) |

A UnityEditor.Search startup indexing ArgumentOutOfRangeException appeared in the capture log; Play Mode and all captures completed. It is an Editor tooling caveat, not a clean-Console claim. Scripted captures were visually reviewed; human interaction and natural-pressure readability remain outstanding.

## Manual Unity review

1. Open the Integrity scene at both 16:9 sizes. For a repeatable visual walkthrough, disable Auto Generation before Play and leave Randomize Seed unchecked; do not save these temporary review settings.
2. `scan db`: after the existing scan duration, confirm `INTEGRITY 2/2` and unchanged card color. `infect api backdoor`: API should stay neutral while unexamined. `scan api` reveals 1/2 and orange condition.
3. `clean api`: select the high-CPU process. Infection status clears, Integrity remains 1/2, and the card stays orange. Check actual typing, focus restoration, scrolling and ordinary `inspect`/`work` output.
4. Submit `integrityhit db` twice. DB turns red without ending the run or establishing an external red route. Submit `integrityhit web` twice: EXT1–WEB turns red, while WEB–API remains neutral because API still has one Integrity.
5. Submit `integrityhit api`. Confirm CORE BREACHED identifies EXT1 → WEB → API → DB; no simulation effects progress. Confirm the terminal stays usable for read-only review and `restart` clears the ended state. Repeat with CLEAN/Duel open to check safe closure and focus.
6. On a fresh run, `infect jobs phantom-load 16` creates hidden initial damage and severe load. Observe the full warning; overclock JOBS before expiry and confirm the warning resets through effective capacity, without restoring damage. Let overclock expire and observe a fresh full warning. Use the deterministic witness for isolated repeated-overload strikes without an initial infection strike.
7. Restore the authored generation settings and compare natural seeds. Review red physical links under dense lanes, identity pills/sockets, small infrastructure markers, stacked statuses, and whether orange versus neutral/red is enough to prioritize attention. Human play should assess whether the kill-all survival result is acceptable for this experiment.
8. Open the original Connections, Advanced and Pressure scenes for smoke review. Packet scenes are untouched; Packet Play Mode was not run in this implementation.

## Reproduction

Run installed Unity 6000.3.11f1 against an isolated checkout/project copy:

```sh
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.IntegrityWitness.Run -quit -logFile <integrity.log>
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.IntegrityWitness.RunRegressions -quit -logFile <regressions.log>
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.IntegrityPressureWitness.Run -quit -logFile <pressure.log>
Unity -batchmode -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.IntegrityCapture.Run -logFile <captures.log>
```

The regression runner runs all sixteen checks and exits nonzero if any fail; currently the two parent-reproduced failures above remain. Evidence helpers overwrite only this experiment's review output and ordinary ignored recorder logs. `IntegritySceneAuthoring.Build` recreates only the new scene and experimental status prefab; normal review does not require rebuilding them.
