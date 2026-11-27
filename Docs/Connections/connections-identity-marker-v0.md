# Connections — shared identity palette and IMPORTANT marker (PR #139)

Current presentation direction supersedes the earlier importance-family and three-palette experiments. Mechanics remain Business Importance V0: 15% seeded legitimate assignment, fixed request importance and configured failure cost, exact-once whole-job accounting, ordinary business WORK, independent API preference, routing and service budgets.

## One identity palette

All connections use the exact original six teal/cyan slot colors from historical `52f9b771`, regardless of importance, claim or intent. `DisplayStyle.asset → Identity Colors` owns the array. `ConnectionIdentityPalette` retains lifetime slots/history and delegates to that shared asset. There is no separate ordinary/IMPORTANT palette or ordinary Uniform/Muted Hues/Grayscale selector. Activity color is also shared, removing its former importance distinction; density, movement, easing, operation direction and stationary stalls are unchanged.

The colors are `(0.36,0.87,0.91)`, `(0.35,0.91,0.76)`, `(0.43,0.82,0.92)`, `(0.43,0.89,0.79)`, `(0.31,0.85,0.84)`, `(0.49,0.87,0.75)`. Network/terminal identity ownership and aliases remain unchanged.

## IMPORTANT inside the pill

IMPORTANT renders as `i0!`; ordinary renders as `o0`. The `!` is inline in the existing authored TMP label, with no badge, extra GameObject or packing dimension. It uses 75% text size so the large terminal-header three-character label fits the existing authored padding while the identifier keeps its original font size. It rotates with its network pill. The existing shared progress graphic/shader and the WORK fill beneath the text remain unchanged.

Both identifier and marker use one consistently bright authored pale-white text color `(0.85,0.97,0.95)`, independent of identity hue, allocation, slowdown or danger. Existing departure opacity and brief alias border emphasis remain. The marker uses authoritative importance from model/DTO binding, not inferred color or claim. Network, compact/verbose inspect, full resolved/collapsed inspect, LOAD, WORK and LOGS pills participate. LOGS retains its verified canonical-ID semantics; its header now follows the same identity color/importance binding as other terminal pills while resolved log text/chrome may remain quiet.

`IdentityPillView.DisplayedId` continues to return the unadorned identifier. The suffix is not part of the address: type `inspect i0`, `kill i0`, etc. Alias changes preserve the importance marker without changing command lookup. Text formatting does not reset progress or recreate views.

## Live warning comparison

In Unity select **Assets/Prefabs/Connections/Display/DisplayStyle.asset → Live performance color comparison → Performance Color Treatment**. Both canonical scenes and the Display Lab reference this exact asset.

- **Blended:** retains identity-to-warning interpolation, now using the narrow historical palette for every connection.
- **Uniform:** zero-danger/nominal rails retain their own identity color. Positive danger uses the existing common orange/red/pink warning progression directly, independent of identity, with the existing pulse. Warning onset changes to the warm family rather than fading through identity hues; subsequent severity progression remains continuous within warning colors.

Uniform remains the authored default; both modes change live without restart or view recreation. Existing warning targets, severity calculation, killed/timed-out departures, body opacity and socket geometry/packing stay intact. Rail-colored body/socket continue following the selected treatment. Full-speed endangered connections still move at full speed; slowed but safe connections retain disrupted activity. No new mechanical slowdown predicate or decorative effect is introduced.

## Review

The Display Lab uses the same palette and inline marker, including representative IMPORTANT nominal/slow/danger/traversal samples. Its diagnostic subtitle reflects the new convention; layout, geometry, assets and interior/perimeter progress rack remain. Temporary screenshot overrides demonstrate IMPORTANT labels at 0–100% WORK without modifying production recipes.

[Focused checks, screenshots and manual review](../Review/ConnectionsBusinessImportanceV0/IdentityMarker/README.md). Earlier #139 palette/importance-family screenshots remain labeled phase history. No gameplay scene layout or tuning changes accompany this pass. INSPECT route emphasis, TAG investigative memory and the inspect WORK-bar deadline checkpoint remain deferred.
