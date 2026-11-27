# Connections — Connection Visual Integration V0

**Current #139 presentation supersedes the previous palette experiment:** [One historical teal/cyan palette for all connections, inline IMPORTANT `!`, bright identity text, and identity-colored nominal rails with Blended/Uniform warnings](connections-identity-marker-v0.md). Importance mechanics and shared identity ownership remain; earlier descriptions below preserve their phase provenance.

**PR #139 live palette refinement:** [Two independent presentation selectors](connections-palette-experiment-v0.md) now support ordinary Uniform/Muted Hues/Grayscale and Blended/Uniform Performance. Review defaults are Muted Hues + Uniform Performance. IMPORTANT identity remains bright; terminal/history pills follow live changes. Earlier descriptions below retain their phase provenance.

Connections presentation only, November 25, 2026. The canonical Small and Advanced scenes adopt the accepted [Display Lab](connections-display-lab-v0.md) second pass. The lab remains the permanent synthetic development environment for the same connection visuals. Packet and historical experimental scene assets remain unchanged.

**Business Importance V0 follow-up:** [IMPORTANT requests](connections-business-importance-v0.md) adopt the former synthetic Priority family and correct terminal identity consistency through the shared style and lifetime slot. Original integration descriptions below retain their phase provenance; gameplay is no longer always Ordinary, and the old independently colored inspection palette is retired. Current ordinary colors, heartbeat and danger semantics remain.

## Integrated prerequisite baseline

The user explicitly authorized normal GitHub merges in order: #128 (`6620f882`), #129 (`52f9b771`), #130 (`8391df0a`). Both reliability design sections and both implementation records were preserved. Project Core was regenerated from reconciled source documents at each overlap, never manually merged.

Before the feature branch was created, Unity 6000.3.11f1 compiled integrated main `8391df0a5da5901c15571faffc5ca1ff92a84344`. Fragility's 126 assertions, the lab's 498 assertions, canonical parity's 883 checks and the broad existing regression suite ran. The two inherited UI failures reproduced: Advanced Overclock compact tool row and Resource inspect-header pill geometry/color. See [baseline and final evidence](../Review/ConnectionsVisualIntegrationV0/README.md). Experimental PRs #115/#124/#125/#126 were not modified or merged.

## Shared assets and composition

- `ConnectionDisplay.prefab` remains the lab view. Gameplay's small `GameplayConnection.prefab` instantiates its **exact authored RailsAndActivity component**, rather than copying its geometry or renderer. Shared rail-asset changes propagate to both scenes and the lab.
- `DisplayIdentityPill.prefab`, including its progress graphic, TMP identity, material and shader, is used directly by gameplay's existing `IdentityPresenter`. Its initial 62×32 size and 20-point identity follow the accepted lab; authored RectTransform/TMP settings remain the source for future changes, rather than legacy size/font fields. The lab view and gameplay packing call the same pill-state binding, with one opacity owner per composition. The existing solver reads this size; lane ordering, placement, clearance and cycling retain their owners. Packed poses use the overlay's center anchor; lab racks keep their authored anchors.
- `ConnectionDisplayView`, `ConnectionDisplayGraphic`, `ConnectionHeartbeat`, `DisplayStyle.asset` and the progress shader are shared. The view permits separate pill/marker layers; it does not require gameplay to adopt the lab sample hierarchy.
- `LogicalPathView` keeps route geometry, progressive reveal, layout interpolation and sockets. It supplies segments/widths to the shared renderer. Probe clips the same mesh in its existing scanner frame. Payload, Worm, physical links and Integrity presentation retain their existing separate renderers.
- The older path/pill assets remain for historical scenes, terminal headers and the legacy geometry-preview/study tools. Canonical ordinary connections do not instantiate a parallel legacy path renderer. Existing identity-palette ownership remains available to inspection. The initial uniform neutral network palette is superseded by the corrective identity families below.

The lab authoring command now rebuilds only synthetic scene fixtures. Shared prefabs, style, shader and material must already exist; missing assets cause a clear error. It neither regenerates them nor saves unrelated dirty assets. The separate one-time integration authoring utility establishes composition and canonical bindings, and refuses to run again once the gameplay prefab exists. Normal lab experimentation edits the shared assets directly through the Inspector. [Authoring safety checks](../Review/ConnectionsVisualIntegrationV0/authoring-safety-checks.txt) verify fixture regeneration leaves shared assets and both canonical scene files unchanged.

## Gameplay adapter and clock

`ConnectionGameplayDisplay` reads model facts and supplies `ConnectionDisplayState`. It never advances simulation, writes WORK/LOAD, classifies hidden intent or enforces service deadlines.

| Display input | Gameplay source |
|---|---|
| Identity | `KnownIdentity`, preserving aliases, hopping, Probe revelation and existing history lookup |
| Class | Ordinary for every gameplay connection; no invented priority/hostility color |
| Operation | Current operation kind |
| Density | Effective requested LOAD, independent of allocation/performance |
| Productive speed | Actual WORK rate / effective requested LOAD, nonnegative; zero when not demanding |
| Interior fill | Actual completed WORK / effective current-operation requirement; retained termination requirement for resolved service |
| Danger | Authoritative Fragility service remaining time and current-rate projection, with the cosmetic mapping below |
| Resolution | Actual Completed/Killed/TimedOut/Canceled/Withdrawn state and visual departure age |

Full requested LOAD with a zero WORK-rate multiplier produces zero movement. Boosted WORK factors can produce faster activity; allocation satisfaction alone cannot claim productive progress. Operation transitions reset actual fill and hold activity until the next operation starts. No lifetime fraction, elapsed service time or guessed completion is substituted for WORK.

Gameplay uses one externally driven heartbeat with the accepted 120 BPM, 18-unit nominal step, 58% move/42% hold and SmoothStep defaults. Its scaled frame time matches gameplay's time basis. It freezes when the host is disabled, timeScale pauses, CORE ends, or Duel's existing setting pauses simulation. CLEAN and default continuing-simulation Duels leave it running. Restart resets phase and replaces the old connection views. The independent lab retains its unscaled clock and pause/replay controls. Other existing UI/Probe animations keep their existing time semantics.

## Initial service-danger mapping

This is tunable cosmetic presentation, not a new authoritative risk predicate. Fragility's `AtRisk`, deadline enforcement, hostile service-information contract, verbose inspect explanations and business-failure receipts are unchanged. The mapping never reads `IsHostile`.

Let `R` be authoritative remaining service time and `P` the authoritative current-rate-plus-nominal-future projection. Let `N` estimate normal remaining service: remaining effective WORK of the current operation divided by its authored LOAD at 1× productivity, plus authored WORK/LOAD of future operations. A held transition uses the current operation's authored nominal duration. This explicitly ignores future modifiers, capacity changes, Overclock expiry and future interventions. It does not claim that a normal-rate deficit makes rescue impossible.

Style defaults:

- Soft band `B = max(0.75s, 0.05 × N)`.
- Current pressure `C = clamp01((B − (R − P)) / (2B))`; infinite current projection gives `C = 1`.
- Recovery opportunity scale `H = max(B, 0.5 × N)`.
- Urgency `U = 1 − clamp01((R − N) / H)`.
- Severity `S = C × lerp(0.3, 1, U)`.

If `N` is infinite, the finite fixed service budget is used for the soft-band/opportunity scales. A normal-rate deficit is `N > R`; its pink warning contribution grows with `SmoothStep(clamp01((N − R)/B))`. Authored lab flags retain their accepted full pink treatment. This continuous band/blend avoids an abrupt color switch from small LOAD fluctuations. There is no temporal smoothing or delayed authoritative resolution; useful recovery updates immediately, and actual timeout immediately selects the timeout treatment.

Slowed but safe service can stay neutral. A newly stalled service with ample normal-rate headroom begins with modest concern. Danger increases as headroom disappears. Full-speed late service retains full-speed activity with high danger. Current productive speed never derives from severity.

## Lifecycle and retained surfaces

Established rails stay continuous at every speed/risk, including joined bends and width changes. A small outer antialiasing support floor helps subpixel crowded rails without changing core packing widths, activity geometry or normal lab-width rails. Very thin diagonal contrast remains a human-review concern. Connecting paths reveal only the model's reached portion, with no productive activity or premature destination socket. An interruption preserves that revealed prefix.

Completed service fades over 0.8s. Kill and cancellation use the accepted two-flash treatment followed by a short fade (0.83s total at defaults); timeout holds warm warning for 0.25s then fades over 1.1s. Hostile withdrawal uses normal departure fading while retaining actual partial WORK. Blackout/isolation cancellation never looks like a completed operation. The shared cosmetic clock supplies one departure age to path, packed IDs and external sockets. Shared departure curves fade them together. Only visual lanes/IDs remain until retirement; models are already resolved, LOAD released and receipts/history authoritative.

Redundant `AT RISK` text is removed from the canonical network pill. Explicit service timing, projection/risk explanations, timeout history/collapsed summaries and the business-failure count remain. Terminal headers/historical scene assets retain their existing presentation. Aliases and Probe ownership are unchanged; the adapter never substitutes a concealed canonical ID.

## Validation and review

[Review guide](../Review/ConnectionsVisualIntegrationV0/README.md) records actual compilation, deterministic/regression results, Play Mode captures at both resolutions, motion, profiling scope and human-review limitations. Controlled Fragility fixtures remain disposable in-memory tuning; canonical traffic scheduling is unchanged. Ordinary generated gameplay is sampled separately. No natural-congestion or balance verdict is established.

## Corrective visual pass — identity ribbons

Human gameplay review accepted the shared architecture/mechanics and requested stronger ribbon identity with subordinate activity. This presentation-only correction keeps heartbeat timing, density/direction, danger calculation, geometry/packing, shaders/materials and gameplay untouched. Both canonical scene assets are unchanged by this correction.

`ConnectionIdentityPalette.AssignSlot` retains the existing six-slot, lifetime-owned rotating assignment and release/reset behavior. Its original `Assign` still returns the old inspection accents. `LogicalPathPresenter` supplies that same slot to the shared display DTO; aliases, hopping and Probe never recompute it. Every segment, interior-progress pill and destination socket derives color from the shared style. Actual gameplay remains Ordinary; Priority is synthetic only.

Initial shared sRGB component values (0–1):

| Slot | Ordinary neutral | Priority (previous blue/teal family) |
|---|---|---|
| 0 | blue-grey `(0.62, 0.71, 0.78)` | cyan `(0.36, 0.87, 0.91)` |
| 1 | green-grey `(0.66, 0.74, 0.66)` | teal `(0.35, 0.91, 0.76)` |
| 2 | warm grey `(0.78, 0.71, 0.64)` | blue-green `(0.43, 0.82, 0.92)` |
| 3 | slate `(0.59, 0.67, 0.72)` | seafoam `(0.43, 0.89, 0.79)` |
| 4 | violet-grey `(0.71, 0.65, 0.76)` | turquoise `(0.31, 0.85, 0.84)` |
| 5 | mint-grey `(0.62, 0.75, 0.72)` | cool mint `(0.49, 0.87, 0.75)` |

`DisplayStyle.asset` owns both arrays, `bodyFillOpacity = 0.34`, ordinary activity RGB `(0.68, 0.74, 0.77)`, priority activity RGB `(0.53, 0.79, 0.85)`, `activityOpacity = 0.60`, and `stalledActivityOpacityMultiplier = 0.70` (stationary alpha 0.42). Body vertices use the **resolved rail RGB at the same heartbeat instant**, including danger pulse, budget deficit and departure colors, with alpha 0.34. This is a visible translucent fill over the scene, rather than the earlier dark opaque class body. The same miter/width/progressive-reveal/Probe/departure mesh paths apply. No decorative objects or new materials are added.

Activity geometry remains 9 units long and half-width fraction 0.5 plus its existing feather. Only contrast/opacity change. Stepped movement, slowdown missed beats, EXECUTE breathing, requested-LOAD spacing and operation directions are unchanged. ID text, typography and packing are unchanged; the existing dark WORK fill (identity RGB ×0.38 in the shared shader) stays below the bright text. Pill borders retain **base identity** while danger increasingly takes over the ribbon/socket, so identity remains available even at maximum warning. Severe danger deliberately prioritizes status on the route; no new danger formula or smoothing is added.

The existing lab samples now use explicit synthetic identity slots, all six ordinary variants, and several priority examples. The paired progress racks retain interior/perimeter treatments and matched ordinary slot colors. Updated actual Play Mode palette plates show all six colors of each family using the same existing rows; those comparison overrides are disposable capture state. Lab fixture authoring remains protected against shared-asset overwrite.

## Destination socket refinement

The final focused refinement makes the destination a first-order visual feature without changing route placement or activity direction. The shared `ConnectionDisplay.prefab` authors a **32-unit backing, 28-unit ring, 3.5-unit ring weight and 7-unit fully opaque identity/status dot**, with darker backing RGB `(0.006, 0.016, 0.021)`. These nested marker overrides serve both canonical scenes and the lab. The base legacy `TerminalMarker.prefab` remains unchanged. The existing two-unit endpoint outset and boundary intersection formula are retained.

Previously the shared lab composition overrode the backing to 18 units, while the marker component still preferred 26. The initial-size fast path could leave that smaller authored rectangle in place even without packing pressure. Packing also capped a destination by every lane on its final physical link, including transit routes, and imposed a hard 12-unit minimum. The shared marker now synchronizes its authored dimensions on activation. Only actual visible destination sockets compete: immediately before Canvas layout/render, the existing presenter measures boundary-center distances and caps each backing to `min(32, nearest center distance − 3 units)`, clamped at zero. Pairs retain at least three units between their circular backings whenever feasible. Transit ribbons do not shrink a solitary destination socket. Departing sockets continue to reserve their visible footprint until retirement.

Contraction to available space is immediate; expansion retains the existing relayout easing. Sizing occurs after path relayout updates and before Canvas geometry, so animated endpoint positions are respected. No lane/route/identity packing or socket center is moved, and no new GameObjects/materials or decorative effects are introduced. Extreme crowding can reduce diameter below the old 12-unit floor rather than forcing overlap. Compact ring weight is capped at 18% of its current ring diameter, preserving the dark annulus instead of filling the bullseye center. Ring, backing and dot stay within the available diameter; coloration still follows the connection's resolved identity/status and shared departure opacity. This behavior is enabled only on the shared display's marker, preserving legacy scene/attack presentation.

[Focused size, placement and before/after evidence](../Review/ConnectionsVisualIntegrationV0/SocketRefinement/README.md).

## Deferred design concerns — no implementation in this pass

- **Ordinary identity palette:** the current muted hues may accidentally suggest mechanical traffic categories. A future visual pass may prefer a narrower family of tinted slate greys, reserving stronger blue/teal variation for IMPORTANT traffic. This is a design concern to revisit, not an approved palette change; current colors and identity slots are unchanged by the socket refinement.
- **Inspect deadline checkpoint:** retain the idea of a moving deadline checkpoint on the existing WORK progress bar, potentially replacing the two additional SERVICE diagnostic lines. Its semantics and operation-arc behavior still require design. No checkpoint or inspect-panel change is implemented here; existing service timing/explanations remain.
