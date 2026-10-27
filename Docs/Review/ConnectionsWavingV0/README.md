# Connections WAVING V0 — review evidence

This is a **Connections implementation** experiment. WAVING is a manually spawnable, benign LOAD behavior; it is absent from ambient PURPOSE, INFECTOR, and LOADER generation. The authored `Connections.unity` scene serializes `profiles: []`, so `ConnectionTrafficDirector.DefaultProfiles()` is its actual runtime catalog. Both new profiles have zero ambient weight, fixed 2 LOAD, no completion effect, and enough WORK to observe multiple cycles: `waving-moderate` uses −1/+1 offsets, a 20-second period, and a 3-second onset; `waving-strong` uses 0/+8 offsets, a 30-second period, and the same onset. Their durations are playtest examples.

## Runtime behavior

`Waving` is appended to the serialized behavior-kind enum, preserving older numeric values. A waveform instance owns its elapsed simulation time and one scoped modifier identity. At onset it records one activation; it then updates that modifier's keyed dynamic LOAD contribution before allocation, with no per-sample events. Its offset is a phase-adjusted sinusoid around the midpoint of its configured bounds. The phase chooses zero offset at onset, and **period means the whole repeating cycle**. Bounds must be finite, include zero, and have a finite positive period. The final request clamps to zero if a negative offset exceeds the base request.

The same authoritative requested LOAD drives the proportional allocator, actual WORK progress, node totals, recorder snapshots, and existing one-decimal inspection. WAVING does not change the WORK-rate multiplier. Operation-scoped instances end with their operation; connection-scoped instances keep their phase across operation transitions. A modifier removal is recorded at scope end, kill, completion, or cancellation. The behavior definition, concrete queued plan, and run configuration retain minimum/maximum offsets, onset, and period in recorder JSON.

## Deterministic validation

Unity 6000.3.11f1 compiled an isolated project copy. The WAVING witness passed exact onset, intermediate, extremum, half-cycle, full-cycle, and repeat samples; quarter-second smoothness; identical-time samples with 0.1- and 0.25-second simulation steps; operation and connection scope behavior; natural-noise/OVERLOAD/SPIKING/fixed-modifier composition; signed zero clamp; actual WORK; kill/completion/Blackout/reset cleanup; invalid parameters; scene-backed manual commands; and recorder JSON/provenance without event flooding.

Natural LOAD Noise, Resource, Behavior, Traffic, Recorder, Network Health, Lifecycle, Infection, LOADER, INFECTOR, and Inspect V2 witnesses also passed. The two existing catalog-length assertions were updated from 16 to 18 to include only the two new manual profiles.

Six ordinary generated runs were compared with merged PR #47 using seeds **1701, 456, 90210, 42, 1776, 31415**, hostile generation enabled, no player commands, and 3,000 steps of 0.2 simulated seconds each. Every before/after CSV row was identical for benign/hostile plans, completions/cancellations, benign lifetime mean/p90, occupancy mean/peak, contention, Health mean/min/end, LOADER plans/withdrawals, INFECTOR plans, and infections applied. The table shows selected values that were equal before and after.

| Seed | Benign lifetime mean | Contention fraction | Health mean / min / end |
|---|---:|---:|---:|
| 1701 | 76.6 s | 56.2% | 94.7 / 60.1 / 60.1 |
| 456 | 69.6 s | 38.7% | 98.8 / 90.5 / 97.2 |
| 90210 | 79.2 s | 41.8% | 97.9 / 77.3 / 77.3 |
| 42 | 102.5 s | 63.5% | 42.3 / 0.0 / 19.9 |
| 1776 | 78.7 s | 54.2% | 70.8 / 0.0 / 0.0 |
| 31415 | 69.5 s | 39.7% | 72.9 / 19.6 / 64.4 |

These are automated Edit Mode simulations. They show the manual-only catalog additions left the sampled generated runs unchanged; they do not substitute for interactive Play Mode review.

## Manual 16:9 Play Mode review

1. Open `Assets/Scenes/Connections.unity` at 16:9. For a replayable background, disable **Randomize Seed** on the Connections Traffic Director and set Seed to 1701. Enter Play Mode.
2. Run `spawn waving-moderate`, then `inspect <id>` and `inspect -v <id>`. After the 3-second onset, watch the existing LOAD pill move gradually between about **1.0 and 3.0**, with no jump at the cycle boundary.
3. Run `spawn waving-strong`, inspect it in compact and verbose modes, and observe its slower **2.0 to 10.0** curve. During its elevated portion, check that the DB requested LOAD, allocation, and WORK progress follow the displayed demand. Its productive WORK should continue.
4. Keep both cards visible to check multi-ID readability, terminal focus, path labels, and eventual cleanup. Kill one active wave and confirm its node demand is released.

Interactive 16:9 Play Mode readability, input focus, and animation were **not** validated by the batch witnesses.
