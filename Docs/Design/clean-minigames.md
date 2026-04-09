# Clean() Minigames Design

## Overview

`clean(node)` is not just a command — it is a **focus mechanic**.

When the player cleans a node:
- They **stop multitasking**
- They **commit attention**
- The game **continues running**
- They must complete a **short, skill-based minigame**

This creates tension:
> “Can I afford to look away right now?”

---

## Core Design Pillars

A good clean minigame should:

- ⌨️ Keyboard only (no mouse)
- ⚡ Short (2–6 seconds)
- 🧠 Require thinking + reacting
- 👁️ Instantly understandable
- ⚖️ Has real failure risk
- 🔀 Affected by infections

---

## Player Flow

clean A  
→ Node A is highlighted  
→ Overlay appears near node  
→ Minigame begins  
→ Player completes or fails  
→ Result applied  
→ Return to game  

---

## Minigames (ELI5)

### 1. Signal Alignment
You see a bar moving back and forth with a target zone.

Your job: press SPACE when the bar is inside the zone.

Goal: hit it correctly several times.

Simple: press at the right time.

---

### 2. Burst Typing
You see: TYPE: A9K3

Your job: type A9K3 quickly.

Goal: complete a few sequences fast.

Simple: type what you see.

---

### 3. Noise Filtering
You see a stream like: A x B x x A

Rule: only press on A or B.

Your job: press when correct, do nothing when wrong.

Simple: only react to good signals.

---

### 4. Pattern Match
You see a target pattern and options.

Your job: press 1–3 to match it.

Simple: pick the matching picture.

---

### 5. Sequence Memory
You see: A → D → S (then it disappears)

Your job: type it from memory.

Simple: remember then type.

---

### 6. Directional Routing
You see: ↑ → ↓ ←

Your job: press those directions.

Simple: follow the arrows.

---

### 7. Interrupt Spam
Keys light up.

Your job: press them quickly.

Simple: hit what lights up.

---

### 8. Hold Stabilization
You see a moving zone.

Your job: hold a key to stay inside it.

Simple: keep it in the zone.

---

## Difficulty

Easy:
- slow, simple

Medium:
- faster, small tricks

Hard:
- tight timing, traps

---

## Infection Modifiers

Blackout:
- hides UI

Throttler:
- delays input

Spawner:
- adds fake signals

---

## Multi-Infection

Either:
- choose infection

Or:
- others interfere with minigame

---

## Failure

- infection stays or worsens
- node locks
- threats spawn

---

## Success

- infection removed
- node restored

---

## Key Insight

Main game = multitasking  
Cleaning = forced focus  

That contrast creates tension.

---

## V1 Recommendation

Start with:
- Signal Alignment
- Burst Typing
- Noise Filtering


# Clean() Minigames Design

## Overview

`clean(node)` is not just a command — it is a **focus mechanic**.

When the player cleans a node:
- They stop multitasking
- They commit attention
- The game continues running
- They must complete a short, skill-based minigame

This creates tension:
“Can I afford to look away right now?”

---

## Core Design Pillars

A good clean minigame should:

- Keyboard only (no mouse)
- Short (2–10 seconds)
- Require thinking + reacting
- Instantly understandable
- Have real failure risk
- Be affected by infections
- Feel like using real “tools” (terminal / system / network)

---

## Player Flow

clean A  
→ Node A is highlighted  
→ Overlay appears near node / over network panel  
→ Clean Splash Screen (auto, 0.5–1.5s)  
→ Minigame begins  
→ Player completes or fails  
→ Result applied  
→ Return to game  

Notes:
- Overlay obscures ~50–75% of network view
- Avoid fixed positioning (random offset near node)
- Console + ops panel remain visible

---

## Clean Splash Screen

Short automated terminal sequence before gameplay.

Purpose:
- Flavor (“connecting to node”)
- Signals minigame type
- Signals infection modifiers
- Gives player ~1 second prep time

Example:

> ssh node-A.local
connecting...
auth ok
mounting /proc...
scanning memory...

detected anomalies:
- HIGH CPU PROCESS
- SIGNAL NOISE
- INPUT LATENCY

launching cleanup module...

Signal Mapping:

HIGH CPU → Process Killer  
SIGNAL NOISE → Noise Filter  
BUFFER CORRUPTION → Hex Scrubber  
DESYNC → Timing game  
INPUT LATENCY → Throttler  
GHOST SIGNALS → Spawner  
VISIBILITY LOSS → Blackout  

---

## Minigames

### Packet Sniffer
Watch traffic stream and press key on [THR] lines only.

### Hex Scrubber
Remove corrupted bytes (XX) as cursor passes.

### Process Killer
Identify highest CPU process and type its PID.

### Signal Alignment
Press key when moving bar enters target zone.

### Burst Typing
Type displayed sequence quickly.

### Noise Filtering
React only to valid signals, ignore noise.

### Command Repair
Fix broken command string.

### Log Diver
Press key when matching log lines appear.

---

## Difficulty

Easy:
- Slow, simple

Medium:
- Faster, minor tricks

Hard:
- Tight timing, traps, multi-step

---

## Infection Modifiers

Blackout:
- Hides UI / flicker

Throttler:
- Input delay

Spawner:
- Fake signals / noise

---

## Multi-Infection

- Player selects infection OR
- Other infections interfere (preferred)

---

## Failure

- Infection worsens
- Node locks
- Threats spawn

---

## Success

- Infection removed
- Node restored

---

## Key Insight

Main game = multitasking  
Cleaning = forced focus + blindness  

---

## V1 Recommendation

Start with:
- Packet Sniffer
- Hex Scrubber
- Process Killer

---

## Future Expansion

- More tools
- Modifier combinations
- Multi-stage cleans
- Chained minigames


# Clean Minigame Pillars

## Purpose

The clean() system is not a collection of disconnected minigames.
It is a **small set of cognitive interaction pillars** that can be:

* reused
* reskinned
* scaled horizontally

Each pillar represents a **distinct way the player thinks under pressure**.

---

## Core Pillars

### 1. Identify

**Pattern:**
Inspect → find anomaly → type it

**Player Experience:**
"Something is wrong here."

**Examples:**

* Highest CPU process
* Corrupted hex values
* Duplicate ports
* Missing route entries

**Cognitive Load:**
Visual comparison

**Notes:**

* Backbone of the system
* Fastest to understand
* Most scalable via flavor

---

### 2. Filter

**Pattern:**
Stream → react only to valid signals

**Player Experience:**
"Only act on the right things."

**Examples:**

* React only to valid symbols
* Ignore noise in a signal stream
* Accept/reject entries

**Cognitive Load:**
Recognition + inhibition

**Notes:**

* Continuous, not snapshot
* Failure often comes from false positives

---

### 3. Repair

**Pattern:**
See corrupted structure → fix it

**Player Experience:**
"This is broken. Fix it."

**Examples:**

* Fix a command string
* Restore missing characters
* Repair malformed data

**Cognitive Load:**
Pattern completion

**Constraints:**

* The correct answer must feel inevitable
* Avoid ambiguity or multiple valid solutions

---

### 4. Memory

**Pattern:**
Observe → hide → reproduce

**Player Experience:**
"Remember this."

**Examples:**

* Key sequences
* Short strings
* Ordered inputs

**Cognitive Load:**
Short-term memory

**Notes:**

* Forces player to disengage from main game
* Keep duration short

---

### 5. Timing

**Pattern:**
Continuous motion → act at correct moment

**Player Experience:**
"Now."

**Examples:**

* Hit target zone
* Align markers
* Stop at threshold

**Cognitive Load:**
Motor timing

**Notes:**

* Introduces physical tension
* Complements cognitive games

---

### 6. Burst

**Pattern:**
Perform simple action rapidly

**Player Experience:**
"Go fast."

**Examples:**

* Rapid key presses
* Fast typing bursts
* Repeated inputs

**Cognitive Load:**
Speed + accuracy

**Notes:**

* Creates panic pressure
* Good contrast to slower games

---

## Design Principles

### 1. Visual Clarity First

The player should infer the rule from the layout, not instructions.

Bad:

* Requires reading explanation

Good:

* The anomaly is visually obvious

---

### 2. One Cognitive Verb Per Game

Each minigame should test **one thing only**.

Avoid mixing:

* memory + timing
* repair + filtering

---

### 3. Answer Is Already Visible

The player should extract, not invent.

Good:

* Type PID shown on screen

Bad:

* Derive a value through multiple transformations

---

### 4. Prompt Labels Action (Not Rules)

Prompt should be minimal:

Examples:

* terminate >
* scrub >
* repair >

The screen teaches the rule.

---

### 5. Difficulty via Perception, Not Complexity

Scale difficulty by:

* density
* noise
* similarity between options
* speed pressure

Not by:

* adding rules
* increasing instructions

---

## System Model

Minigames are defined by:

* Pillar (Identify, Repair, etc.)
* Flavor (tcpdump, hex, routing, etc.)
* Difficulty

The pillar defines behavior.
The flavor defines presentation.

---

## Development Strategy

Start with:

1. Identify (existing)
2. Repair
3. One non-text pillar (Memory or Timing)

Then expand horizontally by adding flavors within each pillar.

---

## Key Insight

You are not building many games.

You are building a **small vocabulary of actions** that can be expressed through many technical interfaces.
