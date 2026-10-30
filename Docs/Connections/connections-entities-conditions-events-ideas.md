# Connections — Entities, Effects, Conditions, Statuses & Events (Working Ideas)

**Status:** Design exploration captured October 29, 2026. This is a vocabulary and future-direction note, **not an implementation contract, approved feature backlog, or instruction to build a generalized framework**. Connections is the active prototype; Packet's similarly named systems remain separate.

**Implementation context:** [Node Status Presentation V0 (PR #61)](https://github.com/abeering/uptime-game/pull/61) and [Conditions & Port Scan V0 (PR #62)](https://github.com/abeering/uptime-game/pull/62) are the current experimental basis at the time of writing; both were open and PR #62 was stacked on #61. Their branch code is implementation authority, not the proposals below. Refer to [Connections Current State](Current_State.md), [Infections V0](connections-infections-v0.md), and the [PR #62 review notes](../Review/ConnectionsConditionsPortScanV0/README.md) before assuming what has shipped.

## Central design realization

**Entity behavior and what an entity does are not one-to-one.** We can dramatically expand Uptime's hostile vocabulary by changing either dimension, then composing existing pieces into events. The most valuable distinction between entities is often the **kind of network motion, timing, evidence, and counterplay** they create — not a hardcoded attack attached to each entity class.

An adversary can also attack the **player's attention, certainty, and tools**, not only the network's Health or LOAD. A suspicious observation may matter without doing any damage; a temporary disruption may matter only because an infection or another attack arrives while it is active.

### Working vocabulary

| Concept | What it answers | Meaning |
| --- | --- | --- |
| **Entity** | How does it behave? | Motion, progress, timing, presence in the topology, and how the player can interact with it |
| **Effect** | What does an action accomplish? | An immediate consequence at a meaningful point, such as operation completion, node arrival, or a Crawler's visit; may apply a Condition or infection |
| **Condition** | What circumstance now exists? | A separately owned node state, usually temporary, that can be informational, harmful, or beneficial; independent of the thing that applied it |
| **Infection** | What persistent hostile foothold exists? | Resident node compromise that currently persists until actively remediated and may be covert |
| **Status** | What does the player know or see? | A visual projection of actual state, diagnostic knowledge, and brief feedback; not necessarily an authoritative gameplay object |
| **Event** | How are actions composed? | A reusable, possibly reactive sequence or incident that creates entities/effects and responds to outcomes |

These are **design responsibilities**, not a mandate for six managers, one universal interface, or a data-driven event scripting engine. Implement only the next useful seam.

## Entities: identity through network motion

These three planned or existing forms should feel different even when their eventual effect is identical.

| Network motion | Defining behavior | Player-facing tension |
| --- | --- | --- |
| **Logical connection** (implemented) | Takes a route; requests and receives LOAD; performs READ/WRITE/EXECUTE WORK; completes operations and may apply an effect | Observe anomalous behavior, inspect, decide whether and when to kill |
| **Payload** (future) | Slowly traverses one physical segment at a time toward a **hidden** objective; performs one decisive action on arrival | Predict destination/intent and time a protective **isolate** window precisely, particularly against infection delivery |
| **Crawler** (future) | Roams the network of its own accord; visits multiple nodes and performs actions while moving or pausing | Track its movement, anticipate disruptive visits, adapt while its consequences linger |

Payloads and Crawlers are **not implemented by PR #62**. The original Payload idea uses a moving icon reaching a terminal socket at an arrival moment. Isolating a node has an operational cost; isolation blocks an attempted infection at the protected node but does not inherently block a mere pass-through. These details remain subject to their own gameplay prototypes.

One effect could be applied by all three: a logical connection after completing WORK, a Payload upon arrival, or a Crawler during a visit. Conversely, one Crawler could create several different Conditions. The effect must not be defined by which entity happens to deliver it.

### Port Scan is a useful proof

A Port Scan is **not a fourth fundamental motion type**. In V0 it is an ordinary, killable hostile READ connection. Completing WORK applies **PortScanned** to its target. Killing it before completion prevents that application. The resulting Condition outlives its originating connection.

That distinction lets us add future reconnaissance activities without multiplying entity runtimes unnecessarily.

## Conditions are not infections

**Conditions** is the working name for node circumstances distinct from infections. The initial separation:

- Conditions have their **own authoritative lifecycle**, target, and (where possible) provenance, independent of their delivery source.
- V0 Conditions have a timed expiry, can be refreshed, and **are not removed by the clean minigame**.
- Conditions need not all be damaging, hidden, overt, short-lived, or forever uncleanable. Those are **per-effect policies**, not their definition.
- A Condition may be imposed by a short-lived connection, a visiting Crawler, a Payload, an infection, or a future player intervention.
- Future tools/upgrades may prevent, shorten, suppress, or clear Conditions without redefining the infection-removal game.
- An infection might sustain a Condition while resident, or might leave one that survives its removal; the specific causality and clearing rules should be explicit for each effect.

An **infection** remains a different kind of object: a persistent hostile presence with a cleaning/remediation interaction. A Condition must not become a disguised infection merely because it is negative or because its lifetime is substantial.

### Condition ideas to preserve

| Candidate | Player-facing meaning / effect | Intended tone |
| --- | --- | --- |
| **PORT SCANNED** | Hacker recently looked at this node; no intrinsic damage or promise of a later attack; draws attention | Awareness / passive negative; **implemented in PR #62** |
| **SCAN JAMMED** | The player's node infection scan is temporarily unavailable | Pain / urgent; future |
| **CLEAN LOCKED** | The player's node cleaning tool is temporarily unavailable | Pain / urgent; future |
| **WORK STALLED** | Productive WORK at the node temporarily stops or is interfered with | Pain / urgent; future; exact scope unsettled |
| **HOSTILE ACCELERATED** (working name) | Hostile connections make WORK approximately 2× as fast at the **same requested LOAD**; may be irrelevant if no hostile is present | Pain / indirect pressure; future; numeric example, not balance |
| **ISOLATED** | A protective player-imposed window, with lost productive WORK as a meaningful cost, originally proposed against infection-carrying Payload arrival | Beneficial/control with a tradeoff; future |
| **WORK BOOSTED** (illustrative) | Helpful temporary improvement; potential positive Condition | Beneficial; speculative |

The important distinction is **potential** versus **present damage**. For example, HOSTILE ACCELERATED would not prove that a malicious connection is currently active or identify one. CLEAN LOCKED might matter little until an infection needs removal. PORT SCANNED is reconnaissance evidence, not a guaranteed forecast.

More reconnaissance flavors could include authentication probes, vulnerability checks, or multi-node sweeps. They may be different event compositions, different delivered effects, or simply different connection behavior; **do not assume each requires a new entity or Condition kind**.

### Conditions and visible statuses are separate

The node presentation established in PR #61 groups **what the player sees** into emotional families, not by technical origin:

- **Pain** (red): active trouble such as OFFLINE, known INFECTED, or a future SCAN JAMMED.
- **Awareness** (purple): concerning but not necessarily actionable evidence, such as PORT SCANNED.
- **Beneficial / informational** (teal/green): CLEAN confirmation, future ISOLATED, or a useful boost.

Multiple statuses in one family can share a panel and node highlight; multiple families can coexist. Timed statuses can show a per-row radial timer.

**Not every status is a Condition.** OFFLINE is an overt Blackout symptom; INFECTED is player-acquired knowledge from a completed diagnostic scan, **not** automatic exposure of covert infection; CLEAN is a **brief scan-result acknowledgment with no timer**, not a sustained assertion of safety. PORT SCANNED is the first authoritative, expiring Condition shown through Awareness.

An entity applying an effect or an infection arriving must **not automatically disclose hidden state** merely because the status system can display it. Visibility and player knowledge are separate concerns.

## Events: reusable behavior, not deterministic timelines

Packet's earlier [events](../Design/events.md) and [level/run design](../Design/level-and-run-structure.md) leaned toward authored schedules and attack plans. We want to retain deliberate, memorable choreography **without requiring every level to replay identical spawns at identical times**.

The emerging responsibilities are:

- **Company** supplies the business, legitimate traffic, what matters, and the stakes.
- **Villain** supplies preferences, tactics, aggression, escalation, and recognizable reactions.
- **Level** supplies the particular confrontation, repertoire, desired pressure, and potentially guaranteed **signature moments**.
- **Live simulation and player decisions** determine viable timing, targets, evolving conditions, and how events actually unfold.

The eventual **LevelDirector** would set or adjust the policy: benign-traffic goals, hostile opportunity pressure, eligible event repertoire and weights, lulls, explicit signature beats, and reactions to the network/player. It need not curate every individual spawn.

**Ambient generation** can select a single hostile connection **or a reusable event** from the same hostile opportunity process. A LevelDirector can also request the **same event definition explicitly**. Avoid building independent Port Scan, Crawler, and Payload mini-storytellers, each with a separate ambient spawn clock. Selection and execution are distinct responsibilities; the Traffic Director and SimulationHost continue to own concrete connection planning and authoritative network simulation.

**Event execution** can coordinate one or many entity actions, effects, Conditions, and later narrative messages. A deliberately authored attack can still adapt to the chosen target, available routes, player intervention, and unexpected state. Some events are ambient improvisation; signature events can have stronger occurrence guarantees without fixed minute-by-minute choreography.

## Event lifecycles can create villain identity

An event is **not equivalent to its first connection**. It may outlive a killed scanner, wait for an outcome, branch, schedule another action, or conclude after its follow-up resolves.

A useful conceptual lifecycle is: **selected / explicitly initiated → active or waiting → observe outcomes → continue, change course, or finish → retire**. This is not a required runtime enum.

Outcome-aware behavior is a major future source of personality:

| Observed outcome | Possible authored reactions (exploratory, not defaults) |
| --- | --- |
| Reconnaissance completes | Quietly finish; schedule a later infection; select an interesting new target |
| Player kills reconnaissance | Proceed anyway; back off; **retaliate**; accelerate/defer follow-up; change tactics |
| Blackout cancels a carrier | Abandon, postpone, or choose another route/target |
| Follow-up attack is killed, completes, or cannot spawn | Conclude, escalate, switch target, or react with narrative feedback |
| Network condition or player behavior changes | Pause pressure, exploit an opening, switch incident weights, protect a signature beat |

The intentionally extreme example of an **onKill reaction that launches 50 INFECTORs** is useful as an *expression of possible event behavior*, **not a mechanic to implement now**. Other possible knobs include outcome-specific follow-up delays or chances, but avoid committing to a general onKill hook API or arbitrary event script language at V0.

### Reconnaissance V0 and the unresolved kill question

PR #62's initial Reconnaissance event makes a seeded follow-up decision, then — if selected — schedules a target-matched INFECTOR after the Port Scan **resolves**, even if the player killed it. With its initial **8-second post-resolution delay**, an early kill can therefore make the attack happen earlier than if the scan had completed.

This is an **observed implementation consequence**, not a finalized design principle. Killing the scout should **not automatically imply defeating its parent event**, but we have **not** decided what default kill/completion/cancellation reactions should be. Preserve that question for live playtesting rather than adding per-outcome probabilities or callbacks speculatively.

Completed events should retire from the **active runtime collection** after any needed follow-up has finished or failed; records of what happened belong in the run log. The small lifecycle cleanup was requested for PR #62 separately; check that branch rather than assuming it is integrated.

## What to evaluate before broadening the system

- Does PORT SCANNED direct the player's attention in an interesting way, or become background noise? Initial human impressions are positive but possibly **too frequent**; do not rebalance from one run.
- Do players remember the scanned node, notice a later related attack, or assume every scan forecasts one?
- What should happen after kill vs natural completion vs Blackout cancellation, and should different villains behave differently?
- How will multiple Conditions of the same kind from different sources interact: refresh, independent contributions, suppression, or stronger effect?
- Which Conditions should be visibly announced, which should require investigation, and how do we avoid leaking hidden threat classification?
- How can reactive events create memorable differences between villains while maintaining room for randomness and genuine player agency?
- When is a tiny new seam useful, and when would a general framework actually pay for itself?

**Near-term posture:** Keep the initial Port Scan, Condition, and Reconnaissance V0 small; playtest first. This note preserves possible future mechanics, event grammar, and the architectural boundaries between them. It authorizes **no** new Conditions, entities, balancing changes, LevelDirector, or event scripting framework.
