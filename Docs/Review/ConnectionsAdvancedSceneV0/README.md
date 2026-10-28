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

## Adaptive node entries

The advanced scene now opts into node-aware entry packing. When several active
physical links approach the same card face, they divide the usable face and the
near half of its adjoining sides according to their live lane counts. Diagonal
links may use different offsets at their two ends. Vertical and horizontal links
use one offset that fits both ends, keeping their physical spine and all logical
lanes perfectly straight. The thin physical link and its midpoint label follow
the occupied bundle through the same 0.3-second relayout; an empty link returns
to its authored center. The single-link case stays centered. The four-node
scene keeps its authored attachment widths, and node shapes, routing, traffic,
and simulation are unchanged.

At the deterministic 14-active-connection seed-1701 state, the E1/E2/E4/E6
attachment allowances were 163 / 55.9 / 161.5 / 88 UI pixels. E2 gives some
space to its busy neighbors at API. The visible pill stages were E1 centered,
E2 staggered, E4 staggered, and E6 overlaid; before this pass the same capture
had E1 staggered, E2 staggered, E4 overlaid, and E6 overlaid. These are
presentation measures, not connection limits.

The expanded Advanced Scene Witness checks the authored opt-in, on-card
attachments, straight cardinal lanes and physical spines, center-lane alignment,
separation of different bundles at WEB and API, no path crossings outside shared
cards, and clearance between moving midpoint link labels and all planned ID
pills. Geometry and Advanced Scene Witnesses passed in Unity
6000.3.11f1. Scripted 1920x1080 Play Mode captures: compare the
[previous crowded layout](advanced-play-crowded.png) with the
[adaptive crowded layout](advanced-play-crowded-node-ports.png); see also
[normal traffic](advanced-play-traffic-node-ports.png) and
[multi-ID inspect](advanced-play-inspect-node-ports.png). The captures are
scripted Play Mode states, not an interactive check of animated relayout or
command focus. Adjacent-face port competition and nonrectangular nodes are
outside this first pass.

### Three paths through WEB

The crowded capture is a poor way to see the specific WEB junction improvement.
For a direct comparison, a scripted Play Mode session registered three identical
EXT1 -> WEB -> API connections and captured the same state with adaptive ports
off and then on. [Fixed attachment widths](web-three-before.png) put the outer
beam edges 25.6 UI pixels into overlap at WEB's top border.
[Adaptive entries](web-three-after.png) separate those edges by 70.5 UI pixels,
while both links retain 12-pixel beams and three centered ID pills. The diagonal
bundle uses WEB's upper-right boundary; the vertical bundle moves left as one
straight group with its thin link. The
advanced-scene witness now reproduces this exact 3+3 lane arrangement and checks
the clearance improvement. This toggle was only in the validation session; the
authored advanced scene keeps adaptive ports enabled.

Compare the earlier [flexible cardinal layout](web-three-flexible.png) with the
[straight cardinal layout](web-three-after.png). The corresponding
[flexible crowded](advanced-play-crowded-flexible.png) and
[straight crowded](advanced-play-crowded-node-ports.png) captures show the API-DB
bundle straightening while diagonal bundles retain flexible entries. In a
scripted mid-relayout Play Mode check, E1's physical link and the center of its
three-path bundle shared the same X coordinate; E2's physical link stayed on
its center diagonal path. The Geometry, Advanced Scene, and Topology Stress
regression witnesses passed in the isolated Unity 6000.3.11f1 project; the
stress suite also passed Resource and Traffic witnesses. Interactive motion and
command focus still need review.

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
