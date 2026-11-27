# Connections — API Configuration V0

Connections implementation from verified main `79e2d90c5b88bf2fd04aa5ac0c5b85a60f361718`, the normal GitHub merge of human-accepted #135. This implements the first API policy proposed in [Business Operations & Reliability (#128)](../Design/Uptime_Design.md#2-node-configuration-can-change-passing-traffic). Packet is unchanged.

**Accepted follow-up:** Human playtesting accepted #136, merged normally at `6ecb3ce`. [Operation Routing & CACHE V0](connections-operation-routing-cache-v0.md) now supplies actual committed per-operation routes for API eligibility and reuses this authored SSH window for CACHE. Live allocation priority remains independent of locked route/efficiency decisions. The original V0 deferrals below describe that phase, not a prohibition on its authorized follow-up.

## One persistent PURPOSE slot

`ssh api` opens the shared authored `ConnectionsSshOverlay` prefab in either canonical scene. Arrows choose a row, Enter applies it, and coordinated Escape closes the window and returns command focus. The `NONE / CLEAR PRIORITY` row clears the setting. Mouse selection also works. The panel shows API, current policy, weight and known PURPOSE choices. Simulation continues during configuration; a Duel takes exclusive input and restores SSH focus afterward. Reset and shift endings abort the window safely.

The slot starts empty, persists after closing SSH, and clears on restart. Choose from canonical `purposeTemplate` profile IDs; display labels are descriptions, not identities. A policy applies to an existing or newly established connection only if its stable claimed PURPOSE ID matches and its route contains the authored API node. Connections targeting API qualify too. Matching hostile traffic benefits equally; this policy has no knowledge of hidden intent. Unsupported nodes return promptly without opening a configuration window.

Changing or clearing the slot takes effect at the next ordinary allocation update. Inspect's compact and verbose PURPOSE header prefixes `API ×2 ·` when the state carries an applied benefit. This describes allocation policy, not legitimacy or company importance. Resolved history retains its last applied policy. Rail colors, heartbeat, path geometry and progress art are unchanged.

## Weighted capped proportional allocation

Select **SimulationHost → Simulation Host → API configuration: one PURPOSE priority slot → API Allocation Weight**. Both scenes author **2** and reference their actual API node. The weight must be finite and at least 1. Restart after Inspector edits so run metadata describes the configuration used.

For each target, preserve reservation order: baseline, infection, defense, then productive connections. Divide remaining capacity in proportion to each effective requested LOAD multiplied by its applied weight. Cap each allocation at the actual effective request, removing capped requests and redistributing the excess among remaining requests. Separate targets remain independent. With every weight 1, the original allocator arithmetic is retained exactly.

With 10 available capacity, two requests of 8 receive 5/5 without policy; favoring one gives approximately 6.667/3.333. If the favored job requests 1 and its peer 4 against 4.5 capacity, allocations are 1/3.5. Priority changes resource distribution, never capacity, requested LOAD, processing WORK, business WORK credit, nominal duration, service budget or overload thresholds. Transit API adds policy eligibility, not a new transit resource cost.

This is operator-configured class priority. Future contextual business importance belongs to an individual request and its circumstances, and remains unimplemented. No CACHE, dynamic routing, other node policy, extra slot, event system or investigation redesign is added.

## Recording and retained tuning

Optional schema 4 additions:

- Run metadata: `apiConfigurationNodeId`, `apiAllocationWeight`.
- `api_policy_changed`: policy node, previous/new canonical PURPOSE IDs and configured weight, with simulation time. Reapplying the same setting produces no duplicate change event.
- Network snapshot: selected `apiFavoredPurposeId`, configured `apiAllocationWeight`.
- Connection snapshot: applied `allocationWeight`, `allocationPolicyNodeId`, alongside existing requested/allocated LOAD and WORK fields.

The change-event snapshot can show a newly selected policy with the preceding applied allocation, because changes deliberately wait for the next update. Subsequent snapshots show applied weights. Recording never drives allocation.

Both canonical scenes retain 2.5× legitimate processing LOAD/WORK and unscaled business value. Small's mean legitimate interval remains 23.225807s. The user's existing saved Advanced tuning,20s, is preserved rather than reverting to #135's original 18.947369s. Independent arrival scheduling, hostile cadence/weights, PURPOSE distribution, capacities, shift objectives, tolerance and allocator reservation rules otherwise remain unchanged. This is no additional calibration pass.

[Review evidence and manual fixture](../Review/ConnectionsApiConfigurationV0/README.md) cover the controlled save-one/lose-one outcome, allocation witnesses, supported resolutions and known inherited failures. Human gameplay remains necessary to judge the usefulness of the policy at ordinary demand.
