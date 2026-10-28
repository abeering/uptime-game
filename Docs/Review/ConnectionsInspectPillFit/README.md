# Connections inspect pills — fitted padding

These 1920×1080 images were rendered in Unity 6000.3.11f1 from an isolated project copy using the authored `InspectOutput` and `InspectOperationToken` prefabs. The review layout uses an approximately one-third-width terminal column; the right side is preview background. They are Editor compositions, not interactive Play Mode screenshots.

- `verbose-single-1920x1080.png`: an active operation and a fitted `LOAD 2.0` badge.
- `compact-multiple-completed-1920x1080.png`: active operations, completed/future count pills, and a completed `✓ READ → DB` with `LOAD 0.0`.
- `verbose-multiple-completed-1920x1080.png`: active and completed cards in verbose output.

All timeline and LOAD pills use 12-unit outer horizontal padding, centered text, and the existing 40-unit height. Completed pills add the checkmark and its 6-unit gap inside those outer margins. The LOAD badge remains right-aligned and measures its current label instead of retaining a fixed width. LOAD always displays one decimal; WORK formatting and simulation state are unchanged.

The existing Inspect V2 Witness passed command parsing, multi-ID compact/verbose output, live LOAD changes, narrow and extreme widths, completed/killed/canceled states, uniform padding, and the `1.9 → 2.0 → 2.1` width check. Capture reused the existing review composition in a temporary script, with near-zero connection travel to isolate presentation; no capture harness was added to the repository.

In interactive Play Mode at 16:9, inspect several IDs in compact and verbose modes. Watch LOAD cross whole numbers and the one-/two-digit boundary, advance operations, and inspect completed and killed connections. Confirm equal-looking pill margins, centered checkmark/text groups, legible counts, no badge/timeline overlap, terminal scrolling/input focus, and multi-connection label crowding. Interactive review remains outstanding.
