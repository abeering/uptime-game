# Connection visual integration — corrective review

**Checkpoint:** the following records `a0ffc00`, before the [final socket refinement](SocketRefinement/README.md). Its screenshots, motion, isolation hashes and measurements retain that provenance.

This is the focused correction on draft PR #131, following human gameplay review of initial integration `1a72a6e`. Architecture, mechanics, heartbeat and danger mapping are retained. Both canonical scene assets, shared prefab hierarchy, pill artwork/material/shader and packing remain unchanged in this pass. All new evidence uses the real shared Unity renderer.

## Selected starting treatment

- **Identity:** reuse the existing lifetime-owned rotating six-slot assignment. Every segment uses that slot; aliases/hopping/Probe do not reassign it. Ordinary gets muted blue-grey, green-grey, warm grey, slate, violet-grey and mint-grey. Synthetic Priority reuses the previous six cyan/teal accents exactly. Gameplay has no new priority/hostility classification.
- **Ribbon:** resolved rail RGB, including its danger/pulse/departure treatment, fills the continuous interior at **0.34 opacity**. The same joined mesh geometry, width, prefix reveal and Probe clipping apply.
- **Activity:** ordinary RGB **(0.68, 0.74, 0.77)**, priority **(0.53, 0.79, 0.85)**, opacity **0.60**, stationary multiplier **0.70**. Geometry stays 9 units long / 0.5 half-width fraction. No heartbeat/cadence/density/direction changes.
- **Pills/sockets:** pill borders retain base identity while the ribbon and destination socket carry resolved status. Bright ID text and the existing subordinate interior WORK fill are unchanged; no font, placement or packing redesign. At maximum danger the ribbon prioritizes status, with the pill preserving base identity.

[Exact palette values, tunables and assignment](../../Connections/connections-visual-integration-v0.md#corrective-visual-pass--identity-ribbons). All selected parameters are in **Assets/Prefabs/Connections/Display/DisplayStyle.asset**, shared by Small, Advanced and the lab.

## Actual before/after motion

**Left = initial #131 at `1a72a6e`; right = this correction.** Both panels retain native 1280×720 content, side by side at 2560×720 / 24fps. Baseline footage comes from the committed initial-integration MP4s, extracted by Git; current footage is encoded from the new actual Play Mode frames. No painted/generated frames, optical-flow interpolation or speed changes. Baseline compression remains, so this is a salience/readability comparison, not pixel-equality proof.

- [Small: 10-second comparison](Connections-before-after.mp4).
- [Advanced: 10-second comparison](ConnectionsAdvanced-before-after.mp4).
- [Lab: 8-second comparison](../ConnectionsDisplayLabV0/ribbon-before-after.mp4), matching synthetic timestamps 0–8s.

Gameplay comparisons concatenate matching scenario windows: **0–4s ordinary generated traffic; 4–8s endangered service/rescue (terminal Overclock at 6s); 8–10s eight crowded paths**. These are separate deterministic scenario runs. The full updated [Small](Connections-gameplay-review.mp4) / [Advanced](ConnectionsAdvanced-gameplay-review.mp4) reels retain ambient, rescue, timeout and crowding chapters. Additional five-second [Small](Connections-routes-and-stall.mp4) / [Advanced](ConnectionsAdvanced-routes-and-stall.mp4) reels show three shared/diverging routes for 3s, then an actual zero-WORK stall for 2s. Rendered host/heartbeat samples use fixed 1/24-second intervals; other existing UI animations retain capture-time behavior.

## Actual stills at both resolutions

The [main guide's complete table](README.md#actual-gameplay-captures) links all **104 images**, both canonical scenes at 1920×1080 and 1280×720. New cases 22–25 add real shared links with three different destinations and low/medium/high actual WORK on the same identity. Existing cases cover ordinary generation, slowdown, stall, endangered/recovered/full-speed danger, operations, arrivals/departures, narrow/crowded paths, aliases/Probe and attacks.

[Lab palette plates](../ConnectionsDisplayLabV0/README.md#current-corrective-pass-131) show all six ordinary colors and all six priority colors in the same six authored rows, at both resolutions. These palette overrides exist only in capture memory and are restored. The permanent lab samples carry explicit stable slots and retain all existing groups and interior/perimeter racks.

Recorded-image inspection: muted adjacent identities remain distinguishable in safe traffic, the body is visibly filled, blips sit below the rail/pill contrast, and low/high progress still permits ID reading at 720p. The branching case makes the matching destination sockets visible. This is inspection of actual captured frames, **not human Play Mode acceptance**. Very narrow diagonals retain raster/contrast limits, and crowded Advanced status labels can overlap IDs. No final polish or natural-balance claim.

## Checks actually run

Unity **6000.3.11f1**, isolated project copy; original Editor left open. Final changed Assets files match that tested copy byte for byte.

- [Integration](deterministic-checks.txt): **81 PASS**, **2 UNAVAILABLE** scoped allocation probes.
- [Lab](../ConnectionsDisplayLabV0/deterministic-checks.txt): **512 PASS**, **1 UNAVAILABLE** scoped allocation probe. Existing 320 continuity scenarios now check both opaque rails and status-colored translucent body coverage on straight/bent variable-width paths, across performance, danger, class, deficit and pulse phases. Extra assertions check six differentiated ordinary colors and exact restoration of all six priority accents.
- [Gameplay Play Mode](graphical-checks.txt): **546 PASS** across both scenes/resolutions, including slot stability through actual Probe revelation, branching routes and actual WORK fill stages.
- [Lab Play Mode](../ConnectionsDisplayLabV0/play-mode-checks.txt): **225 PASS**, including both six-color comparison families, pointer controls, hierarchy/cache and shader/layout checks.
- [Fragility](fragility-checks.txt): **126 PASS**. Canonical parity **883 PASS**, with the final [canonical regression report](canonical-regression-checks.txt) byte-identical to the integrated baseline. Broad geometry, lifecycle, identity/Probe, inspect/history, WORK, threats and CORE regressions ran; [raw aggregate](regression-checks.txt) retains exact outcomes.
- [Authoring safety](authoring-safety-checks.txt): **11 PASS**, including shared/canonical file hashes across lab regeneration and the repeated-migration guard.
- [Corrective isolation](corrective-isolation-checks.txt): unchanged canonical scenes, heartbeat/adapter/danger mapping, simulation, allocator, traffic, shader/material, shared prefabs, identity-packing owner and Packet scenes. No new Assets files or GameObjects. Project Core regenerated from sources and checked; whitespace checked.

The broad command still exits **1** for the same three inherited failures: **Advanced Overclock compact tool row; Resource inspect-header pill geometry/color; Generated Identity stale Small hostile-family weights/arrival assertion**. The latter was reproduced on pristine integrated main in the initial pass; later cases in that witness are not reached. No unrelated assertions/tuning were changed to force green. Separate hopping/reactive identity and Probe/sweep pass. The initial 54-check Fragility graphical report remains historical evidence, rather than a newly rerun corrective result.

## Performance and remaining review

[Fresh measurements](presentation-profile.txt): eight-connection sampled presentation means **2.980ms Small / 3.518ms Advanced**, meshes peak **1,632 / 2,716 vertices**, as in the initial pass. The sample includes two existing layout passes, paths and forced Canvas work, excluding host stepping, PNG encoding and GPU. Separate 30-frame ordinary PlayerLoop samples with no PNG capture calibrate whole Editor/engine/UI/simulation mean allocations **82,428 / 115,020 bytes per frame**. They do not isolate renderer allocations. Scoped counters still fail sentinel calibration; no zero-allocation claim. These Editor observations do not establish a standalone FPS budget or a significant cross-run performance change. The lab retains **334 Canvas GameObjects** and **3,912** peak path vertices. Body fill reuses existing quads; there are no per-blip objects or material clones.

Known nonfatal Unity QuickSearch indexing exception remains during graphical startup. One first capture attempt exposed a queued-connection lookup error in the new capture extension; lookup was moved after registration, and the complete final 546-check run passed. No gameplay fix was needed.

Human review: enter both canonical scenes at 1080p and 720p. Use the existing Healthy/Slowed Safe/Benign Fragility fixtures; inspect and rescue with `overclock db`, then allow timeout. Trace a connection by its color through nodes, compare its pill and socket, and assess crowded paths at native scale. Use ordinary generated traffic for keyboard/alias/Probe review. Open the lab, compare both progress racks and palette plates, and watch paused/replayed heartbeat states. Do not run the guarded one-time migration. Physical keyboard/IME feel, final color acceptance, narrow diagonals/crowded labels and standalone/GPU performance remain human review. This PR stays draft and unmerged.
