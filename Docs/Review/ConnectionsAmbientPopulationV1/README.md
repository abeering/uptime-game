# Connections Ambient Population V1 — playtest amendment

## Two-stream Traffic Director restructure — current PR #40

The fixed mixed-arrival source is gone. Benign coverage is the sole ordinary benign producer, and a separate exponential clock produces hostile PURPOSE plans (scene mean: 54 seconds). The six canonical PURPOSE profiles retain their doubled mean WORK. The benign controller retains its 1→3 goal ramp over 75 simulated seconds; its scene tuning is an 8-second mean opportunity interval and a 12% chance at or above the effective goal. These values are four-node playtest tuning, not a cap.

Unity 6000.3.11f1 ran deterministic Edit Mode simulations for seeds 1701, 456, 90210, and 42, with and without ambient hostiles. Each run advanced 600 simulated seconds in 0.2-second steps. Occupancy below counts **established benign** connections after each step; `benign` and `hostile` arrivals count their respective queued ambient plans. No player commands were issued. `HostileGenerationEnabled` was off for healthy runs and on for normal runs. This is automated simulation evidence, not interactive Play Mode validation.

| Healthy 600 s seed | Mean / peak | Zero / one time | ≥5 time / longest stretch | Benign arrivals | LOAD contention | Health mean / min / end |
|---|---:|---:|---:|---:|---:|---:|
| 1701 | 2.59 / 4 | 0.7% / 9.9% | 0% / 0 s | 20 | 0% | 100 / 100 / 100 |
| 456 | 2.63 / 4 | 0.8% / 5.4% | 0% / 0 s | 21 | 0% | 100 / 100 / 100 |
| 90210 | 2.53 / 4 | 1.4% / 11.7% | 0% / 0 s | 21 | 0% | 100 / 100 / 100 |
| 42 | 2.64 / 5 | 2.2% / 8.1% | 4.1% / 24.8 s | 25 | 0% | 100 / 100 / 100 |

The healthy mean stays reasonably close to the goal of 3 while allowing quiet moments and, in seed 42, sustained crowding. Only 20–25 benign arrivals occur over 600 seconds, compared with more than 50 ordinary-plus-supplemental benign plans in the earlier additive model. The longer-lived connections therefore turn over much less often.

| Normal 600 s seed | Mean / peak | Zero / one time | ≥5 time / longest stretch | Benign / hostile / Spawner plans | Benign cancellations | LOAD contention | Health mean / min / end |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1701 | 2.20 / 4 | 7.9% / 28.6% | 0% / 0 s | 17 / 11 / 48 | 4 | 45.1% | 56.39 / 0 / 0 |
| 456 | 2.86 / 5 | 0.8% / 3.6% | 0.8% / 4.6 s | 13 / 15 / 0 | 0 | 77.8% | 45.05 / 0 / 0 |
| 90210 | 0.16 / 2 | 90.5% / 3.5% | 0% / 0 s | 17 / 12 / 0 | 16 | 0% | 98.67 / 90.35 / 100 |
| 42 | 0.77 / 4 | 68.5% / 5.2% | 0% / 0 s | 22 / 16 / 0 | 16 | 2.9% | 97.47 / 82.59 / 100 |

Blackout prevents establishment in the unattended 90210 and 42 runs; there was no player cleanup. Loader contention and Spawner children produce severe later Health outcomes in 1701 and 456. These outcomes reflect existing threat and Health mechanics, not a direct Health cost from infrastructure or the benign controller. The 54-second hostile mean yielded 11–16 arrivals over 600 seconds, roughly comparable in scale with the former 10–13 hostile plans, without preserving their exact sequence. Sampled next-arrival intervals ranged **2.43–164.37 s** (1701), **0.98–100.67 s** (456), **0.32–171.79 s** (90210), and **2.50–196.78 s** (42): close arrivals and long gaps both occur.

The next table shows **mean / peak established benign occupancy** in each opening window. Every healthy 0–30-second peak is at most 3; no run immediately fills to the steady-state goal. Hostiles can change later windows through LOAD and Blackout.

| Seed / mode | 0–30 s | 30–60 s | 60–90 s | 90–180 s |
|---|---:|---:|---:|---:|
| 1701 healthy | 1.63 / 2 | 1.56 / 2 | 1.75 / 3 | 2.60 / 4 |
| 1701 normal | 1.63 / 2 | 1.56 / 2 | 1.75 / 3 | 2.60 / 4 |
| 456 healthy | 1.55 / 3 | 1.67 / 2 | 2.12 / 3 | 2.85 / 3 |
| 456 normal | 1.55 / 3 | 1.67 / 2 | 2.12 / 3 | 2.75 / 3 |
| 90210 healthy | 0.90 / 2 | 2.00 / 2 | 2.23 / 3 | 2.49 / 3 |
| 90210 normal | 0.90 / 2 | 2.00 / 2 | 0.21 / 2 | 0.00 / 0 |
| 42 healthy | 0.83 / 1 | 1.83 / 3 | 2.95 / 3 | 2.91 / 4 |
| 42 normal | 0.83 / 1 | 1.83 / 3 | 2.95 / 3 | 2.91 / 4 |

Traffic and Recorder Witnesses verify seeded replay, stream and recorder attribution, schedule independence, hostile-disable behavior, complete concrete plans, canonical PURPOSE weighting, and Blackout cancellation/recovery without a catch-up burst. Resource, Network Health, Lifecycle, and Infection Witnesses cover the shared lifecycle, allocation, Health, infection delivery, and Spawner paths. Interactive 16:9 Play Mode review still needs to check arrival rhythm, path-label crowding, inspect retention, terminal focus, and cleaning during a live Blackout.

## Earlier additive-controller evidence (superseded)

### Canonical WORK tuning before the scheduler restructure

After the population-ramp comparison below, all six built-in canonical PURPOSE profiles doubled their normal-operation mean WORK. Requested LOAD, purpose weights, ±25% WORK spread, and the coverage controller stayed unchanged. A fixed-seed rerun used the same 0.2-second samples. The comparison here is **ramped PR #40 before WORK tuning → after WORK tuning**.

| Seed 1701, normal generation | Mean before → after | Peak before → after | Time at ≥5 before → after |
|---|---:|---:|---:|
| 0–30 s | 0.72 → 0.85 | 1 → 1 | 0% → 0% |
| 30–60 s | 2.11 → 2.55 | 3 → 3 | 0% → 0% |
| 60–90 s | 1.03 → 2.78 | 2 → 3 | 0% → 0% |
| 90–180 s | 0.12 → 1.04 | 2 → 5 | 0% → 2.4% |

The other normal-run 0–30-second peaks stayed **2, 1, and 3** for seeds 456, 90210, and 42. At 30–60 seconds, their peaks changed **3→5, 2→2, and 3→3**. Ordinary hostile plan counts through 600 seconds remained **13, 10, 13, and 10** across the four seeds. Later outcomes diverge because each generated connection lives longer.

In the seed-1701 **healthy 600-second run** with hostile generation disabled, mean established benign occupancy rose **3.15→5.10**, peak **6→8**, time at ≥5 **10.5%→70.8%**, and the longest uninterrupted ≥5 stretch **27.2→272.6 seconds**. In its 90–180-second window, mean was **3.01→4.30** and time at ≥5 was **15.1%→39.6%**. The controller continues to produce a low opening without a 0–30-second surge, but it does **not** keep sustained healthy occupancy near its goal of 3 with these longer lifetimes. This is a final review caveat; no compensating population or WORK-rate change is included.

Unity 6000.3.11f1 compiled the amended source. Traffic, Recorder, Resource, Network Health, Lifecycle, and Infection Witnesses passed. The image below was captured for the earlier ramp amendment, before this WORK tuning; it is retained as ramp review evidence, not a visual validation of the longer lifetimes.

![Seed 1701 at about 30 simulation seconds](play-mode-seed1701-30s-1458x820.png)

This 1458×820 (approximately 16:9) frame was captured in rendered Unity 6000.3.11f1 **Editor Play Mode** from an isolated project copy. Randomize Seed was off, Seed was 1701, and an Editor script advanced the host by 300 fixed 0.1-second steps after Play initialization. It reported two runtime connections at 30.14 seconds. The network heading's “NO ACTIVE CONNECTIONS” is static authored copy. This automated capture checks rendering, not interactive terminal input, inspection, focus, or player action. Those still need manual 16:9 Editor review.

## Deterministic population comparison

The pre-amendment PR #40 branch and amended branch used the same scene, seeds, 0.2-second simulation steps, and 600-second horizon. Counts below are **established benign** connections after each step; Connecting and queued attempts are excluded from occupancy but inform the controller. No player commands were issued. Normal runs retain hostile generation, so later Blackout can prevent establishment.

| Seed 1701, normal generation | Mean before → after | Peak before → after | Time at ≥5 before → after |
|---|---:|---:|---:|
| 0–30 s | 3.25 → 0.72 | 5 → 1 | 37.3% → 0% |
| 30–60 s | 4.85 → 2.11 | 7 → 3 | 56.7% → 0% |
| 60–90 s | 1.69 → 1.03 | 3 → 2 | 0% → 0% |
| 90–180 s | 0.41 → 0.12 | 5 → 2 | 2.0% → 0% |

Other normal-run opening peaks (0–30 / 30–60 / 60–90 / 90–180 seconds) changed from **4/4/2/2 to 2/3/1/3** for seed 456, **3/4/4/4 to 1/2/2/1** for seed 90210, and **6/6/4/0 to 3/3/3/0** for seed 42. The ordinary ambient hostile plan counts over 600 seconds remained **13, 10, 13, 10** respectively before and after. The extra coverage plans changed network outcomes, so identical ordinary draws do not imply identical later Health or infections.

For a healthy-network 600-second test, seed 1701 used `hostileChance = 0` while retaining ordinary benign arrivals. Mean occupancy changed **3.63 → 3.15**, peak **7 → 6**, time at ≥4 **49.1% → 41.0%**, and time at ≥5 **22.3% → 10.5%**. The longest uninterrupted ≥5 stretch was **27.4 → 27.2 seconds**. This still permits natural crowding; the controller is not a hard cap. During 90–180 seconds in that healthy run, time at ≥5 fell **37.1% → 15.1%** and the peak fell **7 → 6**. Zero-benign time over 600 seconds was 0.7% in both runs.

## Recovery and validation

The Traffic Witness now checks the 1→3 ramp, skipped opportunities at high population, imminent versus far-future queued plans, unchanged ordinary seeded plans, canonical purpose weighting, deterministic replay, and the Blackout recovery path. A deliberately canceled benign attempt produces bounded retry timing during Blackout; cleaning the node clears cancellation pressure and advances the next coverage opportunity to about one simulated second. The Recorder Witness checks the saved configuration and the opportunity's effective goal, roll, chance, and linked complete queued plan. Traffic, Recorder, Resource, Network Health, and Lifecycle Witnesses passed in the isolated Unity project; compilation passed. The healthy 600-second measurement is an automated Edit Mode simulation, not interactive Play Mode validation. Manual review should confirm arrival rhythm, path labels, terminal focus, and cleaning during a live Blackout at 16:9.
