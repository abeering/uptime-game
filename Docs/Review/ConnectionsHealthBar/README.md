# Connections Health bar — authored prefab preview

`health-bar-states.png` is an 850×420 Unity 6000.3.11f1 Editor render of five instances of the authored `NetworkHealthPanel` prefab, at 1.3× canvas scale. It was captured in an isolated project copy with explicit Health snapshots. It is a presentation preview, not interactive Play Mode evidence.

Rows, top to bottom:

- Stable Health 100: teal, full width.
- Health 80 draining at −11.11/s: muted orange, with clearance for a signed two-digit rate.
- Health 90 immediately after a loss from 100, already recovering at +0.35/s: orange, with width partway through its eased drop (70 ms).
- Health 20 draining: red.
- Health 20 recovering: still red.

The bar is 18 scene units high and 40 units wider than before this presentation change. Its margins balance `HEALTH 100` against reserved `-11.11/s` text; the rate remains right-aligned at its previous position. Health rules and tuning are unchanged. Color and width smoothing belong to `NetworkHealthPanel.cs`; geometry, colors, the critical threshold, and lerp speed are serialized in `Assets/Prefabs/Connections/NetworkHealthPanel.prefab`.

The expanded Network Health Witness passed the layout, color priority, repeated-loss animation, convergence, recovery, configurable threshold, frame-subdivision, and snapping checks, along with its existing Health, command, and history-layout checks. The numeric Health stays immediate; only bar width is smoothed in Play Mode. Unscaled time lets animation settle while simulation time is paused.

For manual review, play `Connections.unity` at 16:9. Check a benign kill or cancellation, repeated penalties, sustained congestion, recovery, and crossing the critical threshold in both directions. Check compact and TAB-expanded layouts, long `inspect` history and input focus, and multi-connection label crowding. The state preview does not validate OS input or live Play Mode behavior.
