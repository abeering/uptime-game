# Connections LOAD diagnostic V0 — review evidence

This is a Connections terminal presentation change. `load <nodeId>` immediately opens a live terminal-history panel with the node's published requested LOAD, capacity, each demanding connection's effective requested LOAD, and an aggregated `OTHER` remainder. It does not alter allocation or traffic generation.

## Deterministic 16:9 Play Mode captures

These 1920×1080 images were generated in Unity 6000.3.11f1 Play Mode from `ConnectionsAdvanced.unity` with automatic traffic disabled and a scripted four-connection DB workload. The capture script stepped the simulation, submitted ordinary terminal commands, refreshed the UI, and snapped the bar animation to its current target for each still image. They show presentation and integration; interactive animation, keyboard focus, and scrolling still need a human Play Mode review.

| Image | Observed published requests |
| --- | --- |
| [DB, low SPIKING phase](load-db-low-play.png) | DB 21.0 / 10.0; `a3` 8.0, `h7` 5.0, `s9` 4.0, `b2` 3.0, `OTHER` 1.0 |
| [Same DB panel, high SPIKING phase](load-db-high-play.png) | DB 35.0 / 10.0; `s9` increases to 18.0 and its bar turns red while the other rows stay in place |
| [Several live panels in terminal history](load-multiple-panels-play.png) | Two independent DB panels and an API panel; API shows its 1.0 non-connection baseline request |

The screenshots also show several simultaneous logical paths at DB. The terminal remains one-third of the 16:9 workspace; the node panel is scrollable history, not a floating overlay. The row-bar scale begins at 1.5× node capacity and grows only when an individual request exceeds it. It stays at the larger scale through the next low phase, so the SPIKING bar visibly contracts.

## Unity validation actually run

An isolated project copy under ignored `Temp/AdvancedSceneValidation` compiled the branch and ran `LoadRegressionBatch.Run` in the Unity Editor. The batch passed `WormWitness`, `PayloadWitness`, `GeometryWitness`, `ConditionWitness`, `ReconEventLifecycleWitness`, `NodeStatusWitness`, `ScanWitness`, `InfectionWitness`, `TrafficWitness`, `RecorderWitness`, `NetworkHealthWitness`, `ResourceWitness`, `BehaviorWitness`, `LifecycleWitness`, `AdvancedSceneWitness`, `InspectV2Witness`, and the new `LoadOutputWitness`.

The new witness checks both scene bindings and authored pill material; valid, case-insensitive, missing, extra, and unknown node commands; empty demand; baseline `OTHER`; multiple panels; several same-node rows; requested 18.0 versus allocated below 18.0 during contention; SPIKING and WAVING effective requests; stable row order and scale; new arrival, operation transition, kill, reset, and narrow width; and covert infection demand reconciled into `OTHER` without an `INFECTED` label. Destroyed panels are followed by another reset to check that no stale callback remains.

The scripted Play Mode capture completed. Its batch Editor log emitted one nonfatal Unity Search indexing `ArgumentOutOfRangeException` during startup, then produced all three images and reported `LOAD PLAY CAPTURE PASS`. No project script compile error or witness failure remained.

## Interactive review

Open `ConnectionsAdvanced.unity` in the Editor and enter Play Mode. Use `load db` while traffic is active, then issue `load api` or another `load db`. Verify that bars update without another command, especially for SPIKING/WAVING requests; rows arrive and disappear; the terminal keeps command focus; and scrolling remains steady as panel heights change. Repeat `load db` in `Connections.unity`. The captures above do not prove those real-time input and animation interactions.
