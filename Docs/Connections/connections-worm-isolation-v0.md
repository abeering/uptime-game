# Connections — Worm Propagation & Isolation V0

**Scope:** Connections implementation on `feat/connections-worm-propagation-isolation-v0`. This is a manual, playable experiment; Packet and ambient threat generation are unchanged.

## Behavior

`worm <origin> scan-jammed|port-scanned` starts one outbreak at a physical node and reports its `wN` ID. The origin is already entered when the command runs, so its selected Condition applies immediately. Every later branch has its own `wN.M` ID and traverses one physical link. Its thread establishes first, then a thick segment crawls along the same path. The head reaches the next node before the tail; only full entry applies the Condition. A branch dwells, then independently rolls for each eligible outgoing link in stable link-ID order. A node can be claimed only once per outbreak, including while a branch is traveling toward it. Parent links cannot immediately backtrack. The seeded roll, selected and skipped links, lineage, timing, and effect provenance are internal recorder data.

The Worm Director Inspector owns provisional propagation probability, node budget, establishment, travel, action, and Condition duration settings. The default Condition duration equals action duration; changing either does not change the other. The thread and body share ordinary connection and Payload lane/port packing. Worms do not consume LOAD, alter Health directly, or appear in ambient generation.

`SCAN JAMMED` is a timed Pain Condition. New scans of that node fail while it lasts; a scan already running may complete. `PORT SCANNED` reuses the timed Awareness Condition. Cleaning removes infections through its existing flow and does not remove Conditions. Killing or containing a branch does not erase a Condition it already applied.

`isolate <node>` immediately makes a node unavailable for eight simulation seconds by default. It destroys Worm bodies dwelling there and bodies entering it, and prevents a traveling Worm from entering at arrival. Already departed siblings continue. Isolation uses the existing service cancellation and Health consequences: benign canceled connections cost five Health each, hostile cancellations cost zero, with no additional flat isolation penalty. It is separate from Blackout, has its own `Isolation` cancellation cause, and expires independently; a remaining Blackout keeps the node unavailable. The node has a 30-second cooldown measured from activation. Isolation is visible as OFFLINE plus timed ISOLATED; it can coexist with infection knowledge, CLEAN, SCAN JAMMED, PORT SCANNED, and an active interceptor.

**Payload interaction:** Isolation is a service shutdown and Worm containment action. It does not destroy Payloads or intercept them. Payload traversal and final-action interception retain their existing rules; a Payload may pass through an isolated node. This is an explicit V0 rule to keep `isolate` from replacing the prediction and LOAD cost of `intercept`.

## Recorder and reset

The run log records outbreak seed/effect/tuning, branch parentage and per-hop state, entry/effect/action boundaries, every propagation decision, termination, isolation action/counts/duration/expiry, and Worm/Isolation snapshots. The Condition receipt carries the source Worm branch ID. Reset clears outbreaks, branches, Conditions, active isolation, and cooldowns. These details are not exposed by ordinary `inspect`.

## Play Mode review

In **Connections** and **ConnectionsAdvanced**, reopen the scene from disk after switching branches, enter Play Mode, and use `worm ext1 scan-jammed`. Watch the thin thread grow from the origin, the thicker segment emerge, and the tail enter before the next status appears. The advanced scene provides a clear fork at API; use `worm api port-scanned` and wait for multiple outgoing branches. Use `isolate db` before an incoming branch enters, and separately while one is resident. Confirm `scan db` reports the jam only while SCAN JAMMED is active, `clean db` does not erase it, and an established benign connection through an isolated node receives one cancellation receipt and the ordinary Health penalty. Try `payload db variable` with `intercept <id> @ db` to confirm isolation leaves Payload defense separate. Review at 16:9 with ordinary traffic both sparse and crowded, including node-status callouts and command focus.

The visual judgment is whether the thickened curve reads as one body wriggling through a persistent thread, and whether the fork communicates propagation rather than independent unrelated arrivals.
