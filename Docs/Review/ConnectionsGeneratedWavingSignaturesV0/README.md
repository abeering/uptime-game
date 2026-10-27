# Connections Generated WAVING Signatures V0 — review evidence

This is a **Connections generation** change on merged PR #48. The root [TODO](../../../TODO.md) remains a future tuning note: natural LOAD noise cadence/amplitude and WAVING runtime mathematics are unchanged here. The authored scene keeps `benignOutlierChance: 0.08` and now serializes `benignWavingOutlierChance: 0.3`, INFECTOR primary weights 50/10/15/10/15, and the existing 10% subtle and 15% secondary chances. These are provisional four-node playtest values.

Benign WAVING outliers and INFECTOR WAVING signatures call the same operation-scope sampler. It chooses a negative offset of 40–75% of up to 2 sampled base LOAD, a positive offset from 0.4 up to the smaller of 1.5 or 75% of sampled base LOAD, a 15–30-second full period, and a 2–5-second onset. Both retain canonical natural LOAD noise and productive WORK. A WAVING signature cannot pair with high/low LOAD or another WAVING; operation deviation and long WORK remain compatible. STANDARD carriers mutate a sampled PURPOSE operation; DELAYED carriers keep the full original PURPOSE arc and put WAVING on the appended delivery operation. No player-facing intent or anomaly label was added.

For a WAVING INFECTOR only, the final-stage WORK correction uses each operation's upper possible demand (base, WAVING maximum, and the configured natural-noise amplitude) as a conservative lower bound on actual duration. It targets at least 25 seconds after establishment and, when the first deviation is in the delivery operation, at least 18 seconds after the WAVING onset. The recorder keeps pre/post conservative estimates, WORK added and reason; other signatures retain their previous nominal timing rule.

## Unity validation

Unity 6000.3.11f1 compiled an isolated project copy. Natural LOAD Noise, WAVING, Resource, Behavior, Traffic, Recorder, Network Health, Lifecycle, Infection, LOADER, INFECTOR, Inspect V2, and six-seed metrics runners passed. The deterministic generation witness sampled 2,500 benign plans: **206 outliers (8.24%)**, including **57 WAVING (27.7% of outliers)**. In 2,400 INFECTOR plans it found 222 subtle carriers and 2,178 anomalous carriers, with primary counts high LOAD 1,078 (49.5%), low LOAD 220 (10.1%), operation deviation 306 (14.0%), long WORK 213 (9.8%), and WAVING 361 (16.6%). These are finite seeded samples, not exact guarantees.

The witness checked shared parameter bounds and near-equal positive-offset means for benign and INFECTOR WAVING on the same PURPOSE, selected-operation and queued-plan provenance, STANDARD/DELAYED arc preservation, natural-noise composition, WORK progress, typed infection installation, kill and Blackout interruption, compatible and rejected secondary signatures, and seeded replay. Four uncontended forced WAVING cases delivered after **52.2, 106.3, 91.2, and 121.5 seconds** from establishment. A separate seeded case required **13.2 extra final-stage WORK**; it delivered after **92.5 seconds**, with a **35.5-second** final operation and **3.9-second** wave onset. These cases establish the minimum windows without extending already long carriers. Unity Edit Mode simulation does not establish interactive readability.

## Six ordinary 600-second runs

The before run used merged PR #48 code; PR #49 added only `TODO.md`. The after run used this branch. Both used seeds 1701, 456, 90210, 42, 1776, and 31415; hostile generation enabled; no player commands; 3,000 steps of 0.2 simulated seconds. Each seed queued the same number of hostile arrivals before and after: **11, 15, 12, 16, 9, 12** (75 total). The generator now consumes different plan draws when selecting and sampling signatures, so the objective mix and downstream coverage feedback can differ even though the separate hostile arrival clock and LOADER recipe are unchanged.

| Seed | Benign plans | Benign outliers | Benign WAVING | INFECTOR plans | INFECTOR WAVING | LOADER plans | Infections applied |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1701 | 18→20 | 2→3 | 0→0 | 8→5 | 0→1 | 3→6 | 4→5 |
| 456 | 20→19 | 1→1 | 0→1 | 7→8 | 0→0 | 8→7 | 3→4 |
| 90210 | 20→23 | 3→3 | 0→0 | 4→4 | 0→0 | 8→8 | 2→2 |
| 42 | 15→21 | 0→2 | 0→0 | 8→6 | 0→0 | 8→10 | 4→1 |
| 1776 | 15→20 | 0→1 | 0→0 | 4→3 | 0→2 | 5→6 | 2→1 |
| 31415 | 22→22 | 0→0 | 0→0 | 5→6 | 0→0 | 7→6 | 2→2 |

| Seed | Completed INFECTOR mean service | Contention fraction | Health mean | Health minimum | Health end |
|---|---:|---:|---:|---:|---:|
| 1701 | 96.0→188.3 s | 56.2→4.5% | 94.7→99.4 | 60.1→91.6 | 60.1→99.8 |
| 456 | 75.6→76.2 s | 38.7→18.6% | 98.8→99.0 | 90.5→90.2 | 97.2→100.0 |
| 90210 | 102.4→102.9 s | 41.8→40.0% | 97.9→99.2 | 77.3→89.2 | 77.3→89.2 |
| 42 | 185.8→105.2 s | 63.5→19.8% | 42.3→87.5 | 0.0→48.3 | 19.9→100.0 |
| 1776 | 93.2→239.6 s | 54.2→28.5% | 70.8→97.8 | 0.0→81.8 | 0.0→97.8 |
| 31415 | 166.3→166.3 s | 39.7→39.7% | 72.9→72.9 | 19.6→19.6 | 64.4→64.4 |

Across these six runs, benign plans changed **110→125**, INFECTOR plans **36→32**, WAVING INFECTOR plans **0→3**, infections applied **17→15**, and completed benign connections **71→76**. Completion-time means are based on small, changing sets of completed INFECTORs, so the per-seed movement is not a controlled effect estimate for WAVING. The large contention and Health shifts in some seeds follow changed objective/signature draws and coverage feedback; unchanged hostile arrival counts and the passing LOADER witness argue against a scheduler or LOADER-mechanics regression. The sample is too small to judge balance and was not tuned toward a target Health score.

## Manual 16:9 Play Mode review

Open `Assets/Scenes/Connections.unity` at 16:9. Disable **Randomize Seed** to repeat a chosen run. Observe naturally generated traffic and use the internal run log after the fact to identify a benign WAVING outlier and an INFECTOR WAVING carrier. Compare their existing one-decimal LOAD pills and productive WORK without using hidden objective information during play. Check that the wave is noticeable above normal noise, sometimes concerning without immediately exhausting node capacity, and not a reliable threat verdict. Confirm inspection readability and command focus. No interactive Play Mode capture or input test was performed by the batch validation.
