# Scan Difficulty & Intel Progression — Future Design Notes

## Current Model (Baseline)

* `scanDifficulty` controls:

  * Confidence gain per tick (scan speed)
  * Early class accuracy (via `RollReportedClass`)
* Scan stages are fixed:

  * Probable: 20%
  * Likely: 55%
  * Confirmed: 100%

### Implication

All packets share the same **progression shape**, only differing in speed.

---

## Design Goal

Create packets that feel:

* Fast and obvious (low friction traffic)
* Slow but straightforward
* Fast but deceptive
* Slow and deeply ambiguous

→ “Tricky” should mean **behaviorally different**, not just slower.

---

## Problem with Current Approach

Single scalar (`scanDifficulty`) cannot express:

* Deception vs clarity
* Early vs late certainty
* Stage-specific friction (e.g., stuck at Likely)

Also:

* Random threshold fuzz (e.g., 18–24%) adds noise, not meaning

---

## Proposed Direction (Minimal Expansion)

### 1. Keep `scanDifficulty`

* Controls **scan pacing only**
* “How fast does confidence increase?”

---

### 2. Introduce `scanProfile` (lightweight)

Example:

```csharp
public enum ScanProfile
{
    Standard,
    FastStart,
    SlowStart,
    StickyLikely,
    Deceptive
}
```

---

### 3. Profiles modify stage thresholds

| Profile      | Probable | Likely | Behavior                        |
| ------------ | -------- | ------ | ------------------------------- |
| Standard     | 20       | 55     | baseline                        |
| FastStart    | 12       | 50     | quick early read                |
| SlowStart    | 30       | 60     | delayed initial clarity         |
| StickyLikely | 20       | 70     | lingers in “uncertain”          |
| Deceptive    | 15       | 75     | early signal, slow confirmation |

---

### 4. Future Extensions (Optional)

Profiles may also control:

* Class misclassification likelihood
* Stage-specific confidence gain modifiers
* Reveal order (e.g., delayed kind/infection)
* Confidence instability (backslides / wobble)

---

## Mental Model

* **scanDifficulty = speed**
* **scanProfile = shape**

These should remain independent.

---

## Design Principle

Avoid:

> “Hard packets are just slower”

Aim for:

> “Packets behave differently under observation”

---

## Content Strategy

Use distribution:

* Majority: easy, fast, low-noise packets
* Some: moderate friction
* Few: high-complexity / deceptive packets

This preserves:

* game pace
* cognitive load balance
* meaningful decision spikes

---

## Status

⏸ Deferred for now
→ Revisit after trace system + encounter pacing are stable
