# Connections — Business Importance V0

**PR #139 live palette refinement:** [Two independent presentation selectors](connections-palette-experiment-v0.md) now support ordinary Uniform/Muted Hues/Grayscale and Blended/Uniform Performance. Review defaults are Muted Hues + Uniform Performance. IMPORTANT identity remains bright; terminal/history pills follow live changes. Earlier descriptions below retain their phase provenance.

Connections implementation, based on human-accepted #137 normally merged at `f43b89758c07098e0939d59dfa429a25f7e81b82`. Small and Advanced retain existing workload, demand intervals, hostile tuning, capacities, objectives, allocator, service budgets and timeout tolerance. Packet is unchanged.

## Company importance and operator preference

Each individual logical request has `BusinessImportance.Ordinary` or `.Important`. IMPORTANT is trustworthy company information, independent of claimed PURPOSE. It persists through concrete-plan copies, workload scaling, logical identity, operations and committed route changes. It does not change business WORK, processing WORK/LOAD, service deadlines, effects, route eligibility or allocation weight. Hostile IMPORTANT assignments are rejected in V0. Hostile PURPOSE claims still qualify for API/CACHE normally.

The company determines consequences; the operator determines preferential processing. API preferential allocation can provisionally be described as FAST LANE. No API/SSH terminology redesign, automatic preference or new business priority mechanism is added here.

## Assignment and Inspector tuning

- **ConnectionTrafficDirector → Business importance → Important Connection Chance**: authored **0.15** in both canonical scenes. Every successfully generated legitimate concrete request draws once after recipe/anomaly composition, including developer-generated legitimate requests. A separate `System.Random(seed XOR 0x1B057A1)` stream resets with the existing director. PURPOSE, origin, anomalies, workload and hostile/arrival random streams consume no extra draws. Hostile generation does not draw from this stream.
- **SimulationHost → Business importance → Important Failure Multiplier**: authored **3**, positive integer. Restart after Inspector edits. Every admitted logical state freezes its failure cost: ordinary 1, IMPORTANT configured multiplier, hostile 0. A queued request's designation is already fixed; admission freezes the then-configured cost. Live Inspector tuning is not a gameplay configuration interface.
- Authored `LogicalConnectionDefinition` exposes the serialized **Importance** enum. Programmatic scenarios/future company-event producers may pass `importance:` to `ConnectionSpawnPlan` or use its immutable `WithBusinessImportance(...)` copy. No live designation-changing command is introduced. Explicit assignments have selection roll −1; generated legitimate requests carry their selection roll and chance. Copies preserve both assignment and evidence.

Importance never follows an alias or derives from an API/CACHE policy, PURPOSE, LOAD, rate, claimed legitimacy or palette. Existing identity hopping remains hostile-only; its rules are not expanded to legitimate IMPORTANT requests.

## One failure receipt, weighted units

The existing `BusinessFailure` receipt ledger remains authoritative, recording exactly one unsuccessful legitimate whole connection. The host's canonical `Resolve` guard still claims once before accounting/observers.

- `LegitimateFailureCount` / `BusinessFailures.Count`: number of unsuccessful jobs, preserving their previous meaning.
- `BusinessFailureUnits`: sum of those receipts' immutable `Units`.
- Receipt `CumulativeTotal`: unsuccessful jobs; `CumulativeUnits`: weighted units. A failed IMPORTANT job normally has `Units = 3`, not three receipts.
- `Shift.failures` and existing HUD/result FAILURES show **units** against the allowance; additive `Shift.failedJobs` preserves the corresponding job count. The threshold uses `>=`, so 8/10 plus one IMPORTANT loss ends at 11/10 rather than clipping or requiring another failure.

Kill, timeout, BLACKOUT/cancellation and every other already-accounted unsuccessful legitimate resolution use this same seam. Success grants the original unscaled whole-job business WORK with no bonus. Intermediate operations grant none. Existing same-boundary CORE → FIRED → SHIFT COMPLETE precedence, frozen unresolved jobs and reset semantics remain unchanged. Recording can be disabled without changing accounting. No additional score or persistent HUD element exists.

## Shared identity color

The IMPORTANT family restores the exact six pre-redesign cyan/teal accents from `ConnectionIdentityPalette.cs` at pre-visual-integration commit `52f9b771`: `(0.36,0.87,0.91)`, `(0.35,0.91,0.76)`, `(0.43,0.82,0.92)`, `(0.43,0.89,0.79)`, `(0.31,0.85,0.84)`, `(0.49,0.87,0.75)`. These already existed as the lab's synthetic Priority family. That family is now named **Important**, retaining serialized enum value 1 and field migration aliases; no second palette is introduced. Ordinary muted values are unchanged.

`DisplayStyle.asset` is the color source. `ConnectionIdentityPalette` owns lifetime slots, delegates family/color lookup to that exact shared style, and retains historical assignments until reset while freeing live slots on departure. The presenter configures this reference from its existing gameplay display binding. Thus network rails, body/border, pills and sockets and terminal pills use the same request classification and slot. Inspect (compact/verbose, full resolved card and collapsed summary), LOAD and WORK identity pills were audited. Resolved status/text can remain quiet while their identity pills retain identity color. A missing renderer style has a neutral gray fallback rather than an independently maintained palette.

Heartbeat, interpolation, missed beats, LOAD density, stationary stalls, operation direction, progress, geometry, REROUTING and service danger keep their existing owners. Danger still takes precedence on warning rails/socket; the pill retains base identity, including bright IMPORTANT color at risk. No badges, mandatory IMPORTANT text, sounds or special animation are added. Display Lab comparison headings are diagnostic fixture labels only.

The existing lab retains 26 samples, shared assets, layout and progress rack. Its prior synthetic priority examples become IMPORTANT; matched slowdown and danger comparisons and a synthetic CONNECTING/REROUTING reveal sequence use the same shared graphic. Gameplay's actual rerouting remains the authoritative mechanical evidence.

## Recorder additions (schema 4, observational)

- Run metadata: effective `importantConnectionChance` and `importantFailureMultiplier`.
- Queued/spawned/resolved `definition`: `importance`, `importanceSelectionRoll`, `importanceSelectionChance`. Queue event time records when the assigned concrete plan was observed. A generated ordinary assignment is also recorded; −1 denotes explicit assignment without a probability draw.
- `connection_spawned`, `connection_resolved`, and connection snapshots: designation and frozen `businessFailureCost` (snapshots use the same field). `business_failure` contains designation, configured cost and actual `businessFailureUnits` incurred.
- `businessFailureTotalUnits` is separate from unchanged `legitimateFailureTotal` (job count) on failure/resolution events and network snapshots. `Shift.failures` is units, `failedJobs` is count. `business_work_credit` retains ordinary original-value semantics.

Old schema-4 logs lack these additive fields: absent importance means the earlier all-ordinary contract; absent unit totals must be interpreted using that historical contract rather than zero. Field absence is not equivalent to explicit zero hostile cost. The earlier [API/CACHE sampled-allocation map](../Review/ConnectionsOperationRoutingCacheV0/RecorderCompleteness/README.md) continues to apply. No analytics, counterfactual allocation, new sampling frequency or per-frame events are added.

## Deferred direction

IMPORTANT is authoritative and trustworthy by default. Future hackers, compromised systems or company circumstances may deliberately undermine that trust as a late-game environmental escalation that breaks a learned rule. This is not ambient spoofing, a hidden current chance or a designed implementation in this phase.

Company events/contextual importance, additional tiers, deception, extra rewards/deadlines, broader ordinary palette changes, preferential-allocation effectiveness visualization and SSH overhaul remain deferred. [Review evidence and manual fixture](../Review/ConnectionsBusinessImportanceV0/README.md) distinguish automated validation from human gameplay acceptance.
