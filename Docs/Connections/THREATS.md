# Connections — THREATS

> **Status:** Active working design foundation, October 25, 2026. This page captures the agreed model and the outstanding design space; it is **not** a claim that the proposed generator changes are implemented or that numerical tuning is final.
>
> **Scope:** Uptime's **Connections** prototype. Packet remains a separate playable experiment. This is the *top-level design anchor* for what a hostile connection is trying to achieve and how that intent should be translated into spawn plans. Existing focused implementation contracts remain authoritative for current behavior.

## Central premise

**Every generated connection is grounded in a canonical benign PURPOSE. Hostile generation starts with a legitimate-purpose sample, then transforms that concrete connection to pursue an attacker objective.**

The PURPOSE tells us what normal behavior *would have been*. The threat genus tells us *why* the resulting connection is dangerous. Anomalies and timed behaviors tell us *how it departs from that baseline*, and what evidence may become visible to the player.

These concepts should not be collapsed:

- **PURPOSE / cover identity:** Claimed business activity such as Customer Authentication, Product Detail Retrieval, or Checkout Order Submission. A purpose defines legitimate operation kind(s), destination, expected requested LOAD, and expected WORK, with normal variation.
- **Threat genus / objective:** The class of harm the attacker is attempting. Our first two are **LOADER** (ongoing resource pressure) and **INFECTOR** (delivering a persistent node infection). They are the first two examples, **not** the exhaustive hostile taxonomy.
- **Signature / anomaly:** A deviation from the sampled normal purpose—high LOAD, low LOAD, unusual WORK, or a different operation. An anomaly is *evidence*, not a verdict. Benign outliers can exhibit the same signatures.
- **Tactic / temporal behavior:** When and how a deviation appears: immediate, delayed onset, sustained escalation, repeated spikes, or a normal-looking first operation followed by an unusual operation. A timed tactic can be legible while active and misleading while dormant.
- **Payload / consequence:** What is actually delivered or changed in the network. An INFECTOR's payload (Phantom LOAD, Blackout, Spawner) is not the same thing as the LOAD/WORK signature of its carrier.
- **Attack surface:** The means through which the attack reaches the simulated network. Currently this is primarily a logical connection; future tunnels, compromised nodes, etc. need not fit a single-connection model.

A connection can claim a legitimate purpose while being conspicuous **from its first active moment**. A normal-looking opening period is important for *delayed* tactics, not a requirement for every threat. The same canonical purpose must support clearly benign, anomalous-but-benign, and hostile instances.

## Generation model: PURPOSE first, hostile goal second

The conceptual recipe is:

1. **Select a compatible PURPOSE.** It supplies a claim, eligible target(s), operation arc, and normal LOAD/WORK distributions. Scheduling or intended attack target may constrain which purposes are eligible.
2. **Sample a concrete normal connection.** Preserve this sample in generation evidence: kind, requested LOAD, WORK, order of operations, and implied nominal duration. Do not mutate the canonical definition.
3. **Choose the connection's role.** It may remain benign (possibly with a benign outlier), or receive a hostile **genus and objective**.
4. **Construct an objective-aware attack plan.** Decide the intended effect, strength, timing, target, and eligible tactics **before** finalizing abnormal numbers. This is where LOADER and INFECTOR differ.
5. **Apply signatures and temporal behaviors to the normal sample.** Derive the final operation(s), requested LOAD, WORK, timed modifiers, and any infection payload so the concrete plan has a plausible opportunity to achieve its objective.
6. **Validate and record.** Check that the chosen parameters are physically valid and mechanically consistent; queue a normal `ConnectionSpawnPlan` and retain baseline-vs-final and intent data **only in internal diagnostics**. The existing `SimulationHost` remains authoritative for allocation, advancement, delivery, and resolution.

The final connection should still obey universal simulation rules. An internally hostile objective does not automatically color it red, alter the allocator, or get revealed by `inspect`.

### Why a canonical PURPOSE matters

A typical Customer Authentication is meaningful because it can be compared with other Customer Authentication operations—not with every operation in the network. It establishes a real, reusable expectation instead of making each threat's “normal” behavior arbitrary.

This also gives the player a potential *company knowledge* reference for investigation without requiring hours of memorization. We have discussed comparing current LOAD with typical LOAD, and nominal operation duration with typical duration. Whether this appears in `inspect -v`, a service-reference tool, or a difficulty-assisted inspection mode is still open.

## The first two threat genera

| Dimension | LOADER | INFECTOR |
|---|---|---|
| **Primary objective** | Degrade legitimate service by taking usable capacity | Establish a persistent compromise on a target node |
| **Mechanism** | Requested LOAD consumes a share of target-node capacity while the attack runs | A connection operation completes and delivers a typed infection |
| **Danger window** | Primarily **during active operation** | Primarily **at delivery**, followed by the infection's own lifetime |
| **Relationship to LOAD** | The resource pressure **is the attack** | Carrier LOAD may be ordinary or anomalous; payload impact is independent |
| **Relationship to WORK** | Enough WORK/lifetime is needed to carry out the intended pressure pattern | Enough WORK determines the time until delivery; extra WORK can itself be a signature |
| **Primary player challenge** | Notice degradation, attribute it to the responsible connection, stop further pressure | Evaluate incomplete evidence and interrupt the carrier **before** delivery |
| **Success** | Actually disrupt useful service while active; the amount of harm can be partial | Intended infection **applies** successfully; merely attempting a rejected duplicate is not equivalent |
| **Interruption** | Killing ends future resource pressure; damage already caused remains | Killing before delivery prevents that pending payload; killing the carrier afterward does not remove an installed infection |

The distinction is about objective and lifecycle, **not** whether a connection has an abnormal LOAD number. An INFECTOR can create incidental contention before delivery; a LOADER can have a long, otherwise ordinary-looking preparation phase. These overlaps should not erase their different goals.

### LOADER: spawn contract (working design)

A LOADER plan should begin with the question: **Which node should experience how much additional requested LOAD, by what temporal pattern, and for how long?**

| Plan input | What it controls |
|---|---|
| Cover PURPOSE and compatible target | Legitimate-looking operation(s), route, expected normal behavior |
| Attack target / context | Where resource contention is likely to matter; target capacity and legitimate demand are useful planning inputs |
| Pressure strength | Desired attack-phase requested LOAD or increment, ideally considered relative to capacity and competing traffic |
| Onset / pattern | Immediate sustained LOAD, delayed escalation, repeated SPIKING, or a normal first operation followed by an attack operation |
| Preparation interval | How long initial baseline behavior survives before visible escalation, when applicable |
| Attack window | Intended duration and/or number of high-demand intervals after onset |
| Optional additional signature | Any deviation beyond the characteristic pressure pattern; not required for the genus |
| End behavior | Ordinary completion/kill ends the connection's resource demand; no infection-delivery requirement |

**Key generation constraint:** **Do not multiply LOAD without accounting for WORK and time.** In the current model, allocated LOAD advances WORK at the same numeric rate; at full allocation, four times the LOAD with unchanged WORK makes the operation approximately four times shorter.

For a simple constant-LOAD operation, choose a *nominal target duration*, then derive enough WORK to support it. For temporal LOAD patterns, size WORK against the **whole expected demand-versus-time sequence**, not just the peak LOAD. Example: 10 seconds requesting 1 LOAD followed by 30 seconds requesting 8 LOAD implies about **250 WORK** at full requested allocation, before considering fixed-step boundaries. This is illustrative, not a locked formula for every variant.

Allocation may be less than requested LOAD because of contention, extending actual lifetime. Realized harm must be measured from **legitimate service effects**, not just an impressive LOAD request or the LOADER's eventual completion. A LOADER on an idle node can be mechanically loud without having caused much harm.

**Temporal identity matters:**

- **Immediate/sustained:** The active pressure is obvious immediately; attribution and prioritization are the player's problem.
- **OVERLOAD:** Initial baseline-looking demand, then a sustained positive LOAD modifier after a delay. Its **effective** requested LOAD becomes obvious on activation. The plan must survive long enough for that activation and for meaningful time afterward.
- **SPIKING:** Repeated baseline and elevated LOAD intervals; it may look normal between spikes. The point is incongruent *timing*, not a covert peak effect.
- **DELAYED (broader pattern):** May use a delayed OVERLOAD activation or a normal operation followed by an attack operation. “DELAYED” is **not** currently an independent implemented keyword/genus.

Do **not** pre-inflate the base LOAD of a delayed or spiking LOADER so that the advertised quiet phase already displays the full attack. The recognizable resource effect should occur when its planned behavior activates.

### INFECTOR: spawn contract (working design)

An INFECTOR plan should begin with: **What infection is being delivered, to which node, and what opportunity does the player have to intercept it?**

| Plan input | What it controls |
|---|---|
| Cover PURPOSE and target | Legitimate operation plan, route, and expected values |
| Infection payload | Phantom LOAD strength, Blackout, or Spawner cadence; this is the *result*, not carrier evidence |
| Delivery operation / phase | Which operation's successful completion applies the payload; potentially preceded by normal operations |
| Delivery window | Intended nominal time before successful delivery, including any earlier stages |
| Evidence signature | High LOAD, low LOAD, operation deviation, WORK-only deviation, or intentionally no added anomaly |
| Signature magnitude / placement | Which operation changes and how detectable it might be |
| Optional temporal behavior | How evidence appears or varies over time, without redefining the payload |

**Key generation constraint:** A high-LOAD **carrier signature** should not automatically turn a reasonable investigation window into a few seconds. After changing carrier LOAD, recalculate supporting WORK as needed to preserve the intended delivery window. That extra resulting WORK is a **consequence of preserving time**, not necessarily a separately rolled WORK-only anomaly.

A **WORK-only deviation** remains a distinct, valuable signature: preserve the normal requested LOAD, deliberately increase WORK, and create a substantially longer *nominal operation duration*. The evidence is **“Why is this still running?”** rather than “Why is this consuming so much?” It should be relatively rare at our current introductory tuning, **not removed or redefined**.

Keep nominal/at-speed duration separate from elapsed time or ETA **under contention**. A benign operation made slow by a crowded node is not thereby an INFECTOR. An operation deviation may likewise change or append a stage without automatically revealing intent.

A fully ordinary-looking carrier with no chosen anomaly is a possible higher-difficulty/alternative-evidence case. It should not become the main early gameplay pattern until the player has credible other investigative tools or clues.

The INFECTOR's success is successful *application* of its payload. If a duplicate infection is rejected, the attempted completion happened but the intended new compromise did not take hold. Once delivered, the carrier can retire while the infection persists and must be addressed separately.

## Signatures and temporal behaviors are shared vocabulary

| Vocabulary | What it means | Relation to genus |
|---|---|---|
| **high-load** | An operation requests unusually high LOAD versus its PURPOSE baseline | Core active effect for many LOADERs; possible INFECTOR signature |
| **low-load** | An operation requests unusually low LOAD versus baseline | Possible INFECTOR signature or benign outlier; not automatically malicious |
| **work-deviation** | WORK differs from the normal sampled amount at ordinary LOAD | May make an operation unusually long/short; distinct from compensating WORK for a LOAD change |
| **operation-deviation** | An operation kind changes or an unexpected kind is appended | Possible evidence in either role |
| **OVERLOAD** | A sustained positive LOAD modifier begins after a configurable delay | Implemented timed keyword; suitable LOADER tactic, possible benign noise |
| **SPIKING** | Alternating ordinary/elevated LOAD intervals | Implemented timed keyword; suitable LOADER tactic, possible benign noise |
| **DELAYED** | A temporal structure with an inconspicuous opening then later attack behavior | Design pattern realized through available behaviors/arcs; not yet a standalone keyword |

A signature is **not** a threat genus, an infection type, a confirmed classification, or a guarantee that the operation is malicious. Benign outliers remain essential to making investigation a judgment rather than a threshold test.

For **early INFECTOR anomaly selection**, our *proposed playtest weights* are **65% high-load, 10% low-load, 15% operation-deviation, 10% work-deviation**. These are **not** implemented weights or a permanent difficulty progression. LOADERs should use an **objective-aware temporal-tactic mix**, not inherit this generic INFECTOR table. Optional secondary anomalies, subtle deliveries, and benign outlier frequencies need separate tuning.

## Scheduler and director responsibilities

Distinguish **ordinary network traffic** from **hostile opportunities**:

- **Ambient network schedule:** How much legitimate company activity exists, which purposes are common, and how much natural contention/counterexample traffic appears.
- **Threat opportunity schedule:** How often the adversary attempts something, which genus and target it chooses, how many hostile plans can overlap, and how attacks scale across a run.
- **Genus-specific plan construction:** Starting from a concrete normal PURPOSE sample, generate a LOADER pressure plan or INFECTOR delivery plan with its own parameters and viability checks.
- **Simulation execution:** Existing `ConnectionSpawnPlan`, `SimulationHost`, allocator, operation effects, timed modifiers, and infection state continue to execute the concrete plan.

A separate `ThreatDirector` **might** eventually own adversary scheduling and multi-surface campaigns. **Do not assume that new class is required for the next slice.** The near-term implementation could separate plan-building and scheduling responsibilities inside the existing `ConnectionTrafficDirector`, then extract them when needed.

The important architectural seam is **attack intent → objective-aware concrete plan**, not the MonoBehaviour name.

### Implementation reality at the time of writing

Current Connections code does **not yet** follow all of this intended recipe:

- `ConnectionTrafficDirector` uses one ambient cadence and rolls hostile chance, then LOADER versus DELIVERY. This couples benign density and hostile frequency.
- It samples six purpose templates with independent normal LOAD and WORK. LOADER currently applies high LOAD **without** automatically compensating WORK, optionally adds WORK deviation, and can add an OVERLOAD/SPIKING keyword; many generated examples therefore end too quickly to exert sustained pressure.
- DELIVERY currently selects zero/one/occasionally two anomalies and attaches a typed infection-delivery effect to the **final operation**. A high-LOAD signature can unintentionally shorten the available interception period.
- `OVERLOAD` and `SPIKING` exist as timed modifier behaviors; the earlier `delayed-threat` manual profile is an operation arc, **not** proof of a general DELAYED keyword.
- The recorder already captures the generated **normal** and **final** operation values and decision branch. Ordinary inspection does not currently provide a comparative baseline or intent diagnosis.
- The simulation already supports Phantom LOAD, Blackout, Spawner, interruption, and player cleaning. Their exact effects and current scene defaults remain the responsibility of their focused contracts and runtime code.

This design foundation is intended to guide a focused revision, **not** retroactively describe implemented behavior.

## Boundaries and future taxonomy

LOADER and INFECTOR are **initial genera**, not the two permanent categories of all hostile activity. A future genus earns its place by introducing a different *attacker objective and player problem*, not simply another LOAD multiplier or keyword.

Potential examples to investigate later, **not selected designs**:

- **THROTTLER:** Impairs legitimate throughput by some mechanism other than simply consuming corresponding LOAD. Is causal attribution different enough to deserve a genus?
- **TUNNELER:** Establishes a new access route. Is this itself an attack objective, or instead an *attack surface* through which LOADERs/INFECTORs arrive?
- Other evasive, moving, or coordinated attackers may be tactics or multi-attack plans rather than new genera.

Do not prematurely implement these, or insist every future attack is exactly one connection or one objective. Keep the genus question distinct from the delivery surface and the tactics.

## Questions to settle during the next generation pass

1. **LOADER pressure target:** absolute LOAD, proportion of capacity, expected legitimate slowdown, or a hybrid? How much should current network demand influence target choice?
2. **LOADER lifetime:** what minimum preparation, attack-on, and spike counts are needed? Should a runtime duration constraint supplement WORK-derived lifetime?
3. **INFECTOR delivery window:** preserve sampled nominal duration, choose a genus-level range, or condition by payload and difficulty? How should multi-operation arcs allocate that window?
4. **Signature choices:** which anomalies are viable for each genus, in which stage, and with what magnitude? How do benign outliers avoid a trivial signature=hostile rule?
5. **Scheduling:** when to decouple benign density from hostile attempts, and how should genus weights/concurrent threat budgets work?
6. **Player reference information:** where do typical LOAD and nominal duration comparisons live, and which difficulty levels provide assistance without declaring “threat”?
7. **Success evidence:** what measurable player-facing outcomes represent LOADER disruption versus INFECTOR installation, without conflating the two?
8. **Future orchestration:** when does an attack plan need multiple connections or different surfaces rather than a single purposeful connection?

The working principle for the next implementation proposal is: **sample a credible benign PURPOSE, decide what the attacker wants, then build and validate a concrete connection that can plausibly achieve it.**

## Related source documents

- [Connections Current State](Current_State.md) — current integrated scene and runtime posture.
- [Purpose & Anomaly Generation V0](connections-purpose-anomaly-generation-v0.md) — six canonical purpose profiles and *current* anomaly mutation/rate behavior.
- [Threats V0](connections-threats-v0.md) — provenance of delivery, kill interception, operation arcs, and LOAD keywords.
- [Behavior & Modifier Foundation](connections-behavior-foundation-v0.md) — actual OVERLOAD/SPIKING timing and effective LOAD/WORK.
- [Infections V0](connections-infections-v0.md) — typed payloads, persistent node effects, and cleaning.
- [Network Health V0](connections-network-health-v0.md) — consequences to legitimate service.
- [Run Recorder V0](connections-run-recorder-v0.md) — private generation decisions, snapshots, and cause/effect evidence.
- [Threat Signals and Investigation (ideas)](connections-threat-investigation-ideas.md) — speculative tools and evidence, not yet an approved build plan.
