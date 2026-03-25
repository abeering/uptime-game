# Scan Timing Baseline (Reference Model)

## Reference Edge

Use a “medium” edge as the primary tuning anchor:

- `lengthSteps = 10`
- `latency = 2`
- `ticksPerStep = latency`
- **Total traversal time ≈ 20 ticks**

This edge represents a “typical” packet traversal for baseline gameplay feel.

---

## Core Design Principle

> Scan timing should be comparable to packet traversal time.

This creates meaningful tension:

- Can the player learn enough **before the packet reaches its next node**?
- Should they **commit to this scan** or switch targets?

---

## Target Scan Milestones (Medium Packet)

For a **medium-difficulty packet**, with **1 active scan**:

- **Probable:** ~7–8 ticks  
- **Likely:** ~18–20 ticks  
- **Confirmed:** ~28–32 ticks  

---

## Intended Gameplay Feel

- **Probable (early):**
  - Fast feedback
  - Encourages engagement
  - Still risky

- **Likely (midpoint):**
  - Arrives around edge traversal time
  - Primary decision point
  - “Good enough to act, but not guaranteed”

- **Confirmed (late):**
  - Requires extended commitment
  - Full certainty is not free

---

## Difficulty Scaling (First Pass)

Relative to the 20-tick traversal baseline:

### Easy Packet
- Likely: ~12–15 ticks
- Confirmed: ~20–24 ticks

### Medium Packet
- Likely: ~18–20 ticks
- Confirmed: ~28–32 ticks

### Hard Packet
- Likely: ~24–28 ticks
- Confirmed: ~38–45 ticks

---

## Tuning Guideline

Use a scan-duration knob such as:

- `baseScanDurationTicks`

And tune so that:

> A medium packet reaches **Likely** in ~1× traversal time (≈20 ticks)

---

## Key Insight

- **Traversal time defines pressure**
- **Scan timing defines knowledge**
- The overlap between them creates gameplay tension

---

## Future Extensions (Not Phase 1)

- Non-linear scan curves (fast early, slow late)
- Difficulty-based curve shaping
- Pre-confirmation uncertainty / misclassification

---