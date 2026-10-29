# Connections Node Status Presentation V0

**Implementation scope:** Connections presentation in `Connections.unity` and
`ConnectionsAdvanced.unity`. The two scenes use one authored status prefab and
one presenter per network area. This does not change infection, scan, cleaning,
traffic, LOAD, Health, or Packet mechanics.

## Visible states

| Status | Family | Appears | Clears |
|---|---|---|---|
| OFFLINE | Pain, red | Blackout currently makes the node unavailable | Blackout is removed or the simulation resets |
| INFECTED | Pain, red | A completed `scan <node>` reports INFECTED | A successful PID-Select clean, a later CLEAN scan, or reset |
| CLEAN | Beneficial, teal | A completed scan reports CLEAN | After 2.5 unscaled seconds or reset |

OFFLINE and INFECTED share a single Pain panel with separate text rows. The
former replaces the old node-role subtitle swap; the node keeps its normal role
label. INFECTED is player-acquired diagnostic knowledge. Delivery of a hidden
infection never activates it directly. A successful clean clears that
knowledge without showing CLEAN or revealing an infection delivered after the
clean session began. A new scan can establish new knowledge. The temporary
CLEAN acknowledgment reports a completed diagnosis, not a lasting guarantee.
Terminal scan cards and command feedback remain in place.

The Awareness family is wired for **Editor-only preview** with `PORT SCAN` text.
It is not a gameplay status or an implemented PORT SCANNED mechanic. Each
panel row has an optional radial timer binding; V0 statuses leave both timers
hidden, including CLEAN.

## Presentation

Each active family adds a translucent full-card exterior band. The three bands
can coexist. They fade and expand outward on activation; panel text pops into
place, and the band pulses gently while active. Status panels use the existing
antialiased Connections pill material. The shared prefab has no raycast targets,
so terminal and network input remain available.

One callout stack per node chooses among N, NE, E, SE, S, SW, W, and NW. The
choice scores viewport clipping, other node cards, live terminal sockets and
identity pills, and existing status stacks. The chosen anchor remains stable
as traffic moves. It is reconsidered when the stack composition or viewport
size changes; the stack follows its node. Connection beams may pass beneath
opaque panels. This is an intentionally small placement heuristic, not a
global layout solver.

## Validation and review

`NodeStatusWitness` checks both scene and prefab bindings, state transitions,
same-family grouping, independent preview family, optional per-row timers,
placement, clean-session overlap with a surviving hidden infection, and reset.
The existing Scan, Infection, Geometry, and Advanced Scene witnesses cover the
adjacent behavior. [Review captures and manual steps](../Review/ConnectionsNodeStatusV0/README.md)
show the scripted 16:9 Play Mode examples.
