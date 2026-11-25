# Connection Display Lab V0 — review

Open **Assets/Scenes/ConnectionsDisplayLab.unity** in Unity 6000.3.11f1 and enter Play Mode. It remains an independent synthetic lab. [Visual Integration V0](../ConnectionsVisualIntegrationV0/README.md) now uses the same authored assets and rendering in both canonical gameplay scenes. The original lab began independently from main `802a98e` before #128/#129 merged; those isolation statements are historical checkpoint evidence.

[Implementation, mappings, asset inventory and integration seams](../../Connections/connections-display-lab-v0.md).

## Second visual pass

Human feedback approved the existing heartbeat and missed-beat slowdown. This pass makes established rails continuous at every performance/danger state and promotes interior fill to the leading production candidate. Colors, danger pulses, beat defaults, activity density/direction and departure timing are preserved. No GameObjects, decorative assets, gameplay mechanics or new rendering abstractions are added.

## What to watch

- A: grey/blue traffic class, READ/WRITE direction, local EXECUTE rhythm, low/high LOAD density.
- B: missed beats at moderate/severe slowdown, unchanged density, stationary stalled activity, and a bent variable-width route. The bent example changes operation at 12 seconds and resets WORK without changing geometry.
- C: full-speed danger beside full-speed safety; slowed safety separately in B; normal-rate budget deficit separately labeled. All severity/estimate/budget inputs are synthetic.
- D: repeating recovery, deterioration and three different departures. Full speed does not automatically clear danger. All primary animated examples, including D, use interior WORK fill. The static comparison rack retains both treatments.
- Bottom left: both progress treatments at 0/25/50/75/99%, with independent high-contrast identity text.

The shared clock begins at 120 BPM, 18 UI-unit steps, 58% moving/42% holding, SmoothStep easing. Healthy motion advances every beat, moderate slowdown every second beat, heavy slowdown every fourth. All examples use phase 0/subdivision 1 by default. Requested LOAD changes spacing independently. EXECUTE expands/contracts in place with the same eased movement/hold.

Use the authored **PAUSE / RESUME** and **REPLAY** buttons. Inspector tuning is on the Canvas's `ConnectionHeartbeat`, `DisplayStyle.asset`, each sample's synthetic phases, and each display's phase/subdivision/progress-treatment fields. REPLAY resets every transition and the heartbeat while preserving pause state. No commands or setup are required to see useful comparisons.

## Actual Play Mode captures

| Presentation timestamp | 1920×1080 | 1280×720 |
|---|---|---|
| Initial comparisons | [Overview](overview-0.0s-1920x1080.png) | [Overview](overview-0.0s-1280x720.png) |
| Full-speed danger and transition examples | [8.2s](overview-8.2s-1920x1080.png) | [8.2s](overview-8.2s-1280x720.png) |
| Recovery and timeout | [16.4s](overview-16.4s-1920x1080.png) | [16.4s](overview-16.4s-1280x720.png) |

**[Watch the 18-second motion recording](display-lab-play-mode.mp4)** — H.264, 1280×720, 24 fps, 432 frames. This is actual Unity Play Mode rendering, not an illustrative mockup. The capture driver samples the shared presentation timeline at exact 1/24-second intervals for reproducible playback, with the ordinary automatic clock disabled during frame sampling. Before sampling, it separately verifies the real unscaled clock and actual pointer pause/resume/replay controls. Encoding uses those ReadPixels PNG frames with ffmpeg; no generated or painted imagery is substituted.

**[Watch the 8-second synchronized motion comparison](continuous-rails-comparison.mp4)** — first pass in the left two columns (B/D), second pass in the right two columns (B/D), 1216×594 at 24 fps. These are matching crops of raw Unity Play Mode frames at identical timestamps 0–8 seconds. Compare the interrupted versus continuous slowed/stalled rails and perimeter versus interior transition pills; no retiming or interpolation is added.

The full matrix remains visible in every frame of the 18-second recording. Small diagnostic text is secondary at 720p; use the 1080p captures or native Editor view for close comparison. Human aesthetic judgment, mouse feel and sustained native Game-view review remain for the user.

## Actual checks

All checks ran in an isolated project copy because the original Editor already had the repository open. The original Editor session was left running.

- Unity 6000.3.11f1 compiled the new C# and imported/rendered the new shader.
- [497 deterministic assertions](deterministic-checks.txt) passed: authored prefab/CanvasRenderer bindings, independent inputs, exact eased movement and holds, beat continuity, missed beats/stall, density, existing operation direction semantics, full-speed danger, clipped bent-path activity, fixed progress racks, operation reset, replay, all outcomes and geometry-cache checks. The scoped allocation probe is now marked unavailable after failing sentinel calibration; earlier zero readings from the thread API cannot establish zero allocations. The added 320 opaque-rail coverage cases test both sides along straight and three-segment variable-width paths across four speeds, five severity levels, both traffic classes, both normal-budget flags and both extremes of the warning pulse. All 26 animated examples assert interior fill.
- [223 actual Play Mode assertions](play-mode-checks.txt) passed: timeScale-zero heartbeat, pointer raycasts and controls, both viewport sizes, every sample's bounds/title/mesh, stable hierarchy across 432 motion frames, and retained full-speed severe-danger comparison.
- [Existing regression witnesses](regression-checks.txt) passed: Geometry and 883 canonical-scene serialized/functional checks on the integrated baseline. Full integration regressions and inherited failures are documented in the integration review.
- [Motion preservation](motion-preservation-checks.txt): 25 activity-center strips across all 432 frames compare exactly (3,240,000 RGB bytes, zero differences), covering operation rhythms, slowdown/stall, danger independence, transitions and departures. This excludes the deliberately changed rails and pill interiors.
- The [original lab isolation check](isolation-checks.txt) records the independent #130 checkpoint. The [current integration isolation check](../ConnectionsVisualIntegrationV0/isolation-checks.txt) records the now-shared presentation and preserved authoritative simulation/Packet/historical scenes.
- Project Core generation/check and staged diff whitespace checks passed.

Screenshot inspection at both sizes found the independent IDs legible over the 0/25/50/75/99% interior rack and animated fills; the completion frames also retain the ID at 100% before fading. Crowded production gameplay readability is still untested.

Measured: 334 authored GameObjects under the Canvas, plus authored Camera/EventSystem roots. There are 26 path graphics and 36 progress-pill graphics, with no individual blip objects. The motion capture's peak total path mesh count was 3,912 vertices. Animation does not rebuild unchanged cached route arrays. The earlier thread-counter zero-allocation reading is superseded: this Editor reports zero even for an explicit allocation, and the scoped GC.Alloc recorder fails calibration. No binding-loop zero-allocation claim is retained. The integration review reports separately calibrated whole-frame Editor measurements and their limits.

The graphical Editor startup emitted a nonfatal Unity QuickSearch indexing `ArgumentOutOfRangeException`; the complete assertions/captures still passed. The new opaque-rail coverage check first failed against the first-pass dashed renderer at speed 0.65, demonstrating that the continuous body cannot mask a broken rail. The retained reports above are final successful runs.

## Manual review

1. Review the whole scene at 1920×1080, then 1280×720. All important comparisons should stay visible. Read IDs over every fill value, inspect thin rails/blips and bent joins, and compare both progress racks.
2. Watch A1/A2: high LOAD must be denser without moving faster. READ must travel toward the origin; WRITE toward the marker. EXECUTE should act locally, with visible settle/hold rather than continuous scrolling.
3. Compare B1/B2/B3/B4. Verify common tempo, missed beats, heavy slowdown and complete stall. High-LOAD slow examples should stay dense. Confirm rails remain continuous at every speed, including stall; only the activity misses beats or stops.
4. Compare C1/C2/C3 and C4/C5/C6/C7. Full-speed danger must keep full-speed motion. The whole rail should warn uniformly; no moving warning should suggest a particular physical segment is at fault. “Exceeds normal-rate budget” does not mean future interventions are impossible.
5. Watch D for at least 20 seconds. Look for smooth activity continuity through speed changes, clear timeout/kill/completion differences, recovery that remains endangered initially, then scripted clearing. Confirm no object destruction/recreation is needed between loops.
6. Pause, inspect the current frame, replay while paused, then resume. Change BPM 80→160, moving fraction and easing in the Inspector. Explore subdivisions/offsets cautiously while keeping a coherent shared tempo.
7. Observe B6 across 12 seconds: READ becomes WRITE on the same bent route and current-operation WORK resets. This is an operation switch, not dynamic routing. Confirm blips continue around the joins and variable widths, the identity stays placed, and the endpoint marker remains visible.
8. Compare the interior versus perimeter treatment visually. The border uses an angular sweep from 12 o'clock; it does not traverse equal physical border length per percentage. Neither fill is service-time consumption. Interior is the leading integration candidate; actual gameplay readability still needs review.

Stop Play Mode to discard Inspector experimentation unless intentionally authoring the lab. **Tools → Connections → Author Connection Display Lab** rebuilds only lab scene fixtures from existing shared assets; it never regenerates shared production assets or saves unrelated dirty assets. Preserve manual lab-scene edits before rebuilding.

## Reproduction

- Deterministic checks on the saved scene: `Uptime.Connections.EditorTools.ConnectionDisplayLabWitness.Run` with Unity batchmode/nographics/quit.
- Existing geometry/canonical checks: `Uptime.Connections.EditorTools.ConnectionDisplayLabWitness.Regressions` with batchmode/nographics/quit.
- Actual graphical evidence: `Uptime.Connections.EditorTools.ConnectionDisplayLabCapture.Run` with graphical batchmode; omit nographics/quit because the runner enters Play and exits itself. Six PNGs/reports go here; 432 temporary motion frames go to `/tmp/uptime-display-lab-motion`.
- Encode: `ffmpeg -framerate 24 -i /tmp/uptime-display-lab-motion/frame-%04d.png -c:v libx264 -crf 18 -pix_fmt yuv420p -movflags +faststart display-lab-play-mode.mp4`.

The gameplay adapter and initial cosmetic danger mapping are now in Visual Integration V0; review its separate evidence. Actual priority, authoritative danger changes, natural congestion tuning, dynamic routing, new threats and final art decisions remain outside this work.

[Shared-style edit proof](shared-style-proof.png): the same ordinary rail color was temporarily changed to white in the shared asset and restored, matching the gameplay proof captures. Both compositions now use the same pill-state binding; authored RectTransform/TMP edits control size and typography.
