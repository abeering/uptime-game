# Connections — Connection Display Lab V0

**Scope:** an isolated presentation lab and reusable renderer candidate. Open `Assets/Scenes/ConnectionsDisplayLab.unity`. The canonical gameplay scenes and their rendering/simulation code are unchanged. This does not adopt the new visuals into gameplay or settle final art direction.

**Repository posture:** built independently from main `802a98e25209ef4c81e6ac90ef329a5382f1f6bc`. The user has accepted Fragility V0 mechanically, with natural congestion tuning deferred. At task start [#129](https://github.com/abeering/uptime-game/pull/129) remained open/unmerged, as did the broader milestone proposal [#128](https://github.com/abeering/uptime-game/pull/128). Neither PR was changed or merged. The lab has no runtime dependency on #129: all render inputs are synthetic. An eventual gameplay adapter must use the then-current accepted lifecycle/performance/service state and receive separate approval for integration.

## Scene and controls

The scene opens directly into four comparative columns at a 1920×1080 reference resolution, scaling to 1280×720:

- **A — Operation / LOAD:** ordinary READ at low/high LOAD, WRITE and local EXECUTE, priority READ and WRITE. Two static progress racks compare interior and perimeter at 0%, 25%, 50%, 75% and 99% WORK.
- **B — Performance:** full speed, moderate slowdown, heavy slowdown, stalled but safe for now, slowed priority, plus a joined three-segment path whose width changes 10→16→10. That path alternates READ/WRITE operations, resetting current-operation WORK without rerouting.
- **C — Deadline risk:** full-speed safe/behind/severely endangered, slowed behind/severely endangered, stalled imminent timeout, and full-speed service exceeding its normal-rate budget. Current/normal estimates and budget are separately labeled synthetic inputs.
- **D — Transitions / outcomes:** healthy→slowed→healthy; safe→behind→increasing danger→timeout; slowed danger→full-speed danger→safe; slowed danger→full speed but exceeding normal-rate budget; killed, timed out and naturally completed departures. All repeat. These rows use animated perimeter progress; A–C use interior progress.

**PAUSE / RESUME** freezes only the presentation clock; **REPLAY** resets the heartbeat and every transition, preserving pause state. Both are authored buttons with persistent bindings. No gameplay commands or simulation services are added. Static risk comparisons intentionally hold their WORK/timing inputs so severity can be compared; A/B and transition rows animate WORK at the supplied productive rate. Fixed progress racks never count elapsed time as WORK.

Inspector controls:

| Location | Default / purpose |
|---|---|
| `ConnectionHeartbeat` on the Canvas | 120 BPM; 18 UI-unit steps; 0.58 moving fraction, remainder holding; SmoothStep easing; pause toggle |
| Each `ConnectionDisplayView` | shared phase 0 and subdivision 1; optional phase/subdivision exploration, pill position and progress treatment |
| `DisplayStyle.asset` | ordinary greys, priority blues, warm danger colors, rail thickness 1.7, density/shape/departure tuning |
| Each `ConnectionDisplayLabSample` | authored phases, durations, independent render state, nominal WORK seconds, static timing diagnostics and optional severity ramp |

Changes to BPM preserve accumulated heartbeat phase during ordinary playback. Linear and SmootherStep easing are also available. The clock uses unscaled time and never reads or advances `SimulationHost`.

## Initial mappings — exploratory

**Heartbeat and speed.** Healthy activity advances to discrete positions on each common beat, eases during the first 58% of that beat and then holds. Moderate speed uses every second beat; severe slowdown uses every fourth; zero speed holds still. Step distance compensates for skipped beats so average travel remains proportional to supplied productive speed. At 120 BPM and 18 units/beat, full speed averages 36 units/second. The 0.65× example advances 23.4 units every two beats; 0.22× advances 15.84 every four. Offsets/subdivisions default to a coherent common tempo.

**LOAD and operation.** Spacing is `max(22, 78 / (1 + 0.22 × max(0, LOAD - 1)))`, independent of speed. READ travels toward the origin, WRITE toward the target, using the existing `LogicalPathView.DirectionFor` semantics. EXECUTE expands/contracts symmetrically in place on alternating shared beats, with the same move/hold easing. Its centers do not scroll. Glyphs are provisional rectangular activity marks, not final operation iconography.

**Persistent rail.** A continuous body and bounded rectangular outline remain visible even with stalled activity. Ordinary traffic uses neutral greys; synthetic priority uses blues. Full-speed edges are solid; slowed edges have increasingly interrupted cadence; stall retains a broken edge and fixed, dim activity. Width is geometry input, not a new LOAD allocation rule. The independent danger treatment is one uniform, full-route warning pulse on the edges and terminal socket, avoiding a sweep that could imply one physical segment caused the danger.

**Danger.** Severity 0 retains class color; increasing supplied severity mixes warm amber/orange/red into the rail and increases pulse depth. `exceedsNormalBudget` selects a pink warning family at the supplied severity. It means the synthetic normal-rate estimate exceeds the supplied budget, not that future interventions cannot change the outcome. Activity travel never reads danger severity. Full-speed severe-risk rows retain full-speed motion.

This is **not** a new authoritative risk calculation. C's 0/.30/.80/.35/.85/.98/1 severity values are authored comparisons, not a proposed finalized formula. In particular C3 has only 0.5 seconds of synthetic normal-rate margin and deliberately uses high visual severity while its estimate is still before the deadline. C7 supplies a true normal-rate budget deficit. D's severity changes/recoveries are scripted presentation fixtures and do not claim a valid simulation forecast or a specific gameplay intervention.

**WORK progress.** Interior fill grows left-to-right behind independent high-contrast TMP identity text. Perimeter fill reveals the rounded border clockwise from twelve o'clock using an angular mask; this is a radial border sweep, not constant arc-length traversal. Both receive the same normalized current-operation WORK input. Animated rows progress at `productiveSpeed / nominalWorkSeconds`; stall stops progress. A new synthetic operation resets that progress. Neither treatment represents service-budget consumption, and neither is chosen as final.

**Outcomes.** Completion fades over 0.8 seconds. Kill reuses the existing two-flash convention (0.12-second half cycles), followed by a short cut/fade. Timeout holds warm warning briefly, then fades over 1.1 seconds. A CanvasGroup fades the display, identity and marker together; the lab's labels remain. Views are reused, not destroyed/recreated for each loop.

Priority is a generic display class only. It does not implement or equate company-assigned business importance with operator-configured processing priority.

## Assets, hierarchy and production seam

New reusable assets live under `Assets/Prefabs/Connections/Display/`:

- `ConnectionDisplay.prefab`: a view, one path/activity mesh, one progress pill and TMP identity, and the existing authored terminal marker.
- `DisplayIdentityPill.prefab`: one SDF progress quad plus TMP identity. No border-segment GameObjects.
- `DisplayLabSample.prefab`: instrumentation labels and a linked display prefab.
- `DisplayStyle.asset`: centralized replaceable styling.

New `ProgressPill.shader` / material live under `Assets/Shaders/ConnectionsDisplay/`. They reuse the existing antialiased pill's rounded-rectangle SDF and UI stencil/clipping conventions, adding per-vertex progress/treatment inputs. There are no per-view material clones. Existing JetBrains Mono font assets, `TerminalMarkerStyle`/marker prefab, ordinary Unity UI infrastructure and operation directions are reused without altering those assets or components. No custom bitmap artwork is required for this pass.

The authored Canvas hierarchy contains 334 GameObjects for 26 path examples, ten comparison pills, labels and controls. An authored camera and EventSystem are separate roots. Each connection has one graphic for **all** its rails and blips. There are no per-blip objects or runtime decorative hierarchy construction. The explicit Editor authoring recipe creates the scene and linked prefabs once; normal Play Mode only binds them.

`ConnectionDisplayState` keeps identity, class, operation, requested LOAD, normalized productive speed, current-operation WORK, danger severity, normal-budget flag and resolution/age separate. `ConnectionDisplayView.Render` binds it. `ConnectionDisplayGraphic.SetGeometry` accepts segment starts/ends/widths in its local coordinate system—the same array contract used by `PathGraphic` and existing packing outputs. Connected segments share bounded miter joins; gaps remain gaps. Activity uses one arc-length phase across all segments and is clipped at segment ends/corners rather than restarting its phase at every bend. Geometry arrays resize only when topology size changes and are cached between animation frames.

This keeps the renderer reusable without changing existing route/lane solvers. Later gameplay integration still needs an adapter, coordinate/identity placement decisions, an agreed danger mapping, appropriate per-connection clock/step semantics during changing rates, and production profiling. The lab does not replace `LogicalPathPresenter`, `IdentityPresenter`, lane packing, physical links or any gameplay prefab. Dynamic target/routing changes are deferred; `SetGeometry` is a rendering seam, not a new routing or lifecycle policy.

## Validation and limitations

[Review guide, actual results, screenshots and motion](../Review/ConnectionsDisplayLabV0/README.md) distinguish deterministic checks, scripted actual Play Mode rendering, and unperformed human review. Compilation/authoring/testing used an isolated Unity 6000.3.11f1 copy because the user's original Editor was open.

The binding-loop allocation measurement excludes Unity internals and PNG/video capture instrumentation; it is not a claim that every engine frame allocates zero bytes. No standalone-player/GPU performance target or final art decision is established. At 720p the small diagnostics are intentionally secondary to labels and rail/motion comparison. The rectangular activity glyphs and small cells are starting points for iteration.

The renderer accumulates signed travel across productive-speed, cadence and operation changes, preserving activity positions while the new motion follows the common heartbeat. Seeking backwards or replaying resets that motion. Inputs are assumed to change when delivered to the view; the exact activity-pattern phase during scripted transitions can depend on sampling cadence. This is cosmetic state and never affects WORK or resource allocation.

To regenerate deliberately: **Tools → Connections → Author Connection Display Lab** outside Play Mode. This overwrites only the lab's own scene/prefabs/style/material paths; do not use it to preserve hand-authored edits to those lab assets. Normal review does not run the authoring command.
