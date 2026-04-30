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

## 3. Traffic Direction

Add keyword weight bias to TrafficModifier
Add infection weight bias to TrafficModifier
Apply modifier bias during PacketKindProfile realization
Let events shape ambient traffic texture, not just class/kind frequency
Keep explicit authored event packets as a parallel first-class path


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
  - shows path of packet temporarily on network view if traced 
- Help system for keywords (`help <keyword>`)
- Node state effects (scan speed, spawning, etc.)
- More meaningful Priority packet interactions