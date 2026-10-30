# Conditions & Port Scan V0 review

This is Connections implementation work stacked on Node Status Presentation V0. Packet remains unchanged.

`SimulationHost` owns active typed Conditions and their simulation-time expiration. Reapplying `PortScanned` to one node refreshes its expiry and source. Connection completion applies the typed payload only after productive READ WORK finishes. Killing the carrier before completion prevents delivery. The condition persists after the carrier resolves and does not enter the infection list; cleaning infections leaves it in place.

`HostileActivityDirector` selects ordinary hostile traffic or Reconnaissance at the existing hostile opportunity. It uses a separate seeded random stream for activity and follow-up decisions; the benign stream remains separate. A Reconnaissance event queues a normal PORT SCAN connection and optionally a target-matched INFECTOR after the scan resolves plus its configured delay. Killing the scan is recorded but does not cancel a preselected follow-up. `portscan <nodeId>` and `recon <nodeId>` are development-only explicit entry points. The same event code handles ambient and explicit Reconnaissance.

Advanced starts with a 38-second hostile opportunity mean, ordinary/reconnaissance weights 2/1, a 40% follow-up chance, and an 8-second follow-up delay. Four-node hostile timing and mix remain at 54 seconds and ordinary only. These are provisional playtest settings. PORT SCAN requests 8 LOAD and 56 WORK, nominally 7 seconds after establishment at full allocation. `PortScanned` lasts 20 simulation seconds. The activity component exposes weights and follow-up timing; the traffic director exposes hostile mean for a future level controller.

The run log records the complete condition payload in `plan_queued`, event ID on queued plans, activity weights/selection, reconnaissance target and follow-up decision, scan outcome, follow-up timing and outcome, condition apply/refresh/expire with source, and active conditions in snapshots. Ordinary inspect does not reveal hostile intent.

Reconnaissance keeps the scan's typed resolution while a selected follow-up is pending, allowing future event behavior to distinguish completion, player kill, and Blackout cancellation. No-follow-up events retire when the scan resolves. Selected events retire when the follow-up resolves or cannot be generated; reset clears pending events. This changes event ownership only: a killed scan still launches any preselected follow-up on the existing delay.

## Manual review

1. In each Connections scene at 16:9 Play Mode, enter `portscan db`. Inspect the connection while connecting/active, watch READ progress and the terminal socket, then watch DB's purple `PORT SCANNED` panel and radial timer for about 20 simulated seconds.
2. Enter `portscan db` again, then `kill <id>` before its READ finishes. Confirm no new condition is applied. Repeating after an existing condition should only refresh when the later scan completes.
3. In Advanced, enter `recon jobs` and watch its scan. Some seeded events plan a later INFECTOR on JOBS; killing the scan does not cancel a selected follow-up. Use the run log to see the decision and event ID.
4. Combine a PORT SCANNED node with Blackout or a known infection, then clean the infection. Check the separate pain, Awareness, and transient CLEAN families and stack placement under crowded paths.

Interactive questions: Is 8 LOAD / 56 WORK readable as a short reconnaissance attempt under normal Advanced contention? Does the 2:1 ordinary/recon mix improve pressure without making scanner arrivals too common? Is the 8-second post-resolution follow-up delay legible in play?

## 16:9 Play Mode captures

These are **scripted Play Mode captures** at 1920×1080 from the GPU-backed Unity Editor validation project, using seed 1701. The capture script drove the simulation explicitly, so the terminal clock remains at its startup display; the images do not represent a human interactive playtest.

| Scene | During PORT SCAN | Condition active |
| --- | --- | --- |
| Four nodes | [READ connection](condition-four-scan-play.png) | [DB PORT SCANNED](condition-four-active-play.png) |
| Advanced, busy traffic | [Mixed traffic and Port Scan](condition-advanced-scan-play.png) | [JOBS PORT SCANNED](condition-advanced-active-play.png) |

[Four-node OFFLINE, known INFECTED and PORT SCANNED stacked](condition-four-stacked-play.png) shows concurrent status families.

## Validation run

Unity 6000.3.11f1 compiled the isolated validation project. The final batch passed Condition, Node Status, Scan, Infection, Traffic, Recorder, Network Health, Resource, Behavior, and Advanced Scene witnesses. The expanded Condition witness passed again after adding the condition expiry, CLEAN coexistence, and seeded follow-up replay assertions. `git diff --check` passed.

The Advanced Scene witness ran three deterministic 600-second simulations:

| Seed | Benign plans | Ordinary hostile plans | Recon scans | Infections applied |
| --- | ---: | ---: | ---: | ---: |
| 1701 | 25 | 9 | 4 | 5 |
| 3141 | 22 | 1 | 5 | 1 |
| 9001 | 20 | 18 | 6 | 6 |

These are seeded examples of the provisional settings, not a balance claim. Interactive Play Mode review remains open.

The PR #62 lifecycle follow-up added `ReconEventLifecycleWitness`. In Unity 6000.3.11f1 it passed scan and follow-up `Completed`, `Killed`, and Blackout `Canceled` outcomes, failed follow-up generation, retained typed scan resolution, recorder linkage, and reset. The full relevant Connections witness batch also passed after this cleanup.
