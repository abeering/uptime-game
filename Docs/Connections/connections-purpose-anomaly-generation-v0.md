# Connections — Purpose & Anomaly Generation V0

**Ambient Population V1 final WORK tuning (October 26, 2026):** The six built-in canonical PURPOSE recipes now use twice their original mean WORK at every normal operation. The table below gives the current values; the original means were 24, 24, 35, 60/18, 40/65, and 145 respectively. Requested LOAD ranges, purpose weights, ±25% WORK spread, generation rules, and the population controller are unchanged. Hostile plans derived from these PURPOSE baselines inherit the larger WORK; threat-specific duration behavior is deferred.

**Infections V0 Slice 1 follow-up:** DELIVERY plans now include a concrete Phantom LOAD payload and apply it on operation completion. Developer `spawn <purposeId>` objective overrides reuse purpose sampling and anomaly recipes while bypassing ambient objective probability rolls. See [the infection contract](connections-infections-v0.md). The original generation-phase description below records its pre-infection behavior.

**Status:** Focused Connections implementation, stacked on Generated Hostile Traffic V0. The source proposal is `Uptime_Connections_Purpose_Anomaly_Generation_V0.md` supplied for this task. Its numbers are provisional playtest guesses, not a settled balance contract. Packet gameplay is unchanged.

## Current generator

The single seeded ambient scheduler first selects a purpose using the existing effective ambient-weight catalog. The scene retains a 3-second opening grace, 13.5-second interval, and ±1.5-second jitter. `ConnectionTrafficDirector` samples a normal operation plan with a fixed purpose target and route through the authored physical topology. Each operation samples requested LOAD from its range and WORK independently from mean ±25%; the allocator still advances WORK by allocated LOAD per simulation second. An anomaly changes the sampled concrete plan without changing the route or target.

| Purpose ID / claim | Target | Normal operations: kind [LOAD; mean WORK] | Weight |
|---|---|---|---:|
| `product-lookup` / Product Detail Retrieval | DB | READ [0.5–1.1; 48] | 1.5 |
| `customer-login` / Customer Authentication | AUTH | EXECUTE [0.8–1.6; 48] | 1.0 |
| `cart-pricing` / Cart Price Calculation | API | EXECUTE [1.0–2.0; 70] | 1.0 |
| `order-submit` / Checkout Order Submission | API | WRITE [1.5–3.0; 120] → READ [0.6–1.3; 36] | 0.9 |
| `inventory-sync` / Inventory Reconciliation | DB | READ [1.0–1.8; 80] → WRITE [1.7–3.2; 130] | 0.6 |
| `sales-export` / Sales Report Export | DB | READ [2.5–4.5; 290] | 0.25 |

The director's serialized rates start at 25% hostile, 50% DELIVERY among hostile, 8% benign outlier, 15% optional second hostile anomaly, and 10% subtle DELIVERY with no anomaly. These are independent generation rolls, not a separate hostile arrival clock. Manual `spawn <purposeId>` builds a normal benign purpose plan; all ten earlier fixed profiles remain manually spawnable. Their default ambient weights are zero, and the existing keyed weight override can re-enable any profile. Custom Inspector profile arrays retain the legacy duration-based builder unless marked as purpose templates with a positive mean WORK.

The four anomaly IDs are `operation-deviation`, `high-load`, `low-load`, and `work-deviation`. One operation deviation changes kind or appends a different kind at the end. High LOAD multiplies one operation's requested LOAD by 2.5–4; low LOAD multiplies it by 0.2–0.5. WORK deviation multiplies one operation's WORK by 2–4 or 0.25–0.5. A LOAD change alone preserves WORK, and a WORK change alone preserves LOAD. A second roll may modify the same operation again; the recorder retains both ordered anomaly IDs and the final concrete values.

LOADER always applies high LOAD and sometimes adds high WORK for persistence. It can also use a shared OVERLOAD or SPIKING LOAD keyword. It has no delivery effect. DELIVERY attaches the existing `InfectionDelivery` completion effect to the final operation, with zero, one, or occasionally two anomalies. Rare benign outliers use one anomaly or a shared keyword, and never carry delivery. Sales export itself is a legitimate high-demand example. These internal choices are not threat labels in inspect or network presentation. A delivery still records an occurrence at normal WORK completion only; it creates no persistent infection or direct Health penalty.

Each generated `ConnectionSpawnPlan` carries an internal generation trace copied into its `plan_queued` run event: purpose ID, objective, ordered anomaly IDs, and normal/final operation kind, requested LOAD, WORK, completion effect, and behavior IDs. Run metadata records the effective profile weights, purpose flag, mean WORK, and generation rates. This is additive version-1 diagnostic data. The terminal and inspect card consume only the ordinary connection state. The one queue/registration seam and `SimulationHost` mechanics are unchanged.

**Generation Decision Recording V0 follow-up:** The trace also names the generation branch, so intentional anomaly-free DELIVERY is distinct from missing evidence, and benign outliers are explicit. Selected keywords carry behavior ID, kind, and operation index. Every `plan_queued` event now includes the full concrete plan definition, including future operations and complete behavior settings, before the connection spawns. This remains internal recorder evidence; see [recorder format](connections-run-recorder-v0.md).

## Review

The Traffic Witness checks seeded replay, all six purposes, legal fixed targets, normal LOAD and independent WORK ranges, both objective recipes, benign outliers, subtle hostile attempts, LOAD/WORK mutation independence, recorder-event serialization, generated delivery interruption/completion, and the older manual lifecycle cases. The Recorder Witness checks profile/rate metadata and JSON generation provenance alongside its existing lifecycle and Health cases. Run the Resource, Behavior, Network Health, Inspect V2, and Geometry witnesses for regression coverage. In 16:9 interactive Play Mode, inspect the same claimed purpose in benign and hostile cases, an anomalous benign case, a normal-looking DELIVERY attempt, and a legitimate sales export. Use a marked seed and the internal run log to compare what the player saw with generation truth afterward. This remains a small playtest scenario; it does not add logs, probes, trace, stealth telemetry, infection state, cleaning, a Level Director, scoring, or topology changes.
