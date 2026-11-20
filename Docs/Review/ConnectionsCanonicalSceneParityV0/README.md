# Canonical Connections Scenes V0 — review

Integration branch `feat/connections-canonical-scene-parity-v0`, based on #118 at `e551207c30236cfeb0dd95e7a5a6ce5ab0eb52df`. Draft PR targets `feat/connections-hacker-directives-v0`; references [roadmap #119](https://github.com/abeering/uptime-game/issues/119). No PR is merged/closed and Living Shift #115 is excluded.

Small `Connections.unity` and Advanced `ConnectionsAdvanced.unity` are now the supported gameplay scenes. The [pre-edit feature matrix](PARITY_AUDIT.md) records the actual gaps; the [canonical contract](../../Connections/connections-canonical-scenes-v0.md) defines the resulting boundary. [FILES_CHANGED.txt](FILES_CHANGED.txt) is the exact manifest relative to #118.

| Result | Small | Advanced |
|---|---|---|
| Topology and scene identity | Original four nodes / three links / GUID | Original eight nodes / seven links / GUID |
| Canonical content | Six existing legal PURPOSEs explicitly serialized; API/DB/AUTH cover | Original six plus complete storefront WEB recipe; original JOBS/RPT overrides |
| Ambient activity availability | All seven families; weights 4/2/2/1/1/1/1 | Same |
| Infection availability | All five positive weights | Same |
| Default intensity | 54s hostile mean / benign goal3 | 30s / goal4 |
| Integrity and knowledge | Same permanent 2/2 rules, reveal, overload and Duel attribution | Same |
| Presentation and loss | Neutral/orange/red cards, physical-region links, shared seven-row status prefab, CORE panel/freeze/restart | Same |
| Directive controls | Inspector SPREAD/BREACH, default SPREAD / 60s / 0.7 | Same |
| Existing commands/overlays/defenses | Available with existing bindings | Same |
| Survival strip | Existing clock/tools visible; Health value/bar/rate hidden, internal model retained | Same |

No runtime gameplay script or prefab changed. Editor authoring reuses one Integrity presentation path for both scenes. Original node/UI RectTransform geometry is preserved: all 69 Small and 121 Advanced existing RectTransforms match the base, as do exact nodes/edges and scene GUIDs. Unity serialization accounts for substantial scene diff volume; Small's full pre-existing fallback catalog is now explicit. The shared status prefab is reused, not regenerated. No WORK accounting, titlebar redesign, shift lifecycle, concession or balance framework is introduced.

## Actual validation

Unity **6000.3.11f1**, isolated project `/private/tmp/uptime-integrity-validation` because the working project was open:

- [parity-checks.txt](parity-checks.txt): **881 checks**, including actual serialized defaults before witness overrides, all node/link/presentation/terminal bindings, explicit legal catalogs, every internal node's benign cover, all families at low/high intervals under both directives, shared Surprise planning, legal generated routes, scan/CLEAN/defenses, hidden/revealed Integrity, physical CORE ending, freeze and real terminal restart.
- **16 natural runs**: both scenes × SPREAD/BREACH × authored/eight-second pressure × seeds 1701/3141, up to 600 simulation seconds or breach. Four seed1701 authored-mode runs exactly replay all events/snapshots/graph samples. [RESULTS.md](RESULTS.md), [natural-metrics.csv](natural-metrics.csv), [natural-activities.csv](natural-activities.csv), [replay-checks.txt](replay-checks.txt) and compressed full logs retain observations.
- Every supported family is **actually produced**, not merely selected, in mixed natural runs in each scene. Every internal node is observed as a benign destination. All queued routes validate against actual physical adjacency. [summarize.py](summarize.py) regenerates tables and [preservation checks](preservation-and-natural-checks.txt).
- [regression-checks.txt](regression-checks.txt): **18 witnesses pass**, including Traffic, Ambient Hostile Activity, Network Health, Duel, Consequence, historical Advanced, Recorder, Payload, Worm, Ambient Payload Threat, Scan, Infection, Node Status, Geometry, Integrity, Hacker Directive, Storefront and Maximum Pressure. Integrity retains 327 assertions; directive 375; storefront 177. Maximum Pressure retains its sixteen 180-second comparisons/four seeded replays.
- **Two inherited failures remain:** Resource inspect-header pill geometry/color and Overclock Advanced tool-row width. The aggregate command exits 1. Their assertions/thresholds are unchanged; parent reproduction remains in [Integrity validation](../ConnectionsIntegrityV0/validation.txt).
- Actual graphical Play Mode in **both scenes**, **28 final frames** at 1920×1080 and 1280×720, with **52 checks** covering fixed capture viewport, overlay bounds, focus, CLEAN, severe-load presentation, Duel, frozen CORE ending and terminal restart. [graphical-checks.txt](graphical-checks.txt) records checks.
- Generated Core freshness, whitespace, exact topology/GUID/geometry, historical fixture/Packet asset and runtime/prefab preservation checks pass. The known Unity Search startup indexing exception recurs in graphical startup; game checks complete and Editor exits 0. Human physical-keyboard playtesting remains separate.

Natural default runs include both breaches and survival to the horizon; stress runs tend to end earlier. BREACH is not universally faster in this tiny two-seed integration sample. Small BREACH seed3141 produces no ordinary hostile connection before loss, but does produce real direct Duels/Worm activity; other Small runs produce ordinary LOADER/INFECTOR traffic. Family availability is not a guarantee each short run samples every category. No pressure was strengthened to manufacture evidence. This is parity/causal integration evidence, not final balance or fun validation.

## Preserving older regression coverage

[regression-initial.txt](regression-initial.txt) records the first pass before reconciling legacy assumptions. Fifteen failures included obsolete Small exclusions, three-infection sampling, six-row opt-out presentation and tests expecting uninterrupted pre-Integrity step behavior.

Functional assertions were retained. Seven older component witnesses now explicitly use an **in-memory legacy mechanic fixture**: disable Integrity/directives while testing prior allocation/lifecycle/timing semantics, restore the old Small ambient/infection mix, and where required use the original status prefab. This is never saved to canonical scenes. The new 881-check witness separately verifies the real canonical settings/mechanics and shared seven-row prefab.

The original Advanced and ambient-selection regression scenarios use the preserved Pressure scene with only its four original control tuning changes reversed in memory. SHA-256 comparison proves this reconstructs the entire pre-promotion Advanced scene byte-for-byte, rather than accepting a loosely similar substitute. The Maximum Pressure comparison still exercises both its original control and pressure behavior. Assertions about canonical Small family availability, Integrity/directive presence and Advanced storefront content are updated to the explicitly approved contract; Pressure remains opt-out. The two old UI assertions still fail and were not relaxed.

Pressure, Integrity, Status Lab, Topology Stress, Packet and all existing prefab assets remain byte-identical to #118. Existing experiment raw evidence remains intact; only README status banners identify it as historical. Older phase authoring helpers are not canonical rebuild tools.

## Graphical evidence

These are constructed fixtures using actual host/input/overlay paths, not natural-run screenshots. The host's automatic stepping is disabled and ordinary fixed steps advance chosen scenarios. Camera/Canvas viewport is fixed at 16:9 **before** opening windows, and both capture sizes are checked. An initial capture attempt used a mismatched viewport and was discarded; final captures include bounds checks.

| State | Small 1280 | Advanced 1280 |
|---|---|---|
| Healthy DB / hidden API damage neutral | [frame](Captures/Connections/01-neutral-hidden-1280x720.png) | [frame](Captures/ConnectionsAdvanced/01-neutral-hidden-1280x720.png) |
| Known orange API, isolated red DB, CLEAN PID overlay | [frame](Captures/Connections/02-clean-orange-and-isolated-red-1280x720.png) | [frame](Captures/ConnectionsAdvanced/02-clean-orange-and-isolated-red-1280x720.png) |
| Six active paths, structured inspect and LOAD | [frame](Captures/Connections/03-crowded-inspect-1280x720.png) | [frame](Captures/ConnectionsAdvanced/03-crowded-inspect-1280x720.png) |
| Overload countdown | [frame](Captures/Connections/04-overload-1280x720.png) | [frame](Captures/ConnectionsAdvanced/04-overload-1280x720.png) |
| Duel overlay | [frame](Captures/Connections/05-duel-overlay-1280x720.png) | [frame](Captures/ConnectionsAdvanced/05-duel-overlay-1280x720.png) |
| CORE path / frozen simulation | [frame](Captures/Connections/06-core-breached-1280x720.png) | [frame](Captures/ConnectionsAdvanced/06-core-breached-1280x720.png) |
| Restart | [frame](Captures/Connections/07-restart-1280x720.png) | [frame](Captures/ConnectionsAdvanced/07-restart-1280x720.png) |

Each folder has matching 1920×1080 frames. Rendered CLEAN, crowded inspect, Duel and CORE frames were visually reviewed at both sizes across the two scenes. Fixed focus/bounds assertions do not replace physical keyboard/attention testing. Dense logical lanes can still crowd and labels may truncate in compact terminal history, as in existing UI; no layout redesign was attempted.

## Reproduce / manual Unity review

On a disposable copy using the documented Editor:

```text
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.CanonicalSceneWitness.Run -quit -logFile <parity.log>
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.CanonicalSceneWitness.Natural -quit -logFile <natural.log>
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.CanonicalSceneWitness.Regressions -quit -logFile <regressions.log>
Unity -batchmode -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.CanonicalSceneCapture.Run -logFile <graphical.log>
python3 Docs/Review/ConnectionsCanonicalSceneParityV0/summarize.py
python3 scripts/build_uptime_project_core.py --check
```

The regression command deliberately returns 1 for the inherited failures. Legacy witnesses still write normal outputs in the disposable copy; preserve committed historical evidence in the working repository. Natural/capture output goes only to this integration's evidence folder. Do not run one-time `CanonicalSceneAuthoring.Promote` again on already promoted assets.

1. Open each canonical scene. Confirm topology/layout and SimulationHost's enabled Integrity and HackerDirectives. Check traffic's explicit catalog and positive family/infection weights. Small should show 54s / goal 3, Advanced 30s / goal 4.
2. Disable Randomize Seed, choose 1701 or3141, and compare SPREAD/BREACH. Use the same Inspector hostile interval at 8s to try high pressure in either scene without zeroing any activity weight. Restore authored settings afterward.
3. Play at 1920×1080 and 1280×720. Check neutral/orange/red cards, infrastructure markers, physical compromise versus logical lanes, existing clock/tool readiness, absent Health survival gauge, crowded labels, terminal history and `inspect`/`logs`/`load` focus.
4. Exercise `scan db`, `clean db`, PROBE, intercept/isolate and overclock. Verify scan reveals current Integrity, PID CLEAN preserves damage, overlays remain visible and keyboard focus returns. Compare useful benign WORK and hostile cover through internal logs; every internal destination should have legitimate traffic possibilities.
5. Let a natural run breach, or use development-only `integrityhit` along a valid physical ingress/core route. Verify exact-once CORE ending, frozen simulation and `restart` clearing state while retaining the selected directive/seed. Exercise Duels with current consequences; no concession/WORK/shift behavior is added.
