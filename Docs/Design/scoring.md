# SCORING SYSTEM – NETWORK DEFENSE (WORKING DESIGN)

## PURPOSE

Define a **flexible, event-driven scoring system** that:

* Reflects **player decision quality under pressure**
* Separates **score from failure**
* Evolves alongside game mechanics
* Supports both **deep analysis** and **casual readability**

---

# CORE PRINCIPLES

## 1. Score ≠ Survival

* Survival = pass/fail condition
* Score = **how well the player performed while surviving**

A messy survival should score worse than a clean one.

---

## 2. Score Reflects Judgment, Not Just Output

The game is about:

> **making imperfect decisions under pressure**

Therefore score must evaluate:

* outcomes (what happened)
* efficiency (how well it flowed)
* decisions (how smart the player was)

---

## 3. Event-Driven, Not Formula-Driven

Score is NOT:

> one giant equation

Score IS:

> a collection of **events**, each evaluated with context

---

# SYSTEM STRUCTURE

## Overview

```
Score Category
  → Score Event
    → Base Value
    → Modifiers (context-aware)
    → Final Score Contribution
```

---

## 1. Score Categories (Top-Level Buckets)

Used for:

* UI breakdown
* tuning
* player understanding

### Initial Categories

* **Throughput**

  * did the system function?

* **Efficiency**

  * did it function well?

* **Threat Handling**

  * did you deal with danger correctly?

* **Information**

  * did you use scanning intelligently?

* **Mistakes**

  * what went wrong?

* **Survival Bonus**

  * did you hold it together?

---

## 2. Score Events (What Happened)

Discrete, meaningful gameplay outcomes.

### Examples

#### Throughput

* HealthyPacketDelivered
* HealthyPacketLost

#### Threat Handling

* ThreatBlocked
* ThreatReachedNode
* ThreatCleaned

#### Information

* ThreatConfirmedByScan
* ScanCanceled

#### Mistakes

* BenignBlocked
* OverScan (low-value scan usage)

---

## 3. Base Values

Simple, readable defaults.

Example:

```
HealthyPacketDelivered = +10
HealthyPacketLost      = -20
ThreatBlocked          = +15
BenignBlocked          = -20
ThreatConfirmed        = +8
```

These are intentionally **flat and dumb**.

---

## 4. Modifiers (Where the Game Lives)

Modifiers interpret **context**.

### Key Modifier Inputs

* confidence at time of action
* packet lifetime / response speed
* node criticality
* system pressure level
* whether action was canceled
* downstream consequences

---

### Example

#### Event: ThreatBlocked

```
Base: +15

Modifiers:
- Confidence Factor (lower confidence = higher reward)
- Response Time (faster = better)
- Critical Node Bonus
```

---

#### Event: BenignBlocked

```
Base: -20

Modifiers:
- Confidence Mitigation (honest mistake vs careless)
- Priority Traffic Penalty
- Cascading Damage Penalty
```

---

## DESIGN RULE

> **Events describe facts. Modifiers interpret meaning.**

---

# SCORE LEDGER (IMPORTANT)

Two distinct systems:

## 1. Score Definition

* static rules
* designer-controlled
* easily tunable

## 2. Score Ledger

* runtime record of events
* stores:

  * event type
  * base value
  * modifiers applied
  * final score

---

### Benefits

* debugging
* replay analysis
* breakdown UI
* balancing visibility

---

# MINIMAL V1 IMPLEMENTATION

Start small.

### Events

* HealthyPacketDelivered
* HealthyPacketLost
* ThreatBlocked
* BenignBlocked
* ThreatReachedNode
* ThreatConfirmedByScan

### Modifiers (only 2–3)

* confidence
* response time
* node importance

---

# RUN VS LEVEL SCORING

## Level Score

* immediate feedback
* reinforces learning loop

## Run Score

* cumulative
* reflects build + decisions over time

---

# QUALIFIER / RANK SYSTEM (PLAYER-FACING)

## Purpose

Most players will not care about raw numbers.

They need:

> **a fast, flavorful judgment of performance**

---

## Design Goals

* readable in 1 second
* thematic (hacker/sysadmin tone)
* loosely tied to score categories
* not overly granular

---

## Example Tier System (6 Levels)

### Top → Bottom

* **ELITE**
* **OPERATOR**
* **SCRIPT KIDDIE**
* **UNSTABLE**
* **COMPROMISED**
* **BREACHED**

---

## Alternate Flavor (More Hacker-Oriented)

* **ROOT**
* **PRIVILEGED**
* **ELEVATED**
* **USERLAND**
* **THROTTLED**
* **OWNED**

---

## How It’s Determined

Not just total score.

Instead, evaluate:

* throughput success
* mistake rate
* efficiency

Example logic:

```
if survival + low mistakes + high efficiency → ELITE
if survival + moderate mistakes → OPERATOR
if survival + high mistakes → USERLAND
if near-failure → COMPROMISED
if failure → BREACHED
```

---

## Important Rule

> Rank should reflect **how it felt**, not just math.

---

# FUTURE EXTENSIONS

Because the system is event-driven, we can easily add:

* automation scoring
* infection containment scoring
* node specialization scoring
* business-specific scoring biases

Example:

* Finance → penalize mistakes heavily
* Social → reward throughput more

---

# PITFALLS TO AVOID

* Overweighting speed → encourages reckless play
* Pure throughput scoring → ignores decision quality
* Too many micro-events → noise, hard to balance
* Hidden scoring → player confusion

---

# FINAL SUMMARY

The scoring system should:

* reward **correct decisions under uncertainty**
* punish **meaningful mistakes**
* reflect **flow and efficiency**
* remain **flexible and evolvable**

---

> **Do not chase the perfect formula.
> Build a system that can grow with the game.**
