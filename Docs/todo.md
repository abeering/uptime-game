# Uptime – TODO

## 1. Scan System Overhaul

### Scan Difficulty + Progression
- Replace fixed `confidencePercent → stage` mapping
- Introduce `scanDifficulty`-driven progression curves
  - Faster/slower growth rates
  - Potential non-linear curves per packet
- Maintain stage labels:
  - Unknown → Probable → Likely → Confirmed
- Allow stage thresholds to be *fuzzy* (not strictly tied to %)

### Visual Representation
- Replace color lerp (grey → identity color) with:
  - Stage-based color states
  - Optional confidence overlay (secondary signal)
- Ensure packet color reflects **perceived identity**, not raw %

---

## 2. Console + Operations Panel Clarity

### Operations Panel
- Standardize formatting of operations:
  - Scan progress: `[■□□] 25%`
  - Clear stage labeling
  - Highlight "at-risk" scan (replacement indicator)
- Move **Recents** below active scans (secondary importance)
- Ensure completed operations linger cleanly, then expire

### Console Output
- Improve readability of command results
- Normalize phrasing across commands (`scan`, `block`, etc.)
- Reduce ambiguity in system feedback (especially scan results)

---

## 3. Keyword System Expansion

### Keyword Design
- Add new keywords (brainstorm + implement)
- Ensure keywords act as **modifiers**, not primary identity
- Clarify:
  - Which keywords are visible via scan
  - Which are hidden until deeper inspection

### TrafficDirector Integration
- Formalize spawn pipeline:
  - `rollBase`
  - `rollClass` (Benign / Threat / Priority)
  - `rollKind` (Virus, Worm, etc.)
  - `rollInfectious`
  - `rollInfectionType`
  - `rollKeywords`
- Support:
  - Variable number of keywords per packet
  - Weighted distributions per threat type


## 6. Packet Spawn + Flow Tuning

- Refine spawn timing:
  - Interval ramp
  - Jitter
- Explore:
  - Overtaking behavior (faster packets catching slower ones)
- Balance:
  - Readability vs chaos

---

## 7. Attack Patterns (Design Stub)

- Define "Attack Plans":
  - Scripted sequences (e.g., DDoS waves, mixed threats)
- Support:
  - Time-based spawning windows
  - Coordinated behaviors
- Keep as data-driven (no heavy implementation yet)


## 11. Nice-to-Have / Later

- `inspect` command (deeper packet info)
- Help system for keywords (`help <keyword>`)
- Node state effects (scan speed, spawning, etc.)
- More meaningful Priority packet interactions