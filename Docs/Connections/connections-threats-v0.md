# Connections — Threats V0: selected baseline and phased plan

**Status:** Selected design direction and proposed implementation sequence, October 23, 2026. **Slice 1 is implemented on the focused implementation branch; Phases C–E remain plans.**
**Authority:** The October 23 Threats V0 design conversation is the baseline for threat architecture and first-phase scope. The earlier [threat/investigation idea archive](connections-threat-investigation-ideas.md) remains useful for subsequent mechanics, but does not override decisions here. Runtime code and serialized Unity scenes remain authoritative for implemented behavior.

## Objective and first playable slice

Build one consequential, interceptable hostile connection using the **existing fixed operation WORK and LOAD rules**:

1. Spawn a single hostile WRITE operation with unusually large constant requested LOAD, alongside normal benign traffic.
2. Associate an **operation-WORK-completion** payload delivery with that operation.
3. On normal WORK completion, emit a structured delivery occurrence and visible terminal feedback such as `[THREAT] c17 delivered infection payload to DB`.
4. `kill <connectionId>` before delivery stops the connection, releases its LOAD, and prevents the pending completion-triggered occurrence. Killing after delivery cannot undo an occurrence already emitted.
5. **Do not create node infection state, infection effects, cleaning, minigames, node modifiers, infection timers, or a fake persistent infected flag.** An infection *delivery event* is observable; a persistent infection is not implemented. Avoid terminal wording that falsely asserts persistent infection state.

A high LOAD request alone is not the definition of a threat. Benign connections may eventually use identical demand patterns; hostile connections may also have ordinary demand and may never deliver an infection. Classification/intent, demand behavior, and payload effects are independent axes.

## Slice 1 implementation

`basic-threat` is a manual-only profile (`ambientWeight = 0`) with a single WRITE requesting 12 LOAD and 240 fixed WORK against DB. Its claimed purpose is Database Maintenance; hostile intent is internal profile/plan/connection data and does not color the path or appear in ordinary inspection. `spawn basic-threat` queues a concrete plan with a seeded letter-digit ID. `spawn <profileId>` also accepts the five existing benign profiles. An unknown ID returns an error, and manual queue dispatch works with ambient generation disabled. Traffic Director ambient selection, manual debug spawning, and any future Level Director remain separate producers of concrete plans; Level Director scheduling is not implemented.

Each operation definition can carry a completion effect independently of its READ/WRITE/EXECUTE kind and LOAD. The allocator returns completed operation indices after normal WORK progress. `SimulationHost` records the operation completion, asks that connection instance for its effect, records and emits any resulting occurrence, then resolves a final connection. The instance owns a per-operation delivery flag. An infection-delivery occurrence has effect type, connection ID, operation index, target-node ID, and simulation time. The terminal announces delivery; the run recorder stores an `effect` event. No persistent node infection state is created.

`kill <connectionId>` immediately resolves an active connection as Killed and publishes released target LOAD. An unfinished operation produces no completion effect. The old per-connection `block <id>` command is removed; `block <source>` remains reserved for future source prevention.

## Reconciliation with the October 22–23 idea archive

The [archive](connections-threat-investigation-ideas.md) was deliberately speculative. This conversation supersedes or resolves these aspects for the Threats V0 plan:

- **SPIKING** means recurring temporary high-LOAD bursts with recovery intervals within **one operation** (illustration: 10 seconds normal / 2 seconds elevated / repeat).
- **OVERLOAD** means one transition to sustained elevated LOAD during **one operation**; the transition may occur at operation start (our first loud WRITE) or partway through. These are **working keywords**, not promises of exact player-visible UI strings.
- Both keywords are **operation-arc behaviors** and can belong to benign or malicious connections. They are not infection-delivery mechanics or proof of hostility.
- An operation will eventually distinguish immutable authored/base WORK and requested LOAD from **effective WORK and effective requested LOAD** computed from its runtime behaviors. The allocator continues to allocate effective requested LOAD proportionally; it does not acquire threat-specific rules. Completed WORK is not retroactively rewritten when requirements change.
- Lifecycle execution is universal for all connections. Definitions are reusable; each spawned connection owns its own behavior state, counters, and timers.
- A delivery can eventually occur multiple times during one operation or across several operations, even after the player cleans a previous infection. **Do not** bake in one delivery per connection or only at final connection completion.
- The chosen first slice implements none of the above dynamic behaviors; it establishes the narrow operation-completion delivery seam first.

The archive also retains proposed `logs <connectionId>`, time-consuming `trace <connectionId>`, temporary edge `probe <edgeId>`, covert/overt infections, asynchronous `scan <nodeId>`, blind `clean <nodeId>`, and hacker counterplay. These are **not part of the selected first slice** and are not implicitly authorized by this document.

## Command vocabulary and branch truth

- **`kill <connectionId>`:** terminates one active connection. This is the selected name for the existing per-connection termination action.
- **`block <source>`:** reserved for future prevention of arrivals from a source/family; semantics and source identity resolution remain open. Not needed for Threats V0's first playable slice.
- The PR #15 archive explicitly documents current `block <id>` as the *implemented* command at its snapshot and the rename to `kill` as a *proposal*. Another in-flight branch may have since migrated the code. **Codex must inspect its actual starting branch and existing tests, and must not assume the rename is already merged or silently overwrite concurrent work.**
- Distinguish proposed per-connection `logs` from existing run-recorder `log save`, `log dir`, and `log folder`.

## Universal operation lifecycle — minimal now, extensible later

Connection simulation owns advancement and deterministic step order; connection instances own their behavior state. Consider lifecycle boundaries such as `OnOperationStart`, `OnOperationTick`, `OnWorkComplete`, `OnOperationEnd(reason)`, and `OnConnectionEnd(reason)`, **but implement only what is needed now**. An operation can be active with zero WORK progress, so `OnOperationTick` is more precise than `OnWorkTick` for elapsed-time behaviors.

Separate normal WORK completion from interrupted/aborted operation end: **a kill must not fire a completion-triggered payload**. Treat operation-completion effect occurrences as explicit simulation facts that the terminal and recorder may observe. Avoid reflection, general event-bus frameworks, inheritance splits by benign/threat, and speculative modifier stacking.

Later, effective demand changes must be applied **before allocation** each fixed step; progress and WORK-completion effects happen **after allocation/progress**. Never drive simulation state from render time or UI animations. Retain the current arc invariant: same connection ID, route, and target across operations; next operation begins on the next fixed step.

## Proposed delivery sequence

### Phase A — Inspect and reconcile (read-first, no simulation changes)
- Inspect active branch, PR #15 documentation, any newer command migration, current Connections runtime, arc and traffic contracts, witnesses, terminal, recorder, and Packet threat/infection/lifecycle code for relevant lessons.
- Identify the smallest seam for attaching an operation-completion behavior/effect to a concrete spawned operation, without making all connections threat-specific.
- Record implementation findings and explicitly flag any conflict between old speculative notes and this baseline.

### Phase B — First threat, real consequence, minimal code
- A reproducible spawn path for one hostile, high constant LOAD WRITE. Preserve existing ambient benign traffic.
- One operation-completion infection-*delivery* occurrence with connection ID, operation identity/index, target, and simulation time; terminal logging is required, recorder integration if it fits existing structured events cleanly.
- Correct `kill` interruption behavior and release of LOAD; no delivery if interrupted; no duplicate delivery.
- Visible enough for hands-on playtest without exposing malicious classification to the player automatically merely because internal profile is hostile. Threat tuning and reveal policy remain provisional.
- No actual infection or cleaning systems.

### Phase C — Second threat, operation scope
- Normal first operation followed by a high-LOAD WRITE with a completion delivery.
- Verify that the first operation completing does **not** deliver the second operation's effect; delivery at its intended operation does not require final-connection special casing.
- Preserve inspection history, arc identity, next-step transition, target/route, and LOAD release.

### Phase D — Dynamic operation requirements
- Introduce authored/base and effective WORK and LOAD, with operation-owned evolving behavior state.
- Preserve completed WORK when effective total WORK changes. Resolve decreases to or below completed WORK through ordinary completion rules; define ordering explicitly.
- Deterministic witnesses for changes within one operation, zero allocation, stage transitions, and independent concurrent instances.
- Do not yet require a general stacking algebra for multiple modifiers.

### Phase E — Reusable keywords and third threat
- `OVERLOAD`: one sustained elevated-demand transition, including possible start-at-zero variant.
- `SPIKING`: repeated high-demand windows and normal recovery intervals during a single operation, driven by operation-local simulation time, not wall clock.
- Third hostile example: otherwise normal-looking operation that spikes according to its keyword. Validate that benign operations can use the same keyword without infection delivery.
- Keep threat classification and payload delivery independent of these operation-arc modifiers.

Phases C–E are planned direction, **not** a request to implement them in the first PR. Prefer a small reviewable PR for A–B, then reassess based on gameplay and code.

## Deterministic witnesses and review criteria

First PR should prove:
- An ordinary benign operation completes without any threat occurrence.
- The hostile WRITE completes and emits exactly one delivery occurrence associated with the **operation**, not a generic connection end.
- Killing just before completion emits none, releases LOAD, and preserves terminal/history consistency.
- Killing after a delivery does not erase the recorded occurrence.
- LOAD allocation/contention remains the existing proportional model; no regression to benign traffic and multi-stage arcs.
- Reset/replay avoids stale delivery state and duplicate effects.
- The same authored profile instantiated more than once does not share mutable runtime delivery state.

Later witnesses should prove dynamic requested LOAD changes before allocation, repeated SPIKING, sustained OVERLOAD, changes to WORK requirements, and independent runtime instances.

## Repository and documentation boundaries

- Current integrated Connections contracts: [LOAD](connections-arcade-load-v1.md), [operation arcs](connections-arcade-operation-arcs-v1.md), [traffic](connections-arcade-traffic-v0.md), and [run recorder](connections-run-recorder-v0.md).
- Packet's threat and infection implementation can inform lifecycle architecture, but its packet ticks, traversal, and commands are not drop-in Connections rules.
- Keep PR #15's idea archive intact as a historical/exploratory record. This page is a selected phase plan, not a retroactive rewrite.
- Do not modify the generated `Docs/_generated/Uptime_Project_Core.md` directly. If canonical Core-included sources change, regenerate via `scripts/build_uptime_project_core.py` and verify `--check`. This docs-only branch adds an indexed plan and does not need a Core regeneration.
- This documentation branch is intentionally **based on PR #15's head** and targeted at `main`. Merge PR #15 first; after it merges, the follow-on PR should reduce to just this plan and index changes. Recheck diff if PR #15 is modified or squash-merged.

## Codex handoff — recommended first implementation

> Start from current `main` after the documentation PRs are merged. Read `Docs/Connections/README.md`, this selected Threats V0 baseline, the older exploratory threat/investigation archive, Connections Current State, operation arcs, traffic and LOAD contracts, plus relevant Packet threat/infection runtime for architecture lessons. Treat **this baseline** as authoritative on selected Threats V0 decisions and the archive as deferred hypotheses. Audit the actual code for current `kill` versus `block` command semantics before editing. Implement **only Phase A and Phase B**, using the narrowest shared operation-lifecycle seam: a single reproducible hostile high-LOAD WRITE with an operation-WORK-completion infection-delivery occurrence, console feedback, correct interruption via `kill`, deterministic witnesses, and no persistent infection state or clean gameplay. Keep benign traffic, proportional allocation, and existing arc invariants intact. Report design tradeoffs, tests, and Play Mode review steps in a PR. Do not implement SPIKING, OVERLOAD, trace, source blocking, probes, scans, infection state, or a generalized modifier engine in this PR.
