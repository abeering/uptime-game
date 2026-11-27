# Uptime — Living Game Design

**Status:** Shared game-design overview, October 22, 2026. This document states the direction of **one Uptime game** while Packet and Connections test different representations of network activity. It is a navigation and synthesis layer, not a replacement for the full documents linked below. Those sources retain their original examples, alternatives, questions, and wording. No design statement here implies that either prototype already implements it; use [Uptime Current State](../Uptime_Current_State.md) for implementation posture.

## The game we are making

The player is an overworked network operator facing escalating pressure while making fast, typed decisions with incomplete information. Their job is to keep a company's systems useful under stress, infer what matters, choose imperfect interventions, and live with the consequences. Speed of command entry matters because attention and timing matter, not because the interface should add arbitrary friction. The network should visibly respond to real activity and interventions, so the player can learn its behavior and form better judgments.

This is the shared Uptime fantasy. Packet tests it through individual packets, discrete traversal, scanning, classification, and interception. Connections tests it through persistent logical connections, operations, WORK, LOAD, inspection, and blocking. These are different mechanical lenses on the same game. Neither automatically inherits the other's timing, resource model, command delays, threat labels, or progression rules. Connections is the active experiment; Packet remains a viable one.

The broadest earlier working statement is preserved in [game.md](game.md). Its emotional loop, system-shaping ambitions, and network philosophy continue to inform Uptime. Its particular run lengths, upgrade counts, topology rules, failure thresholds, and other numeric or structural proposals remain subject to play and design decisions.

## Established shared intentions

- **Pressure and judgment:** The central skill is allocating attention, acting with imperfect knowledge, and accepting tradeoffs. The player should understand why the network changed, even when they did not know everything in advance. See [game.md](game.md) and the three pressure surfaces in [gamesurfaces.md](gamesurfaces.md).
- **A company worth protecting:** Company-specific operations and priorities should change what matters in a run. A hacker adversary supplies hostile pressure; ordinary load, infrastructure trouble, and mistaken interventions can be consequential too. The company and adversary direction is shared even though exact catalogs, attack schedules, and failure rules are open. See [company-designs.md](company-designs.md), [level-and-run-structure.md](level-and-run-structure.md), and [events.md](events.md).
- **Fast, keyboard-first operation:** Commands are the player's working tools. Information and consequences should be legible enough for quick decisions. Packet's queued scan/trace and Connections' immediate inspect are prototype choices, not one shared command contract. See [game.md](game.md), the Packet [scan/trace note](../Packet/scan-vs-trace.md), and the [original Connections brief](../historical/connections-v0.md).
- **A system that pushes back:** Traffic, infrastructure, and information can all create pressure. Infections, failures, clean interventions, and confusing messages may demand attention rather than simply increasing traffic volume. These are Uptime design concerns even where existing examples use packet vocabulary. See [gamesurfaces.md](gamesurfaces.md), [infections.md](infections.md), [clean-minigames.md](clean-minigames.md), and [notifications.md](notifications.md).
- **Strategic run identity:** Network growth, node specialization, upgrades, and different company needs should make runs feel meaningfully different. Choices should change behavior and create tradeoffs, not merely improve every number. See [upgrade-strategies.md](upgrade-strategies.md), [company-designs.md](company-designs.md), and the topology documents below.
- **Evaluation with meaning:** Scoring should distinguish judgment and performance from merely surviving. What is measured, rewarded, and presented remains unsettled. Packet's current score ledger is one implementation, not a universal scoring specification; Connections' run recorder captures evidence but is not a score system. See [scoring.md](scoring.md).
- **Human consequences and fiction:** Messages, characters, and events should connect simulated state to the people affected by it. They may inform, misinterpret, pressure, or occasionally mislead the operator. The detailed cue architecture and content are still proposals. See [notifications.md](notifications.md) and [events.md](events.md).

These are game-design intentions, not an assertion that every listed system is built. Future work can revise them when human play and prototype evidence warrant it.

## Proposed next milestone — Connections: business operations and reliability (2026-11-24)

**Status:** Exploratory design / sprint guidance, **not** an implementation contract or a fixed feature backlog. The active Connections game is already compelling as real-time investigation and threat response. This direction aims to make ordinary company traffic a game in its own right, without losing the fast, arcade-first, typing-focused experience. Older run, scoring, and pacing proposals remain useful context; this milestone does not silently resolve them.

### North star: configure the system, investigate connections, handle threats

- **Configure the system:** Make a few persistent, understandable decisions on nodes that affect whole *classes* of connections, rather than continually micromanaging individual connections. Prefer small `ssh <nodeId>` management consoles (arrows/Space/Enter; Escape returns to the main terminal) to a growing list of global verbs.
- **Investigate connections:** Understand PURPOSE, expected operations, anomalies, routing, and whether the network behaves as configured.
- **Handle threats:** Kill hostile connections, intercept attacks, clean infections, and deal with the operational consequences. Hackers should complicate the company-operations game, not be the only reason it exists.

**Target experience:** Mid-run, the operator has configured API for the current demand, selected CACHE purposes, traced and blocked two hostile sources in FIREWALL, and has IDS coverage. Attacks bounce off the firewall; multiple legitimate connections rapidly complete through CACHE; LOAD is below 50% everywhere. The player takes their hands off the keyboard and enjoys a network that is working *because of their choices*. A new business circumstance or attack eventually makes those choices worth reconsidering. Earned calm is a payoff, not an automatic director lull.

### 1. Legitimate connection fragility and company failure (foundation)

- **Connections need to fail naturally**, not just become slower. Explore timeouts under sustained LOAD contention, plus interruption (e.g., BLACKOUT, restart, mistaken kill) and rejection/unavailability as distinct causes of the same outcome: *legitimate business failed*.
- Determine a sensible starting timeout relative to expected lifetime/purpose; give enough warning and recovery opportunity for real triage. Show impending failure clearly on the network (tag/state/visual cue) and in inspect (time remaining, progress, projected completion/at-risk state). A connection may become unsalvageable; killing it early can free resources but must not erase its failure.
- **Business importance:** Some legitimate connections are specially marked as priority by company events/circumstances; losing them may cost more against a reliability/failure allowance. Ordinary connections still count. **Company-assigned importance** is separate from **operator-configured network priority**; the network does not automatically favor important work.
- The working fantasy is **“lose too much legitimate business and you get fired.”** Critical/core compromise may separately end a run. Whether failure tolerance is per shift, cumulative over the run, weighted, partially refreshed, or an evolving company-operation measure remains *open*. Do not reintroduce a hidden global WORK-by-time impossibility as the default solution.
- Completed legitimate **WORK remains useful as a measure of productivity and progress toward breaks, upgrades, and run advancement**, without requiring a hard completion deadline. Its relationship to score and varying operation costs (including CACHE) still needs definition.

**Core test:** Can the Small scene, with **no hacker**, produce readable, recoverable connection failures under legitimate congestion—and can an operator's decisions save those connections?

### 2. Node configuration can change passing traffic

A node may **perform WORK when targeted** and **modify traffic passing through it**. Avoid a rigid WORK-node-versus-transit-node taxonomy; API, for example, can do both and still be a legitimate infection target. Node identity, topology/path, and visible connection behavior should communicate what a policy affects. Today, intermediate nodes are primarily route geometry in the Connections LOAD model; applying policies to transiting connections would be new behavior, not a description of current implementation.

- **API first candidate — purpose priority:** In `ssh api`, select from observed/known PURPOSES to favor. Matching connections traversing API receive a greater share of downstream processing capacity, while other work is comparatively slower. This changes allocation, not total capacity. Start simple; exact weighting, slot limits, and operation-level scope are playtest questions.
- **CACHE second candidate — purpose selection:** An addable/upgradable node offers a limited number of observed PURPOSE slots. Eligible READ operations for selected purposes use CACHE (possibly all of a pure-read connection or only part of a mixed connection), reducing the LOAD/processing WORK needed. Preserve the distinction between *cheaper execution* and *less business delivered* when measuring company success. CACHE has capacity and can be infected or manipulated; unexpected purposes routed through it can become an investigation clue.
- **FIREWALL, IDS, load balancers, etc.** remain future node-management opportunities, not prerequisites for the first test. A traced source becoming a firewall-blocklist entry is a promising illustration of persistent preparation.

### 3. Routine node operations: conditions, maintenance windows, and scheduled jobs

This is **not** a timer saying “do maintenance in 30 seconds or lose score.” A node can show `<!> REQUIREMENT`; `ssh <nodeId>` reveals a very small menu of **what is needed, what it costs, what improves, and what is currently going wrong if deferred**. The operator chooses when there is room to act. For example:

    > ssh db
      [a] System maintenance — uses 10 LOAD for 15s; restores lost efficiency
      [b] Security update — restart for 10s; interrupts active connections
      [Esc] Leave for later
      Current effect: reduced effective capacity until maintenance completes

- **Maintenance** may restore a node's reduced efficiency or remove a vulnerability; doing it during heavy traffic might consume LOAD or interrupt/drop work, while deferring it leaves a concrete, visible disadvantage. An occasional explicitly time-critical request is possible, but countdown penalties are not the default foundation.
- **Scheduled/background jobs** were raised alongside maintenance and must not get lost: backups, reports, syncs, migrations, etc. occupy a node's job queue and compete for the same LOAD as connections. Explore a simple pause/resume or a cap/ratio for **connections vs. jobs**. The jobs represent real company obligations; delaying them can affect later circumstances without merely subtracting arbitrary score.
- **Company messages** forecast demand and obligations (e.g., a sale, tax reports, maintenance) and give the player an opportunity to preconfigure API/CACHE or find a maintenance window. Messages can be explicit and funny. The challenge is limited attention and overlapping requests—not requiring the player to solve an obscure riddle or watch a long story.

### Sprint questions / boundaries

1. Prove **timeout danger** from legitimate traffic alone, with observable warnings and actual failures.
2. Prove that **one small persistent node setting** (API purpose priority is the leading candidate) can prevent or cause those failures; its consequences should be apparent in network and inspect views.
3. Explore **node maintenance and background jobs** as competing, player-timed obligations with concrete efficiency/availability effects, rather than arbitrary score deadlines.
4. Preserve CACHE, firewall/IDS, and broader topology as coherent follow-on design opportunities, **not** a demand to build all node types in one sprint.
5. Defer final decisions on duration/shift rhythm, cumulative versus shift-scoped failure budgets, precise priority weights, WORK progression thresholds, and hacker encounter/endgame structure until gameplay evidence exists.

The aim is **changing business demand → player-controlled network behavior → visible connection success or failure**. Configuration, investigation, and threat handling should reinforce one another; no extra management screen is worthwhile unless it changes what happens on the existing network.

## Connections reliability milestone — next visual phase (2026-11-25)

The proposed [Business Operations & Reliability milestone (#128)](https://github.com/abeering/uptime-game/pull/128) remains design provenance, not a claim that its broader configuration, priority, maintenance or background-job systems are implemented. [Connection Fragility V0](../Connections/connections-fragility-v0.md) is its first foundational implementation slice. Immediately after fragility playtesting, **Connection Visual Language is the next major implementation phase**. Deferral is intentional and temporary: improve rapid information parsing and give the network a substantially stronger aesthetic identity.

Established intentions for that separate phase:

- **Network heartbeat:** a shared visual rhythm; blips advance in rhythmic steps with smooth lerped/eased movement, then settle. Coordinated motion should feel distinctive, rather than generic continuous particle scrolling. The rhythm need not carry gameplay meaning.
- **Persistent connection rails:** retain the rectangular line body/boundaries as a persistent visible element and primary color/state carrier. Do not place all information in traveling blips. Explore greys for ordinary business traffic, blues for priority traffic, and distinct slowed, near-timeout, timeout and killed treatments. Company-assigned business importance remains distinct from operator-configured processing priority.
- **Activity blips:** explore requested LOAD through density/spacing, operation kind through direction/shape/pattern, and slowdown through cadence, distance or another treatment compatible with the heartbeat.
- **Identity pill:** explore interior or perimeter/radial fill for current-operation progress. Exact progress and timing semantics remain a visual-design question.
- **Future dynamic operation routing:** account for operations within one connection targeting different nodes, especially CACHE. Persistent identity, route redrawing, shared route segments, target changes, transition animation and whether rerouting consumes simulation time require a separate design and implementation pass.

None of this redesign or dynamic routing belongs in Fragility V0. Its danger text/countdown and failure count are temporary review surfaces. Failure allowances, firing, weighted importance, WORK progression, final ambient demand and shift/run policy remain open; unmerged experiments #115/#124/#125/#126 remain evidence rather than current gameplay.

**Accepted visual reference (#130, November 25):** [Display Lab V0](../Connections/connections-display-lab-v0.md) establishes the shared stepped/eased heartbeat, continuous established rails at every performance/danger state, disrupted activity for slowdown/stall, and interior current-operation WORK fill as the leading integration candidate. The perimeter rack remains a reference. This accepted lab does not itself integrate gameplay, priority or dynamic routing.

## Connections single-shift rule — Business Reliability & Shift Objectives V0

**Accepted follow-up — Legitimate Workload Increase V0:** Both canonical scenes scale legitimate generated processing LOAD and WORK by 2.5× while preserving nominal service duration, timeout budget and equivalent unscaled business credit. Hostile composition, arrival policy and PURPOSE weights remain unchanged. This incremental gameplay trial follows human acceptance of #132; it does not settle final balance or adopt the #126 scheduling experiments. See [the implementation contract](../Connections/connections-legitimate-workload-v0.md).

The accepted single-shift contract is **complete successful legitimate business WORK before the company failure limit is reached**, with CORE breach as independent catastrophe. A connection is the unit of business success/failure; operations are processing units. Only successful whole-connection completion earns its fixed concrete authored WORK value. Failed arcs earn zero, including completed intermediate processing. This supersedes the unmerged #124 experiment's operation credits and fixed six-minute quota deadline.

There is **no enforced shift duration, insufficient-WORK defeat or required WORK-per-second pace**. Slow success takes longer and extends exposure to existing problems. WORK target success ends the shift; reaching the failure limit or CORE ends the run. One playable shift currently stops at any result, while the outcome model keeps successful shift completion distinct from run failure. All genuine same-boundary receipts settle with CORE, then dismissal, then WORK-success precedence.

Initial future between-shift policy is a **complete WORK/allowance reset**, permitting narrowly saved shifts without permanently carrying a depleted allowance. The actual next-shift transition, persistent run state, upgrades and topology expansion remain deferred. Business priority weighting, API/CACHE and company operations remain open. [Current V0 contract](../Connections/connections-business-reliability-shift-v0.md) documents provisional independent goals and single-shift scope. Its UI adapts existing authored elements; accepted Connection Visual Integration is not reopened. Next comes Business Demand & Congestion, informed by unmerged #125/#126 without importing their schedulers into this phase. This is Connections scope, not a Packet mechanics change or a final balance policy for all Uptime designs.

## Connections — independent business demand

The accepted next implementation replaces population-responsive legitimate arrivals with an independent seeded exponential business clock. Network improvement should not automatically cause compensating demand; congestion, failures and outages do not suppress subsequent requests. Comparable-volume settings from #125 are provisional starting references, with the human-accepted 2.5× workload and current business objectives retained. Nominal occupancy from #126 is not the production model. See [Independent Business Demand V0](../Connections/connections-independent-business-demand-v0.md).

Future company circumstances may deliberately change frequency, PURPOSE mix, workload, directed batches and individual-request importance. Importance belongs to requests and their circumstances, rather than permanently to PURPOSES; operator-configured processing priority is separate. This phase implements neither company events nor priority mechanics. Historical experiments remain evidence, not dependencies.

## Connections — first persistent API policy (2026-11-26)

[API Configuration V0](../Connections/connections-api-configuration-v0.md) implements the narrow API candidate from #128: one known-PURPOSE slot, empty by default, changed through `ssh api`, with a provisional configurable 2× allocation weight for matching routes containing API. Allocation is capped at actual demand and redistributes spare shares; favoring one class can save its business WORK at the expense of another. Hostiles claiming the same stable PURPOSE receive the same benefit. This is allocation priority, not company-assigned importance or knowledge of legitimacy. CACHE, other policies, multiple slots, maintenance, events and contextual importance remain proposals. The earlier milestone wording is retained as design provenance; this contract establishes only the first slot.

### Deferred observations from API Configuration V0

Threat readability and investigative fairness need a dedicated future pass. Suspicion should lead to meaningful investigation rather than arbitrary identification. Benign anomalies and genuine hostiles should be distinguishable through sufficient evidence, without making ordinary demand itself proof of hostility.

High-density operational pressure and lower-density investigative pressure are different legitimate gameplay profiles. Company demand characteristics and hacker behavior characteristics can independently shape those profiles. Future LOADER refinement should explore greater prevalence and richer keyword combinations. These are recorded design observations, not changes to current hostile frequency, keyword generation, palettes or investigation mechanics.

## Connections — operation routing and CACHE (2026-11-26)

[Operation Routing & CACHE V0](../Connections/connections-operation-routing-cache-v0.md) implements the narrow second node-configuration candidate after human acceptance of API Configuration #136. A persistent logical connection retains its identity, claimed PURPOSE, operation sequence, original business value and whole-connection service budget while each operation independently resolves its actual destination and physical route against current configuration. Commit before traversal/processing and lock until completion/termination. API allocation remains live; configuration affects later routing decisions without migrating unfinished WORK or automatically failing over.

One known eligible READ-PURPOSE slot in `ssh cache`, empty by default, redirects matching READs to an ordinary 10-LOAD CACHE pool with provisional 0.25× processing WORK and 0.5× requested LOAD. WRITE/EXECUTE stay ordinary. Matching hostile claims benefit equally and retain their behaviors, effects and earned evidence. Cheaper processing earns identical original business value only when the entire connection succeeds. The same authored SSH architecture and shared rails, heartbeat, pills and sockets remain; this is no new positive rail status or SSH redesign. CACHE population/freshness, extra slots, load balancing, upgrades, contextual importance and company events remain unimplemented. Earlier milestone proposals above retain their original wording and provenance.

**Route-transition refinement (#137):** A changed operation route is REROUTING, distinct from initial CONNECTING. Preserve the longest established prefix; retract only the obsolete tail and extend the new suffix. Charge only new-link traversal, with cosmetic retraction adding no delay. Unchanged routes switch operations directly. Logical identity, inspect history and the original service deadline continue; no processing WORK occurs while rerouting. The accepted renderer, heartbeat and palette remain. Strict-prefix shortening has zero traversal time and may resume processing while its obsolete tail retracts.

### Consolidated post-milestone review TODOs — deferred

- **SSH presentation:** cohesive terminal-style windows; reuse existing terminal sugar, splash screens and connection sequences. Explore modular ncurses-inspired capability sections, Tab/Shift+Tab between sections and arrow/Enter interaction within the focused section, with window dimensions adapting to available capabilities. Accommodate future FAST LANE, maintenance and honeypot sections without implementing those hypothetical capabilities now.
- **Connection presentation:** visible positive feedback for beneficial configuration, distinguishing preferential allocation from processing optimization while keeping coexisting slowdown and service risk visible. This does not approve a new palette or rail language in this pass; the deferred identity-palette and WORK-bar deadline-checkpoint ideas remain recorded in the visual integration contract.
- **Gameplay:** meaningful company demand scenarios that make network configuration compelling; dedicated threat readability and investigative fairness; sufficient evidence distinguishing benign anomalies from genuine hostiles; independent operational versus investigative pressure profiles shaped separately by company demand and hacker behavior; more prevalent, mechanically varied LOADERs and richer keyword combinations. These remain review/design work, with no hostile or demand retuning in CACHE V0.

## Active working designs and unresolved details

**Run framing.** Overarching run and progression design belongs to Uptime as a whole. “Run = job” and “level = day/shift” is the working shared frame: the operator works for a company through a series of increasingly difficult shifts; survival and performance may be judged separately. Exact number and length of days, failure thresholds, advancement, difficulty selection, and reward schedule remain flexible. Prototype-specific progression mechanics do not transfer automatically between Packet and Connections. [level-and-run-structure.md](level-and-run-structure.md) contains the full proposal, including Packet-shaped examples; [levels-phase1.md](../historical/levels-phase1.md) and the [illustrative first prototype level](../research/first-prototype-level.md) preserve other level-thinking and implementation hypotheses.

**Company and adversary.** Different businesses can create different traffic, tolerance, topology, tone, and operational stakes. Hacker adversaries can bring evolving tactics and coordinated incidents. The ten [company sketches](company-designs.md), specific villain pacing, catalog sizes, and a one-to-one company/attacker structure are design material to test, not locked content or required data models. The [original Connections V0 appendix](../historical/connections-v0.md) also explores company-authored connection purposes and event orchestration without making them current implementation.

**Infrastructure and topology.** Growth and specialization are strategic goals. The [topology overview](toplogy-system-v1.md), [node-role study](topology-nodes.md), [constrained grammar](topology-grammar.md), and [runtime proposal](../research/topology-runtime-spec-v1.md) retain complementary detail about roles, generation, layouts, and mutations. They currently describe proposals, including Packet-oriented mechanics and an unbuilt `TopologyDirector`; neither prototype's authored scene is evidence that the generator exists. Do not collapse those perspectives or silently adopt their exact graph grammar for Connections.

**Threats and interventions.** Infections, clean minigames, disruptive events, and attention-demanding interventions are relevant to Uptime generally. [infections.md](infections.md) surveys threat effects; [clean-minigames.md](clean-minigames.md) contains multiple detailed minigame drafts; [clean.md](../research/clean.md) retains a separate small observation; [gamesurfaces.md](gamesurfaces.md) explores infrastructure and information disruptions. Packet implements portions of this space. Connections generates benign and hostile traffic and has a narrow [Phantom LOAD infection slice](../Connections/connections-infections-v0.md). Further translation requires explicit design and implementation decisions, not word substitution.

**Information and scoring.** [scoring.md](scoring.md) proposes event/context evaluation and a score ledger; actual scoring semantics for a full Uptime run are open. Packet's [scan difficulty](../Packet/scandifficulty.md), [scan versus trace](../Packet/scan-vs-trace.md), [packet keywords](../Packet/keywords.md), and [scan timing reference](../Packet/edge-traversal.md) explore one representation's information game. Their larger attention and uncertainty questions may transfer, but their packet stages, tick timing, and keyword mechanics do not automatically transfer.

**Narrative presentation.** [notifications.md](notifications.md) preserves an extensive communication design, including speakers, escalation, disagreement, and silence. [events.md](events.md) provides older tick-based examples. Whether messages arrive from an authored run plan, reactive state, or both remains a design choice. They should have causal relationships to the playable system rather than imply simulated incidents that never occurred.

## Prototype-specific authority

Read [Packet Current State](../Packet/Current_State.md) or [Connections Current State](../Connections/Current_State.md) for the implemented representation. The current Connections [LOAD](../Connections/connections-arcade-load-v1.md), [operation arcs](../Connections/connections-arcade-operation-arcs-v1.md), [traffic](../Connections/connections-arcade-traffic-v0.md), and [recorder](../Connections/connections-run-recorder-v0.md) documents explain its milestone, subject to runtime and serialized scene truth. The older [network and connection grammar](../historical/network-connection-grammar-v1.md) preserves conceptual language and a superseded resource model; it must not be used as the current allocator contract.

Packet-focused notes and examples, including [keyword ideas](../Packet/kind-keyword-term-dump.md), [debug spawn commands](../Packet/spawn.md), and [UI ideation](../Packet/gpt-prompts.md), remain complete in the Packet folder. [Old TODOs](../historical/todo.md) and [bug observations](../historical/bugs.md) remain complete as historical records. Their presence records design work, not an instruction to build every item. The [research index](../research/README.md) and [historical index](../historical/README.md) explain the status of other relocated sources.
