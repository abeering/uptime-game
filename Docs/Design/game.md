# GAME THEORY – NETWORK DEFENSE (WORKING DOCUMENT)

## OVERVIEW

This document captures the high-level design philosophy, run structure, and core systems direction for the game.

The goal is not to define implementation details, but to solidify the **emotional experience**, **player decision space**, and **long-term replayability structure**.

---

# CORE GAME THESIS

> **A real-time system defense game where players make high-speed decisions under uncertainty, expressed through typing, while shaping the behavior of a network under escalating pressure.**

### Key Pillars

- Real-time, rapid command input
- Overworked sysadmin/operator fantasy
- Retro console interface (diegetic, grounded)
- Decision-making under uncertainty and pressure

---

# DESIGN SHIFT (CRITICAL INSIGHT)

The game is not about:
- typing commands
- or executing mechanics efficiently

The game *is about*:
> **Making imperfect decisions under pressure, with incomplete information, and living with the consequences.**

---

# CORE STRUCTURE: THREE LAYERS OF GAMEPLAY

## 1. Micro (Execution Layer)
- Commands: `scan`, `block`, `clean`, `throttle`, etc.
- Fast, real-time input
- Immediate consequences

## 2. Tactical (Mid-Run Adjustments)
- Node specialization
- Temporary interventions
- Flow adjustments

## 3. Strategic (Run Identity)
- Network evolution
- Upgrade selection
- System-wide behavior shaping

---

# RUN STRUCTURE

## 1. Contract Selection
- Choose 1 of 3 randomized businesses
- Each defines:
  - topology style
  - attack profile
  - flavor (CEO tone, events, interruptions)

## 2. Difficulty Selection
- Unlockable tiers
- Affects:
  - spawn pressure
  - complexity
  - modifiers

## 3. Multi-Level Loop (5–10 Levels)

Each level:
- Real-time defense phase
- Survive escalating pressure

### Failure Conditions
- Critical nodes compromised
- Too much normal traffic lost

---

## 4. Inter-Level Phase (CRITICAL)

After each level:
- Select upgrades
- Apply node modifications
- Receive possible events

> This is where **run identity is formed**

---

## 5. Network Expansion

Between levels:
- Topology expands
- New nodes introduced
- New pressure points emerge

> The network persists and evolves throughout the run

---

## 6. Final Level (Crescendo)

- Maximum pressure
- Combined threats
- Tests entire build

> Should feel like: **“everything I built is being tested”**

---

## 7. End of Run

- Score based on:
  - survival
  - efficiency
  - mistakes

- Leaderboards per:
  - business
  - difficulty

---

# EMOTIONAL LOOP (CRITICAL)

Each level should follow:

1. Rising pressure
2. Overload moment
3. Desperation decisions
4. Barely survive
5. Immediate reflection:
   > “what would have helped?”

Then:

> **Upgrade phase answers that question**

---

## KEY RULE

> **Every level should feel like you barely survived.**

There should be:
- no comfort plateau
- no “easy middle”

---

# UPGRADE SYSTEM PHILOSOPHY

## Core Idea

> The game offers far more upgrades than any single run can realize.

- Total pool: large (100–200+)
- Per run: ~10–15 selections

---

## Properties of Upgrades

Upgrades must:
- Change behavior (not just numbers)
- Introduce tradeoffs
- Alter decision-making

---

## BAD UPGRADES (avoid)

- +10% speed
- +5% efficiency

---

## GOOD UPGRADES

- Change how commands behave
- Modify node roles
- Alter system dynamics

---

## Persistence

- All upgrades are permanent within the run
- The network accumulates identity over time

---

## Synergy (Important)

Some upgrades should:
- Modify or enhance previous upgrades
- Create compounding effects

---

# NETWORK PHILOSOPHY

## Core Principle

> **The player does not build the network—they shape how it behaves.**

---

## Why NOT Full Topology Design

Avoid:
- analysis paralysis
- slow gameplay
- loss of real-time pressure

---

## Instead: Node Specialization

Each node acts as a **slot for identity expression**

Nodes can be modified to:
- change traffic behavior
- affect command outcomes
- introduce strengths and weaknesses

---

## Categories of Node Mods

### 1. Information
- Better visibility
- Earlier detection

Tradeoff:
- slower system

---

### 2. Flow
- Speed, routing, prioritization

Tradeoff:
- less control / visibility

---

### 3. Defense
- Blocking, mitigation

Tradeoff:
- bottlenecks

---

### 4. Containment
- Isolation, segmentation

Tradeoff:
- fragmentation

---

### 5. Automation
- Auto-handling cases

Tradeoff:
- reduced precision

---

## Design Rule

> Nodes should become more specialized and less flexible over time.

---

# TEMPORARY SYSTEMS (ACTIVE PLAY)

Temporary node deployment / effects:

- Short-lived, high-impact
- Used during pressure spikes

Examples:
- emergency firewall
- scan hub
- buffer node
- quarantine zone

---

## Purpose

- Adds burst decision-making
- Enhances real-time tension
- Supports clutch plays

---

# ARCHETYPES (SYSTEM-DRIVEN, NOT CHARACTER-DRIVEN)

The game supports different **operational doctrines**:

## 1. Information-Oriented
- prioritize knowledge
- slower, precise

## 2. Throughput-Oriented
- prioritize flow
- fast, less visibility

## 3. Aggressive
- eliminate threats quickly
- accepts collateral damage

## 4. Containment
- isolate problems
- slow but safe

## 5. Automation
- reduce manual input
- setup-heavy

---

## IMPORTANT

Archetypes emerge from:
> **network specialization + upgrade choices**

NOT from:
- fixed classes
- locked roles

---

# CORE DESIGN LOOP (SYNTHESIS)

1. Level nearly kills you
2. You identify what would have helped
3. You pick an upgrade to address that
4. Next level introduces a new problem
5. Your solution has a weakness

---

> **This loop drives replayability**

---

# ESCALATION DESIGN

## Critical Rule

> New levels must introduce new *types* of pressure, not just more volume.

---

## Example Progression

- Early: basic threats
- Mid: volume spikes
- Late: deception / hybrid threats
- End: combined pressure

---

## Goal

> No single strategy should remain dominant

---

# CONSTRAINT SYSTEMS (REQUIRED)

To prevent optimal play:

- limited upgrade slots
- mutually exclusive node roles
- opportunity cost
- time pressure

---

# BUSINESS SYSTEM (RUN VARIATION)

Each business should meaningfully differ:

## Dimensions

- traffic type
- tolerance for failure
- topology structure
- attack patterns

---

## Example

- Finance → precision
- Social → volume chaos
- Healthcare → critical nodes

---

## Purpose

> Push players toward different strategies per run

---

# FINAL DESIGN STATEMENT

> **The player defends a dynamically evolving network by issuing real-time commands while specializing nodes to shape traffic behavior, information flow, and system resilience—creating distinct strategic identities each run under escalating pressure.**

---

# NEXT STEPS

- Define upgrade categories in detail
- Define 3 initial businesses
- Simulate full runs
- Validate that different builds produce different moment-to-moment gameplay

---