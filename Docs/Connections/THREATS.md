# Connections — THREATS

> **Status:** Active working design foundation, updated October 26, 2026. Captures the current design agreements and unresolved questions. **Not** a claim that the proposed changes have been implemented, or that provisional tuning values are final.
>
> **Scope:** Uptime's **Connections** prototype. Packet remains separate and playable. This page is the conceptual entry point for ambient threat generation; the linked focused contracts and current runtime/serialized scene remain authoritative for implemented behavior.

## The game-design premise

Uptime's player is defending a network that is doing real, legitimate work. Hostile activity is interesting because it interferes with that work, blends into it, or exploits it—not because the simulation labels a connection as malicious.

**Canonical PURPOSES supply the legitimate behavioral baseline; threat genera define the attacker's goal; attack signatures, structure, and temporal tactics determine what can be observed and how the attack plays out.**

Keep these concepts distinct:

| Concept | Role |
|---|---|
| **PURPOSE** | The expected business activity: claim, eligible destination(s), operation sequence, normal LOAD, and normal WORK. It is primarily an **investigative expectation** and secondarily world-building. |
| **Genus / objective** | What a hostile connection tries to achieve. Initially **LOADER** or **INFECTOR**; not an exhaustive taxonomy. |
| **Anomaly / signature** | A measurable mismatch with the legitimate PURPOSE baseline: LOAD, WORK, or operation deviation. Anomaly is **evidence**, not a threat verdict. |
| **Attack pattern / tactic** | How and when pressure appears: immediate high LOAD, OVERLOAD, SPIKING, etc. |
| **Connection structure** | The number/order of operations; **DELAYED** means the original normal PURPOSE arc is retained and **an additional malicious operation is appended**. This can apply to LOADERs and INFECTORs. |
| **Payload** | What an INFECTOR installs on delivery (e.g. Phantom LOAD, Blackout, Spawner); separate from the carrier's investigative signature. |
| **Information presentation** | What PURPOSE, operation kind, activity description, and evidence the player can observe. Not necessarily equal to hidden intent or a future independent classification. |

The canonical PURPOSE is **not** a placeholder label applied after making arbitrary hostile traffic. Normal and hostile connections share a credible legitimate baseline. A LOADER can *abuse the actual functionality* of Customer Authentication by forcing it to consume excessive processing capacity; it need not be fictionally pretending to do something entirely unrelated. An INFECTOR can use the same purpose as cover for delivering a persistent infection.

We do **not** need to redesign PURPOSE, introduce UNKNOWN/UNCATEGORIZED, implement independent classification, or add node *types* to complete this generation pass. Those are interesting open lines of inquiry. Preserve the current PURPOSE system for now.

## Ownership: ambient Traffic Director versus Level Director

**Ambient `ConnectionTrafficDirector` is the source of relatively random background connections.** It ensures a tunable amount of legitimate network activity and a tunable amount/mix of ambient hostility. It respects meaningful PURPOSE → eligible target mapping, but does **not** attempt coordinated campaigns, hand-pick ideal victims, or intelligently plan simultaneous attacks. The genus and variants of any particular ambient threat may still be random.

**Level Director + events are the future orchestration layer.** They can increase or decrease ambient density and threat pressure, enable/disable ambient generation altogether, and schedule deliberately overlapping or targeted attacks, possibly with several concrete spawn plans. The Packet prototype already explored coordinated packet spawns; this does not imply its scheduling code transfers directly to Connections.

A separate `ThreatDirector` or threat-planning MonoBehaviour is **not an established requirement**. A genus-specific connection composer may be useful to both the ambient Traffic Director and eventual Level Director, but coordinated planning belongs conceptually to the **Level Director**, not the ambient producer. Do not add architecture just to satisfy a hypothetical class diagram.

### Ambient population / coverage objective

**Phase 1 implementation note (PR #40):** Connections now uses option (b) from the earlier scheduling question below: a benign population controller and a separate stochastic hostile arrival clock in one Traffic Director. The exact long-term rates and Level Director controls remain open; the original alternatives below record the design question that led to this choice.

Benign ambient activity exists to keep the network **occupied enough for attacks to have somewhere to hide**, without being perpetually crowded.

The emerging tuning question is whether to replace or augment “spawn every N seconds ± jitter” with a **soft occupancy / coverage goal over time**:

- Coverage means **how many legitimate active logical connections** occupy the network across a window—not “cover all targets” or “send traffic to every node.”
- The default should be *generally busy, not constantly saturated*. Natural lulls and occasional high-roll crowding should still occur.
- The ambient director still selects valid PURPOSES and random eligible targets; target diversity should arise mainly from the purpose catalog and meaningful destination mappings.
- Maintain or measure **benign occupancy separately from hostile occupancy**. A persistent LOADER should not satisfy the ordinary-traffic coverage goal merely by staying alive.
- Use simulation/recorder evidence (active population over time, zero/one-connection stretches, overlap distribution, contention, benign service outcomes) rather than assuming a shorter fixed spawn interval solves the design problem.
- Actual coverage algorithm, desired range, smoothing window, and random process are **not yet decided**. Do not turn a soft goal into an artificial guarantee that the network never goes quiet.

**Ambient threat scheduling remains open:** either (a) classify a fraction of ordinary ambient arrivals as hostile, or (b) generate benign coverage and hostile attempts through separate stochastic processes. We want control over the *amount/mix* of ambient threat pressure without creating an obviously patterned threat signal. Two threats should be able to arrive almost together, or far apart. The Level Director should eventually be able to tune these controls independently.

## Two ambient generation recipes

### Benign

1. Randomly select a canonical PURPOSE from the current valid/weighted catalog.
2. Choose a random eligible target for that PURPOSE; derive a valid route.
3. Sample its defined normal operation(s), requested LOAD, WORK, and activity descriptions.
4. Optionally apply the existing small chance of a **benign outlier**, using shared signatures/tactics.
5. Queue the concrete connection and record its baseline/changes internally.

No advanced target prioritization or attack planning is needed to generate ordinary business traffic.

### Hostile

1. Decide to spawn **an ambient threat** (arrival process still TBD).
2. **Choose its genus first:** LOADER or INFECTOR.
3. Let the chosen genus define compatible PURPOSES/targets and the parameters it needs.
4. Select a compatible canonical PURPOSE, eligible target, and sample a **normal baseline**.
5. Compose the genus-specific attack from that baseline, including structure, timing, signature, LOAD, WORK, behavior, and (for INFECTOR) payload.
6. Validate mechanical viability, queue a normal `ConnectionSpawnPlan`, and record both the sampled baseline and the final plan.

For hostile generation **genus comes before PURPOSE choice**; benign generation simply rolls PURPOSE. Both still share the canonical purpose catalog and normal sampling rules. The genus dictates the transformation—not an undifferentiated “make it anomalous” operation.

## Genus 1 — LOADER

### Objective and player challenge

A LOADER attempts to **degrade legitimate services by consuming node LOAD while it is active**. The active pressure is meant to be obvious *when it happens*. Its difficulty comes from onset, timing, misdirection/attribution, congestion, prioritization, and the player's need to act—not from hiding the magnitude of a full attack.

Unlike an INFECTOR, LOADER success is **not** successful WORK completion. It is the actual interference caused to legitimate service while the attack runs. Killing it stops future pressure but cannot undo past disruption. A loud LOADER on an otherwise empty node might do little damage; ambient random generation should not always “solve” that for the attacker by finding the busiest node.

### Selected generation decisions

- **Attack LOAD is derived relative to the selected target node's LOAD capacity** (Option B), rather than simply multiplying the PURPOSE's sampled normal LOAD. Exact fractions/ranges are playtest tuning, not agreed values.
- Retain the sampled PURPOSE as the connection's overall claimed purpose. It can meaningfully represent functionality being abused.
- **Structure and attack pattern are independent dimensions.**
- A single-operation LOADER can begin in high-LOAD attack mode, or run normally before an OVERLOAD/SPIKING activation.
- **DELAYED appends one additional malicious operation after the complete sampled normal PURPOSE arc.** Its appended operation may itself be immediate high LOAD, OVERLOAD, or SPIKING.
- The time *before* a temporal attack's first activation is a **meaningful, variable tuning input**, not a fixed keyword delay.
- Operation kind remains READ / WRITE / EXECUTE; a suspicious activity/WORK description in the appended operation can communicate a changed phase without mutating PURPOSE or inventing a new operation kind. Exact description honesty/visibility is open.

### Composition matrix

| Structure | Immediate high LOAD | OVERLOAD | SPIKING |
|---|---|---|---|
| **Single-operation attack** | High attack LOAD from activation | Normal-demand opening, then sustained high demand | Normal-demand opening, then recurring high-demand bursts |
| **DELAYED appended attack** | Full normal PURPOSE arc, then obvious high-LOAD added operation | Full normal arc, then added operation that escalates after its own onset delay | Full normal arc, then added operation that starts recurring bursts |

Here **DELAYED is an arc-composition choice**, not a fourth competitor to OVERLOAD/SPIKING and not a separate genus. In the current code OVERLOAD and SPIKING are timed modifiers which can be scoped to a connection or operation; for an appended attack operation, **operation-scoped timing** is a natural fit so the timer does not begin during earlier benign stages. No new named “DELAYED keyword” is required just to append a stage.

**EASY / MEDIUM** from exploration described relative player difficulty, not distinct runtime types: immediate high LOAD is straightforward to identify; timed escalation and intermittent spikes complicate attribution. The word “Packet Flood” as an alternative claimed PURPOSE, and UNKNOWN/UNCATEGORIZED, remain exploratory rather than selected generation rules.

### Attack state and a general-purpose WORK rate scalar

**Selected direction:** An attacking LOADER needs an **attack-active versus not-attack-active state**, and a general-purpose, real-time adjustable **WORK rate multiplier**. Attack mode changes productive progress, not the amount of requested/allocated LOAD or the connection's original total WORK.

Current rule:

    WORK progress per second = allocated LOAD

Proposed universal rule:

    WORK progress per second = allocated LOAD × workRateMultiplier

- Normal operation defaults to **1.0×**.
- A LOADER's initial attack mode may set **0.0×**: it continues requesting/receiving LOAD and contending normally, but makes no WORK progress and therefore does not naturally complete via WORK while attack-active.
- Other positive values, e.g. 0.25×, 0.5×, 2.0×, remain available for future scaling, throttling/efficiency mechanics, or finite variants. The scalar is a **general runtime dimension**, not a hardcoded LOADER-only exemption.
- This is **different** from the current `workDelta` modifier, which changes the total WORK requirement rather than the **rate of WORK completion**. Do not silently reinterpret `workDelta`.
- Existing proportional LOAD allocation stays authoritative. Zero productive progress does not mean zero resource consumption, nor necessarily “stalled due to allocation.”
- The multiplier may be changed by appropriate connection- or operation-scoped behaviors in the future; the exact stacking/composition rule is not yet designed. Avoid building an overly broad general modifier framework ahead of this slice.

**SPIKING nuance:** Once a LOADER is in its *attack-active phase*, its WORK rate can remain 0× **during quiet intervals between spikes**. Attack-active state is distinct from “requested LOAD happens to be elevated at this instant.” The spike timer continues to progress in simulation time even while WORK progress is zero.

For temporal patterns, the **pre-attack period still needs to survive** long enough for the planned onset. It may require enough baseline WORK, or a carefully selected pre-activation progress rate, to avoid natural completion just before the attack starts. **Do not** return to calculating huge total WORK merely to fake a sustained attack after activation.

At 0×, an attack will not resolve by ordinary WORK completion unless its state changes. A player kill, an explicit end-of-attack/attacker event, or other connection termination can end it; whether ambient LOADERs require an automatic timeout is **unresolved and important for population stability**. Do not silently make every ambient LOADER immortal and thereby create irreversible accumulation.

### LOADER spawn parameters (conceptual; not all need serialized fields)

| Parameter | Responsibility |
|---|---|
| `purpose / target` | Cover identity, valid node and route, initial normal operations |
| `structure` | Single attack operation or DELAYED appended operation |
| `pattern` | Immediate sustained, OVERLOAD, SPIKING |
| `attackLoadFraction` | Desired elevated LOAD relative to target node capacity |
| `preAttackDelay` | Variable initial wait before the attack first becomes active, where applicable |
| `spikeNormalInterval` / `spikeElevatedInterval` | Temporal rhythm while SPIKING |
| `workRateMultiplier` | Effective productive WORK scaling, e.g. 1× normally and 0× attack-active |
| `exitPolicy` | How persistent LOADERs eventually end; **open design**, separate from WORK computation |

Avoid treating `attackDuration` or spike count as a required *WORK budget* when attack-active rate is zero. Those can still become constraints/tuning parameters if explicit attack termination or finite variants are later selected.

### LOADER generation viability checks

- A normal-looking pre-attack interval is genuinely normal, not already inflated to peak LOAD.
- OVERLOAD and SPIKING reach their first intended activation before a baseline operation would complete.
- Spiking timelines continue independently of WORK advancement.
- Once attack-active at 0×, LOAD is still allocated and impacts benign service.
- One active LOADER does not count as enough benign population for ambient coverage.
- A LOADER can be killed; allocation releases; the recorder and any status/ETA output do not misdescribe 0× productive progress as no LOAD.
- Natural termination and optional timeout semantics are explicit rather than accidental.

## Genus 2 — INFECTOR

### Objective and player challenge

An INFECTOR attempts to **deliver a typed infection**, generally by successfully completing a carrier operation. The infection remains after the carrier retires and presents a new problem (e.g. Phantom LOAD, Blackout, or Spawner). Killing the carrier *before* delivery can prevent the pending infection; killing it afterward does not clean the node.

An INFECTOR's carrier is typically designed to resemble a compatible benign PURPOSE. Its anomaly signatures are **investigative clues** rather than the essence of the attack.

### INFECTOR spawn inputs

| Parameter | Responsibility |
|---|---|
| Infection type / payload parameters | What happens on successful delivery; payload is independent of carrier LOAD |
| Compatible PURPOSE / valid target | Which legitimate operation(s) may carry this payload |
| Structure | Existing purpose arc with delivery, or DELAYED append of one malicious delivery operation |
| Evidence signature | High LOAD, low LOAD, operation deviation, WORK-only duration deviation, potentially rare no-anomaly |
| Signature magnitude / placement | Where the mutation occurs and how detectable it may be |
| Delivery window | The time the player has before delivery in nominal uncongested execution |

A high-LOAD carrier signature **must not accidentally shrink the investigation window** by leaving WORK unchanged. Generate LOAD and supporting WORK with intentional delivery timing. This is a *different* reason to change total WORK than LOADER persistence, now served by a 0× attack rate.

**WORK-only deviation remains important and distinct:** normal-looking requested LOAD, but abnormally large WORK and hence an unusually long nominal duration. The player notices “Why is this still running?” rather than high consumption. Keep it **rare, not removed**, at current introductory tuning. Actual slowdown from allocation contention must not automatically be treated as evidence of malicious WORK.

**Working early INFECTOR anomaly weights (proposed, not yet implemented):**

- High LOAD: **65%**
- Low LOAD: **10%**
- Operation deviation: **15%**
- WORK-only deviation: **10%**

A subtle carrier with *no anomaly* is a future/rare case requiring other plausible detection channels; do not make the early game rely on evidence the player cannot obtain. Legitimate **benign outliers** must remain possible so deviation ≠ confirmed hostility.

**DELAYED applies to INFECTOR too:** generate and retain the normal PURPOSE arc, then append a malicious delivery operation. The additional operation may carry a strange activity description; ordinary READ/WRITE/EXECUTE and stable claimed PURPOSE are sufficient for now. Avoid automatically changing PURPOSE or leaking hostile identity in inspection.

Unlike a sustained LOADER, an INFECTOR must advance toward completion: the 0× attack-active rate is not its default delivery mechanic.

## Investigation and fiction boundaries

The player should be able to compare observed LOAD and *nominal duration* with expected behavior for the claimed PURPOSE. A typical-range comparison may eventually appear in verbose inspection, in a separate service reference, or through optional difficulty assistance. **Not yet decided**; no new command or classifier is authorized by this document.

Separately, UNKNOWN/UNCATEGORIZED might represent genuinely unrecognized legitimate activity or incomplete classification and should not become an automatic threat marker. Independent classification, probes, logs, tracing, node scanning, and richer source evidence are future ideas rather than prerequisites for this generator.

PURPOSE is useful because legitimate traffic usually behaves consistently with it; attackers exploit or imitate that consistency. Activity descriptions and operation history may communicate the malicious phase, but should not replace actual simulated evidence.

## Current code versus selected direction

As of this document's review baseline, current Connections code already has:

- A seeded `ConnectionTrafficDirector` generating six ecommerce purpose templates, with sampled normal LOAD and independent WORK, plus a single 13.5 ± 1.5 second ambient arrival schedule, hostile chance, and LOADER/DELIVERY objective roll.
- High/low LOAD, WORK, and operation mutations; benign outliers; OVERLOAD and SPIKING behaviors; manual delayed threat profiles; typed infection-delivery effects and persistent node infections.
- A `ConnectionSpawnPlan` → `SimulationHost` execution path, proportional target-node allocation, operation arcs, and a run recorder storing original and final values and internal generation decisions.
- `kill`, `inspect` / `inspect -v`, and basic node cleaning; the current inspector does not show a canonical-purpose baseline or work-rate scaling.

**Not yet implemented by these design decisions:**

- Occupancy/coverage-aware benign ambient generation; independently tunable benign/threat opportunity streams if chosen.
- Objective-aware LOADER composition with capacity-relative attack strength, variable onset, DELAYED appended attack, and an attack-active state that changes productive WORK rate.
- A general **WORK rate multiplier** in allocator/progress semantics.
- INFECTOR composition with intentional delivery windows, the proposed weighted anomaly mix, and optional DELAYED appended delivery.
- An actual Connections Level Director that tunes ambient parameters or owns coordinated attack events.

Implementations must continue to respect the *working branch's actual code and scene*. Existing legacy Threats/Traffic/Behavior documents explain implemented slices or older proposals; they are not automatically updated to this new design.

## Proposed near-term Codex sequence (not yet an implementation authorization)

**A. Population/coverage baseline.** Read the latest director/scene and recorder; instrument and tune time-weighted benign active counts, overlaps, and gaps. Explore a *soft target* approach, not guaranteed node coverage. Preserve randomness, allow lulls/high rolls, and avoid counting persistent threats as benign background. Keep adversary orchestration out of the Traffic Director.

**B. WORK-rate foundation.** Add a general effective per-operation/connection WORK rate scalar (default 1×); scale productive progress after LOAD allocation while leaving demanded/allocated LOAD, total WORK, and `workDelta` meaning unchanged. Test 0×, positive values, mid-operation transitions, operation scope, zero allocation, completion, kill, and recorder/inspect correctness.

**C. Objective-aware LOADERs.** Compose immediate, OVERLOAD, and SPIKING attack patterns, plus DELAYED appended variants, with capacity-relative peak LOAD, varied initial time-to-attack, explicit attack-active state, and a 0× attack productive-rate default. Test all six structural combinations and pending-onset viability. Define or consciously defer an ambient-LOADER exit policy instead of allowing unbounded accumulation accidentally.

**D. INFECTOR timing and signatures.** Choose payload and compatible PURPOSE; preserve nominal delivery opportunity when mutating carrier LOAD, support rare WORK-only long-duration anomalies and the proposed signature weights, and add DELAYED appended deliveries. Preserve benign outliers and evidence ambiguity.

Throughout: retain seeded determinism, stable purpose baselines, source/target validity, accurate recorded normal-vs-final plans, actual Network Health consequences, and manually invokable witnesses. **No mandatory new ThreatDirector; no coordinated campaign logic in the ambient director.** The Level Director can later tune ambient parameters, turn ambient off, and supply intentional overlapping spawn plans.

## Open questions to resolve with playtests

1. **Population control:** soft occupancy target, adaptive arrival rate, rolling window, or another randomized controller? What active-count distribution actually feels busy rather than crowded?
2. **Hostile opportunity scheduling:** roll threat as part of ambient arrivals or maintain a separate tunable stochastic stream? Ensure near-simultaneous threats and long gaps remain possible.
3. **LOADER lifetime:** Do ambient permanent 0× attacks need a finite termination/recovery mechanism, attacker-initiated exit, or just an explicit policy in the Level Director? What prevents unattended attacks accumulating without meaningful play?
4. **Temporal variety:** weights for immediate/OVERLOAD/SPIKING and DELAYED, onset-delay ranges, peak capacity fractions, and the minimum useful number of spikes.
5. **WORK rate semantics:** stacking priority, connection vs operation scope, and what 0× should display in inspection/ETA without leaking “malicious” as a label.
6. **INFECTOR delivery timing:** how long should baseline and anomalous carriers take, and where should an appended delivery operation fit in that window?
7. **PURPOSE / information evolution:** future node types, unknown/unclassified behavior, reference baselines, logs/probes/trace, and degree of trustworthy activity descriptions.
8. **Future genera:** THROTTLER, TUNNELER, other attackers—are they distinct objectives, tactics, payloads, or attack surfaces?

## Future genera are possibilities, not scope

LOADER and INFECTOR are the first two *genera*, not a complete hostile taxonomy. Later objectives may impair productive work without consuming equivalent LOAD (a possible **THROTTLER**), establish a new route/access surface (possible **TUNNELER**), or behave in ways we have not named. A new genus should pose a **meaningfully different player problem**, not merely relabel an anomaly or the same attack at another magnitude.

Coordinated or multi-surface campaigns belong to future Level Director design. They should be able to reuse genus-specific connection composition without making the ambient Traffic Director “smart.”

## Related sources

- [Connections Current State](Current_State.md) — working baseline and code authority.
- [Purpose & Anomaly Generation V0](connections-purpose-anomaly-generation-v0.md) — current canonical PURPOSE profiles and implemented anomaly handling.
- [Threats V0](connections-threats-v0.md) — earlier threat slices, operation delivery, and LOAD keyword provenance.
- [Behavior Foundation](connections-behavior-foundation-v0.md) — effective WORK/LOAD and implemented timed modifier semantics.
- [Infections V0](connections-infections-v0.md) — node payloads, application, and removal.
- [Network Health V0](connections-network-health-v0.md) — real service degradation consequences.
- [Run Recorder V0](connections-run-recorder-v0.md) — generation provenance, snapshots, and marks.
- [Threat Investigation Ideas](connections-threat-investigation-ideas.md) — investigative tooling proposals, not an implementation contract.
- [Shared Uptime Design](../Design/Uptime_Design.md) — broader game/Level Director direction.
