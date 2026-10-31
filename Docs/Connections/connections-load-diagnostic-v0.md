# Connections — live node LOAD diagnostic V0

**Scope:** Connections terminal presentation and observation. `load <nodeId>` works in the four-node and Advanced scenes; Packet is unchanged. The PR #68 refinement removes passive baseline LOAD from every node in those two gameplay scenes. `NodeResources` and `ResourceAllocator` retain optional baseline LOAD for explicitly authored scenarios and tests.

The player can issue `load db` to append an immediately populated, continuously updating terminal-history panel. Node IDs are case-insensitive and the command accepts exactly one node. Each panel independently watches its selected node until its history entry is removed. Multiple panels can watch the same or different nodes without a scan slot, timer, cost, or cooldown. Normal command focus returns after invocation.

The compact header contains the node identity pill and `LOAD` title. Each active, demanding connection targeting that node gets a row with its current effective `LogicalConnectionState.DesiredLoad`, identity pill, numeric value, and requested-LOAD bar. The row does not show allocation, WORK, purpose, behavior labels, or threat classification. An empty panel says `NO ACTIVE CONNECTION REQUESTS`.

The panel intentionally shows **connection requests only**. It does not display the node's aggregate requested LOAD, capacity, baseline, defensive demand, infection demand, or a remainder row. The node's normal network-view LOAD bar still uses authoritative total `NodeResources.RequestedLoad` against capacity, including infections and defensive demand. A mismatch between the node bar and visible connection requests may be noticed, but the diagnostic does not calculate or name its cause. Payloads and Worms have no ordinary connection LOAD row.

Bar **length** compares current requests within the panel: the target scale is at least 3 LOAD and approximately 1.15 times the largest visible request, rounded upward in 0.5-LOAD steps. Growth is immediate; after a 0.5-real-second grace period, contraction takes about 3 seconds. These values are serialized presentation tuning. Bar **color** remains an absolute request-size cue relative to node capacity: teal below half capacity, amber from half to below full capacity, red at or above full capacity. Exact numeric values remain visible. A small request can therefore have a nearly full teal bar, and a very large request a nearly full red bar. Neither color identifies hostile intent.

Rows start in descending requested-LOAD order and stay in place while values fluctuate; new demand joins the list and ended demand is removed. Height updates use the terminal history's visible-entry anchor so scrolling remains readable. A simulation reset clears old row references and refreshes against the new run. A connection asking for 20 but receiving 2 displays 20, because both its numeric value and bar use effective `DesiredLoad`, never `AllocatedLoad`.

Future identity concealment must provide the player-visible label at the row binding seam; V0 follows the same visible connection ID as `inspect` and does not implement ID hopping or Probe.

See the [focused review](../Review/ConnectionsLoadDiagnosticV0/README.md) for Unity witnesses, deterministic baseline-change measurements, and scripted Play Mode captures. Interactive review should check 16:9 terminal width, bar animation, command focus, and history scrolling.
