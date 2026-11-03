# Connections Probe X-ray V0

**Scope:** Connections implementation. This is the first playable slice of the [Probe working design](connections-probe-xray-design.md). The working design still holds open questions and describes later ID hopping and Payload-route ideas; this slice implements neither.

`probe <linkId>` immediately selects one physical link, matched without regard to letter case. The host allows one active Probe. Its serialized defaults are 10 simulation seconds active and 35 simulation seconds of cooldown measured from activation. Another deployment can begin at the cooldown deadline after the first has expired. Invalid links, a busy Probe, and cooldown attempts leave the current state unchanged. Reset and disable clear both timers and the selected link.

The simulation owns availability and uses each logical connection's authoritative `IsHostile` flag. Only Connecting or Active ordinary logical connections on the selected physical link are eligible for classification. Connecting paths reveal only their already reached distance; no later route segment is exposed. A resolved connection stops being classified. Payloads and Worms use their existing independent rendering and response commands.

An authored translucent blue/steel rectangle spans 80% of the physical link. Its cross-edge width follows the actual outer edges of visible rendered lines, including line thickness, with about 14 UI pixels of padding per side and a minimum for an empty edge. The logical path mesh clips both body and activity pulses against that rotated rectangle: hostile parts become vivid red, benign parts become desaturated gray, and line sections outside keep their identity/performance presentation. Identity pills and their packing are unchanged. Width expands for new traffic and eases inward after departures.

`help` lists the command. The terminal acknowledges activation and expiration, reports busy or invalid links, and gives remaining simulation seconds on cooldown. Probe does not kill, block, allocate LOAD, alter Health, or change connection progress. The recorder emits `player_action` for every attempted command and `probe_activated` / `probe_expired` at simulation timestamps. Probe records include `probeLinkId`; activation and expiry include the window and cooldown deadlines. Classification is not added to ordinary inspection.

## Review

Run the Probe Witness and the existing Geometry, Traffic, Resource, Inspect, and Recorder witnesses in a Unity 6000.3.11f1 isolated project copy. In Play Mode, inspect both `Connections.unity` and `ConnectionsAdvanced.unity` at 16:9: a crowded edge, an empty edge, diagonal, horizontal, and vertical links, lane reflow, a connection crossing multiple links, new arrivals and departures, and terminal focus/history while Probe expires. Use developer `spawn basic-threat` and `spawn spiking-benign` to compare truth versus LOAD appearance. Confirm ordinary `kill` remains a separate action.
