# Connections status placement refinement review

This Connections-only presentation experiment changes the placement heuristic, not status content or gameplay. A node's stack now occupies only the region above or below its card. It starts centered and may shift horizontally by 30 or 60 UI units and outward by 24 or 48 units to clear terminal markers, identity pills, other cards, and the viewport. The presenter reevaluates live obstacles each render but holds a clear previous choice to avoid needless movement. Connection beams are allowed behind panels.

The optional width change was left out. The existing 220-unit panel keeps long labels and remaining-seconds timers readable, including two-family stacks in these captures.

## Before and after

All eight images are **scripted Unity Editor renders**, not interactive Play Mode captures. The same temporary capture script opened each production scene, queued long-lived connections on adjacent links, stepped the simulation for two seconds, and applied `ScanJammed` and `PortScanned` Conditions to several nodes. This puts terminal markers on different sides of nodes and shows multiple simultaneous status families. The capture helper was kept outside the repository.

| Scene | Resolution | Current-main baseline | Above/below refinement |
| --- | --- | --- | --- |
| Connections | 1920×1080 | [before](before-Connections-1920x1080.png) | [after](after-Connections-1920x1080.png) |
| Connections | 1280×720 | [before](before-Connections-1280x720.png) | [after](after-Connections-1280x720.png) |
| ConnectionsAdvanced | 1920×1080 | [before](before-ConnectionsAdvanced-1920x1080.png) | [after](after-ConnectionsAdvanced-1920x1080.png) |
| ConnectionsAdvanced | 1280×720 | [before](before-ConnectionsAdvanced-1280x720.png) | [after](after-ConnectionsAdvanced-1280x720.png) |

The [before](before-metrics.txt) and [after](after-metrics.txt) measurements record stack, node, marker, and pill bounds in network-area coordinates. The Advanced baseline put six stacks to a side or corner; the refinement puts all six directly above or below their owners. Both scenes have **zero terminal-marker overlaps** at both resolutions. The Advanced scene has seven live markers and pills; the four-node scene has four of each.

The final capture records **one identity-pill bounding-box overlap at API in each scene** at both resolutions. For the four-node scene, p3 occupies the above region while p1/p2/p4 occupy the below region. In Advanced, the top-edge viewport and live terminal markers limit API's choices. No candidate within the small allowed offsets clears every padded marker, card, viewport edge, and pill at those locations. These are conservative axis-aligned bounds for rotated pills; the visible pill text should also be judged in the captures. The overlap is recorded rather than moving a panel to a side or covering a terminal marker.

## Validation

- Unity 6000.3.11f1 batch Editor compilation and `NodeStatusWitness.Run` passed in an isolated project copy. The updated placement cases cover centered preference, terminal-marker and pill clearance, other cards and stacks, top and bottom edges, stable previous choices, reaction to a new obstruction, and best available placement when both regions are blocked. The existing status grouping and timer checks also passed.
- The scripted captures rendered successfully at both resolutions in both scenes. The measurement files document the API pill-bound overlaps.
- `git diff --check` passed.

## Manual Play Mode review

Open `Connections.unity` and `ConnectionsAdvanced.unity` at 1920×1080 and 1280×720. Run live traffic, then create concurrent statuses with `worm <nodeId> scan-jammed|port-scanned`, `infect <nodeId> blackout`, and completed `scan <nodeId>` results. Watch top-edge API, bottom-edge AUTH/RPT, and a DB stack with multiple families. Check that terminal markers stay visible as connections arrive or depart, identity pills remain readable, and stacks do not bounce between positions. Confirm terminal command input focus. Interactive Play Mode behavior was not validated by these Editor renders.
