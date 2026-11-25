# Connection Visual Integration V0 — review

Open **Assets/Scenes/Connections.unity** or **Assets/Scenes/ConnectionsAdvanced.unity** in Unity 6000.3.11f1 and enter Play Mode. The assets are already authored; do not run the migration command. The new integration PR remains unmerged for human review.

[Architecture, model mapping, danger formula and clock policy](../../Connections/connections-visual-integration-v0.md) · [Shared Display Lab](../../Connections/connections-display-lab-v0.md).

## Preparation and source boundary

Normal GitHub merges, in the explicitly authorized order: #128 → `6620f882`, #129 → `52f9b771`, #130 → `8391df0a`. Both source design sections and both implementation records were retained; conflicting Core output was regenerated from sources. Integrated main `8391df0a5da5901c15571faffc5ca1ff92a84344` compiled and ran the Fragility/lab/canonical and broad regression checks **before** the fresh feature branch was created. Experimental PRs #115/#124/#125/#126 were untouched.

The adapter reads actual visible identity, effective requested LOAD, productive WORK rate, operation progress and Fragility service information. Both scenes and the lab use the exact same authored rail component, progress-pill prefab, style, shader and material. Separate route and identity layers retain their existing placement owners. Shared pill size/typography come from the authored RectTransform/TMP settings. Original legacy assets remain for historical scenes, geometry-only preview/study tools and terminal headers.

## Actual checks

- Unity 6000.3.11f1 compiled and rendered in an isolated project copy; the original Editor session was left open.
- [Focused integration](deterministic-checks.txt): 75 semantic/mesh assertions passed; two scoped allocation measurements are explicitly unavailable after failed sentinel calibration. Covers real overload/recovery, zero-WORK/full-LOAD stalls, actual progress/reset, full-speed danger, cancellation, withdrawal, hostile information parity, shared style propagation, prefix reveal and Probe clipping.
- [Fragility](fragility-checks.txt): 126 assertions passed. [Canonical and mechanical regressions](canonical-regression-checks.txt), [Fragility/terminal regressions](fragility-regression-checks.txt), and [additional identity/Probe results](regression-checks.txt) retain exact outcomes. Canonical parity: 883 checks passed. Geometry/route ordering, lifecycle, behavior/WORK, natural LOAD noise, BACKDOOR, LOADER, INFECTOR, Payload, Worm, Infection, Integrity/CORE, inspect/history, diagnostics, identity hopping/reactive aliases, Probe and sweep checks passed.
- [Gameplay Play Mode](graphical-checks.txt): 466 scripted assertions passed across both canonical scenes at 1920×1080 and 1280×720. Includes actual terminal submission/focus, visible IDs and bounds, rescue, timeout/retirement, sacrifice, connecting interruption, crowding, alias/Probe revelation, moving attacks, zero productivity, operation reset, pause/overlay/reset semantics and CORE freezing.
- [Original Fragility graphical runner](fragility-graphical-checks.txt): 54 scripted assertions passed across both scenes and resolutions.
- [Authoring safety](authoring-safety-checks.txt): repeat migration is refused; lab fixture regeneration leaves nine shared/canonical asset files byte-identical and the regenerated lab retains its focused checks. Shared assets are never rebuilt by ordinary lab authoring.
- The lab's focused/graphical reports are linked from [its review guide](../ConnectionsDisplayLabV0/README.md). Allocation claims from the earlier uncalibrated thread-counter experiment are superseded; no per-renderer zero-allocation claim is made here.
- [Isolation](isolation-checks.txt) verifies unchanged authoritative simulation, allocator, threats, Packet and historical scene assets. Project Core generation/check and staged whitespace checks pass.

### Retained inherited failures

| Witness | Actual result | Baseline evidence |
|---|---|---|
| Overclock | Advanced compact three-tool-row UI assertion fails | [Integrated baseline](baseline-canonical-checks.txt) |
| Resource | Inspect-header pill geometry/color UI assertion fails | [Integrated baseline](baseline-canonical-checks.txt) |
| Generated Identity | Stale Small hostile-family weight assertion fails; later cases in this witness are not reached | [Exact pristine-main reproduction](baseline-generated-identity.txt) |

The aggregate regression command exits **1**, preserving those failures. No unrelated system or historical tuning expectation was changed to force green results. Hopping/reactive identity and actual Probe revelation are covered separately and pass.

## Actual gameplay captures

Every row has both resolutions for both scenes. The links below open 720p; change the suffix to `1920x1080.png` or open the [Small folder](Captures/Connections) / [Advanced folder](Captures/ConnectionsAdvanced) for native 1080p.

| Situation | Small | Advanced |
|---|---|---|
| Ordinary generated gameplay | [Frame](Captures/Connections/00-generated-gameplay-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/00-generated-gameplay-1280x720.png) |
| Healthy service | [Frame](Captures/Connections/01-healthy-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/01-healthy-1280x720.png) |
| Slowed but safe | [Frame](Captures/Connections/02-slowed-safe-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/02-slowed-safe-1280x720.png) |
| Current-rate danger | [Frame](Captures/Connections/03-at-risk-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/03-at-risk-1280x720.png) |
| Overclock rescue | [Frame](Captures/Connections/04-rescue-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/04-rescue-1280x720.png) |
| Actual timeout departure | [Frame](Captures/Connections/05-timeout-departure-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/05-timeout-departure-1280x720.png) |
| Verbose timeout history | [Frame](Captures/Connections/06-timeout-history-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/06-timeout-history-1280x720.png) |
| Collapsed summary and prompt | [Frame](Captures/Connections/07-resolved-summary-and-prompt-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/07-resolved-summary-and-prompt-1280x720.png) |
| Legitimate sacrifice | [Frame](Captures/Connections/08-sacrifice-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/08-sacrifice-1280x720.png) |
| Connecting prefix | [Frame](Captures/Connections/09-connecting-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/09-connecting-1280x720.png) |
| Interrupted establishment | [Frame](Captures/Connections/10-interrupted-establishment-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/10-interrupted-establishment-1280x720.png) |
| Eight crowded paths | [Frame](Captures/Connections/11-crowded-paths-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/11-crowded-paths-1280x720.png) |
| Advertised hopping alias | [Frame](Captures/Connections/12-hopping-alias-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/12-hopping-alias-1280x720.png) |
| Probe revelation | [Frame](Captures/Connections/13-probe-revelation-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/13-probe-revelation-1280x720.png) |
| Existing Payload and Worm | [Frame](Captures/Connections/14-existing-moving-attacks-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/14-existing-moving-attacks-1280x720.png) |
| Configured Duel pause | [Frame](Captures/Connections/15-duel-pause-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/15-duel-pause-1280x720.png) |
| CORE freeze | [Frame](Captures/Connections/16-core-frozen-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/16-core-frozen-1280x720.png) |
| Full speed but endangered | [Frame](Captures/Connections/17-at-speed-endangered-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/17-at-speed-endangered-1280x720.png) |
| Full LOAD, zero WORK | [Frame](Captures/Connections/18-zero-work-stall-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/18-zero-work-stall-1280x720.png) |
| Operation before transition | [Frame](Captures/Connections/19-operation-before-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/19-operation-before-1280x720.png) |
| Operation progress reset | [Frame](Captures/Connections/20-operation-reset-1280x720.png) | [Frame](Captures/ConnectionsAdvanced/20-operation-reset-1280x720.png) |

[Shared-style edit: Small](Captures/Connections/21-shared-style-proof-1920x1080.png) · [Advanced](Captures/ConnectionsAdvanced/21-shared-style-proof-1920x1080.png) · [Lab](../ConnectionsDisplayLabV0/shared-style-proof.png). The same `DisplayStyle.asset.ordinaryRail` was temporarily set to white in memory, then restored. No independent canonical scene styling change was needed. The ordinary screenshots use the accepted palette.

The controlled cases reuse the real Fragility PURPOSE fixture/queue/allocator. Late recovery uses the same safe two-request demand, waiting until earlier delay leaves too little normal-rate budget; sacrificing the peer restores full speed without erasing danger. The zero-WORK and two-operation review plans reuse the actual generated route and existing generic rate/operation mechanisms. These are disposable in-memory witnesses. Ordinary ambient scheduling is sampled separately and is not retuned. No natural-congestion or overall-balance verdict is established.

## Motion

**[Small review reel](Connections-gameplay-review.mp4)** · **[Advanced review reel](ConnectionsAdvanced-gameplay-review.mp4)** — each H.264, 1280×720, 24 fps, 447 actual Unity frames, 18.625 seconds.

| Reel time | Actual scenario |
|---|---|
| 0–6s | Ordinary generated gameplay |
| 6–14s | Controlled danger; terminal Overclock at 8s clears danger and restores productive activity |
| 14–16.625s | Last service second, actual timeout, warning/fade and retirement |
| 16.625–18.625s | Eight simultaneous crowded connections |

The chapters concatenate unpainted Play Mode PNG frames. They are separate scenario runs, not one continuous run. Host and shared heartbeat are sampled at 1/24-second intervals; other existing unscaled animations, including label cycling and Probe sweep, retain real capture-time behavior. There is no motion interpolation or illustrative substitute. Real frame-clock pause/resume/overlay behavior is tested separately. These recordings validate stepped activity, not physical keyboard feel or sustained native Game-view performance.

## Performance observations and limits

[Raw measurements](presentation-profile.txt). A sampled presentation pass includes **two existing layout passes**, all path updates and forced Canvas work; it excludes authoritative stepping, object discovery, PNG encoding and GPU timing. With eight connections, the observed mean was approximately **2.7ms Small / 4.1ms Advanced**, with peak path meshes **1,632 / 2,716 vertices**. These are instrumented Editor observations, not a standalone FPS budget.

A separate 30-frame ordinary PlayerLoop sample with eight connections and **no PNG capture** calibrated `GC Allocated In Frame` with a 1-MB sentinel. Whole Editor/engine/simulation/UI means were approximately **82KB / 117KB per frame**. These bytes cannot be attributed solely to the renderer. Existing route/pill layout creates collections, and UI/simulation also allocate; those owners were not broadly refactored. The per-thread API reports zero even for an explicit allocation, and the scoped GC.Alloc recorder also fails calibration, so scoped bytes are marked **UNAVAILABLE**. No whole-engine or binding-loop zero-allocation claim is made.

There is one path/activity mesh per connection, no per-blip GameObjects or material clones. Geometry arrays are reused when the route is unchanged; dynamic mesh content is rebuilt for animation. Resolved models release resources immediately, then their visual meshes, packed IDs and sockets retire after the shared departure. The timeout capture verifies retirement, and no visual objects survive restart.

Very narrow diagonal rails remain a contrast/readability concern at extreme packing, and crowded Advanced status labels can compete with IDs. The antialiasing support floor helps subpixel edges without changing width allocation or the normal lab-width appearance. Native human review and standalone/GPU profiling remain open. Graphical startup reports the known nonfatal Unity QuickSearch indexing exception; final assertions still complete.

## Human Play Mode review

1. Reopen each canonical scene and test 1920×1080 and 1280×720. Type normally; compare ordinary physical links, logical rails, node sockets, IDs and current-operation fill. Inspect multiple IDs and history while retaining a command draft/caret.
2. Use the existing Tools → Connections Healthy, Slowed Safe and Benign Fragility fixture menus. Check shared stepped/eased motion, dense slowed traffic, continuous rails and immediate inspect results. Let a pair time out, then retry with `overclock db` around ten service seconds and with a single legitimate kill. Stop Play Mode/reload to discard fixture tuning; restart alone retains its in-memory configuration.
3. Watch full-speed danger, zero-WORK stalls and operation transitions in the retained evidence, then inspect equivalent real connections. Fill must track actual WORK, stop/reset with the model and remain readable near empty/full. Danger does not set productive speed.
4. Use ordinary generated traffic and `spawn basic-threat idhop`; compare the advertised alias, `probe <occupied-link>` revelation and later history. Test Payload/Worm counterplay and terminal focus. These systems keep their own appearance.
5. Watch crowded paths at native scale, including bends, narrow diagonal rails, label cycling and arrivals/departures. Assess whether color/activity are sufficient without network AT RISK text. Do not infer natural traffic balance from forced fixtures.
6. CLEAN should leave activity running. Default Duels continue simulation; changing the existing Duel setting to pause should freeze the shared clock. Pause timeScale, resume, trigger CORE/restart and confirm the defined clock/fade behavior. Physical keyboard/IME and aesthetic acceptance remain human review.
7. Open ConnectionsDisplayLab. Compare all curated groups/racks, pause/replay and change a shared style value. The same asset controls gameplay on the next scene run. Do not run the one-time integration migration; it is guarded. Ordinary lab fixture authoring rebuilds only the lab scene and protects shared production assets.

## Reproduction

Use an isolated Unity project copy if the original Editor is open. `ConnectionVisualIntegrationWitness.Regressions` runs the focused and broad tests with batchmode/nographics/quit, preserving inherited nonzero exits. `ConnectionVisualIntegrationWitness.AuthoringSafety` tests guarded migration and fixture regeneration. `FragilityCapture.RunVisualIntegration` runs graphical gameplay evidence; `ConnectionDisplayLabCapture.Run` runs the lab. Omit nographics/quit for capture runners because they enter Play Mode and exit themselves. Their temporary PNG sequences live under `/tmp/uptime-visual-motion` and `/tmp/uptime-display-lab-motion`.

The generated Project Core is current on this feature branch. Refresh the uploaded ChatGPT Project Source manually after the accepted source changes are merged; the upload is not synchronized by Git.
