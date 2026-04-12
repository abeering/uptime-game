# UPGRADE CATEGORIES – SYSTEM DESIGN

## OVERVIEW

This document defines the high-level categories of upgrades that shape run identity.

Upgrades should:
- Change behavior (not just numbers)
- Introduce tradeoffs
- Alter decision-making
- Reinforce different playstyles

These categories are intentionally broad and system-focused, not implementation-specific.

---

# PRIMARY UPGRADE CATEGORIES

## 1. Visibility / Intelligence

> How much the player knows, how early they know it, and how reliable that knowledge is.

Supports:
- Information-driven play
- Precision decision-making

### Effects May Include:
- Earlier reveal of packet traits
- More accurate scan results
- Passive intel from nodes
- Logs, tracebacks, anomaly detection
- Route or intent visibility

### Tradeoffs:
- Slower system performance
- Higher input or attention cost

---

## 2. Traffic Control / Throughput

> How traffic moves through the network.

Supports:
- Flow-based play
- System stability over precision

### Effects May Include:
- Speed adjustments (faster/slower traffic)
- Routing changes
- Traffic prioritization
- Buffers, queues, lanes
- Congestion mitigation

### Tradeoffs:
- Reduced visibility
- Less precise control over individual threats

---

## 3. Interdiction / Enforcement

> How effectively the player can stop or eliminate threats.

Supports:
- Aggressive, decisive play

### Effects May Include:
- Stronger block outcomes
- Wider or faster block windows
- Class-specific suppression
- More reliable threat elimination
- Precision targeting improvements

### Tradeoffs:
- Collateral damage
- Overblocking
- System bottlenecks

---

## 4. Containment / Damage Control

> How the system limits spread and cascading failure.

Supports:
- Defensive, stability-focused play
- Accepting that threats will get through

### Effects May Include:
- Isolation of compromised nodes
- Segmentation of the network
- Reduced spread from infections
- Localized failure zones
- Sacrificial systems

### Tradeoffs:
- Slower overall system
- Reduced efficiency
- Increased complexity

---

## 5. Automation / Policy

> What the system can handle without direct player input.

Supports:
- Strategic delegation
- Reduced manual load

### Effects May Include:
- Auto-handling simple cases
- Conditional triggers
- Node-specific policies
- Pattern-based responses
- Passive command augmentation

### Tradeoffs:
- Reduced flexibility
- Potential misfires
- Less precise control

---

## 6. Command Layer / Operator Tools

> How the player interacts with the system via commands.

Supports:
- Faster execution
- Improved input efficiency

### Effects May Include:
- Shorter command aliases
- Chained or batched commands
- Command modifiers
- Reduced typing overhead
- Improved feedback or confirmation

### Tradeoffs:
- Improves execution but not system strength
- May not solve underlying system weaknesses

---

# SECONDARY / SUPPORTING CATEGORIES

## 7. Recovery / Resilience

> How the system recovers from failure.

### Effects May Include:
- Faster cleanup
- Node repair
- Traffic recovery
- Error forgiveness

### Tradeoffs:
- Reactive rather than preventative
- Can fall behind under heavy pressure

---

## 8. Temporary Infrastructure / Active Deployables

> Short-lived, high-impact tools for crisis situations.

### Effects May Include:
- Temporary firewall nodes
- Rapid scan hubs
- Emergency rerouting
- Quarantine zones
- Priority traffic lanes

### Tradeoffs:
- Limited duration
- High cost
- Requires precise timing

---

## 9. Business-Specific Adaptation

> Upgrades that are especially effective in a specific run context.

### Effects May Include:
- Priority traffic boosts
- Anti-surge tools
- Critical node protection
- Targeted threat counters

### Tradeoffs:
- Narrow applicability
- Reduced usefulness in other runs

---

# DESIGN PRINCIPLES

## 1. Categories Represent Questions

Each category answers a core question:

- Visibility → What do I know?
- Traffic → How does the system flow?
- Interdiction → How do I stop problems?
- Containment → How do I limit damage?
- Automation → What can the system do for me?
- Command → How do I execute faster?

---

## 2. Identity Through Specialization

> A run should emphasize some categories and neglect others.

This creates:
- Strengths
- Weaknesses
- Meaningful tradeoffs

---

## 3. Avoid Universal Power

No category should:
- Solve all problems
- Be strictly better than others

---

## 4. Upgrades Must Be Transformative

Avoid:
- Flat stat increases

Prefer:
- Behavioral changes
- System-level impact
- New decision patterns

---

## 5. Categories Should Interact

The most interesting runs will:
- Combine 2–3 categories
- Create synergy and tension

Example:
- High throughput + low visibility
- Strong automation + weak precision

---

# SUMMARY

> Upgrade categories define how the player shapes the system, not just how they react to it.

They are the foundation for:
- Run identity
- Strategic diversity
- Replayability


# random notes 

- what if upgrading is like a puzzle - not hard, not game-losing/winning, but instead you just have to do a sort of "terminal / hacker puzzle" (<30s) in making your decision 
- what if some upgrades were not just "upgrade node" - but were "create programmable" - basically allow you to tune / specify ranges / packet info etc on a node to do "automation" like tasks 
-- need more ideas on automation 