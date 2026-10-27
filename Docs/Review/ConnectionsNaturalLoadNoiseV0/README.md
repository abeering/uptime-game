# Connections Natural LOAD Noise V0 — review evidence

This is a **Connections implementation** change on the four-node scene. It does not alter Packet, arrival scheduling, PURPOSE selection, threat recipes, the allocator, Health rules, or inspection layout.

## Behavior and tuning

Canonical generated PURPOSE plans, whether benign, INFECTOR, LOADER, or Spawner child, store an opt-in flag and the run seed. Each active operation hashes that seed with its connection ID, operation index, and neighboring time anchors. Smooth interpolation between independently mixed anchors gives a gently irregular contribution, starting at zero for each operation. The scene defaults to a maximum contribution of **10% of sampled base LOAD** and an operation-specific **3–6-second anchor interval**. These are provisional playtest values.

The host samples one contribution at the start of each allocation segment. `ConnectionBehaviorRuntime` combines sampled base LOAD, existing fixed modifier deltas, and keyed dynamic LOAD contributions, then clamps the request to nonnegative. All readers in that segment see the cached value. Operation end, completion, kill, cancellation, and LOADER withdrawal clear the contribution. The keyed contribution seam is available to a later phase; OVERLOAD and SPIKING still use their current fixed-modifier behavior. Noise consumes no traffic RNG draws and emits no modifier events.

Authored `c1/c2/c3`, legacy manual profiles, and explicitly queued witness plans stay exact unless opted in. The Simulation Host Inspector can disable natural noise globally. Run metadata stores the enable flag, amplitude, and period bounds; concrete `plan_queued` definitions store the opt-in and seed; snapshots store the actual requested LOAD and the natural contribution. The existing inspect pill still formats current requested LOAD to one decimal.

## Deterministic validation

Unity **6000.3.11f1** compiled an isolated project copy. Natural LOAD Noise, Resource, Behavior, Traffic, Recorder, Network Health, Lifecycle, Infection, LOADER, INFECTOR, and Inspect V2 witnesses passed in batch Edit Mode. The new witness checks two independent ID patterns, exact replay, smooth bounded changes, one-decimal visibility at 0.8 base LOAD, additive OVERLOAD behavior, node demand, allocation, WORK, recorder and inspect agreement, global disable, and cleanup at operation transition, completion, kill, and Blackout cancellation. The LOADER witness keeps its 0× WORK pause and timed withdrawal checks while comparing its fixed attack level after subtracting the natural contribution. The Infection witness checks that Spawner children carry the seed and opt-in in `plan_queued`.

Six ordinary generated runs were compared against merged Phase 4 `main` before this change, with the same scene, seeds, 0.2-second steps, 600-second horizon, and no player commands. Hostile generation remained enabled. Lifetime is mean established-to-completed duration of benign connections; contention is the fraction of samples where any node requested more than capacity. These are automated Edit Mode simulations, not interactive Play Mode validation.

| Seed | Benign lifetime, s before → after | Mean established benign before → after | Contention before → after | Mean Health before → after | End Health before → after |
|---|---:|---:|---:|---:|---:|
| 1701 | 83.1 → 76.6 | 1.68 → 1.55 | 57.1% → 56.2% | 76.7 → 94.7 | 0.0 → 60.1 |
| 456 | 69.2 → 69.6 | 1.50 → 1.50 | 39.9% → 38.7% | 98.9 → 98.8 | 98.4 → 97.2 |
| 90210 | 79.0 → 79.2 | 2.59 → 2.65 | 39.7% → 41.8% | 98.6 → 97.9 | 81.9 → 77.3 |
| 42 | 102.4 → 102.5 | 2.79 → 2.79 | 63.0% → 63.5% | 41.8 → 42.3 | 21.1 → 19.9 |
| 1776 | 78.2 → 78.7 | 3.17 → 3.18 | 54.1% → 54.2% | 70.8 → 70.8 | 0.0 → 0.0 |
| 31415 | 69.5 → 69.5 | 1.61 → 1.64 | 38.8% → 39.7% | 74.9 → 72.9 | 70.4 → 64.4 |

Across seeds, mean benign lifetime was **80.2 → 79.4 s**, mean established benign **2.223 → 2.218**, contention **48.8% → 49.0%**, and mean Health **77.0 → 79.6**. Each seed kept the same benign and hostile plan counts, benign completion/cancellation counts, LOADER withdrawal counts, and infection application counts. Seed 1701 had a large favorable end-Health difference despite those identical event counts; the timing-sensitive Health and contention system can amplify small LOAD changes. This is a review caveat, not evidence of a scheduling or threat-generation change.

## Manual 16:9 Play Mode review

1. Open `Assets/Scenes/Connections.unity`, set the Game view to 16:9, and check the Simulation Host has Natural Load Noise enabled at amplitude 0.1 and period bounds 3–6 seconds. For replay, uncheck Traffic Director **Randomize Seed** and set Seed to 1701.
2. Enter Play Mode. When an ordinary connection establishes, run `inspect <id>` and watch its LOAD pill for several seconds. Run `inspect -v <id>` for the same connection and confirm the one-decimal LOAD changes while WORK, target LOAD, and performance respond consistently.
3. Inspect multiple IDs together, including a low-LOAD purpose such as Product Detail Retrieval and a connection with an explicit LOAD behavior. Confirm independent movement and readable compact/verbose cards. Repeat at 16:9 during contention, then kill a live connection and verify its demand leaves the node.

The batch witnesses verify binding and one-decimal changes; **interactive Play Mode readability, input focus, and live animation have not been validated here**.
