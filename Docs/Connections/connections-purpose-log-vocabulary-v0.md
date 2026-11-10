# Connections — PURPOSE Activity Vocabulary & Initial Log Corpus V0

**Status:** Working content reference, November 5, 2026. Originally a docs-only starting dataset for the first Connections logs experiment; not a generation grammar or gameplay/UI contract. Phase 3 implements a finite corpus based on it, as noted below.

**Scope:** Connections, not Packet. This page captures how the six **existing canonical PURPOSES** should sound when expressed through repeated, operation-local log activity. It is deliberately separate from [Operation Logs & Live Tailing](connections-operation-logs-design.md) (the mechanic), [Purpose & Anomaly Generation](connections-purpose-anomaly-generation-v0.md) (the current generated recipes), and [THREATS](THREATS.md) (objectives, cover, and disguise sophistication). Use the actual scene/code for current names, targets, operation arcs, and values.

**Phase 3 implementation addendum:** The six proposed display names and a
finite V0 corpus based on this page are implemented on the Phase 3 branch;
see the [implementation note](connections-purpose-logs-phase3-implementation.md).
The original proposal wording and candidate evidence below remain as
provenance, not a procedural generation rule.

## Core fiction

**PURPOSE defines what a connection is accomplishing; logs offer glimpses of how that work is being accomplished.**

Connections is an abstraction, not necessarily one HTTP request or one transaction. There are at least two useful fictional scales:

- **Aggregate workload:** one long-lived logical connection represents many similar business activities. Example: **Customer Checkouts** may repeatedly process different checkouts.
- **Singular heavyweight job:** one long-lived connection represents one substantial piece of work. Example: **Sales Report Export** can repeatedly read, aggregate, and emit report segments without becoming many separate exports.

Both use the **same** logical connection, target, READ/WRITE/EXECUTE arc, LOAD/WORK rules, and eventual logging mechanic. Neither scale demands a perfect narrative lifecycle or separately simulated sub-transactions.

Repetition is a feature, not filler: for an aggregate purpose it evokes successive instances; for a heavyweight purpose it evokes incremental processing within the same job.

## Canonical PURPOSE baseline and proposed display names

These IDs, targets, and operation kinds reflect the six built-in canonical-purpose recipes at this document's original baseline. The **proposed display names** were a content direction from the Logs V0 design discussion and are implemented in Phase 3. The old-name column preserves the pre-implementation claim. Internal purpose IDs remain stable.

| Stable purpose ID | Current player-facing claim | Proposed player-facing claim | Target | Normal arc | Fictional scale |
| --- | --- | --- | --- | --- | --- |
| product-lookup | Product Detail Retrieval | **Product Detail Lookups** | DB | READ | Aggregate lookups |
| customer-login | Customer Authentication | **Customer Authentications** | AUTH | EXECUTE | Aggregate attempts/sessions |
| cart-pricing | Cart Price Calculation | **Cart Price Calculations** | API | EXECUTE | Aggregate cart recalculations |
| order-submit | Checkout Order Submission | **Customer Checkouts** | API | WRITE → READ | Aggregate checkouts/confirmations |
| inventory-sync | Inventory Reconciliation | **Inventory Reconciliations** | DB | READ → WRITE | Aggregate reconciliation batches |
| sales-export | Sales Report Export | **Sales Report Export** | DB | READ | One heavyweight export |

The naming pass should affect the displayed PURPOSE claim, not the target, route, arc, demand, WORK values, weights, objective selection, or infection mechanics. The singular/plural choice is a *fictional scope*, not a request for separate simulation types. Some future PURPOSES may also be intentionally singular.

## Content grammar and guardrails (not a parser grammar)

**Suggested line vocabulary:** a terse dotted activity name plus an optional concise result. The existing operation kind supplies the READ/WRITE/EXECUTE label; the viewer may supply simulation timestamp, current target and connection identity from real simulation state.

Examples (activity and result only):

~~~text
checkout.received         OK
payment.authorize         RETRY
receipt.fetch             OK
report.segment.read       OK
report.aggregate.partial  OK
~~~

Conventions for this initial corpus:

1. **Topology honesty:** do not introduce an AUTH, DB, API, CACHE or other *service/source column* that appears to describe a separate node involved in the connection. If a node is identified at all, use the connection's actual target as supplied by the simulation. A dotted application activity is not evidence of network travel, another connection, or work on another node.
2. **Aggregate honesty:** repeated checkout/authentication/lookup lines imply many activities under one abstract connection, not individually simulated orders, customers, sessions, packets, or new routes. For a singular export, repeated segments belong to the one job.
3. **Operation honesty:** author messages in the vocabulary of the active operation's work. A WRITE followed by READ can describe multiple checkouts in its WRITE phase and many confirmation checks in its READ phase. Individual messages are illustrative observations, not independently executable operation stages.
4. **No hidden classification in formatting:** suspicious events use the same syntax, density, color, and status conventions as ordinary events. A line is not an automatic verdict; an error, retry, unusual action, or high-volume pattern can be legitimate.
5. **No fake completion:** don't claim a node infection, malicious completion effect, Payload launch, or remote action has succeeded before the actual simulation event. Content can foreshadow an effect or indicate suspicious preparation.
6. **Skill ceiling:** logs are the deepest, expert-facing investigation layer. They may feel arcane to beginners. Prefer coherent learnable vocabulary over explanations, badges, highlighting, or automatic suspicion scoring.

Potential result words: OK, HIT, MISS, START, DONE, RETRY, SKIP, REJECTED, CONFLICT, QUEUED. These are examples, not a required enum. A plain operation/entry can omit a result.

Log output remains fictional application telemetry; no separate application transaction engine, row database, retry scheduler, payment authorization service, or secondary network topology is implied.

## Initial reusable corpus by PURPOSE

The lines below are **content candidates**, not exact spawn-time templates, fixed required sequences, or frequency distributions. Reorder, repeat, omit, and vary results to establish normal-looking activity. A single event can occur multiple times without creating a new logical connection.

### Product Detail Lookups — DB, READ, aggregate

| Activity | Illustrative result |
| --- | --- |
| product.key.normalize | OK |
| product.sku.lookup | HIT |
| product.sku.lookup | MISS |
| product.variant.read | OK |
| product.price.read | OK |
| product.availability.read | OK |
| product.catalog.scan | OK |
| product.detail.assemble | OK |
| product.batch.lookup | DONE |
| product.detail.return | OK |
| product.lookup.retry | RETRY |

A typical run repeats lookups and reads, sometimes misses, and continues with another lookup. It need not describe one product from start to finish.

### Customer Authentications — AUTH, EXECUTE, aggregate

| Activity | Illustrative result |
| --- | --- |
| login.attempt | START |
| login.token.parse | OK |
| login.token.verify | OK |
| login.token.verify | REJECTED |
| login.challenge.request | START |
| login.challenge.verify | OK |
| login.policy.check | OK |
| login.session.issue | OK |
| login.session.refresh | OK |
| login.session.validate | OK |
| login.attempt | RETRY |
| login.session.expire | OK |

A rejected token or repeated challenge can be ordinary customer activity. Repeated logins are appropriate; no single-user lifecycle is required.

### Cart Price Calculations — API, EXECUTE, aggregate

| Activity | Illustrative result |
| --- | --- |
| pricing.cart.open | OK |
| pricing.item.normalize | OK |
| pricing.promo.evaluate | NONE |
| pricing.promo.evaluate | APPLIED |
| pricing.tax.estimate | OK |
| pricing.shipping.estimate | OK |
| pricing.discount.reconcile | OK |
| pricing.cart.total | OK |
| pricing.cart.reprice | START |
| pricing.total.commit | OK |
| pricing.calculation.retry | RETRY |

A series of different carts can produce similar totals and promotion checks. No actual cart entity or separate payment transaction is simulated.

### Customer Checkouts — API, WRITE → READ, aggregate

**WRITE activity candidates:**

| Activity | Illustrative result |
| --- | --- |
| checkout.received | OK |
| checkout.validate | OK |
| payment.authorize | OK |
| payment.authorize | RETRY |
| checkout.commit | OK |
| checkout.received | OK |
| checkout.batch.flush | OK |
| checkout.confirmation.prepare | OK |
| checkout.receipt.queue | QUEUED |

**READ activity candidates:**

| Activity | Illustrative result |
| --- | --- |
| checkout.status.read | OK |
| receipt.fetch | OK |
| receipt.fetch | RETRY |
| checkout.confirmation.read | OK |
| receipt.lookup | HIT |
| checkout.status.read | OK |
| checkout.confirmation.read | OK |

The same connection can handle many checkouts during WRITE and multiple confirmation/receipt lookups during READ. A phase boundary is still a real operation transition, not a new connection.

### Inventory Reconciliations — DB, READ → WRITE, aggregate

**READ activity candidates:**

| Activity | Illustrative result |
| --- | --- |
| inventory.snapshot.open | OK |
| inventory.segment.read | OK |
| inventory.batch.compare | OK |
| inventory.discrepancy.count | 3 |
| inventory.discrepancy.count | 0 |
| inventory.stock.check | OK |
| inventory.segment.read | OK |
| inventory.snapshot.verify | OK |

**WRITE activity candidates:**

| Activity | Illustrative result |
| --- | --- |
| inventory.delta.prepare | OK |
| inventory.delta.apply | OK |
| inventory.ledger.update | OK |
| inventory.batch.conflict | RETRY |
| inventory.delta.apply | OK |
| inventory.reconciliation.commit | OK |
| inventory.commit.verify | OK |

Repeated comparisons and adjustments evoke successive inventory batches. Counts are illustrative content, not newly simulated inventory quantities.

### Sales Report Export — DB, READ, singular heavyweight

| Activity | Illustrative result |
| --- | --- |
| export.job.open | OK |
| report.query.prepare | OK |
| report.partition.read | OK |
| report.segment.read | OK |
| report.segment.read | OK |
| report.aggregate.partial | OK |
| report.index.seek | OK |
| report.chunk.emit | OK |
| report.segment.read | OK |
| report.aggregate.partial | OK |
| report.footer.prepare | OK |
| report.finalize | OK |
| export.job.complete | OK |

This is intentionally singular. One large export can process numerous segments and aggregates while remaining a single READ operation. The exact number of segments, rows, chunks and partial aggregates is fictional color, not an additional resource simulation.

## Worked stream A — Customer Checkouts (aggregate, two operations)

This example demonstrates multiple instances of checkout activity under one connection; clustered emissions; a contextual clue during an otherwise ordinary WRITE; and later READ activity that can roll that clue out of a short tail. **The contextual clue is an authored example, not a mandatory signature on every hostile checkout.**

Milestones are *illustrative percentages of WORK completed within the named operation*. They show creative rhythm only; the runtime representation, interpretation under dynamic WORK requirements, and exact tuning belong in the later Logs V0 implementation contract.

| Operation | Illustrative progress | Activity | Result |
| --- | ---: | --- | --- |
| WRITE | 3% | checkout.received | OK |
| WRITE | 4% | checkout.validate | OK |
| WRITE | 5% | payment.authorize | OK |
| WRITE | 14% | checkout.commit | OK |
| WRITE | 17% | checkout.received | OK |
| WRITE | 18% | payment.authorize | RETRY |
| WRITE | 19% | payment.authorize | OK |
| WRITE | 21% | checkout.commit | OK |
| WRITE | 51% | checkout.received | OK |
| WRITE | 53% | checkout.validate | OK |
| WRITE | 54% | checkout.session.export | START |
| WRITE | 55% | payment.authorize | OK |
| WRITE | 56% | checkout.commit | OK |
| WRITE | 85% | checkout.batch.flush | OK |
| WRITE | 92% | checkout.receipt.queue | QUEUED |
| READ | 4% | receipt.fetch | OK |
| READ | 5% | checkout.status.read | OK |
| READ | 6% | receipt.fetch | OK |
| READ | 18% | receipt.fetch | RETRY |
| READ | 19% | receipt.fetch | OK |
| READ | 40% | checkout.confirmation.read | OK |
| READ | 71% | receipt.fetch | OK |
| READ | 72% | checkout.status.read | OK |
| READ | 93% | checkout.confirmation.read | OK |

At WRITE 54%, the connection appears to be exporting session material while processing customer checkouts. The same line format is used as for ordinary messages. It hints at a problem without asserting an infection or crossing to another target. The later READ operation can remain superficially ordinary.

## Worked stream B — Sales Report Export (one heavyweight job)

This example stays singular while being repetitive. It has no hostile tell; its primary interest is the natural workload rhythm. Several similar entries can arrive in a burst, followed by quiet progress until another chunk is prepared.

| Operation | Illustrative progress | Activity | Result |
| --- | ---: | --- | --- |
| READ | 2% | export.job.open | OK |
| READ | 3% | report.query.prepare | OK |
| READ | 4% | report.partition.read | OK |
| READ | 12% | report.segment.read | OK |
| READ | 13% | report.aggregate.partial | OK |
| READ | 22% | report.segment.read | OK |
| READ | 23% | report.segment.read | OK |
| READ | 24% | report.aggregate.partial | OK |
| READ | 40% | report.chunk.emit | OK |
| READ | 41% | report.segment.read | OK |
| READ | 42% | report.aggregate.partial | OK |
| READ | 55% | report.segment.read | OK |
| READ | 56% | report.index.seek | OK |
| READ | 57% | report.segment.read | OK |
| READ | 58% | report.aggregate.partial | OK |
| READ | 72% | report.chunk.emit | OK |
| READ | 86% | report.footer.prepare | OK |
| READ | 94% | report.finalize | OK |
| READ | 98% | export.job.complete | OK |

The export repeatedly reads and aggregates, not because it is secretly many exports, but because one report requires many processing steps. Whether an individual line is technically emitted by a READ operation is part of the game's abstract fiction, not a real filesystem or database trace.

## Evidence vocabulary — not a threat-signature catalog

This is an **initial authored evidence corpus**, not a procedural hostile-line generator. The entries are fictional observations that a writer may place among ordinary PURPOSE-specific activity. **Their grouping below is an authoring note, never a property exposed to the player.** In the actual log they have exactly the same typography, operation label, timestamp treatment, and ordinary result vocabulary as any other line.

Use these as examples of *what a line might lead the player to suspect*, not automatic proof of attacker state or proof that a gameplay effect happened. A hostile connection can have unremarkable logs, and a benign one can have unexpected entries or errors. Nothing in this corpus assigns specific attack genera, infection kinds, or fixed evidence chances.

### Candidate overt, recognizable, and contextual clues

The 22 candidates below deliberately range from silly to subtle. For **contextual** examples, the sample PURPOSE is the *unexpected* context; the final column names where the same action would seem ordinary. An event may be inappropriate for a PURPOSE without being impossible at that connection's real target. If a proposed line implicitly requires a new route or node, rewrite or reject it.

| Authoring feel | Candidate line | Illustrative connection and operation | What the player might infer |
| --- | --- | --- | --- |
| Overt | `attack.protocol.arm START` | Deliberately obvious hostile connection, appropriate active stage | Absurdly conspicuous preparation |
| Overt | `stealth.mode.enable START` | Tutorial/demo threat, any appropriate operation | Intentionally obvious evasive intent |
| Overt | `audit.disable.request START` | Overt hostile connection, EXECUTE | Suspicious intent, *not* proof auditing was disabled |
| Recognizable | `login.session.enumerate START` | Customer Authentications, EXECUTE | Bulk session probing rather than normal authentication |
| Recognizable | `login.token.bulk.issue START` | Customer Authentications, EXECUTE | Unusually broad credential issuance |
| Recognizable | `checkout.session.export START` | Customer Checkouts, WRITE | Session material being packaged during checkouts |
| Recognizable | `payment.authorization.replay START` | Customer Checkouts, WRITE | Reuse of prior payment authorizations |
| Recognizable | `pricing.rule.force START` | Cart Price Calculations, EXECUTE | Bypassing ordinary price evaluation |
| Recognizable | `pricing.discount.override START` | Cart Price Calculations, EXECUTE | Unusual adjustment of pricing decisions |
| Recognizable | `inventory.delta.inject START` | Inventory Reconciliations, WRITE | Manufactured adjustments among legitimate inventory updates |
| Recognizable | `inventory.ledger.skip START` | Inventory Reconciliations, WRITE | Skipping expected reconciliation bookkeeping |
| Recognizable | `product.catalog.bulk.dump START` | Product Detail Lookups, READ | Bulk extraction rather than ordinary lookups |
| Recognizable | `report.private_fields.include START` | Sales Report Export, READ | Unexpected inclusion of sensitive report fields |
| Contextual | `report.chunk.emit OK` | Customer Authentications, EXECUTE | Ordinary export work, but expected for Sales Report Export |
| Contextual | `receipt.fetch OK` | Inventory Reconciliations, READ | Ordinary confirmation work, but expected for Customer Checkouts |
| Contextual | `inventory.segment.read OK` | Product Detail Lookups, READ | Normal in Inventory Reconciliations, not normal product detail activity |
| Contextual | `product.variant.read OK` | Inventory Reconciliations, READ | Normal in Product Detail Lookups, questionable here |
| Contextual | `login.session.refresh OK` | Cart Price Calculations, EXECUTE | Normal in Customer Authentications, questionable during pricing |
| Contextual | `pricing.promo.evaluate APPLIED` | Customer Authentications, EXECUTE | Normal in Cart Price Calculations, questionable during login work |
| Contextual | `checkout.status.read OK` | Sales Report Export, READ | Normal in Customer Checkouts, unexpected in one export job |
| Contextual | `report.aggregate.partial OK` | Product Detail Lookups, READ | Normal in Sales Report Export, not obviously part of a product lookup |
| Contextual | `inventory.snapshot.open OK` | Sales Report Export, READ | Normal in Inventory Reconciliations, incongruous with the export claim |

Three overt clues and ten recognizable clues are deliberately *examples*, not a promise that each hostile run contains one. An attentive player may find one important line among many innocuous ones; alternatively, the right evidence may live in LOAD, WORK, the operation arc, or Probe instead of logs.

### Benign lookalikes and ambiguous observations

The following should appear naturally in legitimate traffic too. Don't let error- or retry-looking vocabulary become a trivial hostile classifier.

| Candidate line | Plausible legitimate reason |
| --- | --- |
| `login.token.verify REJECTED` | Mistyped or expired credentials |
| `payment.authorize RETRY` | Ordinary payment retry |
| `inventory.batch.conflict RETRY` | Competing reconciliation work |
| `product.sku.lookup MISS` | Missing or retired catalog item |
| `report.segment.read RETRY` | Transient retry within one long export |

Even a recognizable clue might turn out to be part of an unusual authorized process in a later authored encounter. The fiction should allow interpretation and uncertainty, not demand that every ominous token is a real attack.

### Short embedding sketches

These are **compositions of previously listed vocabulary**, not new implementation fixtures. They show how evidence can be buried amid repeated legitimate activity without a special clue format.

**Customer Checkouts — suspicious WRITE amid ordinary aggregate work:**

~~~text
WRITE  checkout.received             OK
WRITE  checkout.validate             OK
WRITE  payment.authorize             OK
WRITE  checkout.commit               OK
WRITE  checkout.received             OK
WRITE  checkout.session.export       START
WRITE  payment.authorize             RETRY
WRITE  checkout.commit               OK
~~~

**Customer Authentications — contextual EXECUTE anomaly among many attempts:**

~~~text
EXECUTE login.attempt                 START
EXECUTE login.token.verify            OK
EXECUTE login.session.issue           OK
EXECUTE login.attempt                 START
EXECUTE report.chunk.emit             OK
EXECUTE login.policy.check            OK
EXECUTE login.session.refresh         OK
EXECUTE login.token.verify            REJECTED
~~~

**Sales Report Export — one heavyweight READ whose repetitive work hides an unusual choice:**

~~~text
READ   report.segment.read           OK
READ   report.aggregate.partial      OK
READ   report.segment.read           OK
READ   report.private_fields.include START
READ   report.chunk.emit             OK
READ   report.segment.read           OK
READ   report.aggregate.partial      OK
READ   report.footer.prepare         OK
~~~

The last example is still a **single export**, not many separate exports. In all three cases the emitted lines describe abstract activity at the connection's actual target; they do not establish secondary network traffic, completed infections, or new gameplay state.

### Placement and fairness notes for later encounter authoring

- Volume, burst timing, clue frequency, and recognizability are independently useful choices. More difficult does not simply mean more lines or one smaller keyword.
- A meaningful clue may appear during an earlier apparently benign operation, foreshadowing a later dangerous operation. The rolling tail must retain **emitted** lines across operation boundaries, subject to the player's display count.
- A line can arrive in a burst and later leave the default five-line view. The player may retrieve older emitted lines with a larger tail; **not currently on screen** does not mean **deleted from the connection's history**.
- For encounters where logs are meant to be the actionable path, don't place the only useful clue after the effect has already happened, or bury it at an impossible reaction time. Other encounters can deliberately be easier to diagnose through a different tool.
- Avoid always pairing a particular entry with one genus or infection; the eventual connection grammar and villain disguise composition can select a distribution of evidence. No frequency table, evidence tier enum, or procedural framework is approved here.

## Relationship to the proposed live-tail viewer

This page is **not** the viewer implementation spec. It is designed to supply content for the separate Logs V0 handoff. The latest working interaction direction discussed on November 5 is:

- Every operation may emit a **much longer stream** of entries as actual WORK progresses, often in clusters with gaps.
- A connection retains emitted entries across operation transitions and after resolution; killing prevents future emission.
- The default live tail shows the most recent **five** entries regardless of operation, with each line labeled by its actual operation.
- A larger tail (for example, **logs -n 20 x1**) uses the same panel with a greater display budget. These are viewer decisions, not log-content grammar.
- A five-line **visible window is not a five-line production budget**. A connection may produce dozens of lines over its lifetime.
- Filtering/pipelines such as grep, automatic evidence scoring, and authored per-villain generation are deferred.

These are conversation-level directions and may supersede older examples in the separate logs design document, which still describes a five-entry *total* feel and a current-operation-only view. Reconcile that mechanic document when preparing the implementation handoff; do not silently implement its older suggestions.

## Future composition questions (deferred)

Eventually a connection planner may choose normal activity vocabulary, volume, emission rhythms, evidence location, and disguise quality based on PURPOSE, operation, objective, villain, level, and run. This follows [THREATS](THREATS.md)'s principle that disguise sophistication is separate from frequency/severity, and the deferred canonical connection/threat grammar work.

Do **not** build a procedural log-generation framework, fixed evidence tiers, one-clue-per-threat rule, new simulated business entities, or new operations from this document. For the first playable slice, a small number of complete, manually authored streams can prove the experience.

## Content review checklist

- The six current internal IDs, target nodes, and normal operation arcs are represented correctly.
- Proposed display-name changes are explicit and can be applied without changing ID keys or simulation balance.
- Aggregate and singular examples both feature repeatable activity without requiring different mechanics.
- No example implies traffic to another real or imaginary node, a new route, or an unmodeled gameplay effect.
- A normal workload can contain retries/errors; a hostile workload can contain mundane logs.
- Bursts and quiet WORK intervals exist in the complete examples.
- Each example remains legible with a five-line rolling window and makes sense when read with a larger tail.
- Unity compilation, scene review, prefab validation, and mechanical tests belong to the later implementation task, not this content document.
