# Connections — Threat Signals, Investigation, and Response (Working Ideas)

**Status:** Active design exploration captured October 22–23, 2026. **Not implemented, not an approved phase contract, and not an implementation task list.**  
**Scope:** The Connections prototype's proposed threat/information gameplay. Packet remains a separate prototype; similarly named commands there do not define these mechanics.

See [Connections Current State](Current_State.md) for what the scene actually does, [LOAD](connections-arcade-load-v1.md) for the current allocator, [operation arcs](connections-arcade-operation-arcs-v1.md), and [traffic generation](connections-arcade-traffic-v0.md). Shared [infection](../Design/infections.md) and [clean-minigame](../Design/clean-minigames.md) designs are relevant background, not installed Connections features.

## Central hypothesis

Connections could reveal their nature **through behavior over time**, rather than having a fixed, immediately obvious threat appearance. The player notices anomalies, chooses where to spend attention, acquires evidence, and decides whether to intervene early or wait for certainty.

Some connections are obviously disruptive; others initially look benign, lie dormant, or cause ambiguous symptoms. Some problems originate at the **connection**; others originate at an infected **node** and affect otherwise legitimate connections. Tools should give different kinds of information and tactical leverage, not collapse into a mandatory inspect-everything sequence.

## Proposed command and tool vocabulary

| Command or tool | Proposed role | Distinction |
|---|---|---|
| `inspect <connectionId>` | Immediate current-state snapshot. | Already exists; not a deep behavioral investigation. |
| `logs <connectionId>` | Tail/view more detailed activity from a logical connection. | Different from existing recorder commands `log save`, `log dir`, and `log folder`. |
| `kill <connectionId>` | Immediately terminate an active logical connection. | **Proposed rename** of today's `block <connectionId>` action; does not prevent recurrence. |
| `trace <connectionId>` | Spend simulation time identifying information about its source. | Vulnerable to the connection ending before the trace completes. |
| `block <source>` | Stop future traffic from a known/traced source. | A different action and target from current `block <connectionId>`. |
| `probe <edgeId>` | Deploy a temporary edge-mounted traffic-identification sensor. | Surveillance, not automatic blocking; `firewall` is reserved for another possible mechanic. |
| `scan <nodeId>` | Asynchronously check whether a node is infected. | Confirmation without occupying a clean minigame. |
| `clean <nodeId>` | Immediately attempt an infection-removal minigame. | Blind cleaning can be risky if there is no infection and thus no valid target. |

Names, arguments, timing, permissions, and exact outputs are provisional. The table does **not** say any new command is already in the Connections terminal.

## 1. Connection logs and delayed attacks

`logs c1` would expose a deeper stream of behavioral evidence than `inspect c1`. A connection could have ordinary claimed purpose, operations, and requested LOAD but still deserve scrutiny because it:

- survives much longer than similar benign connections;
- progresses through WORK strangely or inconsistently;
- shows access attempts, retries, unexpected file operations, or other red flags in its logs; or
- performs apparently normal operations before transitioning into a node-infecting operation near its end.

The goal is **evidence** rather than a log line that simply announces "MALICIOUS." It should be possible for a suspicious symptom to have a benign explanation, and for a covert threat to avoid obvious LOAD abuse. Whether logs are live-only, retain history, have noise, or reveal partial information remains open.

**Alternative diagnosis:** strange WORK progression might be caused by an already-infected node rather than a malicious connection. This could make `logs c1` and `scan n2` reasonable competing investigations of the same symptom.

## 2. SPIKING connection behavior

**Proposed keyword:** `SPIKING`.

Some threats request normal LOAD most of the time but produce **large, short-lived intermittent demand spikes**. For illustration only, a connection might ordinarily request 2 LOAD, jump briefly to 15–25, and fall back again. The frequency, duration, magnitude, and visibility of the keyword are not specified.

The player might notice a node's LOAD suddenly turn overloaded and recover, then need to identify which connection caused it. Other benign connections contending for that node could temporarily slow down during the spike. The key investigative signal is **timing**, not sustained high average demand.

Whether benign traffic can also spike, how warnings appear in `inspect` or `logs`, and whether a spike must happen before threat identification are open.

## 3. Kill, trace, and source block

### Kill

`kill c1` immediately destroys the currently active connection. It is intended to take over the **current** Connections behavior of `block c1`. Killing a connection does not, by itself, stop an attacker from sending another one.

### Trace

`trace c1` needs elapsed time to identify its source. The information might accumulate during tracing or appear on completion; neither reveal pattern is settled.

A capable hacker may notice the trace and deliberately **terminate the connection while it is being traced**. This frustrates identification and can trigger hacker-player repartee. The tactical choice becomes: kill a threat immediately, or preserve a dangerous connection long enough to trace its origin.

Open issues include partial trace results, whether the trace requires the connection to remain live throughout, whether interruption can be prevented, and how the attacker decides to respond.

### Block

`block <source>` would consume/use information obtained from tracing and prevent **future connection spawns** from that source. Its lifetime, scope (IP/origin/attacker identity), and implications for legitimate traffic sharing a source remain open.

Successful prevention must be **visible**. When blocked traffic attempts to arrive, show a tiny, short-lived connection emerging near the origin, recoiling or shrinking back toward the source, then disappearing. It should not become a real active connection or progress through the network. The command console also explicitly reports rejected attempts.

Conceptual output, not a command specification:

```text
BLOCK 192.0.2.41 ACTIVE
REJECTED incoming connection from 192.0.2.41
```

This is feedback about prevention, not an invitation to spawn and then instantly destroy real connections through the ordinary allocator.

## 4. Probe — temporary link surveillance

**Working name:** **Probe**. Reserve **firewall** for a possible separate, actual filtering/blocking tool.

A probe is a temporary traffic-identification process/device attached to an existing **physical link/edge**, not a permanent special-purpose node. Its fiction is closer to a network tap, probe, or IDS sensor than a firewall.

- Place a probe on an edge, e.g., conceptually `probe e3`.
- It runs for a limited active duration, then expires.
- It recharges/has a cooldown before reuse.
- Progression might give the player roughly **1–4 probes** or deployable probe slots across a run.
- While active, it marks or colors the **threat potential** of connections traversing that edge, both in the network view and in `inspect`.
- Some threats can **evade probe detection**.

A probe does not itself imply automatic rejection or a guarantee that anything unmarked is benign. Its particularly interesting payoff is **early warning**: it might identify a threat early enough for the player to finish a trace *before* that connection reaches an infecting operation, and then block the origin of later attacks.

Open issues: false positives, confidence representation, timing of classification, multiple simultaneous deployments, what exact "1–4 probes" means, and interaction with connections that traverse several edges.

## 5. Covert node infections, scan, and blind clean

Some infections should be **overt**: strong node color changes, obvious warnings, countdowns, or worm/spawner behavior that demands immediate cleaning.

Others could be **covert**: a node remains superficially normal but silently reduces or destabilizes useful traffic's WORK progress, without outright blocking traffic, announcing a countdown, or spawning worms. This makes node health a question the player sometimes has to investigate.

### Scan for certainty

`scan n2` starts an **asynchronous node-level infection check**. It occupies simulation time but does not require the player's attention in a clean minigame. Its result establishes whether an infection is present.

Crucial proposed guarantee: **a positive scan means there is a real infection and a valid solution/target in the subsequent cleaning minigame.** It does not guarantee that the player will execute that solution correctly.

### Clean without certainty

`clean n2` remains an immediate option even without a scan. This commits the player to an attention-demanding minigame based on their suspicion.

If the node is not infected, a cleaning minigame **may have no malicious target and no proper answer**. In a PID Killer example, no PID is actually the bad process; killing a legitimate PID/pod can harm benign operation. Blind cleaning risks wasted time and collateral damage. A player who first obtains positive scan results avoids that particular uncertainty.

This is a proposed twist on shared clean-minigame design, **not** an assertion that all existing minigame drafts support no-solution cases. The rule must be made legible so an empty minigame feels like the risk of an unverified clean, not a broken puzzle.

Open issues: scan duration, feedback for a clean node, false-negative policy (the current hypothesis favors scan confirmation), whether multiple infections can be scanned independently, and how failed or unnecessary cleans affect traffic.

## Interaction sketches (not scripts to implement)

**Delayed infection:** A connection appears ordinary but lives unusually long; `logs` shows warning signs. The player starts `trace`. The hacker terminates the connection mid-trace. Later, an edge probe flags a similar connection early enough to try again.

**Bursting demand:** A node experiences short overload flashes while traffic looks normal between them. The player notices the temporal pattern, identifies a `SPIKING` connection, and chooses whether to kill or trace it.

**Node or connection?:** A legitimate connection's WORK progress misbehaves. The player can read its logs, scan the destination node, or risk a blind clean. The same visible symptom admits more than one cause.

**Tracing into prevention:** Probe detection → trace the still-live connection → gain source information → block the source. Future attempts visibly emerge and retract at their origin, and the console reports each rejection.

These are possible encounters, not required sequences. Any single tool should be situationally useful without forcing the full chain every time.

## Compatibility with the implemented Connections V1 model

This note **does not** silently change existing contracts:

- Today `block <connectionId>` immediately resolves a live connection and releases requested LOAD. Renaming it to `kill` and introducing source-level `block` would be an explicit command/API migration.
- The current target-node allocator uses proportional sharing; **one allocated LOAD advances one operation-local WORK unit per second**. An unexplained change to WORK speed at unchanged allocation would require a deliberate new modifier, infection effect, or presentation rule. Ordinary fluctuations caused by changes in *allocated* LOAD are not themselves evidence of infection.
- Node bars currently display **requested LOAD / capacity**, capped at full width, not total granted allocation. Spiking demand should not be confused with exceeding the actual capacity granted by the allocator.
- Current generated traffic is benign. There is no Connections threat detection, infection, clean minigame, source tracing, blocking of future sources, or deployable probe.
- Existing `log save`/`log dir`/`log folder` deal with the **run recorder**, not per-connection `logs`. Packet's scan and trace are a different prototype's mechanics.

## Unresolved design decisions

- What leads the player to investigate *some* connections/nodes without routinely checking all of them?
- What evidence is ambiguous, what is confirmed, and how does the UI communicate confidence without converting suspicion to ground truth?
- How do deliberate WORK anomalies coexist with the current deterministic LOAD-to-WORK foundation?
- What makes a trace worth attempting under time pressure, and how should a clever hacker's counterplay feel fair?
- Does blocking a source affect existing connections, benign connections from that source, or only future hostile spawn plans?
- How expensive in attention, time, or opportunity should probes and scans be?
- Can covert infections and false clean attempts be taught without turning the interface into a guessing game?
- Which single slice could be tested first to evaluate the core behavior-over-time hypothesis?

**Disposition:** Preserve these ideas for later design and testing. There is no selected V1/V2 scope, implementation order, keyword schema, UI contract, tuning, or approved command migration.
