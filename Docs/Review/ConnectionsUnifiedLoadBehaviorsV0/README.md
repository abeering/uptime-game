# Connections Unified LOAD Behaviors V0 — review evidence

This is a **Connections implementation** refactor of existing LOAD behavior. Natural noise, OVERLOAD, and SPIKING now use one keyed dynamic contribution store. Ordinary timed adjustments remain fixed modifiers. Each keyword's active modifier remains the source of its ID, scope, operation index, activation/removal history, configured LOAD magnitude, and recorder events; its resource contribution is read from the keyed store exactly once. Connection-scope entries survive operation transitions; operation-scope entries and natural noise clear at operation end. The existing host step ordering, allocator, LOADER WORK pause/withdrawal, and inspection formatting are unchanged.

## Unity validation

Unity 6000.3.11f1 compiled an isolated project copy. Natural LOAD Noise, Resource, Behavior, Traffic, Recorder, Network Health, Lifecycle, Infection, LOADER, INFECTOR, and Inspect V2 witnesses passed in batch Edit Mode. The Behavior witness checks zero-delay and delayed OVERLOAD, SPIKING phase timing and distinct burst IDs, large-step threshold reconciliation, zero-allocation timers, connection-scope phase continuity through operation transitions, operation-scope cleanup, and ordered recorder events. The extended Natural LOAD Noise witness checks simultaneous fixed adjustment, operation OVERLOAD, connection SPIKING, and seeded noise; it uses the same behavior ID in both scopes and verifies additive requested LOAD without double-counting or recorder noise events. LOADER checks still cover 0× productive WORK during attack and quiet phases, activation, and withdrawal. The authored Resource witness retains exact LOAD expectations.

## Six-seed comparison with merged PR #46

The same normal generated runs used seeds **1701, 456, 90210, 42, 1776, 31415**, 3,000 simulation steps of 0.2 seconds each, hostile generation enabled, and no player commands. The merged PR #46 baseline and this refactor produced **identical CSV rows** for every seed: benign/hostile plan counts, benign completion/cancellation counts, established benign mean/peak, completed benign lifetime mean/p90, LOAD contention fraction, Health mean/min/end, LOADER plans/withdrawals, INFECTOR plans, and applied infections. These runs are automated Edit Mode simulations, not interactive Play Mode validation.

| Seed | Benign completed lifetime mean, before = after | Contention fraction, before = after | Health mean / min / end, before = after |
|---|---:|---:|---:|
| 1701 | 76.6 s | 56.2% | 94.7 / 60.1 / 60.1 |
| 456 | 69.6 s | 38.7% | 98.8 / 90.5 / 97.2 |
| 90210 | 79.2 s | 41.8% | 97.9 / 77.3 / 77.3 |
| 42 | 102.5 s | 63.5% | 42.3 / 0.0 / 19.9 |
| 1776 | 78.7 s | 54.2% | 70.8 / 0.0 / 0.0 |
| 31415 | 69.5 s | 39.7% | 72.9 / 19.6 / 64.4 |

The means across seeds therefore remain **79.4 s** benign lifetime, **49.0%** contention, and **79.6** mean Health. The baseline and changed runs also retained the same per-seed benign occupancy and threat outcome counts. The comparison is evidence for the tested seeds and step size; it does not prove bit-for-bit identity for every possible plan or frame rate.

## Manual 16:9 Play Mode review

1. Open `Assets/Scenes/Connections.unity` in a 16:9 Game view. To replay a known run, disable **Randomize Seed** on the Connections Traffic Director and set Seed to 1701. Enter Play Mode.
2. Inspect an ordinary generated connection with `inspect <id>` and `inspect -v <id>`. Watch its one-decimal LOAD change smoothly over several seconds while its node LOAD, allocation, and WORK respond to the same demand.
3. Run `spawn overload-demo`, inspect its ID, and verify the expected delayed LOAD increase. Run `spawn spiking-benign` (or `spawn spiking-threat`) and inspect its ID through ordinary and elevated phases. Keep another ordinary connection visible to compare its independent natural movement.
4. Inspect multiple IDs together in compact and verbose layouts, then kill a live keyword connection. Confirm its LOAD leaves the target, the prompt keeps focus, and the card/path cleanup remains legible. For a generated LOADER, verify WORK stays paused through a SPIKING quiet interval and the attack withdraws on schedule.

The batch witnesses verify model behavior and inspection bindings. **Interactive 16:9 Play Mode readability, animation, and command focus remain for manual review.**
