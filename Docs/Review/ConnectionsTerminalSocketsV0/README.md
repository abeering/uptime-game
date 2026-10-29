# Connections terminal sockets V0 review

This is a **Connections presentation** change. The shared `LogicalPath` prefab now
instantiates a circular `TerminalMarker` socket at the final route node in both
`Connections.unity` and `ConnectionsAdvanced.unity`. A dark backing disk breaks
the node border behind an identity-colored ring and a muted inner dot. The
socket sits 2 UI pixels outside the visible card edge, follows the final lane
through relayout, and retains the path's establishment, fade, and failure-flash
timing. A route passing through a node gets no socket there.

The authored socket is 26 UI pixels across, with a 20-pixel ring and 4-pixel
dot. When several paths terminate along one link, its diameter eases down over
the existing 0.3-second relayout interval to fit the current line pitch, with
a 12-pixel floor. Its ring and dot scale with the backing disk. Traffic,
simulation, resource allocation, and gameplay commands are unchanged.

## Validation

Unity 6000.3.11f1 compiled and ran in an isolated project copy. Geometry and
Advanced Scene witnesses passed. The Geometry witness checks the authored
material and bindings, circular footprint, final-node-only appearance,
diagonal and crowded sizing, interpolated lane position, completion fade, and
failure flashes. Lifecycle Witness passed. The Topology Stress regression suite
passed, including Geometry, Resource, and Traffic witnesses.

These are scripted **1920×1080 Play Mode** captures:

- [Sparse diagonal termination](sparse-diagonal.png): one
  EXT2 → JOBS → API connection passes through JOBS and ends with one socket at
  API. Ambient generation was disabled for this capture.
- [Crowded advanced scene](crowded-advanced.png): seed 1701 at 335 simulated
  seconds, with 14 active or Connecting connections. Sockets remain visible
  along the busy API, DB, and JOBS borders.

The captures show rendered states, not interactive input or the motion between
states. For manual review, open each Connections scene at 16:9 in the Unity
Editor and press Play. Watch a single route reach a target, then watch multiple
routes terminate at the same node. Check a diagonal target, an intermediate
pass-through node, lane relayout, a normal completion fade, and a killed path's
red failure flashes. Also check identity pills, `inspect`, and command focus
while the endpoint sockets are visible.
