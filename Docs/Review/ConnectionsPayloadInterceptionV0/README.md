# Connections Payload & Interception V0 — review

This branch adds a separate moving Payload entity and targeted interception to Connections. It does not add ambient Payloads, Worms, isolation, or Packet changes. [Implemented behavior and manual steps](../../Connections/connections-payload-interception-v0.md).

## 16:9 scripted Play Mode captures

- [Four-node scene: two moving Payloads and an active interceptor](payload-four-moving-play.png)
- [Four-node scene: successful catch at DB](payload-four-intercepted-play.png)
- [Advanced scene: two moving Payloads amid ordinary traffic](payload-advanced-moving-play.png)
- [Advanced scene: active window expired](payload-advanced-expired-play.png)

The captures were rendered at 1920×1080 by a GPU-backed Unity Editor Play Mode script from an isolated project copy. The script selected a fixed seed, directly spawned test Payloads, advanced the authoritative host, and rendered the Game camera. They are **Editor-driven Play Mode captures**, not a human keyboard playtest. The four-node example disables ambient traffic to isolate the new motion; the Advanced moving example includes seeded ordinary traffic.

The follow-up images show Payloads occupying the same packed lanes and node ports as ordinary connections. Each amber moving circle carries its `xN` ID; there is no separate Payload identity pill. The capture script explicitly refreshed the production layout before rendering each frame because its batch-mode timing can skip the usual `LateUpdate` cadence. The Advanced moving image is the useful crowded comparison. Moving-body overlap with an ordinary pill remains an interactive visual check.

Both Play Mode capture runs reached `PAYLOAD PLAY CAPTURE PASS`. Unity's SearchDatabase emitted a nonfatal startup `ArgumentOutOfRangeException` in the isolated Editor project; it did not stop either capture.

## Validation

Unity 6000.3.11f1 compiled the isolated project. The final batch passed `PayloadWitness`, `GeometryWitness`, `ConditionWitness`, `ReconEventLifecycleWitness`, `NodeStatusWitness`, `ScanWitness`, `InfectionWitness`, `TrafficWitness`, `RecorderWitness`, `NetworkHealthWitness`, `ResourceWitness`, `BehaviorWitness`, `LifecycleWitness`, and `AdvancedSceneWitness`. The Payload witness covers deterministic route/timing replay; one visible hop; establishment before traversal; intermediate pass-through; matching, wrong-node, wrong-ID, expired, and simultaneous interception; cooldown; infection delivery and prevention; zero-allocation protection; legitimate WORK/Health contention; combined CLEAN/interceptor status; recorder action linkage and snapshots; reset; both scene bindings; authored circular ID; and a Payload sharing a packed link with an ordinary connection. `scripts/build_uptime_project_core.py --check` and `git diff --check` passed.

The GPU capture Editor logged a Unity SearchDatabase startup indexing `ArgumentOutOfRangeException`; it did not interrupt the Play Mode script or captures. No project-script compile errors or witness failures were reported. Interactive keyboard timing, text legibility across window sizes, and sustained crowded Play Mode behavior still need human review.

## Suggested manual review

Open either Connections scene in Play Mode. Use development command `payload db variable`, then test `intercept x1 @ <nodeId>` against an early window, a wrong node, and a near-arrival final node. Check that wrong-route guesses are accepted without target disclosure, the one-hop line and moving circle stay readable beside normal traffic, the four-LOAD reservation appears immediately, and the successful catch leaves no new infection. Repeat with two Payloads approaching one node and watch the beneficial status alongside CLEAN and PORT SCANNED.
