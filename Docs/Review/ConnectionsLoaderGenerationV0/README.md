# Connections Phase 3: objective-aware LOADER generation

This Connections-only review branch builds on the merged Phase 2 WORK-rate foundation. Generated hostile traffic still uses the independent seeded hostile arrival clock and the existing LOADER/DELIVERY objective probability. A LOADER selects a compatible weighted canonical PURPOSE after the objective roll, then independently chooses a single or DELAYED structure and an immediate, OVERLOAD, or SPIKING pattern. DELAYED keeps the full sampled normal arc before an appended attack operation. DELIVERY and benign coverage recipes are unchanged.

The attack operation samples elevated LOAD around 80–120% of target capacity, always above its sampled normal demand. OVERLOAD and SPIKING start after a sampled 5–15 simulated seconds in that operation; SPIKING repeats 5 seconds quiet and 2 seconds elevated. Attack activation pauses productive WORK at 0× while LOAD allocation and timers continue. An activated LOADER withdraws after a sampled 30–60 seconds unless killed or Blackout-canceled first. Withdrawal releases LOAD, has no direct Health penalty, is a distinct `Withdrawn` outcome with `AttackerWithdrawal` cause, and leaves the resolved connection inspectable. These values are provisional four-node playtest tuning.

## Validation

Unity 6000.3.11f1 compiled an isolated project copy. `LoaderWitness` passed all six structure/pattern combinations, capacity scaling at two node capacities, full DELAYED multi-operation baseline, onset and 0× WORK semantics, SPIKING quiet phases, real contention, kill release, timed withdrawal, Blackout interruption, no double resolution, and seeded replay. Behavior, Resource, Recorder, Traffic, Network Health, Lifecycle, and Infection Witnesses all passed in batch Editor mode. The authored Connections scene remains the source of serialized tuning.

Four ordinary generated runs used scene tuning, seeds 1701, 456, 90210, and 42, with 0.2-second simulation steps for 600 simulated seconds each. No player actions were taken. Contention is the fraction of samples with a node's requested LOAD above capacity. Active LOADER counts include established LOADERs before and after attack activation.

| Seed | Hostile plans | LOADER plans | Six variant counts (single immediate / overload / spiking; delayed immediate / overload / spiking) | Activated / withdrew / Blackout canceled | Peak active / mean active | Seconds with 2+ active | Contention | Health mean / minimum / end |
|---:|---:|---:|---|---:|---:|---:|---:|---:|
| 1701 | 11 | 4 | 0 / 1 / 2; 0 / 1 / 0 | 0 / 0 / 4 | 0 / 0.00 | 0 | 3.3% | 98.2 / 85.0 / 100.0 |
| 456 | 15 | 9 | 4 / 1 / 2; 1 / 1 / 0 | 3 / 2 / 7 | 2 / 0.24 | 23.0 | 15.9% | 97.5 / 86.8 / 97.9 |
| 90210 | 12 | 7 | 1 / 2 / 1; 1 / 0 / 2 | 6 / 6 / 1 | 3 / 0.85 | 124.4 | 23.1% | 99.5 / 90.0 / 100.0 |
| 42 | 16 | 5 | 1 / 0 / 0; 2 / 1 / 1 | 4 / 4 / 1 | 2 / 0.54 | 74.0 | 29.1% | 94.4 / 64.8 / 100.0 |

Across these runs, all six variants appeared: 25 LOADER plans, 13 actual activations, 12 timed withdrawals, and 13 Blackout cancellations. No LOADER remained active at 600 seconds. Seed 1701's four LOADER attempts were canceled by an ambient Blackout before they established; this is a lifecycle result, not a generator skip. Health varies because legitimate service degrades under contention and infection-driven cancellations; LOADER withdrawal itself has no direct Health cost. Four seeds are a sanity check, not a balance distribution estimate.

## Manual Play Mode review

Open `Assets/Scenes/Connections.unity` at 16:9. Set `ConnectionTrafficDirector.Randomize Seed` off to replay a chosen seed, then inspect normal and delayed LOADER arcs and the transition into elevated LOAD. Check the node LOAD bar, path retirement, `inspect` / `inspect -v`, terminal input focus, and retained `WITHDRAWN` history after exit. Compare the internal run JSON `plan_queued`, `loader_activated`, `loader_withdrawal`, and `connection_resolved` events. Batch Editor witnesses and simulated runs do not replace interactive Play Mode review.
