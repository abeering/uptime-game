# Connections Arcade V1 — traffic generation V0

**Current status (October 22, 2026):** Integrated generated-traffic contract from [PR #6](https://github.com/abeering/uptime-game/pull/6). The closing "no run telemetry" limit records this traffic phase's original boundary; automatic run recording later merged in [#7](https://github.com/abeering/uptime-game/pull/7). The other listed limits remain subject to current runtime truth. See [Connections Current State](Current_State.md).

This phase gives Connections a continuous benign traffic rhythm. Packet remains an independent game and its traffic classes are unchanged.

## Ownership and spawn pipeline

`ConnectionTrafficDirector` owns a local seeded `System.Random`, an ambient cadence, generated profiles, a queue of fully specified `ConnectionSpawnPlan`s, and stable same-time insertion order. Every ambient arrival and externally submitted plan calls `QueueSpawnPlan`; due plans are converted into definitions and registered through `SimulationHost.RegisterConnection`. The director is called from the host's fixed simulation step before resource allocation. Ambient generation can be disabled without suppressing queued plans. A plan includes its scheduled simulation time, unique short ID, explicit route and origin, target, profile ID, purpose, and concrete operation WORK and LOAD. Future event or infection producers can construct a plan with an internal origin and valid physical route, then call the same queue API. They are not implemented here.

`SimulationHost` validates node membership, route links, uniqueness, finite positive operations, and operation kinds. It owns active states, LOAD allocation, WORK, stage transitions, resolution timestamps and reasons, and inspection history. Completed generated states leave the active array after the allocation step and keep a history reference. `kill <id>` removes active ownership immediately and publishes released node demand; peers receive new allocation on the next step. `ConnectionResolved` exposes Completed and Killed outcomes. No scoring or FLOW effect is attached.

Threats V0 Slice 1 adds development-only `spawn <profileId>` through the same profile-to-plan builder and `QueueSpawnPlan` path used by ambient generation. The command reports a queued ID or unknown-profile error. Queued plans dispatch even with ambient generation disabled, and authored witness mode suppresses ambient generation while retaining explicit queue dispatch. Manual spawns are not a Level Director or a scheduling language.

The Packet game's separation between procedural traffic, concrete spawn plans, and level events informed this seam. Its tick schedules, modifiers, threat generation, and runtime code are not reused. Connections uses its existing simulation clock and LOAD allocator.

## Modes and tuning assumptions

`Connections.unity` defaults to Generated mode. The host Inspector can switch to Authored Witness mode for the existing c1/c2/c3 demonstration; the Resource and Geometry Witness menu checks select that mode during their run. Reset clears time, queues, history, and ID reservations, then replays the selected mode from zero. The generated mode does not start the authored connections.

The initial profile catalog is the director's default when no Inspector profiles are supplied. Each stage independently samples its configured LOAD and nominal duration, then sets `WORK = LOAD × duration`. These are initial playtest ranges, not fixed gameplay rules:

| Profile | Target | Operations | Desired LOAD | Nominal duration |
|---|---|---|---|---|
| Record Retrieval | DB | READ | 1–2.5 | 40–70 s |
| Record Indexing | DB | WRITE → READ | 1.5–3, then 1–2.5 | 20–35 s per stage, 40–70 s total |
| Customer Authentications | AUTH | EXECUTE | 1–2.5 | 40–70 s |
| Application Updates | API | WRITE → READ | 1.5–3, then 1–2.5 | 20–35 s per stage, 40–70 s total |
| Quick Record Lookup | DB | READ | 0.5–1.5 | 4–8 s |
| Basic Threat (`basic-threat`, manual only) | DB | WRITE with completion delivery | 12 | 20 s (240 WORK) |

Relative ambient weights of the five benign profiles are 1, 0.8, 1, 0.8, and 0.35 respectively; Basic Threat has weight zero. The seed is 1701, grace period 3 s, interval 13.5 s with ±1.5 s jitter, and ordinary origin EXT1. Routes are found from the configured physical topology in stable link order. Node capacities come from the scene, currently 10 LOAD for each node. The optional `demandtest` command queues one 10–12 LOAD READ to DB through the same pipeline; `QueueHighDemandWitness(..., false)` covers the 7–9 LOAD range. Neither the demand witness nor Basic Threat belongs to the ambient mix.

The arrival cadence does not observe active population. Completion time depends on allocated LOAD and may increase during contention. There is no target concurrency, replenishment, or gameplay cap. A congested network can accumulate more connections; tuning should be adjusted after playtesting rather than hidden by population control.

## Validation and limits

Use `Tools/Connections/Run Traffic Witness`, `Run Resource Witness`, and `Run Geometry Witness`. The traffic witness checks seeded replay, routes, duplicate IDs, explicit arrivals with ambient off, single and multi-stage completion, inspection history, a 12-plan burst, proportional LOAD, kill resolution and release, plus manual benign/threat lifecycle, independent deliveries, and reset/replay. Play Mode at 16:9 is still needed to assess cadence and congestion, overlapping labels and identity cycling, arrivals without path resets, READ/WRITE/EXECUTE effects, `inspect` history, `kill`, `spawn`, `demandtest`, and prompt focus.

The profile catalog and cadence are provisional. Slice 1 has internal hostile classification and an infection-delivery occurrence, but no persistent infection, level event scheduler, scoring, FLOW, stage-specific target, or routing action. Completed inspection history persists for the run and is not yet bounded. The high-demand command is a mechanical witness, not a threat designation.
