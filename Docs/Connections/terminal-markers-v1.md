# Connections: terminal markers V1

An active logical connection has one small identity-colored bar on the border of its target node. Transit nodes and the origin have no marker. The bar indicates where the connection consumes LOAD, regardless of its READ, WRITE, or EXECUTE operation. It adds no resource or routing rule.

`LogicalPathView` owns the marker and uses the authored `TerminalMarker` Image prefab (14 × 4 UI pixels). The scene's existing `IdentityOverlay` draws it above node borders. Each frame the view intersects its final lane, at the **currently interpolated** offset, with the target card's visible border; it does not use the hidden path underlap. The rectangle aligns with the border side and keeps the assigned identity accent even when the path beam uses a slowed or stalled warning color.

The marker appears only after the establishing path reaches the target, remains through operation transitions, and fades with the path on completion or blocking. It is destroyed with that path view. It neither receives raycasts nor changes paths, pills, widths, gaps, or palette assignment.

The Geometry Witness checks target versus transit placement, multiple markers at one node, horizontal and diagonal sides, interpolated reflow, establishment, operation transition, completion, blocking, color, and cleanup. In Play Mode at 16:9, review roughly six simultaneous connections, including routes through API and routes ending there. Check arrivals, departures, border alignment, node label and LOAD bar clearance, `inspect`, `block`, and command focus.
