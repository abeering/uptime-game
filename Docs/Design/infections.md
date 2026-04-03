# Infection Brainstorm (Design Notes)

## Core Premise
Infections are the **primary expression of threat** in the game.

Depth will come from:
- Multiple infections interacting
- Player tradeoffs in which infections to prioritize
- Increasing pressure on attention, not just mechanics

---

## 🧠 1. Information Warfare (Scan / Intel Disruption)

**Scrambler**
- Reduces scan confidence gain on node
- May cap scans at "Likely"

**Spoofer**
- Packets appear as incorrect class longer

**Echo**
- Creates phantom packets (visual clutter / noise)

**Obfuscator**
- Hides packet IDs intermittently

➡️ Enhances core scan gameplay loop

---

## 🌊 2. Flow Manipulation (Traffic Behavior)

**Floodgate**
- Increases packet throughput through node

**Redirector**
- Reroutes packets unpredictably

**Backpressure**
- Causes upstream packet bunching / bursts

**Desync Field**
- Adds jitter to packet movement

➡️ Makes network behavior less predictable

---

## 🧬 3. Propagation / Growth (Map Pressure)

**Spreader**
- Infects adjacent nodes over time

**Parasite**
- Jumps between nodes periodically

**Chain Infection**
- Moves to new node when cleaned

**Dormant Seed**
- Activates into another infection after delay

➡️ Introduces evolving board state

---

## ⚡ 4. Player Interaction Punishers

**Reactive**
- Triggers when player interacts (scan/block/etc)

**Fragile Core**
- Safe until blocked → then escalates

**Honeypot**
- Appears high threat but low impact

**Retaliator**
- Punishes cleaning (burst, latency spike, etc)

➡️ Adds risk to player decisions

---

## 🏗️ 5. Infrastructure Damage

**Corruptor**
- Slows scan speed

**Leak**
- Allows threats to bypass blocks

**Decay**
- Gradually increases latency over time

**Firewall Flip**
- Blocks benign packets instead

➡️ Creates long-term degradation

---

## 🎯 6. Targeted Behavior

**Hunter**
- Targets specific node

**Priority Hijacker**
- Converts benign → threat

**Sinkhole**
- Absorbs packets (good or bad)

➡️ Adds intentionality to threats

---

## 🧩 7. Combo / Stack-Oriented

**Amplifier**
- Boosts other infections on node

**Stabilizer**
- Harder to remove infection

**Conduit**
- Spreads effects to connected nodes

➡️ Enables multi-infection depth

---

## 🔥 Recommended Early Infections

Start with:

1. Spawner (expand parameters)
2. Throttle
3. Scrambler
4. Spreader
5. Reactive

These provide:
- Time pressure
- Information uncertainty
- Spatial growth
- Decision risk

---

## Key Design Insight

Infections are sufficient **if they interact**.

Depth comes from:
- Multiple infections per node
- Conflicting priorities
- Emergent combinations

NOT from:
- Large number of isolated infection types

---

## Future Direction

- Enable multi-infection nodes (controlled initially)
- Introduce infection parameterization
- Design around interaction, not quantity