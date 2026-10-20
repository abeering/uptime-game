# Uptime — Network and Connection Grammar V1

**Status:** Phase A design contract for Connection Generation V0. Canonical vocabulary for the connection experiment; not a claim that every element is implemented.
**Implementation baseline:** `main`, `0906f6f` (October 20, 2026), with runtime types in `Uptime.Connections` and the authored scene at `Assets/Scenes/Connections.unity`. The original document was drafted against `feat/connections-toy-phase3` at `0d25e3a`.
**Related:** `Docs/Design/connections-v0.md`.

## Guiding fiction

Uptime borrows recognizable network concepts without literally simulating sockets and distributed applications.

**A logical connection is one identifiable story**: a persistent, bounded episode of activity that the player can observe, investigate, and remember. It often aggregates *many* real-world tasks: **Customer Authentications**, **Payment Processing**, or **Inventory Synchronization**, not a single login or checkout. Do not implicitly spawn API-to-database callbacks, separate dependent sockets, or other real networking machinery to explain one connection's activity.

The connection's identity remains stable while its active operation, workload, target, and route might someday change. These transitions matter because the player should be able to notice: *Why did C17 change its target? Why is it now hammering API CPU? When will it stop clogging the link?*

## 1. Topology and network elements

| Term | Canonical meaning |
|---|---|
| **Network / Topology** | The graph of addressable nodes and the links between them; "topology" emphasizes graph structure. |
| **Node** | An addressable element that can be traversed, perform work when targeted, and potentially influence passing connections. It owns resource capacities. A node need not represent one physical computer or one separately modeled service. |
| **Node Role** | Structural/gameplay characterization such as Entry, Transit, Service, Core. Not the same as operational capability. |
| **Node Capability** | Work that can plausibly be targeted at that node (for example database processing or authentication). A separate `Service` runtime entity is not required in V0. |
| **Link** | Physical *or virtual* pathway joining two nodes, with capacity-limited bandwidth. Use "link" as the canonical gameplay term; "edge" is graph-theory terminology. |
| **Route** | Ordered nodes and the intervening links connecting a connection's origin to its current target. |

**Origin, Transit, Target** are per-connection relationships, not fixed node types. API can be the target of C1 while serving as transit for C2.

### Default connection interaction rules

1. Every traversed **link** supplies shared bandwidth capacity. A connection's demand on each link derives from its current operation's bandwidth-per-work cost.
2. **Transit nodes may influence the passing connection**. Ordinarily the connection does **not** perform target-like operations on, or otherwise modify, transit nodes. There are no automatic intermediary requests, service calls, or hidden child connections.
3. **The target performs the connection's current operation** and incurs its mechanical demands (CPU, disk I/O, memory as applicable), while links carry the associated data demand.
4. Future exceptional behaviors, notably malicious interference with transit nodes, must be explicitly authored rather than emergent side effects of route traversal.
5. A connection has **one operational target at a time**. A later stage may change it and its route without changing the connection ID.

In the current `EXT1 → API → DB` path, API is ordinarily *transit* and DB is the *target*. This is intentional game fiction, not how a real API/database system must behave. The four-node toy is `EXT1 — API — {DB, AUTH}`. V0 links currently use one pool of bandwidth in either direction; directional bandwidth is deferred.

## 2. Logical connection, stage and arc

**Logical Connection:** Persistent, individually identifiable story with a stable ID (`c17`), origin, claimed purpose, active operation, lifecycle, and potentially a history of changing behavior. It is distinct from a physical Link, packet, TCP socket, or individual business transaction.

**Lifecycle:** Status of connection establishment/resolution. Current implementation: `Pending → Active → Complete`. Blocking/failing may be added later. Lifecycle is *not* the same as the primitive operation.

**Stage:** A phase of the story characterized by an Operation, its target, and its Workload. A stage ends when its required work is complete or when an explicit future transition occurs.

**Arc / Plan:** A possible sequence (including conditional sequences) of stages over the lifetime of a connection. A planned arc and the actual observed story need not match. **V0 generates one-stage connections only.** Do not yet build an arc planner.

Connections may eventually relate to *other* connections (e.g. coordinated DDoS sources, traceroute correlations, or compromised nodes initiating new internal connections), but relationship groups do not define a connection's identity and are not part of V0.

## 3. Operation — current active state

**Operation:** What the connection is mechanically doing **right now** to its target. V0's complete primitive vocabulary:

| Primitive | Mechanical meaning |
|---|---|
| **READ** | Retrieve or consume information from a target. |
| **WRITE** | Deliver, store, or modify information at a target. |
| **EXECUTE** | Perform computation or processing at a target. |

These primitives do **not** impose special allocator rules or fixed costs. A READ may be CPU-heavy. An EXECUTE may consume bandwidth. Rich labels like "Authentication Processing," "Database Queries," and "File Uploads" are **profiles or descriptive business activities**, not additional primitive execution types.

An active operation instance conceptually supplies: primitive, current target, human-readable activity description, and concrete Workload.

**Presentation principle:** Each active connection should communicate its READ / WRITE / EXECUTE primitive directly in the network view, possibly via animation, movement or pulse language, without requiring inspection. This is **not a color requirement**. Keep primitive identity, amount of actual activity, and degree of resource satisfaction independently representable. Do not inadvertently reveal hidden malicious ground truth through visual effects.

## 4. Work and Workload

**Work:** A measurable quantity of progress necessary to complete **one operation**. Work units are abstract and may represent different things for different operations; **do not sum unlike raw work units** for a connection's eventual multi-stage arc.

**Workload:** The current operation's concrete mechanical requirements:

- `TotalWork`, `CompletedWork`, `RemainingWork` (operation-local).
- `DesiredWorkRate` in work units/second.
- Per-work-unit **CPU cost** and **disk-I/O cost** at the target.
- Per-work-unit **bandwidth cost** on every traversed link.
- A **memory reservation** held while admitted/active.
- Runtime-derived `ActualWorkRate`, resource consumption, bottleneck, and **Resource Satisfaction**.

The profile/generator defines *demand*. Only the authoritative resource allocator decides *allocation* and progress; never fake congestion with scripted display values.

At 5 actual work units/s, illustrative costs of 3 CPU units/work, 0.2 disk MB/work, and 0.5 network MB/work mean 15 CPU units/s plus 1 MB/s of disk at the target, and 2.5 MB/s on **each link** in the route, plus reserved memory.

**Resource Satisfaction** is the ratio of allocated actual work rate to desired work rate, reflecting whether a connection is struggling mechanically. It is not a moral classification, a measure of urgency, or proof it is meeting a deadline.

Workload belongs to the **current operation**, not immutably to the connection's whole identity. Completing an operation may advance an eventual arc, or complete the single-stage V0 connection.

## 5. Two independent kinds of expectation

**Performance Expectation:** The desired or required completion outcome, independent of resource physics. It may attach to the connection as a whole (e.g. a 90-second end-to-end target), and optionally to individual operations. This can eventually govern urgency, scoring, rewards, and losses. **Do not implement scoring yet.**

For an arc whose nominal operation durations are known, operation time budgets *may* be proportionally derived from:
- `NominalDuration_i = TotalWork_i / DesiredWorkRate_i`
- `Share_i = NominalDuration_i / sum(NominalDuration)`
- `OperationExpectation_i = ConnectionExpectation × Share_i`

Use nominal **duration**, not summed raw work units across different operations. These are optional *diagnostic budgets*; missing one need not automatically score a penalty. Changes to an arc's future stages will require an explicit policy later.

**Behavioral Expectation:** What workload size, operation, target, and resource demands are *plausible* for a claimed profile/purpose. These patterns can be authored, learned from observations, or compared among similar connections. This is separate from performance deadlines and from allocation satisfaction. Unusual need not mean malicious.

Future inspection should be able to derive four views from the same model, without fake data:
1. **Connection execution:** desired vs actual rate and bottleneck — *is it struggling?*
2. **Connection performance:** progress/ETA vs expectation — *does it need intervention to meet the target?*
3. **Node resource attribution:** which targeted operations plus idle baseline account for CPU, memory and disk use?
4. **Purpose/profile statistics:** what ranges are typical among comparable activities — *is this workload plausible?*

These are architectural guardrails, not V0 UI commitments.

## 6. Profile, Purpose, and claims

**Connection Profile:** A reusable **generative guideline for a recognizable aggregate activity**, not a generic "Heavy CPU" archetype, rigid script, or guaranteed list of operations. It informs:
- Identity and plausible player-facing descriptions/claimed purposes.
- Typical and weighted READ/WRITE/EXECUTE choices.
- Eligible/favored targets (and hence routes).
- Ranges/distributions for work, desired rate, CPU/disk/bandwidth cost, memory, and duration.
- Frequency/selection weight; optionally later importance and performance expectations.

**Purpose:** What business activity the connection *claims* to support. Examples use a sustained or bulk framing: **Customer Authentications**, **Payment Processing**, **Record Retrieval**, **Inventory Synchronization** — rather than a specific customer's login or checkout. A profile describes behavioral tendencies; a Purpose provides business meaning, potentially company-specific.

**Description:** Human-readable account of claimed activity, not guaranteed mechanical or moral truth.

**Company Catalog (future):** Composes shared profile grammar into company-specific purposes, mixes, importance, and narrative context. **Traffic/event orchestration (future):** Determines when/more of what activity arises. Neither needs a full system for V0.

Keep **claimed purpose/identity**, **observable operation, route, workload and actual performance**, and **hidden objective/ground truth** conceptually separate. A malicious connection may use identical mechanical grammar and resource requirements to a legitimate connection. Unusually resource-hungry benign traffic may exist. No `Malicious = true` flag should automatically change visible presentation or imply a verdict.

## 7. Existing prototype mapping and V0 boundaries

Current implementation in `Uptime.Connections`:

| Existing type | Present responsibility |
|---|---|
| `LogicalConnectionDefinition` | Serialized ID, claimed purpose, current primitive, activity description, route, abstract work and desired work rate, per-work bandwidth cost, per-node CPU/disk/memory requirements, start delay. |
| `LogicalConnectionState` | Lifecycle, work progress, work-rate allocation, bandwidth use, satisfaction, limiting resource. |
| `ResourceAllocator` | Central deterministic memory admission and shared CPU/disk/link allocation; progress and release on completion. |
| `SimulationHost` | Owns fixed-step time and static scene-authored connection initialization. |
| `LogicalPathPresenter` | Presents logical paths; currently binds/rebuilds the full collection at once. |
| `Terminal` / inspect components | Displays current connection identity, route and allocation/progress. |
| Legacy `Assets/Scripts/TrafficDirector.cs` | Drives the **old packet game**. Do not retrofit it for the toy. |

The current serialized scene has two manually authored connections and per-node route costs, including transit API costs. Preserve compatibility with that witness, but **new ordinary generated profiles should place operation resource demand at the target**; transit-node costs are explicit exceptions.

**Next-epic implementation seams:** Abstract work progress and per-work bandwidth are established in Phase 1. Later phases may generate concrete single-stage definitions from profiles, register and retire connection states dynamically, and add/remove path views without tearing down active ones. Preserve the authoritative allocator and legacy Packet simulation.

**Connection Generation V0 acceptance (later phase):** Press Play in `Connections.unity` and see weighted, seeded, believable connections enter, compete for resources, finish, and get replaced. Begin with **3–6 simultaneous** as a hypothesis and **30–90 seconds** as an observable duration target, configurable and not hard limits. Distinguish spawn population, real resource contention, and visual crowding. Preserve authored/high-count stress-test mode.

**Explicitly deferred:** Multistage arcs, topology generation, network expansion, real sockets/services, company campaigns, blocking/scoring/infections/cleaning, connection families, new visual polishing, and the investigative UI.

### Deliberately unsettled

Numeric profile ranges and balancing; how primitives are visually encoded; future deadline/scoring policy; dynamically changing arc budgets; whether several detailed resource meters ultimately collapse into a simpler player KPI. These are questions for playtesting, not prerequisites for building traffic.
