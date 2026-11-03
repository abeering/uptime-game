# Connections — Global Tool Readiness Strip V0

**Scope:** Connections presentation on the Probe X-ray V0 branch. The authored 52-unit top bar in `Connections.unity` and `ConnectionsAdvanced.unity` now shows one passive, fixed-width rectangle each for global Probe and Scan. Labels and horizontal slot containers are authored in `NetworkHealthPanel.prefab`. The containers size from their contents so a later additional slot can extend a group leftward without changing the current one-slot mechanics. The clock, Health value, Health bar, signed rate, terminal, and network retain their existing positions.

The strip reads `SimulationHost` each render. Probe shows `ACTIVE Ns` while deployed using its actual expiry, then the remaining cooldown from its activation-origin deadline, then `READY`. Scan shows `ACTIVE Ns` from its active session's scheduled completion time and returns directly to `READY` when the slot is released. Positive remaining times round up to whole seconds; active/cooling states never display `READY` early. Without a simulation step, the numbers do not move. Reset updates both displays from the cleared host state. Colors are serialized muted green, teal, and amber; rectangles and text ignore raycasts.

This slice does not change tool durations, availability, commands, Scan result timing, Probe classification, or other gameplay. It does not add charges, node-specific cooldowns, icons, or controls.

## Validation and review

`ToolReadinessWitness` checks both scene bindings, the authored 52-unit layout and slot groups, passive fixed-width indicators, initial state, simultaneous activation, fractional countdown rounding, Scan completion, Probe expiry/cooldown/readiness, stationary renders without simulation progress, and reset. The existing Network Health, Scan, and Probe witnesses remain relevant. [Editor preview captures](../Review/ConnectionsToolReadinessV0/README.md) show all three display states in both scenes at 1280×720 and 1920×1080. They are deterministic Editor renders, not interactive Play Mode evidence.

In Play Mode at both resolutions, check text clarity and right alignment during simultaneous use, the exact Scan completion and Probe expiry transitions, a stopped simulation clock, reset, terminal command focus and scrolling, and window/notification activity beneath the fixed bar.
