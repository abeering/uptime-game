# Operation Routing & CACHE V0 — review

Connections implementation after the normal GitHub merge of human-accepted #136 at `6ecb3ce6c30159e2f2b1a5217a8c03e46dc3a65b`. The new implementation PR stays draft/unmerged for human review. [Runtime, architecture and Inspector contract](../../Connections/connections-operation-routing-cache-v0.md).

**Current transition refinement:** [Shared-prefix REROUTING mechanics, lifecycle audit, updated checks and actual motion evidence](TransitionRefinement/README.md) supersede the initial full-origin mixed-route transition behavior/captures below. The earlier CACHE evidence is retained as phase history.

## What changed

- Separate original logical plans from immutable per-operation route/efficiency decisions. One operation commits once, uses its actual target for allocation/effects/interruptions, and keeps that decision until completion/termination. A subsequent operation resolves independently from the same request origin; identical routes reuse established traversal, different routes traverse before processing. The original whole-connection deadline continues throughout.
- Add one authored ordinary CACHE node and API leaf link per canonical scene. Small: 5 nodes/4 links with northeast CACHE/E4. Advanced: 9 nodes/8 links with CACHE/E8 in the vacant upper-right position. Existing cards/topology links/capacities remain; CACHE capacity 10, normal conditions/infections/defenses/Integrity.
- Reuse shared `ConnectionsSshOverlay.prefab` for `ssh api` and `ssh cache`. One eligible READ PURPOSE, empty default, 0.25× processing WORK/0.5× LOAD, clear/change/persist/restart and existing keyboard/Duel/Escape ownership. Known IDs match without hidden hostility checks. API stays independent and live on actual routes.
- Adapt shared route geometry and the existing view/socket in place; preserve identity, palette, heartbeat and renderer assets. Inspect shows actual/historical destinations and reduced requirements. Recorder stores original definitions and committed routing/efficiency evidence. No positive rail status or SSH overhaul.
- Preserve authored Small 23.225807s / Advanced 20s means, 2.5× workload, unscaled business value, independent arrival clocks, PURPOSE mix, existing hostile weights/cadence, original capacities/objectives/tolerance. Only one generation copy helper changes to retain optional intended-destination metadata; sampling/scheduling recipes are untouched.

`LogicalConnectionDefinition` and `ConnectionSpawnPlan` remain original requests. `OperationStage.IntendedDestination` defaults to their original target. `OperationRouteResolution` and `SimulationHost.CommitOperationRoute` are a small execution boundary, not a comprehensive routing framework. No allocator arithmetic changes are required: it already reads the state's target/effective request. The state now exposes committed target/route and cached requirements; API/BLACKOUT/Probe and shared geometry read those same facts.

## Validation actually run

Unity 6000.3.11f1 compiled runtime and Editor code in an isolated imported project, leaving the user's original Editor open. The canonical additions and shared SSH binding were saved by Unity, retaining native serialization. The user's existing dynamic TMP fallback asset remains untouched and excluded.

- **221 focused deterministic assertions pass**: [exact checks](deterministic-checks.txt), [business outcomes](benefit-outcomes.csv), [Small decision](Connections-route-record.json), [Advanced decision](ConnectionsAdvanced-route-record.json).
- **306 graphical Play Mode checks pass**, including both scenes at 1920×1080 and 1280×720: [checks](graphical-checks.txt), 20 final [PNG captures](Captures/), per-viewport allocation snapshots and paired mixed-route events.
- **12 integrated regression groups pass; 3 inherited failures remain**: [report](regression-checks.txt). Passing groups: CACHE, API (131 assertions), Shift, Fragility (126), Demand (47), Workload (456), Integrity (327), Behavior, Traffic, Recorder, Canonical (971), Duel Concession.
- Core generator/check and Git diff checks pass. [Authored tuning/scope checks](scope-checks.txt) compare against the merged baseline.

The broad regression command exits nonzero for the same inherited Resource inspect-header geometry/identity styling, Advanced Overclock compact tool row, and stale Generated Identity tuning assertions. They are retained. The broad CACHE run predates the final added noise/deadline/Integrity/reservation/source/API-combination cases; those all pass in the final 221-assertion focused run. The only subsequent runtime clarification is the recorded CACHE-origin fallback reason; its dedicated focused test passes. Graphical startup also reports the inherited nonfatal Unity QuickSearch indexing exception. No human keyboard/IME or final balance acceptance is claimed.

The new topology required updating exact canonical/recorder node counts and including configured READ cover for CACHE. A legacy fixture inferred Small from exactly four nodes; it now also recognizes canonical Small by scene path so it still tests its original ordinary-only hostile setup. Its mechanical assertions are unchanged. This fixes newly exposed fixture assumptions without retuning production traffic or weakening the inherited failing assertions.

## Numerical and lifecycle evidence

| Case | Expected/observed |
|---|---|
| Empty policy | Original route, 100 WORK/5 LOAD, business 100, budget 30s |
| Cached READ | 25 WORK/2.5 LOAD, same business 100/budget 30s |
| Nonmatching identity / WRITE / EXECUTE | Original ordinary requirements/destination |
| Three cached requests, raw 8 each, capacity 10 | Effective 4 each; 3.3333 allocation each |
| CACHE baseline 1 / infection 2 / defense 4 | Remaining 3 shared 1.5/1.5; total 10 |
| Two committed cached classes, API 2×, capacity 5 | 3.3333/1.6667; changing API swaps live shares without rerouting |
| Mixed READ/WRITE/READ | CACHE→ordinary→CACHE decisions, one connection, three starts, final-only credit |
| Current policy changed/cleared | Current decision unchanged; next operation uses then-current policy |
| Original deadline during changed-route traversal | One ordinary timeout/failure, no partial credit or restarted budget |
| Cached planned logs | Same authored fractions/order/content, scaled processing thresholds, all earned |
| Cached hostile READ | Real infection at CACHE, one effect, zero business credit |
| Cached hostile keywords / 0× WORK factor | Timers/activation preserved; productive WORK stays paused |
| CACHE conditions/Integrity | Normal condition accepted; two genuine infections can compromise it |
| Advanced WEB bypass | Ordinary route excludes API; cached route includes API; next WEB WRITE removes benefit and reduces segment count |

Offline/unreachable CACHE falls back at resolution only. Locked active/traversing CACHE routes fail through existing BLACKOUT semantics. An adjacent-launch effect incompatible with CACHE falls back and still launches normally from its ordinary destination. CACHE-origin requests explicitly fall back because local zero-link path rendering/execution is not introduced. Cached natural noise is scaled once and recorded in effective LOAD units.

The controlled benefit fixture uses the real profile builder and existing concrete-plan queue at a fixed seed, with zero spread/outliers/noise and two manual scheduled requests at 8s. It samples base 3.2 LOAD/32 WORK and applies the retained 2.5× legitimate pipeline, producing ordinary 8 LOAD/80 processing WORK and **32 business WORK each**. Both target DB ordinarily. CACHE gives product 4 LOAD/20 WORK at CACHE while sales keeps 8/80 at DB. Both retain nominal 10s/15s service budgets and original 32 business value. Goal 64/failure limit 2 and capacity 10 are disposable fixture settings, not scene tuning.

- Empty CACHE: both time out, 0 business WORK, 2 failures, FIRED.
- Cache `product-lookup`: both finish, 64 business WORK in exactly two whole-job credits, 0 failures, SHIFT COMPLETE.

These outcomes match in both canonical scenes. The mixed-route fixture separately proves the same `r0` model/view/socket persists across READ at CACHE and WRITE at DB (Small) or WEB (Advanced, 3 segments→1). Live inspect updates to WRITE while retaining READ→CACHE in its timeline. Reusing the accepted renderer does not add another positive status or identity color.

## Actual Play Mode evidence limits

Cameras render native Canvas viewports at the requested sizes. Layout assertions include all authored node-card pairs and actual text dimensions; image review caught/fixed a too-long test ID and a capture taken before inspect's normal refresh interval. Final captures use `r0` and wait for the normal refresh. Existing terminal history deliberately persists across fixture resets, so earlier SHIFT COMPLETE/FIRED messages in a later screenshot belong to the previous fixture; the strip/current card identify the active sample.

The Editor-only driver injects dynamic Input System state and invokes the same SSH Update once per press because batch Editors have no focused Game view. Focus-routing settings are temporary and restored afterward. It exercises CACHE selection, running simulation, Escape/focus, Duel takeover/return, independent API keyboard selection and restart. A brief ordinary frame-driven interval proves SSH does not pause simulation; controlled progression otherwise uses the real host's deterministic steps with the test clock paused. No physical human typing or extended natural gameplay run is claimed.

## Manual Play Mode review

1. Open `Assets/Scenes/Connections.unity`, enter Play Mode at 1920×1080, then1280×720. Repeat with `ConnectionsAdvanced.unity`. Check the new CACHE card/link placement, other cards, node LOAD/status presentation and command focus.
2. `ssh cache`: check empty slot, eligible PURPOSEs and WORK/LOAD factors. Arrows/Enter select, `NONE / CLEAR CACHE` clears, Escape returns terminal focus. `ssh api` still selects its separate class priority. A live Duel should take typing and return it to the same configuration window afterward.
3. New matching READs should resolve to CACHE with reduced requirements. `inspect <id>` / `inspect -v <id>` should show READ→CACHE, `CACHE ·`, reduced WORK/LOAD and unchanged service budget. Changing CACHE must leave an already committed operation alone. API can still change its live share if its current route includes API.
4. Choose **Tools → Connections → Load CACHE Benefit (Play Mode only)**. You have eight simulation seconds to `ssh cache`, choose `product-lookup`, Enter and Escape. Inspect the product/sales IDs printed in Console. They should complete for 64 business WORK/SHIFT COMPLETE. Reload the fixture and leave CACHE empty to see both timeout/FIRED. The same original business value is delivered; CACHE relieves actual DB demand.
5. Choose **Tools → Connections → Load CACHE Mixed Route (Play Mode only)** and `inspect -v r0`. The longer human-review READ runs at CACHE, then WRITE moves to DB or WEB. Check identity/color/pill/socket continuity, destination/history, WORK reset and existing heartbeat. Clear CACHE while READ is active: the current READ stays committed. A BLACKOUT on its current path should interrupt normally, without failover.
6. `restart` clears both slots; it retains temporary in-memory fixture tuning. **Stop/reload Play Mode** to restore production authored settings. Do not save the disposable fixture into a scene.

Inspector: **SimulationHost → Simulation Host → CACHE configuration: one READ PURPOSE slot** exposes the node reference and 0.25/0.5 multipliers. Capacity is in that component's **Nodes → CACHE → Load Capacity**, default 10. One slot is the fixed V0 capability, chosen through SSH. Restart after edits for coherent run metadata. No calibration matrix is included.

## Limitations and deferred direction

CACHE changes topology-dependent attack choices and can itself be infected/compromised. Faster hostile READ completion can shorten investigation time; behavior/evidence content and effect semantics remain, with no hostile retuning. Extra traversal consumes the same service deadline and can make very short mixed arcs fail despite cheaper processing. Unknown future policies, local zero-link execution, cache population/freshness, upgrades and final difficulty remain outside this slice.

The design source retains the consolidated SSH/connection/gameplay TODOs: terminal sugar and splash/connection sequences; modular ncurses sections, Tab/Shift+Tab and arrow/Enter navigation with capability-sized windows; future FAST LANE/maintenance/honeypot; positive configuration feedback distinct from allocation versus efficiency while retaining slowdown/risk; compelling business scenarios, threat fairness/evidence, independent operational/investigative profiles, and richer/more prevalent LOADERs. None is implemented here.
