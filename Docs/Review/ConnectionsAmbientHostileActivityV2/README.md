# Connections Ambient Hostile Activity V2 — review evidence

This is a **Connections implementation** review for the Advanced Scenario. The four-node scene retains its ordinary hostile baseline. The Advanced scene uses one hostile opportunity clock with provisional 4:2:2:1 weights for ordinary hostile connections, Reconnaissance, Payload, and Worm. Mean opportunity interval is 30 simulated seconds. All results below came from Unity 6000.3.11f1 in an isolated copy of the project, on this branch.

## Four deterministic Advanced runs

Each run began from a clean scene with seeded auto generation, no developer spawn commands, no player intervention, and 600 one-second simulation steps. Counts are recorder events. `Ord.` is selected ordinary hostile plans; `Recon` is selected Reconnaissance events; `Payload` and `Worm` are successful entity spawns. `P del.` and `P rej.` are delivered and rejected Payload effects. `Branches` counts Worm branches created. `Inf.` and `Cond.` count applied infections and Conditions. `Benign canc.` counts benign connection cancellations. `Slowed s` counts sampled seconds with a slowed or overdue legitimate connection. Health columns show final and minimum sampled Health.

| Seed | Opportunities | Ord. | Recon | Payload | Worm | P del. / rej. | Branches | Inf. | Cond. | Benign canc. | Slowed s | Peak live connections | Health final / min. |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1701 | 22 | 13 | 5 | 2 | 2 | 1 / 1 | 9 | 5 | 10 | 14 | 383 | 14 | 94.48 / 90 |
| 1702 | 15 | 8 | 5 | 1 | 1 | 1 / 0 | 3 | 6 | 8 | 12 | 420 | 14 | 17.58 / 0 |
| 1703 | 20 | 11 | 4 | 4 | 1 | 2 / 2 | 7 | 6 | 9 | 5 | 526 | 13 | 0 / 0 |
| 1704 | 21 | 8 | 6 | 3 | 4 | 2 / 1 | 9 | 4 | 13 | 6 | 462 | 7 | 12.53 / 0 |

All selections succeeded in these four runs. Every selected Reconnaissance scan resolved (5, 5, 4, 6); follow-ups queued 1, 4, 2, 2 times. Selected ordinary hostile connections resolved 13, 8, 9, 8 times by the 600-second endpoint. Worm outbreaks completed 1, 1, 1, 4 times by that endpoint. Some connections or outbreaks remain in flight at 600 seconds. No interventions means these runs measure unopposed pressure. The two zero-Health runs and long slowed intervals call for human pacing review; the provisional values are not balance targets.

## Scripted Play Mode captures

These are rendered **Play Mode** frames at 1920×1080 from the Advanced scene, seed 1702. The capture driver issued no developer spawn commands; it advanced the simulation clock with `StepSimulation` and refreshed the presenters for each frame. This is visual and integration evidence, **not interactive player validation**.

| Simulated time | Frame | Evidence |
| ---: | --- | --- |
| 220 s | [Payload in traffic](ambient-v2-payload-play.png) | Ambient Payload alongside ordinary connections. |
| 250 s | [Worm arrival](ambient-v2-worm-arrival-play.png) | Worm begins while Payload and connection pressure overlap. |
| 260 s | [Mixed pressure](ambient-v2-mixed-pressure-play.png) | Worm, Payload, connections, infections, and node state compete for attention. |
| 280 s | [Worm propagation](ambient-v2-worm-propagation-play.png) | Later Worm propagation state on the same run. |

## Validation actually run

- `AmbientHostileActivityWitness`: all four choices, each zero weight, four-node defaults, valid Payload ingress/route/internal target and profile, Worm ingress/effect/tuning, unique identities, recorder provenance, deterministic replay, benign random-stream independence, unavailable targets, isolated ingress, connection-ID exhaustion, toggles and manual spawning, coarse versus repeated one-second stepping, and four 600-second runs.
- `WormWitness` and `ConditionsBatch`: Payload, geometry, Conditions, Reconnaissance lifecycle, node status, scan, infection, traffic, recorder, Network Health, resources, behaviors, lifecycle, and Advanced scene. See the CI/PR summary for pass status.

The rendered capture run completed successfully. Its log also contains a Unity Editor Search startup indexing exception (`UnityEditor.Search.SearchDatabase`), outside the game/capture stack; the four Play Mode frames were produced.

## Manual Play Mode review

Open `Assets/Scenes/ConnectionsAdvanced.unity` from disk at 16:9 and play without manual spawning. Watch one quiet and one busy interval. Inspect ordinary traffic while a Payload approaches, and decide whether to intercept it or pursue the connection. Observe a Worm entering and branching while another threat is active; try isolation and inspect the resulting service disruption. Confirm Reconnaissance and ordinary hostile generation continue. Check node-status placement, connection labels, `inspect` output, terminal input focus, and Health receipts. Repeat in `Connections.unity` to check the ordinary-only ambient baseline. Interactive pacing and prioritization remain for human review.
