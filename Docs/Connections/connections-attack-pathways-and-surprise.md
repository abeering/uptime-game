# Connections — Attack Pathways, Consequences & Surprise

> **Status:** Working design direction agreed in the November 9–13, 2026 discussions, after the first playable Hacker Duel. **Not an implementation contract or authorization for a generic event framework.** Connections is the active prototype; Packet remains separate. For executable truth, use [Current State](Current_State.md), the runtime/scenes, and the individual implementation contracts.

## Why this matters

The game has accumulated functional LOADER/INFECTOR connections, persistent infections, timed Conditions, Worms, Payloads, and a Command Race Duel. Yet a knowledgeable playtester can often predict the consequence from the entity's identity. More individually polished threats won't necessarily make the network feel less solved.

**Goal:** The player can learn how an attack surface works without automatically knowing what the adversary intends to accomplish with it. The most exciting situations come from **attack pathways that compound, diverge, or respond to player action**, forcing reassessment of the live network. Surprise matters especially while the game's designer is its principal playtester.

This does **not** mean "make everything random." Deliberate authored events, contextual selections, conditional reactions, delayed effects, difficulty progression, and eventual hacker personalities are all valid ways to create uncertainty.

## Separate the responsibilities

| Concept | Question | Examples |
| --- | --- | --- |
| **Trigger** | Why is something requested now? | Connection operation completion, Duel outcome, Worm node entry, infection removal/CLEAN, an authored shift beat, a later gameplay-state condition |
| **Attack surface / delivery mechanism** | How does the attack appear, move, occupy time, and offer counterplay? | Logical connection, Worm, Payload, infection, or imposed Duel |
| **Consequence / effect** | What state or tactical situation changes? | Infection, timed Condition, temporary tool denial, additional traffic, Worm outbreak, Payload burst, Duel warning, protection/intelligence on success |
| **Event package / pathway** | Which concrete actions are composed, possibly over time? | Install infection and temporarily block cleaning; launch several Payloads; schedule a Duel; have a particular infection react to successful cleaning |
| **Selection policy** | Why this package, target, and timing rather than another? | Explicitly authored choice, seeded eligible pool, current network context, difficulty, Level Director, hacker preference |

These are **design distinctions, not five required managers or a universal object schema**. A connection should not need bespoke knowledge of every downstream consequence; similarly a CLEAN minigame should report a real outcome rather than directly owning every possible on-clean retaliation.

An event need not die when its initiating entity dies. Conversely, killing a carrier **before** its operation completes can still prevent the package from being scheduled. Specify that causal boundary per event rather than assuming one universal cancel policy.

## The player's experience: surprise with understandable rules

- **Unknown intention, stable mechanics.** A Worm still traverses physical links and admits isolation counterplay; a Payload still has its visible approach/interception window; a Duel still imposes a timed race. Its *outcome or follow-up* can vary.
- **Observable effects remain legible.** Don't conceal an overt Worm burst, tool lock, or incoming Payload merely to create mystery. Preserve covert infection knowledge: a player need not receive authoritative infection details on delivery.
- **Do not equate a symptom with a diagnosis.** For example, a CLEAN LOCKED status could exist on an uninfected node. The restriction is real; it does not prove infection.
- **Give the player something to do.** Temporary denial can be compelling when other traffic continues, other tools remain useful, or the player must contain a follow-up. Avoid punishments that amount to waiting through an inert timer.
- **Preserve causal evidence.** Record the selected package, source, target, seed/roll where relevant, scheduled and actual action times, and each success/rejection. Player-facing receipts report what was genuinely observed or applied, not secret future intentions.
- **Avoid fake comeback mechanics.** A Duel opponent's progress remains its own; unpredictable consequences should not secretly rubber-band to the player's momentary performance.

### Illustrative consequence candidates

| Candidate | New tactical situation | Current limitation |
| --- | --- | --- |
| Infection + temporary **CLEAN LOCKED** | Compromise persists while immediate remediation is unavailable | Implemented in Surprise V0 as Breach & Lockdown; the lock may apply when infection installation fails |
| Condition-spreading Worm outbreak | Visible branching pressure and node isolation decisions | Surprise V0 selects SCAN JAMMED or BACKDOOR on the existing Worm system |
| Infection-spreading Worm | Persistent compromise may spread across multiple nodes | Worms do **not** currently install persistent infections |
| CLEAN LOCKED without an infection | Creates uncertainty without fabricating a diagnosis | Supported as a direct atomic Condition; the default Duel Tactic also attempts an infection |
| Multiple attacking Payloads from one location | Immediate interception/triage under pressure | Surprise V0 resolves distinct reachable destinations at Duel scheduling |
| Duel victory benefit | Skill creates an unexpected *advantage*, not just the absence of harm | Source reveal, protection, Probe recharge and other rolled benefits remain candidates |

Other possible pathways include a hostile connection scheduling a Duel or a Worm, a targeted infection with an **onClean** reaction, and a coordinated follow-up after reconnaissance. These are **examples of composition**, not a checklist to ship in one milestone. Don't assume any example is balanced or guaranteed to be feasible at an arbitrary target.

A central-node Worm launch may be substantially stronger than a leaf-node launch. Budget, topology, isolation and route constraints matter; variety should not erase meaningful counterplay.

## How to select consequences, eventually

A package can be chosen **explicitly** by authored content or from a small **seeded eligible pool**. Where an attack is genuinely planned in advance, selecting its intention at planning time is appealing: there is a hidden plan the player may interrupt or later uncover. Some reactive events necessarily choose or validate parts of a response at trigger time. Do not pretend those are identical.

Selection should be valid for the current topology and available systems. If a package cannot execute, use a documented rejection/fallback policy, not a fabricated success. Difficulty and later hacker personalities may influence eligibility, weights, target choice, follow-up cadence, disguise, and intensity without hard-binding an entity species to one consequence.

A chain may emit another event, but recursion, repeated on-clean triggers and unlimited outbreak fan-out require explicit bounded behavior. Do **not** build an arbitrary event graph now.

## Grounded implementation boundary — Surprise V0 branch

**Implemented in the Surprise V0 branch:** Connections Command Race Duel retains its visible warning, startup splash, global slot, seeded hacker, and CLEAN focus behavior from [Duel V0](../Review/ConnectionsHackerDuelV0/README.md). Each accepted Duel now stores a concrete, seeded loss plan before countdown. The shared planner accepts atomic effects, named Tactics, and inline compound candidates; defaults, additions, explicit together-lists, and custom weight overrides share one vocabulary. A loss executes that stored plan component by component. Advanced hostile opportunities can schedule a Duel directly or queue a killable target-aware connection whose completion schedules one. See [Surprise V0](connections-surprise-v0.md). Rival difficulty remains a playtest tuning question.

**Not implemented:** A rolled Duel-success reward, infection-spreading Worms, migration of every threat into the shared planner, general on-clean reactions, a Level Director, hacker personalities, or shift-to-shift progression. The planner is threat-neutral, but Duel is its first runtime consumer.

### Intended order of work

1. **Docs reconciliation (this pass).** Keep existing design explorations and actual implementation authority distinct.
2. **Surprise V0 (bounded gameplay slice).** Try a small, testable set of variable Duel-loss recipes using existing mechanisms where possible—initial candidates: infection + short CLEAN LOCKED, Condition Worm outbreak, and several Payloads. Pick only what can be delivered coherently in a focused PR. Make outcomes explicitly invokable, seeded/replayable, and visible; separate choosing a recipe from performing it. **Do not** build the universal package framework first. Revisit Duel's easy tuning through actual typing playtests rather than a grand balance model.
3. **Progression vertical slice (next major milestone).** Build an authored short shift with a beginning, escalation, deliberate events, and an ending; potentially a second shift using the existing topologies to test progression. Let the shift coordinate eligible events/pressure while leaving per-incident outcomes partly emergent. No procedural topology, full campaign, or complete hacker identity system as prerequisites.
4. **Generalize only after evidence.** If variable consequences actually create richer situations, migrate additional trigger/attack surfaces toward shared package entry points incrementally, and give future Level Director/difficulty/hacker systems selection authority.

**Design principle to carry forward:** *Attack surfaces provide observable behavior and counterplay; event pathways determine how attacks compound; selection creates surprise. The player can know the rules without knowing the plan.*
