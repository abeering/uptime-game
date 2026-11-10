# Connections — Operation Logs & Live Tailing (Working Design)

**Status:** Logs V0 **implemented and merged** via PRs #105 (simulation), #106 (viewer), and #107 (PURPOSE content), after PR #104's design documentation. This is a record of the design rationale, implemented choices, and future possibilities—not authorization for deferred features. The old five-total-lines and current-operation-only ideas were superseded.

**Scope:** Connections only. The runtime, scenes, [Current State](Current_State.md), [Phase 2 viewer note](connections-operation-logs-phase2-implementation.md), and [Phase 3 content note](connections-purpose-logs-phase3-implementation.md) are implementation authority. See the [PURPOSE vocabulary](connections-purpose-log-vocabulary-v0.md) and earlier [investigation ideas](connections-threat-investigation-ideas.md) for content and original hypotheses. Packet logging and the run recorder are separate systems.

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

**Live-network principle:** Uptime is about defending a **living network**, not postmortem forensics. Earned history stays recorded, but resolved LOGS cards immediately lose active emphasis and collapse promptly. Attention should return to information that can still support a defensive action.

## Implemented V0 interaction — a rolling, persistent live tail

**Implemented commands:**

~~~text
logs x1           # live tail of the five most recently emitted entries
logs -n 20 x1     # same live tail, with a larger 20-line display window
~~~

- `logs <connectionId>` immediately opens a small, structured **terminal-history card bound to that logical connection**. Like `load` and `work`, it stays live without repeatedly issuing the command.
- **Five displayed lines is the default viewing budget, not a log-production budget.** A connection may produce many more entries over its operations; the panel renders the latest N *already emitted* entries at the moment it is opened.
- `-n` selects a player-chosen viewing footprint with **integer N from 1 through 30**, default five; this remains the same rolling view.
- Emitted entries appear in deterministic order. Reissuing with larger `-n` recovers recent displaced lines, but **only the latest 30 entries are accessible through the terminal**. The full earned history and recorder provenance are retained even when older lines cannot be viewed.
- The view is one chronological tail **across every operation**. Each line carries its originating READ/WRITE/EXECUTE label, so an operation transition does **not** wipe the panel or require a selector. Previous-stage lines roll off naturally as later-stage lines arrive.
- Entries already emitted when the panel opens can be viewed within the last-N budget, including after resolution. Resolved panels display their earned tail only briefly and then collapse.
- This is not `grep` or a pipeline parser. `logs | grep`, alternate filters, keyword search, sorting, and output redirection are interesting future expert tools but **not Logs V0**.
- `logs` (plural) is distinct from today's recorder-management `log save`, `log dir`, and `log folder`. Neither the recorder command names nor the recorder's purpose change.

**Window size and terminal discipline:** the card's maximum displayed-row count derives from N rather than from total connection lifetime. The five-line default must be genuinely compact, reflecting the recent effort to reduce inspect/diagnostic footprint. A larger `-n` intentionally consumes space at the player's discretion. Ordinary history scrollback, anchor-preserving reflow, and keyboard-first command focus still apply. A newly emitted line must never steal focus or forcibly pull the player back from scrollback they are reading. Do not add nested scrolling or mouse-only operation controls merely to view previous stages.

Pending and connecting states show a waiting message; active connections without earned entries say `NO LOGS EMITTED`. Repeated requests create independent history cards for the same canonical ID.

### Example: a short tail crossing operations

The schematic stream below represents a single Customer Checkouts connection during READ, with earlier WRITE observations still among the latest five. The actual Unity UI has an ID pill, `LOGS` label, PURPOSE title, and a small rotating observation icon by `TAIL N`.

~~~text
> logs x1

[X1]  Customer Checkouts                           TAIL 5
00:29.3  WRITE  checkout.validate          OK
00:30.7  WRITE  checkout.session.export    START
00:32.0  WRITE  checkout.commit            OK
00:42.4  READ   receipt.fetch              OK
00:42.9  READ   checkout.status.read       OK
~~~

It does **not** imply another logical connection or an independent route to another network node. The actual target comes from the connection; the log line's activity name describes observed work at that target.

## Log production is separate from the viewing window

Operations can emit **many** authored entries. The six canonical PURPOSEs currently have fixed schedules of **19–23 entries per connection**, and the manual checkout-evidence example has 24. These are V0 content choices, not quotas or retained-history caps; see [Phase 3](connections-purpose-logs-phase3-implementation.md).

- Mundane repeated application activity is most of the stream. Within a PURPOSE, that activity should feel like recurring instances (aggregate Customer Checkouts) or repeated sub-work of one heavyweight job (singular Sales Report Export).
- Entries should arrive in **bursts and quiet stretches**, not necessarily evenly spaced. One cluster might cross several WORK milestones in rapid succession, followed by meaningful silence.
- Some hostile connections may contain one incriminating observation among dozens of normal ones. Others have no suspicious log entries at all. Benign connections can include retries, errors, anomalies, and occasional alarming-looking content.
- The stream is finite with respect to its concrete plan: it does **not** fabricate infinite entries once per update simply to keep the window moving.
- A five-line window can reveal a clue briefly before later entries displace it; full earned history remains in the simulation, but the viewer displays at most the latest 30. Displacement creates attention pressure, not erasure.

There is no need for an endless background log writer or extra wall-clock emission system to get the desired effect.

## Authored entries belong to operations; WORK drives emission

**Implemented model:** each concrete READ/WRITE/EXECUTE operation can carry ordered, operation-local normalized WORK milestones with activity/result content. Opening LOGS never itself emits a line.

- As actual `CompletedWork` crosses a milestone, the simulator emits the entry once, with the current simulation timestamp, concrete connection identity, originating operation index/kind, and content.
- Contention delays emission because productive WORK advances more slowly. More allocated LOAD or a WORK-rate multiplier makes entries arrive earlier in simulation time. A 0× WORK rate (as used by active LOADERs) stops WORK-driven entries too, even if requested LOAD remains high. BACKDOOR's accelerated INFECTOR WORK likewise accelerates milestone crossings.
- If one fixed simulation step crosses several milestones, emit every eligible entry exactly once in stable authored order. A burst should not silently lose lines.
- Stage transitions must not replay earlier entries, reset chronological history, or reveal the next stage's future activity. The operation index must be captured before a completed stage advances and resets `CompletedWork`.
- A killed, canceled, withdrawn, or naturally completed connection can never produce future *unearned* WORK entries; it retains everything legitimately emitted before its resolution.

**Settled V0 milestone semantics:** normalized fractions become fixed thresholds against the concrete operation's base WORK when state is created. Changes to effective required WORK do not rescale them. The allocator emits earned entries before stage transitions, honoring its existing completion tolerance while capping emission at the effective WORK requirement so reduced requirements do not fabricate entries. Lines crossed within one simulation segment share its end timestamp.

The **simulator** owns the emitted facts. Presentation consumes the retained emitted history, never planned-but-unearned entries or hidden hostile intent. The existing completion-effect path remains authoritative: do not claim an infection was installed, a Payload launched, or a node changed state merely because a fictional log line says an attempt was prepared. Optional recorder provenance is useful, but the viewer should not read the recorder JSON as gameplay state.

## Presentation and lifecycle

The viewer is a compact terminal-history panel reusing INSPECT's identity and muted-resolution treatment.

- **Header:** verified canonical ID pill at left with `LOGS` beneath; claimed PURPOSE title to the right; `TAIL N` and a small rotating Lucide observation icon at top right. No explicit `LIVE` label. The spinner indicates **continued observation**, even during log silence or zero productive WORK, and pauses with scaled simulation time.
- **Rows:** earned timestamps, original READ/WRITE/EXECUTE label, activity, and optional result, with larger legible type and stacked rows at narrow widths. No invented service/node columns, hostile highlighting, nested scroller, or duplicate INSPECT progress bars.
- **Resolution:** immediately muted grey; the final earned tail holds for **four unscaled seconds** and then clips/shrinks to a **48-unit summary**. A freshly requested **historical** LOGS panel also gets only the same four-second hold before collapsing. The underlying earned history remains available for that run.
- **Addressability:** like `inspect` and `kill`, `logs` accepts only canonical connection IDs, not false hopping aliases. Its verified identity pill remains stable through alias changes and Probe expiry. Independent panels preserve history scrolling/input focus and invalidate bindings on run reset.

**Rationale:** resolved traffic is functionally less relevant than active threats; a prompt post-resolution collapse reinforces Uptime's focus on a *live* network rather than postmortem log analysis. See the [Phase 2 viewer note](connections-operation-logs-phase2-implementation.md) for precise UI behavior.

## Evidence quality, PURPOSE fiction, and difficulty

The [PURPOSE/log vocabulary document](connections-purpose-log-vocabulary-v0.md) is the V0 content reference. Its naming pass **has been implemented**: aggregate workloads such as **Customer Checkouts** and **Cart Price Calculations** use plural names; the heavyweight **Sales Report Export** remains singular. Stable IDs, targets, and operation arcs were preserved.

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

## Implemented playable slice and review focus

All six canonical PURPOSEs now emit finite, mostly mundane operation-specific streams. The manual zero-ambient `spawn logs-checkout-evidence` profile hides `checkout.session.export START` in Customer Checkouts WRITE activity before a real final-READ infection delivery. This clue does not itself cause the infection. Generated benign, LOADER, and INFECTOR plans inherit ordinary PURPOSE content, so some hostile connections have unremarkable logs.

Phase 1–3 witnesses cover emission, history, viewer, profile composition, and recorder provenance. Later live playtests should evaluate the attention cost, burst/quiet rhythm, and fairness of clues under contention and accelerated WORK before investing in more content systems.

## Remaining choices and deliberate deferrals

- **Content variation:** fixed V0 PURPOSE sequences and milestones may become predictable. A later seeded, authored composition system could vary vocabulary, volume, results, rhythm, and silence without simulating individual business transactions or disturbing traffic randomness.
- **Hostile evidence and disguise:** future encounter construction may choose overt, recognizable, contextual, ambiguous, or absent clues based on PURPOSE, villain, level, and run, separately from frequency/severity/objective. Maintain benign lookalikes and threats whose evidence lies outside LOGS; never require one guaranteed hostile line.
- **Deeper tools and history:** `logs -n` is limited to the latest **1–30** entries although more remain recorded. Filtering, `grep`/pipes, deep-history browsing, and richer Probe/trace/source-block interaction remain deferred and must be weighed against the live-network philosophy.
- **Timing and fairness:** test evidence before consequences under contention, 0× WORK, and BACKDOOR acceleration. Multiple lines in one simulation segment share the segment-end timestamp; finer interpolation is not part of V0.

The initial mechanic, UI, naming pass, and content are **implemented**. These are possible future directions, **not an approved roadmap**.
