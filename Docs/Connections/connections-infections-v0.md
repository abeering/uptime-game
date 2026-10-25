# Connections Infections V0 — Slice 1: Phantom LOAD

**Status:** Connections implementation on the Phantom LOAD V0 branch. This contract describes the first persistent node infection only; Packet remains independent.

## Delivery and ownership

A concrete connection plan carries an optional typed infection payload on an operation whose completion effect is `InfectionDelivery`. The first kind is `PhantomLoad`, with a finite positive requested LOAD amount. The Connections scene sets the default to **8 LOAD** on `SimulationHost`; the Inspector value applies to newly generated DELIVERY plans and manual legacy threat profiles. The payload and parameter are fixed when the plan is queued. Existing delivery profiles receive the default payload while keeping their route, operation sequence, WORK, and requested LOAD. Newly generated DELIVERY purposes place the payload on their final operation.

Normal WORK completion emits the existing delivery occurrence and applies its payload to that connection's target node. `kill` before completion prevents both. Application happens once per operation; killing or retiring the carrier afterward leaves the node infected. The simulation host owns infection instances independently of connections and clears all of them on reset. An instance records its target, kind, amount, simulation application time, and source connection ID and operation index. One instance of a kind may be active on a node. A repeated application is rejected and recorded without stacking or refreshing the original.

The fixed-step clock is authoritative. There is no duration, automatic recovery, cleaning, or direct Health damage from application.

## Resource effect

For each target node, the allocator reserves authored baseline LOAD first, then grants infection demand from remaining capacity, then shares what remains proportionally among active logical connections. Requested LOAD is baseline plus infection requests plus connection requests. Allocated LOAD is the corresponding sum and never exceeds capacity. Phantom LOAD continues requesting resources even with no logical connections. For capacity 10, baseline 0, Phantom LOAD 8, and two 5-LOAD connections, each connection receives 1 LOAD and advances WORK at 1 unit per second. With baseline 2, the same infection receives 8 and those connections receive zero.

The existing node LOAD bar reads authoritative total requested LOAD. Ordinary inspect does not name the infection or reveal hidden intent. Legitimate connections slowed by the reduced allocation affect Network Health through its existing service rules. The infection is not a logical connection and does not directly penalize Health.

## Developer controls

Editor and development builds support `spawn <profileId>` as before. For a purpose template, `spawn <purposeId> obj:loader` forces LOADER and runs the existing high-LOAD anomaly recipe. `spawn <purposeId> obj:delivery inf:phantom-load` forces DELIVERY and uses the scene's default infection amount; append `infp:load=8` to force an explicit positive amount. `obj:delivery` also defaults to Phantom LOAD when `inf:` is omitted. Forced objectives skip the ambient hostile and delivery probability rolls, while normal purpose sampling and the selected anomaly recipe still use the seeded generator. Infection options require DELIVERY; `infp:load` requires `inf:phantom-load`. Invalid options, unknown purposes, and invalid amounts return errors before a plan is queued. The command creates a real queued connection; infection application waits for its operation to complete. The older fixed profile IDs retain their plain `spawn <profileId>` behavior.

For mechanical tests in Editor and development builds: `infect <nodeId> phantom-load [load]` injects directly, `uninfect <nodeId>` removes Phantom LOAD, and `infections` prints authoritative state. These are debug controls, not player investigation or `clean` gameplay. Direct injection and removal use the same host state and allocator as delivered infections.

## Internal recorder evidence

`plan_queued.definition.operations` includes each concrete operation's infection kind and LOAD parameter. Purpose-generated plans retain their decision and anomaly trace; `generation.forcedOptions` records the explicit objective, infection type, and whether a LOAD amount was forced. Application, duplicate rejection, and debug removal emit structured `infection_applied`, `infection_rejected`, and `infection_removed` events with target, kind, amount, time, reason, and source provenance. Snapshots include active infection instances and per-node baseline, infection request/allocation, connection request/allocation, and totals. This truth remains in the internal run log rather than ordinary inspection. The additive fields keep schema version 1.

## Validation and manual review

`Tools/Connections/Run Infection Witness` checks forced command forms and errors, seeded payload replay, interruption, successful and duplicate delivery, carrier retirement, direct controls, baseline priority, WORK and Health effects, JSON provenance, and legacy manual profiles. Existing Traffic, Resource, Recorder, Network Health, Behavior, Inspect V2, and Geometry witnesses cover adjacent behavior.

At 16:9 in `Connections.unity` Play Mode:

1. Use `spawn product-lookup obj:delivery inf:phantom-load infp:load=8`. Observe the real connection before completion; after delivery and retirement, DB LOAD should remain elevated. Try `kill` on another carrier before it completes.
2. Use `infect DB phantom-load 8` with no connection active, then `infections` and `uninfect DB`. Watch DB LOAD rise and return. With benign traffic to DB, compare its progress before and after removal.
3. Use `mark phantom load review` and `log save`; inspect the JSON for queued payload/forced decisions, infection events, active infection snapshots, and node LOAD breakdown. Test terminal focus after commands.

This slice defers Blackout, Spawner, Throttle, cleaning, infection investigation, timers, scoring, and Packet integration.
