# Connections — live node LOAD diagnostic V0

**Scope:** Connections terminal presentation and observation. `load <nodeId>` works in the four-node and Advanced scenes; Packet is unchanged.

The player can issue `load db` to append an immediately populated, continuously updating terminal-history panel. Node IDs are case-insensitive and the command accepts exactly one node. Each panel independently watches its selected node until its history entry is removed. Multiple panels can watch the same or different nodes without a scan slot, timer, cost, or cooldown. Normal command focus returns after invocation.

The header displays `NodeResources.RequestedLoad` and `LoadCapacity`. Each active, demanding connection targeting that node gets a row with its current effective `LogicalConnectionState.DesiredLoad`, identity pill, numeric value, and requested-LOAD bar. The row does not show allocation, WORK, purpose, behavior labels, or threat classification. The bar is scaled from node capacity and grows its scale only when a request exceeds it; scale does not contract on a quiet phase. Teal, amber, and red thresholds are serialized presentation settings relative to capacity. Bars respond quickly while numeric values update at the panel's real-time refresh cadence.

An `OTHER` row shows the non-connection remainder of the node's published requested total when it is meaningful. That remainder can contain baseline, defensive, or infection demand; the panel does not name covert sources. Payloads and Worms have no ordinary connection LOAD row. Row order starts by descending requested LOAD and remains stable while values fluctuate; new demand joins the list, and ended demand is removed. Height updates use the terminal history's visible-entry anchor so scrolling remains readable. A simulation reset clears old row references and rebinds the panel to the current node resources on its next refresh.

This is a diagnostic of **requested**, not **allocated**, LOAD: a connection asking for 20 but receiving 2 displays 20. The node total and connection requests come from the same published allocator state. Future identity concealment must provide the player-visible label at the row binding seam; V0 follows the same visible connection ID as `inspect` and does not implement ID hopping or Probe.

See the [focused review](../Review/ConnectionsLoadDiagnosticV0/README.md) for Unity witness and Play Mode evidence. Interactive review should check 16:9 terminal width, crowded rows, command focus, and live SPIKING/WAVING animation.
