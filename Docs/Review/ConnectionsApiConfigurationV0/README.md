# API Configuration V0 — review

Scope: Connections Small and Advanced, following normal GitHub merge of human-accepted #135 at `79e2d90c5b88bf2fd04aa5ac0c5b85a60f361718`. [Implementation/Inspector contract](../../Connections/connections-api-configuration-v0.md). The new implementation PR stays unmerged for human review.

## Changes and tuning

- One shared authored `ConnectionsSshOverlay.prefab`, bound into both canonical scenes. `ssh api`, arrows, Enter, `NONE / CLEAR PRIORITY`, coordinated Escape. Configuration continues simulation, persists after close, yields to Duel and clears on restart.
- Stable known PURPOSE IDs and actual API-route membership apply a configurable 2× allocation weight, including API targets and matching hostile claims. Capped redistribution follows unchanged baseline/infection/defense reservations; inspect and recorder expose applied policy without classifying legitimacy.
- No requested LOAD/WORK, business value, timeout, node capacity, objective, hostile or PURPOSE tuning changes. Both scenes retain 2.5× workload. Small's arrival mean 23.225807s remains; the user's existing Advanced 20s Inspector edit is preserved, with its harmless Unity serialization alongside the scene binding changes. The user's dynamic TMP fallback asset is left local and excluded from this implementation commit.
- Deferred threat readability/fairness, evidence-based investigation, independent company/hacker pressure profiles and richer/more prevalent LOADER combinations are documented only. Company importance remains separate and unimplemented.

## Deterministic evidence

Final focused run: **131 assertions pass**. The broader regression run passes ten groups and retains the three inherited failures below; its API group predates the final added configurable-weight/kill/SHIFT COMPLETE assertions, which pass in the final focused run.

`Tools/Connections/Run API Configuration Witness` exercises actual canonical host and allocator state, not a separate model. [Checks](deterministic-checks.txt) and [outcomes](tradeoff-outcomes.csv) and actual [policy-change receipts](policy-change-events.csv) include:

| Situation | Expected allocation / result |
|---|---|
| Empty policy, two requests 8, capacity 10 |5 /5, original neutral arithmetic |
| One favored at 2× |6.6667 /3.3333 |
| Favored request 1, ordinary 4, capacity 4.5 |1 /3.5 after redistribution |
| Two favored 8 and one ordinary 8, capacity 10 |4 /4 /2 |
| Two favored 2 and one ordinary 8, capacity 10 |2 /2 /6 |
| Baseline 1, infection 2, defense 4, capacity 10 | Reserved 1+2+4, connections 2 /1 |
| Configured 3×, two requests 8 |7.5 /2.5 |
| Same class claimed by hostile | Same benefit, no hidden-intent filter |
| Route bypassing API | Original5 /5, even with matching PURPOSE |
| Policy change/clear/new connection | Subsequent update, correct existing/new shares |

Small is an API-star topology: every valid internal destination route traverses API. The bypass case uses Advanced's actual EXT1→WEB route, rather than inventing an impossible Small route. Both scenes independently test API-target eligibility and multiple target pools. Stable-ID versus identical/different display-label cases ensure labels never become policy identity.

The controlled fixture uses the existing generator and concrete-plan queue. Both single-READ profiles (`product-lookup`, `sales-export`) target DB from EXT1 and sample base LOAD 3.2  / WORK 32. The accepted 2.5× pipeline produces actual requests 8 /processing WORK 80, nominal 10s and service budget 15s, while business value stays 32. Only fixture state changes: capacity 10, baseline 0, neutral noise/spread, no new ambient/hostile arrivals, WORK goal 64, failure limit 2. Advanced destination/origin overrides are cleared in memory so both requests compete at the same actual target. Production authored values are untouched. Integrity remains enabled; the genuine brief DB overload can cost one Integrity point.

- No policy: both time out, 0 WORK, 2 failures, FIRED.
- Favor product: product completes, sales times out, 32 WORK, 1 failure.
- Favor sales: sales completes, product times out, 32 WORK, 1 failure.
- A separate goal 32 check ends SHIFT COMPLETE on the favored whole-job credit and freezes the unfinished peer without inventing another failure. Favored kill releases resources and records one ordinary failure with zero credit.

[Regression report](regression-checks.txt) runs API, Shift, Fragility, Independent Demand, Legitimate Workload, Integrity, Traffic, Recorder, Canonical and Duel Concession witnesses. The following existing failures are retained, not rewritten to force green:

1. ResourceWitness: inspect-header pill authored geometry/identity color assertion.
2. OverclockWitness: Advanced compact three-tool row.
3. GeneratedIdentityWitness: stale hostile activity weights/arrival interval assertion.

API assertions, weighted reservation cases and passing broader witnesses establish the new allocator behavior; the broad run remains nonzero because of these three known failures. Compilation uses Unity 6000.3.11f1; existing obsolete Unity API warnings remain. Graphical startup also logs the inherited nonfatal Unity QuickSearch indexing exception; it does not abort Play Mode checks.

## Actual Play Mode evidence

**110 checks pass; 16 final PNG captures** cover both scenes and both supported resolutions. Text bounds were corrected in the shared authored prefab after actual layout checks, including full choice descriptions.

[Graphical checks](graphical-checks.txt), native [captures](Captures/), and allocation/outcome JSON record the actual canonical UI at 1920×1080 and 1280×720. The runner injects keyboard state across real frames and explicitly invokes the window Update after a dynamic Input System update because a batch Editor has no focused Game view. Input focus routing is changed only for this driver and restored afterward. It submits the actual terminal command, applies policy, observes simulation advancing, closes with Escape, checks input focus, inspects the benefit, verifies the save-one/lose-one outcome, exercises Duel preemption/return, clears policy, restarts and verifies ending gates. It does not claim physical human keyboard testing or final gameplay balance.

A graphics-enabled isolated Unity project copy was used, leaving the user's original Editor open. The authored shared prefab/scenes were saved by Unity; runtime capture cameras and fixture settings are test-only and never saved into production scenes. Full simulation time is advanced deterministically for the controlled outcome; only a brief ordinary frame-driven interval proves configuration does not pause gameplay.

The policy takes effect on the next allocation update. Its immediate event snapshot can truthfully carry the new selected PURPOSE with old allocation state. The allocation JSON is sampled after an update and includes applied weight/source, actual requests/grants, business value, service timing and unchanged total capacity.

## Manual review

1. Open `Assets/Scenes/Connections.unity`, enter Play Mode at 1920×1080, then1280×720. Repeat in `ConnectionsAdvanced.unity`. Keep the current 2.5× workload and authored arrival intervals. `help` lists `ssh api`.
2. Type `ssh api`. Check node/current empty policy, all known PURPOSE choices, readable selection/hint. Use arrows and Enter to favor a PURPOSE. Watch the running network, Escape, then continue typing immediately. Reopen to confirm persistence. Change and clear the selection. Unsupported `ssh db` returns a quick unavailable message.
3. `inspect <id>` and `inspect -v <id>` a matching active route: PURPOSE header shows `API ×2 ·`. LOAD requests and business value should not increase; the shared network presentation remains unchanged. A matching hostile claim gets the same treatment.
4. For a quick deterministic tradeoff, while playing choose **Tools → Connections → Load API PURPOSE Tradeoff (Play Mode only)**. Immediately `ssh api`, choose `product-lookup` or `sales-export`, Enter, Escape. Use the IDs printed in Console for inspect. The favored class should finish for 32 WORK; the other times out. Reload the fixture and leave policy empty to see both lost/FIRED. Select the other PURPOSE to reverse the saved class.
5. Test a Duel arriving while SSH is open: typing goes to Duel, then returns to SSH. Closing SSH must return terminal focus without replaying a held Enter/Escape. Check a passive call/message does not obscure useful configuration text.
6. `restart` clears policy and closes SSH. Stop/reload Play Mode to discard the fixture's in-memory tuning; restart alone deliberately retains those temporary Inspector settings.

For weight tuning select **SimulationHost → Simulation Host → API configuration: one PURPOSE priority slot → API Allocation Weight**, default 2. Arrival frequency remains at **Connection Traffic Director → Independent business demand → Legitimate Mean Arrival Seconds**. Restart after Inspector edits to refresh run metadata. This pass performs no calibration matrix.
