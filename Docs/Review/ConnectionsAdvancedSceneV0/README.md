# Connections Advanced V0 scene review

This review concerns the **Connections implementation** in
`Assets/Scenes/ConnectionsAdvanced.unity`. The scene is an independently openable
eight-node workstation built from the existing playable Connections scene. It uses
the merged simulation, traffic director, terminal, inspection, scan, clean, Health,
path presentation, and run recorder. The original four-node scene and Packet scenes
are unchanged.

## Scene contract

The two ambient origins are EXT1 (60%) and EXT2 (40%). The seven physical links
are EXT1–WEB, WEB–API, WEB–AUTH, API–DB, DB–RPT, API–JOBS, and JOBS–EXT2.
The only PURPOSE destination overrides are inventory-sync → JOBS and
sales-export → RPT. The other four canonical destinations remain authored in the
existing profile catalog. The effective benign coverage goal ramps from 1 to 4
over 75 simulated seconds. Hostile arrivals retain the 54-second mean. Normal
Play Mode randomizes the seed; witnesses pin it for replay.

## Validation

`Tools/Connections/Run Advanced Scene Witness` opens the authored scene and
checks IDs, links, resource defaults, generated and recording defaults, weighted
origins, effective PURPOSE destinations, and the 1920×1080 topology geometry.
It runs three deterministic 600-second sessions with real ambient benign and
hostile generation, then checks Blackout cancellation and Spawner routing.

Unity 6000.3.11f1 compiled the isolated validation project. On merged `main`, the
Multi-Ingress, Traffic, Resource, Infection, Lifecycle, Recorder, Scan, and Geometry
witnesses all passed before scene authoring. The final scene witness passed with
zero card overlaps, viewport overflow, link crossings, links through other cards,
or links shorter than 120 px; the shortest visible physical link is **269 px**.
The scripted Play Mode run produced no full-size identity-pill fit warnings.

## Midpoint link-label pass

Connection pills now pack without treating the `E#` label as a fixed midpoint
obstacle. The label keeps its midpoint along the link and moves only perpendicular
to clear the occupied path lanes and full-size pills. When the link empties, it
returns to its authored offset. The label objects and their scene bindings remain
in place. This presentation rule applies to both Connections scenes; it does not
change topology, traffic, resources, or simulation.

The Geometry Witness passed on the four-node scene and checks that label centers
stay at link midpoints and clear every planned pill. The Advanced Scene Witness
passed with all seven labels still bound and visible. In a fixed-seed 1701 Play
Mode capture at 90 simulated seconds, the EXT1-WEB pills moved to the center
while E1 stayed at the midpoint beside them. A second capture at 335 seconds had
14 active connections; its busiest edges used the existing staggered and overlaid
pill stages while link labels remained outside the occupied bundles. No
full-size-pill fit warnings appeared in either capture.

| Seed | Ambient benign | Ambient hostile | EXT1 / EXT2 plans | Peak active connections | Delivered infections |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1701 | 24 | 11 | 23 / 12 | 14 | 6 |
| 3141 | 29 | 5 | 22 / 12 | 8 | 2 |
| 9001 | 23 | 18 | 25 / 16 | 9 | 4 |

Each 600-second run reached all five effective target nodes, including JOBS and
RPT. These are observed counts, not a claim that 60/40 is achieved in small
samples or that the soft population goal caps peak occupancy. The Blackout
witness recorded a legitimate EXT1 connection canceled at WEB with cause
`Blackout` and one −5 Health receipt. The Spawner witness generated children
from DB to eligible internal destinations. The JSON logs are under
`Temp/AdvancedSceneValidation/Logs/UptimeRuns/` in the local isolated project.

Compare the [original fixed-label traffic capture](advanced-play-traffic-before-midpoint-labels.png)
with the [midpoint-label traffic capture](advanced-play-traffic.png). The
[crowded capture](advanced-play-crowded.png) and
[multi-ID inspection](advanced-play-inspect.png) are also scripted batch
**Play Mode** captures at 1920×1080. The capture script exercised `scan DB`,
`kill m7`, `mark`, `log save`, and `inspect -v m7 g5`. It opened `clean DB`, but
the offscreen batch capture did not advance the overlay's real-time typing
animation enough to judge its body or input. These captures do not replace a
human interactive Play Mode review of motion, readability, input focus, and
terminal scrolling. Open `ConnectionsAdvanced.unity` in Unity, press Play, use
`inspect -v <id>`, `scan <node>`, `clean <node>`, `kill <id>`, `mark <note>`, and
`log save`; then review both ingress paths and crowded WEB/API junctions.
