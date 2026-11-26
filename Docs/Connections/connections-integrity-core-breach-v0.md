# Connections — Integrity & CORE Breach V0

**Canonical objectives follow-up:** [Business Reliability & Shift Objectives V0](connections-business-reliability-shift-v0.md) routes CORE through the common terminal outcome owner alongside FIRED and SHIFT COMPLETE, settling all same-boundary effects before choosing CORE precedence. The original experimental scene retains its own scope below; physical Integrity, breach paths and damage rules are unchanged.

**Canonical promotion:** [Canonical Scenes V0](connections-canonical-scenes-v0.md) now enables these same rules/presentation in Small and Advanced. Scene exclusions below describe the original experiment. Its Integrity scene/evidence remain unchanged historical fixtures.

Experimental implementation on `feat/connections-integrity-core-breach-v0`, stacked on Maximum Pressure PR #116. The agreed presentation and two-point revision supersede the original pasted brief's three-point/badge suggestions. This is a survival hypothesis and provisional tuning, not a settled balance contract. Living Shift PR #115 is excluded.

Open `Assets/Scenes/ConnectionsAdvancedIntegrity.unity`. The four-node Connections, Advanced, Pressure and Packet scenes retain their authored settings. Integrity is disabled by default in shared runtime and enabled only in the new scene.

## Permanent damage and incomplete knowledge

Internal nodes start at **2 Integrity, maximum 2** and become **COMPROMISED at 0**. Damage clamps at zero and lasts until restart. It causes no operational penalties or bonuses. Allocation, capacity, WORK, routing, connection participation, infection effects and diagnostic/cleaning availability retain their existing rules. Critical nodes function normally, including at zero.

Configured ambient origins are external/untrusted ingress, excluded from Integrity. DB and AUTH are critical in this scene; the model accepts an arbitrary critical-ID list and multiple external roots.

Actual Integrity and revealed knowledge are separate simulation state. A first covert infection strike remains hidden. Successful scan completion and successful PID-Select CLEAN reveal current Integrity; overload and lost-Duel strikes reveal it immediately. Once revealed, subsequent damage updates known condition. Zero is always revealed. With maximum two, a second one-point strike after hidden first damage reaches zero and reveals compromise immediately. The more general subsequent-damage announcement rule remains, but there is no still-positive intermediate value after two strikes.

CLEAN preserves damage. Existing infection capture/removal and scan timing remain intact. Exact revealed values appear in scan/CLEAN terminal receipts. No new diagnostic command is introduced.

## Damage and causal ownership

- **New infection accepted: −1.** The single authoritative acceptance path handles carriers, Payloads, Spawner children and developer injection. Duplicate active kinds are rejected without damage. Different kinds and later accepted reapplications can strike independently.
- **Sustained severe overload: −1.** Provisional scene tuning is requested LOAD **strictly above 150%** of effective capacity for **10 simulation seconds**. This reads the same published authoritative demand used by the allocator; allocated LOAD does not trigger damage. Baseline, infections, defensive demand and ordinary connections contribute through existing RequestedLoad semantics. At or below threshold, elapsed warning time resets fully. Each strike requires a fresh full window, with no retained debt. Overclock's existing effective capacity can stop/reset the warning. At zero there is no remaining Integrity to threaten, so its damage countdown stops; operational LOAD presentation continues.
- **Lost Command Race: −1 direct strike.** Execute the encounter's already frozen consequence plan as before, suppressing only its immediate infection components' Integrity damage, then apply the direct strike once with its Duel ID. Explicit per-execution provenance prevents double counting. Later independent Payloads, Spawner children and other attacks retain normal infection damage. A rejected consequence still leaves a real direct Duel strike; receipts distinguish that from a fully blocked attack.

The allocator still computes allocation once per step. The opt-in overload observer publishes that allocation's current demand before WORK completion and samples the elapsed interval. Integrity-mode coarse witness calls subdivide into ordinary fixed steps, preserving full warning windows and operation boundaries. Presentation is not a damage source.

## Graph and terminal loss

Traverse actual physical adjacency from every external ingress, admitting only compromised internal nodes. Highlight every physical link in the reached compromised region. Isolated compromised nodes remain red but do not establish an external route; even a pair of adjacent isolated compromised nodes leaves their connecting edge neutral.

Reaching a compromised critical node latches CORE BREACHED once. The deterministic breadth-first traversal uses sorted roots and edge IDs, handles cycles and branches, and retains a concrete winning path. If several strikes occur at one simulation boundary, the first event that establishes a complete path retains its path and cause; all same-boundary effects settle before freeze. The final snapshot includes the full graph, connected links, node Integrity/knowledge/overload state and causal damage event.

The host stops simulation time, generation, WORK and effect progression after loss. Direct mutation APIs and late CLEAN/Duel callbacks reject the terminal state. The result closes active CLEAN/Duel presentation, shows the critical target and winning path, and returns terminal focus. `restart` resets the same scene with the same chosen seed, clearing damage, knowledge, timers, loss state and existing sessions. Terminal history remains available.

This scene has no other run-loss condition. Existing Network Health internals and recording remain, but its value/bar/rate are hidden; the clock and tool readiness remain visible.

## Agreed visual language

No node Integrity badges, numeric totals, shields or question marks.

| State | Card treatment |
|---|---|
| Unrevealed or known 2 | Existing default appearance: no known damage |
| Known 1 | Orange accent and border; ordinary dark background |
| Compromised 0 | Permanent dark red background, red border and accent |

A successful healthy scan can leave the card unchanged: its explicit terminal result provides confirmation. Hidden first damage does not recolor an unrevealed node. CLEAN at zero removes infection status without removing the red card. Node IDs, role labels and LOAD meanings remain legible and independent.

CRITICAL and UNTRUSTED are persistent infrastructure markers, independent of damage. Physical compromised-region lines and edge IDs turn red, while logical connection identity/performance colors remain intact. Winning physical edges receive a thicker, lighter red stroke. OVERLOADED uses the existing timed Pain callout; an experiment-specific authored status prefab supports seven concurrent rows. The original shared status prefab is unchanged.

All ordinary UI additions are authored/serialized in the scene or prefab. `IntegritySceneAuthoring.Build` is a narrow reproducible Editor recipe for those two assets, not runtime UI generation or a topology framework.

## Development and review

Editor/development-only `integrityhit <internal-node>` applies one overt strike through the authoritative host. It rejects external nodes, zero Integrity and ended runs. `restart` is available in this experimental scene. No player repair or other management command is added.

See [review evidence and manual steps](../Review/ConnectionsIntegrityV0/README.md) for deterministic cases, natural seeded Pressure runs, Play Mode captures and validation caveats. Natural runs do not force damage or alter opportunity rates. Scripted visual fixtures are explicitly separate from natural evidence.

The experiment does not add progression, scores, quotas, topology generation, strategic AI, new threats, changed CLEAN/Duel rules, or compromised-node operational effects. Attack mix and ordinary service mechanics remain those of Maximum Pressure.
