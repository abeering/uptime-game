# Connections — Palette Experiment (PR #139)

Presentation refinement on the existing Business Importance V0 branch. No gameplay, importance, allocation, recipe, traffic, routing, timing or socket-packing changes. The shared renderer, progress fill, heartbeat, LOAD density and activity movement retain their current owners.

## Two independent live selectors

Select **Assets/Prefabs/Connections/Display/DisplayStyle.asset → Live palette experiment** in the Inspector. This exact asset is referenced by **SimulationHost → Connection Gameplay Display → Style** in both canonical scenes and **Connection Display Lab → Connection Display Lab → Style** in the lab. Click the reference to select the same asset. Defaults are **Ordinary Identity Palette: Muted Hues** and **Performance Color Treatment: Uniform**.

| Setting | Mode | Behavior |
| --- | --- | --- |
| Ordinary Identity Palette | Uniform | Every ordinary lifetime slot uses one neutral `(0.68, 0.72, 0.75)` identity. Slots remain owned and are not reassigned. |
| | Muted Hues | Exact pre-experiment six ordinary colors, unchanged. |
| | Grayscale | Six achromatic shades: 0.53, 0.61, 0.69, 0.77, 0.85, 0.93 in each RGB channel. Their opaque identity accents contrast with the dark network/pill background; resolved/departing opacity still follows existing behavior. |
| Performance Color Treatment | Blended | Existing identity-to-warning interpolation and pulse, using whichever identity palette is selected. |
| | Uniform | One shared neutral rail base `(0.65, 0.70, 0.74)` at zero danger, independent of family/slot; existing continuous orange/red/pink danger targets and pulse follow. No individual identity hue participates in interpolation. |

All six combinations work in canonical gameplay and the Display Lab. IMPORTANT **identity** stays in its exact bright cyan/teal family in every ordinary-palette mode. Uniform Performance deliberately gives ordinary and IMPORTANT rails the same color at equivalent danger/pulse/departure state; their pills retain their respective identities. Rail-colored body fill and socket follow the same shared rail treatment. Activity colors/geometry, WORK fill, danger calculation, warning target colors, departure targets and opacity remain unchanged. A slow but safe connection still communicates disruption through activity; this pass adds no separate slowdown-color predicate.

The selectors are read during normal rendering. Change either during Play Mode: no restart, new logical connection, view recreation or lifetime-slot reassignment is needed. A paused gameplay clock can be used for a direct frozen-state comparison; normal running gameplay also updates live. Switching back to Muted Hues restores each connection's existing slot color.

The terminal correction extends the existing identity-pill component to follow the authoritative state/palette. Active inspect, LOAD, WORK and frozen/resolved/collapsed history update live, including pills whose parent `InspectOutput` component has stopped refreshing. WORK row identity text also refreshes through the same source. This changes color only; it does not rebind IDs, reset alias emphasis, rewrite progress or alter history accounting. Old unbound/legacy pills keep their existing behavior.

Play Mode edits to a shared ScriptableObject may persist in Unity. Restore **Muted Hues + Uniform** after comparing if you want the authored review baseline; no scene save or fixture regeneration is required. The existing `Run Business Importance Witness` and historical authored-fixture commands can replace scene state, so do not run them during a normal gameplay palette experiment.

## Deferred presentation / investigative fairness ideas

- INSPECT could temporarily emphasize the connection's entire physical route, with naturally decaying emphasis, as an on-demand aid to spatial comprehension rather than another persistent layer.
- A player-controlled TAG command could provide persistent investigative memory for connections already considered. Exact semantics and presentation remain undesigned.

Both ideas must respect limited network-view visual bandwidth. Neither is implemented. Existing deferred API preference/effectiveness visibility, importance trust escalation and inspect WORK-bar deadline checkpoint remain unchanged.

[Comparison evidence, exact checks and manual review](../Review/ConnectionsBusinessImportanceV0/PaletteExperiment/README.md).
