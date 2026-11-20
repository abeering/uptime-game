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

`EligibleDirectiveTargets` and `RetargetHostilePlan` are small traffic-generator seams. They exclude benign plans, external targets, the original origin and plans containing a fixed Payload-launch completion effect. Retargeting preserves the original origin, connection ID, operation objects, behaviors, sampled WORK/LOAD, infection parameters, generation trace, LOADER potency, noise and identity seeds; it runs existing physical route and host plan validation. The original implementation left the catalog unchanged; the scene-only storefront addition below subsequently supplies legitimate WEB cover through that same pipeline. The generation trace retains the original recipe sampling context; recorder target decisions plus queued definitions explain its final route.

Retargeting does not rescale a LOADER against a new target capacity; its existing sampled `targetCapacity` and numeric attack request are preserved. Current internal capacities are equal. Per-link movement timing is unchanged; a different legal route can naturally have a different total traversal duration. That is a routing consequence, not travel tuning.

## Reset and observability

The existing hostile director reset resets directive seed/state. Terminal `restart` retains the configured mode but clears its offensive and reproduces its seeded decisions. `ConfigureForDevelopment(mode, commitment, preference)` exists only in Editor/development builds and drops the current selector; Inspector changes of mode similarly take effect on the next opportunity. There is no generalized runtime management interface.

The recorder adds run settings, `hacker_offensive_selected`, `hacker_target_decision` and a snapshot field. Offensive records include objective, path, selection cost, timestamps, expiry and reassessment reason. Target records include original/final target, eligible candidates, probability roll, preferred-branch membership, actual change, offensive ID, activity and fallback reason. An eligible preference can retain an already-matching ambient target; `preferred` and `influenced` distinguish these cases. Existing activity records still report actual construction/scheduling success separately, so target preference does not imply an attack was produced or succeeded.

All of this is internal recorder/development data. No inspection formatter, scan result, label, status badge, terminal result or HUD binds it. Actual Integrity knowledge and existing presentation rules remain unchanged. Unity's inline JSON serialization can emit empty objects for null class fields; snapshot `hasOffensive` distinguishes an active path from such placeholders. The recorder schema is additively extended; existing events and causal receipts remain.

## Evidence and limits

The [review guide](../Review/ConnectionsHackerDirectivesV0/README.md) links focused checks, regressions, real Play Mode restart/inspection checks, and 24 natural fixed-seed runs. The natural policies use exactly the parent Integrity witness, including its unattended Duel handling, limited visible intercept/overclock policy, and connection kill-all policy. They receive no hidden intent.

Before the storefront addition, SPREAD reproduced all twelve parent runs' gameplay events, snapshots and configuration exactly after removing the added observability and normalizing event/action sequence references. In all twelve original SPREAD/BREACH comparisons, the common observed prefixes retained identical hostile opportunity schedules, category selections and category rolls. Those evidence files are preserved as the pre-patch baseline. The controlled generator witness separately checks clock/category independence for 600 seconds without closing the loop through damage. Different loss times mean different total observed activity budgets.

The pre-storefront BREACH experiment improved unattended breach time in all four seeds, but four paired seeds do not establish statistical significance or final balance. Kill-all survived three of four BREACH runs. That baseline identified WEB as the only internal node with no legitimate ambient destination recipe, making direct WEB targets an exploitable tell. The follow-up below addresses that content gap; the original run outcomes are historical evidence, not measurements of the updated catalog.

## Scene-only legitimate WEB traffic follow-up

The Integrity scene's serialized profile array adds `storefront-page` / **Storefront Page Requests**, targeting WEB with ambient weight **1.0** and `purposeTemplate` enabled. Its normal arc is READ (retrieving page resources, LOAD 0.5–1.1, mean WORK 24) then EXECUTE (rendering/serving content, LOAD 0.8–1.6, mean WORK 36). WORK retains independent ±25% sampling, natural LOAD variation and existing productive allocation. Twelve operation-local activity milestones use ordinary storefront request/resource/render/serve vocabulary, including a plausible cache miss/retry.

This is an ordinary canonical-style template, not a benign-only traffic class. The existing weighted catalog selects it for benign coverage, ordinary LOADERs and ordinary INFECTORs with their unchanged objective probabilities, anomaly composition, infection payloads, behavior and lifecycle. A benign `isHostile: false` template does not suppress the existing hostile PURPOSE generation path. The six existing recipes, effective destinations and weights remain identical. Their 5.25 combined weight plus the new 1.0 gives the storefront template a provisional 16% catalog share; no clock, population goal, coverage rule, origin weight, topology, allocator or damage rule changes.

BREACH still fully samples an ordinary hostile recipe before applying its target preference. Sampling `storefront-page` can therefore produce a matching-purpose WEB attack without any special recipe replacement or directive RNG change. Other sampled purposes can still be retargeted to WEB, and storefront cover can be retargeted elsewhere. This patch deliberately retains that variety; it removes certainty from WEB destination and storefront claim alone, rather than making every destination/claim pairing equally plausible. Recon's existing matching-purpose follow-up can also select the new template naturally.

Only `ConnectionsAdvancedIntegrity.unity` gains this recipe; built-in defaults and all other scenes remain unchanged. A changed catalog changes recipe draws, contention, coverage feedback and seeded outcomes relative to the old catalog. “SPREAD control” now means unchanged ambient behavior with the **new configured catalog**, not replay equivalence with the six-recipe parent. Spawn processes and tuning remain unchanged; this is population redistribution, not an additional producer. See [storefront comparison, natural examples and checks](../Review/ConnectionsHackerDirectivesV0/StorefrontPatch/README.md). Original directive evidence remains intact in its original folders.
