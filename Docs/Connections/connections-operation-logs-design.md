# Connections — Operation Logs & Live Tailing (Working Design)

**Status:** Working design first captured November 2, 2026, updated after the November 5 Logs V0 discussion. **Not implemented, and not an approved implementation contract.** This page records the preferred first-playable interaction direction while retaining important unsettled questions. The original five-entries-*per-connection* and current-operation-only viewing ideas have been reconsidered and superseded below.

**Scope:** Connections only. Packet-era logging and the Connections run recorder are separate systems. For implementation authority consult [Current State](Current_State.md), [operation arcs](connections-arcade-operation-arcs-v1.md), [behavior/modifier foundation](connections-behavior-foundation-v0.md), [Inspect V2](connections-inspect-v2.md), and the runtime. For authored content, see the separate [PURPOSE Activity Vocabulary & Initial Log Corpus](connections-purpose-log-vocabulary-v0.md). The earlier [Threat Signals, Investigation, and Response](connections-threat-investigation-ideas.md) note preserves the investigation hypothesis and its original open questions.

## Why logs exist

`logs` is a **deep, expert-facing investigation tool**, not a replacement for fast `inspect`, `load`, `work`, or scarce `probe`. Different threats reveal themselves at different layers of evidence:

| Tool / depth | What the player can notice | Cost / character |
| --- | --- | --- |
| `load <nodeId>` and `inspect <id>` | Excessive LOAD, an implausible claimed PURPOSE/target, or an obvious operation mismatch | Quick triage; sometimes enough |
| `work <nodeId>` and `inspect -v <id>` | Unexpected WORK requirements, progress, operation arc, or duration | Comparison and judgment |
| `logs <id>` | A succession of ordinary activity observations, occasional anomalous entries, contextual contradictions, and sometimes no tell | Player attention, literacy, and time |
| `probe <linkId>` | Temporary direct intelligence on traffic at one physical edge | A limited defensive opportunity rather than sustained reading |

These are **not a mandatory sequence**. A huge LOADER may be obvious in LOAD while its application logs are boring. A subtle INFECTOR may look normal until an odd log line arrives, or may have another clue entirely. A benign outlier may look unusual. An occasional threatening line is not automatically proof of an attacker.

Logs may legitimately be arcane to a new player; this is intended to be the deepest, hardest layer of investigation and a learned skill ceiling. Do not automatically highlight bad entries, translate them into verdicts, or guarantee a readable giveaway in every attack. The difficulty should arise from understanding the connection's claimed business activity and noticing meaningful deviations, not only from making text fly past impossibly fast.

**Tradeoff:** the network keeps evolving while the player studies logs. `probe` can provide earlier direct intelligence for a resource/opportunity cost; logs cost attention and require interpretation.

## Preferred V0 interaction — a rolling, persistent live tail

**Working command surfaces:**

~~~text
logs x1           # live tail of the five most recently emitted entries
logs -n 20 x1     # same live tail, with a larger 20-line display window
~~~

- `logs <connectionId>` immediately opens a small, structured **terminal-history card bound to that logical connection**. Like `load` and `work`, it stays live without repeatedly issuing the command.
- **Five displayed lines is the default viewing budget, not a log-production budget.** A connection may produce many more entries over its operations; the panel renders the latest N *already emitted* entries at the moment it is opened.
- `-n` lets a player explicitly spend more terminal space to get more context. The same panel behavior applies at larger counts; there is no separate history-navigation mode. A reasonable positive-integer limit is still to be selected, so `-n 20` is illustrative and supported by the current design direction, not a finalized maximum.
- New emitted entries appear in deterministic sequence at the bottom of the display. Entries displaced by the five-line tail **are not deleted from the connection's actual retained history**. Reissuing the command with a larger `-n` can recover earlier entries.
- The view is one chronological tail **across every operation**. Each line carries its originating READ/WRITE/EXECUTE label, so an operation transition does **not** wipe the panel or require a selector. Previous-stage lines roll off naturally as later-stage lines arrive.
- Entries generated *before the player opens a panel* are still available, including after the connection resolves, subject only to the requested last-N viewing budget.
- This is not `grep` or a pipeline parser. `logs | grep`, alternate filters, keyword search, sorting, and output redirection are interesting future expert tools but **not Logs V0**.
- `logs` (plural) is distinct from today's recorder-management `log save`, `log dir`, and `log folder`. Neither the recorder command names nor the recorder's purpose change.

**Window size and terminal discipline:** the card's maximum displayed-row count derives from N rather than from total connection lifetime. The five-line default must be genuinely compact, reflecting the recent effort to reduce inspect/diagnostic footprint. A larger `-n` intentionally consumes space at the player's discretion. Ordinary history scrollback, anchor-preserving reflow, and keyboard-first command focus still apply. A newly emitted line must never steal focus or forcibly pull the player back from scrollback they are reading. Do not add nested scrolling or mouse-only operation controls merely to view previous stages.

For a not-yet-active connection the panel can show a compact waiting/empty state rather than invented logs. Exact empty-state copy and whether repeated requests for the same connection create independent cards follow normal terminal conventions and can be checked in the playable slice.

### Example: a short tail crossing operations

The stream below represents a **single** Customer Checkouts connection, presently in READ. The previous WRITE lines are still visible because they are among the last five emitted observations.

~~~text
> logs x1

X1 / API / CUSTOMER CHECKOUTS               LIVE · TAIL 5
00:29.3  WRITE  checkout.validate          OK
00:30.7  WRITE  checkout.session.export    START
00:32.0  WRITE  checkout.commit            OK
00:42.4  READ   receipt.fetch              OK
00:42.9  READ   checkout.status.read       OK
~~~

It does **not** imply another logical connection or an independent route to another network node. The actual target comes from the connection; the log line's activity name describes observed work at that target.

## Log production is separate from the viewing window

Operations may emit **many** authored entries rather than a handful over an entire connection. A first sample might have 15–30 entries over one connection, or more for a chatty or multi-operation case. These are playtest examples, not fixed per-operation quotas, a hard connection cap, or a content-generation formula. The [initial corpus](connections-purpose-log-vocabulary-v0.md) includes a 24-entry two-operation Customer Checkouts example and a 19-entry singular Sales Report Export example.

- Mundane repeated application activity is most of the stream. Within a PURPOSE, that activity should feel like recurring instances (aggregate Customer Checkouts) or repeated sub-work of one heavyweight job (singular Sales Report Export).
- Entries should arrive in **bursts and quiet stretches**, not necessarily evenly spaced. One cluster might cross several WORK milestones in rapid succession, followed by meaningful silence.
- Some hostile connections may contain one incriminating observation among dozens of normal ones. Others have no suspicious log entries at all. Benign connections can include retries, errors, anomalies, and occasional alarming-looking content.
- The stream is finite with respect to its concrete plan: it does **not** fabricate infinite entries once per update simply to keep the window moving.
- A five-line window makes a clue temporarily prominent, then pushes it offscreen as later entries arrive. This creates *observation pressure*, but the emitted evidence persists and is recoverable with a larger `-n`. Displacement is a **display choice**, not irreversible erasure.

There is no need for an endless background log writer or extra wall-clock emission system to get the desired effect.

## Authored entries belong to operations; WORK drives emission

**Working mental model:** every READ/WRITE/EXECUTE operation in the final concrete connection plan may carry an ordered set of planned log observations. Each has application activity/result content and an **operation-local WORK milestone**. No line appears simply because the viewer opened.

- As actual `CompletedWork` crosses a milestone, the simulator emits the entry once, with the current simulation timestamp, concrete connection identity, originating operation index/kind, and content.
- Contention delays emission because productive WORK advances more slowly. More allocated LOAD or a WORK-rate multiplier makes entries arrive earlier in simulation time. A 0× WORK rate (as used by active LOADERs) stops WORK-driven entries too, even if requested LOAD remains high. BACKDOOR's accelerated INFECTOR WORK likewise accelerates milestone crossings.
- If one fixed simulation step crosses several milestones, emit every eligible entry exactly once in stable authored order. A burst should not silently lose lines.
- Stage transitions must not replay earlier entries, reset chronological history, or reveal the next stage's future activity. The operation index must be captured before a completed stage advances and resets `CompletedWork`.
- A killed, canceled, withdrawn, or naturally completed connection can never produce future *unearned* WORK entries; it retains everything legitimately emitted before its resolution.

**Authoring-coordinate question:** absolute completed WORK or normalized operation progress remain options. One attractive V0 authoring approach is to specify fractions (e.g., 3%, 4%, 5%, then 25%) and convert them to **fixed concrete WORK thresholds after plan composition**. This lets normal-work variation scale the authored rhythm without dynamically rescaling lines each time effective required WORK changes. The exact transformation and any effect of runtime WORK-requirement modifiers need explicit validation, not an unexamined percentage comparison each frame. In particular, a reduced requirement can finish an operation before some originally scheduled entries would have been reached; those lines should not be fabricated as a reward for completion.

The **simulator** owns the emitted facts. Presentation consumes the retained emitted history, never planned-but-unearned entries or hidden hostile intent. The existing completion-effect path remains authoritative: do not claim an infection was installed, a Payload launched, or a node changed state merely because a fictional log line says an attempt was prepared. Optional recorder provenance is useful, but the viewer should not read the recorder JSON as gameplay state.

## Presentation and lifecycle

The intended presentation is a **small attractive live terminal-history panel**, borrowing existing `InspectOutput`, `LoadOutput`, `WorkOutput`, history layout, and focus patterns rather than creating a new window manager.

- Header: verified connection identity, optional actual target / claimed PURPOSE for context, and LIVE or final-state label.
- Entries: a terse chronological stream with operation label on **every line**, application activity, short result, and optionally simulation timestamp. Avoid separate `AUTH`, `DB`, `CACHE`, or other pseudo-service columns that could be mistaken for nodes or implied network routes.
- Do **not** duplicate `inspect -v` details, a full WORK progress bar, or a complete operation timeline inside the logs panel.
- One rolling tail spans operation changes. A five-line display remains compact; a twenty-line display will occupy much more history space by deliberate player choice.
- When the connection ends, freeze the final earned tail and show COMPLETE, KILLED, CANCELED, or WITHDRAWN accurately. A short final hold followed by an inspect-style compact summary is an attractive direction to prevent terminal history from filling with resolved panels. The exact compact layout/hold and how a newly requested historical panel behaves are presentation-tuning details, not established contracts.
- Historical `logs` requests should read the retained emitted stream, not regenerate logs, even after the original live card has collapsed. Simulation reset clears old-run state and should not leave panels bound to new unrelated connections.

**Identity/addressability:** current ID hopping retains a canonical ID for `inspect` and `kill`; visible aliases do not resolve those commands, while Probe can expose the true ID. Logs V0 should follow the same existing command-addressability semantics, not create a new alias-based backdoor or a new guarantee that guessing a real canonical ID is impossible. The displayed panel should respect the already-established identity presentation conventions.

## Evidence quality, PURPOSE fiction, and difficulty

The [PURPOSE/log vocabulary document](connections-purpose-log-vocabulary-v0.md) is the content reference for V0. Its proposed small naming pass pluralizes naturally aggregate workloads, including **Customer Checkouts** and **Cart Price Calculations**, while keeping **Sales Report Export** deliberately singular as a heavyweight job. That is a proposed *player-facing naming change* for the later implementation, not a mutation of stable internal purpose IDs, targets, or arcs.

The same logging system supports both fictions: many checkouts can yield recurring authorize/commit observations within one WRITE operation, while one heavyweight report can yield repeated reads, partial aggregates, and emitted chunks within one READ operation. No separate transaction, report-chunk simulation, or perfect lifecycle of messages is required.

Clues may be:

| Evidence feel | Example | What the player does |
| --- | --- | --- |
| Comically obvious | `attack.protocol.arm START` | Understands the danger immediately |
| Recognizably concerning | `credentials.download START` | Notices an action that seems improper |
| Contextual | `report.chunk.emit OK` during Customer Authentications | Relates an ordinary-looking action to an unexpected PURPOSE |
| Mundane / ambiguous | `payment.authorize RETRY` during Customer Checkouts | Uses context rather than treating retries as verdicts |
| No tell here | A LOADER with entirely ordinary log activity | Should have investigated abnormal LOAD instead |

Avoid a universal one-threat/one-bad-line pattern or an inevitable `logs` step for every attack. The existing [THREATS](THREATS.md) design treats disguise sophistication as a level/run/villain lever **separate from** objective, frequency, severity, timing, operation anomaly, and identity counterplay. The future connection-construction grammar may select purpose-appropriate activity vocabulary, log volume, rhythm, evidence frequency, and where the useful tell lives. It is not this V0 task.

Two illustrative ends of that spectrum remain useful: an overwhelming attacker can be frequent, loud and best handled by fast triage; a patient infiltrator can be rare, credible and detectable only after expertise-intensive observation. Those illustrative attack ratios and difficulties are **not** selected spawn rates. Where logs are the intended actionable clue, it should appear before the attack has already succeeded, with enough reaction time to act; this timing must be checked with current WORK rates, including acceleration.

## First playable slice to test

Prefer a **small manually authored comparison set**, using the existing operation and terminal systems, before ambient procedural logging:

1. An aggregate, two-stage Customer Checkouts stream containing recurring ordinary activity and one contextual WRITE clue, with READ entries continuing to displace it from a five-line tail.
2. A singular heavyweight Sales Report Export with repetitive but uneventful READ activity and bursts/gaps.
3. A disruptive LOADER or similarly obvious hostile example whose log content is mostly mundane, proving `logs` is not always the best investigative response.

A deliberately overt sample, benign errors/false positives, and other PURPOSE vocabularies can be added without attempting a complete generator. The actual playable test is whether a player can notice and interpret a surprising entry among mundane ones, decide how much space to dedicate with `-n`, and react while the network continues moving.

Review: entry order, no future-content leaks, contention/backdoor timing, 0× WORK, completion and early kills, operation transitions, historical queries, stable terminal scrolling, focus, 16:9 size, and replay/recorder determinism where relevant.

## Remaining open choices and deliberate deferrals

- **WORK coordinate details:** percentage-to-fixed WORK translation, dynamic requirement changes, and exact coarse-step timestamp semantics.
- **Display policy:** maximum `-n`, narrow-width and very-large-tail layout, empty-state wording, resolved-card hold/collapse measurements, and whether repeated same-ID requests reuse or append cards.
- **Content tuning:** actual volume/rhythm distributions, how much boring repetition feels authentic rather than tiresome, and whether some operations emit no lines.
- **Evidence and fairness:** how early clues arrive for subtle/accelerated attacks, and how many benign lookalikes support useful learned patterns.
- **Generation:** seeded selection and replay provenance when PURPOSE vocabulary, attack objective, disguise sophistication, and villain/run pacing eventually compose concrete logs.
- **Other tools:** `grep`/pipes, trace/source blocking, more sophisticated investigations, and interactions with Probe remain separate future work.

No UI prefab, runtime type, recorder schema, traffic configuration, test, level contract, procedural log generator, or exact balance value is approved by this **docs-only** update. The aim is a grounded first experiment, with vocabulary and mechanics kept in separate documents for a later focused Codex implementation handoff.
