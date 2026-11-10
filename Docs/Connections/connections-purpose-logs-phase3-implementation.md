# Connections Operation Logs V0, Phase 3: PURPOSE content

This is a Connections implementation and review note. The [vocabulary source](connections-purpose-log-vocabulary-v0.md)
supplies the activity language and two worked streams. The existing
`PlannedOperationLog` WORK milestones, retained history, and `logs` viewer
provide the mechanics; this phase adds finite content to the six canonical
PURPOSE profiles in the built-in catalog and the serialized
`ConnectionsAdvanced.unity` catalog.

| Internal ID | Player-facing PURPOSE | Stages and authored entries | WORK rhythm |
| --- | --- | --- | --- |
| `product-lookup` | Product Detail Lookups | READ 20 | Early lookup bursts; a MISS and RETRY; later batch and return |
| `customer-login` | Customer Authentications | EXECUTE 20 | Repeated attempts, a rejected token, challenge, refresh and retry |
| `cart-pricing` | Cart Price Calculations | EXECUTE 20 | Repeated carts, applied/absent promotions, repricing and retry |
| `order-submit` | Customer Checkouts | WRITE 14 → READ 9 | Repeated checkout/payment bursts, then receipt and status reads |
| `inventory-sync` | Inventory Reconciliations | READ 11 → WRITE 10 | Snapshot and comparison bursts, then deltas, conflict retry and commit |
| `sales-export` | Sales Report Export | READ 19 | One report job with segment/aggregate bursts and later finalization |

All entries have authored normalized WORK fractions within their own
operation. The distributions cluster near the start and at several later
milestones, with quieter gaps. The two checkout schedules and the report
schedule follow the vocabulary document's worked streams. Ordinary checkout
logs omit its suspicious example. Canonical IDs, targets, operation kinds,
LOAD ranges, WORK means, weights, and sampling remain unchanged; only the
agreed display names and log arrays changed.

Generated benign, LOADER, and INFECTOR plans start from these same operation
profiles. Existing stage construction, copying, and rewriting retain their
planned log arrays, including DELAYED stages and operation deviations. A
deviation can therefore make an ordinary activity sound incongruous with its
new operation. No hostile plan receives a guaranteed suspicious line. A
LOADER whose productive WORK is paused may retain planned entries without
earning later entries. This phase adds no random text draws.

## Manual checkout evidence

`spawn logs-checkout-evidence` is a zero-ambient-weight fixed hostile profile
in both scenes. It claims Customer Checkouts and targets API. Its 50-second
nominal WRITE carries 15 mostly ordinary lines; at 54% WORK it emits
`checkout.session.export START` in the same format as surrounding entries.
Four later WRITE lines and nine READ lines can push that clue out of the
default five-entry tail. Its final 35-second nominal READ has the existing
`InfectionDelivery` completion effect, applying Phantom LOAD at API only
if that operation completes. At authored uncontended LOAD, the total nominal
window is about 85 seconds, with the clue around 27 seconds into WRITE.
Contention and interruption alter the observed timeline. The clue itself
is fictional telemetry and has no effect.

The [synthetic `logs-demo`](connections-operation-logs-phase2-implementation.md)
remains manually spawnable and zero weight. It is separate from the normal
PURPOSE corpus.

## Validation and limits

`PurposeLogsWitness` checks both catalogs, six baseline recipes, each
operation's log schedule, benign and hostile plan copying, seeded replay,
WORK-dependent timing under different requirements and shared LOAD,
the evidence clue before delivery, and recorder plan/emission events. It also
reads the saved JSON to confirm the concrete clue and all 24 emitted entries.
After one simulation second, the focused duration fixture observed 7 versus
3 entries for short versus long WORK, and 11 versus 7 entries without versus
with shared-target LOAD contention.

Unity 6000.3.11f1 batch Editor validation in an isolated project passed the
focused witness and all nine selected regressions: Traffic, Recorder,
Operation Logs, LOGS Output, Behavior, INFECTOR, LOADER, BACKDOOR, and
Infection. The focused witness passed again after the saved-JSON assertion.
Interactive Play Mode review has not been run.

These streams repeat verbatim for a given PURPOSE and operation. They are a
finite V0 playtest corpus, not a transaction engine, procedural hostile
evidence system, or final connection-construction grammar.

At 16:9 in both `Connections.unity` and `ConnectionsAdvanced.unity`, try
`spawn product-lookup`, `customer-login`, `order-submit`, and
`sales-export`; use `logs <id>` and `logs -n 20 <id>`. Compare benign
Customer Checkouts with `spawn logs-checkout-evidence`. Also try
`spawn order-submit obj:loader` and `obj:delivery`. Review ordinary
chatter, burst/quiet pacing, the clue's visibility in five versus twenty
rows, LOADER plausibility, terminal scrolling, and retained history.
Interactive Play Mode review in both scenes remains necessary.
