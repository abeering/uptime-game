# Connections — behavior and modifier foundation (Slice 3A)

**Status:** Implemented on the focused Slice 3A branch. This is a simulation foundation and deterministic witness, not a gameplay keyword or threat-profile phase.

## Ownership and terms

The **Connection Plan** is the intended sequence in `LogicalConnectionDefinition`: ordered operation definitions, completion effects, and optional behavior definitions. `ConnectionSpawnPlan` remains a different object: a concrete scheduled arrival with an ID, route, and copied plan data. Existing `basic-threat`, `delayed-threat`, and benign profiles have no modifier behaviors and retain their previous mechanics.

Each logical connection owns one `ConnectionBehaviorRuntime`. It instantiates the same small timed modifier behavior at either connection scope or operation scope. Connection-scope instances keep elapsed simulation time and active LOAD adjustments across operation transitions. Operation-scope instances begin when their operation starts and stop when it ends. A connection-scope WORK delta is rejected in this slice: whether persistent WORK belongs to a connection or a particular operation remains a design question. Operation-scoped WORK deltas are supported.

Each behavior can add one additive modifier at an authored elapsed time and optionally remove it at a later elapsed time. A zero stop time keeps it until its scope ends. The demonstration behavior is intentionally narrow; it does not implement SPIKING, OVERLOAD, conditionals, or a keyword catalog. Behavior timers advance once per fixed simulation step even if allocated LOAD is zero. A threshold reached by a prior step takes effect at the start of the next step, before allocation. An operation transition still starts the next operation on the following step.

Active modifiers are passive data with a connection-local unique ID, source behavior ID, connection or operation scope, operation index (`-1` for connection scope), and WORK/LOAD deltas. The runtime is the only owner of addition and removal. Every change appends a timestamped entry to that connection's modifier history and is also observed by the run recorder. Removing a modifier does not remove its history. No gameplay state is reconstructed from history.

For the current operation, effective required WORK is base WORK plus applicable active WORK deltas; effective requested LOAD is base LOAD plus applicable active LOAD deltas. Effective WORK must remain finite and positive; effective LOAD must remain finite and nonnegative. The host advances behaviors before its existing proportional allocator computes demand. WORK still advances only by allocated LOAD × fixed-step duration. A reduction of effective required WORK below already accomplished WORK completes through the ordinary completion path without reducing `CompletedWork`. Completion effects still fire only on normal WORK completion; `kill` stops behavior instances, removes their active modifiers, and does not fire unfinished completion effects.

Inspection, node bars, pulses, and snapshots consume the effective values through existing connection-state properties. The recorder retains base definitions, effective snapshots, operation completion facts, and attributable `modifier_added`/`modifier_removed` events. Detailed modifier history is diagnostic data; there is no player-facing `logs` command or special threat presentation.

## Deterministic review

`Tools/Connections/Run Behavior Witness` uses only explicitly queued plans with the scene's four-node topology. It checks a base 2 LOAD operation with connection +3 and operation +5 modifiers (effective 10), same-step allocation, removal back to 5 then 2, unchanged authored values, operation cleanup, persisted connection timer, proportional sharing, ordered source-attributed history, zero-allocation time advancement, a WORK requirement reduced below 8 accomplished WORK, kill cleanup, independent concurrent instances, and reset/replay. Existing Resource, Traffic, Recorder, and Geometry Witnesses cover the no-modifier baseline and retained threat behavior.

## Deferred

Connection-wide persistent WORK ownership, changes to operation sequence, multiplicative or conflicting modifiers, external reactions, more complex lifetime rules, player-facing explanations, SPIKING, and OVERLOAD remain open. The timed demonstration behavior does not establish those rules.
