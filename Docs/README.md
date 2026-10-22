# Uptime documentation

Uptime is one game design explored through two independent gameplay prototypes: Packet and Connections. Both remain playable and neither is declared the final implementation.

## Start here

1. [Uptime Current State](Uptime_Current_State.md) — project posture, implementation boundaries, validation, and source authority at the documented baseline.
2. [Uptime Design](Design/Uptime_Design.md) — living overview of the whole game and links to the full design documents.
3. [Packet Current State](Packet/Current_State.md) or [Connections Current State](Connections/Current_State.md) — what the relevant prototype currently implements and where to inspect its executable truth.
4. The relevant code and serialized Unity scene on the working branch when exact behavior matters.

## Where sources live

- [Design](Design/Uptime_Design.md) holds shared Uptime game direction, including company identity, runs, scoring, upgrades, threats, cleaning, notifications, events, and topology. Packet terminology in a source does not by itself make its larger design intent Packet-only. Documents may contain both active direction and unsettled mechanics.
- [Packet](Packet/Current_State.md) holds Packet-specific scanning, keywords, traversal, command examples, and interface ideas. Packet is parked and retained.
- [Connections](Connections/Current_State.md) holds the integrated [LOAD](Connections/connections-arcade-load-v1.md), [operation arcs](Connections/connections-arcade-operation-arcs-v1.md), [traffic](Connections/connections-arcade-traffic-v0.md), and [recorder](Connections/connections-run-recorder-v0.md) contracts, plus the [recorder example](Connections/examples/connections-run-recorder-example.json). Their status notes distinguish original phase deferrals from later merged work.
- [Research](research/README.md) preserves unresolved examples and implementation proposals that are not current authority. [Historical](historical/README.md) preserves superseded contracts, completed checkpoints, and unverified old observations. The [Connections V0 brief](historical/connections-v0.md) and [network and connection grammar](historical/network-connection-grammar-v1.md) remain complete; the grammar's older CPU/disk/memory/link allocation is not the current LOAD model.
- `Review/` retains four Connections inspect UI Editor-preview folders with their READMEs and images. These are presentation evidence, not interactive Play Mode validation.

Phase 2 moved source documents without condensing their design material. Follow the links above or the prototype current-state pages; a directory, versioned title, or old "next" list alone does not establish current authority.

## Source authority

For **implemented behavior**, inspect current branch runtime code and serialized scene/prefab settings first. Current-state documents summarize that evidence; focused prototype contracts explain intended behavior but must be corrected if execution differs. For **the game we are designing**, use [Uptime Design](Design/Uptime_Design.md) and its linked full source documents, distinguishing established intentions and active working designs from unresolved details. Shared intent does not assert that either prototype implements it.

Research, old phase briefs, review captures, PR descriptions, and Git history remain evidence and provenance. They do not override current execution or silently authorize new mechanics. When a claim changes, update the appropriate individual source and its current-state pointer. No generated ChatGPT Project Core exists yet; generating one is a later approved phase.
