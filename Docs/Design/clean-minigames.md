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
