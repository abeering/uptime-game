# Attack Surfaces & System Layers (Uptime Design Notes)

## Core Principle
Nothing happens without a visible cause. Each system must be:
- Telegraphable
- Predictable
- Interruptible

---

## 1. Traffic (Packets)
**Fiction:** Standard network traffic (benign, priority, threats)  
**Surface Type:** Movement  

**Behavior:**
- Moves along edges
- Interacted with via commands (scan, block, boost)

**Player Pressure:**
- Triage and prioritization

---

## 2. Unauthorized Tunnels (Ingress Layer)
**Fiction:** Hacker establishes direct access into the network  

**Telegraph:**
- Small “ghost node” appears near a real node
- Glitchy/dashed connection
- INTEL: Unauthorized tunnel detected @ NODE

**Behavior:**
- Countdown timer (e.g., 5 ticks)
- On completion:
  - infect node
  - spawn packets
  - apply node effect

**Interaction:**
- block tunnel@node
- clean node
- throttle slows timer

**Player Pressure:**
- Prevent entry before it completes

---

## 3. Control Plane / Command Injection (Execution Layer)
**Fiction:** Attacker is issuing commands using valid or stolen access  

**Telegraph:**
- Node shows “executing command” state
- INTEL: unauthorized command issued @ NODE

**Behavior:**
- After delay:
  - disables node
  - reroutes traffic
  - opens tunnel

**Player Pressure:**
- Interrupt execution before completion

---

## 4. Delayed Payloads (Time-Based Threats)
**Fiction:** Malicious code was planted earlier  

**Telegraph:**
- Countdown indicator on node
- INTEL: delayed payload detected @ NODE

**Behavior:**
- Triggers after timer:
  - infection
  - burst traffic
  - shutdown

**Player Pressure:**
- Decide whether to handle now or accept impact

---

## 5. Signal / Broadcast Effects (Area Influence)
**Fiction:** Node emits interference or malicious signaling  

**Telegraph:**
- Pulsing wave effect from node
- INTEL: anomalous signal detected

**Behavior:**
- Affects nearby:
  - packet speed
  - scan accuracy
  - latency

**Player Pressure:**
- Stop source vs mitigate effects

---

## 6. Resource Drain / Load Attacks (State Pressure)
**Fiction:** System resources are being consumed  

**Telegraph:**
- Node “heats up”
- INTEL: abnormal load detected

**Behavior:**
- Gradual degradation:
  - latency increase
  - packet loss
  - command delays

**Player Pressure:**
- Manage long-term degradation

---

## 7. Topology Manipulation (Network Structure)
**Fiction:** Network routing is being altered  

**Telegraph:**
- Edges flicker/change
- INTEL: routing anomaly detected

**Behavior:**
- Adds/removes/modifies connections

**Player Pressure:**
- Adapt to changing network

---

## 8. False Intel / Deception
**Fiction:** Attacker is corrupting system data  

**Telegraph:**
- Flickering or inconsistent info
- INTEL: data integrity compromised

**Behavior:**
- Incorrect scan results
- Hidden threats

**Player Pressure:**
- Question information reliability

---

## 9. Internal Actors (Rogue Processes)
**Fiction:** Malicious process operating inside network  

**Telegraph:**
- Intermittent node activity
- INTEL: unknown internal process

**Behavior:**
- Periodically:
  - spawns packets
  - modifies state

**Player Pressure:**
- Identify and eliminate source

---

## 10. Global Conditions (System-Wide Effects)
**Fiction:** Entire system is under stress  

**Telegraph:**
- UI tint / system message

**Behavior:**
- Global modifiers:
  - slower scans
  - faster threats
  - reduced capacity

**Player Pressure:**
- Operate under constraint

---

## Recommended Core Set (Keep It Tight)

Start with:
- Packets (Traffic)
- Tunnels (Ingress)
- Delayed Payloads (Time)
- Command Injection (Execution)

These four give:
- Movement
- Entry
- Timing
- Control

---

## Design Goal

Create **predictable weirdness**:
- Player may not fully understand systems
- But recognizes patterns and can react

