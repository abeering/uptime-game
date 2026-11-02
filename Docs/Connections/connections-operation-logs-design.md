# Connections — Operation Logs & Live Tailing (Working Design)

**Status:** Working design captured November 2, 2026, from the October 31 Connections investigation discussion and the follow-up idea of WORK-triggered, operation-local log entries. **Not implemented, not an approved implementation contract.**

**Scope:** The Connections prototype. Packet-era logging and existing Connections run-recorder commands are separate systems. For implemented behavior, use [Current State](Current_State.md), [operation arcs](connections-arcade-operation-arcs-v1.md), [behavior/modifier foundation](connections-behavior-foundation-v0.md), and [Inspect V2](connections-inspect-v2.md). This refines the earlier [threat-investigation ideas](connections-threat-investigation-ideas.md), which deliberately left the log lifetime and content model unsettled.

## Why logs exist

Investigating every connection all the way down should **not** be the universally correct strategy. Different kinds of hostile connections should betray themselves at different levels of inquiry:

| Tool / depth | What the player can notice | Typical effort |
| --- | --- | --- |
| `load <nodeId>` and ordinary `inspect <id>` | Exorbitant requested LOAD, implausible claimed purpose/target, clearly wrong operation type | Fast triage, sometimes sufficient to act |
| `inspect -v <id>` | Excessive WORK for that purpose, inconsistent operation arc or time spent working | Some comparison and judgment |
| `logs <id>` (proposed) | Actual activity evidence among a handful of mundane, technical-looking entries | Attention spent reading, recognizing, and sometimes comparing context |
| `probe <linkId>` (separately explored) | Temporary, scarce, direct intelligence about hostile traffic on one physical link | Minimal reading effort, but opportunity/cooldown cost |

These layers are **not a mandatory sequence** and not a rule that deeper evidence must always be stronger. A huge LOADER may be obvious in `load` and have completely ordinary logs; a subtle INFECTOR may have normal LOAD and a plausible arc but incriminating activity. An occasional benign connection can look unusual without being hostile. The player may decide to kill without conclusive evidence and accept the risk.

A long-term aspiration is for a connection's real nature to be potentially discoverable through evidence, with different levels of effort. That is *not* an approved guarantee that every individual threat exposes a perfect tell, and it does not override separate identity-hopping/Probe rules.

**Player tradeoff:** `logs` spends the player's attention; `probe` potentially saves attention by spending a limited opportunity. The network continues evolving while the player reads.

## Proposed player interaction: `logs <connectionId>`

`logs a3` opens **one live, structured terminal-history panel bound to that logical connection**, in the vein of `inspect` and `load`. It behaves more like `watch tail` than a one-time dump or an endless console feed.

- Opening the panel is immediate. It displays **entries already emitted for the currently active operation**, then appends later entries as that operation progresses. It never shows future entries just because they were authored in the plan.
- The panel continuously watches its connection while it exists in terminal history; there is no need to reissue the command for each new line.
- The panel identifies the connection and **current operation** (for example, `2/3 WRITE`). Its main content is a short, readable ordered log stream, not a duplicate of `inspect -v`.
- Each line may carry a simulation-time stamp and a terse protocol/service/action label. It should feel like fictional TCP/IP and application logs, not require professional networking knowledge.
- Ordinary terminal scrollback, prompt focus, and other live panels should continue behaving normally. A new log must not steal keyboard focus or forcibly yank a player who has scrolled back to read something else.
- When the connection finishes, is killed, or is canceled, no unearned future entries appear. A retained final panel/state is attractive, but the exact completed-panel treatment remains open.

The command name is deliberately **`logs` (plural)**, distinct from today's run-recorder management commands `log save`, `log dir`, and `log folder`. This proposal does not modify the recorder's player-facing management commands.

## Finite log budgets, not infinite noise

A connection should produce a **bounded handful of purposeful log entries**, rather than fabricating a new line every update.

- Roughly **five lines over a connection** is a useful initial feel, not a fixed universal count or balance commitment.
- A multi-operation connection's total budget can be distributed unevenly among its operations: one stage may emit several entries, another only one, and another none.
- Noise is important: normal acknowledgments, session checks, transport chatter, retries, and service activity make players scan the lines. But noise must stay bounded so reading logs does not become compulsory clerical work.
- Don't implicitly multiply a five-line budget by every stage of a long arc and create an accidental wall of text.
- The content budget and severity mix can vary by purpose, connection archetype, and eventual villain/level design.

The panel is **live** because *when* a line appears matters, not because the screen should constantly scroll.

## Authored entries belong to individual operations

**Proposed authoring mental model:** a connection's ordered READ/WRITE/EXECUTE operations each may carry their own planned log entries. An entry is associated with that operation and an **operation-local WORK progress milestone**, not an arbitrary wall-clock delay.

For example, in an operation requiring 80 WORK, fictional entries might be scheduled near 8, 35, and 66 completed WORK. The exact representation (absolute WORK milestone versus normalized progress) is not yet selected.

The simulator advances WORK according to actual allocation and effective WORK-rate modifiers. Log emission follows the actual crossing of those milestones:

- Heavy contention delays the log because the operation completes WORK more slowly.
- A faster operation emits the associated lines sooner in simulation time.
- A killed or canceled connection never emits entries it did not reach.
- Multiple milestones passed in one simulation step should be emitted once, in deterministic order.
- An operation transition must not accidentally replay an earlier stage's entries or expose the next stage's future content.
- Dynamic effective WORK requirements can change while a stage is in progress. The rule for milestones under such changes needs deliberate design; don't assume fixed percentages or current total WORK always behave identically.

This is a proposed **future simulation-data seam**, not a request to add logging behavior to `OperationStage` or `SimulationHost` in this PR. Runtime emission and optional recorder provenance would be the authoritative source; the terminal panel would only present already-emitted events.

### The multi-operation viewing question

The current design instinct is: **the log panel follows the connection's current operation and shows the entries belonging to that operation**.

A possible V0 behavior is:

1. `logs a3` opens during operation 1 and shows its already-emitted lines.
2. New lines arrive as operation 1 advances through WORK.
3. When operation 2 becomes current, the same panel changes its operation heading and begins showing that operation's emitted lines.
4. Future operation-2 lines stay hidden until the required WORK milestones are reached.

**Open:** Should previous-operation logs remain accessible through a compact per-stage history, a manual selection, or only a reissued command? Showing just the current operation keeps the panel small, but permanently losing earlier evidence could be unfair if the player starts investigating late. Preserve prior emitted events in the eventual data model until this UI decision is tested.

### Illustrative operation-local output

```text
> logs k7

K7  /  OP 2 OF 3  /  WRITE      LIVE
00:41.2  TCP   ACK received
00:44.9  AUTH  session.validate       OK
00:48.1  DB    credentials.download   START
         (watching for further entries)
```

The lines above are authored fictional *examples*, not an assertion that a real TCP/IP stack is being simulated or that every connection produces a suspicious third line.

## Evidence should vary in obviousness and in where it appears

An important aim is a **spectrum of readable evidence**, not a binary "logs contain one bad keyword" puzzle:

| Log difficulty | Illustrative tell | Player realization |
| --- | --- | --- |
| Comically overt | `INITIATE_ATTACK_PROTOCOL` | Sometimes the game should just hand you a satisfying, obvious clue |
| Recognizably bad | `credentials.download` | Looks inappropriate without specialized knowledge |
| Contextual | `report.export` while the connection claims to validate authentication sessions | The line itself may be normal elsewhere; compare it with purpose, operation, WORK, or target |

Every sample should contain ordinary-looking surrounding activity. Sometimes a malicious connection's logs can look mundane because its **main tell is elsewhere** (for example, excessive LOAD). Sometimes legitimate activity can appear surprising until its business purpose is considered.

Avoid two traps:

1. **Always-check-logs:** If every hostile connection has a reliably obvious log signature, players will skip earlier tools and automatically run `logs` on everything.
2. **Too-arcane-to-read:** If every log requires careful cross-referencing of five technical fields and three other panels, investigation becomes a chore rather than a game.

Aim for an authored mix: many obvious or recognizable clues, some contextual cases that reward growing expertise, and some connections whose useful clue lives in `load` or `inspect -v` instead of their logs. Do not turn evidence difficulty into a rigid inverse formula that itself becomes predictable.

## Connection to villain tempo and threat severity

The evidence distribution can make different attackers feel different **without inventing entirely new mechanics**.

- **Overwhelming attacker (illustrative 1 hostile in 4 connections):** frequent and relatively obvious attacks, continuous LOAD pressure, infections and distractions. The challenge is triage, execution, and limited attention. Reading logs may be a poor use of time.
- **Patient infiltrator (illustrative 1 hostile in 12 connections):** rare and comparatively subtle attacks with catastrophic potential. Ordinary LOAD/purpose can look fine; a late log entry or contextual mismatch may be critical. Suspense and vigilance replace nonstop reaction.

These rates are expressive poles, not selected spawn configuration. Rare, catastrophic attacks need **actionable, discoverable evidence early enough to respond**. Requiring the player to wait until the attack has already succeeded before emitting the only useful log line would feel arbitrary.

## Boundaries and open design questions

- **Current-only versus history:** Should the panel show exclusively current-operation entries, retain a folded previous-stage summary, or allow revisiting past operations? What happens to a panel once the connection resolves?
- **WORK authoring coordinates:** Are events placed at absolute completed-WORK milestones or normalized operation progress? How do WORK-changing modifiers affect those thresholds?
- **Budget:** What is a good typical number of entries per connection and per operation? Which purposes warrant none or more?
- **Generation:** How are believable benign noise and meaningful hostile evidence selected for a generated connection while maintaining deterministic replay and not accidentally revealing hostile state via a fixed template?
- **Player readability:** How much protocol jargon, timestamp detail, and line formatting makes this feel like tailing a terminal without overwhelming a nontechnical player?
- **Clue timing and fairness:** What fraction of an operation should pass before meaningful evidence appears? How do early kill, fast completion, and late inspection affect access to clues?
- **Identity concealment:** Future ID-hopping must not be bypassed by guessing an internal connection ID through `logs`; discovery and command-addressability are separate questions.
- **Relationship to other tools:** `load`, `inspect -v`, `probe`, and future trace/source-block mechanics may provide overlapping evidence. Keep their costs and roles distinct without assuming a mandatory investigative chain.

No UI prefab, runtime type, recorder schema, traffic configuration, test, or level contract is approved by this document. Its purpose is to preserve the direction for a later small gameplay experiment.
