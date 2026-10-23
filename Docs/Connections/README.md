# Connections documentation

Connections is Uptime's active gameplay prototype, separate from the retained Packet prototype. **Executable behavior** is defined by runtime code and the serialized Unity scene. Treat working ideas as hypotheses rather than implemented mechanics.

## Current state and integrated contracts

- [Current State](Current_State.md) — implementation baseline, validation, and source authority.
- [LOAD foundation](connections-arcade-load-v1.md) — target-node LOAD capacity, proportional allocation, and WORK.
- [Operation arcs](connections-arcade-operation-arcs-v1.md) — READ/WRITE/EXECUTE stages and visual presentation.
- [Traffic generation](connections-arcade-traffic-v0.md) — generated benign profiles, spawn plans, and cadence.
- [Run recorder](connections-run-recorder-v0.md) — Play Mode JSON telemetry and terminal commands.
- [Route-aware lanes](route-aware-lanes-v1.md) — path continuity and physical-link lane ordering.
- [Threats V0, Slices 1–2](connections-threats-v0.md) — manual single-operation and delayed READ-to-WRITE threats, operation-completion delivery, and `kill` interception. SPIKING and OVERLOAD remain unimplemented.
- [Behavior and modifier foundation](connections-behavior-foundation-v0.md) — Slice 3A connection and operation behavior scopes, effective WORK/LOAD, and modifier history. Gameplay keywords remain unimplemented.

## Selected phase plans (not yet implemented)

- [Threats V0 — selected baseline](connections-threats-v0.md) — later operation modifiers (`OVERLOAD`/`SPIKING`) and follow-on phases remain selected plans, not implementation requirements for Slice 1. This baseline takes precedence over the older exploratory archive for Threats V0 scope.

## Working designs, not implemented contracts

- [Threat signals, investigation, and response](connections-threat-investigation-ideas.md) — exploratory connection logs, SPIKING, proposed kill/trace/source-block commands, temporary edge probes, covert node infections, asynchronous scanning, and risky blind cleaning. **Not an approved implementation plan.**
- [Lane-spacing continuity](../research/connections-lane-spacing-continuity.md) — parked visual-layout research.

## Broader Uptime design

Shared [Uptime Design](../Design/Uptime_Design.md), [infection ideas](../Design/infections.md), and [clean minigames](../Design/clean-minigames.md) provide context, but their vocabulary and examples do not automatically become Connections gameplay rules. The [Packet prototype](../Packet/Current_State.md) has independent mechanics.
