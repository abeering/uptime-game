# Connections — Connection Visual Integration V0

Connections presentation only, November 25, 2026. The canonical Small and Advanced scenes adopt the accepted [Display Lab](connections-display-lab-v0.md) second pass. The lab remains the permanent synthetic development environment for the same connection visuals. Packet and historical experimental scene assets remain unchanged.

## Integrated prerequisite baseline

The user explicitly authorized normal GitHub merges in order: #128 (`6620f882`), #129 (`52f9b771`), #130 (`8391df0a`). Both reliability design sections and both implementation records were preserved. Project Core was regenerated from reconciled source documents at each overlap, never manually merged.

Before the feature branch was created, Unity 6000.3.11f1 compiled integrated main `8391df0a5da5901c15571faffc5ca1ff92a84344`. Fragility's 126 assertions, the lab's 498 assertions, canonical parity's 883 checks and the broad existing regression suite ran. The two inherited UI failures reproduced: Advanced Overclock compact tool row and Resource inspect-header pill geometry/color. See [baseline and final evidence](../Review/ConnectionsVisualIntegrationV0/README.md). Experimental PRs #115/#124/#125/#126 were not modified or merged.

## Shared assets and composition

- `ConnectionDisplay.prefab` remains the lab view. Gameplay's small `GameplayConnection.prefab` instantiates its **exact authored RailsAndActivity component**, rather than copying its geometry or renderer. Shared rail-asset changes propagate to both scenes and the lab.
- `DisplayIdentityPill.prefab`, including its progress graphic, TMP identity, material and shader, is used directly by gameplay's existing `IdentityPresenter`. Its initial 62×32 size and 20-point identity follow the accepted lab; authored RectTransform/TMP settings remain the source for future changes, rather than legacy size/font fields. The lab view and gameplay packing call the same pill-state binding, with one opacity owner per composition. The existing solver reads this size; lane ordering, placement, clearance and cycling retain their owners. Packed poses use the overlay's center anchor; lab racks keep their authored anchors.
- `ConnectionDisplayView`, `ConnectionDisplayGraphic`, `ConnectionHeartbeat`, `DisplayStyle.asset` and the progress shader are shared. The view permits separate pill/marker layers; it does not require gameplay to adopt the lab sample hierarchy.
- `LogicalPathView` keeps route geometry, progressive reveal, layout interpolation and sockets. It supplies segments/widths to the shared renderer. Probe clips the same mesh in its existing scanner frame. Payload, Worm, physical links and Integrity presentation retain their existing separate renderers.
- The older path/pill assets remain for historical scenes, terminal headers and the legacy geometry-preview/study tools. Canonical ordinary connections do not instantiate a parallel legacy path renderer. Existing identity-palette ownership remains available to inspection; ordinary network rails/pills use the shared neutral palette.

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
