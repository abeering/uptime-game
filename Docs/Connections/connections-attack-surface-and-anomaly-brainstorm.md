# Connections — Attack Surfaces, Counterplay & Behavioral Anomalies (Brainstorm)

> **Status:** Open-ended idea catalogue from the November 13–18, 2026 design conversation. **Speculative, not an approved backlog, implementation contract, feature list, tuning decision, or Codex handoff.** Do not change gameplay because an idea is recorded here. Connections is the active experiment; Packet remains a separate, retained prototype.
>
> **Scope:** What appears in the network view, how it behaves, how the player recognizes it, and what the player might do about it. **Not a catalogue of consequences/effects.** For implemented behavior, defer to [Current State](Current_State.md), focused contracts, runtime code, and serialized Unity scenes.

## Why this note exists

A network of Connections, Payloads, Worms, and eventually Tunnels can be extended in two different directions: **new attack surfaces** that inhabit or manipulate different parts of the network, and **behavioral anomalies/keywords** that make an existing entity act differently. A third, equally important design dimension is **counterplay**: what is the player actually asked to do?

The motivation is not to produce more ways to install an infection or cause damage. It is to preserve a wide vocabulary of distinctly recognizable, interesting, and sometimes surprising **situations**. An attack surface should ideally introduce a new sort of attention, inference, positioning, or action; its eventual consequences can be selected independently. See [Attack Pathways, Consequences & Surprise](connections-attack-pathways-and-surprise.md).

### Keep these responsibilities separate

| Layer | Design question | Example |
| --- | --- | --- |
| **Surface / entity** | Where does it live and how does it move or persist? | Link parasite attached to an edge; Circuit spanning three nodes |
| **Behavior / anomaly / keyword** | What unusual rules does a known entity follow? | Payload reverses; Connection interacts with nodes along its route |
| **Interaction / counterplay** | What can the player observe and do, including prerequisites? | Expose a shield by throttling a link, then kill during a window |
| **Consequence / effect** | What actually happens if the hostile action succeeds? | Deliberately **outside** this brainstorm |
| **Pathway / selection** | Why does this show up now, and what follows? | Existing [attack-pathways notes](connections-attack-pathways-and-surprise.md); no fixed answer here |

A counterplay verb is **not** automatically a command proposal. Examples such as detach, revoke, sever, redirect, and disarm express possible gameplay interactions; naming, costs, prerequisites, and UI remain unresolved.

## Starting point: recognizable surfaces

These are the existing or previously discussed comparison points, **not a claim of exhaustive implementation status**:

| Surface | Recognizable network behavior | Distinct player attention |
| --- | --- | --- |
| **Logical Connections** | Persistent paths traverse nodes, request target LOAD, and perform READ/WRITE/EXECUTE WORK; inspectable and killable | Observe behavior, inspect/logs, judge whether and when to kill |
| **Payloads** | Timed, hop-by-hop visible movement; final destination hidden; final approach can be intercepted | Predict destination, choose node, time interception |
| **Worms** | Branching propagation through physical topology; branch bodies are not ordinary killable connections | Follow branches, choose where and when to isolate nodes |
| **Tunnels** (prior proposal) | A new node near an existing network node establishes a singular ingress point | Recognize an added access point and decide how to close/defend it |

Reference implementations/designs: [Payload & Interception](connections-payload-interception-v0.md), [Worm Propagation & Isolation](connections-worm-isolation-v0.md), [Threat Investigation Ideas](connections-threat-investigation-ideas.md), and [THREATS](THREATS.md). Do not infer a Tunnel implementation from its inclusion here.

## A. Candidate new attack surfaces (14 ideas; preserve all)

These are **possible interactions**, not promises. The important unit is the new player problem, not a species name or predetermined effect.

### 1. LINK PARASITES — Attachments on physical edges

- **Presence:** A visible clamp/object lives on a physical link, while unrelated logical connections pass it.
- **Possible interaction:** Spot abnormalities, use an edge Probe to reveal it, then remove or disable the attachment; perhaps the link must first be quiet.
- **Different challenge:** Investigate *infrastructure occupied by something*, not a particular suspicious flow. Is removal targeted, or does it temporarily disrupt the edge?

### 2. PORT BREACHES — Contested entrances to nodes

- **Presence:** A foothold or object establishes itself at an individual node port, rather than traversing a path.
- **Possible interaction:** Inspect/close/challenge that entrance while the attempt grows; perhaps respond before it finishes anchoring.
- **Different challenge:** Defend a *place of entry*. Keep distinct from Handshakes: this is a persistent spatial attachment, not merely pre-connection negotiation.

### 3. HANDSHAKES — Activity before a connection exists

- **Presence:** Brief negotiations/attempts appear at a node perimeter before an ordinary connection is established.
- **Possible interaction:** Allow, reject, or challenge them; legitimate attempts should exist too, so rejection is not always free.
- **Different challenge:** Judge traffic at the *establishment* stage, under a short decision window, rather than terminating already-active traffic.

### 4. PIGGYBACKS — Passengers carried by another entity

- **Presence:** A second identifiable body rides an otherwise normal connection; the host might be innocent.
- **Possible interaction:** Kill the entire host at a potential benign cost, or inspect and attempt to detach the passenger. Could it detach/escape when disturbed?
- **Different challenge:** Separate host from parasite. This is a *relationship* between objects, not merely a hostile connection subtype.

### 5. ACCESS LEASES — Persistent permissions and trust

- **Presence:** A credential, session, key, or authorization relationship exists between endpoints/sources, even after associated traffic ends.
- **Possible interaction:** Inspect who has permission, identify suspect leases, revoke/challenge one; active use may complicate timing.
- **Different challenge:** Manage *trust relationships*, not things moving through the graph. Legitimate and hostile leases might look similar.

### 6. ROUTE SWITCHES — Contested network junctions

- **Presence:** A switch/controller appears at a junction and influences the path available to traffic.
- **Possible interaction:** Inspect, restore, lock, or contest the switch rather than killing each affected connection.
- **Different challenge:** Fight over *how things are routed*. Requires new routing semantics; today's drawn transit paths do not imply switch controls exist.

### 7. GHOST BRIDGES — New internal links

- **Presence:** A hostile edge gradually forms directly between two existing nodes.
- **Possible interaction:** Notice the new connectivity, sever the bridge, or protect/contest its endpoints.
- **Different challenge:** The *graph itself changes*. Unlike a Tunnel, this adds an internal relationship rather than necessarily adding an external ingress node.

### 8. SCHEDULED JOBS — Objects that live in time

- **Presence:** Visible recurring or countdown tasks persist on a node, possibly long after the initiating connection disappears.
- **Possible interaction:** Inspect the schedule, delay/cancel/disarm a task, or prioritize an imminent execution.
- **Different challenge:** Intervene in *future scheduled activity*. Must be more than a differently named Spawner or delayed effect.

### 9. QUEUE POISONING — Hostile items waiting for processing

- **Presence:** A visible backlog or queue of pending work accumulates at a node.
- **Possible interaction:** Inspect/filter/reorder/reject queue items, weighing legitimate work against unknown items.
- **Different challenge:** Manage *what is waiting*, not only what has already started doing WORK. Requires a real queue interaction, not just a LOAD spike.

### 10. SWARMS — Coordinated groups of independent units

- **Presence:** Multiple separate contacts share a collective identity/control relationship.
- **Possible interaction:** Identify a controller, break coordination, peel off individual units, or contain the formation.
- **Different challenge:** Reason about a *group acting together*. Distinct from a Worm's ancestry and physical propagation.

### 11. SIEGES — Sustained contests over a node

- **Presence:** A ring, perimeter, or several visible points of pressure surrounds one node over time.
- **Possible interaction:** Engage chosen points, open temporary weaknesses, or defend the site as a continuing contest.
- **Different challenge:** *Contest a location*, potentially selecting order and timing of engagements. Avoid reducing this to three compulsory Command Race Duels.

### 12. CIRCUITS — Hostile geometric formations

- **Presence:** Several anchors on different nodes are connected by a forming structure; a closed loop/formation is the entity.
- **Possible interaction:** Identify the vulnerable anchor/link; isolate or break one component to disrupt the formation, perhaps before it closes.
- **Different challenge:** Solve a *topological/geometric puzzle*. The shape and relationship between anchors matter more than killing all components.

### 13. TERRITORY — Regions of influence

- **Presence:** A boundary or colored region encompasses a connected part of the graph and may expand, shrink, or move.
- **Possible interaction:** Cut the perimeter, split the region, contest boundary nodes, or contain further expansion.
- **Different challenge:** Manage *areas*, not discrete travel bodies. It must play differently from a large Worm outbreak.

### 14. NAME HIJACKING — Contested service identities

- **Presence:** Visible mappings/aliases connect service names (for example, payments/accounts) to actual nodes.
- **Possible interaction:** Inspect resolution, compare intended and observed destinations, restore or contest a mapping.
- **Different challenge:** Distinguish *where a connection believes it is going* from where it actually goes. An information/identity surface, not a new kind of packet.

### Early candidates that seemed especially different

**Link Parasites**, **Access Leases**, **Circuits**, and **Sieges** were especially appealing because they depart from the existing travel-and-intercept pattern. This is a reminder of the brainstorm's energy, **not** a priority order or selection. The other ten candidates are retained in full, not rejected.

## B. Counterplay and multi-step encounter grammar

A motivating **Packet-era thought experiment**: a packet may be unblockable until its incoming edge has been throttled. This is preserved as an inspiration, **not** a statement that Packet implements this exact rule or that Connections should copy its commands.

The interesting principle is **the player can create the conditions in which an enemy becomes interactable**. The steps should be visually signaled, learnable, fast to execute, and meaningfully affected by surrounding traffic.

### Example: ARMORED Connection (unapproved)

1. A visible shield on a connection says an ordinary kill will not work.
2. The player notices its incoming edge and performs a hypothetical throttle/interruption there.
3. The shield visibly breaks and the connection becomes vulnerable for a short window.
4. The player kills it during that window.

This is a *cross-surface* sequence: intervene on the physical link, then on the logical connection. The exact commands, edge slowdown rules, shield timing, benign-traffic cost, and whether kill is rejected while shielded are **unsettled**. The shield and broken-shield states are examples of readable visual grammar, not prescribed art/UI.

### Counterplay patterns worth retaining

| Pattern | Player challenge | Potential surface |
| --- | --- | --- |
| **Expose → destroy** | Create a vulnerability, then act in a short window | ARMORED connection |
| **Locate → intervene** | Find the relevant location/object before acting | Hidden Link Parasite |
| **Contain → remove** | Stop movement/escape/propagation first | Worm, Piggyback, Swarm |
| **Disable controller → eliminate units** | Identify and break a supporting relationship | Swarm, Siege |
| **Break formation** | Choose a critical anchor or edge rather than clear everything | Circuit, Territory |
| **Interrupt → exploit** | Force a behavior/state change, then use the opening | Handshake, Route Switch, ARMORED |
| **Choose → commit** | Multiple countermeasures work but have different costs | Piggyback host versus passenger |

**Warning:** Two rote commands are not intrinsically deeper than one. If every ARMORED connection is always “throttle, kill,” with no surrounding decision, it becomes a tax on typing. Timing, competing benign work, another incoming Payload, ambiguous identity, alternate targets, or opportunity costs should make *when and where* the player takes those steps matter. Not every threat should be multi-step.

### Telegraphing and teaching

- A player should be able to recognize “this is **that** threat, so I need to do **X then Y**” without memorizing undocumented rules.
- Reusable signals might include **shield/broken shield**, **tether**, **segmented ring**, **reversal arrow**, **forming anchor links**, or **countdown**. These are possible visual cues, not UI specifications.
- Inspection may explain a limitation concisely (e.g. “SHIELDED — protected by incoming edge API–DB”), but avoid turning every encounter into a verbose checklist.
- Show honest, stateful feedback when a prerequisite is met or an action is rejected; don't secretly change mechanics or leak hidden hostile intent through generic UI.
- Reuse existing interactions where they create real decisions (kill, isolate, intercept, scan/clean, Probe); new verbs are warranted only if they create a distinct player action.

**Additional interaction verbs to explore, without approving any:** capture, redirect, detach, disarm, sever, commandeer, revoke, challenge. Ask what distinct tactical choice each represents before making a command.

## C. Behavioral anomalies / keywords on existing surfaces

This is intentionally a **separate idea pool**. The user specifically did not want to lose weird behaviors while brainstorming new entities. They need not be new consequences or new genera.

| Candidate keyword | Applies to | Unusual *behavior* | Possible interaction question |
| --- | --- | --- | --- |
| **PASS-THROUGH** | Connection | Meaningfully interacts with **every intermediate node traversed**, not only its final WORK destination | Can the player interrupt its next intermediate interaction? How is each visit signaled? |
| **REVERSING** | Payload | Reverses direction during travel; may revisit a node instead of always advancing toward a hidden end | Can the player predict/force reversal or revise interception timing? |
| **ARMORED** | Connection (possibly others) | Initially resists ordinary termination until another surface is manipulated | Which prerequisite produces a readable temporary opening? |
| **SPLITTING** | Payload | Becomes several independently moving entities | Does the player contain a parent before division or triage children afterward? |
| **TETHERED** | Payload/Connection | Maintains a live relationship to another object or anchor | Does severing the tether change its vulnerability or route? |
| **BURROWING** | Payload | Disappears/pauses inside a node and resumes later | Is there a fair, visible way to track or flush it out? |
| **PHASING** | Connection | Alternates between interactable and non-interactable states | What signals the next exposed window? |
| **FEINTING** | Payload | Presents an apparent final approach, then redirects | What evidence separates a feint from a real final approach without cheating? |
| **MIGRATING** | Connection | Changes its route or endpoint during its lifetime while retaining identity | How does inspection and lane/path continuity represent that change? |

Two specific anchors from the brainstorm deserve emphasis: **connections affecting transit nodes** (a genuine change from target-only LOAD/WORK thinking), and **Payloads that reverse direction** (a genuine change to the currently forward-only visible-hop rhythm). Neither implies a particular infection/Condition/damage effect.

These concepts are distinct from existing LOAD/WORK signatures such as OVERLOAD, SPIKING, WAVING, natural LOAD variation, and identity-hopping behavior. Those established or separately documented mechanisms provide precedent for a **composable behavior vocabulary**, not evidence that the new keyword candidates are implemented. See [Behavior Foundation](connections-behavior-foundation-v0.md), [Purpose & Anomaly Generation](connections-purpose-anomaly-generation-v0.md), and [Generated Identity Behaviors](connections-generated-identity-v0.md).

The older [Packet keyword brainstorm](../Packet/keywords.md) also preserves ideas such as Mutating, Desynced, Invulnerable, Accelerant, Drag Field, Chameleon, Leech, Linked, Carrier, Jammer, Scrambler, Cloak, Volatile, Echo, and Floodgate. **Do not silently import Packet rules into Connections**; revisit them as inspiration when ready.

## D. Composition without locking in effects

Potential combinations, meant to evoke play rather than define legal recipes:

- An **ARMORED** connection passes through a **Link Parasite** edge: the player might need to act on the link before the connection.
- A **REVERSING** Payload creates a second approach window at an unexpected node: interception becomes a prediction problem.
- A **PIGGYBACK** rides an ordinary legitimate connection that also has an **Access Lease**: preserving good work competes with detaching/revoking hostile presence.
- A **Circuit** forms while a **Siege** occupies one anchor node: the player chooses which visible structure to break first.
- A **Swarm** coordinating around a **Route Switch** could ask whether to fight the group or sever its shared route control.

Do **not** generalize this into a universal entity-modifier graph yet. Compatibility, costs, source of truth, and counterplay would need to be established by a future small experiment. The consequence of any successful attack remains separate under the [attack-pathways distinction](connections-attack-pathways-and-surprise.md).

## E. Questions for a later brainstorm (not implementation tickets)

1. **Player verb:** What concrete action is different from inspect/kill/intercept/isolate/clean? Does it require a new command at all?
2. **Visual footprint:** Can a first-time player notice where the object lives (edge, port, node, relationship, queue, region, geometry, identity)?
3. **Recognizable rule:** Can the player learn a simple rule and perceive when its prerequisites are satisfied?
4. **Decision, not rote macro:** Does timing, uncertainty, topology, competing traffic, or alternate counterplay materially change the choice?
5. **Fairness:** Which observations are always truthful, which intentions remain hidden, and which failures should be explained?
6. **Identity and lifecycle:** How does the thing spawn, persist, leave, change ownership, or outlive its source? Is it truly a new surface or an existing effect/Condition in disguise?
7. **Composition:** Which existing surfaces and keywords make it newly interesting without requiring an unbounded combinations system?
8. **Distinctness:** In human play, would this feel like a new problem or merely “another Payload/Worm/Spawner” with different art?

**Disposition:** Preserve all fourteen surface candidates, the starting Tunnel idea, the counterplay patterns, and all nine keyword candidates as an open pool. No rankings, acceptance criteria, implementation sequence, new runtime abstractions, art contract, or gameplay behavior are authorized by this note.
