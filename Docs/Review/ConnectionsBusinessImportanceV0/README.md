# Business Importance V0 review

Connections implementation on `feature/connections-business-importance-v0`, based on accepted #137 normally merged at `f43b89758c07098e0939d59dfa429a25f7e81b82`. The new implementation PR remains draft/unmerged for human gameplay review. [Contract and Inspector instructions](../../Connections/connections-business-importance-v0.md).

## What changed

- Individual legitimate requests can be IMPORTANT: a separate seeded generation draw defaults to **15%** in both canonical scenes, independent of PURPOSE and existing generator clocks/recipes. Authored definitions/plans can assign importance explicitly. Hostile assignments are rejected.
- One failed IMPORTANT job produces **one receipt worth three configurable failure units**. The existing HUD, result and `>=` dismissal threshold use units; the count of unsuccessful jobs remains separate. Success grants ordinary original business WORK. Importance changes no allocation preference, processing, service budget, route or effects.
- The previous bright cyan/teal family from `52f9b771` denotes IMPORTANT. Muted ordinary colors remain. Network and terminal identity pills share `DisplayStyle.asset` and lifetime slot ownership; retired paths free live slots while history retains their colors. Compact/verbose inspect, resolved full/collapsed summaries, LOAD and WORK were audited.
- Existing recordings add assignment/cost/unit evidence. Display Lab retains its shared assets, 26-sample layout and comparison rack, with representative importance states. Canonical scene YAML adds only the two importance values per scene. Original lab hierarchy/references are retained; only diagnostic labels and two phase recipes change.

No badges, special animations/sounds, mandatory network/inspect IMPORTANT labels, automatic FAST LANE preference, additional success rewards, service exceptions, UI panels, runtime analytics or difficulty retuning.

## Deterministic and regression evidence

Unity **6000.3.11f1**, isolated validation project copy, actual batch Editor compilation and disk JSON round trips:

| Check | Observed result |
| --- | --- |
| New importance witness | **314 assertions pass** across Small/Advanced: ordinary 1, IMPORTANT 3, custom frozen cost, allowance crossing, exact once, success credit, unchanged processing/deadlines/allocation/routes, explicit serialized assignment, plan/event/workload/identity copies, real CACHE→DB REROUTING, timeout/BLACKOUT, seeded probability/reset and hostile exclusion. Bounded 24-legitimate/8-hostile recipe comparisons at one seed compare 0/15/100% without changing existing random recipes. |
| Recorder policy completeness | **74 checks pass**: composed uncached LOAD with active modifiers/noise, reservations, paused productive rate versus actual demand, held-stage demand membership, saved JSON. |
| Route transition witness | **108 assertions pass**. |
| Display Lab | **512 assertions pass**; hierarchy 334, same shared assets/layout and progress comparisons. |
| Existing integrated groups | **12 pass**: CACHE 221, API 131, Shift, Fragility 126, Demand 47, Workload 456, Integrity 327, Behavior, Traffic, Recorder, Canonical 971, Duel Concession. |
| Core / scope | Regenerated from source, `--check` and whitespace checks pass. Packet, shared shaders/materials/heartbeat and simulation allocator are unchanged. |

[Exact importance checks](deterministic-checks.txt), [integrated group results](regression-checks.txt), [Unity summary](validation-summary.txt), [Small recording excerpt](Connections-failures.json) and [Advanced excerpt](ConnectionsAdvanced-failures.json). Excerpts retain real saved metadata, failure/resolution events and the observed snapshot, not synthetic expected outputs.

The integrated process intentionally exits nonzero for **three inherited failures**, matching the documented #137 baseline: Resource inspect-header geometry/identity-style assertion, Advanced Overclock compact three-tool row, Generated Identity stale hostile tuning. They are preserved, not relabeled as passing. The Resource assertion predates this branch; its old resolved-pill quiet-color expectation is also superseded by the requested retained identity color, while unrelated header geometry is not redesigned. Graphical startup also retains the inherited nonfatal Unity QuickSearch indexing exception. See the original [#137 transition review](../ConnectionsOperationRoutingCacheV0/TransitionRefinement/README.md).

## Actual Play Mode evidence

Scripted graphical Play Mode uses the ordinary running host in both canonical scenes at **1920×1080 and 1280×720**, then the authored Display Lab at both sizes. Real model changes drive route traversal, REROUTING, contention and danger. The capture never steps a counterfactual or rewrites performance/importance/deadline state for screenshots. Capture-only camera/viewports and temporary scenario setup are discarded on exit. **302 actual Play Mode checks pass**, with **34 PNG screenshots**. [Exact graphical checks](graphical-checks.txt). The inherited nonfatal QuickSearch startup exception was observed; no gameplay/capture exception remains in the successful run.

| Presentation | Small | Advanced |
| --- | --- | --- |
| Inspect / shared colors (1080p) | [Small](Captures/Connections-1920-inspect-colors.png) | [Advanced](Captures/ConnectionsAdvanced-1920-inspect-colors.png) |
| LOAD pills (720p) | [Small](Captures/Connections-1280-load-colors.png) | [Advanced](Captures/ConnectionsAdvanced-1280-load-colors.png) |
| WORK pills (720p) | [Small](Captures/Connections-1280-work-colors.png) | [Advanced](Captures/ConnectionsAdvanced-1280-work-colors.png) |
| Actual REROUTING (1080p) | [Small](Captures/Connections-1920-rerouting.png) | [Advanced](Captures/ConnectionsAdvanced-1920-rerouting.png) |
| Service danger (720p) | [Small](Captures/Connections-1280-danger.png) | [Advanced](Captures/ConnectionsAdvanced-1280-danger.png) |
| Two jobs / four units + history (720p) | [Small](Captures/Connections-1280-resolved-four-units.png) | [Advanced](Captures/ConnectionsAdvanced-1280-resolved-four-units.png) |

[Lab connecting 720p](Captures/Lab-1280-2.png), [nominal 720p](Captures/Lab-1280-8.png), [rerouting 720p](Captures/Lab-1280-14.png); [1080p comparison](Captures/Lab-1920-8.png). Other matching-resolution states remain in `Captures/`. Immediate transition captures can retain the preceding inspect label until its existing 0.2-second UI refresh; the longer danger/history captures show settled panels. This does not change model timing or inspect ownership.

The controlled fixture's requests are `o0`/`i0` (ordinary/IMPORTANT, same mixed READ→WRITE) and `o1`/`i1` (ordinary/IMPORTANT, same competing WRITE). CACHE selects product; API stays empty. Shared lines, pills, sockets and terminal surfaces consume the same styles. A captured warning rail/socket can differ from its base identity pill by design: service danger remains independent, and identity stays on the pill.

No human keyboard/IME acceptance, final difficulty/fairness verdict or broad ordinary-palette redesign is claimed. Human play should judge whether the bright family is sufficiently recognizable at speed, under danger and in crowded paths. Pixel screenshots and exact-color assertions establish rendering/binding evidence, not perceptual acceptance for every display.

## Manual review

1. Open **Connections** (Small), enter Play, then choose **Tools → Connections → Load Business Importance Comparison**. Repeat in **ConnectionsAdvanced**. This temporary fixture disables ambient generation, resets the run, raises the fixture target/allowance to 100000 WORK/100 units, configures CACHE product and creates the four requests above. Stop/reload to restore authored tuning; do not save the fixture.
2. `inspect i0 o0`, or `inspect -v i0 o0`: compare bright IMPORTANT and muted ordinary IDs with their network pills. Follow the cached READ, real REROUTING and ordinary WRITE without a changed identity, service budget or logical connection.
3. `load DB` and `work DB`: identity colors should match each corresponding network pill; importance itself grants neither allocation weight nor extra processing. Both classes can slow or become endangered. Preserve current heartbeat, density, progress and warning behavior.
4. `kill o0`, then `kill i0`: HUD advances 1 then 3 units, reaching **4/100** with two unsuccessful jobs. Repeating kill cannot add another receipt. Inspect resolved/collapsed history to check retained identity color. A generated ordinary run retains its authored 10-unit allowance; no extra HUD element is added.
5. Stop Play and open **ConnectionsDisplayLab**. Nominal/slow/danger importance comparisons and the READ traversal sequence use shared style assets; interior/perimeter progress rack remains. Compare both supported resolutions.
6. Tune **ConnectionTrafficDirector → Business importance → Important Connection Chance** and **SimulationHost → Business importance → Important Failure Multiplier**, then restart. Defaults are 0.15 and 3 in both scenes. Inspect `Logs/UptimeRuns/*.json` for `importance`, selection roll/chance, frozen cost, failed job count and actual unit totals. Authored Witness definitions expose the Importance enum for explicit assignments.

For deterministic reproduction outside Play Mode, **Tools → Connections → Run Business Importance Witness** opens disposable canonical fixtures and saves evidence; reload without saving afterward. Existing **Run Recorder Policy Evidence Witness**, **Run Route Transition Witness** and **Run Connection Display Lab Witness** cover supporting paths. Compilation/regressions/captures ran in an isolated copy; the user's open Editor and unrelated TMP edit were preserved.

## Deliberate limits and deferred direction

Importance probability/failure cost are provisional tuning, not final balance. The request designation is fixed at generation/authoring; failure cost freezes at admission. Live Inspector edits require restart for normal run metadata semantics. Historical recordings without new fields follow the earlier all-ordinary/unit-count contract; use field presence to distinguish them from explicit hostile zero cost.

IMPORTANT remains trustworthy company information. A future deliberate late-game escalation may undermine that trust through hackers, compromise or company circumstances; this is recorded in the living design without implementation or further design here. Consolidated presentation TODOs retain both API preference eligibility and when it materially improves allocation, alongside naturally visible CACHE routing. Company events, contextual scheduling, spoofing, additional tiers, SSH overhaul and API effectiveness visualization remain deferred.
