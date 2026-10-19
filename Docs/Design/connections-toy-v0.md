# Uptime — Connections Toy V0

**Status:** Proposed experimental design brief — not a commitment to rebuild Uptime  
**Implementation home:** `abeering/uptime-game`; original packet game remains in the same repository.  
**Purpose:** Test whether a handful of persistent, inspectable connections competing for real network resources produces enjoyable real-time command play.

## Thesis

The network is a finite machine. Connections have objectives and demands; nodes and links have capacities. A connection's performance and progress emerge from the resources actually available to it. The player can read the network, inspect an individual connection, and eventually issue small, fast commands that change the outcome.

**First proof:** Without villains, infections, run structure, or scripted incidents, can two or three ordinary connections create an understandable, changing situation worth paying attention to?

## 1. Scene boundary and topology

Build an **isolated new Unity scene** (`ConnectionsToy.unity`, name provisional), retaining the original scene and simulation intact. Start a new scene shell with a **roughly one-third-width terminal and two-thirds-width network view**. Reuse truly distinct assets/systems (e.g., keyboard input patterns, notifications, audio) selectively, but do not force the legacy packet engine or old UI panels to host the toy.

Topology (four nodes, three physical links):

```text
        EXT1
          |
          | E1
          v
         API
        /   \
      E2     E3
      /       \
     DB       AUTH
```

Physical links are capacity-limited shared resources. Logical connections are **different entities**: `c1`, `c2`, etc., each of which uses a route such as `EXT1 -> API -> DB`. This distinction matters in the current repository: `Assets/Scripts/ConnectionView.cs` already names *physical edges* “connections.” Do not replace its semantics in-place for the experiment. Use unambiguous new names (e.g. `NetworkLink`, `LogicalConnection`) in the experimental implementation.

### Starting editable values (illustrative only)

| Element | Capacity and baseline |
|---|---|
| EXT1 | CPU 100 units/s, memory 8 GB, disk IO 100 MB/s; no meaningful service cost initially |
| API | CPU 100 units/s, memory 8 GB, disk IO 80 MB/s; nonzero idle usage |
| DB | CPU 80 units/s, memory 16 GB, disk IO 100 MB/s; nonzero idle usage |
| AUTH | CPU 60 units/s, memory 8 GB, disk IO 40 MB/s; nonzero idle usage |
| E1: EXT1–API | 50 MB/s bandwidth |
| E2: API–DB | 35 MB/s bandwidth |
| E3: API–AUTH | 25 MB/s bandwidth |

The numbers are knobs, not balancing claims. Store capacities, baseline usage, and per-connection processing costs in inspector-editable data. CPU and disk IO are **rates**; memory is **occupied capacity**. If disk *storage occupancy* is wanted later, model it separately from disk IO; do not conflate them.

## 2. Logical connection V0

A logical connection is a persistent activity, not a moving packet and not a physical edge.

Minimum authored fields:

- `Id` — short console target, e.g. `c1`.
- `Route` — ordered nodes/links (e.g. EXT1 -> API -> DB).
- `TotalWorkMB` — the amount of work/data it needs to complete.
- `DesiredRateMBps` — how fast it *wants* to progress when unhindered.
- `NodeResourceProfile` — simple deterministic CPU-units/MB, disk-IO-MB/MB, and memory-reservation requirements per touched node, initially supplied by a shared preset. **Avoid requiring the designer to author many fields per connection in V0.**

Runtime-derived state:

- `RemainingWorkMB`, `CompletedWorkMB`.
- `ActualRateMBps`, computed from allocation, **not** simply copied from desired rate.
- `DemandMetPct` = actual / desired, and estimated time to finish using current rate.
- Lifecycle: `Pending -> Active -> Completed` (blocked/failed may come later).
- Affected links and nodes; measured resource use and current bottleneck.

A connection starts, continuously consumes the resources of **every relevant link and node in its route**, makes progress only at its actual allocated rate, and stops consuming resources when finished. Avoid per-edge position or literal unit-by-unit traversal. A later phase may add multi-stage goals, route extension, changing demands, deadlines, and malicious behaviors.

## 3. Authoritative simulation semantics

Use one central simulation clock (e.g. a fixed-step 0.1 s update) with presentation running independently at frame rate. **Do not use one coroutine per connection to independently claim shared capacity.** Coroutines are fine for cosmetic animation.

At each simulation step:

1. Gather active connections and their current desired rates and resource requirements.
2. Determine each connection's route and which shared edges/nodes it uses.
3. Allocate capacity deterministically so that **no edge, CPU, disk-IO, or memory constraint is violated**. Start with a simple documented policy (e.g. equal-share/max-min fair allocation limited by demand); sophistication is not the goal.
4. Produce each connection's effective rate based on its tightest constraint. A connection requiring resources along a route cannot run faster than any bottleneck permits.
5. Advance `CompletedWorkMB += ActualRateMBps * dt`, capped at total work.
6. When completed, mark it complete, stop its demand, and let the **next simulation step** allocate the freed resources to remaining connections.
7. Publish a read-only snapshot for node/link/connection views and inspection.

Node load should be a **result** of actual allocated work (plus configured idle usage), not a scripted CPU spike. Memory reservation is held while active; if reservations exceed capacity, start with a clear, deterministic admission/degradation rule rather than silently exceeding memory. Specify allocation order so different enumeration orders do not change results.

### First witness scenario

- E1 shared capacity = **50 MB/s**.
- `c1`: EXT1 -> API -> DB; 120 MB total, desires 35 MB/s.
- `c2`: EXT1 -> API -> AUTH; 60 MB total, desires 35 MB/s.
- Both initially compete on E1 (70 MB/s requested versus 50 available). Under equal-share allocation, each may initially receive ~25 MB/s, subject to other limits.
- When `c2` completes, `c1` should automatically speed up toward 35 MB/s without intervention.
- If DB CPU or disk becomes more restrictive than link E1, `c1` should be limited by **that** resource instead.

This is the central proof that connections affect one another through **real shared constraints**, not hand-authored story outcomes.

## 4. Player-visible information and terminal

**Nodes:** show current/maximum CPU, memory occupied/capacity, and disk IO demand/utilization (plus baseline usage). A healthy node should look visibly different from a saturated node. Values must agree with simulation snapshots.

**Physical edges:** show capacity versus aggregate **requested** bandwidth and **actually delivered** bandwidth (these are not the same). Make oversubscription visually obvious, e.g. `70 requested / 50 available`; a raw 100% utilization bar alone does not show unmet demand.

**Logical connections:** draw a thin path over the static topology from origin through their route. Give each a stable visible ID (`c1`). Each occupied physical edge initially shows a small ID tag on a lane attached to the logical path; labels must not cover nodes or overlap. Reuse a stable lane while a connection exists to avoid jitter. Test three concurrent connection paths sharing E1, including branching toward DB and AUTH. Show progress/performance through width, pulse, opacity, or a small status treatment. Crucially, **red/degraded means not getting what it needs**, not malicious. Completed connections visibly conclude and disappear or briefly fade; capacity is immediately freed in the simulation.

**Inspect:** introduce the simple, immediate console command:

```text
inspect c1
inspect c1 c2
```

Inspection should report, in compact, keyword-driven form:

```text
C1  FILE TRANSFER   ACTIVE   DEGRADED
ROUTE      EXT1 > API > DB
WORK       48 / 120 MB
RATE       25 / 35 MB/s    (71% DEMAND MET)
EST. LEFT  2.9s at current rate
BOTTLENECK E1  (50 / 70 MB/s aggregate demand)
NODE COST  API CPU 0.2u/MB  | DB CPU 0.5u/MB | DB DISK 1.0MB/MB
```

All figures must come from the live model. No inspection delay, old scan slots, hidden moral classification, or artificial answer reveal. The view is an instrument for comparing real demands.

**Terminal interaction:** The terminal is the primary player control, not a decorative log. Keep keyboard command input persistent and responsive while output history scrolls. Support both compact, color-coded text rows and richer **authored output-block prefabs** (small boxed records, tags, progress bars, and side-by-side or stacked comparisons). `inspect` produces an **instant snapshot** of current truth, not a continuously mutating result. Preserve the terminal feeling through monospaced typography, square-ish technical chrome, and restrained graphical elements; do not constrain implementation to ASCII art. Ordinary background events stay compact and low-visual-weight; explicit `inspect` results get structured output.

The initial command grammar may be minimal: `inspect c1` and `inspect c1 c2`. Existing console implementation can be adapted for input, but the scan panel, operations/status sidebar, and old packet logs do **not** belong in the toy scene. Avoid coding a general-purpose terminal UI framework beyond the output-block seam needed by this phase.

## 5. Phased approach

### Phase 0 — Isolated experimental scene, terminal shell, and names

Create the four-node/three-edge layout in a separate scene. Establish a simple ~1/3 terminal + ~2/3 network layout, a working command prompt, scrollable output history, and a minimal reusable output-block interface (plain rows and a placeholder structured result). Remove legacy scan/operations/status side panels **from this scene only**. Add a new simulation host independent of legacy `TrafficDirector`, `PacketView`, and old tick movement. Keep the existing Uptime scene fully playable. Decide unambiguous names for *physical link* versus *logical connection*. Do not implement the full inspection panel yet.

**Done when:** Scene opens, topology and terminal shell render, a test command produces a text line and a structured placeholder, and the original game remains untouched.

### Phase 1 — Resource model, no connections

Implement node CPU/memory/disk-IO capacities plus idle usage and link bandwidth. Show simple accurate metrics on the topology. Add developer/debug readouts of resources.

**Done when:** All four nodes and three links clearly advertise configurable capacities and idle baselines, with no fake demand or changing values.

### Phase 2 — Two or three connections and real allocation

Spawn authored `c1`, `c2`, optionally `c3`. Give each route, work, desired rate, and a lightweight resource profile. Run the deterministic shared allocator. Advance progress, complete activities, and release resource use. Begin with benign static single-stage connections only.

**Done when:** The shared-edge witness works: 70 requested on a 50-capacity link degrades actual rates; completion of one frees capacity and visibly accelerates another. No capacity oversubscribes. Add a second witness where a node, not an edge, is the bottleneck.

### Phase 3 — Connection visualization and inspect

Overlay persistent connection lines and **non-overlapping ID tags** on physical edges; show unmet demand graphically. Wire `inspect c1` and `inspect c1 c2` through a **new/adapter command path** that reuses the existing console UX where practical. Render inspect snapshots via structured terminal-output prefabs with clear text hierarchy and progress bars. Report demands, allocations, progress, affected resources, and bottleneck.

**Done when:** A player can look at the network, identify that something is struggling, inspect two connections, and understand *why* one or both are behind—without a debug inspector.

### Phase 4 — One player intervention (next experiment)

Add only **one** first control, tentatively `throttle c1 <rate>`, that caps a connection's requested rate and makes resources available to peers; add an easy way to restore it. Resist adding route, boost, block, watch, stages, and villain behaviors simultaneously.

**Done when:** The player can deliberately reduce one connection's rate, see another improve, and later reverse the decision. This is the first playable 'handle it for now' moment.

### Phase 5 — Evaluate the toy before expansion

Run a few short scenarios: a shared-edge bottleneck; an API CPU bottleneck; a DB disk bottleneck; one connection finishing naturally; a throttle used and then undone. Verify all observables match calculations, then assess whether the decision making is actually interesting. No mandatory level, scoring, or full UI polish work required.

## 6. Explicitly out of scope for this brief

- Malicious connections, infection, cleaning minigames, villain escalation.
- Multi-stage connection arcs, route extensions, related child connections.
- Advanced `route`, `boost`, `block`, `watch`, subnet predicates, or policy automation.
- Deadlines, business priority, Slack notifications, scoring and good-traffic loss.
- Procedural topology, campaign progression, upgrades, company selection.
- High-fidelity beam/particle art or a total rewrite of old Uptime.

These are excellent follow-ups **after** the resource/satisfaction model works. The existing compact, fast typing interaction is a design constraint, not something to discard.

## 7. Engineering boundaries / open decisions

**Repository check (October 2026):** `Assets/Scripts/GameController.cs` currently runs a coroutine-based tick loop; `TrafficDirector.cs` advances packets through ticks; `ConnectionView.cs` represents a physical edge with `lengthSteps`, `latency`, and throttle ticks; `NetworkRuntime.cs` stores packet, node, and physical-connection views; `CommandParser.cs` holds the current compact verb syntax. This supports building a **parallel model**, rather than reinterpreting these legacy types in place.

Decisions to tune while playing, not before coding:

- Default capacities and CPU/disk/memory costs.
- Fair sharing versus priority weighting (start equal).
- Whether progress is strictly limited by current end-to-end path capacity or later modeled as queued per-hop work (start end-to-end).
- How memory insufficiency manifests (start with transparent admission/degradation behavior).
- Exact line styling, stable lane offsets and the overflow strategy for several tagged paths.
- Final retro terminal styling, output-block density, and whether comparison cards stack or wrap in one-third screen width.

## Design / work boundaries

- **Two experiments in one repo:** old `GameScene.unity` / `ABScene.unity` and new `ConnectionsToy.unity` remain independent; this work is not a migration.
- **Input versus output:** keyboard commands are the player-facing action system; Unity output can be richer than strict console text.
- **Instant commands, evolving consequences:** input/inspection is immediate; simulated resource contention and connection arcs take time.
- **Visual authority:** node/edge displays, labels and inspect panels must not contradict the shared simulation snapshot.
- **Codex handoff:** implement one phase at a time with the brief as the acceptance witness; do not invent later-phase mechanics or rewrite legacy systems proactively.

## Success criterion

**Without any attacker or scripted incident, I can watch two connections compete, understand the shortage, inspect them, change one allocation with a short typed command, and see the other connection measurably benefit.**

If that basic moment feels good, Uptime has a new foundation worth playing with. If it doesn't, the experiment is intentionally small enough to change or abandon without disrupting Duel or the original Uptime prototype.
