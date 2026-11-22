# Connections — Congestion & Ambient Traffic Experiment V1

Exploratory implementation, November 22, 2026. Based on PR [#125](https://github.com/abeering/uptime-game/pull/125), `7c98f4fc20fe75e3af808e3a05677f76f696c19e`, stacked on #124. Separate branch `experiment/connections-congestion-ambient-v1`; no modification or merge of main, #124, #125 or #115. Packet, scenes, prefabs and canonical tuning remain unchanged.

## Small scheduling seam

`ConnectionTrafficDirector.BenignArrivalPolicy` adds `NominalOccupancy` after the two existing enum values. PopulationResponsive remains zero/default. These are disposable research configurations in one director, not three permanent game modes. The existing PURPOSE builder, concrete queue, registration, lifecycle, authoritative allocator, recorder and WORK ledger remain common.

Nominal occupancy feeds the existing coverage probability/mean response with an expected population ledger. When a selected ambient request produces a plan, it reserves one slot until:

`scheduled time + route links × authored travel seconds/link + Σ(authored operation WORK / LOAD) + .02s × operation transitions`

The estimate uses the final concrete operations, including sampled static outliers. It ignores later natural LOAD noise, dynamic modifiers, allocation, outages, actual completion and termination. The .02-second transition allowance approximates this project's fixed-step operation handoff; nominal lifetime is an estimate, not a second execution. Slots expire by simulation time, even if the actual request is still active or was killed/canceled earlier. No actual active/connecting counts, cancellation memory, Blackout state or Overclock capacity enter this policy's response.

Opportunities advance from their ideal due times and retain every due opportunity under coarse scheduler calls. Expiries are evaluated at each due time, not prematurely at the end of a coarse call. Explicit ambient disable creates no new requests; nominal reservations continue aging, and re-enable starts a fresh seeded opportunity wait. Authored Witness mode suppresses ambient requests. Reset clears the ledger and replays the seeded clocks/recipes. No whole-shift preplanning is required.

Only the ambient producer's selected requests reserve slots. Manual spawns and consequence-generated traffic do not automatically become ambient business demand. A future directed-company producer must deliberately define its relationship to ambient reservations rather than accidentally replacing killed jobs through actual occupancy. See the [traffic-source architecture map](connections-traffic-source-map-v1.md).

## Focused tuning probes

Two opt-in Inspector controls isolate legitimate workload experiments:

- `Experimental Benign Load Multiplier` defaults to 1. It scales each sampled normal operation's LOAD and WORK together before existing benign-outlier composition, preserving normal authored WORK/LOAD duration. PURPOSE structure, routes and kind/log recipes are retained. Natural noise and dynamic outliers may still alter actual duration. Scaling also changes real completed WORK per operation; quotas are not normalized or retuned.
- `Experimental Benign Purpose Weights` is a sparse, validated array of existing profile-ID weights. Only ambient benign PURPOSE selection reads it. Missing entries retain existing effective weights. Manual commands, hostile selection/recipes, spawners and directed target preferences retain their prior inputs. The witness's concentration settings favor PURPOSEs whose **actual scene destination** is DB; Advanced's inventory→JOBS and export→RPT overrides are respected. No operation is retargeted merely to manufacture contention.

Existing population-goal fields supply the nominal-headroom variations. All experiment settings require restart after edits. No allocator, enemy rule, natural-noise function, CORE damage, Overclock, scoring/credit, timeout, ending or quota rule changes are included.

## Observation and admission

Selected nominal requests reuse the separate arrival/admission accounting introduced in #125. They record `benign_demand_arrived` and `benign_demand_admitted`; the constant `demandMeanSeconds` is zero for this nonhomogeneous selection process. `nominal_coverage_opportunity` records ideal due time, expected population before/after, selected request's nominal lifetime, goal, probability/roll, next mean/interval and failure/skip reason. `plan_queued` source is `nominal_benign`. Snapshots expose nominal population; metadata includes policy, LOAD multiplier and each profile's effective benign weight separately from the ordinary ambient weight.

Registration admits a Connecting attempt; it does not guarantee establishment or completed WORK. The existing queue has no busy-network admission cap. Finite-ID exhaustion or an empty weighted catalog remains an explicit unadmitted request, never an invented waiting customer. All retained measured independent/nominal requests were admitted; the focused witness separately exercises failures. Actual backlog is ordinary active/connecting work.

## Decision boundary

[338 actual Unity runs and review evidence](../Review/ConnectionsCongestionAmbientV1/README.md) show a partial positive result from workload/headroom/concentration tuning, not nominal scheduling alone. Keep the decoupled-demand hypothesis for human testing; do not promote a canonical policy or ship these numbers. Selected Small and Advanced cases produce source-caused post-removal shortfall and useful Overclock/kill tradeoffs at modest counts, but effects vary by seed and the current quotas often finish much earlier. Ordinary attacks still do not reliably endanger final confirmed WORK in the tested recoverable settings.

After the design decision, remove unused clocks/ledgers/enum branches from this same director. Rejected tuning probes should be removed; adopted workload values should become deliberate authored PURPOSE/scenario settings instead of permanent research multipliers. Preserve historical logs and measurements. The common simulation pipeline and future company/hacker separation need not be replaced.
