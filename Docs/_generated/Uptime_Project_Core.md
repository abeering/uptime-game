# Uptime Project Core

> **GENERATED FILE — DO NOT EDIT DIRECTLY.** Run `python scripts/build_uptime_project_core.py` after changing an included source.
>
> **Source digest:** `sha256:3f9585141443687103f971be42210e43167000784ee922247fd01a9027352425`. This hashes the ordered manifest paths and their LF-normalized UTF-8 contents. It does not depend on the clock or Git HEAD.
>
> **Snapshot:** This file is a cold-start Project Source, not a live GitHub feed. Repository links below target current `main`, which may be newer than this snapshot.

## How to use this Core

- Uptime is **one game** explored through two gameplay prototypes. Packet is parked but retained; Connections is the active experiment. Neither is declared final. Shared Uptime design includes systems neither prototype has implemented yet.
- Shared design describes the game being made; the Packet and Connections current-state pages summarize their implementations. Runtime code and serialized Unity scenes decide executable behavior. Working hypotheses are not installed features, and prototype-specific mechanics do not transfer automatically.
- This Core contains five entry points, not the full documentation. `Docs/Design/` retains shared design; `Docs/Packet/` and `Docs/Connections/` retain detailed mechanics; `Docs/research/` and `Docs/historical/` preserve unresolved alternatives and prior models. PR discussions and `Docs/Review/` preserve provenance. Follow the linked sources when a topic needs detail.
- For current implementation or recent PRs, inspect the working repository or GitHub rather than relying on this snapshot. Maintain individual sources directly and regenerate this file; never hand-edit generated content. After merging a change to an included source, refresh the uploaded ChatGPT Project Source separately.

## Included source files

1. [`Docs/README.md`](https://github.com/abeering/uptime-game/blob/main/Docs/README.md)
2. [`Docs/Uptime_Current_State.md`](https://github.com/abeering/uptime-game/blob/main/Docs/Uptime_Current_State.md)
3. [`Docs/Design/Uptime_Design.md`](https://github.com/abeering/uptime-game/blob/main/Docs/Design/Uptime_Design.md)
4. [`Docs/Packet/Current_State.md`](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/Current_State.md)
5. [`Docs/Connections/Current_State.md`](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/Current_State.md)

---

<!-- BEGIN SOURCE: Docs/README.md -->
## Source 1: `Docs/README.md`

Canonical file: [`Docs/README.md`](https://github.com/abeering/uptime-game/blob/main/Docs/README.md).

# Uptime documentation

Uptime is one game design explored through two independent gameplay prototypes: Packet and Connections. Both remain playable and neither is declared the final implementation.

## Start here

1. [Uptime Current State](https://github.com/abeering/uptime-game/blob/main/Docs/Uptime_Current_State.md) — project posture, implementation boundaries, validation, and source authority at the documented baseline.
2. [Uptime Design](https://github.com/abeering/uptime-game/blob/main/Docs/Design/Uptime_Design.md) — living overview of the whole game and links to the full design documents.
3. [Packet Current State](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/Current_State.md) or [Connections Current State](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/Current_State.md) — what the relevant prototype currently implements and where to inspect its executable truth.
4. The relevant code and serialized Unity scene on the working branch when exact behavior matters.

## Where sources live

- [Design](https://github.com/abeering/uptime-game/blob/main/Docs/Design/Uptime_Design.md) holds shared Uptime game direction, including company identity, runs, scoring, upgrades, threats, cleaning, notifications, events, and topology. Packet terminology in a source does not by itself make its larger design intent Packet-only. Documents may contain both active direction and unsettled mechanics.
- [Packet](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/Current_State.md) holds Packet-specific scanning, keywords, traversal, command examples, and interface ideas. Packet is parked and retained.
- [Connections](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/Current_State.md) holds the integrated [LOAD](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-arcade-load-v1.md), [operation arcs](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-arcade-operation-arcs-v1.md), [traffic](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-arcade-traffic-v0.md), and [recorder](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-run-recorder-v0.md) contracts, plus the [recorder example](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/examples/connections-run-recorder-example.json). Their status notes distinguish original phase deferrals from later merged work.
- [Research](https://github.com/abeering/uptime-game/blob/main/Docs/research/README.md) preserves unresolved examples and implementation proposals that are not current authority. [Historical](https://github.com/abeering/uptime-game/blob/main/Docs/historical/README.md) preserves superseded contracts, completed checkpoints, and unverified old observations. The [Connections V0 brief](https://github.com/abeering/uptime-game/blob/main/Docs/historical/connections-v0.md) and [network and connection grammar](https://github.com/abeering/uptime-game/blob/main/Docs/historical/network-connection-grammar-v1.md) remain complete; the grammar's older CPU/disk/memory/link allocation is not the current LOAD model.
- `Review/` retains four Connections inspect UI Editor-preview folders with their READMEs and images. These are presentation evidence, not interactive Play Mode validation.

Phase 2 moved source documents without condensing their design material. Follow the links above or the prototype current-state pages; a directory, versioned title, or old "next" list alone does not establish current authority.

## Source authority

For **implemented behavior**, inspect current branch runtime code and serialized scene/prefab settings first. Current-state documents summarize that evidence; focused prototype contracts explain intended behavior but must be corrected if execution differs. For **the game we are designing**, use [Uptime Design](https://github.com/abeering/uptime-game/blob/main/Docs/Design/Uptime_Design.md) and its linked full source documents, distinguishing established intentions and active working designs from unresolved details. Shared intent does not assert that either prototype implements it.

Research, old phase briefs, review captures, PR descriptions, and Git history remain evidence and provenance. They do not override current execution or silently authorize new mechanics. When a claim changes, update the appropriate individual source and its current-state pointer.

## ChatGPT Project Core

The [generated Uptime Project Core](https://github.com/abeering/uptime-game/blob/main/Docs/_generated/Uptime_Project_Core.md) is a cold-start snapshot containing exactly this map, Uptime Current State, Uptime Design, Packet Current State, and Connections Current State, in that order. It excludes the full design archive, prototype contracts, research, historical sources, review images, and recorder JSON. Its SHA-256 source digest covers the ordered paths and LF-normalized source contents; freshness does not depend on the clock or Git HEAD. Links inside the aggregate target GitHub `main` and may lead to newer material than the snapshot.

From the repository root, run `python scripts/build_uptime_project_core.py` to regenerate, `python scripts/build_uptime_project_core.py --check` to verify without writing, or `python scripts/build_uptime_project_core.py --list` to see the manifest. The script also works when invoked from another directory; `--repo-root <path>` can select a repository copy explicitly.

When any of those five source documents changes, regenerate the Core and commit the source and generated file together. Changes to deeper documents require regeneration only if an included source is also updated. Edit the individual sources, never the aggregate. After merging a change to a Core source, replace or refresh the uploaded ChatGPT Project Source manually; GitHub and the generator do not synchronize that upload. For current implementation or recent PRs, inspect the working repository or GitHub rather than assuming the uploaded snapshot is live.

<!-- END SOURCE: Docs/README.md -->

<!-- BEGIN SOURCE: Docs/Uptime_Current_State.md -->
## Source 2: `Docs/Uptime_Current_State.md`

Canonical file: [`Docs/Uptime_Current_State.md`](https://github.com/abeering/uptime-game/blob/main/Docs/Uptime_Current_State.md).

# Uptime — Current State

**Documented baseline:** `main` at `225d524d34905b700b128faafeaad3584333c2bf`, October 22, 2026. This is a dated repository snapshot, not a live branch-status feed. Check GitHub and the working branch before using it for a later implementation decision.

Uptime is one game being explored through two different gameplay prototypes. **Packet** models individual packets and discrete network traversal. **Connections** models persistent logical connections, operations, WORK, and shared LOAD. Both are retained. Connections is the current development focus and is promising, but no decision has been made to replace or abandon Packet. The broader game design is shared and can evolve as either prototype teaches us more.

## Playable boundaries

| Prototype | Scene and runtime | Current posture |
|---|---|---|
| Packet | `Assets/Scenes/GameScene.unity` and `Assets/Scenes/ABScene.unity`; principally `Assets/Scripts/` outside `Connections/` | Parked, not deprecated. Tick/packet traversal, scan/trace, blocking, infections, cleaning, scoring, and notifications have implementation code. See [Packet Current State](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/Current_State.md) for the evidence and limits of that summary. |
| Connections | `Assets/Scenes/Connections.unity`; `Assets/Scripts/Connections/` and `Assets/Prefabs/Connections/` | Active experiment. The scene defaults to generated benign traffic on its four-node, three-link network; Authored Witness mode remains available. See [Connections Current State](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/Current_State.md). |

The scenes and simulation paths are separate. `Assets/Scripts/TrafficDirector.cs` drives Packet; `Assets/Scripts/Connections/ConnectionTrafficDirector.cs` drives Connections. Packet's `ConnectionView.cs` names a physical edge, not a Connections logical connection. Sharing a game vision does not make their current mechanical rules interchangeable.

The project's `ProjectSettings/EditorBuildSettings.asset` lists `SampleScene`, not either playable prototype scene. A standalone player needs a game-specific build scene list or profile. The documented Editor version is Unity 6000.3.11f1.

## Integrated Connections milestone and validation

The current Connections stack was merged into `main` through merge commits: [#3 operation identity and abstract WORK](https://github.com/abeering/uptime-game/pull/3), [#4 target-node LOAD](https://github.com/abeering/uptime-game/pull/4), [#5 operation arcs](https://github.com/abeering/uptime-game/pull/5), [#8 inspect UI](https://github.com/abeering/uptime-game/pull/8), [#6 generated traffic](https://github.com/abeering/uptime-game/pull/6), and [#7 run recorder](https://github.com/abeering/uptime-game/pull/7). [PR #1](https://github.com/abeering/uptime-game/pull/1) established the original separate experiment and working instructions. The final integrated `main` file tree matched the recorder branch at the milestone.

Unity 6000.3.11f1 batch Editor compilation and Resource, Geometry, Traffic, and Recorder Witnesses passed against that integrated `main`. The checks cover deterministic model cases and scene/prefab bindings exercised by the witnesses. They are not a substitute for interactive scene review. A full Connections Play Mode smoke test and Packet Play Mode test were **not** run as part of the integration. In particular, the integrated check did not establish 16:9 readability, command focus under play, or the interactive folder-opening behavior.

## Game design and implementation authority

| Question | Start with | Then inspect |
|---|---|---|
| What game are we trying to build? | [Uptime Design](https://github.com/abeering/uptime-game/blob/main/Docs/Design/Uptime_Design.md) | Its linked full design documents, including active working proposals and alternatives. |
| What does Packet now do? | [Packet Current State](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/Current_State.md) | Packet runtime and serialized `GameScene`/`ABScene` on the working branch. |
| What does Connections now do? | [Connections Current State](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/Current_State.md) | Connections runtime, prefabs, and serialized `Connections.unity` on the working branch. |
| Why was an approach chosen or changed? | The relevant current contract and linked PR | Original briefs, review assets, Git history, and research as evidence, with their original baseline in mind. |

Current executable and serialized state wins a disagreement about **what is implemented**; correct stale current prose. Shared game-design intent may describe a future feature that neither prototype implements. An old proposal or checkpoint does not become a current rule by being in `Docs/Design/`. The [generated Project Core](https://github.com/abeering/uptime-game/blob/main/Docs/_generated/Uptime_Project_Core.md) is a cold-start navigation snapshot, not a source of independent authority or a live account of later changes.

## Open work, without implied authorization

Company contracts, hacker adversaries, multi-day runs, upgrades, scoring for Connections, network growth, infrastructure events, infections, cleaning, and narrative notification systems remain important parts of the broader Uptime direction. Their exact mechanics and integration into Connections are unresolved. Packet already implements portions of some of these themes using packet-specific rules. The next task should name its shared-design or prototype scope and inspect the relevant current state before changing behavior.

<!-- END SOURCE: Docs/Uptime_Current_State.md -->

<!-- BEGIN SOURCE: Docs/Design/Uptime_Design.md -->
## Source 3: `Docs/Design/Uptime_Design.md`

Canonical file: [`Docs/Design/Uptime_Design.md`](https://github.com/abeering/uptime-game/blob/main/Docs/Design/Uptime_Design.md).

# Uptime — Living Game Design

**Status:** Shared game-design overview, October 22, 2026. This document states the direction of **one Uptime game** while Packet and Connections test different representations of network activity. It is a navigation and synthesis layer, not a replacement for the full documents linked below. Those sources retain their original examples, alternatives, questions, and wording. No design statement here implies that either prototype already implements it; use [Uptime Current State](https://github.com/abeering/uptime-game/blob/main/Docs/Uptime_Current_State.md) for implementation posture.

## The game we are making

The player is an overworked network operator facing escalating pressure while making fast, typed decisions with incomplete information. Their job is to keep a company's systems useful under stress, infer what matters, choose imperfect interventions, and live with the consequences. Speed of command entry matters because attention and timing matter, not because the interface should add arbitrary friction. The network should visibly respond to real activity and interventions, so the player can learn its behavior and form better judgments.

This is the shared Uptime fantasy. Packet tests it through individual packets, discrete traversal, scanning, classification, and interception. Connections tests it through persistent logical connections, operations, WORK, LOAD, inspection, and blocking. These are different mechanical lenses on the same game. Neither automatically inherits the other's timing, resource model, command delays, threat labels, or progression rules. Connections is the active experiment; Packet remains a viable one.

The broadest earlier working statement is preserved in [game.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/game.md). Its emotional loop, system-shaping ambitions, and network philosophy continue to inform Uptime. Its particular run lengths, upgrade counts, topology rules, failure thresholds, and other numeric or structural proposals remain subject to play and design decisions.

## Established shared intentions

- **Pressure and judgment:** The central skill is allocating attention, acting with imperfect knowledge, and accepting tradeoffs. The player should understand why the network changed, even when they did not know everything in advance. See [game.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/game.md) and the three pressure surfaces in [gamesurfaces.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/gamesurfaces.md).
- **A company worth protecting:** Company-specific operations and priorities should change what matters in a run. A hacker adversary supplies hostile pressure; ordinary load, infrastructure trouble, and mistaken interventions can be consequential too. The company and adversary direction is shared even though exact catalogs, attack schedules, and failure rules are open. See [company-designs.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/company-designs.md), [level-and-run-structure.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/level-and-run-structure.md), and [events.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/events.md).
- **Fast, keyboard-first operation:** Commands are the player's working tools. Information and consequences should be legible enough for quick decisions. Packet's queued scan/trace and Connections' immediate inspect are prototype choices, not one shared command contract. See [game.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/game.md), the Packet [scan/trace note](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/scan-vs-trace.md), and the [original Connections brief](https://github.com/abeering/uptime-game/blob/main/Docs/historical/connections-v0.md).
- **A system that pushes back:** Traffic, infrastructure, and information can all create pressure. Infections, failures, clean interventions, and confusing messages may demand attention rather than simply increasing traffic volume. These are Uptime design concerns even where existing examples use packet vocabulary. See [gamesurfaces.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/gamesurfaces.md), [infections.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/infections.md), [clean-minigames.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/clean-minigames.md), and [notifications.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/notifications.md).
- **Strategic run identity:** Network growth, node specialization, upgrades, and different company needs should make runs feel meaningfully different. Choices should change behavior and create tradeoffs, not merely improve every number. See [upgrade-strategies.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/upgrade-strategies.md), [company-designs.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/company-designs.md), and the topology documents below.
- **Evaluation with meaning:** Scoring should distinguish judgment and performance from merely surviving. What is measured, rewarded, and presented remains unsettled. Packet's current score ledger is one implementation, not a universal scoring specification; Connections' run recorder captures evidence but is not a score system. See [scoring.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/scoring.md).
- **Human consequences and fiction:** Messages, characters, and events should connect simulated state to the people affected by it. They may inform, misinterpret, pressure, or occasionally mislead the operator. The detailed cue architecture and content are still proposals. See [notifications.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/notifications.md) and [events.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/events.md).

These are game-design intentions, not an assertion that every listed system is built. Future work can revise them when human play and prototype evidence warrant it.

## Active working designs and unresolved details

**Run framing.** Overarching run and progression design belongs to Uptime as a whole. “Run = job” and “level = day/shift” is the working shared frame: the operator works for a company through a series of increasingly difficult shifts; survival and performance may be judged separately. Exact number and length of days, failure thresholds, advancement, difficulty selection, and reward schedule remain flexible. Prototype-specific progression mechanics do not transfer automatically between Packet and Connections. [level-and-run-structure.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/level-and-run-structure.md) contains the full proposal, including Packet-shaped examples; [levels-phase1.md](https://github.com/abeering/uptime-game/blob/main/Docs/historical/levels-phase1.md) and the [illustrative first prototype level](https://github.com/abeering/uptime-game/blob/main/Docs/research/first-prototype-level.md) preserve other level-thinking and implementation hypotheses.

**Company and adversary.** Different businesses can create different traffic, tolerance, topology, tone, and operational stakes. Hacker adversaries can bring evolving tactics and coordinated incidents. The ten [company sketches](https://github.com/abeering/uptime-game/blob/main/Docs/Design/company-designs.md), specific villain pacing, catalog sizes, and a one-to-one company/attacker structure are design material to test, not locked content or required data models. The [original Connections V0 appendix](https://github.com/abeering/uptime-game/blob/main/Docs/historical/connections-v0.md) also explores company-authored connection purposes and event orchestration without making them current implementation.

**Infrastructure and topology.** Growth and specialization are strategic goals. The [topology overview](https://github.com/abeering/uptime-game/blob/main/Docs/Design/toplogy-system-v1.md), [node-role study](https://github.com/abeering/uptime-game/blob/main/Docs/Design/topology-nodes.md), [constrained grammar](https://github.com/abeering/uptime-game/blob/main/Docs/Design/topology-grammar.md), and [runtime proposal](https://github.com/abeering/uptime-game/blob/main/Docs/research/topology-runtime-spec-v1.md) retain complementary detail about roles, generation, layouts, and mutations. They currently describe proposals, including Packet-oriented mechanics and an unbuilt `TopologyDirector`; neither prototype's authored scene is evidence that the generator exists. Do not collapse those perspectives or silently adopt their exact graph grammar for Connections.

**Threats and interventions.** Infections, clean minigames, disruptive events, and attention-demanding interventions are relevant to Uptime generally. [infections.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/infections.md) surveys threat effects; [clean-minigames.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/clean-minigames.md) contains multiple detailed minigame drafts; [clean.md](https://github.com/abeering/uptime-game/blob/main/Docs/research/clean.md) retains a separate small observation; [gamesurfaces.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/gamesurfaces.md) explores infrastructure and information disruptions. Packet implements portions of this space, while Connections V1 currently generates benign traffic. Translating these concepts to Connections requires an explicit design and implementation decision, not a word substitution.

**Information and scoring.** [scoring.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/scoring.md) proposes event/context evaluation and a score ledger; actual scoring semantics for a full Uptime run are open. Packet's [scan difficulty](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/scandifficulty.md), [scan versus trace](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/scan-vs-trace.md), [packet keywords](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/keywords.md), and [scan timing reference](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/edge-traversal.md) explore one representation's information game. Their larger attention and uncertainty questions may transfer, but their packet stages, tick timing, and keyword mechanics do not automatically transfer.

**Narrative presentation.** [notifications.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/notifications.md) preserves an extensive communication design, including speakers, escalation, disagreement, and silence. [events.md](https://github.com/abeering/uptime-game/blob/main/Docs/Design/events.md) provides older tick-based examples. Whether messages arrive from an authored run plan, reactive state, or both remains a design choice. They should have causal relationships to the playable system rather than imply simulated incidents that never occurred.

## Prototype-specific authority

Read [Packet Current State](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/Current_State.md) or [Connections Current State](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/Current_State.md) for the implemented representation. The current Connections [LOAD](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-arcade-load-v1.md), [operation arcs](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-arcade-operation-arcs-v1.md), [traffic](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-arcade-traffic-v0.md), and [recorder](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-run-recorder-v0.md) documents explain its milestone, subject to runtime and serialized scene truth. The older [network and connection grammar](https://github.com/abeering/uptime-game/blob/main/Docs/historical/network-connection-grammar-v1.md) preserves conceptual language and a superseded resource model; it must not be used as the current allocator contract.

Packet-focused notes and examples, including [keyword ideas](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/kind-keyword-term-dump.md), [debug spawn commands](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/spawn.md), and [UI ideation](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/gpt-prompts.md), remain complete in the Packet folder. [Old TODOs](https://github.com/abeering/uptime-game/blob/main/Docs/historical/todo.md) and [bug observations](https://github.com/abeering/uptime-game/blob/main/Docs/historical/bugs.md) remain complete as historical records. Their presence records design work, not an instruction to build every item. The [research index](https://github.com/abeering/uptime-game/blob/main/Docs/research/README.md) and [historical index](https://github.com/abeering/uptime-game/blob/main/Docs/historical/README.md) explain the status of other relocated sources.

<!-- END SOURCE: Docs/Design/Uptime_Design.md -->

<!-- BEGIN SOURCE: Docs/Packet/Current_State.md -->
## Source 4: `Docs/Packet/Current_State.md`

Canonical file: [`Docs/Packet/Current_State.md`](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/Current_State.md).

# Packet — Current State

**Baseline:** Uptime `main` at `225d524d34905b700b128faafeaad3584333c2bf`, October 22, 2026. Packet is a parked, retained Uptime gameplay prototype, not an abandoned or deprecated game. Its scenes are `Assets/Scenes/GameScene.unity` and `Assets/Scenes/ABScene.unity`. This document is an orientation to the inspected code, not a claim of fresh Packet Play Mode validation.

## Representation and implementation boundaries

Packet represents traffic as individual packets moving along physical network connections in discrete ticks. `Assets/Scripts/NetworkRuntime.cs`, `TrafficDirector.cs`, `PacketView.cs`, and `ConnectionView.cs` are Packet-side runtime paths. In this codebase, Packet's `ConnectionView` is a **physical edge**; it is not the persistent logical connection modeled by the separate Connections prototype. Packet's `TrafficDirector.cs` must not be mistaken for `Assets/Scripts/Connections/ConnectionTrafficDirector.cs`.

The inspected Packet code includes:

- `CommandParser`, `CommandDirector`, and `ScanDirector` for typed commands and packet scan/trace interaction; `CommandDirector` also has block, clean, boost, throttle, spawn, and autospawn command paths.
- `LevelDirector` for tick events and traffic-loss/core-node failure state, plus traffic and infection classes. This is not evidence that the broader multi-day job/run proposal or a topology generator is implemented.
- `CleanDirector` for cleaning sessions. Its current dispatch uses Process Killer for supported infection types; the many alternative minigames in the design drafts are proposals, not a list of installed modes.
- `ScoreDirector` with packet event values and a score ledger, and `NotificationDirector` with an on-screen message feed. Their existence does not establish the full shared scoring or character/cue architecture proposed in design documents.

The exact effects, timing, serialized references, and tuning of those systems should be read from the working branch's code and `GameScene`/`ABScene` Inspector state. Packet Play Mode was **not** run during the Connections V1 integration or this documentation phase. Do not infer current polish, balance, or bug resolution from old notes.

## Design sources and status

Packet implements one representation of the wider [Uptime design](https://github.com/abeering/uptime-game/blob/main/Docs/Design/Uptime_Design.md). The broader company, adversary, run, upgrade, scoring, network-growth, infection, and notification goals are not Packet-only simply because many sources were written with packet examples.

For Packet-specific ideas and evidence, retain the complete [packet keyword document](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/keywords.md), [scan difficulty proposal](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/scandifficulty.md), [scan/trace note](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/scan-vs-trace.md), [scan timing reference](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/edge-traversal.md), [infection brainstorm](https://github.com/abeering/uptime-game/blob/main/Docs/Design/infections.md), [clean minigame drafts](https://github.com/abeering/uptime-game/blob/main/Docs/Design/clean-minigames.md), [level-phase checkpoint](https://github.com/abeering/uptime-game/blob/main/Docs/historical/levels-phase1.md), [debug spawn examples](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/spawn.md), and [UI ideation](https://github.com/abeering/uptime-game/blob/main/Docs/Packet/gpt-prompts.md). These sources mix implemented observations, proposals, and old instructions. In particular, an item labeled “DONE” or “next” in an old document should be checked against current code and scene before being treated as present work.

The [older TODO](https://github.com/abeering/uptime-game/blob/main/Docs/historical/todo.md) and [bug observations](https://github.com/abeering/uptime-game/blob/main/Docs/historical/bugs.md) remain preserved but untriaged in historical sources. Do not treat them as a verified current task queue or dismiss them without reproduction.

## Validation and future work

The integrated Connections branch was compiled and witnessed in Unity 6000.3.11f1; those witnesses do not validate Packet gameplay. Before asserting exact Packet behavior or making Packet changes, open `GameScene.unity` and `ABScene.unity`, inspect serialized references, run the relevant scene in Play Mode, and check the command and event paths needed for the task. Keep Packet work separate from Connections runtime changes unless a shared component is deliberately chosen.

<!-- END SOURCE: Docs/Packet/Current_State.md -->

<!-- BEGIN SOURCE: Docs/Connections/Current_State.md -->
## Source 5: `Docs/Connections/Current_State.md`

Canonical file: [`Docs/Connections/Current_State.md`](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/Current_State.md).

# Connections — Current State

**Baseline:** Uptime `main` at `225d524d34905b700b128faafeaad3584333c2bf`, October 22, 2026. Connections is the active Uptime gameplay experiment. It is not a migration of Packet or a declaration that Packet has been replaced. For exact behavior, inspect `Assets/Scenes/Connections.unity`, its serialized references, `Assets/Scripts/Connections/`, and `Assets/Prefabs/Connections/` on the working branch.

## Playable slice and simulation

The scene has an authored four-node, three-physical-link topology: EXT1–API, API–DB, and API–AUTH. A **logical connection** has a stable short ID and a route through that graph. Physical links and logical paths have separate view responsibilities. The scene's serialized `runMode` is **Generated**; Authored Witness mode retains c1/c2/c3 for focused review and tests.

`SimulationHost` owns one fixed-step simulation clock, active connection state, inspection history, and resolution. `ConnectionTrafficDirector` uses its own seeded random stream to queue concrete spawn plans and register arrivals through the host; queued plans can also be supplied explicitly. The default generated traffic is benign. Profiles and arrival cadence are playtest tuning, not balanced game rules. Completed or killed generated connections leave active ownership but remain inspectable for that run. See the full [traffic document](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-arcade-traffic-v0.md).

The current allocator grants **target-node LOAD** proportionally when requests exceed capacity after baseline LOAD. Transit nodes and physical links are route geometry in this model; they are not CPU/disk/memory/link-bandwidth allocation constraints. One allocated LOAD advances one current-operation WORK unit per second. An operation may be READ, WRITE, or EXECUTE and have its own WORK and desired LOAD. Authored multi-operation arcs advance to the next operation on the following fixed step, retaining the connection ID, route, target, and path; only final completion resolves the connection. See [LOAD](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-arcade-load-v1.md) and [operation arcs](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-arcade-operation-arcs-v1.md). The current arcs do not change target or route between operations.

Presentation consumes simulation state. Node LOAD bars show requested LOAD / capacity with capped fill and threshold colors; they do not separately show allocated LOAD, though the simulation tracks requested and allocated values independently. Overloaded color is not a threat label. Path pulses express WRITE toward the target, READ toward origin, or EXECUTE locally, while speed/satisfaction treatments reflect allocation. The terminal has structured inspect output with an operation timeline and retains keyboard input. `inspect`/`i` default to [compact cards](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-inspect-v2.md) with ID, purpose, timeline, and current effective requested LOAD badge; `-v`/`--verbose` adds allocation, WORK, performance, activity, and ETA. Any number of IDs can be inspected in one command, including retained completed or killed history. Inspect cards have a small content inset; their connection ID, operation, and LOAD pill borders use a rounded-rectangle antialiasing material derived from the line-rendering coverage approach. `kill <id>` resolves an active connection and releases its LOAD; `demandtest` queues a deliberate high-demand mechanical witness. Development-only `spawn <profileId>` queues benign and manual-only profiles, including `basic-threat`, `delayed-threat`, `spiking-threat`, `spiking-benign`, and `overload-demo`, independently of ambient generation. Inspection and command entry are immediate; consequences progress in simulation time. [Inspect V2 Editor captures](https://github.com/abeering/uptime-game/blob/main/Docs/Review/ConnectionsInspectV2/README.md) show the authored prefab at 16:9, but they are not Play Mode screenshots.

[Threats V0 Slice 1](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-threats-v0.md) adds one manual-only hostile profile: a 12-LOAD WRITE with 240 fixed WORK against DB. Its operation-completion effect emits one infection-delivery occurrence with connection ID, operation index, target node, effect type, and simulation time. The terminal reports delivery and the recorder observes it. `kill` before completion prevents delivery; a kill after delivery cannot erase the occurrence. Hostile intent is internal and is not inferred from demand or revealed by ordinary inspect. There is no persistent infected node, cleaning, Level Director, or source blocking.

Threats V0 Slice 2 adds manual-only `delayed-threat`: an ordinary 2-LOAD, 16-WORK READ followed on the next simulation step by `basic-threat`'s 12-LOAD, 240-WORK WRITE. Only normal completion of the WRITE emits its one delivery occurrence, at operation index 1. The arc keeps its ID, route, path, and target; inspection and pulse direction follow the current operation. Killing during either operation prevents pending delivery. Both threat profiles remain outside ambient generation.

[Slice 3A's behavior foundation and Slice 3B's LOAD keywords](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-behavior-foundation-v0.md) treat the existing logical definition as a **Connection Plan**, distinct from a scheduled `ConnectionSpawnPlan`. A connection-owned runtime executes optional connection-scope and operation-scope behaviors before allocation each fixed step. Active modifiers add to base WORK and requested LOAD; inspection, presentation, allocation, and recorder snapshots read the resulting effective values. Modifier changes append attributable per-connection history and recorder events. OVERLOAD adds one sustained LOAD delta after a delay; SPIKING alternates normal and elevated demand with independent timers per connection. Manual-only `spiking-threat`, `spiking-benign`, and `overload-demo` profiles exercise those behaviors through `spawn <profileId>`; existing profiles and ambient weights are unchanged. Connection-scope WORK adjustment, dynamic plan edits, and player-facing modifier logs remain deferred.

Logical paths now use [route-aware lane ordering](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/route-aware-lanes-v1.md) across occupied physical links. The presentation solver favors continuous paths, grouped onward routes, geometric branching, and stable prior positions. It retains the current per-link width and pill packing rules; the path view animates changed offsets.

Generated connections use [randomized lowercase letter-digit IDs and six rotating identity accents](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/identity-presentation-v1.md). The ID pool is deterministic per traffic seed and separate from traffic random draws; explicit authored IDs remain valid. Active at-speed paths and identity pill outlines carry the assigned accent, while slowed and stalled paths retain warning colors. Active Inspect cards share the accent and completed history stays muted.

Active paths now show [one identity-colored terminal marker](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/terminal-markers-v1.md) where their final lane meets the target-node border. Transit nodes have no marker. Its size follows the beam width and arrival angle, with prefab padding; it tracks lane reflow, appears after establishment reaches the target, and fades with path departure. This is presentation of the existing target-node LOAD model.

`ConnectionsRunRecorder` automatically starts a new JSON recording on Play start, saves the same file every 30 seconds of real elapsed time, and saves on Play stop. `mark <note>` records a bookmark with an immediate snapshot and saves; `log save`, `log dir`, and Editor-only `log folder` expose the file. Editor logs go to `<project-root>/Logs/UptimeRuns/`, which Git ignores. This is observational run evidence, **not** a game save, score ledger, replay engine, or source of random traffic. See the [recorder document](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-run-recorder-v0.md) and its [example JSON](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/examples/connections-run-recorder-example.json).

[Network Health V0](https://github.com/abeering/uptime-game/blob/main/Docs/Connections/connections-network-health-v0.md) is a Connections-only, live service instrument. A fixed deadline per connection comes from the authored operation plan at activation. The simulation applies substantial LOAD-shortfall or escalating overdue drain to legitimate active service, passive recovery only when no degradation is present, and one immediate penalty for killing a legitimate connection. Slowed and overdue observations include all active connections, while hostile traffic causes no direct Health penalty. Zero latches failure without ending the run. Lifecycle service facts remain with each connection for future evaluation. A compact clock/Health row with signed rate sits above the terminal; TAB expands the same instrument to show aggregate conditions, peak requested/capacity node LOAD, and recent events while keeping the terminal and topology visible. Terminal history stays bottom-anchored above the persistent prompt on resize. The display-only 24-hour clock has configurable shift bounds and simulation-time mapping; shift progression and Performance scoring are not implemented. The model and values are provisional.

## Authority and provenance

The four current phase documents linked above retain detailed contracts and witness cases. Their older “future/deferred” lists were written at different points; where a later phase has merged, this current-state document and the final runtime/scene take precedence. In particular, the original [Connections V0 brief](https://github.com/abeering/uptime-game/blob/main/Docs/historical/connections-v0.md) is historical phase context. [Network and Connection Grammar V1](https://github.com/abeering/uptime-game/blob/main/Docs/historical/network-connection-grammar-v1.md) preserves useful conceptual distinctions but maps to an earlier CPU/disk/memory/link resource model and one-stage baseline. Those implementation claims were superseded by the LOAD, arc, traffic, and recorder merges. Do not use it as the current allocator or scene contract.

The integration history is [PR #3](https://github.com/abeering/uptime-game/pull/3) (operation identity and abstract WORK), [#4](https://github.com/abeering/uptime-game/pull/4) (LOAD), [#5](https://github.com/abeering/uptime-game/pull/5) (arcs), [#8](https://github.com/abeering/uptime-game/pull/8) (inspect), [#6](https://github.com/abeering/uptime-game/pull/6) (traffic), and [#7](https://github.com/abeering/uptime-game/pull/7) (recorder). Their descriptions and review assets explain decisions and limits; they do not override the integrated source.

## Validation and boundaries

Unity 6000.3.11f1 compiled integrated `main` and Resource, Geometry, Traffic, and Recorder Witnesses passed at the earlier baseline. These cover deterministic LOAD contention and release, stage transitions, generated plan behavior, bindings, geometry cases, and recorder output exercised by the witnesses. Slice 2 extended Traffic and Recorder Witnesses for the delayed arc; Slice 3A added the Behavior Witness. On the Slice 3B branch, Unity compiled an isolated project copy and Behavior, Resource, Traffic, Recorder, and Geometry Witnesses passed. The expanded Behavior Witness covers OVERLOAD and SPIKING boundaries, repeated modifier history, scope transitions, zero allocation, replay, and benign versus hostile completion effects; its final rerun also covers fractional fixed-step boundaries. On the Inspect V2 branch and follow-up, isolated Unity projects compiled; Inspect V2, Resource, and Geometry Witnesses passed, including five-ID inspection and pill material bindings. Five 16:9 Editor UI previews were rendered and refreshed after the padding and pill changes. Interactive Play Mode review remains outstanding: inspect focus and scrolling under live traffic, arrivals and congestion rhythm, path/label crowding, operation transitions, `kill`, `spawn`, automatic file updates, `mark`, `log save`, `log folder`, and final save on exit.

Connections now has three manual-only internal hostile profiles and an infection-delivery occurrence, but does not implement persistent infections, cleaning, company contracts, hacker campaigns, full run progression, scoring, upgrades, or network growth. Those remain relevant **Uptime-wide design directions** in [Uptime Design](https://github.com/abeering/uptime-game/blob/main/Docs/Design/Uptime_Design.md); their precise Connections mechanics are not yet established. Nor should the Packet simulation's tick, scan, or score rules be imported implicitly.

<!-- END SOURCE: Docs/Connections/Current_State.md -->
