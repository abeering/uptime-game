# Connections — Legitimate Workload Increase V0

Based on accepted, human-playtested #132, normally merged at `9ceff51e5e1b4a6a4dd7b6ae61c25a94b54a82fc`. Scope: Small `Connections.unity` and Advanced `ConnectionsAdvanced.unity`. Accepted provisional **Legitimate Workload Multiplier = 2.5** in each scene. This is incremental gameplay tuning, not final difficulty balance or a new demand scheduler.

Human playtesting of #134 accepted the implementation and selected 2.5× for both canonical scenes after observing brief legitimate overload. The earlier 1.5× evidence remains historical; final balance is open. Focused acceptance checks additionally cover the authored 2.5×, unchanged nominal budget/business value and real queued completion.

## Generation and business value

`ConnectionTrafficDirector` finishes ordinary sampling, benign/hostile selection and anomaly composition before scaling a concrete **non-hostile** generated plan. Each operation's requested base LOAD and required processing WORK are multiplied by the configured factor. Their WORK/LOAD ratio, summed nominal duration and nominal-duration-based service budget remain equal to the equivalent unscaled plan, subject to ordinary float precision. A 100-WORK / 5-LOAD operation becomes 150 processing WORK / 7.5 LOAD; both have nominal duration 20s and, at unchanged tolerance 1.5, budget 30s.

`ConnectionSpawnPlan.BusinessWorkValue` captures the sum of concrete operation WORK **after sampling/anomalies and before workload scaling**. The scaler and existing event/identity/route-copy methods preserve it. `LogicalConnectionDefinition.FromPlan` carries this value into runtime state. Authored definitions without a generated value retain the existing sum-of-authored-WORK fallback. Business value remains independent of later processing modifiers.

The equivalent successful job still credits 100 business WORK, not 150. Intermediate processing, failed jobs and hostile completion still credit zero through the unchanged authoritative resolution seam. Shift targets (Small 1,000 / Advanced 1,500), failure limit 10, outcome priority, exact-once receipts, freeze and restart are unchanged.

Scope includes ambient benign PURPOSEs and non-hostile manual profile generation through the same builder. Explicit already-concrete `QueueSpawnPlan` submissions, authored Witness definitions and the dedicated high-demand witness are not rescaled. Hostile PURPOSE variants, LOADER/INFECTOR composition, Spawner children, launchers and identity selection retain their previous inputs. Shared profile recipes are never modified by scaling.

Scaling adds no random draws. The same seed/configuration produces the same profile, route, operation structure, anomaly selections, effects, logs and behavior definitions. Scaling after anomaly composition preserves sampled static anomaly ratios and absolute dynamic keyword/WAVING offsets and timings. Natural-noise parameters/functions remain unchanged. Nominal duration is preserved; dynamic behavior, contention and actual completion can still change actual lifetime. With more requested LOAD, existing node capacity is exhausted sooner; that is the intended gameplay effect. Actual population feedback can consequently react to longer-lived jobs, but no scheduling or count-goal rule changes.

## Inspector and recording

In either canonical scene, select **SimulationHost → Connection Traffic Director → Legitimate generated workload → Legitimate Workload Multiplier**. It must be finite and positive; the component default is 1 for historical scenes. Restart after edits to regenerate plans and refresh run metadata. Already queued jobs retain their concrete values.

Schema 4 gains additive `run.legitimateWorkloadMultiplier` and `definition.businessWorkValue` fields. Queued definitions and final-operation generation traces show the actual scaled processing requirements; normal-operation trace values remain the sampled unscaled baseline. Existing connection snapshots and credit receipts retain the unscaled business value. Older records predate the multiplier and represent 1× generation; their missing fields do not imply zero workload or zero job value. No event stream or schema-number change is needed for these optional fields.

## Provenance and boundaries

Reviewed [#126](https://github.com/abeering/uptime-game/pull/126), head `9cc564807c9b6cd48e6900fb2d4a24b0569018dd`, as the donor for proportional benign-only LOAD/WORK scaling. This phase uses the current generator, scales after composition to preserve anomaly parameters, and explicitly separates business value from processing. It imports no arrival policy, occupancy ledger, PURPOSE weights, calibration matrix or operation-credit rules. #115/#124/#125/#126 remain untouched and unmerged.

No allocator, node-capacity, timeout tolerance, hostile frequency/composition, visuals, API, priority or Packet changes. Historical mechanical fixtures explicitly use 1× for their fixed examples; current authored 2.5× is tested separately. See [focused evidence and manual review](../Review/ConnectionsLegitimateWorkloadV0/README.md). The next decision is based on human gameplay feel.
