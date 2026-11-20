# BUSINESS SYSTEM – RUN VARIATION DESIGN

## OVERVIEW

Businesses define the **context of a run**.

They are not just flavor—they:
- Create immediate identity
- Bias player strategy
- Introduce unique pressures and failure modes

---

# DESIGN GOALS

Each business should:

1. Create a distinct gameplay feel
2. Push the player toward specific upgrade categories
3. Introduce unique stress and failure conditions

---

# CORE DESIGN PATTERN

Each business defines:

## 1. What Matters Most
- Speed?
- Accuracy?
- Uptime?
- Containment?

## 2. What Fails Hardest
- Latency?
- Overload?
- Infection?
- Misclassification?

## 3. What Feels Stressful
- Chaos?
- Uncertainty?
- Fragility?
- Scale?

---

# COMPANY TRAFFIC RHYTHM / CONNECTION SHAPE (EXPLORATORY)

Companies can feel different because their **normal connections behave differently**, not just because they have different stakes, threats, or failure conditions. In Connections, the shape and rhythm of everyday traffic could be a major part of company identity.

Possible examples (not fixed rules):
- **E-commerce:** A varied but relatively straightforward mix of connections and operation arcs, like the current prototype baseline.
- **Financial system:** Lots of short-lived connections and rapid turnover, creating a different tempo for observation and response.
- **Data processing / enterprise workflows:** Fewer, longer-lived connections with many sequential operations per connection.
- **Media streaming:** Long-lived connections with sustained LOAD, where keeping flows running matters more than managing constant arrivals.

Knobs to explore include connection arrival rate and burstiness, lifetime, number and mix of operations, and LOAD / WORK patterns. The goal is for each company's network to have a distinct **visual and gameplay feel even before attacks begin**. These are examples to revisit, not settled archetypes or implementation requirements.

---

# BUSINESS CONCEPTS

## 1. High-Frequency Trading Firm
> “Milliseconds matter.”

### Characteristics:
- Extreme sensitivity to latency
- Priority traffic must not be delayed

### Pressure:
- Speed and timing precision

### Pushes Toward:
- Traffic Control / Throughput
- Command Layer

### Punishes:
- Slow systems
- Over-analysis

---

## 2. Social Media Platform
> “Chaos at scale.”

### Characteristics:
- Massive traffic volume
- Sudden unpredictable spikes
- Mixed traffic quality

### Pressure:
- Overload and congestion

### Pushes Toward:
- Traffic Control
- Automation

### Punishes:
- Manual micromanagement
- Precision-only builds

---

## 3. Hospital Network
> “Some things cannot fail.”

### Characteristics:
- Critical nodes must never go down
- Lower volume, high stakes

### Pressure:
- Catastrophic failure risk

### Pushes Toward:
- Containment
- Recovery / Resilience

### Punishes:
- Aggression
- Risk-taking

---

## 4. Cloud Hosting Provider
> “Everything depends on you.”

### Characteristics:
- Many services sharing infrastructure
- Cascading failure potential

### Pressure:
- System-wide instability

### Pushes Toward:
- Segmentation
- Traffic Control

### Punishes:
- Centralized weak points

---

## 5. Government Intelligence Agency
> “Know before you act.”

### Characteristics:
- Hidden and deceptive threats
- False signals

### Pressure:
- Uncertainty

### Pushes Toward:
- Visibility / Intelligence

### Punishes:
- Blind blocking
- Overreaction

---

## 6. E-Commerce Platform
> “Peak traffic at the worst time.”

### Characteristics:
- Predictable traffic surges
- Revenue tied to uptime

### Pressure:
- Burst scaling

### Pushes Toward:
- Traffic Control
- Temporary Infrastructure

### Punishes:
- Under-prepared systems

---

## 7. IoT Smart City Grid
> “Too many weak points.”

### Characteristics:
- Many distributed devices
- Large attack surface

### Pressure:
- Distributed failures

### Pushes Toward:
- Containment
- Automation

### Punishes:
- Centralized control

---

## 8. Media Streaming Service
> “Keep it smooth.”

### Characteristics:
- Continuous high bandwidth usage
- Quality-sensitive delivery

### Pressure:
- Sustained load

### Pushes Toward:
- Flow optimization

### Punishes:
- Bottlenecks

---

## 9. Cybersecurity Firm
> “You are the target.”

### Characteristics:
- Constant aggressive attacks
- High threat frequency

### Pressure:
- Continuous assault

### Pushes Toward:
- Interdiction
- Intelligence

### Punishes:
- Passive builds

---

## 10. Legacy Enterprise System
> “Everything is fragile.”

### Characteristics:
- Slow, brittle infrastructure
- Limited flexibility

### Pressure:
- System fragility

### Pushes Toward:
- Recovery / Resilience
- Careful control

### Punishes:
- High-speed aggressive play

---

# BALANCE RULE

Each business should:
- Favor 2–3 upgrade categories
- Punish 1–2 categories

This creates:
> Natural strategic direction without forcing builds

---

# DESIGN INSIGHT

> Businesses act as the **run archetype selector**

Before upgrades are chosen:
- The player is already nudged toward certain strategies

---

# SUMMARY TABLE

| Business | Gameplay Feel | Core Stress |
|----------|-------------|------------|
| Trading Firm | Fast, precise | Latency |
| Social Platform | Overwhelming | Volume |
| Hospital | Tense, careful | Critical failure |
| Intelligence | Uncertain | Ambiguity |
| Cloud | Systemic | Cascading failure |
| IoT Grid | Fragmented | Distributed risk |

---

# NEXT STEP

- Select 3 businesses
- Simulate full runs for each
- Validate that gameplay feels meaningfully different