# Connections: terminal markers V1

**Implementation note:** The original rectangular marker contract below is
preserved for provenance. The circular socket follow-up at the end supersedes
its shape and sizing rules in the current Connections implementation.

An active logical connection has one small identity-colored bar on the border of its target node. Transit nodes and the origin have no marker. The bar indicates where the connection consumes LOAD, regardless of its READ, WRITE, or EXECUTE operation. It adds no resource or routing rule.

`LogicalPathView` owns the marker and uses the authored `TerminalMarker` Image prefab. The scene's existing `IdentityOverlay` draws it above node borders. Each frame the view intersects its final lane, at the **currently interpolated** offset and beam width, with the target card's visible border; it does not use the hidden path underlap. A diagonal beam covers more of the border than a straight beam of the same width: the border span is `beam width / |direction component normal to the border|`, clipped at the card corners. The marker rectangle adds the prefab's `TerminalMarkerStyle` horizontal and vertical padding (8 px each by default) to that span. Its dimensions therefore follow lane packing and angle during reflow. The rectangle keeps the assigned identity accent even when the path beam uses a slowed or stalled warning color.

The marker appears only after the establishing path reaches the target, remains through operation transitions, and fades with the path on completion or blocking. It is destroyed with that path view. It neither receives raycasts nor changes paths, pills, widths, gaps, or palette assignment.

The Geometry Witness checks target versus transit placement, multiple markers at one node, horizontal and diagonal sides, beam-width and angle sizing, corner clipping, interpolated reflow, establishment, operation transition, completion, blocking, color, and cleanup. In Play Mode at 16:9, review roughly six simultaneous connections, including routes through API and routes ending there. Check arrivals, departures, border alignment, marker separation under crowding, node label and LOAD bar clearance, `inspect`, `block`, and command focus.

## Circular socket follow-up — October 29, 2026

The shared `TerminalMarker` prefab now authors a dark circular backing, an
identity-colored antialiased ring, and a muted center dot. The backing interrupts
the node border so a matching identity color still reads as an endpoint. The
socket center sits 2 UI pixels outside the visible border along the final lane's
approach. A pass-through node gets no socket. The preferred backing diameter is
26 UI pixels; the ring is 20 and the dot is 4. For multiple paths ending along
one physical link, the backing, ring, and dot ease down together to fit the
current line pitch, with a 12-pixel floor. The previous rectangular beam-span
calculation and 8-pixel padding no longer drive presentation.

`LogicalPathView` still owns one marker per connection. It follows the
interpolated final lane, appears only after establishment reaches the target,
keeps the identity accent during LOAD warnings, flashes on failure, fades on
normal completion, and is destroyed with the path. `Connections.unity` and
`ConnectionsAdvanced.unity` share the same prefab. This is presentation only;
route, traffic, LOAD, WORK, Health, and recorder behavior remain unchanged.

The updated Geometry Witness checks the authored socket material and bindings,
target-only visibility, straight and diagonal placement, crowded sizing,
interpolated position, lifecycle colors, and cleanup. Sparse and crowded
1920×1080 scripted Play Mode captures are in the
[review folder](../Review/ConnectionsTerminalSocketsV0/README.md); interactive
motion and input focus still require manual Editor review.
