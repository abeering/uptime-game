# Connections Phase 4: objective-aware INFECTOR generation

This Connections-only change builds on merged Phase 3. The independent hostile clock, benign coverage, canonical PURPOSE catalog, LOADER implementation, and infection lifecycle remain in place. A DELIVERY/INFECTOR now selects its typed payload first, chooses a compatible weighted PURPOSE, samples the complete normal arc, and composes a STANDARD or DELAYED carrier. DELAYED preserves the entire normal arc and appends exactly one delivery operation.

The provisional Inspector settings choose DELAYED for 33% of anomalous carriers. The existing 10% subtle choice stays STANDARD with unchanged normal operation kinds, LOAD, and WORK. Anomalous primary signatures have relative weights 65/10/15/10 for high LOAD, low LOAD, operation deviation, and long WORK-only deviation. Compatible secondary signatures use the existing 15% roll. High LOAD receives proportional WORK so it cannot accidentally shorten the nominal operation duration. Final WORK correction targets 25 seconds from establishment to delivery and 18 seconds in a delivery operation that first reveals the deviation. Naturally short subtle baselines remain unchanged; they are not given a hidden numerical deviation. These values are playtest controls, not final balance rules.

The internal `plan_queued` record carries original and final operations, payload, structure, ordered signatures, nominal timing before and after composition, correction WORK and reason, and final delivery index. `hasInfector` distinguishes a real decision from Unity's empty default nested JSON object. The player sees the same PURPOSE, operation timeline, LOAD, WORK, activity, and ETA information as before. Benign outliers retain their recipe and can still share a three-operation purpose arc with an INFECTOR.

## Unity validation

Unity 6000.3.11f1 compiled an isolated copy of the authored Connections scene. The focused INFECTOR Witness passed STANDARD and DELAYED on single- and multi-operation PURPOSES, all four signatures, subtle carriers, compatible secondary signatures, high-LOAD timing, long WORK-only timing, full DELAYED arc preservation, kill and Blackout interruption, one-shot infection installation, manual parameter delivery, Spawner-child provenance, seeded replay, and saved JSON. A constructed benign outlier and malicious carrier shared Checkout Order Submission and the same visible three-operation kind sequence.

The witness sampled 2,400 seeded INFECTOR plans: 246 subtle (10.3%); among 2,154 anomalous carriers, 701 DELAYED (32.5%) and 340 with a second signature (15.8%). Primary signatures were 1,386 high LOAD (64.3%), 209 low LOAD (9.7%), 328 operation deviation (15.2%), and 231 WORK-only long duration (10.7%).

Traffic, Recorder, Resource, Behavior, Network Health, Lifecycle, Loader, and Infection Witnesses passed in batch Editor mode. Project Core was regenerated and checked. These are mechanical and recorder checks, not interactive Play Mode validation.

## Ordinary generated runs

Six seeds used the scene's ordinary benign population and hostile clock for 600 simulated seconds at 0.2-second steps, with no player actions. A `delivered` count means an infection was actually installed; duplicate rejection is counted separately. `Open` means the INFECTOR attempt had not resolved by 600 seconds. Contention is the fraction of samples with requested node LOAD above capacity.

| Seed | Hostile opportunities | INFECTOR: STANDARD / DELAYED | Subtle; H / L / O / W primary | Payload: Phantom / Blackout / Spawner | Installed / duplicate / canceled / open |
|---:|---:|---:|---|---|---:|
| 1701 | 11 | 5 / 3 | 2; 2 / 2 / 1 / 1 | 3 / 1 / 4 | 2 / 0 / 6 / 0 |
| 456 | 15 | 6 / 1 | 1; 5 / 1 / 0 / 0 | 3 / 2 / 2 | 3 / 1 / 2 / 1 |
| 90210 | 12 | 2 / 2 | 1; 3 / 0 / 0 / 0 | 1 / 1 / 2 | 2 / 0 / 0 / 2 |
| 42 | 16 | 6 / 2 | 1; 5 / 0 / 2 / 0 | 4 / 2 / 2 | 4 / 1 / 3 / 0 |
| 1776 | 9 | 3 / 1 | 0; 2 / 0 / 0 / 2 | 2 / 1 / 1 | 2 / 0 / 0 / 2 |
| 31415 | 12 | 5 / 0 | 0; 3 / 1 / 0 / 1 | 4 / 1 / 0 | 2 / 0 / 3 / 0 |

| Seed | Nominal delivery seconds: median (range), all INFECTOR plans | Actual seconds: median (range), carriers whose effect fired | Mean / peak benign | Contention | Health minimum / end |
|---:|---|---|---:|---:|---:|
| 1701 | 95.3 (32.0–512.4) | 95.7 (73.2–118.2) | 1.68 / 4 | 57.1% | 0.0 / 0.0 |
| 456 | 67.1 (57.1–116.0) | 70.6 (58.8–100.0) | 1.50 / 4 | 39.9% | 90.5 / 98.4 |
| 90210 | 99.0 (65.9–163.8) | 102.8 (88.2–117.4) | 2.59 / 4 | 39.7% | 81.9 / 81.9 |
| 42 | 81.7 (44.4–146.3) | 172.6 (49.2–363.8) | 2.79 / 5 | 63.0% | 0.0 / 21.1 |
| 1776 | 169.2 (74.9–376.9) | 94.0 (76.0–112.0) | 3.17 / 5 | 54.1% | 0.0 / 0.0 |
| 31415 | 112.3 (91.5–213.3) | 163.4 (124.0–202.8) | 1.61 / 4 | 38.8% | 25.6 / 70.4 |

Across 75 hostile opportunities, 36 ambient INFECTORs and 39 LOADERs were queued. Seventeen carrier effects fired: 15 installations and two duplicate rejections. Fourteen INFECTORs were Blackout-canceled before delivery and five were still open at 600 seconds. Across all 36 plans, nominal delivery time had median 89.1 seconds and 10th/90th percentiles 48.8/243.2 seconds (range 32.0–512.4). Among the 17 effects that actually fired, nominal median was 86.9 seconds and actual median was 100.0 seconds, with actual 10th/90th percentiles 58.8/202.8 seconds (range 49.2–363.8). These are different cohorts; the all-plan nominal mean should not be compared directly with delivered-only actual mean.

The first four seeds retained exactly the Phase 3 hostile opportunity counts (11, 15, 12, 16). The LOADER recipe and serialized tuning were untouched. Exact LOADER plan counts shifted because INFECTOR composition consumes the shared hostile-generation random stream; the hostile scheduling stream is separate. Seeds 1701, 42, and 1776 reached zero Health during infection-heavy unattended runs. That is a playtest caveat, not a reason to tune infection counts toward a target in this phase.

## Manual Play Mode review

Open `Assets/Scenes/Connections.unity` at 16:9. Disable **Randomize Seed** to replay a chosen seed. Inspect ordinary and anomalous Checkout Order Submission connections, including three-operation benign and hostile arcs. Compare high LOAD with retained delivery time, low LOAD and long WORK-only carriers, and a DELAYED carrier's unchanged first stages. Verify terminal input focus, `inspect` / `inspect -v`, node LOAD, Health, and infection installation or interruption. Use `mark` and the internal JSON run log to compare visible evidence with hidden `plan_queued` decisions and actual effect and infection events. Interactive Play Mode was not performed by the batch witnesses.
