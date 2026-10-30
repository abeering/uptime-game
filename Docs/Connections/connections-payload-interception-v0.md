# Connections — Payload & Interception V0

This is a Connections implementation. The entity named Payload here is distinct from the typed *infection payload* already carried by INFECTOR connections. Packet mechanics are unchanged.

## Playable experiment

`payload <targetNodeId> [steady|variable]` is a development-build command. It chooses a connected origin, creates a stable `xN` identity, and plans a physical route to the specified target. The target and complete route are retained internally but are not shown by ordinary player commands. V0 has no ambient Payload generation. A deterministic witness may specify an origin and per-hop timings directly.

Only the current hop appears. Its thin amber segment grows for 1.2 simulation seconds, with a socket at the next node. Then the Payload circle appears and traverses the segment at a constant speed. At an intermediate node, that segment closes and the next begins establishing. Steady hop durations sample 15–20 simulation seconds; Variable samples 7–25 seconds. A separate seeded random stream keeps these samples reproducible without consuming ambient traffic rolls. At its final node the Payload attempts one Phantom LOAD application through the existing infection machinery. It consumes no LOAD or WORK while moving.

`intercept <payloadId> @ <nodeId>` activates an interceptor immediately for five simulation seconds. The node accepts one active interceptor, then has a 15-second cooldown measured from activation. Both timings and its four-LOAD demand are serialized tuning fields on the scene's `PayloadDirector`. A pass-through, a different Payload, an incorrect node, or an arrival after expiry cannot be intercepted. Incorrect node predictions are accepted without route validation. The successful slot remains occupied and consumes LOAD until its original expiry. At an exact shared arrival/expiry timestamp, arrival is evaluated first. Interception prevents the entity's final action before any effect is applied.

The allocator grants baseline LOAD first, existing infection demand second, defensive demand third, then shares the remainder proportionally among ordinary connection requests. This preserves the established baseline and infection priority. Interception remains reliable even if its allocated defensive LOAD is less than requested; allocation measures its operational cost, not success probability. It has no direct Health penalty. A legitimate connection can slow under the reduced capacity and affect Health through the existing connection-only rule.

The beneficial node-status family shows `INTERCEPT xN` with a radial timer, or `CAUGHT xN` after a successful final-node catch. It shares the family panel with CLEAN when both are present. The status reads authoritative simulation state; the terminal confirms `INTERCEPTED xN`. Internal run logs retain creation route and hop timings, movement transitions, final action, infection provenance, interceptor window/cooldown/outcome, defensive demand/allocation, and snapshots. The ordinary terminal does not reveal the planned target.

## Manual Play Mode review

In either Connections scene, use the development command `payload db variable`. The returned `xN` ID is the input to `intercept xN @ db`. Try a wrong node while the Payload is still moving, an early activation that expires, and a different ID at a busy target. For a successful catch, activate at DB shortly before the visible Payload reaches its final hop socket. Verify that the amber line reveals only one hop, the moving circle appears after establishment, the status timer and terminal feedback are legible, and the DB LOAD cost appears during the window. Recheck with several simultaneous Payloads and ordinary logical connections. A scripted Play Mode capture checks presentation at a fixed state but does not replace this interactive timing review.

This phase does not introduce Worms, isolation, new effects, ambient Payload spawning, or Packet changes. Further playtesting should assess whether the fixed five-second window and the initial speed ranges yield useful prediction decisions.
