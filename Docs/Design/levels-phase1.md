# PHASE 1 – LEVEL FEEL + ATTACK FOUNDATION

## PURPOSE

This phase is about achieving the first **playable, satisfying level experience**.

NOT building full systems.

We are proving:

> “Does a level feel like a coherent, authored experience with pressure, events, and resolution?”

---

# CORE GOALS

### 1. First Real Attack Plan

* Attacks should feel like:

  * intentional
  * noticeable
  * different from ambient traffic
* Player should think:

  > “something just happened”

---

### 2. `spawnattack <key>` Debug Command

* Allows manual triggering of authored attack patterns
* Must use the same pipeline as future level content
* Becomes the foundation for:

  * level scripting
  * testing
  * tuning

---

### 3. Stub Run + Level Hierarchy (LIGHT ONLY)

We are NOT building full systems yet.

Just enough structure to avoid rework later.

#### Conceptual Model

Run
→ Level
→ Attack Plans
→ Spawn Plans

#### Minimal Stubs

* `RunDefinition` (placeholder)
* `LevelDefinition` (placeholder)
* `LevelDirector` (runtime stub)

These should:

* not be feature-complete
* only support Phase 1 testing

---

### 4. Autospawn Tuning (Background Noise)

Autospawn = ambient traffic layer

It should:

* exist independently of attacks
* provide constant pressure
* be tunable per level/difficulty

#### Key Insight

> Autospawn is NOT the level.
> It is the **baseline tension layer**.

Attacks are what create:

* spikes
* identity
* memorable moments

#### Phase 1 Rules

* Keep autospawn simple
* Use existing `TrafficDirector` logic
* Add ability to:

  * enable/disable
  * tweak intensity (interval, ramp)

Later:

* move to per-level definitions

---

### 5. Keyword System Integration

We previously defined a spawn pipeline that must now be formalized.

## Spawn Pipeline (FINALIZED SHAPE)

Every packet spawn should follow:

1. `rollBase`
2. `rollClass` (Benign / Threat / Priority)
3. `rollKind` (Virus, Worm, etc.)
4. `rollInfectious`
5. `rollInfectionType`
6. `rollKeywords`

---

## Keyword System Requirements

### A. Variable Keyword Count

Packets may have:

* 0 keywords (common)
* 1 keyword (standard)
* 2–3 keywords (rare, dangerous)

Example distribution:

* 0 → 50%
* 1 → 35%
* 2 → 12%
* 3 → 3%

---

### B. Weighted by Context

Keyword selection should depend on:

* packet class
* packet kind

Examples:

#### Worm

* infectious (high weight)
* spreading
* evasive

#### Virus

* payload-based
* stealth
* delayed

#### Priority

* fragile
* fast
* time-sensitive

---

### C. Keyword Role

Keywords should:

* modify behavior
* change player decision-making
* increase uncertainty

NOT just be flavor.

---

### D. Integration Strategy (Phase 1)

Do NOT overbuild.

Implement:

* keyword roll function
* keyword list on packet
* minimal behavioral hooks (if already supported)

Do NOT implement:

* full keyword ecosystem
* complex interactions

---

# ATTACK PLAN DESIGN (PHASE 1)

## Definition (Conceptual)

Attack Plan = **authored pressure pattern**

Not:

* single spawn

But:

* coordinated sequence of spawns

---

## Phase 1 Implementation (NO SYSTEM YET)

Attack plans should be:

* simple methods/functions
* manually scheduled
* reusable

Example:

* `Attack_NoisyProbe`
* `Attack_VipWindow`
* `Attack_InfectionBurst`

---

## Attack Characteristics

Each attack should define:

* timing window
* number of packets
* route bias
* packet composition:

  * benign
  * threat
  * priority
* optional infection
* optional keyword bias

---

## Design Goal

Player should feel:

> “This is different from normal traffic.”

---

# LEVEL SHAPE (TARGET EXPERIENCE)

Each level should feel like:

1. Opening (calm)
2. Ramp (pressure)
3. Attack moment(s)
4. Climax (overload)
5. Easing (pressure stops)
6. Resolution (scoring)

---

## CRITICAL RULE

> Pressure must STOP at some point.

Without this:

* no relief
* no ending
* no satisfaction

---

# WHAT WE ARE NOT DOING (IMPORTANT)

Do NOT build:

* full run system
* upgrade system
* company modifiers
* final scoring UI
* full attack definition framework

---

# SUCCESS CRITERIA

Phase 1 is successful when:

* A single level feels:

  * structured
  * tense
  * readable
  * satisfying at the end

* Attacks feel:

  * intentional
  * noticeable
  * different from ambient traffic

* Autospawn + attack interaction feels:

  * natural
  * not chaotic noise

* Keywords begin to:

  * appear
  * influence behavior

---

# NEXT PHASE (NOT NOW)

After success:

* formal AttackPlanDefinition
* LevelDefinition data-driven system
* RunDefinition + progression
* company modifiers
* upgrade system

---

# SUMMARY

Phase 1 builds:

* feeling
* not systems

---

> If it doesn’t feel good, architecture doesn’t matter yet.


# prompt 

I’m working on a Unity (C#) real-time network defense game and starting Phase 1 of level design.

Goal:
Build the first playable level that feels like a complete experience:
- rising pressure
- meaningful attack moments
- climax
- easing
- satisfying resolution

I already have:
- Packet system
- TrafficDirector (spawning + lifecycle)
- NetworkRuntime
- Command system (scan/block/etc)
- A working ScoreDirector (event-driven, timing-aware)

Phase 1 tasks:

1) First real attack plan
- needs to feel intentional and noticeable
- not just “more traffic”

2) `spawnattack <key>` debug command
- should trigger attack patterns using real spawn pipeline

3) Light run/level stubs
- NOT full system
- just enough to avoid rework

4) Autospawn tuning
- define role of ambient traffic vs attacks
- ability to scale or disable during phases

5) Keyword system integration into spawning
We previously defined this pipeline:

- rollBase
- rollClass
- rollKind
- rollInfectious
- rollInfectionType
- rollKeywords

Requirements:
- variable keyword count per packet
- weighted by class/kind
- minimal implementation (no overengineering)

What I want help with:
- Designing the FIRST attack plan (concrete, not abstract)
- Where attack logic should live (without overbuilding)
- How to cleanly hook `spawnattack` into current code
- How to integrate keyword rolling into TrafficDirector incrementally
- How to structure a minimal LevelDirector for phase-based flow

Important:
Do NOT jump to full architecture or data-driven systems.
Keep everything:
- small
- incremental
- directly testable in-game

I will share relevant code next.