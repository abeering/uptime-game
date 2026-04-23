# TOPOLOGY SYSTEM – V1 NODE ROLES & GENERATION

---

# CORE PRINCIPLE

> Generate **structure first**, place **visually second**.

Topology is a **persistent, evolving graph**, not just positioned nodes.

---

# NODE ROLES

Node roles define **behavior and placement**, independent of node name.

---

## Entry

**Examples**
- ext1
- ext2

**Purpose**
- Traffic ingress/egress
- Source of external pressure

**Traits**
- Low branching nearby
- On perimeter
- Feeds into Transit or Service

---

## Transit

**Examples**
- fw

**Purpose**
- Backbone routing
- Primary decision points (block, reroute, throttle)

**Traits**
- High connectivity
- Central placement
- Forms mainline spine

---

## Service

**Examples**
- web
- api

**Purpose**
- Business systems
- Intermediate or semi-critical destinations

**Traits**
- Mid-importance
- Connect Transit → Core
- Can branch or sit on mainline

---

## Core

**Examples**
- db
- auth
- root

**Purpose**
- Critical infrastructure
- Catastrophic failure targets

**Traits**
- Low branching
- Deep in graph
- Visually protected

---

## Buffer

**Examples**
- cache

**Purpose**
- Absorbs rerouted pressure
- Sacrificial relief

**Traits**
- Leaf node (or near-leaf)
- Limited durability
- Temporarily disables when overloaded

---

## Control

**Examples**
- quarantine

**Purpose**
- Alters packet behavior (hold, delay, transform)

**Traits**
- Side path
- Limited capacity
- Not a primary destination

---

## Utility (Future)

**Examples**
- scanner relay
- scrubber
- honeypot

**Purpose**
- Supports network behavior
- Enables advanced mechanics

**Traits**
- Sparse
- High strategic value
- Not traffic endpoints

---

# CURRENT NODE MAPPING

| Node | Role |
|------|------|
| ext1 | Entry |
| ext2 | Entry |
| fw   | Transit |
| web  | Service |
| api  | Service |
| db   | Core |
| cache| Buffer |

---

# CONNECTION RULES

## Allowed Connections

- Entry → Transit, Service
- Transit → Transit, Service, Core, Buffer, Control, Utility
- Service → Transit, Service, Core, Buffer, Control
- Core → Transit, Service
- Buffer → Transit, Service
- Control → Transit, Service
- Utility → Transit, Service

---

## Restrictions

### Core
- Avoid Core → Entry
- Avoid Core → Buffer
- Keep low branching

### Buffer
- Prefer degree ≤ 2
- Usually leaf

### Control
- Prefer single parent connection
- Optional rejoin later

---

# EDGE LENGTH CLASSES

Defined at topology level (not incidental)

## Short
- Tight coupling
- Fast traversal
- Example: api → db

## Medium
- Default connections
- Example: fw → web

## Long
- Expensive routes
- Example: entry → backbone
- Example: service → quarantine

---

# TOPOLOGY TEMPLATES

---

## Template A – Single Spine + Buffer

Entry → Transit → Service → Service → Core  
+ Buffer off Service

**Use**
- Early game
- Teach reroute

---

## Template B – Dual Entry Spine

Two Entries feed same backbone

**Use**
- Midgame
- Multi-origin pressure

---

## Template C – Spine + Control Spur

Mainline + one Control node

**Use**
- Introduce quarantine
- Enable reroute decisions

---

## Template D – Split Services, Shared Core

Two service branches converge into Core

**Use**
- Larger maps
- L-shaped layouts

---

# TOPOLOGY MUTATIONS (BETWEEN LEVELS)

---

## Add Buffer Spur
Add Buffer leaf off Transit/Service

**Effect**
- New reroute sink
- More resilience

---

## Add Control Spur
Add Control node off Transit/Service

**Effect**
- Enables reroute mechanics
- Adds delay/hold strategies

---

## Add Service Branch
Add new Service node + connection

**Effect**
- Expands topology
- Adds routing complexity

---

## Reinforce Core Access
Improve path to Core

**Effect**
- Better throughput
- More routing options

---

## Add Secondary Ingress
Add new Entry node

**Effect**
- More pressure
- Larger map footprint

---

## Upgrade Buffer
Improve existing Buffer

**Effect**
- More durability
- Preserves topology familiarity

---

# PLACEMENT REGIONS (SCENE-AWARE)

---

## Perimeter (Left / Edges)
- Entry nodes

## Upper Mid
- Service nodes

## Center / Protected
- Core nodes

## Lower / Side Pockets
- Buffer nodes
- Control nodes

## Extended Regions (Right / L-shape)
- Secondary ingress
- Advanced branches

---

# GENERATION CONSTRAINTS (V1)

- One clear mainline spine
- Max 2 Entry nodes
- Max 1 Buffer (early)
- Max 1 Control (early)
- Buffer must be leaf
- Control must be side branch
- Core ≥ 2 hops from Entry
- Branch depth ≤ 1
- No loops (initially)

---

# NODE MODEL (RECOMMENDED)

Each node has:

- `NodeRole` (Entry, Transit, etc.)
- `Tags[]` (optional modifiers)

**Example**
- api → Role: Service, Tags: [Hub]
- cache → Role: Buffer, Tags: [Fragile]

---

# SYSTEM ARCHITECTURE OVERVIEW

---

## TopologyGraph
Persistent graph state across run

Contains:
- nodes
- edges
- roles
- growth history

---

## TopologyTemplate
Starting structure per company

---

## TopologyMutation
Between-level modification

---

## TopologyValidator
Ensures valid + readable graph

---

## TopologyLayoutSolver
Maps graph → positions

---

## TopologyBuildRuntime
Spawns Unity objects

---

# DESIGN GOAL

> Build a **persistent, growing operational network**  
> where structure drives gameplay decisions.

---

# FINAL PRINCIPLE

> Reroute, infection, and pressure systems only work if topology is intentional.