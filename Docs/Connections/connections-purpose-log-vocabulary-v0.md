# Connections — PURPOSE Activity Vocabulary & Initial Log Corpus V0

**Status:** Working content reference, November 5, 2026. A docs-only starting dataset for the first Connections logs experiment; not an implemented feature, an approved generation grammar, or a gameplay/UI contract.

**Scope:** Connections, not Packet. This page captures how the six **existing canonical PURPOSES** should sound when expressed through repeated, operation-local log activity. It is deliberately separate from [Operation Logs & Live Tailing](connections-operation-logs-design.md) (the mechanic), [Purpose & Anomaly Generation](connections-purpose-anomaly-generation-v0.md) (the current generated recipes), and [THREATS](THREATS.md) (objectives, cover, and disguise sophistication). Use the actual scene/code for current names, targets, operation arcs, and values.

## Core fiction

**PURPOSE defines what a connection is accomplishing; logs offer glimpses of how that work is being accomplished.**

Connections is an abstraction, not necessarily one HTTP request or one transaction. There are at least two useful fictional scales:

- **Aggregate workload:** one long-lived logical connection represents many similar business activities. Example: **Customer Checkouts** may repeatedly process different checkouts.
- **Singular heavyweight job:** one long-lived connection represents one substantial piece of work. Example: **Sales Report Export** can repeatedly read, aggregate, and emit report segments without becoming many separate exports.

Both use the **same** logical connection, target, READ/WRITE/EXECUTE arc, LOAD/WORK rules, and eventual logging mechanic. Neither scale demands a perfect narrative lifecycle or separately simulated sub-transactions.

Repetition is a feature, not filler: for an aggregate purpose it evokes successive instances; for a heavyweight purpose it evokes incremental processing within the same job.

## Canonical PURPOSE baseline and proposed display names

These IDs, targets, and operation kinds reflect the current six built-in canonical-purpose recipes. The **proposed display names** are a content direction from the Logs V0 design discussion, **not names already changed in code**. Keep internal purpose IDs stable when implementing the later naming pass.

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

## Contrasting evidence examples, not a signature catalog

The same vocabulary can be contextual, innocuous, or suspicious depending on the claimed PURPOSE and established baseline. Examples below are *candidate encounter writing*, not runtime hostility tags or mandatory tells.

| Example observation | Fictional context | Why it might matter |
| --- | --- | --- |
| report.chunk.emit OK | Sales Report Export | Expected progress, no suspicion implied |
| report.export START | Customer Authentications | Contextual mismatch with the claimed workload; not a claim of access to a separate DB node |
| credentials.download START | Customer Authentications | Recognizably concerning, but not a formal threat classification |
| checkout.session.export START | Customer Checkouts | Contextual evidence during otherwise normal checkout activity |
| login.token.verify REJECTED | Customer Authentications | Benign failures should happen; error does not equal attacker |
| payment.authorize RETRY | Customer Checkouts | Ordinary retry noise, not necessarily fraud |
| inventory.batch.conflict RETRY | Inventory Reconciliations | Operational friction without a threat |
| INITIATE_ATTACK_PROTOCOL START | Any deliberately overt/tutorial threat | Intentionally comical, obvious exception; not a default style |
| ordinary repeated activity only | A disruptive LOADER | Threat's useful evidence may be LOAD, not logs |

No line by itself proves or disproves hostility. A player may choose logs as a deep investigation tool but should not be trained to run it on every connection to find a fixed giveaway.

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
