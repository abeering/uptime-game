# PR #137 recorder completeness

Connections implementation audit for offline API/CACHE analysis. Ordinary schema-4 gameplay logs already carry the requested policy, demand and resource evidence. The only added raw field is `snapshots[].connections[].isDemanding`, copied from authoritative simulation state. Snapshot frequency, existing events, simulation, UI and policy behavior are unchanged. No counterfactual allocator or effectiveness metric runs in gameplay.

## Evidence map

All snapshot paths below are relative to one `snapshots[]` entry. Join connection events by `connectionId` and `operationIndex`; event `sequence` orders equal simulation `timeSeconds`.

| Required evidence | Existing JSON |
| --- | --- |
| Effective capacity and reservations | `nodes[]`: `id`, `loadCapacity` (current effective capacity, including Overclock), `baselineLoad`, `infectionRequestedLoad` / `infectionAllocatedLoad`, `defensiveRequestedLoad` / `defensiveAllocatedLoad`, connection and total requested/allocated LOAD. `run.nodes` is initial configuration, not current capacity. |
| Ordinary and actual destination | `connections[].target`, `operationRouting.intendedDestination`, `actualDestination`, committed `route`, `reason`, `committedAtSeconds`. Queued/spawned `definition` contains original per-operation intended destinations and full request identity/PURPOSE. |
| Original and composed requirements | `operationRouting.originalWork` / `originalLoad` and scaled base `processingWork` / `processingLoad`; sampled `baseWork`, `baseLoad`, `totalWork`, `desiredLoad`, `naturalLoadDelta`. Full behavior definitions are in `plan_queued.definition` / `connection_spawned.definition`; generated normal/final values remain in generation evidence. |
| Allocation and processing | `connections[].allocatedLoad`, `actualWorkRate`, `effectiveWorkRateMultiplier`, `completedWork`, and new `isDemanding`. |
| API policy | Applied per-operation `allocationWeight` / `allocationPolicyNodeId`; current `apiFavoredPurposeId` / `apiAllocationWeight`; original stable request PURPOSE and committed route permit external eligibility evaluation. |
| CACHE policy | `operationRouting.cached`, `selectedCachePurpose`, locked positive `workMultiplier` / `loadMultiplier`; current `cachePurposeId` is separate and can differ from the committed choice. |
| Operation boundaries | `operation_started`, `operation_completed` with `timeSeconds`; interrupted operation ends are `connection_resolved` with operation index, outcome, cause, phase and resolution time. |
| Rerouting boundaries | `connection_rerouting_started` / `connection_rerouting_finished`, including previous route, shared-node/new-link counts and start/finish/travel times; interrupted reroutes end at `connection_resolved` with `terminationPhase: Rerouting`. |
| Configuration timestamps | `api_policy_changed` / `cache_policy_changed` include simulation time, node and previous/selected PURPOSE; API event includes weight. Each also captures its existing immediate snapshot. Inspector settings are copied to run metadata at reset/run start; authoring edits are not live gameplay policy changes. |

Other existing raw evidence includes topology, initial configuration, active infections/conditions and their timing, defense/interceptor state, and infection/condition/Overclock/player-action events. These allow external evaluation of route eligibility and destination-dependent rate effects. No duplicate fields are added for those inputs.

## Uncached demand at a sampled moment

For a **demanding** connection with committed routing for its current operation:

```text
uncached composed requested LOAD = desiredLoad / operationRouting.loadMultiplier
uncached composed required WORK  = totalWork / operationRouting.workMultiplier
unscaled natural-noise delta     = naturalLoadDelta / operationRouting.loadMultiplier
productive node capacity        = max(0, loadCapacity - baselineLoad
                                      - infectionAllocatedLoad - defensiveAllocatedLoad)
```

The runtime composes behavior changes and noise, clamps nonnegative LOAD/positive WORK, then applies the locked CACHE factor. Division therefore retains active fixed, timed, OVERLOAD, SPIKING/WAVING and natural-noise contributions. `originalLoad` is the unoptimized **base** requirement; it is insufficient by itself for sampled uncached demand. `baseLoad / loadMultiplier` recovers that original base, not the composed request. Floating-point serialization/division has ordinary rounding error. Factors are validated positive and finite; non-cached committed operations use 1.

Filter `isDemanding` before comparing allocations. A held between-operation boundary has lifecycle `Active` but requests no allocation; its already-advanced operation index can temporarily coexist with the preceding routing record. CONNECTING and REROUTING also request none. Conversely, a zero WORK-rate modifier can leave an operation demanding and allocated LOAD: neither zero rate nor zero allocation proves absence of demand. Historical recordings missing the new key have unknown demand membership at held boundaries; readers must distinguish an absent key from explicit `false`. Unity inline null records may deserialize as empty objects, so do not divide missing/pending routing factors.

Disabling API externally sets weights to 1. Disabling CACHE externally restores sampled composed demand and ordinary destinations/routes, then recomputes applicable API eligibility and destination-dependent effects. Keep sampled infection/defense/baseline reservations explicit and use the existing capped weighted proportional allocation contract. This is a frozen-state comparison, not a reconstructed alternate history of arrivals, completion, infection delivery or deadlines.

## Timing limits

Snapshots are periodic end-of-step observations, not a continuous allocation trace. On a completion/resource-release boundary, surviving allocations can still reflect the preceding allocation pass until the next update; node demand publication can already reflect the released request. Compare external steady-state allocations with this boundary behavior in mind. The recorder does not add an allocation-pass stream or increase its authored one-second cadence.

An interruption produces no fictitious operation completion or successful reroute-finished event. Run stop/shift ending can leave unfinished operations or reroutes: treat these as censored at the recorded boundary/last observation, not completed. `run.endedUtc` is a wall-clock save boundary, not an invented simulation operation end.

## Focused Unity validation

Unity 6000.3.11f1, isolated project copy, batch Editor compilation and real disk save/readback via `JsonUtility`:

- **74 recorder-policy checks** across both canonical scenes, default CACHE LOAD factor 0.5 and non-power-of-two 0.3. Actual saved values reconstruct authoritative raw composed demand with active requirement, OVERLOAD, WAVING, zero-rate modifiers and nonzero natural noise. Both scenes exercise committed versus current policy, ordinary DB versus actual CACHE destination, 1.5× Overclock capacity, baseline/infection/defense reservations, API weight, positive allocation with zero processing, and held-stage exclusion/resumption. Cadence remains one second.
- **108 existing route-transition checks**, now also checking recorded non-demanding REROUTING; existing start/finish counts, timing, interruptions and exact-once accounting remain covered.
- Existing **RecorderWitness** passes metadata, lifecycle, coherent LOAD/WORK, markers, actual JSON saves, autosave, kill, distinct runs and enabled/disabled parity.

[Exact policy checks](checks.txt) and [Small sample](Connections-sample.json) / [Advanced sample](ConnectionsAdvanced-sample.json) are snapshot excerpts deserialized from full saved run JSON, not synthetic log objects. Full transient runs remain in the ignored validation-copy `Logs/UptimeRuns` folder. Existing PR gameplay movies and inherited broad-regression caveats remain in the transition review; no new graphical Play Mode acceptance is claimed for this recorder-only follow-up.

Manual review: run **Tools → Connections → Run Recorder Policy Evidence Witness** outside Play Mode (temporary scene fixture, do not save), then reload a canonical scene. For ordinary gameplay, enter Play, select API/CACHE PURPOSES, stop, and inspect `Logs/UptimeRuns/*.json` using the paths/formulas above. Existing **Run Route Transition Witness** and **Run Recorder Witness** provide the corresponding focused regressions.
