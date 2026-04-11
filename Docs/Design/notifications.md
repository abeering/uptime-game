# Chat Notification / Comms System Design (Uptime)

## Core Philosophy

This system is NOT flavor text.

It is:
> A narrative feedback system that converts network state into human consequences.

It acts as:
- Emotional amplifier
- Soft tutorial layer
- Pressure system
- World-building layer

Core loop extension:
- Detect → Decide → Act
- Interpret → React → Pressure

---

# System Architecture (Conceptual)

## 1. Event Sources

### Scripted Events
- Authored, deterministic or semi-deterministic
- Triggered by LevelDirector / RunDirector

Examples:
- CEO sale kickoff → traffic surge
- Datacenter accident → node offline
- Villain scripted attack → special spawn

---

### Reactive Triggers
- System-driven
- Based on thresholds + cooldowns

Examples:
- Latency > threshold → complaints
- Infection present → ops warning
- Packet delays → service degradation

---

### Evaluative Checkpoints
- Occur at specific ticks (e.g. 1, 40, 60)
- Evaluate player performance snapshot

Outputs:
- Praise
- Criticism
- Villain commentary

---

## 2. Message Resolution Flow

Event/Trigger → ChatCueType → Message Pool → Speaker → Notification

Key rule:
> Events request meaning, not specific lines

---

# Message System

## ChatCueType (Semantic Layer)

Examples:
- VillainHardPacket
- VillainTaunt
- BenignTrafficSurge
- NodeFailure
- InfectionDetected
- LatencyWarning
- CEOConcern
- OpsAlert

Purpose:
- Decouple gameplay from content
- Enable scalable content pools

---

## Message Pool

Collection of authored lines grouped by:
- cueType
- speaker
- optional tags

Each message:
- text
- weight
- optional tags (mutating, worm, api, etc.)
- optional cooldown
- optional escalation level

Key idea:
> Many lines per meaning, not one

---

## Speaker / Character Pool

RunDirector generates a company per run.

Each character:
- id
- displayName
- role (CEO, Analyst, Support, etc.)
- tone profile
- avatar

### Example Roles

CEO:
- Dramatic
- Money-focused
- Escalates quickly

Analyst:
- Technical
- Calm
- Accurate

Support:
- Chaotic
- Anecdotal
- Overreacts inconsistently

Employee:
- Confused
- Irrelevant but funny

Villain:
- Taunting
- Predictive
- Personal

---

## Tone & Personality Rules

Same event → different interpretations

Example:
Latency spike

CEO:
"Are we losing revenue right now?"

Analyst:
"Latency increasing on api node"

Support:
"Customers saying checkout is broken"

Villain:
"You can’t keep up"

---

# Escalation System

## Core Idea

Each problem tracks its own escalation level.

Examples:
- Latency
- Infection
- Node Failure

---

## Escalation Levels

0 = None  
1 = Mild  
2 = Noticeable  
3 = Severe  
4 = Critical  

---

## Example: Latency

Level 1:
"Site feels slow"

Level 2:
"Customers noticing delays"

Level 3:
"Checkout lagging badly"

Level 4:
"We are losing orders"

---

## De-escalation

Messages reflect recovery:
- "Stabilizing"
- "Looks better now"

---

## Escalation Rules

- Based on thresholds over time (not single events)
- Shared per problem type
- Drives tone + message selection

---

# Message Priority System

## Priority Levels

Critical:
- Node down
- Major infection

Event:
- Scripted narrative beats

Warning:
- Performance issues

Flavor:
- Low impact chatter

---

## Rules

- Higher priority suppresses lower
- Limit messages per tick
- Cooldown per concept (not per line)

---

# Message Container / Feed

## Behavior

- Messages stack in a feed
- Max visible (4–6)
- Fade out over time
- High priority linger longer

---

## Modes

Normal:
- Slow trickle

Crisis:
- Rapid stacking
- Overlapping messages

Recovery:
- Gradual quieting

---

# Freakout Moments (Peak System)

Triggered when:
- Multiple escalations active
- Critical thresholds exceeded

Behavior:
- Relax suppression rules
- Allow multi-message bursts
- Multiple speakers fire

Result:
> “Entire company reacting”

---

# Timing & Delivery

## Delayed Reactions

Messages fire 2–5 ticks after cause

Effect:
- Feels human
- Reduces spam

---

## Message Timing Types

Pre-event:
- Foreshadowing

During:
- Pressure

Post-event:
- Reflection

---

# Advanced Design Concepts

## Contradictory Messaging

Different roles disagree

Example:
Analyst: "System stable"
Support: "Customers still complaining"

---

## Silence as Signal

No messages = tension

Examples:
- Before major attack
- After failure spike

---

## Overreaction Curves

Different roles escalate at different speeds

CEO: fast  
Analyst: slow  
Support: chaotic  

---

## Escalation Memory

Repeated issues escalate tone

Example progression:
1. "Site slow"
2. "Customers complaining"
3. "Losing orders"
4. "This is unacceptable"

---

## Spatial Awareness

Messages reference nodes:
- "API node slow"
- "Cache acting up"

---

## Misdirection (Limited Use)

Some characters give wrong info

Used sparingly:
- Early game teaching
- Specific roles

---

# Design Principles

Messages must:
- Inform
- Reinforce
- Pressure
- Characterize

Avoid:
- Noise
- Redundancy
- Irrelevance

---

# System Goals

- Make network feel alive
- Teach without tutorials
- Create emotional stakes
- Scale with content

---

# Minimal Implementation Plan

Start with:

## Systems
- ChatCueType enum
- Message pool
- Speaker pool
- ChatDirector

## Content
- 3 speakers (CEO, Analyst, Villain)
- 6 cue types
- 15–20 total lines

## Escalation
- 2–3 tracks (latency, infection, node)

---

# Final Summary

You are building:

> A pressure amplifier that turns mechanical state into emotional experience.

This system provides:
- Narrative
- Feedback
- Humor
- Tension

With minimal mechanical overhead but massive impact.
