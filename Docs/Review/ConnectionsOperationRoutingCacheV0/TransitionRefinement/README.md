# PR #137 — operation route transition refinement

Refines the existing CACHE implementation in place; keep PR #137 draft and unmerged. Earlier full-origin transition captures remain as historical evidence in the parent folder. This is Connections work; scenes, topology, tuning, shared renderer assets, colors and heartbeat defaults are unchanged.

## Result and timing

Changed routes enter real `Lifecycle.Rerouting`, distinct from initial `Connecting`. The host retains the longest common prefix as already reached and traverses only the new suffix. Unchanged routes start the next operation directly. The old operation releases its processing allocation; REROUTING allocates no processing LOAD and advances no WORK. Establishment time, identity/aliases/color, original service deadline/value, operation history and exact-once whole-connection accounting persist.

| Case | Small / Advanced behavior |
|---|---|
| CACHE→DB | Retain EXT1→API / EXT1→WEB→API; charge only API→DB, 0.6s |
| CACHE→API | New route is already established; charge 0s and resume processing immediately |
| API→CACHE | Keep existing prefix; extend API→CACHE for 0.6s, with no obsolete tail to retract |
| API-origin DB→CACHE | Divergence is the origin; no prior links retained, charge one new link, 0.6s |
| Identical route | No REROUTING state/event or additional travel |

Strict-prefix shortening still enters and exits the real state at the same simulation timestamp. Its old tail retracts cosmetically while processing can already run at the retained destination. Retraction never adds simulation time. For nonzero travel, it uses the existing relayout duration, bounded to half the traversal interval; suffix extension uses the remainder of that interval and finishes with mechanical establishment. Pure route growth begins extending immediately.

The same graphic, view, identity owner, socket and heartbeat remain. The view retracts its previous geometry to divergence, preserving shared lanes, then extends the committed suffix using progressive establishment rendering. Existing lane/pill packing temporarily follows the actually drawn route. Shared-prefix offsets survive the geometry switch. No whole-path fade, new object/renderer/palette, heartbeat reset or decorative state is added. The original terminal inspect card updates to the next operation/destination while preserving its preceding operation history.

## Actual real-time Play Mode motion

These clips come from native graphical Unity Play Mode with the host enabled and timeScale 1 during recording. Encoding preserves observed simulation-time frame durations from the accompanying JSON; raw frames remain outside the repository. Cameras use native 1920×1080 /1280×720 Canvas viewports. No simulation is invented or stepped to fake the motion.

| Scene | Divergent route at 1280×720 | Additional shapes |
|---|---|---|
| Small | [CACHE→DB: retract, retain prefix, extend](Motion/Connections-1280-divergent.mp4) | [Shorter](Motion/Connections-1280-shorter.mp4), [longer](Motion/Connections-1280-longer.mp4) |
| Advanced | [CACHE→DB: retract, retain prefix, extend](Motion/ConnectionsAdvanced-1280-divergent.mp4) | [Shorter](Motion/ConnectionsAdvanced-1280-shorter.mp4), [longer](Motion/ConnectionsAdvanced-1280-longer.mp4) |

[Small 1920×1080](Motion/Connections-1920-divergent.mp4) · [Advanced 1920×1080](Motion/ConnectionsAdvanced-1920-divergent.mp4) · [28 still captures](Captures/). Inspect's normal refresh interval remains; the clips show REROUTING in the live card. Older terminal history persists between disposable fixture resets; the current lower card and UPTIME/WORK strip identify the current sample.

The recording fixture uses the existing CACHE mixed-route helper with short READs and long WRITEs, or explicit intended destinations for growth/shortening. It changes only disposable in-memory fixture settings. Stop/reload Play Mode to restore normal authored traffic/settings. The original Editor was left open; validation used the isolated imported project.

## Lifecycle audit

| System | Deliberate REROUTING handling |
|---|---|
| Fixed-step boundaries / arrival | Resolve before selecting the next boundary; retain reached prefix and emit arrivals only for new links. Zero-link shortening is immediate. |
| Processing allocator / node usage | `IsDemanding` remains processing-only. Previous allocation releases; new allocation resumes after arrival. Existing allocator arithmetic is untouched. |
| Service / Health / business | Keep initial establishment and original deadline; timeout can occur during REROUTING. No repeated Health activation, intermediate credit or duplicate failure. |
| Behaviors | Connection-wide timers advance; future operation timers wait until establishment. An audit test caught the old operation-only early return; a dedicated connection advancement seam fixes it without changing active-operation timing. |
| Identity / commands | Canonical ID, palette ownership, alias hopping/reservation, reactive addressing and kill remain live. Initial Pending/Connecting identity startup is not repeated. |
| API policy / Probe | API reads committed mechanical routes. Probe knowledge uses reached new-route edges; obsolete cosmetic tails cannot reveal discarded-route activity. |
| BLACKOUT / Isolation | Retained reached prefix interrupts immediately; future offline nodes cancel on arrival. Obsolete tail is cosmetic and cannot trigger cancellation/failover. |
| LOADER clocks | Activated connection lifetime/withdrawal clocks remain live while rerouting; initial connecting does not start an attack prematurely. |
| Inspect / logs / danger | Compact PURPOSE prefix, verbose status and empty-log status say REROUTING. Existing deadline-danger mapping remains applicable; no new colors. |
| Recorder / counters | Emit rerouting start/finish plus previous path, shared/new counts and timestamps. Rerouting remains an established live connection, not anticipated demand again. Per-connection lifecycle distinguishes nonprocessing travel. |
| Renderer / packing | Retain visible prefix, retract old suffix, extend new suffix using the same shared view; lanes and pills follow currently visible geometry. Socket remains the same object. |
| Reset / end / pauses | Existing reset/end gates remain; shared scaled presentation respects host, timeScale and Duel pause behavior. |

`Rerouting` is appended to the enum so old numeric values remain compatible. `IsTraversing` deliberately groups only initial establishment and rerouting where shared arrival behavior is appropriate; `IsLive` groups addressable connection states. Resource and behavior handling still distinguish processing from travel.

## Checks actually run

- Unity 6000.3.11f1 compiles runtime and Editor code.
- [108 deterministic assertions](deterministic-checks.txt) pass, with [exact timings](timings.csv): identical/shared-prefix/shorter/longer/origin divergence; prefix/future/obsolete-node BLACKOUT; timeout/kill during REROUTING; retained identity and alias hopping; connection versus operation timers; old-target peer allocation release and new-target resumption; original budget/value and exact-once boundaries/accounting.
- [59 real-time Play Mode checks](graphical-checks.txt) pass. The same model/view/socket/graphic/heartbeat and identity color persist, the established first edge remains fully visible, processing stays zero while REROUTING, and inspection shows the state. Eight clips and 28 stills cover both scenes and both supported resolutions, including zero-time shortening and growth without retraction.
- Final integrated [regressions](regression-checks.txt) retain 12 passing groups plus this transition witness. Existing CACHE 221, API 131, Fragility 126, Demand 47, Workload 456, Integrity 327 and Canonical 971 assertions pass, as do Shift, Behavior, Traffic, Recorder and Duel Concession.
- Three inherited failures remain: Resource inspect-header geometry/identity styling, Advanced Overclock compact tool row, stale Generated Identity tuning. The aggregate command exits nonzero for them. Graphical startup also logs the inherited nonfatal Unity QuickSearch indexing exception. No human balance/keyboard acceptance is claimed.
- [Scope checks](scope-checks.txt), Core generator/check and Git diff checks pass. Scene/prefab authoring and all demand/workload/objective/capacity settings are untouched. The user's local TMP fallback cache stays untouched and uncommitted.

The motion fixtures contain no modifiers; the subsequent final deterministic/regression run additionally validates the connection-timer audit fix. Actual active-operation behavior remains unchanged and its existing regression witness passes.

## Manual review

1. Enter either canonical scene in Play Mode. Choose **Tools → Connections → Load CACHE Mixed Route (Play Mode only)**, then `inspect -v r0` (or compact `inspect r0`). Observe the same identity/history while the old tail retracts to divergence. Small extends from API to DB; Advanced's default shorter route to WEB resumes immediately while the tail retracts. The eight movies above also show divergent DB routes in both scenes.
2. Check the established prefix never disappears, heartbeat/color stay coherent, WORK stops during real rerouting, the LOAD badge drops to 0 during travel, and processing resumes only after new links establish. Strict-prefix shortening has no mechanical waiting period.
3. Use `restart`, then normal traffic with `ssh cache`/`ssh api`. Policy changes affect future route commitments and live allocation respectively; no committed route fails over. Stop/reload to discard fixture tuning.
4. Outside Play Mode, run **Tools → Connections → Run Route Transition Witness** for deterministic edge cases. Existing **Run Operation Routing CACHE Witness** remains available with explicit REROUTING expectations.

V0 limitation: retraction is presentational. It does not simulate a teardown resource cost or delay mechanics. If a render frame misses an entire very short transition, the view catches up to committed state; the recorder retains exact mechanical boundaries. No new colors, broader SSH redesign, failover or topology changes are included.
