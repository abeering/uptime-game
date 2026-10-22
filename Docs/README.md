# Uptime documentation

Uptime is one game design explored through two independent gameplay prototypes: Packet and Connections. Both remain playable and neither is declared the final implementation.

## Start here

1. [Uptime Current State](Uptime_Current_State.md) — project posture, implementation boundaries, validation, and source authority at the documented baseline.
2. [Uptime Design](Design/Uptime_Design.md) — living overview of the whole game and links to the full design documents.
3. [Packet Current State](Packet/Current_State.md) or [Connections Current State](Connections/Current_State.md) — what the relevant prototype currently implements and where to inspect its executable truth.
4. The relevant code and serialized Unity scene on the working branch when exact behavior matters.

## Reading existing documents

This is the first documentation cleanup phase. Existing files have **not** been moved, shortened, or archived. `Docs/Design/` still contains shared Uptime thinking, prototype-specific mechanics, research, and older implementation contracts side by side. A document's location or versioned title does not make every statement a current rule.

The original [Connections V0 brief](Design/connections-v0.md) records the first experiment and future hypotheses. The [network and connection grammar](Design/network-connection-grammar-v1.md) preserves useful vocabulary and an earlier resource model; its CPU/disk/memory/link allocation and one-stage implementation mapping were superseded by later Arcade work. For current Connections behavior, start with its Current State and the [LOAD](Design/connections-arcade-load-v1.md), [operation arcs](Design/connections-arcade-operation-arcs-v1.md), [traffic](Design/connections-arcade-traffic-v0.md), and [recorder](Design/connections-run-recorder-v0.md) documents, checking the scene and runtime where prose may be stale.

The four folders under `Docs/Review/` preserve Connections inspect UI Editor previews. Their READMEs state what was rendered; the images are not Play Mode validation. Packet-oriented notes, proposals, and debug examples also remain in their original paths for now. The current-state files identify useful entry points without replacing those sources.

## Source authority

For **implemented behavior**, inspect current branch runtime code and serialized scene/prefab settings first. Current-state documents summarize that evidence; focused prototype contracts explain intended behavior but must be corrected if execution differs. For **the game we are designing**, use [Uptime Design](Design/Uptime_Design.md) and its linked full source documents, distinguishing established intentions and active working designs from unresolved details. Shared intent does not assert that either prototype implements it.

Research, old phase briefs, review captures, PR descriptions, and Git history remain evidence and provenance. They do not override current execution or silently authorize new mechanics. When a claim changes, update the appropriate individual source and its current-state pointer. No generated ChatGPT Project Core exists yet; generating one is a later approved phase.
