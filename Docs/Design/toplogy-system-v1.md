# TOPOLOGY SYSTEM – V1

---

# FRAMING

> The topology is a persistent, evolving network that defines  
> where consequences occur, not just how packets move.

- Reroute = relocate consequence
- Nodes = roles, not just visuals
- Growth = player-driven evolution

---

# NODE ROLES

## Entry
- Introduces traffic
- Placed at perimeter
- Feeds into Transit/Service

## Transit
- Backbone routing
- High connectivity
- Main decision points

## Service
- Business systems
- Mid-importance destinations
- Connects Transit → Core

## Core
- Critical infrastructure
- Failure = catastrophic
- Protected placement

## Buffer
- Absorbs rerouted pressure
- Leaf node
- Limited durability

## Control
- Alters packet behavior (hold/delay)
- Side path
- Limited capacity

---

# GRAPH GRAMMAR

## MAINLINE

Entry → Transit → (Transit | Service)+ → Core

Constraints:
- One clear spine
- Core ≥ 2 hops from Entry
- Length: 3–5 nodes

---

## EXPANSIONS

### Buffer Spur
(Service | Transit) → Buffer

### Control Spur
(Service | Transit) → Control

### Service Branch
(Service | Transit) → Service

### Secondary Ingress
Entry₂ → Transit | Service

---

# MUTATION SYSTEM

Between levels, player selects 1 of 3 mutations.

A mutation:
- modifies existing graph
- preserves structure
- introduces new decisions

## Mutation Types

- Add Buffer Spur
- Add Control Spur
- Add Service Branch
- Add Entry
- Reinforce Path
- Upgrade Buffer

## Selection Design Rules

Each set should include:
- Safety option
- Complexity option
- Risk/reward option

---

# GROWTH MODEL

- Topology persists across levels
- Nodes are never replaced, only expanded or modified
- Player builds familiarity

Supports:
- scars (damaged nodes)
- upgrades
- expansion paths

---

# PLACEMENT REGIONS

- Perimeter → Entry
- Backbone lane → Transit
- Service cluster → Service
- Protected zone → Core
- Side pockets → Buffer / Control

---

# LAYOUT GOALS

- Clear mainline
- Readable branches
- Minimal edge crossing
- Use full space (including L-shape)

---

# EDGE MODEL

Edges have:
- Length class (short / medium / long)
- Latency
- Visual spacing target

## Design Intent

- Long = expensive / safe reroute
- Short = fast / dangerous

---

# VALIDATION RULES

A topology must:
- Have path Entry → Core
- Maintain readable mainline
- Respect role constraints
- Limit branch depth
- Avoid clutter

---

# COMPLEXITY TIERS

## Early
- 1 Entry
- 1 Buffer
- No Control
- Shallow graph

## Mid
- 2 Entries
- 1 Control
- More branches

## Late
- Multiple Buffers/Controls
- Core clusters
- Higher complexity

---

# DESIGN PRINCIPLES

- Topology expresses decisions, not just paths
- Reroute is meaningful because destinations differ
- Growth is additive, not destructive
- Readability > complexity
