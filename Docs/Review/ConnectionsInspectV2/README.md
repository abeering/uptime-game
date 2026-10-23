# Inspect V2 Editor review

These 1920×1080 images were rendered by `Tools/Connections/Capture Inspect V2 Review` in Unity 6000.3.11f1. The capture opens `Connections.unity`, advances deterministic connection states, and instantiates the authored `InspectOutput` prefab in a 650-pixel terminal-column preview. The dark area at right represents the network's share of the 16:9 frame; it is a backdrop, not a captured network view. The comparison arranges two consecutive two-ID compact outputs. The capture does not save scene changes.

| View | Image |
| --- | --- |
| Single-operation compact | [compact-single-1920x1080.png](compact-single-1920x1080.png) |
| Single-operation verbose | [verbose-single-1920x1080.png](verbose-single-1920x1080.png) |
| Six-operation middle stage, compact | [compact-long-1920x1080.png](compact-long-1920x1080.png) |
| Six-operation middle stage, verbose | [verbose-long-1920x1080.png](verbose-long-1920x1080.png) |
| Four compact cards from two consecutive commands | [compact-consecutive-1920x1080.png](compact-consecutive-1920x1080.png) |

In interactive Play Mode at 16:9, inspect a generated single-operation connection with `inspect <id>` and `inspect -v <id>`, use `spawn delayed-threat` to watch the READ-to-WRITE transition, and issue two consecutive `inspect <id> <id>` commands. Check pill crowding, badge alignment, live LOAD changes, terminal scroll, and command focus. These Editor renders do not establish Play Mode behavior.
