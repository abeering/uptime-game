# Inspect operation timeline review

These screenshots were rendered in Unity Editor at the Connections scene's 1920×1080 reference resolution. The full scene image uses the authored scene UI and a deterministic simulation state. For the middle-stage preview only, the Editor capture used a temporary three-operation arc on `c3`: READ, WRITE, EXECUTE. This preview does not change `Connections.unity` or its authored connection data. The close-ups render the same authored inspect prefab at native UI scale.

| State | Screenshot |
| --- | --- |
| Full 16:9 scene, active middle stage | [middle-stage-1920x1080.png](middle-stage-1920x1080.png) |
| Completed READ, slowed WRITE, future `+1` | [middle-stage-closeup.png](middle-stage-closeup.png) |
| Completed READ and WRITE, active EXECUTE | [next-stage-closeup.png](next-stage-closeup.png) |
| Stalled active operation with red outline | [stalled-closeup.png](stalled-closeup.png) |
| All operations complete and compact panel | [complete-closeup.png](complete-closeup.png) |

The renders are Editor previews, not a Play Mode test. In Play Mode, review `inspect c1`, `inspect c3`, and `inspect c1 c3` at 16:9. Check the terminal column at gameplay scale, orange and green operation outlines, stage changes, completed history height, active progress and ETA, command focus, and network label crowding. A red outline can be reviewed by setting DB baseline LOAD to its capacity in a temporary scene copy.
