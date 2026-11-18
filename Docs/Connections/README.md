# Connections documentation

Connections is Uptime's active gameplay prototype, separate from the retained Packet prototype. **Executable behavior** is defined by runtime code and the serialized Unity scene. Treat working ideas as hypotheses rather than implemented mechanics.

## Core working design

- [THREATS — objective-first generation](THREATS.md) — canonical PURPOSE baselines, LOADER vs INFECTOR objectives, genus-specific spawn parameters, shared signatures/tactics, and open decisions. **A working design anchor, not an implemented contract.**

## Current state and integrated contracts

- [Current State](Current_State.md) — implementation baseline, validation, and source authority.
- [Surprise V0](connections-surprise-v0.md) — reusable scheduling-time consequence plans, initial compound Tactics, Duel integration, and two Advanced ambient Duel routes.
- [Hacker Duel V0 review](../Review/ConnectionsHackerDuelV0/README.md) — merged PR #110: node INTRUSION warning, CLEAN-sized Command Race with startup splash, one global Duel slot, interruption/focus restoration, and single-infection loss effect.
- [LOAD foundation](connections-arcade-load-v1.md) — target-node LOAD capacity, proportional allocation, and WORK.
- [Operation arcs](connections-arcade-operation-arcs-v1.md) — READ/WRITE/EXECUTE stages and visual presentation.
- [Traffic generation](connections-arcade-traffic-v0.md) — generated benign profiles, spawn plans, and cadence.
- [Advanced Scenario V0](connections-advanced-scenario-v0.md) — approved eight-node, dual-ingress playground and staged implementation contract.
- [Run recorder](connections-run-recorder-v0.md) — Play Mode JSON telemetry and terminal commands.
- [Route-aware lanes](route-aware-lanes-v1.md) — path continuity and physical-link lane ordering.
- [Threats V0, Slices 1–3B](connections-threats-v0.md) — manual threats, operation-completion delivery, `kill` interception, and reusable OVERLOAD/SPIKING LOAD behaviors.
- [Behavior and modifier foundation](connections-behavior-foundation-v0.md) — connection and operation behavior scopes, effective WORK/LOAD, modifier history, and Slice 3B fixed-step keyword timing.
- [Inspect V2](connections-inspect-v2.md) — compact default, verbose command mode, width-aware operation pills, and current effective LOAD badge.
- [Video Call Presentation & Soft Placement V1](connections-video-call-presentation-v1.md) — large feed, translucent subtitles, red/gray supplied handset control, node-aware open-time positioning, and delayed developer samples.
- [Video Calls & Escape Coordination V0](connections-video-calls-escape-v0.md) — reusable non-modal caller window, shared LIFO Escape targeting, native input preservation, and CLEAN integration.
- [Network Health V0](connections-network-health-v0.md) — live service Health, deadlines, recovery, failure latch, and permanent compact status strip.
- [Lifecycle V1](connections-lifecycle-v1.md) — Connecting route traversal, cancellation receipts, and connection-only Health.
- [Infections V0: Phantom LOAD](connections-infections-v0.md) — typed operation delivery, persistent node demand, developer controls, and recorder evidence.
- [BACKDOOR V0](connections-backdoor-v0.md) — overt timed Worm Condition and covert persistent infection accelerating INFECTOR WORK without extra LOAD; includes the cascading-threat rationale and deferred experiments.
- [Probe X-ray V0](connections-probe-v0.md) — temporary, truthful physical-edge hostile classification and local line coloring.
- [ID Hopping V0](connections-id-hopping-v0.md) — manual hostile identity aliases, canonical command lookup, and temporary Probe revelation.
- [Reactive Identity Misdirection V0](connections-reactive-identity-v0.md) — stable false IDs that change after failed alias commands.
- [Generated Identity Behaviors V0](connections-generated-identity-v0.md) — provisional ambient selection of hopping and reactive aliases on ordinary hostile connections.

## Selected phase plans (not yet implemented)

- [Threats V0 — selected baseline](connections-threats-v0.md) — later operation modifiers (`OVERLOAD`/`SPIKING`) and follow-on phases remain selected plans, not implementation requirements for Slice 1. This baseline takes precedence over the older exploratory archive for Threats V0 scope.

## Working designs, not implemented contracts

- [Attack pathways, consequences & surprise](connections-attack-pathways-and-surprise.md) — November 2026 design synthesis separating triggers, delivery surfaces, consequences, and selection policy; bounded Surprise V0 followed by an authored progression slice. **Future direction, not an implemented event framework.**
- [Hacker challenges, command races & skill rewards](connections-hacker-command-races.md) — November 1 historical design with a November 13 implementation reconciliation; Duel V0 is merged, while variable consequences, positive rewards, event cards, and upgrade ideas remain proposals.
- [Windowed workspace, Escape & communications V0](connections-windowed-workspace-comms-v0.md) — original October 30 direction, reconciled with implemented desktop/CLEAN/calls/Escape/text messaging (#69–#74) and still-open menu, pause, narrative and density questions. **Partially implemented; use current contracts for executable behavior.**
- [Entities, Conditions, statuses, and reactive events](connections-entities-conditions-events-ideas.md) — exploratory separation of network motion, effects, timed Conditions, persistent infections, player-visible knowledge, and ambient/directed event lifecycles; candidate indirect-pressure Conditions and on-resolution villain behavior. **Not an implementation plan.**
- [Threat signals, investigation, and response](connections-threat-investigation-ideas.md) — exploratory connection logs, SPIKING, proposed kill/trace/source-block commands, temporary edge probes, covert node infections, asynchronous scanning, and risky blind cleaning. **Not an approved implementation plan.**
- [Operation logs & live tailing](connections-operation-logs-design.md) — working design for bounded, operation-local log evidence emitted at WORK milestones and watched through a live `logs <connectionId>` terminal panel. **Not implemented or an approved phase contract.**
- [Edge Probe X-ray & ID hopping](connections-probe-xray-design.md) — working design for temporary, cooldown-limited link surveillance, confirmed hostile traffic visualization, rotating-identity counterplay, and separately parked Payload-route tracking. **Probe and ID Hopping now have separate implementation contracts; Payload-route tracking remains parked.**
- [Lane-spacing continuity](../research/connections-lane-spacing-continuity.md) — parked visual-layout research.

## Broader Uptime design

Shared [Uptime Design](../Design/Uptime_Design.md), [infection ideas](../Design/infections.md), and [clean minigames](../Design/clean-minigames.md) provide context, but their vocabulary and examples do not automatically become Connections gameplay rules. The [Packet prototype](../Packet/Current_State.md) has independent mechanics.
