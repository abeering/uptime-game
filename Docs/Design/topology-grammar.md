# TOPOLOGY GRAMMAR – V1 (CONSTRAINED GENERATION)

---

# PURPOSE

This defines **how valid network graphs are constructed and evolved**.

> This is NOT freeform procgen.  
> This is a **rule-driven grammar** that produces readable, intentional topologies.

---

# CORE IDEA

A topology is built from:

- **Nodes (with roles)**
- **Edges (with length classes)**
- **Patterns (structural shapes)**
- **Mutations (controlled growth over time)**

---

# 1. BASE GRAMMAR (SEED STRUCTURE)

All topologies begin from a **Mainline Spine**.

---

## MAINLINE RULE

Entry → Transit → Service → Service → Core

### Constraints
- Exactly 1 spine in early versions
- Spine must be **visually readable and mostly linear**
- Core must be ≥ 2 hops from Entry
- Spine length = 3–5 nodes (early game)

---

## NODE ROLE SEQUENCE (VALID FOR SPINE)

Entry → Transit → (Transit | Service)+ → Core

Allowed examples:
- Entry → Transit → Service → Core
- Entry → Transit → Transit → Service → Core

---

# 2. STRUCTURAL PATTERNS (EXPANSIONS)

These are **legal graph additions**.

---

## PATTERN: BUFFER SPUR

(Service | Transit) → Buffer

### Rules
- Buffer must be **leaf**
- Only 1 connection initially
- Cannot attach directly to Entry or Core

### Purpose
- Reroute sink
- Damage absorption

---

## PATTERN: CONTROL SPUR

(Service | Transit) → Control

### Rules
- Control must be side branch
- Initially no rejoin edge
- Optional rejoin allowed in later tiers

### Purpose
- Delay / manipulate packets

---

## PATTERN: SERVICE BRANCH

(Service | Transit) → Service

### Rules
- Adds lateral expansion
- Can later connect toward Core or remain side path
- Must not create deep branching early

### Purpose
- Increases routing complexity

---

## PATTERN: SECONDARY INGRESS

Entry₂ → Transit | Service

### Rules
- Max 2 Entry nodes (early)
- Must connect into existing graph
- Should approach from different spatial region

### Purpose
- Multi-source pressure
- Uses more screen space

---

## PATTERN: CORE CLUSTER (LATER)

Service → Core₁  
Service → Core₂

### Rules
- Cores should be near each other
- Avoid deep chaining Core → Core
- Limit cluster size early

### Purpose
- Multiple failure targets
- Late-game pressure

---

# 3. EDGE RULES

Edges are not arbitrary — they follow constraints.

---

## DEGREE CONSTRAINTS

| Role     | Max Degree (Early) |
|----------|-------------------|
| Entry    | 1–2               |
| Transit  | 2–4               |
| Service  | 2–4               |
| Core     | 1–2               |
| Buffer   | 1                 |
| Control  | 1–2               |

---

## FORBIDDEN CONNECTIONS (V1)

- Entry ↔ Core
- Core ↔ Buffer
- Buffer ↔ Buffer
- Control ↔ Entry

---

## OPTIONAL LATER

- Control → rejoin edge
- Limited loops
- Buffer chaining (rare)

---

# 4. EDGE LENGTH ASSIGNMENT

Each edge gets a **length class** at generation time.

---

## RULES

- Spine edges = mix of **short + medium**
- Entry → first node = **long**
- Buffer edges = **short or medium**
- Control edges = **medium or long** (to enforce cost)
- Secondary ingress edges = **long**

---

## PURPOSE

Length defines:
- travel time
- reroute cost
- visual spacing target

---

# 5. GENERATION PROCESS

---

## STEP 1 – CREATE SPINE

- Select spine length (3–5)
- Assign roles per grammar
- Create linear chain

---

## STEP 2 – APPLY PATTERNS

For each eligible node:

- Roll for:
  - Buffer Spur (low-mid chance)
  - Control Spur (low chance early)
  - Service Branch (mid chance)

Apply constraints:
- Max 1 Buffer (early)
- Max 1 Control (early)
- Max branch depth = 1

---

## STEP 3 – OPTIONAL INGRESS

- Chance to add second Entry
- Connect to early/mid spine node

---

## STEP 4 – VALIDATION

Reject graph if:

- No path Entry → Core
- Core too close to Entry
- Too many branches
- Buffer not leaf
- Control incorrectly placed
- Graph unreadable (heuristic)

---

## STEP 5 – ASSIGN EDGE LENGTHS

- Apply rules above
- Store as logical property

---

## STEP 6 – HAND OFF TO LAYOUT

Graph → Layout Solver

---

# 6. MUTATION GRAMMAR (BETWEEN LEVELS)

These are **player-facing upgrades**.

Each mutation modifies existing graph.

---

## MUTATION: ADD_BUFFER

Attach Buffer to (Transit | Service)

---

## MUTATION: ADD_CONTROL

Attach Control to (Transit | Service)

---

## MUTATION: ADD_SERVICE

Attach new Service node

---

## MUTATION: ADD_ENTRY

Add new Entry node

---

## MUTATION: UPGRADE_BUFFER

Modify Buffer properties (HP, capacity)

---

## MUTATION: REINFORCE_PATH

Improve edge(s) toward Core

Examples:
- shorten edge
- reduce latency
- add alternate connection (later)

---

# 7. GROWTH RULES

- Mutations must be **local**
- Existing nodes persist
- Graph should remain recognizable
- No full regeneration mid-run

---

# 8. COMPLEXITY TIERS

---

## EARLY

- 1 Entry
- 1 Buffer
- 0 Control
- No loops
- Shallow graph

---

## MID

- 2 Entries
- 1 Buffer
- 1 Control
- More Service branching

---

## LATE

- Multiple Buffers
- Multiple Control nodes
- Limited loops
- Core clusters

---

# 9. DESIGN INTENT

This grammar ensures:

- Readable networks
- Meaningful reroute decisions
- Controlled complexity growth
- Compatibility with persistent topology

---

# FINAL PRINCIPLE

> A valid topology is not just connected —  
> it **expresses clear paths, meaningful branches, and intentional tradeoffs**.
