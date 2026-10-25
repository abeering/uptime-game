# Connections terminal and inspect cleanup — Editor previews

These six 1920×1080 images were rendered by `Tools/Connections/Capture Terminal Inspect Cleanup Review` in Unity 6000.3.11f1 from an isolated project copy. They use the authored `InspectOutput` prefab and deterministic Connections simulation states. The left column is a review canvas at the scene's approximate terminal width; the dark right area is only preview background. These are **Editor previews**, not interactive Play Mode or a screenshot of the full `Connections.unity` topology.

The refreshed cards have no horizontal TitleBar strip behind the purpose. Verbose status shows performance and any contention reason without an `ALLOC` fraction; the separate LOAD pill remains.

- `compact-single-1920x1080.png` and `verbose-single-1920x1080.png`: one active connection.
- `compact-long-1920x1080.png` and `verbose-long-1920x1080.png`: a multi-operation connection with completed and future stages.
- `compact-multiple-completed-1920x1080.png`: five IDs in one result, including completed `e5` with muted identity, checked operation, and `LOAD 0`.
- `verbose-multiple-completed-1920x1080.png`: active `a1` and completed `e5` in one detailed result.

In interactive Play Mode at 16:9, review `inspect` and `inspect -v` under live traffic, multi-connection label crowding, scroll position, and input focus after inspect and multi-kill commands. The Editor previews do not establish those interactions.
