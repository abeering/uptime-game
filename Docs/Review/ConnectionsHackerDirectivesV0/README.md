# Hacker Directives V0 — review guide

**Storefront follow-up:** The same PR now adds a scene-only legitimate `storefront-page` template. See the [patch review and before/after traffic evidence](StorefrontPatch/README.md). The 24 runs and WEB-tell measurements below are the preserved **pre-storefront baseline**, not results for the updated seven-template scene. SPREAD still uses unchanged ambient selection, but the new catalog intentionally changes its seeded recipe/outcome sequence relative to the six-template parent.

Connections implementation experiment, November 20, 2026. Branch `feat/connections-hacker-directives-v0`, based on Integrity PR #117 at `0c1fd9ab4ae8e7a9094623052ecaab23261003ad`; PR target is `feat/connections-integrity-core-breach-v0`. Maximum Pressure #116 is inherited; Living Shift #115 is excluded. No branches are merged.

The [contract](../../Connections/connections-hacker-directives-v0.md) describes the selector and generation seams. The [manifest](FILES_CHANGED.txt) lists every changed file. The original scene change added one Inspector component on SimulationHost in `ConnectionsAdvancedIntegrity.unity`, authored as SPREAD / 60 seconds / 70%; the follow-up adds a serialized storefront PURPOSE template there. All other scene and prefab assets remain byte-identical to the parent. There is no presentation hierarchy change, new player command, attack type, damage source or pressure tuning.

## What the natural experiment shows

BREACH reaches CORE sooner in all four unattended seeds with the same opportunity/category prefixes. Mean unattended loss time is **200.341 seconds versus 268.286 seconds** for SPREAD, a 25.3% reduction in this four-seed sample. Both directives lose all four unattended runs. Every BREACH loss takes `EXT1 → WEB → AUTH`; that route costs four initial hits, while longer alternatives cost more. This is a topology/Integrity result, not a hard-coded preference for AUTH. The model witness uses entirely different IDs and multiple ingresses/criticals.

| Seed | SPREAD unattended loss | BREACH unattended loss | SPREAD visible-defense loss | BREACH visible-defense loss | BREACH kill-all loss |
|---:|---:|---:|---:|---:|---:|
| 1701 | 403.517 | 244.535 | 402.838 | 165.278 | none by 599.963 |
| 1702 | 205.117 | 119.532 | none by 599.963 | 206.957 | 451.091 |
| 3141 | 331.157 | 314.945 | none by 599.963 | 317.024 | none by 599.963 |
| 9001 | 133.352 | 122.351 | 347.928 | 122.351 | none by 599.963 |

SPREAD kill-all survives all four windows. BREACH kill-all survives three, so killing every visible connection remains a strong survival exploit, although it no longer protects every tested seed. Seed 1702's kill-all loss comes from four infection strikes and two Duel strikes; its graph remains one hit away for 360.147 seconds. Independent threats still matter after connections are killed. This sample does not establish indefinite immunity or a general win rate.

The limited visible-defense policy loses all four BREACH runs, versus two SPREAD losses. It delays BREACH seed 1702 to 206.957 seconds, but seed 1701 loses earlier than unattended. Its coarse intercept/overclock interventions do not guarantee monotonic improvement and do not investigate or clean hidden damage. These are measurements of the existing policy, not evidence that every form of defense is ineffective. We did not strengthen attacks or modify tuning to improve the result.

Across unattended BREACH runs, 76 opportunities have an eligible offensive target: 52 enter the preferred branch and 39 actually change target. Across all twelve BREACH runs, 249 of 356 eligible opportunities enter that branch (69.9%); 200 change target. Unsupported categories, the ambient probability branch and generator/scheduling failures remain separately observable. Shorter runs naturally observe fewer total opportunities; “same budget” means unchanged opportunity generation, not equal lifetime totals after one run ends.

Four fixed paired seeds provide encouraging evidence for the target-selection hypothesis, not statistical significance, fun validation or settled balance.

## Evidence

- [RESULTS.md](RESULTS.md): every run's loss/path, exact one-hit-away duration, eligible/preferred/changed counts and fallbacks.
- [metrics.csv](metrics.csv): additionally includes hostile target distributions, per-node/per-source damage, fallback reasons, commands and observation horizons. Target distributions count selected ambient opportunities, including failed scheduling and targetless Worms; they do not count every secondary child as a new ambient opportunity.
- [damage.csv](damage.csv), [pressure-summary.json](pressure-summary.json): every authoritative strike with cause/time and final Integrity graph.
- `SPREAD/Runs/` and `BREACH/Runs/`: compressed complete recorder logs and one-second graph samples for all 24 runs.
- [baseline-equivalence.txt](baseline-equivalence.txt): SPREAD exactly matches all twelve committed parent runs' gameplay events, snapshots and configuration. Only additive directive observability and wall-clock metadata are removed; sequence/action references are normalized, preserving their relationships. No gameplay values or causal events are omitted.
- [clock-category-comparison.csv](clock-category-comparison.csv): all twelve common observed prefixes have identical schedules, categories and category rolls. [replay-checks.txt](replay-checks.txt): seed 1701 exactly replays all events/snapshots/topology samples for each mode and all three policies.
- [analyze.py](analyze.py): reproduces the derived tables and parent comparison from the committed raw evidence. It computes one-hit-away duration using minimum **remaining Integrity sum**, integrated at damage-event boundaries; the parent's “additional compromises” samples count nodes and are a different metric.

Natural runs use seeds 1701, 1702, 3141 and 9001, fixed `.02`-second host steps, and up to 30,000 steps or CORE BREACHED (approximately 599.963–600 seconds due to float accumulation). They call the parent's existing `IntegrityPressureWitness.Observe` with a development configuration callback and separate evidence output path; policies and threat settings are unchanged. No forced damage or hidden-state decisions are added. The limited policy checks every six seconds, intercepts visible final-hop Payloads or overclocks the most overloaded visible node. Kill-all checks once per second when not blocked by an existing Duel/result; normal Duel races receive no player typing. Both policies remain unaware of hacker intent.

## WEB targeting tell (pre-storefront baseline)

[transit-target-tell.csv](transit-target-tell.csv) audits canonical destinations and queued definitions, separating target from transit. The authored benign targets are API, AUTH, DB, JOBS and RPT. **WEB is the only internal node absent from all eligible ambient benign destinations.** Its mere inclusion in a route is common for benign traffic and proves nothing.

Every direct WEB-targeting connection in the 24 recorded runs is hostile: 66 queued plans under BREACH and 21 under SPREAD, with zero benign WEB-targeting plans. Thus, within this experimental traffic contract, a player can kill a connection known to **terminate at WEB** without investigating its PURPOSE, WORK or activity logs and without killing a legitimate ambient connection. This tell predates BREACH but becomes more frequent with directed carriers. It is a reliable classification shortcut for those connections, not a survival guarantee: standalone Payloads, Worms, direct Duels, other targets and already-applied effects remain. We did not run a WEB-only defense policy or claim a measured survival rate for it.

No other current internal node has the same structural transit-only tell. A future scene with another internal node excluded from all legitimate eligible target sets would inherit it. This is a separate traffic/design question; this experiment intentionally does not manufacture benign WEB recipes to hide it.

## Automated validation

Unity **6000.3.11f1** compiled and ran in `/private/tmp/uptime-integrity-validation`, an isolated project copy because the working project was already open.

- [deterministic-checks.txt](deterministic-checks.txt): **375 directive assertions**: hidden-state path cost, multiple ingress/critical nodes, zero-cost cycles, disconnection, 60-second commitment, invalid/complete reevaluation, seeded ties/replays, weighted preference, no direct damage, legal copied routes and preserved recipes/random draws, all seven categories, fixed launcher/Worm fallbacks, recon target retention, 600-second controlled category/clock comparison, and other-scene isolation.
- [integrity-checks.txt](integrity-checks.txt): **327 existing Integrity assertions** pass, including damage, reveal rules, overload/overclock, infection, Duel attribution, allocation examples, CORE breach, frozen state and restart.
- [regression-checks.txt](regression-checks.txt): **14 existing witnesses pass**. Resource's inspect-header geometry/color assertion and Overclock's Advanced tool-row assertion fail, exactly as documented on Integrity and its untouched parent. The aggregate regression command therefore exits 1. See the [parent reproduction](../ConnectionsIntegrityV0/validation.txt); no assertion was weakened.
- [play-mode-checks.txt](play-mode-checks.txt): actual graphical Play Mode runs BREACH for 45 fixed-step simulation seconds, submits node/connection inspect through the input field, checks active text for intent leakage, submits terminal `restart`, verifies fresh two-point state/no offensive, and exactly replays seeded offensive/target receipts. This is scripted, not human keyboard/layout validation.

The graphical Editor startup again logged Unity Search's existing `SearchDatabase.GetDefaultSearchDatabase` indexing exception. The Play Mode witness completed and the Editor exited 0. No new screenshot/layout claim is made; no visible hierarchy was changed. Parent visual captures remain presentation evidence for the unchanged interface.

## Reproduce and review in Unity

On a disposable project copy with the same Unity version:

```text
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.HackerDirectiveWitness.Run -quit -logFile <focused.log>
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.HackerDirectivePressureWitness.Run -quit -logFile <natural.log>
Unity -batchmode -nographics -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.HackerDirectivePressureWitness.Regressions -quit -logFile <regressions.log>
Unity -batchmode -projectPath <copy> -executeMethod Uptime.Connections.EditorTools.HackerDirectivePlayWitness.Run -logFile <play.log>
python3 Docs/Review/ConnectionsHackerDirectivesV0/analyze.py
python3 scripts/build_uptime_project_core.py --check
```

The regression wrapper copies existing check reports to this evidence folder; its underlying parent witnesses still write their normal outputs in the disposable copy. Preserve committed parent evidence in the working repository. `ConfigureScene` is an explicit authoring helper; normal checks do not rebuild scenes or reset your Inspector choice. Natural evidence overwrites only this experiment's mode subfolders.

Manual review still required:

1. Open `Assets/Scenes/ConnectionsAdvancedIntegrity.unity`, select SimulationHost, and compare SPREAD and BREACH in HackerDirectives. Uncheck Randomize Seed on ConnectionTrafficDirector and set one of the four seeds. Confirm other scenes have no directive component.
2. Enter Play at 1920×1080 and 1280×720. Check the unchanged one-third terminal/two-thirds graph layout, crowded logical labels, `inspect web`, `inspect -v <id>`, scroll history, and keyboard focus. No hacker route, hidden Integrity, directive label or new badge should appear. Existing visible compromise/overload information is expected.
3. For internal review, inspect the recorder's `hacker_offensive_selected` and `hacker_target_decision` entries. Follow commitment/reassessment, eligible targets, original/final choices and launcher/Worm reasons. Compare them with separately recorded activity success and actual damage receipts; a preference must not itself strike Integrity.
4. Type `restart` in each mode. Confirm the configured mode persists and seed/state replay, fresh Integrity, neutral cards and clean timers return. Let an unattended BREACH seed reach CORE and confirm the existing frozen loss/path/restart behavior.
5. Try defending through ordinary visible commands and separately try kill-all. Review the WEB destination shortcut deliberately, distinguishing a direct target from a transit hop. Evaluate attention and fun through human play; the scripted policies do not establish either.
