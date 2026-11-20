# Connections — Hacker Directives V0

Experimental Connections implementation, November 20, 2026. Stacked on Integrity PR #117 (`0c1fd9ab4ae8e7a9094623052ecaab23261003ad`), which is stacked on Maximum Pressure PR #116. Living Shift #115 is excluded. This contract concerns the Integrity scene only; Packet and the other Connections scenes retain their behavior.

## Control and objective

`ConnectionsAdvancedIntegrity.unity` alone has a `HackerDirectives` component on SimulationHost. Its authored default is **SPREAD**, the exact ambient control. Select **BREACH** in the Inspector to test path preference. Provisional Inspector settings are 60 simulation seconds of commitment and 0.7 preference probability. There is no automatic switching, HUD, terminal command, difficulty adjustment, new threat, or direct damage from this component. The existing 2/2 Integrity model and all consequences remain authoritative.

BREACH reads actual Integrity, including unrevealed damage. It evaluates physical paths from every configured external ingress to every configured internal critical node, charging each internal arrival its remaining Integrity and external arrivals zero. Compromised nodes cost zero, including compromised critical nodes. A small Dijkstra traversal per ingress finds minimum-cost paths; this is target selection, not an attack scheduler. No network IDs are embedded in production selection code.

Integrity exposes nodes in ordinal ID order. A separate seeded Fisher–Yates permutation gives equal-distance nodes and predecessors a deterministic priority. The selector then chooses uniformly among the minimum-cost ingress/critical candidates. It does not enumerate every equal-cost simple path or promise a uniform distribution across all possible routes. Visited nodes prevent zero-cost cycles from creating cyclic predecessor chains. Disconnected criticals are skipped; if none are reachable, the offensive records that fact and attacks retain ambient behavior.

An offensive is selected lazily at the first hostile opportunity. It is retained across damage changes. At each subsequent opportunity, the selector reassesses only if the commitment has expired, a route node/edge/root/critical has become invalid, or every internal node on the route is compromised. Thus expiry is serviced at the next opportunity, not by an additional clock. An empty/disconnected objective is retried at commitment expiry. The existing host ends a completed breach; the selector never ends a run. These rules deliberately do not chase the cheapest path after every strike.

## Applying a preference

The existing activity director still selects the category and constructs its normal ambient target/recipe first. The directive uses a separately salted random generator (`seed XOR 0x41D1CE73`). SPREAD performs no directive random draws and returns the original target. BREACH considers only legal, uncompromised internal candidates on its committed path. At eligible opportunities, the 70% preferred branch samples with weight `1 / remaining Integrity`: a one-point node has twice the weight of a two-point node, with no guaranteed finishing strike. The remaining 30% uses the original ambient target. The unchanged ambient branch can still choose a compromised node; the preferred branch excludes it. Unsupported and ineligible opportunities are never rerolled or replaced.

| Activity | Preference seam |
|---|---|
| Ordinary hostile connection | Fully sample the existing weighted PURPOSE, LOADER/INFECTOR objective, infection kind and complete arc; optionally copy with a validated route to the selected target. |
| Reconnaissance | Redirect the initial normal scan; store its final target in the existing recon event. Delayed follow-up keeps that target even if the directive changes. |
| Standalone Payload | Preserve the normal origin draw, profile draw, construction and lifecycle; prefer among existing reachable internal targets. |
| Direct Duel | Preserve the normal ambient target draw and existing scheduling/slot validation; optionally prefer another legal internal node. |
| Connection-triggered Duel | Redirect the fully sampled carrier, retaining its behavior and operations. |
| Payload launcher | Retain its fixed launcher destination/effect; record `fixed_launcher_destination`. No alternate recipe is substituted. |
| Worm | Retain normal propagation; record `propagating_worm`. No artificial final destination. |

`EligibleDirectiveTargets` and `RetargetHostilePlan` are small traffic-generator seams. They exclude benign plans, external targets, the original origin and plans containing a fixed Payload-launch completion effect. Retargeting preserves the original origin, connection ID, operation objects, behaviors, sampled WORK/LOAD, infection parameters, generation trace, LOADER potency, noise and identity seeds; it runs existing physical route and host plan validation. The canonical benign catalog and its destinations are unchanged. WEB receives a hostile copy of the sampled arc, never a new benign PURPOSE. The generation trace retains the original recipe sampling context; recorder target decisions plus queued definitions explain its final route.

Retargeting does not rescale a LOADER against a new target capacity; its existing sampled `targetCapacity` and numeric attack request are preserved. Current internal capacities are equal. Per-link movement timing is unchanged; a different legal route can naturally have a different total traversal duration. That is a routing consequence, not travel tuning.

## Reset and observability

The existing hostile director reset resets directive seed/state. Terminal `restart` retains the configured mode but clears its offensive and reproduces its seeded decisions. `ConfigureForDevelopment(mode, commitment, preference)` exists only in Editor/development builds and drops the current selector; Inspector changes of mode similarly take effect on the next opportunity. There is no generalized runtime management interface.

The recorder adds run settings, `hacker_offensive_selected`, `hacker_target_decision` and a snapshot field. Offensive records include objective, path, selection cost, timestamps, expiry and reassessment reason. Target records include original/final target, eligible candidates, probability roll, preferred-branch membership, actual change, offensive ID, activity and fallback reason. An eligible preference can retain an already-matching ambient target; `preferred` and `influenced` distinguish these cases. Existing activity records still report actual construction/scheduling success separately, so target preference does not imply an attack was produced or succeeded.

All of this is internal recorder/development data. No inspection formatter, scan result, label, status badge, terminal result or HUD binds it. Actual Integrity knowledge and existing presentation rules remain unchanged. Unity's inline JSON serialization can emit empty objects for null class fields; snapshot `hasOffensive` distinguishes an active path from such placeholders. The recorder schema is additively extended; existing events and causal receipts remain.

## Evidence and limits

The [review guide](../Review/ConnectionsHackerDirectivesV0/README.md) links focused checks, regressions, real Play Mode restart/inspection checks, and 24 natural fixed-seed runs. The natural policies use exactly the parent Integrity witness, including its unattended Duel handling, limited visible intercept/overclock policy, and connection kill-all policy. They receive no hidden intent.

SPREAD reproduces all twelve parent runs' gameplay events, snapshots and configuration exactly after removing the added observability and normalizing event/action sequence references. In all twelve SPREAD/BREACH comparisons, the common observed prefixes retain identical hostile opportunity schedules, category selections and category rolls. The controlled generator witness separately checks clock/category independence for 600 seconds without closing the loop through damage. Different loss times mean different total observed activity budgets.

BREACH improves unattended breach time in all four seeds, but four paired seeds do not establish statistical significance or final balance. Kill-all still survives three of four BREACH runs. Target-only WEB connections remain an exploitable tell: no ambient benign PURPOSE targets WEB, although many benign routes transit it. WEB is the only transit-only internal node under the current authored eligible-target sets. This experiment does not expand benign traffic or claim that killing WEB-targeting connections alone ensures survival.
