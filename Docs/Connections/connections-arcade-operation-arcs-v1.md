# Connections Arcade V1 — operation arcs and presentation

**Current status (October 22, 2026):** Integrated operation-arc contract from [PR #5](https://github.com/abeering/uptime-game/pull/5), with inspect presentation refined in [#8](https://github.com/abeering/uptime-game/pull/8). The closing deferral of traffic and spawning is historical to this phase; generated traffic later merged in [#6](https://github.com/abeering/uptime-game/pull/6) and recording in [#7](https://github.com/abeering/uptime-game/pull/7). Target-changing or conditional arcs, threat mechanics, and scoring are not established by those merges. See [Connections Current State](Current_State.md).

Threats V0 Slice 1 subsequently attaches an optional completion effect to an operation. After ordinary WORK completion, the host receives the completed operation index, records that boundary, asks the connection instance for its one-shot effect, then resolves the connection if that was its final operation. `kill` interrupts without invoking the WORK-completion effect. Operation kind and LOAD do not imply hostility or payload delivery. See [Threats V0](connections-threats-v0.md) for the implemented profiles and deferred behaviors.

Threats V0 Slice 2 uses that same arc and effect path for manual-only `delayed-threat`: a 2-LOAD, 16-WORK READ with no effect, then a 12-LOAD, 240-WORK WRITE with one infection-delivery effect on normal completion. READ completion records its boundary and leaves the WRITE waiting until the following fixed step. The ID, route, target, path, and inspect history remain the same. Killing during either operation interrupts the arc without delivering an unfinished WRITE effect.

Slice 3A calls the existing ordered logical definition a **Connection Plan**, while retaining the serialized `operations` field and older arc terminology for compatibility. It adds optional connection- and operation-scope behavior definitions without changing operation order or next-step transitions. The [behavior foundation](connections-behavior-foundation-v0.md) defines its narrow timing, effective requirements, and history rules. `ConnectionSpawnPlan` still means the concrete scheduled arrival, not the Connection Plan.

Phase 2 adds authored, ordered operations to one logical connection. Packet is unchanged.

## Stage model

- Each operation has READ, WRITE, or EXECUTE, total WORK, desired LOAD, and an optional activity description.
- The connection keeps its ID, claimed purpose, route, target, lifecycle, and path view for the whole arc. Only the current operation index, stage WORK, and LOAD change.
- The fixed-step allocator completes one stage, clears its LOAD in that snapshot, and starts the next stage on the following step. Only the final operation makes the connection Complete and starts departure.
- Empty `operations` means the pre-existing single-operation fields remain authoritative. Those serialized fields are retained even on the new authored arc, so older scene data is not discarded.
- Target-node LOAD sharing remains proportional. A stage requests LOAD only while it is active and ready to run.

## Visual mapping

- The existing line mesh gets bright pulse ticks without changing route geometry, width, identity pills, packing, or cycling. WRITE moves origin to target; READ moves target to origin; EXECUTE has stationary, neutral ticks.
- Pulse spacing shrinks with desired LOAD using `pulseSpacingAtOneLoad / (1 + spacingReductionPerLoad * (LOAD - 1))`, capped at 18 pixels. Pulse speed is separately configurable. An 8-LOAD connection therefore has denser activity than a 2-LOAD connection regardless of its allocation.
- AT SPEED is smooth green; SLOWED is orange with repeated pauses; STALLED is red with faint stationary ticks. These are performance states, not threat labels. Cosmetic time never advances WORK.
- The authored inspection block has an operation timeline. Inspect V2 presents it in compact form by default with a right-aligned effective requested LOAD badge; `-v`/`--verbose` reveals the colored status and allocated/requested LOAD line, current-stage WORK percentage and bar, activity, and ETA. All visible timeline items use pills; completed stages are muted and older history and future stages collapse to counts when width is limited. See the [current inspect contract](connections-inspect-v2.md).

## Authored witness and review

`c1` remains a single READ (360 WORK, 5 LOAD) to DB. `c2` remains a single EXECUTE (180 WORK, 5 LOAD) to AUTH. `c3` performs WRITE (30 WORK, 8 LOAD) then READ (60 WORK, 4 LOAD) at DB. It starts after 3 seconds. During WRITE, DB demand is 13/10 and both DB connections slow proportionally. During READ, DB demand becomes 9/10 and `c1` returns to full speed. The route and target stay fixed.

Unity 6000.3.11f1 batch Editor compilation, Resource Witness, and Geometry Witness passed. Resource Witness covers legacy single operations, the authored transition and independent stage values, deferred next-stage demand, capacity and stalled WORK, final release, and compact inspection values. Geometry Witness covers existing layout cases and direction/density settings. Play Mode and visual Game View inspection were not run.

At 16:9 in Unity Play Mode, verify c3's pulses move toward DB during WRITE and reverse during READ without a line or pill reset. Compare c1/c3 pulse density and orange stutter while DB is overloaded; confirm c1 becomes smooth when DB demand drops to 9. Inspect `c3` before and after its transition, `c1`, and `c1 c3`; check the timeline, status color, WORK reset, bar width, terminal history, and command focus. Confirm c3 departs only after READ and all node bars remain legible.

TrafficDirector, spawning, target changes, conditional arcs, threat mechanics, and scoring remain outside this phase.
