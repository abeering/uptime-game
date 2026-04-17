# LEVEL DESIGN & RUN STRUCTURE – CORE SPEC

---

# OVERVIEW

This document defines the **core structure of runs, levels, failure conditions, and narrative drivers**.

It establishes:

* What a run is
* What a level represents
* How success and failure work
* How company and villain roles are separated
* How escalation is structured

This is the **foundation for building cohesive, intentional levels**.

---

# CORE FRAMING

## Run = Job

## Level = Day (Shift)

* The player is a sysadmin/operator hired by a company
* Each level represents a **day of work**
* The run represents the **entire job**

---

## Failure

> Failing a level = you are fired

* Run ends immediately
* Player starts a new run (new job)

---

## Success

> You are not trying to win — you are trying to survive

* Completing a level = surviving the day
* Completing a run = surviving all days

---

# LEVEL COMPLETION

## Success Condition

A level is completed when:

* The player survives until the end of the level duration
* No failure conditions are triggered

There is:

* No score requirement
* No “win state” beyond survival

---

## Failure Conditions (Run Ends Immediately)

### 1. Good Traffic Loss Threshold Exceeded

* Too many benign / priority packets are lost
* Represents business operations failure

---

### 2. Critical Node Compromised

* Core infrastructure (DB, root, auth, etc.) is lost
* Represents catastrophic system failure

---

## Design Principle

> Failure is systemic, not mechanical.

The player does not fail because of:

* a missed command
* a single mistake

The player fails because:

> the system degraded beyond viability

---

# SCORE (SEPARATE FROM SURVIVAL)

Score is:

* Evaluative only
* Not tied to level completion

---

## Score Reflects

* Timing (early / late delivery)
* Threat handling
* Mistakes (false blocks, etc.)
* Maintenance (cleaning, recovery)

---

## Important Rule

> Score does NOT determine success or failure.

---

## Timing (Packet Speed)

Packet speed:

* Does NOT cause failure
* Exists only as:

  * score modifier
  * warning signal
  * pressure indicator

---

# RUN STRUCTURE

## Start of Run

* Player selects:

  * Company
  * Difficulty

* Run begins at Day 1

---

## Multi-Day Structure

Each run consists of:

* 5–10 days (levels)

Each day:

* Has a title and identity
* Introduces or reinforces a type of pressure
* Escalates overall difficulty

---

## End of Run

* Occurs on:

  * failure (fired), OR
  * completion of final day

* Score is summarized

---

# DAY DESIGN (LEVEL STRUCTURE)

Each day represents a **shift that went wrong**.

---

## Day Progression Pattern

Typical structure:

1. Day 1 – Calibration

   * Learn system
   * Low pressure

2. Day 2 – Increased Load

   * Confidence building
   * Slight stress

3. Day 3 – First Disruption

   * Introduce major threat type

4. Day 4 – Compounding Problems

   * Multiple systems interact

5. Day 5 – System Stress Test

   * Full pressure
   * Combined threats

---

## Emotional Loop (Per Day)

1. Rising pressure
2. Overload moment
3. Desperation decisions
4. Barely survive
5. Reflection (what would have helped?)

---

## Key Rule

> Every day should feel like you barely survived.

---

# PACING MODEL

Each level should contain:

## 1. Flow (Baseline)

* Normal traffic
* Player stabilizes system

## 2. Pressure (Event)

* Spikes (DDOS, infection, etc.)
* Player is stressed

## 3. Recovery (Lull)

* Reduced pressure
* Player regains control

---

## Design Insight

> Lulls are not empty — they are controlled breathing space.

---

# DUAL RUN DRIVERS

Each run is defined by two independent forces:

---

## 1. Company (Context)

Defines:

* Topology
* Traffic profile
* Failure sensitivity
* Flavor / tone

---

### Company Role

> Defines what you are protecting

---

### Company Influences

* What matters most (speed, accuracy, uptime, etc.)
* What fails hardest
* What feels stressful
* Narrative tone (funny, chaotic, corporate)

---

---

## 2. Hacker Villain (Escalation)

Defines:

* Attack patterns
* Event types
* Difficulty progression

---

### Villain Role

> Defines how the system is attacked

---

### Villain Influences

* What types of threats appear
* When spikes occur
* How pressure escalates across days

---

# DESIGN PRINCIPLE

> Company = stakes
> Villain = pressure

---

# ESCALATION MODEL

Difficulty increases because:

> An adversary is actively applying pressure to the system

---

## Villain Phase Progression

Across days:

* Day 1 → probing
* Day 2 → disruption
* Day 3 → first real attack
* Day 4 → compounded attacks
* Day 5 → full assault

---

## Design Rule

> New days must introduce new TYPES of pressure, not just more volume.

---

# GAME IDENTITY (FINAL STATEMENT)

> The player is an overworked sysadmin trying to survive consecutive days at a dysfunctional company while an increasingly aggressive attacker pushes the system toward failure.

---

# SUMMARY

## You succeed by:

* Surviving each day
* Keeping the system operational

## You fail by:

* Losing too much good traffic
* Losing critical infrastructure

## The experience is driven by:

* Company → context and stakes
* Villain → escalation and pressure

---

## Final Design Principle

> The player is not trying to win.
> The player is trying to survive a failing system long enough to not get fired.
