# Uptime Design Notes – “Surfaces” & System Depth

## Core Insight
The game doesn’t need more mechanics—it needs more “surfaces”:
More places where the game can push back on the player.

---

## The 3 Core Surfaces

### 1. Traffic Surface
- Packets
- Routes
- Scanning
- Blocking

### 2. Infrastructure Surface
- Node failures
- Edge latency spikes
- Connection instability

### 3. Information Surface
- Mislabeling
- Scan interference
- Ghost packets

---

## Infection Design
- Delayed infection triggers
- Carrier packets
- Chain infection

---

## Event Types

### Physical Failures
- Cable cuts
- Power flickers

### Congestion
- Load spikes
- Queue overflow

### Tool Degradation
- Slower scans
- Command delay

---

## Design Principle
Do not add new verbs.
Change the rules temporarily.

---

## Example Events
- Edge failure
- Scan interference
- False confidence
- Latency surge
- Node brownout

---

## Final Goal
From:
Manage traffic

To:
Manage a failing system under pressure

---

## Next Steps
- Add 1 infrastructure event
- Add 1 information disruption
- Add 1 keyword-based packet
- Playtest

---

## Guiding Principle
Make differences more obvious before adding more systems
