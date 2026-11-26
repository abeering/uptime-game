# Connections — Independent Business Demand V0

Implementation from verified main `98908528099cdde7eb29ed8aa88515fe7e6bfdfc`, the normal GitHub merge of human-accepted #134 after setting both canonical scenes to **2.5× legitimate workload**. Scope: Connections Small and Advanced. Packet, visual presentation and the accepted workload/shift rules remain separate and unchanged.

## One business clock

The company's ordinary legitimate requests now arrive independently of network performance. `ConnectionTrafficDirector` uses a dedicated seeded exponential clock, reusing #125's seed stream (`seed XOR 0x1B3D4A7`). The first request is opening grace plus a sampled wait; each later ideal due time is the previous due time plus another wait. A one-microsecond numerical guard keeps waits positive without imposing a gameplay cadence. Natural bursts and lulls remain possible.

The clock reads no active population, allocation, completion, timeout, kill, cancellation, infection or outage state. Improving throughput does not cause automatic replacement pressure. A coarse scheduler call retains every due arrival, advancing from ideal due times rather than observation time. Existing canonical host simulation boundaries still determine when a due request is observed and registered.

Explicit `Auto Generation Enabled`/Authored Witness opt-out creates no requests while disabled. On re-enable a fresh seeded wait begins at the current time, following the hostile clock's existing convention; disabled time does not create a catch-up backlog. An outage is not this opt-out: requests continue to be made and admitted as ordinary Connecting attempts, which can then fail through existing offline-route lifecycle rules.

The former population ramp, occupancy selection probability, cancellation memory and outage-recovery rescheduling are removed from the active generator. No production policy selector, nominal-occupancy ledger or competing director is added. Old population settings in historical scenes have no scheduling effect. Historical recorder fields/evidence remain readable; they are not active configuration.

## Starting intervals and Inspector

Select **SimulationHost → Connection Traffic Director → Independent business demand (restart after edits) → Legitimate Mean Arrival Seconds**. It must be finite and positive. Smaller means more requests. Restart after changing it so run metadata and deterministic schedules reflect the new setting.

- **Small: 23.225807 seconds**, about 2.58 requests/minute after grace.
- **Advanced: 18.947369 seconds**, about 3.17 requests/minute after grace.

These are the comparable-volume reference means measured in [#125](https://github.com/abeering/uptime-game/pull/125), rather than its faster 8s/6s stress comparisons. That experiment measured an earlier workload/accounting baseline. These means are a starting reference, not a new measured equivalence at 2.5× or a final balance recommendation. No fresh rate sweep or calibration matrix was performed; human gameplay will determine adjustments.

Both scenes retain 2.5× processing LOAD/WORK, the pre-scaling business WORK value, nominal-duration-based service budgets/tolerance, node capacities, current WORK/failure objectives and allocator. Contention can change actual lifetimes and outcomes. Existing PURPOSE weighting, destinations, recipe/anomaly RNG, concrete plan queue and lifecycle remain authoritative.

## Demand, admission and failures

A scheduled request calls the existing `GenerateBenignPlan`, then `QueueSpawnPlan`; the existing dispatch loop registers its concrete definition with the host. An admission means a registered connection, including Connecting, not successful establishment or completed business. There is no additional admission queue or busy-network cap. Ordinary active and Connecting jobs remain the operational backlog.

Hostile activity scheduling, frequencies, composition and recipe functions are unchanged. Both streams still share finite canonical IDs and network state, so changing execution or ID consumption can change later hostile identities/targets/outcomes. Comparisons of complete hostile recipes require equivalent execution boundaries and generation context; an unchanged hostile clock does not promise identical games after their states diverge.

The authoritative demand snapshot contains arrived, admitted, waiting, unadmitted and next ideal due time. Recording is observational. A generated-ID exhaustion, absent weighted PURPOSE, invalid generated/queued plan or rejected registration produces an explicit unadmitted receipt while the clock continues. There is no hidden occupancy skip, invented connection, promised retry or fabricated WORK/failure credit. Registered connections that subsequently cancel/timeout use the existing business-failure ledger exactly once.

The existing finite 260-ID pool remains a limit. Requests after exhaustion continue as recorded unadmitted demand until reset; this phase does not implement unlimited IDs or business-loss policy for requests that never materialize. Unexpected errors after registration are surfaced while retaining the truthful admission.

## Recorder and reset

Schema4 gains `traffic.legitimateArrivalPolicy = IndependentBusinessDemand`, `traffic.legitimateMeanArrivalSeconds`, `business_demand_arrived`, `business_demand_admitted`, `business_demand_unadmitted`, and `snapshot.businessDemand`. Receipts include request sequence, ideal double-precision due time, observation time, next sampled wait/due time, canonical ID when available and failure reason. `plan_queued` source is `independent_business` and retains the full concrete definition/business value. Existing operation progress, business WORK credits, reliability receipts and outcomes are unchanged.

Reset clears demand counters and pending associations, restores the same selected seed streams and samples the same schedule. All terminal outcomes stop further demand through the existing host-owned ending gate. No per-frame demand event is emitted.

## Architecture direction and provenance

Company demand should reflect business circumstances, not adaptive attempts to defeat the player's configured network. Future events may deliberately alter arrival frequency, PURPOSE distribution, workload intensity, directed batches or the importance of individual requests. Business importance belongs to a particular request and its circumstances, not permanently to a PURPOSE. Operator-configured processing priority remains a separate concept. None of those future systems is implemented here.

Primary donor: unmerged #125 at `7c98f4fc20fe75e3af808e3a05677f76f696c19e`; selectively adapted its independent-clock and demand/admission recording seams. #126's nominal occupancy is not adopted: its extra feedback/ledger complexity was not justified by the gameplay evidence. Full historical branches and evidence are retained. #115/#124/#125/#126 are unchanged and unmerged.

[Focused checks, actual gameplay samples, limitations and manual tuning review](../Review/ConnectionsIndependentBusinessDemandV0/README.md) distinguish mechanical comparisons from human gameplay/balance claims.
