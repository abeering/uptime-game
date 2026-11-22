# Validation, reproduction and limits

Actual **Unity6000.3.11f1 Editor on macOS**, using disposable project copies. Canonical scenes and prefabs were opened/read but never saved. Runtime source, serialized scene settings and actual execution are the authority. The original #124/#125 evidence remains intact.

## Checks actually run

| Check | Expected | Observed |
|---|---|---|
| V1 focused witness | Reproducible nominal requests/recipes; no kill/outage replenishment; reset/coarse scheduling; scoped tuning; metadata and admission failures | [30 assertions passed](deterministic-checks.txt) |
| Screening replay after benign-only weight isolation | Same controlled gameplay as prior concentration screen with hostiles off | [Four complete gameplay event/snapshot hashes match exactly](screening-replay-checks.txt), healthy/30s source in both scenes |
| #125 independent witness | Existing clock/lifecycle/admission checks remain valid | [32 passed](v0-deterministic-checks.txt) |
| #124 WORK witness | Original authoritative accounting/ending behavior | [52 passed](parent-work-checks.txt) |
| Original actual quota calibration | Same18 ending, time and credit-prefix predictions | [All18 exact checks passed](parent-quota-checks.txt) |
| Canonical parity | Serialized bindings/topologies/catalogs/mechanics retained | [881 passed](canonical-parity-checks.txt) |
| Existing gameplay and terminal/concession suites | Applicable prior regressions retained | [Gameplay suite results](parent-regression-checks.txt), [terminal results](terminal-regression-checks.txt); aggregate exits1 for two inherited UI assertions |
| Actual graphical Play Mode | Both scenes, two real16:9 viewports, real source/kill/expired boost, inspect focus and matched plans | [42 passed](graphical-checks.txt),16 captures |
| Data reduction | All nominal/independent controlled pairs preserve scheduled demand/complete recipe prefix; no admission loss | 338 retained Unity runs,232 paired comparisons checked |
| Whitespace / generated Core | Clean changes and fresh derived source snapshot | Checked before commit |

The two aggregate failures match #124/#125: Overclock's `ConnectionsAdvanced compact three-tool row` and Resource's `inspect header pill keeps authored large geometry and identity color`. Assertions/layouts were not relaxed. The [aggregate receipt](validation-checks.txt) correctly records failure alongside the passing focused suites. Graphical startup also emits a Unity QuickSearch `SearchDatabase.EnumerateAll` index exception before successfully completing captures; this is not claimed as a clean Editor console.

The initial concentration fixture used catalog targets, which missed Advanced's JOBS/RPT scene overrides. Those misconfigured concentration rows were excluded and rerun using actual destinations. Retained nonconcentration rows are unchanged. The first corrected screen used existing shared weights with hostiles disabled; the final prototype isolates benign weights, and four exact full gameplay replays establish equivalent controlled results. This correction does not rewrite historical #125 measurements or hide an unsuccessful intended configuration.

## Repeat in a disposable checkout

Use the existing project/version/license setup. Preserve unsaved user scenes. The methods open both scenes without saving; validation writes reports in historical witness folders **inside the disposable checkout**, so keep the committed historical artifacts unchanged.

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -executeMethod Uptime.Connections.EditorTools.CongestionAmbientWitness.Validate -logFile /tmp/congestion-validation.log
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -executeMethod Uptime.Connections.EditorTools.CongestionAmbientWitness.NominalFirst -logFile /tmp/congestion-nominal.log
```

Run the other suite methods the same way: `Comparisons`, `Variations`, `Headroom`, `Concentration`, `Full`, `Quotas`, `Natural`, `AdvancedTarget`, and `AdvancedShifts`. The final method writes both advanced quota/natural files. Each method overwrites its own V1 suite file; no hidden planner or external simulator is needed. `Run Congestion Ambient V1 Witness` is also under Tools/Connections. `Validate` needs the committed `concentration.json.gz` and original #124 calibration file already present on this branch.

```sh
"$UNITY" -batchmode -projectPath "$PWD" -executeMethod Uptime.Connections.EditorTools.IndependentDemandCapture.Run -demandEvidence Docs/Review/ConnectionsCongestionAmbientV1 -demandPolicy NominalOccupancy -demandLoadScale 2 -demandDbWeight 2.5 -demandLoaderLoad 8 -demandLoaderSurvival 20 -demandRecoveryAt 116 -logFile /tmp/congestion-graphics.log
python3 Docs/Review/ConnectionsCongestionAmbientV1/summarize.py
```

Do not pass `-quit` or `-nographics` to the capture method: it enters/exits actual Play Mode and exits batch mode itself. `summarize.py --plots` also recreates both graphs when matplotlib is installed; delivered graphs used matplotlib3.9.4. The Python file only reduces saved Unity traces. Optional graphical rate/goal arguments remain in the shared capture helper; V0 defaults remain independent-demand capture behavior when V1 arguments are absent.

## Measurement definitions

- Diagnostic horizon360 seconds, unreachable target1,000,000, ordinary Integrity/CORE rules retained. Diagnostic `InsufficientWork` is expected, not an actual900/1,200-quota failure. A real CORE ending censors its trace. Actual-quota groups preserve900/1,200 and360s; earlier success also censors subsequent traffic/threat exposure.
- Screening seeds1701/3141; four-seed follow-ups add1702/9001. Fixed-step calls are0.02s, with real host segmentation and approximately one-second samples. Each pair shares the same scheduled requests and complete definitions/noise seeds until its common ending. Prefix matching permits real CORE truncation, or quota success in quota-enabled groups, without inventing arrivals after an ending. Mixed shifts make no exact demand/identity pairing claim because ordinary hostile producers share the existing ID pool.
- All retained independent/nominal measured arrivals are admitted, waiting/unadmitted zero. The focused witness separately verifies finite-ID exhaustion remains counted. Population-responsive pairs deliberately change demand when occupancy changes, so sampled causal-difference fields are blank for them.
- The steady source uses real LOADER zero-productive-rate/withdrawal behavior and8 LOAD. It registers around90s, travels the actual EXT1 route and activates normally. Survival5/10/20/30s begins at activation; at30s normal withdrawal may precede the kill boundary. An overlapping source registers five seconds after first activation, has its own Connecting travel and20 active seconds. Post-source metrics begin only after **both** sources resolve. No infection/lingering penalty is added by the fixture.
- Screens use fixed DB. Follow-ups choose the node with highest legitimate requested LOAD in the healthy sample at/after90s, with stable scene-order tie breaking. That exogenous target is fixed across the pair and recovery tools. It does not read the disrupted future or implement new enemy targeting. `fixtureTargetId`, load, survival, source count and overlap offset are retained per run.
- Tools act after source removal: existing Overclock on that target, or at most one eligible known legitimate demanding job killed, preferring highest visible desired LOAD then lowest current progress. The controlled workload makes all other connections legitimate; this is an idealized diagnostic decision, not demonstrated hidden-threat detection or human typing. No eligible victim means no kill. Commands/actual kills and foregone WORK are recorded.
- Active averages integrate legitimate Active counts over observed simulation time; Connecting is separate. Peak visible is logical Active/Connecting state count, not every Payload/Worm/presentation object. Healthy peak and paired accumulation should not be conflated.
- Actual global contention is summed legitimate desired-minus-allocated LOAD>.01; actual severity integrates it at simulation boundaries. Raw per-node records contain requested/available/capacity integrals and peak/excess values. Resources, displayed status and WORK progression all use the same allocator.
- Sampled extra metrics align each run with its healthy sample at the same floored simulation second, through their common horizon. Positive extra shortfall sums only increases; signed extra contention subtracts healthy busy samples. They omit subsecond details and should be read as approximate LOAD·seconds/seconds. A positive severity difference can occur while both runs are already busy. The longest extra-shortfall interval uses difference>.1 LOAD. These are not new authoritative simulation variables.
- Outstanding WORK is actual remaining current work plus future authored work. The debt-clear estimate is the first five-second window within1 extra WORK of healthy. `-1` means unobserved before censoring. Killing legitimate work can erase this debt while losing future credit; consult `lostWork`/credit deficit.
- `recoveryDelay` is the first ten-second no-shortfall window's start after removal; `normalCapacityRecoveryDelay` additionally excludes an active Overclock. Neither promises that later healthy bursts cannot contend. The sampled healthy quiet timestamp prevents attributing the entire first-quiet wait to an attack.
- Completed-operation actual time excludes Connecting/travel. Authored nominal is WORK/LOAD; natural noise/outliers can change actual duration. Paired operation means compare common completed ID/index pairs; incomplete operations are censored, never assigned zero delay. Generation decisions remain on operation rows.
- Confirmed WORK is credited only by ordinary host completion. Peak credit deficits can be whole, briefly delayed operation chunks; they are not permanent losses. The CSV separately retains common-horizon deficit, deficit area, lost WORK and unfinished materialized work.
- Mixed shifts use existing ambient enemies, SPREAD/BREACH, resources and real seeded Duel races. Limited visible defense acts at most once per6 seconds, intercepting a visible final-hop Payload or Overclocking the most overloaded node; it does not scan, CLEAN, solve Duels or use hidden threat labels. It is a reproducible limited policy, not a skilled-human model. The higher Advanced point has only a two-seed mixed/quota check.

Real keyboard/IME, full human shifts, sustained crowding at peak density and Packet Play Mode remain unverified. The sixteen captures include a real compromised DB after a20-second source; existing Integrity damage is not undone when LOAD recovers. No claim of complete infrastructure recovery or final balance is made.
