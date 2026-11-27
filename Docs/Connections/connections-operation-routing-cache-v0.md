# Connections — Operation Routing & CACHE V0

Connections implementation from verified main `6ecb3ce6c30159e2f2b1a5217a8c03e46dc3a65b`, the normal GitHub merge of human-accepted API Configuration #136. Implements the narrow CACHE candidate from the [Business Operations & Reliability design](../Design/Uptime_Design.md#2-node-configuration-can-change-passing-traffic). Packet remains separate and unchanged.

Player explanation: **API makes selected traffic faster when the network is busy. CACHE makes selected READ traffic cheaper and faster to process.** These are different mechanisms; neither changes the original business value.

## Persistent request, committed operation execution

`LogicalConnectionDefinition` remains the original logical plan: identity, stable claimed PURPOSE, ordered operation requirements, effects, behaviors, evidence, original ordinary route and fixed business value. Each `OperationStage` can optionally author an `Intended Destination`; absent that reference, it inherits the plan's original target. Copying, workload scaling and validation preserve this optional destination. Existing generated recipes retain their current destinations and sampling.

`SimulationHost.CommitOperationRoute` is the small resolution boundary combining operation requirements, physical topology and current node configuration. It creates one `OperationRouteResolution` per operation, storing ordinary/actual destinations, physical nodes/links, locked multipliers, reason, selected CACHE PURPOSE and commit time. The source definition is never rewritten by a CACHE hit. Presentation, allocator, interruptions, Probe/API eligibility and effects use runtime committed route/target state.

Resolve the first operation immediately before initial traversal, or when a Pending request becomes due. Resolve a subsequent operation on the next simulation step after the preceding completion, using the configuration then in effect. Every operation starts from the original request origin. A READ's direction still expresses fulfillment toward that origin; it does not migrate partially completed WORK between processors.

A committed route and efficiency remain locked until completion/termination. Configuration cannot change an in-flight route or reprice accomplished WORK. Existing API allocation priority remains live at subsequent allocation updates and reads the **actual operation route**, including CACHE paths and later bypasses.

When the next operation resolves to the same physical route, reuse the established path and preserve the original next-step operation behavior. A different route begins ordinary progressive traversal from the request origin before processing at its new target. The existing path view, connection identity, palette ownership, heartbeat and destination marker are retained; only their committed geometry/destination changes. No automatic failover, topology construction or routing-policy framework is introduced.

## CACHE eligibility and availability

`ssh cache` uses the same authored SSH window, keyboard ownership, arrow/Enter choices, mouse rows, Escape close and Duel handoff as `ssh api`. One slot, empty by default, selects from existing known PURPOSE profiles containing READ operations. Clear/change persists after close; restart clears CACHE and API independently. WRITE/EXECUTE stages in an otherwise eligible class remain ordinary. Canonical profile IDs match, never display strings or hidden hostility.

For a matching READ, use CACHE if its physical route is available and the operation can still fulfill its effect requirements. Stable breadth-first physical traversal consumes no generation RNG. At resolution:

- Missing/offline/unreachable CACHE, including BLACKOUT or Isolation along its candidate path: keep the ordinary destination/requirements and record `cache_unavailable_or_unreachable`.
- A required adjacent Payload launch destination unavailable from CACHE: keep the ordinary route and record `cache_effect_route_incompatible`. Do not silently drop or sanitize the launch.
- CACHE-origin requests: keep the ordinary route and record `cache_local_source_unsupported`. The retained path model requires distinct route endpoints; local zero-link execution is outside this slice.
- A valid match: commit CACHE and `cache_read_match`; all other cases record `ordinary`.

Ordinary fallback retains the original route when using the plan's ordinary target. It does not route around offline ordinary nodes. Once CACHE is committed, later unavailability follows existing lifecycle rules: an offline reached node interrupts; an offline future node cancels on arrival. Active BLACKOUT/Isolation interrupts the committed route. No switch back to the original processor occurs. The same source/node identities and failure ledger apply.

CACHE is an ordinary internal, noncritical node with normal capacity, baseline, infections, conditions, defensive reservations, scanning/CLEAN/Overclock and two-point Integrity. It can be damaged and compromised. The Small topology adds CACHE northeast of API with E4; Advanced fills the vacant upper-right card position with E8. Each adds only one API–CACHE leaf link: Small 5 nodes/4 links; Advanced 9 nodes/8 links. Existing nodes, links and their capacity/tuning remain. No new PURPOSE destination weights are needed: configured READ redirection supplies CACHE's legitimate cover.

## Efficiency, effects and service

Initial authored tuning:

| Setting | Value / Inspector |
|---|---|
| CACHE capacity | 10 LOAD, SimulationHost → Simulation Host → Nodes → CACHE entry → Load Capacity |
| PURPOSE slots | 1, fixed V0 capability; select via `ssh cache` |
| Cached processing WORK | 0.25×, Simulation Host → CACHE configuration → Cached Work Multiplier |
| Cached requested LOAD | 0.5×, same section → Cached Load Multiplier |

Multipliers must be finite and positive. Restart after Inspector edits to refresh run metadata. The efficiency decision is captured at route commitment and remains fixed for that operation.

Apply CACHE after the existing 2.5× legitimate workload pipeline. The unchanged behavior runtime composes original operation requirements, modifiers and seeded noise; the locked CACHE factors then scale its effective WORK/LOAD once. Behavior timing, rate multipliers (including zero productive rate), effects, keywords and source history remain. Natural-noise snapshots report effective LOAD units. Earned log milestones use the same authored fractions of the reduced processing requirement, preserving their order, content and identity. Faster execution shortens the investigation window; this phase does not retune hostile recipes or evidence pacing.

A 100-WORK/5-LOAD operation becomes 25 WORK/2.5 LOAD: 10 nominal seconds instead of 20, with identical original business value. Matching hostile READs receive the same benefit. Infection/condition completion effects apply normally to the actual execution destination, including CACHE. Payload launches retain physical adjacency requirements as described above. No hidden hostile filter exists.

The whole connection retains its original nominal service duration and tolerance-derived budget, calculated from the unoptimized plan at first destination establishment. Subsequent route traversal and processing consume that same deadline; a later arrival never restarts it. Projection uses actual current processing or remaining traversal plus nominal current execution, with unresolved future operations estimated ordinarily. Extra traversal can outweigh efficiency for short mixed arcs and cause a timeout. Completed intermediate operations earn no business credit. Only final successful whole-connection completion credits the fixed original job value once; timeout/kill/interruption retains one ordinary failure.

## Presentation and recording

Use current-operation committed geometry in shared rail/pill/socket rendering. No positive rail status, new material, palette change or heartbeat redesign is added. Inspect's operation timeline shows actual destinations and retained historical destinations. `CACHE ·` identifies a cached operation; verbose WORK shows reduced progress/requirement and locked WORK/LOAD factors, alongside the existing requested LOAD badge and activity. API benefit remains a separate indicator.

Optional schema 4 additions record CACHE node/multiplier metadata, `cache_policy_changed`, `snapshot.cachePurposeId`, and `operation_route_committed` decisions. Routing records include operation index, original and actual destinations, physical route, original/reduced base requirements, committed multipliers/time, decision reason and selected CACHE PURPOSE **at commitment**. Started-operation events and active snapshots carry the same routing evidence alongside effective LOAD/WORK. Original concrete recipes remain in definition records with per-operation intended destinations. A policy change does not rewrite an earlier decision. Recording remains observational.

## Retained scope and review

Preserve authored legitimate means Small 23.225807s / Advanced 20s, 2.5× workload, independent demand/hostile clocks, PURPOSE distribution, existing hostile weights/cadence, original node capacities, objectives and timeout tolerance. CACHE's new attackable node necessarily changes topology-dependent attack choices; it does not promise identical hostile recipes after execution state diverges. No calibration matrix or final difficulty verdict is claimed.

No cache population/eviction/freshness, load balancing, automatic failover, upgrades, additional slots, company importance/events, SSH overhaul or new rail language is implemented. [Review evidence, architecture checks, limitations and two controlled Play Mode fixtures](../Review/ConnectionsOperationRoutingCacheV0/README.md).
