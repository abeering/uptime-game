# Ops Panel + Intel System Ideation

## 1. Ops Panel Cleanup

### Sections

**ACTIVE BLOCKS**
- Represents *armed defenses*, not outcomes
- Needs clear, cancellable identifiers

Example:
ACTIVE BLOCKS
#1  g8 @ api   armed
#2  q3 @ web   armed

- `#1` chosen for clarity + avoids collision with packet IDs
- Supports: `cancel #1`

---

**KNOWN THREATS**
- No artificial IDs (T1, T2 removed)
- First column = packet ID (matches command language)

Example:
KNOWN THREATS
g8  CONFIRMED  WORM
    kw=infectious,evasive
    inf=blackout
    src=2.2.2.1  dest=cache
    blocked @ api

### Display Rules

**Header line**
- packet id
- scan stage
- best known identity (class → kind)

**Detail lines (only if known)**
- kw=...
- inf=...
- src=... dest=... (trace)
- blocked @ ...

---

## 2. Scan vs Trace

### Scan
Reveals:
- stage
- class
- kind
- keywords
- infection

→ answers: “what is this?”

---

### Trace
Reveals:
- source IP
- destination node

→ answers: “what pattern is this part of?”

---

### Design Intent

Scan = classification  
Trace = context  
Block = action  

---

## 3. Why Trace Matters

Trace enables strategic play, not just info:

### A. Rule-based blocking
block ip=2.2.2.1  
block dest=cache  

→ shifts from reactive → systemic defense

---

### B. Attack pattern recognition
Multiple packets share:
src=2.2.2.1 dest=cache  

→ player detects coordinated attack

---

### C. Target prioritization
- Identify high-value nodes under attack
- Preemptively defend

## 5. Command Acceleration ("Focus")

### Problem
Need a way to:
- resolve actions faster
- without breaking concurrent system

### Solution
Inline command modifier, not separate ability

---

### Syntax (recommended)
scan g8!  
trace g8!  

Meaning:
allocate more resources to this process

NOT:
run before others

---

### Behavior
- increases rate of progress (e.g. 2–3x)
- applies to:
  - scan
  - trace
  - (optionally block later)

---

### Design Intent

Represents:
- bandwidth allocation
- CPU/resource focus
- urgency under pressure

---

### Player Decision

“Which process do I accelerate right now?”

Creates:
- micro-priority decisions
- tempo control
- skill expression

---

## 6. Core Gameplay Triangle

Scan → Identity  
Trace → Context  
Block → Action  
! modifier → Urgency  

---

## 7. Key Principles

- Show only known intel
- Avoid fake identifiers
- Keep commands aligned with display
- Prefer system-like language, not gamey verbs
- Trace must unlock new capabilities, not just info
- Acceleration = resource allocation, not priority queueing
